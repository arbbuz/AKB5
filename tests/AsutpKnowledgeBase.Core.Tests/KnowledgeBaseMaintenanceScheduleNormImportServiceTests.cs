using System.Globalization;
using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace AsutpKnowledgeBase.Core.Tests;

public class KnowledgeBaseMaintenanceScheduleNormImportServiceTests
{
    private readonly KnowledgeBaseMaintenanceScheduleNormImportService _service = new();

    [Fact]
    public void ImportWorkbook_AggregatesSplitTo3AndMatchesBySystemInventoryAndName()
    {
        var roots = CreateWorkshopRoots(systemInventoryNumber: "SYS-01", equipmentName: "Шкаф 4");
        byte[] workbookBytes = BuildWorkbook(
            ("КЦ (1)", CreateMonthlyRows("Система 1", "SYS-01", "Шкаф 4", dayWorkCells: new[] { (17, "ТО1/2") })),
            ("КЦ (2)", CreateMonthlyRows("Система 1", "SYS-01", "Шкаф 4", dayWorkCells: new[] { (18, "ТО2/4") })),
            ("КЦ (3)", CreateMonthlyRows("Система 1", "SYS-01", "Шкаф 4", dayWorkCells: new[] { (20, "ТО3/4"), (24, "ТО3/4") })));

        KnowledgeBaseMaintenanceScheduleNormImportResult result = _service.ImportWorkbook(
            workbookBytes,
            roots,
            Array.Empty<KbMaintenanceScheduleProfile>());

        Assert.True(result.IsSuccess);
        Assert.Empty(result.UnresolvedEntries);
        Assert.Equal(1, result.ImportedEquipmentCount);
        Assert.Equal(1, result.CreatedProfileCount);
        Assert.Equal(0, result.UpdatedProfileCount);
        Assert.Equal(0, result.UnchangedProfileCount);
        Assert.Equal(0, result.MatchedByInventoryCount);
        Assert.Equal(1, result.MatchedByNameCount);

        KbMaintenanceScheduleProfile profile = Assert.Single(result.MaintenanceScheduleProfiles);
        Assert.Equal("cabinet-1", profile.OwnerNodeId);
        Assert.True(profile.IsIncludedInSchedule);
        Assert.Equal(2, profile.To1Hours);
        Assert.Equal(4, profile.To2Hours);
        Assert.Equal(8, profile.To3Hours);
    }

    [Fact]
    public void ImportWorkbook_UpdatesExistingProfileAndPreservesInclusionFlag()
    {
        var roots = CreateWorkshopRoots(systemInventoryNumber: "SYS-02", equipmentName: "ЩКМ 2");
        byte[] workbookBytes = BuildWorkbook(
            ("КЦ (4)", CreateMonthlyRows("Система 2", "SYS-02", "ЩКМ 2", dayWorkCells: new[] { (21, "ТО3/12") })));

        KnowledgeBaseMaintenanceScheduleNormImportResult result = _service.ImportWorkbook(
            workbookBytes,
            roots,
            new[]
            {
                new KbMaintenanceScheduleProfile
                {
                    MaintenanceProfileId = "maintenance-cabinet-1",
                    OwnerNodeId = "cabinet-1",
                    IsIncludedInSchedule = false,
                    To1Hours = 1,
                    To2Hours = 2,
                    To3Hours = 3
                }
            });

        Assert.True(result.IsSuccess);
        Assert.Empty(result.UnresolvedEntries);
        Assert.Equal(0, result.CreatedProfileCount);
        Assert.Equal(1, result.UpdatedProfileCount);
        Assert.Equal(0, result.UnchangedProfileCount);

        KbMaintenanceScheduleProfile profile = Assert.Single(result.MaintenanceScheduleProfiles);
        Assert.False(profile.IsIncludedInSchedule);
        Assert.Equal(0, profile.To1Hours);
        Assert.Equal(0, profile.To2Hours);
        Assert.Equal(12, profile.To3Hours);
    }

    [Fact]
    public void ImportWorkbook_ParsesAnnualWorkbookByStructureAndImportsManualYearSchedule()
    {
        var roots = CreateWorkshopRoots(
            systemInventoryNumber: "65526",
            equipmentName: "ЩКМ 1");
        byte[] workbookBytes = BuildWorkbook(
            ("КЦ (2)", CreateAnnualRows(
                "Система контроля и регулирования технологических параметров АКТ",
                "065526",
                "ЩКМ 1.",
                monthWorkCells: new[]
                {
                    (1, "ТО1/2"),
                    (2, "ТО2/4"),
                    (3, "ТО3/8"),
                    (4, "ТО1/3")
                })));

        KnowledgeBaseMaintenanceScheduleNormImportResult result = _service.ImportWorkbook(
            workbookBytes,
            roots,
            Array.Empty<KbMaintenanceScheduleProfile>());

        Assert.True(result.IsSuccess);
        Assert.Empty(result.UnresolvedEntries);
        Assert.Equal(1, result.ImportedEquipmentCount);
        Assert.Equal(1, result.CreatedProfileCount);
        Assert.Equal(1, result.MatchedByNameCount);
        Assert.Equal(1, result.YearScheduleAppliedProfileCount);

        KbMaintenanceScheduleProfile profile = Assert.Single(result.MaintenanceScheduleProfiles);
        Assert.Equal("cabinet-1", profile.OwnerNodeId);
        Assert.True(profile.IsIncludedInSchedule);
        Assert.Equal(3, profile.To1Hours);
        Assert.Equal(4, profile.To2Hours);
        Assert.Equal(8, profile.To3Hours);

        KbMaintenanceYearScheduleEntry[] entries = profile.YearScheduleEntries
            .OrderBy(static entry => entry.Month)
            .ToArray();
        Assert.Equal(new[] { 1, 2, 3, 4 }, entries.Select(static entry => entry.Month).ToArray());
        Assert.Equal(
            new[]
            {
                KbMaintenanceWorkKind.To1,
                KbMaintenanceWorkKind.To2,
                KbMaintenanceWorkKind.To3,
                KbMaintenanceWorkKind.To1
            },
            entries.Select(static entry => entry.WorkKind).ToArray());
    }

    [Fact]
    public void ImportWorkbook_WhenNameMatchIsAmbiguous_LeavesEntryUnresolved()
    {
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
                        Children =
                        {
                            new KbNode
                            {
                                NodeId = "cabinet-1",
                                Name = "ШУ АСУТП",
                                NodeType = KbNodeType.Cabinet
                            }
                        }
                    },
                    new KbNode
                    {
                        NodeId = "system-2",
                        Name = "Система 2",
                        NodeType = KbNodeType.System,
                        Children =
                        {
                            new KbNode
                            {
                                NodeId = "cabinet-2",
                                Name = "ШУ АСУТП",
                                NodeType = KbNodeType.Cabinet
                            }
                        }
                    }
                }
            }
        };

        byte[] workbookBytes = BuildWorkbook(
            ("КЦ (5)", CreateMonthlyRows("Система без инвентаря", string.Empty, "ШУ АСУТП", dayWorkCells: new[] { (19, "ТО1/5") })));

        KnowledgeBaseMaintenanceScheduleNormImportResult result = _service.ImportWorkbook(
            workbookBytes,
            roots,
            Array.Empty<KbMaintenanceScheduleProfile>());

        Assert.True(result.IsSuccess);
        Assert.Empty(result.MaintenanceScheduleProfiles);
        Assert.Single(result.UnresolvedEntries);
        Assert.Contains("КЦ (5), строка 18", result.UnresolvedEntries[0], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("несколько совпадений", result.UnresolvedEntries[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ImportWorkbook_WhenWorkbookContainsNoPlanRows_ReturnsFailure()
    {
        byte[] workbookBytes = BuildWorkbook(
            ("КЦ (1)", new[]
            {
                CreateRow(16, (1, "1"), (2, "Система 1"), (4, "SYS-01")),
                CreateRow(17, (2, "Шкаф 1"), (5, "факт"), (17, "2"))
            }));

        KnowledgeBaseMaintenanceScheduleNormImportResult result = _service.ImportWorkbook(
            workbookBytes,
            CreateWorkshopRoots(systemInventoryNumber: "SYS-01", equipmentName: "Шкаф 1"),
            Array.Empty<KbMaintenanceScheduleProfile>());

        Assert.False(result.IsSuccess);
        Assert.Contains("не найдены", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ImportWorkbook_WithHiddenWorkshopRoot_MatchesBySystemInventoryAndCompactEquipmentName()
    {
        IReadOnlyList<KbNode> roots = CreateWrappedWorkshopRoots(
            systemInventoryNumber: "SYS-01",
            equipmentName: "ЩКМ1",
            systemName: "Аппараты колонного типа (АКТ)");

        byte[] workbookBytes = BuildWorkbook(
            ("КЦ (1)", CreateMonthlyRows(
                "Система контроля и регулирования технологических параметров АКТ",
                "SYS-01",
                "ЩКМ 1",
                dayWorkCells: new[] { (17, "ТО1/2") })));

        KnowledgeBaseMaintenanceScheduleNormImportResult result = _service.ImportWorkbook(
            workbookBytes,
            roots,
            Array.Empty<KbMaintenanceScheduleProfile>());

        Assert.True(result.IsSuccess);
        Assert.Empty(result.UnresolvedEntries);
        Assert.Equal(1, result.CreatedProfileCount);
        Assert.Equal(1, result.MatchedByNameCount);

        KbMaintenanceScheduleProfile profile = Assert.Single(result.MaintenanceScheduleProfiles);
        Assert.Equal("cabinet-1", profile.OwnerNodeId);
        Assert.Equal(2, profile.To1Hours);
    }

    [Fact]
    public void ImportWorkbook_TrimsTrailingSystemSuffixFromEquipmentName()
    {
        IReadOnlyList<KbNode> roots = CreateWorkshopRootsWithSystemName(
            systemInventoryNumber: "43363",
            equipmentName: "КН-ЩК1",
            systemName: "АСУ никелевого отделения");

        byte[] workbookBytes = BuildWorkbook(
            ("КЦ (1)", CreateMonthlyRows(
                "АСУ никелевого отделения",
                "43363",
                "КН-ЩК1 АСУ никелевого отделения",
                dayWorkCells: new[] { (18, "ТО2/6") })));

        KnowledgeBaseMaintenanceScheduleNormImportResult result = _service.ImportWorkbook(
            workbookBytes,
            roots,
            Array.Empty<KbMaintenanceScheduleProfile>());

        Assert.True(result.IsSuccess);
        Assert.Empty(result.UnresolvedEntries);

        KbMaintenanceScheduleProfile profile = Assert.Single(result.MaintenanceScheduleProfiles);
        Assert.Equal("cabinet-1", profile.OwnerNodeId);
        Assert.Equal(6, profile.To2Hours);
    }

    [Fact]
    public void ImportWorkbook_TrimsDotSeparatedContextTailFromEquipmentName()
    {
        IReadOnlyList<KbNode> roots = CreateWorkshopRootsWithSystemName(
            systemInventoryNumber: "69439",
            equipmentName: "ШУ1 FFS600",
            systemName: "АСУ линии фасовки HAVER");

        byte[] workbookBytes = BuildWorkbook(
            ("КЦ (1)", CreateMonthlyRows(
                "АСУ линии фасовки HAVER",
                "69439",
                "ШУ1. FFS600.Линия фасовки HAVER",
                dayWorkCells: new[] { (19, "ТО3/8") })));

        KnowledgeBaseMaintenanceScheduleNormImportResult result = _service.ImportWorkbook(
            workbookBytes,
            roots,
            Array.Empty<KbMaintenanceScheduleProfile>());

        Assert.True(result.IsSuccess);
        Assert.Empty(result.UnresolvedEntries);

        KbMaintenanceScheduleProfile profile = Assert.Single(result.MaintenanceScheduleProfiles);
        Assert.Equal("cabinet-1", profile.OwnerNodeId);
        Assert.Equal(8, profile.To3Hours);
    }

    [Fact]
    public void ImportWorkbook_MatchesInventoryNumberWhenWorkbookKeepsLeadingZero()
    {
        IReadOnlyList<KbNode> roots = CreateWorkshopRoots(
            systemInventoryNumber: "43363",
            equipmentName: "Фильтр-пресс ASKM800",
            systemName: "АСУ никелевого отделения",
            equipmentInventoryNumber: "355178");

        byte[] workbookBytes = BuildWorkbook(
            ("КЦ (1)", CreateMonthlyRows(
                "АСУ никелевого отделения",
                "43363",
                "АСУ фильтр-прессом медного отделения №3(Фильтр-пресс ASKM800)",
                equipmentInventoryNumber: "0355178",
                dayWorkCells: new[] { (20, "ТО3/12") })));

        KnowledgeBaseMaintenanceScheduleNormImportResult result = _service.ImportWorkbook(
            workbookBytes,
            roots,
            Array.Empty<KbMaintenanceScheduleProfile>());

        Assert.True(result.IsSuccess);
        Assert.Empty(result.UnresolvedEntries);
        Assert.Equal(1, result.MatchedByInventoryCount);

        KbMaintenanceScheduleProfile profile = Assert.Single(result.MaintenanceScheduleProfiles);
        Assert.Equal("cabinet-1", profile.OwnerNodeId);
        Assert.Equal(12, profile.To3Hours);
    }

    [Fact]
    public void ImportWorkbook_MatchesByParentheticalEquipmentNameInsideSystemInventoryScope()
    {
        IReadOnlyList<KbNode> roots = CreateWorkshopRoots(
            systemInventoryNumber: "43363",
            equipmentName: "Фильтр-пресс камерно-мембранный ФЗГМ 50-1-Н-800/52",
            systemName: "АСУ никелевого отделения");

        byte[] workbookBytes = BuildWorkbook(
            ("КЦ (1)", CreateMonthlyRows(
                "АСУ никелевого отделения",
                "43363",
                "АСУ фильтр-прессом никелевого отделения №10.(Фильтр-пресс камерно-мембранный ФЗГМ 50-1-Н-800/52)",
                dayWorkCells: new[] { (20, "ТО3/10") })));

        KnowledgeBaseMaintenanceScheduleNormImportResult result = _service.ImportWorkbook(
            workbookBytes,
            roots,
            Array.Empty<KbMaintenanceScheduleProfile>());

        Assert.True(result.IsSuccess);
        Assert.Empty(result.UnresolvedEntries);
        Assert.Equal(1, result.MatchedByNameCount);

        KbMaintenanceScheduleProfile profile = Assert.Single(result.MaintenanceScheduleProfiles);
        Assert.Equal("cabinet-1", profile.OwnerNodeId);
        Assert.Equal(10, profile.To3Hours);
    }

    private static IReadOnlyList<KbNode> CreateWorkshopRoots(
        string systemInventoryNumber,
        string equipmentName,
        string systemName = "Система 1",
        string equipmentInventoryNumber = "")
    {
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
                        Name = "Система 1",
                        NodeType = KbNodeType.System,
                        Details = new KbNodeDetails
                        {
                            InventoryNumber = systemInventoryNumber
                        },
                        Children =
                        {
                            new KbNode
                            {
                                NodeId = "cabinet-1",
                                Name = equipmentName,
                                NodeType = KbNodeType.Cabinet,
                                Details = new KbNodeDetails
                                {
                                    InventoryNumber = equipmentInventoryNumber
                                }
                            }
                        }
                    }
                }
            }
        ];
    }

    private static IReadOnlyList<KbNode> CreateWorkshopRootsWithSystemName(
        string systemInventoryNumber,
        string equipmentName,
        string systemName)
    {
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
                            InventoryNumber = systemInventoryNumber
                        },
                        Children =
                        {
                            new KbNode
                            {
                                NodeId = "cabinet-1",
                                Name = equipmentName,
                                NodeType = KbNodeType.Cabinet
                            }
                        }
                    }
                }
            }
        ];
    }

    private static IReadOnlyList<KbNode> CreateWrappedWorkshopRoots(
        string systemInventoryNumber,
        string equipmentName,
        string systemName)
    {
        return
        [
            new KbNode
            {
                NodeId = "workshop-root-1",
                Name = "Купоросный цех (КЦ)",
                LevelIndex = 0,
                NodeType = KbNodeType.WorkshopRoot,
                Children = CreateWorkshopRootsWithSystemName(systemInventoryNumber, equipmentName, systemName).ToList()
            }
        ];
    }

    private static IReadOnlyList<TestRow> CreateMonthlyRows(
        string systemName,
        string systemInventoryNumber,
        string equipmentName,
        IReadOnlyList<(int DayColumnIndex, string WorkCellValue)> dayWorkCells,
        string equipmentInventoryNumber = "")
    {
        var rowCells = new List<(int ColumnIndex, string Value)>
        {
            (1, "1"),
            (2, systemName)
        };

        if (!string.IsNullOrWhiteSpace(systemInventoryNumber))
            rowCells.Add((4, systemInventoryNumber));

        var equipmentRowCells = new List<(int ColumnIndex, string Value)>
        {
            (2, equipmentName),
            (5, "план")
        };
        if (!string.IsNullOrWhiteSpace(equipmentInventoryNumber))
            equipmentRowCells.Add((4, equipmentInventoryNumber));

        foreach ((int dayColumnIndex, string workCellValue) in dayWorkCells)
            equipmentRowCells.Add((dayColumnIndex, workCellValue));

        return
        [
            CreateRow(16, rowCells.ToArray()),
            CreateRow(18, equipmentRowCells.ToArray())
        ];
    }

    private static IReadOnlyList<TestRow> CreateAnnualRows(
        string systemName,
        string systemInventoryNumber,
        string equipmentName,
        IReadOnlyList<(int Month, string WorkCellValue)> monthWorkCells,
        string equipmentInventoryNumber = "")
    {
        var monthHeaderCells = new List<(int ColumnIndex, string Value)>();
        var planHeaderCells = new List<(int ColumnIndex, string Value)>();
        for (int month = 1; month <= 12; month++)
        {
            int planColumnIndex = GetAnnualPlanColumnIndex(month);
            monthHeaderCells.Add((planColumnIndex, month.ToString(CultureInfo.InvariantCulture)));
            planHeaderCells.Add((planColumnIndex, "план"));
            planHeaderCells.Add((planColumnIndex + 1, "выпол."));
        }

        var equipmentRowCells = new List<(int ColumnIndex, string Value)>
        {
            (2, equipmentName)
        };
        if (!string.IsNullOrWhiteSpace(equipmentInventoryNumber))
            equipmentRowCells.Add((4, equipmentInventoryNumber));

        foreach ((int month, string workCellValue) in monthWorkCells)
            equipmentRowCells.Add((GetAnnualPlanColumnIndex(month), workCellValue));

        return
        [
            CreateRow(12, (2, "Наименование системы и электрооборудования"), (4, "Инв.№"), (5, "Месяцы")),
            CreateRow(13, monthHeaderCells.ToArray()),
            CreateRow(14, planHeaderCells.ToArray()),
            CreateRow(15, (1, "1"), (2, "2"), (4, "4")),
            CreateRow(16, (1, "1"), (2, systemName), (4, systemInventoryNumber)),
            CreateRow(17, equipmentRowCells.ToArray())
        ];
    }

    private static int GetAnnualPlanColumnIndex(int month) => 3 + (month * 2);

    private static TestRow CreateRow(uint rowIndex, params (int ColumnIndex, string Value)[] cells) =>
        new(rowIndex, cells);

    private static byte[] BuildWorkbook(params (string SheetName, IReadOnlyList<TestRow> Rows)[] sheets)
    {
        using var stream = new MemoryStream();
        using (SpreadsheetDocument document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
        {
            WorkbookPart workbookPart = document.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();
            Sheets workbookSheets = workbookPart.Workbook.AppendChild(new Sheets());
            uint sheetId = 1;

            foreach ((string sheetName, IReadOnlyList<TestRow> rows) in sheets)
            {
                WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                var sheetData = new SheetData();

                foreach (TestRow row in rows.OrderBy(static row => row.RowIndex))
                {
                    var worksheetRow = new Row { RowIndex = row.RowIndex };
                    foreach ((int columnIndex, string value) in row.Cells.OrderBy(static cell => cell.ColumnIndex))
                        worksheetRow.Append(CreateInlineStringCell(columnIndex, row.RowIndex, value));

                    sheetData.Append(worksheetRow);
                }

                worksheetPart.Worksheet = new Worksheet(sheetData);
                worksheetPart.Worksheet.Save();

                workbookSheets.Append(new Sheet
                {
                    Id = workbookPart.GetIdOfPart(worksheetPart),
                    SheetId = sheetId++,
                    Name = sheetName
                });
            }

            workbookPart.Workbook.Save();
        }

        return stream.ToArray();
    }

    private static Cell CreateInlineStringCell(int columnIndex, uint rowIndex, string value)
    {
        return new Cell
        {
            CellReference = $"{GetColumnName(columnIndex)}{rowIndex}",
            DataType = CellValues.InlineString,
            InlineString = new InlineString(new Text(value ?? string.Empty))
        };
    }

    private static string GetColumnName(int columnIndex)
    {
        var name = string.Empty;
        int currentIndex = columnIndex;
        while (currentIndex > 0)
        {
            int remainder = (currentIndex - 1) % 26;
            name = (char)('A' + remainder) + name;
            currentIndex = (currentIndex - 1) / 26;
        }

        return name;
    }

    private sealed record TestRow(uint RowIndex, IReadOnlyList<(int ColumnIndex, string Value)> Cells);
}
