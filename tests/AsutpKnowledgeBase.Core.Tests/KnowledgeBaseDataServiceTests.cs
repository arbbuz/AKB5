using System.Text.Json;
using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.Core.Tests;

public class KnowledgeBaseDataServiceTests
{
    [Fact]
    public void CreateDefaultData_ReturnsExpectedInitialWorkshop()
    {
        var data = KnowledgeBaseDataService.CreateDefaultData();

        Assert.Equal("Новый цех", data.LastWorkshop);
        Assert.True(data.Workshops.ContainsKey("Новый цех"));
        Assert.Empty(data.Workshops["Новый цех"]);
        Assert.Equal(10, data.Config.MaxLevels);
        Assert.Equal("Уровень 1", data.Config.LevelNames[0]);
        Assert.Equal("Уровень 10", data.Config.LevelNames[9]);
    }

    [Fact]
    public void NormalizeConfig_FillsMissingNamesAndTrimsExtraOnes()
    {
        var normalized = KnowledgeBaseDataService.NormalizeConfig(
            new KbConfig
            {
                MaxLevels = 2,
                LevelNames = new List<string> { "  Цех  ", "", "Лишний" }
            });

        Assert.Equal(2, normalized.MaxLevels);
        Assert.Equal(new[] { "Цех", "Лишний" }, normalized.LevelNames);
    }

    [Fact]
    public void NormalizeWorkshops_SkipsBlankNamesAndCreatesDefaultWhenEmpty()
    {
        var normalized = KnowledgeBaseDataService.NormalizeWorkshops(
            new Dictionary<string, List<KbNode>>
            {
                ["   "] = new List<KbNode>(),
                [" Цех 1 "] = new List<KbNode> { new() { Name = "Линия 1" } }
            });

        Assert.Single(normalized);
        Assert.True(normalized.ContainsKey("Цех 1"));

        var fallback = KnowledgeBaseDataService.NormalizeWorkshops(null);
        Assert.True(fallback.ContainsKey("Новый цех"));
    }

    [Fact]
    public void NormalizeWorkshops_WhenNamesConflictAfterTrimAndCase_Throws()
    {
        var error = Assert.Throws<InvalidOperationException>(() =>
            KnowledgeBaseDataService.NormalizeWorkshops(
                new Dictionary<string, List<KbNode>>
                {
                    [" Цех 1 "] = new List<KbNode>(),
                    ["цех 1"] = new List<KbNode>()
                }));

        Assert.Contains("без учёта регистра", error.Message);
    }

    [Fact]
    public void NormalizeSavedData_AssignsStableNodeIdsAndResolvedNodeTypesForLegacyNodes()
    {
        var legacyData = new SavedData
        {
            SchemaVersion = 2,
            Config = new KbConfig
            {
                MaxLevels = 3,
                LevelNames = new List<string> { "Цех", "Линия", "Щит" }
            },
            Workshops = new Dictionary<string, List<KbNode>>
            {
                ["Цех 1"] = new List<KbNode>
                {
                    new()
                    {
                        Name = "Линия 1",
                        LevelIndex = 0,
                        Children =
                        {
                            new KbNode
                            {
                                Name = "Щит 1",
                                LevelIndex = 1
                            }
                        }
                    },
                    new()
                    {
                        Name = "Линия 2",
                        LevelIndex = 0
                    }
                }
            },
            LastWorkshop = "Цех 1"
        };

        var first = KnowledgeBaseDataService.NormalizeSavedData(Clone(legacyData));
        var second = KnowledgeBaseDataService.NormalizeSavedData(Clone(legacyData));

        var firstRoot = Assert.Single(first.Workshops["Цех 1"].Where(node => node.Name == "Линия 1"));
        var secondRoot = Assert.Single(second.Workshops["Цех 1"].Where(node => node.Name == "Линия 1"));
        var firstChild = Assert.Single(firstRoot.Children);
        var secondChild = Assert.Single(secondRoot.Children);

        Assert.Equal(SavedData.CurrentSchemaVersion, first.SchemaVersion);
        Assert.False(string.IsNullOrWhiteSpace(firstRoot.NodeId));
        Assert.False(string.IsNullOrWhiteSpace(firstChild.NodeId));
        Assert.Equal(firstRoot.NodeId, secondRoot.NodeId);
        Assert.Equal(firstChild.NodeId, secondChild.NodeId);
        Assert.Equal(KbNodeType.System, firstRoot.NodeType);
        Assert.Equal(KbNodeType.Cabinet, firstChild.NodeType);
    }

    [Fact]
    public void NormalizeSavedData_ClearsTechnicalFieldsForNonTechnicalNodeTypes()
    {
        var normalized = KnowledgeBaseDataService.NormalizeSavedData(
            new SavedData
            {
                SchemaVersion = SavedData.CurrentSchemaVersion,
                Config = new KbConfig
                {
                    MaxLevels = 4,
                    LevelNames = new List<string> { "Цех", "Линия", "Участок", "Документы" }
                },
                Workshops = new Dictionary<string, List<KbNode>>
                {
                    ["Цех 1"] = new List<KbNode>
                    {
                        new()
                        {
                            NodeId = "doc-root",
                            Name = "Документы",
                            LevelIndex = 3,
                            NodeType = KbNodeType.DocumentNode,
                            Details = new KbNodeDetails
                            {
                                IpAddress = "10.10.10.10",
                                SchemaLink = "https://intra/docs"
                            }
                        }
                    }
                },
                LastWorkshop = "Цех 1"
            });

        var node = Assert.Single(normalized.Workshops["Цех 1"]);
        Assert.Equal(KbNodeType.DocumentNode, node.NodeType);
        Assert.Equal(string.Empty, node.Details.IpAddress);
        Assert.Equal(string.Empty, node.Details.SchemaLink);
    }

    [Fact]
    public void NormalizeSavedData_PreservesInventoryNumberForVisibleLevel2NodeWithoutHiddenWrapper()
    {
        var normalized = KnowledgeBaseDataService.NormalizeSavedData(
            new SavedData
            {
                SchemaVersion = SavedData.CurrentSchemaVersion,
                Workshops = new Dictionary<string, List<KbNode>>
                {
                    ["Shop 1"] = new()
                    {
                        new KbNode
                        {
                            NodeId = "root-1",
                            Name = "Department 1",
                            LevelIndex = 0,
                            NodeType = KbNodeType.Department,
                            Children =
                            {
                                new KbNode
                                {
                                    NodeId = "level2-node",
                                    Name = "Line 1",
                                    LevelIndex = 1,
                                    NodeType = KbNodeType.System,
                                    Details = new KbNodeDetails
                                    {
                                        InventoryNumber = " INV-001 "
                                    },
                                    Children =
                                    {
                                        new KbNode
                                        {
                                            NodeId = "level3-node",
                                            Name = "Cabinet 1",
                                            LevelIndex = 2,
                                            NodeType = KbNodeType.Cabinet,
                                            Details = new KbNodeDetails
                                            {
                                                InventoryNumber = " INV-CHILD "
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
                LastWorkshop = "Shop 1"
            });

        var level2Node = Assert.Single(normalized.Workshops["Shop 1"]).Children.Single();
        Assert.Equal(" INV-001 ", level2Node.Details.InventoryNumber);
        Assert.Equal(string.Empty, level2Node.Children.Single().Details.InventoryNumber);
    }

    [Fact]
    public void NormalizeSavedData_ClearsLocationPhotoAndTechnicalFieldsForVisibleLevel2Node()
    {
        var normalized = KnowledgeBaseDataService.NormalizeSavedData(
            new SavedData
            {
                SchemaVersion = SavedData.CurrentSchemaVersion,
                Workshops = new Dictionary<string, List<KbNode>>
                {
                    ["Shop 1"] = new()
                    {
                        new KbNode
                        {
                            NodeId = "level1-node",
                            Name = "Department 1",
                            LevelIndex = 0,
                            NodeType = KbNodeType.Department,
                            Children =
                            {
                                new KbNode
                                {
                                    NodeId = "level2-node",
                                    Name = "Line 1",
                                    LevelIndex = 1,
                                    NodeType = KbNodeType.System,
                                    Details = new KbNodeDetails
                                    {
                                        Location = "Should be cleared",
                                        PhotoPath = @"C:\line-photo.jpg",
                                        IpAddress = "10.10.10.10",
                                        SchemaLink = "https://intra/system"
                                    }
                                }
                            }
                        }
                    }
                },
                LastWorkshop = "Shop 1"
            });

        var level2Node = Assert.Single(normalized.Workshops["Shop 1"]).Children.Single();
        Assert.Equal(string.Empty, level2Node.Details.Location);
        Assert.Equal(string.Empty, level2Node.Details.PhotoPath);
        Assert.Equal(string.Empty, level2Node.Details.IpAddress);
        Assert.Equal(string.Empty, level2Node.Details.SchemaLink);
    }

    [Fact]
    public void NormalizeSavedData_NormalizesCompositionEntries()
    {
        var normalized = KnowledgeBaseDataService.NormalizeSavedData(
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
                        SlotNumber = -1,
                        PositionOrder = -2,
                        ComponentType = " CPU ",
                        Model = " PLC-1 ",
                        Notes = " Main "
                    },
                    new()
                    {
                        ParentNodeId = "   "
                    }
                },
                LastWorkshop = "Цех 1"
            });

        var entry = Assert.Single(normalized.CompositionEntries);
        Assert.Equal("cabinet-1", entry.ParentNodeId);
        Assert.Null(entry.SlotNumber);
        Assert.Equal(0, entry.PositionOrder);
        Assert.Equal("CPU", entry.ComponentType);
        Assert.Equal("PLC-1", entry.Model);
        Assert.Equal("Main", entry.Notes);
        Assert.False(string.IsNullOrWhiteSpace(entry.EntryId));
    }

    [Fact]
    public void NormalizeSavedData_NormalizesDocumentAndSoftwareRecords()
    {
        var normalized = KnowledgeBaseDataService.NormalizeSavedData(
            new SavedData
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
                        OwnerNodeId = " cabinet-1 ",
                        Kind = (KbDocumentKind)999,
                        Title = " Wiring Diagram ",
                        Path = " \\\\srv\\docs\\wiring.pdf ",
                        UpdatedAt = new DateTime(2026, 4, 3, 15, 0, 0)
                    },
                    new()
                    {
                        OwnerNodeId = "   "
                    }
                },
                SoftwareRecords = new List<KbSoftwareRecord>
                {
                    new()
                    {
                        OwnerNodeId = " cabinet-1 ",
                        Title = " PLC Backup ",
                        Path = " \\\\srv\\backup\\plc.zip ",
                        AddedAt = new DateTime(2026, 4, 2, 9, 0, 0),
                        LastChangedAt = new DateTime(2026, 4, 4, 9, 0, 0),
                        LastBackupAt = new DateTime(2026, 4, 5, 9, 0, 0),
                        Notes = " weekly "
                    },
                    new()
                    {
                        OwnerNodeId = string.Empty
                    }
                },
                LastWorkshop = "Shop 1"
            });

        var link = Assert.Single(normalized.DocumentLinks);
        Assert.Equal("cabinet-1", link.OwnerNodeId);
        Assert.Equal(KbDocumentKind.Manual, link.Kind);
        Assert.Equal("Wiring Diagram", link.Title);
        Assert.Equal("\\\\srv\\docs\\wiring.pdf", link.Path);
        Assert.Equal(new DateTime(2026, 4, 3), link.UpdatedAt);
        Assert.False(string.IsNullOrWhiteSpace(link.DocumentId));

        var record = Assert.Single(normalized.SoftwareRecords);
        Assert.Equal("cabinet-1", record.OwnerNodeId);
        Assert.Equal("PLC Backup", record.Title);
        Assert.Equal("\\\\srv\\backup\\plc.zip", record.Path);
        Assert.Equal(new DateTime(2026, 4, 2), record.AddedAt);
        Assert.Equal(new DateTime(2026, 4, 4), record.LastChangedAt);
        Assert.Equal(new DateTime(2026, 4, 5), record.LastBackupAt);
        Assert.Equal("weekly", record.Notes);
        Assert.False(string.IsNullOrWhiteSpace(record.SoftwareId));
    }

    [Fact]
    public void NormalizeSavedData_NormalizesNetworkFileReferences()
    {
        var normalized = KnowledgeBaseDataService.NormalizeSavedData(
            new SavedData
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
                NetworkFileReferences = new List<KbNetworkFileReference>
                {
                    new()
                    {
                        OwnerNodeId = " cabinet-1 ",
                        Title = " Topology ",
                        Path = " \\\\srv\\network\\topology.png ",
                        PreviewKind = (KbNetworkPreviewKind)999
                    },
                    new()
                    {
                        OwnerNodeId = "   "
                    }
                },
                LastWorkshop = "Shop 1"
            });

        var reference = Assert.Single(normalized.NetworkFileReferences);
        Assert.Equal("cabinet-1", reference.OwnerNodeId);
        Assert.Equal("Topology", reference.Title);
        Assert.Equal("\\\\srv\\network\\topology.png", reference.Path);
        Assert.Equal(KbNetworkPreviewKind.Image, reference.PreviewKind);
        Assert.False(string.IsNullOrWhiteSpace(reference.NetworkAssetId));
    }

    [Fact]
    public void NormalizeSavedData_NormalizesMaintenanceScheduleProfiles()
    {
        var normalized = KnowledgeBaseDataService.NormalizeSavedData(
            new SavedData
            {
                SchemaVersion = SavedData.CurrentSchemaVersion,
                Workshops = new Dictionary<string, List<KbNode>>
                {
                    ["Shop 1"] = new()
                    {
                        new KbNode
                        {
                            NodeId = "system-1",
                            Name = "Line 1",
                            LevelIndex = 0,
                            NodeType = KbNodeType.System
                        }
                    }
                },
                MaintenanceScheduleProfiles = new List<KbMaintenanceScheduleProfile>
                {
                    new()
                    {
                        OwnerNodeId = " system-1 ",
                        IsIncludedInSchedule = true,
                        To1Hours = 2,
                        To2Hours = -4,
                        To3Hours = 8,
                        YearScheduleEntries = new List<KbMaintenanceYearScheduleEntry>
                        {
                            new() { Month = 3, WorkKind = KbMaintenanceWorkKind.To2 },
                            new() { Month = 13, WorkKind = KbMaintenanceWorkKind.To3 },
                            new() { Month = 4, WorkKind = (KbMaintenanceWorkKind)999 },
                            new() { Month = 3, WorkKind = KbMaintenanceWorkKind.To3 }
                        }
                    },
                    new()
                    {
                        OwnerNodeId = "   ",
                        To1Hours = 1
                    },
                    new()
                    {
                        OwnerNodeId = "system-1",
                        IsIncludedInSchedule = false,
                        To1Hours = 99
                    }
                },
                LastWorkshop = "Shop 1"
            });

        var profile = Assert.Single(normalized.MaintenanceScheduleProfiles);
        Assert.Equal("system-1", profile.OwnerNodeId);
        Assert.True(profile.IsIncludedInSchedule);
        Assert.Equal(2, profile.To1Hours);
        Assert.Equal(0, profile.To2Hours);
        Assert.Equal(8, profile.To3Hours);
        var scheduleEntry = Assert.Single(profile.YearScheduleEntries);
        Assert.Equal(3, scheduleEntry.Month);
        Assert.Equal(KbMaintenanceWorkKind.To3, scheduleEntry.WorkKind);
        Assert.False(string.IsNullOrWhiteSpace(profile.MaintenanceProfileId));
    }

    [Fact]
    public void NormalizeSavedData_UsesSingleMaintenanceProfilePerOwnerNode()
    {
        var normalized = KnowledgeBaseDataService.NormalizeSavedData(
            new SavedData
            {
                SchemaVersion = SavedData.CurrentSchemaVersion,
                Workshops = new Dictionary<string, List<KbNode>>
                {
                    ["Shop 1"] = new()
                    {
                        new KbNode
                        {
                            NodeId = "device-1",
                            Name = "Pump 1",
                            LevelIndex = 0,
                            NodeType = KbNodeType.Device
                        }
                    }
                },
                MaintenanceScheduleProfiles = new List<KbMaintenanceScheduleProfile>
                {
                    new()
                    {
                        OwnerNodeId = "device-1",
                        IsIncludedInSchedule = true,
                        To1Hours = 2
                    },
                    new()
                    {
                        OwnerNodeId = "device-1",
                        IsIncludedInSchedule = false,
                        To1Hours = 7
                    }
                },
                LastWorkshop = "Shop 1"
            });

        var profile = Assert.Single(normalized.MaintenanceScheduleProfiles);
        Assert.True(profile.IsIncludedInSchedule);
        Assert.Equal(2, profile.To1Hours);
    }

    [Fact]
    public void ResolveWorkshop_UsesPreferredWorkshopWhenItExists()
    {
        var workshops = new Dictionary<string, List<KbNode>>
        {
            ["Первый"] = new(),
            ["Второй"] = new()
        };

        Assert.Equal("Второй", KnowledgeBaseDataService.ResolveWorkshop(workshops, " Второй "));
        Assert.Equal("Второй", KnowledgeBaseDataService.ResolveWorkshop(workshops, "второй"));
        Assert.Equal("Первый", KnowledgeBaseDataService.ResolveWorkshop(workshops, "Несуществующий"));
    }

    [Fact]
    public void SerializeSnapshot_CanOmitCurrentWorkshopFromDirtyCheck()
    {
        var snapshot = KnowledgeBaseDataService.SerializeSnapshot(
            KnowledgeBaseDataService.CreateDefaultConfig(),
            new Dictionary<string, List<KbNode>>
            {
                ["Цех 1"] = new List<KbNode>()
            },
            currentWorkshop: "Цех 1",
            includeCurrentWorkshop: false);

        var restored = JsonSerializer.Deserialize<SavedData>(snapshot);

        Assert.NotNull(restored);
        Assert.Equal(string.Empty, restored!.LastWorkshop);
    }

    private static SavedData Clone(SavedData source) =>
        JsonSerializer.Deserialize<SavedData>(JsonSerializer.Serialize(source))!;
}
