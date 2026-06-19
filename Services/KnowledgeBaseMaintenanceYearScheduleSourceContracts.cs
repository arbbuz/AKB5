using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public interface IKnowledgeBaseMaintenanceYearScheduleSourceService
    {
        List<KnowledgeBaseMaintenanceYearScheduleSourceRow> BuildRows(
            IReadOnlyList<KbNode>? roots,
            IReadOnlyList<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles);

        KnowledgeBaseMaintenanceYearScheduleSourceApplyResult ApplyRows(
            IReadOnlyList<KnowledgeBaseMaintenanceYearScheduleSourceRow>? rows,
            IReadOnlyList<KbNode>? roots,
            IReadOnlyList<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles);
    }

    public interface IKnowledgeBaseMaintenanceYearScheduleSourceExchange
    {
        KnowledgeBaseMaintenanceYearScheduleSourceExportResult ExportWorkbook(
            IReadOnlyList<KbNode>? roots,
            IReadOnlyList<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles);

        KnowledgeBaseMaintenanceYearScheduleSourceImportResult ImportWorkbook(
            byte[] workbookPackage,
            IReadOnlyList<KbNode>? roots,
            IReadOnlyList<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles);
    }

    public sealed class KnowledgeBaseMaintenanceYearScheduleSourceRow
    {
        public string OwnerNodeId { get; init; } = string.Empty;

        public string Path { get; init; } = string.Empty;

        public string NodeName { get; init; } = string.Empty;

        public string InventoryNumber { get; init; } = string.Empty;

        public int SequenceNumber { get; init; }

        public string SystemNodeId { get; init; } = string.Empty;

        public string SystemName { get; init; } = string.Empty;

        public string SystemInventoryNumber { get; init; } = string.Empty;

        public int SystemTreeOrder { get; init; }

        public bool IsIncludedInSchedule { get; init; }

        public List<KbMaintenanceYearScheduleEntry> YearScheduleEntries { get; init; } = new();

        public int TreeOrder { get; init; }

        public uint SourceRowNumber { get; init; }

        public bool HasManualSchedule => YearScheduleEntries.Count > 0;
    }

    public static class KnowledgeBaseMaintenanceYearScheduleSourceRowUtilities
    {
        public static List<KbMaintenanceYearScheduleEntry> CloneYearScheduleEntries(
            IEnumerable<KbMaintenanceYearScheduleEntry>? entries) =>
            entries?
                .Select(static entry => new KbMaintenanceYearScheduleEntry
                {
                    Month = entry.Month,
                    WorkKind = entry.WorkKind,
                    Hours = entry.Hours
                })
                .ToList()
            ?? new List<KbMaintenanceYearScheduleEntry>();
    }

    public sealed class KnowledgeBaseMaintenanceYearScheduleSourceApplyResult
    {
        public bool IsSuccess { get; init; }

        public string ErrorMessage { get; init; } = string.Empty;

        public List<KbMaintenanceScheduleProfile> MaintenanceScheduleProfiles { get; init; } = new();

        public int EditedRowCount { get; init; }

        public int UpdatedProfileCount { get; init; }

        public int ClearedProfileCount { get; init; }

        public int UnchangedProfileCount { get; init; }

        public List<string> UnresolvedRows { get; init; } = new();
    }

    public sealed class KnowledgeBaseMaintenanceYearScheduleSourceExportResult
    {
        public bool IsSuccess { get; init; }

        public string ErrorMessage { get; init; } = string.Empty;

        public byte[] WorkbookPackage { get; init; } = Array.Empty<byte>();

        public int ExportedProfileCount { get; init; }

        public int ManualScheduleProfileCount { get; init; }

        public int AutomaticFallbackProfileCount { get; init; }
    }

    public sealed class KnowledgeBaseMaintenanceYearScheduleSourceImportResult
    {
        public bool IsSuccess { get; init; }

        public string ErrorMessage { get; init; } = string.Empty;

        public List<KbMaintenanceScheduleProfile> MaintenanceScheduleProfiles { get; init; } = new();

        public int ImportedRowCount { get; init; }

        public int UpdatedProfileCount { get; init; }

        public int ClearedProfileCount { get; init; }

        public int UnchangedProfileCount { get; init; }

        public List<string> UnresolvedRows { get; init; } = new();
    }
}
