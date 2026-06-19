using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public sealed class KnowledgeBaseExcelExchangePlugin :
        IKnowledgeBaseExcelExchangeService,
        IKnowledgeBaseMaintenanceScheduleNormImporter,
        IKnowledgeBaseMaintenanceWorkbookGenerator,
        IKnowledgeBaseMaintenanceYearScheduleSourceService,
        IKnowledgeBaseMaintenanceYearScheduleSourceExchange
    {
        private readonly KnowledgeBaseExcelExchangeService _excelExchangeService;
        private readonly KnowledgeBaseMaintenanceScheduleNormImportService _maintenanceScheduleNormImportService = new();
        private readonly KnowledgeBaseMaintenanceYearScheduleSourceService _maintenanceYearScheduleSourceService = new();
        private readonly KnowledgeBaseMaintenanceYearScheduleSourceExchangeService _maintenanceYearScheduleSourceExchangeService = new();

        public KnowledgeBaseExcelExchangePlugin()
            : this(NullAppLogger.Instance)
        {
        }

        public KnowledgeBaseExcelExchangePlugin(IAppLogger? logger)
        {
            _excelExchangeService = new KnowledgeBaseExcelExchangeService(logger);
        }

        public byte[] BuildWorkbookPackage(SavedData data) =>
            _excelExchangeService.BuildWorkbookPackage(data);

        public KnowledgeBaseExcelExportResult Export(SavedData data, string path) =>
            _excelExchangeService.Export(data, path);

        public KnowledgeBaseExcelImportResult Import(string path) =>
            _excelExchangeService.Import(path);

        public KnowledgeBaseExcelImportResult ImportFromPackage(byte[] packageBytes) =>
            _excelExchangeService.ImportFromPackage(packageBytes);

        KnowledgeBaseMaintenanceScheduleNormImportResult IKnowledgeBaseMaintenanceScheduleNormImporter.ImportWorkbook(
            byte[] packageBytes,
            IReadOnlyList<KbNode>? roots,
            IReadOnlyList<KbMaintenanceScheduleProfile>? existingProfiles) =>
            _maintenanceScheduleNormImportService.ImportWorkbook(packageBytes, roots, existingProfiles);

        public KnowledgeBaseMaintenanceWorkbookGenerationResult GenerateSingleMonthWorkbook(
            int year,
            int month,
            int totalMonthlyHourBudget,
            IReadOnlyList<KbNode>? roots,
            IReadOnlyList<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles,
            IReadOnlyList<KbProductionCalendarYear>? productionCalendarYears) =>
            CreateWorkbookGenerationService(productionCalendarYears).GenerateSingleMonthWorkbook(
                year,
                month,
                totalMonthlyHourBudget,
                roots,
                maintenanceScheduleProfiles);

        public KnowledgeBaseMaintenanceAnnualWorkbookGenerationResult GenerateAnnualWorkbook(
            int year,
            string workshopName,
            IReadOnlyList<KbNode>? roots,
            IReadOnlyList<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles,
            IReadOnlyList<KbProductionCalendarYear>? productionCalendarYears) =>
            CreateWorkbookGenerationService(productionCalendarYears).GenerateAnnualWorkbook(
                year,
                workshopName,
                roots,
                maintenanceScheduleProfiles);

        public KnowledgeBaseMaintenanceYearWorkbookGenerationResult GenerateYearWorkbook(
            byte[]? existingWorkbookPackage,
            int year,
            int totalMonthlyHourBudget,
            IReadOnlyList<KbNode>? roots,
            IReadOnlyList<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles,
            IReadOnlyList<KbProductionCalendarYear>? productionCalendarYears) =>
            CreateWorkbookGenerationService(productionCalendarYears).GenerateYearWorkbook(
                existingWorkbookPackage,
                year,
                totalMonthlyHourBudget,
                roots,
                maintenanceScheduleProfiles);

        public KnowledgeBaseMaintenanceYearWorkbookGenerationResult GenerateYearWorkbookFromMonth(
            byte[]? existingWorkbookPackage,
            int year,
            int startMonth,
            int totalMonthlyHourBudget,
            IReadOnlyList<KbNode>? roots,
            IReadOnlyList<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles,
            IReadOnlyList<KbProductionCalendarYear>? productionCalendarYears) =>
            CreateWorkbookGenerationService(productionCalendarYears).GenerateYearWorkbookFromMonth(
                existingWorkbookPackage,
                year,
                startMonth,
                totalMonthlyHourBudget,
                roots,
                maintenanceScheduleProfiles);

        public List<KnowledgeBaseMaintenanceYearScheduleSourceRow> BuildRows(
            IReadOnlyList<KbNode>? roots,
            IReadOnlyList<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles) =>
            _maintenanceYearScheduleSourceService.BuildRows(roots, maintenanceScheduleProfiles);

        public KnowledgeBaseMaintenanceYearScheduleSourceApplyResult ApplyRows(
            IReadOnlyList<KnowledgeBaseMaintenanceYearScheduleSourceRow>? rows,
            IReadOnlyList<KbNode>? roots,
            IReadOnlyList<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles) =>
            _maintenanceYearScheduleSourceService.ApplyRows(rows, roots, maintenanceScheduleProfiles);

        public KnowledgeBaseMaintenanceYearScheduleSourceExportResult ExportWorkbook(
            IReadOnlyList<KbNode>? roots,
            IReadOnlyList<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles) =>
            _maintenanceYearScheduleSourceExchangeService.ExportWorkbook(roots, maintenanceScheduleProfiles);

        KnowledgeBaseMaintenanceYearScheduleSourceImportResult IKnowledgeBaseMaintenanceYearScheduleSourceExchange.ImportWorkbook(
            byte[] workbookPackage,
            IReadOnlyList<KbNode>? roots,
            IReadOnlyList<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles) =>
            _maintenanceYearScheduleSourceExchangeService.ImportWorkbook(workbookPackage, roots, maintenanceScheduleProfiles);

        private static KnowledgeBaseMaintenanceWorkbookGenerationService CreateWorkbookGenerationService(
            IReadOnlyList<KbProductionCalendarYear>? productionCalendarYears)
        {
            var calendarService = new KnowledgeBaseRussianProductionCalendarService(productionCalendarYears);
            var plannerService = new KnowledgeBaseMaintenanceMonthlyPlannerService(calendarService);
            return new KnowledgeBaseMaintenanceWorkbookGenerationService(plannerService);
        }
    }
}
