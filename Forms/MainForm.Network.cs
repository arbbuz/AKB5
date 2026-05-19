using System.Diagnostics;
using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase
{
    public partial class MainForm
    {
        private void AddNetworkDevice(object? sender, EventArgs e)
        {
            if (!TryGetNetworkPassportOwnerNode(out var ownerNode))
                return;

            EditNetworkDeviceCore(
                ownerNode,
                new KbNetworkDevice
                {
                    OwnerNodeId = ownerNode.NodeId
                },
                "Добавить сетевое устройство",
                "Сетевое устройство добавлено.");
        }

        private void EditSelectedNetworkDevice(object? sender, EventArgs e)
        {
            if (!TryGetNetworkPassportOwnerNode(out var ownerNode))
                return;

            var device = FindSelectedNetworkDevice(ownerNode);
            if (device == null)
            {
                MessageBox.Show(
                    this,
                    "Выберите сетевое устройство для изменения.",
                    "Сеть",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            EditNetworkDeviceCore(
                ownerNode,
                CloneNetworkDevice(device),
                "Изменить сетевое устройство",
                "Сетевое устройство обновлено.");
        }

        private void DeleteSelectedNetworkDevice(object? sender, EventArgs e)
        {
            if (!TryGetNetworkPassportOwnerNode(out var ownerNode))
                return;

            var device = FindSelectedNetworkDevice(ownerNode);
            if (device == null)
            {
                MessageBox.Show(
                    this,
                    "Выберите сетевое устройство для удаления.",
                    "Сеть",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            var confirmDelete = MessageBox.Show(
                this,
                $"Удалить сетевое устройство \"{device.Name}\" вместе с его интерфейсами и соединениями?",
                "Сеть",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Warning);
            if (confirmDelete != DialogResult.OK)
                return;

            ApplyNetworkPassportMutation(
                _networkMutationService.DeleteDevice(
                    ownerNode,
                    _session.NetworkDevices,
                    _session.NetworkInterfaces,
                    _session.NetworkConnections,
                    device.NetworkDeviceId,
                    GetVisibleLevelForNode(ownerNode)),
                "Сетевое устройство удалено.");
        }

        private void AddNetworkInterface(object? sender, EventArgs e)
        {
            if (!TryGetNetworkPassportOwnerNode(out var ownerNode))
                return;

            var devices = GetOwnedNetworkDevices(ownerNode);
            if (devices.Count == 0)
            {
                MessageBox.Show(
                    this,
                    "Сначала добавьте сетевое устройство.",
                    "Сеть",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            string selectedDeviceId = selectedNodeNetworkScreen.SelectedDeviceId;
            string initialDeviceId = devices.Any(device =>
                string.Equals(device.NetworkDeviceId, selectedDeviceId, StringComparison.Ordinal))
                ? selectedDeviceId
                : devices[0].NetworkDeviceId;

            EditNetworkInterfaceCore(
                ownerNode,
                devices,
                new KbNetworkInterface
                {
                    NetworkDeviceId = initialDeviceId
                },
                "Добавить сетевой интерфейс",
                "Сетевой интерфейс добавлен.");
        }

        private void EditSelectedNetworkInterface(object? sender, EventArgs e)
        {
            if (!TryGetNetworkPassportOwnerNode(out var ownerNode))
                return;

            var networkInterface = FindSelectedNetworkInterface(ownerNode);
            if (networkInterface == null)
            {
                MessageBox.Show(
                    this,
                    "Выберите сетевой интерфейс для изменения.",
                    "Сеть",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            EditNetworkInterfaceCore(
                ownerNode,
                GetOwnedNetworkDevices(ownerNode),
                CloneNetworkInterface(networkInterface),
                "Изменить сетевой интерфейс",
                "Сетевой интерфейс обновлен.");
        }

        private void DeleteSelectedNetworkInterface(object? sender, EventArgs e)
        {
            if (!TryGetNetworkPassportOwnerNode(out var ownerNode))
                return;

            var networkInterface = FindSelectedNetworkInterface(ownerNode);
            if (networkInterface == null)
            {
                MessageBox.Show(
                    this,
                    "Выберите сетевой интерфейс для удаления.",
                    "Сеть",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            var confirmDelete = MessageBox.Show(
                this,
                $"Удалить сетевой интерфейс \"{FormatNetworkInterfaceTitle(networkInterface)}\" вместе с его соединениями?",
                "Сеть",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Warning);
            if (confirmDelete != DialogResult.OK)
                return;

            ApplyNetworkPassportMutation(
                _networkMutationService.DeleteInterface(
                    ownerNode,
                    _session.NetworkDevices,
                    _session.NetworkInterfaces,
                    _session.NetworkConnections,
                    networkInterface.NetworkInterfaceId,
                    GetVisibleLevelForNode(ownerNode)),
                "Сетевой интерфейс удален.");
        }

        private void AddNetworkConnection(object? sender, EventArgs e)
        {
            if (!TryGetNetworkPassportOwnerNode(out var ownerNode))
                return;

            var interfaces = GetOwnedNetworkInterfaces(ownerNode);
            if (interfaces.Count < 2)
            {
                MessageBox.Show(
                    this,
                    "Для соединения нужны минимум два интерфейса.",
                    "Сеть",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            EditNetworkConnectionCore(
                ownerNode,
                GetOwnedNetworkDevices(ownerNode),
                interfaces,
                new KbNetworkConnection
                {
                    EndpointAInterfaceId = interfaces[0].NetworkInterfaceId,
                    EndpointBInterfaceId = interfaces[1].NetworkInterfaceId
                },
                "Добавить сетевое соединение",
                "Сетевое соединение добавлено.");
        }

        private void EditSelectedNetworkConnection(object? sender, EventArgs e)
        {
            if (!TryGetNetworkPassportOwnerNode(out var ownerNode))
                return;

            var connection = FindSelectedNetworkConnection(ownerNode);
            if (connection == null)
            {
                MessageBox.Show(
                    this,
                    "Выберите сетевое соединение для изменения.",
                    "Сеть",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            EditNetworkConnectionCore(
                ownerNode,
                GetOwnedNetworkDevices(ownerNode),
                GetOwnedNetworkInterfaces(ownerNode),
                CloneNetworkConnection(connection),
                "Изменить сетевое соединение",
                "Сетевое соединение обновлено.");
        }

        private void DeleteSelectedNetworkConnection(object? sender, EventArgs e)
        {
            if (!TryGetNetworkPassportOwnerNode(out var ownerNode))
                return;

            var connection = FindSelectedNetworkConnection(ownerNode);
            if (connection == null)
            {
                MessageBox.Show(
                    this,
                    "Выберите сетевое соединение для удаления.",
                    "Сеть",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            var confirmDelete = MessageBox.Show(
                this,
                $"Удалить сетевое соединение \"{connection.CableLabel}\"?",
                "Сеть",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Warning);
            if (confirmDelete != DialogResult.OK)
                return;

            ApplyNetworkPassportMutation(
                _networkMutationService.DeleteConnection(
                    ownerNode,
                    _session.NetworkDevices,
                    _session.NetworkInterfaces,
                    _session.NetworkConnections,
                    connection.NetworkConnectionId,
                    GetVisibleLevelForNode(ownerNode)),
                "Сетевое соединение удалено.");
        }

        private void EditNetworkDeviceCore(
            KbNode ownerNode,
            KbNetworkDevice draftDevice,
            string dialogTitle,
            string successStatusText)
        {
            using var dialog = new KnowledgeBaseNetworkDeviceDialog(dialogTitle, ownerNode, draftDevice);
            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            ApplyNetworkPassportMutation(
                _networkMutationService.UpsertDevice(
                    ownerNode,
                    _session.NetworkDevices,
                    _session.NetworkInterfaces,
                    _session.NetworkConnections,
                    dialog.Result,
                    GetVisibleLevelForNode(ownerNode)),
                successStatusText);
        }

        private void EditNetworkInterfaceCore(
            KbNode ownerNode,
            IReadOnlyList<KbNetworkDevice> devices,
            KbNetworkInterface draftInterface,
            string dialogTitle,
            string successStatusText)
        {
            using var dialog = new KnowledgeBaseNetworkInterfaceDialog(dialogTitle, devices, draftInterface);
            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            ApplyNetworkPassportMutation(
                _networkMutationService.UpsertInterface(
                    ownerNode,
                    _session.NetworkDevices,
                    _session.NetworkInterfaces,
                    _session.NetworkConnections,
                    dialog.Result,
                    GetVisibleLevelForNode(ownerNode)),
                successStatusText);
        }

        private void EditNetworkConnectionCore(
            KbNode ownerNode,
            IReadOnlyList<KbNetworkDevice> devices,
            IReadOnlyList<KbNetworkInterface> interfaces,
            KbNetworkConnection draftConnection,
            string dialogTitle,
            string successStatusText)
        {
            using var dialog = new KnowledgeBaseNetworkConnectionDialog(dialogTitle, devices, interfaces, draftConnection);
            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            ApplyNetworkPassportMutation(
                _networkMutationService.UpsertConnection(
                    ownerNode,
                    _session.NetworkDevices,
                    _session.NetworkInterfaces,
                    _session.NetworkConnections,
                    dialog.Result,
                    GetVisibleLevelForNode(ownerNode)),
                successStatusText);
        }

        private void ApplyNetworkPassportMutation(
            KnowledgeBaseNetworkPassportMutationResult result,
            string successStatusText)
        {
            if (!result.IsSuccess)
            {
                MessageBox.Show(
                    this,
                    result.ErrorMessage,
                    "Сеть",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            _session.ReplaceNetworkDevices(result.NetworkDevices);
            _session.ReplaceNetworkInterfaces(result.NetworkInterfaces);
            _session.ReplaceNetworkConnections(result.NetworkConnections);
            UpdateDirtyState();
            UpdateUI();
            SetLastActionText(successStatusText);
        }

        private void AddNetworkFileReference(object? sender, EventArgs e)
        {
            if (!TryGetNetworkOwnerNode(out var ownerNode))
                return;

            EditNetworkFileReferenceCore(
                ownerNode,
                new KbNetworkFileReference
                {
                    OwnerNodeId = ownerNode.NodeId
                },
                "Добавить файл сети",
                "Файл сети добавлен.");
        }

        private void OpenSelectedNetworkFileReference(object? sender, EventArgs e)
        {
            if (!TryGetNetworkOwnerNode(out var ownerNode))
                return;

            var networkFileReference = FindSelectedNetworkFileReference(ownerNode);
            if (networkFileReference == null || string.IsNullOrWhiteSpace(networkFileReference.Path))
            {
                MessageBox.Show(
                    this,
                    "Сначала выберите файл сети с заполненным путем.",
                    "Сеть",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = networkFileReference.Path.Trim(),
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    $"Не удалось открыть файл сети: {ex.Message}",
                    "Сеть",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void EditSelectedNetworkFileReference(object? sender, EventArgs e)
        {
            if (!TryGetNetworkOwnerNode(out var ownerNode))
                return;

            var networkFileReference = FindSelectedNetworkFileReference(ownerNode);
            if (networkFileReference == null)
            {
                MessageBox.Show(
                    this,
                    "Выберите файл сети для изменения.",
                    "Сеть",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            EditNetworkFileReferenceCore(
                ownerNode,
                CloneNetworkFileReference(networkFileReference),
                "Изменить файл сети",
                "Файл сети обновлен.");
        }

        private void DeleteSelectedNetworkFileReference(object? sender, EventArgs e)
        {
            if (!TryGetNetworkOwnerNode(out var ownerNode))
                return;

            var networkFileReference = FindSelectedNetworkFileReference(ownerNode);
            if (networkFileReference == null)
            {
                MessageBox.Show(
                    this,
                    "Выберите файл сети для удаления.",
                    "Сеть",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            var confirmDelete = MessageBox.Show(
                this,
                $"Удалить файл сети \"{networkFileReference.Title}\"?",
                "Сеть",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Warning);
            if (confirmDelete != DialogResult.OK)
                return;

            ApplyNetworkFileReferenceMutation(
                _networkMutationService.DeleteNetworkFileReference(
                    ownerNode,
                    _session.NetworkFileReferences,
                    networkFileReference.NetworkAssetId,
                    GetVisibleLevelForNode(ownerNode)),
                "Файл сети удален.");
        }

        private void EditNetworkFileReferenceCore(
            KbNode ownerNode,
            KbNetworkFileReference draftReference,
            string dialogTitle,
            string successStatusText)
        {
            using var dialog = new KnowledgeBaseNetworkFileReferenceDialog(dialogTitle, draftReference);
            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            ApplyNetworkFileReferenceMutation(
                _networkMutationService.UpsertNetworkFileReference(
                    ownerNode,
                    _session.NetworkFileReferences,
                    dialog.Result,
                    GetVisibleLevelForNode(ownerNode)),
                successStatusText);
        }

        private void ApplyNetworkFileReferenceMutation(
            KnowledgeBaseNetworkFileReferenceMutationResult result,
            string successStatusText)
        {
            if (!result.IsSuccess)
            {
                MessageBox.Show(
                    this,
                    result.ErrorMessage,
                    "Сеть",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            _session.ReplaceNetworkFileReferences(result.NetworkFileReferences);
            UpdateDirtyState();
            UpdateUI();
            SetLastActionText(successStatusText);
        }

        private bool TryGetNetworkOwnerNode(out KbNode ownerNode)
        {
            ownerNode = new KbNode();
            if (TryGetSelectedTreeNode(out KbNode selectedNode) &&
                KnowledgeBaseNetworkStateService.SupportsRecords(
                    selectedNode.NodeType,
                    GetVisibleLevelForNode(selectedNode)))
            {
                ownerNode = selectedNode;
                return true;
            }

            MessageBox.Show(
                this,
                "Вкладка \"Сеть\" доступна только для инженерных узлов.",
                "Сеть",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return false;
        }

        private bool TryGetNetworkPassportOwnerNode(out KbNode ownerNode)
        {
            ownerNode = new KbNode();
            if (TryGetSelectedTreeNode(out KbNode selectedNode) &&
                KnowledgeBaseNetworkStateService.SupportsRecords(
                    selectedNode.NodeType,
                    GetVisibleLevelForNode(selectedNode)) &&
                GetVisibleLevelForNode(selectedNode) == 2)
            {
                ownerNode = selectedNode;
                return true;
            }

            MessageBox.Show(
                this,
                "Сетевой паспорт доступен только для системы уровня 2.",
                "Сеть",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return false;
        }

        private KbNetworkFileReference? FindSelectedNetworkFileReference(KbNode ownerNode)
        {
            string selectedItemId = selectedNodeNetworkScreen.SelectedItemId;
            if (string.IsNullOrWhiteSpace(selectedItemId))
                return null;

            return _session.NetworkFileReferences.FirstOrDefault(reference =>
                string.Equals(reference.NetworkAssetId, selectedItemId, StringComparison.Ordinal) &&
                string.Equals(reference.OwnerNodeId, ownerNode.NodeId, StringComparison.Ordinal));
        }

        private KbNetworkDevice? FindSelectedNetworkDevice(KbNode ownerNode)
        {
            string selectedDeviceId = selectedNodeNetworkScreen.SelectedDeviceId;
            if (string.IsNullOrWhiteSpace(selectedDeviceId))
                return null;

            return _session.NetworkDevices.FirstOrDefault(device =>
                string.Equals(device.NetworkDeviceId, selectedDeviceId, StringComparison.Ordinal) &&
                string.Equals(device.OwnerNodeId, ownerNode.NodeId, StringComparison.Ordinal));
        }

        private KbNetworkInterface? FindSelectedNetworkInterface(KbNode ownerNode)
        {
            string selectedInterfaceId = selectedNodeNetworkScreen.SelectedInterfaceId;
            if (string.IsNullOrWhiteSpace(selectedInterfaceId))
                return null;

            var ownedDeviceIds = GetOwnedNetworkDeviceIds(ownerNode);
            return _session.NetworkInterfaces.FirstOrDefault(networkInterface =>
                string.Equals(networkInterface.NetworkInterfaceId, selectedInterfaceId, StringComparison.Ordinal) &&
                ownedDeviceIds.Contains(networkInterface.NetworkDeviceId));
        }

        private KbNetworkConnection? FindSelectedNetworkConnection(KbNode ownerNode)
        {
            string selectedConnectionId = selectedNodeNetworkScreen.SelectedConnectionId;
            if (string.IsNullOrWhiteSpace(selectedConnectionId))
                return null;

            var ownedInterfaceIds = GetOwnedNetworkInterfaceIds(ownerNode);
            return _session.NetworkConnections.FirstOrDefault(connection =>
                string.Equals(connection.NetworkConnectionId, selectedConnectionId, StringComparison.Ordinal) &&
                ownedInterfaceIds.Contains(connection.EndpointAInterfaceId) &&
                ownedInterfaceIds.Contains(connection.EndpointBInterfaceId));
        }

        private List<KbNetworkDevice> GetOwnedNetworkDevices(KbNode ownerNode) =>
            _session.NetworkDevices
                .Where(device => string.Equals(device.OwnerNodeId, ownerNode.NodeId, StringComparison.Ordinal))
                .ToList();

        private HashSet<string> GetOwnedNetworkDeviceIds(KbNode ownerNode) =>
            GetOwnedNetworkDevices(ownerNode)
                .Select(device => device.NetworkDeviceId)
                .ToHashSet(StringComparer.Ordinal);

        private List<KbNetworkInterface> GetOwnedNetworkInterfaces(KbNode ownerNode)
        {
            var ownedDeviceIds = GetOwnedNetworkDeviceIds(ownerNode);
            return _session.NetworkInterfaces
                .Where(networkInterface => ownedDeviceIds.Contains(networkInterface.NetworkDeviceId))
                .ToList();
        }

        private HashSet<string> GetOwnedNetworkInterfaceIds(KbNode ownerNode) =>
            GetOwnedNetworkInterfaces(ownerNode)
                .Select(networkInterface => networkInterface.NetworkInterfaceId)
                .ToHashSet(StringComparer.Ordinal);

        private static KbNetworkFileReference CloneNetworkFileReference(KbNetworkFileReference reference) =>
            new()
            {
                NetworkAssetId = reference.NetworkAssetId,
                OwnerNodeId = reference.OwnerNodeId,
                Title = reference.Title,
                Path = reference.Path,
                PreviewKind = reference.PreviewKind
            };

        private static KbNetworkDevice CloneNetworkDevice(KbNetworkDevice device) =>
            new()
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
            };

        private static KbNetworkInterface CloneNetworkInterface(KbNetworkInterface networkInterface) =>
            new()
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
            };

        private static KbNetworkConnection CloneNetworkConnection(KbNetworkConnection connection) =>
            new()
            {
                NetworkConnectionId = connection.NetworkConnectionId,
                EndpointAInterfaceId = connection.EndpointAInterfaceId,
                EndpointBInterfaceId = connection.EndpointBInterfaceId,
                CableLabel = connection.CableLabel,
                CableType = connection.CableType,
                Length = connection.Length,
                Status = connection.Status,
                Notes = connection.Notes
            };

        private static string FormatNetworkInterfaceTitle(KbNetworkInterface networkInterface)
        {
            if (!string.IsNullOrWhiteSpace(networkInterface.InterfaceName))
                return networkInterface.InterfaceName.Trim();

            if (!string.IsNullOrWhiteSpace(networkInterface.PortNumber))
                return $"Порт {networkInterface.PortNumber.Trim()}";

            if (!string.IsNullOrWhiteSpace(networkInterface.IpAddress))
                return networkInterface.IpAddress.Trim();

            return networkInterface.NetworkInterfaceId;
        }
    }
}
