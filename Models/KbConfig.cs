namespace AsutpKnowledgeBase.Models
{
    public class KbConfig
    {
        public const string DefaultActDocumentsDirectoryPath = @"Documents\Acts";

        public int MaxLevels { get; set; }
        public List<string> LevelNames { get; set; } = new List<string>();
        public List<KbProductionCalendarYear> ProductionCalendarYears { get; set; } = new();
        public string ActDocumentsDirectoryPath { get; set; } = DefaultActDocumentsDirectoryPath;
    }
}
