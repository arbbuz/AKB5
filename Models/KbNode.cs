namespace AsutpKnowledgeBase.Models
{
    public class KbNode
    {
        public string NodeId { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public int LevelIndex { get; set; }

        public KbNodeType NodeType { get; set; } = KbNodeType.Unknown;

        public KbNodeDetails Details { get; set; } = new();

        public List<KbNode> Children { get; set; } = new();
    }
}
