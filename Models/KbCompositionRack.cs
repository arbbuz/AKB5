namespace AsutpKnowledgeBase.Models
{
    public class KbCompositionRack
    {
        public string RackId { get; set; } = string.Empty;

        public string ParentNodeId { get; set; } = string.Empty;

        public int RackNumber { get; set; }

        public int SortOrder { get; set; }

        public string RackType { get; set; } = "UR";

        public string Label { get; set; } = string.Empty;

        public string Notes { get; set; } = string.Empty;

        public List<KbCompositionRackProperty> Properties { get; set; } = new();
    }

    public class KbCompositionRackProperty
    {
        public string Name { get; set; } = string.Empty;

        public string Value { get; set; } = string.Empty;
    }
}
