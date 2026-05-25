namespace AsutpKnowledgeBase.Models
{
    public class KbMaintenanceMonthPlanAssignment
    {
        public DateOnly Date { get; set; }

        public string OwnerNodeId { get; set; } = string.Empty;

        public string NodeName { get; set; } = string.Empty;

        public string SystemNodeId { get; set; } = string.Empty;

        public int SystemLevel3NodeCount { get; set; }

        public KbMaintenanceWorkKind WorkKind { get; set; } = KbMaintenanceWorkKind.To1;

        public int Hours { get; set; }
    }
}
