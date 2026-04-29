using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.Core.Tests;

public class KnowledgeBaseMaintenanceMonthSheetModelBuilderServiceTests
{
    private readonly KnowledgeBaseMaintenanceMonthSheetModelBuilderService _service = new();

    [Fact]
    public void Build_GroupsAssignmentsByVisibleLevel2ParentAndPreservesTreeOrder()
    {
        var cabinet1 = new KbNode { NodeId = "cabinet-1", Name = "Шкаф 1", NodeType = KbNodeType.Cabinet };
        var cabinet2 = new KbNode { NodeId = "cabinet-2", Name = "Шкаф 2", NodeType = KbNodeType.Cabinet };
        var cabinet3 = new KbNode { NodeId = "cabinet-3", Name = "Шкаф 3", NodeType = KbNodeType.Cabinet };
        var roots = new[]
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
                        Name = "Система 1",
                        NodeType = KbNodeType.System,
                        Details = new KbNodeDetails { InventoryNumber = "INV-01" },
                        Children = { cabinet1, cabinet2 }
                    },
                    new KbNode
                    {
                        NodeId = "system-2",
                        Name = "Система 2",
                        NodeType = KbNodeType.System,
                        Details = new KbNodeDetails { InventoryNumber = "INV-02" },
                        Children = { cabinet3 }
                    }
                }
            }
        };

        var planResult = new KnowledgeBaseMaintenanceMonthPlanResult
        {
            IsSuccess = true,
            PlannedDays =
            {
                new KbMaintenanceMonthPlanDay
                {
                    Date = new DateOnly(2026, 1, 12),
                    TotalHours = 6,
                    Assignments =
                    {
                        new KbMaintenanceMonthPlanAssignment
                        {
                            Date = new DateOnly(2026, 1, 12),
                            OwnerNodeId = "cabinet-2",
                            NodeName = "Шкаф 2",
                            WorkKind = KbMaintenanceWorkKind.To2,
                            Hours = 4
                        },
                        new KbMaintenanceMonthPlanAssignment
                        {
                            Date = new DateOnly(2026, 1, 12),
                            OwnerNodeId = "cabinet-1",
                            NodeName = "Шкаф 1",
                            WorkKind = KbMaintenanceWorkKind.To1,
                            Hours = 2
                        }
                    }
                },
                new KbMaintenanceMonthPlanDay
                {
                    Date = new DateOnly(2026, 1, 13),
                    TotalHours = 3,
                    Assignments =
                    {
                        new KbMaintenanceMonthPlanAssignment
                        {
                            Date = new DateOnly(2026, 1, 13),
                            OwnerNodeId = "cabinet-3",
                            NodeName = "Шкаф 3",
                            WorkKind = KbMaintenanceWorkKind.To1,
                            Hours = 3
                        }
                    }
                }
            }
        };

        KnowledgeBaseMaintenanceMonthSheetModelBuildResult result = _service.Build(2026, 1, roots, planResult);

        Assert.True(result.IsSuccess);
        KbMaintenanceMonthSheetModel model = Assert.IsType<KbMaintenanceMonthSheetModel>(result.SheetModel);
        Assert.Equal(9, model.TotalPlannedHours);
        Assert.Collection(
            model.DailyTotals,
            day =>
            {
                Assert.Equal(12, day.DayOfMonth);
                Assert.Equal(6, day.TotalHours);
            },
            day =>
            {
                Assert.Equal(13, day.DayOfMonth);
                Assert.Equal(3, day.TotalHours);
            });

        Assert.Equal(2, model.SystemGroups.Count);

        KbMaintenanceMonthSheetSystemGroup firstSystem = model.SystemGroups[0];
        Assert.Equal(1, firstSystem.SequenceNumber);
        Assert.Equal("system-1", firstSystem.SystemNodeId);
        Assert.Equal("Система 1", firstSystem.SystemName);
        Assert.Equal("INV-01", firstSystem.InventoryNumber);
        Assert.Collection(
            firstSystem.DetailRows,
            detail =>
            {
                Assert.Equal("cabinet-1", detail.OwnerNodeId);
                Assert.Equal("Шкаф 1", detail.NodeName);
                KbMaintenanceMonthSheetDayCell dayCell = Assert.Single(detail.DayCells);
                Assert.Equal(12, dayCell.DayOfMonth);
                Assert.Equal(2, dayCell.TotalHours);
                KbMaintenanceMonthSheetWorkEntry entry = Assert.Single(dayCell.WorkEntries);
                Assert.Equal("ТО1/2", entry.PlanText);
            },
            detail =>
            {
                Assert.Equal("cabinet-2", detail.OwnerNodeId);
                KbMaintenanceMonthSheetDayCell dayCell = Assert.Single(detail.DayCells);
                Assert.Equal(12, dayCell.DayOfMonth);
                Assert.Equal(4, dayCell.TotalHours);
                KbMaintenanceMonthSheetWorkEntry entry = Assert.Single(dayCell.WorkEntries);
                Assert.Equal("ТО2/4", entry.PlanText);
            });

        KbMaintenanceMonthSheetSystemGroup secondSystem = model.SystemGroups[1];
        Assert.Equal(2, secondSystem.SequenceNumber);
        Assert.Equal("INV-02", secondSystem.InventoryNumber);
        KbMaintenanceMonthSheetDetailRow secondSystemDetail = Assert.Single(secondSystem.DetailRows);
        Assert.Equal("cabinet-3", secondSystemDetail.OwnerNodeId);
        Assert.Equal(3, secondSystemDetail.TotalHours);
    }

    [Fact]
    public void Build_AggregatesMultipleAssignmentsIntoSameDayCell()
    {
        var module = new KbNode { NodeId = "module-1", Name = "Модуль 1", NodeType = KbNodeType.Module };
        var roots = new[]
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
                        Name = "Система 1",
                        NodeType = KbNodeType.System,
                        Details = new KbNodeDetails { InventoryNumber = "INV-01" },
                        Children = { module }
                    }
                }
            }
        };

        var planResult = new KnowledgeBaseMaintenanceMonthPlanResult
        {
            IsSuccess = true,
            PlannedDays =
            {
                new KbMaintenanceMonthPlanDay
                {
                    Date = new DateOnly(2026, 1, 14),
                    TotalHours = 8,
                    Assignments =
                    {
                        new KbMaintenanceMonthPlanAssignment
                        {
                            Date = new DateOnly(2026, 1, 14),
                            OwnerNodeId = "module-1",
                            NodeName = "Модуль 1",
                            WorkKind = KbMaintenanceWorkKind.To1,
                            Hours = 2
                        },
                        new KbMaintenanceMonthPlanAssignment
                        {
                            Date = new DateOnly(2026, 1, 14),
                            OwnerNodeId = "module-1",
                            NodeName = "Модуль 1",
                            WorkKind = KbMaintenanceWorkKind.To2,
                            Hours = 6
                        }
                    }
                }
            }
        };

        KnowledgeBaseMaintenanceMonthSheetModelBuildResult result = _service.Build(2026, 1, roots, planResult);

        Assert.True(result.IsSuccess);
        KbMaintenanceMonthSheetDetailRow detail = Assert.Single(Assert.Single(result.SheetModel!.SystemGroups).DetailRows);
        KbMaintenanceMonthSheetDayCell dayCell = Assert.Single(detail.DayCells);
        Assert.Equal(14, dayCell.DayOfMonth);
        Assert.Equal(8, dayCell.TotalHours);
        Assert.Collection(
            dayCell.WorkEntries,
            entry => Assert.Equal("ТО1/2", entry.PlanText),
            entry => Assert.Equal("ТО2/6", entry.PlanText));
    }

    [Fact]
    public void Build_FailsWhenAssignedNodeCannotBeMappedToVisibleLevel2Parent()
    {
        var roots = new[]
        {
            new KbNode
            {
                NodeId = "cabinet-1",
                Name = "Шкаф 1",
                NodeType = KbNodeType.Cabinet
            }
        };

        var planResult = new KnowledgeBaseMaintenanceMonthPlanResult
        {
            IsSuccess = true,
            PlannedDays =
            {
                new KbMaintenanceMonthPlanDay
                {
                    Date = new DateOnly(2026, 1, 12),
                    TotalHours = 2,
                    Assignments =
                    {
                        new KbMaintenanceMonthPlanAssignment
                        {
                            Date = new DateOnly(2026, 1, 12),
                            OwnerNodeId = "cabinet-1",
                            NodeName = "Шкаф 1",
                            WorkKind = KbMaintenanceWorkKind.To1,
                            Hours = 2
                        }
                    }
                }
            }
        };

        KnowledgeBaseMaintenanceMonthSheetModelBuildResult result = _service.Build(2026, 1, roots, planResult);

        Assert.False(result.IsSuccess);
        Assert.Contains("Lvl2", result.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_WhenPlannerFailed_PropagatesReadableError()
    {
        KnowledgeBaseMaintenanceMonthSheetModelBuildResult result = _service.Build(
            2026,
            1,
            Array.Empty<KbNode>(),
            new KnowledgeBaseMaintenanceMonthPlanResult
            {
                IsSuccess = false,
                ErrorMessage = "Недостаточно часов."
            });

        Assert.False(result.IsSuccess);
        Assert.Contains("Недостаточно часов", result.ErrorMessage, StringComparison.Ordinal);
    }
}
