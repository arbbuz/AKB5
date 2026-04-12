using System.Collections.Generic;

namespace AsutpKnowledgeBase.Services
{
    internal sealed record KnowledgeBaseSpreadsheetWorkbookData(
        IReadOnlyList<KnowledgeBaseSpreadsheetWorksheetData> Worksheets);

    internal sealed record KnowledgeBaseSpreadsheetWorksheetData(
        string SheetName,
        List<string[]> Rows);
}
