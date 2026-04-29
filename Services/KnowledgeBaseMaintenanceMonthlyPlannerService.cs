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
        private const int MaxHoursPerWorkingDay = 8;
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
            int calendarCapacityHours = workingDays.Count * MaxHoursPerWorkingDay;
            int availableCapacityHours = Math.Min(totalMonthlyHourBudget, calendarCapacityHours);

            if (requestedHours > availableCapacityHours)
            {
                return Failure(
                    $"Невозможно разместить {requestedHours} ч в {month:D2}.{year}: доступно только {availableCapacityHours} ч.",
                    workingDays.Count,
                    requestedHours,
                    totalMonthlyHourBudget,
                    calendarCapacityHours,
                    availableCapacityHours,
                    normalizedWorkItems);
            }

            var plannedDays = new List<KbMaintenanceMonthPlanDay>();
            if (requestedHours == 0)
            {
                return Success(
                    workingDays.Count,
                    requestedHours,
                    totalMonthlyHourBudget,
                    calendarCapacityHours,
                    availableCapacityHours,
                    normalizedWorkItems,
                    plannedDays);
            }

            int dayIndex = 0;
            foreach (KbMaintenanceMonthWorkItem workItem in normalizedWorkItems)
            {
                int remainingHours = workItem.Hours;
                while (remainingHours > 0)
                {
                    while (dayIndex < plannedDays.Count && plannedDays[dayIndex].TotalHours >= MaxHoursPerWorkingDay)
                        dayIndex++;

                    if (dayIndex >= workingDays.Count)
                    {
                        return Failure(
                            $"Не удалось распределить все часы в {month:D2}.{year} при дневном лимите {MaxHoursPerWorkingDay} ч.",
                            workingDays.Count,
                            requestedHours,
                            totalMonthlyHourBudget,
                            calendarCapacityHours,
                            availableCapacityHours,
                            normalizedWorkItems);
                    }

                    if (dayIndex == plannedDays.Count)
                        plannedDays.Add(new KbMaintenanceMonthPlanDay { Date = workingDays[dayIndex] });

                    KbMaintenanceMonthPlanDay dayPlan = plannedDays[dayIndex];
                    int freeHours = MaxHoursPerWorkingDay - dayPlan.TotalHours;
                    int assignedHours = Math.Min(remainingHours, freeHours);

                    dayPlan.Assignments.Add(new KbMaintenanceMonthPlanAssignment
                    {
                        Date = dayPlan.Date,
                        OwnerNodeId = workItem.OwnerNodeId?.Trim() ?? string.Empty,
                        NodeName = workItem.NodeName?.Trim() ?? string.Empty,
                        WorkKind = workItem.WorkKind,
                        Hours = assignedHours
                    });
                    dayPlan.TotalHours += assignedHours;
                    remainingHours -= assignedHours;
                }
            }

            return Success(
                workingDays.Count,
                requestedHours,
                totalMonthlyHourBudget,
                calendarCapacityHours,
                availableCapacityHours,
                normalizedWorkItems,
                plannedDays);
        }

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
    }
}
