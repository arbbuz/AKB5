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
    public void AddNode_WhenUsingVirtualHiddenWorkshopRoot_PersistsWrapperAndAddsFirstVisibleChild()
    {
        var session = CreateSession(
            new Dictionary<string, List<KbNode>>
            {
                ["Цех 1"] = new List<KbNode>()
            });
        var history = new UndoRedoService();
        var controller = new KnowledgeBaseTreeController(session);
        var sessionWorkflow = new KnowledgeBaseSessionWorkflowService(session);
        var workflow = new KnowledgeBaseTreeMutationWorkflowService(session, sessionWorkflow, controller, history);
        var projection = KnowledgeBaseWorkshopTreeProjection.Create(session.CurrentWorkshop, session.GetCurrentWorkshopNodes());

        var result = workflow.AddNode(
            session.CurrentWorkshop,
            parentNode: projection.GetEffectiveParentForRootOperations(),
            nodeName: "Отделение",
            currentRoots: projection.CreatePersistedRootsSnapshot(projection.VisibleRoots));

        Assert.True(result.IsSuccess);
        var wrapperRoot = Assert.Single(session.Workshops["Цех 1"]);
        var addedNode = Assert.Single(wrapperRoot.Children);
        Assert.Equal("Цех 1", wrapperRoot.Name);
        Assert.Equal(0, wrapperRoot.LevelIndex);
        Assert.Equal(KbNodeType.WorkshopRoot, wrapperRoot.NodeType);
        Assert.Equal("Отделение", addedNode.Name);
        Assert.Equal(1, addedNode.LevelIndex);
        Assert.Same(wrapperRoot, result.ViewState.CurrentRoots[0]);
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
        var wrapperRoot = new KbNode
        {
            Name = "Цех 1",
            LevelIndex = 0,
            NodeType = KbNodeType.WorkshopRoot
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
    public void DeleteNode_RemovesTypedRecordsForWholeDeletedSubtree()
    {
        var childNode = new KbNode
        {
            NodeId = "controller-1",
            Name = "Controller 1",
            LevelIndex = 1,
            NodeType = KbNodeType.Controller
        };
        var root1 = new KbNode
        {
            NodeId = "cabinet-1",
            Name = "Cabinet 1",
            LevelIndex = 0,
            NodeType = KbNodeType.Cabinet,
            Children = { childNode }
        };
        var root2 = new KbNode
        {
            NodeId = "cabinet-2",
            Name = "Cabinet 2",
            LevelIndex = 0,
            NodeType = KbNodeType.Cabinet
        };
        var session = CreateSession(
            new Dictionary<string, List<KbNode>>
            {
                ["Р¦РµС… 1"] = new List<KbNode> { root1, root2 }
            });
        session.ReplaceCompositionEntries(
            new[]
            {
                new KbCompositionEntry { ParentNodeId = "cabinet-1", ComponentType = "CPU" },
                new KbCompositionEntry { ParentNodeId = "controller-1", ComponentType = "Module" },
                new KbCompositionEntry { ParentNodeId = "cabinet-2", ComponentType = "CPU" }
            });
        session.ReplaceDocumentLinks(
            new[]
            {
                new KbDocumentLink { OwnerNodeId = "cabinet-1", Kind = KbDocumentKind.Manual, Title = "Manual", Path = "\\\\srv\\manual.pdf" },
                new KbDocumentLink { OwnerNodeId = "controller-1", Kind = KbDocumentKind.SchemeLink, Title = "Scheme", Path = "\\\\srv\\scheme.pdf" },
                new KbDocumentLink { OwnerNodeId = "cabinet-2", Kind = KbDocumentKind.Manual, Title = "Other", Path = "\\\\srv\\other.pdf" }
            });
        session.ReplaceSoftwareRecords(
            new[]
            {
                new KbSoftwareRecord { OwnerNodeId = "cabinet-1", Title = "Backup 1", Path = "\\\\srv\\backup1.zip" },
                new KbSoftwareRecord { OwnerNodeId = "controller-1", Title = "Backup 2", Path = "\\\\srv\\backup2.zip" },
                new KbSoftwareRecord { OwnerNodeId = "cabinet-2", Title = "Backup 3", Path = "\\\\srv\\backup3.zip" }
            });
        session.ReplaceNetworkFileReferences(
            new[]
            {
                new KbNetworkFileReference
                {
                    OwnerNodeId = "cabinet-1",
                    Title = "Topology 1",
                    Path = "\\\\srv\\network\\topology-1.png"
                },
                new KbNetworkFileReference
                {
                    OwnerNodeId = "controller-1",
                    Title = "Topology 2",
                    Path = "\\\\srv\\network\\topology-2.png"
                },
                new KbNetworkFileReference
                {
                    OwnerNodeId = "cabinet-2",
                    Title = "Topology 3",
                    Path = "\\\\srv\\network\\topology-3.png"
                }
            });
        session.ReplaceMaintenanceScheduleProfiles(
            new[]
            {
                new KbMaintenanceScheduleProfile
                {
                    OwnerNodeId = "cabinet-1",
                    IsIncludedInSchedule = true,
                    To1Hours = 2,
                    To2Hours = 4,
                    To3Hours = 8
                },
                new KbMaintenanceScheduleProfile
                {
                    OwnerNodeId = "controller-1",
                    IsIncludedInSchedule = true,
                    To1Hours = 1,
                    To2Hours = 2,
                    To3Hours = 3
                },
                new KbMaintenanceScheduleProfile
                {
                    OwnerNodeId = "cabinet-2",
                    IsIncludedInSchedule = true,
                    To1Hours = 2,
                    To2Hours = 2,
                    To3Hours = 2
                }
            });

        var history = new UndoRedoService();
        var controller = new KnowledgeBaseTreeController(session);
        var sessionWorkflow = new KnowledgeBaseSessionWorkflowService(session);
        var workflow = new KnowledgeBaseTreeMutationWorkflowService(session, sessionWorkflow, controller, history);

        var result = workflow.DeleteNode(session.CurrentWorkshop, root1, session.GetCurrentWorkshopNodes());

        Assert.True(result.IsSuccess);
        var remainingComposition = Assert.Single(session.CompositionEntries);
        Assert.Equal("cabinet-2", remainingComposition.ParentNodeId);
        var remainingDocument = Assert.Single(session.DocumentLinks);
        Assert.Equal("cabinet-2", remainingDocument.OwnerNodeId);
        var remainingSoftware = Assert.Single(session.SoftwareRecords);
        Assert.Equal("cabinet-2", remainingSoftware.OwnerNodeId);
        var remainingNetwork = Assert.Single(session.NetworkFileReferences);
        Assert.Equal("cabinet-2", remainingNetwork.OwnerNodeId);
        var remainingMaintenanceProfile = Assert.Single(session.MaintenanceScheduleProfiles);
        Assert.Equal("cabinet-2", remainingMaintenanceProfile.OwnerNodeId);
    }

    [Fact]
    public void CreateObjectFromTemplate_WhenSuccessful_AddsSubtreeAndRemapsTypedDefaults()
    {
        var parentNode = new KbNode
        {
            NodeId = "system-1",
            Name = "System 1",
            LevelIndex = 0,
            NodeType = KbNodeType.System
        };
        var session = CreateSession(
            new Dictionary<string, List<KbNode>>
            {
                ["Workshop 1"] = new List<KbNode> { parentNode }
            });
        session.ReplaceObjectTemplates(new[] { CreateCabinetObjectTemplate() });

        var history = new UndoRedoService();
        var controller = new KnowledgeBaseTreeController(session);
        var sessionWorkflow = new KnowledgeBaseSessionWorkflowService(session);
        var workflow = new KnowledgeBaseTreeMutationWorkflowService(session, sessionWorkflow, controller, history);

        var result = workflow.CreateObjectFromTemplate(
            session.CurrentWorkshop,
            parentNode,
            "cabinet-template",
            "Cabinet A",
            session.GetCurrentWorkshopNodes());

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.True(workflow.CanUndo);

        var cabinet = Assert.Single(parentNode.Children);
        Assert.Same(cabinet, result.AffectedNode);
        Assert.Equal("Cabinet A", cabinet.Name);
        Assert.Equal(1, cabinet.LevelIndex);
        Assert.Equal(KbNodeType.Cabinet, cabinet.NodeType);
        Assert.False(string.Equals("cabinet", cabinet.NodeId, StringComparison.Ordinal));

        var controllerNode = Assert.Single(cabinet.Children);
        Assert.Equal("Template controller", controllerNode.Name);
        Assert.Equal(2, controllerNode.LevelIndex);
        Assert.Equal(KbNodeType.Controller, controllerNode.NodeType);
        Assert.NotEqual(cabinet.NodeId, controllerNode.NodeId);

        var composition = Assert.Single(session.CompositionEntries);
        Assert.Equal(cabinet.NodeId, composition.ParentNodeId);
        Assert.Equal("CPU", composition.ComponentType);

        var document = Assert.Single(session.DocumentLinks);
        Assert.Equal(controllerNode.NodeId, document.OwnerNodeId);
        Assert.Equal(KbDocumentKind.SchemeLink, document.Kind);

        var software = Assert.Single(session.SoftwareRecords);
        Assert.Equal(controllerNode.NodeId, software.OwnerNodeId);

        var networkFile = Assert.Single(session.NetworkFileReferences);
        Assert.Equal(controllerNode.NodeId, networkFile.OwnerNodeId);
        Assert.Equal(KbNetworkPreviewKind.Image, networkFile.PreviewKind);

        var maintenanceProfile = Assert.Single(session.MaintenanceScheduleProfiles);
        Assert.Equal(controllerNode.NodeId, maintenanceProfile.OwnerNodeId);
        Assert.Equal(2, maintenanceProfile.To1Hours);

        var undoResult = workflow.Undo(session.GetCurrentWorkshopNodes());
        Assert.True(undoResult.IsSuccess);
        Assert.Empty(Assert.Single(session.GetCurrentWorkshopNodes()).Children);
        Assert.Empty(session.CompositionEntries);
        Assert.Empty(session.DocumentLinks);
        Assert.Empty(session.SoftwareRecords);
        Assert.Empty(session.NetworkFileReferences);
        Assert.Empty(session.MaintenanceScheduleProfiles);
    }

    [Fact]
    public void CreateObjectFromTemplate_WhenTemplateDoesNotFitDepth_ReturnsDepthLimitFailure()
    {
        var parentNode = new KbNode
        {
            NodeId = "system-1",
            Name = "System 1",
            LevelIndex = 1,
            NodeType = KbNodeType.System
        };
        var rootNode = new KbNode
        {
            NodeId = "department-1",
            Name = "Department 1",
            LevelIndex = 0,
            NodeType = KbNodeType.Department,
            Children = { parentNode }
        };
        var session = CreateSession(
            new Dictionary<string, List<KbNode>>
            {
                ["Workshop 1"] = new List<KbNode> { rootNode }
            },
            maxLevels: 2);
        session.ReplaceObjectTemplates(new[] { CreateCabinetObjectTemplate() });

        var history = new UndoRedoService();
        var controller = new KnowledgeBaseTreeController(session);
        var sessionWorkflow = new KnowledgeBaseSessionWorkflowService(session);
        var workflow = new KnowledgeBaseTreeMutationWorkflowService(session, sessionWorkflow, controller, history);

        Assert.False(workflow.CanCreateObjectFromTemplate(parentNode));

        var result = workflow.CreateObjectFromTemplate(
            session.CurrentWorkshop,
            parentNode,
            "cabinet-template",
            "Cabinet A",
            session.GetCurrentWorkshopNodes());

        Assert.False(result.IsSuccess);
        Assert.Equal(KnowledgeBaseTreeMutationFailure.DepthLimitExceeded, result.Failure);
        Assert.Empty(parentNode.Children);
        Assert.Empty(session.CompositionEntries);
        Assert.False(workflow.CanUndo);
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
            NodeType = KbNodeType.WorkshopRoot,
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

    private static KnowledgeBaseSessionService CreateSession(
        Dictionary<string, List<KbNode>> workshops,
        int maxLevels = 4)
    {
        var session = new KnowledgeBaseSessionService();
        session.ApplyLoadedData(
            new SavedData
            {
                SchemaVersion = SavedData.CurrentSchemaVersion,
                Config = new KbConfig
                {
                    MaxLevels = maxLevels,
                    LevelNames = new List<string> { "Цех", "Линия", "Щит", "Модуль" }
                },
                Workshops = workshops,
                LastWorkshop = "Цех 1"
            },
            recordAsSavedState: true);
        return session;
    }

    private static KbObjectTemplate CreateCabinetObjectTemplate() =>
        new()
        {
            TemplateId = "cabinet-template",
            DisplayName = "Cabinet template",
            RootNode = new KbObjectTemplateNode
            {
                TemplateNodeId = "cabinet",
                Name = "Template cabinet",
                NodeType = KbNodeType.Cabinet,
                Details = new KbNodeDetails
                {
                    Description = "Template description",
                    Location = "Panel room"
                },
                Children =
                {
                    new KbObjectTemplateNode
                    {
                        TemplateNodeId = "controller",
                        Name = "Template controller",
                        NodeType = KbNodeType.Controller,
                        Details = new KbNodeDetails
                        {
                            IpAddress = "10.10.10.20"
                        }
                    }
                }
            },
            CompositionEntries =
            {
                new KbObjectTemplateCompositionEntry
                {
                    ParentTemplateNodeId = "cabinet",
                    SlotNumber = 1,
                    ComponentType = "CPU",
                    Model = "PLC CPU"
                }
            },
            DocumentLinks =
            {
                new KbObjectTemplateDocumentLink
                {
                    OwnerTemplateNodeId = "controller",
                    Kind = KbDocumentKind.SchemeLink,
                    Title = "Scheme",
                    Path = "\\\\srv\\scheme.pdf"
                }
            },
            SoftwareRecords =
            {
                new KbObjectTemplateSoftwareRecord
                {
                    OwnerTemplateNodeId = "controller",
                    Title = "Backup",
                    Path = "\\\\srv\\backup.zip"
                }
            },
            NetworkFileReferences =
            {
                new KbObjectTemplateNetworkFileReference
                {
                    OwnerTemplateNodeId = "controller",
                    Title = "Topology",
                    Path = "\\\\srv\\network\\topology.png"
                }
            },
            MaintenanceScheduleProfiles =
            {
                new KbObjectTemplateMaintenanceScheduleProfile
                {
                    OwnerTemplateNodeId = "controller",
                    IsIncludedInSchedule = true,
                    To1Hours = 2,
                    To2Hours = 4,
                    To3Hours = 8
                }
            }
        };
}
