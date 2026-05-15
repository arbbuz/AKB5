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

            foreach (KbMaintenanceMonthWorkItem workItem in OrderWorkItems(normalizedWorkItems))
            {
                if (workItem.Hours <= 0)
                    continue;

                foreach (int assignmentHours in SplitWorkItemHours(workItem))
                {
                    DayPlanBuilder selectedDay = SelectBestDay(dayBuilders, workItem, assignmentHours);
                    selectedDay.Assignments.Add(new KbMaintenanceMonthPlanAssignment
                    {
                        Date = selectedDay.Date,
                        OwnerNodeId = workItem.OwnerNodeId?.Trim() ?? string.Empty,
                        NodeName = workItem.NodeName?.Trim() ?? string.Empty,
                        SystemNodeId = workItem.SystemNodeId?.Trim() ?? string.Empty,
                        WorkKind = workItem.WorkKind,
                        Hours = assignmentHours
                    });
                    selectedDay.TotalHours += assignmentHours;
                    selectedDay.Register(workItem);
                    if (IsMajorWork(workItem.WorkKind))
                        selectedDay.HasMajorWork = true;
                }
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
            KbMaintenanceMonthWorkItem workItem,
            int assignmentHours)
        {
            List<DayPlanBuilder> candidates = dayBuilders;
            if (IsMajorWork(workItem.WorkKind))
            {
                List<DayPlanBuilder> daysWithoutMajorWork = dayBuilders
                    .Where(static day => !day.HasMajorWork)
                    .ToList();
                if (daysWithoutMajorWork.Count > 0)
                    candidates = daysWithoutMajorWork;
            }

            // Prefer non-adjacent owner chunks and same-system days; 16h remains a soft packing target.
            string ownerNodeId = workItem.OwnerNodeId?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(ownerNodeId))
            {
                List<DayPlanBuilder> daysWithoutSameOwner = candidates
                    .Where(day => !day.HasOwner(ownerNodeId))
                    .ToList();
                if (daysWithoutSameOwner.Count > 0)
                    candidates = daysWithoutSameOwner;

                List<DayPlanBuilder> daysWithoutAdjacentOwner = candidates
                    .Where(day => !HasOwnerOnAdjacentWorkingDay(dayBuilders, day, ownerNodeId))
                    .ToList();
                if (daysWithoutAdjacentOwner.Count > 0)
                    candidates = daysWithoutAdjacentOwner;
            }

            return candidates
                .OrderBy(day => GetSystemAffinityRank(day, workItem.SystemNodeId, assignmentHours))
                .ThenBy(static day => day.TotalHours)
                .ThenBy(static day => day.Date)
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

        private static int GetSystemAffinityRank(
            DayPlanBuilder day,
            string? systemNodeId,
            int assignmentHours)
        {
            string normalizedSystemNodeId = systemNodeId?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalizedSystemNodeId))
                return 0;

            if (day.HasOnlySystem(normalizedSystemNodeId))
            {
                return day.TotalHours + assignmentHours <= DailySystemPackingTargetHours ? 0 : 2;
            }

            if (day.Assignments.Count == 0)
                return 1;

            if (day.HasSystem(normalizedSystemNodeId))
                return 2;

            return 3;
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

            public bool HasMajorWork { get; set; }

            public List<KbMaintenanceMonthPlanAssignment> Assignments { get; } = new();

            private HashSet<string> OwnerNodeIds { get; } = new(StringComparer.Ordinal);

            private HashSet<string> SystemNodeIds { get; } = new(StringComparer.Ordinal);

            public void Register(KbMaintenanceMonthWorkItem workItem)
            {
                string ownerNodeId = workItem.OwnerNodeId?.Trim() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(ownerNodeId))
                    OwnerNodeIds.Add(ownerNodeId);

                string systemNodeId = workItem.SystemNodeId?.Trim() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(systemNodeId))
                    SystemNodeIds.Add(systemNodeId);
            }

            public bool HasOwner(string ownerNodeId) =>
                OwnerNodeIds.Contains(ownerNodeId);

            public bool HasSystem(string systemNodeId) =>
                SystemNodeIds.Contains(systemNodeId);

            public bool HasOnlySystem(string systemNodeId) =>
                SystemNodeIds.Count == 1 && SystemNodeIds.Contains(systemNodeId);

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
