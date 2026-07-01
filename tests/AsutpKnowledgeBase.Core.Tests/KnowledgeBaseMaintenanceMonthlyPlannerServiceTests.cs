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
        Assert.All(result.PlannedDays, static day => Assert.True(day.TotalHours <= 8));
        Assert.Equal(5, result.PlannedDays.Sum(static day => day.Assignments.Count));
        AssertRouteSystemMixRules(result);
        AssertNoSameOwnerDateConflicts(result);

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
        Assert.Equal(3, cabinetDates.Distinct().Count());
    }

    [Fact]
    public void PlanMonth_SpreadsSameSystemWorkAcrossDifferentDays()
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
        Assert.Equal(9, result.PlannedDays.Sum(static day => day.TotalHours));
        AssertRouteSystemMixRules(result);
        AssertNoSameOwnerDateConflicts(result);

        DateOnly[] systemADates = result.PlannedDays
            .SelectMany(static day => day.Assignments)
            .Where(static assignment => assignment.SystemNodeId == "system-a")
            .Select(static assignment => assignment.Date)
            .Distinct()
            .ToArray();
        Assert.Single(systemADates);

        DateOnly[] systemBDates = result.PlannedDays
            .SelectMany(static day => day.Assignments)
            .Where(static assignment => assignment.SystemNodeId == "system-b")
            .Select(static assignment => assignment.Date)
            .Distinct()
            .ToArray();
        Assert.Single(systemBDates);
    }

    [Fact]
    public void PlanMonth_UsesOneVisitPerSystemPerDay()
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
        Assert.Equal(19, result.PlannedDays.Sum(static day => day.TotalHours));
        AssertRouteSystemMixRules(result);
        AssertNoSameOwnerDateConflicts(result);
    }

    [Fact]
    public void PlanMonth_FillsFirstDayWithPrimarySystemBeforeAddingOtherSystems()
    {
        KnowledgeBaseMaintenanceMonthPlanResult result = _service.PlanMonth(
            2026,
            1,
            totalMonthlyHourBudget: 80,
            new[]
            {
                new KbMaintenanceMonthWorkItem
                {
                    OwnerNodeId = "system-a-device-1",
                    NodeName = "Device A1",
                    SystemNodeId = "system-a",
                    SystemPreorderIndex = 1,
                    OwnerPreorderIndex = 1,
                    WorkKind = KbMaintenanceWorkKind.To1,
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
                    Hours = 6
                },
                new KbMaintenanceMonthWorkItem
                {
                    OwnerNodeId = "system-a-device-3",
                    NodeName = "Device A3",
                    SystemNodeId = "system-a",
                    SystemPreorderIndex = 1,
                    OwnerPreorderIndex = 3,
                    WorkKind = KbMaintenanceWorkKind.To1,
                    Hours = 5
                },
                new KbMaintenanceMonthWorkItem
                {
                    OwnerNodeId = "system-b-device-1",
                    NodeName = "Device B1",
                    SystemNodeId = "system-b",
                    SystemPreorderIndex = 2,
                    OwnerPreorderIndex = 4,
                    WorkKind = KbMaintenanceWorkKind.To1,
                    Hours = 5
                },
                new KbMaintenanceMonthWorkItem
                {
                    OwnerNodeId = "system-c-device-1",
                    NodeName = "Device C1",
                    SystemNodeId = "system-c",
                    SystemPreorderIndex = 3,
                    OwnerPreorderIndex = 5,
                    WorkKind = KbMaintenanceWorkKind.To1,
                    Hours = 4
                }
            });

        Assert.True(result.IsSuccess);
        KbMaintenanceMonthPlanDay firstDay = Assert.Single(result.PlannedDays.Where(static day => day.Date == new DateOnly(2026, 1, 12)));
        Assert.All(firstDay.Assignments, static assignment => Assert.Equal("system-a", assignment.SystemNodeId));
        Assert.Equal(14, firstDay.TotalHours);
        AssertRouteSystemMixRules(result);
        AssertNoSameOwnerDateConflicts(result);
    }

    [Fact]
    public void PlanMonth_KeepsLargeSystemsOnSeparateDaysAndUsesSmallSystemsAsFillers()
    {
        KnowledgeBaseMaintenanceMonthPlanResult result = _service.PlanMonth(
            2026,
            1,
            totalMonthlyHourBudget: 290,
            new[]
            {
                CreateSystemWorkItem("large-a-device-1", "Large A1", "large-a", 1, 1, 3, 8),
                CreateSystemWorkItem("large-a-device-2", "Large A2", "large-a", 1, 2, 3, 6),
                CreateSystemWorkItem("large-a-device-3", "Large A3", "large-a", 1, 3, 3, 5),
                CreateSystemWorkItem("large-b-device-1", "Large B1", "large-b", 2, 4, 4, 8),
                CreateSystemWorkItem("large-b-device-2", "Large B2", "large-b", 2, 5, 4, 6),
                CreateSystemWorkItem("large-b-device-3", "Large B3", "large-b", 2, 6, 4, 5),
                CreateSystemWorkItem("small-c-device-1", "Small C1", "small-c", 3, 7, 2, 4),
                CreateSystemWorkItem("small-c-device-2", "Small C2", "small-c", 3, 8, 2, 3)
            });

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.PlannedDays.Count);
        Assert.DoesNotContain(result.PlannedDays, static day => day.TotalHours >= 38);
        Assert.DoesNotContain(result.PlannedDays, static day => day.TotalHours >= 27);
        Assert.All(result.PlannedDays, static day => Assert.True(day.TotalHours <= 16));
        Assert.Contains(result.PlannedDays, static day => day.TotalHours == 7);
        AssertRouteSystemMixRules(result);
        AssertNoSameOwnerDateConflicts(result);
    }

    [Fact]
    public void PlanMonth_UsesEmptyWorkingDayBeforeOverloadingNearAverageDay()
    {
        KbMaintenanceMonthWorkItem[] largeWorkItems = Enumerable.Range(1, 14)
            .Select(index => CreateSystemWorkItem(
                $"large-{index}-device-1",
                $"Large {index}",
                $"large-{index}",
                index,
                index,
                systemLevel3NodeCount: 3,
                hours: 19))
            .ToArray();

        KbMaintenanceMonthWorkItem smallWorkItem = CreateSystemWorkItem(
            "small-device-1",
            "Small",
            "small",
            99,
            99,
            systemLevel3NodeCount: 2,
            hours: 8);

        KnowledgeBaseMaintenanceMonthPlanResult result = _service.PlanMonth(
            2026,
            1,
            totalMonthlyHourBudget: 290,
            largeWorkItems.Append(smallWorkItem).ToArray());

        Assert.True(result.IsSuccess);
        Assert.Equal(15, result.PlannedDays.Count);
        Assert.Contains(result.PlannedDays, static day => day.TotalHours == 8);
        Assert.DoesNotContain(result.PlannedDays, static day => day.TotalHours == 27);
        Assert.All(result.PlannedDays, static day => Assert.True(day.TotalHours > 0));
        AssertRouteSystemMixRules(result);
        AssertNoSameOwnerDateConflicts(result);
    }

    [Fact]
    public void PlanMonth_PlacesSameSystemContinuationOnNextWorkingDay()
    {
        KnowledgeBaseMaintenanceMonthPlanResult result = _service.PlanMonth(
            2026,
            1,
            totalMonthlyHourBudget: 40,
            new[]
            {
                CreateSystemWorkItem("large-a-device-1", "Large A1", "large-a", 1, 1, 3, 8),
                CreateSystemWorkItem("large-a-device-2", "Large A2", "large-a", 1, 2, 3, 8),
                CreateSystemWorkItem("large-a-device-3", "Large A3", "large-a", 1, 3, 3, 8),
                CreateSystemWorkItem("large-a-device-4", "Large A4", "large-a", 1, 4, 3, 2)
            });

        Assert.True(result.IsSuccess);
        DateOnly[] dates = result.PlannedDays
            .SelectMany(static day => day.Assignments)
            .Where(static assignment => assignment.SystemNodeId == "large-a")
            .Select(static assignment => assignment.Date)
            .Distinct()
            .OrderBy(static date => date)
            .ToArray();

        Assert.Equal(new[] { new DateOnly(2026, 1, 12), new DateOnly(2026, 1, 13) }, dates);
        AssertRouteSystemMixRules(result);
        AssertNoSameOwnerDateConflicts(result);
    }

    [Fact]
    public void PlanMonth_AllowsThirdAndLaterSmallSystemsAsFillers()
    {
        KbMaintenanceMonthWorkItem[] workItems = Enumerable.Range(1, 5)
            .Select(index => CreateSystemWorkItem(
                $"small-{index}-device-1",
                $"Small {index}",
                $"small-{index}",
                index,
                index,
                systemLevel3NodeCount: 2,
                hours: 4))
            .ToArray();

        KnowledgeBaseMaintenanceMonthPlanResult result = _service.PlanMonth(
            2026,
            1,
            totalMonthlyHourBudget: 300,
            workItems);

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.PlannedDays.Count);
        Assert.All(result.PlannedDays, static day => Assert.True(day.TotalHours <= 16));
        Assert.All(result.PlannedDays, static day => Assert.Equal(4, day.TotalHours));
        Assert.Equal(5, result.PlannedDays
            .SelectMany(static day => day.Assignments)
            .Select(static assignment => assignment.SystemNodeId)
            .Distinct()
            .Count());
        AssertRouteSystemMixRules(result);
        AssertNoSameOwnerDateConflicts(result);
    }

    [Fact]
    public void PlanMonth_WhenAverageExceedsShiftLimit_AllowsMinimalOverSixteen()
    {
        KbMaintenanceMonthWorkItem[] workItems = Enumerable.Range(1, 15)
            .Select(index => new KbMaintenanceMonthWorkItem
            {
                OwnerNodeId = $"device-{index}",
                NodeName = $"Device {index}",
                WorkKind = KbMaintenanceWorkKind.To1,
                Hours = 18
            })
            .ToArray();

        KnowledgeBaseMaintenanceMonthPlanResult result = _service.PlanMonth(
            2026,
            1,
            totalMonthlyHourBudget: 270,
            workItems);

        Assert.True(result.IsSuccess);
        Assert.Equal(15, result.PlannedDays.Count);
        Assert.All(result.PlannedDays, static day => Assert.InRange(day.TotalHours, 17, 19));
        AssertNoSameOwnerDateConflicts(result);
    }

    [Fact]
    public void PlanMonth_WhenAllWorkingDaysAreOccupied_UsesLeastBadOverloadInsteadOfFailing()
    {
        KbMaintenanceMonthWorkItem[] largeSystemItems = Enumerable.Range(1, 19)
            .Select(index => CreateSystemWorkItem(
                $"large-{index}-device-1",
                $"Large {index}",
                $"large-{index}",
                index,
                index,
                systemLevel3NodeCount: 3,
                hours: 16))
            .ToArray();
        KbMaintenanceMonthWorkItem smallFiller = CreateSystemWorkItem(
            "small-device-1",
            "ЦСУ",
            "small",
            99,
            99,
            systemLevel3NodeCount: 2,
            hours: 8);

        KnowledgeBaseMaintenanceMonthPlanResult result = _service.PlanMonth(
            2026,
            2,
            totalMonthlyHourBudget: 312,
            largeSystemItems.Append(smallFiller).ToArray());

        Assert.True(result.IsSuccess);
        Assert.Equal(19, result.WorkingDayCount);
        Assert.Equal(19, result.PlannedDays.Count);
        Assert.Contains(result.PlannedDays, static day => day.TotalHours == 24);
        AssertNoSameOwnerDateConflicts(result);
    }

    [Fact]
    public void PlanMonth_RebalancesLowDayWithoutBreakingRouteRules()
    {
        KbMaintenanceMonthWorkItem[] largeSystems = Enumerable.Range(1, 14)
            .Select(index => CreateSystemWorkItem(
                $"large-{index}-device",
                $"Large {index}",
                $"large-{index}",
                index,
                index,
                systemLevel3NodeCount: 3,
                hours: 12))
            .ToArray();
        KbMaintenanceMonthWorkItem[] smallFillers = Enumerable.Range(1, 14)
            .Select(index => CreateSystemWorkItem(
                $"small-{index}-device",
                $"Small {index}",
                $"small-{index}",
                100 + index,
                100 + index,
                systemLevel3NodeCount: 1,
                hours: 4))
            .ToArray();
        KbMaintenanceMonthWorkItem lowDayWork = CreateSystemWorkItem(
            "small-low-device",
            "Small low",
            "small-low",
            200,
            200,
            systemLevel3NodeCount: 1,
            hours: 7);

        KnowledgeBaseMaintenanceMonthPlanResult result = _service.PlanMonth(
            2026,
            1,
            totalMonthlyHourBudget: 300,
            largeSystems.Concat(smallFillers).Append(lowDayWork).ToArray());

        Assert.True(result.IsSuccess);
        Assert.Equal(231, result.RequestedHours);
        Assert.Equal(15, result.WorkingDayCount);
        Assert.Equal(15, result.PlannedDays.Count);
        Assert.All(result.PlannedDays, static day => Assert.InRange(day.TotalHours, 11, 16));
        Assert.DoesNotContain(result.PlannedDays, static day => day.TotalHours == 7);
        AssertRouteSystemMixRules(result);
        AssertNoSameOwnerDateConflicts(result);
    }

    [Fact]
    public void PlanMonth_PacksSameSystemMajorAndLightWorkIntoSingleSystemDay()
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
        Assert.Single(result.PlannedDays);
        Assert.Equal(16, result.PlannedDays.Sum(static day => day.TotalHours));
        Assert.Equal(3, result.PlannedDays[0].Assignments.Count);
        AssertRouteSystemMixRules(result);
        AssertNoSameOwnerDateConflicts(result);
    }

    [Fact]
    public void PlanMonth_SplitsLargeSameSystemWorkIntoSeparateDays()
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
        AssertRouteSystemMixRules(result);
        AssertNoSameOwnerDateConflicts(result);
    }

    [Fact]
    public void PlanMonth_WhenSameSystemNeedsMoreVisitsThanWorkingDays_StillBuildsPlan()
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
        Assert.Equal(15, result.WorkingDayCount);
        Assert.Equal(31, result.PlannedDays.Sum(static day => day.Assignments.Count));
        Assert.Contains(result.PlannedDays, static day => day.Assignments.Count > 1);
        AssertNoSameOwnerDateConflicts(result);
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
        AssertRouteSystemMixRules(result);
        AssertNoSameOwnerDateConflicts(result);
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
        AssertRouteSystemMixRules(result);
        AssertNoSameOwnerDateConflicts(result);
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

    [Fact]
    public void PlanMonth_BalancedV2_KeepsDailyLoadNearAverageWithoutFallback()
    {
        KbMaintenanceMonthWorkItem[] workItems = Enumerable.Range(1, 15)
            .Select(index => CreateSystemWorkItem(
                $"device-{index}",
                $"Узел {index}",
                $"system-{index}",
                index,
                index,
                systemLevel3NodeCount: 1,
                hours: 12))
            .ToArray();

        KnowledgeBaseMaintenanceMonthPlanResult result = _service.PlanMonth(
            2026,
            1,
            totalMonthlyHourBudget: 200,
            workItems,
            KnowledgeBaseMaintenancePlanningMode.BalancedV2);

        Assert.True(result.IsSuccess);
        Assert.Equal(KnowledgeBaseMaintenancePlanningMode.BalancedV2, result.PlanningMode);
        Assert.False(result.UsedFallback);
        Assert.Equal(14, result.PreferredDailyLoadLimitHours);
        Assert.Equal(14, result.DailyLoadLimitHours);
        Assert.Equal(15, result.PlannedDays.Count);
        Assert.All(result.PlannedDays, static day => Assert.InRange(day.TotalHours, 1, 14));
        AssertRouteSystemMixRules(result);
        AssertNoSameOwnerDateConflicts(result);
    }

    [Fact]
    public void PlanMonth_BalancedV2_DoesNotCreateFifteenAndNineWhenTwelveAndTwelveIsPossible()
    {
        var service = CreatePlannerWithWorkingDays(
            new DateOnly(2026, 1, 12),
            new DateOnly(2026, 1, 13));

        KnowledgeBaseMaintenanceMonthPlanResult result = service.PlanMonth(
            2026,
            1,
            totalMonthlyHourBudget: 40,
            new[]
            {
                CreateSystemWorkItem("system-a-device-1", "A1", "system-a", 1, 1, 2, 8),
                CreateSystemWorkItem("system-a-device-2", "A2", "system-a", 1, 2, 2, 7),
                CreateSystemWorkItem("system-b-device-1", "B1", "system-b", 2, 3, 2, 5),
                CreateSystemWorkItem("system-b-device-2", "B2", "system-b", 2, 4, 2, 4)
            },
            KnowledgeBaseMaintenancePlanningMode.BalancedV2);

        Assert.True(result.IsSuccess);
        Assert.False(result.UsedFallback);
        Assert.Equal(2, result.PlannedDays.Count);
        Assert.Equal(new[] { 12, 12 }, result.PlannedDays.Select(static day => day.TotalHours).Order().ToArray());
        AssertRouteSystemMixRules(result);
        AssertNoSameOwnerDateConflicts(result);
    }

    [Fact]
    public void PlanMonth_BalancedV2_FallsBackToSixteenWhenPreferredLimitCannotPlaceMajorWork()
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
                    Hours = 16
                }
            },
            KnowledgeBaseMaintenancePlanningMode.BalancedV2);

        Assert.True(result.IsSuccess);
        Assert.Equal(KnowledgeBaseMaintenancePlanningMode.BalancedV2, result.PlanningMode);
        Assert.True(result.UsedFallback);
        Assert.Equal(4, result.PreferredDailyLoadLimitHours);
        Assert.Equal(16, result.DailyLoadLimitHours);
        Assert.Equal(2, result.PlannedDays.Count);
        Assert.All(result.PlannedDays, static day => Assert.True(day.TotalHours <= 16));
        Assert.Equal(new[] { 8, 8 }, result.PlannedDays.SelectMany(static day => day.Assignments).Select(static assignment => assignment.Hours).ToArray());
        AssertNoSameOwnerDateConflicts(result);
    }

    [Fact]
    public void PlanMonth_SequentialV3_PreservesSystemOrderAndBalancesSoftAverage()
    {
        var service = CreatePlannerWithWorkingDays(
            new DateOnly(2026, 1, 12),
            new DateOnly(2026, 1, 13));

        KnowledgeBaseMaintenanceMonthPlanResult result = service.PlanMonth(
            2026,
            1,
            totalMonthlyHourBudget: 40,
            new[]
            {
                CreateSystemWorkItem("system-a-device-1", "A1", "system-a", 1, 1, 2, 8),
                CreateSystemWorkItem("system-a-device-2", "A2", "system-a", 1, 2, 2, 7),
                CreateSystemWorkItem("system-b-device-1", "B1", "system-b", 2, 3, 2, 5),
                CreateSystemWorkItem("system-b-device-2", "B2", "system-b", 2, 4, 2, 4)
            },
            KnowledgeBaseMaintenancePlanningMode.SequentialV3);

        Assert.True(result.IsSuccess);
        Assert.Equal(KnowledgeBaseMaintenancePlanningMode.SequentialV3, result.PlanningMode);
        Assert.False(result.UsedFallback);
        Assert.Equal(12, result.PreferredDailyLoadLimitHours);
        Assert.Equal(16, result.DailyLoadLimitHours);
        Assert.Equal(new[] { 9, 15 }, result.PlannedDays.Select(static day => day.TotalHours).Order().ToArray());
        Assert.All(result.PlannedDays, static day => Assert.True(day.TotalHours <= 16));
        AssertSequentialSystemOrder(result);
        AssertRouteSystemMixRules(result);
        AssertNoSameOwnerDateConflicts(result);
    }

    [Fact]
    public void PlanMonth_SequentialV3_StartsNextSystemOnSameDayOnlyAfterPreviousSystem()
    {
        var service = CreatePlannerWithWorkingDays(
            new DateOnly(2026, 1, 12),
            new DateOnly(2026, 1, 13));

        KnowledgeBaseMaintenanceMonthPlanResult result = service.PlanMonth(
            2026,
            1,
            totalMonthlyHourBudget: 40,
            new[]
            {
                CreateSystemWorkItem("system-a-device-1", "A1", "system-a", 1, 1, 2, 8),
                CreateSystemWorkItem("system-a-device-2", "A2", "system-a", 1, 2, 2, 4),
                CreateSystemWorkItem("system-b-device-1", "B1", "system-b", 2, 3, 2, 5),
                CreateSystemWorkItem("system-b-device-2", "B2", "system-b", 2, 4, 2, 7)
            },
            KnowledgeBaseMaintenancePlanningMode.SequentialV3);

        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { 12, 12 }, result.PlannedDays.Select(static day => day.TotalHours).Order().ToArray());
        AssertSequentialSystemOrder(result);
        AssertRouteSystemMixRules(result);
        AssertNoSameOwnerDateConflicts(result);
    }

    [Fact]
    public void PlanMonth_SequentialV3_KeepsSmallVisitWholeWhenHardLimitAllows()
    {
        var service = CreatePlannerWithWorkingDays(
            new DateOnly(2026, 1, 12),
            new DateOnly(2026, 1, 13));

        KnowledgeBaseMaintenanceMonthPlanResult result = service.PlanMonth(
            2026,
            1,
            totalMonthlyHourBudget: 24,
            new[]
            {
                CreateSystemWorkItem("device-1", "A1", "system-a", 1, 1, 2, 11),
                CreateSystemWorkItem("device-2", "A2", "system-a", 1, 2, 2, 2),
                CreateSystemWorkItem("device-3", "A3", "system-a", 1, 3, 2, 11)
            },
            KnowledgeBaseMaintenancePlanningMode.SequentialV3);

        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { 11, 13 }, result.PlannedDays.Select(static day => day.TotalHours).Order().ToArray());
        KbMaintenanceMonthPlanAssignment smallVisit = Assert.Single(result.PlannedDays
            .SelectMany(static day => day.Assignments)
            .Where(static assignment => assignment.OwnerNodeId == "device-2"));
        Assert.Equal(new DateOnly(2026, 1, 12), smallVisit.Date);
        Assert.Equal(2, smallVisit.Hours);
        AssertSequentialSystemOrder(result);
        AssertNoSameOwnerDateConflicts(result);
    }

    [Fact]
    public void PlanMonth_SequentialV3_AllowsRepeatedOwnerWhenStrictScheduleCannotFit()
    {
        var service = CreatePlannerWithWorkingDays(
            new DateOnly(2026, 1, 12),
            new DateOnly(2026, 1, 13));

        KnowledgeBaseMaintenanceMonthPlanResult result = service.PlanMonth(
            2026,
            1,
            totalMonthlyHourBudget: 40,
            new[]
            {
                CreateSystemWorkItem("device-1", "A1", "system-a", 1, 1, 2, 8),
                CreateSystemWorkItem("device-1", "A1", "system-a", 1, 1, 2, 8),
                CreateSystemWorkItem("device-1", "A1", "system-a", 1, 1, 2, 8)
            },
            KnowledgeBaseMaintenancePlanningMode.SequentialV3);

        Assert.True(result.IsSuccess);
        Assert.True(result.UsedFallback);
        Assert.Equal(24, result.PlannedDays.Sum(static day => day.TotalHours));
        Assert.All(result.PlannedDays, static day => Assert.True(day.TotalHours <= 16));
        AssertSequentialSystemOrder(result);
    }

    [Fact]
    public void PlanMonth_SequentialV3_RaisesDailyLimitWhenSixteenCannotFit()
    {
        var service = CreatePlannerWithWorkingDays(
            new DateOnly(2026, 1, 12),
            new DateOnly(2026, 1, 13));

        KnowledgeBaseMaintenanceMonthPlanResult result = service.PlanMonth(
            2026,
            1,
            totalMonthlyHourBudget: 16,
            new[]
            {
                CreateSystemWorkItem("system-a-device-1", "A1", "system-a", 1, 1, 2, 8),
                CreateSystemWorkItem("system-a-device-2", "A2", "system-a", 1, 2, 2, 8),
                CreateSystemWorkItem("system-a-device-3", "A3", "system-a", 1, 3, 2, 8),
                CreateSystemWorkItem("system-b-device-1", "B1", "system-b", 2, 4, 2, 8),
                CreateSystemWorkItem("system-b-device-2", "B2", "system-b", 2, 5, 2, 8)
            },
            KnowledgeBaseMaintenancePlanningMode.SequentialV3);

        Assert.True(result.IsSuccess);
        Assert.True(result.UsedFallback);
        Assert.Equal(20, result.DailyLoadLimitHours);
        Assert.Equal(20, result.PreferredDailyLoadLimitHours);
        Assert.Equal(40, result.PlannedDays.Sum(static day => day.TotalHours));
        Assert.All(result.PlannedDays, static day => Assert.True(day.TotalHours <= 20));
        AssertSequentialSystemOrder(result);
    }

    [Fact]
    public void PlanMonth_SequentialV3_UsesEarlierCapacityBeforeLastDayOverload()
    {
        var service = CreatePlannerWithWorkingDays(
            new DateOnly(2026, 1, 12),
            new DateOnly(2026, 1, 13),
            new DateOnly(2026, 1, 14));

        KnowledgeBaseMaintenanceMonthPlanResult result = service.PlanMonth(
            2026,
            1,
            totalMonthlyHourBudget: 45,
            new[]
            {
                CreateSystemWorkItem("device-1", "Device 1", "system-a", 1, 1, 6, 10),
                CreateSystemWorkItem("device-2", "Device 2", "system-a", 1, 2, 6, 8),
                CreateSystemWorkItem("device-3", "Device 3", "system-a", 1, 3, 6, 8),
                CreateSystemWorkItem("device-4", "Device 4", "system-a", 1, 4, 6, 8),
                CreateSystemWorkItem("device-5", "Device 5", "system-a", 1, 5, 6, 8),
                CreateSystemWorkItem("device-6", "Device 6", "system-a", 1, 6, 6, 3)
            },
            KnowledgeBaseMaintenancePlanningMode.SequentialV3);

        Assert.True(result.IsSuccess);
        Assert.Equal(45, result.PlannedDays.Sum(static day => day.TotalHours));
        Assert.Equal(new[] { 16, 16, 13 }, result.PlannedDays.Select(static day => day.TotalHours).ToArray());
        Assert.All(result.PlannedDays, static day => Assert.True(day.TotalHours <= 16));
        AssertSequentialSystemOrder(result);
    }

    [Fact]
    public void PlanMonth_SequentialV3_DistributesLargeMonthWithoutLastDayPileup()
    {
        KbMaintenanceMonthWorkItem[] workItems = Enumerable.Range(1, 71)
            .Select(index => CreateSystemWorkItem(
                $"device-{index:D3}",
                $"Device {index:D3}",
                $"system-{index:D3}",
                index,
                index,
                systemLevel3NodeCount: 1,
                hours: 4))
            .ToArray();

        KnowledgeBaseMaintenanceMonthPlanResult result = _service.PlanMonth(
            2026,
            7,
            totalMonthlyHourBudget: 284,
            workItems,
            KnowledgeBaseMaintenancePlanningMode.SequentialV3);

        Assert.True(result.IsSuccess);
        Assert.Equal(284, result.RequestedHours);
        Assert.Equal(23, result.PlannedDays.Count);
        Assert.All(result.PlannedDays, static day => Assert.InRange(day.TotalHours, 12, 16));
        Assert.True(result.PlannedDays.Last().TotalHours <= 16);
        AssertAssignmentsFollowOwnerOrder(result);
    }

    [Fact]
    public void PlanMonth_SequentialV3_UsesSystemOrderBeforeOwnerOrder()
    {
        var service = CreatePlannerWithWorkingDays(
            new DateOnly(2026, 1, 12),
            new DateOnly(2026, 1, 13));

        KnowledgeBaseMaintenanceMonthPlanResult result = service.PlanMonth(
            2026,
            1,
            totalMonthlyHourBudget: 40,
            new[]
            {
                CreateSystemWorkItem("system-b-device-1", "B1", "system-b", 2, 1, 2, 4),
                CreateSystemWorkItem("system-a-device-1", "A1", "system-a", 1, 10, 2, 4),
                CreateSystemWorkItem("system-b-device-2", "B2", "system-b", 2, 2, 2, 4),
                CreateSystemWorkItem("system-a-device-2", "A2", "system-a", 1, 11, 2, 4)
            },
            KnowledgeBaseMaintenancePlanningMode.SequentialV3);

        Assert.True(result.IsSuccess);
        AssertSequentialSystemOrder(result);
    }

    private static KnowledgeBaseMaintenanceMonthlyPlannerService CreatePlannerWithWorkingDays(params DateOnly[] workingDays)
    {
        Assert.NotEmpty(workingDays);

        int year = workingDays[0].Year;
        int month = workingDays[0].Month;
        var workingDaySet = workingDays.ToHashSet();
        var additionalNonWorkingDays = Enumerable.Range(1, DateTime.DaysInMonth(year, month))
            .Select(day => new DateOnly(year, month, day))
            .Where(date => !workingDaySet.Contains(date))
            .ToList();
        var calendarService = new KnowledgeBaseRussianProductionCalendarService(new[]
        {
            new KbProductionCalendarYear
            {
                Year = year,
                AdditionalNonWorkingDays = additionalNonWorkingDays
            }
        });

        return new KnowledgeBaseMaintenanceMonthlyPlannerService(calendarService);
    }

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

    private static void AssertSequentialSystemOrder(KnowledgeBaseMaintenanceMonthPlanResult result)
    {
        int previousSystemIndex = int.MinValue;
        foreach (KbMaintenanceMonthPlanAssignment assignment in result.PlannedDays
            .OrderBy(static day => day.Date)
            .SelectMany(static day => day.Assignments))
        {
            if (string.IsNullOrWhiteSpace(assignment.SystemNodeId))
                continue;

            int systemIndex = assignment.SystemNodeId switch
            {
                "system-a" => 1,
                "system-b" => 2,
                _ => int.MaxValue
            };

            Assert.True(
                systemIndex >= previousSystemIndex,
                $"Expected sequential system order, got {assignment.SystemNodeId} after index {previousSystemIndex}.");
            previousSystemIndex = systemIndex;
        }
    }

    private static void AssertAssignmentsFollowOwnerOrder(KnowledgeBaseMaintenanceMonthPlanResult result)
    {
        int previousIndex = 0;
        foreach (KbMaintenanceMonthPlanAssignment assignment in result.PlannedDays
            .OrderBy(static day => day.Date)
            .SelectMany(static day => day.Assignments))
        {
            string digits = new string((assignment.OwnerNodeId ?? string.Empty)
                .Where(char.IsDigit)
                .ToArray());
            if (!int.TryParse(digits, out int index))
                continue;

            Assert.True(index >= previousIndex, $"Expected owner order, got {assignment.OwnerNodeId} after {previousIndex}.");
            previousIndex = index;
        }
    }

    private static KbMaintenanceMonthWorkItem CreateSystemWorkItem(
        string ownerNodeId,
        string nodeName,
        string systemNodeId,
        int systemPreorderIndex,
        int ownerPreorderIndex,
        int systemLevel3NodeCount,
        int hours,
        KbMaintenanceWorkKind workKind = KbMaintenanceWorkKind.To1) =>
        new()
        {
            OwnerNodeId = ownerNodeId,
            NodeName = nodeName,
            SystemNodeId = systemNodeId,
            SystemPreorderIndex = systemPreorderIndex,
            OwnerPreorderIndex = ownerPreorderIndex,
            SystemLevel3NodeCount = systemLevel3NodeCount,
            WorkKind = workKind,
            Hours = hours
        };

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
