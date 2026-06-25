namespace AsutpKnowledgeBase.Models
{
    public class KbActDocument
    {
        public string DocumentId { get; set; } = string.Empty;

        public string ActId { get; set; } = string.Empty;

        public int VersionNumber { get; set; } = 1;

        public string TemplateId { get; set; } = string.Empty;

        public string TemplateVersion { get; set; } = string.Empty;

        public string Path { get; set; } = string.Empty;

        public DateTime? GeneratedAt { get; set; }

        public string ContentHash { get; set; } = string.Empty;

        public bool IsLatest { get; set; } = true;
    }
}
