namespace AsutpKnowledgeBase.Models
{
    public sealed class KbCompositionTemplate
    {
        public string TemplateId { get; init; } = string.Empty;

        public string DisplayName { get; init; } = string.Empty;

        public string Description { get; init; } = string.Empty;

        public string SuggestedNodeName { get; init; } = string.Empty;

        public KbNodeType TargetNodeType { get; init; } = KbNodeType.Unknown;

        public IReadOnlyList<KbCompositionTemplateEntry> Entries { get; init; } =
            Array.Empty<KbCompositionTemplateEntry>();
    }

    public sealed class KbCompositionTemplateEntry
    {
        public int? SlotNumber { get; init; }

        public int PositionOrder { get; init; }

        public string ComponentType { get; init; } = string.Empty;

        public string Model { get; init; } = string.Empty;

        public string IpAddress { get; init; } = string.Empty;

        public DateTime? LastCalibrationAt { get; init; }

        public DateTime? NextCalibrationAt { get; init; }

        public string Notes { get; init; } = string.Empty;
    }
}
