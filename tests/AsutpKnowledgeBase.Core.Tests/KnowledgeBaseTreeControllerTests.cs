using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.Core.Tests;

public class KnowledgeBaseTreeControllerTests
{
    [Fact]
    public void AddNode_AddsRootAndChildUsingCurrentStructure()
    {
        var workshops = new Dictionary<string, List<KbNode>>();
        var controller = CreateController(workshops, maxLevels: 4);

        var root = controller.AddNode("Цех 1", null, "Линия 1");
        var child = controller.AddNode("Цех 1", root, "Щит 1");

        Assert.Equal(0, root.LevelIndex);
        Assert.Equal(1, child.LevelIndex);
        Assert.Single(workshops["Цех 1"]);
        Assert.Same(child, root.Children[0]);
    }

    [Fact]
    public void CopyAndPasteNode_UsesClipboardClone()
    {
        var root = new KbNode { Name = "Линия", LevelIndex = 0 };
        var source = new KbNode
        {
            Name = "Щит",
            LevelIndex = 1,
            Children = { new KbNode { Name = "Модуль", LevelIndex = 2 } }
        };
        root.Children.Add(source);

        var workshops = new Dictionary<string, List<KbNode>>
        {
            ["Цех 1"] = new List<KbNode> { root }
        };

        var controller = CreateController(workshops, maxLevels: 5);
        controller.CopyNode(source);

        var pasted = controller.PasteNode(root);

        Assert.True(controller.HasClipboardNode);
        Assert.Equal("Щит", pasted.Name);
        Assert.Equal(1, pasted.LevelIndex);
        Assert.Equal(2, pasted.Children[0].LevelIndex);
        Assert.NotSame(source, pasted);
        Assert.NotSame(source.Children[0], pasted.Children[0]);
        Assert.Equal(2, root.Children.Count);
    }

    [Fact]
    public void CanPasteNode_ReturnsFalseWhenClipboardOrDepthDoNotAllow()
    {
        var root = new KbNode { Name = "Линия", LevelIndex = 0 };
        var controller = CreateController(
            new Dictionary<string, List<KbNode>> { ["Цех 1"] = new List<KbNode> { root } },
            maxLevels: 2);

        Assert.False(controller.CanPasteNode(root));

        controller.CopyNode(new KbNode
        {
            Name = "Щит",
            Children = { new KbNode { Name = "Модуль" } }
        });

        Assert.False(controller.CanPasteNode(root));
    }

    [Fact]
    public void RenameNode_UpdatesNodeName()
    {
        var controller = CreateController(new Dictionary<string, List<KbNode>>(), maxLevels: 3);
        var node = new KbNode { Name = "Старое" };

        controller.RenameNode(node, "  Новое  ");

        Assert.Equal("Новое", node.Name);
    }

    [Fact]
    public void MoveNode_ReparentsNodeAndReindexesSubtree()
    {
        var dragged = new KbNode
        {
            Name = "Щит 1",
            LevelIndex = 1,
            Children = { new KbNode { Name = "Модуль", LevelIndex = 2 } }
        };
        var sourceParent = new KbNode
        {
            Name = "Линия 1",
            LevelIndex = 0,
            Children = { dragged }
        };
        var targetParent = new KbNode { Name = "Линия 2", LevelIndex = 0 };

        var workshops = new Dictionary<string, List<KbNode>>
        {
            ["Цех 1"] = new List<KbNode> { sourceParent, targetParent }
        };

        var controller = CreateController(workshops, maxLevels: 4);

        bool moved = controller.MoveNode("Цех 1", dragged, sourceParent, targetParent);

        Assert.True(moved);
        Assert.Empty(sourceParent.Children);
        Assert.Single(targetParent.Children);
        Assert.Equal(1, dragged.LevelIndex);
        Assert.Equal(2, dragged.Children[0].LevelIndex);
    }

    [Fact]
    public void CanMoveNode_ReturnsFalseWhenTargetIsDescendantOfDraggedNode()
    {
        var grandChild = new KbNode { Name = "Модуль", LevelIndex = 2 };
        var dragged = new KbNode
        {
            Name = "Щит 1",
            LevelIndex = 1,
            Children = { grandChild }
        };
        var root = new KbNode
        {
            Name = "Линия 1",
            LevelIndex = 0,
            Children = { dragged }
        };

        var workshops = new Dictionary<string, List<KbNode>>
        {
            ["Цех 1"] = new List<KbNode> { root }
        };

        var controller = CreateController(workshops, maxLevels: 4);

        Assert.False(controller.CanMoveNode(grandChild, dragged));
        Assert.True(controller.WouldCreateCycle(grandChild, dragged));
    }

    private static KnowledgeBaseTreeController CreateController(
        Dictionary<string, List<KbNode>> workshops,
        int maxLevels)
    {
        return new KnowledgeBaseTreeController(
            new KbConfig
            {
                MaxLevels = maxLevels,
                LevelNames = new List<string>()
            },
            workshops);
    }
}
