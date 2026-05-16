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
        var roots = CreateLevel3Roots(device1, device2);

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
        Assert.Single(result.PlannedDays);
        Assert.Equal(new DateOnly(2026, 1, 12), result.PlannedDays[0].Date);
        Assert.Equal(8, result.PlannedDays[0].TotalHours);
        Assert.Equal(2, result.PlannedDays[0].Assignments.Count);
    }

    [Fact]
    public void PlanMonth_FromRootsAndProfiles_SplitsMajorProfileWorkIntoTargetVisits()
    {
        var device = new KbNode
        {
            NodeId = "device-1",
            Name = "Насос 1",
            NodeType = KbNodeType.Device
        };
        var roots = CreateLevel3Roots(device);

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
                    To3Hours = 18,
                    YearScheduleEntries = new List<KbMaintenanceYearScheduleEntry>
                    {
                        new() { Month = 1, WorkKind = KbMaintenanceWorkKind.To3 }
                    }
                }
            });

        Assert.True(result.IsSuccess);
        Assert.Single(result.PlannedWorkItems);
        Assert.Equal(18, result.RequestedHours);
        Assert.Equal(2, result.PlannedDays.Count);
        Assert.Equal(16, result.PlannedDays[0].TotalHours);
        Assert.Equal(2, result.PlannedDays[1].TotalHours);

        int[] splitHours = result.PlannedDays
            .SelectMany(static day => day.Assignments)
            .Select(static assignment => assignment.Hours)
            .ToArray();
        Assert.Equal(new[] { 8, 8, 2 }, splitHours);
        Assert.Equal(2, result.PlannedDays[0].Assignments.Count);
        Assert.Single(result.PlannedDays[1].Assignments);
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
            CreateLevel3Roots(node),
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
            CreateLevel3Roots(node),
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

    private static KbNode[] CreateLevel3Roots(params KbNode[] nodes) =>
        new[]
        {
            new KbNode
            {
                NodeId = "department-1",
                Name = "Отделение 1",
                NodeType = KbNodeType.Department,
                Children =
                {
                    new KbNode
                    {
                        NodeId = "system-1",
                        Name = "АСУ установки",
                        NodeType = KbNodeType.System,
                        Children = nodes.ToList()
                    }
                }
            }
        };
}
