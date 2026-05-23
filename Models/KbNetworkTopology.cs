namespace AsutpKnowledgeBase.Models
{
    public enum KbNetworkElementKind
    {
        Plc = 0,
        FrequencyConverter = 1,
        [System.Obsolete("Use FrequencyConverter.")]
        Panel = FrequencyConverter,
        Scalance = 2,
        Arm = 3,
        Hmi = 4,
        Server = 5,
        Io = 6,
        Other = 99
    }

    public class KbNetworkTopology
    {
        public List<KbNetworkElement> Elements { get; set; } = new();

        public List<KbNetworkLink> Links { get; set; } = new();
    }

    public class KbNetworkElement
    {
        public string ElementId { get; set; } = string.Empty;

        public KbNetworkElementKind Kind { get; set; }

        public string Name { get; set; } = string.Empty;

        public string IpAddress { get; set; } = string.Empty;

        public int X { get; set; }

        public int Y { get; set; }
    }

    public class KbNetworkLink
    {
        public string LinkId { get; set; } = string.Empty;

        public string FromElementId { get; set; } = string.Empty;

        public string ToElementId { get; set; } = string.Empty;

        public string Label { get; set; } = string.Empty;
    }
}
