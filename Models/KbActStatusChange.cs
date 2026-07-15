namespace AsutpKnowledgeBase.Models
{
    public class KbActStatusChange
    {
        public string ChangeId { get; set; } = string.Empty;

        public KbActStatus PreviousStatus { get; set; } = KbActStatus.Draft;

        public KbActStatus NewStatus { get; set; } = KbActStatus.Draft;

        public DateTime ChangedAt { get; set; }

        public string ChangedBy { get; set; } = string.Empty;
    }
}
