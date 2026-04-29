using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.Core.Tests;

public class KnowledgeBaseMaintenanceMonthlyPlannerServiceTests
{
    private readonly KnowledgeBaseMaintenanceMonthlyPlannerService _service = new();

    [Fact]
    public void PlanMonth_DistributesHoursAcrossWorkingDaysWithDailyCap()
    {
        var result = _service.PlanMonth(
            2026,
            1,
            totalMonthlyHourBudget: 24,
            new[]
            {
                new KbMaintenanceMonthWorkItem
                {
                    OwnerNodeId = "cabinet-1",
                    NodeName = "Шкаф 1",
                    WorkKind = KbMaintenanceWorkKind.To1,
                    Hours = 8
                },
                new KbMaintenanceMonthWorkItem
                {
                    OwnerNodeId = "cabinet-2",
                    NodeName = "Шкаф 2",
                    WorkKind = KbMaintenanceWorkKind.To2,
                    Hours = 8
                },
                new KbMaintenanceMonthWorkItem
                {
                    OwnerNodeId = "device-1",
                    NodeName = "Насос 1",
                    WorkKind = KbMaintenanceWorkKind.To3,
                    Hours = 3
                }
            });

        Assert.True(result.IsSuccess);
        Assert.Equal(15, result.WorkingDayCount);
        Assert.Equal(19, result.RequestedHours);
        Assert.Equal(3, result.PlannedDays.Count);
        Assert.Equal(new DateOnly(2026, 1, 12), result.PlannedDays[0].Date);
        Assert.Equal(8, result.PlannedDays[0].TotalHours);
        Assert.Equal(8, result.PlannedDays[1].TotalHours);
        Assert.Equal(3, result.PlannedDays[2].TotalHours);
        Assert.All(result.PlannedDays, day => Assert.InRange(day.TotalHours, 0, 8));
    }

    [Fact]
    public void PlanMonth_SkipsTransferredNonWorkingDaysFromProductionCalendar()
    {
        var result = _service.PlanMonth(
            2025,
            5,
            totalMonthlyHourBudget: 8,
            new[]
            {
                new KbMaintenanceMonthWorkItem
                {
                    OwnerNodeId = "cabinet-1",
                    NodeName = "Шкаф 1",
                    WorkKind = KbMaintenanceWorkKind.To1,
                    Hours = 8
                }
            });

        Assert.True(result.IsSuccess);
        var day = Assert.Single(result.PlannedDays);
        Assert.Equal(new DateOnly(2025, 5, 5), day.Date);
    }

    [Fact]
    public void PlanMonth_WhenMonthlyBudgetIsTooSmall_ReturnsReadableFailure()
    {
        var result = _service.PlanMonth(
            2026,
            1,
            totalMonthlyHourBudget: 10,
            new[]
            {
                new KbMaintenanceMonthWorkItem
                {
                    OwnerNodeId = "cabinet-1",
                    NodeName = "Шкаф 1",
                    WorkKind = KbMaintenanceWorkKind.To1,
                    Hours = 12
                }
            });

        Assert.False(result.IsSuccess);
        Assert.Contains("доступно только 10 ч", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PlanMonth_WhenRequestedHoursExceedCalendarCapacity_ReturnsReadableFailure()
    {
        var result = _service.PlanMonth(
            2026,
            1,
            totalMonthlyHourBudget: 500,
            new[]
            {
                new KbMaintenanceMonthWorkItem
                {
                    OwnerNodeId = "cabinet-1",
                    NodeName = "Шкаф 1",
                    WorkKind = KbMaintenanceWorkKind.To1,
                    Hours = 121
                }
            });

        Assert.False(result.IsSuccess);
        Assert.Equal(120, result.CalendarCapacityHours);
        Assert.Contains("доступно только 120 ч", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PlanMonth_WhenThereIsNoWork_ReturnsSuccessfulEmptyPlan()
    {
        var result = _service.PlanMonth(2026, 1, totalMonthlyHourBudget: 40, Array.Empty<KbMaintenanceMonthWorkItem>());

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.RequestedHours);
        Assert.Empty(result.PlannedDays);
    }
}
