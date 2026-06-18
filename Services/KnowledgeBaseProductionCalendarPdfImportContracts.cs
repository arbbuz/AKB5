using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public interface IKnowledgeBaseProductionCalendarPdfImporter
    {
        KnowledgeBaseProductionCalendarPdfImportResult ImportPdf(byte[]? pdfBytes);
    }

    public sealed class KnowledgeBaseProductionCalendarPdfImportResult
    {
        public bool IsSuccess { get; init; }

        public string ErrorMessage { get; init; } = string.Empty;

        public List<KbProductionCalendarYear> ProductionCalendarYears { get; init; } = new();

        public int ImportedYearCount { get; init; }

        public List<string> Warnings { get; init; } = new();
    }
}
