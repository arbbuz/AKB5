namespace AsutpKnowledgeBase.Models
{
    public class KbProductionCalendarYear
    {
        public int Year { get; set; }

        public List<DateOnly> AdditionalNonWorkingDays { get; set; } = new();
    }
}
