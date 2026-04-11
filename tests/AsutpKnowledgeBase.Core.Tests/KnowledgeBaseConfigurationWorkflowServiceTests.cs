using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.Core.Tests;

public class KnowledgeBaseConfigurationWorkflowServiceTests
{
    private readonly KnowledgeBaseConfigurationWorkflowService _service = new();

    [Fact]
    public void ValidateAndNormalize_NormalizesConfigOnSuccess()
    {
        var result = _service.ValidateAndNormalize(
            new KbConfig
            {
                MaxLevels = 2,
                LevelNames = new List<string> { "  Цех  ", "" }
            },
            new Dictionary<string, List<KbNode>>
            {
                ["Цех 1"] =
                [
                    new KbNode { Name = "Линия", LevelIndex = 0 }
                ]
            });

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Config.MaxLevels);
        Assert.Equal("Цех", result.Config.LevelNames[0]);
        Assert.Equal("Отделение", result.Config.LevelNames[1]);
    }

    [Fact]
    public void ValidateAndNormalize_RejectsReducingBelowUsedLevel()
    {
        var result = _service.ValidateAndNormalize(
            new KbConfig
            {
                MaxLevels = 2,
                LevelNames = new List<string> { "Цех", "Отделение" }
            },
            new Dictionary<string, List<KbNode>>
            {
                ["Цех 1"] =
                [
                    new KbNode
                    {
                        Name = "Root",
                        LevelIndex = 0,
                        Children =
                        [
                            new KbNode
                            {
                                Name = "Child",
                                LevelIndex = 1,
                                Children =
                                [
                                    new KbNode { Name = "Leaf", LevelIndex = 2 }
                                ]
                            }
                        ]
                    }
                ]
            });

        Assert.False(result.IsSuccess);
        Assert.Equal(2, result.MaxUsedLevel);
        Assert.Equal(
            "Нельзя уменьшить количество уровней до 2. В базе уже используется уровень 3.",
            result.ErrorMessage);
    }

    [Fact]
    public void ValidateAndNormalize_AllowsEmptyWorkshopSet()
    {
        var result = _service.ValidateAndNormalize(
            new KbConfig
            {
                MaxLevels = 1,
                LevelNames = new List<string> { "Цех" }
            },
            new Dictionary<string, List<KbNode>>());

        Assert.True(result.IsSuccess);
        Assert.Equal(-1, result.MaxUsedLevel);
    }
}
