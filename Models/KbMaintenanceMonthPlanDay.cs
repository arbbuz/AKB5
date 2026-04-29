namespace AsutpKnowledgeBase.Models
{
    public class KbMaintenanceMonthPlanDay
    {
        public DateOnly Date { get; set; }

        public int TotalHours { get; set; }

        public List<KbMaintenanceMonthPlanAssignment> Assignments { get; set; } = new();
    }
}
