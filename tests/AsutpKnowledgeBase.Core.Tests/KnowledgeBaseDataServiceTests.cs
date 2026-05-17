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
        Assert.Contains(data.Config.ProductionCalendarYears, static year => year.Year == 2025);
        Assert.Contains(data.Config.ProductionCalendarYears, static year => year.Year == 2026);
        Assert.Empty(data.EquipmentCatalogItems);
        Assert.Empty(data.ObjectTemplates);
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
    public void NormalizeConfig_MergesCustomProductionCalendarYearsWithBuiltInDefaults()
    {
        KbConfig normalized = KnowledgeBaseDataService.NormalizeConfig(
            new KbConfig
            {
                MaxLevels = 2,
                ProductionCalendarYears = new List<KbProductionCalendarYear>
                {
                    new()
                    {
                        Year = 2027,
                        AdditionalNonWorkingDays =
                        {
                            new DateOnly(2027, 1, 8),
                            new DateOnly(2027, 1, 8),
                            new DateOnly(2027, 5, 10)
                        },
                        AdditionalWorkingDays =
                        {
                            new DateOnly(2027, 2, 20),
                            new DateOnly(2027, 2, 20)
                        }
                    }
                }
            });

        Assert.Contains(normalized.ProductionCalendarYears, static year => year.Year == 2025);
        Assert.Contains(normalized.ProductionCalendarYears, static year => year.Year == 2026);
        KbProductionCalendarYear year2027 = Assert.Single(
            normalized.ProductionCalendarYears,
            static year => year.Year == 2027);
        Assert.Equal(
            new[] { new DateOnly(2027, 1, 8), new DateOnly(2027, 5, 10) },
            year2027.AdditionalNonWorkingDays);
        Assert.Equal(new[] { new DateOnly(2027, 2, 20) }, year2027.AdditionalWorkingDays);
    }

    [Fact]
    public void NormalizeConfig_RejectsProductionCalendarDatesFromAnotherYear()
    {
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() =>
            KnowledgeBaseDataService.NormalizeConfig(
                new KbConfig
                {
                    MaxLevels = 2,
                    ProductionCalendarYears = new List<KbProductionCalendarYear>
                    {
                        new()
                        {
                            Year = 2027,
                            AdditionalNonWorkingDays = { new DateOnly(2028, 1, 1) }
                        }
                    }
                }));

        Assert.Contains("2027", error.Message, StringComparison.Ordinal);
        Assert.Contains("01.01.2028", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void NormalizeConfig_RejectsProductionCalendarDatesConfiguredAsWorkingAndNonWorking()
    {
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() =>
            KnowledgeBaseDataService.NormalizeConfig(
                new KbConfig
                {
                    MaxLevels = 2,
                    ProductionCalendarYears = new List<KbProductionCalendarYear>
                    {
                        new()
                        {
                            Year = 2027,
                            AdditionalNonWorkingDays = { new DateOnly(2027, 2, 20) },
                            AdditionalWorkingDays = { new DateOnly(2027, 2, 20) }
                        }
                    }
                }));

        Assert.Contains("20.02.2027", error.Message, StringComparison.Ordinal);
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
                        RackNumber = -2,
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
        Assert.Equal(0, entry.RackNumber);
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
    public void NormalizeSavedData_MovesDocsSoftwareAndNetworkFromLevel3ToLevel2Owner()
    {
        var normalized = KnowledgeBaseDataService.NormalizeSavedData(
            new SavedData
            {
                SchemaVersion = SavedData.CurrentSchemaVersion,
                Config = new KbConfig
                {
                    MaxLevels = 3,
                    LevelNames = new List<string> { "Отделение", "Система", "Шкаф" }
                },
                Workshops = new Dictionary<string, List<KbNode>>
                {
                    ["Цех 1"] = new List<KbNode>
                    {
                        new()
                        {
                            NodeId = "department-1",
                            Name = "Медное отделение",
                            LevelIndex = 0,
                            NodeType = KbNodeType.Department,
                            Children =
                            {
                                new KbNode
                                {
                                    NodeId = "system-1",
                                    Name = "АСУ установкой получения МДК",
                                    LevelIndex = 1,
                                    NodeType = KbNodeType.System,
                                    Children =
                                    {
                                        new KbNode
                                        {
                                            NodeId = "cabinet-1",
                                            Name = "ШУ",
                                            LevelIndex = 2,
                                            NodeType = KbNodeType.Cabinet
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
                DocumentLinks = new List<KbDocumentLink>
                {
                    new()
                    {
                        OwnerNodeId = "cabinet-1",
                        Kind = KbDocumentKind.SchemeLink,
                        Title = "Схема",
                        Path = "\\\\srv\\docs\\scheme.pdf"
                    }
                },
                SoftwareRecords = new List<KbSoftwareRecord>
                {
                    new()
                    {
                        OwnerNodeId = "cabinet-1",
                        Title = "Архив ПО",
                        Path = "\\\\srv\\software\\backup.zip"
                    }
                },
                NetworkFileReferences = new List<KbNetworkFileReference>
                {
                    new()
                    {
                        OwnerNodeId = "cabinet-1",
                        Title = "Сеть",
                        Path = "\\\\srv\\network\\topology.png"
                    }
                },
                LastWorkshop = "Цех 1"
            });

        Assert.Equal("system-1", Assert.Single(normalized.DocumentLinks).OwnerNodeId);
        Assert.Equal("system-1", Assert.Single(normalized.SoftwareRecords).OwnerNodeId);
        Assert.Equal("system-1", Assert.Single(normalized.NetworkFileReferences).OwnerNodeId);
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
    public void NormalizeSavedData_NormalizesEquipmentCatalogItems()
    {
        var normalized = KnowledgeBaseDataService.NormalizeSavedData(
            new SavedData
            {
                SchemaVersion = SavedData.CurrentSchemaVersion,
                Workshops = new Dictionary<string, List<KbNode>>
                {
                    ["Цех"] = new()
                },
                EquipmentCatalogItems = new List<KbEquipmentCatalogItem>
                {
                    new()
                    {
                        CatalogItemId = " plc-1214 ",
                        EquipmentKind = " ПЛК ",
                        Manufacturer = " Siemens ",
                        Series = " S7-1200 ",
                        Model = " CPU 1214C ",
                        DefaultNodeType = KbNodeType.Controller,
                        Description = " Контроллер шкафа ",
                        Properties =
                        {
                            new KbEquipmentCatalogProperty { Name = " Питание ", Value = " 24 В DC " },
                            new KbEquipmentCatalogProperty { Name = "питание", Value = "дубликат" },
                            new KbEquipmentCatalogProperty { Name = " ", Value = "пустое имя" },
                            new KbEquipmentCatalogProperty { Name = " Интерфейс ", Value = " Profinet " }
                        }
                    },
                    new()
                    {
                        EquipmentKind = "плк",
                        Manufacturer = "siemens",
                        Series = "s7-1200",
                        Model = "cpu 1214c"
                    },
                    new()
                    {
                        EquipmentKind = " ",
                        Manufacturer = "",
                        Series = "",
                        Model = ""
                    }
                },
                LastWorkshop = "Цех"
            });

        KbEquipmentCatalogItem item = Assert.Single(normalized.EquipmentCatalogItems);
        Assert.Equal("plc-1214", item.CatalogItemId);
        Assert.Equal("ПЛК", item.EquipmentKind);
        Assert.Equal("Siemens", item.Manufacturer);
        Assert.Equal("S7-1200", item.Series);
        Assert.Equal("CPU 1214C", item.Model);
        Assert.Equal(KbNodeType.Controller, item.DefaultNodeType);
        Assert.Equal("Контроллер шкафа", item.Description);
        Assert.Equal(new[] { "Интерфейс", "Питание" }, item.Properties.Select(static property => property.Name));
        Assert.Equal("Profinet", item.Properties[0].Value);
        Assert.Equal("24 В DC", item.Properties[1].Value);
    }

    [Fact]
    public void NormalizeSavedData_DefaultsInvalidEquipmentCatalogNodeType()
    {
        var normalized = KnowledgeBaseDataService.NormalizeSavedData(
            new SavedData
            {
                SchemaVersion = SavedData.CurrentSchemaVersion,
                Workshops = new Dictionary<string, List<KbNode>>
                {
                    ["Цех"] = new()
                },
                EquipmentCatalogItems = new List<KbEquipmentCatalogItem>
                {
                    new()
                    {
                        EquipmentKind = "Модуль",
                        Manufacturer = "Phoenix Contact",
                        Model = "DI 16",
                        DefaultNodeType = (KbNodeType)999
                    }
                },
                LastWorkshop = "Цех"
            });

        KbEquipmentCatalogItem item = Assert.Single(normalized.EquipmentCatalogItems);
        Assert.Equal(KbNodeType.Device, item.DefaultNodeType);
    }

    [Fact]
    public void NormalizeSavedData_SkipsDuplicateEquipmentCatalogIds()
    {
        var normalized = KnowledgeBaseDataService.NormalizeSavedData(
            new SavedData
            {
                SchemaVersion = SavedData.CurrentSchemaVersion,
                Workshops = new Dictionary<string, List<KbNode>>
                {
                    ["Цех"] = new()
                },
                EquipmentCatalogItems = new List<KbEquipmentCatalogItem>
                {
                    new()
                    {
                        CatalogItemId = "catalog-shared",
                        EquipmentKind = "ПЛК",
                        Manufacturer = "Siemens",
                        Model = "CPU 1214C"
                    },
                    new()
                    {
                        CatalogItemId = " catalog-shared ",
                        EquipmentKind = "Панель оператора",
                        Manufacturer = "Siemens",
                        Model = "KTP700"
                    }
                },
                LastWorkshop = "Цех"
            });

        KbEquipmentCatalogItem item = Assert.Single(normalized.EquipmentCatalogItems);
        Assert.Equal("catalog-shared", item.CatalogItemId);
        Assert.Equal("CPU 1214C", item.Model);
    }

    [Fact]
    public void SerializeSnapshot_IncludesEquipmentCatalogItems()
    {
        var snapshot = KnowledgeBaseDataService.SerializeSnapshot(
            KnowledgeBaseDataService.CreateDefaultConfig(),
            new Dictionary<string, List<KbNode>>
            {
                ["Цех"] = new()
            },
            compositionRacks: null,
            compositionEntries: null,
            documentLinks: null,
            softwareRecords: null,
            networkFileReferences: null,
            maintenanceScheduleProfiles: null,
            equipmentCatalogItems:
            [
                new KbEquipmentCatalogItem
                {
                    CatalogItemId = "catalog-hmi",
                    EquipmentKind = "Панель оператора",
                    Manufacturer = "Siemens",
                    Model = "KTP700",
                    DefaultNodeType = KbNodeType.Device
                }
            ],
            currentWorkshop: "Цех",
            includeCurrentWorkshop: true);

        var restored = JsonSerializer.Deserialize<SavedData>(snapshot);

        Assert.NotNull(restored);
        KbEquipmentCatalogItem item = Assert.Single(restored!.EquipmentCatalogItems);
        Assert.Equal("catalog-hmi", item.CatalogItemId);
        Assert.Equal("KTP700", item.Model);
    }

    [Fact]
    public void NormalizeSavedData_NormalizesObjectTemplates()
    {
        var normalized = KnowledgeBaseDataService.NormalizeSavedData(
            new SavedData
            {
                SchemaVersion = SavedData.CurrentSchemaVersion,
                Workshops = new Dictionary<string, List<KbNode>>
                {
                    ["Цех"] = new()
                },
                ObjectTemplates = new List<KbObjectTemplate>
                {
                    new()
                    {
                        TemplateId = " cabinet-template ",
                        DisplayName = " Типовой шкаф ",
                        Description = " Шаблон шкафа ",
                        Category = " Шкафы ",
                        RootNode = new KbObjectTemplateNode
                        {
                            TemplateNodeId = " cabinet ",
                            CatalogItemId = " catalog-cabinet ",
                            Name = " Шкаф АСУТП ",
                            NodeType = KbNodeType.Cabinet,
                            Details = new KbNodeDetails
                            {
                                Description = " Вводной шкаф ",
                                Location = " Машзал ",
                                InventoryNumber = " INV-001 ",
                                IpAddress = " 192.168.1.10 "
                            },
                            Children =
                            {
                                new KbObjectTemplateNode
                                {
                                    TemplateNodeId = " controller ",
                                    Name = " Контроллер ",
                                    NodeType = KbNodeType.Controller
                                }
                            }
                        },
                        CompositionEntries =
                        {
                            new KbObjectTemplateCompositionEntry
                            {
                                ParentTemplateNodeId = " cabinet ",
                                SlotNumber = 1,
                                PositionOrder = -1,
                                ComponentType = " CPU ",
                                Model = " S7-1200 "
                            },
                            new KbObjectTemplateCompositionEntry
                            {
                                ParentTemplateNodeId = " missing ",
                                ComponentType = " Игнорируется "
                            }
                        },
                        DocumentLinks =
                        {
                            new KbObjectTemplateDocumentLink
                            {
                                OwnerTemplateNodeId = " cabinet ",
                                Kind = KbDocumentKind.SchemeLink,
                                Title = " Схема ",
                                Path = " \\\\srv\\schema.pdf "
                            }
                        },
                        SoftwareRecords =
                        {
                            new KbObjectTemplateSoftwareRecord
                            {
                                OwnerTemplateNodeId = " controller ",
                                Title = " Проект PLC ",
                                Path = " \\\\srv\\plc "
                            }
                        },
                        NetworkFileReferences =
                        {
                            new KbObjectTemplateNetworkFileReference
                            {
                                OwnerTemplateNodeId = " cabinet ",
                                Title = " Топология ",
                                Path = " \\\\srv\\topology.png "
                            }
                        },
                        MaintenanceScheduleProfiles =
                        {
                            new KbObjectTemplateMaintenanceScheduleProfile
                            {
                                OwnerTemplateNodeId = " cabinet ",
                                IsIncludedInSchedule = true,
                                To1Hours = 2,
                                To2Hours = -4,
                                To3Hours = 8,
                                YearScheduleEntries =
                                {
                                    new KbMaintenanceYearScheduleEntry { Month = 12, WorkKind = KbMaintenanceWorkKind.To3 },
                                    new KbMaintenanceYearScheduleEntry { Month = 13, WorkKind = KbMaintenanceWorkKind.To1 }
                                }
                            },
                            new KbObjectTemplateMaintenanceScheduleProfile
                            {
                                OwnerTemplateNodeId = " cabinet ",
                                To1Hours = 99
                            }
                        },
                        NetworkInterfaceStubs =
                        {
                            new KbObjectTemplateNetworkInterfaceStub
                            {
                                OwnerTemplateNodeId = " controller ",
                                InterfaceId = " eth0 ",
                                Name = " Ethernet 1 ",
                                IpAddress = " 192.168.1.20 ",
                                Protocol = " Profinet "
                            },
                            new KbObjectTemplateNetworkInterfaceStub
                            {
                                OwnerTemplateNodeId = " missing ",
                                Name = " Игнорируется "
                            }
                        }
                    },
                    new()
                    {
                        TemplateId = " cabinet-template ",
                        DisplayName = " Дубликат id ",
                        RootNode = new KbObjectTemplateNode { Name = "Не должен попасть" }
                    }
                },
                LastWorkshop = "Цех"
            });

        KbObjectTemplate template = Assert.Single(normalized.ObjectTemplates);
        Assert.Equal("cabinet-template", template.TemplateId);
        Assert.Equal("Типовой шкаф", template.DisplayName);
        Assert.Equal("Шкафы", template.Category);
        Assert.Equal("cabinet", template.RootNode.TemplateNodeId);
        Assert.Equal("catalog-cabinet", template.RootNode.CatalogItemId);
        Assert.Equal("Шкаф АСУТП", template.RootNode.Name);
        Assert.Equal(KbNodeType.Cabinet, template.RootNode.NodeType);
        Assert.Equal("Вводной шкаф", template.RootNode.Details.Description);
        Assert.Equal("Машзал", template.RootNode.Details.Location);
        Assert.Equal("INV-001", template.RootNode.Details.InventoryNumber);
        Assert.Equal("192.168.1.10", template.RootNode.Details.IpAddress);
        Assert.Equal("controller", Assert.Single(template.RootNode.Children).TemplateNodeId);

        KbObjectTemplateCompositionEntry compositionEntry = Assert.Single(template.CompositionEntries);
        Assert.Equal("cabinet", compositionEntry.ParentTemplateNodeId);
        Assert.Equal(1, compositionEntry.SlotNumber);
        Assert.Equal(0, compositionEntry.PositionOrder);
        Assert.Equal("CPU", compositionEntry.ComponentType);
        Assert.Equal("S7-1200", compositionEntry.Model);

        Assert.Equal("Схема", Assert.Single(template.DocumentLinks).Title);
        Assert.Equal("Проект PLC", Assert.Single(template.SoftwareRecords).Title);
        Assert.Equal("Топология", Assert.Single(template.NetworkFileReferences).Title);

        KbObjectTemplateMaintenanceScheduleProfile maintenance = Assert.Single(template.MaintenanceScheduleProfiles);
        Assert.True(maintenance.IsIncludedInSchedule);
        Assert.Equal(2, maintenance.To1Hours);
        Assert.Equal(0, maintenance.To2Hours);
        Assert.Equal(8, maintenance.To3Hours);
        Assert.Equal(12, Assert.Single(maintenance.YearScheduleEntries).Month);

        KbObjectTemplateNetworkInterfaceStub networkInterface = Assert.Single(template.NetworkInterfaceStubs);
        Assert.Equal("eth0", networkInterface.InterfaceId);
        Assert.Equal("controller", networkInterface.OwnerTemplateNodeId);
        Assert.Equal("Ethernet 1", networkInterface.Name);
        Assert.Equal("Profinet", networkInterface.Protocol);
    }

    [Fact]
    public void SerializeSnapshot_IncludesObjectTemplates()
    {
        var snapshot = KnowledgeBaseDataService.SerializeSnapshot(
            KnowledgeBaseDataService.CreateDefaultConfig(),
            new Dictionary<string, List<KbNode>>
            {
                ["Цех"] = new()
            },
            compositionRacks: null,
            compositionEntries: null,
            documentLinks: null,
            softwareRecords: null,
            networkFileReferences: null,
            maintenanceScheduleProfiles: null,
            equipmentCatalogItems: null,
            objectTemplates:
            [
                new KbObjectTemplate
                {
                    TemplateId = "template-cabinet",
                    DisplayName = "Шкаф",
                    RootNode = new KbObjectTemplateNode
                    {
                        TemplateNodeId = "root",
                        Name = "Шкаф",
                        NodeType = KbNodeType.Cabinet
                    }
                }
            ],
            currentWorkshop: "Цех",
            includeCurrentWorkshop: true);

        var restored = JsonSerializer.Deserialize<SavedData>(snapshot);

        Assert.NotNull(restored);
        KbObjectTemplate template = Assert.Single(restored!.ObjectTemplates);
        Assert.Equal("template-cabinet", template.TemplateId);
        Assert.Equal("Шкаф", template.RootNode.Name);
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
