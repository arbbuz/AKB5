using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.Core.Tests;

public class KnowledgeBaseMaintenanceScheduleStateServiceTests
{
    private readonly KnowledgeBaseMaintenanceScheduleStateService _service = new();

    [Fact]
    public void Build_ForSupportedNodeWithProfile_ReturnsProfileSummary()
    {
        var selectedNode = new KbNode
        {
            NodeId = "device-1",
            Name = "Device 1",
            NodeType = KbNodeType.Device
        };

        var state = _service.Build(
            selectedNode,
            new[]
            {
                new KbMaintenanceScheduleProfile
                {
                    MaintenanceProfileId = "maintenance-1",
                    OwnerNodeId = "device-1",
                    IsIncludedInSchedule = true,
                    To1Hours = 2,
                    To2Hours = 5,
                    To3Hours = 12
                }
            });

        Assert.True(state.SupportsEditing);
        Assert.True(state.HasProfile);
        Assert.Equal("Да", state.InclusionText);
        Assert.Equal("2 ч", state.To1HoursText);
        Assert.Equal("12 ч", state.To3HoursText);
        Assert.Contains("включён", state.SummaryText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Build_ForSupportedNodeWithoutProfile_ReturnsEditableEmptyState()
    {
        var selectedNode = new KbNode
        {
            NodeId = "device-1",
            Name = "Device 1",
            NodeType = KbNodeType.Device
        };

        var state = _service.Build(selectedNode, Array.Empty<KbMaintenanceScheduleProfile>());

        Assert.True(state.SupportsEditing);
        Assert.False(state.HasProfile);
        Assert.Equal("Профиль ТО для этого узла ещё не настроен.", state.EmptyStateText);
        Assert.Equal("Профиль ТО для этого узла ещё не настроен.", state.SummaryText);
    }

    [Fact]
    public void Build_ForUnsupportedNode_ReturnsUnavailableState()
    {
        var selectedNode = new KbNode
        {
            NodeId = "system-1",
            Name = "Line 1",
            NodeType = KbNodeType.System
        };

        var state = _service.Build(selectedNode, Array.Empty<KbMaintenanceScheduleProfile>());

        Assert.False(state.SupportsEditing);
        Assert.False(state.HasProfile);
        Assert.Contains("недоступна", state.EmptyStateText, StringComparison.OrdinalIgnoreCase);
    }
}
