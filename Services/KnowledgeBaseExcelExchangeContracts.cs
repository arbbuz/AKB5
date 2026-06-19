using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public interface IKnowledgeBaseExcelExchangeService
    {
        byte[] BuildWorkbookPackage(SavedData data);

        KnowledgeBaseExcelExportResult Export(SavedData data, string path);

        KnowledgeBaseExcelImportResult Import(string path);

        KnowledgeBaseExcelImportResult ImportFromPackage(byte[] packageBytes);
    }

    public class KnowledgeBaseExcelExportResult
    {
        public bool IsSuccess { get; init; }

        public string? ErrorMessage { get; init; }
    }

    public class KnowledgeBaseExcelImportResult
    {
        public bool IsSuccess { get; init; }

        public SavedData? Data { get; init; }

        public string? ErrorMessage { get; init; }
    }
}
