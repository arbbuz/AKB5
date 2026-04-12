using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.Core.Tests;

public class KnowledgeBaseExcelExchangeServiceTests
{
    [Fact]
    public void BuildWorkbookPackage_CreatesExpectedSheetsAndMetadata()
    {
        var service = new KnowledgeBaseExcelExchangeService();

        byte[] packageBytes = service.BuildWorkbookPackage(CreateSampleData());
        string[] sheetNames = GetWorksheetNames(packageBytes);
        var metaRows = ReadWorksheetRows(packageBytes, "Meta");

        Assert.Equal(new[] { "Meta", "Levels", "Workshops", "Nodes" }, sheetNames);
        Assert.Contains(metaRows, row => row.SequenceEqual(new[] { "FormatId", KnowledgeBaseExcelExchangeService.WorkbookFormatId }));
        Assert.Contains(metaRows, row => row.SequenceEqual(new[] { "FormatVersion", KnowledgeBaseExcelExchangeService.WorkbookFormatVersion.ToString() }));
        Assert.Contains(metaRows, row => row.SequenceEqual(new[] { "SchemaVersion", SavedData.CurrentSchemaVersion.ToString() }));
        Assert.Contains(metaRows, row => row.SequenceEqual(new[] { "LastWorkshop", "Пустой цех" }));
        Assert.Contains(metaRows, row => row.SequenceEqual(new[] { "LastWorkshopId", "W2" }));
    }

    [Fact]
    public void BuildWorkbookPackage_PreservesEmptyWorkshopsAndNodeHierarchy()
    {
        var service = new KnowledgeBaseExcelExchangeService();

        byte[] packageBytes = service.BuildWorkbookPackage(CreateSampleData());
        var workshopRows = ReadWorksheetRows(packageBytes, "Workshops");
        var nodeRows = ReadWorksheetRows(packageBytes, "Nodes");

        Assert.Contains(workshopRows, row => row.SequenceEqual(new[] { "2", "Пустой цех", "TRUE", "W2" }));

        var rootRow = nodeRows.Single(row => row[6] == "Линия 1");
        var childRow = nodeRows.Single(row => row[6] == "Щит 1");
        var secondRootRow = nodeRows.Single(row => row[6] == "Линия 2");

        Assert.Equal(string.Empty, rootRow[2]);
        Assert.Equal(rootRow[0], childRow[2]);
        Assert.Equal("1", childRow[3]);
        Assert.Equal("2", secondRootRow[3]);
        Assert.Equal("Цех 1 / Линия 1 / Щит 1", childRow[7]);
    }

    [Fact]
    public void BuildWorkbookPackage_StoresNumericColumnsAsNumericCells()
    {
        var service = new KnowledgeBaseExcelExchangeService();

        byte[] packageBytes = service.BuildWorkbookPackage(CreateSampleData());

        var levelIndexCell = GetCell(packageBytes, "Levels", rowIndex: 2, cellIndex: 1);
        var workshopOrderCell = GetCell(packageBytes, "Workshops", rowIndex: 2, cellIndex: 1);
        var selectedWorkshopCell = GetCell(packageBytes, "Workshops", rowIndex: 3, cellIndex: 3);
        var nodeIdCell = GetCell(packageBytes, "Nodes", rowIndex: 2, cellIndex: 1);

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
    public void ImportFromPackage_RoundTripsExportedWorkbook()
    {
        var service = new KnowledgeBaseExcelExchangeService();

        byte[] packageBytes = service.BuildWorkbookPackage(CreateSampleData());
        var result = service.ImportFromPackage(packageBytes);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        AssertSavedDataEquivalent(CreateSampleData(), result.Data!);
    }

    [Fact]
    public void Import_WhenRequiredSheetMissing_ReturnsReadableError()
    {
        var service = new KnowledgeBaseExcelExchangeService();

        byte[] packageBytes = service.BuildWorkbookPackage(CreateSampleData());
        packageBytes = UpdateXmlEntry(packageBytes, "xl/workbook.xml", document =>
        {
            XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            document.Descendants(ns + "sheet")
                .Single(sheet => string.Equals(sheet.Attribute("name")?.Value, "Nodes", StringComparison.Ordinal))
                .Remove();
        });

        var result = service.ImportFromPackage(packageBytes);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("Nodes", result.ErrorMessage);
    }

    [Fact]
    public void Import_WhenParentNodeMissing_ReturnsReadableError()
    {
        var service = new KnowledgeBaseExcelExchangeService();

        byte[] packageBytes = service.BuildWorkbookPackage(CreateSampleData());
        packageBytes = UpdateWorksheetCellValue(packageBytes, "Nodes", rowIndex: 3, cellIndex: 3, value: "999", cellType: null);

        var result = service.ImportFromPackage(packageBytes);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("ParentNodeId", result.ErrorMessage);
    }

    [Fact]
    public void Import_WhenNodeNameChangedButPathNotUpdated_StillSucceeds()
    {
        var service = new KnowledgeBaseExcelExchangeService();

        byte[] packageBytes = service.BuildWorkbookPackage(CreateSampleData());
        packageBytes = UpdateWorksheetCellValue(packageBytes, "Nodes", rowIndex: 2, cellIndex: 7, value: "Линия 1 renamed", cellType: "inlineStr");

        var result = service.ImportFromPackage(packageBytes);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("Линия 1 renamed", result.Data!.Workshops["Цех 1"][0].Name);
    }

    [Fact]
    public void Import_WhenLevelNamesChangedButNodeLevelNameColumnIsStale_StillSucceeds()
    {
        var service = new KnowledgeBaseExcelExchangeService();

        byte[] packageBytes = service.BuildWorkbookPackage(CreateSampleData());
        packageBytes = UpdateWorksheetCellValue(packageBytes, "Levels", rowIndex: 2, cellIndex: 2, value: "Производство", cellType: "inlineStr");

        var result = service.ImportFromPackage(packageBytes);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("Производство", result.Data!.Config.LevelNames[0]);
        Assert.Equal("Линия 1", result.Data.Workshops["Цех 1"][0].Name);
    }

    [Fact]
    public void Import_WhenMetaLastWorkshopIsStale_UsesSelectedWorkshopRow()
    {
        var service = new KnowledgeBaseExcelExchangeService();

        byte[] packageBytes = service.BuildWorkbookPackage(CreateSampleData());
        packageBytes = UpdateMetaPropertyValue(packageBytes, "LastWorkshop", "Цех 1");

        var result = service.ImportFromPackage(packageBytes);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("Пустой цех", result.Data!.LastWorkshop);
    }

    [Fact]
    public void Import_WhenWorkshopRenamedButNodeWorkshopColumnIsStale_StillSucceeds()
    {
        var service = new KnowledgeBaseExcelExchangeService();
        var sourceData = CreateWorkshopRemapSampleData(
            new TestWorkshop("Цех 1", HasNodes: true),
            new TestWorkshop("Цех 2", HasNodes: true));

        byte[] packageBytes = service.BuildWorkbookPackage(sourceData);
        packageBytes = UpdateWorkshopRow(packageBytes, rowIndex: 2, workshopName: "Производство");

        var result = service.ImportFromPackage(packageBytes);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        AssertSavedDataEquivalent(
            RenameWorkshops(sourceData, ("Цех 1", "Производство")),
            result.Data!);
    }

    [Fact]
    public void Import_WhenWorkshopRenamedAndUnusedWorkshopExists_StillSucceeds()
    {
        var service = new KnowledgeBaseExcelExchangeService();
        var sourceData = CreateSampleData();

        byte[] packageBytes = service.BuildWorkbookPackage(sourceData);
        packageBytes = UpdateWorkshopRow(packageBytes, rowIndex: 2, workshopName: "Производство");

        var result = service.ImportFromPackage(packageBytes);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        AssertSavedDataEquivalent(
            RenameWorkshops(sourceData, ("Цех 1", "Производство")),
            result.Data!);
    }

    [Fact]
    public void Import_WhenMultipleAdjacentWorkshopsRenamedInSameGap_StillSucceeds()
    {
        var service = new KnowledgeBaseExcelExchangeService();
        var sourceData = CreateWorkshopRemapSampleData(
            new TestWorkshop("Цех 1", HasNodes: true),
            new TestWorkshop("Цех 2", HasNodes: true),
            new TestWorkshop("Цех 3", HasNodes: true),
            new TestWorkshop("Цех 4", HasNodes: true));

        byte[] packageBytes = service.BuildWorkbookPackage(sourceData);
        packageBytes = UpdateWorkshopRow(packageBytes, rowIndex: 3, workshopName: "Производство 2");
        packageBytes = UpdateWorkshopRow(packageBytes, rowIndex: 4, workshopName: "Производство 3");

        var result = service.ImportFromPackage(packageBytes);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        AssertSavedDataEquivalent(
            RenameWorkshops(
                sourceData,
                ("Цех 2", "Производство 2"),
                ("Цех 3", "Производство 3")),
            result.Data!);
    }

    [Fact]
    public void Import_WhenAllWorkshopNamesRenamedSoCommonAnchorsDisappear_StillSucceeds()
    {
        var service = new KnowledgeBaseExcelExchangeService();
        var sourceData = CreateComplexRoundTripData();

        byte[] packageBytes = service.BuildWorkbookPackage(sourceData);
        packageBytes = UpdateWorkshopRow(packageBytes, rowIndex: 2, workshopName: "Производство A");
        packageBytes = UpdateWorkshopRow(packageBytes, rowIndex: 3, workshopName: "Производство B");
        packageBytes = UpdateWorkshopRow(packageBytes, rowIndex: 4, workshopName: "Производство C");
        packageBytes = UpdateWorkshopRow(packageBytes, rowIndex: 5, workshopName: "Архив");

        var result = service.ImportFromPackage(packageBytes);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        AssertSavedDataEquivalent(
            RenameWorkshops(
                sourceData,
                ("Цех 1", "Производство A"),
                ("Цех 2", "Производство B"),
                ("Цех 3", "Производство C"),
                ("Пустой цех", "Архив")),
            result.Data!);
    }

    [Fact]
    public void Import_WhenNodeWorkshopNamesAndMetaLastWorkshopAreStale_StillUsesSelectedWorkshop()
    {
        var service = new KnowledgeBaseExcelExchangeService();
        var sourceData = CreateComplexRoundTripData();

        byte[] packageBytes = service.BuildWorkbookPackage(sourceData);
        packageBytes = UpdateWorkshopRow(packageBytes, rowIndex: 3, workshopName: "Производство B");
        packageBytes = UpdateMetaPropertyValue(packageBytes, "LastWorkshop", "Цех 2");

        var result = service.ImportFromPackage(packageBytes);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        AssertSavedDataEquivalent(
            RenameWorkshops(sourceData, ("Цех 2", "Производство B")),
            result.Data!);
    }

    [Fact]
    public void Import_WhenWorkbookMutatedLikeUserInExcel_RoundTripsWithoutLosingStructure()
    {
        var service = new KnowledgeBaseExcelExchangeService();
        var sourceData = CreateComplexRoundTripData();

        byte[] packageBytes = service.BuildWorkbookPackage(sourceData);
        packageBytes = UpdateWorkshopRow(packageBytes, rowIndex: 2, workshopName: "Производство A");
        packageBytes = UpdateWorkshopRow(packageBytes, rowIndex: 3, workshopName: "Производство B");
        packageBytes = UpdateWorkshopRow(packageBytes, rowIndex: 4, workshopName: "Производство C");
        packageBytes = UpdateWorkshopRow(packageBytes, rowIndex: 5, workshopName: "Архив");
        packageBytes = UpdateWorksheetCellValue(packageBytes, "Levels", rowIndex: 2, cellIndex: 2, value: "Производство", cellType: "inlineStr");
        packageBytes = UpdateWorksheetCellValue(packageBytes, "Levels", rowIndex: 3, cellIndex: 2, value: "Линия обновлена", cellType: "inlineStr");
        packageBytes = UpdateMetaPropertyValue(packageBytes, "LastWorkshop", "Цех 2");

        var result = service.ImportFromPackage(packageBytes);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        AssertSavedDataEquivalent(
            RenameLevels(
                RenameWorkshops(
                    sourceData,
                    ("Цех 1", "Производство A"),
                    ("Цех 2", "Производство B"),
                    ("Цех 3", "Производство C"),
                    ("Пустой цех", "Архив")),
                "Производство",
                "Линия обновлена",
                "Участок",
                "Шкаф"),
            result.Data!);
    }

    [Fact]
    public void Import_WhenLegacyWorkbookWithoutStableWorkshopIds_RemainsBackwardCompatible()
    {
        var service = new KnowledgeBaseExcelExchangeService();
        var sourceData = CreateComplexRoundTripData();

        byte[] packageBytes = service.BuildWorkbookPackage(sourceData);
        packageBytes = DowngradeWorkbookToLegacyFormat(packageBytes);

        var result = service.ImportFromPackage(packageBytes);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        AssertSavedDataEquivalent(sourceData, result.Data!);
    }

    [Fact]
    public void Import_WhenLegacyWorkbookWorkshopRenameIsAmbiguous_FailsWithClearError()
    {
        var service = new KnowledgeBaseExcelExchangeService();

        byte[] packageBytes = service.BuildWorkbookPackage(CreateSampleData());
        packageBytes = UpdateWorkshopRow(packageBytes, rowIndex: 2, workshopName: "Производство");
        packageBytes = DowngradeWorkbookToLegacyFormat(packageBytes);

        var result = service.ImportFromPackage(packageBytes);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("Не удалось однозначно сопоставить имена цехов", result.ErrorMessage);
        Assert.DoesNotContain("неизвестный цех", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Import_WhenLegacyWorkbookGapAlignmentIsAmbiguous_FailsWithClearError()
    {
        var service = new KnowledgeBaseExcelExchangeService();

        byte[] packageBytes = service.BuildWorkbookPackage(CreateWorkshopRemapSampleData(
            new TestWorkshop("Цех 1", HasNodes: true),
            new TestWorkshop("Цех 2", HasNodes: true),
            new TestWorkshop("Цех 3", HasNodes: true)));
        packageBytes = UpdateWorkshopRow(packageBytes, rowIndex: 2, workshopOrder: 2, workshopName: "Производство 1");
        packageBytes = UpdateWorkshopRow(packageBytes, rowIndex: 3, workshopOrder: 1);
        packageBytes = UpdateWorkshopRow(packageBytes, rowIndex: 4, workshopOrder: 3, workshopName: "Производство 3");
        packageBytes = DowngradeWorkbookToLegacyFormat(packageBytes);

        var result = service.ImportFromPackage(packageBytes);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("Не удалось однозначно сопоставить имена цехов", result.ErrorMessage);
        Assert.DoesNotContain("неизвестный цех", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Import_WhenNoRenameButUnusedWorkshopsExist_StillSucceeds()
    {
        var service = new KnowledgeBaseExcelExchangeService();
        var sourceData = CreateWorkshopRemapSampleData(
            new TestWorkshop("Цех 1", HasNodes: true),
            new TestWorkshop("Цех 2", HasNodes: true),
            new TestWorkshop("Пустой цех", HasNodes: false));

        byte[] packageBytes = service.BuildWorkbookPackage(sourceData);

        var result = service.ImportFromPackage(packageBytes);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        AssertSavedDataEquivalent(sourceData, result.Data!);
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
            .ToList();
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

    private static byte[] UpdateMetaPropertyValue(byte[] sourceBytes, string propertyName, string value)
    {
        string entryPath = GetWorksheetEntryPath(sourceBytes, "Meta");
        return UpdateXmlEntry(sourceBytes, entryPath, document =>
        {
            XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            var row = document
                .Descendants(ns + "row")
                .Skip(1)
                .Single(row => string.Equals(ReadRowValues(row).FirstOrDefault(), propertyName, StringComparison.Ordinal));
            var cell = row.Elements(ns + "c").ElementAt(1);

            cell.Elements().Remove();
            cell.Attribute("t")?.Remove();
            cell.Add(new XAttribute("t", "inlineStr"));
            cell.Add(new XElement(ns + "is", new XElement(ns + "t", value)));
        });
    }

    private static byte[] DowngradeWorkbookToLegacyFormat(byte[] sourceBytes)
    {
        byte[] updatedBytes = UpdateMetaPropertyValue(
            sourceBytes,
            "FormatVersion",
            KnowledgeBaseExcelExchangeService.LegacyWorkbookFormatVersion.ToString());

        updatedBytes = RemoveMetaProperty(updatedBytes, "LastWorkshopId");
        updatedBytes = RemoveWorksheetColumn(updatedBytes, "Workshops", cellIndex: 4);
        updatedBytes = RemoveWorksheetColumn(updatedBytes, "Nodes", cellIndex: 9);

        return updatedBytes;
    }

    private static byte[] UpdateWorkshopRow(
        byte[] sourceBytes,
        int rowIndex,
        int? workshopOrder = null,
        string? workshopName = null,
        bool? isLastSelected = null)
    {
        byte[] updatedBytes = sourceBytes;

        if (workshopOrder.HasValue)
        {
            updatedBytes = UpdateWorksheetCellValue(
                updatedBytes,
                "Workshops",
                rowIndex,
                cellIndex: 1,
                value: workshopOrder.Value.ToString(),
                cellType: null);
        }

        if (workshopName != null)
        {
            updatedBytes = UpdateWorksheetCellValue(
                updatedBytes,
                "Workshops",
                rowIndex,
                cellIndex: 2,
                value: workshopName,
                cellType: "inlineStr");
        }

        if (isLastSelected.HasValue)
        {
            updatedBytes = UpdateWorksheetCellValue(
                updatedBytes,
                "Workshops",
                rowIndex,
                cellIndex: 3,
                value: isLastSelected.Value ? "1" : "0",
                cellType: "b");
        }

        return updatedBytes;
    }

    private static byte[] RemoveMetaProperty(byte[] sourceBytes, string propertyName)
    {
        string entryPath = GetWorksheetEntryPath(sourceBytes, "Meta");
        return UpdateXmlEntry(sourceBytes, entryPath, document =>
        {
            XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            document
                .Descendants(ns + "row")
                .Skip(1)
                .Single(row => string.Equals(ReadRowValues(row).FirstOrDefault(), propertyName, StringComparison.Ordinal))
                .Remove();
        });
    }

    private static byte[] RemoveWorksheetColumn(byte[] sourceBytes, string worksheetName, int cellIndex)
    {
        string entryPath = GetWorksheetEntryPath(sourceBytes, worksheetName);
        return UpdateXmlEntry(sourceBytes, entryPath, document =>
        {
            XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

            foreach (var row in document.Descendants(ns + "row"))
            {
                var cells = row.Elements(ns + "c").ToList();
                if (cells.Count >= cellIndex)
                    cells[cellIndex - 1].Remove();
            }
        });
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
                        Children = new List<KbNode>
                        {
                            new()
                            {
                                Name = "Щит 1",
                                LevelIndex = 1
                            }
                        }
                    },
                    new()
                    {
                        Name = "Линия 2",
                        LevelIndex = 0
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
                                Children = new List<KbNode>
                                {
                                    new()
                                    {
                                        Name = "Шкаф 1.1.1.1",
                                        LevelIndex = 2
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
                                LevelIndex = 1
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

    private static SavedData RenameLevels(SavedData source, params string[] levelNames)
    {
        var clone = CloneSavedData(source);
        clone.Config.LevelNames = levelNames.ToList();
        clone.Config.MaxLevels = clone.Config.LevelNames.Count;
        return clone;
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
            Children = source.Children.Select(CloneNode).ToList()
        };

    private static void AssertSavedDataEquivalent(SavedData expected, SavedData actual)
    {
        Assert.Equal(expected.SchemaVersion, actual.SchemaVersion);
        Assert.Equal(expected.Config.MaxLevels, actual.Config.MaxLevels);
        Assert.Equal(expected.Config.LevelNames, actual.Config.LevelNames);
        Assert.Equal(expected.LastWorkshop, actual.LastWorkshop);
        Assert.Equal(expected.Workshops.Keys, actual.Workshops.Keys);
        Assert.Equal(CountNodes(expected.Workshops), CountNodes(actual.Workshops));

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

    private static void AssertNodesEquivalent(IReadOnlyList<KbNode> expected, IReadOnlyList<KbNode> actual)
    {
        Assert.Equal(expected.Count, actual.Count);

        for (int index = 0; index < expected.Count; index++)
        {
            Assert.Equal(expected[index].Name, actual[index].Name);
            Assert.Equal(expected[index].LevelIndex, actual[index].LevelIndex);
            AssertNodesEquivalent(expected[index].Children, actual[index].Children);
        }
    }

    private static string CreateTempDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), $"asutp-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed record WorksheetCellInfo(string? Type, string Value);

    private sealed record TestWorkshop(string Name, bool HasNodes);
}
