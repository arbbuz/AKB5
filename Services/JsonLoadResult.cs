using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public class JsonLoadResult
    {
        public SavedData? Data { get; init; }
        public string SourcePath { get; init; } = string.Empty;
        public string? BackupPath { get; init; }
        public string? ErrorMessage { get; init; }
        public string? PrimaryErrorMessage { get; init; }
        public bool FileMissing { get; init; }
        public bool LoadedFromBackup { get; init; }

        public bool IsSuccess => Data != null;
    }
}
