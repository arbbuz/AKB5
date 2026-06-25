using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.Core.Tests;

public class KnowledgeBaseActDraftServiceTests
{
    [Fact]
    public void CreateDraft_FromLvl3RackEntry_CreatesActWithEquipmentSnapshot()
    {
        (List<KbNode> roots, KbNode lvl3Node) = CreateLvl3Tree();
        var rack = new KbCompositionRack
        {
            RackId = "rack-1",
            ParentNodeId = "lvl3-cabinet",
            RackNumber = 1,
            RackType = "UR2",
            Label = "Main rack"
        };
        var entry = new KbCompositionEntry
        {
            EntryId = "entry-1",
            ParentNodeId = "lvl3-cabinet",
            RackNumber = 1,
            SlotNumber = 4,
            PositionOrder = 7,
            ComponentType = " SIMATIC S7-300, PS 307, БЛОК ПИТАНИЯ, ВХОД: ~120/230В, ВЫХОД: =24В/2A ",
            Model = " SM 321 ",
            OrderNumber = " 6ES7 321-1BH02-0AA0 ",
            Notes = " Input module "
        };
        DateTime now = new(2026, 6, 25, 10, 30, 0, DateTimeKind.Utc);
        var service = new KnowledgeBaseActDraftService(
            clock: () => now,
            actIdFactory: () => "act-draft-1");

        KnowledgeBaseActDraftResult result = service.CreateDraft(new KnowledgeBaseActDraftRequest
        {
            Lvl3Node = lvl3Node,
            WorkshopRoots = roots,
            WorkshopName = " Купоросный цех ",
            VisibleLevel = 3,
            Rack = rack,
            CompositionEntry = entry,
            CreatedBy = " Operator "
        });

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.NotNull(result.Act);
        KbAct act = result.Act!;
        Assert.Equal("act-draft-1", act.ActId);
        Assert.Equal(2026, act.ActYear);
        Assert.Equal(string.Empty, act.ActNumber);
        Assert.Equal(KbActType.EquipmentFailure, act.ActType);
        Assert.Equal(KbActStatus.Draft, act.Status);
        Assert.Equal(new DateTime(2026, 6, 25), act.ActDate);
        Assert.Equal("Купоросный цех", act.WorkshopName);
        Assert.Equal("lvl3-cabinet", act.Lvl3NodeId);
        Assert.Equal("Шкаф PLC", act.Lvl3NameSnapshot);
        Assert.Equal("Линия 1", act.ObjectNameSnapshot);
        Assert.Equal("Цех 1 / Линия 1 / Шкаф PLC", act.ObjectPathSnapshot);
        Assert.Equal("rack-1", act.RackId);
        Assert.Equal(1, act.RackNumberSnapshot);
        Assert.Equal("(1) UR2 / Rack1 - Main rack", act.RackNameSnapshot);
        Assert.Equal("entry-1", act.CompositionEntryId);
        Assert.Equal("SIMATIC S7-300, PS 307, БЛОК ПИТАНИЯ", act.EquipmentName);
        Assert.Null(act.FailureDate);
        Assert.Equal(string.Empty, act.FaultDescription);
        Assert.Equal(string.Empty, act.FailureReason);
        Assert.Equal(string.Empty, act.InspectionResult);
        Assert.Equal(string.Empty, act.FaultCriterion);
        Assert.Equal(string.Empty, act.RequestDocument);
        Assert.Equal(string.Empty, act.ActualLaborHours);
        Assert.Equal("Operator", act.CreatedBy);
        Assert.Equal(now, act.CreatedAt);
        Assert.Equal(now, act.UpdatedAt);

        Assert.Equal("Шкаф PLC", act.EquipmentSnapshot.Lvl3Name);
        Assert.Equal("Цех 1 / Линия 1 / Шкаф PLC", act.EquipmentSnapshot.ObjectPath);
        Assert.Equal("rack-1", act.EquipmentSnapshot.RackId);
        Assert.Equal(1, act.EquipmentSnapshot.RackNumber);
        Assert.Equal("(1) UR2 / Rack1 - Main rack", act.EquipmentSnapshot.RackName);
        Assert.Equal("entry-1", act.EquipmentSnapshot.CompositionEntryId);
        Assert.Equal("SIMATIC S7-300, PS 307, БЛОК ПИТАНИЯ, ВХОД: ~120/230В, ВЫХОД: =24В/2A", act.EquipmentSnapshot.ComponentType);
        Assert.Equal("SM 321", act.EquipmentSnapshot.Model);
        Assert.Equal("6ES7 321-1BH02-0AA0", act.EquipmentSnapshot.OrderNumber);
        Assert.Equal(string.Empty, act.EquipmentSnapshot.SerialNumber);
        Assert.Equal("Input module", act.EquipmentSnapshot.Notes);
    }

    [Fact]
    public void CreateDraft_WhenModelAndOrderNumberAreEmpty_LeavesSnapshotFieldsEmpty()
    {
        (List<KbNode> roots, KbNode lvl3Node) = CreateLvl3Tree();
        var entry = new KbCompositionEntry
        {
            EntryId = "entry-empty",
            ParentNodeId = "lvl3-cabinet",
            RackNumber = 0,
            ComponentType = " ",
            Model = " ",
            OrderNumber = " "
        };
        var service = new KnowledgeBaseActDraftService(
            clock: () => new DateTime(2026, 6, 25, 10, 30, 0, DateTimeKind.Utc),
            actIdFactory: () => "act-draft-empty");

        KnowledgeBaseActDraftResult result = service.CreateDraft(new KnowledgeBaseActDraftRequest
        {
            Lvl3Node = lvl3Node,
            WorkshopRoots = roots,
            CompositionEntry = entry
        });

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.NotNull(result.Act);
        KbAct act = result.Act!;
        Assert.Equal(string.Empty, act.EquipmentName);
        Assert.Equal(string.Empty, act.EquipmentSnapshot.ComponentType);
        Assert.Equal(string.Empty, act.EquipmentSnapshot.Model);
        Assert.Equal(string.Empty, act.EquipmentSnapshot.OrderNumber);
        Assert.Equal(string.Empty, act.EquipmentSnapshot.SerialNumber);
        Assert.Equal("entry-empty", act.CompositionEntryId);
    }

    [Fact]
    public void CreateDraft_DoesNotCopyOrderNumberToSerialNumber()
    {
        (_, KbNode lvl3Node) = CreateLvl3Tree();
        var entry = new KbCompositionEntry
        {
            EntryId = "entry-order",
            ParentNodeId = "lvl3-cabinet",
            RackNumber = 0,
            Model = "CPU 1214C",
            OrderNumber = "6ES7 214-1AG40-0XB0"
        };
        var service = new KnowledgeBaseActDraftService(
            clock: () => new DateTime(2026, 6, 25, 10, 30, 0, DateTimeKind.Utc),
            actIdFactory: () => "act-draft-order");

        KnowledgeBaseActDraftResult result = service.CreateDraft(new KnowledgeBaseActDraftRequest
        {
            Lvl3Node = lvl3Node,
            CompositionEntry = entry
        });

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.NotNull(result.Act);
        KbAct act = result.Act!;
        Assert.Equal("6ES7 214-1AG40-0XB0", act.EquipmentSnapshot.OrderNumber);
        Assert.Equal(string.Empty, act.EquipmentSnapshot.SerialNumber);
    }

    [Fact]
    public void BuildDefaultEquipmentName_WhenModelDuplicatesOrderNumber_ReturnsEmpty()
    {
        string equipmentName = KnowledgeBaseActDraftService.BuildDefaultEquipmentName(
            componentType: "",
            model: " 6ES7 214-1AG40-0XB0 ",
            orderNumber: "6ES7 214-1AG40-0XB0");

        Assert.Equal(string.Empty, equipmentName);
    }

    [Fact]
    public void CreateDraft_WhenEntryHasNoId_ReturnsFailure()
    {
        (_, KbNode lvl3Node) = CreateLvl3Tree();
        var service = new KnowledgeBaseActDraftService();

        KnowledgeBaseActDraftResult result = service.CreateDraft(new KnowledgeBaseActDraftRequest
        {
            Lvl3Node = lvl3Node,
            CompositionEntry = new KbCompositionEntry
            {
                ParentNodeId = "lvl3-cabinet",
                Model = "Placeholder"
            }
        });

        Assert.False(result.IsSuccess);
        Assert.Null(result.Act);
        Assert.Contains("пустой строке", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    private static (List<KbNode> Roots, KbNode Lvl3Node) CreateLvl3Tree()
    {
        var lvl3Node = new KbNode
        {
            NodeId = "lvl3-cabinet",
            Name = "Шкаф PLC",
            LevelIndex = 2,
            NodeType = KbNodeType.Cabinet
        };
        var lvl2Node = new KbNode
        {
            NodeId = "lvl2-line",
            Name = "Линия 1",
            LevelIndex = 1,
            NodeType = KbNodeType.System,
            Children = { lvl3Node }
        };
        var rootNode = new KbNode
        {
            NodeId = "lvl1-workshop",
            Name = "Цех 1",
            LevelIndex = 0,
            NodeType = KbNodeType.Department,
            Children = { lvl2Node }
        };

        return (new List<KbNode> { rootNode }, lvl3Node);
    }
}
