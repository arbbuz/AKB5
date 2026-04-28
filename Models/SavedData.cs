namespace AsutpKnowledgeBase.Models
{
    public class SavedData
    {
        public const int CurrentSchemaVersion = 3;

        public int SchemaVersion { get; set; } = CurrentSchemaVersion;
        public KbConfig Config { get; set; } = new();
        public Dictionary<string, List<KbNode>> Workshops { get; set; } = new();
        public List<KbCompositionEntry> CompositionEntries { get; set; } = new();
        public List<KbDocumentLink> DocumentLinks { get; set; } = new();
        public List<KbSoftwareRecord> SoftwareRecords { get; set; } = new();
        public List<KbNetworkFileReference> NetworkFileReferences { get; set; } = new();
        public string LastWorkshop { get; set; } = string.Empty;
    }
}
