namespace AsutpKnowledgeBase.Models
{
    public class KbCompositionEntry
    {
        public string EntryId { get; set; } = string.Empty;

        public string ParentNodeId { get; set; } = string.Empty;

        public int? SlotNumber { get; set; }

        public int PositionOrder { get; set; }

        public string ComponentType { get; set; } = string.Empty;

        public string Model { get; set; } = string.Empty;

        public string IpAddress { get; set; } = string.Empty;

        public DateTime? LastCalibrationAt { get; set; }

        public DateTime? NextCalibrationAt { get; set; }

        public string Notes { get; set; } = string.Empty;
    }
}
