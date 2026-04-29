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

        private int ResolveSuggestedMonthlyBudget(
            int year,
            int month,
            IReadOnlyList<KbNode> roots,
            IReadOnlyList<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles)
        {
            return _demandSummaryService.Build(year, month, roots, maintenanceScheduleProfiles).TotalHours;
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
    }
}
