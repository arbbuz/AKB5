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

        public List<KbMaintenanceMonthWorkItem> PlannedWorkItems { get; init; } = new();

        public List<KbMaintenanceMonthPlanDay> PlannedDays { get; init; } = new();
    }

    public sealed class KnowledgeBaseMaintenanceMonthlyPlannerService
    {
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
                    normalizedWorkItems);
            }

            List<DayPlanBuilder> dayBuilders = workingDays
                .Select(static date => new DayPlanBuilder(date))
                .ToList();

            foreach (KbMaintenanceMonthWorkItem workItem in OrderWorkItems(normalizedWorkItems))
            {
                if (workItem.Hours <= 0)
                    continue;

                DayPlanBuilder selectedDay = SelectBestDay(dayBuilders, workItem.WorkKind);
                selectedDay.Assignments.Add(new KbMaintenanceMonthPlanAssignment
                {
                    Date = selectedDay.Date,
                    OwnerNodeId = workItem.OwnerNodeId?.Trim() ?? string.Empty,
                    NodeName = workItem.NodeName?.Trim() ?? string.Empty,
                    WorkKind = workItem.WorkKind,
                    Hours = workItem.Hours
                });
                selectedDay.TotalHours += workItem.Hours;
                if (IsMajorWork(workItem.WorkKind))
                    selectedDay.HasMajorWork = true;
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
                normalizedWorkItems,
                plannedDays);
        }

        private static IEnumerable<KbMaintenanceMonthWorkItem> OrderWorkItems(
            IReadOnlyList<KbMaintenanceMonthWorkItem> workItems) =>
            workItems
                .OrderBy(static item => GetWorkPriority(item.WorkKind))
                .ThenByDescending(static item => item.Hours)
                .ThenBy(static item => item.OwnerNodeId, StringComparer.Ordinal)
                .ThenBy(static item => item.NodeName, StringComparer.Ordinal);

        private static DayPlanBuilder SelectBestDay(
            List<DayPlanBuilder> dayBuilders,
            KbMaintenanceWorkKind workKind)
        {
            IEnumerable<DayPlanBuilder> candidates = dayBuilders;
            if (IsMajorWork(workKind))
            {
                List<DayPlanBuilder> daysWithoutMajorWork = dayBuilders
                    .Where(static day => !day.HasMajorWork)
                    .ToList();
                if (daysWithoutMajorWork.Count > 0)
                    candidates = daysWithoutMajorWork;
            }

            // Balance monthly hours and only softly separate ТО2/ТО3 when there is still a free day without major work.
            return candidates
                .OrderBy(static day => day.TotalHours)
                .ThenBy(static day => day.Date)
                .First();
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

        private static KnowledgeBaseMaintenanceMonthPlanResult Success(
            int workingDayCount,
            int requestedHours,
            int budgetHours,
            int calendarCapacityHours,
            int availableCapacityHours,
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
