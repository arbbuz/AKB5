using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public interface IKnowledgeBaseMaintenanceWorkbookGenerator
    {
        KnowledgeBaseMaintenanceWorkbookGenerationResult GenerateSingleMonthWorkbook(
            int year,
            int month,
            int totalMonthlyHourBudget,
            IReadOnlyList<KbNode>? roots,
            IReadOnlyList<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles,
            IReadOnlyList<KbProductionCalendarYear>? productionCalendarYears,
            KnowledgeBaseMaintenancePlanningMode planningMode = KnowledgeBaseMaintenancePlanningMode.Default);

        KnowledgeBaseMaintenanceAnnualWorkbookGenerationResult GenerateAnnualWorkbook(
            int year,
            string workshopName,
            IReadOnlyList<KbNode>? roots,
            IReadOnlyList<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles,
            IReadOnlyList<KbProductionCalendarYear>? productionCalendarYears);

        KnowledgeBaseMaintenanceYearWorkbookGenerationResult GenerateYearWorkbook(
            byte[]? existingWorkbookPackage,
            int year,
            int totalMonthlyHourBudget,
            IReadOnlyList<KbNode>? roots,
            IReadOnlyList<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles,
            IReadOnlyList<KbProductionCalendarYear>? productionCalendarYears,
            KnowledgeBaseMaintenancePlanningMode planningMode = KnowledgeBaseMaintenancePlanningMode.Default);

        KnowledgeBaseMaintenanceYearWorkbookGenerationResult GenerateYearWorkbookFromMonth(
            byte[]? existingWorkbookPackage,
            int year,
            int startMonth,
            int totalMonthlyHourBudget,
            IReadOnlyList<KbNode>? roots,
            IReadOnlyList<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles,
            IReadOnlyList<KbProductionCalendarYear>? productionCalendarYears,
            KnowledgeBaseMaintenancePlanningMode planningMode = KnowledgeBaseMaintenancePlanningMode.Default);
    }

    public sealed class KnowledgeBaseMaintenanceWorkbookGenerationResult
    {
        public bool IsSuccess { get; init; }

        public string ErrorMessage { get; init; } = string.Empty;

        public KnowledgeBaseMaintenanceMonthPlanResult? PlanResult { get; init; }

        public KbMaintenanceMonthSheetModel? SheetModel { get; init; }

        public byte[]? WorkbookPackage { get; init; }
    }

    public sealed class KnowledgeBaseMaintenanceYearWorkbookGenerationMonthResult
    {
        public int Month { get; init; }

        public KnowledgeBaseMaintenanceMonthPlanResult? PlanResult { get; init; }

        public KbMaintenanceMonthSheetModel? SheetModel { get; init; }
    }

    public sealed class KnowledgeBaseMaintenanceYearWorkbookGenerationResult
    {
        public bool IsSuccess { get; init; }

        public string ErrorMessage { get; init; } = string.Empty;

        public int FailedMonth { get; init; }

        public IReadOnlyList<KnowledgeBaseMaintenanceYearWorkbookGenerationMonthResult> MonthResults { get; init; } =
            Array.Empty<KnowledgeBaseMaintenanceYearWorkbookGenerationMonthResult>();

        public byte[]? WorkbookPackage { get; init; }
    }

    public sealed class KnowledgeBaseMaintenanceAnnualWorkbookGenerationResult
    {
        public bool IsSuccess { get; init; }

        public string ErrorMessage { get; init; } = string.Empty;

        public KbMaintenanceAnnualWorkbookModel? WorkbookModel { get; init; }

        public byte[]? WorkbookPackage { get; init; }
    }
}
