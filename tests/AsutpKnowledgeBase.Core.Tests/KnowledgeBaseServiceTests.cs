using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.Core.Tests;

public class KnowledgeBaseServiceTests
{
    [Fact]
    public void AddRootNode_ReindexesEntireSubtree()
    {
        var workshops = new Dictionary<string, List<KbNode>>();
        var service = new KnowledgeBaseService(
            new KbConfig { MaxLevels = 5, LevelNames = new List<string>() },
            workshops);

        var root = new KbNode
        {
            Name = "Корень",
            LevelIndex = 99,
            Children =
            {
                new KbNode
                {
                    Name = "Дочерний",
                    LevelIndex = 99,
                    Children =
                    {
                        new KbNode
                        {
                            Name = "Лист",
                            LevelIndex = 99
                        }
                    }
                }
            }
        };

        service.AddRootNode("Цех 1", root);

        var savedRoot = Assert.Single(workshops["Цех 1"]);
        Assert.Equal(0, savedRoot.LevelIndex);
        Assert.Equal(1, Assert.Single(savedRoot.Children).LevelIndex);
        Assert.Equal(2, Assert.Single(savedRoot.Children[0].Children).LevelIndex);
    }

    [Fact]
    public void CanAttachSubtree_RespectsMaxLevels()
    {
        var service = new KnowledgeBaseService(
            new KbConfig { MaxLevels = 3, LevelNames = new List<string>() },
            new Dictionary<string, List<KbNode>>());

        var parent = new KbNode { Name = "Щит", LevelIndex = 1 };
        var subtree = new KbNode
        {
            Name = "Поддерево",
            Children =
            {
                new KbNode
                {
                    Name = "Узел 2",
                    Children =
                    {
                        new KbNode { Name = "Узел 3" }
                    }
                }
            }
        };

        Assert.True(service.CanAttachSubtree(null, subtree));
        Assert.False(service.CanAttachSubtree(parent, subtree));
    }

    [Fact]
    public void DeleteNode_RemovesNestedNode()
    {
        var child = new KbNode { Name = "Контроллер", LevelIndex = 1 };
        var root = new KbNode
        {
            Name = "Линия",
            LevelIndex = 0,
            Children = { child }
        };

        var workshops = new Dictionary<string, List<KbNode>>
        {
            ["Цех 1"] = new List<KbNode> { root }
        };

        var service = new KnowledgeBaseService(
            new KbConfig { MaxLevels = 4, LevelNames = new List<string>() },
            workshops);

        Assert.True(service.DeleteNode("Цех 1", child));
        Assert.Empty(root.Children);
    }

    [Fact]
    public void CloneNode_CreatesDeepCopy()
    {
        var service = new KnowledgeBaseService(
            new KbConfig { MaxLevels = 4, LevelNames = new List<string>() },
            new Dictionary<string, List<KbNode>>());

        var source = new KbNode
        {
            Name = "Источник",
            LevelIndex = 0,
            Details = new KbNodeDetails
            {
                Description = "Исходное описание",
                PhotoPath = @"\\server\images\source.jpg"
            },
            Children = { new KbNode { Name = "Ребенок", LevelIndex = 1 } }
        };

        var clone = service.CloneNode(source);
        clone.Name = "Копия";
        clone.Details.Description = "Измененное описание";
        clone.Children[0].Name = "Измененный ребенок";

        Assert.NotSame(source, clone);
        Assert.Equal("Источник", source.Name);
        Assert.Equal("Исходное описание", source.Details.Description);
        Assert.Equal("Ребенок", source.Children[0].Name);
    }

    [Fact]
    public void ReindexSubtree_ClearsTechnicalFieldsForUpperLevels()
    {
        var service = new KnowledgeBaseService(
            new KbConfig { MaxLevels = 5, LevelNames = new List<string>() },
            new Dictionary<string, List<KbNode>>());

        var node = new KbNode
        {
            Name = "Щит",
            LevelIndex = 2,
            Details = new KbNodeDetails
            {
                IpAddress = "10.10.0.5",
                SchemaLink = "https://intra/cabinet"
            },
            Children =
            {
                new KbNode
                {
                    Name = "Модуль",
                    LevelIndex = 3,
                    Details = new KbNodeDetails
                    {
                        IpAddress = "10.10.0.6",
                        SchemaLink = "https://intra/module"
                    }
                }
            }
        };

        service.ReindexSubtree(node, 1);

        Assert.Equal(1, node.LevelIndex);
        Assert.Equal(string.Empty, node.Details.IpAddress);
        Assert.Equal(string.Empty, node.Details.SchemaLink);
        Assert.Equal(2, node.Children[0].LevelIndex);
        Assert.Equal("10.10.0.6", node.Children[0].Details.IpAddress);
        Assert.Equal("https://intra/module", node.Children[0].Details.SchemaLink);
    }
}
