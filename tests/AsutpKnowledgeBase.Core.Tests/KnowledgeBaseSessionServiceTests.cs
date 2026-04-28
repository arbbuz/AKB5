using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.Core.Tests;

public class KnowledgeBaseSessionServiceTests
{
    [Fact]
    public void ApplyLoadedData_NormalizesAndReindexesWorkshops()
    {
        var session = new KnowledgeBaseSessionService();

        session.ApplyLoadedData(
            new SavedData
            {
                Config = new KbConfig
                {
                    MaxLevels = 2,
                    LevelNames = new List<string> { "  Цех  ", "" }
                },
                Workshops = new Dictionary<string, List<KbNode>>
                {
                    [" Цех 1 "] =
                    [
                        new KbNode
                        {
                            Name = "Root",
                            LevelIndex = 99,
                            Children =
                            [
                                new KbNode { Name = "Child", LevelIndex = 99 }
                            ]
                        }
                    ]
                },
                LastWorkshop = "Цех 1"
            },
            recordAsSavedState: true);

        Assert.Equal(2, session.Config.MaxLevels);
        Assert.Equal("Цех", session.Config.LevelNames[0]);
        Assert.True(session.Workshops.ContainsKey("Цех 1"));
        Assert.Equal("Цех 1", session.CurrentWorkshop);
        Assert.False(session.IsDirty);
        Assert.False(session.RequiresSave);
        Assert.Equal("Цех 1", session.LastSavedWorkshop);

        var root = session.Workshops["Цех 1"].Single();
        Assert.Equal(0, root.LevelIndex);
        Assert.NotNull(root.Details);
        Assert.Equal(string.Empty, root.Details.Description);
        Assert.Equal(1, root.Children.Single().LevelIndex);
        Assert.NotNull(root.Children.Single().Details);
    }

    [Fact]
    public void TrySelectWorkshop_SavesCurrentRootsBeforeSwitch()
    {
        var session = new KnowledgeBaseSessionService();
        session.ApplyLoadedData(
            new SavedData
            {
                Workshops = new Dictionary<string, List<KbNode>>
                {
                    ["Цех 1"] = new(),
                    ["Цех 2"] = new()
                },
                LastWorkshop = "Цех 1"
            },
            recordAsSavedState: true);

        var currentRoots = new List<KbNode> { new() { Name = "Линия 1" } };

        Assert.True(session.TrySelectWorkshop("Цех 2", currentRoots));
        Assert.Equal("Цех 2", session.CurrentWorkshop);
        Assert.Single(session.Workshops["Цех 1"]);
        Assert.Equal("Линия 1", session.Workshops["Цех 1"][0].Name);
    }

    [Fact]
    public void RefreshDirtyState_IgnoresOnlyCurrentWorkshopSwitch()
    {
        var session = new KnowledgeBaseSessionService();
        session.ApplyLoadedData(
            new SavedData
            {
                Workshops = new Dictionary<string, List<KbNode>>
                {
                    ["Цех 1"] = new(),
                    ["Цех 2"] = new()
                },
                LastWorkshop = "Цех 1"
            },
            recordAsSavedState: true);

        Assert.True(session.TrySelectWorkshop("Цех 2", new List<KbNode>()));

        session.RefreshDirtyState(new List<KbNode>());

        Assert.False(session.IsDirty);
    }

    [Fact]
    public void RefreshDirtyState_HiddenWrapperProjectionDoesNotMarkSessionDirty()
    {
        var wrapperRoot = new KbNode
        {
            Name = "Цех 1",
            LevelIndex = 0,
            NodeType = KbNodeType.WorkshopRoot,
            Children = { new KbNode { Name = "Отделение", LevelIndex = 1, NodeType = KbNodeType.Department } }
        };
        var session = new KnowledgeBaseSessionService();
        session.ApplyLoadedData(
            new SavedData
            {
                Workshops = new Dictionary<string, List<KbNode>>
                {
                    ["Цех 1"] = new() { wrapperRoot }
                },
                LastWorkshop = "Цех 1"
            },
            recordAsSavedState: true);
        var projection = KnowledgeBaseWorkshopTreeProjection.Create(
            session.CurrentWorkshop,
            session.GetCurrentWorkshopNodes());

        session.RefreshDirtyState(projection.CreatePersistedRootsSnapshot(projection.VisibleRoots));

        Assert.False(session.IsDirty);
    }

    [Fact]
    public void RefreshDirtyState_DocumentLinksAffectSnapshot()
    {
        var session = new KnowledgeBaseSessionService();
        session.ApplyLoadedData(
            new SavedData
            {
                Workshops = new Dictionary<string, List<KbNode>>
                {
                    ["Р¦РµС… 1"] = new()
                    {
                        new KbNode
                        {
                            NodeId = "cabinet-1",
                            Name = "Cabinet 1",
                            LevelIndex = 0,
                            NodeType = KbNodeType.Cabinet
                        }
                    }
                },
                LastWorkshop = "Р¦РµС… 1"
            },
            recordAsSavedState: true);

        session.ReplaceDocumentLinks(
            new[]
            {
                new KbDocumentLink
                {
                    OwnerNodeId = "cabinet-1",
                    Kind = KbDocumentKind.Manual,
                    Title = "Manual",
                    Path = "\\\\srv\\manual.pdf"
                }
            });
        session.RefreshDirtyState(session.GetCurrentWorkshopNodes());

        Assert.True(session.IsDirty);
    }

    [Fact]
    public void RefreshDirtyState_NetworkFileReferencesAffectSnapshot()
    {
        var session = new KnowledgeBaseSessionService();
        session.ApplyLoadedData(
            new SavedData
            {
                Workshops = new Dictionary<string, List<KbNode>>
                {
                    ["Цех 1"] = new()
                    {
                        new KbNode
                        {
                            NodeId = "cabinet-1",
                            Name = "Cabinet 1",
                            LevelIndex = 0,
                            NodeType = KbNodeType.Cabinet
                        }
                    }
                },
                LastWorkshop = "Цех 1"
            },
            recordAsSavedState: true);

        session.ReplaceNetworkFileReferences(
            new[]
            {
                new KbNetworkFileReference
                {
                    OwnerNodeId = "cabinet-1",
                    Title = "Topology",
                    Path = "\\\\srv\\network\\topology.png"
                }
            });
        session.RefreshDirtyState(session.GetCurrentWorkshopNodes());

        Assert.True(session.IsDirty);
    }

    [Fact]
    public void TryAddWorkshop_RejectsDuplicateNamesIgnoringTrimAndCase()
    {
        var session = new KnowledgeBaseSessionService();
        session.ApplyLoadedData(
            new SavedData
            {
                Workshops = new Dictionary<string, List<KbNode>>
                {
                    ["Цех 1"] = new()
                },
                LastWorkshop = "Цех 1"
            },
            recordAsSavedState: true);

        Assert.False(session.TryAddWorkshop(" Цех 1 ", new List<KbNode>()));
        Assert.False(session.TryAddWorkshop("цех 1", new List<KbNode>()));
        Assert.True(session.TryAddWorkshop("Новый цех", new List<KbNode>()));
        Assert.Equal("Новый цех", session.CurrentWorkshop);
        Assert.True(session.Workshops.ContainsKey("Новый цех"));
        var root = Assert.Single(session.Workshops["Новый цех"]);
        Assert.Equal("Новый цех", root.Name);
        Assert.Equal(0, root.LevelIndex);
        Assert.Equal(KbNodeType.WorkshopRoot, root.NodeType);
    }

    [Fact]
    public void TryRenameCurrentWorkshop_RenamesWorkshopAndMatchingHiddenRoot()
    {
        var session = new KnowledgeBaseSessionService();
        session.ApplyLoadedData(
            new SavedData
            {
                Workshops = new Dictionary<string, List<KbNode>>
                {
                    ["Цех 1"] =
                    [
                        new KbNode
                        {
                            Name = "Цех 1",
                            LevelIndex = 0,
                            NodeType = KbNodeType.WorkshopRoot,
                            Children = { new KbNode { Name = "Отделение", LevelIndex = 1, NodeType = KbNodeType.Department } }
                        }
                    ],
                    ["Цех 2"] = new()
                },
                LastWorkshop = "Цех 1"
            },
            recordAsSavedState: true);

        Assert.True(session.TryRenameCurrentWorkshop("Новый цех", session.GetCurrentWorkshopNodes()));
        Assert.Equal("Новый цех", session.CurrentWorkshop);
        Assert.False(session.Workshops.ContainsKey("Цех 1"));
        var wrapper = Assert.Single(session.Workshops["Новый цех"]);
        Assert.Equal("Новый цех", wrapper.Name);
        Assert.Equal(KbNodeType.WorkshopRoot, wrapper.NodeType);
        Assert.Equal("Отделение", wrapper.Children.Single().Name);
    }

    [Fact]
    public void TryRenameCurrentWorkshop_DoesNotRenameSingleNonWrapperRoot()
    {
        var session = new KnowledgeBaseSessionService();
        session.ApplyLoadedData(
            new SavedData
            {
                Workshops = new Dictionary<string, List<KbNode>>
                {
                    ["Цех 1"] =
                    [
                        new KbNode
                        {
                            Name = "Линия 1",
                            LevelIndex = 0,
                            NodeType = KbNodeType.System
                        }
                    ],
                    ["Цех 2"] = new()
                },
                LastWorkshop = "Цех 1"
            },
            recordAsSavedState: true);

        Assert.True(session.TryRenameCurrentWorkshop("Новый цех", session.GetCurrentWorkshopNodes()));

        var root = Assert.Single(session.Workshops["Новый цех"]);
        Assert.Equal("Линия 1", root.Name);
        Assert.Equal(KbNodeType.System, root.NodeType);
    }

    [Fact]
    public void TryDeleteCurrentWorkshop_RemovesSelectedWorkshopAndSelectsRemaining()
    {
        var session = new KnowledgeBaseSessionService();
        session.ApplyLoadedData(
            new SavedData
            {
                Workshops = new Dictionary<string, List<KbNode>>
                {
                    ["Цех 1"] = new(),
                    ["Цех 2"] = new()
                },
                LastWorkshop = "Цех 1"
            },
            recordAsSavedState: true);

        Assert.True(session.TryDeleteCurrentWorkshop(session.GetCurrentWorkshopNodes()));
        Assert.False(session.Workshops.ContainsKey("Цех 1"));
        Assert.Equal("Цех 2", session.CurrentWorkshop);
        Assert.Single(session.Workshops);
    }

    [Fact]
    public void TryDeleteCurrentWorkshop_RejectsDeletingLastWorkshop()
    {
        var session = new KnowledgeBaseSessionService();
        session.ApplyLoadedData(
            new SavedData
            {
                Workshops = new Dictionary<string, List<KbNode>>
                {
                    ["Цех 1"] = new()
                },
                LastWorkshop = "Цех 1"
            },
            recordAsSavedState: true);

        Assert.False(session.TryDeleteCurrentWorkshop(session.GetCurrentWorkshopNodes()));
        Assert.True(session.Workshops.ContainsKey("Цех 1"));
        Assert.Equal("Цех 1", session.CurrentWorkshop);
    }
}
