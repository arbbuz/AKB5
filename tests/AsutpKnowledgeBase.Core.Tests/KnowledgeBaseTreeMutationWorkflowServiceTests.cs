using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.Core.Tests;

public class KnowledgeBaseTreeMutationWorkflowServiceTests
{
    [Fact]
    public void AddNode_WhenSuccessful_PushesUndoSnapshotAndReturnsCreatedNodeWithCurrentViewState()
    {
        var session = CreateSessionWithDefaultData();
        var history = new UndoRedoService();
        var controller = new KnowledgeBaseTreeController(session);
        var sessionWorkflow = new KnowledgeBaseSessionWorkflowService(session);
        var workflow = new KnowledgeBaseTreeMutationWorkflowService(session, sessionWorkflow, controller, history);

        var result = workflow.AddNode(
            session.CurrentWorkshop,
            parentNode: null,
            nodeName: "  Линия 1  ",
            currentRoots: session.GetCurrentWorkshopNodes());

        Assert.True(result.IsSuccess);
        Assert.True(workflow.CanUndo);
        Assert.Equal("Линия 1", result.AffectedNode!.Name);
        Assert.Single(session.GetCurrentWorkshopNodes());
        Assert.Equal(session.CurrentWorkshop, result.ViewState.CurrentWorkshop);
        Assert.Same(session.GetCurrentWorkshopNodes(), result.ViewState.CurrentRoots);
        Assert.Same(result.AffectedNode, result.ViewState.CurrentRoots[0]);
    }

    [Fact]
    public void RenameNode_WhenSuccessful_ReturnsViewStateWithRenamedNode()
    {
        var root = new KbNode { Name = "Линия 1", LevelIndex = 0 };
        var session = CreateSession(
            new Dictionary<string, List<KbNode>>
            {
                ["Цех 1"] = new List<KbNode> { root }
            });
        var history = new UndoRedoService();
        var controller = new KnowledgeBaseTreeController(session);
        var sessionWorkflow = new KnowledgeBaseSessionWorkflowService(session);
        var workflow = new KnowledgeBaseTreeMutationWorkflowService(session, sessionWorkflow, controller, history);

        var result = workflow.RenameNode(root, "  Линия A  ", session.GetCurrentWorkshopNodes());

        Assert.True(result.IsSuccess);
        Assert.Equal("Линия A", root.Name);
        Assert.Equal(session.CurrentWorkshop, result.ViewState.CurrentWorkshop);
        Assert.Same(session.GetCurrentWorkshopNodes(), result.ViewState.CurrentRoots);
        Assert.Equal("Линия A", result.ViewState.CurrentRoots[0].Name);
    }

    [Fact]
    public void AddNode_WhenUsingHiddenWrapperParent_AddsVisibleChildInsideWorkshopWrapper()
    {
        var wrapperRoot = new KbNode { Name = "Цех 1", LevelIndex = 0 };
        var session = CreateSession(
            new Dictionary<string, List<KbNode>>
            {
                ["Цех 1"] = new List<KbNode> { wrapperRoot }
            });
        var history = new UndoRedoService();
        var controller = new KnowledgeBaseTreeController(session);
        var sessionWorkflow = new KnowledgeBaseSessionWorkflowService(session);
        var workflow = new KnowledgeBaseTreeMutationWorkflowService(session, sessionWorkflow, controller, history);

        var result = workflow.AddNode(
            session.CurrentWorkshop,
            parentNode: wrapperRoot,
            nodeName: "Отделение",
            currentRoots: session.GetCurrentWorkshopNodes());

        Assert.True(result.IsSuccess);
        var persistedRoot = Assert.Single(session.Workshops["Цех 1"]);
        var addedNode = Assert.Single(persistedRoot.Children);
        Assert.Same(wrapperRoot, persistedRoot);
        Assert.Same(addedNode, result.AffectedNode);
        Assert.Equal("Отделение", addedNode.Name);
        Assert.Equal(1, addedNode.LevelIndex);
        Assert.Same(wrapperRoot, result.ViewState.CurrentRoots[0]);
    }

    [Fact]
    public void DeleteNode_WhenSuccessful_ReturnsViewStateWithoutDeletedNode()
    {
        var root1 = new KbNode { Name = "Линия 1", LevelIndex = 0 };
        var root2 = new KbNode { Name = "Линия 2", LevelIndex = 0 };
        var session = CreateSession(
            new Dictionary<string, List<KbNode>>
            {
                ["Цех 1"] = new List<KbNode> { root1, root2 }
            });
        var history = new UndoRedoService();
        var controller = new KnowledgeBaseTreeController(session);
        var sessionWorkflow = new KnowledgeBaseSessionWorkflowService(session);
        var workflow = new KnowledgeBaseTreeMutationWorkflowService(session, sessionWorkflow, controller, history);

        var result = workflow.DeleteNode(session.CurrentWorkshop, root1, session.GetCurrentWorkshopNodes());

        Assert.True(result.IsSuccess);
        Assert.Equal(session.CurrentWorkshop, result.ViewState.CurrentWorkshop);
        Assert.Same(session.GetCurrentWorkshopNodes(), result.ViewState.CurrentRoots);
        Assert.Single(result.ViewState.CurrentRoots);
        Assert.Equal("Линия 2", result.ViewState.CurrentRoots[0].Name);
    }

    [Fact]
    public void MoveNode_WhenTargetIsDescendant_ReturnsCycleFailure()
    {
        var root = new KbNode { Name = "Линия 1", LevelIndex = 0 };
        var child = new KbNode { Name = "Щит 1", LevelIndex = 1 };
        var grandChild = new KbNode { Name = "Модуль 1", LevelIndex = 2 };
        child.Children.Add(grandChild);
        root.Children.Add(child);

        var session = CreateSession(
            new Dictionary<string, List<KbNode>>
            {
                ["Цех 1"] = new List<KbNode> { root }
            });
        var history = new UndoRedoService();
        var controller = new KnowledgeBaseTreeController(session);
        var sessionWorkflow = new KnowledgeBaseSessionWorkflowService(session);
        var workflow = new KnowledgeBaseTreeMutationWorkflowService(session, sessionWorkflow, controller, history);

        var result = workflow.MoveNode(
            session.CurrentWorkshop,
            draggedNode: child,
            oldParentNode: root,
            targetNode: grandChild,
            currentRoots: session.GetCurrentWorkshopNodes());

        Assert.False(result.IsSuccess);
        Assert.Equal(KnowledgeBaseTreeMutationFailure.MoveWouldCreateCycle, result.Failure);
        Assert.False(workflow.CanUndo);
    }

    [Fact]
    public void MoveNode_WhenVisibleRootUsesHiddenWrapperAsOldParent_Succeeds()
    {
        var draggedNode = new KbNode { Name = "Отделение 1", LevelIndex = 1 };
        var targetNode = new KbNode { Name = "Отделение 2", LevelIndex = 1 };
        var wrapperRoot = new KbNode
        {
            Name = "Цех 1",
            LevelIndex = 0,
            Children = { draggedNode, targetNode }
        };
        var session = CreateSession(
            new Dictionary<string, List<KbNode>>
            {
                ["Цех 1"] = new List<KbNode> { wrapperRoot }
            });
        var history = new UndoRedoService();
        var controller = new KnowledgeBaseTreeController(session);
        var sessionWorkflow = new KnowledgeBaseSessionWorkflowService(session);
        var workflow = new KnowledgeBaseTreeMutationWorkflowService(session, sessionWorkflow, controller, history);

        var result = workflow.MoveNode(
            session.CurrentWorkshop,
            draggedNode,
            oldParentNode: wrapperRoot,
            targetNode,
            currentRoots: session.GetCurrentWorkshopNodes());

        Assert.True(result.IsSuccess);
        Assert.Single(wrapperRoot.Children);
        Assert.Same(targetNode, wrapperRoot.Children[0]);
        Assert.Single(targetNode.Children);
        Assert.Same(draggedNode, targetNode.Children[0]);
        Assert.Equal(2, draggedNode.LevelIndex);
    }

    [Fact]
    public void Undo_RestoresPreviousSnapshot()
    {
        var root = new KbNode
        {
            Name = "Линия 1",
            LevelIndex = 0,
            Details = new KbNodeDetails
            {
                Description = "Исходная линия",
                Location = "Цех 1"
            }
        };
        var session = CreateSession(
            new Dictionary<string, List<KbNode>>
            {
                ["Цех 1"] = new List<KbNode> { root }
            });
        var history = new UndoRedoService();
        var controller = new KnowledgeBaseTreeController(session);
        var sessionWorkflow = new KnowledgeBaseSessionWorkflowService(session);
        var workflow = new KnowledgeBaseTreeMutationWorkflowService(session, sessionWorkflow, controller, history);

        var addResult = workflow.AddNode(
            session.CurrentWorkshop,
            parentNode: null,
            nodeName: "Линия 2",
            currentRoots: session.GetCurrentWorkshopNodes());
        Assert.True(addResult.IsSuccess);

        var undoResult = workflow.Undo(session.GetCurrentWorkshopNodes());

        Assert.True(undoResult.IsSuccess);
        var restoredRoot = Assert.Single(undoResult.ViewState.CurrentRoots);
        Assert.Equal("Линия 1", restoredRoot.Name);
        Assert.Equal("Исходная линия", restoredRoot.Details.Description);
        Assert.True(workflow.CanRedo);
    }

    [Fact]
    public void RenameNode_WhenNameDoesNotChange_ReturnsNoChanges()
    {
        var root = new KbNode { Name = "Линия 1", LevelIndex = 0 };
        var session = CreateSession(
            new Dictionary<string, List<KbNode>>
            {
                ["Цех 1"] = new List<KbNode> { root }
            });
        var history = new UndoRedoService();
        var controller = new KnowledgeBaseTreeController(session);
        var sessionWorkflow = new KnowledgeBaseSessionWorkflowService(session);
        var workflow = new KnowledgeBaseTreeMutationWorkflowService(session, sessionWorkflow, controller, history);

        var result = workflow.RenameNode(root, "Линия 1", session.GetCurrentWorkshopNodes());

        Assert.False(result.IsSuccess);
        Assert.Equal(KnowledgeBaseTreeMutationFailure.NoChanges, result.Failure);
        Assert.False(workflow.CanUndo);
    }

    private static KnowledgeBaseSessionService CreateSessionWithDefaultData()
    {
        var session = new KnowledgeBaseSessionService();
        session.InitializeDefaultData(recordAsSavedState: true);
        return session;
    }

    private static KnowledgeBaseSessionService CreateSession(Dictionary<string, List<KbNode>> workshops)
    {
        var session = new KnowledgeBaseSessionService();
        session.ApplyLoadedData(
            new SavedData
            {
                SchemaVersion = SavedData.CurrentSchemaVersion,
                Config = new KbConfig
                {
                    MaxLevels = 4,
                    LevelNames = new List<string> { "Цех", "Линия", "Щит", "Модуль" }
                },
                Workshops = workshops,
                LastWorkshop = "Цех 1"
            },
            recordAsSavedState: true);
        return session;
    }
}
