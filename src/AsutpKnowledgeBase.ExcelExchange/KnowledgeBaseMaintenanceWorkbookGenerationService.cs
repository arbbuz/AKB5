using System.Globalization;
using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public sealed class KnowledgeBaseMaintenanceWorkbookGenerationService
    {
        private readonly KnowledgeBaseMaintenanceMonthlyPlannerService _plannerService;
        private readonly KnowledgeBaseMaintenanceMonthSheetModelBuilderService _sheetModelBuilderService;
        private readonly KnowledgeBaseMaintenanceAnnualWorkbookModelBuilderService _annualWorkbookModelBuilderService;
        private readonly KnowledgeBaseMaintenanceWorkbookExportService _workbookExportService;

        public KnowledgeBaseMaintenanceWorkbookGenerationService(
            KnowledgeBaseMaintenanceMonthlyPlannerService? plannerService = null,
            KnowledgeBaseMaintenanceMonthSheetModelBuilderService? sheetModelBuilderService = null,
            KnowledgeBaseMaintenanceAnnualWorkbookModelBuilderService? annualWorkbookModelBuilderService = null,
            KnowledgeBaseMaintenanceWorkbookExportService? workbookExportService = null)
        {
            _plannerService = plannerService ?? new KnowledgeBaseMaintenanceMonthlyPlannerService();
            _sheetModelBuilderService = sheetModelBuilderService ?? new KnowledgeBaseMaintenanceMonthSheetModelBuilderService();
            _annualWorkbookModelBuilderService = annualWorkbookModelBuilderService ?? new KnowledgeBaseMaintenanceAnnualWorkbookModelBuilderService();
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
            return GenerateMonthWorkbookCore(
                existingWorkbookPackage,
                year,
                month,
                totalMonthlyHourBudget,
                roots,
                maintenanceScheduleProfiles,
                singleMonthWorkbook: false);
        }

        public KnowledgeBaseMaintenanceWorkbookGenerationResult GenerateSingleMonthWorkbook(
            int year,
            int month,
            int totalMonthlyHourBudget,
            IReadOnlyList<KbNode>? roots,
            IReadOnlyList<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles)
        {
            return GenerateMonthWorkbookCore(
                existingWorkbookPackage: null,
                year,
                month,
                totalMonthlyHourBudget,
                roots,
                maintenanceScheduleProfiles,
                singleMonthWorkbook: true);
        }

        private KnowledgeBaseMaintenanceWorkbookGenerationResult GenerateMonthWorkbookCore(
            byte[]? existingWorkbookPackage,
            int year,
            int month,
            int totalMonthlyHourBudget,
            IReadOnlyList<KbNode>? roots,
            IReadOnlyList<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles,
            bool singleMonthWorkbook)
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
                _sheetModelBuilderService.Build(year, month, normalizedRoots, planResult, maintenanceScheduleProfiles);
            if (!sheetModelBuildResult.IsSuccess || sheetModelBuildResult.SheetModel == null)
            {
                return Failure(
                    string.IsNullOrWhiteSpace(sheetModelBuildResult.ErrorMessage)
                        ? "Не удалось построить модель листа графика ТО."
                        : sheetModelBuildResult.ErrorMessage,
                    planResult);
            }

            KnowledgeBaseMaintenanceWorkbookExportResult exportResult = singleMonthWorkbook
                ? _workbookExportService.ExportSingleMonth(sheetModelBuildResult.SheetModel)
                : _workbookExportService.ExportMonth(
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

        public KnowledgeBaseMaintenanceAnnualWorkbookGenerationResult GenerateAnnualWorkbook(
            int year,
            string workshopName,
            IReadOnlyList<KbNode>? roots,
            IReadOnlyList<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles)
        {
            KnowledgeBaseMaintenanceAnnualWorkbookModelBuildResult modelBuildResult =
                _annualWorkbookModelBuilderService.Build(year, workshopName, roots, maintenanceScheduleProfiles);
            if (!modelBuildResult.IsSuccess || modelBuildResult.WorkbookModel == null)
            {
                return AnnualFailure(
                    string.IsNullOrWhiteSpace(modelBuildResult.ErrorMessage)
                        ? "Не удалось построить модель годового графика ТО."
                        : modelBuildResult.ErrorMessage);
            }

            KnowledgeBaseMaintenanceWorkbookExportResult exportResult =
                _workbookExportService.ExportAnnual(modelBuildResult.WorkbookModel);
            if (!exportResult.IsSuccess || exportResult.WorkbookPackage == null)
            {
                return AnnualFailure(
                    string.IsNullOrWhiteSpace(exportResult.ErrorMessage)
                        ? "Не удалось подготовить книгу годового графика ТО."
                        : exportResult.ErrorMessage,
                    modelBuildResult.WorkbookModel);
            }

            return new KnowledgeBaseMaintenanceAnnualWorkbookGenerationResult
            {
                IsSuccess = true,
                WorkbookModel = modelBuildResult.WorkbookModel,
                WorkbookPackage = exportResult.WorkbookPackage
            };
        }

        public KnowledgeBaseMaintenanceYearWorkbookGenerationResult GenerateYearWorkbook(
            byte[]? existingWorkbookPackage,
            int year,
            int totalMonthlyHourBudget,
            IReadOnlyList<KbNode>? roots,
            IReadOnlyList<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles)
        {
            // Full-year generation rewrites every month, so start from the monthly template.
            // Existing packages are only meaningful for partial recalculation from a selected month.
            return GenerateYearWorkbookFromMonth(
                existingWorkbookPackage: null,
                year,
                startMonth: 1,
                totalMonthlyHourBudget,
                roots,
                maintenanceScheduleProfiles);
        }

        public KnowledgeBaseMaintenanceYearWorkbookGenerationResult GenerateYearWorkbookFromMonth(
            byte[]? existingWorkbookPackage,
            int year,
            int startMonth,
            int totalMonthlyHourBudget,
            IReadOnlyList<KbNode>? roots,
            IReadOnlyList<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles)
        {
            if (year < 1)
                return YearFailure("Год графика ТО должен быть положительным.");

            if (startMonth is < 1 or > 12)
                return YearFailure("Стартовый месяц графика ТО должен быть в диапазоне от 1 до 12.");

            KnowledgeBaseMaintenanceWorkbookExportResult preparationResult =
                _workbookExportService.PrepareYearRecalculationWorkbook(existingWorkbookPackage, startMonth);
            if (!preparationResult.IsSuccess)
            {
                return YearFailure(
                    string.IsNullOrWhiteSpace(preparationResult.ErrorMessage)
                        ? "Не удалось подготовить книгу графика ТО для пересчёта."
                        : preparationResult.ErrorMessage);
            }

            byte[]? workbookPackage = preparationResult.WorkbookPackage;
            var monthResults = new List<KnowledgeBaseMaintenanceYearWorkbookGenerationMonthResult>(13 - startMonth);

            for (int month = startMonth; month <= 12; month++)
            {
                KnowledgeBaseMaintenanceWorkbookGenerationResult monthResult = GenerateMonthWorkbook(
                    workbookPackage,
                    year,
                    month,
                    totalMonthlyHourBudget,
                    roots,
                    maintenanceScheduleProfiles);
                if (!monthResult.IsSuccess || monthResult.WorkbookPackage == null)
                {
                    string errorMessage = string.IsNullOrWhiteSpace(monthResult.ErrorMessage)
                        ? "Не удалось сформировать месячный лист графика ТО."
                        : monthResult.ErrorMessage;

                    return YearFailure(
                        $"Не удалось сформировать график ТО за {GetMonthName(month)} {year}: {errorMessage}",
                        failedMonth: month,
                        monthResults);
                }

                workbookPackage = monthResult.WorkbookPackage;
                monthResults.Add(new KnowledgeBaseMaintenanceYearWorkbookGenerationMonthResult
                {
                    Month = month,
                    PlanResult = monthResult.PlanResult,
                    SheetModel = monthResult.SheetModel
                });
            }

            return new KnowledgeBaseMaintenanceYearWorkbookGenerationResult
            {
                IsSuccess = true,
                MonthResults = monthResults,
                WorkbookPackage = workbookPackage
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

        private static KnowledgeBaseMaintenanceYearWorkbookGenerationResult YearFailure(
            string errorMessage,
            int failedMonth = 0,
            IReadOnlyList<KnowledgeBaseMaintenanceYearWorkbookGenerationMonthResult>? monthResults = null) =>
            new()
            {
                IsSuccess = false,
                ErrorMessage = errorMessage,
                FailedMonth = failedMonth,
                MonthResults = monthResults ?? Array.Empty<KnowledgeBaseMaintenanceYearWorkbookGenerationMonthResult>()
            };

        private static KnowledgeBaseMaintenanceAnnualWorkbookGenerationResult AnnualFailure(
            string errorMessage,
            KbMaintenanceAnnualWorkbookModel? workbookModel = null) =>
            new()
            {
                IsSuccess = false,
                ErrorMessage = errorMessage,
                WorkbookModel = workbookModel
            };

        private static string GetMonthName(int month)
        {
            if (month is < 1 or > 12)
                return month.ToString(CultureInfo.InvariantCulture);

            return CultureInfo.GetCultureInfo("ru-RU").DateTimeFormat.GetMonthName(month);
        }
    }
}
