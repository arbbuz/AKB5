namespace AsutpKnowledgeBase.Models
{
    public class KbMaintenanceYearScheduleEntry
    {
        public int Month { get; set; }

        public KbMaintenanceWorkKind WorkKind { get; set; }

        public int Hours { get; set; }
    }
}
