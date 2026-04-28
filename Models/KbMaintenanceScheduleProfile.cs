namespace AsutpKnowledgeBase.Models
{
    public class KbMaintenanceScheduleProfile
    {
        public string MaintenanceProfileId { get; set; } = string.Empty;

        public string OwnerNodeId { get; set; } = string.Empty;

        public bool IsIncludedInSchedule { get; set; }

        public int To1Hours { get; set; }

        public int To2Hours { get; set; }

        public int To3Hours { get; set; }
    }
}
