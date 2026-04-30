using System.Globalization;
using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.UiServices
{
    public sealed class KnowledgeBaseMaintenanceWorkbookUiWorkflowService
    {
        private readonly KnowledgeBaseMaintenanceWorkbookGenerationService _generationService;
        private readonly KnowledgeBaseMaintenanceMonthWorkResolverService _workResolverService;
        private readonly KnowledgeBaseMaintenanceMonthDemandSummaryService _demandSummaryService;

        public KnowledgeBaseMaintenanceWorkbookUiWorkflowService(
            KnowledgeBaseMaintenanceWorkbookGenerationService? generationService = null,
            KnowledgeBaseMaintenanceMonthWorkResolverService? workResolverService = null,
            KnowledgeBaseMaintenanceMonthDemandSummaryService? demandSummaryService = null)
        {
            _generationService = generationService ?? new KnowledgeBaseMaintenanceWorkbookGenerationService();
            _workResolverService = workResolverService ?? new KnowledgeBaseMaintenanceMonthWorkResolverService();
            _demandSummaryService = demandSummaryService ?? new KnowledgeBaseMaintenanceMonthDemandSummaryService(_workResolverService);
        }

        public void Export(
            IWin32Window owner,
            string workshopName,
            IReadOnlyList<KbNode> roots,
            IReadOnlyList<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles,
            string currentDataPath,
            Action<string> setStatusText)
        {
            if (string.IsNullOrWhiteSpace(workshopName))
            {
                MessageBox.Show(
                    owner,
                    "Сначала выберите цех для формирования графика ТО.",
                    "График ТО",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

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
                Title = "Сохранить годовой график ТО",
                Filter = "Книги Excel (*.xlsx)|*.xlsx|Все файлы (*.*)|*.*",
                DefaultExt = "xlsx",
                AddExtension = true,
                OverwritePrompt = true,
                FileName = BuildSuggestedFileName(workshopName, exportDialog.SelectedYear)
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

                KnowledgeBaseMaintenanceWorkbookGenerationResult generationResult =
                    _generationService.GenerateMonthWorkbook(
                        existingWorkbookPackage,
                        exportDialog.SelectedYear,
                        exportDialog.SelectedMonth,
                        exportDialog.MonthlyBudgetHours,
                        roots,
                        maintenanceScheduleProfiles);

                if (!generationResult.IsSuccess || generationResult.WorkbookPackage == null)
                {
                    string errorMessage = string.IsNullOrWhiteSpace(generationResult.ErrorMessage)
                        ? "Не удалось сформировать график ТО."
                        : generationResult.ErrorMessage;
                    MessageBox.Show(
                        owner,
                        errorMessage,
                        "График ТО",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    setStatusText($"Ошибка формирования графика ТО: {errorMessage}");
                    return;
                }

                File.WriteAllBytes(saveDialog.FileName, generationResult.WorkbookPackage);
                MessageBox.Show(
                    owner,
                    "График ТО сформирован.",
                    "График ТО",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                setStatusText(
                    $"Сформирован график ТО: {Path.GetFileName(saveDialog.FileName)} ({exportDialog.SelectedMonth:D2}.{exportDialog.SelectedYear})");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    owner,
                    $"Ошибка формирования графика ТО: {ex.Message}",
                    "График ТО",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                setStatusText($"Ошибка формирования графика ТО: {ex.Message}");
            }
        }

        public void ExportYear(
            IWin32Window owner,
            string workshopName,
            IReadOnlyList<KbNode> roots,
            IReadOnlyList<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles,
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
                FileName = BuildSuggestedFileName(workshopName, exportDialog.SelectedYear)
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
                    _generationService.GenerateYearWorkbook(
                        existingWorkbookPackage,
                        exportDialog.SelectedYear,
                        exportDialog.MonthlyBudgetHours,
                        roots,
                        maintenanceScheduleProfiles);

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
                    $"Сформирован годовой график ТО: {Path.GetFileName(saveDialog.FileName)} ({exportDialog.SelectedYear}, {generationResult.MonthResults.Count} мес.)");
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

        public void RecalculateYearToDecember(
            IWin32Window owner,
            string workshopName,
            IReadOnlyList<KbNode> roots,
            IReadOnlyList<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles,
            string currentDataPath,
            Action<string> setStatusText)
        {
            if (string.IsNullOrWhiteSpace(workshopName))
            {
                MessageBox.Show(
                    owner,
                    "Сначала выберите цех для пересчёта графика ТО.",
                    "График ТО",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

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
                Title = "Выберите годовой график ТО для пересчёта",
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
                $"Пересчитать в выбранной книге листы с {startMonthName} по декабрь {recalculationDialog.SelectedYear} года?",
                "График ТО",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Question);
            if (confirmResult != DialogResult.OK)
                return;

            try
            {
                byte[] existingWorkbookPackage = ReadWorkbookPackage(openDialog.FileName);

                KnowledgeBaseMaintenanceYearWorkbookGenerationResult generationResult =
                    _generationService.GenerateYearWorkbookFromMonth(
                        existingWorkbookPackage,
                        recalculationDialog.SelectedYear,
                        recalculationDialog.SelectedStartMonth,
                        recalculationDialog.MonthlyBudgetHours,
                        roots,
                        maintenanceScheduleProfiles);

                if (!generationResult.IsSuccess || generationResult.WorkbookPackage == null)
                {
                    string errorMessage = string.IsNullOrWhiteSpace(generationResult.ErrorMessage)
                        ? "Не удалось пересчитать график ТО до конца года."
                        : generationResult.ErrorMessage;
                    MessageBox.Show(
                        owner,
                        errorMessage,
                        "График ТО",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    setStatusText($"Ошибка пересчёта графика ТО: {errorMessage}");
                    return;
                }

                File.WriteAllBytes(openDialog.FileName, generationResult.WorkbookPackage);
                MessageBox.Show(
                    owner,
                    $"График ТО пересчитан с {startMonthName} по декабрь.",
                    "График ТО",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                setStatusText(
                    $"Пересчитан график ТО: {Path.GetFileName(openDialog.FileName)} ({startMonthName} - декабрь {recalculationDialog.SelectedYear}, {generationResult.MonthResults.Count} мес.)");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    owner,
                    $"Ошибка пересчёта графика ТО: {ex.Message}",
                    "График ТО",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                setStatusText($"Ошибка пересчёта графика ТО: {ex.Message}");
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

        private static byte[] ReadWorkbookPackage(string path)
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var memory = new MemoryStream();
            stream.CopyTo(memory);
            return memory.ToArray();
        }

        private static string BuildSuggestedFileName(string workshopName, int year)
        {
            string safeWorkshopName = string.Concat(
                (workshopName ?? string.Empty)
                    .Trim()
                    .Select(character => Path.GetInvalidFileNameChars().Contains(character) ? '_' : character));
            if (string.IsNullOrWhiteSpace(safeWorkshopName))
                safeWorkshopName = "Цех";

            return $"{safeWorkshopName}_ГрафикТО_{year}.xlsx";
        }

        private static string GetMonthName(int month)
        {
            if (month is < 1 or > 12)
                return month.ToString(CultureInfo.InvariantCulture);

            return CultureInfo.GetCultureInfo("ru-RU").DateTimeFormat.GetMonthName(month);
        }
    }
}
