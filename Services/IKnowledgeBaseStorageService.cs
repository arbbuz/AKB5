using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public interface IKnowledgeBaseStorageService
    {
        string SavePath { get; set; }

        KnowledgeBaseStorageLoadResult Load();

        bool Save(SavedData data, out string? errorMessage);

        KnowledgeBaseSnapshotCreateResult CreateManualSnapshot(SavedData data, string note);

        KnowledgeBaseSnapshotListResult ListSnapshots();

        KnowledgeBaseSnapshotDataResult ReadSnapshotData(KnowledgeBaseSnapshotEntry snapshot);

        KnowledgeBaseSnapshotRestoreResult RestoreSnapshot(KnowledgeBaseSnapshotEntry snapshot);

        KnowledgeBaseChangeLogAppendResult AppendChangeLog(
            string actionKind,
            string summary,
            string details = "");

        KnowledgeBaseChangeLogListResult ListChangeLog();
    }
}
