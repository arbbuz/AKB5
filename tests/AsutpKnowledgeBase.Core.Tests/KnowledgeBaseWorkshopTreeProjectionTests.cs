using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.Core.Tests;

public class KnowledgeBaseWorkshopTreeProjectionTests
{
    [Fact]
    public void Create_HidesSingleMatchingRootWithEmptyDetails()
    {
        var wrapperRoot = new KbNode
        {
            Name = "Цех 1",
            LevelIndex = 0,
            Children = { new KbNode { Name = "Отделение", LevelIndex = 1 } }
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
    public void Create_DoesNotHideWhenMultipleRootsExist()
    {
        var projection = KnowledgeBaseWorkshopTreeProjection.Create(
            "Цех 1",
            new List<KbNode>
            {
                new() { Name = "Цех 1", LevelIndex = 0 },
                new() { Name = "Отделение", LevelIndex = 0 }
            });

        Assert.False(projection.HasHiddenWrapper);
        Assert.Equal(2, projection.VisibleRoots.Count);
    }

    [Fact]
    public void Create_DoesNotHideWhenWrapperContainsDetails()
    {
        var projection = KnowledgeBaseWorkshopTreeProjection.Create(
            "Цех 1",
            new List<KbNode>
            {
                new()
                {
                    Name = "Цех 1",
                    LevelIndex = 0,
                    Details = new KbNodeDetails { Description = "Не технический узел" },
                    Children = { new KbNode { Name = "Отделение", LevelIndex = 1 } }
                }
            });

        Assert.False(projection.HasHiddenWrapper);
        Assert.Single(projection.VisibleRoots);
        Assert.Equal("Цех 1", projection.VisibleRoots[0].Name);
    }

    [Fact]
    public void CreatePersistedRootsSnapshot_RestoresHiddenWrapperWithoutChangingLevels()
    {
        var child = new KbNode
        {
            Name = "Отделение",
            LevelIndex = 1,
            Children = { new KbNode { Name = "Участок", LevelIndex = 2 } }
        };
        var wrapperRoot = new KbNode
        {
            Name = "Цех 1",
            LevelIndex = 0,
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
    public void GetEffectiveParentForRootOperations_ReturnsHiddenWrapper()
    {
        var wrapperRoot = new KbNode
        {
            Name = "Цех 1",
            LevelIndex = 0,
            Children = { new KbNode { Name = "Отделение", LevelIndex = 1 } }
        };
        var projection = KnowledgeBaseWorkshopTreeProjection.Create(
            "Цех 1",
            new List<KbNode> { wrapperRoot });

        Assert.Same(wrapperRoot, projection.GetEffectiveParentForRootOperations());
    }

    [Fact]
    public void ResolveActualParent_ForVisibleRootWithoutVisibleParent_ReturnsHiddenWrapper()
    {
        var child = new KbNode { Name = "Отделение", LevelIndex = 1 };
        var wrapperRoot = new KbNode
        {
            Name = "Цех 1",
            LevelIndex = 0,
            Children = { child }
        };
        var projection = KnowledgeBaseWorkshopTreeProjection.Create(
            "Цех 1",
            new List<KbNode> { wrapperRoot });

        Assert.Same(wrapperRoot, projection.ResolveActualParent(child, visibleParentNode: null));
    }
}
