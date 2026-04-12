using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.Core.Tests;

public class KnowledgeBaseTreeSearchServiceTests
{
    private readonly KnowledgeBaseTreeSearchService _service = new();

    [Fact]
    public void FindMatches_WhenQueryMatchesNodeName_ReturnsNameMatch()
    {
        var roots = CreateRoots();

        var matches = _service.FindMatches(roots, CreateConfig(), "насос");

        var match = Assert.Single(matches);
        Assert.Equal("имя узла", match.MatchFieldLabel);
        Assert.Equal("Насос подпитки", match.MatchValue);
        Assert.Equal("Линия аммиака / Насос подпитки", match.NodePath);
    }

    [Fact]
    public void FindMatches_WhenQueryMatchesNodePath_ReturnsPathMatch()
    {
        var roots = CreateRoots();

        var matches = _service.FindMatches(roots, CreateConfig(), "линия аммиака /");

        Assert.Equal(3, matches.Count);
        Assert.All(matches, match => Assert.Equal("полный путь", match.MatchFieldLabel));
        Assert.Contains(matches, match => match.NodePath == "Линия аммиака / Насос подпитки");
        Assert.Contains(matches, match => match.NodePath == "Линия аммиака / Шкаф АВР / Модуль AI-01");
    }

    [Fact]
    public void FindMatches_WhenQueryMatchesLevelName_ReturnsLevelMatch()
    {
        var roots = CreateRoots();

        var matches = _service.FindMatches(roots, CreateConfig(), "модули");

        var match = Assert.Single(matches);
        Assert.Equal("имя уровня", match.MatchFieldLabel);
        Assert.Equal("Модули", match.MatchValue);
        Assert.Equal("Линия аммиака / Шкаф АВР / Модуль AI-01", match.NodePath);
    }

    private static KbConfig CreateConfig() =>
        new()
        {
            MaxLevels = 4,
            LevelNames = new List<string> { "Линии", "Щит", "Модули", "Примечание" }
        };

    private static IReadOnlyList<KbNode> CreateRoots()
    {
        return new List<KbNode>
        {
            new()
            {
                Name = "Линия аммиака",
                LevelIndex = 0,
                Children = new List<KbNode>
                {
                    new()
                    {
                        Name = "Насос подпитки",
                        LevelIndex = 1
                    },
                    new()
                    {
                        Name = "Шкаф АВР",
                        LevelIndex = 1,
                        Children = new List<KbNode>
                        {
                            new()
                            {
                                Name = "Модуль AI-01",
                                LevelIndex = 2
                            }
                        }
                    }
                }
            }
        };
    }
}
