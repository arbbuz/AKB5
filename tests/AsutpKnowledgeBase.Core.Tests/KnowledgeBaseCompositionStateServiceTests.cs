using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.Core.Tests;

public class KnowledgeBaseCompositionStateServiceTests
{
    private readonly KnowledgeBaseCompositionStateService _service = new();

    [Fact]
    public void Build_ForTypedEntries_SortsBySlotThenPosition()
    {
        var selectedNode = new KbNode
        {
            NodeId = "cabinet-1",
            Name = "Шкаф 1",
            NodeType = KbNodeType.Cabinet
        };

        var state = _service.Build(
            selectedNode,
            new List<KbCompositionEntry>
            {
                new()
                {
                    EntryId = "aux-2",
                    ParentNodeId = "cabinet-1",
                    PositionOrder = 4,
                    ComponentType = "Вентилятор",
                    Model = "Fan-1"
                },
                new()
                {
                    EntryId = "slot-2",
                    ParentNodeId = "cabinet-1",
                    RackNumber = 1,
                    SlotNumber = 2,
                    PositionOrder = 10,
                    ComponentType = "Модуль DI",
                    Model = "SM321"
                },
                new()
                {
                    EntryId = "slot-1",
                    ParentNodeId = "cabinet-1",
                    SlotNumber = 1,
                    PositionOrder = 30,
                    ComponentType = "CPU",
                    Model = "S7-1500"
                }
            });

        Assert.True(state.HasEntries);
        Assert.Equal("Слот 1", state.Entries[0].PositionText);
        Assert.Equal("Слот 2", state.Entries[1].PositionText);
        Assert.Equal(1, state.Entries[1].RackNumber);
        Assert.Equal("Позиция 1", state.Entries[2].PositionText);
        Assert.Equal(3, state.TotalEntries);
        Assert.Equal(2, state.RackCount);
        Assert.Equal(2, state.SlottedEntries);
        Assert.Equal(1, state.AuxiliaryEntries);
        Assert.Equal(2, state.SlottedEntryStates.Count);
        Assert.Single(state.AuxiliaryEntryStates);
        Assert.Equal(new[] { "(0) UR / Rack0", "(1) UR / Rack1" }, state.RackStates.Select(rack => rack.Title));
        Assert.Equal(11, state.RackStates[0].SlotRows.Count);
        Assert.Equal("CPU", state.RackStates[0].SlotRows[1].SlotRoleText);
        Assert.True(state.RackStates[0].SlotRows[1].IsPlaceholder);
        Assert.True(state.SupportsEditing);
        Assert.Contains("сохран", state.SourceText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Build_WithoutTypedEntries_FallsBackToLegacyChildren()
    {
        var selectedNode = new KbNode
        {
            NodeId = "cabinet-1",
            Name = "Шкаф 1",
            NodeType = KbNodeType.Cabinet,
            Children =
            {
                new KbNode
                {
                    NodeId = "module-1",
                    Name = "CPU 1",
                    NodeType = KbNodeType.Controller,
                    Details = new KbNodeDetails
                    {
                        Description = "Основной контроллер",
                        IpAddress = "10.0.0.15"
                    }
                }
            }
        };

        var state = _service.Build(selectedNode, Array.Empty<KbCompositionEntry>());

        var entry = Assert.Single(state.Entries);
        Assert.Equal("Controller", entry.ComponentTypeText);
        Assert.Equal("CPU 1", entry.ComponentText);
        Assert.Equal("10.0.0.15", entry.IpAddressText);
        Assert.Empty(state.SlottedEntryStates);
        Assert.Single(state.AuxiliaryEntryStates);
        Assert.Single(state.RackStates);
        Assert.Equal("(0) UR / Rack0", state.RackStates[0].Title);
        Assert.True(state.SupportsEditing);
        Assert.Contains("дочерние узлы", state.SourceText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Build_ForTypedEntries_AddsS7300WarningsWithoutBlockingState()
    {
        var selectedNode = new KbNode
        {
            NodeId = "cabinet-1",
            Name = "Шкаф 1",
            NodeType = KbNodeType.Cabinet
        };

        var state = _service.Build(
            selectedNode,
            new List<KbCompositionEntry>
            {
                new()
                {
                    EntryId = "wrong-slot-1",
                    ParentNodeId = "cabinet-1",
                    RackNumber = 0,
                    SlotNumber = 1,
                    ComponentType = "CPU",
                    Model = "CPU 315-2 DP"
                },
                new()
                {
                    EntryId = "rack0-slot-2",
                    ParentNodeId = "cabinet-1",
                    RackNumber = 0,
                    SlotNumber = 2,
                    ComponentType = "CPU",
                    Model = "6ES7 315-2AG10-0AB0"
                },
                new()
                {
                    EntryId = "rack0-slot-3",
                    ParentNodeId = "cabinet-1",
                    RackNumber = 0,
                    SlotNumber = 3,
                    ComponentType = "IM",
                    Model = "IM 153"
                },
                new()
                {
                    EntryId = "rack1-slot-2",
                    ParentNodeId = "cabinet-1",
                    RackNumber = 1,
                    SlotNumber = 2,
                    ComponentType = "CPU",
                    Model = "CPU 315"
                },
                new()
                {
                    EntryId = "rack1-slot-3",
                    ParentNodeId = "cabinet-1",
                    RackNumber = 1,
                    SlotNumber = 3,
                    ComponentType = "IM",
                    Model = "IM 361"
                }
            });

        Assert.True(state.HasEntries);
        Assert.Equal("SIMATIC S7-300", state.ProfileText);
        Assert.Equal(2, state.WarningCount);
        Assert.Equal(1, state.HintCount);
        Assert.Contains(state.RackStates[0].SlotRows, row =>
            row.SlotNumberValue == 1 &&
            row.HasSlotWarning &&
            row.SlotAdvisoryText.Contains("PS", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(state.RackStates[0].SlotRows, row =>
            row.SlotNumberValue == 3 &&
            row.HasSlotHint &&
            row.SlotAdvisoryText.Contains("IM 360", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(state.RackStates[1].SlotRows, row =>
            row.SlotNumberValue == 2 &&
            row.HasSlotWarning &&
            row.SlotAdvisoryText.Contains("свободен", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(state.RackStates[1].SlotRows, row =>
            row.SlotNumberValue == 3 &&
            !row.HasSlotWarning &&
            !row.HasSlotHint);
    }

    [Fact]
    public void Build_ForExpansionRack_AddsInterfaceModulePlaceholderHints()
    {
        var selectedNode = new KbNode
        {
            NodeId = "cabinet-1",
            Name = "Шкаф 1",
            NodeType = KbNodeType.Cabinet
        };

        var state = _service.Build(
            selectedNode,
            new List<KbCompositionEntry>
            {
                new()
                {
                    EntryId = "rack1-slot-4",
                    ParentNodeId = "cabinet-1",
                    RackNumber = 1,
                    SlotNumber = 4,
                    ComponentType = "DI",
                    Model = "SM 321"
                }
            });

        Assert.Equal(0, state.WarningCount);
        Assert.Equal(2, state.HintCount);
        Assert.Contains(state.RackStates[0].SlotRows, row =>
            row.SlotNumberValue == 3 &&
            row.IsPlaceholder &&
            row.HasSlotHint &&
            row.SlotAdvisoryText.Contains("send IM", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(state.RackStates[1].SlotRows, row =>
            row.SlotNumberValue == 3 &&
            row.IsPlaceholder &&
            row.HasSlotHint &&
            row.SlotAdvisoryText.Contains("receive IM", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Build_ForUnsupportedNodeType_ReturnsEmptyState()
    {
        var selectedNode = new KbNode
        {
            NodeId = "department-1",
            Name = "Отделение 1",
            NodeType = KbNodeType.Department
        };

        var state = _service.Build(selectedNode, Array.Empty<KbCompositionEntry>());

        Assert.False(state.HasEntries);
        Assert.False(state.SupportsEditing);
        Assert.Contains("недоступна", state.EmptyStateText, StringComparison.OrdinalIgnoreCase);
    }
}
