using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.UiServices
{
    public class KnowledgeBaseFileUiState
    {
        public bool IsDirty { get; init; }

        public bool RequiresSave { get; init; }

        public string CurrentWorkshop { get; init; } = string.Empty;

        public string LastSavedWorkshop { get; init; } = string.Empty;
    }

    public class KnowledgeBaseFileUiWorkflowContext
    {
        public IWin32Window Owner { get; init; } = null!;

        public Func<List<KbNode>> GetCurrentTreeData { get; init; } = null!;

        public Action SaveCurrentWorkshopState { get; init; } = null!;

        public Action UpdateDirtyState { get; init; } = null!;

        public Func<KnowledgeBaseFileUiState> GetUiState { get; init; } = null!;

        public Action OnSuccessfulLoad { get; init; } = null!;

        public Action UpdateUi { get; init; } = null!;

        public Action<string> SetStatusText { get; init; } = null!;
    }

    /// <summary>
    /// Координирует WinForms-специфичные file/session сценарии:
    /// диалоги открытия/сохранения, prompt'ы и close handling поверх file-workflow service.
    /// </summary>
    public class KnowledgeBaseFileUiWorkflowService
    {
        private readonly KnowledgeBaseFileWorkflowService _fileWorkflowService;
        private readonly KnowledgeBaseFormStateService _formStateService;

        public KnowledgeBaseFileUiWorkflowService(
            KnowledgeBaseFileWorkflowService fileWorkflowService,
            KnowledgeBaseFormStateService formStateService)
        {
            _fileWorkflowService = fileWorkflowService;
            _formStateService = formStateService;
        }

        public string CurrentDataPath => _fileWorkflowService.SavePath;

        public string CurrentDataFileName => Path.GetFileName(CurrentDataPath);

        public bool LoadData(
            KnowledgeBaseFileUiWorkflowContext context,
            bool createDefaultIfMissing = true,
            bool fallbackToDefaultOnError = true)
        {
            var result = _fileWorkflowService.Load(createDefaultIfMissing, fallbackToDefaultOnError);

            switch (result.Outcome)
            {
                case KnowledgeBaseFileLoadOutcome.FileMissingError:
                    MessageBox.Show(
                        context.Owner,
                        $"Файл '{CurrentDataPath}' не найден.",
                        "Файл не найден",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    context.UpdateUi();
                    context.SetStatusText("⚠️ Файл базы не найден");
                    return false;

                case KnowledgeBaseFileLoadOutcome.LoadError:
                    MessageBox.Show(
                        context.Owner,
                        BuildLoadFailureMessage(result),
                        "Ошибка загрузки",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    context.UpdateUi();
                    context.SetStatusText("❌ Ошибка загрузки базы");
                    return false;

                case KnowledgeBaseFileLoadOutcome.CreatedDefaultAfterError:
                    HandleSuccessfulLoad(context);
                    MessageBox.Show(
                        context.Owner,
                        BuildLoadFailureMessage(result),
                        "Ошибка загрузки",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    context.SetStatusText("⚠️ Загружена пустая база из-за ошибки чтения");
                    return true;

                case KnowledgeBaseFileLoadOutcome.CreatedDefaultAndSaved:
                    HandleSuccessfulLoad(context);
                    context.SetStatusText("🆕 Создана новая база данных");
                    return true;

                case KnowledgeBaseFileLoadOutcome.CreatedDefaultUnsaved:
                    HandleSuccessfulLoad(context);
                    context.SetStatusText("⚠️ База создана в памяти, но не сохранена на диск");
                    if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
                    {
                        MessageBox.Show(
                            context.Owner,
                            $"Ошибка сохранения: {result.ErrorMessage}",
                            "Ошибка сохранения",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }

                    return true;

                case KnowledgeBaseFileLoadOutcome.LoadedBackup:
                    HandleSuccessfulLoad(context);
                    MessageBox.Show(
                        context.Owner,
                        BuildBackupLoadMessage(result),
                        "Загружена резервная копия",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    context.SetStatusText($"⚠️ Загружена резервная копия: {Path.GetFileName(result.SourcePath)}");
                    return true;

                case KnowledgeBaseFileLoadOutcome.LoadedExisting:
                    HandleSuccessfulLoad(context);
                    context.SetStatusText($"📂 Загружен цех: {context.GetUiState().CurrentWorkshop}");
                    return true;

                default:
                    return false;
            }
        }

        public void OpenDatabase(KnowledgeBaseFileUiWorkflowContext context)
        {
            if (!ConfirmContinueWithUnsavedChanges(context, "открытием другой базы"))
                return;

            using var dialog = new OpenFileDialog
            {
                Title = "Открыть базу знаний",
                CheckFileExists = true
            };
            ConfigureJsonDialog(dialog);

            if (dialog.ShowDialog(context.Owner) != DialogResult.OK)
                return;

            string previousPath = CurrentDataPath;
            _fileWorkflowService.SavePath = dialog.FileName;

            if (!LoadData(context, createDefaultIfMissing: false, fallbackToDefaultOnError: false))
            {
                _fileWorkflowService.SavePath = previousPath;
                context.UpdateUi();
            }
        }

        public void ReloadDatabase(KnowledgeBaseFileUiWorkflowContext context)
        {
            if (!ConfirmContinueWithUnsavedChanges(context, "перезагрузкой базы из файла"))
                return;

            if (LoadData(context, createDefaultIfMissing: false, fallbackToDefaultOnError: false))
                context.SetStatusText(BuildReloadSuccessMessage(context));
        }

        public void SaveCurrentDatabase(KnowledgeBaseFileUiWorkflowContext context)
        {
            if (SaveAllData(context, showSuccessMessage: true, showErrorMessage: true))
                context.SetStatusText($"✅ Данные сохранены: {CurrentDataFileName}");
        }

        public bool ConfirmContinueBeforeReplace(
            KnowledgeBaseFileUiWorkflowContext context,
            string actionDescription) =>
            ConfirmContinueWithUnsavedChanges(context, actionDescription);

        public void SaveDatabaseAs(KnowledgeBaseFileUiWorkflowContext context)
        {
            using var dialog = new SaveFileDialog
            {
                Title = "Сохранить базу как",
                OverwritePrompt = true
            };
            ConfigureJsonDialog(dialog);

            if (dialog.ShowDialog(context.Owner) != DialogResult.OK)
                return;

            string previousPath = CurrentDataPath;
            _fileWorkflowService.SavePath = dialog.FileName;

            if (!SaveAllData(context, showSuccessMessage: true, showErrorMessage: true))
            {
                _fileWorkflowService.SavePath = previousPath;
                context.UpdateUi();
                return;
            }

            context.SetStatusText($"✅ База сохранена как: {CurrentDataFileName}");
        }

        public KnowledgeBaseFileSaveResult ReplaceAllData(
            KnowledgeBaseFileUiWorkflowContext context,
            SavedData data)
        {
            var result = _fileWorkflowService.ReplaceAllData(data);
            if (result.IsSuccess)
                HandleSuccessfulLoad(context);

            return result;
        }

        public void HandleFormClosing(KnowledgeBaseFileUiWorkflowContext context, FormClosingEventArgs e)
        {
            context.SaveCurrentWorkshopState();
            context.UpdateDirtyState();

            var state = context.GetUiState();
            if (_formStateService.RequiresSavePromptOnClose(state.IsDirty, state.RequiresSave))
            {
                var result = MessageBox.Show(
                    context.Owner,
                    "Есть несохранённые изменения. Сохранить перед закрытием?",
                    "Закрытие приложения",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }

                if (result == DialogResult.Yes &&
                    !SaveAllData(context, showSuccessMessage: false, showErrorMessage: true))
                {
                    e.Cancel = true;
                }

                return;
            }

            if (_formStateService.ShouldSaveSilentlyOnClose(state.CurrentWorkshop, state.LastSavedWorkshop) &&
                !SaveAllData(context, showSuccessMessage: false, showErrorMessage: true))
            {
                e.Cancel = true;
            }
        }

        private void HandleSuccessfulLoad(KnowledgeBaseFileUiWorkflowContext context)
        {
            context.OnSuccessfulLoad();
            context.UpdateUi();
        }

        private bool SaveAllData(
            KnowledgeBaseFileUiWorkflowContext context,
            bool showSuccessMessage,
            bool showErrorMessage)
        {
            var saveResult = _fileWorkflowService.Save(context.GetCurrentTreeData());

            if (saveResult.IsSuccess)
            {
                context.UpdateUi();

                if (showSuccessMessage)
                {
                    MessageBox.Show(
                        context.Owner,
                        "Данные сохранены.",
                        "Сохранение",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }

                return true;
            }

            context.SetStatusText($"❌ Ошибка сохранения: {saveResult.ErrorMessage}");
            if (showErrorMessage)
            {
                MessageBox.Show(
                    context.Owner,
                    $"Ошибка сохранения: {saveResult.ErrorMessage}",
                    "Ошибка сохранения",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }

            return false;
        }

        private bool ConfirmContinueWithUnsavedChanges(
            KnowledgeBaseFileUiWorkflowContext context,
            string actionDescription)
        {
            context.SaveCurrentWorkshopState();
            context.UpdateDirtyState();

            var state = context.GetUiState();
            if (!_formStateService.RequiresSavePromptBeforeContinue(state.IsDirty, state.RequiresSave))
                return true;

            var result = MessageBox.Show(
                context.Owner,
                $"Есть несохранённые изменения. Сохранить перед {actionDescription}?",
                "Несохранённые изменения",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Cancel)
                return false;

            if (result == DialogResult.Yes)
                return SaveAllData(context, showSuccessMessage: false, showErrorMessage: true);

            return true;
        }

        private void ConfigureJsonDialog(FileDialog dialog)
        {
            dialog.Filter = "JSON (*.json)|*.json|Все файлы (*.*)|*.*";
            dialog.DefaultExt = "json";
            dialog.AddExtension = true;

            string? directory = Path.GetDirectoryName(CurrentDataPath);
            if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
                dialog.InitialDirectory = directory;

            dialog.FileName = CurrentDataFileName;
        }

        private string BuildLoadFailureMessage(KnowledgeBaseFileLoadResult loadResult)
        {
            string message = $"Ошибка загрузки файла '{CurrentDataPath}': {loadResult.ErrorMessage}";
            if (!string.IsNullOrWhiteSpace(loadResult.BackupPath))
                message += $"\nРезервная копия '{loadResult.BackupPath}' тоже не была загружена.";

            return message;
        }

        private string BuildBackupLoadMessage(KnowledgeBaseFileLoadResult loadResult)
        {
            return
                $"Основной файл '{CurrentDataPath}' не удалось прочитать: {loadResult.PrimaryErrorMessage}\n" +
                $"Загружена резервная копия '{loadResult.SourcePath}'. После проверки данных сохраните базу заново.";
        }

        private string BuildReloadSuccessMessage(KnowledgeBaseFileUiWorkflowContext context)
        {
            var state = context.GetUiState();
            int totalNodes = CountNodes(context.GetCurrentTreeData());

            return
                $"Файл перечитан с диска: {CurrentDataFileName} | " +
                $"Цех: {state.CurrentWorkshop} | " +
                $"Узлов: {totalNodes}";
        }

        private static int CountNodes(IEnumerable<KbNode> nodes)
        {
            int count = 0;
            foreach (var node in nodes)
                count += 1 + CountNodes(node.Children);

            return count;
        }
    }
}
