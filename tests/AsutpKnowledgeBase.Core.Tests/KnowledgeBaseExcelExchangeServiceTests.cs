using System.IO.Compression;
using System.Xml.Linq;
using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Validation;

namespace AsutpKnowledgeBase.Core.Tests;

public class KnowledgeBaseExcelExchangeServiceTests
{
    private static readonly string[] NonNodeSheetNames = { "Инструкция", "Meta", "Levels", "Workshops" };

    private static readonly string[] RequiredNodeHeadersV3 =
    {
        "NodeId",
        "ParentNodeId",
        "SiblingOrder",
        "LevelIndex",
        "LevelName",
        "NodeName",
        "Path"
    };

    private static readonly string[] ExportedNodeHeadersV3 =
    {
        "NodeId",
        "ParentNodeId",
        "SiblingOrder",
        "LevelIndex",
        "LevelName",
        "NodeName",
        "Description",
        "Location",
        "PhotoPath",
        "IpAddress",
        "SchemaLink",
        "Path"
    };

    [Fact]
    public void BuildWorkbookPackage_V3ExportCreatesExpectedWorkbookStructure()
    {
        var service = new KnowledgeBaseExcelExchangeService();

        byte[] packageBytes = service.BuildWorkbookPackage(CreateSampleData());
        string[] sheetNames = GetWorksheetNames(packageBytes);
        var metaRows = ReadWorksheetRows(packageBytes, "Meta");
        var workshopRows = ReadWorksheetRows(packageBytes, "Workshops");
        var instructionRows = ReadWorksheetRows(packageBytes, "Инструкция");

        Assert.Equal(6, sheetNames.Length);
        Assert.Equal("Инструкция", sheetNames[0]);
        Assert.Equal(new[] { "Meta", "Levels", "Workshops" }, sheetNames.Skip(1).Take(3).ToArray());
        Assert.DoesNotContain("Nodes", sheetNames);
        Assert.Contains(instructionRows, row => row.SequenceEqual(new[]
        {
            "Можно редактировать",
            "Levels.LevelName, Workshops.WorkshopName, Workshops.IsLastSelected и поля узлов NodeName, Description, Location, PhotoPath, IpAddress, SchemaLink."
        }));
        Assert.Contains(metaRows, row => row.SequenceEqual(new[] { "FormatId", KnowledgeBaseExcelExchangeService.WorkbookFormatId }));
        Assert.Contains(metaRows, row => row.SequenceEqual(new[] { "FormatVersion", KnowledgeBaseExcelExchangeService.WorkbookFormatVersion.ToString() }));
        Assert.Contains(metaRows, row => row.SequenceEqual(new[] { "SchemaVersion", SavedData.CurrentSchemaVersion.ToString() }));
        Assert.Contains(metaRows, row => row.SequenceEqual(new[] { "LastWorkshopId", "W2" }));
        Assert.Contains(metaRows, row => row.SequenceEqual(new[] { "LastWorkshop", "Пустой цех" }));
        Assert.Contains(workshopRows, row => row.SequenceEqual(new[] { "1", "W1", "Цех 1", "FALSE", "NS1" }));
        Assert.Contains(workshopRows, row => row.SequenceEqual(new[] { "2", "W2", "Пустой цех", "TRUE", "NS2" }));
    }

    [Fact]
    public void BuildWorkbookPackage_CreatesPerWorkshopNodeSheetsWithMetaBlockAndHierarchy()
    {
        var service = new KnowledgeBaseExcelExchangeService();

        byte[] packageBytes = service.BuildWorkbookPackage(CreateSampleData());
        var workshopOneMetaRows = ReadWorkshopNodeMetaRows(packageBytes, "W1");
        var workshopOneNodeRows = ReadWorkshopNodeTableRows(packageBytes, "W1");
        var emptyWorkshopMetaRows = ReadWorkshopNodeMetaRows(packageBytes, "W2");
        var emptyWorkshopNodeRows = ReadWorkshopNodeTableRows(packageBytes, "W2");
        string workshopOneSheetName = GetWorkshopNodesSheetName(packageBytes, "W1");
        string emptyWorkshopSheetName = GetWorkshopNodesSheetName(packageBytes, "W2");

        Assert.Contains(workshopOneMetaRows, row => row.SequenceEqual(new[] { "SheetKind", "WorkshopNodes" }));
        Assert.Contains(workshopOneMetaRows, row => row.SequenceEqual(new[] { "WorkshopId", "W1" }));
        Assert.Contains(workshopOneMetaRows, row => row.SequenceEqual(new[] { "NodesSheetKey", "NS1" }));
        Assert.Contains(emptyWorkshopMetaRows, row => row.SequenceEqual(new[] { "WorkshopId", "W2" }));
        Assert.Equal("Узлы - Цех 1", workshopOneSheetName);
        Assert.Equal("Узлы - Пустой цех", emptyWorkshopSheetName);
        Assert.NotEqual("W1", workshopOneSheetName);
        Assert.NotEqual("W2", emptyWorkshopSheetName);
        Assert.Empty(emptyWorkshopNodeRows);

        var headerMap = GetWorkshopNodeHeaderMap(packageBytes, "W1");
        var rootRow = workshopOneNodeRows.Single(row => ReadColumn(row, headerMap, "NodeName") == "Линия 1");
        var childRow = workshopOneNodeRows.Single(row => ReadColumn(row, headerMap, "NodeName") == "Щит 1");
        var secondRootRow = workshopOneNodeRows.Single(row => ReadColumn(row, headerMap, "NodeName") == "Линия 2");

        Assert.Equal(string.Empty, ReadColumn(rootRow, headerMap, "ParentNodeId"));
        Assert.Equal(ReadColumn(rootRow, headerMap, "NodeId"), ReadColumn(childRow, headerMap, "ParentNodeId"));
        Assert.Equal("1", ReadColumn(childRow, headerMap, "SiblingOrder"));
        Assert.Equal("2", ReadColumn(secondRootRow, headerMap, "SiblingOrder"));
        Assert.Equal("Главная линия печи", ReadColumn(rootRow, headerMap, "Description"));
        Assert.Equal("Корпус А", ReadColumn(rootRow, headerMap, "Location"));
        Assert.Equal(@"\\server\photos\line-1.jpg", ReadColumn(rootRow, headerMap, "PhotoPath"));
        Assert.Equal("Линия 1 / Щит 1", ReadColumn(childRow, headerMap, "Path"));
    }

    [Fact]
    public void BuildWorkbookPackage_StoresNumericColumnsAsNumericCells()
    {
        var service = new KnowledgeBaseExcelExchangeService();

        byte[] packageBytes = service.BuildWorkbookPackage(CreateSampleData());

        var levelIndexCell = GetCell(packageBytes, "Levels", rowIndex: 2, cellIndex: 1);
        var workshopOrderCell = GetCell(packageBytes, "Workshops", rowIndex: 2, cellIndex: 1);
        var selectedWorkshopCell = GetCell(packageBytes, "Workshops", rowIndex: 3, cellIndex: 4);
        var nodeIdCell = GetWorkshopNodeCell(packageBytes, "W1", rowIndex: 2, cellIndex: 1);

        Assert.Null(levelIndexCell.Type);
        Assert.Equal("0", levelIndexCell.Value);
        Assert.Null(workshopOrderCell.Type);
        Assert.Equal("1", workshopOrderCell.Value);
        Assert.Equal("b", selectedWorkshopCell.Type);
        Assert.Equal("1", selectedWorkshopCell.Value);
        Assert.Null(nodeIdCell.Type);
        Assert.Equal("1", nodeIdCell.Value);
    }

    [Fact]
    public void BuildWorkbookPackage_HardensWorkbookForManualEditing()
    {
        var service = new KnowledgeBaseExcelExchangeService();

        byte[] packageBytes = service.BuildWorkbookPackage(CreateSampleData());
        string workshopSheetName = GetWorkshopNodesSheetName(packageBytes, "W1");

        Assert.True(IsSheetProtected(packageBytes, "Levels"));
        Assert.True(IsSheetProtected(packageBytes, "Workshops"));
        Assert.True(IsSheetProtected(packageBytes, workshopSheetName));
        Assert.Equal(1U, GetFrozenRowCount(packageBytes, "Инструкция"));
        Assert.Equal(1U, GetFrozenRowCount(packageBytes, "Levels"));
        Assert.Equal(1U, GetFrozenRowCount(packageBytes, "Workshops"));
        Assert.Equal(6U, GetFrozenRowCount(packageBytes, workshopSheetName));
        Assert.True(IsColumnHidden(packageBytes, "Levels", 1));
        Assert.True(IsColumnHidden(packageBytes, "Workshops", 1));
        Assert.True(IsColumnHidden(packageBytes, "Workshops", 2));
        Assert.True(IsColumnHidden(packageBytes, "Workshops", 5));
        Assert.True(IsColumnHidden(packageBytes, workshopSheetName, 1));
        Assert.True(IsColumnHidden(packageBytes, workshopSheetName, 2));
        Assert.True(IsColumnHidden(packageBytes, workshopSheetName, 3));
        Assert.True(IsColumnHidden(packageBytes, workshopSheetName, 4));
        Assert.False(IsCellLocked(packageBytes, "Levels", rowIndex: 2, cellIndex: 2));
        Assert.False(IsCellLocked(packageBytes, "Workshops", rowIndex: 2, cellIndex: 3));
        Assert.False(IsCellLocked(packageBytes, "Workshops", rowIndex: 2, cellIndex: 4));
        Assert.False(IsWorkshopNodeCellLocked(packageBytes, "W1", rowIndex: 2, cellIndex: 6));
        Assert.False(IsWorkshopNodeCellLocked(packageBytes, "W1", rowIndex: 2, cellIndex: 7));
        Assert.False(IsWorkshopNodeCellLocked(packageBytes, "W1", rowIndex: 2, cellIndex: 8));
        Assert.True(IsCellLocked(packageBytes, "Levels", rowIndex: 2, cellIndex: 1));
        Assert.True(IsWorkshopNodeCellLocked(packageBytes, "W1", rowIndex: 2, cellIndex: 1));
    }

    [Fact]
    public void Export_WritesXlsxFile()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            string path = Path.Combine(tempDirectory, "kb-export.xlsx");
            var service = new KnowledgeBaseExcelExchangeService();

            var result = service.Export(CreateSampleData(), path);

            Assert.True(result.IsSuccess);
            Assert.Null(result.ErrorMessage);
            Assert.True(File.Exists(path));

            using var archive = OpenArchive(File.ReadAllBytes(path));
            Assert.NotNull(archive.GetEntry("[Content_Types].xml"));
            Assert.NotNull(archive.GetEntry("xl/workbook.xml"));
            Assert.NotNull(archive.GetEntry("xl/worksheets/sheet1.xml"));
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Export_WhenSuccessful_WritesLogEvents()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            string path = Path.Combine(tempDirectory, "kb-export.xlsx");
            var logger = new InMemoryAppLogger();
            var service = new KnowledgeBaseExcelExchangeService(logger);

            var result = service.Export(CreateSampleData(), path);

            Assert.True(result.IsSuccess);

            var startedEntry = Assert.Single(logger.Entries.Where(entry => entry.EventName == "ExcelExportStarted"));
            Assert.Equal(AppLogLevel.Information, startedEntry.Level);

            var succeededEntry = Assert.Single(logger.Entries.Where(entry => entry.EventName == "ExcelExportSucceeded"));
            Assert.Equal(AppLogLevel.Information, succeededEntry.Level);
            Assert.Equal(path, succeededEntry.Properties["path"]);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void BuildWorkbookPackage_V3ExportCreatesOneNodeSheetPerWorkshop()
    {
        var service = new KnowledgeBaseExcelExchangeService();
        var sourceData = CreateComplexRoundTripData();

        byte[] packageBytes = service.BuildWorkbookPackage(sourceData);
        var workshopRows = ReadWorksheetRows(packageBytes, "Workshops");
        var nodeSheets = GetWorkshopNodeSheetInfos(packageBytes);

        Assert.Equal(sourceData.Workshops.Count, workshopRows.Count);
        Assert.Equal(sourceData.Workshops.Count, nodeSheets.Count);

        foreach (var workshopRow in workshopRows)
        {
            string workshopId = workshopRow[1];
            string workshopName = workshopRow[2];
            string nodesSheetKey = workshopRow[4];

            var nodeSheet = nodeSheets.Single(sheet =>
                string.Equals(sheet.WorkshopId, workshopId, StringComparison.Ordinal) &&
                string.Equals(sheet.NodesSheetKey, nodesSheetKey, StringComparison.Ordinal));

            Assert.Equal("WorkshopNodes", nodeSheet.SheetKind);
            Assert.False(string.IsNullOrWhiteSpace(nodeSheet.SheetName));
            Assert.DoesNotContain(nodeSheet.SheetName, NonNodeSheetNames);

            if (sourceData.Workshops[workshopName].Count == 0)
                Assert.Equal(0, nodeSheet.NodeRowCount);
        }
    }

    [Fact]
    public void BuildWorkbookPackage_V3ExportPassesOpenXmlValidation()
    {
        var service = new KnowledgeBaseExcelExchangeService();
        var sourceData = CreateComplexRoundTripData();

        byte[] packageBytes = service.BuildWorkbookPackage(sourceData);

        using var stream = new MemoryStream(packageBytes, writable: false);
        using var document = SpreadsheetDocument.Open(stream, false);

        var validator = new OpenXmlValidator();
        var errors = validator.Validate(document)
            .Select(error => error.Description)
            .ToList();

        Assert.True(errors.Count == 0, string.Join(Environment.NewLine, errors));
        AssertSuccessfulImportMatches(sourceData, service.ImportFromPackage(packageBytes));
    }

    [Fact]
    public void Import_FullRoundTripWithoutLossOfStructure()
    {
        var service = new KnowledgeBaseExcelExchangeService();
        var sourceData = CreateComplexRoundTripData();

        byte[] packageBytes = service.BuildWorkbookPackage(sourceData);
        AssertSuccessfulImportMatches(sourceData, service.ImportFromPackage(packageBytes));
    }

    [Fact]
    public void Import_WhenColumnsReordered_UsesHeaderNamesInsteadOfPositions()
    {
        var service = new KnowledgeBaseExcelExchangeService();

        byte[] packageBytes = service.BuildWorkbookPackage(CreateSampleData());
        packageBytes = ReorderWorksheetColumnsWithOpenXml(packageBytes, "Levels", "LevelName", "LevelIndex");
        packageBytes = ReorderWorksheetColumnsWithOpenXml(packageBytes, "Workshops", "WorkshopName", "NodesSheetKey", "IsLastSelected", "WorkshopId", "WorkshopOrder");
        packageBytes = ReorderWorkshopNodeColumnsWithOpenXml(
            packageBytes,
            "W1",
            "NodeName",
            "Description",
            "Path",
            "ParentNodeId",
            "Location",
            "NodeId",
            "LevelName",
            "PhotoPath",
            "SchemaLink",
            "SiblingOrder",
            "IpAddress",
            "LevelIndex");

        AssertSuccessfulImportMatches(CreateSampleData(), service.ImportFromPackage(packageBytes));
    }

    [Fact]
    public void Import_WhenOlderV3WorkbookDoesNotContainNodeDetailColumns_TreatsThemAsEmpty()
    {
        var service = new KnowledgeBaseExcelExchangeService();
        var sourceData = CreateSampleData();
        var expectedData = ClearNodeDetails(sourceData);

        byte[] packageBytes = service.BuildWorkbookPackage(sourceData);
        packageBytes = ReorderWorkshopNodeColumnsWithOpenXml(
            packageBytes,
            "W1",
            "NodeName",
            "Path",
            "ParentNodeId",
            "NodeId",
            "LevelName",
            "SiblingOrder",
            "LevelIndex");

        AssertSuccessfulImportMatches(expectedData, service.ImportFromPackage(packageBytes));
    }

    [Fact]
    public void Import_WhenUnknownColumnsAdded_IgnoresThem()
    {
        var service = new KnowledgeBaseExcelExchangeService();

        byte[] packageBytes = service.BuildWorkbookPackage(CreateSampleData());
        packageBytes = AddWorksheetColumnWithOpenXml(packageBytes, "Levels", "Comment", "root", "child");
        packageBytes = AddWorksheetColumnWithOpenXml(packageBytes, "Workshops", "UserComment", "main", "empty");
        packageBytes = AddWorkshopNodeColumnWithOpenXml(packageBytes, "W1", "UserComment", "r1", "c1", "r2");

        AssertSuccessfulImportMatches(CreateSampleData(), service.ImportFromPackage(packageBytes));
    }

    [Fact]
    public void Import_WhenExtraNonAkb5WorksheetExists_IgnoresIt()
    {
        var service = new KnowledgeBaseExcelExchangeService();

        byte[] packageBytes = service.BuildWorkbookPackage(CreateSampleData());
        packageBytes = AddPlainWorksheetWithOpenXml(packageBytes, "Notes", new[] { "Header", "Value" }, new[] { "Any", "Text" });

        AssertSuccessfulImportMatches(CreateSampleData(), service.ImportFromPackage(packageBytes));
    }

    [Fact]
    public void Import_WhenMetaLastWorkshopTextIsStaleButIdValid_UsesLastWorkshopId()
    {
        var service = new KnowledgeBaseExcelExchangeService();

        byte[] packageBytes = service.BuildWorkbookPackage(CreateSampleData());
        packageBytes = UpdateWorkshopRowWithOpenXml(packageBytes, rowIndex: 2, isLastSelected: false);
        packageBytes = UpdateWorkshopRowWithOpenXml(packageBytes, rowIndex: 3, isLastSelected: false);
        packageBytes = UpdateMetaPropertyValueWithOpenXml(packageBytes, "LastWorkshop", "Несуществующий цех");

        AssertSuccessfulImportMatches(CreateSampleData(), service.ImportFromPackage(packageBytes));
    }

    [Fact]
    public void Import_WhenNodeNameChangedButPathNotUpdated_StillSucceeds()
    {
        var service = new KnowledgeBaseExcelExchangeService();
        var sourceData = CreateSampleData();
        var expectedData = RenameNode(sourceData, "Цех 1", "Линия 1", "Линия 1 renamed");

        byte[] packageBytes = service.BuildWorkbookPackage(sourceData);
        int lineOneRowIndex = GetWorkshopNodeTableRowIndex(packageBytes, "W1", "Линия 1");
        packageBytes = UpdateWorkshopNodeCellValueByHeaderWithOpenXml(packageBytes, "W1", lineOneRowIndex, "NodeName", "Линия 1 renamed");

        AssertSuccessfulImportMatches(expectedData, service.ImportFromPackage(packageBytes));
    }

    [Fact]
    public void Import_WhenLevelNamesChangedButNodeLevelNameColumnIsStale_StillSucceeds()
    {
        var service = new KnowledgeBaseExcelExchangeService();
        var sourceData = CreateSampleData();
        var expectedData = RenameLevel(sourceData, levelIndex: 0, "Производство");

        byte[] packageBytes = service.BuildWorkbookPackage(sourceData);
        packageBytes = UpdateWorksheetCellValueByHeaderWithOpenXml(packageBytes, "Levels", rowIndex: 2, "LevelName", "Производство");

        AssertSuccessfulImportMatches(expectedData, service.ImportFromPackage(packageBytes));
    }

    [Fact]
    public void Import_WhenWorkshopRenamedAndNodeSheetTabRenamed_StillSucceeds()
    {
        var service = new KnowledgeBaseExcelExchangeService();
        var sourceData = CreateWorkshopRemapSampleData(
            new TestWorkshop("Цех 1", HasNodes: true),
            new TestWorkshop("Цех 2", HasNodes: true));

        byte[] packageBytes = service.BuildWorkbookPackage(sourceData);
        packageBytes = UpdateWorkshopRowWithOpenXml(packageBytes, rowIndex: 2, workshopName: "Производство");
        packageBytes = RenameWorksheetTabWithOpenXml(packageBytes, GetWorkshopNodesSheetName(packageBytes, "W1"), "Временное имя");

        AssertSuccessfulImportMatches(
            RenameWorkshops(sourceData, ("Цех 1", "Производство")),
            service.ImportFromPackage(packageBytes));
    }

    [Fact]
    public void Import_WhenWorkshopRenamed_UsesWorkbookMetadataAndKeepsNodesInPlace()
    {
        var service = new KnowledgeBaseExcelExchangeService();
        var sourceData = CreateComplexRoundTripData();
        var expectedData = RenameWorkshops(sourceData, ("Цех 2", "Производство 2"));

        byte[] packageBytes = service.BuildWorkbookPackage(sourceData);
        packageBytes = UpdateWorkshopRowWithOpenXml(packageBytes, rowIndex: 3, workshopName: "Производство 2");

        AssertSuccessfulImportMatches(expectedData, service.ImportFromPackage(packageBytes));
    }

    [Fact]
    public void Import_WhenNodeSheetTabRenamedManually_UsesSheetMetadataAndKeepsStructure()
    {
        var service = new KnowledgeBaseExcelExchangeService();
        var sourceData = CreateComplexRoundTripData();

        byte[] packageBytes = service.BuildWorkbookPackage(sourceData);
        packageBytes = RenameWorksheetTabWithOpenXml(packageBytes, GetWorkshopNodesSheetName(packageBytes, "W2"), "Переименованный лист узлов");

        AssertSuccessfulImportMatches(sourceData, service.ImportFromPackage(packageBytes));
    }

    [Fact]
    public void Import_WhenBrokenWorkshopSheetMetadataIntroduced_FailsClearly()
    {
        var service = new KnowledgeBaseExcelExchangeService();

        byte[] packageBytes = service.BuildWorkbookPackage(CreateSampleData());
        packageBytes = UpdateWorksheetPropertyValueWithOpenXml(
            packageBytes,
            GetWorkshopNodesSheetName(packageBytes, "W1"),
            "NodesSheetKey",
            "BROKEN-NS1");

        var result = service.ImportFromPackage(packageBytes);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("NS1", result.ErrorMessage);
    }

    [Fact]
    public void Import_WhenDuplicateWorkshopNamesIntroduced_FailsClearly()
    {
        var service = new KnowledgeBaseExcelExchangeService();

        byte[] packageBytes = service.BuildWorkbookPackage(CreateComplexRoundTripData());
        packageBytes = UpdateWorkshopRowWithOpenXml(packageBytes, rowIndex: 3, workshopName: "Цех 1");

        var result = service.ImportFromPackage(packageBytes);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("дублирующий цех", result.ErrorMessage);
    }

    [Fact]
    public void Import_WhenUserAddsUnusedEmptyWorkshop_PreservesExistingStructure()
    {
        var service = new KnowledgeBaseExcelExchangeService();
        var sourceData = CreateComplexRoundTripData();
        var expectedData = AddWorkshop(sourceData, "Новый пустой цех");

        byte[] packageBytes = service.BuildWorkbookPackage(sourceData);
        packageBytes = AddWorkshopWithOpenXml(
            packageBytes,
            workshopOrder: 5,
            workshopId: "W5",
            workshopName: "Новый пустой цех",
            nodesSheetKey: "NS5",
            sheetName: "Узлы - Новый пустой цех");

        AssertSuccessfulImportMatches(expectedData, service.ImportFromPackage(packageBytes));
    }

    [Fact]
    public void Import_WhenNodeSheetRenamedToReservedWorkbookSheetName_FailsClearly()
    {
        var service = new KnowledgeBaseExcelExchangeService();

        byte[] packageBytes = service.BuildWorkbookPackage(CreateSampleData());
        packageBytes = RenameWorksheetTabWithOpenXml(packageBytes, GetWorkshopNodesSheetName(packageBytes, "W1"), "Levels");

        var result = service.ImportFromPackage(packageBytes);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("встречается более одного раза", result.ErrorMessage);
    }

    [Fact]
    public void Import_WhenLegacyWorkbookVersionProvided_FailsClearly()
    {
        var service = new KnowledgeBaseExcelExchangeService();

        byte[] packageBytes = service.BuildWorkbookPackage(CreateSampleData());
        packageBytes = UpdateMetaPropertyValue(packageBytes, "FormatVersion", "2");

        var result = service.ImportFromPackage(packageBytes);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("v1/v2", result.ErrorMessage);
    }

    [Fact]
    public void ImportFromPackage_WhenWorkbookValidationFails_WritesParseFailureLogEvent()
    {
        var logger = new InMemoryAppLogger();
        var service = new KnowledgeBaseExcelExchangeService(logger);

        byte[] packageBytes = service.BuildWorkbookPackage(CreateSampleData());
        packageBytes = UpdateMetaPropertyValue(packageBytes, "FormatVersion", "2");

        var result = service.ImportFromPackage(packageBytes);

        Assert.False(result.IsSuccess);

        var startedEntry = Assert.Single(logger.Entries.Where(entry => entry.EventName == "ExcelImportStarted"));
        Assert.Equal(AppLogLevel.Information, startedEntry.Level);

        var parseFailedEntry = Assert.Single(logger.Entries.Where(entry => entry.EventName == "ExcelImportParseFailed"));
        Assert.Equal(AppLogLevel.Warning, parseFailedEntry.Level);
        Assert.NotNull(parseFailedEntry.Exception);
        Assert.Equal("KnowledgeBaseExcelImportException", parseFailedEntry.Exception!.GetType().Name);
    }

    [Fact]
    public void Import_WhenWorkshopNodeSheetMissing_ReturnsReadableError()
    {
        var service = new KnowledgeBaseExcelExchangeService();

        byte[] packageBytes = service.BuildWorkbookPackage(CreateSampleData());
        string missingSheetName = GetWorkshopNodesSheetName(packageBytes, "W1");
        packageBytes = RemoveWorksheetFromWorkbook(packageBytes, missingSheetName);

        var result = service.ImportFromPackage(packageBytes);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("NS1", result.ErrorMessage);
    }

    [Fact]
    public void Import_WhenNodeSheetMetadataPointsToUnknownWorkshop_FailsClearly()
    {
        var service = new KnowledgeBaseExcelExchangeService();

        byte[] packageBytes = service.BuildWorkbookPackage(CreateSampleData());
        packageBytes = UpdateWorksheetPropertyValue(packageBytes, GetWorkshopNodesSheetName(packageBytes, "W1"), "WorkshopId", "W999");

        var result = service.ImportFromPackage(packageBytes);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("неизвестный WorkshopId", result.ErrorMessage);
    }

    [Fact]
    public void Import_WhenDuplicateNodeId_ReturnsReadableError()
    {
        var service = new KnowledgeBaseExcelExchangeService();

        byte[] packageBytes = service.BuildWorkbookPackage(CreateSampleData());
        int lineOneRowIndex = GetWorkshopNodeTableRowIndex(packageBytes, "W1", "Линия 1");
        int shieldRowIndex = GetWorkshopNodeTableRowIndex(packageBytes, "W1", "Щит 1");
        string duplicatedNodeId = GetWorkshopNodeCellValueByHeader(packageBytes, "W1", lineOneRowIndex, "NodeId");
        packageBytes = UpdateWorkshopNodeCellValueByHeader(packageBytes, "W1", shieldRowIndex, "NodeId", duplicatedNodeId, null);

        var result = service.ImportFromPackage(packageBytes);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("NodeId", result.ErrorMessage);
    }

    [Fact]
    public void Import_WhenDuplicateSiblingOrderWithinParent_ReturnsReadableError()
    {
        var service = new KnowledgeBaseExcelExchangeService();

        byte[] packageBytes = service.BuildWorkbookPackage(CreateSampleData());
        int lineTwoRowIndex = GetWorkshopNodeTableRowIndex(packageBytes, "W1", "Линия 2");
        packageBytes = UpdateWorkshopNodeCellValueByHeader(packageBytes, "W1", lineTwoRowIndex, "SiblingOrder", "1", null);

        var result = service.ImportFromPackage(packageBytes);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("SiblingOrder", result.ErrorMessage);
    }

    [Fact]
    public void Import_WhenParentNodeMissing_ReturnsReadableError()
    {
        var service = new KnowledgeBaseExcelExchangeService();

        byte[] packageBytes = service.BuildWorkbookPackage(CreateSampleData());
        int shieldRowIndex = GetWorkshopNodeTableRowIndex(packageBytes, "W1", "Щит 1");
        packageBytes = UpdateWorkshopNodeCellValueByHeader(packageBytes, "W1", shieldRowIndex, "ParentNodeId", "999", null);

        var result = service.ImportFromPackage(packageBytes);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("ParentNodeId", result.ErrorMessage);
    }

    [Fact]
    public void Import_WhenParentPointsToAnotherWorkshop_FailsClearly()
    {
        var service = new KnowledgeBaseExcelExchangeService();
        var sourceData = CreateComplexRoundTripData();

        byte[] packageBytes = service.BuildWorkbookPackage(sourceData);
        int sourceRowIndex = GetWorkshopNodeTableRowIndex(packageBytes, "W1", "Линия 1.1");
        int targetRowIndex = GetWorkshopNodeTableRowIndex(packageBytes, "W2", "Линия 2.1");
        string foreignParentId = GetWorkshopNodeCellValueByHeader(packageBytes, "W1", sourceRowIndex, "NodeId");
        packageBytes = UpdateWorkshopNodeCellValueByHeader(packageBytes, "W2", targetRowIndex, "ParentNodeId", foreignParentId, null);

        var result = service.ImportFromPackage(packageBytes);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("родителя из другого цеха", result.ErrorMessage);
    }

    [Fact]
    public void Import_WhenCycleIntroduced_ReturnsReadableError()
    {
        var service = new KnowledgeBaseExcelExchangeService();

        byte[] packageBytes = service.BuildWorkbookPackage(CreateSampleData());
        int shieldRowIndex = GetWorkshopNodeTableRowIndex(packageBytes, "W1", "Щит 1");
        string shieldNodeId = GetWorkshopNodeCellValueByHeader(packageBytes, "W1", shieldRowIndex, "NodeId");
        packageBytes = UpdateWorkshopNodeCellValueByHeader(packageBytes, "W1", shieldRowIndex, "ParentNodeId", shieldNodeId, null);

        var result = service.ImportFromPackage(packageBytes);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("цикл", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Import_WhenWorkshopNamesCollapseToSameProjectedTabName_FailsClearly()
    {
        var service = new KnowledgeBaseExcelExchangeService();

        byte[] packageBytes = service.BuildWorkbookPackage(CreateSampleData());
        packageBytes = UpdateWorkshopRow(packageBytes, rowIndex: 2, workshopName: "Цех/1");
        packageBytes = UpdateWorkshopRow(packageBytes, rowIndex: 3, workshopName: "Цех:1");

        var result = service.ImportFromPackage(packageBytes);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("sanitization/truncation", result.ErrorMessage);
    }

    [Fact]
    public void Import_WhenXmlFileProvided_ReturnsXlsxOnlyError()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            string path = Path.Combine(tempDirectory, "legacy.xml");
            File.WriteAllText(path, "<xml />");

            var service = new KnowledgeBaseExcelExchangeService();
            var result = service.Import(path);

            Assert.False(result.IsSuccess);
            Assert.NotNull(result.ErrorMessage);
            Assert.Contains("*.xlsx", result.ErrorMessage);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private static string[] GetWorksheetNames(byte[] packageBytes)
    {
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var workbook = GetXmlEntry(packageBytes, "xl/workbook.xml");

        return workbook
            .Descendants(ns + "sheet")
            .Select(sheet => sheet.Attribute("name")?.Value ?? string.Empty)
            .ToArray();
    }

    private static List<string[]> ReadWorksheetRows(byte[] packageBytes, string worksheetName)
    {
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var worksheet = GetWorksheetDocument(packageBytes, worksheetName);

        return worksheet
            .Descendants(ns + "row")
            .Skip(1)
            .Select(ReadRowValues)
            .Where(row => row.Length > 0)
            .ToList();
    }

    private static List<string[]> ReadWorkshopNodeMetaRows(byte[] packageBytes, string workshopId)
    {
        var worksheet = GetWorksheetDocument(packageBytes, GetWorkshopNodesSheetName(packageBytes, workshopId));
        var rows = GetAllWorksheetRows(worksheet);
        int tableHeaderIndex = GetWorkshopNodeHeaderRowIndex(rows);

        return rows
            .Skip(1)
            .Take(tableHeaderIndex - 1)
            .Where(row => row.Length > 0)
            .ToList();
    }

    private static List<string[]> ReadWorkshopNodeTableRows(byte[] packageBytes, string workshopId)
    {
        var worksheet = GetWorksheetDocument(packageBytes, GetWorkshopNodesSheetName(packageBytes, workshopId));
        var rows = GetAllWorksheetRows(worksheet);
        int tableHeaderIndex = GetWorkshopNodeHeaderRowIndex(rows);

        return rows
            .Skip(tableHeaderIndex)
            .Where(row => row.Length > 0)
            .ToList();
    }

    private static Dictionary<string, int> GetWorkshopNodeHeaderMap(byte[] packageBytes, string workshopId)
    {
        string sheetName = GetWorkshopNodesSheetName(packageBytes, workshopId);
        var rows = GetAllWorksheetRows(GetWorksheetDocument(packageBytes, sheetName));
        int headerRowIndex = GetWorkshopNodeHeaderRowIndex(rows);
        return GetWorksheetHeaderMap(packageBytes, sheetName, headerRowIndex);
    }

    private static WorksheetCellInfo GetCell(byte[] packageBytes, string worksheetName, int rowIndex, int cellIndex)
    {
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var worksheet = GetWorksheetDocument(packageBytes, worksheetName);
        var row = worksheet.Descendants(ns + "row").ElementAt(rowIndex - 1);
        var cell = row.Elements(ns + "c").ElementAt(cellIndex - 1);

        return new WorksheetCellInfo(
            cell.Attribute("t")?.Value,
            cell.Element(ns + "v")?.Value ?? string.Concat(cell.Descendants(ns + "t").Select(text => text.Value)));
    }

    private static WorksheetCellInfo GetWorkshopNodeCell(byte[] packageBytes, string workshopId, int rowIndex, int cellIndex)
    {
        string worksheetName = GetWorkshopNodesSheetName(packageBytes, workshopId);
        var rows = GetAllWorksheetRows(GetWorksheetDocument(packageBytes, worksheetName));
        int headerRowIndex = GetWorkshopNodeHeaderRowIndex(rows);
        return GetCell(packageBytes, worksheetName, headerRowIndex + rowIndex - 1, cellIndex);
    }

    private static string GetWorkshopNodeCellValueByHeader(byte[] packageBytes, string workshopId, int rowIndex, string headerName)
    {
        string worksheetName = GetWorkshopNodesSheetName(packageBytes, workshopId);
        int headerRowIndex = GetWorkshopNodeHeaderRowIndex(GetAllWorksheetRows(GetWorksheetDocument(packageBytes, worksheetName)));
        var headerMap = GetWorksheetHeaderMap(packageBytes, worksheetName, headerRowIndex);
        string[] rows = GetAllWorksheetRows(GetWorksheetDocument(packageBytes, worksheetName))
            .Skip(headerRowIndex)
            .Where(row => row.Length > 0)
            .Select(row => ReadColumn(row, headerMap, headerName))
            .ToArray();

        return rows[rowIndex - 1];
    }

    private static int GetWorkshopNodeTableRowIndex(byte[] packageBytes, string workshopId, string nodeName)
    {
        var rows = ReadWorkshopNodeTableRows(packageBytes, workshopId);
        var headerMap = GetWorkshopNodeHeaderMap(packageBytes, workshopId);
        int nodeNameIndex = headerMap["NodeName"] - 1;

        return rows
            .Select((row, index) => new { row, index })
            .Single(item => item.row.Length > nodeNameIndex && string.Equals(item.row[nodeNameIndex], nodeName, StringComparison.Ordinal))
            .index + 1;
    }

    private static string[] ReadRowValues(XElement row)
    {
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var values = new List<string>();
        int currentIndex = 1;

        foreach (var cell in row.Elements(ns + "c"))
        {
            string? reference = cell.Attribute("r")?.Value;
            int requestedIndex = string.IsNullOrWhiteSpace(reference) ? currentIndex : GetColumnIndex(reference);

            while (currentIndex < requestedIndex)
            {
                values.Add(string.Empty);
                currentIndex++;
            }

            string cellType = cell.Attribute("t")?.Value ?? string.Empty;
            string value = cellType switch
            {
                "b" => (cell.Element(ns + "v")?.Value ?? string.Empty) == "1" ? "TRUE" : "FALSE",
                "inlineStr" => string.Concat(cell.Descendants(ns + "t").Select(text => text.Value)),
                _ => cell.Element(ns + "v")?.Value ?? string.Empty
            };

            values.Add(value);
            currentIndex++;
        }

        return TrimTrailingEmptyValues(values.ToArray());
    }

    private static List<string[]> GetAllWorksheetRows(XDocument worksheet)
    {
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        return worksheet
            .Descendants(ns + "row")
            .Select(ReadRowValues)
            .ToList();
    }

    private static int GetWorkshopNodeHeaderRowIndex(IReadOnlyList<string[]> rows)
    {
        int headerIndex = rows
            .Select((row, index) => new { row, index })
            .Single(item => HasRequiredHeaders(item.row, RequiredNodeHeadersV3))
            .index;

        return headerIndex + 1;
    }

    private static bool HasRequiredHeaders(string[] row, IReadOnlyCollection<string> requiredHeaders)
    {
        Dictionary<string, int>? headerMap = TryBuildHeaderMap(row);
        return headerMap != null && requiredHeaders.All(headerMap.ContainsKey);
    }

    private static string GetWorkshopNodesSheetName(byte[] packageBytes, string workshopId)
    {
        foreach (string sheetName in GetWorksheetNames(packageBytes))
        {
            if (IsNonNodeSheet(sheetName))
                continue;

            var rows = GetAllWorksheetRows(GetWorksheetDocument(packageBytes, sheetName));
            bool matches = rows.Any(row =>
                row.Length >= 2 &&
                string.Equals(row[0], "WorkshopId", StringComparison.Ordinal) &&
                string.Equals(row[1], workshopId, StringComparison.Ordinal));

            if (matches)
                return sheetName;
        }

        throw new InvalidOperationException($"Не найден лист узлов для WorkshopId '{workshopId}'.");
    }

    private static XDocument GetWorksheetDocument(byte[] packageBytes, string worksheetName)
    {
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        XNamespace relNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        XNamespace packageRelNs = "http://schemas.openxmlformats.org/package/2006/relationships";

        using var archive = OpenArchive(packageBytes);
        var workbook = LoadXml(archive.GetEntry("xl/workbook.xml")!);
        var relationships = LoadXml(archive.GetEntry("xl/_rels/workbook.xml.rels")!);

        var sheet = workbook
            .Descendants(ns + "sheet")
            .Single(item => string.Equals(item.Attribute("name")?.Value, worksheetName, StringComparison.Ordinal));
        string relationshipId = sheet.Attribute(relNs + "id")?.Value ?? string.Empty;
        string target = relationships
            .Descendants(packageRelNs + "Relationship")
            .Single(item => string.Equals(item.Attribute("Id")?.Value, relationshipId, StringComparison.Ordinal))
            .Attribute("Target")?.Value ?? string.Empty;

        string path = NormalizePartPath("xl/workbook.xml", target);
        return LoadXml(archive.GetEntry(path)!);
    }

    private static XDocument GetXmlEntry(byte[] packageBytes, string entryPath)
    {
        using var archive = OpenArchive(packageBytes);
        return LoadXml(archive.GetEntry(entryPath)!);
    }

    private static byte[] UpdateXmlEntry(byte[] sourceBytes, string entryPath, Action<XDocument> update)
    {
        using var stream = new MemoryStream();
        stream.Write(sourceBytes, 0, sourceBytes.Length);
        stream.Position = 0;

        using (var archive = new ZipArchive(stream, ZipArchiveMode.Update, leaveOpen: true))
        {
            var entry = archive.GetEntry(entryPath)!;
            var document = LoadXml(entry);
            update(document);
            entry.Delete();

            var replacement = archive.CreateEntry(entryPath, CompressionLevel.Optimal);
            using var replacementStream = replacement.Open();
            document.Save(replacementStream);
        }

        return stream.ToArray();
    }

    private static byte[] UpdateWorksheetCellValue(
        byte[] sourceBytes,
        string worksheetName,
        int rowIndex,
        int cellIndex,
        string value,
        string? cellType)
    {
        string entryPath = GetWorksheetEntryPath(sourceBytes, worksheetName);
        return UpdateXmlEntry(sourceBytes, entryPath, document =>
        {
            XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            var row = document.Descendants(ns + "row").ElementAt(rowIndex - 1);
            var cell = row.Elements(ns + "c").ElementAt(cellIndex - 1);

            cell.Elements().Remove();
            cell.Attribute("t")?.Remove();

            if (!string.IsNullOrWhiteSpace(cellType))
                cell.Add(new XAttribute("t", cellType));

            if (string.Equals(cellType, "inlineStr", StringComparison.Ordinal))
            {
                cell.Add(new XElement(ns + "is", new XElement(ns + "t", value)));
            }
            else
            {
                cell.Add(new XElement(ns + "v", value));
            }
        });
    }

    private static byte[] UpdateWorksheetCellValueByHeader(
        byte[] sourceBytes,
        string worksheetName,
        int rowIndex,
        string headerName,
        string value,
        string? cellType)
    {
        var headerMap = GetWorksheetHeaderMap(sourceBytes, worksheetName);
        return UpdateWorksheetCellValue(sourceBytes, worksheetName, rowIndex, headerMap[headerName], value, cellType);
    }

    private static byte[] UpdateWorkshopNodeCellValueByHeader(
        byte[] sourceBytes,
        string workshopId,
        int rowIndex,
        string headerName,
        string value,
        string? cellType)
    {
        string worksheetName = GetWorkshopNodesSheetName(sourceBytes, workshopId);
        int headerRowIndex = GetWorkshopNodeHeaderRowIndex(GetAllWorksheetRows(GetWorksheetDocument(sourceBytes, worksheetName)));
        var headerMap = GetWorksheetHeaderMap(sourceBytes, worksheetName, headerRowIndex);

        return UpdateWorksheetCellValue(
            sourceBytes,
            worksheetName,
            rowIndex: headerRowIndex + rowIndex,
            cellIndex: headerMap[headerName],
            value,
            cellType);
    }

    private static byte[] UpdateMetaPropertyValue(byte[] sourceBytes, string propertyName, string value) =>
        UpdateWorksheetPropertyValue(sourceBytes, "Meta", propertyName, value);

    private static byte[] UpdateWorksheetPropertyValue(byte[] sourceBytes, string worksheetName, string propertyName, string value)
    {
        string entryPath = GetWorksheetEntryPath(sourceBytes, worksheetName);
        return UpdateXmlEntry(sourceBytes, entryPath, document =>
        {
            XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            var row = document
                .Descendants(ns + "row")
                .Skip(1)
                .Single(currentRow => string.Equals(ReadRowValues(currentRow).FirstOrDefault(), propertyName, StringComparison.Ordinal));
            var cell = row.Elements(ns + "c").ElementAt(1);

            cell.Elements().Remove();
            cell.Attribute("t")?.Remove();
            cell.Add(new XAttribute("t", "inlineStr"));
            cell.Add(new XElement(ns + "is", new XElement(ns + "t", value)));
        });
    }

    private static byte[] UpdateWorkshopRow(
        byte[] sourceBytes,
        int rowIndex,
        int? workshopOrder = null,
        string? workshopName = null,
        bool? isLastSelected = null)
    {
        byte[] updatedBytes = sourceBytes;
        var headerMap = GetWorksheetHeaderMap(updatedBytes, "Workshops");

        if (workshopOrder.HasValue)
        {
            updatedBytes = UpdateWorksheetCellValue(
                updatedBytes,
                "Workshops",
                rowIndex,
                cellIndex: headerMap["WorkshopOrder"],
                value: workshopOrder.Value.ToString(),
                cellType: null);
        }

        if (workshopName != null)
        {
            updatedBytes = UpdateWorksheetCellValue(
                updatedBytes,
                "Workshops",
                rowIndex,
                cellIndex: headerMap["WorkshopName"],
                value: workshopName,
                cellType: "inlineStr");
        }

        if (isLastSelected.HasValue)
        {
            updatedBytes = UpdateWorksheetCellValue(
                updatedBytes,
                "Workshops",
                rowIndex,
                cellIndex: headerMap["IsLastSelected"],
                value: isLastSelected.Value ? "1" : "0",
                cellType: "b");
        }

        return updatedBytes;
    }

    private static byte[] RenameWorksheetTab(byte[] sourceBytes, string originalSheetName, string newSheetName)
    {
        return UpdateXmlEntry(sourceBytes, "xl/workbook.xml", document =>
        {
            XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            document
                .Descendants(ns + "sheet")
                .Single(sheet => string.Equals(sheet.Attribute("name")?.Value, originalSheetName, StringComparison.Ordinal))
                .SetAttributeValue("name", newSheetName);
        });
    }

    private static byte[] RemoveWorksheetFromWorkbook(byte[] sourceBytes, string worksheetName)
    {
        return UpdateXmlEntry(sourceBytes, "xl/workbook.xml", document =>
        {
            XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            document
                .Descendants(ns + "sheet")
                .Single(sheet => string.Equals(sheet.Attribute("name")?.Value, worksheetName, StringComparison.Ordinal))
                .Remove();
        });
    }

    private static byte[] ReorderWorksheetColumns(byte[] sourceBytes, string worksheetName, params string[] orderedHeaders)
    {
        var rows = GetAllWorksheetRows(GetWorksheetDocument(sourceBytes, worksheetName));
        var reorderedRows = ReorderTableRows(rows, headerRowIndex: 1, orderedHeaders);
        return RewriteWorksheetRows(sourceBytes, worksheetName, reorderedRows);
    }

    private static byte[] ReorderWorkshopNodeColumns(byte[] sourceBytes, string workshopId, params string[] orderedHeaders)
    {
        string worksheetName = GetWorkshopNodesSheetName(sourceBytes, workshopId);
        var rows = GetAllWorksheetRows(GetWorksheetDocument(sourceBytes, worksheetName));
        int headerRowIndex = GetWorkshopNodeHeaderRowIndex(rows);
        var reorderedRows = ReorderTableRows(rows, headerRowIndex, orderedHeaders);
        return RewriteWorksheetRows(sourceBytes, worksheetName, reorderedRows);
    }

    private static byte[] AddWorksheetColumn(byte[] sourceBytes, string worksheetName, string headerName, params string[] values)
    {
        var rows = GetAllWorksheetRows(GetWorksheetDocument(sourceBytes, worksheetName));
        var updatedRows = AddTableColumn(rows, headerRowIndex: 1, headerName, values);
        return RewriteWorksheetRows(sourceBytes, worksheetName, updatedRows);
    }

    private static byte[] AddWorkshopNodeColumn(byte[] sourceBytes, string workshopId, string headerName, params string[] values)
    {
        string worksheetName = GetWorkshopNodesSheetName(sourceBytes, workshopId);
        var rows = GetAllWorksheetRows(GetWorksheetDocument(sourceBytes, worksheetName));
        int headerRowIndex = GetWorkshopNodeHeaderRowIndex(rows);
        var updatedRows = AddTableColumn(rows, headerRowIndex, headerName, values);
        return RewriteWorksheetRows(sourceBytes, worksheetName, updatedRows);
    }

    private static byte[] RewriteWorksheetRows(byte[] sourceBytes, string worksheetName, IReadOnlyList<string[]> rows)
    {
        string entryPath = GetWorksheetEntryPath(sourceBytes, worksheetName);
        return UpdateXmlEntry(sourceBytes, entryPath, document =>
        {
            XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            var sheetData = document.Root?.Element(ns + "sheetData")
                ?? throw new InvalidOperationException($"Лист '{worksheetName}' не содержит sheetData.");

            sheetData.RemoveNodes();

            for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                sheetData.Add(
                    new XElement(
                        ns + "row",
                        new XAttribute("r", rowIndex + 1),
                        BuildInlineCells(ns, rowIndex + 1, rows[rowIndex])));
            }
        });
    }

    private static IEnumerable<XElement> BuildInlineCells(XNamespace ns, int rowIndex, string[] values)
    {
        for (int columnIndex = 0; columnIndex < values.Length; columnIndex++)
        {
            string value = values[columnIndex];
            var text = new XElement(ns + "t", value);
            if (value.Length != value.Trim().Length)
                text.Add(new XAttribute(XNamespace.Xml + "space", "preserve"));

            yield return new XElement(
                ns + "c",
                new XAttribute("r", $"{GetColumnName(columnIndex + 1)}{rowIndex}"),
                new XAttribute("t", "inlineStr"),
                new XElement(
                    ns + "is",
                    text));
        }
    }

    private static List<string[]> ReorderTableRows(IReadOnlyList<string[]> rows, int headerRowIndex, IReadOnlyList<string> orderedHeaders)
    {
        var updatedRows = rows.Select(row => row.ToArray()).ToList();
        var headerMap = BuildHeaderMap(updatedRows[headerRowIndex - 1]);

        updatedRows[headerRowIndex - 1] = orderedHeaders.ToArray();
        for (int rowIndex = headerRowIndex; rowIndex < updatedRows.Count; rowIndex++)
        {
            updatedRows[rowIndex] = orderedHeaders
                .Select(header => ReadColumn(updatedRows[rowIndex], headerMap, header))
                .ToArray();
        }

        return updatedRows;
    }

    private static List<string[]> AddTableColumn(
        IReadOnlyList<string[]> rows,
        int headerRowIndex,
        string headerName,
        IReadOnlyList<string> values)
    {
        var updatedRows = rows.Select(row => row.ToArray()).ToList();
        updatedRows[headerRowIndex - 1] = updatedRows[headerRowIndex - 1].Concat(new[] { headerName }).ToArray();

        int dataRowCount = updatedRows.Count - headerRowIndex;
        if (values.Count > dataRowCount)
            throw new InvalidOperationException($"Передано {values.Count} значений для колонки '{headerName}', но строк данных только {dataRowCount}.");

        for (int rowIndex = headerRowIndex; rowIndex < updatedRows.Count; rowIndex++)
        {
            string value = rowIndex - headerRowIndex < values.Count
                ? values[rowIndex - headerRowIndex]
                : string.Empty;
            updatedRows[rowIndex] = updatedRows[rowIndex].Concat(new[] { value }).ToArray();
        }

        return updatedRows;
    }

    private static byte[] AddPlainWorksheet(byte[] sourceBytes, string sheetName, params string[][] rows)
    {
        using var stream = new MemoryStream();
        stream.Write(sourceBytes, 0, sourceBytes.Length);
        stream.Position = 0;

        using (var document = SpreadsheetDocument.Open(stream, true))
        {
            var workbookPart = document.WorkbookPart!;
            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            worksheetPart.Worksheet = new Worksheet();

            var sheetData = new SheetData();
            for (int rowIndex = 0; rowIndex < rows.Length; rowIndex++)
            {
                var row = new Row { RowIndex = (uint)(rowIndex + 1) };
                for (int columnIndex = 0; columnIndex < rows[rowIndex].Length; columnIndex++)
                {
                    row.Append(CreateInlineStringCell(rows[rowIndex][columnIndex], rowIndex + 1, columnIndex + 1));
                }

                sheetData.Append(row);
            }

            worksheetPart.Worksheet.Append(sheetData);
            worksheetPart.Worksheet.Save();

            var sheets = workbookPart.Workbook.GetFirstChild<Sheets>() ?? workbookPart.Workbook.AppendChild(new Sheets());
            uint nextSheetId = sheets.Elements<Sheet>()
                .Select(sheet => sheet.SheetId?.Value ?? 0U)
                .DefaultIfEmpty(0U)
                .Max() + 1;

            sheets.Append(new Sheet
            {
                Id = workbookPart.GetIdOfPart(worksheetPart),
                SheetId = nextSheetId,
                Name = sheetName
            });

            workbookPart.Workbook.Save();
        }

        return stream.ToArray();
    }

    private static Cell CreateInlineStringCell(string value, int rowIndex, int columnIndex)
    {
        var text = new Text(value);
        if (value.Length != value.Trim().Length)
            text.Space = SpaceProcessingModeValues.Preserve;

        return new Cell
        {
            CellReference = $"{GetColumnName(columnIndex)}{rowIndex}",
            DataType = CellValues.InlineString,
            InlineString = new InlineString(text)
        };
    }

    private static Dictionary<string, int> GetWorksheetHeaderMap(byte[] packageBytes, string worksheetName, int headerRowIndex = 1)
    {
        var worksheet = GetWorksheetDocument(packageBytes, worksheetName);
        var header = GetAllWorksheetRows(worksheet).ElementAt(headerRowIndex - 1);
        return BuildHeaderMap(header);
    }

    private static Dictionary<string, int> BuildHeaderMap(string[] headerRow)
    {
        var headerMap = new Dictionary<string, int>(StringComparer.Ordinal);

        for (int index = 0; index < headerRow.Length; index++)
        {
            string value = headerRow[index];
            if (string.IsNullOrWhiteSpace(value))
                continue;

            headerMap.Add(value, index + 1);
        }

        return headerMap;
    }

    private static Dictionary<string, int>? TryBuildHeaderMap(string[] headerRow)
    {
        var headerMap = new Dictionary<string, int>(StringComparer.Ordinal);

        for (int index = 0; index < headerRow.Length; index++)
        {
            string value = headerRow[index].Trim();
            if (string.IsNullOrWhiteSpace(value))
                continue;

            if (!headerMap.TryAdd(value, index))
                return null;
        }

        return headerMap;
    }

    private static string ReadColumn(string[] row, IReadOnlyDictionary<string, int> headerMap, string headerName)
    {
        if (!headerMap.TryGetValue(headerName, out int index))
            return string.Empty;

        int zeroBasedIndex = index - 1;
        return zeroBasedIndex >= 0 && zeroBasedIndex < row.Length ? row[zeroBasedIndex] : string.Empty;
    }

    private static SavedData AssertSuccessfulImportMatches(SavedData expected, KnowledgeBaseExcelImportResult result)
    {
        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.NotNull(result.Data);
        AssertSavedDataEquivalent(expected, result.Data!);
        return result.Data!;
    }

    private static IReadOnlyList<WorkshopNodeSheetInfo> GetWorkshopNodeSheetInfos(byte[] packageBytes)
    {
        return GetWorksheetNames(packageBytes)
            .Where(sheetName => !IsNonNodeSheet(sheetName))
            .Select(sheetName =>
            {
                var rows = GetAllWorksheetRows(GetWorksheetDocument(packageBytes, sheetName));
                int headerRowIndex = GetWorkshopNodeHeaderRowIndex(rows);
                var metaRows = rows
                    .Skip(1)
                    .Take(headerRowIndex - 1)
                    .Where(row => row.Length > 0)
                    .ToList();

                return new WorkshopNodeSheetInfo(
                    sheetName,
                    ReadProperty(metaRows, "SheetKind"),
                    ReadProperty(metaRows, "WorkshopId"),
                    ReadProperty(metaRows, "NodesSheetKey"),
                    rows.Skip(headerRowIndex).Count(row => row.Length > 0));
            })
            .ToList();
    }

    private static bool IsSheetProtected(byte[] packageBytes, string worksheetName)
    {
        using var stream = new MemoryStream(packageBytes, writable: false);
        using var document = SpreadsheetDocument.Open(stream, false);
        return GetWorksheetPart(document, worksheetName).Worksheet.Elements<SheetProtection>().Any();
    }

    private static uint GetFrozenRowCount(byte[] packageBytes, string worksheetName)
    {
        using var stream = new MemoryStream(packageBytes, writable: false);
        using var document = SpreadsheetDocument.Open(stream, false);

        return GetWorksheetPart(document, worksheetName)
            .Worksheet
            .Elements<SheetViews>()
            .SelectMany(views => views.Elements<SheetView>())
            .Select(view =>
            {
                double? split = view.GetFirstChild<Pane>()?.VerticalSplit?.Value;
                return split.HasValue ? (uint?)split.Value : null;
            })
            .FirstOrDefault(value => value.HasValue) ?? 0U;
    }

    private static bool IsColumnHidden(byte[] packageBytes, string worksheetName, uint columnIndex)
    {
        using var stream = new MemoryStream(packageBytes, writable: false);
        using var document = SpreadsheetDocument.Open(stream, false);

        var column = GetWorksheetPart(document, worksheetName)
            .Worksheet
            .GetFirstChild<Columns>()?
            .Elements<Column>()
            .SingleOrDefault(current => current.Min!.Value <= columnIndex && current.Max!.Value >= columnIndex);

        return column?.Hidden?.Value ?? false;
    }

    private static bool IsCellLocked(byte[] packageBytes, string worksheetName, int rowIndex, int cellIndex)
    {
        using var stream = new MemoryStream(packageBytes, writable: false);
        using var document = SpreadsheetDocument.Open(stream, false);

        var worksheetPart = GetWorksheetPart(document, worksheetName);
        var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>()
            ?? throw new InvalidOperationException($"Лист '{worksheetName}' не содержит sheetData.");
        var row = sheetData.Elements<Row>().ElementAt(rowIndex - 1);
        var cell = row.Elements<Cell>().ElementAt(cellIndex - 1);
        uint styleIndex = cell.StyleIndex?.Value ?? 0U;

        var cellFormat = (CellFormat)document.WorkbookPart!.WorkbookStylesPart!.Stylesheet.CellFormats!.ElementAt((int)styleIndex);
        return cellFormat.Protection?.Locked?.Value ?? true;
    }

    private static bool IsWorkshopNodeCellLocked(byte[] packageBytes, string workshopId, int rowIndex, int cellIndex)
    {
        string worksheetName = GetWorkshopNodesSheetName(packageBytes, workshopId);
        var rows = GetAllWorksheetRows(GetWorksheetDocument(packageBytes, worksheetName));
        int headerRowIndex = GetWorkshopNodeHeaderRowIndex(rows);
        return IsCellLocked(packageBytes, worksheetName, headerRowIndex + rowIndex - 1, cellIndex);
    }

    private static bool IsNonNodeSheet(string sheetName) =>
        NonNodeSheetNames.Contains(sheetName, StringComparer.Ordinal);

    private static string ReadProperty(IEnumerable<string[]> rows, string propertyName)
    {
        return rows
            .Single(row => row.Length >= 2 && string.Equals(row[0], propertyName, StringComparison.Ordinal))[1];
    }

    private static byte[] UpdateWorksheetCellValueByHeaderWithOpenXml(
        byte[] sourceBytes,
        string worksheetName,
        int rowIndex,
        string headerName,
        string value)
    {
        return MutateWorkbookWithOpenXml(sourceBytes, document =>
        {
            var rows = ReadWorksheetRowsFromOpenXml(document, worksheetName);
            var headerMap = BuildHeaderMap(rows[0]);
            int targetColumnIndex = headerMap[headerName] - 1;
            rows[rowIndex - 1] = EnsureLength(rows[rowIndex - 1], targetColumnIndex + 1);
            rows[rowIndex - 1][targetColumnIndex] = value;
            RewriteWorksheetRowsWithOpenXml(document, worksheetName, rows);
        });
    }

    private static byte[] UpdateWorkshopNodeCellValueByHeaderWithOpenXml(
        byte[] sourceBytes,
        string workshopId,
        int rowIndex,
        string headerName,
        string value)
    {
        string worksheetName = GetWorkshopNodesSheetName(sourceBytes, workshopId);

        return MutateWorkbookWithOpenXml(sourceBytes, document =>
        {
            var rows = ReadWorksheetRowsFromOpenXml(document, worksheetName);
            int headerRowIndex = GetWorkshopNodeHeaderRowIndex(rows);
            var headerMap = BuildHeaderMap(rows[headerRowIndex - 1]);
            int targetColumnIndex = headerMap[headerName] - 1;
            int targetRowIndex = headerRowIndex + rowIndex - 1;
            rows[targetRowIndex] = EnsureLength(rows[targetRowIndex], targetColumnIndex + 1);
            rows[targetRowIndex][targetColumnIndex] = value;
            RewriteWorksheetRowsWithOpenXml(document, worksheetName, rows);
        });
    }

    private static byte[] UpdateMetaPropertyValueWithOpenXml(byte[] sourceBytes, string propertyName, string value) =>
        UpdateWorksheetPropertyValueWithOpenXml(sourceBytes, "Meta", propertyName, value);

    private static byte[] UpdateWorksheetPropertyValueWithOpenXml(
        byte[] sourceBytes,
        string worksheetName,
        string propertyName,
        string value)
    {
        return MutateWorkbookWithOpenXml(sourceBytes, document =>
        {
            var rows = ReadWorksheetRowsFromOpenXml(document, worksheetName);
            int targetRowIndex = rows
                .Select((row, index) => new { row, index })
                .Single(item =>
                    item.row.Length >= 1 &&
                    string.Equals(item.row[0], propertyName, StringComparison.Ordinal))
                .index;

            rows[targetRowIndex] = EnsureLength(rows[targetRowIndex], 2);
            rows[targetRowIndex][1] = value;
            RewriteWorksheetRowsWithOpenXml(document, worksheetName, rows);
        });
    }

    private static byte[] UpdateWorkshopRowWithOpenXml(
        byte[] sourceBytes,
        int rowIndex,
        int? workshopOrder = null,
        string? workshopName = null,
        bool? isLastSelected = null)
    {
        byte[] updatedBytes = sourceBytes;

        if (workshopOrder.HasValue)
        {
            updatedBytes = UpdateWorksheetCellValueByHeaderWithOpenXml(
                updatedBytes,
                "Workshops",
                rowIndex,
                "WorkshopOrder",
                workshopOrder.Value.ToString());
        }

        if (workshopName != null)
        {
            updatedBytes = UpdateWorksheetCellValueByHeaderWithOpenXml(
                updatedBytes,
                "Workshops",
                rowIndex,
                "WorkshopName",
                workshopName);
        }

        if (isLastSelected.HasValue)
        {
            updatedBytes = UpdateWorksheetCellValueByHeaderWithOpenXml(
                updatedBytes,
                "Workshops",
                rowIndex,
                "IsLastSelected",
                isLastSelected.Value ? "TRUE" : "FALSE");
        }

        return updatedBytes;
    }

    private static byte[] RenameWorksheetTabWithOpenXml(byte[] sourceBytes, string originalSheetName, string newSheetName)
    {
        return MutateWorkbookWithOpenXml(sourceBytes, document =>
        {
            var sheet = GetSheet(document, originalSheetName);
            sheet.Name = new StringValue(newSheetName);
            document.WorkbookPart!.Workbook.Save();
        });
    }

    private static byte[] ReorderWorksheetColumnsWithOpenXml(byte[] sourceBytes, string worksheetName, params string[] orderedHeaders)
    {
        return MutateWorkbookWithOpenXml(sourceBytes, document =>
        {
            var rows = ReadWorksheetRowsFromOpenXml(document, worksheetName);
            RewriteWorksheetRowsWithOpenXml(document, worksheetName, ReorderTableRows(rows, headerRowIndex: 1, orderedHeaders));
        });
    }

    private static byte[] ReorderWorkshopNodeColumnsWithOpenXml(byte[] sourceBytes, string workshopId, params string[] orderedHeaders)
    {
        string worksheetName = GetWorkshopNodesSheetName(sourceBytes, workshopId);

        return MutateWorkbookWithOpenXml(sourceBytes, document =>
        {
            var rows = ReadWorksheetRowsFromOpenXml(document, worksheetName);
            int headerRowIndex = GetWorkshopNodeHeaderRowIndex(rows);
            RewriteWorksheetRowsWithOpenXml(document, worksheetName, ReorderTableRows(rows, headerRowIndex, orderedHeaders));
        });
    }

    private static byte[] AddWorksheetColumnWithOpenXml(byte[] sourceBytes, string worksheetName, string headerName, params string[] values)
    {
        return MutateWorkbookWithOpenXml(sourceBytes, document =>
        {
            var rows = ReadWorksheetRowsFromOpenXml(document, worksheetName);
            RewriteWorksheetRowsWithOpenXml(document, worksheetName, AddTableColumn(rows, headerRowIndex: 1, headerName, values));
        });
    }

    private static byte[] AddWorkshopNodeColumnWithOpenXml(byte[] sourceBytes, string workshopId, string headerName, params string[] values)
    {
        string worksheetName = GetWorkshopNodesSheetName(sourceBytes, workshopId);

        return MutateWorkbookWithOpenXml(sourceBytes, document =>
        {
            var rows = ReadWorksheetRowsFromOpenXml(document, worksheetName);
            int headerRowIndex = GetWorkshopNodeHeaderRowIndex(rows);
            RewriteWorksheetRowsWithOpenXml(document, worksheetName, AddTableColumn(rows, headerRowIndex, headerName, values));
        });
    }

    private static byte[] AddPlainWorksheetWithOpenXml(byte[] sourceBytes, string sheetName, params string[][] rows) =>
        MutateWorkbookWithOpenXml(sourceBytes, document => AppendWorksheetWithOpenXml(document, sheetName, rows));

    private static byte[] AddWorkshopWithOpenXml(
        byte[] sourceBytes,
        int workshopOrder,
        string workshopId,
        string workshopName,
        string nodesSheetKey,
        string sheetName)
    {
        return MutateWorkbookWithOpenXml(sourceBytes, document =>
        {
            var workshopRows = ReadWorksheetRowsFromOpenXml(document, "Workshops");
            var updatedWorkshopRows = workshopRows.Select(row => row.ToArray()).ToList();
            updatedWorkshopRows.Add(new[]
            {
                workshopOrder.ToString(),
                workshopId,
                workshopName,
                "FALSE",
                nodesSheetKey
            });

            RewriteWorksheetRowsWithOpenXml(document, "Workshops", updatedWorkshopRows);
            AppendWorksheetWithOpenXml(
                document,
                sheetName,
                new[] { "Property", "Value" },
                new[] { "SheetKind", "WorkshopNodes" },
                new[] { "WorkshopId", workshopId },
                new[] { "NodesSheetKey", nodesSheetKey },
                Array.Empty<string>(),
                ExportedNodeHeadersV3);
        });
    }

    private static byte[] MutateWorkbookWithOpenXml(byte[] sourceBytes, Action<SpreadsheetDocument> mutate)
    {
        using var stream = new MemoryStream();
        stream.Write(sourceBytes, 0, sourceBytes.Length);
        stream.Position = 0;

        using (var document = SpreadsheetDocument.Open(stream, true))
        {
            mutate(document);
            document.WorkbookPart?.Workbook.Save();
        }

        return stream.ToArray();
    }

    private static List<string[]> ReadWorksheetRowsFromOpenXml(SpreadsheetDocument document, string worksheetName)
    {
        WorksheetPart worksheetPart = GetWorksheetPart(document, worksheetName);
        var sharedStrings = ReadSharedStrings(document.WorkbookPart?.SharedStringTablePart);
        var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>()
            ?? throw new InvalidOperationException($"Лист '{worksheetName}' не содержит sheetData.");

        return sheetData.Elements<Row>()
            .Select(row => TrimTrailingEmptyValues(ReadRowValues(row, sharedStrings).ToArray()))
            .ToList();
    }

    private static IReadOnlyList<string> ReadSharedStrings(SharedStringTablePart? part)
    {
        if (part?.SharedStringTable == null)
            return Array.Empty<string>();

        return part.SharedStringTable
            .Elements<SharedStringItem>()
            .Select(item => string.Concat(item.Descendants<Text>().Select(text => text.Text)))
            .ToList();
    }

    private static List<string> ReadRowValues(Row row, IReadOnlyList<string> sharedStrings)
    {
        var values = new List<string>();
        int currentIndex = 1;

        foreach (var cell in row.Elements<Cell>())
        {
            int requestedIndex = currentIndex;
            string? reference = cell.CellReference?.Value;
            if (!string.IsNullOrWhiteSpace(reference))
                requestedIndex = GetColumnIndex(reference);

            while (currentIndex < requestedIndex)
            {
                values.Add(string.Empty);
                currentIndex++;
            }

            values.Add(ReadCellValue(cell, sharedStrings));
            currentIndex++;
        }

        return values;
    }

    private static string ReadCellValue(Cell cell, IReadOnlyList<string> sharedStrings)
    {
        if (cell.DataType?.Value == CellValues.SharedString)
        {
            string rawIndex = cell.CellValue?.InnerText ?? string.Empty;
            if (int.TryParse(rawIndex, out int index) && index >= 0 && index < sharedStrings.Count)
                return sharedStrings[index];
        }

        if (cell.DataType?.Value == CellValues.Boolean)
            return (cell.CellValue?.InnerText ?? string.Empty) == "1" ? "TRUE" : "FALSE";

        if (cell.DataType?.Value == CellValues.InlineString)
        {
            return string.Concat(
                (cell.InlineString?.Descendants<Text>() ?? Enumerable.Empty<Text>())
                    .Select(text => text.Text));
        }

        return cell.CellValue?.InnerText ?? string.Empty;
    }

    private static void RewriteWorksheetRowsWithOpenXml(
        SpreadsheetDocument document,
        string worksheetName,
        IReadOnlyList<string[]> rows)
    {
        WorksheetPart worksheetPart = GetWorksheetPart(document, worksheetName);
        var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>()
            ?? worksheetPart.Worksheet.AppendChild(new SheetData());

        sheetData.RemoveAllChildren();

        for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            var row = new Row { RowIndex = (uint)(rowIndex + 1) };
            for (int columnIndex = 0; columnIndex < rows[rowIndex].Length; columnIndex++)
            {
                row.Append(CreateInlineStringCell(rows[rowIndex][columnIndex], rowIndex + 1, columnIndex + 1));
            }

            sheetData.Append(row);
        }

        worksheetPart.Worksheet.Save();
    }

    private static void AppendWorksheetWithOpenXml(
        SpreadsheetDocument document,
        string sheetName,
        params string[][] rows)
    {
        var workbookPart = document.WorkbookPart
            ?? throw new InvalidOperationException("Workbook part is missing.");

        var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
        worksheetPart.Worksheet = new Worksheet(new SheetData());
        var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>()
            ?? worksheetPart.Worksheet.AppendChild(new SheetData());

        sheetData.RemoveAllChildren();

        for (int rowIndex = 0; rowIndex < rows.Length; rowIndex++)
        {
            var row = new Row { RowIndex = (uint)(rowIndex + 1) };
            for (int columnIndex = 0; columnIndex < rows[rowIndex].Length; columnIndex++)
            {
                row.Append(CreateInlineStringCell(rows[rowIndex][columnIndex], rowIndex + 1, columnIndex + 1));
            }

            sheetData.Append(row);
        }

        worksheetPart.Worksheet.Save();

        var sheets = workbookPart.Workbook.GetFirstChild<Sheets>() ?? workbookPart.Workbook.AppendChild(new Sheets());
        uint nextSheetId = sheets.Elements<Sheet>()
            .Select(sheet => sheet.SheetId?.Value ?? 0U)
            .DefaultIfEmpty(0U)
            .Max() + 1;

        sheets.Append(new Sheet
        {
            Id = workbookPart.GetIdOfPart(worksheetPart),
            SheetId = nextSheetId,
            Name = sheetName
        });

        workbookPart.Workbook.Save();
    }

    private static WorksheetPart GetWorksheetPart(SpreadsheetDocument document, string worksheetName)
    {
        var workbookPart = document.WorkbookPart
            ?? throw new InvalidOperationException("Workbook part is missing.");
        var sheet = GetSheet(document, worksheetName);
        return (WorksheetPart)workbookPart.GetPartById(sheet.Id!.Value!);
    }

    private static Sheet GetSheet(SpreadsheetDocument document, string worksheetName)
    {
        var workbookPart = document.WorkbookPart
            ?? throw new InvalidOperationException("Workbook part is missing.");
        return workbookPart.Workbook.Sheets!.Elements<Sheet>()
            .Single(sheet => string.Equals(sheet.Name?.Value, worksheetName, StringComparison.Ordinal));
    }

    private static string[] EnsureLength(string[] source, int requiredLength)
    {
        if (source.Length >= requiredLength)
            return source.ToArray();

        return source.Concat(Enumerable.Repeat(string.Empty, requiredLength - source.Length)).ToArray();
    }

    private static string GetWorksheetEntryPath(byte[] packageBytes, string worksheetName)
    {
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        XNamespace relNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        XNamespace packageRelNs = "http://schemas.openxmlformats.org/package/2006/relationships";

        using var archive = OpenArchive(packageBytes);
        var workbook = LoadXml(archive.GetEntry("xl/workbook.xml")!);
        var relationships = LoadXml(archive.GetEntry("xl/_rels/workbook.xml.rels")!);

        var sheet = workbook
            .Descendants(ns + "sheet")
            .Single(item => string.Equals(item.Attribute("name")?.Value, worksheetName, StringComparison.Ordinal));
        string relationshipId = sheet.Attribute(relNs + "id")?.Value ?? string.Empty;
        string target = relationships
            .Descendants(packageRelNs + "Relationship")
            .Single(item => string.Equals(item.Attribute("Id")?.Value, relationshipId, StringComparison.Ordinal))
            .Attribute("Target")?.Value ?? string.Empty;

        return NormalizePartPath("xl/workbook.xml", target);
    }

    private static int GetColumnIndex(string cellReference)
    {
        int index = 0;
        foreach (char symbol in cellReference)
        {
            if (!char.IsLetter(symbol))
                break;

            index = (index * 26) + (char.ToUpperInvariant(symbol) - 'A' + 1);
        }

        return index;
    }

    private static string NormalizePartPath(string basePartPath, string target)
    {
        if (target.StartsWith('/'))
            return target.TrimStart('/');

        string baseDirectory = Path.GetDirectoryName(basePartPath)?.Replace('\\', '/') ?? string.Empty;
        string combined = string.IsNullOrWhiteSpace(baseDirectory)
            ? target
            : $"{baseDirectory}/{target}";

        var segments = new Stack<string>();
        foreach (var segment in combined.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            if (segment == ".")
                continue;

            if (segment == "..")
            {
                if (segments.Count > 0)
                    segments.Pop();

                continue;
            }

            segments.Push(segment);
        }

        return string.Join("/", segments.Reverse());
    }

    private static string[] TrimTrailingEmptyValues(string[] values)
    {
        int lastNonEmptyIndex = values.Length - 1;
        while (lastNonEmptyIndex >= 0 && string.IsNullOrWhiteSpace(values[lastNonEmptyIndex]))
            lastNonEmptyIndex--;

        if (lastNonEmptyIndex < 0)
            return Array.Empty<string>();

        return values.Take(lastNonEmptyIndex + 1).ToArray();
    }

    private static ZipArchive OpenArchive(byte[] packageBytes)
    {
        var stream = new MemoryStream(packageBytes, writable: false);
        return new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);
    }

    private static XDocument LoadXml(ZipArchiveEntry entry)
    {
        using var stream = entry.Open();
        return XDocument.Load(stream);
    }

    private static string GetColumnName(int columnIndex)
    {
        var chars = new Stack<char>();
        int current = columnIndex;
        while (current > 0)
        {
            current--;
            chars.Push((char)('A' + (current % 26)));
            current /= 26;
        }

        return new string(chars.ToArray());
    }

    private static SavedData CreateSampleData() =>
        new()
        {
            SchemaVersion = SavedData.CurrentSchemaVersion,
            Config = new KbConfig
            {
                MaxLevels = 3,
                LevelNames = new List<string> { "Цех", "Линия", "Щит" }
            },
            Workshops = new Dictionary<string, List<KbNode>>
            {
                ["Цех 1"] = new List<KbNode>
                {
                    new()
                    {
                        Name = "Линия 1",
                        LevelIndex = 0,
                        Details = new KbNodeDetails
                        {
                            Description = "Главная линия печи",
                            Location = "Корпус А",
                            PhotoPath = @"\\server\photos\line-1.jpg"
                        },
                        Children = new List<KbNode>
                        {
                            new()
                            {
                                Name = "Щит 1",
                                LevelIndex = 1,
                                Details = new KbNodeDetails
                                {
                                    Description = "Локальный щит",
                                    Location = "Площадка 3",
                                    PhotoPath = @"\\server\photos\shield-1.jpg"
                                }
                            }
                        }
                    },
                    new()
                    {
                        Name = "Линия 2",
                        LevelIndex = 0,
                        Details = new KbNodeDetails
                        {
                            Description = "Резервная линия"
                        }
                    }
                },
                ["Пустой цех"] = new List<KbNode>()
            },
            LastWorkshop = "Пустой цех"
        };

    private static SavedData CreateWorkshopRemapSampleData(params TestWorkshop[] workshops)
    {
        var workshopMap = new Dictionary<string, List<KbNode>>();
        int nodeIndex = 1;

        foreach (var workshop in workshops)
        {
            var roots = new List<KbNode>();
            if (workshop.HasNodes)
            {
                roots.Add(new KbNode
                {
                    Name = $"Линия {nodeIndex}",
                    LevelIndex = 0
                });
                nodeIndex++;
            }

            workshopMap[workshop.Name] = roots;
        }

        return new SavedData
        {
            SchemaVersion = SavedData.CurrentSchemaVersion,
            Config = new KbConfig
            {
                MaxLevels = 2,
                LevelNames = new List<string> { "Цех", "Линия" }
            },
            Workshops = workshopMap,
            LastWorkshop = workshops.Last().Name
        };
    }

    private static SavedData CreateComplexRoundTripData() =>
        new()
        {
            SchemaVersion = SavedData.CurrentSchemaVersion,
            Config = new KbConfig
            {
                MaxLevels = 4,
                LevelNames = new List<string> { "Цех", "Линия", "Участок", "Шкаф" }
            },
            Workshops = new Dictionary<string, List<KbNode>>
            {
                ["Цех 1"] = new List<KbNode>
                {
                    new()
                    {
                        Name = "Линия 1.1",
                        LevelIndex = 0,
                        Children = new List<KbNode>
                        {
                            new()
                            {
                                Name = "Участок 1.1.1",
                                LevelIndex = 1,
                                Details = new KbNodeDetails
                                {
                                    Description = "Основной участок",
                                    Location = "Южная галерея",
                                    PhotoPath = @"\\server\photos\section-111.jpg"
                                },
                                Children = new List<KbNode>
                                {
                                    new()
                                    {
                                        Name = "Шкаф 1.1.1.1",
                                        LevelIndex = 2,
                                        Details = new KbNodeDetails
                                        {
                                            Description = "Силовой шкаф",
                                            Location = "Ось 5-6",
                                            PhotoPath = @"\\server\photos\cabinet-1111.jpg",
                                            IpAddress = "10.20.30.40",
                                            SchemaLink = "https://intra/schemes/cabinet-1111"
                                        }
                                    }
                                }
                            },
                            new()
                            {
                                Name = "Участок 1.1.2",
                                LevelIndex = 1
                            }
                        }
                    },
                    new()
                    {
                        Name = "Линия 1.2",
                        LevelIndex = 0
                    }
                },
                ["Цех 2"] = new List<KbNode>
                {
                    new()
                    {
                        Name = "Линия 2.1",
                        LevelIndex = 0,
                        Children = new List<KbNode>
                        {
                            new()
                            {
                                Name = "Участок 2.1.1",
                                LevelIndex = 1,
                                Details = new KbNodeDetails
                                {
                                    Description = "Резервный участок",
                                    Location = "Северная зона"
                                }
                            }
                        }
                    },
                    new()
                    {
                        Name = "Линия 2.2",
                        LevelIndex = 0
                    }
                },
                ["Цех 3"] = new List<KbNode>
                {
                    new()
                    {
                        Name = "Линия 3.1",
                        LevelIndex = 0
                    }
                },
                ["Пустой цех"] = new List<KbNode>()
            },
            LastWorkshop = "Цех 2"
        };

    private static SavedData RenameWorkshops(SavedData source, params (string OldName, string NewName)[] renames)
    {
        var clone = CloneSavedData(source);
        var renameMap = renames.ToDictionary(pair => pair.OldName, pair => pair.NewName, StringComparer.Ordinal);
        var renamedWorkshops = new Dictionary<string, List<KbNode>>();

        foreach (var workshop in clone.Workshops)
        {
            string workshopName = renameMap.TryGetValue(workshop.Key, out var renamedName)
                ? renamedName
                : workshop.Key;
            renamedWorkshops[workshopName] = workshop.Value;
        }

        clone.Workshops = renamedWorkshops;
        if (renameMap.TryGetValue(clone.LastWorkshop, out var renamedLastWorkshop))
            clone.LastWorkshop = renamedLastWorkshop;

        return clone;
    }

    private static SavedData RenameLevel(SavedData source, int levelIndex, string newLevelName)
    {
        var clone = CloneSavedData(source);
        clone.Config.LevelNames[levelIndex] = newLevelName;
        return clone;
    }

    private static SavedData RenameNode(SavedData source, string workshopName, string currentNodeName, string newNodeName)
    {
        var clone = CloneSavedData(source);
        KbNode node = FindNodeByName(clone.Workshops[workshopName], currentNodeName);
        node.Name = newNodeName;
        return clone;
    }

    private static SavedData AddWorkshop(SavedData source, string workshopName)
    {
        var clone = CloneSavedData(source);
        clone.Workshops.Add(workshopName, new List<KbNode>());
        return clone;
    }

    private static SavedData ClearNodeDetails(SavedData source)
    {
        var clone = CloneSavedData(source);
        foreach (var workshop in clone.Workshops.Values)
            ClearNodeDetails(workshop);

        return clone;
    }

    private static void ClearNodeDetails(IEnumerable<KbNode> nodes)
    {
        foreach (var node in nodes)
        {
            node.Details = new KbNodeDetails();
            ClearNodeDetails(node.Children);
        }
    }

    private static KbNode FindNodeByName(IEnumerable<KbNode> nodes, string nodeName)
    {
        KbNode? match = null;

        foreach (var node in nodes)
        {
            if (string.Equals(node.Name, nodeName, StringComparison.Ordinal))
            {
                if (match != null)
                    throw new InvalidOperationException($"Узел '{nodeName}' не уникален в тестовых данных.");

                match = node;
            }

            KbNode? childMatch = TryFindNodeByName(node.Children, nodeName);
            if (childMatch != null)
            {
                if (match != null)
                    throw new InvalidOperationException($"Узел '{nodeName}' не уникален в тестовых данных.");

                match = childMatch;
            }
        }

        return match ?? throw new InvalidOperationException($"Узел '{nodeName}' не найден в тестовых данных.");
    }

    private static KbNode? TryFindNodeByName(IEnumerable<KbNode> nodes, string nodeName)
    {
        KbNode? match = null;

        foreach (var node in nodes)
        {
            if (string.Equals(node.Name, nodeName, StringComparison.Ordinal))
            {
                if (match != null)
                    throw new InvalidOperationException($"Узел '{nodeName}' не уникален в тестовых данных.");

                match = node;
            }

            KbNode? childMatch = TryFindNodeByName(node.Children, nodeName);
            if (childMatch != null)
            {
                if (match != null)
                    throw new InvalidOperationException($"Узел '{nodeName}' не уникален в тестовых данных.");

                match = childMatch;
            }
        }

        return match;
    }

    private static SavedData CloneSavedData(SavedData source)
    {
        var workshops = new Dictionary<string, List<KbNode>>();
        foreach (var workshop in source.Workshops)
        {
            workshops[workshop.Key] = workshop.Value.Select(CloneNode).ToList();
        }

        return new SavedData
        {
            SchemaVersion = source.SchemaVersion,
            Config = new KbConfig
            {
                MaxLevels = source.Config.MaxLevels,
                LevelNames = source.Config.LevelNames.ToList()
            },
            Workshops = workshops,
            LastWorkshop = source.LastWorkshop
        };
    }

    private static KbNode CloneNode(KbNode source) =>
        new()
        {
            Name = source.Name,
            LevelIndex = source.LevelIndex,
            Details = new KbNodeDetails
            {
                Description = source.Details.Description,
                Location = source.Details.Location,
                PhotoPath = source.Details.PhotoPath,
                IpAddress = source.Details.IpAddress,
                SchemaLink = source.Details.SchemaLink
            },
            Children = source.Children.Select(CloneNode).ToList()
        };

    private static void AssertSavedDataEquivalent(SavedData expected, SavedData actual)
    {
        Assert.Equal(expected.SchemaVersion, actual.SchemaVersion);
        Assert.Equal(expected.Config.MaxLevels, actual.Config.MaxLevels);
        Assert.Equal(expected.Config.LevelNames, actual.Config.LevelNames);
        Assert.Equal(expected.LastWorkshop, actual.LastWorkshop);
        Assert.Equal(expected.Workshops.Count, actual.Workshops.Count);
        Assert.Equal(expected.Workshops.Keys, actual.Workshops.Keys);
        Assert.Equal(GetEmptyWorkshopNames(expected), GetEmptyWorkshopNames(actual));
        Assert.Equal(GetRootNodeCounts(expected), GetRootNodeCounts(actual));
        Assert.Equal(GetNodeCountsByWorkshop(expected), GetNodeCountsByWorkshop(actual));
        Assert.Equal(CountNodes(expected.Workshops), CountNodes(actual.Workshops));
        AssertLevelIndexConsistency(expected);
        AssertLevelIndexConsistency(actual);

        foreach (var workshopName in expected.Workshops.Keys)
        {
            Assert.True(actual.Workshops.ContainsKey(workshopName));
            AssertNodesEquivalent(expected.Workshops[workshopName], actual.Workshops[workshopName]);
        }
    }

    private static int CountNodes(IReadOnlyDictionary<string, List<KbNode>> workshops) =>
        workshops.Values.Sum(CountNodes);

    private static int CountNodes(IEnumerable<KbNode> nodes) =>
        nodes.Sum(node => 1 + CountNodes(node.Children));

    private static IReadOnlyDictionary<string, int> GetRootNodeCounts(SavedData data) =>
        data.Workshops.ToDictionary(workshop => workshop.Key, workshop => workshop.Value.Count, StringComparer.Ordinal);

    private static IReadOnlyDictionary<string, int> GetNodeCountsByWorkshop(SavedData data) =>
        data.Workshops.ToDictionary(workshop => workshop.Key, workshop => CountNodes(workshop.Value), StringComparer.Ordinal);

    private static string[] GetEmptyWorkshopNames(SavedData data) =>
        data.Workshops
            .Where(workshop => workshop.Value.Count == 0)
            .Select(workshop => workshop.Key)
            .ToArray();

    private static void AssertNodesEquivalent(IReadOnlyList<KbNode> expected, IReadOnlyList<KbNode> actual)
    {
        Assert.Equal(expected.Count, actual.Count);

        for (int index = 0; index < expected.Count; index++)
        {
            Assert.Equal(expected[index].Name, actual[index].Name);
            Assert.Equal(expected[index].LevelIndex, actual[index].LevelIndex);
            Assert.Equal(expected[index].Details.Description, actual[index].Details.Description);
            Assert.Equal(expected[index].Details.Location, actual[index].Details.Location);
            Assert.Equal(expected[index].Details.PhotoPath, actual[index].Details.PhotoPath);
            Assert.Equal(expected[index].Details.IpAddress, actual[index].Details.IpAddress);
            Assert.Equal(expected[index].Details.SchemaLink, actual[index].Details.SchemaLink);
            AssertNodesEquivalent(expected[index].Children, actual[index].Children);
        }
    }

    private static void AssertLevelIndexConsistency(SavedData data)
    {
        foreach (var workshop in data.Workshops)
        {
            foreach (var rootNode in workshop.Value)
            {
                Assert.Equal(0, rootNode.LevelIndex);
                AssertLevelIndexConsistency(rootNode, data.Config.LevelNames.Count);
            }
        }
    }

    private static void AssertLevelIndexConsistency(KbNode node, int levelCount)
    {
        Assert.InRange(node.LevelIndex, 0, levelCount - 1);

        foreach (var child in node.Children)
        {
            Assert.Equal(node.LevelIndex + 1, child.LevelIndex);
            AssertLevelIndexConsistency(child, levelCount);
        }
    }

    private static string CreateTempDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), $"asutp-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed record WorkshopNodeSheetInfo(
        string SheetName,
        string SheetKind,
        string WorkshopId,
        string NodesSheetKey,
        int NodeRowCount);

    private sealed record WorksheetCellInfo(string? Type, string Value);

    private sealed record TestWorkshop(string Name, bool HasNodes);
}
