using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.Core.Tests;

public class KnowledgeBaseNetworkStateServiceTests
{
    private readonly KnowledgeBaseNetworkStateService _service = new();

    [Fact]
    public void Build_IncludesNetworkPassportRows_ForLevel2Owner()
    {
        var ownerNode = CreateOwnerNode();

        var state = _service.Build(
            ownerNode,
            networkFileReferences: new[]
            {
                new KbNetworkFileReference
                {
                    NetworkAssetId = "file-1",
                    OwnerNodeId = "system-1",
                    Title = "Topology",
                    Path = "\\\\srv\\net\\topology.png",
                    PreviewKind = KbNetworkPreviewKind.Image
                }
            },
            networkDevices: new[]
            {
                new KbNetworkDevice
                {
                    NetworkDeviceId = "device-1",
                    OwnerNodeId = "system-1",
                    LinkedNodeId = "cabinet-1",
                    Name = "PLC-1",
                    Role = "Controller"
                },
                new KbNetworkDevice
                {
                    NetworkDeviceId = "device-other",
                    OwnerNodeId = "system-2",
                    Name = "Other PLC"
                }
            },
            networkInterfaces: new[]
            {
                new KbNetworkInterface
                {
                    NetworkInterfaceId = "iface-1",
                    NetworkDeviceId = "device-1",
                    InterfaceName = "X1",
                    IpAddress = "10.0.0.10"
                },
                new KbNetworkInterface
                {
                    NetworkInterfaceId = "iface-2",
                    NetworkDeviceId = "device-1",
                    PortNumber = "2",
                    IpAddress = "10.0.0.11"
                },
                new KbNetworkInterface
                {
                    NetworkInterfaceId = "iface-other",
                    NetworkDeviceId = "device-other",
                    IpAddress = "10.0.1.10"
                }
            },
            networkConnections: new[]
            {
                new KbNetworkConnection
                {
                    NetworkConnectionId = "connection-1",
                    EndpointAInterfaceId = "iface-1",
                    EndpointBInterfaceId = "iface-2",
                    CableLabel = "W1",
                    CableType = "PN cable",
                    Protocol = "PROFINET",
                    Medium = "copper",
                    RouteText = "Operator room +7.0",
                    Status = "active",
                    Notes = "scheme source"
                },
                new KbNetworkConnection
                {
                    NetworkConnectionId = "connection-other",
                    EndpointAInterfaceId = "iface-1",
                    EndpointBInterfaceId = "iface-other"
                }
            },
            visibleLevel: 2);

        Assert.True(state.SupportsEditing);
        Assert.True(state.HasPassportRows);
        Assert.True(state.HasEntries);
        Assert.Equal(1, state.DeviceCount);
        Assert.Equal(2, state.InterfaceCount);
        Assert.Equal(1, state.ConnectionCount);
        Assert.Equal(1, state.FileReferencesCount);

        var device = Assert.Single(state.DeviceStates);
        Assert.Equal("PLC-1", device.NameText);
        Assert.Equal("Cabinet 1", device.LinkedNodeText);
        Assert.Equal(2, device.InterfacesCount);
        Assert.Equal(1, device.ConnectionsCount);

        var firstInterface = state.InterfaceStates.Single(item => item.NetworkInterfaceId == "iface-1");
        Assert.Equal("X1", firstInterface.InterfaceNameText);
        Assert.Equal("PLC-1 / X1 / 10.0.0.10", state.ConnectionStates[0].EndpointAText);
        Assert.Equal("PLC-1 / Порт 2 / 10.0.0.11", state.ConnectionStates[0].EndpointBText);
        Assert.Equal("PROFINET", state.ConnectionStates[0].ProtocolText);
        Assert.Equal("copper", state.ConnectionStates[0].MediumText);
        Assert.Equal("Operator room +7.0", state.ConnectionStates[0].RouteText);
    }

    [Fact]
    public void Build_ForUnsupportedVisibleLevel_DoesNotExposePassportRows()
    {
        var cabinetNode = new KbNode
        {
            NodeId = "cabinet-1",
            Name = "Cabinet 1",
            NodeType = KbNodeType.Cabinet
        };

        var state = _service.Build(
            cabinetNode,
            networkFileReferences: Array.Empty<KbNetworkFileReference>(),
            networkDevices: new[]
            {
                new KbNetworkDevice
                {
                    NetworkDeviceId = "device-1",
                    OwnerNodeId = "system-1",
                    Name = "PLC-1"
                }
            },
            networkInterfaces: Array.Empty<KbNetworkInterface>(),
            networkConnections: Array.Empty<KbNetworkConnection>(),
            visibleLevel: 3);

        Assert.False(state.SupportsEditing);
        Assert.False(state.HasPassportRows);
        Assert.Empty(state.DeviceStates);
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
}
