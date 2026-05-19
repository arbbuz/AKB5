using System.Text.Json;
using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.Core.Tests;

public class JsonStorageServiceCompositionTests
{
    [Fact]
    public void SaveAndLoad_RoundTripsCompositionEntries()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            string path = Path.Combine(tempDirectory, "kb.json");
            var service = new JsonStorageService(path);
            var data = new SavedData
            {
                SchemaVersion = SavedData.CurrentSchemaVersion,
                Config = new KbConfig
                {
                    MaxLevels = 2,
                    LevelNames = new List<string> { "Цех", "Шкаф" }
                },
                Workshops = new Dictionary<string, List<KbNode>>
                {
                    ["Цех 1"] = new List<KbNode>
                    {
                        new()
                        {
                            NodeId = "cabinet-1",
                            Name = "Шкаф 1",
                            LevelIndex = 0,
                            NodeType = KbNodeType.Cabinet
                        }
                    }
                },
                CompositionEntries = new List<KbCompositionEntry>
                {
                    new()
                    {
                        EntryId = "entry-1",
                        ParentNodeId = "cabinet-1",
                        RackNumber = 1,
                        SlotNumber = 1,
                        PositionOrder = 0,
                        ComponentType = "CPU",
                        Model = "S7-1500",
                        IpAddress = "10.10.0.15",
                        Notes = "Main PLC"
                    }
                },
                LastWorkshop = "Цех 1"
            };

            Assert.True(service.Save(data, out var errorMessage));
            Assert.Null(errorMessage);

            var loaded = service.Load();

            Assert.True(loaded.IsSuccess);
            var entry = Assert.Single(loaded.Data!.CompositionEntries);
            Assert.Equal("entry-1", entry.EntryId);
            Assert.Equal("cabinet-1", entry.ParentNodeId);
            Assert.Equal(1, entry.RackNumber);
            Assert.Equal(1, entry.SlotNumber);
            Assert.Equal("CPU", entry.ComponentType);
            Assert.Equal("S7-1500", entry.Model);
            Assert.Equal("10.10.0.15", entry.IpAddress);
            Assert.Equal("Main PLC", entry.Notes);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Save_NormalizesCompositionEntriesBeforeWriting()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            string path = Path.Combine(tempDirectory, "kb.json");
            var service = new JsonStorageService(path);

            Assert.True(service.Save(
                new SavedData
                {
                    SchemaVersion = SavedData.CurrentSchemaVersion,
                    Config = new KbConfig
                    {
                        MaxLevels = 2,
                        LevelNames = new List<string> { "Цех", "Шкаф" }
                    },
                    Workshops = new Dictionary<string, List<KbNode>>
                    {
                        ["Цех 1"] = new List<KbNode>
                        {
                            new()
                            {
                                NodeId = "cabinet-1",
                                Name = "Шкаф 1",
                                LevelIndex = 0,
                                NodeType = KbNodeType.Cabinet
                            }
                        }
                    },
                    CompositionEntries = new List<KbCompositionEntry>
                    {
                        new()
                        {
                            ParentNodeId = " cabinet-1 ",
                            PositionOrder = -1,
                            ComponentType = " CPU ",
                            Model = " PLC-1 "
                        }
                    },
                    LastWorkshop = "Цех 1"
                },
                out _));

            var saved = JsonSerializer.Deserialize<SavedData>(File.ReadAllText(path));

            var entry = Assert.Single(saved!.CompositionEntries);
            Assert.Equal("cabinet-1", entry.ParentNodeId);
            Assert.Equal(0, entry.PositionOrder);
            Assert.Equal("CPU", entry.ComponentType);
            Assert.Equal("PLC-1", entry.Model);
            Assert.False(string.IsNullOrWhiteSpace(entry.EntryId));
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void SaveAndLoad_RoundTripsDocumentAndSoftwareRecords()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            string path = Path.Combine(tempDirectory, "kb.json");
            var service = new JsonStorageService(path);
            var data = new SavedData
            {
                SchemaVersion = SavedData.CurrentSchemaVersion,
                Config = new KbConfig
                {
                    MaxLevels = 2,
                    LevelNames = new List<string> { "Shop", "Cabinet" }
                },
                Workshops = new Dictionary<string, List<KbNode>>
                {
                    ["Shop 1"] = new List<KbNode>
                    {
                        new()
                        {
                            NodeId = "cabinet-1",
                            Name = "Cabinet 1",
                            LevelIndex = 0,
                            NodeType = KbNodeType.Cabinet
                        }
                    }
                },
                DocumentLinks = new List<KbDocumentLink>
                {
                    new()
                    {
                        DocumentId = "doc-1",
                        OwnerNodeId = "cabinet-1",
                        Kind = KbDocumentKind.SchemeLink,
                        Title = "Wiring Diagram",
                        Path = "\\\\srv\\docs\\wiring.pdf",
                        UpdatedAt = new DateTime(2026, 4, 1)
                    }
                },
                SoftwareRecords = new List<KbSoftwareRecord>
                {
                    new()
                    {
                        SoftwareId = "software-1",
                        OwnerNodeId = "cabinet-1",
                        Title = "PLC Backup",
                        Path = "\\\\srv\\backup\\plc.zip",
                        AddedAt = new DateTime(2026, 4, 1),
                        LastChangedAt = new DateTime(2026, 4, 2),
                        LastBackupAt = new DateTime(2026, 4, 3),
                        Notes = "Main backup"
                    }
                },
                NetworkFileReferences = new List<KbNetworkFileReference>
                {
                    new()
                    {
                        NetworkAssetId = "network-1",
                        OwnerNodeId = "cabinet-1",
                        Title = "Topology",
                        Path = "\\\\srv\\network\\topology.png",
                        PreviewKind = KbNetworkPreviewKind.Image
                    }
                },
                NetworkDevices = new List<KbNetworkDevice>
                {
                    new()
                    {
                        NetworkDeviceId = "device-1",
                        OwnerNodeId = "cabinet-1",
                        LinkedNodeId = "cabinet-1",
                        Name = "PLC-1",
                        Role = "Controller",
                        Vendor = "Siemens",
                        Model = "CPU 1214C",
                        ProfinetName = "plc-1",
                        MacAddress = "00-11-22-33-44-55",
                        CabinetText = "Cabinet 1",
                        Notes = "Main controller"
                    },
                    new()
                    {
                        NetworkDeviceId = "device-2",
                        OwnerNodeId = "cabinet-1",
                        Name = "HMI-1",
                        Role = "Operator panel",
                        Vendor = "Siemens",
                        Model = "KTP700",
                        ProfinetName = "hmi-1"
                    }
                },
                NetworkInterfaces = new List<KbNetworkInterface>
                {
                    new()
                    {
                        NetworkInterfaceId = "interface-1",
                        NetworkDeviceId = "device-1",
                        InterfaceName = "X1",
                        PortNumber = "1",
                        IpAddress = "10.0.0.10",
                        SubnetMask = "255.255.255.0",
                        Gateway = "10.0.0.1",
                        Protocol = "Profinet"
                    },
                    new()
                    {
                        NetworkInterfaceId = "interface-2",
                        NetworkDeviceId = "device-2",
                        InterfaceName = "X1",
                        PortNumber = "1",
                        IpAddress = "10.0.0.20",
                        SubnetMask = "255.255.255.0",
                        Gateway = "10.0.0.1",
                        Protocol = "Profinet"
                    }
                },
                NetworkConnections = new List<KbNetworkConnection>
                {
                    new()
                    {
                        NetworkConnectionId = "connection-1",
                        EndpointAInterfaceId = "interface-1",
                        EndpointBInterfaceId = "interface-2",
                        CableLabel = "W1",
                        CableType = "Profinet",
                        Length = "12 m",
                        Status = "Active"
                    }
                },
                LastWorkshop = "Shop 1"
            };

            Assert.True(service.Save(data, out var errorMessage));
            Assert.Null(errorMessage);

            var loaded = service.Load();

            Assert.True(loaded.IsSuccess);
            var link = Assert.Single(loaded.Data!.DocumentLinks);
            Assert.Equal("doc-1", link.DocumentId);
            Assert.Equal("cabinet-1", link.OwnerNodeId);
            Assert.Equal(KbDocumentKind.SchemeLink, link.Kind);
            Assert.Equal("Wiring Diagram", link.Title);

            var record = Assert.Single(loaded.Data.SoftwareRecords);
            Assert.Equal("software-1", record.SoftwareId);
            Assert.Equal("cabinet-1", record.OwnerNodeId);
            Assert.Equal("PLC Backup", record.Title);
            Assert.Equal("\\\\srv\\backup\\plc.zip", record.Path);
            Assert.Equal(new DateTime(2026, 4, 1), record.AddedAt);
            Assert.Equal(new DateTime(2026, 4, 2), record.LastChangedAt);
            Assert.Equal(new DateTime(2026, 4, 3), record.LastBackupAt);
            Assert.Equal("Main backup", record.Notes);

            var networkReference = Assert.Single(loaded.Data.NetworkFileReferences);
            Assert.Equal("network-1", networkReference.NetworkAssetId);
            Assert.Equal("cabinet-1", networkReference.OwnerNodeId);
            Assert.Equal("Topology", networkReference.Title);
            Assert.Equal("\\\\srv\\network\\topology.png", networkReference.Path);
            Assert.Equal(KbNetworkPreviewKind.Image, networkReference.PreviewKind);

            Assert.Equal(2, loaded.Data.NetworkDevices.Count);
            Assert.Equal("PLC-1", loaded.Data.NetworkDevices[0].Name);
            Assert.Equal("cabinet-1", loaded.Data.NetworkDevices[0].OwnerNodeId);
            Assert.Equal("cabinet-1", loaded.Data.NetworkDevices[0].LinkedNodeId);

            Assert.Equal(2, loaded.Data.NetworkInterfaces.Count);
            Assert.Equal("interface-1", loaded.Data.NetworkInterfaces[0].NetworkInterfaceId);
            Assert.Equal("device-1", loaded.Data.NetworkInterfaces[0].NetworkDeviceId);
            Assert.Equal("10.0.0.10", loaded.Data.NetworkInterfaces[0].IpAddress);

            var connection = Assert.Single(loaded.Data.NetworkConnections);
            Assert.Equal("connection-1", connection.NetworkConnectionId);
            Assert.Equal("interface-1", connection.EndpointAInterfaceId);
            Assert.Equal("interface-2", connection.EndpointBInterfaceId);
            Assert.Equal("W1", connection.CableLabel);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private static string CreateTempDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), $"asutp-composition-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }
}
