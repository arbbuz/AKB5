using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.Core.Tests;

public class KnowledgeBaseTreeMutationWorkflowServiceTests
{
    [Fact]
    public void AddNode_WhenSuccessful_PushesUndoSnapshotAndReturnsCreatedNode()
    {
        var session = CreateSessionWithDefaultData();
        var history = new UndoRedoService();
        var controller = new KnowledgeBaseTreeController(session.Config, session.Workshops);
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
        var controller = new KnowledgeBaseTreeController(session.Config, session.Workshops);
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
    public void Undo_RestoresPreviousSnapshot()
    {
        var session = CreateSessionWithDefaultData();
        var history = new UndoRedoService();
        var controller = new KnowledgeBaseTreeController(session.Config, session.Workshops);
        var sessionWorkflow = new KnowledgeBaseSessionWorkflowService(session);
        var workflow = new KnowledgeBaseTreeMutationWorkflowService(session, sessionWorkflow, controller, history);

        var addResult = workflow.AddNode(
            session.CurrentWorkshop,
            parentNode: null,
            nodeName: "Линия 1",
            currentRoots: session.GetCurrentWorkshopNodes());
        Assert.True(addResult.IsSuccess);

        var undoResult = workflow.Undo(session.GetCurrentWorkshopNodes());

        Assert.True(undoResult.IsSuccess);
        Assert.Empty(undoResult.ViewState.CurrentRoots);
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
        var controller = new KnowledgeBaseTreeController(session.Config, session.Workshops);
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
