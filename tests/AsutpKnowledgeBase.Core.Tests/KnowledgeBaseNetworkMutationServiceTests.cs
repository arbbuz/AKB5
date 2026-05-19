using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.Core.Tests;

public class KnowledgeBaseNetworkMutationServiceTests
{
    private readonly KnowledgeBaseNetworkMutationService _service = new();

    [Fact]
    public void UpsertNetworkFileReference_AddsNewImageReference_ForSupportedNode()
    {
        var ownerNode = new KbNode
        {
            NodeId = "cabinet-1",
            Name = "Cabinet 1",
            NodeType = KbNodeType.Cabinet
        };

        var result = _service.UpsertNetworkFileReference(
            ownerNode,
            Array.Empty<KbNetworkFileReference>(),
            new KbNetworkFileReference
            {
                Title = " Network scheme ",
                Path = " \\\\srv\\network\\scheme.png "
            });

        Assert.True(result.IsSuccess);
        var reference = Assert.Single(result.NetworkFileReferences);
        Assert.Equal("cabinet-1", reference.OwnerNodeId);
        Assert.Equal("Network scheme", reference.Title);
        Assert.Equal("\\\\srv\\network\\scheme.png", reference.Path);
        Assert.Equal(KbNetworkPreviewKind.Image, reference.PreviewKind);
    }

    [Fact]
    public void UpsertNetworkFileReference_UpdatesExistingReference_ForSameOwner()
    {
        var ownerNode = new KbNode
        {
            NodeId = "controller-1",
            Name = "Controller 1",
            NodeType = KbNodeType.Controller
        };

        var result = _service.UpsertNetworkFileReference(
            ownerNode,
            new List<KbNetworkFileReference>
            {
                new()
                {
                    NetworkAssetId = "network-1",
                    OwnerNodeId = "controller-1",
                    Title = "Old topology",
                    Path = "\\\\srv\\network\\topology.pdf",
                    PreviewKind = KbNetworkPreviewKind.MetadataOnly
                }
            },
            new KbNetworkFileReference
            {
                NetworkAssetId = "network-1",
                Title = "Updated topology",
                Path = "\\\\srv\\network\\topology.jpg"
            });

        Assert.True(result.IsSuccess);
        var reference = Assert.Single(result.NetworkFileReferences);
        Assert.Equal("network-1", reference.NetworkAssetId);
        Assert.Equal("Updated topology", reference.Title);
        Assert.Equal("\\\\srv\\network\\topology.jpg", reference.Path);
        Assert.Equal(KbNetworkPreviewKind.Image, reference.PreviewKind);
    }

    [Fact]
    public void DeleteNetworkFileReference_RemovesOnlySelectedReference_ForSameOwner()
    {
        var ownerNode = new KbNode
        {
            NodeId = "cabinet-1",
            Name = "Cabinet 1",
            NodeType = KbNodeType.Cabinet
        };

        var result = _service.DeleteNetworkFileReference(
            ownerNode,
            new List<KbNetworkFileReference>
            {
                new()
                {
                    NetworkAssetId = "network-1",
                    OwnerNodeId = "cabinet-1",
                    Title = "Main scheme",
                    Path = "\\\\srv\\network\\main.png",
                    PreviewKind = KbNetworkPreviewKind.Image
                },
                new()
                {
                    NetworkAssetId = "network-2",
                    OwnerNodeId = "cabinet-2",
                    Title = "Other scheme",
                    Path = "\\\\srv\\network\\other.png",
                    PreviewKind = KbNetworkPreviewKind.Image
                }
            },
            "network-1");

        Assert.True(result.IsSuccess);
        var remaining = Assert.Single(result.NetworkFileReferences);
        Assert.Equal("network-2", remaining.NetworkAssetId);
    }

    [Fact]
    public void UpsertNetworkFileReference_ForUnsupportedNode_ReturnsFailure()
    {
        var ownerNode = new KbNode
        {
            NodeId = "system-1",
            Name = "System 1",
            NodeType = KbNodeType.System
        };

        var result = _service.UpsertNetworkFileReference(
            ownerNode,
            Array.Empty<KbNetworkFileReference>(),
            new KbNetworkFileReference
            {
                Title = "Network scheme",
                Path = "\\\\srv\\network\\scheme.png"
            });

        Assert.False(result.IsSuccess);
        Assert.Contains("Сеть", result.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void UpsertNetworkFileReference_ForVisibleLevel3System_ReturnsFailure()
    {
        var ownerNode = new KbNode
        {
            NodeId = "legacy-cabinet-1",
            Name = "Шкаф 1",
            NodeType = KbNodeType.System
        };

        var result = _service.UpsertNetworkFileReference(
            ownerNode,
            Array.Empty<KbNetworkFileReference>(),
            new KbNetworkFileReference
            {
                Title = "Network scheme",
                Path = "\\\\srv\\network\\scheme.png"
            },
            visibleLevel: 3);

        Assert.False(result.IsSuccess);
        Assert.Contains("Сеть", result.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void UpsertNetworkFileReference_ForVisibleLevel2System_AddsReference()
    {
        var ownerNode = CreateOwnerNode();

        var result = _service.UpsertNetworkFileReference(
            ownerNode,
            Array.Empty<KbNetworkFileReference>(),
            new KbNetworkFileReference
            {
                Title = "Topology",
                Path = "\\\\srv\\network\\scheme.png"
            },
            visibleLevel: 2);

        Assert.True(result.IsSuccess);
        var reference = Assert.Single(result.NetworkFileReferences);
        Assert.Equal("system-1", reference.OwnerNodeId);
    }

    [Fact]
    public void UpsertDevice_AddsDevice_ForLevel2Owner()
    {
        var ownerNode = CreateOwnerNode();

        var result = _service.UpsertDevice(
            ownerNode,
            Array.Empty<KbNetworkDevice>(),
            Array.Empty<KbNetworkInterface>(),
            Array.Empty<KbNetworkConnection>(),
            new KbNetworkDevice
            {
                LinkedNodeId = " cabinet-1 ",
                Name = " PLC-1 ",
                Role = " Controller ",
                MacAddress = " 00-11-22-33-44-55 "
            },
            visibleLevel: 2);

        Assert.True(result.IsSuccess);
        var device = Assert.Single(result.NetworkDevices);
        Assert.Equal("system-1", device.OwnerNodeId);
        Assert.Equal("cabinet-1", device.LinkedNodeId);
        Assert.Equal("PLC-1", device.Name);
        Assert.Equal("Controller", device.Role);
        Assert.Equal("00-11-22-33-44-55", device.MacAddress);
    }

    [Fact]
    public void UpsertDevice_WithLinkedNodeOutsideOwner_ReturnsFailure()
    {
        var ownerNode = CreateOwnerNode();

        var result = _service.UpsertDevice(
            ownerNode,
            Array.Empty<KbNetworkDevice>(),
            Array.Empty<KbNetworkInterface>(),
            Array.Empty<KbNetworkConnection>(),
            new KbNetworkDevice
            {
                LinkedNodeId = "other-cabinet",
                Name = "PLC-1"
            },
            visibleLevel: 2);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void UpsertDevice_ForVisibleLevel3Owner_ReturnsFailure()
    {
        var ownerNode = new KbNode
        {
            NodeId = "cabinet-1",
            Name = "Cabinet 1",
            NodeType = KbNodeType.Cabinet
        };

        var result = _service.UpsertDevice(
            ownerNode,
            Array.Empty<KbNetworkDevice>(),
            Array.Empty<KbNetworkInterface>(),
            Array.Empty<KbNetworkConnection>(),
            new KbNetworkDevice
            {
                Name = "PLC-1"
            },
            visibleLevel: 3);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void UpsertInterface_AddsInterface_ForOwnedDevice()
    {
        var ownerNode = CreateOwnerNode();

        var result = _service.UpsertInterface(
            ownerNode,
            new[]
            {
                new KbNetworkDevice
                {
                    NetworkDeviceId = "device-1",
                    OwnerNodeId = "system-1",
                    Name = "PLC-1"
                }
            },
            Array.Empty<KbNetworkInterface>(),
            Array.Empty<KbNetworkConnection>(),
            new KbNetworkInterface
            {
                NetworkDeviceId = "device-1",
                InterfaceName = " X1 ",
                IpAddress = " 10.0.0.10 ",
                SubnetMask = " 255.255.255.0 "
            },
            visibleLevel: 2);

        Assert.True(result.IsSuccess);
        var networkInterface = Assert.Single(result.NetworkInterfaces);
        Assert.Equal("device-1", networkInterface.NetworkDeviceId);
        Assert.Equal("X1", networkInterface.InterfaceName);
        Assert.Equal("10.0.0.10", networkInterface.IpAddress);
        Assert.Equal("255.255.255.0", networkInterface.SubnetMask);
    }

    [Fact]
    public void UpsertInterface_ForDeviceOutsideOwner_ReturnsFailure()
    {
        var ownerNode = CreateOwnerNode();

        var result = _service.UpsertInterface(
            ownerNode,
            new[]
            {
                new KbNetworkDevice
                {
                    NetworkDeviceId = "device-1",
                    OwnerNodeId = "system-2",
                    Name = "Other PLC"
                }
            },
            Array.Empty<KbNetworkInterface>(),
            Array.Empty<KbNetworkConnection>(),
            new KbNetworkInterface
            {
                NetworkDeviceId = "device-1",
                InterfaceName = "X1"
            },
            visibleLevel: 2);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void UpsertConnection_AddsConnection_ForOwnedInterfaces()
    {
        var ownerNode = CreateOwnerNode();
        var devices = CreateDevices();
        var interfaces = CreateInterfaces();

        var result = _service.UpsertConnection(
            ownerNode,
            devices,
            interfaces,
            Array.Empty<KbNetworkConnection>(),
            new KbNetworkConnection
            {
                EndpointAInterfaceId = "iface-1",
                EndpointBInterfaceId = "iface-2",
                CableLabel = " W1 ",
                CableType = " Profinet ",
                Protocol = " PROFINET ",
                Medium = " copper ",
                Length = " 12 m ",
                RouteText = " Operator room +7.0 ",
                Status = " active ",
                Notes = " scheme row "
            },
            visibleLevel: 2);

        Assert.True(result.IsSuccess);
        var connection = Assert.Single(result.NetworkConnections);
        Assert.Equal("iface-1", connection.EndpointAInterfaceId);
        Assert.Equal("iface-2", connection.EndpointBInterfaceId);
        Assert.Equal("W1", connection.CableLabel);
        Assert.Equal("Profinet", connection.CableType);
        Assert.Equal("PROFINET", connection.Protocol);
        Assert.Equal("copper", connection.Medium);
        Assert.Equal("12 m", connection.Length);
        Assert.Equal("Operator room +7.0", connection.RouteText);
        Assert.Equal("active", connection.Status);
        Assert.Equal("scheme row", connection.Notes);
    }

    [Fact]
    public void UpsertConnection_ForDuplicateEndpointPair_ReturnsFailure()
    {
        var ownerNode = CreateOwnerNode();

        var result = _service.UpsertConnection(
            ownerNode,
            CreateDevices(),
            CreateInterfaces(),
            new[]
            {
                new KbNetworkConnection
                {
                    NetworkConnectionId = "connection-1",
                    EndpointAInterfaceId = "iface-1",
                    EndpointBInterfaceId = "iface-2"
                }
            },
            new KbNetworkConnection
            {
                EndpointAInterfaceId = "iface-2",
                EndpointBInterfaceId = "iface-1"
            },
            visibleLevel: 2);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void DeleteDevice_CascadesInterfacesAndConnections()
    {
        var ownerNode = CreateOwnerNode();

        var result = _service.DeleteDevice(
            ownerNode,
            CreateDevices(),
            CreateInterfaces(),
            CreateConnections(),
            "device-1",
            visibleLevel: 2);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.NetworkDevices);
        Assert.Empty(result.NetworkInterfaces);
        Assert.Empty(result.NetworkConnections);
    }

    [Fact]
    public void DeleteInterface_CascadesConnections()
    {
        var ownerNode = CreateOwnerNode();

        var result = _service.DeleteInterface(
            ownerNode,
            CreateDevices(),
            CreateInterfaces(),
            CreateConnections(),
            "iface-1",
            visibleLevel: 2);

        Assert.True(result.IsSuccess);
        Assert.Single(result.NetworkInterfaces);
        Assert.Empty(result.NetworkConnections);
    }

    [Fact]
    public void DeleteConnection_RemovesOwnedConnection()
    {
        var ownerNode = CreateOwnerNode();

        var result = _service.DeleteConnection(
            ownerNode,
            CreateDevices(),
            CreateInterfaces(),
            CreateConnections(),
            "connection-1",
            visibleLevel: 2);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.NetworkConnections);
    }

    private static KbNode CreateOwnerNode()
    {
        var ownerNode = new KbNode
        {
            NodeId = "system-1",
            Name = "System 1",
            NodeType = KbNodeType.System
        };
        ownerNode.Children.Add(
            new KbNode
            {
                NodeId = "cabinet-1",
                Name = "Cabinet 1",
                NodeType = KbNodeType.Cabinet
            });

        return ownerNode;
    }

    private static KbNetworkDevice[] CreateDevices() =>
    [
        new()
        {
            NetworkDeviceId = "device-1",
            OwnerNodeId = "system-1",
            Name = "PLC-1"
        }
    ];

    private static KbNetworkInterface[] CreateInterfaces() =>
    [
        new()
        {
            NetworkInterfaceId = "iface-1",
            NetworkDeviceId = "device-1",
            InterfaceName = "X1",
            IpAddress = "10.0.0.10"
        },
        new()
        {
            NetworkInterfaceId = "iface-2",
            NetworkDeviceId = "device-1",
            InterfaceName = "X2",
            IpAddress = "10.0.0.11"
        }
    ];

    private static KbNetworkConnection[] CreateConnections() =>
    [
        new()
        {
            NetworkConnectionId = "connection-1",
            EndpointAInterfaceId = "iface-1",
            EndpointBInterfaceId = "iface-2",
            CableLabel = "W1",
            Protocol = "PROFINET",
            Medium = "copper",
            RouteText = "Operator room +7.0"
        }
    ];
}
