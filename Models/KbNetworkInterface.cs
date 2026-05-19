namespace AsutpKnowledgeBase.Models
{
    public class KbNetworkInterface
    {
        public string NetworkInterfaceId { get; set; } = string.Empty;

        public string NetworkDeviceId { get; set; } = string.Empty;

        public string InterfaceName { get; set; } = string.Empty;

        public string PortNumber { get; set; } = string.Empty;

        public string MacAddress { get; set; } = string.Empty;

        public string IpAddress { get; set; } = string.Empty;

        public string SubnetMask { get; set; } = string.Empty;

        public string Gateway { get; set; } = string.Empty;

        public string Vlan { get; set; } = string.Empty;

        public string Protocol { get; set; } = string.Empty;

        public string MpiDpPnAddress { get; set; } = string.Empty;

        public string Speed { get; set; } = string.Empty;

        public string Medium { get; set; } = string.Empty;

        public string Notes { get; set; } = string.Empty;
    }
}
