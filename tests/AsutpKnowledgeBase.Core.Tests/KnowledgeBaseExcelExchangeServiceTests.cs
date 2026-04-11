using System.Xml.Linq;
using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.Core.Tests;

public class KnowledgeBaseExcelExchangeServiceTests
{
    [Fact]
    public void BuildWorkbookXml_CreatesExpectedSheetsAndMetadata()
    {
        var service = new KnowledgeBaseExcelExchangeService();

        string xml = service.BuildWorkbookXml(CreateSampleData());
        var document = XDocument.Parse(xml);
        var sheetNames = GetWorksheetNames(document);
        var metaRows = ReadWorksheetRows(document, "Meta");

        Assert.Equal(new[] { "Meta", "Levels", "Workshops", "Nodes" }, sheetNames);
        Assert.Contains(metaRows, row => row.SequenceEqual(new[] { "FormatId", KnowledgeBaseExcelExchangeService.WorkbookFormatId }));
        Assert.Contains(metaRows, row => row.SequenceEqual(new[] { "FormatVersion", KnowledgeBaseExcelExchangeService.WorkbookFormatVersion.ToString() }));
        Assert.Contains(metaRows, row => row.SequenceEqual(new[] { "SchemaVersion", SavedData.CurrentSchemaVersion.ToString() }));
        Assert.Contains(metaRows, row => row.SequenceEqual(new[] { "LastWorkshop", "Пустой цех" }));
    }

    [Fact]
    public void BuildWorkbookXml_PreservesEmptyWorkshopsAndNodeHierarchy()
    {
        var service = new KnowledgeBaseExcelExchangeService();

        string xml = service.BuildWorkbookXml(CreateSampleData());
        var document = XDocument.Parse(xml);
        var workshopRows = ReadWorksheetRows(document, "Workshops");
        var nodeRows = ReadWorksheetRows(document, "Nodes");

        Assert.Contains(workshopRows, row => row.SequenceEqual(new[] { "2", "Пустой цех", "TRUE" }));

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
    public void Export_WritesSpreadsheetXmlFile()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            string path = Path.Combine(tempDirectory, "kb-export.xml");
            var service = new KnowledgeBaseExcelExchangeService();

            var result = service.Export(CreateSampleData(), path);

            Assert.True(result.IsSuccess);
            Assert.Null(result.ErrorMessage);
            Assert.True(File.Exists(path));

            string xml = File.ReadAllText(path);
            Assert.Contains("Excel.Sheet", xml);
            Assert.Contains("Worksheet", xml);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void ImportFromXml_RoundTripsExportedWorkbook()
    {
        var service = new KnowledgeBaseExcelExchangeService();

        string xml = service.BuildWorkbookXml(CreateSampleData());
        var result = service.ImportFromXml(xml);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("Пустой цех", result.Data!.LastWorkshop);
        Assert.Equal(new[] { "Цех", "Линия", "Щит" }, result.Data.Config.LevelNames);
        Assert.Equal(2, result.Data.Workshops.Count);
        Assert.Empty(result.Data.Workshops["Пустой цех"]);
        Assert.Equal("Линия 1", result.Data.Workshops["Цех 1"][0].Name);
        Assert.Equal("Щит 1", result.Data.Workshops["Цех 1"][0].Children[0].Name);
    }

    [Fact]
    public void ImportFromXml_WhenRequiredSheetMissing_ReturnsReadableError()
    {
        var service = new KnowledgeBaseExcelExchangeService();

        string xml = service.BuildWorkbookXml(CreateSampleData());
        var document = XDocument.Parse(xml);
        XNamespace ns = "urn:schemas-microsoft-com:office:spreadsheet";
        document.Descendants(ns + "Worksheet")
            .Single(worksheet => string.Equals(worksheet.Attribute(ns + "Name")?.Value, "Nodes", StringComparison.Ordinal))
            .Remove();

        var result = service.ImportFromXml(document.ToString());

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("Nodes", result.ErrorMessage);
    }

    [Fact]
    public void ImportFromXml_WhenParentNodeMissing_ReturnsReadableError()
    {
        var service = new KnowledgeBaseExcelExchangeService();

        string xml = service.BuildWorkbookXml(CreateSampleData());
        var document = XDocument.Parse(xml);
        SetWorksheetCellValue(document, "Nodes", rowIndex: 3, cellIndex: 3, value: "999");

        var result = service.ImportFromXml(document.ToString());

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("ParentNodeId", result.ErrorMessage);
    }

    private static string[] GetWorksheetNames(XDocument document)
    {
        XNamespace ns = "urn:schemas-microsoft-com:office:spreadsheet";
        return document
            .Descendants(ns + "Worksheet")
            .Select(worksheet => worksheet.Attribute(ns + "Name")?.Value ?? string.Empty)
            .ToArray();
    }

    private static List<string[]> ReadWorksheetRows(XDocument document, string worksheetName)
    {
        XNamespace ns = "urn:schemas-microsoft-com:office:spreadsheet";
        var worksheet = document
            .Descendants(ns + "Worksheet")
            .Single(worksheet => string.Equals(worksheet.Attribute(ns + "Name")?.Value, worksheetName, StringComparison.Ordinal));

        return worksheet
            .Descendants(ns + "Row")
            .Skip(1)
            .Select(row => row
                .Elements(ns + "Cell")
                .Select(cell => cell.Element(ns + "Data")?.Value ?? string.Empty)
                .ToArray())
            .ToList();
    }

    private static void SetWorksheetCellValue(XDocument document, string worksheetName, int rowIndex, int cellIndex, string value)
    {
        XNamespace ns = "urn:schemas-microsoft-com:office:spreadsheet";
        var row = document
            .Descendants(ns + "Worksheet")
            .Single(worksheet => string.Equals(worksheet.Attribute(ns + "Name")?.Value, worksheetName, StringComparison.Ordinal))
            .Descendants(ns + "Row")
            .ElementAt(rowIndex - 1);

        var cell = row.Elements(ns + "Cell").ElementAt(cellIndex - 1);
        cell.Element(ns + "Data")!.Value = value;
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

    private static string CreateTempDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), $"asutp-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }
}
