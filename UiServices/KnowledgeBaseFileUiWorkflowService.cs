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

        public Func<List<KbNode>> GetPersistedTreeData { get; init; } = null!;

        public Action SaveCurrentWorkshopState { get; init; } = null!;

        public Action UpdateDirtyState { get; init; } = null!;

        public Func<KnowledgeBaseFileUiState> GetUiState { get; init; } = null!;

        public Action ResetTransientUiStateAfterLoad { get; init; } = null!;

        public Action<KnowledgeBaseSessionViewState> ApplyLoadedSessionView { get; init; } = null!;

        public Action UpdateUi { get; init; } = null!;

        public Action<string> SetStatusText { get; init; } = null!;
    }

    /// <summary>
    /// Координирует WinForms-специфичные file/session сценарии:
    /// диалоги открытия/сохранения, prompt'ы и close handling поверх file-workflow service.
    /// </summary>
    public class KnowledgeBaseFileUiWorkflowService
    {
        private enum ProtectiveSnapshotPromptChoice
        {
            CreateSnapshotAndContinue,
            ContinueWithoutSnapshot,
            Cancel
        }

        private readonly KnowledgeBaseFileWorkflowService _fileWorkflowService;
        private readonly KnowledgeBaseFormStateService _formStateService;
        private readonly KnowledgeBaseFullJsonExchangeService _fullJsonExchangeService = new();
        private readonly KnowledgeBaseSnapshotComparisonService _snapshotComparisonService = new();

        public KnowledgeBaseFileUiWorkflowService(
            KnowledgeBaseFileWorkflowService fileWorkflowService,
            KnowledgeBaseFormStateService formStateService)
        {
            _fileWorkflowService = fileWorkflowService;
            _formStateService = formStateService;
        }

        public string CurrentDataPath => _fileWorkflowService.SavePath;

        public string CurrentDataFileName => Path.GetFileName(CurrentDataPath);

        public KnowledgeBaseFileLoadResult LoadData(
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
                    return result;

                case KnowledgeBaseFileLoadOutcome.LoadError:
                    MessageBox.Show(
                        context.Owner,
                        BuildLoadFailureMessage(result),
                        "Ошибка загрузки",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    context.UpdateUi();
                    context.SetStatusText("❌ Ошибка загрузки базы");
                    return result;

                case KnowledgeBaseFileLoadOutcome.CreatedDefaultAfterError:
                    HandleSuccessfulLoad(context, RequireViewState(result.ViewState));
                    MessageBox.Show(
                        context.Owner,
                        BuildLoadFailureMessage(result),
                        "Ошибка загрузки",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    context.SetStatusText("⚠️ Загружена пустая база из-за ошибки чтения");
                    return result;

                case KnowledgeBaseFileLoadOutcome.CreatedDefaultAndSaved:
                    HandleSuccessfulLoad(context, RequireViewState(result.ViewState));
                    context.SetStatusText("🆕 Создана новая база данных");
                    return result;

                case KnowledgeBaseFileLoadOutcome.CreatedDefaultUnsaved:
                    HandleSuccessfulLoad(context, RequireViewState(result.ViewState));
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

                    return result;

                case KnowledgeBaseFileLoadOutcome.LoadedBackup:
                    HandleSuccessfulLoad(context, RequireViewState(result.ViewState));
                    MessageBox.Show(
                        context.Owner,
                        BuildBackupLoadMessage(result),
                        "Загружена резервная копия",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    context.SetStatusText($"⚠️ Загружена резервная копия: {Path.GetFileName(result.SourcePath)}");
                    return result;

                case KnowledgeBaseFileLoadOutcome.LoadedExisting:
                    var viewState = RequireViewState(result.ViewState);
                    HandleSuccessfulLoad(context, viewState);
                    context.SetStatusText($"📂 Загружен цех: {viewState.CurrentWorkshop}");
                    return result;

                default:
                    return result;
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
            ConfigureDatabaseDialog(dialog);

            if (dialog.ShowDialog(context.Owner) != DialogResult.OK)
                return;

            string previousPath = CurrentDataPath;
            _fileWorkflowService.SavePath = dialog.FileName;

            var loadResult = LoadData(context, createDefaultIfMissing: false, fallbackToDefaultOnError: false);
            if (!loadResult.IsSuccess)
            {
                _fileWorkflowService.SavePath = previousPath;
                context.UpdateUi();
            }
        }

        public void ReloadDatabase(KnowledgeBaseFileUiWorkflowContext context)
        {
            if (!ConfirmContinueWithUnsavedChanges(context, "перезагрузкой базы из файла"))
                return;

            LoadData(context, createDefaultIfMissing: false, fallbackToDefaultOnError: false);
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

        public bool OfferProtectiveSnapshotBeforeDangerousOperation(
            KnowledgeBaseFileUiWorkflowContext context,
            string operationDescription,
            string snapshotNote)
        {
            context.SaveCurrentWorkshopState();
            ProtectiveSnapshotPromptChoice choice = ShowProtectiveSnapshotPrompt(
                context.Owner,
                operationDescription);
            if (choice == ProtectiveSnapshotPromptChoice.Cancel)
                return false;

            if (choice == ProtectiveSnapshotPromptChoice.ContinueWithoutSnapshot)
                return true;

            var result = _fileWorkflowService.CreateManualSnapshot(
                context.GetPersistedTreeData(),
                string.IsNullOrWhiteSpace(snapshotNote)
                    ? $"Перед операцией: {operationDescription}"
                    : snapshotNote.Trim());
            if (result.IsSuccess)
            {
                context.SetStatusText($"Снимок базы создан: {Path.GetFileName(result.SnapshotPath)}");
                return true;
            }

            context.SetStatusText($"Ошибка создания снимка базы: {result.ErrorMessage}");
            MessageBox.Show(
                context.Owner,
                $"Не удалось создать снимок базы. Операция отменена.\n\n{result.ErrorMessage}",
                "Защитный снимок",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return false;
        }

        public void SaveDatabaseAs(KnowledgeBaseFileUiWorkflowContext context)
        {
            using var dialog = new SaveFileDialog
            {
                Title = "Сохранить базу как",
                OverwritePrompt = true
            };
            ConfigureDatabaseDialog(dialog);

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

        public void ExportDatabaseJson(KnowledgeBaseFileUiWorkflowContext context)
        {
            context.SaveCurrentWorkshopState();
            SavedData data = _fileWorkflowService.CreateSaveData(context.GetPersistedTreeData());
            KnowledgeBaseFullJsonExportResult exportResult = _fullJsonExchangeService.ExportJson(data);
            if (!exportResult.IsSuccess)
            {
                context.SetStatusText($"Ошибка экспорта JSON: {exportResult.ErrorMessage}");
                MessageBox.Show(
                    context.Owner,
                    exportResult.ErrorMessage,
                    "Экспорт базы JSON",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            using var dialog = new SaveFileDialog
            {
                Title = "Экспорт базы в JSON",
                OverwritePrompt = true
            };
            ConfigureJsonExchangeDialog(dialog);

            if (dialog.ShowDialog(context.Owner) != DialogResult.OK)
                return;

            try
            {
                File.WriteAllBytes(dialog.FileName, exportResult.JsonBytes);
                context.SetStatusText($"База экспортирована в JSON: {Path.GetFileName(dialog.FileName)}");
                MessageBox.Show(
                    context.Owner,
                    $"База экспортирована в JSON:\n{dialog.FileName}",
                    "Экспорт базы JSON",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                context.SetStatusText($"Ошибка записи JSON: {ex.Message}");
                MessageBox.Show(
                    context.Owner,
                    $"Не удалось записать JSON-файл: {ex.Message}",
                    "Экспорт базы JSON",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        public void ImportDatabaseJson(KnowledgeBaseFileUiWorkflowContext context)
        {
            if (!ConfirmContinueWithUnsavedChanges(context, "импортом базы из JSON"))
                return;

            using var dialog = new OpenFileDialog
            {
                Title = "Импорт базы из JSON",
                CheckFileExists = true
            };
            ConfigureJsonExchangeDialog(dialog);

            if (dialog.ShowDialog(context.Owner) != DialogResult.OK)
                return;

            DialogResult replaceConfirmation = MessageBox.Show(
                context.Owner,
                "Импорт JSON заменит текущие данные базы и сохранит результат в текущий файл базы. Продолжить?",
                "Импорт базы JSON",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Warning);
            if (replaceConfirmation != DialogResult.OK)
                return;

            KnowledgeBaseFullJsonImportResult importResult;
            try
            {
                importResult = _fullJsonExchangeService.ImportJson(File.ReadAllBytes(dialog.FileName));
            }
            catch (Exception ex)
            {
                importResult = new KnowledgeBaseFullJsonImportResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Не удалось прочитать JSON-файл: {ex.Message}"
                };
            }

            if (!importResult.IsSuccess || importResult.Data == null)
            {
                context.SetStatusText($"Ошибка импорта JSON: {importResult.ErrorMessage}");
                MessageBox.Show(
                    context.Owner,
                    importResult.ErrorMessage,
                    "Импорт базы JSON",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            if (!OfferProtectiveSnapshotBeforeDangerousOperation(
                    context,
                    "заменой текущей базы из JSON",
                    $"Перед заменой текущей базы из JSON: {Path.GetFileName(dialog.FileName)}"))
            {
                return;
            }

            KnowledgeBaseFileSaveResult replaceResult = ReplaceAllData(context, importResult.Data);
            if (replaceResult.IsSuccess)
            {
                context.SetStatusText($"База импортирована из JSON: {Path.GetFileName(dialog.FileName)}");
                MessageBox.Show(
                    context.Owner,
                    "База импортирована из JSON и сохранена в текущий файл базы.",
                    "Импорт базы JSON",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            context.SetStatusText($"Ошибка сохранения импортированной базы: {replaceResult.ErrorMessage}");
            MessageBox.Show(
                context.Owner,
                $"JSON прочитан, но базу не удалось сохранить: {replaceResult.ErrorMessage}",
                "Импорт базы JSON",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        public void CreateManualSnapshot(KnowledgeBaseFileUiWorkflowContext context)
        {
            string? note = PromptSnapshotNote(context.Owner);
            if (note == null)
                return;

            context.SaveCurrentWorkshopState();
            var result = _fileWorkflowService.CreateManualSnapshot(context.GetPersistedTreeData(), note);
            if (result.IsSuccess)
            {
                context.SetStatusText($"Снимок базы создан: {Path.GetFileName(result.SnapshotPath)}");
                MessageBox.Show(
                    context.Owner,
                    $"Снимок базы создан:\n{result.SnapshotPath}",
                    "Снимок базы",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            context.SetStatusText($"Ошибка создания снимка базы: {result.ErrorMessage}");
            MessageBox.Show(
                context.Owner,
                $"Не удалось создать снимок базы: {result.ErrorMessage}",
                "Ошибка создания снимка",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        public void BrowseSnapshots(KnowledgeBaseFileUiWorkflowContext context)
        {
            var result = _fileWorkflowService.ListSnapshots();
            if (!result.IsSuccess)
            {
                context.SetStatusText($"Ошибка просмотра снимков базы: {result.ErrorMessage}");
                MessageBox.Show(
                    context.Owner,
                    $"Не удалось прочитать снимки базы: {result.ErrorMessage}",
                    "Снимки базы",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            using var dialog = new AsutpKnowledgeBase.KnowledgeBaseSnapshotBrowserForm(
                result.Snapshots,
                result.SnapshotDirectoryPath);
            dialog.ShowDialog(context.Owner);

            if (dialog.SelectedAction == AsutpKnowledgeBase.KnowledgeBaseSnapshotBrowserAction.Restore &&
                dialog.SelectedSnapshots.Count == 1)
            {
                RestoreSelectedSnapshot(context, dialog.SelectedSnapshots[0], confirmUnsavedChanges: true);
                return;
            }

            if (dialog.SelectedAction == AsutpKnowledgeBase.KnowledgeBaseSnapshotBrowserAction.Compare &&
                dialog.SelectedSnapshots.Count == 2)
            {
                CompareSelectedSnapshots(context, dialog.SelectedSnapshots[0], dialog.SelectedSnapshots[1]);
                return;
            }

            context.SetStatusText(result.Snapshots.Count == 0
                ? "Снимков базы нет"
                : $"Снимков базы: {result.Snapshots.Count}");
        }

        public void BrowseSnapshotsAndHistory(KnowledgeBaseFileUiWorkflowContext context)
        {
            var snapshotResult = _fileWorkflowService.ListSnapshots();
            if (!snapshotResult.IsSuccess)
            {
                context.SetStatusText($"Ошибка просмотра снимков базы: {snapshotResult.ErrorMessage}");
                MessageBox.Show(
                    context.Owner,
                    $"Не удалось прочитать снимки базы: {snapshotResult.ErrorMessage}",
                    "Снимки и история базы",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            KnowledgeBaseChangeLogListResult historyResult = _fileWorkflowService.ListChangeLog();
            bool isHistorySupported = historyResult.IsSuccess && historyResult.IsSupported;
            string historyErrorMessage = historyResult.IsSuccess
                ? string.Empty
                : historyResult.ErrorMessage ?? string.Empty;

            using var dialog = new AsutpKnowledgeBase.KnowledgeBaseSnapshotsAndHistoryForm(
                snapshotResult.Snapshots,
                snapshotResult.SnapshotDirectoryPath,
                isHistorySupported ? historyResult.Entries : Array.Empty<KnowledgeBaseChangeLogEntry>(),
                isHistorySupported,
                historyResult.IsSupported ? historyErrorMessage : "История изменений доступна для базы .akb.");
            dialog.ShowDialog(context.Owner);

            if (dialog.SelectedAction == AsutpKnowledgeBase.KnowledgeBaseSnapshotsAndHistoryAction.CreateSnapshot)
            {
                CreateManualSnapshot(context);
                return;
            }

            if (dialog.SelectedAction == AsutpKnowledgeBase.KnowledgeBaseSnapshotsAndHistoryAction.Restore &&
                dialog.SelectedSnapshots.Count == 1)
            {
                RestoreSelectedSnapshot(context, dialog.SelectedSnapshots[0], confirmUnsavedChanges: true);
                return;
            }

            if (dialog.SelectedAction == AsutpKnowledgeBase.KnowledgeBaseSnapshotsAndHistoryAction.Compare &&
                dialog.SelectedSnapshots.Count == 2)
            {
                CompareSelectedSnapshots(context, dialog.SelectedSnapshots[0], dialog.SelectedSnapshots[1]);
                return;
            }

            context.SetStatusText(snapshotResult.Snapshots.Count == 0
                ? "Снимков базы нет"
                : $"Снимков базы: {snapshotResult.Snapshots.Count}");
        }

        public void CompareSnapshots(KnowledgeBaseFileUiWorkflowContext context)
        {
            var result = _fileWorkflowService.ListSnapshots();
            if (!result.IsSuccess)
            {
                context.SetStatusText($"Ошибка просмотра снимков базы: {result.ErrorMessage}");
                MessageBox.Show(
                    context.Owner,
                    $"Не удалось прочитать снимки базы: {result.ErrorMessage}",
                    "Сравнение снимков",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            if (result.Snapshots.Count < 2)
            {
                context.SetStatusText("Для сравнения нужно минимум два снимка базы");
                MessageBox.Show(
                    context.Owner,
                    "Для сравнения нужно минимум два снимка базы.",
                    "Сравнение снимков",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            using var dialog = new AsutpKnowledgeBase.KnowledgeBaseSnapshotBrowserForm(
                result.Snapshots,
                result.SnapshotDirectoryPath);
            dialog.ShowDialog(context.Owner);

            if (dialog.SelectedAction != AsutpKnowledgeBase.KnowledgeBaseSnapshotBrowserAction.Compare ||
                dialog.SelectedSnapshots.Count != 2)
            {
                return;
            }

            CompareSelectedSnapshots(context, dialog.SelectedSnapshots[0], dialog.SelectedSnapshots[1]);
        }

        public void BrowseChangeHistory(KnowledgeBaseFileUiWorkflowContext context)
        {
            KnowledgeBaseChangeLogListResult result = _fileWorkflowService.ListChangeLog();
            if (!result.IsSuccess)
            {
                context.SetStatusText($"Ошибка чтения истории изменений: {result.ErrorMessage}");
                MessageBox.Show(
                    context.Owner,
                    $"Не удалось прочитать историю изменений: {result.ErrorMessage}",
                    "История изменений",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            if (!result.IsSupported)
            {
                context.SetStatusText("История изменений доступна для базы .akb");
                MessageBox.Show(
                    context.Owner,
                    "История изменений хранится внутри базы .akb и недоступна для legacy JSON-файла.",
                    "История изменений",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            using var dialog = new AsutpKnowledgeBase.KnowledgeBaseChangeHistoryForm(result.Entries);
            dialog.ShowDialog(context.Owner);
            context.SetStatusText(result.Entries.Count == 0
                ? "История изменений пуста"
                : $"История изменений: {result.Entries.Count} зап.");
        }

        public KnowledgeBaseChangeLogAppendResult AppendChangeLog(
            string actionKind,
            string summary,
            string details = "") =>
            _fileWorkflowService.AppendChangeLog(actionKind, summary, details);

        public void RestoreSnapshot(KnowledgeBaseFileUiWorkflowContext context)
        {
            var result = _fileWorkflowService.ListSnapshots();
            if (!result.IsSuccess)
            {
                context.SetStatusText($"Ошибка просмотра снимков базы: {result.ErrorMessage}");
                MessageBox.Show(
                    context.Owner,
                    $"Не удалось прочитать снимки базы: {result.ErrorMessage}",
                    "Восстановление снимка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            if (result.Snapshots.Count == 0)
            {
                context.SetStatusText("Снимков базы нет");
                MessageBox.Show(
                    context.Owner,
                    "Для текущей базы нет снимков.",
                    "Восстановление снимка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            using var dialog = new AsutpKnowledgeBase.KnowledgeBaseSnapshotBrowserForm(
                result.Snapshots,
                result.SnapshotDirectoryPath);
            dialog.ShowDialog(context.Owner);

            if (dialog.SelectedAction != AsutpKnowledgeBase.KnowledgeBaseSnapshotBrowserAction.Restore ||
                dialog.SelectedSnapshots.Count != 1)
            {
                return;
            }

            RestoreSelectedSnapshot(context, dialog.SelectedSnapshots[0], confirmUnsavedChanges: true);
        }

        public KnowledgeBaseFileSaveResult ReplaceAllData(
            KnowledgeBaseFileUiWorkflowContext context,
            SavedData data)
        {
            var result = _fileWorkflowService.ReplaceAllData(data);
            if (result.IsSuccess)
                HandleSuccessfulLoad(context, RequireViewState(result.ViewState));

            return result;
        }

        private void RestoreSelectedSnapshot(
            KnowledgeBaseFileUiWorkflowContext context,
            KnowledgeBaseSnapshotEntry snapshot,
            bool confirmUnsavedChanges)
        {
            if (confirmUnsavedChanges &&
                !ConfirmContinueWithUnsavedChanges(context, "восстановлением снимка базы"))
            {
                return;
            }

            DialogResult confirmation = MessageBox.Show(
                context.Owner,
                $"Восстановить базу из выбранного снимка?\n\n" +
                $"Снимок: {snapshot.SnapshotFileName}\n" +
                $"Создан: {snapshot.CreatedAt.ToLocalTime():dd.MM.yyyy HH:mm:ss}\n\n" +
                "Перед восстановлением будет создан защитный снимок текущего состояния.",
                "Восстановление снимка",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Warning);
            if (confirmation != DialogResult.OK)
                return;

            KnowledgeBaseSnapshotRestoreWorkflowResult restoreResult =
                _fileWorkflowService.RestoreSnapshot(snapshot);
            if (restoreResult.IsSuccess)
            {
                HandleSuccessfulLoad(context, RequireViewState(restoreResult.ViewState));
                context.SetStatusText($"База восстановлена из снимка: {snapshot.SnapshotFileName}");
                MessageBox.Show(
                    context.Owner,
                    string.IsNullOrWhiteSpace(restoreResult.ProtectiveSnapshotPath)
                        ? "База восстановлена из выбранного снимка."
                        : $"База восстановлена из выбранного снимка.\n\nЗащитный снимок перед восстановлением:\n{restoreResult.ProtectiveSnapshotPath}",
                    "Восстановление снимка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            context.SetStatusText($"Ошибка восстановления снимка: {restoreResult.ErrorMessage}");
            MessageBox.Show(
                context.Owner,
                $"Не удалось восстановить снимок базы: {restoreResult.ErrorMessage}",
                "Восстановление снимка",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        private void CompareSelectedSnapshots(
            KnowledgeBaseFileUiWorkflowContext context,
            KnowledgeBaseSnapshotEntry left,
            KnowledgeBaseSnapshotEntry right)
        {
            KnowledgeBaseSnapshotDataResult leftData = _fileWorkflowService.ReadSnapshotData(left);
            if (!leftData.IsSuccess || leftData.Data == null)
            {
                ShowSnapshotComparisonReadFailure(context, left, leftData.ErrorMessage);
                return;
            }

            KnowledgeBaseSnapshotDataResult rightData = _fileWorkflowService.ReadSnapshotData(right);
            if (!rightData.IsSuccess || rightData.Data == null)
            {
                ShowSnapshotComparisonReadFailure(context, right, rightData.ErrorMessage);
                return;
            }

            KnowledgeBaseSnapshotComparisonResult comparison =
                _snapshotComparisonService.Compare(leftData.Data, rightData.Data);
            string text = _snapshotComparisonService.BuildDisplayText(
                comparison,
                BuildSnapshotLabel(left),
                BuildSnapshotLabel(right));

            context.SetStatusText(comparison.HasChanges
                ? "Снимки базы сравнены: есть отличия"
                : "Снимки базы сравнены: отличий нет");
            MessageBox.Show(
                context.Owner,
                text,
                "Сравнение снимков",
                MessageBoxButtons.OK,
                comparison.HasChanges ? MessageBoxIcon.Information : MessageBoxIcon.None);
        }

        private static void ShowSnapshotComparisonReadFailure(
            KnowledgeBaseFileUiWorkflowContext context,
            KnowledgeBaseSnapshotEntry snapshot,
            string? errorMessage)
        {
            context.SetStatusText($"Ошибка чтения снимка базы: {errorMessage}");
            MessageBox.Show(
                context.Owner,
                $"Не удалось прочитать снимок '{snapshot.SnapshotFileName}': {errorMessage}",
                "Сравнение снимков",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        private static string BuildSnapshotLabel(KnowledgeBaseSnapshotEntry snapshot) =>
            $"{snapshot.SnapshotFileName} ({snapshot.CreatedAt.ToLocalTime():dd.MM.yyyy HH:mm:ss})";

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

        private void HandleSuccessfulLoad(
            KnowledgeBaseFileUiWorkflowContext context,
            KnowledgeBaseSessionViewState viewState)
        {
            context.ResetTransientUiStateAfterLoad();
            context.ApplyLoadedSessionView(viewState);
            context.UpdateUi();
        }

        private bool SaveAllData(
            KnowledgeBaseFileUiWorkflowContext context,
            bool showSuccessMessage,
            bool showErrorMessage)
        {
            var saveResult = _fileWorkflowService.Save(context.GetPersistedTreeData());

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

        private static string? PromptSnapshotNote(IWin32Window owner)
        {
            using var form = new Form
            {
                Text = "Создать снимок базы",
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MinimizeBox = false,
                MaximizeBox = false,
                ShowInTaskbar = false,
                ClientSize = new Size(520, 220)
            };
            AsutpKnowledgeBase.AppIconProvider.Apply(form);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                ColumnCount = 1,
                RowCount = 3
            };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var label = new Label
            {
                Text = "Примечание",
                Dock = DockStyle.Fill,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 6)
            };
            var txtNote = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                MaxLength = 500
            };
            var buttonsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                AutoSize = true,
                Margin = new Padding(0, 10, 0, 0)
            };
            var btnCancel = new Button
            {
                Text = "Отмена",
                DialogResult = DialogResult.Cancel,
                AutoSize = true
            };
            var btnCreate = new Button
            {
                Text = "Создать",
                DialogResult = DialogResult.OK,
                AutoSize = true,
                Enabled = false
            };
            txtNote.TextChanged += (_, _) =>
                btnCreate.Enabled = !string.IsNullOrWhiteSpace(txtNote.Text);

            buttonsPanel.Controls.Add(btnCancel);
            buttonsPanel.Controls.Add(btnCreate);
            layout.Controls.Add(label, 0, 0);
            layout.Controls.Add(txtNote, 0, 1);
            layout.Controls.Add(buttonsPanel, 0, 2);
            form.Controls.Add(layout);
            form.AcceptButton = btnCreate;
            form.CancelButton = btnCancel;

            return form.ShowDialog(owner) == DialogResult.OK
                ? txtNote.Text.Trim()
                : null;
        }

        private static ProtectiveSnapshotPromptChoice ShowProtectiveSnapshotPrompt(
            IWin32Window owner,
            string operationDescription)
        {
            using var form = new Form
            {
                Text = "Защитный снимок",
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MinimizeBox = false,
                MaximizeBox = false,
                ShowInTaskbar = false,
                ClientSize = new Size(560, 190)
            };
            AsutpKnowledgeBase.AppIconProvider.Apply(form);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                ColumnCount = 1,
                RowCount = 2
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            string operationText = string.IsNullOrWhiteSpace(operationDescription)
                ? "этой операцией"
                : operationDescription.Trim();
            var label = new Label
            {
                Text =
                    $"Перед {operationText} рекомендуется создать снимок базы.\n\n" +
                    "Снимок позволит вернуться к текущему состоянию, если результат операции окажется неверным.",
                Dock = DockStyle.Fill,
                AutoSize = false
            };

            var buttonsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                AutoSize = true,
                Margin = new Padding(0, 10, 0, 0)
            };
            var btnCancel = new Button
            {
                Text = "Отмена",
                DialogResult = DialogResult.Cancel,
                AutoSize = true
            };
            var btnContinue = new Button
            {
                Text = "Продолжить без снимка",
                AutoSize = true
            };
            var btnCreate = new Button
            {
                Text = "Создать снимок и продолжить",
                AutoSize = true
            };

            ProtectiveSnapshotPromptChoice choice = ProtectiveSnapshotPromptChoice.Cancel;
            btnCreate.Click += (_, _) =>
            {
                choice = ProtectiveSnapshotPromptChoice.CreateSnapshotAndContinue;
                form.DialogResult = DialogResult.OK;
                form.Close();
            };
            btnContinue.Click += (_, _) =>
            {
                choice = ProtectiveSnapshotPromptChoice.ContinueWithoutSnapshot;
                form.DialogResult = DialogResult.Ignore;
                form.Close();
            };
            btnCancel.Click += (_, _) =>
            {
                choice = ProtectiveSnapshotPromptChoice.Cancel;
                form.DialogResult = DialogResult.Cancel;
                form.Close();
            };

            buttonsPanel.Controls.Add(btnCancel);
            buttonsPanel.Controls.Add(btnContinue);
            buttonsPanel.Controls.Add(btnCreate);
            layout.Controls.Add(label, 0, 0);
            layout.Controls.Add(buttonsPanel, 0, 1);
            form.Controls.Add(layout);
            form.AcceptButton = btnCreate;
            form.CancelButton = btnCancel;

            return form.ShowDialog(owner) == DialogResult.Cancel
                ? ProtectiveSnapshotPromptChoice.Cancel
                : choice;
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

        private void ConfigureDatabaseDialog(FileDialog dialog)
        {
            dialog.Filter = "Файлы базы AKB (*.akb)|*.akb|Все файлы (*.*)|*.*";
            dialog.DefaultExt = "akb";
            dialog.AddExtension = true;

            string? directory = Path.GetDirectoryName(CurrentDataPath);
            if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
                dialog.InitialDirectory = directory;

            dialog.FileName = KnowledgeBaseStoragePaths.IsSqlitePath(CurrentDataPath)
                ? CurrentDataFileName
                : Path.ChangeExtension(CurrentDataFileName, KnowledgeBaseStoragePaths.SqliteExtension);
        }

        private void ConfigureJsonExchangeDialog(FileDialog dialog)
        {
            dialog.Filter = "Файлы JSON (*.json)|*.json|Все файлы (*.*)|*.*";
            dialog.DefaultExt = "json";
            dialog.AddExtension = true;

            string? directory = Path.GetDirectoryName(CurrentDataPath);
            if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
                dialog.InitialDirectory = directory;

            dialog.FileName = KnowledgeBaseStoragePaths.IsSqlitePath(CurrentDataPath)
                ? Path.ChangeExtension(CurrentDataFileName, KnowledgeBaseStoragePaths.JsonExtension)
                : CurrentDataFileName;
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

        private static KnowledgeBaseSessionViewState RequireViewState(KnowledgeBaseSessionViewState? viewState) =>
            viewState ?? throw new InvalidOperationException(
                "Successful file workflow result must contain session ViewState.");
    }
}
