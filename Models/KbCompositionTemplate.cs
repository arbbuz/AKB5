namespace AsutpKnowledgeBase.Models
{
    public sealed class KbCompositionTemplate
    {
        public string TemplateId { get; init; } = string.Empty;

        public string DisplayName { get; init; } = string.Empty;

        public string Description { get; init; } = string.Empty;

        public string SuggestedNodeName { get; init; } = string.Empty;

        public KbNodeType TargetNodeType { get; init; } = KbNodeType.Unknown;

        public IReadOnlyList<KbCompositionTemplateRack> Racks { get; init; } =
            Array.Empty<KbCompositionTemplateRack>();

        public IReadOnlyList<KbCompositionTemplateEntry> Entries { get; init; } =
            Array.Empty<KbCompositionTemplateEntry>();
    }

    public sealed class KbCompositionTemplateRack
    {
        public int RackNumber { get; init; }

        public int SortOrder { get; init; }

        public string RackType { get; init; } = "UR";

        public string Label { get; init; } = string.Empty;

        public string NetworkLink { get; init; } = string.Empty;

        public string Notes { get; init; } = string.Empty;
    }

    public sealed class KbCompositionTemplateEntry
    {
        public int RackNumber { get; init; }

        public int? SlotNumber { get; init; }

        public int PositionOrder { get; init; }

        public string ComponentType { get; init; } = string.Empty;

        public string Model { get; init; } = string.Empty;

        public string OrderNumber { get; init; } = string.Empty;

        public string Firmware { get; init; } = string.Empty;

        public string MpiDpPnAddress { get; init; } = string.Empty;

        public string InputAddress { get; init; } = string.Empty;

        public string OutputAddress { get; init; } = string.Empty;

        public string Comment { get; init; } = string.Empty;

        public string InterfaceRows { get; init; } = string.Empty;

        public string IpAddress { get; init; } = string.Empty;

        public DateTime? LastCalibrationAt { get; init; }

        public DateTime? NextCalibrationAt { get; init; }

        public string Notes { get; init; } = string.Empty;
    }
}
