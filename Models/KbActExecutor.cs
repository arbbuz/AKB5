namespace AsutpKnowledgeBase.Models
{
    public class KbActExecutor
    {
        public string ExecutorId { get; set; } = string.Empty;

        public string ActId { get; set; } = string.Empty;

        public int SortOrder { get; set; }

        public string LastName { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;

        public string MiddleName { get; set; } = string.Empty;

        public string Position { get; set; } = string.Empty;
    }
}
