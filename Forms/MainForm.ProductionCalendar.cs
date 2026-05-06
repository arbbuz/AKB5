using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase
{
    public partial class MainForm
    {
        private void EditProductionCalendar(object? sender, EventArgs e)
        {
            SaveCurrentWorkshopState();

            using var dialog = new KnowledgeBaseProductionCalendarForm(_session.Config.ProductionCalendarYears);
            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            ReplaceProductionCalendarYears(
                dialog.ResultYears,
                "Производственный календарь обновлён.");

            MessageBox.Show(
                this,
                BuildProductionCalendarSummary(dialog.ResultYears, "Настройка производственного календаря сохранена."),
                "Производственный календарь",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void ImportProductionCalendar(object? sender, EventArgs e)
        {
            SaveCurrentWorkshopState();

            using var dialog = new OpenFileDialog
            {
                Title = "Импортировать производственный календарь из JSON",
                Filter = "JSON-файлы (*.json)|*.json|Все файлы (*.*)|*.*",
                CheckFileExists = true
            };

            string? directory = Path.GetDirectoryName(CurrentDataPath);
            if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
                dialog.InitialDirectory = directory;

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            KnowledgeBaseProductionCalendarJsonImportResult importResult;
            try
            {
                byte[] jsonBytes;
                using (var stream = new FileStream(dialog.FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                using (var memory = new MemoryStream())
                {
                    stream.CopyTo(memory);
                    jsonBytes = memory.ToArray();
                }

                importResult = _productionCalendarJsonImportService.ImportJson(jsonBytes);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    $"Ошибка чтения JSON-файла: {ex.Message}",
                    "Производственный календарь",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                SetLastActionText($"Ошибка импорта производственного календаря: {ex.Message}");
                return;
            }

            if (!importResult.IsSuccess)
            {
                MessageBox.Show(
                    this,
                    importResult.ErrorMessage,
                    "Производственный календарь",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                SetLastActionText($"Ошибка импорта производственного календаря: {importResult.ErrorMessage}");
                return;
            }

            List<KbProductionCalendarYear> mergedYears = MergeProductionCalendarYears(
                _session.Config.ProductionCalendarYears,
                importResult.ProductionCalendarYears);
            ReplaceProductionCalendarYears(
                mergedYears,
                $"Импортирован производственный календарь: {importResult.ImportedYearCount} г.");

            MessageBox.Show(
                this,
                BuildProductionCalendarSummary(
                    mergedYears,
                    $"Импорт производственного календаря завершён. Импортировано лет: {importResult.ImportedYearCount}."),
                "Производственный календарь",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void ImportProductionCalendarPdf(object? sender, EventArgs e)
        {
            SaveCurrentWorkshopState();

            using var dialog = new OpenFileDialog
            {
                Title = "Импортировать производственный календарь из PDF",
                Filter = "PDF-файлы (*.pdf)|*.pdf|Все файлы (*.*)|*.*",
                CheckFileExists = true
            };

            string? directory = Path.GetDirectoryName(CurrentDataPath);
            if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
                dialog.InitialDirectory = directory;

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            KnowledgeBaseProductionCalendarPdfImportResult importResult;
            try
            {
                byte[] pdfBytes;
                using (var stream = new FileStream(dialog.FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                using (var memory = new MemoryStream())
                {
                    stream.CopyTo(memory);
                    pdfBytes = memory.ToArray();
                }

                importResult = _productionCalendarPdfImportService.ImportPdf(pdfBytes);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    $"Ошибка чтения PDF-файла: {ex.Message}",
                    "Производственный календарь",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                SetLastActionText($"Ошибка импорта производственного календаря из PDF: {ex.Message}");
                return;
            }

            if (!importResult.IsSuccess)
            {
                MessageBox.Show(
                    this,
                    importResult.ErrorMessage,
                    "Производственный календарь",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                SetLastActionText($"Ошибка импорта производственного календаря из PDF: {importResult.ErrorMessage}");
                return;
            }

            using var previewDialog = new KnowledgeBaseProductionCalendarPdfImportPreviewForm(
                importResult,
                Path.GetFileName(dialog.FileName));
            if (previewDialog.ShowDialog(this) != DialogResult.OK)
                return;

            List<KbProductionCalendarYear> mergedYears = MergeProductionCalendarYears(
                _session.Config.ProductionCalendarYears,
                previewDialog.ResultYears);
            ReplaceProductionCalendarYears(
                mergedYears,
                $"Импортирован производственный календарь из PDF: {importResult.ImportedYearCount} г.");

            MessageBox.Show(
                this,
                BuildProductionCalendarSummary(
                    mergedYears,
                    $"Импорт производственного календаря из PDF завершён. Импортировано лет: {importResult.ImportedYearCount}."),
                "Производственный календарь",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void ReplaceProductionCalendarYears(
            IEnumerable<KbProductionCalendarYear> productionCalendarYears,
            string statusText)
        {
            KbConfig updatedConfig = CloneConfig(_session.Config);
            updatedConfig.ProductionCalendarYears =
                KnowledgeBaseDataService.NormalizeProductionCalendarYears(productionCalendarYears);

            _session.UpdateConfig(KnowledgeBaseDataService.NormalizeConfig(updatedConfig));
            UpdateDirtyState();
            UpdateUI(refreshSelectedNodeState: false);
            SetLastActionText(statusText);
        }

        private static List<KbProductionCalendarYear> MergeProductionCalendarYears(
            IReadOnlyList<KbProductionCalendarYear>? currentYears,
            IReadOnlyList<KbProductionCalendarYear>? importedYears)
        {
            Dictionary<int, KbProductionCalendarYear> merged = KnowledgeBaseDataService
                .NormalizeProductionCalendarYears(currentYears)
                .ToDictionary(static year => year.Year, CloneProductionCalendarYear);

            foreach (KbProductionCalendarYear year in importedYears ?? Array.Empty<KbProductionCalendarYear>())
                merged[year.Year] = CloneProductionCalendarYear(year);

            return KnowledgeBaseDataService.NormalizeProductionCalendarYears(merged.Values);
        }

        private static KbConfig CloneConfig(KbConfig config) =>
            new()
            {
                MaxLevels = config.MaxLevels,
                LevelNames = (config.LevelNames ?? new List<string>()).ToList(),
                ProductionCalendarYears = (config.ProductionCalendarYears ?? new List<KbProductionCalendarYear>())
                    .Select(CloneProductionCalendarYear)
                    .ToList()
            };

        private static KbProductionCalendarYear CloneProductionCalendarYear(KbProductionCalendarYear year) =>
            new()
            {
                Year = year.Year,
                AdditionalNonWorkingDays = (year.AdditionalNonWorkingDays ?? new List<DateOnly>())
                    .OrderBy(static date => date)
                    .ToList(),
                AdditionalWorkingDays = (year.AdditionalWorkingDays ?? new List<DateOnly>())
                    .OrderBy(static date => date)
                    .ToList()
            };

        private static string BuildProductionCalendarSummary(
            List<KbProductionCalendarYear> years,
            string title)
        {
            var lines = new List<string>
            {
                title,
                $"Настроено лет: {years.Count}"
            };

            foreach (KbProductionCalendarYear year in years.OrderBy(static year => year.Year).Take(12))
            {
                lines.Add(
                    $"- {year.Year}: доп. нерабочих дней {year.AdditionalNonWorkingDays.Count}, доп. рабочих дней {year.AdditionalWorkingDays.Count}");
            }

            if (years.Count > 12)
                lines.Add($"- ... ещё {years.Count - 12}");

            return string.Join(Environment.NewLine, lines);
        }
    }
}
