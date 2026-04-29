using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.Core.Tests;

public class KnowledgeBaseMaintenanceMonthlyPlannerIntegrationTests
{
    private readonly KnowledgeBaseMaintenanceMonthlyPlannerService _service = new();

    [Fact]
    public void PlanMonth_FromRootsAndProfiles_BuildsEndToEndMonthlyPlan()
    {
        var device1 = new KbNode
        {
            NodeId = "device-1",
            Name = "Насос 1",
            NodeType = KbNodeType.Device
        };
        var device2 = new KbNode
        {
            NodeId = "device-2",
            Name = "Насос 2",
            NodeType = KbNodeType.Device
        };
        var roots = new[]
        {
            new KbNode
            {
                NodeId = "cabinet-1",
                Name = "Шкаф 1",
                NodeType = KbNodeType.Cabinet,
                Children = { device1, device2 }
            }
        };

        KnowledgeBaseMaintenanceMonthPlanResult result = _service.PlanMonth(
            2026,
            1,
            totalMonthlyHourBudget: 32,
            roots,
            new[]
            {
                new KbMaintenanceScheduleProfile
                {
                    OwnerNodeId = "device-1",
                    IsIncludedInSchedule = true,
                    To1Hours = 3
                },
                new KbMaintenanceScheduleProfile
                {
                    OwnerNodeId = "device-2",
                    IsIncludedInSchedule = true,
                    To1Hours = 5
                }
            });

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.PlannedWorkItems.Count);
        Assert.Equal(8, result.RequestedHours);
        KbMaintenanceMonthPlanDay firstDay = Assert.Single(result.PlannedDays);
        Assert.Equal(new DateOnly(2026, 1, 12), firstDay.Date);
        Assert.Equal(8, firstDay.TotalHours);
        Assert.Equal(2, firstDay.Assignments.Count);
    }

    [Fact]
    public void PlanMonth_FromRootsAndProfiles_PropagatesResolvedDemandIntoBudgetFailure()
    {
        var node = new KbNode
        {
            NodeId = "device-1",
            Name = "Насос 1",
            NodeType = KbNodeType.Device
        };

        KnowledgeBaseMaintenanceMonthPlanResult result = _service.PlanMonth(
            2026,
            1,
            totalMonthlyHourBudget: 4,
            new[] { node },
            new[]
            {
                new KbMaintenanceScheduleProfile
                {
                    OwnerNodeId = "device-1",
                    IsIncludedInSchedule = true,
                    To1Hours = 6
                }
            });

        Assert.False(result.IsSuccess);
        Assert.Single(result.PlannedWorkItems);
        Assert.Contains("4 ч", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PlanMonth_FromRootsAndProfiles_ReturnsEmptyPlanWhenNothingIsDue()
    {
        var node = new KbNode
        {
            NodeId = "device-1",
            Name = "Насос 1",
            NodeType = KbNodeType.Device
        };

        KnowledgeBaseMaintenanceMonthPlanResult result = _service.PlanMonth(
            2026,
            1,
            totalMonthlyHourBudget: 40,
            new[] { node },
            new[]
            {
                new KbMaintenanceScheduleProfile
                {
                    OwnerNodeId = "device-1",
                    IsIncludedInSchedule = false,
                    To1Hours = 3,
                    To2Hours = 5,
                    To3Hours = 7
                }
            });

        Assert.True(result.IsSuccess);
        Assert.Empty(result.PlannedWorkItems);
        Assert.Empty(result.PlannedDays);
    }
}
