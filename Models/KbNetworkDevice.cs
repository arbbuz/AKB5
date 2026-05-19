namespace AsutpKnowledgeBase.Models
{
    public class KbNetworkDevice
    {
        public string NetworkDeviceId { get; set; } = string.Empty;

        public string OwnerNodeId { get; set; } = string.Empty;

        public string LinkedNodeId { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

        public string Vendor { get; set; } = string.Empty;

        public string Model { get; set; } = string.Empty;

        public string OrderNumber { get; set; } = string.Empty;

        public string SerialNumber { get; set; } = string.Empty;

        public string Firmware { get; set; } = string.Empty;

        public string ProfinetName { get; set; } = string.Empty;

        public string MacAddress { get; set; } = string.Empty;

        public string LocationText { get; set; } = string.Empty;

        public string CabinetText { get; set; } = string.Empty;

        public string Notes { get; set; } = string.Empty;
    }
}
