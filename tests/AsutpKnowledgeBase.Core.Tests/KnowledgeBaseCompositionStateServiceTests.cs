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
        Assert.Equal("Позиция 1", state.Entries[2].PositionText);
        Assert.Equal(3, state.TotalEntries);
        Assert.Equal(2, state.SlottedEntries);
        Assert.Equal(1, state.AuxiliaryEntries);
        Assert.Equal(2, state.SlottedEntryStates.Count);
        Assert.Single(state.AuxiliaryEntryStates);
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
        Assert.True(state.SupportsEditing);
        Assert.Contains("дочерние узлы", state.SourceText, StringComparison.OrdinalIgnoreCase);
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
