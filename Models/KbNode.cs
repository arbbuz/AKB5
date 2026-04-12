namespace AsutpKnowledgeBase.Models
{
    public class KbNode
    {
        public string Name { get; set; } = string.Empty;

        public int LevelIndex { get; set; }

        public KbNodeDetails Details { get; set; } = new();

        public List<KbNode> Children { get; set; } = new();
    }
}
