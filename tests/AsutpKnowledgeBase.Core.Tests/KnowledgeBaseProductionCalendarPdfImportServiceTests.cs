using System.Text;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.Core.Tests;

public class KnowledgeBaseProductionCalendarPdfImportServiceTests
{
    private readonly KnowledgeBaseProductionCalendarPdfImportService _service = new();

    [Fact]
    public void ImportText_ReadsTransferredDaysAndRestPeriods()
    {
        const string text = """
            ПРОИЗВОДСТВЕННЫЙ КАЛЕНДАРЬ НА 2027 ГОД
            В 2027 году в соответствии с Проектом Постановления Правительства РФ
            перенесены следующие выходные дни:
            с субботы 2 января на пятницу 5 ноября;
            с воскресенья 3 января на пятницу 31 декабря;
            c субботы 20 февраля на понедельник 22 февраля.
            Следовательно, дни отдыха будут с 31 декабря 2026 года по 10 января 2027 года.
            Днями отдыха в связи с Днем защитника Отечества будут периоды с 21 по 23 февраля 2027 года.
            В мае 2027 года работники будут отдыхать с 1 по 3 мая, а также с 8 по 10 мая.
            В июне период отдыха продлится с 12 по 14 июня, а в ноябре с 4 по 7 ноября.
            2027 год 365 247 118 1972
            """;

        KnowledgeBaseProductionCalendarPdfImportResult result = _service.ImportText(text);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        var year = Assert.Single(result.ProductionCalendarYears);
        Assert.Equal(2027, year.Year);
        Assert.Equal(
            new[]
            {
                new DateOnly(2027, 2, 22),
                new DateOnly(2027, 5, 3),
                new DateOnly(2027, 5, 10),
                new DateOnly(2027, 6, 14),
                new DateOnly(2027, 11, 5),
                new DateOnly(2027, 12, 31)
            },
            year.AdditionalNonWorkingDays);
        Assert.Equal(new[] { new DateOnly(2027, 2, 20) }, year.AdditionalWorkingDays);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void ImportText_WhenTextLayerIsMissing_ReturnsFailure()
    {
        KnowledgeBaseProductionCalendarPdfImportResult result = _service.ImportText(" ");

        Assert.False(result.IsSuccess);
        Assert.Contains("текстовый слой", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ImportPdf_WhenBytesAreInvalid_ReturnsFailure()
    {
        KnowledgeBaseProductionCalendarPdfImportResult result = _service.ImportPdf(Encoding.UTF8.GetBytes("not a pdf"));

        Assert.False(result.IsSuccess);
        Assert.Contains("PDF", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }
}
