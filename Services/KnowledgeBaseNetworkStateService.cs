using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public sealed class KnowledgeBaseNetworkObjectState
    {
        public string NodeId { get; init; } = string.Empty;

        public string NameText { get; init; } = string.Empty;

        public string TypeText { get; init; } = string.Empty;

        public string ParentText { get; init; } = string.Empty;

        public string ParentNodeId { get; init; } = string.Empty;

        public int ChildCount { get; init; }
    }

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

        public string WarningText { get; init; } = string.Empty;

        public int InterfacesCount { get; init; }

        public int ConnectionsCount { get; init; }
    }

    public sealed class KnowledgeBaseNetworkInterfaceState
    {
        public string NetworkInterfaceId { get; init; } = string.Empty;

        public string NetworkDeviceId { get; init; } = string.Empty;

        public string DeviceNameText { get; init; } = string.Empty;

        public string DeviceRoleText { get; init; } = string.Empty;

        public string EndpointText { get; init; } = string.Empty;

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

        public string WarningText { get; init; } = string.Empty;

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

        public string WarningText { get; init; } = string.Empty;
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

        public int ObjectCount { get; init; }

        public int DeviceWarningCount => CountWarningRows(DeviceStates.Select(static device => device.WarningText));

        public int InterfaceWarningCount =>
            CountWarningRows(InterfaceStates.Select(static networkInterface => networkInterface.WarningText));

        public int ConnectionWarningCount =>
            CountWarningRows(ConnectionStates.Select(static connection => connection.WarningText));

        public int ReviewWarningCount => DeviceWarningCount + InterfaceWarningCount + ConnectionWarningCount;

        public IReadOnlyList<KnowledgeBaseNetworkObjectState> ObjectStates { get; init; } =
            Array.Empty<KnowledgeBaseNetworkObjectState>();

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

        private static int CountWarningRows(IEnumerable<string> warnings) =>
            warnings.Count(static warning =>
                !string.IsNullOrWhiteSpace(warning) &&
                !string.Equals(warning, "-", StringComparison.Ordinal));
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
            var objectStates = BuildObjectStates(selectedNode);

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
                ObjectCount = objectStates.Count,
                ObjectStates = objectStates,
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
            var duplicateProfinetNames = BuildDuplicateKeys(devices, static device => device.ProfinetName);
            var duplicateMacAddresses = BuildDuplicateKeys(devices, static device => device.MacAddress);

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
                    WarningText = BuildWarningText(
                        duplicateProfinetNames.Contains(NormalizeReviewKey(device.ProfinetName))
                            ? "Повтор PROFINET-name"
                            : string.Empty,
                        duplicateMacAddresses.Contains(NormalizeReviewKey(device.MacAddress))
                            ? "Повтор MAC"
                            : string.Empty),
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
            var duplicateIpAddresses = BuildDuplicateKeys(interfaces, static networkInterface => networkInterface.IpAddress);
            var duplicateMacAddresses = BuildDuplicateKeys(interfaces, static networkInterface => networkInterface.MacAddress);
            var duplicateInterfaceNames = BuildDuplicateInterfaceNames(interfaces);
            var states = new List<KnowledgeBaseNetworkInterfaceState>(interfaces.Count);

            foreach (var networkInterface in interfaces)
            {
                string interfaceId = networkInterface.NetworkInterfaceId?.Trim() ?? string.Empty;
                string interfaceNameKey = BuildInterfaceNameKey(networkInterface);
                devicesById.TryGetValue(networkInterface.NetworkDeviceId?.Trim() ?? string.Empty, out var device);

                states.Add(new KnowledgeBaseNetworkInterfaceState
                {
                    NetworkInterfaceId = interfaceId,
                    NetworkDeviceId = networkInterface.NetworkDeviceId?.Trim() ?? string.Empty,
                    DeviceNameText = GetDisplayTitle(device?.Name, networkInterface.NetworkDeviceId),
                    DeviceRoleText = GetDisplayText(device?.Role),
                    EndpointText = GetEndpointText(networkInterface, device),
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
                    WarningText = BuildWarningText(
                        duplicateIpAddresses.Contains(NormalizeReviewKey(networkInterface.IpAddress))
                            ? "Повтор IP"
                            : string.Empty,
                        duplicateMacAddresses.Contains(NormalizeReviewKey(networkInterface.MacAddress))
                            ? "Повтор MAC"
                            : string.Empty,
                        duplicateInterfaceNames.Contains(interfaceNameKey)
                            ? "Повтор порта"
                            : string.Empty),
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
            var duplicateCableLabels = BuildDuplicateKeys(connections, static connection => connection.CableLabel);

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
                        NotesText = GetDisplayText(connection.Notes),
                        WarningText = BuildWarningText(
                            duplicateCableLabels.Contains(NormalizeReviewKey(connection.CableLabel))
                                ? "Повтор кабеля"
                                : string.Empty)
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

        private static List<KnowledgeBaseNetworkObjectState> BuildObjectStates(KbNode selectedNode)
        {
            var objectStates = new List<KnowledgeBaseNetworkObjectState>();
            CollectObjectStates(selectedNode, parentName: string.Empty, parentNodeId: string.Empty, objectStates);
            return objectStates;
        }

        private static void CollectObjectStates(
            KbNode node,
            string parentName,
            string parentNodeId,
            ICollection<KnowledgeBaseNetworkObjectState> objectStates)
        {
            if (objectStates.Count >= 80)
                return;

            string nodeId = node.NodeId?.Trim() ?? string.Empty;
            string nodeName = GetDisplayTitle(node.Name, nodeId);
            objectStates.Add(new KnowledgeBaseNetworkObjectState
            {
                NodeId = nodeId,
                NameText = nodeName,
                TypeText = GetNodeTypeText(node),
                ParentText = string.IsNullOrWhiteSpace(parentName) ? "-" : parentName,
                ParentNodeId = parentNodeId,
                ChildCount = node.Children?.Count ?? 0
            });

            foreach (var child in node.Children ?? new List<KbNode>())
                CollectObjectStates(child, nodeName, nodeId, objectStates);
        }

        private static string GetNodeTypeText(KbNode node) =>
            node.NodeType switch
            {
                KbNodeType.WorkshopRoot => "Цех",
                KbNodeType.Department => "Отделение",
                KbNodeType.System => "Система",
                KbNodeType.Cabinet => "Шкаф / узел",
                KbNodeType.Device => "Устройство",
                KbNodeType.Controller => "Контроллер",
                KbNodeType.Module => "Модуль",
                KbNodeType.DocumentNode => "Документ",
                _ => node.LevelIndex > 0 ? $"Уровень {node.LevelIndex}" : "Объект"
            };

        private static HashSet<string> BuildDuplicateKeys<T>(
            IEnumerable<T> items,
            Func<T, string?> keySelector)
        {
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in items)
            {
                string key = NormalizeReviewKey(keySelector(item));
                if (string.IsNullOrWhiteSpace(key))
                    continue;

                counts[key] = counts.TryGetValue(key, out int count)
                    ? count + 1
                    : 1;
            }

            return counts
                .Where(static pair => pair.Value > 1)
                .Select(static pair => pair.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        private static HashSet<string> BuildDuplicateInterfaceNames(IEnumerable<KbNetworkInterface> interfaces) =>
            interfaces
                .Select(BuildInterfaceNameKey)
                .Where(static key => !string.IsNullOrWhiteSpace(key))
                .GroupBy(static key => key, StringComparer.OrdinalIgnoreCase)
                .Where(static group => group.Count() > 1)
                .Select(static group => group.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

        private static string BuildInterfaceNameKey(KbNetworkInterface networkInterface)
        {
            string deviceId = NormalizeReviewKey(networkInterface.NetworkDeviceId);
            string portOrName = NormalizeReviewKey(networkInterface.PortNumber);
            if (string.IsNullOrWhiteSpace(portOrName))
                portOrName = NormalizeReviewKey(networkInterface.InterfaceName);

            if (string.IsNullOrWhiteSpace(deviceId) || string.IsNullOrWhiteSpace(portOrName))
                return string.Empty;

            return deviceId + "\u001F" + portOrName;
        }

        private static string BuildWarningText(params string[] warnings)
        {
            var visibleWarnings = warnings
                .Where(static warning => !string.IsNullOrWhiteSpace(warning))
                .ToArray();
            return visibleWarnings.Length == 0
                ? "-"
                : string.Join("; ", visibleWarnings);
        }

        private static string NormalizeReviewKey(string? value) =>
            value?.Trim() ?? string.Empty;

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
            string mpiDpPnAddress = networkInterface.MpiDpPnAddress?.Trim() ?? string.Empty;

            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(deviceName))
                parts.Add(deviceName);
            if (!string.IsNullOrWhiteSpace(interfaceName) && !string.Equals(interfaceName, "-", StringComparison.Ordinal))
                parts.Add(interfaceName);
            if (!string.IsNullOrWhiteSpace(ipAddress))
                parts.Add($"IP {ipAddress}");
            if (!string.IsNullOrWhiteSpace(mpiDpPnAddress))
                parts.Add($"MPI/DP/PN {mpiDpPnAddress}");

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
