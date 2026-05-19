using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public sealed class KnowledgeBaseNetworkFileReferenceState
    {
        public string NetworkAssetId { get; init; } = string.Empty;

        public string TitleText { get; init; } = string.Empty;

        public string PathText { get; init; } = string.Empty;

        public string SourceNoteText { get; init; } = string.Empty;

        public KbNetworkPreviewKind PreviewKind { get; init; }

        public string PreviewKindText { get; init; } = string.Empty;

        public bool CanPreviewInForm { get; init; }
    }

    public sealed class KnowledgeBaseNetworkDeviceState
    {
        public string NetworkDeviceId { get; init; } = string.Empty;

        public string OwnerNodeId { get; init; } = string.Empty;

        public string LinkedNodeId { get; init; } = string.Empty;

        public string LinkedNodeText { get; init; } = string.Empty;

        public string NameText { get; init; } = string.Empty;

        public string RoleText { get; init; } = string.Empty;

        public string VendorText { get; init; } = string.Empty;

        public string ModelText { get; init; } = string.Empty;

        public string OrderNumberText { get; init; } = string.Empty;

        public string SerialNumberText { get; init; } = string.Empty;

        public string FirmwareText { get; init; } = string.Empty;

        public string ProfinetNameText { get; init; } = string.Empty;

        public string MacAddressText { get; init; } = string.Empty;

        public string LocationText { get; init; } = string.Empty;

        public string CabinetText { get; init; } = string.Empty;

        public string NotesText { get; init; } = string.Empty;

        public int InterfacesCount { get; init; }

        public int ConnectionsCount { get; init; }
    }

    public sealed class KnowledgeBaseNetworkInterfaceState
    {
        public string NetworkInterfaceId { get; init; } = string.Empty;

        public string NetworkDeviceId { get; init; } = string.Empty;

        public string DeviceNameText { get; init; } = string.Empty;

        public string DeviceRoleText { get; init; } = string.Empty;

        public string InterfaceNameText { get; init; } = string.Empty;

        public string PortNumberText { get; init; } = string.Empty;

        public string MacAddressText { get; init; } = string.Empty;

        public string IpAddressText { get; init; } = string.Empty;

        public string SubnetMaskText { get; init; } = string.Empty;

        public string GatewayText { get; init; } = string.Empty;

        public string VlanText { get; init; } = string.Empty;

        public string ProtocolText { get; init; } = string.Empty;

        public string MpiDpPnAddressText { get; init; } = string.Empty;

        public string SpeedText { get; init; } = string.Empty;

        public string MediumText { get; init; } = string.Empty;

        public string NotesText { get; init; } = string.Empty;

        public int ConnectionsCount { get; init; }
    }

    public sealed class KnowledgeBaseNetworkConnectionState
    {
        public string NetworkConnectionId { get; init; } = string.Empty;

        public string EndpointAInterfaceId { get; init; } = string.Empty;

        public string EndpointBInterfaceId { get; init; } = string.Empty;

        public string EndpointAText { get; init; } = string.Empty;

        public string EndpointBText { get; init; } = string.Empty;

        public string CableLabelText { get; init; } = string.Empty;

        public string CableTypeText { get; init; } = string.Empty;

        public string ProtocolText { get; init; } = string.Empty;

        public string MediumText { get; init; } = string.Empty;

        public string LengthText { get; init; } = string.Empty;

        public string RouteText { get; init; } = string.Empty;

        public string StatusText { get; init; } = string.Empty;

        public string NotesText { get; init; } = string.Empty;
    }

    public sealed class KnowledgeBaseNetworkState
    {
        public bool SupportsEditing { get; init; }

        public bool SupportsPassportEditing { get; init; }

        public string SourceText { get; init; } = string.Empty;

        public string EmptyStateText { get; init; } = string.Empty;

        public int DeviceCount { get; init; }

        public int InterfaceCount { get; init; }

        public int ConnectionCount { get; init; }

        public int FileReferencesCount { get; init; }

        public IReadOnlyList<KnowledgeBaseNetworkDeviceState> DeviceStates { get; init; } =
            Array.Empty<KnowledgeBaseNetworkDeviceState>();

        public IReadOnlyList<KnowledgeBaseNetworkInterfaceState> InterfaceStates { get; init; } =
            Array.Empty<KnowledgeBaseNetworkInterfaceState>();

        public IReadOnlyList<KnowledgeBaseNetworkConnectionState> ConnectionStates { get; init; } =
            Array.Empty<KnowledgeBaseNetworkConnectionState>();

        public IReadOnlyList<KnowledgeBaseNetworkFileReferenceState> FileReferenceStates { get; init; } =
            Array.Empty<KnowledgeBaseNetworkFileReferenceState>();

        public bool HasPassportRows => DeviceCount > 0 || InterfaceCount > 0 || ConnectionCount > 0;

        public bool HasEntries => FileReferencesCount > 0;
    }

    public class KnowledgeBaseNetworkStateService
    {
        public KnowledgeBaseNetworkState Build(
            KbNode? selectedNode,
            IReadOnlyList<KbNetworkFileReference>? networkFileReferences,
            int visibleLevel = 0) =>
            Build(
                selectedNode,
                networkFileReferences,
                networkDevices: null,
                networkInterfaces: null,
                networkConnections: null,
                visibleLevel);

        public KnowledgeBaseNetworkState Build(
            KbNode? selectedNode,
            IReadOnlyList<KbNetworkFileReference>? networkFileReferences,
            IReadOnlyList<KbNetworkDevice>? networkDevices,
            IReadOnlyList<KbNetworkInterface>? networkInterfaces,
            IReadOnlyList<KbNetworkConnection>? networkConnections,
            int visibleLevel = 0)
        {
            if (selectedNode == null || !SupportsRecords(selectedNode.NodeType, visibleLevel))
            {
                return new KnowledgeBaseNetworkState
                {
                    EmptyStateText = "Вкладка \"Сеть\" недоступна для выбранного узла."
                };
            }

            string ownerNodeId = selectedNode.NodeId?.Trim() ?? string.Empty;
            bool supportsPassportRows = visibleLevel == 2;
            var nodeFileReferences = GetOwnedFileReferences(ownerNodeId, networkFileReferences);
            var nodeDevices = supportsPassportRows
                ? GetOwnedDevices(ownerNodeId, networkDevices)
                : new List<KbNetworkDevice>();
            var nodeInterfaces = supportsPassportRows
                ? GetOwnedInterfaces(nodeDevices, networkInterfaces)
                : new List<KbNetworkInterface>();
            var nodeConnections = supportsPassportRows
                ? GetOwnedConnections(nodeInterfaces, networkConnections)
                : new List<KbNetworkConnection>();
            var nodeNamesById = BuildNodeNamesById(selectedNode);

            var deviceStates = BuildDeviceStates(nodeDevices, nodeInterfaces, nodeConnections, nodeNamesById);
            var interfaceStates = BuildInterfaceStates(nodeDevices, nodeInterfaces, nodeConnections);
            var connectionStates = BuildConnectionStates(nodeDevices, nodeInterfaces, nodeConnections);
            var fileReferenceStates = BuildFileReferenceStates(nodeFileReferences);

            return new KnowledgeBaseNetworkState
            {
                SupportsEditing = true,
                SupportsPassportEditing = supportsPassportRows,
                SourceText = supportsPassportRows
                    ? "Показан сетевой паспорт выбранной системы: устройства, интерфейсы/IP, соединения и файлы сети."
                    : "Показаны файлы сетевых схем, адресации и других материалов по сети для этого узла.",
                EmptyStateText = supportsPassportRows
                    ? "Для этой системы пока нет записей сетевого паспорта."
                    : "Для этого узла пока нет файлов сети.",
                DeviceCount = deviceStates.Count,
                InterfaceCount = interfaceStates.Count,
                ConnectionCount = connectionStates.Count,
                FileReferencesCount = fileReferenceStates.Count,
                DeviceStates = deviceStates,
                InterfaceStates = interfaceStates,
                ConnectionStates = connectionStates,
                FileReferenceStates = fileReferenceStates
            };
        }

        public static bool SupportsRecords(KbNodeType nodeType, int visibleLevel = 0) =>
            KnowledgeBaseEngineeringNodeSupportService.SupportsNetworkRecords(nodeType, visibleLevel);

        private static List<KbNetworkFileReference> GetOwnedFileReferences(
            string ownerNodeId,
            IReadOnlyList<KbNetworkFileReference>? networkFileReferences)
        {
            if (string.IsNullOrWhiteSpace(ownerNodeId) || networkFileReferences == null)
                return new List<KbNetworkFileReference>();

            return networkFileReferences
                .Where(reference => string.Equals(reference.OwnerNodeId, ownerNodeId, StringComparison.Ordinal))
                .OrderBy(reference => reference.Title, KnowledgeBaseNaturalStringComparer.Instance)
                .ThenBy(reference => reference.Path, KnowledgeBaseNaturalStringComparer.Instance)
                .ThenBy(reference => reference.NetworkAssetId, StringComparer.Ordinal)
                .ToList();
        }

        private static List<KbNetworkDevice> GetOwnedDevices(
            string ownerNodeId,
            IReadOnlyList<KbNetworkDevice>? networkDevices)
        {
            if (string.IsNullOrWhiteSpace(ownerNodeId) || networkDevices == null)
                return new List<KbNetworkDevice>();

            return networkDevices
                .Where(device => string.Equals(device.OwnerNodeId, ownerNodeId, StringComparison.Ordinal))
                .OrderBy(device => device.Name, KnowledgeBaseNaturalStringComparer.Instance)
                .ThenBy(device => device.Role, KnowledgeBaseNaturalStringComparer.Instance)
                .ThenBy(device => device.NetworkDeviceId, StringComparer.Ordinal)
                .ToList();
        }

        private static List<KbNetworkInterface> GetOwnedInterfaces(
            IReadOnlyList<KbNetworkDevice> ownerDevices,
            IReadOnlyList<KbNetworkInterface>? networkInterfaces)
        {
            if (ownerDevices.Count == 0 || networkInterfaces == null)
                return new List<KbNetworkInterface>();

            var deviceIds = ownerDevices
                .Select(device => device.NetworkDeviceId?.Trim() ?? string.Empty)
                .Where(static deviceId => !string.IsNullOrWhiteSpace(deviceId))
                .ToHashSet(StringComparer.Ordinal);

            return networkInterfaces
                .Where(networkInterface => deviceIds.Contains(networkInterface.NetworkDeviceId?.Trim() ?? string.Empty))
                .OrderBy(networkInterface => GetDeviceSortText(networkInterface.NetworkDeviceId, ownerDevices),
                    KnowledgeBaseNaturalStringComparer.Instance)
                .ThenBy(networkInterface => networkInterface.InterfaceName, KnowledgeBaseNaturalStringComparer.Instance)
                .ThenBy(networkInterface => networkInterface.PortNumber, KnowledgeBaseNaturalStringComparer.Instance)
                .ThenBy(networkInterface => networkInterface.NetworkInterfaceId, StringComparer.Ordinal)
                .ToList();
        }

        private static List<KbNetworkConnection> GetOwnedConnections(
            IReadOnlyList<KbNetworkInterface> ownerInterfaces,
            IReadOnlyList<KbNetworkConnection>? networkConnections)
        {
            if (ownerInterfaces.Count == 0 || networkConnections == null)
                return new List<KbNetworkConnection>();

            var interfaceIds = ownerInterfaces
                .Select(networkInterface => networkInterface.NetworkInterfaceId?.Trim() ?? string.Empty)
                .Where(static interfaceId => !string.IsNullOrWhiteSpace(interfaceId))
                .ToHashSet(StringComparer.Ordinal);

            return networkConnections
                .Where(connection =>
                    interfaceIds.Contains(connection.EndpointAInterfaceId?.Trim() ?? string.Empty) &&
                    interfaceIds.Contains(connection.EndpointBInterfaceId?.Trim() ?? string.Empty))
                .OrderBy(connection => connection.CableLabel, KnowledgeBaseNaturalStringComparer.Instance)
                .ThenBy(connection => connection.EndpointAInterfaceId, StringComparer.Ordinal)
                .ThenBy(connection => connection.EndpointBInterfaceId, StringComparer.Ordinal)
                .ThenBy(connection => connection.NetworkConnectionId, StringComparer.Ordinal)
                .ToList();
        }

        private static List<KnowledgeBaseNetworkDeviceState> BuildDeviceStates(
            IReadOnlyList<KbNetworkDevice> devices,
            IReadOnlyList<KbNetworkInterface> interfaces,
            IReadOnlyList<KbNetworkConnection> connections,
            IReadOnlyDictionary<string, string> nodeNamesById)
        {
            var interfacesByDeviceId = interfaces
                .GroupBy(static networkInterface => networkInterface.NetworkDeviceId, StringComparer.Ordinal)
                .ToDictionary(static group => group.Key, static group => group.ToList(), StringComparer.Ordinal);

            var states = new List<KnowledgeBaseNetworkDeviceState>(devices.Count);
            foreach (var device in devices)
            {
                string deviceId = device.NetworkDeviceId?.Trim() ?? string.Empty;
                interfacesByDeviceId.TryGetValue(deviceId, out var deviceInterfaces);
                deviceInterfaces ??= new List<KbNetworkInterface>();

                var interfaceIds = deviceInterfaces
                    .Select(static networkInterface => networkInterface.NetworkInterfaceId?.Trim() ?? string.Empty)
                    .Where(static interfaceId => !string.IsNullOrWhiteSpace(interfaceId))
                    .ToHashSet(StringComparer.Ordinal);

                states.Add(new KnowledgeBaseNetworkDeviceState
                {
                    NetworkDeviceId = deviceId,
                    OwnerNodeId = device.OwnerNodeId?.Trim() ?? string.Empty,
                    LinkedNodeId = device.LinkedNodeId?.Trim() ?? string.Empty,
                    LinkedNodeText = GetLinkedNodeText(device.LinkedNodeId, nodeNamesById),
                    NameText = GetDisplayTitle(device.Name, device.NetworkDeviceId),
                    RoleText = GetDisplayText(device.Role),
                    VendorText = GetDisplayText(device.Vendor),
                    ModelText = GetDisplayText(device.Model),
                    OrderNumberText = GetDisplayText(device.OrderNumber),
                    SerialNumberText = GetDisplayText(device.SerialNumber),
                    FirmwareText = GetDisplayText(device.Firmware),
                    ProfinetNameText = GetDisplayText(device.ProfinetName),
                    MacAddressText = GetDisplayText(device.MacAddress),
                    LocationText = GetDisplayText(device.LocationText),
                    CabinetText = GetDisplayText(device.CabinetText),
                    NotesText = GetDisplayText(device.Notes),
                    InterfacesCount = deviceInterfaces.Count,
                    ConnectionsCount = connections.Count(connection =>
                        interfaceIds.Contains(connection.EndpointAInterfaceId?.Trim() ?? string.Empty) ||
                        interfaceIds.Contains(connection.EndpointBInterfaceId?.Trim() ?? string.Empty))
                });
            }

            return states;
        }

        private static List<KnowledgeBaseNetworkInterfaceState> BuildInterfaceStates(
            IReadOnlyList<KbNetworkDevice> devices,
            IReadOnlyList<KbNetworkInterface> interfaces,
            IReadOnlyList<KbNetworkConnection> connections)
        {
            var devicesById = devices
                .Where(static device => !string.IsNullOrWhiteSpace(device.NetworkDeviceId))
                .ToDictionary(static device => device.NetworkDeviceId.Trim(), StringComparer.Ordinal);
            var states = new List<KnowledgeBaseNetworkInterfaceState>(interfaces.Count);

            foreach (var networkInterface in interfaces)
            {
                string interfaceId = networkInterface.NetworkInterfaceId?.Trim() ?? string.Empty;
                devicesById.TryGetValue(networkInterface.NetworkDeviceId?.Trim() ?? string.Empty, out var device);

                states.Add(new KnowledgeBaseNetworkInterfaceState
                {
                    NetworkInterfaceId = interfaceId,
                    NetworkDeviceId = networkInterface.NetworkDeviceId?.Trim() ?? string.Empty,
                    DeviceNameText = GetDisplayTitle(device?.Name, networkInterface.NetworkDeviceId),
                    DeviceRoleText = GetDisplayText(device?.Role),
                    InterfaceNameText = GetInterfaceDisplayName(networkInterface),
                    PortNumberText = GetDisplayText(networkInterface.PortNumber),
                    MacAddressText = GetDisplayText(networkInterface.MacAddress),
                    IpAddressText = GetDisplayText(networkInterface.IpAddress),
                    SubnetMaskText = GetDisplayText(networkInterface.SubnetMask),
                    GatewayText = GetDisplayText(networkInterface.Gateway),
                    VlanText = GetDisplayText(networkInterface.Vlan),
                    ProtocolText = GetDisplayText(networkInterface.Protocol),
                    MpiDpPnAddressText = GetDisplayText(networkInterface.MpiDpPnAddress),
                    SpeedText = GetDisplayText(networkInterface.Speed),
                    MediumText = GetDisplayText(networkInterface.Medium),
                    NotesText = GetDisplayText(networkInterface.Notes),
                    ConnectionsCount = connections.Count(connection =>
                        string.Equals(connection.EndpointAInterfaceId, interfaceId, StringComparison.Ordinal) ||
                        string.Equals(connection.EndpointBInterfaceId, interfaceId, StringComparison.Ordinal))
                });
            }

            return states;
        }

        private static List<KnowledgeBaseNetworkConnectionState> BuildConnectionStates(
            IReadOnlyList<KbNetworkDevice> devices,
            IReadOnlyList<KbNetworkInterface> interfaces,
            IReadOnlyList<KbNetworkConnection> connections)
        {
            var devicesById = devices
                .Where(static device => !string.IsNullOrWhiteSpace(device.NetworkDeviceId))
                .ToDictionary(static device => device.NetworkDeviceId.Trim(), StringComparer.Ordinal);
            var interfacesById = interfaces
                .Where(static networkInterface => !string.IsNullOrWhiteSpace(networkInterface.NetworkInterfaceId))
                .ToDictionary(static networkInterface => networkInterface.NetworkInterfaceId.Trim(), StringComparer.Ordinal);

            return connections
                .Select(connection =>
                {
                    interfacesById.TryGetValue(connection.EndpointAInterfaceId?.Trim() ?? string.Empty, out var endpointA);
                    interfacesById.TryGetValue(connection.EndpointBInterfaceId?.Trim() ?? string.Empty, out var endpointB);
                    var endpointADevice = GetDeviceForInterface(endpointA, devicesById);
                    var endpointBDevice = GetDeviceForInterface(endpointB, devicesById);

                    return new KnowledgeBaseNetworkConnectionState
                    {
                        NetworkConnectionId = connection.NetworkConnectionId?.Trim() ?? string.Empty,
                        EndpointAInterfaceId = connection.EndpointAInterfaceId?.Trim() ?? string.Empty,
                        EndpointBInterfaceId = connection.EndpointBInterfaceId?.Trim() ?? string.Empty,
                        EndpointAText = GetEndpointText(endpointA, endpointADevice),
                        EndpointBText = GetEndpointText(endpointB, endpointBDevice),
                        CableLabelText = GetDisplayText(connection.CableLabel),
                        CableTypeText = GetDisplayText(connection.CableType),
                        ProtocolText = GetDisplayText(connection.Protocol),
                        MediumText = GetDisplayText(connection.Medium),
                        LengthText = GetDisplayText(connection.Length),
                        RouteText = GetDisplayText(connection.RouteText),
                        StatusText = GetDisplayText(connection.Status),
                        NotesText = GetDisplayText(connection.Notes)
                    };
                })
                .ToList();
        }

        private static List<KnowledgeBaseNetworkFileReferenceState> BuildFileReferenceStates(
            IEnumerable<KbNetworkFileReference> references) =>
            references.Select(reference => new KnowledgeBaseNetworkFileReferenceState
            {
                NetworkAssetId = reference.NetworkAssetId,
                TitleText = GetDisplayTitle(reference.Title, reference.Path),
                PathText = GetDisplayText(reference.Path),
                SourceNoteText = GetDisplayText(reference.SourceNote),
                PreviewKind = reference.PreviewKind,
                PreviewKindText = KnowledgeBaseNetworkPreviewService.GetPreviewKindText(reference.PreviewKind),
                CanPreviewInForm = KnowledgeBaseNetworkPreviewService.CanPreviewInForm(reference.PreviewKind)
            })
                .ToList();

        private static Dictionary<string, string> BuildNodeNamesById(KbNode selectedNode)
        {
            var nodeNamesById = new Dictionary<string, string>(StringComparer.Ordinal);
            CollectNodeNames(selectedNode, nodeNamesById);
            return nodeNamesById;
        }

        private static void CollectNodeNames(KbNode node, IDictionary<string, string> nodeNamesById)
        {
            string nodeId = node.NodeId?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(nodeId) && !nodeNamesById.ContainsKey(nodeId))
                nodeNamesById.Add(nodeId, node.Name?.Trim() ?? string.Empty);

            foreach (var child in node.Children ?? new List<KbNode>())
                CollectNodeNames(child, nodeNamesById);
        }

        private static string GetDeviceSortText(
            string? networkDeviceId,
            IEnumerable<KbNetworkDevice> devices)
        {
            string normalizedDeviceId = networkDeviceId?.Trim() ?? string.Empty;
            var device = devices.FirstOrDefault(item =>
                string.Equals(item.NetworkDeviceId, normalizedDeviceId, StringComparison.Ordinal));

            return device?.Name?.Trim() ?? normalizedDeviceId;
        }

        private static KbNetworkDevice? GetDeviceForInterface(
            KbNetworkInterface? networkInterface,
            IReadOnlyDictionary<string, KbNetworkDevice> devicesById)
        {
            if (networkInterface == null)
                return null;

            devicesById.TryGetValue(networkInterface.NetworkDeviceId?.Trim() ?? string.Empty, out var device);
            return device;
        }

        private static string GetLinkedNodeText(
            string? linkedNodeId,
            IReadOnlyDictionary<string, string> nodeNamesById)
        {
            string normalizedLinkedNodeId = linkedNodeId?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalizedLinkedNodeId))
                return "-";

            if (nodeNamesById.TryGetValue(normalizedLinkedNodeId, out string? nodeName) &&
                !string.IsNullOrWhiteSpace(nodeName))
            {
                return nodeName.Trim();
            }

            return normalizedLinkedNodeId;
        }

        private static string GetEndpointText(KbNetworkInterface? networkInterface, KbNetworkDevice? device)
        {
            if (networkInterface == null)
                return "-";

            string deviceName = device?.Name?.Trim() ?? string.Empty;
            string interfaceName = GetInterfaceDisplayName(networkInterface);
            string ipAddress = networkInterface.IpAddress?.Trim() ?? string.Empty;

            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(deviceName))
                parts.Add(deviceName);
            if (!string.IsNullOrWhiteSpace(interfaceName) && !string.Equals(interfaceName, "-", StringComparison.Ordinal))
                parts.Add(interfaceName);
            if (!string.IsNullOrWhiteSpace(ipAddress))
                parts.Add(ipAddress);

            if (parts.Count > 0)
                return string.Join(" / ", parts);

            return GetDisplayText(networkInterface.NetworkInterfaceId);
        }

        private static string GetInterfaceDisplayName(KbNetworkInterface networkInterface)
        {
            string interfaceName = networkInterface.InterfaceName?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(interfaceName))
                return interfaceName;

            string portNumber = networkInterface.PortNumber?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(portNumber))
                return $"Порт {portNumber}";

            return GetDisplayText(networkInterface.NetworkInterfaceId);
        }

        private static string GetDisplayTitle(string? title, string? fallback)
        {
            string normalizedTitle = title?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(normalizedTitle))
                return normalizedTitle;

            string normalizedFallback = fallback?.Trim() ?? string.Empty;
            return string.IsNullOrWhiteSpace(normalizedFallback)
                ? "(без названия)"
                : normalizedFallback;
        }

        private static string GetDisplayText(string? value) =>
            string.IsNullOrWhiteSpace(value)
                ? "-"
                : value.Trim();
    }
}
