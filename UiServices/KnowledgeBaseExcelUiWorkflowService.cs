using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.UiServices
{
    public class KnowledgeBaseExcelImportUiWorkflowContext
    {
        public IWin32Window Owner { get; init; } = null!;

        public string CurrentDataPath { get; init; } = string.Empty;

        public Func<string, bool> ConfirmContinueBeforeImport { get; init; } = null!;

        public Func<string, string, bool> OfferSnapshotBeforeDangerousImport { get; init; } = null!;

        public Func<SavedData, KnowledgeBaseFileSaveResult> ReplaceAllData { get; init; } = null!;

        public Action<string> SetStatusText { get; init; } = null!;
    }

    /// <summary>
    /// Координирует WinForms-специфичные сценарии импорта и экспорта базы
    /// в Excel workbook формата xlsx.
    /// </summary>
    public class KnowledgeBaseExcelUiWorkflowService
    {
        private readonly KnowledgeBaseExcelExchangePluginLoader _excelExchangePluginLoader;

        public KnowledgeBaseExcelUiWorkflowService(KnowledgeBaseExcelExchangePluginLoader excelExchangePluginLoader)
        {
            _excelExchangePluginLoader = excelExchangePluginLoader;
        }

        public void Export(
            IWin32Window owner,
            SavedData data,
            string currentDataPath,
            Action<string> setStatusText)
        {
            if (!TryEnsureExcelModuleAvailable(owner, setStatusText))
                return;

            using var dialog = new SaveFileDialog
            {
                Title = "Экспортировать базу в книгу Excel",
                Filter = "Книги Excel (*.xlsx)|*.xlsx|Все файлы (*.*)|*.*",
                DefaultExt = "xlsx",
                AddExtension = true,
                OverwritePrompt = true,
                FileName = BuildSuggestedFileName(currentDataPath)
            };

            string? directory = Path.GetDirectoryName(currentDataPath);
            if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
                dialog.InitialDirectory = directory;

            if (dialog.ShowDialog(owner) != DialogResult.OK)
                return;

            var result = _excelExchangePluginLoader.Export(data, dialog.FileName);
            if (result.IsSuccess)
            {
                MessageBox.Show(
                    owner,
                    "Экспорт в Excel завершён.",
                    "Экспорт",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                setStatusText($"📊 Экспортировано в Excel: {Path.GetFileName(dialog.FileName)}");
                return;
            }

            MessageBox.Show(
                owner,
                $"Ошибка экспорта: {result.ErrorMessage}",
                "Ошибка экспорта",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            setStatusText($"❌ Ошибка экспорта в Excel: {result.ErrorMessage}");
        }

        public void Import(KnowledgeBaseExcelImportUiWorkflowContext context)
        {
            if (!TryEnsureExcelModuleAvailable(context.Owner, context.SetStatusText))
                return;

            if (!context.ConfirmContinueBeforeImport("импортом базы из Excel"))
                return;

            using var dialog = new OpenFileDialog
            {
                Title = "Импортировать базу из книги Excel",
                Filter = "Книги Excel (*.xlsx)|*.xlsx|Все файлы (*.*)|*.*",
                CheckFileExists = true
            };

            string? directory = Path.GetDirectoryName(context.CurrentDataPath);
            if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
                dialog.InitialDirectory = directory;

            if (dialog.ShowDialog(context.Owner) != DialogResult.OK)
                return;

            var importResult = _excelExchangePluginLoader.Import(dialog.FileName);
            if (!importResult.IsSuccess || importResult.Data == null)
            {
                MessageBox.Show(
                    context.Owner,
                    $"Ошибка импорта: {importResult.ErrorMessage}",
                    "Ошибка импорта",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                context.SetStatusText($"❌ Ошибка импорта из Excel: {importResult.ErrorMessage}");
                return;
            }

            if (!context.OfferSnapshotBeforeDangerousImport(
                    "заменой текущей базы из Excel",
                    $"Перед заменой текущей базы из Excel: {Path.GetFileName(dialog.FileName)}"))
            {
                return;
            }

            var applyResult = context.ReplaceAllData(importResult.Data);
            if (!applyResult.IsSuccess)
            {
                MessageBox.Show(
                    context.Owner,
                    $"Ошибка сохранения импортированных данных: {applyResult.ErrorMessage}",
                    "Ошибка импорта",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                context.SetStatusText($"❌ Ошибка применения Excel-импорта: {applyResult.ErrorMessage}");
                return;
            }

            MessageBox.Show(
                context.Owner,
                "Импорт из Excel завершён.",
                "Импорт",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            context.SetStatusText($"📥 Импортировано из Excel: {Path.GetFileName(dialog.FileName)}");
        }

        private static string BuildSuggestedFileName(string currentDataPath)
        {
            string fileName = Path.GetFileName(currentDataPath);
            if (string.IsNullOrWhiteSpace(fileName))
                return "ASUTP_KnowledgeBase.xlsx";

            return Path.ChangeExtension(fileName, "xlsx");
        }

        private bool TryEnsureExcelModuleAvailable(IWin32Window owner, Action<string> setStatusText)
        {
            if (_excelExchangePluginLoader.TryEnsureAvailable(out string errorMessage))
                return true;

            MessageBox.Show(
                owner,
                errorMessage,
                "Excel",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            setStatusText($"Excel недоступен: {errorMessage}");
            return false;
        }
    }
}
