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
    public void GenerateSingleMonthWorkbook_BuildsWorkbookWithOnlySelectedMonth()
    {
        KbNode[] roots = BuildSingleCabinetRoots();

        KnowledgeBaseMaintenanceWorkbookGenerationResult result = _service.GenerateSingleMonthWorkbook(
            year: 2026,
            month: 5,
            totalMonthlyHourBudget: 20,
            roots,
            BuildSingleCabinetProfile());

        Assert.True(result.IsSuccess);
        byte[] packageBytes = Assert.IsType<byte[]>(result.WorkbookPackage);
        Assert.Equal(new[] { "КЦ (5)" }, ReadSheetNames(packageBytes));
        Assert.Equal("на май 2026 года", ReadCellText(packageBytes, "КЦ (5)", "A13"));
        Assert.Equal("Система 1", ReadCellText(packageBytes, "КЦ (5)", "B17"));
    }

    [Fact]
    public void GenerateAnnualWorkbook_BuildsEstablishedAnnualForm()
    {
        KbNode[] roots = BuildSingleCabinetRoots();

        KnowledgeBaseMaintenanceAnnualWorkbookGenerationResult result = _service.GenerateAnnualWorkbook(
            year: 2026,
            workshopName: "КЦ",
            roots,
            new[]
            {
                new KbMaintenanceScheduleProfile
                {
                    OwnerNodeId = "cabinet-1",
                    IsIncludedInSchedule = true,
                    YearScheduleEntries =
                    {
                        new KbMaintenanceYearScheduleEntry
                        {
                            Month = 1,
                            WorkKind = KbMaintenanceWorkKind.To1,
                            Hours = 2
                        },
                        new KbMaintenanceYearScheduleEntry
                        {
                            Month = 3,
                            WorkKind = KbMaintenanceWorkKind.To3,
                            Hours = 8
                        }
                    }
                }
            });

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.WorkbookModel);
        Assert.Equal(10, result.WorkbookModel!.TotalHours);
        byte[] packageBytes = Assert.IsType<byte[]>(result.WorkbookPackage);
        Assert.Equal(new[] { "КЦ (2)", "Лист1" }, ReadSheetNames(packageBytes));
        Assert.Equal("на 2026 год", ReadCellText(packageBytes, "КЦ (2)", "A10"));
        Assert.Equal("Система 1", ReadCellText(packageBytes, "КЦ (2)", "B16"));
        Assert.Equal("Шкаф 1", ReadCellText(packageBytes, "КЦ (2)", "B17"));
        Assert.Equal("ТО1/2", ReadCellText(packageBytes, "КЦ (2)", "E17"));
        Assert.Equal("ТО3/8", ReadCellText(packageBytes, "КЦ (2)", "I17"));
        Assert.Equal("10", ReadCellText(packageBytes, "КЦ (2)", "AC17"));
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
    public void GenerateYearWorkbook_PacksSameSystemAssignmentsAndChecksRouteSystemMix()
    {
        KnowledgeBaseMaintenanceYearWorkbookGenerationResult result = _service.GenerateYearWorkbook(
            existingWorkbookPackage: null,
            year: 2026,
            totalMonthlyHourBudget: 40,
            BuildTwoCabinetRoots(),
            new[]
            {
                new KbMaintenanceScheduleProfile
                {
                    OwnerNodeId = "cabinet-1",
                    IsIncludedInSchedule = true,
                    YearScheduleEntries =
                    {
                        new KbMaintenanceYearScheduleEntry
                        {
                            Month = 1,
                            WorkKind = KbMaintenanceWorkKind.To3,
                            Hours = 8
                        }
                    }
                },
                new KbMaintenanceScheduleProfile
                {
                    OwnerNodeId = "cabinet-2",
                    IsIncludedInSchedule = true,
                    YearScheduleEntries =
                    {
                        new KbMaintenanceYearScheduleEntry
                        {
                            Month = 1,
                            WorkKind = KbMaintenanceWorkKind.To3,
                            Hours = 8
                        }
                    }
                }
            });

        Assert.True(result.IsSuccess, result.ErrorMessage);
        KnowledgeBaseMaintenanceYearWorkbookGenerationMonthResult januaryResult =
            Assert.Single(result.MonthResults.Where(static monthResult => monthResult.Month == 1));
        Assert.NotNull(januaryResult.PlanResult);
        AssertRouteSystemMixRules(januaryResult.PlanResult!);
        AssertNoSameOwnerDateConflicts(januaryResult.PlanResult!);

        DateOnly[] systemDates = januaryResult.PlanResult!.PlannedDays
            .SelectMany(static day => day.Assignments)
            .Where(static assignment => assignment.SystemNodeId == "system-1")
            .Select(static assignment => assignment.Date)
            .Distinct()
            .ToArray();
        Assert.Single(systemDates);
    }

    [Fact]
    public void GenerateYearWorkbook_WhenExistingPathContainsAnnualWorkbook_StartsFromMonthlyTemplate()
    {
        byte[] annualWorkbook = Assert.IsType<byte[]>(
            _service.GenerateAnnualWorkbook(
                year: 2026,
                workshopName: "КЦ",
                BuildSingleCabinetRoots(),
                new[]
                {
                    new KbMaintenanceScheduleProfile
                    {
                        OwnerNodeId = "cabinet-1",
                        IsIncludedInSchedule = true,
                        YearScheduleEntries =
                        {
                            new KbMaintenanceYearScheduleEntry
                            {
                                Month = 5,
                                WorkKind = KbMaintenanceWorkKind.To1,
                                Hours = 2
                            }
                        }
                    }
                }).WorkbookPackage);

        Assert.Equal(new[] { "КЦ (2)", "Лист1" }, ReadSheetNames(annualWorkbook));

        KnowledgeBaseMaintenanceYearWorkbookGenerationResult result = _service.GenerateYearWorkbook(
            annualWorkbook,
            year: 2026,
            totalMonthlyHourBudget: 20,
            BuildSingleCabinetRoots(),
            BuildSingleCabinetProfile());

        Assert.True(result.IsSuccess);
        byte[] packageBytes = Assert.IsType<byte[]>(result.WorkbookPackage);
        Assert.Contains("КЦ (1)", ReadSheetNames(packageBytes));
        Assert.Contains("КЦ (12)", ReadSheetNames(packageBytes));
        Assert.Equal("на январь 2026 года", ReadCellText(packageBytes, "КЦ (1)", "A12"));
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
    public void GenerateYearWorkbookFromMonth_WhenExistingWorkbookMissesStartMonth_NormalizesFromMonthlyTemplate()
    {
        byte[] octoberWorkbook = Assert.IsType<byte[]>(
            _service.GenerateSingleMonthWorkbook(
                year: 2026,
                month: 10,
                totalMonthlyHourBudget: 20,
                BuildSingleCabinetRoots(systemName: "Старая система", inventoryNumber: "INV-OLD"),
                BuildSingleCabinetProfile()).WorkbookPackage);

        Assert.Equal(new[] { "КЦ (10)" }, ReadSheetNames(octoberWorkbook));
        Assert.DoesNotContain("КЦ (11)", ReadSheetNames(octoberWorkbook));

        KnowledgeBaseMaintenanceYearWorkbookGenerationResult result = _service.GenerateYearWorkbookFromMonth(
            octoberWorkbook,
            year: 2026,
            startMonth: 11,
            totalMonthlyHourBudget: 20,
            BuildSingleCabinetRoots(systemName: "Новая система", inventoryNumber: "INV-NEW"),
            BuildSingleCabinetProfile());

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal(2, result.MonthResults.Count);
        Assert.Equal(Enumerable.Range(11, 2), result.MonthResults.Select(static monthResult => monthResult.Month));

        byte[] packageBytes = Assert.IsType<byte[]>(result.WorkbookPackage);
        IReadOnlyList<string> sheetNames = ReadSheetNames(packageBytes);
        Assert.Contains("КЦ (10)", sheetNames);
        Assert.Contains("КЦ (11)", sheetNames);
        Assert.Contains("КЦ (12)", sheetNames);
        Assert.Contains(
            "Старая система",
            new[] { ReadCellText(packageBytes, "КЦ (10)", "B16"), ReadCellText(packageBytes, "КЦ (10)", "B17") });
        Assert.Equal("Новая система", ReadCellText(packageBytes, "КЦ (11)", "B16"));
        Assert.Equal("Новая система", ReadCellText(packageBytes, "КЦ (12)", "B16"));
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

    private static KbNode[] BuildTwoCabinetRoots() =>
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
                    Name = "Система 1",
                    NodeType = KbNodeType.System,
                    Children =
                    {
                        new KbNode
                        {
                            NodeId = "cabinet-1",
                            Name = "Шкаф 1",
                            NodeType = KbNodeType.Cabinet
                        },
                        new KbNode
                        {
                            NodeId = "cabinet-2",
                            Name = "Шкаф 2",
                            NodeType = KbNodeType.Cabinet
                        }
                    }
                }
            }
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

    private static IReadOnlyList<string> ReadSheetNames(byte[] workbookPackage)
    {
        using var stream = new MemoryStream(workbookPackage);
        using SpreadsheetDocument document = SpreadsheetDocument.Open(stream, false);
        return document.WorkbookPart!.Workbook.Sheets!
            .Elements<Sheet>()
            .Select(static sheet => sheet.Name?.Value ?? string.Empty)
            .ToArray();
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
