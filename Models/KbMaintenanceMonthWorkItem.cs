namespace AsutpKnowledgeBase.Models
{
    public class KbMaintenanceMonthWorkItem
    {
        public string OwnerNodeId { get; set; } = string.Empty;

        public string NodeName { get; set; } = string.Empty;

        public KbMaintenanceWorkKind WorkKind { get; set; } = KbMaintenanceWorkKind.To1;

        public int Hours { get; set; }
    }
}
