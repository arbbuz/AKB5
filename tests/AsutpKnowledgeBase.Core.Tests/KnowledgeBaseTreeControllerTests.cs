using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.Core.Tests;

public class KnowledgeBaseTreeControllerTests
{
    [Fact]
    public void AddNode_AddsRootAndChildUsingCurrentStructure()
    {
        var session = CreateSession(new Dictionary<string, List<KbNode>>(), maxLevels: 4);
        var controller = CreateController(session);

        var root = controller.AddNode("Цех 1", null, "Линия 1");
        var child = controller.AddNode("Цех 1", root, "Щит 1");

        Assert.Equal(0, root.LevelIndex);
        Assert.Equal(1, child.LevelIndex);
        Assert.Single(session.Workshops["Цех 1"]);
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

        var session = CreateSession(
            new Dictionary<string, List<KbNode>>
            {
                ["Цех 1"] = new List<KbNode> { root }
            },
            maxLevels: 5);

        var controller = CreateController(session);
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
        var session = CreateSession(
            new Dictionary<string, List<KbNode>> { ["Цех 1"] = new List<KbNode> { root } },
            maxLevels: 2);
        var controller = CreateController(session);

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
        var session = CreateSession(new Dictionary<string, List<KbNode>>(), maxLevels: 3);
        var controller = CreateController(session);
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

        var session = CreateSession(
            new Dictionary<string, List<KbNode>>
            {
                ["Цех 1"] = new List<KbNode> { sourceParent, targetParent }
            },
            maxLevels: 4);
        var controller = CreateController(session);

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

        var session = CreateSession(
            new Dictionary<string, List<KbNode>>
            {
                ["Цех 1"] = new List<KbNode> { root }
            },
            maxLevels: 4);
        var controller = CreateController(session);

        Assert.False(controller.CanMoveNode(grandChild, dragged));
        Assert.True(controller.WouldCreateCycle(grandChild, dragged));
    }

    [Fact]
    public void CanAddNode_UsesUpdatedSessionConfigWithoutRebinding()
    {
        var root = new KbNode { Name = "Линия", LevelIndex = 0 };
        var session = CreateSession(
            new Dictionary<string, List<KbNode>>
            {
                ["Цех 1"] = new List<KbNode> { root }
            },
            maxLevels: 1);
        var controller = CreateController(session);

        Assert.False(controller.CanAddNode(root));

        session.UpdateConfig(new KbConfig
        {
            MaxLevels = 2,
            LevelNames = new List<string> { "Цех", "Линия" }
        });

        Assert.True(controller.CanAddNode(root));
    }

    private static KnowledgeBaseTreeController CreateController(KnowledgeBaseSessionService session)
        => new(session);

    private static KnowledgeBaseSessionService CreateSession(
        Dictionary<string, List<KbNode>> workshops,
        int maxLevels)
    {
        var session = new KnowledgeBaseSessionService();
        session.ApplyLoadedData(
            new SavedData
            {
                SchemaVersion = SavedData.CurrentSchemaVersion,
                Config = new KbConfig
                {
                    MaxLevels = maxLevels,
                    LevelNames = new List<string>()
                },
                Workshops = workshops,
                LastWorkshop = workshops.Keys.FirstOrDefault() ?? string.Empty
            },
            recordAsSavedState: true);
        return session;
    }
}
