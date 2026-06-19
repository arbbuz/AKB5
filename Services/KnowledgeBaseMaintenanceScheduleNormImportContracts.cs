using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public interface IKnowledgeBaseMaintenanceScheduleNormImporter
    {
        KnowledgeBaseMaintenanceScheduleNormImportResult ImportWorkbook(
            byte[] packageBytes,
            IReadOnlyList<KbNode>? roots,
            IReadOnlyList<KbMaintenanceScheduleProfile>? existingProfiles);
    }

    public sealed class KnowledgeBaseMaintenanceScheduleNormImportResult
    {
        public bool IsSuccess { get; init; }

        public string ErrorMessage { get; init; } = string.Empty;

        public List<KbMaintenanceScheduleProfile> MaintenanceScheduleProfiles { get; init; } = new();

        public int ImportedEquipmentCount { get; init; }

        public int CreatedProfileCount { get; init; }

        public int UpdatedProfileCount { get; init; }

        public int UnchangedProfileCount { get; init; }

        public int MatchedByInventoryCount { get; init; }

        public int MatchedByNameCount { get; init; }

        public int YearScheduleAppliedProfileCount { get; init; }

        public int DisabledMissingProfileCount { get; init; }

        public List<string> UnresolvedEntries { get; init; } = new();

        public List<string> WorkbookWarnings { get; init; } = new();

        public List<KnowledgeBaseMaintenanceScheduleMissingProfile> MissingIncludedProfiles { get; init; } = new();
    }

    public sealed class KnowledgeBaseMaintenanceScheduleMissingProfile
    {
        public string OwnerNodeId { get; init; } = string.Empty;

        public string DisplayText { get; init; } = string.Empty;
    }
}
