namespace AsutpKnowledgeBase.Models
{
    public sealed class KbMaintenanceMonthSheetModel
    {
        public int Year { get; init; }

        public int Month { get; init; }

        public int TotalPlannedHours { get; init; }

        public List<KbMaintenanceMonthSheetDayTotal> DailyTotals { get; init; } = new();

        public List<KbMaintenanceMonthSheetSystemGroup> SystemGroups { get; init; } = new();
    }

    public sealed class KbMaintenanceMonthSheetDayTotal
    {
        public int DayOfMonth { get; init; }

        public int TotalHours { get; init; }
    }

    public sealed class KbMaintenanceMonthSheetSystemGroup
    {
        public int SequenceNumber { get; init; }

        public string SystemNodeId { get; init; } = string.Empty;

        public string SystemName { get; init; } = string.Empty;

        public string InventoryNumber { get; init; } = string.Empty;

        public List<KbMaintenanceMonthSheetDetailRow> DetailRows { get; init; } = new();
    }

    public sealed class KbMaintenanceMonthSheetDetailRow
    {
        public string OwnerNodeId { get; init; } = string.Empty;

        public string NodeName { get; init; } = string.Empty;

        public KbNodeType NodeType { get; init; } = KbNodeType.Unknown;

        public int TotalHours { get; init; }

        public List<KbMaintenanceMonthSheetDayCell> DayCells { get; init; } = new();
    }

    public sealed class KbMaintenanceMonthSheetDayCell
    {
        public int DayOfMonth { get; init; }

        public int TotalHours { get; init; }

        public List<KbMaintenanceMonthSheetWorkEntry> WorkEntries { get; init; } = new();
    }

    public sealed class KbMaintenanceMonthSheetWorkEntry
    {
        public KbMaintenanceWorkKind WorkKind { get; init; }

        public int Hours { get; init; }

        public string PlanText { get; init; } = string.Empty;
    }
}
