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

            IReadOnlyList<MaintenanceVisitPlan> visitPlans = BuildVisitPlans(normalizedWorkItems);
            foreach (MaintenanceVisitPlan visitPlan in OrderVisitPlans(visitPlans))
            {
                DayPlanBuilder selectedDay = SelectBestDay(dayBuilders, visitPlan);
                foreach (WorkAssignmentDraft assignmentDraft in visitPlan.Assignments)
                {
                    selectedDay.Assignments.Add(new KbMaintenanceMonthPlanAssignment
                    {
                        Date = selectedDay.Date,
                        OwnerNodeId = assignmentDraft.OwnerNodeId,
                        NodeName = assignmentDraft.NodeName,
                        SystemNodeId = assignmentDraft.SystemNodeId,
                        WorkKind = assignmentDraft.WorkKind,
                        Hours = assignmentDraft.Hours
                    });
                }

                selectedDay.TotalHours += visitPlan.TotalHours;
                selectedDay.Register(visitPlan);
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

        private static IReadOnlyList<MaintenanceVisitPlan> BuildVisitPlans(
            IReadOnlyList<KbMaintenanceMonthWorkItem> workItems)
        {
            List<WorkAssignmentDraft> assignmentDrafts = OrderWorkItems(workItems)
                .SelectMany(CreateAssignmentDrafts)
                .ToList();
            var visitPlans = new List<MaintenanceVisitPlan>();
            int nextVisitOrderIndex = 0;

            foreach (IGrouping<string, WorkAssignmentDraft> systemGroup in assignmentDrafts
                .Where(static assignment => !string.IsNullOrWhiteSpace(assignment.SystemNodeId))
                .GroupBy(static assignment => assignment.SystemNodeId, StringComparer.Ordinal)
                .OrderBy(static group => group.Min(static assignment => assignment.SystemPreorderIndex))
                .ThenBy(static group => group.Key, StringComparer.Ordinal))
            {
                foreach (List<WorkAssignmentDraft> visitAssignments in PackSystemAssignments(systemGroup))
                {
                    visitPlans.Add(new MaintenanceVisitPlan(nextVisitOrderIndex++, visitAssignments));
                }
            }

            foreach (WorkAssignmentDraft assignmentDraft in assignmentDrafts
                .Where(static assignment => string.IsNullOrWhiteSpace(assignment.SystemNodeId)))
            {
                visitPlans.Add(new MaintenanceVisitPlan(nextVisitOrderIndex++, new[] { assignmentDraft }));
            }

            return visitPlans;
        }

        private static IEnumerable<WorkAssignmentDraft> CreateAssignmentDrafts(
            KbMaintenanceMonthWorkItem workItem)
        {
            if (workItem.Hours <= 0)
                yield break;

            foreach (int assignmentHours in SplitWorkItemHours(workItem))
            {
                yield return new WorkAssignmentDraft(
                    OwnerNodeId: workItem.OwnerNodeId?.Trim() ?? string.Empty,
                    NodeName: workItem.NodeName?.Trim() ?? string.Empty,
                    SystemNodeId: workItem.SystemNodeId?.Trim() ?? string.Empty,
                    SystemPreorderIndex: GetSystemPreorderSortKey(workItem),
                    OwnerPreorderIndex: GetOwnerPreorderSortKey(workItem),
                    WorkKind: workItem.WorkKind,
                    Hours: assignmentHours);
            }
        }

        private static IReadOnlyList<List<WorkAssignmentDraft>> PackSystemAssignments(
            IEnumerable<WorkAssignmentDraft> assignments)
        {
            var visitBuilders = new List<MaintenanceVisitBuilder>();
            foreach (WorkAssignmentDraft assignment in assignments
                .OrderByDescending(static item => item.Hours)
                .ThenBy(static item => GetWorkPriority(item.WorkKind))
                .ThenBy(static item => item.OwnerPreorderIndex)
                .ThenBy(static item => item.OwnerNodeId, StringComparer.Ordinal)
                .ThenBy(static item => item.NodeName, StringComparer.Ordinal))
            {
                MaintenanceVisitBuilder? bestFitVisit = visitBuilders
                    .Where(visit => visit.TotalHours + assignment.Hours <= DailySystemPackingTargetHours)
                    .OrderBy(visit => DailySystemPackingTargetHours - (visit.TotalHours + assignment.Hours))
                    .ThenBy(visit => visit.OrderIndex)
                    .FirstOrDefault();

                if (bestFitVisit == null)
                {
                    bestFitVisit = new MaintenanceVisitBuilder(visitBuilders.Count);
                    visitBuilders.Add(bestFitVisit);
                }

                bestFitVisit.Assignments.Add(assignment);
                bestFitVisit.TotalHours += assignment.Hours;
            }

            return visitBuilders
                .OrderBy(static visit => visit.OrderIndex)
                .Select(static visit => visit.Assignments
                    .OrderBy(static assignment => GetWorkPriority(assignment.WorkKind))
                    .ThenBy(static assignment => assignment.OwnerPreorderIndex)
                    .ThenByDescending(static assignment => assignment.Hours)
                    .ThenBy(static assignment => assignment.OwnerNodeId, StringComparer.Ordinal)
                    .ThenBy(static assignment => assignment.NodeName, StringComparer.Ordinal)
                    .ToList())
                .ToList();
        }

        private static IEnumerable<MaintenanceVisitPlan> OrderVisitPlans(
            IReadOnlyList<MaintenanceVisitPlan> visitPlans) =>
            visitPlans
                .OrderByDescending(static visit => Math.Min(visit.TotalHours, DailySystemPackingTargetHours))
                .ThenBy(static visit => visit.HasMajorWork ? 0 : 1)
                .ThenBy(static visit => visit.SystemPreorderIndex)
                .ThenBy(static visit => visit.OrderIndex);

        private static IEnumerable<int> SplitWorkItemHours(KbMaintenanceMonthWorkItem workItem)
        {
            int hours = Math.Max(0, workItem.Hours);
            if (!IsMajorWork(workItem.WorkKind) || hours <= MajorWorkSplitChunkHours)
            {
                yield return hours;
                yield break;
            }

            int remainingHours = hours;
            while (remainingHours > 0)
            {
                int assignmentHours = Math.Min(MajorWorkSplitChunkHours, remainingHours);
                yield return assignmentHours;
                remainingHours -= assignmentHours;
            }
        }

        private static DayPlanBuilder SelectBestDay(
            List<DayPlanBuilder> dayBuilders,
            MaintenanceVisitPlan visitPlan)
        {
            List<DayPlanBuilder> candidatesUnderTarget = dayBuilders
                .Where(day => day.TotalHours + visitPlan.TotalHours <= DailySystemPackingTargetHours)
                .ToList();
            IReadOnlyList<DayPlanBuilder> candidates = candidatesUnderTarget.Count > 0
                ? candidatesUnderTarget
                : dayBuilders;

            return candidates
                .Select(day => new DayCandidateScore(
                    day,
                    ProjectedHours: day.TotalHours + visitPlan.TotalHours,
                    OwnerAdjacencyRank: HasOwnerOnAdjacentWorkingDay(dayBuilders, day, visitPlan) ? 1 : 0,
                    SystemAdjacencyRank: HasSystemOnAdjacentWorkingDay(dayBuilders, day, visitPlan.SystemNodeId) ? 1 : 0))
                .OrderBy(static score => score.ProjectedHours <= DailySystemPackingTargetHours ? 0 : 1)
                .ThenBy(static score => score.Day.TotalHours)
                .ThenBy(static score => score.SystemAdjacencyRank)
                .ThenBy(static score => score.OwnerAdjacencyRank)
                .ThenBy(static score => score.ProjectedHours)
                .ThenBy(static score => score.Day.Date)
                .Select(static score => score.Day)
                .First();
        }

        private static bool HasOwnerOnAdjacentWorkingDay(
            IReadOnlyList<DayPlanBuilder> dayBuilders,
            DayPlanBuilder day,
            string ownerNodeId)
        {
            int dayIndex = -1;
            for (int index = 0; index < dayBuilders.Count; index++)
            {
                if (ReferenceEquals(dayBuilders[index], day))
                {
                    dayIndex = index;
                    break;
                }
            }

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

            int dayIndex = -1;
            for (int index = 0; index < dayBuilders.Count; index++)
            {
                if (ReferenceEquals(dayBuilders[index], day))
                {
                    dayIndex = index;
                    break;
                }
            }

            if (dayIndex < 0)
                return false;

            return (dayIndex > 0 && dayBuilders[dayIndex - 1].HasSystem(systemNodeId)) ||
                   (dayIndex < dayBuilders.Count - 1 && dayBuilders[dayIndex + 1].HasSystem(systemNodeId));
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
            int ProjectedHours,
            int OwnerAdjacencyRank,
            int SystemAdjacencyRank);

        private sealed record WorkAssignmentDraft(
            string OwnerNodeId,
            string NodeName,
            string SystemNodeId,
            int SystemPreorderIndex,
            int OwnerPreorderIndex,
            KbMaintenanceWorkKind WorkKind,
            int Hours);

        private sealed class MaintenanceVisitBuilder
        {
            public MaintenanceVisitBuilder(int orderIndex)
            {
                OrderIndex = orderIndex;
            }

            public int OrderIndex { get; }

            public int TotalHours { get; set; }

            public List<WorkAssignmentDraft> Assignments { get; } = new();
        }

        private sealed class MaintenanceVisitPlan
        {
            public MaintenanceVisitPlan(int orderIndex, IEnumerable<WorkAssignmentDraft> assignments)
            {
                OrderIndex = orderIndex;
                Assignments = assignments.ToList();
                TotalHours = Assignments.Sum(static assignment => assignment.Hours);
                HasMajorWork = Assignments.Any(static assignment => IsMajorWork(assignment.WorkKind));
                SystemNodeId = Assignments
                    .Select(static assignment => assignment.SystemNodeId)
                    .FirstOrDefault(static systemNodeId => !string.IsNullOrWhiteSpace(systemNodeId))
                    ?? string.Empty;
                SystemPreorderIndex = Assignments
                    .Where(static assignment => assignment.SystemPreorderIndex != int.MaxValue)
                    .Select(static assignment => assignment.SystemPreorderIndex)
                    .DefaultIfEmpty(int.MaxValue)
                    .Min();
                OwnerNodeIds = Assignments
                    .Select(static assignment => assignment.OwnerNodeId)
                    .Where(static ownerNodeId => !string.IsNullOrWhiteSpace(ownerNodeId))
                    .Distinct(StringComparer.Ordinal)
                    .ToList();
            }

            public int OrderIndex { get; }

            public List<WorkAssignmentDraft> Assignments { get; }

            public int TotalHours { get; }

            public bool HasMajorWork { get; }

            public string SystemNodeId { get; }

            public int SystemPreorderIndex { get; }

            public IReadOnlyList<string> OwnerNodeIds { get; }
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

            private HashSet<string> OwnerNodeIds { get; } = new(StringComparer.Ordinal);

            private HashSet<string> SystemNodeIds { get; } = new(StringComparer.Ordinal);

            public void Register(MaintenanceVisitPlan visitPlan)
            {
                foreach (string ownerNodeId in visitPlan.OwnerNodeIds)
                {
                    OwnerNodeIds.Add(ownerNodeId);
                }

                if (!string.IsNullOrWhiteSpace(visitPlan.SystemNodeId))
                    SystemNodeIds.Add(visitPlan.SystemNodeId);
            }

            public bool HasOwner(string ownerNodeId) =>
                OwnerNodeIds.Contains(ownerNodeId);

            public bool HasSystem(string systemNodeId) =>
                SystemNodeIds.Contains(systemNodeId);

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
