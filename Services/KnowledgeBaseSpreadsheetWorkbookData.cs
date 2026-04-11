using System.Collections.Generic;

namespace AsutpKnowledgeBase.Services
{
    internal sealed record KnowledgeBaseSpreadsheetWorkbookData(
        List<string[]> MetaRows,
        List<string[]> LevelRows,
        List<string[]> WorkshopRows,
        List<string[]> NodeRows);
}
