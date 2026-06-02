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
    }

    [Fact]
    public void FindMatches_WhenCardScopeMatchesLevel3Description_DoesNotPreferHiddenInfoTab()
    {
        var fixture = CreateFixture();

        var match = Assert.Single(Search(fixture, "шкаф управления", KnowledgeBaseSearchScope.Card));

        Assert.Equal(KnowledgeBaseSearchDomain.Card, match.Domain);
        Assert.Equal(KnowledgeBaseNodeWorkspaceTabKind.Composition, match.PreferredTabKind);
        Assert.Equal("описание", match.MatchFieldLabel);
        Assert.Equal("ШКМ1", match.Node.Name);
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
    }

    [Fact]
    public void FindMatches_WhenMaintenanceScopeMatchesYearScheduleMonth_ReturnsMaintenanceMatch()
    {
        var fixture = CreateFixture();

        var matches = Search(fixture, "март", KnowledgeBaseSearchScope.Maintenance);
        var match = Assert.Single(matches, static match => match.MatchFieldLabel == "месяц ТО");

        Assert.Equal(KnowledgeBaseSearchDomain.Maintenance, match.Domain);
        Assert.Equal(KnowledgeBaseNodeWorkspaceTabKind.Maintenance, match.PreferredTabKind);
        Assert.Equal("месяц ТО", match.MatchFieldLabel);
        Assert.Equal("ШКМ1", match.Node.Name);
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
                            Label = "АВР"
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
                    OrderNumber = "6ES7312-1AE13-0AB0"
                },
                new()
                {
                    EntryId = "additional-1",
                    ParentNodeId = cabinet.NodeId,
                    PositionOrder = 2,
                    ComponentType = "Коннектор",
                    OrderNumber = "6ES7972-0BA52-0XA0",
                    Notes = "Profibus"
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
                    Path = @"\\server\docs\avr-scheme.pdf"
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
                    AddedAt = new DateTime(2026, 4, 27)
                }
            },
            MaintenanceScheduleProfiles = new List<KbMaintenanceScheduleProfile>
            {
                new()
                {
                    MaintenanceProfileId = "maintenance-1",
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
