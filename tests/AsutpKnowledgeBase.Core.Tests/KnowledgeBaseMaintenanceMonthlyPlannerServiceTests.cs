using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.Core.Tests;

public class KnowledgeBaseMaintenanceMonthlyPlannerServiceTests
{
    private readonly KnowledgeBaseMaintenanceMonthlyPlannerService _service = new();

    [Fact]
    public void PlanMonth_SplitsMajorWorkItemsAcrossMultipleDays()
    {
        KnowledgeBaseMaintenanceMonthPlanResult result = _service.PlanMonth(
            2026,
            1,
            totalMonthlyHourBudget: 40,
            new[]
            {
                new KbMaintenanceMonthWorkItem
                {
                    OwnerNodeId = "cabinet-1",
                    NodeName = "Шкаф 1",
                    WorkKind = KbMaintenanceWorkKind.To2,
                    Hours = 18
                },
                new KbMaintenanceMonthWorkItem
                {
                    OwnerNodeId = "cabinet-2",
                    NodeName = "Шкаф 2",
                    WorkKind = KbMaintenanceWorkKind.To1,
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
        Assert.Equal(29, result.RequestedHours);
        Assert.Equal(5, result.PlannedDays.Count);
        Assert.Equal(new DateOnly(2026, 1, 12), result.PlannedDays[0].Date);
        Assert.Equal(8, result.PlannedDays[0].TotalHours);
        Assert.Equal(new DateOnly(2026, 1, 13), result.PlannedDays[1].Date);
        Assert.Equal(8, result.PlannedDays[1].TotalHours);
        Assert.Equal(new DateOnly(2026, 1, 14), result.PlannedDays[2].Date);
        Assert.Equal(8, result.PlannedDays[2].TotalHours);
        Assert.Equal(new DateOnly(2026, 1, 15), result.PlannedDays[3].Date);
        Assert.Equal(3, result.PlannedDays[3].TotalHours);
        Assert.Equal(new DateOnly(2026, 1, 16), result.PlannedDays[4].Date);
        Assert.Equal(2, result.PlannedDays[4].TotalHours);
        Assert.Equal(5, result.PlannedDays.Sum(static day => day.Assignments.Count));
        Assert.All(result.PlannedDays, static day => Assert.Single(day.Assignments));

        int[] splitHours = result.PlannedDays
            .SelectMany(static day => day.Assignments)
            .Where(static assignment => assignment.OwnerNodeId == "cabinet-1")
            .Select(static assignment => assignment.Hours)
            .ToArray();
        Assert.Equal(new[] { 8, 8, 2 }, splitHours);

        DateOnly[] cabinetDates = result.PlannedDays
            .SelectMany(static day => day.Assignments)
            .Where(static assignment => assignment.OwnerNodeId == "cabinet-1")
            .Select(static assignment => assignment.Date)
            .ToArray();
        Assert.Equal(
            new[] { new DateOnly(2026, 1, 12), new DateOnly(2026, 1, 14), new DateOnly(2026, 1, 16) },
            cabinetDates);
    }

    [Fact]
    public void PlanMonth_PacksSameSystemWorkBeforeMovingToAnotherSystem()
    {
        KnowledgeBaseMaintenanceMonthPlanResult result = _service.PlanMonth(
            2026,
            1,
            totalMonthlyHourBudget: 40,
            new[]
            {
                new KbMaintenanceMonthWorkItem
                {
                    OwnerNodeId = "system-a-device-1",
                    NodeName = "Device A1",
                    SystemNodeId = "system-a",
                    SystemPreorderIndex = 1,
                    OwnerPreorderIndex = 2,
                    WorkKind = KbMaintenanceWorkKind.To1,
                    Hours = 2
                },
                new KbMaintenanceMonthWorkItem
                {
                    OwnerNodeId = "system-a-device-2",
                    NodeName = "Device A2",
                    SystemNodeId = "system-a",
                    SystemPreorderIndex = 1,
                    OwnerPreorderIndex = 3,
                    WorkKind = KbMaintenanceWorkKind.To1,
                    Hours = 3
                },
                new KbMaintenanceMonthWorkItem
                {
                    OwnerNodeId = "system-b-device-1",
                    NodeName = "Device B1",
                    SystemNodeId = "system-b",
                    SystemPreorderIndex = 4,
                    OwnerPreorderIndex = 5,
                    WorkKind = KbMaintenanceWorkKind.To1,
                    Hours = 4
                }
            });

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.PlannedDays.Count);
        Assert.Equal(new DateOnly(2026, 1, 12), result.PlannedDays[0].Date);
        Assert.Equal(5, result.PlannedDays[0].TotalHours);
        Assert.All(result.PlannedDays[0].Assignments, static assignment => Assert.Equal("system-a", assignment.SystemNodeId));
        Assert.Equal(new DateOnly(2026, 1, 13), result.PlannedDays[1].Date);
        KbMaintenanceMonthPlanAssignment secondDayAssignment = Assert.Single(result.PlannedDays[1].Assignments);
        Assert.Equal("system-b", secondDayAssignment.SystemNodeId);
        Assert.Equal(4, result.PlannedDays[1].TotalHours);
    }

    [Fact]
    public void PlanMonth_UsesSixteenHoursAsSoftSameSystemPackingTarget()
    {
        KnowledgeBaseMaintenanceMonthPlanResult result = _service.PlanMonth(
            2026,
            1,
            totalMonthlyHourBudget: 40,
            new[]
            {
                new KbMaintenanceMonthWorkItem
                {
                    OwnerNodeId = "system-a-device-1",
                    NodeName = "Device A1",
                    SystemNodeId = "system-a",
                    SystemPreorderIndex = 1,
                    OwnerPreorderIndex = 2,
                    WorkKind = KbMaintenanceWorkKind.To1,
                    Hours = 8
                },
                new KbMaintenanceMonthWorkItem
                {
                    OwnerNodeId = "system-a-device-2",
                    NodeName = "Device A2",
                    SystemNodeId = "system-a",
                    SystemPreorderIndex = 1,
                    OwnerPreorderIndex = 3,
                    WorkKind = KbMaintenanceWorkKind.To1,
                    Hours = 6
                },
                new KbMaintenanceMonthWorkItem
                {
                    OwnerNodeId = "system-a-device-3",
                    NodeName = "Device A3",
                    SystemNodeId = "system-a",
                    SystemPreorderIndex = 1,
                    OwnerPreorderIndex = 4,
                    WorkKind = KbMaintenanceWorkKind.To1,
                    Hours = 5
                }
            });

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.PlannedDays.Count);
        Assert.Equal(14, result.PlannedDays[0].TotalHours);
        Assert.Equal(5, result.PlannedDays[1].TotalHours);
        Assert.All(result.PlannedDays[0].Assignments, static assignment => Assert.Equal("system-a", assignment.SystemNodeId));
        Assert.All(result.PlannedDays[1].Assignments, static assignment => Assert.Equal("system-a", assignment.SystemNodeId));
    }

    [Fact]
    public void PlanMonth_PacksSameSystemMajorAndLightWorkIntoSingleVisitWhenItFitsTarget()
    {
        KnowledgeBaseMaintenanceMonthPlanResult result = _service.PlanMonth(
            2026,
            1,
            totalMonthlyHourBudget: 40,
            new[]
            {
                new KbMaintenanceMonthWorkItem
                {
                    OwnerNodeId = "system-a-device-1",
                    NodeName = "Device A1",
                    SystemNodeId = "system-a",
                    SystemPreorderIndex = 1,
                    OwnerPreorderIndex = 1,
                    WorkKind = KbMaintenanceWorkKind.To2,
                    Hours = 8
                },
                new KbMaintenanceMonthWorkItem
                {
                    OwnerNodeId = "system-a-device-2",
                    NodeName = "Device A2",
                    SystemNodeId = "system-a",
                    SystemPreorderIndex = 1,
                    OwnerPreorderIndex = 2,
                    WorkKind = KbMaintenanceWorkKind.To1,
                    Hours = 4
                },
                new KbMaintenanceMonthWorkItem
                {
                    OwnerNodeId = "system-a-device-3",
                    NodeName = "Device A3",
                    SystemNodeId = "system-a",
                    SystemPreorderIndex = 1,
                    OwnerPreorderIndex = 3,
                    WorkKind = KbMaintenanceWorkKind.To1,
                    Hours = 4
                }
            });

        Assert.True(result.IsSuccess);
        KbMaintenanceMonthPlanDay plannedDay = Assert.Single(result.PlannedDays);
        Assert.Equal(16, plannedDay.TotalHours);
        Assert.Equal(3, plannedDay.Assignments.Count);
        Assert.All(plannedDay.Assignments, static assignment => Assert.Equal("system-a", assignment.SystemNodeId));
    }

    [Fact]
    public void PlanMonth_SplitsLargeSameSystemWorkIntoMinimumTargetVisits()
    {
        KnowledgeBaseMaintenanceMonthPlanResult result = _service.PlanMonth(
            2026,
            1,
            totalMonthlyHourBudget: 64,
            new[]
            {
                new KbMaintenanceMonthWorkItem
                {
                    OwnerNodeId = "system-a-device-1",
                    NodeName = "Device A1",
                    SystemNodeId = "system-a",
                    SystemPreorderIndex = 1,
                    OwnerPreorderIndex = 1,
                    WorkKind = KbMaintenanceWorkKind.To2,
                    Hours = 8
                },
                new KbMaintenanceMonthWorkItem
                {
                    OwnerNodeId = "system-a-device-2",
                    NodeName = "Device A2",
                    SystemNodeId = "system-a",
                    SystemPreorderIndex = 1,
                    OwnerPreorderIndex = 2,
                    WorkKind = KbMaintenanceWorkKind.To2,
                    Hours = 8
                },
                new KbMaintenanceMonthWorkItem
                {
                    OwnerNodeId = "system-a-device-3",
                    NodeName = "Device A3",
                    SystemNodeId = "system-a",
                    SystemPreorderIndex = 1,
                    OwnerPreorderIndex = 3,
                    WorkKind = KbMaintenanceWorkKind.To1,
                    Hours = 8
                },
                new KbMaintenanceMonthWorkItem
                {
                    OwnerNodeId = "system-a-device-4",
                    NodeName = "Device A4",
                    SystemNodeId = "system-a",
                    SystemPreorderIndex = 1,
                    OwnerPreorderIndex = 4,
                    WorkKind = KbMaintenanceWorkKind.To1,
                    Hours = 8
                }
            });

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.PlannedDays.Count);
        Assert.All(result.PlannedDays, static day => Assert.Equal(16, day.TotalHours));
        Assert.All(
            result.PlannedDays.SelectMany(static day => day.Assignments),
            static assignment => Assert.Equal("system-a", assignment.SystemNodeId));
    }

    [Fact]
    public void PlanMonth_CanExceedSixteenHoursWhenWorkingDaysAreExhausted()
    {
        KbMaintenanceMonthWorkItem[] workItems = Enumerable.Range(1, 31)
            .Select(index => new KbMaintenanceMonthWorkItem
            {
                OwnerNodeId = $"system-a-device-{index}",
                NodeName = $"Device A{index}",
                SystemNodeId = "system-a",
                SystemPreorderIndex = 1,
                OwnerPreorderIndex = index + 1,
                WorkKind = KbMaintenanceWorkKind.To1,
                Hours = 8
            })
            .ToArray();

        KnowledgeBaseMaintenanceMonthPlanResult result = _service.PlanMonth(
            2026,
            1,
            totalMonthlyHourBudget: 300,
            workItems);

        Assert.True(result.IsSuccess);
        Assert.Equal(15, result.PlannedDays.Count);
        Assert.Contains(result.PlannedDays, static day => day.TotalHours > 16);
    }

    [Fact]
    public void PlanMonth_UsesLowerLoadDaysBeforeExceedingSoftTargetForSameSystem()
    {
        KbMaintenanceMonthWorkItem[] majorWorkItems = Enumerable.Range(1, 15)
            .Select(index => new KbMaintenanceMonthWorkItem
            {
                OwnerNodeId = $"system-{index}-device-1",
                NodeName = $"Device {index}",
                SystemNodeId = $"system-{index}",
                SystemPreorderIndex = index,
                OwnerPreorderIndex = 1,
                WorkKind = KbMaintenanceWorkKind.To2,
                Hours = 8
            })
            .ToArray();
        KbMaintenanceMonthWorkItem[] sameSystemLightWork =
        [
            new KbMaintenanceMonthWorkItem
            {
                OwnerNodeId = "system-1-device-2",
                NodeName = "Device 1.2",
                SystemNodeId = "system-1",
                SystemPreorderIndex = 1,
                OwnerPreorderIndex = 2,
                WorkKind = KbMaintenanceWorkKind.To1,
                Hours = 8
            },
            new KbMaintenanceMonthWorkItem
            {
                OwnerNodeId = "system-1-device-3",
                NodeName = "Device 1.3",
                SystemNodeId = "system-1",
                SystemPreorderIndex = 1,
                OwnerPreorderIndex = 3,
                WorkKind = KbMaintenanceWorkKind.To1,
                Hours = 8
            },
            new KbMaintenanceMonthWorkItem
            {
                OwnerNodeId = "system-1-device-4",
                NodeName = "Device 1.4",
                SystemNodeId = "system-1",
                SystemPreorderIndex = 1,
                OwnerPreorderIndex = 4,
                WorkKind = KbMaintenanceWorkKind.To1,
                Hours = 8
            }
        ];

        KnowledgeBaseMaintenanceMonthPlanResult result = _service.PlanMonth(
            2026,
            1,
            totalMonthlyHourBudget: 200,
            majorWorkItems.Concat(sameSystemLightWork).ToArray());

        Assert.True(result.IsSuccess);
        Assert.Equal(15, result.PlannedDays.Count);
        Assert.All(result.PlannedDays, static day => Assert.True(day.TotalHours <= 16));
        Assert.Equal(144, result.PlannedDays.Sum(static day => day.TotalHours));

        DateOnly[] systemOneVisitDates = result.PlannedDays
            .SelectMany(static day => day.Assignments)
            .Where(static assignment => assignment.SystemNodeId == "system-1")
            .Select(static assignment => assignment.Date)
            .Distinct()
            .OrderBy(static date => date)
            .ToArray();
        Assert.Equal(2, systemOneVisitDates.Length);
    }

    [Fact]
    public void PlanMonth_SkipsTransferredNonWorkingDaysFromProductionCalendar()
    {
        KnowledgeBaseMaintenanceMonthPlanResult result = _service.PlanMonth(
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
        KbMaintenanceMonthPlanDay day = Assert.Single(result.PlannedDays);
        Assert.Equal(new DateOnly(2025, 5, 5), day.Date);
    }

    [Fact]
    public void PlanMonth_WhenMonthlyBudgetIsTooSmall_ReturnsReadableFailure()
    {
        KnowledgeBaseMaintenanceMonthPlanResult result = _service.PlanMonth(
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
        Assert.Contains("месячный лимит составляет 10 ч", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PlanMonth_WhenRequestedHoursExceedOldDailyCapacity_StillSucceeds()
    {
        KbMaintenanceMonthWorkItem[] workItems = Enumerable.Range(1, 16)
            .Select(index => new KbMaintenanceMonthWorkItem
            {
                OwnerNodeId = $"device-{index}",
                NodeName = $"Узел {index}",
                WorkKind = KbMaintenanceWorkKind.To1,
                Hours = 8
            })
            .ToArray();

        KnowledgeBaseMaintenanceMonthPlanResult result = _service.PlanMonth(
            2026,
            1,
            totalMonthlyHourBudget: 200,
            workItems);

        Assert.True(result.IsSuccess);
        Assert.Equal(128, result.RequestedHours);
        Assert.Equal(200, result.CalendarCapacityHours);
        Assert.Equal(200, result.AvailableCapacityHours);
        Assert.Contains(result.PlannedDays, static day => day.TotalHours > 8);
    }

    [Fact]
    public void PlanMonth_SpreadsMajorWorksAcrossDifferentDaysWhenPossible()
    {
        KnowledgeBaseMaintenanceMonthPlanResult result = _service.PlanMonth(
            2026,
            1,
            totalMonthlyHourBudget: 40,
            new[]
            {
                new KbMaintenanceMonthWorkItem
                {
                    OwnerNodeId = "device-1",
                    NodeName = "Узел 1",
                    WorkKind = KbMaintenanceWorkKind.To2,
                    Hours = 4
                },
                new KbMaintenanceMonthWorkItem
                {
                    OwnerNodeId = "device-2",
                    NodeName = "Узел 2",
                    WorkKind = KbMaintenanceWorkKind.To2,
                    Hours = 5
                },
                new KbMaintenanceMonthWorkItem
                {
                    OwnerNodeId = "device-3",
                    NodeName = "Узел 3",
                    WorkKind = KbMaintenanceWorkKind.To3,
                    Hours = 6
                },
                new KbMaintenanceMonthWorkItem
                {
                    OwnerNodeId = "device-4",
                    NodeName = "Узел 4",
                    WorkKind = KbMaintenanceWorkKind.To1,
                    Hours = 2
                }
            });

        Assert.True(result.IsSuccess);
        DateOnly[] majorDates = result.PlannedDays
            .Where(static day => day.Assignments.Any(static assignment => assignment.WorkKind is KbMaintenanceWorkKind.To2 or KbMaintenanceWorkKind.To3))
            .Select(static day => day.Date)
            .ToArray();

        Assert.Equal(3, majorDates.Length);
        Assert.Equal(3, majorDates.Distinct().Count());
    }

    [Fact]
    public void PlanMonth_WhenThereIsNoWork_ReturnsSuccessfulEmptyPlan()
    {
        KnowledgeBaseMaintenanceMonthPlanResult result =
            _service.PlanMonth(2026, 1, totalMonthlyHourBudget: 40, Array.Empty<KbMaintenanceMonthWorkItem>());

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.RequestedHours);
        Assert.Empty(result.PlannedDays);
    }
}
