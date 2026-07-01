using System.Globalization;
using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.UiServices
{
    public sealed class KnowledgeBaseMaintenanceWorkbookUiWorkflowService
    {
        private readonly KnowledgeBaseExcelExchangePluginLoader _excelExchangePluginLoader;
        private readonly IKnowledgeBaseMaintenanceWorkbookGenerator? _generationService;
        private readonly KnowledgeBaseMaintenanceMonthWorkResolverService _workResolverService;
        private readonly KnowledgeBaseMaintenanceMonthDemandSummaryService _demandSummaryService;

        public KnowledgeBaseMaintenanceWorkbookUiWorkflowService(
            KnowledgeBaseExcelExchangePluginLoader? excelExchangePluginLoader = null,
            IKnowledgeBaseMaintenanceWorkbookGenerator? generationService = null,
            KnowledgeBaseMaintenanceMonthWorkResolverService? workResolverService = null,
            KnowledgeBaseMaintenanceMonthDemandSummaryService? demandSummaryService = null)
        {
            _excelExchangePluginLoader = excelExchangePluginLoader ?? new KnowledgeBaseExcelExchangePluginLoader();
            _generationService = generationService;
            _workResolverService = workResolverService ?? new KnowledgeBaseMaintenanceMonthWorkResolverService();
            _demandSummaryService = demandSummaryService ?? new KnowledgeBaseMaintenanceMonthDemandSummaryService(_workResolverService);
        }

        public void Export(
            IWin32Window owner,
            string workshopName,
            IReadOnlyList<KbNode> roots,
            IReadOnlyList<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles,
            IReadOnlyList<KbProductionCalendarYear>? productionCalendarYears,
            string currentDataPath,
            Action<string> setStatusText,
            KnowledgeBaseMaintenancePlanningMode planningMode = KnowledgeBaseMaintenancePlanningMode.Default)
        {
            string caption = BuildPlanningCaption(planningMode);
            string modeSuffix = BuildPlanningModeDisplaySuffix(planningMode);
            if (string.IsNullOrWhiteSpace(workshopName))
            {
                MessageBox.Show(
                    owner,
                    "Сначала выберите цех для формирования графика ТО.",
                    caption,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            if (!TryEnsureExcelModuleAvailable(owner, setStatusText))
                return;

            DateTime now = DateTime.Now;
            int initialYear = now.Year;
            int initialMonth = now.Month;
            int initialBudget = ResolveSuggestedMonthlyBudget(
                initialYear,
                initialMonth,
                roots,
                maintenanceScheduleProfiles);

            using var exportDialog = new KnowledgeBaseMaintenanceWorkbookExportDialog(
                workshopName,
                initialYear,
                initialMonth,
                initialBudget,
                (year, month) => _demandSummaryService.Build(year, month, roots, maintenanceScheduleProfiles));
            if (exportDialog.ShowDialog(owner) != DialogResult.OK)
                return;

            using var saveDialog = new SaveFileDialog
            {
                Title = $"Сохранить график ТО на месяц{modeSuffix}",
                Filter = "Книги Excel (*.xlsx)|*.xlsx|Все файлы (*.*)|*.*",
                DefaultExt = "xlsx",
                AddExtension = true,
                OverwritePrompt = true,
                FileName = BuildSuggestedMonthFileName(
                    workshopName,
                    exportDialog.SelectedYear,
                    exportDialog.SelectedMonth,
                    planningMode)
            };

            string? directory = Path.GetDirectoryName(currentDataPath);
            if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
                saveDialog.InitialDirectory = directory;

            if (saveDialog.ShowDialog(owner) != DialogResult.OK)
                return;

            try
            {
                KnowledgeBaseMaintenanceWorkbookGenerationResult generationResult =
                    GenerateSingleMonthWorkbook(
                        exportDialog.SelectedYear,
                        exportDialog.SelectedMonth,
                        exportDialog.MonthlyBudgetHours,
                        roots,
                        maintenanceScheduleProfiles,
                        productionCalendarYears,
                        planningMode);

                if (!generationResult.IsSuccess || generationResult.WorkbookPackage == null)
                {
                    string errorMessage = string.IsNullOrWhiteSpace(generationResult.ErrorMessage)
                        ? "Не удалось сформировать график ТО."
                        : generationResult.ErrorMessage;
                    MessageBox.Show(
                        owner,
                        errorMessage,
                        caption,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    setStatusText($"Ошибка формирования графика ТО{modeSuffix}: {errorMessage}");
                    return;
                }

                File.WriteAllBytes(saveDialog.FileName, generationResult.WorkbookPackage);
                MessageBox.Show(
                    owner,
                    $"График ТО{modeSuffix} сформирован.",
                    caption,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                setStatusText(
                    $"Сформирован график ТО{modeSuffix}: {Path.GetFileName(saveDialog.FileName)} ({exportDialog.SelectedMonth:D2}.{exportDialog.SelectedYear})");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    owner,
                    $"Ошибка формирования графика ТО{modeSuffix}: {ex.Message}",
                    caption,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                setStatusText($"Ошибка формирования графика ТО{modeSuffix}: {ex.Message}");
            }
        }

        public void ExportYear(
            IWin32Window owner,
            string workshopName,
            IReadOnlyList<KbNode> roots,
            IReadOnlyList<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles,
            IReadOnlyList<KbProductionCalendarYear>? productionCalendarYears,
            string currentDataPath,
            Action<string> setStatusText)
        {
            if (string.IsNullOrWhiteSpace(workshopName))
            {
                MessageBox.Show(
                    owner,
                    "Сначала выберите цех для формирования годового графика ТО.",
                    "График ТО",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            if (!TryEnsureExcelModuleAvailable(owner, setStatusText))
                return;

            DateTime now = DateTime.Now;
            int initialYear = now.Year;

            using var exportDialog = new KnowledgeBaseMaintenanceAnnualWorkbookExportDialog(
                workshopName,
                initialYear,
                (year, month) => _demandSummaryService.Build(year, month, roots, maintenanceScheduleProfiles));
            if (exportDialog.ShowDialog(owner) != DialogResult.OK)
                return;

            using var saveDialog = new SaveFileDialog
            {
                Title = "Сохранить годовой график ТО",
                Filter = "Книги Excel (*.xlsx)|*.xlsx|Все файлы (*.*)|*.*",
                DefaultExt = "xlsx",
                AddExtension = true,
                OverwritePrompt = true,
                FileName = BuildSuggestedAnnualFileName(workshopName, exportDialog.SelectedYear)
            };

            string? directory = Path.GetDirectoryName(currentDataPath);
            if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
                saveDialog.InitialDirectory = directory;

            if (saveDialog.ShowDialog(owner) != DialogResult.OK)
                return;

            try
            {
                KnowledgeBaseMaintenanceAnnualWorkbookGenerationResult generationResult =
                    GenerateAnnualWorkbook(
                        exportDialog.SelectedYear,
                        workshopName,
                        roots,
                        maintenanceScheduleProfiles,
                        productionCalendarYears);

                if (!generationResult.IsSuccess || generationResult.WorkbookPackage == null)
                {
                    string errorMessage = string.IsNullOrWhiteSpace(generationResult.ErrorMessage)
                        ? "Не удалось сформировать годовой график ТО."
                        : generationResult.ErrorMessage;
                    MessageBox.Show(
                        owner,
                        errorMessage,
                        "График ТО",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    setStatusText($"Ошибка формирования годового графика ТО: {errorMessage}");
                    return;
                }

                File.WriteAllBytes(saveDialog.FileName, generationResult.WorkbookPackage);
                MessageBox.Show(
                    owner,
                    "Годовой график ТО сформирован.",
                    "График ТО",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                setStatusText(
                    $"Сформирован годовой график ТО: {Path.GetFileName(saveDialog.FileName)} ({exportDialog.SelectedYear}, {generationResult.WorkbookModel?.TotalHours ?? 0} ч)");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    owner,
                    $"Ошибка формирования годового графика ТО: {ex.Message}",
                    "График ТО",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                setStatusText($"Ошибка формирования годового графика ТО: {ex.Message}");
            }
        }

        public void ExportYearMonthly(
            IWin32Window owner,
            string workshopName,
            IReadOnlyList<KbNode> roots,
            IReadOnlyList<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles,
            IReadOnlyList<KbProductionCalendarYear>? productionCalendarYears,
            string currentDataPath,
            Action<string> setStatusText,
            KnowledgeBaseMaintenancePlanningMode planningMode = KnowledgeBaseMaintenancePlanningMode.Default)
        {
            string caption = BuildPlanningCaption(planningMode);
            string modeSuffix = BuildPlanningModeDisplaySuffix(planningMode);
            if (string.IsNullOrWhiteSpace(workshopName))
            {
                MessageBox.Show(
                    owner,
                    "Сначала выберите цех для формирования графика ТО помесячно.",
                    caption,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            if (!TryEnsureExcelModuleAvailable(owner, setStatusText))
                return;

            DateTime now = DateTime.Now;
            int initialYear = now.Year;
            int initialBudget = ResolveSuggestedYearlyMonthlyBudget(
                initialYear,
                roots,
                maintenanceScheduleProfiles);

            using var exportDialog = new KnowledgeBaseMaintenanceYearWorkbookExportDialog(
                workshopName,
                initialYear,
                initialBudget,
                (year, month) => _demandSummaryService.Build(year, month, roots, maintenanceScheduleProfiles),
                $"Сформировать график ТО помесячно{modeSuffix}");
            if (exportDialog.ShowDialog(owner) != DialogResult.OK)
                return;

            using var saveDialog = new SaveFileDialog
            {
                Title = $"Сохранить график ТО помесячно{modeSuffix}",
                Filter = "Книги Excel (*.xlsx)|*.xlsx|Все файлы (*.*)|*.*",
                DefaultExt = "xlsx",
                AddExtension = true,
                OverwritePrompt = true,
                FileName = BuildSuggestedYearMonthlyFileName(workshopName, exportDialog.SelectedYear, planningMode)
            };

            string? directory = Path.GetDirectoryName(currentDataPath);
            if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
                saveDialog.InitialDirectory = directory;

            if (saveDialog.ShowDialog(owner) != DialogResult.OK)
                return;

            try
            {
                byte[]? existingWorkbookPackage = File.Exists(saveDialog.FileName)
                    ? File.ReadAllBytes(saveDialog.FileName)
                    : null;

                KnowledgeBaseMaintenanceYearWorkbookGenerationResult generationResult =
                    GenerateYearWorkbook(
                        existingWorkbookPackage,
                        exportDialog.SelectedYear,
                        exportDialog.MonthlyBudgetHours,
                        roots,
                        maintenanceScheduleProfiles,
                        productionCalendarYears,
                        planningMode);

                if (!generationResult.IsSuccess || generationResult.WorkbookPackage == null)
                {
                    string errorMessage = string.IsNullOrWhiteSpace(generationResult.ErrorMessage)
                        ? "Не удалось сформировать график ТО помесячно."
                        : generationResult.ErrorMessage;
                    MessageBox.Show(
                        owner,
                        errorMessage,
                        caption,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    setStatusText($"Ошибка формирования графика ТО помесячно{modeSuffix}: {errorMessage}");
                    return;
                }

                File.WriteAllBytes(saveDialog.FileName, generationResult.WorkbookPackage);
                MessageBox.Show(
                    owner,
                    $"График ТО помесячно{modeSuffix} сформирован.",
                    caption,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                setStatusText(
                    $"Сформирован график ТО помесячно{modeSuffix}: {Path.GetFileName(saveDialog.FileName)} ({exportDialog.SelectedYear}, {generationResult.MonthResults.Count} мес.)");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    owner,
                    $"Ошибка формирования графика ТО помесячно{modeSuffix}: {ex.Message}",
                    caption,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                setStatusText($"Ошибка формирования графика ТО помесячно{modeSuffix}: {ex.Message}");
            }
        }

        public void RecalculateYearToDecember(
            IWin32Window owner,
            string workshopName,
            IReadOnlyList<KbNode> roots,
            IReadOnlyList<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles,
            IReadOnlyList<KbProductionCalendarYear>? productionCalendarYears,
            string currentDataPath,
            Action<string> setStatusText,
            KnowledgeBaseMaintenancePlanningMode planningMode = KnowledgeBaseMaintenancePlanningMode.Default)
        {
            string caption = BuildPlanningCaption(planningMode);
            string modeSuffix = BuildPlanningModeDisplaySuffix(planningMode);
            if (string.IsNullOrWhiteSpace(workshopName))
            {
                MessageBox.Show(
                    owner,
                    "Сначала выберите цех для пересчёта графика ТО.",
                    caption,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            if (!TryEnsureExcelModuleAvailable(owner, setStatusText))
                return;

            DateTime now = DateTime.Now;
            int initialYear = now.Year;
            int initialStartMonth = now.Month;
            int initialBudget = ResolveSuggestedYearlyMonthlyBudget(
                initialYear,
                roots,
                maintenanceScheduleProfiles,
                initialStartMonth);

            using var recalculationDialog = new KnowledgeBaseMaintenanceYearWorkbookRecalculationDialog(
                workshopName,
                initialYear,
                initialStartMonth,
                initialBudget,
                (year, month) => _demandSummaryService.Build(year, month, roots, maintenanceScheduleProfiles));
            if (recalculationDialog.ShowDialog(owner) != DialogResult.OK)
                return;

            using var openDialog = new OpenFileDialog
            {
                Title = $"Выберите годовой график ТО для пересчёта{modeSuffix}",
                Filter = "Книги Excel (*.xlsx)|*.xlsx|Все файлы (*.*)|*.*",
                CheckFileExists = true
            };

            string? directory = Path.GetDirectoryName(currentDataPath);
            if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
                openDialog.InitialDirectory = directory;

            if (openDialog.ShowDialog(owner) != DialogResult.OK)
                return;

            string startMonthName = GetMonthName(recalculationDialog.SelectedStartMonth);
            DialogResult confirmResult = MessageBox.Show(
                owner,
                $"Пересчитать в выбранной книге листы с {startMonthName} по декабрь {recalculationDialog.SelectedYear} года{modeSuffix}?",
                caption,
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Question);
            if (confirmResult != DialogResult.OK)
                return;

            try
            {
                byte[] existingWorkbookPackage = ReadWorkbookPackage(openDialog.FileName);

                KnowledgeBaseMaintenanceYearWorkbookGenerationResult generationResult =
                    GenerateYearWorkbookFromMonth(
                        existingWorkbookPackage,
                        recalculationDialog.SelectedYear,
                        recalculationDialog.SelectedStartMonth,
                        recalculationDialog.MonthlyBudgetHours,
                        roots,
                        maintenanceScheduleProfiles,
                        productionCalendarYears,
                        planningMode);

                if (!generationResult.IsSuccess || generationResult.WorkbookPackage == null)
                {
                    string errorMessage = string.IsNullOrWhiteSpace(generationResult.ErrorMessage)
                        ? "Не удалось пересчитать график ТО до конца года."
                        : generationResult.ErrorMessage;
                    MessageBox.Show(
                        owner,
                        errorMessage,
                        caption,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    setStatusText($"Ошибка пересчёта графика ТО{modeSuffix}: {errorMessage}");
                    return;
                }

                File.WriteAllBytes(openDialog.FileName, generationResult.WorkbookPackage);
                MessageBox.Show(
                    owner,
                    $"График ТО{modeSuffix} пересчитан с {startMonthName} по декабрь.",
                    caption,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                setStatusText(
                    $"Пересчитан график ТО{modeSuffix}: {Path.GetFileName(openDialog.FileName)} ({startMonthName} - декабрь {recalculationDialog.SelectedYear}, {generationResult.MonthResults.Count} мес.)");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    owner,
                    $"Ошибка пересчёта графика ТО{modeSuffix}: {ex.Message}",
                    caption,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                setStatusText($"Ошибка пересчёта графика ТО{modeSuffix}: {ex.Message}");
            }
        }

        private int ResolveSuggestedMonthlyBudget(
            int year,
            int month,
            IReadOnlyList<KbNode> roots,
            IReadOnlyList<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles)
        {
            return _demandSummaryService.Build(year, month, roots, maintenanceScheduleProfiles).TotalHours;
        }

        private int ResolveSuggestedYearlyMonthlyBudget(
            int year,
            IReadOnlyList<KbNode> roots,
            IReadOnlyList<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles,
            int startMonth = 1)
        {
            int maxMonthlyDemand = 0;
            for (int month = Math.Clamp(startMonth, 1, 12); month <= 12; month++)
            {
                maxMonthlyDemand = Math.Max(
                    maxMonthlyDemand,
                    ResolveSuggestedMonthlyBudget(year, month, roots, maintenanceScheduleProfiles));
            }

            return maxMonthlyDemand;
        }

        private KnowledgeBaseMaintenanceWorkbookGenerationResult GenerateSingleMonthWorkbook(
            int year,
            int month,
            int totalMonthlyHourBudget,
            IReadOnlyList<KbNode>? roots,
            IReadOnlyList<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles,
            IReadOnlyList<KbProductionCalendarYear>? productionCalendarYears,
            KnowledgeBaseMaintenancePlanningMode planningMode = KnowledgeBaseMaintenancePlanningMode.Default) =>
            _generationService?.GenerateSingleMonthWorkbook(
                year,
                month,
                totalMonthlyHourBudget,
                roots,
                maintenanceScheduleProfiles,
                productionCalendarYears,
                planningMode)
            ?? _excelExchangePluginLoader.GenerateSingleMonthWorkbook(
                year,
                month,
                totalMonthlyHourBudget,
                roots,
                maintenanceScheduleProfiles,
                productionCalendarYears,
                planningMode);

        private KnowledgeBaseMaintenanceAnnualWorkbookGenerationResult GenerateAnnualWorkbook(
            int year,
            string workshopName,
            IReadOnlyList<KbNode>? roots,
            IReadOnlyList<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles,
            IReadOnlyList<KbProductionCalendarYear>? productionCalendarYears) =>
            _generationService?.GenerateAnnualWorkbook(
                year,
                workshopName,
                roots,
                maintenanceScheduleProfiles,
                productionCalendarYears)
            ?? _excelExchangePluginLoader.GenerateAnnualWorkbook(
                year,
                workshopName,
                roots,
                maintenanceScheduleProfiles,
                productionCalendarYears);

        private KnowledgeBaseMaintenanceYearWorkbookGenerationResult GenerateYearWorkbook(
            byte[]? existingWorkbookPackage,
            int year,
            int totalMonthlyHourBudget,
            IReadOnlyList<KbNode>? roots,
            IReadOnlyList<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles,
            IReadOnlyList<KbProductionCalendarYear>? productionCalendarYears,
            KnowledgeBaseMaintenancePlanningMode planningMode = KnowledgeBaseMaintenancePlanningMode.Default) =>
            _generationService?.GenerateYearWorkbook(
                existingWorkbookPackage,
                year,
                totalMonthlyHourBudget,
                roots,
                maintenanceScheduleProfiles,
                productionCalendarYears,
                planningMode)
            ?? _excelExchangePluginLoader.GenerateYearWorkbook(
                existingWorkbookPackage,
                year,
                totalMonthlyHourBudget,
                roots,
                maintenanceScheduleProfiles,
                productionCalendarYears,
                planningMode);

        private KnowledgeBaseMaintenanceYearWorkbookGenerationResult GenerateYearWorkbookFromMonth(
            byte[]? existingWorkbookPackage,
            int year,
            int startMonth,
            int totalMonthlyHourBudget,
            IReadOnlyList<KbNode>? roots,
            IReadOnlyList<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles,
            IReadOnlyList<KbProductionCalendarYear>? productionCalendarYears,
            KnowledgeBaseMaintenancePlanningMode planningMode = KnowledgeBaseMaintenancePlanningMode.Default) =>
            _generationService?.GenerateYearWorkbookFromMonth(
                existingWorkbookPackage,
                year,
                startMonth,
                totalMonthlyHourBudget,
                roots,
                maintenanceScheduleProfiles,
                productionCalendarYears,
                planningMode)
            ?? _excelExchangePluginLoader.GenerateYearWorkbookFromMonth(
                existingWorkbookPackage,
                year,
                startMonth,
                totalMonthlyHourBudget,
                roots,
                maintenanceScheduleProfiles,
                productionCalendarYears,
                planningMode);

        private bool TryEnsureExcelModuleAvailable(IWin32Window owner, Action<string> setStatusText)
        {
            if (_generationService != null ||
                _excelExchangePluginLoader.TryEnsureAvailable(out string errorMessage))
            {
                return true;
            }

            MessageBox.Show(
                owner,
                errorMessage,
                "Excel",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            setStatusText($"Excel недоступен: {errorMessage}");
            return false;
        }

        private static byte[] ReadWorkbookPackage(string path)
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var memory = new MemoryStream();
            stream.CopyTo(memory);
            return memory.ToArray();
        }

        private static string BuildSuggestedMonthFileName(
            string workshopName,
            int year,
            int month,
            KnowledgeBaseMaintenancePlanningMode planningMode = KnowledgeBaseMaintenancePlanningMode.Default) =>
            $"{BuildSafeWorkshopName(workshopName)}_ГрафикТО{BuildPlanningModeFileNameSuffix(planningMode)}_{year}_{month:D2}.xlsx";

        private static string BuildSuggestedAnnualFileName(string workshopName, int year) =>
            $"{BuildSafeWorkshopName(workshopName)}_ГодовойГрафикТО_{year}.xlsx";

        private static string BuildSuggestedYearMonthlyFileName(
            string workshopName,
            int year,
            KnowledgeBaseMaintenancePlanningMode planningMode = KnowledgeBaseMaintenancePlanningMode.Default) =>
            $"{BuildSafeWorkshopName(workshopName)}_ГрафикТО_помесячно{BuildPlanningModeFileNameSuffix(planningMode)}_{year}.xlsx";

        private static string BuildPlanningCaption(KnowledgeBaseMaintenancePlanningMode planningMode) =>
            planningMode switch
            {
                KnowledgeBaseMaintenancePlanningMode.BalancedV2 => "График ТО v2",
                KnowledgeBaseMaintenancePlanningMode.SequentialV3 => "График ТО v3",
                _ => "График ТО"
            };

        private static string BuildPlanningModeDisplaySuffix(KnowledgeBaseMaintenancePlanningMode planningMode) =>
            planningMode switch
            {
                KnowledgeBaseMaintenancePlanningMode.BalancedV2 => " v2",
                KnowledgeBaseMaintenancePlanningMode.SequentialV3 => " v3",
                _ => string.Empty
            };

        private static string BuildPlanningModeFileNameSuffix(KnowledgeBaseMaintenancePlanningMode planningMode) =>
            planningMode switch
            {
                KnowledgeBaseMaintenancePlanningMode.BalancedV2 => "_v2",
                KnowledgeBaseMaintenancePlanningMode.SequentialV3 => "_v3",
                _ => string.Empty
            };

        private static string BuildSafeWorkshopName(string workshopName)
        {
            string safeWorkshopName = string.Concat(
                (workshopName ?? string.Empty)
                    .Trim()
                    .Select(character => Path.GetInvalidFileNameChars().Contains(character) ? '_' : character));
            if (string.IsNullOrWhiteSpace(safeWorkshopName))
                safeWorkshopName = "Цех";

            return safeWorkshopName;
        }

        private static string GetMonthName(int month)
        {
            if (month is < 1 or > 12)
                return month.ToString(CultureInfo.InvariantCulture);

            return CultureInfo.GetCultureInfo("ru-RU").DateTimeFormat.GetMonthName(month);
        }
    }
}
