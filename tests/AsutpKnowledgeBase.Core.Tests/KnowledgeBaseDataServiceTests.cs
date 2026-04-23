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
