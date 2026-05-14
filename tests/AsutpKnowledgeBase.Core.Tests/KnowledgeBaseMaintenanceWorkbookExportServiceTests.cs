using System.Globalization;
using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Validation;

namespace AsutpKnowledgeBase.Core.Tests;

public class KnowledgeBaseMaintenanceWorkbookExportServiceTests
{
    private readonly KnowledgeBaseMaintenanceWorkbookExportService _service = new();

    [Fact]
    public void ExportMonth_UsesTemplateAndWritesSelectedMonthSheet()
    {
        var model = new KbMaintenanceMonthSheetModel
        {
            Year = 2027,
            Month = 1,
            WorkingDayCount = 15,
            TotalPlannedHours = 9,
            DailyTotals =
            {
                new KbMaintenanceMonthSheetDayTotal { DayOfMonth = 12, TotalHours = 6 },
                new KbMaintenanceMonthSheetDayTotal { DayOfMonth = 13, TotalHours = 3 }
            },
            SystemGroups =
            {
                new KbMaintenanceMonthSheetSystemGroup
                {
                    SequenceNumber = 1,
                    SystemNodeId = "system-1",
                    SystemName = "Система 1",
                    InventoryNumber = "INV-01",
                    DetailRows =
                    {
                        new KbMaintenanceMonthSheetDetailRow
                        {
                            OwnerNodeId = "cabinet-1",
                            NodeName = "Шкаф 1",
                            TotalHours = 6,
                            DayCells =
                            {
                                new KbMaintenanceMonthSheetDayCell
                                {
                                    DayOfMonth = 12,
                                    TotalHours = 6,
                                    WorkEntries =
                                    {
                                        new KbMaintenanceMonthSheetWorkEntry { PlanText = "ТО1/2" },
                                        new KbMaintenanceMonthSheetWorkEntry { PlanText = "ТО2/4" }
                                    }
                                }
                            }
                        },
                        new KbMaintenanceMonthSheetDetailRow
                        {
                            OwnerNodeId = "cabinet-2",
                            NodeName = "Шкаф 2",
                            TotalHours = 3,
                            DayCells =
                            {
                                new KbMaintenanceMonthSheetDayCell
                                {
                                    DayOfMonth = 13,
                                    TotalHours = 3,
                                    WorkEntries =
                                    {
                                        new KbMaintenanceMonthSheetWorkEntry { PlanText = "ТО1/3" }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };

        KnowledgeBaseMaintenanceWorkbookExportResult result = _service.ExportMonth(null, model);

        Assert.True(result.IsSuccess);
        byte[] packageBytes = Assert.IsType<byte[]>(result.WorkbookPackage);
        Assert.Empty(result.ErrorMessage);
        AssertValidWorkbook(packageBytes);

        Assert.Equal("на январь 2027 года", ReadCellText(packageBytes, "КЦ (1)", "A12"));
        Assert.Equal("____ _______________ 2027 года", ReadCellText(packageBytes, "КЦ (1)", "AF6"));
        Assert.Equal("9", ReadCellValue(packageBytes, "КЦ (1)", "E8"));
        Assert.Equal("9", ReadCellValue(packageBytes, "КЦ (1)", "AK8"));
        Assert.Equal("6", ReadCellValue(packageBytes, "КЦ (1)", "Q8"));
        Assert.Equal("3", ReadCellValue(packageBytes, "КЦ (1)", "R8"));
        Assert.Equal("0.6", ReadCellValue(packageBytes, "КЦ (1)", "AK9"));
        Assert.Equal("15", ReadCellValue(packageBytes, "КЦ (1)", "AL9"));

        Assert.Equal("25", ReadCellValue(packageBytes, "КЦ (1)", "A16"));
        Assert.Equal("Система 1", ReadCellText(packageBytes, "КЦ (1)", "B16"));
        Assert.Equal("INV-01", ReadCellText(packageBytes, "КЦ (1)", "D16"));
        Assert.Equal("Шкаф 1", ReadCellText(packageBytes, "КЦ (1)", "B18"));
        Assert.Equal("план", ReadCellText(packageBytes, "КЦ (1)", "E18"));
        Assert.Equal("ТО1/2; ТО2/4", ReadCellText(packageBytes, "КЦ (1)", "Q18"));
        Assert.Equal("6", ReadCellValue(packageBytes, "КЦ (1)", "AK18"));
        Assert.Equal("факт", ReadCellText(packageBytes, "КЦ (1)", "E19"));
        Assert.Equal(string.Empty, ReadCellText(packageBytes, "КЦ (1)", "Q19"));

        Assert.Contains("A16:A21", ReadMergedRanges(packageBytes, "КЦ (1)"));
        Assert.Contains("B18:B19", ReadMergedRanges(packageBytes, "КЦ (1)"));
        Assert.Equal("'КЦ (1)'!$A$15:$AQ$25", ReadDefinedName(packageBytes, "_xlnm._FilterDatabase", 0));
        Assert.Equal("'КЦ (1)'!$A$1:$AR$28", ReadDefinedName(packageBytes, "_xlnm.Print_Area", 0));
        Assert.Equal("SUM(AK16:AK21)", ReadCellFormula(packageBytes, "КЦ (1)", "AK22"));
        Assert.False(HasCalculationChain(packageBytes));
        Assert.False(HasSharedFormulas(packageBytes, "КЦ (1)"));
        Assert.Equal("E16", ReadSheetTopLeftCell(packageBytes, "КЦ (1)"));
        Assert.Equal("E16", ReadBottomRightSelection(packageBytes, "КЦ (1)"));
    }

    [Fact]
    public void ExportMonth_WhenWorkbookAlreadyExists_RewritesOnlyRequestedMonthSheet()
    {
        byte[] januaryWorkbook = Assert.IsType<byte[]>(
            _service.ExportMonth(null, CreateModel(2027, 1, "Январская система", "INV-JAN")).WorkbookPackage);

        KnowledgeBaseMaintenanceWorkbookExportResult result = _service.ExportMonth(
            januaryWorkbook,
            CreateModel(2027, 2, "Февральская система", "INV-FEB"));

        Assert.True(result.IsSuccess);
        byte[] packageBytes = Assert.IsType<byte[]>(result.WorkbookPackage);

        Assert.Equal("Январская система", ReadCellText(packageBytes, "КЦ (1)", "B16"));
        Assert.Equal("Февральская система", ReadCellText(packageBytes, "КЦ (2)", "B16"));
        Assert.Equal("на февраль 2027 года", ReadCellText(packageBytes, "КЦ (2)", "A12"));
        Assert.False(HasCalculationChain(packageBytes));
    }

    [Fact]
    public void ExportMonth_WhenRewritingPreviouslyGeneratedMonth_RemovesStaleTailRowsAndStaysValid()
    {
        byte[] firstWorkbook = Assert.IsType<byte[]>(
            _service.ExportMonth(
                null,
                CreateModelWithDetailCount(2027, 4, "First system", "INV-OLD", detailCount: 3)).WorkbookPackage);

        KnowledgeBaseMaintenanceWorkbookExportResult result = _service.ExportMonth(
            firstWorkbook,
            CreateModel(2027, 4, "Updated system", "INV-NEW"));

        Assert.True(result.IsSuccess);
        byte[] packageBytes = Assert.IsType<byte[]>(result.WorkbookPackage);
        AssertValidWorkbook(packageBytes);

        Assert.Equal("Updated system", ReadCellText(packageBytes, "КЦ (4)", "B16"));
        Assert.DoesNotContain(ReadRowIndices(packageBytes, "КЦ (4)"), static rowIndex => rowIndex > 25);
        Assert.False(HasRowBreaks(packageBytes, "КЦ (4)"));
    }

    [Fact]
    public void ExportMonth_WhenWorkbookPackageGrows_UsesExpandableStream()
    {
        KnowledgeBaseMaintenanceWorkbookExportResult result = _service.ExportMonth(
            null,
            CreateModelWithDetailCount(2027, 5, "Large system", "INV-LARGE", detailCount: 300));

        Assert.True(result.IsSuccess);
        byte[] packageBytes = Assert.IsType<byte[]>(result.WorkbookPackage);
        Assert.Empty(result.ErrorMessage);
        AssertValidWorkbook(packageBytes);
        Assert.True(packageBytes.Length > new KnowledgeBaseMaintenanceWorkbookTemplateService().GetTemplatePackage().Length);
    }

    [Fact]
    public void ExportMonth_ForNovemberClearsSharedFormulaArtifacts()
    {
        KnowledgeBaseMaintenanceWorkbookExportResult result =
            _service.ExportMonth(null, CreateModelWithDetailCount(2026, 11, "Ноябрьская система", "INV-NOV", detailCount: 80));

        Assert.True(result.IsSuccess);
        byte[] packageBytes = Assert.IsType<byte[]>(result.WorkbookPackage);
        AssertValidWorkbook(packageBytes);
        Assert.False(HasSharedFormulas(packageBytes, "КЦ (11)"));
    }

    [Fact]
    public void ExportSingleMonth_RemovesOtherMonthSheets()
    {
        KnowledgeBaseMaintenanceWorkbookExportResult result =
            _service.ExportSingleMonth(CreateModel(2027, 5, "Майская система", "INV-MAY"));

        Assert.True(result.IsSuccess);
        byte[] packageBytes = Assert.IsType<byte[]>(result.WorkbookPackage);
        AssertValidWorkbook(packageBytes);

        Assert.Equal(new[] { "КЦ (5)" }, ReadSheetNames(packageBytes));
        Assert.Equal("на май 2027 года", ReadCellText(packageBytes, "КЦ (5)", "A13"));
        Assert.Equal("Майская система", ReadCellText(packageBytes, "КЦ (5)", "B17"));
        Assert.Equal("'КЦ (5)'!$A$16:$AQ$24", ReadDefinedName(packageBytes, "_xlnm._FilterDatabase", 0));
        Assert.Equal("'КЦ (5)'!$A$1:$AN$27", ReadDefinedName(packageBytes, "_xlnm.Print_Area", 0));
        Assert.False(HasSharedFormulas(packageBytes, "КЦ (5)"));
    }

    [Fact]
    public void ExportSingleMonth_ForEveryMonthKeepsServiceRowsHiddenAndHeaderRowsVisible()
    {
        for (int month = 1; month <= 12; month++)
        {
            KnowledgeBaseMaintenanceWorkbookExportResult result =
                _service.ExportSingleMonth(CreateModel(2026, month, $"System {month}", $"INV-{month:D2}"));

            Assert.True(result.IsSuccess);
            byte[] packageBytes = Assert.IsType<byte[]>(result.WorkbookPackage);
            AssertValidWorkbook(packageBytes);
            Assert.Equal(new[] { BuildMonthSheetName(month) }, ReadSheetNames(packageBytes));
            AssertMonthlyHeaderRows(packageBytes, month);
            Assert.False(HasSharedFormulas(packageBytes, BuildMonthSheetName(month)));
        }
    }

    [Fact]
    public void GenerateYearWorkbook_ForEveryMonthKeepsMonthlyFormHeaders()
    {
        var generationService = new KnowledgeBaseMaintenanceWorkbookGenerationService();

        KnowledgeBaseMaintenanceYearWorkbookGenerationResult result =
            generationService.GenerateYearWorkbook(
                null,
                2026,
                0,
                Array.Empty<KbNode>(),
                Array.Empty<KbMaintenanceScheduleProfile>());

        Assert.True(result.IsSuccess);
        byte[] packageBytes = Assert.IsType<byte[]>(result.WorkbookPackage);
        AssertValidWorkbook(packageBytes);

        for (int month = 1; month <= 12; month++)
        {
            AssertMonthlyHeaderRows(packageBytes, month);
            Assert.False(HasSharedFormulas(packageBytes, BuildMonthSheetName(month)));
        }
    }

    [Fact]
    public void GenerateYearWorkbookFromMonth_KeepsRecalculatedMonthlyFormHeaders()
    {
        var generationService = new KnowledgeBaseMaintenanceWorkbookGenerationService();
        byte[] templatePackage = new KnowledgeBaseMaintenanceWorkbookTemplateService().GetTemplatePackage();

        KnowledgeBaseMaintenanceYearWorkbookGenerationResult result =
            generationService.GenerateYearWorkbookFromMonth(
                templatePackage,
                2026,
                6,
                0,
                Array.Empty<KbNode>(),
                Array.Empty<KbMaintenanceScheduleProfile>());

        Assert.True(result.IsSuccess);
        byte[] packageBytes = Assert.IsType<byte[]>(result.WorkbookPackage);
        AssertValidWorkbook(packageBytes);

        for (int month = 6; month <= 12; month++)
        {
            AssertMonthlyHeaderRows(packageBytes, month);
            Assert.False(HasSharedFormulas(packageBytes, BuildMonthSheetName(month)));
        }
    }

    [Fact]
    public void GenerateSingleMonthWorkbook_ForJune2026RecalculatesHeaderDayStyles()
    {
        var generationService = new KnowledgeBaseMaintenanceWorkbookGenerationService();

        KnowledgeBaseMaintenanceWorkbookGenerationResult result =
            generationService.GenerateSingleMonthWorkbook(
                2026,
                6,
                0,
                Array.Empty<KbNode>(),
                Array.Empty<KbMaintenanceScheduleProfile>());

        Assert.True(result.IsSuccess);
        byte[] packageBytes = Assert.IsType<byte[]>(result.WorkbookPackage);
        AssertValidWorkbook(packageBytes);

        uint sundayFillId = ReadCellFillId(packageBytes, "КЦ (6)", "S15");
        uint mondayFillId = ReadCellFillId(packageBytes, "КЦ (6)", "T15");
        uint tuesdayFillId = ReadCellFillId(packageBytes, "КЦ (6)", "U15");
        Assert.NotEqual(sundayFillId, mondayFillId);
        Assert.Equal(tuesdayFillId, mondayFillId);
    }

    [Fact]
    public void ExportSingleMonth_OrdersSystemsByMonthlySourceTemplate()
    {
        var model = new KbMaintenanceMonthSheetModel
        {
            Year = 2026,
            Month = 6,
            WorkingDayCount = 21,
            TotalPlannedHours = 4,
            SystemGroups =
            {
                CreateSystemGroup(1, "АСУ дозатором никелевого купороса", "46739"),
                CreateSystemGroup(2, "Система контроля и регулирования технологических параметров АКТ", "65526")
            }
        };

        KnowledgeBaseMaintenanceWorkbookExportResult result = _service.ExportSingleMonth(model);

        Assert.True(result.IsSuccess);
        byte[] packageBytes = Assert.IsType<byte[]>(result.WorkbookPackage);
        AssertValidWorkbook(packageBytes);
        Assert.Equal("Система контроля и регулирования технологических параметров АКТ", ReadCellText(packageBytes, "КЦ (6)", "B16"));
        Assert.Equal("АСУ дозатором никелевого купороса", ReadCellText(packageBytes, "КЦ (6)", "B20"));
    }

    [Fact]
    public void ExportAnnual_OrdersSystemsByAnnualSourceTemplate()
    {
        var model = new KbMaintenanceAnnualWorkbookModel
        {
            Year = 2026,
            WorkshopName = "КЦ",
            TotalHours = 4,
            SystemGroups =
            {
                CreateAnnualSystemGroup(
                    1,
                    "Система контроля и регулирования технологических параметров ВВК медного отделения. АСУТП ВВК 3-я стадия",
                    "43583"),
                CreateAnnualSystemGroup(
                    2,
                    "Система контроля и регулирования технологических параметров АКТ",
                    "65526")
            }
        };

        KnowledgeBaseMaintenanceWorkbookExportResult result = _service.ExportAnnual(model);

        Assert.True(result.IsSuccess);
        byte[] packageBytes = Assert.IsType<byte[]>(result.WorkbookPackage);
        AssertValidWorkbook(packageBytes);
        Assert.Equal("1", ReadCellValue(packageBytes, "КЦ (2)", "A16"));
        Assert.Equal("Система контроля и регулирования технологических параметров АКТ", ReadCellText(packageBytes, "КЦ (2)", "B16"));
        Assert.Equal("2", ReadCellValue(packageBytes, "КЦ (2)", "A18"));
        Assert.Equal("Система контроля и регулирования технологических параметров ВВК медного отделения. АСУТП ВВК 3-я стадия", ReadCellText(packageBytes, "КЦ (2)", "B18"));
    }

    [Fact]
    public void ExportAnnual_UsesTemplateAndWritesAnnualRows()
    {
        var model = new KbMaintenanceAnnualWorkbookModel
        {
            Year = 2027,
            WorkshopName = "КЦ",
            TotalHours = 14,
            SystemGroups =
            {
                new KbMaintenanceAnnualSystemGroup
                {
                    SequenceNumber = 1,
                    SystemName = "Система 1",
                    InventoryNumber = "SYS-01",
                    DetailRows =
                    {
                        new KbMaintenanceAnnualDetailRow
                        {
                            NodeName = "Шкаф 1",
                            InventoryNumber = "CAB-01",
                            TotalHours = 14,
                            MonthCells =
                            {
                                new KbMaintenanceAnnualMonthCell
                                {
                                    Month = 1,
                                    WorkKind = KbMaintenanceWorkKind.To1,
                                    Hours = 2,
                                    PlanText = "ТО1/2"
                                },
                                new KbMaintenanceAnnualMonthCell
                                {
                                    Month = 3,
                                    WorkKind = KbMaintenanceWorkKind.To3,
                                    Hours = 12,
                                    PlanText = "ТО3/12"
                                }
                            }
                        }
                    }
                }
            }
        };

        KnowledgeBaseMaintenanceWorkbookExportResult result = _service.ExportAnnual(model);

        Assert.True(result.IsSuccess);
        byte[] packageBytes = Assert.IsType<byte[]>(result.WorkbookPackage);
        AssertValidWorkbook(packageBytes);

        Assert.Equal(new[] { "КЦ (2)", "Лист1" }, ReadSheetNames(packageBytes));
        Assert.Equal("на 2027 год", ReadCellText(packageBytes, "КЦ (2)", "A10"));
        Assert.Equal("____ ____________ 2026 года", ReadCellText(packageBytes, "КЦ (2)", "U7"));
        Assert.Equal("25", ReadCellValue(packageBytes, "КЦ (2)", "A16"));
        Assert.Equal("Система 1", ReadCellText(packageBytes, "КЦ (2)", "B16"));
        Assert.Equal("КЦ", ReadCellText(packageBytes, "КЦ (2)", "C16"));
        Assert.Equal("SYS-01", ReadCellText(packageBytes, "КЦ (2)", "D16"));
        Assert.Equal("Шкаф 1", ReadCellText(packageBytes, "КЦ (2)", "B17"));
        Assert.Equal("CAB-01", ReadCellText(packageBytes, "КЦ (2)", "D17"));
        Assert.Equal("ТО1/2", ReadCellText(packageBytes, "КЦ (2)", "E17"));
        Assert.Equal("2", ReadCellValue(packageBytes, "КЦ (2)", "F17"));
        Assert.Equal("ТО3/12", ReadCellText(packageBytes, "КЦ (2)", "I17"));
        Assert.Equal("12", ReadCellValue(packageBytes, "КЦ (2)", "J17"));
        Assert.Equal("14", ReadCellValue(packageBytes, "КЦ (2)", "AC17"));
        Assert.Equal("Итого", ReadCellText(packageBytes, "КЦ (2)", "B18"));
        Assert.Equal("14", ReadCellValue(packageBytes, "КЦ (2)", "AC18"));
        Assert.Contains("A16:A17", ReadMergedRanges(packageBytes, "КЦ (2)"));
        Assert.Equal("'КЦ (2)'!$12:$15", ReadDefinedName(packageBytes, "_xlnm.Print_Titles", 0));
        Assert.Equal("'КЦ (2)'!$A$1:$AQ$29", ReadDefinedName(packageBytes, "_xlnm.Print_Area", 0));
    }

    private static KbMaintenanceMonthSheetModel CreateModel(int year, int month, string systemName, string inventoryNumber) =>
        new()
        {
            Year = year,
            Month = month,
            WorkingDayCount = 10,
            TotalPlannedHours = 2,
            DailyTotals =
            {
                new KbMaintenanceMonthSheetDayTotal { DayOfMonth = 12, TotalHours = 2 }
            },
            SystemGroups =
            {
                new KbMaintenanceMonthSheetSystemGroup
                {
                    SequenceNumber = 1,
                    SystemName = systemName,
                    InventoryNumber = inventoryNumber,
                    DetailRows =
                    {
                        new KbMaintenanceMonthSheetDetailRow
                        {
                            NodeName = "Шкаф",
                            TotalHours = 2,
                            DayCells =
                            {
                                new KbMaintenanceMonthSheetDayCell
                                {
                                    DayOfMonth = 12,
                                    TotalHours = 2,
                                    WorkEntries =
                                    {
                                        new KbMaintenanceMonthSheetWorkEntry { PlanText = "ТО1/2" }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };

    private static KbMaintenanceMonthSheetSystemGroup CreateSystemGroup(
        int sequenceNumber,
        string systemName,
        string inventoryNumber) =>
        new()
        {
            SequenceNumber = sequenceNumber,
            SystemName = systemName,
            InventoryNumber = inventoryNumber,
            DetailRows =
            {
                new KbMaintenanceMonthSheetDetailRow
                {
                    NodeName = "ШУ",
                    TotalHours = 2,
                    DayCells =
                    {
                        new KbMaintenanceMonthSheetDayCell
                        {
                            DayOfMonth = 15,
                            TotalHours = 2,
                            WorkEntries =
                            {
                                new KbMaintenanceMonthSheetWorkEntry { PlanText = "ТО1/2" }
                            }
                        }
                    }
                }
            }
        };

    private static KbMaintenanceAnnualSystemGroup CreateAnnualSystemGroup(
        int sequenceNumber,
        string systemName,
        string inventoryNumber) =>
        new()
        {
            SequenceNumber = sequenceNumber,
            SystemName = systemName,
            InventoryNumber = inventoryNumber,
            DetailRows =
            {
                new KbMaintenanceAnnualDetailRow
                {
                    NodeName = "Шкаф",
                    TotalHours = 2,
                    MonthCells =
                    {
                        new KbMaintenanceAnnualMonthCell
                        {
                            Month = 1,
                            WorkKind = KbMaintenanceWorkKind.To1,
                            Hours = 2,
                            PlanText = "ТО1/2"
                        }
                    }
                }
            }
        };

    private static KbMaintenanceMonthSheetModel CreateModelWithDetailCount(
        int year,
        int month,
        string systemName,
        string inventoryNumber,
        int detailCount)
    {
        var model = new KbMaintenanceMonthSheetModel
        {
            Year = year,
            Month = month,
            WorkingDayCount = 10,
            TotalPlannedHours = detailCount * 2,
            SystemGroups =
            {
                new KbMaintenanceMonthSheetSystemGroup
                {
                    SequenceNumber = 1,
                    SystemName = systemName,
                    InventoryNumber = inventoryNumber
                }
            }
        };

        for (int index = 0; index < detailCount; index++)
        {
            int dayOfMonth = 10 + index;
            model.DailyTotals.Add(new KbMaintenanceMonthSheetDayTotal { DayOfMonth = dayOfMonth, TotalHours = 2 });
            model.SystemGroups[0].DetailRows.Add(
                new KbMaintenanceMonthSheetDetailRow
                {
                    NodeName = $"Cabinet {index + 1}",
                    TotalHours = 2,
                    DayCells =
                    {
                        new KbMaintenanceMonthSheetDayCell
                        {
                            DayOfMonth = dayOfMonth,
                            TotalHours = 2,
                            WorkEntries =
                            {
                                new KbMaintenanceMonthSheetWorkEntry { PlanText = "ТО1/2" }
                            }
                        }
                    }
                });
        }

        return model;
    }

    private static void AssertValidWorkbook(byte[] packageBytes)
    {
        using var stream = new MemoryStream(packageBytes, writable: false);
        using SpreadsheetDocument document = SpreadsheetDocument.Open(stream, false);
        List<ValidationErrorInfo> errors = new OpenXmlValidator().Validate(document).ToList();
        Assert.Empty(errors);
    }

    private static string ReadCellText(byte[] packageBytes, string sheetName, string cellReference)
    {
        using SpreadsheetDocument document = OpenDocument(packageBytes);
        WorksheetPart worksheetPart = GetWorksheetPart(document, sheetName);
        Cell? cell = FindCell(worksheetPart, cellReference);
        if (cell == null)
            return string.Empty;

        if (cell.DataType?.Value == CellValues.InlineString)
        {
            return string.Concat(
                cell.InlineString?.Descendants<Text>().Select(text => text.Text)
                ?? Enumerable.Empty<string>());
        }

        if (cell.DataType?.Value == CellValues.SharedString)
        {
            SharedStringTablePart? sharedStrings = document.WorkbookPart?.SharedStringTablePart;
            if (sharedStrings?.SharedStringTable == null || cell.CellValue == null)
                return string.Empty;

            int sharedStringIndex = int.Parse(cell.CellValue.Text);
            SharedStringItem sharedString = sharedStrings.SharedStringTable.Elements<SharedStringItem>().ElementAt(sharedStringIndex);
            return string.Concat(sharedString.Descendants<Text>().Select(text => text.Text));
        }

        return cell.CellValue?.Text ?? string.Empty;
    }

    private static IReadOnlyList<string> ReadSheetNames(byte[] packageBytes)
    {
        using SpreadsheetDocument document = OpenDocument(packageBytes);
        return document.WorkbookPart!.Workbook.Sheets!
            .Elements<Sheet>()
            .Select(static sheet => sheet.Name?.Value ?? string.Empty)
            .ToArray();
    }

    private static string ReadCellValue(byte[] packageBytes, string sheetName, string cellReference)
    {
        using SpreadsheetDocument document = OpenDocument(packageBytes);
        WorksheetPart worksheetPart = GetWorksheetPart(document, sheetName);
        return FindCell(worksheetPart, cellReference)?.CellValue?.Text ?? string.Empty;
    }

    private static uint ReadCellFillId(byte[] packageBytes, string sheetName, string cellReference)
    {
        using SpreadsheetDocument document = OpenDocument(packageBytes);
        WorksheetPart worksheetPart = GetWorksheetPart(document, sheetName);
        Cell cell = FindCell(worksheetPart, cellReference)
            ?? throw new InvalidOperationException($"Cell '{cellReference}' was not found.");
        uint styleIndex = cell.StyleIndex?.Value ?? 0;
        CellFormats cellFormats = document.WorkbookPart!.WorkbookStylesPart!.Stylesheet.CellFormats!;
        CellFormat cellFormat = (CellFormat)cellFormats.ElementAt((int)styleIndex);
        return cellFormat.FillId?.Value ?? 0;
    }

    private static string ReadCellFormula(byte[] packageBytes, string sheetName, string cellReference)
    {
        using SpreadsheetDocument document = OpenDocument(packageBytes);
        WorksheetPart worksheetPart = GetWorksheetPart(document, sheetName);
        return FindCell(worksheetPart, cellReference)?.CellFormula?.Text ?? string.Empty;
    }

    private static IReadOnlyList<string> ReadMergedRanges(byte[] packageBytes, string sheetName)
    {
        using SpreadsheetDocument document = OpenDocument(packageBytes);
        WorksheetPart worksheetPart = GetWorksheetPart(document, sheetName);
        return worksheetPart.Worksheet.Elements<MergeCells>().FirstOrDefault()?
            .Elements<MergeCell>()
            .Select(cell => cell.Reference?.Value ?? string.Empty)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToArray()
            ?? Array.Empty<string>();
    }

    private static string ReadDefinedName(byte[] packageBytes, string name, uint localSheetId)
    {
        using SpreadsheetDocument document = OpenDocument(packageBytes);
        return document.WorkbookPart!.Workbook.DefinedNames!
            .Elements<DefinedName>()
            .Single(entry => entry.Name?.Value == name && entry.LocalSheetId?.Value == localSheetId)
            .Text;
    }

    private static bool HasCalculationChain(byte[] packageBytes)
    {
        using SpreadsheetDocument document = OpenDocument(packageBytes);
        return document.WorkbookPart?.CalculationChainPart != null;
    }

    private static bool HasRowBreaks(byte[] packageBytes, string sheetName)
    {
        using SpreadsheetDocument document = OpenDocument(packageBytes);
        WorksheetPart worksheetPart = GetWorksheetPart(document, sheetName);
        return worksheetPart.Worksheet.Elements<RowBreaks>().Any();
    }

    private static bool HasSharedFormulas(byte[] packageBytes, string sheetName)
    {
        using SpreadsheetDocument document = OpenDocument(packageBytes);
        WorksheetPart worksheetPart = GetWorksheetPart(document, sheetName);
        return worksheetPart.Worksheet.Descendants<CellFormula>()
            .Any(formula => formula.FormulaType?.Value == CellFormulaValues.Shared);
    }

    private static void AssertMonthlyHeaderRows(byte[] packageBytes, int month)
    {
        string sheetName = BuildMonthSheetName(month);
        uint headerTopRowIndex = FindRowContainingText(packageBytes, sheetName, "N \u043F/\u043F");
        Assert.True(headerTopRowIndex >= 14);
        Assert.True(IsRowHidden(packageBytes, sheetName, 8), $"{sheetName}: row 8 must stay hidden.");
        Assert.True(IsRowHidden(packageBytes, sheetName, 9), $"{sheetName}: row 9 must stay hidden.");

        for (uint rowIndex = 1; rowIndex <= headerTopRowIndex + 1; rowIndex++)
        {
            if (rowIndex is 8 or 9)
                continue;

            Assert.False(IsRowHidden(packageBytes, sheetName, rowIndex), $"{sheetName}: row {rowIndex} must be visible.");
        }
    }

    private static bool IsRowHidden(byte[] packageBytes, string sheetName, uint rowIndex)
    {
        using SpreadsheetDocument document = OpenDocument(packageBytes);
        WorksheetPart worksheetPart = GetWorksheetPart(document, sheetName);
        Row? row = worksheetPart.Worksheet.GetFirstChild<SheetData>()?
            .Elements<Row>()
            .FirstOrDefault(candidate => candidate.RowIndex?.Value == rowIndex);
        return row?.Hidden?.Value == true;
    }

    private static uint FindRowContainingText(byte[] packageBytes, string sheetName, string expectedText)
    {
        using SpreadsheetDocument document = OpenDocument(packageBytes);
        WorksheetPart worksheetPart = GetWorksheetPart(document, sheetName);
        IReadOnlyList<string> sharedStrings = ReadSharedStrings(document);
        foreach (Row row in worksheetPart.Worksheet.GetFirstChild<SheetData>()?.Elements<Row>() ?? Enumerable.Empty<Row>())
        {
            if (row.Elements<Cell>().Any(cell => string.Equals(ReadCellText(document, cell, sharedStrings), expectedText, StringComparison.Ordinal)))
                return row.RowIndex?.Value ?? 0;
        }

        throw new InvalidOperationException($"Text '{expectedText}' was not found on sheet '{sheetName}'.");
    }

    private static string ReadSheetTopLeftCell(byte[] packageBytes, string sheetName)
    {
        using SpreadsheetDocument document = OpenDocument(packageBytes);
        WorksheetPart worksheetPart = GetWorksheetPart(document, sheetName);
        return worksheetPart.Worksheet.Elements<SheetViews>()
            .First()
            .Elements<SheetView>()
            .First()
            .Elements<Pane>()
            .First()
            .TopLeftCell?.Value ?? string.Empty;
    }

    private static string ReadBottomRightSelection(byte[] packageBytes, string sheetName)
    {
        using SpreadsheetDocument document = OpenDocument(packageBytes);
        WorksheetPart worksheetPart = GetWorksheetPart(document, sheetName);
        return worksheetPart.Worksheet.Elements<SheetViews>()
            .First()
            .Elements<SheetView>()
            .First()
            .Elements<Selection>()
            .First(selection => selection.Pane?.Value != PaneValues.TopRight &&
                                selection.Pane?.Value != PaneValues.BottomLeft)
            .ActiveCell?.Value ?? string.Empty;
    }

    private static SpreadsheetDocument OpenDocument(byte[] packageBytes)
    {
        var stream = new MemoryStream(packageBytes, writable: false);
        return SpreadsheetDocument.Open(stream, false);
    }

    private static WorksheetPart GetWorksheetPart(SpreadsheetDocument document, string sheetName)
    {
        Sheet? sheet = document.WorkbookPart!.Workbook.Sheets!
            .Elements<Sheet>()
            .FirstOrDefault(candidate => candidate.Name?.Value == sheetName);
        if (sheet == null && TryExtractMonthFromSheetName(sheetName, out int month))
        {
            string canonicalSheetName = BuildMonthSheetName(month);
            sheet = document.WorkbookPart.Workbook.Sheets!
                .Elements<Sheet>()
                .FirstOrDefault(candidate => candidate.Name?.Value == canonicalSheetName);
        }

        sheet ??= document.WorkbookPart.Workbook.Sheets!
            .Elements<Sheet>()
            .Single(candidate => candidate.Name?.Value == sheetName);
        return (WorksheetPart)document.WorkbookPart.GetPartById(sheet.Id!);
    }

    private static Cell? FindCell(WorksheetPart worksheetPart, string cellReference)
    {
        return worksheetPart.Worksheet.GetFirstChild<SheetData>()?
            .Descendants<Cell>()
            .FirstOrDefault(cell => string.Equals(cell.CellReference?.Value, cellReference, StringComparison.Ordinal));
    }

    private static IReadOnlyList<string> ReadSharedStrings(SpreadsheetDocument document)
    {
        SharedStringTablePart? sharedStrings = document.WorkbookPart?.SharedStringTablePart;
        if (sharedStrings?.SharedStringTable == null)
            return Array.Empty<string>();

        return sharedStrings.SharedStringTable
            .Elements<SharedStringItem>()
            .Select(item => string.Concat(item.Descendants<Text>().Select(text => text.Text)))
            .ToArray();
    }

    private static string ReadCellText(
        SpreadsheetDocument document,
        Cell cell,
        IReadOnlyList<string> sharedStrings)
    {
        if (cell.DataType?.Value == CellValues.InlineString)
        {
            return string.Concat(
                cell.InlineString?.Descendants<Text>().Select(text => text.Text)
                ?? Enumerable.Empty<string>());
        }

        if (cell.DataType?.Value == CellValues.SharedString)
        {
            if (cell.CellValue == null ||
                !int.TryParse(cell.CellValue.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int sharedStringIndex))
            {
                return string.Empty;
            }

            return sharedStringIndex >= 0 && sharedStringIndex < sharedStrings.Count
                ? sharedStrings[sharedStringIndex]
                : string.Empty;
        }

        return cell.CellValue?.Text ?? string.Empty;
    }

    private static IReadOnlyList<uint> ReadRowIndices(byte[] packageBytes, string sheetName)
    {
        using SpreadsheetDocument document = OpenDocument(packageBytes);
        WorksheetPart worksheetPart = GetWorksheetPart(document, sheetName);
        return worksheetPart.Worksheet.GetFirstChild<SheetData>()?
            .Elements<Row>()
            .Select(row => row.RowIndex?.Value ?? 0)
            .ToArray()
            ?? Array.Empty<uint>();
    }

    private static bool TryExtractMonthFromSheetName(string? sheetName, out int month)
    {
        month = 0;
        if (string.IsNullOrWhiteSpace(sheetName))
            return false;

        int openParenIndex = sheetName.LastIndexOf('(');
        int closeParenIndex = sheetName.LastIndexOf(')');
        if (openParenIndex < 0 || closeParenIndex <= openParenIndex)
            return false;

        string monthText = sheetName.Substring(openParenIndex + 1, closeParenIndex - openParenIndex - 1);
        return int.TryParse(monthText, NumberStyles.Integer, CultureInfo.InvariantCulture, out month);
    }

    private static string BuildMonthSheetName(int month) => $"\u041A\u0426 ({month})";
}
