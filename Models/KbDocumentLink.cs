namespace AsutpKnowledgeBase.Models
{
    public class KbDocumentLink
    {
        public string DocumentId { get; set; } = string.Empty;

        public string OwnerNodeId { get; set; } = string.Empty;

        public KbDocumentKind Kind { get; set; } = KbDocumentKind.Manual;

        public string Title { get; set; } = string.Empty;

        public string Path { get; set; } = string.Empty;

        public DateTime? UpdatedAt { get; set; }
    }
}
