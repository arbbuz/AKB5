namespace AsutpKnowledgeBase.Models
{
    public sealed class KbMaintenanceAnnualWorkbookModel
    {
        public int Year { get; init; }

        public string WorkshopName { get; init; } = string.Empty;

        public int TotalHours { get; init; }

        public List<KbMaintenanceAnnualSystemGroup> SystemGroups { get; init; } = new();
    }

    public sealed class KbMaintenanceAnnualSystemGroup
    {
        public int SequenceNumber { get; init; }

        public string SystemNodeId { get; init; } = string.Empty;

        public string SystemName { get; init; } = string.Empty;

        public string InventoryNumber { get; init; } = string.Empty;

        public List<KbMaintenanceAnnualDetailRow> DetailRows { get; init; } = new();
    }

    public sealed class KbMaintenanceAnnualDetailRow
    {
        public string OwnerNodeId { get; init; } = string.Empty;

        public string NodeName { get; init; } = string.Empty;

        public string InventoryNumber { get; init; } = string.Empty;

        public int TotalHours { get; init; }

        public List<KbMaintenanceAnnualMonthCell> MonthCells { get; init; } = new();
    }

    public sealed class KbMaintenanceAnnualMonthCell
    {
        public int Month { get; init; }

        public KbMaintenanceWorkKind WorkKind { get; init; }

        public int Hours { get; init; }

        public string PlanText { get; init; } = string.Empty;
    }
}
