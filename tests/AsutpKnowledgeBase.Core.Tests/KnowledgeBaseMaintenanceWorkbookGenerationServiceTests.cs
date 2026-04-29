using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.Core.Tests;

public class KnowledgeBaseMaintenanceWorkbookGenerationServiceTests
{
    private readonly KnowledgeBaseMaintenanceWorkbookGenerationService _service = new();

    [Fact]
    public void GenerateMonthWorkbook_BuildsPlanSheetModelAndWorkbookPackage()
    {
        var cabinet = new KbNode
        {
            NodeId = "cabinet-1",
            Name = "Шкаф 1",
            NodeType = KbNodeType.Cabinet
        };
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
                        Details = new KbNodeDetails
                        {
                            InventoryNumber = "INV-01"
                        },
                        Children = { cabinet }
                    }
                }
            }
        };

        KnowledgeBaseMaintenanceWorkbookGenerationResult result = _service.GenerateMonthWorkbook(
            existingWorkbookPackage: null,
            year: 2026,
            month: 1,
            totalMonthlyHourBudget: 20,
            roots,
            new[]
            {
                new KbMaintenanceScheduleProfile
                {
                    OwnerNodeId = "cabinet-1",
                    IsIncludedInSchedule = true,
                    To1Hours = 2
                }
            });

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.PlanResult);
        Assert.NotNull(result.SheetModel);
        Assert.NotNull(result.WorkbookPackage);
        Assert.True(result.PlanResult!.IsSuccess);
        Assert.Equal(1, result.SheetModel!.Month);
        Assert.Equal(2026, result.SheetModel.Year);
        Assert.Equal(2, result.SheetModel.TotalPlannedHours);
        Assert.Single(result.SheetModel.SystemGroups);
        Assert.NotEmpty(result.WorkbookPackage!);
    }

    [Fact]
    public void GenerateMonthWorkbook_WhenBudgetIsTooSmall_PropagatesPlannerFailure()
    {
        var cabinet = new KbNode
        {
            NodeId = "cabinet-1",
            Name = "Шкаф 1",
            NodeType = KbNodeType.Cabinet
        };
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
                        Children = { cabinet }
                    }
                }
            }
        };

        KnowledgeBaseMaintenanceWorkbookGenerationResult result = _service.GenerateMonthWorkbook(
            existingWorkbookPackage: null,
            year: 2026,
            month: 1,
            totalMonthlyHourBudget: 1,
            roots,
            new[]
            {
                new KbMaintenanceScheduleProfile
                {
                    OwnerNodeId = "cabinet-1",
                    IsIncludedInSchedule = true,
                    To1Hours = 2
                }
            });

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.PlanResult);
        Assert.False(result.PlanResult!.IsSuccess);
        Assert.Null(result.SheetModel);
        Assert.Null(result.WorkbookPackage);
        Assert.Contains("1 ч", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }
}
