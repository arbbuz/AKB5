using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public sealed class KnowledgeBaseMaintenanceMonthPlanResult
    {
        public bool IsSuccess { get; init; }

        public string ErrorMessage { get; init; } = string.Empty;

        public int WorkingDayCount { get; init; }

        public int RequestedHours { get; init; }

        public int BudgetHours { get; init; }

        public int CalendarCapacityHours { get; init; }

        public int AvailableCapacityHours { get; init; }

        public List<int> NonWorkingDayNumbers { get; init; } = new();

        public List<KbMaintenanceMonthWorkItem> PlannedWorkItems { get; init; } = new();

        public List<KbMaintenanceMonthPlanDay> PlannedDays { get; init; } = new();
    }

    public sealed class KnowledgeBaseMaintenanceMonthlyPlannerService
    {
        private const int MajorWorkSplitChunkHours = 8;
        private const int DailySystemPackingTargetHours = 16;
        private const int LargeSystemLevel3Threshold = 2;

        private readonly KnowledgeBaseRussianProductionCalendarService _calendarService;
        private readonly KnowledgeBaseMaintenanceMonthWorkResolverService _workResolverService;

        public KnowledgeBaseMaintenanceMonthlyPlannerService(
            KnowledgeBaseRussianProductionCalendarService? calendarService = null,
            KnowledgeBaseMaintenanceMonthWorkResolverService? workResolverService = null)
        {
            _calendarService = calendarService ?? new KnowledgeBaseRussianProductionCalendarService();
            _workResolverService = workResolverService ?? new KnowledgeBaseMaintenanceMonthWorkResolverService();
        }

        public KnowledgeBaseMaintenanceMonthPlanResult PlanMonth(
            int year,
            int month,
            int totalMonthlyHourBudget,
            IReadOnlyList<KbNode> roots,
            IReadOnlyList<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles)
        {
            IReadOnlyList<KbMaintenanceMonthWorkItem> workItems =
                _workResolverService.ResolveMonthWorkItems(year, month, roots, maintenanceScheduleProfiles);

            return PlanMonth(year, month, totalMonthlyHourBudget, workItems);
        }

        public KnowledgeBaseMaintenanceMonthPlanResult PlanMonth(
            int year,
            int month,
            int totalMonthlyHourBudget,
            IReadOnlyList<KbMaintenanceMonthWorkItem>? workItems)
        {
            if (totalMonthlyHourBudget < 0)
                return Failure("Месячный лимит часов не может быть отрицательным.");

            IReadOnlyList<KbMaintenanceMonthWorkItem> normalizedWorkItems = workItems ?? Array.Empty<KbMaintenanceMonthWorkItem>();
            foreach (KbMaintenanceMonthWorkItem item in normalizedWorkItems)
            {
                if (item.Hours < 0)
                {
                    return Failure(
                        $"Норма часов для узла '{item.NodeName}' не может быть отрицательной.");
                }
            }

            IReadOnlyList<DateOnly> workingDays = _calendarService.GetWorkingDays(year, month);
            List<int> nonWorkingDayNumbers = BuildNonWorkingDayNumbers(year, month, workingDays);
            int requestedHours = normalizedWorkItems.Sum(static item => Math.Max(0, item.Hours));
            int calendarCapacityHours = totalMonthlyHourBudget;
            int availableCapacityHours = totalMonthlyHourBudget;

            if (requestedHours == 0)
            {
                return Success(
                    workingDays.Count,
                    requestedHours,
                    totalMonthlyHourBudget,
                    calendarCapacityHours,
                    availableCapacityHours,
                    nonWorkingDayNumbers,
                    normalizedWorkItems,
                    new List<KbMaintenanceMonthPlanDay>());
            }

            if (workingDays.Count == 0)
            {
                return Failure(
                    $"В {month:D2}.{year} нет рабочих дней для размещения {requestedHours} ч.",
                    workingDays.Count,
                    requestedHours,
                    totalMonthlyHourBudget,
                    calendarCapacityHours,
                    availableCapacityHours,
                    nonWorkingDayNumbers,
                    normalizedWorkItems);
            }

            if (requestedHours > totalMonthlyHourBudget)
            {
                return Failure(
                    $"Невозможно разместить {requestedHours} ч в {month:D2}.{year}: месячный лимит составляет {totalMonthlyHourBudget} ч.",
                    workingDays.Count,
                    requestedHours,
                    totalMonthlyHourBudget,
                    calendarCapacityHours,
                    availableCapacityHours,
                    nonWorkingDayNumbers,
                    normalizedWorkItems);
            }

            List<DayPlanBuilder> dayBuilders = workingDays
                .Select(static date => new DayPlanBuilder(date))
                .ToList();

            int dailyPackingTargetHours = ResolveDailyPackingTargetHours(requestedHours, workingDays.Count);
            double dailyBalanceTargetHours = ResolveDailyBalanceTargetHours(requestedHours, workingDays.Count);
            IReadOnlyList<MaintenanceVisitGroup> visitGroups = BuildVisitGroups(normalizedWorkItems, dailyPackingTargetHours);
            int visitCount = visitGroups.Sum(static group => group.Visits.Count);
            int targetPlannedDayCount = ResolveTargetPlannedDayCount(
                requestedHours,
                dailyBalanceTargetHours,
                workingDays.Count,
                visitCount);
            if (!TryValidateVisitCapacity(year, month, workingDays.Count, visitGroups, out string visitCapacityError))
            {
                return Failure(
                    visitCapacityError,
                    workingDays.Count,
                    requestedHours,
                    totalMonthlyHourBudget,
                    calendarCapacityHours,
                    availableCapacityHours,
                    nonWorkingDayNumbers,
                    normalizedWorkItems);
            }

            List<MaintenanceVisitGroupScheduleState> visitGroupStates = OrderVisitGroups(visitGroups)
                .Select(static group => new MaintenanceVisitGroupScheduleState(group))
                .ToList();
            while (true)
            {
                MaintenanceVisitGroupScheduleState? selectedVisitGroupState = SelectNextVisitGroupState(
                    visitGroupStates,
                    dayBuilders,
                    dailyPackingTargetHours);
                if (selectedVisitGroupState == null)
                    break;

                MaintenanceVisitPlan visitPlan = selectedVisitGroupState.NextVisit!;
                DayPlanBuilder? selectedDay = SelectBestDay(
                    dayBuilders,
                    visitPlan,
                    selectedVisitGroupState.PreviousDay,
                    dailyBalanceTargetHours,
                    dailyPackingTargetHours,
                    targetPlannedDayCount);
                if (selectedDay == null)
                {
                    return Failure(
                        BuildNoFeasibleDayError(year, month, visitPlan, workingDays.Count),
                        workingDays.Count,
                        requestedHours,
                        totalMonthlyHourBudget,
                        calendarCapacityHours,
                        availableCapacityHours,
                        nonWorkingDayNumbers,
                        normalizedWorkItems);
                }

                selectedDay.AddVisit(visitPlan);
                selectedVisitGroupState.MarkScheduled(selectedDay);
            }

            RebalanceDayLoads(dayBuilders, dailyBalanceTargetHours, dailyPackingTargetHours);

            static MaintenanceVisitGroupScheduleState? SelectNextVisitGroupState(
                IReadOnlyList<MaintenanceVisitGroupScheduleState> visitGroupStates,
                IReadOnlyList<DayPlanBuilder> dayBuilders,
                int dailyLoadLimitHours)
            {
                return visitGroupStates
                    .Where(static state => state.NextVisit != null)
                    .Select(state =>
                    {
                        MaintenanceVisitPlan visitPlan = state.NextVisit!;
                        int feasibleDayCount = dayBuilders.Count(day => CanPlaceVisitOnDayStrict(
                            day,
                            visitPlan,
                            dailyLoadLimitHours));
                        return new
                        {
                            State = state,
                            Visit = visitPlan,
                            FeasibleDayCount = feasibleDayCount
                        };
                    })
                    .OrderBy(static item => item.FeasibleDayCount)
                    .ThenBy(static item => item.Visit.IsLargeSystem ? 0 : 1)
                    .ThenByDescending(static item => item.Visit.TotalHours)
                    .ThenBy(static item => item.State.GroupOrderIndex)
                    .Select(static item => item.State)
                    .FirstOrDefault();
            }

            static string BuildNoFeasibleDayError(
                int year,
                int month,
                MaintenanceVisitPlan visitPlan,
                int workingDayCount) =>
                $"Невозможно сформировать график ТО за {month:D2}.{year} без перегруза смены или повторного ТО по одному объекту в один день: " +
                $"для '{visitPlan.SchedulingConflictName}' нет подходящего рабочего дня из {workingDayCount}.";

            List<KbMaintenanceMonthPlanDay> plannedDays = dayBuilders
                .Where(static day => day.Assignments.Count > 0)
                .Select(static day => day.ToPlanDay())
                .ToList();

            return Success(
                workingDays.Count,
                requestedHours,
                totalMonthlyHourBudget,
                calendarCapacityHours,
                availableCapacityHours,
                nonWorkingDayNumbers,
                normalizedWorkItems,
                plannedDays);
        }

        private static IEnumerable<KbMaintenanceMonthWorkItem> OrderWorkItems(
            IReadOnlyList<KbMaintenanceMonthWorkItem> workItems) =>
            workItems
                .OrderBy(static item => GetWorkPriority(item.WorkKind))
                .ThenBy(static item => GetSystemPreorderSortKey(item))
                .ThenBy(static item => GetOwnerPreorderSortKey(item))
                .ThenByDescending(static item => item.Hours)
                .ThenBy(static item => item.OwnerNodeId, StringComparer.Ordinal)
                .ThenBy(static item => item.NodeName, StringComparer.Ordinal);

        private static IReadOnlyList<MaintenanceVisitGroup> BuildVisitGroups(
            IReadOnlyList<KbMaintenanceMonthWorkItem> workItems,
            int dailyPackingTargetHours)
        {
            List<WorkAssignmentDraft> assignmentDrafts = OrderWorkItems(workItems)
                .SelectMany(workItem => CreateAssignmentDrafts(workItem, dailyPackingTargetHours))
                .ToList();
            var visitGroups = new List<MaintenanceVisitGroup>();
            int nextVisitGroupOrderIndex = 0;
            int nextVisitOrderIndex = 0;

            foreach (IGrouping<string, WorkAssignmentDraft> systemGroup in assignmentDrafts
                .Where(static assignment => !string.IsNullOrWhiteSpace(assignment.SystemNodeId))
                .GroupBy(static assignment => assignment.SystemNodeId, StringComparer.Ordinal)
                .OrderBy(static group => group.Min(static assignment => assignment.SystemPreorderIndex))
                .ThenBy(static group => group.Key, StringComparer.Ordinal))
            {
                IReadOnlyList<MaintenanceVisitPlan> visits = PackSystemAssignments(
                    systemGroup,
                    dailyPackingTargetHours,
                    ref nextVisitOrderIndex);
                visitGroups.Add(new MaintenanceVisitGroup(nextVisitGroupOrderIndex++, visits));
            }

            foreach (IGrouping<string, WorkAssignmentDraft> ownerGroup in assignmentDrafts
                .Where(static assignment => string.IsNullOrWhiteSpace(assignment.SystemNodeId))
                .GroupBy(static assignment => assignment.OwnerNodeId, StringComparer.Ordinal)
                .OrderBy(static group => group.Min(static assignment => assignment.OwnerPreorderIndex))
                .ThenBy(static group => group.Key, StringComparer.Ordinal))
            {
                IReadOnlyList<MaintenanceVisitPlan> visits = PackSystemAssignments(
                    ownerGroup,
                    dailyPackingTargetHours,
                    ref nextVisitOrderIndex);
                visitGroups.Add(new MaintenanceVisitGroup(nextVisitGroupOrderIndex++, visits));
            }

            return visitGroups;
        }

        private static IEnumerable<WorkAssignmentDraft> CreateAssignmentDrafts(
            KbMaintenanceMonthWorkItem workItem,
            int dailyPackingTargetHours)
        {
            if (workItem.Hours <= 0)
                yield break;

            foreach (int assignmentHours in SplitWorkItemHours(workItem, dailyPackingTargetHours))
            {
                yield return new WorkAssignmentDraft(
                    OwnerNodeId: workItem.OwnerNodeId?.Trim() ?? string.Empty,
                    NodeName: workItem.NodeName?.Trim() ?? string.Empty,
                    SystemNodeId: workItem.SystemNodeId?.Trim() ?? string.Empty,
                    SystemPreorderIndex: GetSystemPreorderSortKey(workItem),
                    OwnerPreorderIndex: GetOwnerPreorderSortKey(workItem),
                    SystemLevel3NodeCount: Math.Max(0, workItem.SystemLevel3NodeCount),
                    WorkKind: workItem.WorkKind,
                    Hours: assignmentHours);
            }
        }

        private static IEnumerable<WorkAssignmentDraft> OrderSystemAssignments(
            IEnumerable<WorkAssignmentDraft> assignments) =>
            assignments
                .OrderByDescending(static item => item.Hours)
                .ThenBy(static item => GetWorkPriority(item.WorkKind))
                .ThenBy(static item => item.OwnerPreorderIndex)
                .ThenBy(static item => item.OwnerNodeId, StringComparer.Ordinal)
                .ThenBy(static item => item.NodeName, StringComparer.Ordinal);

        private static IReadOnlyList<MaintenanceVisitPlan> PackSystemAssignments(
            IEnumerable<WorkAssignmentDraft> assignments,
            int dailyPackingTargetHours,
            ref int nextVisitOrderIndex)
        {
            var visitBuilders = new List<MaintenanceVisitBuilder>();
            foreach (WorkAssignmentDraft assignment in OrderSystemAssignments(assignments))
            {
                MaintenanceVisitBuilder? bestFitVisit = visitBuilders
                    .Where(visit => visit.CanAdd(assignment, dailyPackingTargetHours))
                    .OrderBy(visit => dailyPackingTargetHours - (visit.TotalHours + assignment.Hours))
                    .ThenBy(visit => visit.OrderIndex)
                    .FirstOrDefault();

                if (bestFitVisit == null)
                {
                    bestFitVisit = new MaintenanceVisitBuilder(visitBuilders.Count);
                    visitBuilders.Add(bestFitVisit);
                }

                bestFitVisit.Add(assignment);
            }

            var result = new List<MaintenanceVisitPlan>();
            foreach (MaintenanceVisitBuilder visit in visitBuilders.OrderBy(static visit => visit.OrderIndex))
            {
                result.Add(new MaintenanceVisitPlan(
                    nextVisitOrderIndex++,
                    visit.Assignments
                        .OrderBy(static assignment => GetWorkPriority(assignment.WorkKind))
                        .ThenBy(static assignment => assignment.OwnerPreorderIndex)
                        .ThenByDescending(static assignment => assignment.Hours)
                        .ThenBy(static assignment => assignment.OwnerNodeId, StringComparer.Ordinal)
                        .ThenBy(static assignment => assignment.NodeName, StringComparer.Ordinal)));
            }

            return result;
        }

        private static IEnumerable<MaintenanceVisitGroup> OrderVisitGroups(
            IReadOnlyList<MaintenanceVisitGroup> visitGroups) =>
            visitGroups
                .OrderBy(static group => group.IsLargeSystem ? 0 : 1)
                .ThenBy(static group => group.SystemPreorderIndex)
                .ThenBy(static group => group.OrderIndex);

        private static IEnumerable<int> SplitWorkItemHours(
            KbMaintenanceMonthWorkItem workItem,
            int dailyPackingTargetHours)
        {
            int hours = Math.Max(0, workItem.Hours);
            int lightWorkSplitChunkHours = Math.Max(1, dailyPackingTargetHours);
            if (!IsMajorWork(workItem.WorkKind) && hours <= lightWorkSplitChunkHours)
            {
                yield return hours;
                yield break;
            }

            if (IsMajorWork(workItem.WorkKind) && hours <= MajorWorkSplitChunkHours)
            {
                yield return hours;
                yield break;
            }

            int splitChunkHours = IsMajorWork(workItem.WorkKind)
                ? MajorWorkSplitChunkHours
                : lightWorkSplitChunkHours;
            int remainingHours = hours;
            while (remainingHours > 0)
            {
                int assignmentHours = Math.Min(splitChunkHours, remainingHours);
                yield return assignmentHours;
                remainingHours -= assignmentHours;
            }
        }

        private static DayPlanBuilder? SelectBestDay(
            List<DayPlanBuilder> dayBuilders,
            MaintenanceVisitPlan visitPlan,
            DayPlanBuilder? previousGroupDay,
            double dailyBalanceTargetHours,
            int dailyLoadLimitHours,
            int targetPlannedDayCount)
        {
            List<DayPlanBuilder> candidates = dayBuilders
                .Where(day => CanPlaceVisitOnDayFallback(day, visitPlan))
                .ToList();
            if (candidates.Count == 0)
                return null;

            return candidates
                .Select(day => BuildDayCandidateScore(
                    dayBuilders,
                    day,
                    visitPlan,
                    previousGroupDay,
                    dailyBalanceTargetHours,
                    dailyLoadLimitHours,
                    targetPlannedDayCount))
                .OrderBy(static score => score.LargeSystemConflictRank)
                .ThenBy(static score => score.ProjectedLimitOverflowHours)
                .ThenBy(static score => score.TargetOccupancyRank)
                .ThenBy(static score => score.ContinuationRank)
                .ThenBy(static score => score.ContinuationDistance)
                .ThenBy(static score => score.BalanceDeviation)
                .ThenBy(static score => score.CurrentHours)
                .ThenBy(static score => score.RouteFillRank)
                .ThenBy(static score => score.ProjectedHours)
                .ThenBy(static score => score.SystemAdjacencyRank)
                .ThenBy(static score => score.OwnerAdjacencyRank)
                .ThenBy(static score => score.Day.Date)
                .Select(static score => score.Day)
                .First();
        }

        private static DayCandidateScore BuildDayCandidateScore(
            IReadOnlyList<DayPlanBuilder> dayBuilders,
            DayPlanBuilder day,
            MaintenanceVisitPlan visitPlan,
            DayPlanBuilder? previousGroupDay,
            double dailyBalanceTargetHours,
            int dailyLoadLimitHours,
            int targetPlannedDayCount)
        {
            int projectedHours = day.TotalHours + visitPlan.TotalHours;
            int dayIndex = GetDayIndex(dayBuilders, day);
            int previousGroupDayIndex = previousGroupDay == null ? -1 : GetDayIndex(dayBuilders, previousGroupDay);
            int continuationRank = previousGroupDay == null || dayIndex > previousGroupDayIndex ? 0 : 1;
            int continuationDistance = ResolveContinuationDistance(dayBuilders.Count, dayIndex, previousGroupDayIndex);
            int projectedLimitOverflowHours = Math.Max(0, projectedHours - dailyLoadLimitHours);
            int largeSystemConflictRank = ResolveLargeSystemConflictRank(day, visitPlan);
            int targetOccupancyRank = ResolveTargetOccupancyRank(dayBuilders, day, targetPlannedDayCount);
            int routeFillRank = ResolveRouteFillRank(day, visitPlan, projectedHours, dailyBalanceTargetHours);
            double balanceDeviation = Math.Abs(projectedHours - dailyBalanceTargetHours);

            return new DayCandidateScore(
                day,
                ContinuationRank: continuationRank,
                ContinuationDistance: continuationDistance,
                ProjectedLimitOverflowHours: projectedLimitOverflowHours,
                LargeSystemConflictRank: largeSystemConflictRank,
                TargetOccupancyRank: targetOccupancyRank,
                CurrentHours: day.TotalHours,
                RouteFillRank: routeFillRank,
                BalanceDeviation: balanceDeviation,
                ProjectedHours: projectedHours,
                OwnerAdjacencyRank: HasOwnerOnAdjacentWorkingDay(dayBuilders, day, visitPlan) ? 1 : 0,
                SystemAdjacencyRank: HasSystemOnAdjacentWorkingDay(dayBuilders, day, visitPlan.SystemNodeId) ? 1 : 0);
        }

        private static int ResolveRouteFillRank(
            DayPlanBuilder day,
            MaintenanceVisitPlan visitPlan,
            int projectedHours,
            double dailyBalanceTargetHours)
        {
            if (visitPlan.IsLargeSystem)
                return 0;

            bool hasWork = day.TotalHours > 0;
            bool belowTarget = day.TotalHours < dailyBalanceTargetHours;
            bool projectedWithinTarget = projectedHours <= dailyBalanceTargetHours;
            if (hasWork && belowTarget && projectedWithinTarget)
                return 0;

            if (!hasWork)
                return 1;

            if (belowTarget)
                return 2;

            return 3;
        }

        private static int ResolveTargetPlannedDayCount(
            int requestedHours,
            double dailyBalanceTargetHours,
            int workingDayCount,
            int visitCount)
        {
            if (requestedHours <= 0 || workingDayCount <= 0 || visitCount <= 0)
                return 0;

            int daysNeededByMonthlyBalance = dailyBalanceTargetHours > 0
                ? (int)Math.Ceiling(requestedHours / dailyBalanceTargetHours)
                : visitCount;

            return Math.Max(
                1,
                Math.Min(workingDayCount, Math.Min(visitCount, daysNeededByMonthlyBalance)));
        }

        private static int ResolveTargetOccupancyRank(
            IReadOnlyList<DayPlanBuilder> dayBuilders,
            DayPlanBuilder day,
            int targetPlannedDayCount)
        {
            int occupiedDayCount = dayBuilders.Count(static candidate => candidate.TotalHours > 0);
            bool dayIsEmpty = day.TotalHours == 0;
            bool needsMoreOccupiedDays = occupiedDayCount < targetPlannedDayCount;

            return needsMoreOccupiedDays
                ? dayIsEmpty ? 0 : 1
                : dayIsEmpty ? 1 : 0;
        }

        private static int ResolveContinuationDistance(
            int dayCount,
            int dayIndex,
            int previousDayIndex)
        {
            if (previousDayIndex < 0 || dayIndex < 0 || dayCount <= 0)
                return 0;

            return dayIndex > previousDayIndex
                ? dayIndex - previousDayIndex
                : dayCount + dayIndex - previousDayIndex;
        }

        private static void RebalanceDayLoads(
            IReadOnlyList<DayPlanBuilder> dayBuilders,
            double dailyBalanceTargetHours,
            int dailyLoadLimitHours)
        {
            if (dailyBalanceTargetHours <= 0 || dayBuilders.Count < 2)
                return;

            int maxMoveCount = dayBuilders.Sum(static day => day.Visits.Count) * dayBuilders.Count;
            for (int moveIndex = 0; moveIndex < maxMoveCount; moveIndex++)
            {
                DayLoadRebalanceMove? bestMove = FindBestDayLoadRebalanceMove(
                    dayBuilders,
                    dailyBalanceTargetHours,
                    dailyLoadLimitHours);
                if (bestMove == null)
                    break;

                bestMove.Source.MoveVisitTo(bestMove.Visit, bestMove.Target);
            }
        }

        private static DayLoadRebalanceMove? FindBestDayLoadRebalanceMove(
            IReadOnlyList<DayPlanBuilder> dayBuilders,
            double dailyBalanceTargetHours,
            int dailyLoadLimitHours)
        {
            DayLoadRebalanceMove? bestMove = null;
            foreach (DayPlanBuilder sourceDay in dayBuilders
                .Where(day => day.TotalHours > dailyBalanceTargetHours && day.Visits.Count > 1)
                .OrderByDescending(static day => day.TotalHours)
                .ThenBy(static day => day.Date))
            {
                foreach (ScheduledMaintenanceVisit visit in sourceDay.Visits
                    .OrderBy(static visit => visit.TotalHours)
                    .ThenBy(static visit => visit.OrderIndex))
                {
                    foreach (DayPlanBuilder targetDay in dayBuilders
                        .Where(day => !ReferenceEquals(day, sourceDay) && day.TotalHours < dailyBalanceTargetHours)
                        .OrderBy(static day => day.TotalHours)
                        .ThenBy(static day => day.Date))
                    {
                        if (!CanMoveVisitForRebalance(sourceDay, targetDay, visit, dailyLoadLimitHours))
                            continue;

                        double currentScore =
                            CalculateDayLoadBalanceScore(sourceDay.TotalHours, dailyBalanceTargetHours) +
                            CalculateDayLoadBalanceScore(targetDay.TotalHours, dailyBalanceTargetHours);
                        int projectedSourceHours = sourceDay.TotalHours - visit.TotalHours;
                        int projectedTargetHours = targetDay.TotalHours + visit.TotalHours;
                        double projectedScore =
                            CalculateDayLoadBalanceScore(projectedSourceHours, dailyBalanceTargetHours) +
                            CalculateDayLoadBalanceScore(projectedTargetHours, dailyBalanceTargetHours);
                        double improvement = currentScore - projectedScore;
                        if (improvement <= 0.0001)
                            continue;

                        var move = new DayLoadRebalanceMove(
                            sourceDay,
                            targetDay,
                            visit,
                            improvement,
                            Math.Abs(projectedTargetHours - dailyBalanceTargetHours),
                            Math.Abs(projectedSourceHours - dailyBalanceTargetHours));
                        if (bestMove == null || IsBetterRebalanceMove(move, bestMove))
                            bestMove = move;
                    }
                }
            }

            return bestMove;
        }

        private static bool IsBetterRebalanceMove(
            DayLoadRebalanceMove candidate,
            DayLoadRebalanceMove currentBest)
        {
            int improvementComparison = candidate.Improvement.CompareTo(currentBest.Improvement);
            if (improvementComparison != 0)
                return improvementComparison > 0;

            int targetDeviationComparison = candidate.TargetDeviationAfter.CompareTo(currentBest.TargetDeviationAfter);
            if (targetDeviationComparison != 0)
                return targetDeviationComparison < 0;

            int sourceDeviationComparison = candidate.SourceDeviationAfter.CompareTo(currentBest.SourceDeviationAfter);
            if (sourceDeviationComparison != 0)
                return sourceDeviationComparison < 0;

            int visitHoursComparison = candidate.Visit.TotalHours.CompareTo(currentBest.Visit.TotalHours);
            if (visitHoursComparison != 0)
                return visitHoursComparison < 0;

            int sourceDateComparison = candidate.Source.Date.CompareTo(currentBest.Source.Date);
            if (sourceDateComparison != 0)
                return sourceDateComparison < 0;

            return candidate.Target.Date < currentBest.Target.Date;
        }

        private static bool CanMoveVisitForRebalance(
            DayPlanBuilder sourceDay,
            DayPlanBuilder targetDay,
            ScheduledMaintenanceVisit visit,
            int dailyLoadLimitHours)
        {
            if (sourceDay.Visits.Count <= 1)
                return false;

            if (targetDay.TotalHours + visit.TotalHours > dailyLoadLimitHours)
                return false;

            if (visit.IsLargeSystem &&
                targetDay.LargeSystemCount > 0 &&
                !targetDay.HasLargeSystem(visit.SchedulingConflictKey))
            {
                return false;
            }

            foreach (string ownerNodeId in visit.OwnerNodeIds)
            {
                if (targetDay.HasOwner(ownerNodeId))
                    return false;
            }

            return true;
        }

        private static double CalculateDayLoadBalanceScore(
            int totalHours,
            double dailyBalanceTargetHours)
        {
            double deviation = totalHours - dailyBalanceTargetHours;
            return deviation * deviation;
        }

        private static bool TryValidateVisitCapacity(
            int year,
            int month,
            int workingDayCount,
            IReadOnlyList<MaintenanceVisitGroup> visitGroups,
            out string errorMessage)
        {
            var ownerVisitCounts = visitGroups
                .SelectMany(static group => group.Visits)
                .SelectMany(static visit => visit.OwnerNodeIds)
                .Where(static ownerNodeId => !string.IsNullOrWhiteSpace(ownerNodeId))
                .GroupBy(static ownerNodeId => ownerNodeId, StringComparer.Ordinal)
                .Select(static group => new
                {
                    OwnerNodeId = group.Key,
                    VisitCount = group.Count()
                })
                .OrderByDescending(static item => item.VisitCount)
                .ThenBy(static item => item.OwnerNodeId, StringComparer.Ordinal);
            foreach (var ownerVisitCount in ownerVisitCounts)
            {
                if (ownerVisitCount.VisitCount <= workingDayCount)
                    continue;

                errorMessage =
                    $"Невозможно сформировать график ТО за {month:D2}.{year} без двух ТО по одному объекту в один день: " +
                    $"для объекта требуется {ownerVisitCount.VisitCount} рабочих дней, доступно {workingDayCount}.";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        private static string BuildSameSystemDayConflictError(
            int year,
            int month,
            MaintenanceVisitPlan visitPlan,
            int workingDayCount) =>
            $"Невозможно сформировать график ТО за {month:D2}.{year} без двух ТО по одной системе в один день: " +
            $"для '{visitPlan.SchedulingConflictName}' нет свободного рабочего дня из {workingDayCount}.";

        private static int ResolveLargeSystemConflictRank(
            DayPlanBuilder day,
            MaintenanceVisitPlan visitPlan)
        {
            if (!visitPlan.IsLargeSystem)
                return 0;

            return day.LargeSystemCount > 0 && !day.HasLargeSystem(visitPlan.SchedulingConflictKey)
                ? 1
                : 0;
        }

        private static bool CanPlaceVisitOnDayStrict(
            DayPlanBuilder day,
            MaintenanceVisitPlan visitPlan,
            int dailyLoadLimitHours)
        {
            if (!CanPlaceVisitOnDayFallback(day, visitPlan))
                return false;

            if (ResolveLargeSystemConflictRank(day, visitPlan) > 0)
                return false;

            return day.TotalHours + visitPlan.TotalHours <= dailyLoadLimitHours;
        }

        private static bool CanPlaceVisitOnDayFallback(
            DayPlanBuilder day,
            MaintenanceVisitPlan visitPlan)
        {
            foreach (string ownerNodeId in visitPlan.OwnerNodeIds)
            {
                if (day.HasOwner(ownerNodeId))
                    return false;
            }

            return true;
        }

        private static int ResolveDailyPackingTargetHours(int totalMonthlyHourBudget, int workingDayCount)
        {
            if (workingDayCount <= 0)
                return DailySystemPackingTargetHours;

            int averageBudgetHours = (int)Math.Ceiling(totalMonthlyHourBudget / (double)workingDayCount);
            return averageBudgetHours > DailySystemPackingTargetHours
                ? averageBudgetHours + 1
                : DailySystemPackingTargetHours;
        }

        private static double ResolveDailyBalanceTargetHours(int totalMonthlyHourBudget, int workingDayCount)
        {
            if (workingDayCount <= 0)
                return DailySystemPackingTargetHours;

            return totalMonthlyHourBudget / (double)workingDayCount;
        }

        private static bool HasOwnerOnAdjacentWorkingDay(
            IReadOnlyList<DayPlanBuilder> dayBuilders,
            DayPlanBuilder day,
            string ownerNodeId)
        {
            int dayIndex = GetDayIndex(dayBuilders, day);
            if (dayIndex < 0)
                return false;

            return (dayIndex > 0 && dayBuilders[dayIndex - 1].HasOwner(ownerNodeId)) ||
                   (dayIndex < dayBuilders.Count - 1 && dayBuilders[dayIndex + 1].HasOwner(ownerNodeId));
        }

        private static bool HasOwnerOnAdjacentWorkingDay(
            IReadOnlyList<DayPlanBuilder> dayBuilders,
            DayPlanBuilder day,
            MaintenanceVisitPlan visitPlan)
        {
            foreach (string ownerNodeId in visitPlan.OwnerNodeIds)
            {
                if (HasOwnerOnAdjacentWorkingDay(dayBuilders, day, ownerNodeId))
                    return true;
            }

            return false;
        }

        private static bool HasSystemOnAdjacentWorkingDay(
            IReadOnlyList<DayPlanBuilder> dayBuilders,
            DayPlanBuilder day,
            string systemNodeId)
        {
            if (string.IsNullOrWhiteSpace(systemNodeId))
                return false;

            int dayIndex = GetDayIndex(dayBuilders, day);
            if (dayIndex < 0)
                return false;

            return (dayIndex > 0 && dayBuilders[dayIndex - 1].HasSystem(systemNodeId)) ||
                   (dayIndex < dayBuilders.Count - 1 && dayBuilders[dayIndex + 1].HasSystem(systemNodeId));
        }

        private static int GetDayIndex(
            IReadOnlyList<DayPlanBuilder> dayBuilders,
            DayPlanBuilder day)
        {
            for (int index = 0; index < dayBuilders.Count; index++)
            {
                if (ReferenceEquals(dayBuilders[index], day))
                    return index;
            }

            return -1;
        }

        private static bool IsMajorWork(KbMaintenanceWorkKind workKind) =>
            workKind is KbMaintenanceWorkKind.To2 or KbMaintenanceWorkKind.To3;

        private static int GetWorkPriority(KbMaintenanceWorkKind workKind) =>
            workKind switch
            {
                KbMaintenanceWorkKind.To3 => 0,
                KbMaintenanceWorkKind.To2 => 1,
                _ => 2
            };

        private static int GetSystemPreorderSortKey(KbMaintenanceMonthWorkItem item) =>
            HasSystemContext(item) ? item.SystemPreorderIndex : int.MaxValue;

        private static int GetOwnerPreorderSortKey(KbMaintenanceMonthWorkItem item) =>
            HasSystemContext(item) ? item.OwnerPreorderIndex : int.MaxValue;

        private static bool HasSystemContext(KbMaintenanceMonthWorkItem item) =>
            !string.IsNullOrWhiteSpace(item.SystemNodeId) &&
            item.SystemPreorderIndex != int.MaxValue &&
            item.OwnerPreorderIndex != int.MaxValue;

        private sealed record DayCandidateScore(
            DayPlanBuilder Day,
            int ContinuationRank,
            int ContinuationDistance,
            int ProjectedLimitOverflowHours,
            int LargeSystemConflictRank,
            int TargetOccupancyRank,
            int CurrentHours,
            int RouteFillRank,
            double BalanceDeviation,
            int ProjectedHours,
            int OwnerAdjacencyRank,
            int SystemAdjacencyRank);

        private sealed record DayLoadRebalanceMove(
            DayPlanBuilder Source,
            DayPlanBuilder Target,
            ScheduledMaintenanceVisit Visit,
            double Improvement,
            double TargetDeviationAfter,
            double SourceDeviationAfter);

        private sealed record WorkAssignmentDraft(
            string OwnerNodeId,
            string NodeName,
            string SystemNodeId,
            int SystemPreorderIndex,
            int OwnerPreorderIndex,
            int SystemLevel3NodeCount,
            KbMaintenanceWorkKind WorkKind,
            int Hours);

        private sealed class MaintenanceVisitBuilder
        {
            private readonly HashSet<string> _ownerNodeIds = new(StringComparer.Ordinal);

            public MaintenanceVisitBuilder(int orderIndex)
            {
                OrderIndex = orderIndex;
            }

            public int OrderIndex { get; }

            public int TotalHours { get; private set; }

            public List<WorkAssignmentDraft> Assignments { get; } = new();

            public bool CanAdd(WorkAssignmentDraft assignment, int dailyPackingTargetHours) =>
                TotalHours + assignment.Hours <= dailyPackingTargetHours &&
                (string.IsNullOrWhiteSpace(assignment.OwnerNodeId) || !_ownerNodeIds.Contains(assignment.OwnerNodeId));

            public void Add(WorkAssignmentDraft assignment)
            {
                Assignments.Add(assignment);
                TotalHours += assignment.Hours;
                if (!string.IsNullOrWhiteSpace(assignment.OwnerNodeId))
                    _ownerNodeIds.Add(assignment.OwnerNodeId);
            }
        }

        private sealed class MaintenanceVisitGroup
        {
            public MaintenanceVisitGroup(int orderIndex, IReadOnlyList<MaintenanceVisitPlan> visits)
            {
                OrderIndex = orderIndex;
                Visits = visits;
                SchedulingConflictKey = visits
                    .Select(static visit => visit.SchedulingConflictKey)
                    .FirstOrDefault(static key => !string.IsNullOrWhiteSpace(key))
                    ?? string.Empty;
                SchedulingConflictName = visits
                    .Select(static visit => visit.SchedulingConflictName)
                    .FirstOrDefault(static name => !string.IsNullOrWhiteSpace(name))
                    ?? SchedulingConflictKey;
                SystemPreorderIndex = visits
                    .Where(static visit => visit.SystemPreorderIndex != int.MaxValue)
                    .Select(static visit => visit.SystemPreorderIndex)
                    .DefaultIfEmpty(int.MaxValue)
                    .Min();
                IsLargeSystem = visits.Any(static visit => visit.IsLargeSystem);
            }

            public int OrderIndex { get; }

            public IReadOnlyList<MaintenanceVisitPlan> Visits { get; }

            public string SchedulingConflictKey { get; }

            public string SchedulingConflictName { get; }

            public int SystemPreorderIndex { get; }

            public bool IsLargeSystem { get; }
        }

        private sealed class MaintenanceVisitGroupScheduleState
        {
            public MaintenanceVisitGroupScheduleState(MaintenanceVisitGroup group)
            {
                Group = group;
            }

            private MaintenanceVisitGroup Group { get; }

            public int GroupOrderIndex => Group.OrderIndex;

            public DayPlanBuilder? PreviousDay { get; private set; }

            private int NextVisitIndex { get; set; }

            public MaintenanceVisitPlan? NextVisit =>
                NextVisitIndex < Group.Visits.Count ? Group.Visits[NextVisitIndex] : null;

            public void MarkScheduled(DayPlanBuilder day)
            {
                PreviousDay = day;
                NextVisitIndex++;
            }
        }

        private sealed class MaintenanceVisitPlan
        {
            public MaintenanceVisitPlan(int orderIndex, IEnumerable<WorkAssignmentDraft> assignments)
            {
                OrderIndex = orderIndex;
                Assignments = assignments.ToList();
                TotalHours = Assignments.Sum(static assignment => assignment.Hours);
                SystemNodeId = Assignments
                    .Select(static assignment => assignment.SystemNodeId)
                    .FirstOrDefault(static systemNodeId => !string.IsNullOrWhiteSpace(systemNodeId))
                    ?? string.Empty;
                SystemPreorderIndex = Assignments
                    .Where(static assignment => assignment.SystemPreorderIndex != int.MaxValue)
                    .Select(static assignment => assignment.SystemPreorderIndex)
                    .DefaultIfEmpty(int.MaxValue)
                    .Min();
                SystemLevel3NodeCount = Assignments
                    .Select(static assignment => assignment.SystemLevel3NodeCount)
                    .DefaultIfEmpty(0)
                    .Max();
                OwnerNodeIds = Assignments
                    .Select(static assignment => assignment.OwnerNodeId)
                    .Where(static ownerNodeId => !string.IsNullOrWhiteSpace(ownerNodeId))
                    .Distinct(StringComparer.Ordinal)
                    .ToList();
                IsLargeSystem = SystemLevel3NodeCount > LargeSystemLevel3Threshold ||
                                (SystemLevel3NodeCount == 0 && OwnerNodeIds.Count > LargeSystemLevel3Threshold);
                SchedulingConflictKey = !string.IsNullOrWhiteSpace(SystemNodeId)
                    ? $"system:{SystemNodeId}"
                    : OwnerNodeIds
                        .Select(static ownerNodeId => $"owner:{ownerNodeId}")
                        .FirstOrDefault()
                        ?? string.Empty;
                SchedulingConflictName = Assignments
                    .Select(static assignment => assignment.NodeName)
                    .FirstOrDefault(static nodeName => !string.IsNullOrWhiteSpace(nodeName))
                    ?? SchedulingConflictKey;
            }

            public int OrderIndex { get; }

            public List<WorkAssignmentDraft> Assignments { get; }

            public int TotalHours { get; }

            public string SystemNodeId { get; }

            public int SystemPreorderIndex { get; }

            public int SystemLevel3NodeCount { get; }

            public bool IsLargeSystem { get; }

            public IReadOnlyList<string> OwnerNodeIds { get; }

            public string SchedulingConflictKey { get; }

            public string SchedulingConflictName { get; }
        }

        private sealed class ScheduledMaintenanceVisit
        {
            private ScheduledMaintenanceVisit(
                int orderIndex,
                IReadOnlyList<KbMaintenanceMonthPlanAssignment> assignments,
                List<string> ownerNodeIds,
                int totalHours,
                bool isLargeSystem,
                string schedulingConflictKey)
            {
                OrderIndex = orderIndex;
                Assignments = assignments;
                OwnerNodeIds = ownerNodeIds;
                TotalHours = totalHours;
                IsLargeSystem = isLargeSystem;
                SchedulingConflictKey = schedulingConflictKey;
            }

            public int OrderIndex { get; }

            public IReadOnlyList<KbMaintenanceMonthPlanAssignment> Assignments { get; }

            public List<string> OwnerNodeIds { get; }

            public int TotalHours { get; }

            public bool IsLargeSystem { get; }

            public string SchedulingConflictKey { get; }

            public static ScheduledMaintenanceVisit Create(
                MaintenanceVisitPlan visitPlan,
                DateOnly date) =>
                new(
                    visitPlan.OrderIndex,
                    visitPlan.Assignments
                        .Select(assignmentDraft => new KbMaintenanceMonthPlanAssignment
                        {
                            Date = date,
                            OwnerNodeId = assignmentDraft.OwnerNodeId,
                            NodeName = assignmentDraft.NodeName,
                            SystemNodeId = assignmentDraft.SystemNodeId,
                            SystemLevel3NodeCount = assignmentDraft.SystemLevel3NodeCount,
                            WorkKind = assignmentDraft.WorkKind,
                            Hours = assignmentDraft.Hours
                        })
                        .ToList(),
                    visitPlan.OwnerNodeIds.ToList(),
                    visitPlan.TotalHours,
                    visitPlan.IsLargeSystem,
                    visitPlan.SchedulingConflictKey);

            public ScheduledMaintenanceVisit CloneForDate(DateOnly date) =>
                new(
                    OrderIndex,
                    Assignments
                        .Select(assignment => new KbMaintenanceMonthPlanAssignment
                        {
                            Date = date,
                            OwnerNodeId = assignment.OwnerNodeId,
                            NodeName = assignment.NodeName,
                            SystemNodeId = assignment.SystemNodeId,
                            SystemLevel3NodeCount = assignment.SystemLevel3NodeCount,
                            WorkKind = assignment.WorkKind,
                            Hours = assignment.Hours
                        })
                        .ToList(),
                    OwnerNodeIds.ToList(),
                    TotalHours,
                    IsLargeSystem,
                    SchedulingConflictKey);
        }

        private static List<int> BuildNonWorkingDayNumbers(
            int year,
            int month,
            IReadOnlyCollection<DateOnly> workingDays)
        {
            var workingDayNumbers = workingDays
                .Select(static day => day.Day)
                .ToHashSet();
            var nonWorkingDayNumbers = new List<int>();
            int daysInMonth = DateTime.DaysInMonth(year, month);
            for (int day = 1; day <= daysInMonth; day++)
            {
                if (!workingDayNumbers.Contains(day))
                    nonWorkingDayNumbers.Add(day);
            }

            return nonWorkingDayNumbers;
        }

        private static KnowledgeBaseMaintenanceMonthPlanResult Success(
            int workingDayCount,
            int requestedHours,
            int budgetHours,
            int calendarCapacityHours,
            int availableCapacityHours,
            IReadOnlyList<int> nonWorkingDayNumbers,
            IReadOnlyList<KbMaintenanceMonthWorkItem> workItems,
            List<KbMaintenanceMonthPlanDay> plannedDays) =>
            new()
            {
                IsSuccess = true,
                WorkingDayCount = workingDayCount,
                RequestedHours = requestedHours,
                BudgetHours = budgetHours,
                CalendarCapacityHours = calendarCapacityHours,
                AvailableCapacityHours = availableCapacityHours,
                NonWorkingDayNumbers = nonWorkingDayNumbers.ToList(),
                PlannedWorkItems = workItems.ToList(),
                PlannedDays = plannedDays
            };

        private static KnowledgeBaseMaintenanceMonthPlanResult Failure(
            string errorMessage,
            int workingDayCount = 0,
            int requestedHours = 0,
            int budgetHours = 0,
            int calendarCapacityHours = 0,
            int availableCapacityHours = 0,
            IReadOnlyList<int>? nonWorkingDayNumbers = null,
            IReadOnlyList<KbMaintenanceMonthWorkItem>? workItems = null) =>
            new()
            {
                IsSuccess = false,
                ErrorMessage = errorMessage,
                WorkingDayCount = workingDayCount,
                RequestedHours = requestedHours,
                BudgetHours = budgetHours,
                CalendarCapacityHours = calendarCapacityHours,
                AvailableCapacityHours = availableCapacityHours,
                NonWorkingDayNumbers = nonWorkingDayNumbers?.ToList() ?? new List<int>(),
                PlannedWorkItems = workItems?.ToList() ?? new List<KbMaintenanceMonthWorkItem>()
            };

        private sealed class DayPlanBuilder
        {
            public DayPlanBuilder(DateOnly date)
            {
                Date = date;
            }

            public DateOnly Date { get; }

            public int TotalHours { get; set; }

            public List<KbMaintenanceMonthPlanAssignment> Assignments { get; } = new();

            public List<ScheduledMaintenanceVisit> Visits { get; } = new();

            private HashSet<string> OwnerNodeIds { get; } = new(StringComparer.Ordinal);

            private HashSet<string> SchedulingConflictKeys { get; } = new(StringComparer.Ordinal);

            private HashSet<string> LargeSystemKeys { get; } = new(StringComparer.Ordinal);

            public int LargeSystemCount => LargeSystemKeys.Count;

            public void AddVisit(MaintenanceVisitPlan visitPlan) =>
                AddVisit(ScheduledMaintenanceVisit.Create(visitPlan, Date));

            public void AddVisit(ScheduledMaintenanceVisit visit)
            {
                Visits.Add(visit);
                RebuildFromVisits();
            }

            public void MoveVisitTo(
                ScheduledMaintenanceVisit visit,
                DayPlanBuilder targetDay)
            {
                if (!Visits.Remove(visit))
                    return;

                RebuildFromVisits();
                targetDay.AddVisit(visit.CloneForDate(targetDay.Date));
            }

            private void Register(ScheduledMaintenanceVisit visit)
            {
                foreach (string ownerNodeId in visit.OwnerNodeIds)
                {
                    OwnerNodeIds.Add(ownerNodeId);
                }

                if (!string.IsNullOrWhiteSpace(visit.SchedulingConflictKey))
                {
                    SchedulingConflictKeys.Add(visit.SchedulingConflictKey);
                    if (visit.IsLargeSystem)
                        LargeSystemKeys.Add(visit.SchedulingConflictKey);
                }
            }

            private void RebuildFromVisits()
            {
                Assignments.Clear();
                OwnerNodeIds.Clear();
                SchedulingConflictKeys.Clear();
                LargeSystemKeys.Clear();
                TotalHours = 0;

                foreach (ScheduledMaintenanceVisit visit in Visits.OrderBy(static item => item.OrderIndex))
                {
                    Assignments.AddRange(visit.Assignments);
                    TotalHours += visit.TotalHours;
                    Register(visit);
                }
            }

            public bool HasOwner(string ownerNodeId) =>
                OwnerNodeIds.Contains(ownerNodeId);

            public bool HasSystem(string systemNodeId) =>
                SchedulingConflictKeys.Contains($"system:{systemNodeId}");

            public bool HasSchedulingConflict(string schedulingConflictKey) =>
                !string.IsNullOrWhiteSpace(schedulingConflictKey) &&
                SchedulingConflictKeys.Contains(schedulingConflictKey);

            public bool HasLargeSystem(string schedulingConflictKey) =>
                !string.IsNullOrWhiteSpace(schedulingConflictKey) &&
                LargeSystemKeys.Contains(schedulingConflictKey);

            public KbMaintenanceMonthPlanDay ToPlanDay() =>
                new()
                {
                    Date = Date,
                    TotalHours = TotalHours,
                    Assignments = Assignments
                };
        }
    }
}
