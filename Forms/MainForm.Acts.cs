using System.Diagnostics;
using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase
{
    public partial class MainForm
    {
        private void CreateActFromSelectedCompositionEntry(object? sender, EventArgs e)
        {
            CreateActFromSelectedEquipmentEntry(
                selectedNodeCompositionScreen.SelectedEntryId,
                requireSlotted: true,
                KnowledgeBaseActDraftSource.Composition,
                "Выберите строку оборудования в составе.",
                BuildSelectedRackDraft);
        }

        private void CreateActFromSelectedAdditionalEquipmentEntry(object? sender, EventArgs e)
        {
            CreateActFromSelectedEquipmentEntry(
                selectedNodeAdditionalEquipmentScreen.SelectedEntryId,
                requireSlotted: false,
                KnowledgeBaseActDraftSource.AdditionalEquipment,
                "Выберите строку доп. оборудования.",
                _ => null);
        }

        private void CreateActFromSelectedEquipmentEntry(
            string selectedEntryId,
            bool requireSlotted,
            KnowledgeBaseActDraftSource source,
            string missingSelectionMessage,
            Func<KbNode, KbCompositionRack?> rackFactory)
        {
            if (!TryGetCompositionParentNode(out var parentNode))
                return;

            var selectedEntry = FindSelectedCompositionEntry(
                parentNode,
                selectedEntryId,
                requireSlotted);
            if (selectedEntry == null)
            {
                MessageBox.Show(
                    this,
                    missingSelectionMessage,
                    "Акт",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            var result = _actDraftService.CreateDraft(new KnowledgeBaseActDraftRequest
            {
                Lvl3Node = parentNode,
                WorkshopRoots = GetVisibleTreeData(),
                WorkshopName = _currentWorkshop,
                VisibleLevel = GetVisibleLevelForNode(parentNode),
                Source = source,
                Rack = rackFactory(parentNode),
                CompositionEntry = selectedEntry,
                ActType = KbActType.EquipmentFailure
            });

            OpenNewActDraft(result);
        }

        private void CreateInspectionActFromSelectedObject()
        {
            if (!TryGetSelectedTreeNode(out KbNode selectedNode) ||
                GetVisibleLevelForNode(selectedNode) != 2)
            {
                return;
            }

            var result = _actDraftService.CreateDraft(new KnowledgeBaseActDraftRequest
            {
                ObjectNode = selectedNode,
                WorkshopRoots = GetVisibleTreeData(),
                WorkshopName = _currentWorkshop,
                VisibleLevel = GetVisibleLevelForNode(selectedNode),
                Source = KnowledgeBaseActDraftSource.Lvl2Object,
                ActType = KbActType.InspectionWork
            });

            OpenNewActDraft(result);
        }

        private void OpenNewActDraft(KnowledgeBaseActDraftResult result)
        {
            if (!result.IsSuccess || result.Act == null)
            {
                MessageBox.Show(
                    this,
                    result.ErrorMessage,
                    "Акт",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            IEnumerable<KbActExecutor> currentExecutors = _session.ActExecutors
                .Where(executor => string.Equals(executor.ActId, result.Act.ActId, StringComparison.Ordinal));
            using var dialog = new KnowledgeBaseActForm(
                result.Act,
                currentExecutors,
                result.Act.WorkshopName,
                _session.ActInputHistory,
                TryDeleteActInputHistorySuggestion);
            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            ActFormSaveResult saveResult = SaveActFromForm(
                dialog.Result,
                dialog.ResultExecutors,
                prepareDocumentPath: dialog.DocumentGenerationRequested,
                inputHistory: dialog.ResultInputHistory);
            if (!saveResult.IsSuccess)
                return;

            if (dialog.DocumentGenerationRequested)
            {
                GenerateActDocumentAfterSave(saveResult);
                return;
            }

            ShowActSavedMessage(documentGenerationRequested: false);
        }

        private void OpenActsJournal(object? sender, EventArgs e)
        {
            if (_actsJournalForm is { IsDisposed: false } existingForm)
            {
                RefreshActsJournalIfOpen();
                existingForm.RestoreForActivation();
                return;
            }

            var journalForm = new KnowledgeBaseActsJournalForm(
                BuildActsJournalRows(),
                columnWidths: _windowLayoutStateService.LoadColumnWidths(ActsJournalColumnWidthsKey));
            _actsJournalForm = journalForm;
            journalForm.ActionRequested += ActsJournalForm_ActionRequested;
            journalForm.ColumnWidthsChanged += SaveActsJournalColumnWidths;
            journalForm.FormClosed += (_, _) =>
            {
                journalForm.ActionRequested -= ActsJournalForm_ActionRequested;
                journalForm.ColumnWidthsChanged -= SaveActsJournalColumnWidths;
                if (ReferenceEquals(_actsJournalForm, journalForm))
                    _actsJournalForm = null;
            };
            journalForm.Show(this);
            journalForm.RestoreForActivation();
        }

        private IReadOnlyList<KnowledgeBaseActJournalRow> BuildActsJournalRows() =>
            _actJournalService.BuildRows(
                _session.Acts,
                _session.ActDocuments,
                ResolveActDocumentsBaseDirectory());

        private void RefreshActsJournalIfOpen(string? preferredActId = null)
        {
            if (_actsJournalForm is not { IsDisposed: false } journalForm)
                return;

            journalForm.RefreshRows(BuildActsJournalRows(), preferredActId);
        }

        private void CloseActsJournalIfOpen()
        {
            KnowledgeBaseActsJournalForm? journalForm = _actsJournalForm;
            _actsJournalForm = null;
            if (journalForm is { IsDisposed: false })
                journalForm.Close();
        }

        private void ActsJournalForm_ActionRequested(
            object? sender,
            KnowledgeBaseActsJournalActionRequestedEventArgs e)
        {
            if (sender is not KnowledgeBaseActsJournalForm journalForm ||
                journalForm.IsDisposed)
            {
                return;
            }

            journalForm.SetActionInProgress(true);
            try
            {
                switch (e.Action)
                {
                    case KnowledgeBaseActsJournalAction.Open:
                        OpenExistingActFromJournal(e.ActId, journalForm);
                        break;
                    case KnowledgeBaseActsJournalAction.GenerateDocument:
                        PrepareExistingActDocumentFromJournal(e.ActId, journalForm);
                        break;
                    case KnowledgeBaseActsJournalAction.OpenDocument:
                        OpenActDocumentFromJournal(e.ActId, journalForm);
                        break;
                    case KnowledgeBaseActsJournalAction.DeleteDraft:
                        DeleteDraftActFromJournal(e.ActId, journalForm);
                        break;
                    case KnowledgeBaseActsJournalAction.SignAct:
                        ChangeActStatusFromJournal(e.ActId, KbActStatus.Signed, journalForm);
                        break;
                    case KnowledgeBaseActsJournalAction.CancelAct:
                        ChangeActStatusFromJournal(e.ActId, KbActStatus.Cancelled, journalForm);
                        break;
                }
            }
            finally
            {
                if (!journalForm.IsDisposed)
                {
                    RefreshActsJournalIfOpen(e.ActId);
                    journalForm.SetActionInProgress(false);
                    journalForm.RestoreForActivation();
                }
            }
        }

        private bool OpenExistingActFromJournal(string actId, IWin32Window owner)
        {
            KbAct? act = FindActById(actId);
            if (act == null)
            {
                ShowActJournalError("Акт не найден.", owner);
                return false;
            }

            if (!KnowledgeBaseActStatusService.CanEdit(act.Status))
            {
                ShowActJournalError("Подписанный или отмененный акт нельзя редактировать.", owner);
                return false;
            }

            IReadOnlyList<KbActExecutor> executors = GetActExecutors(actId);
            using var dialog = new KnowledgeBaseActForm(
                act,
                executors,
                act.WorkshopName,
                _session.ActInputHistory,
                TryDeleteActInputHistorySuggestion);
            if (dialog.ShowDialog(owner) != DialogResult.OK)
                return false;

            if (dialog.DocumentGenerationRequested &&
                !KnowledgeBaseActJournalService.CanGenerateDocument(act.Status))
            {
                ShowActJournalError("Для подписанного или отмененного акта DOCX не формируется.", owner);
                return false;
            }

            ActFormSaveResult saveResult = SaveActFromForm(
                dialog.Result,
                dialog.ResultExecutors,
                prepareDocumentPath: dialog.DocumentGenerationRequested,
                owner,
                inputHistory: dialog.ResultInputHistory);
            if (!saveResult.IsSuccess)
            {
                return false;
            }

            if (dialog.DocumentGenerationRequested)
                return GenerateActDocumentAfterSave(saveResult, owner);

            ShowActSavedMessage(documentGenerationRequested: false, owner);
            return true;
        }

        private bool PrepareExistingActDocumentFromJournal(string actId, IWin32Window owner)
        {
            KbAct? act = FindActById(actId);
            if (act == null)
            {
                ShowActJournalError("Акт не найден.", owner);
                return false;
            }

            if (!KnowledgeBaseActJournalService.CanGenerateDocument(act.Status))
            {
                ShowActJournalError("Для подписанного или отмененного акта DOCX не формируется.", owner);
                return false;
            }

            var editorService = new KnowledgeBaseActEditorService();
            KnowledgeBaseActEditorSaveResult savePreparation = editorService.PrepareForSave(act);
            if (!savePreparation.IsSuccess || savePreparation.Act == null)
            {
                ShowActJournalError(savePreparation.ErrorMessage, owner);
                return false;
            }

            IReadOnlyList<KbActExecutor> executors = GetActExecutors(actId);
            string? executorValidationError = KnowledgeBaseActEditorService.ValidateExecutorsForSave(executors);
            if (executorValidationError != null)
            {
                ShowActJournalError(executorValidationError, owner);
                return false;
            }

            IReadOnlyList<KbActExecutor> normalizedExecutors = KnowledgeBaseDataService.NormalizeActExecutors(
                executors,
                new[] { actId });
            ActFormSaveResult saveResult = SaveActFromForm(
                savePreparation.Act,
                normalizedExecutors,
                prepareDocumentPath: true,
                owner);
            if (!saveResult.IsSuccess)
            {
                return false;
            }

            return GenerateActDocumentAfterSave(saveResult, owner);
        }

        private bool OpenActDocumentFromJournal(string actId, IWin32Window owner)
        {
            KbActDocument? document = FindLatestActDocument(actId);
            if (document == null || string.IsNullOrWhiteSpace(document.Path))
            {
                ShowActJournalError("Для выбранного акта DOCX еще не сохранен.", owner);
                return false;
            }

            string absolutePath = ResolveActDocumentAbsolutePath(document.Path);
            if (string.IsNullOrWhiteSpace(absolutePath) || !File.Exists(absolutePath))
            {
                ShowActJournalError("Файл DOCX не найден. Сформируйте документ повторно.", owner);
                return false;
            }

            return OpenActDocumentPath(absolutePath, owner);
        }

        private bool DeleteDraftActFromJournal(string actId, IWin32Window owner)
        {
            KbAct? act = FindActById(actId);
            if (act == null)
            {
                ShowActJournalError("Акт не найден.", owner);
                return false;
            }

            if (!_actJournalService.CanDeletePhysically(act, _session.ActDocuments))
            {
                ShowActJournalError("Удалить можно только черновик без номера и без DOCX.", owner);
                return false;
            }

            DialogResult confirmResult = MessageBox.Show(
                owner,
                "Удалить черновик акта? Это действие удалит запись из базы.",
                "Журнал актов",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Warning);
            if (confirmResult != DialogResult.OK)
                return false;

            _session.ReplaceActs(_session.Acts.Where(existingAct =>
                !string.Equals(existingAct.ActId, actId, StringComparison.Ordinal)));
            return SaveActJournalMutation("Черновик акта удален.", owner);
        }

        private bool ChangeActStatusFromJournal(string actId, KbActStatus status, IWin32Window owner)
        {
            KbAct? act = FindActById(actId);
            if (act == null)
            {
                ShowActJournalError("Акт не найден.", owner);
                return false;
            }

            if ((status == KbActStatus.Signed && !KnowledgeBaseActStatusService.CanSign(act.Status)) ||
                (status == KbActStatus.Cancelled && !KnowledgeBaseActStatusService.CanCancel(act.Status)))
            {
                ShowActJournalError("Статус этого акта уже нельзя изменить через журнал.", owner);
                return false;
            }

            if (status == KbActStatus.Cancelled)
            {
                DialogResult confirmResult = MessageBox.Show(
                    owner,
                    "Отменить акт? Сформированный DOCX будет удален.",
                    "Журнал актов",
                    MessageBoxButtons.OKCancel,
                    MessageBoxIcon.Warning);
                if (confirmResult != DialogResult.OK)
                    return false;
            }

            string statusText = KnowledgeBaseActJournalService.FormatStatus(status);

            var statusService = new KnowledgeBaseActStatusService();
            KnowledgeBaseActStatusChangeResult statusChangeResult = statusService.PrepareStatusChange(
                new KnowledgeBaseActStatusChangeRequest
                {
                    Act = act,
                    NewStatus = status,
                    ChangedAt = DateTime.Now,
                    SignedAt = status == KbActStatus.Signed ? DateTime.Today : null
                });
            if (!statusChangeResult.IsSuccess || statusChangeResult.Act == null)
            {
                ShowActJournalError(statusChangeResult.ErrorMessage, owner);
                return false;
            }

            if (status == KbActStatus.Cancelled &&
                !DeleteActDocumentsForCancellation(actId, owner))
            {
                return false;
            }

            KbAct updatedAct = statusChangeResult.Act;
            var acts = _session.Acts
                .Where(existingAct => !string.Equals(existingAct.ActId, actId, StringComparison.Ordinal))
                .ToList();
            acts.Add(updatedAct);
            _session.ReplaceActs(acts);
            return SaveActJournalMutation($"Статус акта изменен на \"{statusText}\".", owner);
        }

        private bool DeleteActDocumentsForCancellation(string actId, IWin32Window owner)
        {
            List<KbActDocument> documentsToDelete = _session.ActDocuments
                .Where(document => string.Equals(document.ActId, actId, StringComparison.Ordinal))
                .ToList();

            foreach (string absolutePath in documentsToDelete
                .Select(document => ResolveActDocumentAbsolutePath(document.Path))
                .Where(static path => !string.IsNullOrWhiteSpace(path))
                .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                try
                {
                    if (File.Exists(absolutePath))
                        File.Delete(absolutePath);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    ShowActJournalError(
                        $"Не удалось удалить DOCX отменяемого акта: {ex.GetBaseException().Message}\n\n" +
                        "Закройте документ, если он открыт, и повторите отмену.",
                        owner);
                    return false;
                }
            }

            _session.ReplaceActDocuments(_session.ActDocuments.Where(document =>
                !string.Equals(document.ActId, actId, StringComparison.Ordinal)));
            return true;
        }

        private bool SaveActJournalMutation(string successMessage, IWin32Window owner)
        {
            UpdateDirtyState();
            KnowledgeBaseFileSaveResult saveResult = _fileWorkflowService.Save(GetPersistedTreeData());
            UpdateUI();
            if (saveResult.IsSuccess)
            {
                MessageBox.Show(
                    owner,
                    successMessage,
                    "Журнал актов",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                SetLastActionText(successMessage);
                return true;
            }

            ShowActJournalError($"Не удалось сохранить изменения журнала актов: {saveResult.ErrorMessage}", owner);
            return false;
        }

        private KbAct? FindActById(string actId) =>
            _session.Acts.FirstOrDefault(act =>
                string.Equals(act.ActId, actId, StringComparison.Ordinal));

        private IReadOnlyList<KbActExecutor> GetActExecutors(string actId) =>
            _session.ActExecutors
                .Where(executor => string.Equals(executor.ActId, actId, StringComparison.Ordinal))
                .ToList();

        private KbActDocument? FindLatestActDocument(string actId) =>
            KnowledgeBaseDataService
                .NormalizeActDocuments(_session.ActDocuments, new[] { actId })
                .OrderByDescending(static document => document.IsLatest)
                .ThenByDescending(static document => document.VersionNumber)
                .ThenBy(static document => document.DocumentId, StringComparer.Ordinal)
                .FirstOrDefault();

        private string ResolveActDocumentAbsolutePath(string documentPath)
        {
            if (string.IsNullOrWhiteSpace(documentPath))
                return string.Empty;

            try
            {
                return Path.GetFullPath(Path.IsPathRooted(documentPath)
                    ? documentPath
                    : Path.Combine(ResolveActDocumentsBaseDirectory(), documentPath));
            }
            catch
            {
                return string.Empty;
            }
        }

        private bool OpenActDocumentPath(string absolutePath, IWin32Window? owner = null)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = absolutePath,
                    UseShellExecute = true
                });
                SetLastActionText($"Открыт DOCX акта: {absolutePath}");
                return true;
            }
            catch (Exception ex)
            {
                ShowActGenerationError($"Не удалось открыть DOCX: {ex.GetBaseException().Message}", owner);
                return false;
            }
        }

        private void ShowActSavedMessage(bool documentGenerationRequested, IWin32Window? owner = null)
        {
            string message = documentGenerationRequested
                ? "Черновик сохранен, путь DOCX выбран."
                : "Черновик сохранен.";
            MessageBox.Show(
                owner ?? this,
                message,
                "Акт",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            SetLastActionText(message);
        }

        private void ShowActJournalError(string errorMessage, IWin32Window? owner = null)
        {
            MessageBox.Show(
                owner ?? this,
                errorMessage,
                "Журнал актов",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            SetLastActionText($"Журнал актов: {errorMessage}");
        }

        private ActFormSaveResult SaveActFromForm(
            KbAct act,
            IReadOnlyList<KbActExecutor> executors,
            bool prepareDocumentPath,
            IWin32Window? owner = null,
            IEnumerable<KbActInputHistoryEntry>? inputHistory = null)
        {
            KbAct? existingAct = FindActById(act.ActId);
            if (existingAct != null && !KnowledgeBaseActStatusService.CanEdit(existingAct.Status))
            {
                ShowActSaveError("Подписанный или отмененный акт нельзя редактировать.", owner);
                return ActFormSaveResult.Failed();
            }

            var acts = _session.Acts
                .Where(existingAct => !string.Equals(existingAct.ActId, act.ActId, StringComparison.Ordinal))
                .ToList();
            acts.Add(act);
            var actDocuments = _session.ActDocuments.ToList();
            List<KbActNumberSequence> actNumberSequences = _session.ActNumberSequences.ToList();
            KnowledgeBaseActDocumentPathResult? documentPathResult = null;

            if (prepareDocumentPath)
            {
                var numberingService = new KnowledgeBaseActNumberingService();
                KnowledgeBaseActNumberingResult numberingResult = numberingService.EnsureActNumber(
                    act,
                    acts,
                    actNumberSequences);
                if (!numberingResult.IsSuccess || numberingResult.Act == null)
                {
                    ShowActSaveError(numberingResult.ErrorMessage, owner);
                    return ActFormSaveResult.Failed();
                }

                act = numberingResult.Act;
                actNumberSequences = numberingResult.NumberSequences;
                acts = acts
                    .Where(existingAct => !string.Equals(existingAct.ActId, act.ActId, StringComparison.Ordinal))
                    .ToList();
                acts.Add(act);

                if (!TrySelectActDocumentPath(act, actDocuments, owner, out documentPathResult) ||
                    documentPathResult == null)
                {
                    return ActFormSaveResult.Failed();
                }

                actDocuments = actDocuments
                    .Where(document => !string.Equals(document.ActId, act.ActId, StringComparison.Ordinal))
                    .ToList();
                actDocuments.Add(CreatePreparedActDocument(act, documentPathResult.StoredPath));
            }

            _session.ReplaceActs(acts);
            IReadOnlyList<KbActExecutor> savedExecutors = KnowledgeBaseDataService.NormalizeActExecutors(
                executors,
                new[] { act.ActId });
            List<KbActInputHistoryEntry>? previousInputHistory = inputHistory == null
                ? null
                : KnowledgeBaseDataService.NormalizeActInputHistory(_session.ActInputHistory);
            var actExecutors = _session.ActExecutors
                .Where(existingExecutor => !string.Equals(existingExecutor.ActId, act.ActId, StringComparison.Ordinal))
                .ToList();
            actExecutors.AddRange(savedExecutors);
            _session.ReplaceActExecutors(actExecutors);
            if (inputHistory != null)
                _session.ReplaceActInputHistory(inputHistory);

            if (prepareDocumentPath)
            {
                _session.ReplaceActDocuments(actDocuments);
                _session.ReplaceActNumberSequences(actNumberSequences);
                KbConfig updatedConfig = CloneConfig(_session.Config);
                updatedConfig.ActDocumentsDirectoryPath = documentPathResult?.StoredDirectoryPath
                    ?? _session.Config.ActDocumentsDirectoryPath;
                _session.UpdateConfig(KnowledgeBaseDataService.NormalizeConfig(updatedConfig));
            }

            UpdateDirtyState();

            KnowledgeBaseFileSaveResult saveResult = _fileWorkflowService.Save(GetPersistedTreeData());
            UpdateUI();
            if (saveResult.IsSuccess)
            {
                RefreshActsJournalIfOpen(act.ActId);
                return new ActFormSaveResult
                {
                    IsSuccess = true,
                    Act = act,
                    Executors = savedExecutors,
                    DocumentPathResult = documentPathResult
                };
            }

            MessageBox.Show(
                owner ?? this,
                $"Не удалось сохранить акт: {saveResult.ErrorMessage}",
                "Акт",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            SetLastActionText($"Ошибка сохранения акта: {saveResult.ErrorMessage}");
            if (previousInputHistory != null)
            {
                _session.ReplaceActInputHistory(previousInputHistory);
                UpdateDirtyState();
            }

            return ActFormSaveResult.Failed();
        }

        private string? TryDeleteActInputHistorySuggestion(
            string workshopName,
            KbActInputHistoryField field,
            string value)
        {
            List<KbActInputHistoryEntry> previousInputHistory =
                KnowledgeBaseDataService.NormalizeActInputHistory(_session.ActInputHistory);
            if (!_session.TryDeleteActInputHistoryValue(workshopName, field, value))
                return null;

            UpdateDirtyState();
            KnowledgeBaseFileSaveResult saveResult = _fileWorkflowService.Save(GetPersistedTreeData());
            UpdateUI();
            if (saveResult.IsSuccess)
                return null;

            _session.ReplaceActInputHistory(previousInputHistory);
            UpdateDirtyState();
            UpdateUI();
            SetLastActionText($"Ошибка сохранения истории ввода: {saveResult.ErrorMessage}");
            return $"Не удалось удалить значение из истории ввода: {saveResult.ErrorMessage}";
        }

        private bool GenerateActDocumentAfterSave(ActFormSaveResult saveResult, IWin32Window? owner = null)
        {
            if (saveResult.Act == null || saveResult.DocumentPathResult == null)
            {
                ShowActGenerationError("Акт сохранен, но путь DOCX не подготовлен.", owner);
                return false;
            }

            if (saveResult.DocumentPathResult.OpenExistingRequested)
                return OpenActDocumentPath(saveResult.DocumentPathResult.AbsolutePath, owner);

            string templatePath = KnowledgeBaseActDocxTemplateService.ResolveTemplatePath(
                saveResult.Act.ActType,
                AppContext.BaseDirectory);
            KnowledgeBaseActDocxGenerationResult generationResult = _actDocxPluginLoader.Generate(
                new KnowledgeBaseActDocxGenerationRequest
                {
                    Act = saveResult.Act,
                    Executors = saveResult.Executors,
                    TemplatePath = templatePath,
                    OutputPath = saveResult.DocumentPathResult.AbsolutePath,
                    OverwriteExisting = saveResult.DocumentPathResult.OverwriteExisting
                });

            if (!generationResult.IsSuccess)
            {
                ShowActGenerationError($"Акт сохранен, но DOCX не сформирован: {generationResult.ErrorMessage}", owner);
                return false;
            }

            if (!MarkActDocumentGenerated(
                saveResult.Act,
                saveResult.DocumentPathResult,
                generationResult.ContentHash,
                owner))
            {
                return false;
            }

            ShowActGeneratedMessage(generationResult.OutputPath, owner);
            return true;
        }

        private bool MarkActDocumentGenerated(
            KbAct act,
            KnowledgeBaseActDocumentPathResult documentPathResult,
            string contentHash,
            IWin32Window? owner = null)
        {
            DateTime now = DateTime.Now;
            KbAct updatedAct;
            if (act.Status == KbActStatus.Draft)
            {
                var statusService = new KnowledgeBaseActStatusService();
                KnowledgeBaseActStatusChangeResult statusChangeResult = statusService.PrepareStatusChange(
                    new KnowledgeBaseActStatusChangeRequest
                    {
                        Act = act,
                        NewStatus = KbActStatus.Generated,
                        ChangedAt = now
                    });
                if (!statusChangeResult.IsSuccess || statusChangeResult.Act == null)
                {
                    ShowActGenerationError(statusChangeResult.ErrorMessage, owner);
                    return false;
                }

                updatedAct = statusChangeResult.Act;
            }
            else if (act.Status == KbActStatus.Generated)
            {
                updatedAct = KnowledgeBaseActEditorService.CloneAct(act);
                updatedAct.UpdatedAt = now;
            }
            else
            {
                ShowActGenerationError("DOCX можно сформировать только для черновика или сформированного акта.", owner);
                return false;
            }

            var acts = _session.Acts
                .Where(existingAct => !string.Equals(existingAct.ActId, act.ActId, StringComparison.Ordinal))
                .ToList();
            acts.Add(updatedAct);
            _session.ReplaceActs(acts);

            var actDocuments = _session.ActDocuments
                .Where(document => !string.Equals(document.ActId, act.ActId, StringComparison.Ordinal))
                .ToList();
            actDocuments.Add(CreateGeneratedActDocument(updatedAct, documentPathResult.StoredPath, contentHash, now));
            _session.ReplaceActDocuments(actDocuments);

            UpdateDirtyState();
            KnowledgeBaseFileSaveResult saveResult = _fileWorkflowService.Save(GetPersistedTreeData());
            UpdateUI();
            if (saveResult.IsSuccess)
            {
                RefreshActsJournalIfOpen(updatedAct.ActId);
                return true;
            }

            MessageBox.Show(
                owner ?? this,
                $"DOCX сформирован, но не удалось обновить запись акта: {saveResult.ErrorMessage}",
                "Акт",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            SetLastActionText($"Ошибка обновления акта после DOCX: {saveResult.ErrorMessage}");
            return false;
        }

        private bool TrySelectActDocumentPath(
            KbAct act,
            IReadOnlyList<KbActDocument> actDocuments,
            IWin32Window? owner,
            out KnowledgeBaseActDocumentPathResult? result)
        {
            result = null;
            string baseDirectory = ResolveActDocumentsBaseDirectory();
            string documentsDirectory = KnowledgeBaseActDocumentPathService.ResolveDocumentsDirectory(
                _session.Config,
                baseDirectory);
            string initialDirectory = Directory.Exists(documentsDirectory)
                ? documentsDirectory
                : baseDirectory;
            var documentPathService = new KnowledgeBaseActDocumentPathService();

            KbActDocument? existingDocument = FindLatestActDocument(act.ActId);
            if (existingDocument != null && !string.IsNullOrWhiteSpace(existingDocument.Path))
            {
                string existingAbsolutePath = ResolveActDocumentAbsolutePath(existingDocument.Path);
                if (!string.IsNullOrWhiteSpace(existingAbsolutePath))
                {
                    result = documentPathService.PrepareDocumentPath(
                        new KnowledgeBaseActDocumentPathRequest
                        {
                            Act = act,
                            Config = _session.Config,
                            ExistingDocuments = actDocuments,
                            SelectedPath = existingAbsolutePath,
                            DatabasePath = CurrentDataPath,
                            ApplicationBasePath = AppContext.BaseDirectory,
                            AllowExistingFile = true
                        });
                    if (!result.IsSuccess)
                    {
                        ShowActSaveError(result.ErrorMessage, owner);
                        result = null;
                        return false;
                    }

                    if (result.TargetFileExists &&
                        !ConfirmGeneratedDocumentOverwrite(result.AbsolutePath, owner))
                    {
                        result = null;
                        return false;
                    }

                    if (result.TargetFileExists)
                        result = CopyDocumentPathResult(result, overwriteExisting: true);

                    return true;
                }
            }

            using var dialog = new SaveFileDialog
            {
                AddExtension = true,
                CheckPathExists = true,
                DefaultExt = "docx",
                FileName = KnowledgeBaseActDocumentPathService.BuildDocumentFileName(act),
                Filter = "Документы Word (*.docx)|*.docx",
                InitialDirectory = Directory.Exists(initialDirectory)
                    ? initialDirectory
                    : AppContext.BaseDirectory,
                OverwritePrompt = false,
                Title = "Сформировать DOCX акта"
            };
            while (true)
            {
                if (dialog.ShowDialog(owner ?? this) != DialogResult.OK)
                    return false;

                if (TryPrepareActDocumentPath(
                    act,
                    actDocuments,
                    dialog.FileName,
                    documentPathService,
                    owner,
                    out result,
                    out bool saveCopyRequested))
                {
                    return true;
                }

                if (saveCopyRequested && result != null)
                    dialog.FileName = BuildCopyDocumentPath(result.AbsolutePath);

                if (result == null)
                    return false;
            }
        }

        private bool TryPrepareActDocumentPath(
            KbAct act,
            IReadOnlyList<KbActDocument> actDocuments,
            string selectedPath,
            KnowledgeBaseActDocumentPathService documentPathService,
            IWin32Window? owner,
            out KnowledgeBaseActDocumentPathResult? result,
            out bool saveCopyRequested)
        {
            saveCopyRequested = false;
            result = documentPathService.PrepareDocumentPath(
                new KnowledgeBaseActDocumentPathRequest
                {
                    Act = act,
                    Config = _session.Config,
                    ExistingDocuments = actDocuments,
                    SelectedPath = selectedPath,
                    DatabasePath = CurrentDataPath,
                    ApplicationBasePath = AppContext.BaseDirectory,
                    AllowExistingFile = true
                });

            if (!result.IsSuccess)
            {
                ShowActSaveError(result.ErrorMessage, owner);
                return false;
            }

            if (!result.TargetFileExists)
                return true;

            return ResolveExistingActDocumentChoice(result, owner, out result, out saveCopyRequested);
        }

        private bool ResolveExistingActDocumentChoice(
            KnowledgeBaseActDocumentPathResult pathResult,
            IWin32Window? owner,
            out KnowledgeBaseActDocumentPathResult? result,
            out bool saveCopyRequested)
        {
            result = null;
            saveCopyRequested = false;
            using var dialog = new KnowledgeBaseActDocumentConflictDialog(pathResult.AbsolutePath);
            if (dialog.ShowDialog(owner ?? this) != DialogResult.OK)
                return false;

            switch (dialog.SelectedAction)
            {
                case KnowledgeBaseActDocumentConflictAction.OpenExisting:
                    result = CopyDocumentPathResult(pathResult, openExistingRequested: true);
                    return true;
                case KnowledgeBaseActDocumentConflictAction.Overwrite:
                    result = CopyDocumentPathResult(pathResult, overwriteExisting: true);
                    return true;
                case KnowledgeBaseActDocumentConflictAction.SaveCopy:
                    result = pathResult;
                    saveCopyRequested = true;
                    return false;
                default:
                    return false;
            }
        }

        private bool ConfirmGeneratedDocumentOverwrite(string documentPath, IWin32Window? owner)
        {
            DialogResult confirmation = MessageBox.Show(
                owner ?? this,
                $"Сформированный DOCX будет перезаписан:\n{documentPath}\n\nПродолжить?",
                "Повторное формирование акта",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            return confirmation == DialogResult.Yes;
        }

        private static string BuildCopyDocumentPath(string originalPath)
        {
            string directory = Path.GetDirectoryName(originalPath) ?? AppContext.BaseDirectory;
            string stem = Path.GetFileNameWithoutExtension(originalPath);
            string candidate = Path.Combine(directory, $"{stem}_копия.docx");
            int index = 2;
            while (File.Exists(candidate))
            {
                candidate = Path.Combine(directory, $"{stem}_копия_{index}.docx");
                index++;
            }

            return candidate;
        }

        private static KnowledgeBaseActDocumentPathResult CopyDocumentPathResult(
            KnowledgeBaseActDocumentPathResult source,
            bool overwriteExisting = false,
            bool openExistingRequested = false) =>
            new()
            {
                IsSuccess = source.IsSuccess,
                ErrorMessage = source.ErrorMessage,
                FileName = source.FileName,
                AbsolutePath = source.AbsolutePath,
                StoredPath = source.StoredPath,
                StoredDirectoryPath = source.StoredDirectoryPath,
                TargetFileExists = source.TargetFileExists,
                OverwriteExisting = overwriteExisting,
                OpenExistingRequested = openExistingRequested
            };

        private string ResolveActDocumentsBaseDirectory()
        {
            string? databaseDirectory = string.IsNullOrWhiteSpace(CurrentDataPath)
                ? null
                : Path.GetDirectoryName(Path.GetFullPath(CurrentDataPath));

            return string.IsNullOrWhiteSpace(databaseDirectory)
                ? AppContext.BaseDirectory
                : databaseDirectory;
        }

        private static KbActDocument CreatePreparedActDocument(KbAct act, string storedPath) =>
            new()
            {
                ActId = act.ActId,
                VersionNumber = 1,
                TemplateId = KnowledgeBaseActDocxTemplateService.GetTemplateId(act.ActType),
                TemplateVersion = KnowledgeBaseActDocxTemplateService.TemplateVersion,
                Path = storedPath,
                IsLatest = true
            };

        private static KbActDocument CreateGeneratedActDocument(
            KbAct act,
            string storedPath,
            string contentHash,
            DateTime generatedAt)
        {
            KbActDocument document = CreatePreparedActDocument(act, storedPath);
            document.GeneratedAt = generatedAt;
            document.ContentHash = contentHash;
            return document;
        }

        private sealed class ActFormSaveResult
        {
            public bool IsSuccess { get; init; }

            public KbAct? Act { get; init; }

            public IReadOnlyList<KbActExecutor> Executors { get; init; } = Array.Empty<KbActExecutor>();

            public KnowledgeBaseActDocumentPathResult? DocumentPathResult { get; init; }

            public static ActFormSaveResult Failed() => new();
        }

        private void ShowActGeneratedMessage(string outputPath, IWin32Window? owner = null)
        {
            string message = $"DOCX акта сформирован: {outputPath}";
            MessageBox.Show(
                owner ?? this,
                message,
                "Акт",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            SetLastActionText(message);
        }

        private void ShowActGenerationError(string errorMessage, IWin32Window? owner = null)
        {
            MessageBox.Show(
                owner ?? this,
                errorMessage,
                "Акт",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            SetLastActionText($"Ошибка формирования DOCX: {errorMessage}");
        }

        private void ShowActSaveError(string errorMessage, IWin32Window? owner = null)
        {
            MessageBox.Show(
                owner ?? this,
                $"Не удалось сохранить акт: {errorMessage}",
                "Акт",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            SetLastActionText($"Ошибка сохранения акта: {errorMessage}");
        }
    }
}
