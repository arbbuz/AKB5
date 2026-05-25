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
        KbMaintenanceMonthPlanDay day = Assert.Single(result.PlannedDays);
        Assert.Equal(8, day.TotalHours);
        Assert.Equal(2, day.Assignments.Count);
        AssertRouteSystemMixRules(result);
        AssertNoSameOwnerDateConflicts(result);
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
        Assert.Equal(3, result.PlannedDays.Count);
        Assert.Equal(18, result.PlannedDays.Sum(static day => day.TotalHours));

        int[] splitHours = result.PlannedDays
            .SelectMany(static day => day.Assignments)
            .Select(static assignment => assignment.Hours)
            .ToArray();
        Assert.Equal(new[] { 8, 8, 2 }, splitHours);
        Assert.All(result.PlannedDays, static day => Assert.Single(day.Assignments));
        AssertRouteSystemMixRules(result);
        AssertNoSameOwnerDateConflicts(result);
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

    private static void AssertNoSameOwnerDateConflicts(KnowledgeBaseMaintenanceMonthPlanResult result)
    {
        var conflicts = result.PlannedDays
            .SelectMany(static day => day.Assignments.Select(assignment => new
            {
                day.Date,
                assignment.OwnerNodeId
            }))
            .Where(static assignment => !string.IsNullOrWhiteSpace(assignment.OwnerNodeId))
            .GroupBy(static assignment => (assignment.Date, assignment.OwnerNodeId))
            .Where(static group => group.Count() > 1)
            .ToList();

        Assert.Empty(conflicts);
    }

    private static void AssertRouteSystemMixRules(KnowledgeBaseMaintenanceMonthPlanResult result)
    {
        Assert.All(
            result.PlannedDays,
            static day =>
            {
                int largeSystemCount = day.Assignments
                    .Where(static assignment => assignment.SystemLevel3NodeCount > 2)
                    .Select(static assignment => string.IsNullOrWhiteSpace(assignment.SystemNodeId)
                        ? $"owner:{assignment.OwnerNodeId}"
                        : $"system:{assignment.SystemNodeId}")
                    .Where(static key => !string.IsNullOrWhiteSpace(key))
                    .Distinct(StringComparer.Ordinal)
                    .Count();

                Assert.True(largeSystemCount <= 1, $"Expected at most 1 large system on {day.Date}, got {largeSystemCount}.");
            });
    }
}
