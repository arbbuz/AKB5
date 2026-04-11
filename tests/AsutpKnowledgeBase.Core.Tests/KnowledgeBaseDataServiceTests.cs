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
    public void ResolveWorkshop_UsesPreferredWorkshopWhenItExists()
    {
        var workshops = new Dictionary<string, List<KbNode>>
        {
            ["Первый"] = new(),
            ["Второй"] = new()
        };

        Assert.Equal("Второй", KnowledgeBaseDataService.ResolveWorkshop(workshops, " Второй "));
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
}
