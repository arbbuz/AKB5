using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public sealed class KnowledgeBaseMaintenanceWorkbookGenerationResult
    {
        public bool IsSuccess { get; init; }

        public string ErrorMessage { get; init; } = string.Empty;

        public KnowledgeBaseMaintenanceMonthPlanResult? PlanResult { get; init; }

        public KbMaintenanceMonthSheetModel? SheetModel { get; init; }

        public byte[]? WorkbookPackage { get; init; }
    }

    public sealed class KnowledgeBaseMaintenanceWorkbookGenerationService
    {
        private readonly KnowledgeBaseMaintenanceMonthlyPlannerService _plannerService;
        private readonly KnowledgeBaseMaintenanceMonthSheetModelBuilderService _sheetModelBuilderService;
        private readonly KnowledgeBaseMaintenanceWorkbookExportService _workbookExportService;

        public KnowledgeBaseMaintenanceWorkbookGenerationService(
            KnowledgeBaseMaintenanceMonthlyPlannerService? plannerService = null,
            KnowledgeBaseMaintenanceMonthSheetModelBuilderService? sheetModelBuilderService = null,
            KnowledgeBaseMaintenanceWorkbookExportService? workbookExportService = null)
        {
            _plannerService = plannerService ?? new KnowledgeBaseMaintenanceMonthlyPlannerService();
            _sheetModelBuilderService = sheetModelBuilderService ?? new KnowledgeBaseMaintenanceMonthSheetModelBuilderService();
            _workbookExportService = workbookExportService ?? new KnowledgeBaseMaintenanceWorkbookExportService();
        }

        public KnowledgeBaseMaintenanceWorkbookGenerationResult GenerateMonthWorkbook(
            byte[]? existingWorkbookPackage,
            int year,
            int month,
            int totalMonthlyHourBudget,
            IReadOnlyList<KbNode>? roots,
            IReadOnlyList<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles)
        {
            IReadOnlyList<KbNode> normalizedRoots = roots ?? Array.Empty<KbNode>();

            KnowledgeBaseMaintenanceMonthPlanResult planResult = _plannerService.PlanMonth(
                year,
                month,
                totalMonthlyHourBudget,
                normalizedRoots,
                maintenanceScheduleProfiles);
            if (!planResult.IsSuccess)
            {
                return Failure(
                    string.IsNullOrWhiteSpace(planResult.ErrorMessage)
                        ? "Не удалось сформировать месячный план ТО."
                        : planResult.ErrorMessage,
                    planResult);
            }

            KnowledgeBaseMaintenanceMonthSheetModelBuildResult sheetModelBuildResult =
                _sheetModelBuilderService.Build(year, month, normalizedRoots, planResult);
            if (!sheetModelBuildResult.IsSuccess || sheetModelBuildResult.SheetModel == null)
            {
                return Failure(
                    string.IsNullOrWhiteSpace(sheetModelBuildResult.ErrorMessage)
                        ? "Не удалось построить модель листа графика ТО."
                        : sheetModelBuildResult.ErrorMessage,
                    planResult);
            }

            KnowledgeBaseMaintenanceWorkbookExportResult exportResult = _workbookExportService.ExportMonth(
                existingWorkbookPackage,
                sheetModelBuildResult.SheetModel);
            if (!exportResult.IsSuccess || exportResult.WorkbookPackage == null)
            {
                return Failure(
                    string.IsNullOrWhiteSpace(exportResult.ErrorMessage)
                        ? "Не удалось подготовить книгу графика ТО."
                        : exportResult.ErrorMessage,
                    planResult,
                    sheetModelBuildResult.SheetModel);
            }

            return new KnowledgeBaseMaintenanceWorkbookGenerationResult
            {
                IsSuccess = true,
                PlanResult = planResult,
                SheetModel = sheetModelBuildResult.SheetModel,
                WorkbookPackage = exportResult.WorkbookPackage
            };
        }

        private static KnowledgeBaseMaintenanceWorkbookGenerationResult Failure(
            string errorMessage,
            KnowledgeBaseMaintenanceMonthPlanResult? planResult = null,
            KbMaintenanceMonthSheetModel? sheetModel = null) =>
            new()
            {
                IsSuccess = false,
                ErrorMessage = errorMessage,
                PlanResult = planResult,
                SheetModel = sheetModel
            };
    }
}
