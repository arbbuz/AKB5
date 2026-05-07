using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase
{
    public partial class MainForm
    {
        private void EditEquipmentCatalog(object? sender, EventArgs e)
        {
            SaveCurrentWorkshopState();

            using var dialog = new KnowledgeBaseEquipmentCatalogForm(_session.EquipmentCatalogItems);
            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            _session.ReplaceEquipmentCatalogItems(dialog.ResultItems);
            UpdateDirtyState();
            UpdateUI(refreshSelectedNodeState: false);
            SetLastActionText($"Каталог оборудования обновлён: {_session.EquipmentCatalogItems.Count} зап.");
        }

        private void ExportCatalogTemplates(object? sender, EventArgs e)
        {
            SaveCurrentWorkshopState();

            KnowledgeBaseCatalogTemplateExportResult exportResult =
                _catalogTemplateExchangeService.ExportJson(
                    _session.EquipmentCatalogItems,
                    _session.ObjectTemplates);
            if (!exportResult.IsSuccess)
            {
                MessageBox.Show(
                    this,
                    exportResult.ErrorMessage,
                    "Каталог и шаблоны",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                SetLastActionText($"Ошибка экспорта каталога и шаблонов: {exportResult.ErrorMessage}");
                return;
            }

            using var dialog = new SaveFileDialog
            {
                Title = "Экспортировать каталог и шаблоны в JSON",
                Filter = "JSON-файлы (*.json)|*.json|Все файлы (*.*)|*.*",
                DefaultExt = "json",
                AddExtension = true,
                OverwritePrompt = true,
                FileName = BuildCatalogTemplateExchangeFileName()
            };

            string? directory = Path.GetDirectoryName(CurrentDataPath);
            if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
                dialog.InitialDirectory = directory;

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            try
            {
                string? targetDirectory = Path.GetDirectoryName(dialog.FileName);
                if (!string.IsNullOrWhiteSpace(targetDirectory))
                    Directory.CreateDirectory(targetDirectory);

                File.WriteAllBytes(dialog.FileName, exportResult.JsonBytes);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    $"Ошибка записи JSON-файла: {ex.Message}",
                    "Каталог и шаблоны",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                SetLastActionText($"Ошибка экспорта каталога и шаблонов: {ex.Message}");
                return;
            }

            MessageBox.Show(
                this,
                BuildCatalogTemplateExportSummary(exportResult),
                "Каталог и шаблоны",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            SetLastActionText(
                $"Экспортированы каталог и шаблоны: {exportResult.ExportedCatalogItemCount} зап., {exportResult.ExportedTemplateCount} шабл.");
        }

        private void ImportCatalogTemplates(object? sender, EventArgs e)
        {
            SaveCurrentWorkshopState();

            using var dialog = new OpenFileDialog
            {
                Title = "Импортировать каталог и шаблоны из JSON",
                Filter = "JSON-файлы (*.json)|*.json|Все файлы (*.*)|*.*",
                CheckFileExists = true
            };

            string? directory = Path.GetDirectoryName(CurrentDataPath);
            if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
                dialog.InitialDirectory = directory;

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            KnowledgeBaseCatalogTemplateImportResult importResult;
            try
            {
                byte[] jsonBytes;
                using (var stream = new FileStream(dialog.FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                using (var memory = new MemoryStream())
                {
                    stream.CopyTo(memory);
                    jsonBytes = memory.ToArray();
                }

                importResult = _catalogTemplateExchangeService.ImportJson(
                    jsonBytes,
                    _session.EquipmentCatalogItems,
                    _session.ObjectTemplates);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    $"Ошибка чтения JSON-файла: {ex.Message}",
                    "Каталог и шаблоны",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                SetLastActionText($"Ошибка импорта каталога и шаблонов: {ex.Message}");
                return;
            }

            if (!importResult.IsSuccess)
            {
                MessageBox.Show(
                    this,
                    importResult.ErrorMessage,
                    "Каталог и шаблоны",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                SetLastActionText($"Ошибка импорта каталога и шаблонов: {importResult.ErrorMessage}");
                return;
            }

            if (importResult.AddedCatalogItemCount > 0 || importResult.AddedTemplateCount > 0)
            {
                _session.ReplaceEquipmentCatalogItems(importResult.EquipmentCatalogItems);
                _session.ReplaceObjectTemplates(importResult.ObjectTemplates);
                _fileUiWorkflowService.AppendChangeLog(
                    "catalog-template-import",
                    "Импортированы каталог оборудования и шаблоны объектов.",
                    $"+{importResult.AddedCatalogItemCount} зап. каталога, +{importResult.AddedTemplateCount} шабл.");
                UpdateDirtyState();
                UpdateUI(refreshSelectedNodeState: false);
            }

            MessageBox.Show(
                this,
                BuildCatalogTemplateImportSummary(importResult),
                "Каталог и шаблоны",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            SetLastActionText(
                $"Импорт каталога и шаблонов: +{importResult.AddedCatalogItemCount} зап., +{importResult.AddedTemplateCount} шабл.");
        }

        private string BuildCatalogTemplateExchangeFileName()
        {
            string sourceName = Path.GetFileNameWithoutExtension(CurrentDataPath);
            if (string.IsNullOrWhiteSpace(sourceName))
                sourceName = "ASUTP_KnowledgeBase";

            return $"{sourceName}-catalog-templates.json";
        }

        private static string BuildCatalogTemplateExportSummary(
            KnowledgeBaseCatalogTemplateExportResult result) =>
            string.Join(
                Environment.NewLine,
                "Экспорт каталога и шаблонов завершён.",
                $"Записей каталога: {result.ExportedCatalogItemCount}",
                $"Шаблонов объектов: {result.ExportedTemplateCount}");

        private static string BuildCatalogTemplateImportSummary(
            KnowledgeBaseCatalogTemplateImportResult result) =>
            string.Join(
                Environment.NewLine,
                "Импорт каталога и шаблонов завершён.",
                $"Прочитано записей каталога: {result.ImportedCatalogItemCount}",
                $"Добавлено записей каталога: {result.AddedCatalogItemCount}",
                $"Пропущено записей каталога: {result.SkippedCatalogItemCount}",
                $"Прочитано шаблонов объектов: {result.ImportedTemplateCount}",
                $"Добавлено шаблонов объектов: {result.AddedTemplateCount}",
                $"Пропущено шаблонов объектов: {result.SkippedTemplateCount}");
    }
}
