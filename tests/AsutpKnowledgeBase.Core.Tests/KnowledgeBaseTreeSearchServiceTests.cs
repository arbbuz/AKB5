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
    public void FindMatches_WhenQueryMatchesAnotherNodeName_ReturnsNameMatch()
    {
        var roots = CreateRoots();

        var matches = _service.FindMatches(roots, CreateConfig(), "мод");

        var match = Assert.Single(matches);
        Assert.Equal("имя узла", match.MatchFieldLabel);
        Assert.Equal("Модуль AI-01", match.MatchValue);
        Assert.Equal("Линия аммиака / Шкаф АВР / Модуль AI-01", match.NodePath);
    }

    [Fact]
    public void FindMatches_WhenQueryMatchesOnlyPath_ReturnsNoMatches()
    {
        var roots = CreateRoots();

        var matches = _service.FindMatches(roots, CreateConfig(), "линия аммиака /");

        Assert.Empty(matches);
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
