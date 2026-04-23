using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.Core.Tests;

public class KnowledgeBaseWorkshopTreeProjectionTests
{
    [Fact]
    public void Create_HidesSingleWorkshopRoot_WhenWorkshopNameMatches()
    {
        var wrapperRoot = new KbNode
        {
            Name = "Цех 1",
            LevelIndex = 0,
            NodeType = KbNodeType.WorkshopRoot,
            Children = { new KbNode { Name = "Отделение", LevelIndex = 1, NodeType = KbNodeType.Department } }
        };

        var projection = KnowledgeBaseWorkshopTreeProjection.Create(
            " ЦЕХ 1 ",
            new List<KbNode> { wrapperRoot });

        Assert.True(projection.HasHiddenWrapper);
        Assert.Same(wrapperRoot, projection.HiddenWrapperRoot);
        Assert.Single(projection.VisibleRoots);
        Assert.Same(wrapperRoot.Children[0], projection.VisibleRoots[0]);
    }

    [Fact]
    public void Create_HidesSingleWorkshopRoot_WhenWorkshopNameDiffers()
    {
        var wrapperRoot = new KbNode
        {
            Name = "Энергоцех",
            LevelIndex = 0,
            NodeType = KbNodeType.WorkshopRoot,
            Children = { new KbNode { Name = "Отделение", LevelIndex = 1, NodeType = KbNodeType.Department } }
        };

        var projection = KnowledgeBaseWorkshopTreeProjection.Create(
            "Энергоцех (ЭнЦ)",
            new List<KbNode> { wrapperRoot });

        Assert.True(projection.HasHiddenWrapper);
        Assert.Same(wrapperRoot, projection.HiddenWrapperRoot);
        Assert.Single(projection.VisibleRoots);
        Assert.Same(wrapperRoot.Children[0], projection.VisibleRoots[0]);
    }

    [Fact]
    public void Create_WhenWorkshopHasNoRoots_CreatesVirtualHiddenWrapper()
    {
        var projection = KnowledgeBaseWorkshopTreeProjection.Create(
            "Цех 1",
            Array.Empty<KbNode>());

        Assert.True(projection.HasHiddenWrapper);
        Assert.NotNull(projection.HiddenWrapperRoot);
        Assert.Equal("Цех 1", projection.HiddenWrapperRoot!.Name);
        Assert.Equal(0, projection.HiddenWrapperRoot.LevelIndex);
        Assert.Equal(KbNodeType.WorkshopRoot, projection.HiddenWrapperRoot.NodeType);
        Assert.Empty(projection.VisibleRoots);
    }

    [Fact]
    public void Create_DoesNotHideWhenMultipleRootsExist()
    {
        var projection = KnowledgeBaseWorkshopTreeProjection.Create(
            "Цех 1",
            new List<KbNode>
            {
                new() { Name = "Цех 1", LevelIndex = 0, NodeType = KbNodeType.WorkshopRoot },
                new() { Name = "Отделение", LevelIndex = 0, NodeType = KbNodeType.Department }
            });

        Assert.False(projection.HasHiddenWrapper);
        Assert.Equal(2, projection.VisibleRoots.Count);
    }

    [Fact]
    public void Create_DoesNotHideSingleNonWorkshopRoot()
    {
        var visibleRoot = new KbNode
        {
            Name = "Линия 1",
            LevelIndex = 0,
            NodeType = KbNodeType.System,
            Children = { new KbNode { Name = "Щит 1", LevelIndex = 1, NodeType = KbNodeType.Cabinet } }
        };

        var projection = KnowledgeBaseWorkshopTreeProjection.Create(
            "Цех 1",
            new List<KbNode> { visibleRoot });

        Assert.False(projection.HasHiddenWrapper);
        Assert.Single(projection.VisibleRoots);
        Assert.Same(visibleRoot, projection.VisibleRoots[0]);
    }

    [Fact]
    public void Create_HidesWorkshopRootEvenWhenWrapperContainsDetails()
    {
        var wrapperRoot = new KbNode
        {
            Name = "Цех 1",
            LevelIndex = 0,
            NodeType = KbNodeType.WorkshopRoot,
            Details = new KbNodeDetails { Description = "Корень цеха" },
            Children = { new KbNode { Name = "Отделение", LevelIndex = 1, NodeType = KbNodeType.Department } }
        };

        var projection = KnowledgeBaseWorkshopTreeProjection.Create(
            "Цех 1",
            new List<KbNode> { wrapperRoot });

        Assert.True(projection.HasHiddenWrapper);
        Assert.Single(projection.VisibleRoots);
        Assert.Same(wrapperRoot, projection.HiddenWrapperRoot);
        Assert.Equal("Отделение", projection.VisibleRoots[0].Name);
    }

    [Fact]
    public void Create_DoesNotHideWhenWorkshopRootIsNotLevelZero()
    {
        var projection = KnowledgeBaseWorkshopTreeProjection.Create(
            "Цех 1",
            new List<KbNode>
            {
                new()
                {
                    Name = "Цех 1",
                    LevelIndex = 1,
                    NodeType = KbNodeType.WorkshopRoot,
                    Children = { new KbNode { Name = "Участок", LevelIndex = 2, NodeType = KbNodeType.Department } }
                }
            });

        Assert.False(projection.HasHiddenWrapper);
        Assert.Single(projection.VisibleRoots);
        Assert.Equal("Цех 1", projection.VisibleRoots[0].Name);
        Assert.Equal(1, projection.VisibleRoots[0].LevelIndex);
    }

    [Fact]
    public void CreatePersistedRootsSnapshot_RestoresHiddenWrapperWithoutChangingLevels()
    {
        var child = new KbNode
        {
            Name = "Отделение",
            LevelIndex = 1,
            NodeType = KbNodeType.Department,
            Children = { new KbNode { Name = "Участок", LevelIndex = 2, NodeType = KbNodeType.System } }
        };
        var wrapperRoot = new KbNode
        {
            Name = "Цех 1",
            LevelIndex = 0,
            NodeType = KbNodeType.WorkshopRoot,
            Children = { child }
        };
        var projection = KnowledgeBaseWorkshopTreeProjection.Create(
            "Цех 1",
            new List<KbNode> { wrapperRoot });

        var persistedRoots = projection.CreatePersistedRootsSnapshot(projection.VisibleRoots);

        var restoredWrapper = Assert.Single(persistedRoots);
        Assert.Same(wrapperRoot, restoredWrapper);
        Assert.Same(child, restoredWrapper.Children[0]);
        Assert.Equal(1, restoredWrapper.Children[0].LevelIndex);
        Assert.Equal(2, restoredWrapper.Children[0].Children[0].LevelIndex);
    }

    [Fact]
    public void CreatePersistedRootsSnapshot_WhenVirtualHiddenWrapperIsEmpty_ReturnsEmptyRoots()
    {
        var projection = KnowledgeBaseWorkshopTreeProjection.Create(
            "Цех 1",
            Array.Empty<KbNode>());

        var persistedRoots = projection.CreatePersistedRootsSnapshot(Array.Empty<KbNode>());

        Assert.Empty(persistedRoots);
    }

    [Fact]
    public void CreatePersistedRootsSnapshot_WhenPersistedHiddenWrapperIsEmpty_KeepsWrapperRoot()
    {
        var wrapperRoot = new KbNode
        {
            Name = "Новый цех",
            LevelIndex = 0,
            NodeType = KbNodeType.WorkshopRoot
        };
        var projection = KnowledgeBaseWorkshopTreeProjection.Create(
            "Новый цех",
            new List<KbNode> { wrapperRoot });

        var persistedRoots = projection.CreatePersistedRootsSnapshot(Array.Empty<KbNode>());

        var restoredWrapper = Assert.Single(persistedRoots);
        Assert.Same(wrapperRoot, restoredWrapper);
        Assert.Empty(restoredWrapper.Children);
    }

    [Fact]
    public void GetEffectiveParentForRootOperations_ReturnsHiddenWrapper()
    {
        var wrapperRoot = new KbNode
        {
            Name = "Цех 1",
            LevelIndex = 0,
            NodeType = KbNodeType.WorkshopRoot,
            Children = { new KbNode { Name = "Отделение", LevelIndex = 1, NodeType = KbNodeType.Department } }
        };
        var projection = KnowledgeBaseWorkshopTreeProjection.Create(
            "Цех 1",
            new List<KbNode> { wrapperRoot });

        Assert.Same(wrapperRoot, projection.GetEffectiveParentForRootOperations());
    }

    [Fact]
    public void ResolveActualParent_ForVisibleRootWithoutVisibleParent_ReturnsHiddenWrapper()
    {
        var child = new KbNode { Name = "Отделение", LevelIndex = 1, NodeType = KbNodeType.Department };
        var wrapperRoot = new KbNode
        {
            Name = "Цех 1",
            LevelIndex = 0,
            NodeType = KbNodeType.WorkshopRoot,
            Children = { child }
        };
        var projection = KnowledgeBaseWorkshopTreeProjection.Create(
            "Цех 1",
            new List<KbNode> { wrapperRoot });

        Assert.Same(wrapperRoot, projection.ResolveActualParent(child, visibleParentNode: null));
    }
}
