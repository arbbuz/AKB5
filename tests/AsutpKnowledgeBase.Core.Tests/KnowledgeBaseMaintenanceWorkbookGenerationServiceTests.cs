using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

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

    [Fact]
    public void GenerateYearWorkbook_BuildsEveryMonthIntoOneWorkbook()
    {
        KbNode[] roots = BuildSingleCabinetRoots();

        KnowledgeBaseMaintenanceYearWorkbookGenerationResult result = _service.GenerateYearWorkbook(
            existingWorkbookPackage: null,
            year: 2026,
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
        Assert.Empty(result.ErrorMessage);
        Assert.Equal(12, result.MonthResults.Count);
        Assert.Equal(Enumerable.Range(1, 12), result.MonthResults.Select(static monthResult => monthResult.Month));
        Assert.All(result.MonthResults, static monthResult =>
        {
            Assert.NotNull(monthResult.PlanResult);
            Assert.NotNull(monthResult.SheetModel);
            Assert.Equal(2, monthResult.SheetModel!.TotalPlannedHours);
        });

        byte[] packageBytes = Assert.IsType<byte[]>(result.WorkbookPackage);
        Assert.NotEmpty(packageBytes);
        Assert.Equal("на январь 2026 года", ReadCellText(packageBytes, "КЦ (1)", "A12"));
        Assert.Equal("на декабрь 2026 года", ReadCellText(packageBytes, "КЦ (12)", "A12"));
        Assert.Equal("Система 1", ReadCellText(packageBytes, "КЦ (1)", "B16"));
        Assert.Equal("Система 1", ReadCellText(packageBytes, "КЦ (12)", "B16"));
    }

    [Fact]
    public void GenerateYearWorkbook_WhenBudgetIsTooSmall_StopsAtFailedMonth()
    {
        KbNode[] roots = BuildSingleCabinetRoots();

        KnowledgeBaseMaintenanceYearWorkbookGenerationResult result = _service.GenerateYearWorkbook(
            existingWorkbookPackage: null,
            year: 2026,
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
        Assert.Equal(1, result.FailedMonth);
        Assert.Empty(result.MonthResults);
        Assert.Null(result.WorkbookPackage);
        Assert.Contains("январь 2026", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("1 ч", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GenerateYearWorkbookFromMonth_PreservesEarlierMonthsAndRewritesThroughDecember()
    {
        byte[] januaryWorkbook = Assert.IsType<byte[]>(
            _service.GenerateMonthWorkbook(
                existingWorkbookPackage: null,
                year: 2026,
                month: 1,
                totalMonthlyHourBudget: 20,
                BuildSingleCabinetRoots(systemName: "Старая система", inventoryNumber: "INV-OLD"),
                BuildSingleCabinetProfile()).WorkbookPackage);

        KnowledgeBaseMaintenanceYearWorkbookGenerationResult result = _service.GenerateYearWorkbookFromMonth(
            januaryWorkbook,
            year: 2026,
            startMonth: 2,
            totalMonthlyHourBudget: 20,
            BuildSingleCabinetRoots(systemName: "Новая система", inventoryNumber: "INV-NEW"),
            BuildSingleCabinetProfile());

        Assert.True(result.IsSuccess);
        Assert.Equal(11, result.MonthResults.Count);
        Assert.Equal(Enumerable.Range(2, 11), result.MonthResults.Select(static monthResult => monthResult.Month));

        byte[] packageBytes = Assert.IsType<byte[]>(result.WorkbookPackage);
        Assert.Equal("Старая система", ReadCellText(packageBytes, "КЦ (1)", "B16"));
        Assert.Equal("Новая система", ReadCellText(packageBytes, "КЦ (2)", "B16"));
        Assert.Equal("Новая система", ReadCellText(packageBytes, "КЦ (12)", "B16"));
        Assert.Equal("INV-OLD", ReadCellText(packageBytes, "КЦ (1)", "D16"));
        Assert.Equal("INV-NEW", ReadCellText(packageBytes, "КЦ (12)", "D16"));
    }

    [Fact]
    public void GenerateYearWorkbookFromMonth_WhenStartMonthIsInvalid_ReturnsFailure()
    {
        KnowledgeBaseMaintenanceYearWorkbookGenerationResult result = _service.GenerateYearWorkbookFromMonth(
            existingWorkbookPackage: null,
            year: 2026,
            startMonth: 13,
            totalMonthlyHourBudget: 20,
            BuildSingleCabinetRoots(),
            BuildSingleCabinetProfile());

        Assert.False(result.IsSuccess);
        Assert.Contains("Стартовый месяц", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(result.MonthResults);
        Assert.Null(result.WorkbookPackage);
    }

    private static KbNode[] BuildSingleCabinetRoots(
        string systemName = "Система 1",
        string inventoryNumber = "INV-01")
    {
        var cabinet = new KbNode
        {
            NodeId = "cabinet-1",
            Name = "Шкаф 1",
            NodeType = KbNodeType.Cabinet
        };

        return
        [
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
                        Name = systemName,
                        NodeType = KbNodeType.System,
                        Details = new KbNodeDetails
                        {
                            InventoryNumber = inventoryNumber
                        },
                        Children = { cabinet }
                    }
                }
            }
        ];
    }

    private static KbMaintenanceScheduleProfile[] BuildSingleCabinetProfile() =>
    [
        new KbMaintenanceScheduleProfile
        {
            OwnerNodeId = "cabinet-1",
            IsIncludedInSchedule = true,
            To1Hours = 2
        }
    ];

    private static string ReadCellText(byte[] workbookPackage, string sheetName, string cellReference)
    {
        using var stream = new MemoryStream(workbookPackage);
        using SpreadsheetDocument document = SpreadsheetDocument.Open(stream, false);
        WorkbookPart workbookPart = document.WorkbookPart
            ?? throw new InvalidOperationException("Workbook part is missing.");
        Sheet sheet = workbookPart.Workbook.Sheets?.Elements<Sheet>()
            .FirstOrDefault(sheet => string.Equals(sheet.Name?.Value, sheetName, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"Sheet '{sheetName}' was not found.");
        WorksheetPart worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id!);
        Cell? cell = worksheetPart.Worksheet.Descendants<Cell>()
            .FirstOrDefault(cell => string.Equals(cell.CellReference?.Value, cellReference, StringComparison.Ordinal));

        return cell == null ? string.Empty : ResolveCellText(workbookPart, cell);
    }

    private static string ResolveCellText(WorkbookPart workbookPart, Cell cell)
    {
        string rawValue = cell.CellValue?.Text ?? string.Empty;
        if (cell.DataType?.Value == CellValues.SharedString &&
            int.TryParse(rawValue, out int sharedStringIndex))
        {
            return workbookPart.SharedStringTablePart?.SharedStringTable
                .Elements<SharedStringItem>()
                .ElementAtOrDefault(sharedStringIndex)
                ?.InnerText ?? string.Empty;
        }

        return cell.InnerText;
    }
}
