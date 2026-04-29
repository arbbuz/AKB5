using AsutpKnowledgeBase.Services;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace AsutpKnowledgeBase.Core.Tests;

public class KnowledgeBaseMaintenanceWorkbookTemplateServiceTests
{
    private readonly KnowledgeBaseMaintenanceWorkbookTemplateService _service = new();

    [Fact]
    public void GetTemplatePackage_ReturnsApprovedWorkbookWith12MonthSheets()
    {
        byte[] packageBytes = _service.GetTemplatePackage();

        using var stream = new MemoryStream(packageBytes, writable: false);
        using SpreadsheetDocument document = SpreadsheetDocument.Open(stream, false);
        WorkbookPart workbookPart = Assert.IsType<WorkbookPart>(document.WorkbookPart);
        string[] sheetNames = workbookPart.Workbook.Sheets!
            .Elements<Sheet>()
            .Select(static sheet => sheet.Name?.Value ?? string.Empty)
            .ToArray();

        Assert.Equal(
            Enumerable.Range(1, 12).Select(static month => $"КЦ ({month})").ToArray(),
            sheetNames);
    }

    [Fact]
    public void GetTemplatePackage_ReturnsIndependentByteArrays()
    {
        byte[] first = _service.GetTemplatePackage();
        byte[] second = _service.GetTemplatePackage();

        Assert.NotSame(first, second);
        Assert.Equal(second.Length, first.Length);

        byte originalSecondByte = second[0];
        first[0] = first[0] == byte.MaxValue ? byte.MinValue : (byte)(first[0] + 1);

        Assert.Equal(originalSecondByte, second[0]);
    }

    [Fact]
    public void GetMonthSheetNames_ReturnsExpectedMonthNames()
    {
        IReadOnlyList<string> sheetNames = _service.GetMonthSheetNames();

        Assert.Equal(
            Enumerable.Range(1, 12).Select(static month => $"КЦ ({month})").ToArray(),
            sheetNames);
    }
}
