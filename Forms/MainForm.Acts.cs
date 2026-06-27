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
                CompositionEntry = selectedEntry
            });

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
            using var dialog = new KnowledgeBaseActForm(result.Act, currentExecutors);
            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            ActFormSaveResult saveResult = SaveActFromForm(
                dialog.Result,
                dialog.ResultExecutors,
                prepareDocumentPath: dialog.DocumentGenerationRequested);
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
            string preferredActId = string.Empty;
            while (true)
            {
                IReadOnlyList<KnowledgeBaseActJournalRow> rows = _actJournalService.BuildRows(
                    _session.Acts,
                    _session.ActDocuments);
                using var dialog = new KnowledgeBaseActsJournalForm(rows, preferredActId);
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;

                preferredActId = dialog.SelectedActId;
                if (string.IsNullOrWhiteSpace(preferredActId))
                    return;

                switch (dialog.SelectedAction)
                {
                    case KnowledgeBaseActsJournalAction.Open:
                        OpenExistingActFromJournal(preferredActId);
                        break;
                    case KnowledgeBaseActsJournalAction.GenerateDocument:
                        PrepareExistingActDocumentFromJournal(preferredActId);
                        break;
                    case KnowledgeBaseActsJournalAction.DeleteDraft:
                        DeleteDraftActFromJournal(preferredActId);
                        break;
                    case KnowledgeBaseActsJournalAction.CancelAct:
                        ChangeActStatusFromJournal(preferredActId, KbActStatus.Cancelled);
                        break;
                    case KnowledgeBaseActsJournalAction.AnnulAct:
                        ChangeActStatusFromJournal(preferredActId, KbActStatus.Annulled);
                        break;
                    default:
                        return;
                }
            }
        }

        private bool OpenExistingActFromJournal(string actId)
        {
            KbAct? act = FindActById(actId);
            if (act == null)
            {
                ShowActJournalError("Акт не найден.");
                return false;
            }

            IReadOnlyList<KbActExecutor> executors = GetActExecutors(actId);
            using var dialog = new KnowledgeBaseActForm(act, executors);
            if (dialog.ShowDialog(this) != DialogResult.OK)
                return false;

            if (dialog.DocumentGenerationRequested &&
                !KnowledgeBaseActJournalService.CanGenerateDocument(act.Status))
            {
                ShowActJournalError("Для отмененного или аннулированного акта DOCX не формируется.");
                return false;
            }

            ActFormSaveResult saveResult = SaveActFromForm(
                dialog.Result,
                dialog.ResultExecutors,
                prepareDocumentPath: dialog.DocumentGenerationRequested);
            if (!saveResult.IsSuccess)
            {
                return false;
            }

            if (dialog.DocumentGenerationRequested)
                return GenerateActDocumentAfterSave(saveResult);

            ShowActSavedMessage(documentGenerationRequested: false);
            return true;
        }

        private bool PrepareExistingActDocumentFromJournal(string actId)
        {
            KbAct? act = FindActById(actId);
            if (act == null)
            {
                ShowActJournalError("Акт не найден.");
                return false;
            }

            if (!KnowledgeBaseActJournalService.CanGenerateDocument(act.Status))
            {
                ShowActJournalError("Для отмененного или аннулированного акта DOCX не формируется.");
                return false;
            }

            var editorService = new KnowledgeBaseActEditorService();
            KnowledgeBaseActEditorSaveResult savePreparation = editorService.PrepareForSave(act);
            if (!savePreparation.IsSuccess || savePreparation.Act == null)
            {
                ShowActJournalError(savePreparation.ErrorMessage);
                return false;
            }

            IReadOnlyList<KbActExecutor> executors = GetActExecutors(actId);
            string? executorValidationError = KnowledgeBaseActEditorService.ValidateExecutorsForSave(executors);
            if (executorValidationError != null)
            {
                ShowActJournalError(executorValidationError);
                return false;
            }

            IReadOnlyList<KbActExecutor> normalizedExecutors = KnowledgeBaseDataService.NormalizeActExecutors(
                executors,
                new[] { actId });
            ActFormSaveResult saveResult = SaveActFromForm(
                savePreparation.Act,
                normalizedExecutors,
                prepareDocumentPath: true);
            if (!saveResult.IsSuccess)
            {
                return false;
            }

            return GenerateActDocumentAfterSave(saveResult);
        }

        private bool DeleteDraftActFromJournal(string actId)
        {
            KbAct? act = FindActById(actId);
            if (act == null)
            {
                ShowActJournalError("Акт не найден.");
                return false;
            }

            if (!_actJournalService.CanDeletePhysically(act, _session.ActDocuments))
            {
                ShowActJournalError("Удалить можно только черновик без номера и без DOCX.");
                return false;
            }

            DialogResult confirmResult = MessageBox.Show(
                this,
                "Удалить черновик акта? Это действие удалит запись из базы.",
                "Журнал актов",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Warning);
            if (confirmResult != DialogResult.OK)
                return false;

            _session.ReplaceActs(_session.Acts.Where(existingAct =>
                !string.Equals(existingAct.ActId, actId, StringComparison.Ordinal)));
            return SaveActJournalMutation("Черновик акта удален.");
        }

        private bool ChangeActStatusFromJournal(string actId, KbActStatus status)
        {
            KbAct? act = FindActById(actId);
            if (act == null)
            {
                ShowActJournalError("Акт не найден.");
                return false;
            }

            if (!KnowledgeBaseActJournalService.CanChangeStatus(act.Status))
            {
                ShowActJournalError("Статус этого акта уже нельзя изменить через журнал.");
                return false;
            }

            string statusText = KnowledgeBaseActJournalService.FormatStatus(status);
            DialogResult confirmResult = MessageBox.Show(
                this,
                $"Изменить статус выбранного акта на \"{statusText}\"?",
                "Журнал актов",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Warning);
            if (confirmResult != DialogResult.OK)
                return false;

            KbAct updatedAct = KnowledgeBaseActEditorService.CloneAct(act);
            updatedAct.Status = status;
            updatedAct.UpdatedAt = DateTime.Now;
            var acts = _session.Acts
                .Where(existingAct => !string.Equals(existingAct.ActId, actId, StringComparison.Ordinal))
                .ToList();
            acts.Add(updatedAct);
            _session.ReplaceActs(acts);
            return SaveActJournalMutation($"Статус акта изменен на \"{statusText}\".");
        }

        private bool SaveActJournalMutation(string successMessage)
        {
            UpdateDirtyState();
            KnowledgeBaseFileSaveResult saveResult = _fileWorkflowService.Save(GetPersistedTreeData());
            UpdateUI();
            if (saveResult.IsSuccess)
            {
                MessageBox.Show(
                    this,
                    successMessage,
                    "Журнал актов",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                SetLastActionText(successMessage);
                return true;
            }

            ShowActJournalError($"Не удалось сохранить изменения журнала актов: {saveResult.ErrorMessage}");
            return false;
        }

        private KbAct? FindActById(string actId) =>
            _session.Acts.FirstOrDefault(act =>
                string.Equals(act.ActId, actId, StringComparison.Ordinal));

        private IReadOnlyList<KbActExecutor> GetActExecutors(string actId) =>
            _session.ActExecutors
                .Where(executor => string.Equals(executor.ActId, actId, StringComparison.Ordinal))
                .ToList();

        private void ShowActSavedMessage(bool documentGenerationRequested)
        {
            string message = documentGenerationRequested
                ? "Черновик сохранен, путь DOCX выбран."
                : "Черновик сохранен.";
            MessageBox.Show(
                this,
                message,
                "Акт",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            SetLastActionText(message);
        }

        private void ShowActJournalError(string errorMessage)
        {
            MessageBox.Show(
                this,
                errorMessage,
                "Журнал актов",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            SetLastActionText($"Журнал актов: {errorMessage}");
        }

        private ActFormSaveResult SaveActFromForm(
            KbAct act,
            IReadOnlyList<KbActExecutor> executors,
            bool prepareDocumentPath)
        {
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
                    ShowActSaveError(numberingResult.ErrorMessage);
                    return ActFormSaveResult.Failed();
                }

                act = numberingResult.Act;
                actNumberSequences = numberingResult.NumberSequences;
                acts = acts
                    .Where(existingAct => !string.Equals(existingAct.ActId, act.ActId, StringComparison.Ordinal))
                    .ToList();
                acts.Add(act);

                if (!TrySelectActDocumentPath(act, actDocuments, out documentPathResult) ||
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
            var actExecutors = _session.ActExecutors
                .Where(existingExecutor => !string.Equals(existingExecutor.ActId, act.ActId, StringComparison.Ordinal))
                .ToList();
            actExecutors.AddRange(savedExecutors);
            _session.ReplaceActExecutors(actExecutors);
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
                return new ActFormSaveResult
                {
                    IsSuccess = true,
                    Act = act,
                    Executors = savedExecutors,
                    DocumentPathResult = documentPathResult
                };
            }

            MessageBox.Show(
                this,
                $"Не удалось сохранить акт: {saveResult.ErrorMessage}",
                "Акт",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            SetLastActionText($"Ошибка сохранения акта: {saveResult.ErrorMessage}");
            return ActFormSaveResult.Failed();
        }

        private bool GenerateActDocumentAfterSave(ActFormSaveResult saveResult)
        {
            if (saveResult.Act == null || saveResult.DocumentPathResult == null)
            {
                ShowActGenerationError("Акт сохранен, но путь DOCX не подготовлен.");
                return false;
            }

            string templatePath = KnowledgeBaseActDocxTemplateService.ResolveTemplatePath(
                saveResult.Act.ActType,
                AppContext.BaseDirectory);
            KnowledgeBaseActDocxGenerationResult generationResult = _actDocxPluginLoader.Generate(
                new KnowledgeBaseActDocxGenerationRequest
                {
                    Act = saveResult.Act,
                    Executors = saveResult.Executors,
                    TemplatePath = templatePath,
                    OutputPath = saveResult.DocumentPathResult.AbsolutePath
                });

            if (!generationResult.IsSuccess)
            {
                ShowActGenerationError($"Акт сохранен, но DOCX не сформирован: {generationResult.ErrorMessage}");
                return false;
            }

            if (!MarkActDocumentGenerated(
                saveResult.Act,
                saveResult.DocumentPathResult,
                generationResult.ContentHash))
            {
                return false;
            }

            ShowActGeneratedMessage(generationResult.OutputPath);
            return true;
        }

        private bool MarkActDocumentGenerated(
            KbAct act,
            KnowledgeBaseActDocumentPathResult documentPathResult,
            string contentHash)
        {
            DateTime now = DateTime.Now;
            KbAct updatedAct = KnowledgeBaseActEditorService.CloneAct(act);
            updatedAct.Status = KbActStatus.Generated;
            updatedAct.UpdatedAt = now;

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
                return true;

            MessageBox.Show(
                this,
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

            var documentPathService = new KnowledgeBaseActDocumentPathService();
            while (true)
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return false;

                result = documentPathService.PrepareDocumentPath(
                    new KnowledgeBaseActDocumentPathRequest
                    {
                        Act = act,
                        Config = _session.Config,
                        ExistingDocuments = actDocuments,
                        SelectedPath = dialog.FileName,
                        DatabasePath = CurrentDataPath,
                        ApplicationBasePath = AppContext.BaseDirectory
                    });

                if (result.IsSuccess)
                    return true;

                ShowActSaveError(result.ErrorMessage);
            }
        }

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

        private void ShowActGeneratedMessage(string outputPath)
        {
            string message = $"DOCX акта сформирован: {outputPath}";
            MessageBox.Show(
                this,
                message,
                "Акт",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            SetLastActionText(message);
        }

        private void ShowActGenerationError(string errorMessage)
        {
            MessageBox.Show(
                this,
                errorMessage,
                "Акт",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            SetLastActionText($"Ошибка формирования DOCX: {errorMessage}");
        }

        private void ShowActSaveError(string errorMessage)
        {
            MessageBox.Show(
                this,
                $"Не удалось сохранить акт: {errorMessage}",
                "Акт",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            SetLastActionText($"Ошибка сохранения акта: {errorMessage}");
        }
    }
}
