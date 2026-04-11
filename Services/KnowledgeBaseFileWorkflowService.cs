using System.Collections.Generic;
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
    }

    /// <summary>
    /// Координирует прикладные файловые сценарии поверх session-state и JSON-хранилища.
    /// Не зависит от UI, поэтому может тестироваться отдельно от формы.
    /// </summary>
    public class KnowledgeBaseFileWorkflowService
    {
        private readonly KnowledgeBaseSessionService _session;
        private readonly JsonStorageService _storage;

        public KnowledgeBaseFileWorkflowService(
            KnowledgeBaseSessionService session,
            JsonStorageService storage)
        {
            _session = session;
            _storage = storage;
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
                    return new KnowledgeBaseFileLoadResult
                    {
                        Outcome = KnowledgeBaseFileLoadOutcome.FileMissingError,
                        SourcePath = loadResult.SourcePath
                    };
                }

                _session.InitializeDefaultData(recordAsSavedState: false);
                var saveResult = Save(_session.GetCurrentWorkshopNodes());

                return new KnowledgeBaseFileLoadResult
                {
                    Outcome = saveResult.IsSuccess
                        ? KnowledgeBaseFileLoadOutcome.CreatedDefaultAndSaved
                        : KnowledgeBaseFileLoadOutcome.CreatedDefaultUnsaved,
                    SourcePath = SavePath,
                    ErrorMessage = saveResult.ErrorMessage
                };
            }

            if (!loadResult.IsSuccess)
            {
                if (!fallbackToDefaultOnError)
                {
                    return new KnowledgeBaseFileLoadResult
                    {
                        Outcome = KnowledgeBaseFileLoadOutcome.LoadError,
                        SourcePath = loadResult.SourcePath,
                        BackupPath = loadResult.BackupPath,
                        ErrorMessage = loadResult.ErrorMessage,
                        PrimaryErrorMessage = loadResult.PrimaryErrorMessage
                    };
                }

                _session.InitializeDefaultData(recordAsSavedState: false);
                _session.RecordSavedState(_session.GetCurrentWorkshopNodes());
                _session.SetRequiresSave(true);

                return new KnowledgeBaseFileLoadResult
                {
                    Outcome = KnowledgeBaseFileLoadOutcome.CreatedDefaultAfterError,
                    SourcePath = loadResult.SourcePath,
                    BackupPath = loadResult.BackupPath,
                    ErrorMessage = loadResult.ErrorMessage,
                    PrimaryErrorMessage = loadResult.PrimaryErrorMessage
                };
            }

            _session.ApplyLoadedData(loadResult.Data!, recordAsSavedState: true);
            _session.SetRequiresSave(loadResult.LoadedFromBackup);

            return new KnowledgeBaseFileLoadResult
            {
                Outcome = loadResult.LoadedFromBackup
                    ? KnowledgeBaseFileLoadOutcome.LoadedBackup
                    : KnowledgeBaseFileLoadOutcome.LoadedExisting,
                SourcePath = loadResult.SourcePath,
                BackupPath = loadResult.BackupPath,
                ErrorMessage = loadResult.ErrorMessage,
                PrimaryErrorMessage = loadResult.PrimaryErrorMessage
            };
        }

        public KnowledgeBaseFileSaveResult Save(List<KbNode> currentRoots)
        {
            var data = _session.CreateSaveData(currentRoots);

            if (_storage.Save(data, out var errorMessage))
            {
                _session.RecordSavedState(currentRoots);
                return new KnowledgeBaseFileSaveResult { IsSuccess = true };
            }

            return new KnowledgeBaseFileSaveResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };
        }
    }
}
