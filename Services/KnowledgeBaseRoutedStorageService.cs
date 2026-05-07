using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public enum KnowledgeBaseStorageKind
    {
        LegacyJson,
        Sqlite
    }

    public sealed class KnowledgeBaseRoutedStorageService : IKnowledgeBaseStorageService
    {
        private readonly IAppLogger _logger;

        public KnowledgeBaseRoutedStorageService(
            string savePath,
            IAppLogger? logger = null)
        {
            SavePath = savePath;
            _logger = logger ?? NullAppLogger.Instance;
        }

        public string SavePath { get; set; }

        public KnowledgeBaseStorageKind CurrentKind => ResolveStorageKind(SavePath);

        public KnowledgeBaseStorageLoadResult Load() =>
            CreateInnerStorage().Load();

        public bool Save(SavedData data, out string? errorMessage) =>
            CreateInnerStorage().Save(data, out errorMessage);

        public KnowledgeBaseSnapshotCreateResult CreateManualSnapshot(SavedData data, string note) =>
            CreateInnerStorage().CreateManualSnapshot(data, note);

        public KnowledgeBaseSnapshotListResult ListSnapshots() =>
            CreateInnerStorage().ListSnapshots();

        public KnowledgeBaseSnapshotDataResult ReadSnapshotData(KnowledgeBaseSnapshotEntry snapshot) =>
            CreateInnerStorage().ReadSnapshotData(snapshot);

        public KnowledgeBaseSnapshotRestoreResult RestoreSnapshot(KnowledgeBaseSnapshotEntry snapshot) =>
            CreateInnerStorage().RestoreSnapshot(snapshot);

        public KnowledgeBaseChangeLogAppendResult AppendChangeLog(
            string actionKind,
            string summary,
            string details = "") =>
            CreateInnerStorage().AppendChangeLog(actionKind, summary, details);

        public KnowledgeBaseChangeLogListResult ListChangeLog() =>
            CreateInnerStorage().ListChangeLog();

        private IKnowledgeBaseStorageService CreateInnerStorage() =>
            CurrentKind == KnowledgeBaseStorageKind.Sqlite
                ? KnowledgeBaseStorageServiceFactory.CreateSqliteStorage(SavePath, _logger)
                : KnowledgeBaseStorageServiceFactory.CreateLegacyJsonFileStorage(SavePath, _logger);

        private static KnowledgeBaseStorageKind ResolveStorageKind(string path) =>
            KnowledgeBaseStoragePaths.IsSqlitePath(path)
                ? KnowledgeBaseStorageKind.Sqlite
                : KnowledgeBaseStorageKind.LegacyJson;
    }
}
