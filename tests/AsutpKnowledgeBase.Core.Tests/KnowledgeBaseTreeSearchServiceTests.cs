using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.Core.Tests;

public class KnowledgeBaseTreeSearchServiceTests
{
    private readonly KnowledgeBaseTreeSearchService _service = new();

    [Fact]
    public void FindMatches_WhenTreeScopeMatchesNodeName_ReturnsTreeMatch()
    {
        var fixture = CreateFixture();

        var matches = _service.FindMatches(
            fixture.Roots,
            CreateConfig(),
            "насос",
            KnowledgeBaseSearchScope.Tree,
            fixture.CompositionEntries,
            fixture.DocumentLinks,
            fixture.SoftwareRecords);

        var match = Assert.Single(matches);
        Assert.Equal(KnowledgeBaseSearchDomain.Tree, match.Domain);
        Assert.Equal(KnowledgeBaseNodeWorkspaceTabKind.Info, match.PreferredTabKind);
        Assert.Equal("имя узла", match.MatchFieldLabel);
        Assert.Equal("Насос подпитки", match.MatchValue);
        Assert.Equal("Линия аммиака / Насос подпитки", match.NodePath);
    }

    [Fact]
    public void FindMatches_WhenCardScopeMatchesIpAddress_ReturnsCardMatch()
    {
        var fixture = CreateFixture();

        var matches = _service.FindMatches(
            fixture.Roots,
            CreateConfig(),
            "192.168.0.10",
            KnowledgeBaseSearchScope.Card,
            fixture.CompositionEntries,
            fixture.DocumentLinks,
            fixture.SoftwareRecords);

        var match = Assert.Single(matches);
        Assert.Equal(KnowledgeBaseSearchDomain.Card, match.Domain);
        Assert.Equal(KnowledgeBaseNodeWorkspaceTabKind.Info, match.PreferredTabKind);
        Assert.Equal("IP-адрес", match.MatchFieldLabel);
        Assert.Equal("Шкаф АВР", match.Node.Name);
    }

    [Fact]
    public void FindMatches_WhenCardScopeMatchesInventoryNumber_ReturnsCardMatch()
    {
        var fixture = CreateFixture();

        var matches = _service.FindMatches(
            fixture.Roots,
            CreateConfig(),
            "inv-42",
            KnowledgeBaseSearchScope.Card,
            fixture.CompositionEntries,
            fixture.DocumentLinks,
            fixture.SoftwareRecords);

        var match = Assert.Single(matches);
        Assert.Equal(KnowledgeBaseSearchDomain.Card, match.Domain);
        Assert.Equal(KnowledgeBaseNodeWorkspaceTabKind.Info, match.PreferredTabKind);
        Assert.Equal("инвентарный номер", match.MatchFieldLabel);
        Assert.Equal("Линия аммиака", match.Node.Name);
    }

    [Fact]
    public void FindMatches_WhenCompositionScopeMatchesModel_ReturnsOwningNodeMatch()
    {
        var fixture = CreateFixture();

        var matches = _service.FindMatches(
            fixture.Roots,
            CreateConfig(),
            "AI-8",
            KnowledgeBaseSearchScope.Composition,
            fixture.CompositionEntries,
            fixture.DocumentLinks,
            fixture.SoftwareRecords);

        var match = Assert.Single(matches);
        Assert.Equal(KnowledgeBaseSearchDomain.Composition, match.Domain);
        Assert.Equal(KnowledgeBaseNodeWorkspaceTabKind.Composition, match.PreferredTabKind);
        Assert.Equal("модель", match.MatchFieldLabel);
        Assert.Equal("Siemens AI-8", match.MatchValue);
        Assert.Equal("Шкаф АВР", match.Node.Name);
    }

    [Fact]
    public void FindMatches_WhenDocsAndSoftwareScopeMatchesAddedDate_ReturnsSoftwareMatch()
    {
        var fixture = CreateFixture();

        var matches = _service.FindMatches(
            fixture.Roots,
            CreateConfig(),
            "2026-04-27",
            KnowledgeBaseSearchScope.DocsAndSoftware,
            fixture.CompositionEntries,
            fixture.DocumentLinks,
            fixture.SoftwareRecords);

        var match = Assert.Single(matches);
        Assert.Equal(KnowledgeBaseSearchDomain.DocsAndSoftware, match.Domain);
        Assert.Equal(KnowledgeBaseNodeWorkspaceTabKind.DocsAndSoftware, match.PreferredTabKind);
        Assert.Equal("дата добавления ПО", match.MatchFieldLabel);
        Assert.Equal("2026-04-27", match.MatchValue);
        Assert.Equal("Шкаф АВР", match.Node.Name);
    }

    [Fact]
    public void FindMatches_WhenAllScopeMatchesAcrossDomains_ReturnsDifferentDomainMatches()
    {
        var fixture = CreateFixture();

        var matches = _service.FindMatches(
            fixture.Roots,
            CreateConfig(),
            "авр",
            KnowledgeBaseSearchScope.All,
            fixture.CompositionEntries,
            fixture.DocumentLinks,
            fixture.SoftwareRecords);

        Assert.Contains(matches, match => match.Domain == KnowledgeBaseSearchDomain.Tree);
        Assert.Contains(matches, match => match.Domain == KnowledgeBaseSearchDomain.DocsAndSoftware);
        Assert.All(matches, match => Assert.Equal("Шкаф АВР", match.Node.Name));
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

    private static KbConfig CreateConfig() =>
        new()
        {
            MaxLevels = 4,
            LevelNames = new List<string> { "Линии", "Шкафы", "Модули", "Примечания" }
        };

    private static SearchFixture CreateFixture()
    {
        var pump = new KbNode
        {
            NodeId = "pump-1",
            Name = "Насос подпитки",
            LevelIndex = 1,
            NodeType = KbNodeType.Device,
            Details = new KbNodeDetails
            {
                Description = "Резервный насос подпитки",
                Location = "Машзал",
                IpAddress = "10.20.30.40"
            }
        };

        var cabinet = new KbNode
        {
            NodeId = "cabinet-1",
            Name = "Шкаф АВР",
            LevelIndex = 1,
            NodeType = KbNodeType.Cabinet,
            Details = new KbNodeDetails
            {
                Description = "Основной шкаф управления",
                Location = "Щитовая 1",
                IpAddress = "192.168.0.10",
                SchemaLink = @"\\server\schemas\avr-main.pdf"
            },
            Children = new List<KbNode>
            {
                new()
                {
                    NodeId = "module-1",
                    Name = "Модуль AI-01",
                    LevelIndex = 2,
                    NodeType = KbNodeType.Module
                }
            }
        };

        var roots = new List<KbNode>
        {
            new()
            {
                NodeId = "line-1",
                Name = "Линия аммиака",
                LevelIndex = 0,
                NodeType = KbNodeType.System,
                Details = new KbNodeDetails
                {
                    InventoryNumber = "INV-42"
                },
                Children = new List<KbNode> { pump, cabinet }
            }
        };

        return new SearchFixture
        {
            Roots = roots,
            CompositionEntries = new List<KbCompositionEntry>
            {
                new()
                {
                    EntryId = "comp-1",
                    ParentNodeId = cabinet.NodeId,
                    SlotNumber = 1,
                    PositionOrder = 1,
                    ComponentType = "AI",
                    Model = "Siemens AI-8",
                    IpAddress = "192.168.0.101",
                    Notes = "Датчик давления"
                },
                new()
                {
                    EntryId = "comp-2",
                    ParentNodeId = cabinet.NodeId,
                    PositionOrder = 2,
                    ComponentType = "PSU",
                    Model = "Power Supply 24V",
                    Notes = "Питание шкафа"
                }
            },
            DocumentLinks = new List<KbDocumentLink>
            {
                new()
                {
                    DocumentId = "doc-1",
                    OwnerNodeId = cabinet.NodeId,
                    Kind = KbDocumentKind.SchemeLink,
                    Title = "Схема шкафа АВР",
                    Path = @"\\server\docs\avr-scheme.pdf"
                },
                new()
                {
                    DocumentId = "doc-2",
                    OwnerNodeId = cabinet.NodeId,
                    Kind = KbDocumentKind.Manual,
                    Title = "Инструкция АВР",
                    Path = @"\\server\docs\avr-manual.docx"
                }
            },
            SoftwareRecords = new List<KbSoftwareRecord>
            {
                new()
                {
                    SoftwareId = "soft-1",
                    OwnerNodeId = cabinet.NodeId,
                    Title = "TIA Portal AVR",
                    Path = @"\\server\software\avr",
                    AddedAt = new DateTime(2026, 4, 27)
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
    }
}
