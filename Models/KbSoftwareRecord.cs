namespace AsutpKnowledgeBase.Models
{
    public class KbSoftwareRecord
    {
        public string SoftwareId { get; set; } = string.Empty;

        public string OwnerNodeId { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Path { get; set; } = string.Empty;

        public DateTime? AddedAt { get; set; }

        public DateTime? LastChangedAt { get; set; }

        public DateTime? LastBackupAt { get; set; }

        public string Notes { get; set; } = string.Empty;
    }
}
