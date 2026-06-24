using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.Core.Tests;

public class KnowledgeBaseTreeSearchServiceTests
{
    private readonly KnowledgeBaseTreeSearchService _service = new();

    [Fact]
    public void FindMatches_WhenTreeScopeMatchesLevel3NodeName_PrefersFirstAvailableTab()
    {
        var fixture = CreateFixture();

        var match = Assert.Single(Search(fixture, "ШКМ", KnowledgeBaseSearchScope.Tree));

        Assert.Equal(KnowledgeBaseSearchDomain.Tree, match.Domain);
        Assert.Equal(KnowledgeBaseNodeWorkspaceTabKind.Composition, match.PreferredTabKind);
        Assert.Equal("имя узла", match.MatchFieldLabel);
        Assert.Equal("ШКМ1", match.MatchValue);
        Assert.Equal("Медное отделение / АСУ котельной / ШКМ1", match.NodePath);
        AssertTarget(
            match,
            KnowledgeBaseSearchTargetKind.Node,
            "cabinet-1",
            "cabinet-1",
            "tree.name");
    }

    [Fact]
    public void FindMatches_WhenTreeScopeMatchesNodeType_ReturnsNoMatch()
    {
        var fixture = CreateFixture();

        var matches = Search(fixture, "система", KnowledgeBaseSearchScope.Tree);

        Assert.Empty(matches);
    }

    [Fact]
    public void FindMatches_WhenCardScopeMatchesVisibleDescription_ReturnsCardMatch()
    {
        var fixture = CreateFixture();

        var match = Assert.Single(Search(fixture, "система управления", KnowledgeBaseSearchScope.Card));

        Assert.Equal(KnowledgeBaseSearchDomain.Card, match.Domain);
        Assert.Equal(KnowledgeBaseNodeWorkspaceTabKind.Info, match.PreferredTabKind);
        Assert.Equal("описание", match.MatchFieldLabel);
        Assert.Equal("АСУ котельной", match.Node.Name);
        AssertTarget(
            match,
            KnowledgeBaseSearchTargetKind.Card,
            "system-1",
            "system-1",
            "card.description");
    }

    [Fact]
    public void FindMatches_WhenCardScopeMatchesLevel3Description_ReturnsNoMatch()
    {
        var fixture = CreateFixture();

        var matches = Search(fixture, "шкаф управления", KnowledgeBaseSearchScope.Card);

        Assert.Empty(matches);
    }

    [Fact]
    public void FindMatches_WhenCardScopeMatchesInventoryNumber_ReturnsCardTechnicalFieldMatch()
    {
        var fixture = CreateFixture();

        var match = Assert.Single(Search(fixture, "INV-42", KnowledgeBaseSearchScope.Card));

        Assert.Equal(KnowledgeBaseSearchDomain.Card, match.Domain);
        Assert.Equal("инвентарный номер", match.MatchFieldLabel);
        Assert.Equal("АСУ котельной", match.Node.Name);
        AssertTarget(
            match,
            KnowledgeBaseSearchTargetKind.Card,
            "system-1",
            "system-1",
            "card.inventoryNumber");
    }

    [Fact]
    public void FindMatches_WhenCompositionScopeMatchesOrderNumber_ReturnsCompositionMatch()
    {
        var fixture = CreateFixture();

        var match = Assert.Single(Search(fixture, "6ES7312", KnowledgeBaseSearchScope.Composition));

        Assert.Equal(KnowledgeBaseSearchDomain.Composition, match.Domain);
        Assert.Equal(KnowledgeBaseNodeWorkspaceTabKind.Composition, match.PreferredTabKind);
        Assert.Equal("заказной номер", match.MatchFieldLabel);
        Assert.Equal("ШКМ1", match.Node.Name);
        AssertTarget(
            match,
            KnowledgeBaseSearchTargetKind.CompositionEntry,
            "cabinet-1",
            "slot-1",
            "composition.orderNumber");
    }

    [Fact]
    public void FindMatches_WhenCompositionScopeMatchesModel_ReturnsNoMatch()
    {
        var fixture = CreateFixture();

        var matches = Search(fixture, "CPU 315", KnowledgeBaseSearchScope.Composition);

        Assert.Empty(matches);
    }

    [Fact]
    public void FindMatches_WhenCompositionScopeMatchesInterfaceRows_ReturnsNoMatch()
    {
        var fixture = CreateFixture();

        var matches = Search(fixture, "X1 P2", KnowledgeBaseSearchScope.Composition);

        Assert.Empty(matches);
    }

    [Fact]
    public void FindMatches_WhenAdditionalEquipmentScopeMatchesOrderNumber_ReturnsAdditionalEquipmentMatch()
    {
        var fixture = CreateFixture();

        var match = Assert.Single(Search(fixture, "6ES7972", KnowledgeBaseSearchScope.AdditionalEquipment));

        Assert.Equal(KnowledgeBaseSearchDomain.AdditionalEquipment, match.Domain);
        Assert.Equal(KnowledgeBaseNodeWorkspaceTabKind.AdditionalEquipment, match.PreferredTabKind);
        Assert.Equal("заказной номер", match.MatchFieldLabel);
        Assert.Equal("ШКМ1", match.Node.Name);
        AssertTarget(
            match,
            KnowledgeBaseSearchTargetKind.AdditionalEquipmentEntry,
            "cabinet-1",
            "additional-1",
            "additionalEquipment.orderNumber");
    }

    [Fact]
    public void FindMatches_WhenAdditionalEquipmentScopeMatchesNotes_ReturnsAdditionalEquipmentTarget()
    {
        var fixture = CreateFixture();

        var match = Assert.Single(Search(fixture, "Profibus spare", KnowledgeBaseSearchScope.AdditionalEquipment));

        Assert.Equal(KnowledgeBaseSearchDomain.AdditionalEquipment, match.Domain);
        Assert.Equal("примечание", match.MatchFieldLabel);
        AssertTarget(
            match,
            KnowledgeBaseSearchTargetKind.AdditionalEquipmentEntry,
            "cabinet-1",
            "additional-1",
            "additionalEquipment.notes");
    }

    [Fact]
    public void FindMatches_WhenAdditionalEquipmentScopeMatchesIpAddress_ReturnsNoMatch()
    {
        var fixture = CreateFixture();

        var matches = Search(fixture, "192.168.100.20", KnowledgeBaseSearchScope.AdditionalEquipment);

        Assert.Empty(matches);
    }

    [Fact]
    public void FindMatches_WhenAdditionalEquipmentScopeMatchesInterfaceRows_ReturnsNoMatch()
    {
        var fixture = CreateFixture();

        var matches = Search(fixture, "RS485-A", KnowledgeBaseSearchScope.AdditionalEquipment);

        Assert.Empty(matches);
    }

    [Fact]
    public void FindMatches_WhenDocsAndSoftwareScopeMatchesAddedDate_ReturnsSoftwareMatch()
    {
        var fixture = CreateFixture();

        var match = Assert.Single(Search(fixture, "27.04.2026", KnowledgeBaseSearchScope.DocsAndSoftware));

        Assert.Equal(KnowledgeBaseSearchDomain.DocsAndSoftware, match.Domain);
        Assert.Equal(KnowledgeBaseNodeWorkspaceTabKind.DocsAndSoftware, match.PreferredTabKind);
        Assert.Equal("дата добавления ПО", match.MatchFieldLabel);
        Assert.Equal("27.04.2026", match.MatchValue);
        Assert.Equal("АСУ котельной", match.Node.Name);
        AssertTarget(
            match,
            KnowledgeBaseSearchTargetKind.Software,
            "system-1",
            "soft-1",
            "software.addedAt");
    }

    [Fact]
    public void FindMatches_WhenDocsAndSoftwareScopeMatchesDocumentUpdatedDate_ReturnsDocumentTarget()
    {
        var fixture = CreateFixture();

        var match = Assert.Single(Search(fixture, "01.04.2026", KnowledgeBaseSearchScope.DocsAndSoftware));

        Assert.Equal(KnowledgeBaseSearchDomain.DocsAndSoftware, match.Domain);
        Assert.Equal("дата обновления документа", match.MatchFieldLabel);
        AssertTarget(
            match,
            KnowledgeBaseSearchTargetKind.Document,
            "system-1",
            "doc-1",
            "document.updatedAt");
    }

    [Fact]
    public void FindMatches_WhenDocsAndSoftwareScopeMatchesSoftwareNotes_ReturnsNoMatch()
    {
        var fixture = CreateFixture();

        var matches = Search(fixture, "nightly backup", KnowledgeBaseSearchScope.DocsAndSoftware);

        Assert.Empty(matches);
    }

    [Fact]
    public void FindMatches_WhenNetworkScopeMatchesAdditionalIp_ReturnsNetworkMatch()
    {
        var fixture = CreateFixture();

        var match = Assert.Single(Search(fixture, "10.10.10.11", KnowledgeBaseSearchScope.Network));

        Assert.Equal(KnowledgeBaseSearchDomain.Network, match.Domain);
        Assert.Equal(KnowledgeBaseNodeWorkspaceTabKind.Network, match.PreferredTabKind);
        Assert.Equal("доп. IP", match.MatchFieldLabel);
        Assert.Equal("АСУ котельной", match.Node.Name);
        AssertTarget(
            match,
            KnowledgeBaseSearchTargetKind.NetworkElement,
            "system-1",
            "plc-1",
            "networkElement.additionalIpAddress");
    }

    [Fact]
    public void FindMatches_WhenNetworkScopeMatchesHiddenElementId_ReturnsNoMatch()
    {
        var fixture = CreateFixture();

        var matches = Search(fixture, "plc-1", KnowledgeBaseSearchScope.Network);

        Assert.Empty(matches);
    }

    [Fact]
    public void FindMatches_WhenNetworkScopeMatchesHiddenLinkId_ReturnsNoMatch()
    {
        var fixture = CreateFixture();

        var matches = Search(fixture, "link-1", KnowledgeBaseSearchScope.Network);

        Assert.Empty(matches);
    }

    [Fact]
    public void FindMatches_WhenNetworkScopeMatchesHiddenLinkLabel_ReturnsNoMatch()
    {
        var fixture = CreateFixture();

        var matches = Search(fixture, "hidden-link-24", KnowledgeBaseSearchScope.Network);

        Assert.Empty(matches);
    }

    [Fact]
    public void FindMatches_WhenMaintenanceScopeMatchesYearScheduleMonth_ReturnsNoMatch()
    {
        var fixture = CreateFixture();

        var matches = Search(fixture, "март", KnowledgeBaseSearchScope.Maintenance);

        Assert.Empty(matches);
    }

    [Fact]
    public void FindMatches_WhenMaintenanceScopeMatchesHiddenProfileId_ReturnsNoMatch()
    {
        var fixture = CreateFixture();

        var matches = Search(fixture, "maintenance-hidden-24", KnowledgeBaseSearchScope.Maintenance);

        Assert.Empty(matches);
    }

    [Fact]
    public void FindMatches_WhenMaintenanceScopeMatchesVisibleTo2Hours_ReturnsMaintenanceProfileTarget()
    {
        var fixture = CreateFixture();

        var match = Assert.Single(Search(fixture, "4 ч", KnowledgeBaseSearchScope.Maintenance));

        Assert.Equal(KnowledgeBaseSearchDomain.Maintenance, match.Domain);
        AssertTarget(
            match,
            KnowledgeBaseSearchTargetKind.MaintenanceProfile,
            "cabinet-1",
            "maintenance-hidden-24",
            "maintenance.to2Hours");
    }

    [Fact]
    public void FindMatches_WhenMaintenanceScopeMatchesVisibleYearScheduleSummary_ReturnsMaintenanceProfileTarget()
    {
        var fixture = CreateFixture();

        var match = Assert.Single(Search(fixture, "Годовой план вручную", KnowledgeBaseSearchScope.Maintenance));

        Assert.Equal(KnowledgeBaseSearchDomain.Maintenance, match.Domain);
        Assert.Equal("годовое размещение", match.MatchFieldLabel);
        AssertTarget(
            match,
            KnowledgeBaseSearchTargetKind.MaintenanceProfile,
            "cabinet-1",
            "maintenance-hidden-24",
            "maintenance.yearSchedule.summary");
    }

    [Fact]
    public void FindMatches_WhenSearchTextMatchesOnlyHiddenNumericValues_ReturnsNoMatch()
    {
        var fixture = CreateFixture();

        var matches = Search(fixture, "24", KnowledgeBaseSearchScope.All);

        Assert.Empty(matches);
    }

    [Fact]
    public void FindMatches_WhenAllScopeMatchesAcrossDomains_ReturnsDifferentDomainMatches()
    {
        var fixture = CreateFixture();

        var matches = Search(fixture, "авр", KnowledgeBaseSearchScope.All);

        Assert.Contains(matches, match => match.Domain == KnowledgeBaseSearchDomain.DocsAndSoftware);
        Assert.Contains(matches, match => match.Domain == KnowledgeBaseSearchDomain.Network);
    }

    [Fact]
    public void FindMatches_WhenTreeScopeReturnsMultipleNodes_KeepsDisplaySortedOrder()
    {
        var roots = new List<KbNode>
        {
            new() { NodeId = "node-10", Name = "Node 10", LevelIndex = 0 },
            new() { NodeId = "node-2", Name = "node 2", LevelIndex = 0 },
            new() { NodeId = "node-1", Name = "Node 1", LevelIndex = 0 }
        };

        var matches = _service.FindMatches(
            roots,
            CreateConfig(),
            "node",
            KnowledgeBaseSearchScope.Tree);

        Assert.Equal(
            new[] { "Node 1", "node 2", "Node 10" },
            matches.Select(static match => match.MatchValue).ToArray());
    }

    private IReadOnlyList<KnowledgeBaseTreeSearchMatch> Search(
        SearchFixture fixture,
        string searchText,
        KnowledgeBaseSearchScope scope) =>
        _service.FindMatches(
            fixture.Roots,
            CreateConfig(),
            searchText,
            scope,
            fixture.CompositionEntries,
            fixture.DocumentLinks,
            fixture.SoftwareRecords,
            fixture.MaintenanceScheduleProfiles);

    private static void AssertTarget(
        KnowledgeBaseTreeSearchMatch match,
        KnowledgeBaseSearchTargetKind kind,
        string ownerNodeId,
        string entityId,
        string fieldKey,
        string rowKey = "")
    {
        Assert.Equal(kind, match.Target.Kind);
        Assert.Equal(ownerNodeId, match.Target.OwnerNodeId);
        Assert.Equal(entityId, match.Target.EntityId);
        Assert.Equal(fieldKey, match.Target.FieldKey);
        Assert.Equal(rowKey, match.Target.RowKey);
    }

    private static KbConfig CreateConfig() =>
        new()
        {
            MaxLevels = 4,
            LevelNames = new List<string> { "Уровень 1", "Уровень 2", "Уровень 3", "Уровень 4" }
        };

    private static SearchFixture CreateFixture()
    {
        var cabinet = new KbNode
        {
            NodeId = "cabinet-1",
            Name = "ШКМ1",
            LevelIndex = 2,
            NodeType = KbNodeType.Cabinet,
            Details = new KbNodeDetails
            {
                Description = "Шкаф управления котельной"
            }
        };

        var system = new KbNode
        {
            NodeId = "system-1",
            Name = "АСУ котельной",
            LevelIndex = 1,
            NodeType = KbNodeType.System,
            Details = new KbNodeDetails
            {
                Description = "Система управления котельной",
                InventoryNumber = "INV-42",
                NetworkTopology = new KbNetworkTopology
                {
                    Elements =
                    {
                        new()
                        {
                            ElementId = "plc-1",
                            Kind = KbNetworkElementKind.Plc,
                            Name = "PLC-AVR",
                            IpAddress = "10.10.10.10",
                            AdditionalIpAddresses = { "10.10.10.11" }
                        },
                        new()
                        {
                            ElementId = "external-1",
                            Kind = KbNetworkElementKind.ExternalConnection,
                            Name = "АВР верхнего уровня"
                        }
                    },
                    Links =
                    {
                        new()
                        {
                            LinkId = "link-1",
                            FromElementId = "plc-1",
                            ToElementId = "external-1",
                            Kind = KbNetworkLinkKind.FiberProfibus,
                            Label = "hidden-link-24"
                        }
                    }
                }
            },
            Children = new List<KbNode> { cabinet }
        };

        var roots = new List<KbNode>
        {
            new()
            {
                NodeId = "department-1",
                Name = "Медное отделение",
                LevelIndex = 0,
                NodeType = KbNodeType.Department,
                Children = new List<KbNode> { system }
            }
        };

        return new SearchFixture
        {
            Roots = roots,
            CompositionEntries = new List<KbCompositionEntry>
            {
                new()
                {
                    EntryId = "slot-1",
                    ParentNodeId = cabinet.NodeId,
                    RackNumber = 0,
                    SlotNumber = 2,
                    PositionOrder = 1,
                    ComponentType = "CPU",
                    Model = "CPU 315-2 PN/DP",
                    OrderNumber = "6ES7312-1AE13-0AB0",
                    Firmware = "V3.3",
                    MpiDpPnAddress = "PN/IE 10.0.0.10",
                    InputAddress = "I0.0",
                    OutputAddress = "Q0.0",
                    Comment = "Основная стойка",
                    InterfaceRows = "X1 P1; X1 P2",
                    IpAddress = "192.168.100.10",
                    LastCalibrationAt = new DateTime(2026, 5, 1),
                    NextCalibrationAt = new DateTime(2027, 5, 1),
                    Notes = "Основной контроллер"
                },
                new()
                {
                    EntryId = "additional-1",
                    ParentNodeId = cabinet.NodeId,
                    PositionOrder = 2,
                    ComponentType = "Коннектор",
                    Model = "RS485 repeater",
                    OrderNumber = "6ES7972-0BA52-0XA0",
                    Firmware = "HW2",
                    MpiDpPnAddress = "DP 12",
                    InputAddress = "I10.0",
                    OutputAddress = "Q10.0",
                    Comment = "Запасной порт",
                    InterfaceRows = "RS485-A",
                    IpAddress = "192.168.100.20",
                    LastCalibrationAt = new DateTime(2026, 6, 1),
                    NextCalibrationAt = new DateTime(2027, 6, 1),
                    Notes = "Profibus spare"
                }
            },
            DocumentLinks = new List<KbDocumentLink>
            {
                new()
                {
                    DocumentId = "doc-1",
                    OwnerNodeId = system.NodeId,
                    Kind = KbDocumentKind.SchemeLink,
                    Title = "Схема АВР",
                    Path = @"\\server\docs\avr-scheme.pdf",
                    UpdatedAt = new DateTime(2026, 4, 1)
                }
            },
            SoftwareRecords = new List<KbSoftwareRecord>
            {
                new()
                {
                    SoftwareId = "soft-1",
                    OwnerNodeId = system.NodeId,
                    Title = "TIA Portal AVR",
                    Path = @"\\server\software\avr",
                    AddedAt = new DateTime(2026, 4, 27),
                    LastChangedAt = new DateTime(2026, 4, 28),
                    LastBackupAt = new DateTime(2026, 4, 29),
                    Notes = "nightly backup"
                }
            },
            MaintenanceScheduleProfiles = new List<KbMaintenanceScheduleProfile>
            {
                new()
                {
                    MaintenanceProfileId = "maintenance-hidden-24",
                    OwnerNodeId = cabinet.NodeId,
                    IsIncludedInSchedule = true,
                    To1Hours = 2,
                    To2Hours = 4,
                    To3Hours = 8,
                    YearScheduleEntries =
                    {
                        new() { Month = 3, WorkKind = KbMaintenanceWorkKind.To2, Hours = 4 }
                    }
                }
            }
        };
    }

    private sealed class SearchFixture
    {
        public IReadOnlyList<KbNode> Roots { get; init; } = Array.Empty<KbNode>();

        public IReadOnlyList<KbCompositionEntry> CompositionEntries { get; init; } =
            Array.Empty<KbCompositionEntry>();

        public IReadOnlyList<KbDocumentLink> DocumentLinks { get; init; } =
            Array.Empty<KbDocumentLink>();

        public IReadOnlyList<KbSoftwareRecord> SoftwareRecords { get; init; } =
            Array.Empty<KbSoftwareRecord>();

        public IReadOnlyList<KbMaintenanceScheduleProfile> MaintenanceScheduleProfiles { get; init; } =
            Array.Empty<KbMaintenanceScheduleProfile>();
    }
}
