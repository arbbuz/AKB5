namespace AsutpKnowledgeBase.Models
{
    public enum KbActInputHistoryField
    {
        ExecutorName = 1,
        ExecutorPosition = 2,
        CustomerName = 3,
        CustomerPosition = 4,
        ApproverName = 5,
        ApproverPosition = 6
    }

    public class KbActInputHistoryEntry
    {
        public string WorkshopName { get; set; } = string.Empty;

        public KbActInputHistoryField Field { get; set; }

        public string DisplayValue { get; set; } = string.Empty;

        public string NormalizedValue { get; set; } = string.Empty;

        public long UseOrder { get; set; }
    }
}
