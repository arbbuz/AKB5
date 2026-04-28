using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.Core.Tests;

public class KnowledgeBaseCompositionTemplateServiceTests
{
    private readonly KnowledgeBaseCompositionTemplateService _service = new();

    [Fact]
    public void ApplyTemplate_ReplacesOnlyTargetNodeEntries()
    {
        var targetNode = new KbNode
        {
            NodeId = "cabinet-target",
            Name = "Cabinet target",
            NodeType = KbNodeType.Cabinet
        };

        var result = _service.ApplyTemplate(
            targetNode,
            new List<KbCompositionEntry>
            {
                new()
                {
                    EntryId = "old-target",
                    ParentNodeId = "cabinet-target",
                    SlotNumber = 1,
                    PositionOrder = 0,
                    ComponentType = "Old",
                    Model = "Remove me"
                },
                new()
                {
                    EntryId = "other-node",
                    ParentNodeId = "cabinet-other",
                    SlotNumber = 1,
                    PositionOrder = 0,
                    ComponentType = "Keep",
                    Model = "Other cabinet"
                }
            },
            "cabinet-plc-standard");

        Assert.True(result.IsSuccess);
        Assert.Equal("Шкаф: типовой PLC", result.AppliedTemplateName);
        Assert.Equal(7, result.ImportedEntryCount);

        var preserved = Assert.Single(result.CompositionEntries.Where(entry => entry.ParentNodeId == "cabinet-other"));
        Assert.Equal("other-node", preserved.EntryId);

        var targetEntries = result.CompositionEntries
            .Where(entry => entry.ParentNodeId == "cabinet-target")
            .OrderBy(entry => entry.SlotNumber.HasValue ? 0 : 1)
            .ThenBy(entry => entry.SlotNumber ?? int.MaxValue)
            .ThenBy(entry => entry.PositionOrder)
            .ToList();

        Assert.Equal(7, targetEntries.Count);
        Assert.All(targetEntries, entry => Assert.Equal(string.Empty, entry.EntryId));
        Assert.Equal("Контроллер", targetEntries[0].ComponentType);
        Assert.Equal("PLC CPU", targetEntries[0].Model);
        Assert.Equal("Блок питания", targetEntries[5].ComponentType);
        Assert.Equal("Сетевой коммутатор", targetEntries[6].ComponentType);
    }

    [Fact]
    public void CopyComposition_ReplacesTargetEntriesWithClonedSourceEntries()
    {
        var sourceNode = new KbNode
        {
            NodeId = "cabinet-source",
            Name = "Cabinet source",
            NodeType = KbNodeType.Cabinet
        };
        var targetNode = new KbNode
        {
            NodeId = "cabinet-target",
            Name = "Cabinet target",
            NodeType = KbNodeType.Cabinet
        };

        var result = _service.CopyComposition(
            targetNode,
            new List<KbCompositionEntry>
            {
                new()
                {
                    EntryId = "source-1",
                    ParentNodeId = "cabinet-source",
                    SlotNumber = 2,
                    PositionOrder = 0,
                    ComponentType = "I/O module",
                    Model = "Digital input"
                },
                new()
                {
                    EntryId = "source-2",
                    ParentNodeId = "cabinet-source",
                    SlotNumber = null,
                    PositionOrder = 1,
                    ComponentType = "Power supply",
                    Model = "24V DC"
                },
                new()
                {
                    EntryId = "target-old",
                    ParentNodeId = "cabinet-target",
                    SlotNumber = 1,
                    PositionOrder = 0,
                    ComponentType = "Old",
                    Model = "Remove"
                }
            },
            sourceNode);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.ImportedEntryCount);

        var sourceEntries = result.CompositionEntries.Where(entry => entry.ParentNodeId == "cabinet-source").ToList();
        Assert.Equal(2, sourceEntries.Count);

        var targetEntries = result.CompositionEntries
            .Where(entry => entry.ParentNodeId == "cabinet-target")
            .OrderBy(entry => entry.SlotNumber.HasValue ? 0 : 1)
            .ThenBy(entry => entry.SlotNumber ?? int.MaxValue)
            .ThenBy(entry => entry.PositionOrder)
            .ToList();

        Assert.Equal(2, targetEntries.Count);
        Assert.All(targetEntries, entry => Assert.Equal(string.Empty, entry.EntryId));
        Assert.Equal("I/O module", targetEntries[0].ComponentType);
        Assert.Equal("Digital input", targetEntries[0].Model);
        Assert.Equal("Power supply", targetEntries[1].ComponentType);
        Assert.Equal("24V DC", targetEntries[1].Model);
    }

    [Fact]
    public void CopyComposition_WhenNodeTypesDiffer_Fails()
    {
        var result = _service.CopyComposition(
            new KbNode
            {
                NodeId = "cabinet-target",
                Name = "Cabinet target",
                NodeType = KbNodeType.Cabinet
            },
            Array.Empty<KbCompositionEntry>(),
            new KbNode
            {
                NodeId = "controller-source",
                Name = "Controller source",
                NodeType = KbNodeType.Controller
            });

        Assert.False(result.IsSuccess);
        Assert.Contains("одного типа", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddNodeFromTemplate_CreatesTypedNodeWithInheritedLocationAndTemplateEntries()
    {
        var systemNode = new KbNode
        {
            NodeId = "system-1",
            Name = "System 1",
            LevelIndex = 2,
            NodeType = KbNodeType.System,
            Details = new KbNodeDetails
            {
                Location = "Workshop A / Line 2"
            }
        };
        var wrapperRoot = new KbNode
        {
            NodeId = "workshop-root",
            Name = "Workshop 1",
            LevelIndex = 0,
            NodeType = KbNodeType.WorkshopRoot,
            Children = { systemNode }
        };

        var session = CreateSession(
            new Dictionary<string, List<KbNode>>
            {
                ["Workshop 1"] = new() { wrapperRoot }
            });
        var history = new UndoRedoService();
        var controller = new KnowledgeBaseTreeController(session);
        var sessionWorkflow = new KnowledgeBaseSessionWorkflowService(session);
        var workflow = new KnowledgeBaseTreeMutationWorkflowService(session, sessionWorkflow, controller, history);

        var result = workflow.AddNodeFromTemplate(
            session.CurrentWorkshop,
            systemNode,
            "PLC cabinet 1",
            "cabinet-plc-standard",
            session.GetCurrentWorkshopNodes());

        Assert.True(result.IsSuccess);
        var newNode = Assert.Single(systemNode.Children);
        Assert.Same(newNode, result.AffectedNode);
        Assert.Equal(KbNodeType.Cabinet, newNode.NodeType);
        Assert.Equal("PLC cabinet 1", newNode.Name);
        Assert.Equal("Workshop A / Line 2", newNode.Details.Location);

        var newEntries = session.CompositionEntries.Where(entry => entry.ParentNodeId == newNode.NodeId).ToList();
        Assert.Equal(7, newEntries.Count);
        Assert.True(workflow.CanUndo);
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
                    MaxLevels = 6,
                    LevelNames = new List<string>
                    {
                        "Workshop",
                        "Department",
                        "System",
                        "Cabinet",
                        "Controller",
                        "Module"
                    }
                },
                Workshops = workshops,
                LastWorkshop = "Workshop 1"
            },
            recordAsSavedState: true);
        return session;
    }
}
