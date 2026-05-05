using System.Text;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.Core.Tests;

public class KnowledgeBaseProductionCalendarJsonImportServiceTests
{
    private readonly KnowledgeBaseProductionCalendarJsonImportService _service = new();

    [Fact]
    public void ImportJson_ReadsProductionCalendarYearsDocument()
    {
        byte[] json = Encoding.UTF8.GetBytes(
            """
            {
              "ProductionCalendarYears": [
                {
                  "Year": 2027,
                  "AdditionalNonWorkingDays": [
                    "2027-01-08",
                    "2027-05-10",
                    "2027-05-10"
                  ]
                }
              ]
            }
            """);

        KnowledgeBaseProductionCalendarJsonImportResult result = _service.ImportJson(json);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.ImportedYearCount);
        var year = Assert.Single(result.ProductionCalendarYears);
        Assert.Equal(2027, year.Year);
        Assert.Equal(
            new[] { new DateOnly(2027, 1, 8), new DateOnly(2027, 5, 10) },
            year.AdditionalNonWorkingDays);
    }

    [Fact]
    public void ImportJson_ReadsProductionCalendarYearArray()
    {
        byte[] json = Encoding.UTF8.GetBytes(
            """
            [
              {
                "Year": 2028,
                "AdditionalNonWorkingDays": [ "2028-01-10" ]
              }
            ]
            """);

        KnowledgeBaseProductionCalendarJsonImportResult result = _service.ImportJson(json);

        Assert.True(result.IsSuccess);
        var year = Assert.Single(result.ProductionCalendarYears);
        Assert.Equal(2028, year.Year);
        Assert.Equal(new DateOnly(2028, 1, 10), Assert.Single(year.AdditionalNonWorkingDays));
    }

    [Fact]
    public void ImportJson_ReadsRussianDateFormat()
    {
        byte[] json = Encoding.UTF8.GetBytes(
            """
            {
              "ProductionCalendarYears": [
                {
                  "Year": 2028,
                  "AdditionalNonWorkingDays": [ "10.01.2028" ]
                }
              ]
            }
            """);

        KnowledgeBaseProductionCalendarJsonImportResult result = _service.ImportJson(json);

        Assert.True(result.IsSuccess);
        var year = Assert.Single(result.ProductionCalendarYears);
        Assert.Equal(2028, year.Year);
        Assert.Equal(new DateOnly(2028, 1, 10), Assert.Single(year.AdditionalNonWorkingDays));
    }

    [Fact]
    public void ImportJson_RejectsDatesFromAnotherYear()
    {
        byte[] json = Encoding.UTF8.GetBytes(
            """
            {
              "ProductionCalendarYears": [
                {
                  "Year": 2027,
                  "AdditionalNonWorkingDays": [ "2028-01-01" ]
                }
              ]
            }
            """);

        KnowledgeBaseProductionCalendarJsonImportResult result = _service.ImportJson(json);

        Assert.False(result.IsSuccess);
        Assert.Contains("01.01.2028", result.ErrorMessage, StringComparison.Ordinal);
        Assert.Contains("2027", result.ErrorMessage, StringComparison.Ordinal);
    }
}
