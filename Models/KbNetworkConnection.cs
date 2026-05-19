namespace AsutpKnowledgeBase.Models
{
    public class KbNetworkConnection
    {
        public string NetworkConnectionId { get; set; } = string.Empty;

        public string EndpointAInterfaceId { get; set; } = string.Empty;

        public string EndpointBInterfaceId { get; set; } = string.Empty;

        public string CableLabel { get; set; } = string.Empty;

        public string CableType { get; set; } = string.Empty;

        public string Protocol { get; set; } = string.Empty;

        public string Medium { get; set; } = string.Empty;

        public string Length { get; set; } = string.Empty;

        public string RouteText { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string Notes { get; set; } = string.Empty;
    }
}
