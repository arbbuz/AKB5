namespace AsutpKnowledgeBase.Models
{
    public class SavedData
    {
        public const int CurrentSchemaVersion = 1;

        public int SchemaVersion { get; set; } = CurrentSchemaVersion;
        public KbConfig Config { get; set; } = new();
        public Dictionary<string, List<KbNode>> Workshops { get; set; } = new();
        public string LastWorkshop { get; set; } = string.Empty;
    }
}
