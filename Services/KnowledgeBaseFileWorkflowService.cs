using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public enum KnowledgeBaseFileLoadOutcome
    {
        LoadedExisting,
        LoadedBackup,
        CreatedDefaultAndSaved,
        CreatedDefaultUnsaved,
        CreatedDefaultAfterError,
        FileMissingError,
        LoadError
    }

    public class KnowledgeBaseFileLoadResult
    {
        public KnowledgeBaseFileLoadOutcome Outcome { get; init; }

        public string SourcePath { get; init; } = string.Empty;

        public string? BackupPath { get; init; }

        public string? ErrorMessage { get; init; }

        public string? PrimaryErrorMessage { get; init; }

        public KnowledgeBaseSessionViewState? ViewState { get; init; }

        public bool IsSuccess =>
            Outcome == KnowledgeBaseFileLoadOutcome.LoadedExisting ||
            Outcome == KnowledgeBaseFileLoadOutcome.LoadedBackup ||
            Outcome == KnowledgeBaseFileLoadOutcome.CreatedDefaultAndSaved ||
            Outcome == KnowledgeBaseFileLoadOutcome.CreatedDefaultUnsaved ||
            Outcome == KnowledgeBaseFileLoadOutcome.CreatedDefaultAfterError;
    }

    public class KnowledgeBaseFileSaveResult
    {
        public bool IsSuccess { get; init; }

        public string? ErrorMessage { get; init; }

        public KnowledgeBaseSessionViewState? ViewState { get; init; }
    }

    /// <summary>
    /// Координирует прикладные файловые сценарии поверх session-state и JSON-хранилища.
    /// Не зависит от UI, поэтому может тестироваться отдельно от формы.
    /// </summary>
    public class KnowledgeBaseFileWorkflowService
    {
        private readonly KnowledgeBaseSessionService _session;
        private readonly KnowledgeBaseSessionWorkflowService _sessionWorkflowService;
        private readonly JsonStorageService _storage;
        private readonly IAppLogger _logger;

        public KnowledgeBaseFileWorkflowService(
            KnowledgeBaseSessionService session,
            JsonStorageService storage,
            IAppLogger? logger = null)
        {
            _session = session;
            _sessionWorkflowService = new KnowledgeBaseSessionWorkflowService(session);
            _storage = storage;
            _logger = logger ?? NullAppLogger.Instance;
        }

        public string SavePath
        {
            get => _storage.SavePath;
            set => _storage.SavePath = value;
        }

        public KnowledgeBaseFileLoadResult Load(
            bool createDefaultIfMissing = true,
            bool fallbackToDefaultOnError = true)
        {
            var loadResult = _storage.Load();

            if (loadResult.FileMissing)
            {
                if (!createDefaultIfMissing)
                {
                    var missingFileResult = new KnowledgeBaseFileLoadResult
                    {
                        Outcome = KnowledgeBaseFileLoadOutcome.FileMissingError,
                        SourcePath = loadResult.SourcePath
                    };

                    Log(
                        "FileWorkflowFileMissingError",
                        AppLogLevel.Warning,
                        "Knowledge base file is missing.",
                        properties: CreateProperties(
                            ("sourcePath", missingFileResult.SourcePath),
                            ("backupPath", missingFileResult.BackupPath),
                            ("requiresSave", _session.RequiresSave)));

                    return missingFileResult;
                }

                _session.InitializeDefaultData(recordAsSavedState: false);
                var saveResult = SaveCore(_session.GetCurrentWorkshopNodes(), emitWorkflowEvent: false);

                var createdDefaultResult = new KnowledgeBaseFileLoadResult
                {
                    Outcome = saveResult.IsSuccess
                        ? KnowledgeBaseFileLoadOutcome.CreatedDefaultAndSaved
                        : KnowledgeBaseFileLoadOutcome.CreatedDefaultUnsaved,
                    SourcePath = SavePath,
                    ErrorMessage = saveResult.ErrorMessage,
                    ViewState = BuildViewState()
                };

                Log(
                    saveResult.IsSuccess
                        ? "FileWorkflowCreateDefaultAndSave"
                        : "FileWorkflowCreateDefaultUnsaved",
                    saveResult.IsSuccess ? AppLogLevel.Information : AppLogLevel.Warning,
                    saveResult.IsSuccess
                        ? "Default knowledge base was created and saved."
                        : "Default knowledge base was created in memory but could not be saved.",
                    properties: CreateProperties(
                        ("sourcePath", createdDefaultResult.SourcePath),
                        ("errorMessage", createdDefaultResult.ErrorMessage),
                        ("requiresSave", _session.RequiresSave)));

                return createdDefaultResult;
            }

            if (!loadResult.IsSuccess)
            {
                if (!fallbackToDefaultOnError)
                {
                    var loadErrorResult = new KnowledgeBaseFileLoadResult
                    {
                        Outcome = KnowledgeBaseFileLoadOutcome.LoadError,
                        SourcePath = loadResult.SourcePath,
                        BackupPath = loadResult.BackupPath,
                        ErrorMessage = loadResult.ErrorMessage,
                        PrimaryErrorMessage = loadResult.PrimaryErrorMessage
                    };

                    Log(
                        "FileWorkflowLoadError",
                        AppLogLevel.Error,
                        "Knowledge base file load failed.",
                        properties: CreateProperties(
                            ("sourcePath", loadErrorResult.SourcePath),
                            ("backupPath", loadErrorResult.BackupPath),
                            ("errorMessage", loadErrorResult.ErrorMessage),
                            ("primaryErrorMessage", loadErrorResult.PrimaryErrorMessage),
                            ("requiresSave", _session.RequiresSave)));

                    return loadErrorResult;
                }

                _session.InitializeDefaultData(recordAsSavedState: false);
                _session.RecordSavedState(_session.GetCurrentWorkshopNodes());
                _session.SetRequiresSave(true);

                var result = new KnowledgeBaseFileLoadResult
                {
                    Outcome = KnowledgeBaseFileLoadOutcome.CreatedDefaultAfterError,
                    SourcePath = loadResult.SourcePath,
                    BackupPath = loadResult.BackupPath,
                    ErrorMessage = loadResult.ErrorMessage,
                    PrimaryErrorMessage = loadResult.PrimaryErrorMessage,
                    ViewState = BuildViewState()
                };

                Log(
                    "FileWorkflowCreateDefaultAfterError",
                    AppLogLevel.Warning,
                    "Default knowledge base was created after a file load error.",
                    properties: CreateProperties(
                        ("sourcePath", result.SourcePath),
                        ("backupPath", result.BackupPath),
                        ("errorMessage", result.ErrorMessage),
                        ("primaryErrorMessage", result.PrimaryErrorMessage),
                        ("requiresSave", _session.RequiresSave)));

                return result;
            }

            _session.ApplyLoadedData(loadResult.Data!, recordAsSavedState: true);
            _session.SetRequiresSave(loadResult.LoadedFromBackup);

            var successResult = new KnowledgeBaseFileLoadResult
            {
                Outcome = loadResult.LoadedFromBackup
                    ? KnowledgeBaseFileLoadOutcome.LoadedBackup
                    : KnowledgeBaseFileLoadOutcome.LoadedExisting,
                SourcePath = loadResult.SourcePath,
                BackupPath = loadResult.BackupPath,
                ErrorMessage = loadResult.ErrorMessage,
                PrimaryErrorMessage = loadResult.PrimaryErrorMessage,
                ViewState = BuildViewState()
            };

            Log(
                loadResult.LoadedFromBackup
                    ? "FileWorkflowLoadBackup"
                    : "FileWorkflowLoadExisting",
                loadResult.LoadedFromBackup
                    ? AppLogLevel.Warning
                    : AppLogLevel.Information,
                loadResult.LoadedFromBackup
                    ? "Knowledge base loaded from backup."
                    : "Knowledge base loaded from primary file.",
                properties: CreateProperties(
                    ("sourcePath", successResult.SourcePath),
                    ("backupPath", successResult.BackupPath),
                    ("errorMessage", successResult.ErrorMessage),
                    ("primaryErrorMessage", successResult.PrimaryErrorMessage),
                    ("requiresSave", _session.RequiresSave)));

            return successResult;
        }

        public KnowledgeBaseFileSaveResult Save(List<KbNode> currentRoots) =>
            SaveCore(currentRoots, emitWorkflowEvent: true);

        public KnowledgeBaseFileSaveResult ReplaceAllData(SavedData data)
        {
            string? schemaVersionError = KnowledgeBaseDataService.ValidateSupportedSchemaVersion(data.SchemaVersion);
            if (schemaVersionError != null)
                return BuildReplaceAllDataValidationFailure(schemaVersionError);

            string? workshopValidationError = KnowledgeBaseDataService.ValidateWorkshopNames(data.Workshops);
            if (workshopValidationError != null)
                return BuildReplaceAllDataValidationFailure(workshopValidationError);

            var normalizedConfig = KnowledgeBaseDataService.NormalizeConfig(data.Config);
            var normalizedWorkshops = KnowledgeBaseDataService.NormalizeWorkshops(data.Workshops);
            string lastWorkshop = KnowledgeBaseDataService.ResolveWorkshop(normalizedWorkshops, data.LastWorkshop);

            var normalizedData = new SavedData
            {
                SchemaVersion = data.SchemaVersion,
                Config = normalizedConfig,
                Workshops = normalizedWorkshops,
                LastWorkshop = lastWorkshop
            };

            if (_storage.Save(normalizedData, out var errorMessage))
            {
                _session.ApplyLoadedData(normalizedData, recordAsSavedState: true);
                var result = new KnowledgeBaseFileSaveResult
                {
                    IsSuccess = true,
                    ViewState = BuildViewState()
                };

                Log(
                    "FileWorkflowReplaceAllDataSucceeded",
                    AppLogLevel.Information,
                    "Knowledge base data was replaced successfully.",
                    properties: CreateProperties(
                        ("requiresSave", _session.RequiresSave)));

                return result;
            }

            var failureResult = new KnowledgeBaseFileSaveResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };

            Log(
                "FileWorkflowReplaceAllDataFailed",
                AppLogLevel.Error,
                "Knowledge base data replacement failed.",
                properties: CreateProperties(
                    ("errorMessage", failureResult.ErrorMessage),
                    ("requiresSave", _session.RequiresSave)));

            return failureResult;
        }

        private KnowledgeBaseFileSaveResult BuildReplaceAllDataValidationFailure(string errorMessage)
        {
            var failureResult = new KnowledgeBaseFileSaveResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };

            Log(
                "FileWorkflowReplaceAllDataFailed",
                AppLogLevel.Error,
                "Knowledge base data replacement failed.",
                properties: CreateProperties(
                    ("errorMessage", failureResult.ErrorMessage),
                    ("requiresSave", _session.RequiresSave)));

            return failureResult;
        }

        private KnowledgeBaseSessionViewState BuildViewState() =>
            _sessionWorkflowService.BuildViewState();

        private KnowledgeBaseFileSaveResult SaveCore(List<KbNode> currentRoots, bool emitWorkflowEvent)
        {
            var data = _session.CreateSaveData(currentRoots);

            if (_storage.Save(data, out var errorMessage))
            {
                _session.RecordSavedState(currentRoots);
                var result = new KnowledgeBaseFileSaveResult { IsSuccess = true };
                if (emitWorkflowEvent)
                {
                    Log(
                        "FileWorkflowSaveSucceeded",
                        AppLogLevel.Information,
                        "Knowledge base saved successfully.",
                        properties: CreateProperties(
                            ("requiresSave", _session.RequiresSave)));
                }

                return result;
            }

            var failureResult = new KnowledgeBaseFileSaveResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };

            if (emitWorkflowEvent)
            {
                Log(
                    "FileWorkflowSaveFailed",
                    AppLogLevel.Error,
                    "Knowledge base save failed.",
                    properties: CreateProperties(
                        ("errorMessage", failureResult.ErrorMessage),
                        ("requiresSave", _session.RequiresSave)));
            }

            return failureResult;
        }

        private void Log(
            string eventName,
            AppLogLevel level,
            string message,
            Exception? exception = null,
            IReadOnlyDictionary<string, object?>? properties = null) =>
            _logger.Log(eventName, level, message, exception, properties);

        private Dictionary<string, object?> CreateProperties(params (string Key, object? Value)[] values)
        {
            var properties = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["savePath"] = SavePath,
                ["currentWorkshop"] = _session.CurrentWorkshop,
                ["lastWorkshop"] = _session.LastSavedWorkshop
            };

            foreach (var (key, value) in values)
            {
                if (string.IsNullOrWhiteSpace(key) || value == null)
                    continue;

                properties[key] = value;
            }

            return properties;
        }
    }
}
