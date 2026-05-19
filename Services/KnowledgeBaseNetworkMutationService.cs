using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public sealed class KnowledgeBaseNetworkFileReferenceMutationResult
    {
        public bool IsSuccess { get; init; }

        public string ErrorMessage { get; init; } = string.Empty;

        public List<KbNetworkFileReference> NetworkFileReferences { get; init; } = new();
    }

    public sealed class KnowledgeBaseNetworkPassportMutationResult
    {
        public bool IsSuccess { get; init; }

        public string ErrorMessage { get; init; } = string.Empty;

        public List<KbNetworkDevice> NetworkDevices { get; init; } = new();

        public List<KbNetworkInterface> NetworkInterfaces { get; init; } = new();

        public List<KbNetworkConnection> NetworkConnections { get; init; } = new();
    }

    public class KnowledgeBaseNetworkMutationService
    {
        public KnowledgeBaseNetworkFileReferenceMutationResult UpsertNetworkFileReference(
            KbNode? ownerNode,
            IReadOnlyList<KbNetworkFileReference>? networkFileReferences,
            KbNetworkFileReference? draftReference,
            int visibleLevel = 0)
        {
            if (!TryValidateOwnerNode(ownerNode, visibleLevel, out var ownerNodeId, out var errorMessage))
                return FailureFileReference(errorMessage);

            if (draftReference == null)
                return FailureFileReference("Черновик файла сети не был передан.");

            string title = draftReference.Title?.Trim() ?? string.Empty;
            string path = draftReference.Path?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(title))
                return FailureFileReference("Укажите наименование файла сети.");

            if (string.IsNullOrWhiteSpace(path))
                return FailureFileReference("Укажите путь или ссылку на файл сети.");

            var updatedReferences = CloneNetworkFileReferences(networkFileReferences);
            int existingIndex = !string.IsNullOrWhiteSpace(draftReference.NetworkAssetId)
                ? updatedReferences.FindIndex(reference =>
                    string.Equals(reference.NetworkAssetId, draftReference.NetworkAssetId, StringComparison.Ordinal))
                : -1;

            if (existingIndex >= 0 &&
                !string.Equals(updatedReferences[existingIndex].OwnerNodeId, ownerNodeId, StringComparison.Ordinal))
            {
                return FailureFileReference("Нельзя перенести файл сети на другой узел через редактирование.");
            }

            var normalizedDraft = new KbNetworkFileReference
            {
                NetworkAssetId = draftReference.NetworkAssetId?.Trim() ?? string.Empty,
                OwnerNodeId = ownerNodeId,
                Title = title,
                Path = path,
                PreviewKind = KnowledgeBaseNetworkPreviewService.ResolvePreviewKind(path)
            };

            if (existingIndex >= 0)
                updatedReferences[existingIndex] = normalizedDraft;
            else
                updatedReferences.Add(normalizedDraft);

            return SuccessFileReference(updatedReferences);
        }

        public KnowledgeBaseNetworkFileReferenceMutationResult DeleteNetworkFileReference(
            KbNode? ownerNode,
            IReadOnlyList<KbNetworkFileReference>? networkFileReferences,
            string? networkAssetId,
            int visibleLevel = 0)
        {
            if (!TryValidateOwnerNode(ownerNode, visibleLevel, out var ownerNodeId, out var errorMessage))
                return FailureFileReference(errorMessage);

            string normalizedNetworkAssetId = networkAssetId?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalizedNetworkAssetId))
                return FailureFileReference("Файл сети не выбран.");

            var updatedReferences = CloneNetworkFileReferences(networkFileReferences);
            int removedCount = updatedReferences.RemoveAll(reference =>
                string.Equals(reference.NetworkAssetId, normalizedNetworkAssetId, StringComparison.Ordinal) &&
                string.Equals(reference.OwnerNodeId, ownerNodeId, StringComparison.Ordinal));

            return removedCount == 0
                ? FailureFileReference("Не удалось найти выбранный файл сети.")
                : SuccessFileReference(updatedReferences);
        }

        public KnowledgeBaseNetworkPassportMutationResult UpsertDevice(
            KbNode? ownerNode,
            IReadOnlyList<KbNetworkDevice>? networkDevices,
            IReadOnlyList<KbNetworkInterface>? networkInterfaces,
            IReadOnlyList<KbNetworkConnection>? networkConnections,
            KbNetworkDevice? draftDevice,
            int visibleLevel = 0)
        {
            if (!TryValidatePassportOwnerNode(ownerNode, visibleLevel, out var ownerNodeId, out var errorMessage))
                return FailurePassport(errorMessage);

            if (draftDevice == null)
                return FailurePassport("Черновик сетевого устройства не был передан.");

            string name = draftDevice.Name?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name))
                return FailurePassport("Укажите наименование сетевого устройства.");

            if (!TryNormalizeLinkedNodeId(ownerNode!, draftDevice.LinkedNodeId, out var linkedNodeId, out errorMessage))
                return FailurePassport(errorMessage);

            var updatedDevices = CloneNetworkDevices(networkDevices);
            var currentInterfaces = CloneNetworkInterfaces(networkInterfaces);
            var currentConnections = CloneNetworkConnections(networkConnections);
            int existingIndex = !string.IsNullOrWhiteSpace(draftDevice.NetworkDeviceId)
                ? updatedDevices.FindIndex(device =>
                    string.Equals(device.NetworkDeviceId, draftDevice.NetworkDeviceId, StringComparison.Ordinal))
                : -1;

            if (existingIndex >= 0 &&
                !string.Equals(updatedDevices[existingIndex].OwnerNodeId, ownerNodeId, StringComparison.Ordinal))
            {
                return FailurePassport("Нельзя перенести сетевое устройство в другую систему через редактирование.");
            }

            var normalizedDraft = new KbNetworkDevice
            {
                NetworkDeviceId = draftDevice.NetworkDeviceId?.Trim() ?? string.Empty,
                OwnerNodeId = ownerNodeId,
                LinkedNodeId = linkedNodeId,
                Name = name,
                Role = draftDevice.Role?.Trim() ?? string.Empty,
                Vendor = draftDevice.Vendor?.Trim() ?? string.Empty,
                Model = draftDevice.Model?.Trim() ?? string.Empty,
                OrderNumber = draftDevice.OrderNumber?.Trim() ?? string.Empty,
                SerialNumber = draftDevice.SerialNumber?.Trim() ?? string.Empty,
                Firmware = draftDevice.Firmware?.Trim() ?? string.Empty,
                ProfinetName = draftDevice.ProfinetName?.Trim() ?? string.Empty,
                MacAddress = draftDevice.MacAddress?.Trim() ?? string.Empty,
                LocationText = draftDevice.LocationText?.Trim() ?? string.Empty,
                CabinetText = draftDevice.CabinetText?.Trim() ?? string.Empty,
                Notes = draftDevice.Notes?.Trim() ?? string.Empty
            };

            if (existingIndex >= 0)
                updatedDevices[existingIndex] = normalizedDraft;
            else
                updatedDevices.Add(normalizedDraft);

            return SuccessPassport(updatedDevices, currentInterfaces, currentConnections);
        }

        public KnowledgeBaseNetworkPassportMutationResult DeleteDevice(
            KbNode? ownerNode,
            IReadOnlyList<KbNetworkDevice>? networkDevices,
            IReadOnlyList<KbNetworkInterface>? networkInterfaces,
            IReadOnlyList<KbNetworkConnection>? networkConnections,
            string? networkDeviceId,
            int visibleLevel = 0)
        {
            if (!TryValidatePassportOwnerNode(ownerNode, visibleLevel, out var ownerNodeId, out var errorMessage))
                return FailurePassport(errorMessage);

            string normalizedDeviceId = networkDeviceId?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalizedDeviceId))
                return FailurePassport("Сетевое устройство не выбрано.");

            var updatedDevices = CloneNetworkDevices(networkDevices);
            int removedCount = updatedDevices.RemoveAll(device =>
                string.Equals(device.NetworkDeviceId, normalizedDeviceId, StringComparison.Ordinal) &&
                string.Equals(device.OwnerNodeId, ownerNodeId, StringComparison.Ordinal));
            if (removedCount == 0)
                return FailurePassport("Не удалось найти выбранное сетевое устройство.");

            var updatedInterfaces = CloneNetworkInterfaces(networkInterfaces);
            var removedInterfaceIds = updatedInterfaces
                .Where(networkInterface =>
                    string.Equals(networkInterface.NetworkDeviceId, normalizedDeviceId, StringComparison.Ordinal))
                .Select(networkInterface => networkInterface.NetworkInterfaceId)
                .ToHashSet(StringComparer.Ordinal);
            updatedInterfaces.RemoveAll(networkInterface =>
                string.Equals(networkInterface.NetworkDeviceId, normalizedDeviceId, StringComparison.Ordinal));

            var updatedConnections = CloneNetworkConnections(networkConnections);
            updatedConnections.RemoveAll(connection =>
                removedInterfaceIds.Contains(connection.EndpointAInterfaceId) ||
                removedInterfaceIds.Contains(connection.EndpointBInterfaceId));

            return SuccessPassport(updatedDevices, updatedInterfaces, updatedConnections);
        }

        public KnowledgeBaseNetworkPassportMutationResult UpsertInterface(
            KbNode? ownerNode,
            IReadOnlyList<KbNetworkDevice>? networkDevices,
            IReadOnlyList<KbNetworkInterface>? networkInterfaces,
            IReadOnlyList<KbNetworkConnection>? networkConnections,
            KbNetworkInterface? draftInterface,
            int visibleLevel = 0)
        {
            if (!TryValidatePassportOwnerNode(ownerNode, visibleLevel, out var ownerNodeId, out var errorMessage))
                return FailurePassport(errorMessage);

            if (draftInterface == null)
                return FailurePassport("Черновик сетевого интерфейса не был передан.");

            var currentDevices = CloneNetworkDevices(networkDevices);
            string deviceId = draftInterface.NetworkDeviceId?.Trim() ?? string.Empty;
            if (!TryFindOwnedDevice(currentDevices, deviceId, ownerNodeId, out _))
                return FailurePassport("Выберите сетевое устройство этой системы для интерфейса.");

            string interfaceName = draftInterface.InterfaceName?.Trim() ?? string.Empty;
            string portNumber = draftInterface.PortNumber?.Trim() ?? string.Empty;
            string ipAddress = draftInterface.IpAddress?.Trim() ?? string.Empty;
            string macAddress = draftInterface.MacAddress?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(interfaceName) &&
                string.IsNullOrWhiteSpace(portNumber) &&
                string.IsNullOrWhiteSpace(ipAddress) &&
                string.IsNullOrWhiteSpace(macAddress))
            {
                return FailurePassport("Укажите имя интерфейса, порт, IP-адрес или MAC-адрес.");
            }

            var updatedInterfaces = CloneNetworkInterfaces(networkInterfaces);
            var currentConnections = CloneNetworkConnections(networkConnections);
            int existingIndex = !string.IsNullOrWhiteSpace(draftInterface.NetworkInterfaceId)
                ? updatedInterfaces.FindIndex(networkInterface =>
                    string.Equals(
                        networkInterface.NetworkInterfaceId,
                        draftInterface.NetworkInterfaceId,
                        StringComparison.Ordinal))
                : -1;

            if (existingIndex >= 0 &&
                !IsInterfaceOwnedByOwner(updatedInterfaces[existingIndex], ownerNodeId, currentDevices))
            {
                return FailurePassport("Нельзя перенести сетевой интерфейс из другой системы через редактирование.");
            }

            var normalizedDraft = new KbNetworkInterface
            {
                NetworkInterfaceId = draftInterface.NetworkInterfaceId?.Trim() ?? string.Empty,
                NetworkDeviceId = deviceId,
                InterfaceName = interfaceName,
                PortNumber = portNumber,
                MacAddress = macAddress,
                IpAddress = ipAddress,
                SubnetMask = draftInterface.SubnetMask?.Trim() ?? string.Empty,
                Gateway = draftInterface.Gateway?.Trim() ?? string.Empty,
                Vlan = draftInterface.Vlan?.Trim() ?? string.Empty,
                Protocol = draftInterface.Protocol?.Trim() ?? string.Empty,
                MpiDpPnAddress = draftInterface.MpiDpPnAddress?.Trim() ?? string.Empty,
                Speed = draftInterface.Speed?.Trim() ?? string.Empty,
                Medium = draftInterface.Medium?.Trim() ?? string.Empty,
                Notes = draftInterface.Notes?.Trim() ?? string.Empty
            };

            if (existingIndex >= 0)
                updatedInterfaces[existingIndex] = normalizedDraft;
            else
                updatedInterfaces.Add(normalizedDraft);

            return SuccessPassport(currentDevices, updatedInterfaces, currentConnections);
        }

        public KnowledgeBaseNetworkPassportMutationResult DeleteInterface(
            KbNode? ownerNode,
            IReadOnlyList<KbNetworkDevice>? networkDevices,
            IReadOnlyList<KbNetworkInterface>? networkInterfaces,
            IReadOnlyList<KbNetworkConnection>? networkConnections,
            string? networkInterfaceId,
            int visibleLevel = 0)
        {
            if (!TryValidatePassportOwnerNode(ownerNode, visibleLevel, out var ownerNodeId, out var errorMessage))
                return FailurePassport(errorMessage);

            string normalizedInterfaceId = networkInterfaceId?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalizedInterfaceId))
                return FailurePassport("Сетевой интерфейс не выбран.");

            var currentDevices = CloneNetworkDevices(networkDevices);
            var updatedInterfaces = CloneNetworkInterfaces(networkInterfaces);
            int removedCount = updatedInterfaces.RemoveAll(networkInterface =>
                string.Equals(networkInterface.NetworkInterfaceId, normalizedInterfaceId, StringComparison.Ordinal) &&
                IsInterfaceOwnedByOwner(networkInterface, ownerNodeId, currentDevices));
            if (removedCount == 0)
                return FailurePassport("Не удалось найти выбранный сетевой интерфейс.");

            var updatedConnections = CloneNetworkConnections(networkConnections);
            updatedConnections.RemoveAll(connection =>
                string.Equals(connection.EndpointAInterfaceId, normalizedInterfaceId, StringComparison.Ordinal) ||
                string.Equals(connection.EndpointBInterfaceId, normalizedInterfaceId, StringComparison.Ordinal));

            return SuccessPassport(currentDevices, updatedInterfaces, updatedConnections);
        }

        public KnowledgeBaseNetworkPassportMutationResult UpsertConnection(
            KbNode? ownerNode,
            IReadOnlyList<KbNetworkDevice>? networkDevices,
            IReadOnlyList<KbNetworkInterface>? networkInterfaces,
            IReadOnlyList<KbNetworkConnection>? networkConnections,
            KbNetworkConnection? draftConnection,
            int visibleLevel = 0)
        {
            if (!TryValidatePassportOwnerNode(ownerNode, visibleLevel, out var ownerNodeId, out var errorMessage))
                return FailurePassport(errorMessage);

            if (draftConnection == null)
                return FailurePassport("Черновик сетевого соединения не был передан.");

            string endpointAInterfaceId = draftConnection.EndpointAInterfaceId?.Trim() ?? string.Empty;
            string endpointBInterfaceId = draftConnection.EndpointBInterfaceId?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(endpointAInterfaceId) ||
                string.IsNullOrWhiteSpace(endpointBInterfaceId))
            {
                return FailurePassport("Выберите оба интерфейса сетевого соединения.");
            }

            if (string.Equals(endpointAInterfaceId, endpointBInterfaceId, StringComparison.Ordinal))
                return FailurePassport("Интерфейсы соединения должны быть разными.");

            var currentDevices = CloneNetworkDevices(networkDevices);
            var currentInterfaces = CloneNetworkInterfaces(networkInterfaces);
            if (!TryFindOwnedInterface(currentInterfaces, currentDevices, endpointAInterfaceId, ownerNodeId, out _) ||
                !TryFindOwnedInterface(currentInterfaces, currentDevices, endpointBInterfaceId, ownerNodeId, out _))
            {
                return FailurePassport("Оба интерфейса соединения должны принадлежать выбранной системе.");
            }

            var updatedConnections = CloneNetworkConnections(networkConnections);
            string connectionId = draftConnection.NetworkConnectionId?.Trim() ?? string.Empty;
            int existingIndex = !string.IsNullOrWhiteSpace(connectionId)
                ? updatedConnections.FindIndex(connection =>
                    string.Equals(connection.NetworkConnectionId, connectionId, StringComparison.Ordinal))
                : -1;

            if (existingIndex >= 0 &&
                !IsConnectionOwnedByOwner(updatedConnections[existingIndex], ownerNodeId, currentInterfaces, currentDevices))
            {
                return FailurePassport("Нельзя перенести сетевое соединение из другой системы через редактирование.");
            }

            if (HasDuplicateConnectionPair(
                updatedConnections,
                connectionId,
                endpointAInterfaceId,
                endpointBInterfaceId))
            {
                return FailurePassport("Такое соединение между интерфейсами уже существует.");
            }

            var normalizedDraft = new KbNetworkConnection
            {
                NetworkConnectionId = connectionId,
                EndpointAInterfaceId = endpointAInterfaceId,
                EndpointBInterfaceId = endpointBInterfaceId,
                CableLabel = draftConnection.CableLabel?.Trim() ?? string.Empty,
                CableType = draftConnection.CableType?.Trim() ?? string.Empty,
                Length = draftConnection.Length?.Trim() ?? string.Empty,
                Status = draftConnection.Status?.Trim() ?? string.Empty,
                Notes = draftConnection.Notes?.Trim() ?? string.Empty
            };

            if (existingIndex >= 0)
                updatedConnections[existingIndex] = normalizedDraft;
            else
                updatedConnections.Add(normalizedDraft);

            return SuccessPassport(currentDevices, currentInterfaces, updatedConnections);
        }

        public KnowledgeBaseNetworkPassportMutationResult DeleteConnection(
            KbNode? ownerNode,
            IReadOnlyList<KbNetworkDevice>? networkDevices,
            IReadOnlyList<KbNetworkInterface>? networkInterfaces,
            IReadOnlyList<KbNetworkConnection>? networkConnections,
            string? networkConnectionId,
            int visibleLevel = 0)
        {
            if (!TryValidatePassportOwnerNode(ownerNode, visibleLevel, out var ownerNodeId, out var errorMessage))
                return FailurePassport(errorMessage);

            string normalizedConnectionId = networkConnectionId?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalizedConnectionId))
                return FailurePassport("Сетевое соединение не выбрано.");

            var currentDevices = CloneNetworkDevices(networkDevices);
            var currentInterfaces = CloneNetworkInterfaces(networkInterfaces);
            var updatedConnections = CloneNetworkConnections(networkConnections);
            int removedCount = updatedConnections.RemoveAll(connection =>
                string.Equals(connection.NetworkConnectionId, normalizedConnectionId, StringComparison.Ordinal) &&
                IsConnectionOwnedByOwner(connection, ownerNodeId, currentInterfaces, currentDevices));

            return removedCount == 0
                ? FailurePassport("Не удалось найти выбранное сетевое соединение.")
                : SuccessPassport(currentDevices, currentInterfaces, updatedConnections);
        }

        private static bool TryValidateOwnerNode(
            KbNode? ownerNode,
            int visibleLevel,
            out string ownerNodeId,
            out string errorMessage)
        {
            if (ownerNode == null)
            {
                ownerNodeId = string.Empty;
                errorMessage = "Не выбран узел для редактирования сетевого паспорта.";
                return false;
            }

            if (!KnowledgeBaseNetworkStateService.SupportsRecords(ownerNode.NodeType, visibleLevel))
            {
                ownerNodeId = string.Empty;
                errorMessage = "Для выбранного узла вкладка \"Сеть\" недоступна.";
                return false;
            }

            ownerNodeId = ownerNode.NodeId?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(ownerNodeId))
            {
                errorMessage = "У выбранного узла отсутствует NodeId.";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        private static bool TryValidatePassportOwnerNode(
            KbNode? ownerNode,
            int visibleLevel,
            out string ownerNodeId,
            out string errorMessage)
        {
            if (!TryValidateOwnerNode(ownerNode, visibleLevel, out ownerNodeId, out errorMessage))
                return false;

            if (visibleLevel == 2)
                return true;

            ownerNodeId = string.Empty;
            errorMessage = "Сетевой паспорт доступен только для узла уровня 2.";
            return false;
        }

        private static bool TryNormalizeLinkedNodeId(
            KbNode ownerNode,
            string? linkedNodeId,
            out string normalizedLinkedNodeId,
            out string errorMessage)
        {
            normalizedLinkedNodeId = linkedNodeId?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalizedLinkedNodeId) ||
                ContainsNodeId(ownerNode, normalizedLinkedNodeId))
            {
                errorMessage = string.Empty;
                return true;
            }

            errorMessage = "Связанная карточка устройства должна находиться внутри выбранной системы.";
            return false;
        }

        private static bool ContainsNodeId(KbNode node, string nodeId)
        {
            if (string.Equals(node.NodeId?.Trim(), nodeId, StringComparison.Ordinal))
                return true;

            foreach (var child in node.Children ?? new List<KbNode>())
            {
                if (ContainsNodeId(child, nodeId))
                    return true;
            }

            return false;
        }

        private static bool TryFindOwnedDevice(
            IEnumerable<KbNetworkDevice> devices,
            string networkDeviceId,
            string ownerNodeId,
            out KbNetworkDevice device)
        {
            device = devices.FirstOrDefault(item =>
                string.Equals(item.NetworkDeviceId, networkDeviceId, StringComparison.Ordinal) &&
                string.Equals(item.OwnerNodeId, ownerNodeId, StringComparison.Ordinal))!;

            return device != null;
        }

        private static bool TryFindOwnedInterface(
            IEnumerable<KbNetworkInterface> interfaces,
            IReadOnlyList<KbNetworkDevice> devices,
            string networkInterfaceId,
            string ownerNodeId,
            out KbNetworkInterface networkInterface)
        {
            networkInterface = interfaces.FirstOrDefault(item =>
                string.Equals(item.NetworkInterfaceId, networkInterfaceId, StringComparison.Ordinal) &&
                IsInterfaceOwnedByOwner(item, ownerNodeId, devices))!;

            return networkInterface != null;
        }

        private static bool IsInterfaceOwnedByOwner(
            KbNetworkInterface networkInterface,
            string ownerNodeId,
            IReadOnlyList<KbNetworkDevice> devices) =>
            devices.Any(device =>
                string.Equals(device.NetworkDeviceId, networkInterface.NetworkDeviceId, StringComparison.Ordinal) &&
                string.Equals(device.OwnerNodeId, ownerNodeId, StringComparison.Ordinal));

        private static bool IsConnectionOwnedByOwner(
            KbNetworkConnection connection,
            string ownerNodeId,
            IReadOnlyList<KbNetworkInterface> interfaces,
            IReadOnlyList<KbNetworkDevice> devices) =>
            TryFindOwnedInterface(
                interfaces,
                devices,
                connection.EndpointAInterfaceId?.Trim() ?? string.Empty,
                ownerNodeId,
                out _) &&
            TryFindOwnedInterface(
                interfaces,
                devices,
                connection.EndpointBInterfaceId?.Trim() ?? string.Empty,
                ownerNodeId,
                out _);

        private static bool HasDuplicateConnectionPair(
            IReadOnlyList<KbNetworkConnection> connections,
            string currentConnectionId,
            string endpointAInterfaceId,
            string endpointBInterfaceId) =>
            connections.Any(connection =>
                !string.Equals(connection.NetworkConnectionId, currentConnectionId, StringComparison.Ordinal) &&
                IsSameConnectionPair(
                    connection.EndpointAInterfaceId,
                    connection.EndpointBInterfaceId,
                    endpointAInterfaceId,
                    endpointBInterfaceId));

        private static bool IsSameConnectionPair(
            string? leftA,
            string? leftB,
            string rightA,
            string rightB)
        {
            string normalizedLeftA = leftA?.Trim() ?? string.Empty;
            string normalizedLeftB = leftB?.Trim() ?? string.Empty;
            return
                string.Equals(normalizedLeftA, rightA, StringComparison.Ordinal) &&
                string.Equals(normalizedLeftB, rightB, StringComparison.Ordinal) ||
                string.Equals(normalizedLeftA, rightB, StringComparison.Ordinal) &&
                string.Equals(normalizedLeftB, rightA, StringComparison.Ordinal);
        }

        private static List<KbNetworkFileReference> CloneNetworkFileReferences(
            IReadOnlyList<KbNetworkFileReference>? networkFileReferences)
        {
            var clones = new List<KbNetworkFileReference>();
            if (networkFileReferences == null)
                return clones;

            foreach (var reference in networkFileReferences)
            {
                clones.Add(new KbNetworkFileReference
                {
                    NetworkAssetId = reference.NetworkAssetId,
                    OwnerNodeId = reference.OwnerNodeId,
                    Title = reference.Title,
                    Path = reference.Path,
                    PreviewKind = reference.PreviewKind
                });
            }

            return clones;
        }

        private static List<KbNetworkDevice> CloneNetworkDevices(IReadOnlyList<KbNetworkDevice>? networkDevices)
        {
            var clones = new List<KbNetworkDevice>();
            if (networkDevices == null)
                return clones;

            foreach (var device in networkDevices)
            {
                clones.Add(new KbNetworkDevice
                {
                    NetworkDeviceId = device.NetworkDeviceId,
                    OwnerNodeId = device.OwnerNodeId,
                    LinkedNodeId = device.LinkedNodeId,
                    Name = device.Name,
                    Role = device.Role,
                    Vendor = device.Vendor,
                    Model = device.Model,
                    OrderNumber = device.OrderNumber,
                    SerialNumber = device.SerialNumber,
                    Firmware = device.Firmware,
                    ProfinetName = device.ProfinetName,
                    MacAddress = device.MacAddress,
                    LocationText = device.LocationText,
                    CabinetText = device.CabinetText,
                    Notes = device.Notes
                });
            }

            return clones;
        }

        private static List<KbNetworkInterface> CloneNetworkInterfaces(
            IReadOnlyList<KbNetworkInterface>? networkInterfaces)
        {
            var clones = new List<KbNetworkInterface>();
            if (networkInterfaces == null)
                return clones;

            foreach (var networkInterface in networkInterfaces)
            {
                clones.Add(new KbNetworkInterface
                {
                    NetworkInterfaceId = networkInterface.NetworkInterfaceId,
                    NetworkDeviceId = networkInterface.NetworkDeviceId,
                    InterfaceName = networkInterface.InterfaceName,
                    PortNumber = networkInterface.PortNumber,
                    MacAddress = networkInterface.MacAddress,
                    IpAddress = networkInterface.IpAddress,
                    SubnetMask = networkInterface.SubnetMask,
                    Gateway = networkInterface.Gateway,
                    Vlan = networkInterface.Vlan,
                    Protocol = networkInterface.Protocol,
                    MpiDpPnAddress = networkInterface.MpiDpPnAddress,
                    Speed = networkInterface.Speed,
                    Medium = networkInterface.Medium,
                    Notes = networkInterface.Notes
                });
            }

            return clones;
        }

        private static List<KbNetworkConnection> CloneNetworkConnections(
            IReadOnlyList<KbNetworkConnection>? networkConnections)
        {
            var clones = new List<KbNetworkConnection>();
            if (networkConnections == null)
                return clones;

            foreach (var connection in networkConnections)
            {
                clones.Add(new KbNetworkConnection
                {
                    NetworkConnectionId = connection.NetworkConnectionId,
                    EndpointAInterfaceId = connection.EndpointAInterfaceId,
                    EndpointBInterfaceId = connection.EndpointBInterfaceId,
                    CableLabel = connection.CableLabel,
                    CableType = connection.CableType,
                    Length = connection.Length,
                    Status = connection.Status,
                    Notes = connection.Notes
                });
            }

            return clones;
        }

        private static KnowledgeBaseNetworkFileReferenceMutationResult SuccessFileReference(
            List<KbNetworkFileReference> networkFileReferences) =>
            new()
            {
                IsSuccess = true,
                NetworkFileReferences = networkFileReferences
            };

        private static KnowledgeBaseNetworkPassportMutationResult SuccessPassport(
            List<KbNetworkDevice> networkDevices,
            List<KbNetworkInterface> networkInterfaces,
            List<KbNetworkConnection> networkConnections) =>
            new()
            {
                IsSuccess = true,
                NetworkDevices = networkDevices,
                NetworkInterfaces = networkInterfaces,
                NetworkConnections = networkConnections
            };

        private static KnowledgeBaseNetworkFileReferenceMutationResult FailureFileReference(string errorMessage) =>
            new()
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };

        private static KnowledgeBaseNetworkPassportMutationResult FailurePassport(string errorMessage) =>
            new()
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };
    }
}
