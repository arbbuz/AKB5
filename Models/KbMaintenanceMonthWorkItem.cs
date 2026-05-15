namespace AsutpKnowledgeBase.Models
{
    public class KbMaintenanceMonthWorkItem
    {
        public string OwnerNodeId { get; set; } = string.Empty;

        public string NodeName { get; set; } = string.Empty;

        public string SystemNodeId { get; set; } = string.Empty;

        public int SystemPreorderIndex { get; set; } = int.MaxValue;

        public int OwnerPreorderIndex { get; set; } = int.MaxValue;

        public KbMaintenanceWorkKind WorkKind { get; set; } = KbMaintenanceWorkKind.To1;

        public int Hours { get; set; }
    }
}
