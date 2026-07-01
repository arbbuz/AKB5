using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public enum KnowledgeBaseMaintenancePlanningMode
    {
        Default = 0,
        BalancedV2 = 1,
        SequentialV3 = 2
    }

    public sealed class KnowledgeBaseMaintenanceMonthPlanResult
    {
        public bool IsSuccess { get; init; }

        public string ErrorMessage { get; init; } = string.Empty;

        public KnowledgeBaseMaintenancePlanningMode PlanningMode { get; init; }

        public bool UsedFallback { get; init; }

        public int DailyLoadLimitHours { get; init; }

        public int PreferredDailyLoadLimitHours { get; init; }

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
        private const int BalancedV2DailyLoadAllowanceHours = 2;
        private const int SequentialV3SoftOverrunToleranceHours = 3;
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
            IReadOnlyList<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles,
            KnowledgeBaseMaintenancePlanningMode planningMode = KnowledgeBaseMaintenancePlanningMode.Default)
        {
            IReadOnlyList<KbMaintenanceMonthWorkItem> workItems =
                _workResolverService.ResolveMonthWorkItems(year, month, roots, maintenanceScheduleProfiles);

            return PlanMonth(year, month, totalMonthlyHourBudget, workItems, planningMode);
        }

        public KnowledgeBaseMaintenanceMonthPlanResult PlanMonth(
            int year,
            int month,
            int totalMonthlyHourBudget,
            IReadOnlyList<KbMaintenanceMonthWorkItem>? workItems,
            KnowledgeBaseMaintenancePlanningMode planningMode = KnowledgeBaseMaintenancePlanningMode.Default)
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

            if (planningMode == KnowledgeBaseMaintenancePlanningMode.BalancedV2)
            {
                int balancedCapacityHours = workingDays.Count * DailySystemPackingTargetHours;
                if (requestedHours > balancedCapacityHours)
                {
                    return Failure(
                        $"Невозможно разместить {requestedHours} ч в {month:D2}.{year}: при лимите 16 ч/день доступно {balancedCapacityHours} ч.",
                        workingDays.Count,
                        requestedHours,
                        totalMonthlyHourBudget,
                        calendarCapacityHours,
                        balancedCapacityHours,
                        nonWorkingDayNumbers,
                        normalizedWorkItems,
                        planningMode,
                        usedFallback: true,
                        DailySystemPackingTargetHours,
                        ResolvePreferredDailyLoadLimitHours(planningMode, requestedHours, workingDays.Count));
                }
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

            if (requestedHours > totalMonthlyHourBudget &&
                planningMode != KnowledgeBaseMaintenancePlanningMode.SequentialV3)
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

            if (planningMode == KnowledgeBaseMaintenancePlanningMode.SequentialV3)
            {
                return PlanSequentialV3(
                    year,
                    month,
                    workingDays,
                    requestedHours,
                    totalMonthlyHourBudget,
                    calendarCapacityHours,
                    availableCapacityHours,
                    nonWorkingDayNumbers,
                    normalizedWorkItems);
            }

            IReadOnlyList<MaintenancePlanningAttempt> planningAttempts = BuildPlanningAttempts(
                planningMode,
                requestedHours,
                workingDays.Count);
            KnowledgeBaseMaintenanceMonthPlanResult? lastFailure = null;
            foreach (MaintenancePlanningAttempt planningAttempt in planningAttempts)
            {
                List<DayPlanBuilder> dayBuilders = workingDays
                    .Select(static date => new DayPlanBuilder(date))
                    .ToList();

                int dailyPackingTargetHours = planningAttempt.DailyLoadLimitHours;
                double dailyBalanceTargetHours = ResolveDailyBalanceTargetHours(requestedHours, workingDays.Count);
                IReadOnlyList<MaintenanceVisitGroup> visitGroups = BuildVisitGroups(
                    normalizedWorkItems,
                    planningAttempt.VisitSplitTargetHours,
                    planningMode == KnowledgeBaseMaintenancePlanningMode.Default);
                int visitCount = visitGroups.Sum(static group => group.Visits.Count);
                int targetPlannedDayCount = ResolveTargetPlannedDayCount(
                    requestedHours,
                    dailyBalanceTargetHours,
                    workingDays.Count,
                    visitCount);
                if (!TryValidateVisitCapacity(
                    year,
                    month,
                    workingDays.Count,
                    visitGroups,
                    planningAttempt.AllowOwnerRepeat,
                    out string visitCapacityError))
                {
                    lastFailure = Failure(
                        visitCapacityError,
                        workingDays.Count,
                        requestedHours,
                        totalMonthlyHourBudget,
                        calendarCapacityHours,
                        availableCapacityHours,
                        nonWorkingDayNumbers,
                        normalizedWorkItems,
                        planningMode,
                        planningAttempt.UsedFallback,
                        planningAttempt.DailyLoadLimitHours,
                        planningAttempt.PreferredDailyLoadLimitHours);
                    continue;
                }

                bool planningAttemptFailed = false;
                bool preserveSequentialOrder = planningMode == KnowledgeBaseMaintenancePlanningMode.SequentialV3;
                DayPlanBuilder? sequentialPreviousDay = null;
                List<MaintenanceVisitGroupScheduleState> visitGroupStates = OrderVisitGroups(visitGroups)
                    .Select(static group => new MaintenanceVisitGroupScheduleState(group))
                    .ToList();
                while (true)
                {
                    MaintenanceVisitGroupScheduleState? selectedVisitGroupState = SelectNextVisitGroupState(
                        visitGroupStates,
                        dayBuilders,
                        dailyPackingTargetHours,
                        preserveSequentialOrder);
                    if (selectedVisitGroupState == null)
                        break;

                    if (preserveSequentialOrder)
                    {
                        TrySplitSequentialVisitToSoftTarget(
                            selectedVisitGroupState,
                            sequentialPreviousDay,
                            dailyBalanceTargetHours,
                            dailyPackingTargetHours,
                            planningAttempt.AllowOwnerRepeat,
                            planningAttempt.AllowLargeSystemConflict);
                    }

                    MaintenanceVisitPlan visitPlan = selectedVisitGroupState.NextVisit!;
                    DayPlanBuilder? selectedDay = SelectBestDay(
                        dayBuilders,
                        visitPlan,
                        preserveSequentialOrder ? sequentialPreviousDay : selectedVisitGroupState.PreviousDay,
                        dailyBalanceTargetHours,
                        dailyPackingTargetHours,
                        targetPlannedDayCount,
                        planningAttempt.AllowDailyLimitOverflow,
                        planningMode == KnowledgeBaseMaintenancePlanningMode.BalancedV2,
                        preserveSequentialOrder,
                        planningAttempt.AllowOwnerRepeat,
                        planningAttempt.AllowLargeSystemConflict);
                    if (selectedDay == null)
                    {
                        lastFailure = Failure(
                            BuildNoFeasibleDayError(year, month, visitPlan, workingDays.Count, dailyPackingTargetHours),
                            workingDays.Count,
                            requestedHours,
                            totalMonthlyHourBudget,
                            calendarCapacityHours,
                            availableCapacityHours,
                            nonWorkingDayNumbers,
                            normalizedWorkItems,
                            planningMode,
                            planningAttempt.UsedFallback,
                            planningAttempt.DailyLoadLimitHours,
                            planningAttempt.PreferredDailyLoadLimitHours);
                        planningAttemptFailed = true;
                        break;
                    }

                    selectedDay.AddVisit(visitPlan);
                    selectedVisitGroupState.MarkScheduled(selectedDay);
                    if (preserveSequentialOrder)
                        sequentialPreviousDay = selectedDay;
                }

                if (planningAttemptFailed)
                    continue;

                if (planningMode != KnowledgeBaseMaintenancePlanningMode.SequentialV3)
                {
                    RebalanceDayLoads(
                        dayBuilders,
                        dailyBalanceTargetHours,
                        dailyPackingTargetHours,
                        planningMode == KnowledgeBaseMaintenancePlanningMode.BalancedV2);
                }

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
                    plannedDays,
                    planningMode,
                    planningAttempt.UsedFallback,
                    planningAttempt.DailyLoadLimitHours,
                    planningAttempt.PreferredDailyLoadLimitHours);
            }

            return lastFailure ?? Failure(
                $"Невозможно сформировать график ТО за {month:D2}.{year}.",
                workingDays.Count,
                requestedHours,
                totalMonthlyHourBudget,
                calendarCapacityHours,
                availableCapacityHours,
                nonWorkingDayNumbers,
                normalizedWorkItems,
                planningMode,
                usedFallback: planningAttempts.LastOrDefault()?.UsedFallback ?? false,
                dailyLoadLimitHours: planningAttempts.LastOrDefault()?.DailyLoadLimitHours ?? 0,
                preferredDailyLoadLimitHours: planningAttempts.FirstOrDefault()?.PreferredDailyLoadLimitHours ?? 0);

            static MaintenanceVisitGroupScheduleState? SelectNextVisitGroupState(
                IReadOnlyList<MaintenanceVisitGroupScheduleState> visitGroupStates,
                IReadOnlyList<DayPlanBuilder> dayBuilders,
                int dailyLoadLimitHours,
                bool preserveSequentialOrder)
            {
                if (preserveSequentialOrder)
                {
                    return visitGroupStates
                        .Where(static state => state.NextVisit != null)
                        .OrderBy(static state => state.GroupOrderIndex)
                        .FirstOrDefault();
                }

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
                int workingDayCount,
                int dailyLoadLimitHours) =>
                $"Невозможно сформировать график ТО за {month:D2}.{year} без перегруза более {dailyLoadLimitHours} ч/день или повторного ТО по одному объекту в один день: " +
                $"для '{visitPlan.SchedulingConflictName}' нет подходящего рабочего дня из {workingDayCount}.";
        }

        private static void TrySplitSequentialVisitToSoftTarget(
            MaintenanceVisitGroupScheduleState selectedVisitGroupState,
            DayPlanBuilder? previousDay,
            double dailyBalanceTargetHours,
            int dailyLoadLimitHours,
            bool allowOwnerRepeat,
            bool allowLargeSystemConflict)
        {
            if (previousDay == null || dailyBalanceTargetHours <= 0)
                return;

            int softTargetHours = Math.Clamp((int)Math.Ceiling(dailyBalanceTargetHours), 1, dailyLoadLimitHours);
            if (previousDay.TotalHours <= 0 || previousDay.TotalHours >= softTargetHours)
                return;

            MaintenanceVisitPlan? visitPlan = selectedVisitGroupState.NextVisit;
            if (visitPlan == null)
                return;

            int remainingToSoftTarget = softTargetHours - previousDay.TotalHours;
            if (remainingToSoftTarget <= 0 || visitPlan.TotalHours <= remainingToSoftTarget)
                return;

            if (previousDay.TotalHours + remainingToSoftTarget > dailyLoadLimitHours)
                return;

            if (!allowOwnerRepeat && ResolveOwnerConflictRank(previousDay, visitPlan) > 0)
                return;

            if (!allowLargeSystemConflict && ResolveLargeSystemConflictRank(previousDay, visitPlan) > 0)
                return;

            selectedVisitGroupState.TrySplitNextVisit(remainingToSoftTarget);
        }

        private static KnowledgeBaseMaintenanceMonthPlanResult PlanSequentialV3(
            int year,
            int month,
            IReadOnlyList<DateOnly> workingDays,
            int requestedHours,
            int totalMonthlyHourBudget,
            int calendarCapacityHours,
            int availableCapacityHours,
            IReadOnlyList<int> nonWorkingDayNumbers,
            IReadOnlyList<KbMaintenanceMonthWorkItem> workItems)
        {
            List<DayPlanBuilder> dayBuilders = workingDays
                .Select(static date => new DayPlanBuilder(date))
                .ToList();
            int preferredDailyLoadLimitHours = ResolveSequentialV3PreferredDailyLoadLimitHours(requestedHours, workingDays.Count);
            int dailyLoadLimitHours = Math.Max(DailySystemPackingTargetHours, preferredDailyLoadLimitHours);
            IReadOnlyList<MaintenanceVisitPlan> visits = BuildSequentialV3Visits(workItems, dailyLoadLimitHours);
            int currentDayIndex = 0;
            int remainingHours = requestedHours;
            int[] dayTargetHours = new int[dayBuilders.Count];

            foreach (MaintenanceVisitPlan visit in visits)
            {
                bool placed = PlaceSequentialV3Visit(
                    dayBuilders,
                    dayTargetHours,
                    visit,
                    dailyLoadLimitHours,
                    ref currentDayIndex,
                    ref remainingHours);
                if (!placed)
                {
                    return Failure(
                        $"Невозможно сформировать график ТО за {month:D2}.{year}: не удалось равномерно распределить работы по рабочим дням.",
                        workingDays.Count,
                        requestedHours,
                        totalMonthlyHourBudget,
                        calendarCapacityHours,
                        availableCapacityHours,
                        nonWorkingDayNumbers,
                        workItems,
                        KnowledgeBaseMaintenancePlanningMode.SequentialV3,
                        usedFallback: true,
                        dailyLoadLimitHours,
                        preferredDailyLoadLimitHours);
                }
            }

            List<KbMaintenanceMonthPlanDay> plannedDays = dayBuilders
                .Where(static day => day.Assignments.Count > 0)
                .Select(static day => day.ToPlanDay())
                .ToList();
            bool usedFallback = dailyLoadLimitHours > DailySystemPackingTargetHours ||
                                plannedDays.Any(static day => day.TotalHours > DailySystemPackingTargetHours) ||
                                HasRepeatedOwnerOnAnyDay(plannedDays);

            return Success(
                workingDays.Count,
                requestedHours,
                totalMonthlyHourBudget,
                calendarCapacityHours,
                availableCapacityHours,
                nonWorkingDayNumbers,
                workItems,
                plannedDays,
                KnowledgeBaseMaintenancePlanningMode.SequentialV3,
                usedFallback,
                dailyLoadLimitHours,
                preferredDailyLoadLimitHours);
        }

        private static IReadOnlyList<MaintenanceVisitPlan> BuildSequentialV3Visits(
            IReadOnlyList<KbMaintenanceMonthWorkItem> workItems,
            int dailyLoadLimitHours)
        {
            var visits = new List<MaintenanceVisitPlan>();
            int nextVisitOrderIndex = 0;
            foreach (KbMaintenanceMonthWorkItem workItem in OrderWorkItemsForSequentialV3(workItems))
            {
                foreach (WorkAssignmentDraft assignment in CreateAssignmentDrafts(workItem, dailyLoadLimitHours))
                {
                    visits.Add(new MaintenanceVisitPlan(nextVisitOrderIndex++, new[] { assignment }));
                }
            }

            return visits;
        }

        private static IEnumerable<KbMaintenanceMonthWorkItem> OrderWorkItemsForSequentialV3(
            IReadOnlyList<KbMaintenanceMonthWorkItem> workItems) =>
            workItems
                .OrderBy(static item => GetSystemPreorderSortKey(item))
                .ThenBy(static item => GetOwnerPreorderSortKey(item))
                .ThenBy(static item => GetWorkPriority(item.WorkKind))
                .ThenBy(static item => item.OwnerNodeId, StringComparer.Ordinal)
                .ThenBy(static item => item.NodeName, StringComparer.Ordinal);

        private static bool PlaceSequentialV3Visit(
            List<DayPlanBuilder> dayBuilders,
            int[] dayTargetHours,
            MaintenanceVisitPlan visit,
            int dailyLoadLimitHours,
            ref int currentDayIndex,
            ref int remainingHours)
        {
            if (dayBuilders.Count == 0 || visit.TotalHours <= 0)
                return true;

            MaintenanceVisitPlan? pendingVisit = visit;
            while (pendingVisit != null)
            {
                if (currentDayIndex >= dayBuilders.Count)
                    return false;

                DayPlanBuilder currentDay = dayBuilders[currentDayIndex];
                if (dayTargetHours[currentDayIndex] <= 0)
                {
                    dayTargetHours[currentDayIndex] = ResolveSequentialV3CurrentDayTarget(
                        remainingHours,
                        dayBuilders.Count - currentDayIndex);
                }

                int currentDayTargetHours = dayTargetHours[currentDayIndex];
                int availableToHardLimit = dailyLoadLimitHours - currentDay.TotalHours;
                if (pendingVisit.TotalHours <= availableToHardLimit)
                {
                    if (ShouldMoveSequentialV3VisitToNextDay(
                        currentDay,
                        pendingVisit,
                        currentDayTargetHours) &&
                        CanMoveSequentialV3VisitToNextDay(dayBuilders, currentDayIndex, remainingHours, dailyLoadLimitHours))
                    {
                        currentDayIndex++;
                        continue;
                    }

                    currentDay.AddVisit(pendingVisit);
                    remainingHours -= pendingVisit.TotalHours;
                    pendingVisit = null;
                    continue;
                }

                if (currentDayIndex < dayBuilders.Count - 1)
                {
                    if (CanMoveSequentialV3VisitToNextDay(dayBuilders, currentDayIndex, remainingHours, dailyLoadLimitHours))
                    {
                        currentDayIndex++;
                        continue;
                    }
                }

                int partHours = Math.Min(pendingVisit.TotalHours, availableToHardLimit);
                if (partHours <= 0)
                {
                    if (currentDayIndex < dayBuilders.Count - 1)
                    {
                        currentDayIndex++;
                        continue;
                    }

                    return false;
                }

                if (partHours < pendingVisit.TotalHours &&
                    pendingVisit.TrySplit(partHours, out MaintenanceVisitPlan? firstPart, out MaintenanceVisitPlan? remainder) &&
                    firstPart != null &&
                    remainder != null)
                {
                    currentDay.AddVisit(firstPart);
                    remainingHours -= firstPart.TotalHours;
                    pendingVisit = remainder;
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        private static bool CanMoveSequentialV3VisitToNextDay(
            IReadOnlyList<DayPlanBuilder> dayBuilders,
            int currentDayIndex,
            int remainingHours,
            int dailyLoadLimitHours)
        {
            if (currentDayIndex >= dayBuilders.Count - 1)
                return false;

            return remainingHours <= CalculateSequentialV3RemainingHardCapacity(dayBuilders, currentDayIndex + 1, dailyLoadLimitHours);
        }

        private static int CalculateSequentialV3RemainingHardCapacity(
            IReadOnlyList<DayPlanBuilder> dayBuilders,
            int startDayIndex,
            int dailyLoadLimitHours)
        {
            int capacityHours = 0;
            for (int index = Math.Max(0, startDayIndex); index < dayBuilders.Count; index++)
                capacityHours += Math.Max(0, dailyLoadLimitHours - dayBuilders[index].TotalHours);

            return capacityHours;
        }

        private static bool ShouldMoveSequentialV3VisitToNextDay(
            DayPlanBuilder currentDay,
            MaintenanceVisitPlan visit,
            int currentDayTargetHours)
        {
            if (currentDay.TotalHours <= 0)
                return false;

            int projectedHours = currentDay.TotalHours + visit.TotalHours;
            if (projectedHours <= currentDayTargetHours)
                return false;

            int targetOverrunHours = projectedHours - currentDayTargetHours;
            if (targetOverrunHours > SequentialV3SoftOverrunToleranceHours)
                return true;

            if (visit.TotalHours <= SequentialV3SoftOverrunToleranceHours &&
                targetOverrunHours <= SequentialV3SoftOverrunToleranceHours)
            {
                return false;
            }

            if (currentDay.TotalHours < currentDayTargetHours)
            {
                int targetDeficitHours = currentDayTargetHours - currentDay.TotalHours;
                return targetDeficitHours * 2 < visit.TotalHours;
            }

            return true;
        }

        private static int ResolveSequentialV3CurrentDayTarget(
            int remainingHours,
            int remainingDayCount)
        {
            if (remainingDayCount <= 0)
                return Math.Max(1, remainingHours);

            return Math.Max(1, (int)Math.Ceiling(Math.Max(0, remainingHours) / (double)remainingDayCount));
        }

        private static bool HasRepeatedOwnerOnAnyDay(
            IReadOnlyList<KbMaintenanceMonthPlanDay> plannedDays)
        {
            foreach (KbMaintenanceMonthPlanDay day in plannedDays)
            {
                bool hasRepeatedOwner = day.Assignments
                    .Where(static assignment => !string.IsNullOrWhiteSpace(assignment.OwnerNodeId))
                    .GroupBy(static assignment => assignment.OwnerNodeId, StringComparer.Ordinal)
                    .Any(static group => group.Count() > 1);
                if (hasRepeatedOwner)
                    return true;
            }

            return false;
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
            int dailyPackingTargetHours,
            bool packSystemAssignments = true)
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
                    ref nextVisitOrderIndex,
                    packSystemAssignments);
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
                    ref nextVisitOrderIndex,
                    packSystemAssignments);
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
            ref int nextVisitOrderIndex,
            bool packSystemAssignments)
        {
            if (!packSystemAssignments)
            {
                var separateVisits = new List<MaintenanceVisitPlan>();
                foreach (WorkAssignmentDraft assignment in OrderSystemAssignments(assignments))
                    separateVisits.Add(new MaintenanceVisitPlan(nextVisitOrderIndex++, new[] { assignment }));

                return separateVisits;
            }

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
            int targetPlannedDayCount,
            bool allowDailyLimitOverflow,
            bool preferBalancedLoad,
            bool preserveSequentialOrder,
            bool allowOwnerRepeat,
            bool allowLargeSystemConflict)
        {
            List<DayPlanBuilder> candidates = dayBuilders
                .Where(day => CanPlaceVisitOnDay(
                    day,
                    visitPlan,
                    dailyLoadLimitHours,
                    allowDailyLimitOverflow,
                    allowOwnerRepeat,
                    allowLargeSystemConflict))
                .ToList();
            if (preserveSequentialOrder && previousGroupDay != null)
            {
                int previousDayIndex = GetDayIndex(dayBuilders, previousGroupDay);
                if (previousDayIndex >= 0)
                {
                    candidates = candidates
                        .Where(day => GetDayIndex(dayBuilders, day) >= previousDayIndex)
                        .ToList();
                }
            }

            if (candidates.Count == 0)
                return null;

            IEnumerable<DayCandidateScore> scores = candidates
                .Select(day => BuildDayCandidateScore(
                    dayBuilders,
                    day,
                    visitPlan,
                    previousGroupDay,
                    dailyBalanceTargetHours,
                    dailyLoadLimitHours,
                    targetPlannedDayCount));

            if (preserveSequentialOrder)
            {
                return scores
                    .OrderBy(static score => score.LargeSystemConflictRank)
                    .ThenBy(static score => score.OwnerConflictRank)
                    .ThenBy(static score => score.ProjectedLimitOverflowHours)
                    .ThenBy(static score => score.SequentialSoftTargetRank)
                    .ThenBy(static score => score.Day.Date)
                    .ThenBy(static score => score.BalanceDeviation)
                    .ThenBy(static score => score.CurrentHours)
                    .ThenBy(static score => score.ProjectedHours)
                    .ThenBy(static score => score.SystemAdjacencyRank)
                    .ThenBy(static score => score.OwnerAdjacencyRank)
                    .Select(static score => score.Day)
                    .First();
            }

            if (preferBalancedLoad)
            {
                return scores
                    .OrderBy(static score => score.LargeSystemConflictRank)
                    .ThenBy(static score => score.OwnerConflictRank)
                    .ThenBy(static score => score.ProjectedLimitOverflowHours)
                    .ThenBy(static score => score.BalanceDeviation)
                    .ThenBy(static score => score.CurrentHours)
                    .ThenBy(static score => score.TargetOccupancyRank)
                    .ThenBy(static score => score.RouteFillRank)
                    .ThenBy(static score => score.ProjectedHours)
                    .ThenBy(static score => score.ContinuationRank)
                    .ThenBy(static score => score.ContinuationDistance)
                    .ThenBy(static score => score.SystemAdjacencyRank)
                    .ThenBy(static score => score.OwnerAdjacencyRank)
                    .ThenBy(static score => score.Day.Date)
                    .Select(static score => score.Day)
                    .First();
            }

            return scores
                .OrderBy(static score => score.LargeSystemConflictRank)
                .ThenBy(static score => score.OwnerConflictRank)
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
            int ownerConflictRank = ResolveOwnerConflictRank(day, visitPlan);
            int largeSystemConflictRank = ResolveLargeSystemConflictRank(day, visitPlan);
            int targetOccupancyRank = ResolveTargetOccupancyRank(dayBuilders, day, targetPlannedDayCount);
            int routeFillRank = ResolveRouteFillRank(day, visitPlan, projectedHours, dailyBalanceTargetHours);
            double balanceDeviation = Math.Abs(projectedHours - dailyBalanceTargetHours);
            int sequentialSoftTargetRank = ResolveSequentialSoftTargetRank(
                day.TotalHours,
                projectedHours,
                dailyBalanceTargetHours);

            return new DayCandidateScore(
                day,
                ContinuationRank: continuationRank,
                ContinuationDistance: continuationDistance,
                ProjectedLimitOverflowHours: projectedLimitOverflowHours,
                OwnerConflictRank: ownerConflictRank,
                LargeSystemConflictRank: largeSystemConflictRank,
                TargetOccupancyRank: targetOccupancyRank,
                CurrentHours: day.TotalHours,
                RouteFillRank: routeFillRank,
                BalanceDeviation: balanceDeviation,
                ProjectedHours: projectedHours,
                OwnerAdjacencyRank: HasOwnerOnAdjacentWorkingDay(dayBuilders, day, visitPlan) ? 1 : 0,
                SystemAdjacencyRank: HasSystemOnAdjacentWorkingDay(dayBuilders, day, visitPlan.SystemNodeId) ? 1 : 0,
                SequentialSoftTargetRank: sequentialSoftTargetRank);
        }

        private static int ResolveSequentialSoftTargetRank(
            int currentHours,
            int projectedHours,
            double dailyBalanceTargetHours)
        {
            if (dailyBalanceTargetHours <= 0)
                return 0;

            if (currentHours < dailyBalanceTargetHours && projectedHours <= dailyBalanceTargetHours)
                return 0;

            if (currentHours == 0)
                return 1;

            if (currentHours < dailyBalanceTargetHours)
                return 2;

            return 3;
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
            int dailyLoadLimitHours,
            bool allowVisitSwaps = false)
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

            if (!allowVisitSwaps)
                return;

            int maxSwapCount = dayBuilders.Sum(static day => day.Visits.Count) * dayBuilders.Count;
            for (int swapIndex = 0; swapIndex < maxSwapCount; swapIndex++)
            {
                DayLoadRebalanceSwap? bestSwap = FindBestDayLoadRebalanceSwap(
                    dayBuilders,
                    dailyBalanceTargetHours,
                    dailyLoadLimitHours);
                if (bestSwap == null)
                    break;

                bestSwap.Source.SwapVisitWith(bestSwap.Target, bestSwap.SourceVisit, bestSwap.TargetVisit);
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

        private static DayLoadRebalanceSwap? FindBestDayLoadRebalanceSwap(
            IReadOnlyList<DayPlanBuilder> dayBuilders,
            double dailyBalanceTargetHours,
            int dailyLoadLimitHours)
        {
            DayLoadRebalanceSwap? bestSwap = null;
            foreach (DayPlanBuilder sourceDay in dayBuilders
                .Where(day => day.TotalHours > dailyBalanceTargetHours && day.Visits.Count > 0)
                .OrderByDescending(static day => day.TotalHours)
                .ThenBy(static day => day.Date))
            {
                foreach (DayPlanBuilder targetDay in dayBuilders
                    .Where(day => !ReferenceEquals(day, sourceDay) && day.TotalHours < sourceDay.TotalHours && day.Visits.Count > 0)
                    .OrderBy(static day => day.TotalHours)
                    .ThenBy(static day => day.Date))
                {
                    foreach (ScheduledMaintenanceVisit sourceVisit in sourceDay.Visits
                        .OrderByDescending(static visit => visit.TotalHours)
                        .ThenBy(static visit => visit.OrderIndex))
                    {
                        foreach (ScheduledMaintenanceVisit targetVisit in targetDay.Visits
                            .Where(visit => visit.TotalHours < sourceVisit.TotalHours)
                            .OrderBy(static visit => visit.TotalHours)
                            .ThenBy(static visit => visit.OrderIndex))
                        {
                            if (!CanSwapVisitsForRebalance(
                                sourceDay,
                                targetDay,
                                sourceVisit,
                                targetVisit,
                                dailyLoadLimitHours))
                            {
                                continue;
                            }

                            int projectedSourceHours = sourceDay.TotalHours - sourceVisit.TotalHours + targetVisit.TotalHours;
                            int projectedTargetHours = targetDay.TotalHours - targetVisit.TotalHours + sourceVisit.TotalHours;
                            double currentScore =
                                CalculateDayLoadBalanceScore(sourceDay.TotalHours, dailyBalanceTargetHours) +
                                CalculateDayLoadBalanceScore(targetDay.TotalHours, dailyBalanceTargetHours);
                            double projectedScore =
                                CalculateDayLoadBalanceScore(projectedSourceHours, dailyBalanceTargetHours) +
                                CalculateDayLoadBalanceScore(projectedTargetHours, dailyBalanceTargetHours);
                            double improvement = currentScore - projectedScore;
                            if (improvement <= 0.0001)
                                continue;

                            var swap = new DayLoadRebalanceSwap(
                                sourceDay,
                                targetDay,
                                sourceVisit,
                                targetVisit,
                                improvement,
                                Math.Abs(projectedSourceHours - dailyBalanceTargetHours),
                                Math.Abs(projectedTargetHours - dailyBalanceTargetHours));
                            if (bestSwap == null || IsBetterRebalanceSwap(swap, bestSwap))
                                bestSwap = swap;
                        }
                    }
                }
            }

            return bestSwap;
        }

        private static bool IsBetterRebalanceSwap(
            DayLoadRebalanceSwap candidate,
            DayLoadRebalanceSwap currentBest)
        {
            int improvementComparison = candidate.Improvement.CompareTo(currentBest.Improvement);
            if (improvementComparison != 0)
                return improvementComparison > 0;

            double candidateWorstDeviation = Math.Max(candidate.SourceDeviationAfter, candidate.TargetDeviationAfter);
            double currentWorstDeviation = Math.Max(currentBest.SourceDeviationAfter, currentBest.TargetDeviationAfter);
            int worstDeviationComparison = candidateWorstDeviation.CompareTo(currentWorstDeviation);
            if (worstDeviationComparison != 0)
                return worstDeviationComparison < 0;

            int sourceDeviationComparison = candidate.SourceDeviationAfter.CompareTo(currentBest.SourceDeviationAfter);
            if (sourceDeviationComparison != 0)
                return sourceDeviationComparison < 0;

            int targetDeviationComparison = candidate.TargetDeviationAfter.CompareTo(currentBest.TargetDeviationAfter);
            if (targetDeviationComparison != 0)
                return targetDeviationComparison < 0;

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

        private static bool CanSwapVisitsForRebalance(
            DayPlanBuilder sourceDay,
            DayPlanBuilder targetDay,
            ScheduledMaintenanceVisit sourceVisit,
            ScheduledMaintenanceVisit targetVisit,
            int dailyLoadLimitHours)
        {
            int projectedSourceHours = sourceDay.TotalHours - sourceVisit.TotalHours + targetVisit.TotalHours;
            int projectedTargetHours = targetDay.TotalHours - targetVisit.TotalHours + sourceVisit.TotalHours;
            if (projectedSourceHours > dailyLoadLimitHours || projectedTargetHours > dailyLoadLimitHours)
                return false;

            return CanReplaceVisitOnDay(sourceDay, sourceVisit, targetVisit) &&
                   CanReplaceVisitOnDay(targetDay, targetVisit, sourceVisit);
        }

        private static bool CanReplaceVisitOnDay(
            DayPlanBuilder day,
            ScheduledMaintenanceVisit outgoingVisit,
            ScheduledMaintenanceVisit incomingVisit)
        {
            foreach (string ownerNodeId in incomingVisit.OwnerNodeIds)
            {
                if (day.Visits
                    .Where(visit => !ReferenceEquals(visit, outgoingVisit))
                    .Any(visit => visit.OwnerNodeIds.Contains(ownerNodeId, StringComparer.Ordinal)))
                {
                    return false;
                }
            }

            if (incomingVisit.IsLargeSystem)
            {
                bool hasOtherLargeSystem = day.Visits
                    .Where(visit => !ReferenceEquals(visit, outgoingVisit))
                    .Any(visit => visit.IsLargeSystem && !string.Equals(
                        visit.SchedulingConflictKey,
                        incomingVisit.SchedulingConflictKey,
                        StringComparison.Ordinal));
                if (hasOtherLargeSystem)
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
            bool allowOwnerRepeat,
            out string errorMessage)
        {
            if (allowOwnerRepeat)
            {
                errorMessage = string.Empty;
                return true;
            }

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
            return CanPlaceVisitOnDay(
                day,
                visitPlan,
                dailyLoadLimitHours,
                allowDailyLimitOverflow: false,
                allowOwnerRepeat: false,
                allowLargeSystemConflict: false);
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

        private static bool CanPlaceVisitOnDay(
            DayPlanBuilder day,
            MaintenanceVisitPlan visitPlan,
            int dailyLoadLimitHours,
            bool allowDailyLimitOverflow,
            bool allowOwnerRepeat,
            bool allowLargeSystemConflict)
        {
            if (!allowOwnerRepeat && ResolveOwnerConflictRank(day, visitPlan) > 0)
                return false;

            if (!allowLargeSystemConflict && ResolveLargeSystemConflictRank(day, visitPlan) > 0)
                return false;

            if (!allowDailyLimitOverflow && day.TotalHours + visitPlan.TotalHours > dailyLoadLimitHours)
                return false;

            return true;
        }

        private static int ResolveOwnerConflictRank(
            DayPlanBuilder day,
            MaintenanceVisitPlan visitPlan)
        {
            foreach (string ownerNodeId in visitPlan.OwnerNodeIds)
            {
                if (day.HasOwner(ownerNodeId))
                    return 1;
            }

            return 0;
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

        private static int ResolveBalancedV2PreferredDailyLoadLimitHours(int requestedHours, int workingDayCount)
        {
            if (workingDayCount <= 0)
                return DailySystemPackingTargetHours;

            int limitHours = (int)Math.Ceiling(
                requestedHours / (double)workingDayCount + BalancedV2DailyLoadAllowanceHours);
            return Math.Clamp(limitHours, 1, DailySystemPackingTargetHours);
        }

        private static int ResolveSequentialV3PreferredDailyLoadLimitHours(int requestedHours, int workingDayCount)
        {
            if (workingDayCount <= 0)
                return DailySystemPackingTargetHours;

            int limitHours = (int)Math.Ceiling(requestedHours / (double)workingDayCount);
            return Math.Max(1, limitHours);
        }

        private static int ResolvePreferredDailyLoadLimitHours(
            KnowledgeBaseMaintenancePlanningMode planningMode,
            int requestedHours,
            int workingDayCount) =>
            planningMode == KnowledgeBaseMaintenancePlanningMode.SequentialV3
                ? ResolveSequentialV3PreferredDailyLoadLimitHours(requestedHours, workingDayCount)
                : ResolveBalancedV2PreferredDailyLoadLimitHours(requestedHours, workingDayCount);

        private static IReadOnlyList<MaintenancePlanningAttempt> BuildPlanningAttempts(
            KnowledgeBaseMaintenancePlanningMode planningMode,
            int requestedHours,
            int workingDayCount)
        {
            if (planningMode == KnowledgeBaseMaintenancePlanningMode.SequentialV3)
            {
                int sequentialPreferredDailyLimitHours = ResolveSequentialV3PreferredDailyLoadLimitHours(requestedHours, workingDayCount);
                return new[]
                {
                    new MaintenancePlanningAttempt(
                        DailySystemPackingTargetHours,
                        sequentialPreferredDailyLimitHours,
                        sequentialPreferredDailyLimitHours,
                        AllowDailyLimitOverflow: false,
                        AllowOwnerRepeat: false,
                        AllowLargeSystemConflict: false,
                        UsedFallback: false),
                    new MaintenancePlanningAttempt(
                        DailySystemPackingTargetHours,
                        sequentialPreferredDailyLimitHours,
                        sequentialPreferredDailyLimitHours,
                        AllowDailyLimitOverflow: false,
                        AllowOwnerRepeat: true,
                        AllowLargeSystemConflict: false,
                        UsedFallback: true),
                    new MaintenancePlanningAttempt(
                        DailySystemPackingTargetHours,
                        sequentialPreferredDailyLimitHours,
                        sequentialPreferredDailyLimitHours,
                        AllowDailyLimitOverflow: true,
                        AllowOwnerRepeat: true,
                        AllowLargeSystemConflict: true,
                        UsedFallback: true)
                };
            }

            if (planningMode != KnowledgeBaseMaintenancePlanningMode.BalancedV2)
            {
                int defaultDailyLimitHours = ResolveDailyPackingTargetHours(requestedHours, workingDayCount);
                return new[]
                {
                    new MaintenancePlanningAttempt(
                        defaultDailyLimitHours,
                        defaultDailyLimitHours,
                        defaultDailyLimitHours,
                        AllowDailyLimitOverflow: true,
                        AllowOwnerRepeat: false,
                        AllowLargeSystemConflict: true,
                        UsedFallback: false)
                };
            }

            int preferredDailyLimitHours = ResolveBalancedV2PreferredDailyLoadLimitHours(requestedHours, workingDayCount);
            if (preferredDailyLimitHours >= DailySystemPackingTargetHours)
            {
                return new[]
                {
                    new MaintenancePlanningAttempt(
                        DailySystemPackingTargetHours,
                        preferredDailyLimitHours,
                        preferredDailyLimitHours,
                        AllowDailyLimitOverflow: false,
                        AllowOwnerRepeat: false,
                        AllowLargeSystemConflict: false,
                        UsedFallback: false)
                };
            }

            return new[]
            {
                new MaintenancePlanningAttempt(
                    preferredDailyLimitHours,
                    preferredDailyLimitHours,
                    preferredDailyLimitHours,
                    AllowDailyLimitOverflow: false,
                    AllowOwnerRepeat: false,
                    AllowLargeSystemConflict: false,
                    UsedFallback: false),
                new MaintenancePlanningAttempt(
                    DailySystemPackingTargetHours,
                    preferredDailyLimitHours,
                    preferredDailyLimitHours,
                    AllowDailyLimitOverflow: false,
                    AllowOwnerRepeat: false,
                    AllowLargeSystemConflict: false,
                    UsedFallback: true)
            };
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
            int OwnerConflictRank,
            int LargeSystemConflictRank,
            int TargetOccupancyRank,
            int CurrentHours,
            int RouteFillRank,
            double BalanceDeviation,
            int ProjectedHours,
            int OwnerAdjacencyRank,
            int SystemAdjacencyRank,
            int SequentialSoftTargetRank);

        private sealed record MaintenancePlanningAttempt(
            int DailyLoadLimitHours,
            int PreferredDailyLoadLimitHours,
            int VisitSplitTargetHours,
            bool AllowDailyLimitOverflow,
            bool AllowOwnerRepeat,
            bool AllowLargeSystemConflict,
            bool UsedFallback);

        private sealed record DayLoadRebalanceMove(
            DayPlanBuilder Source,
            DayPlanBuilder Target,
            ScheduledMaintenanceVisit Visit,
            double Improvement,
            double TargetDeviationAfter,
            double SourceDeviationAfter);

        private sealed record DayLoadRebalanceSwap(
            DayPlanBuilder Source,
            DayPlanBuilder Target,
            ScheduledMaintenanceVisit SourceVisit,
            ScheduledMaintenanceVisit TargetVisit,
            double Improvement,
            double SourceDeviationAfter,
            double TargetDeviationAfter);

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
            private readonly List<MaintenanceVisitPlan> _visits;

            public MaintenanceVisitGroupScheduleState(MaintenanceVisitGroup group)
            {
                Group = group;
                _visits = group.Visits.ToList();
            }

            private MaintenanceVisitGroup Group { get; }

            public int GroupOrderIndex => Group.OrderIndex;

            public DayPlanBuilder? PreviousDay { get; private set; }

            private int NextVisitIndex { get; set; }

            public MaintenanceVisitPlan? NextVisit =>
                NextVisitIndex < _visits.Count ? _visits[NextVisitIndex] : null;

            public bool TrySplitNextVisit(int firstPartHours)
            {
                MaintenanceVisitPlan? visit = NextVisit;
                if (visit == null || !visit.TrySplit(firstPartHours, out MaintenanceVisitPlan? firstPart, out MaintenanceVisitPlan? remainder))
                    return false;

                if (firstPart == null || remainder == null)
                    return false;

                _visits[NextVisitIndex] = firstPart;
                _visits.Insert(NextVisitIndex + 1, remainder);
                return true;
            }

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

            public bool TrySplit(
                int firstPartHours,
                out MaintenanceVisitPlan? firstPart,
                out MaintenanceVisitPlan? remainder)
            {
                firstPart = null;
                remainder = null;

                if (Assignments.Count != 1 || firstPartHours <= 0 || firstPartHours >= TotalHours)
                    return false;

                WorkAssignmentDraft assignment = Assignments[0];
                firstPart = new MaintenanceVisitPlan(
                    OrderIndex,
                    new[] { assignment with { Hours = firstPartHours } });
                remainder = new MaintenanceVisitPlan(
                    OrderIndex,
                    new[] { assignment with { Hours = TotalHours - firstPartHours } });
                return true;
            }
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
            List<KbMaintenanceMonthPlanDay> plannedDays,
            KnowledgeBaseMaintenancePlanningMode planningMode = KnowledgeBaseMaintenancePlanningMode.Default,
            bool usedFallback = false,
            int dailyLoadLimitHours = 0,
            int preferredDailyLoadLimitHours = 0) =>
            new()
            {
                IsSuccess = true,
                PlanningMode = planningMode,
                UsedFallback = usedFallback,
                DailyLoadLimitHours = dailyLoadLimitHours,
                PreferredDailyLoadLimitHours = preferredDailyLoadLimitHours,
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
            IReadOnlyList<KbMaintenanceMonthWorkItem>? workItems = null,
            KnowledgeBaseMaintenancePlanningMode planningMode = KnowledgeBaseMaintenancePlanningMode.Default,
            bool usedFallback = false,
            int dailyLoadLimitHours = 0,
            int preferredDailyLoadLimitHours = 0) =>
            new()
            {
                IsSuccess = false,
                ErrorMessage = errorMessage,
                PlanningMode = planningMode,
                UsedFallback = usedFallback,
                DailyLoadLimitHours = dailyLoadLimitHours,
                PreferredDailyLoadLimitHours = preferredDailyLoadLimitHours,
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

            public void SwapVisitWith(
                DayPlanBuilder otherDay,
                ScheduledMaintenanceVisit visit,
                ScheduledMaintenanceVisit otherVisit)
            {
                if (!Visits.Remove(visit))
                    return;

                if (!otherDay.Visits.Remove(otherVisit))
                {
                    Visits.Add(visit);
                    RebuildFromVisits();
                    return;
                }

                Visits.Add(otherVisit.CloneForDate(Date));
                otherDay.Visits.Add(visit.CloneForDate(otherDay.Date));
                RebuildFromVisits();
                otherDay.RebuildFromVisits();
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
