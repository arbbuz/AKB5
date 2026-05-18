using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.Core.Tests;

public class KnowledgeBaseCompositionMutationServiceTests
{
    private readonly KnowledgeBaseCompositionMutationService _service = new();

    [Fact]
    public void UpsertEntry_AddsNewEntry_ForSupportedNode()
    {
        var parentNode = new KbNode
        {
            NodeId = "cabinet-1",
            Name = "Шкаф 1",
            NodeType = KbNodeType.Cabinet
        };

        var result = _service.UpsertEntry(
            parentNode,
            Array.Empty<KbCompositionEntry>(),
            new KbCompositionEntry
            {
                RackNumber = 2,
                SlotNumber = 1,
                PositionOrder = 0,
                ComponentType = " CPU ",
                Model = " PLC-1 ",
                OrderNumber = " 6ES7 315-2AG10-0AB0 ",
                Firmware = " V2.6 ",
                MpiDpPnAddress = " 2 ",
                InputAddress = " I 0.0 ",
                OutputAddress = " Q 4.0 ",
                Comment = " Main CPU ",
                InterfaceRows = " X1, Port 1 ",
                IpAddress = " 10.0.0.1 "
            });

        Assert.True(result.IsSuccess);
        var entry = Assert.Single(result.CompositionEntries);
        Assert.Equal("cabinet-1", entry.ParentNodeId);
        Assert.Equal(2, entry.RackNumber);
        Assert.Equal(1, entry.SlotNumber);
        Assert.Equal("CPU", entry.ComponentType);
        Assert.Equal("PLC-1", entry.Model);
        Assert.Equal("6ES7 315-2AG10-0AB0", entry.OrderNumber);
        Assert.Equal("V2.6", entry.Firmware);
        Assert.Equal("2", entry.MpiDpPnAddress);
        Assert.Equal("I 0.0", entry.InputAddress);
        Assert.Equal("Q 4.0", entry.OutputAddress);
        Assert.Equal("Main CPU", entry.Comment);
        Assert.Equal("X1, Port 1", entry.InterfaceRows);
        Assert.Equal("10.0.0.1", entry.IpAddress);
    }

    [Fact]
    public void UpsertEntry_UpdatesExistingEntry_ForSameParent()
    {
        var parentNode = new KbNode
        {
            NodeId = "cabinet-1",
            Name = "Шкаф 1",
            NodeType = KbNodeType.Cabinet
        };

        var result = _service.UpsertEntry(
            parentNode,
            new List<KbCompositionEntry>
            {
                new()
                {
                    EntryId = "entry-1",
                    ParentNodeId = "cabinet-1",
                    SlotNumber = 1,
                    PositionOrder = 0,
                    ComponentType = "CPU",
                    Model = "PLC-1"
                }
            },
            new KbCompositionEntry
            {
                EntryId = "entry-1",
                RackNumber = 1,
                SlotNumber = 2,
                PositionOrder = 1,
                ComponentType = "Module",
                Model = "SM321"
            });

        Assert.True(result.IsSuccess);
        var entry = Assert.Single(result.CompositionEntries);
        Assert.Equal(1, entry.RackNumber);
        Assert.Equal(2, entry.SlotNumber);
        Assert.Equal(1, entry.PositionOrder);
        Assert.Equal("Module", entry.ComponentType);
        Assert.Equal("SM321", entry.Model);
    }

    [Fact]
    public void DeleteEntry_RemovesOnlySelectedEntry_ForSameParent()
    {
        var parentNode = new KbNode
        {
            NodeId = "cabinet-1",
            Name = "Шкаф 1",
            NodeType = KbNodeType.Cabinet
        };

        var result = _service.DeleteEntry(
            parentNode,
            new List<KbCompositionEntry>
            {
                new()
                {
                    EntryId = "entry-1",
                    ParentNodeId = "cabinet-1",
                    Model = "PLC-1"
                },
                new()
                {
                    EntryId = "entry-2",
                    ParentNodeId = "cabinet-2",
                    Model = "PLC-2"
                }
            },
            "entry-1");

        Assert.True(result.IsSuccess);
        var remaining = Assert.Single(result.CompositionEntries);
        Assert.Equal("entry-2", remaining.EntryId);
    }

    [Fact]
    public void UpsertEntry_ForUnsupportedNode_ReturnsFailure()
    {
        var parentNode = new KbNode
        {
            NodeId = "department-1",
            Name = "Отделение 1",
            NodeType = KbNodeType.Department
        };

        var result = _service.UpsertEntry(
            parentNode,
            Array.Empty<KbCompositionEntry>(),
            new KbCompositionEntry
            {
                ComponentType = "CPU"
            });

        Assert.False(result.IsSuccess);
        Assert.Contains("Состав", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }
    [Fact]
    public void UpsertEntry_ForVisibleLevel3System_ReturnsSuccess()
    {
        var parentNode = new KbNode
        {
            NodeId = "legacy-cabinet-1",
            Name = "Шкаф 1",
            NodeType = KbNodeType.System
        };

        var result = _service.UpsertEntry(
            parentNode,
            Array.Empty<KbCompositionEntry>(),
            new KbCompositionEntry
            {
                ComponentType = "CPU"
            },
            visibleLevel: 3);

        Assert.True(result.IsSuccess);
        Assert.Single(result.CompositionEntries);
    }
}
