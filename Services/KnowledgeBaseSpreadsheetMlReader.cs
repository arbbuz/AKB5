using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    internal sealed class KnowledgeBaseSpreadsheetMlReader
    {
        private static readonly XNamespace SpreadsheetNamespace = "urn:schemas-microsoft-com:office:spreadsheet";
        private readonly KnowledgeBaseExcelWorkbookParser _parser = new();

        public SavedData ParseWorkbookXml(string xml)
        {
            var document = XDocument.Parse(xml);
            var worksheets = GetWorksheets(document);
            var workbook = new KnowledgeBaseSpreadsheetWorkbookData(
                MetaRows: ReadWorksheetRows(worksheets, "Meta", "Property", "Value"),
                LevelRows: ReadWorksheetRows(worksheets, "Levels", "LevelIndex", "LevelName"),
                WorkshopRows: ReadWorksheetRows(worksheets, "Workshops", "WorkshopOrder", "WorkshopName", "IsLastSelected"),
                NodeRows: ReadWorksheetRows(
                    worksheets,
                    "Nodes",
                    "NodeId",
                    "WorkshopName",
                    "ParentNodeId",
                    "SiblingOrder",
                    "LevelIndex",
                    "LevelName",
                    "NodeName",
                    "Path"));

            return _parser.ParseWorkbook(workbook);
        }

        private static Dictionary<string, XElement> GetWorksheets(XDocument document)
        {
            var worksheets = new Dictionary<string, XElement>(StringComparer.Ordinal);

            foreach (var worksheet in document.Descendants(SpreadsheetNamespace + "Worksheet"))
            {
                string name = worksheet.Attribute(SpreadsheetNamespace + "Name")?.Value?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(name))
                    throw new KnowledgeBaseExcelImportException("Обнаружен лист Excel без имени.");

                if (!worksheets.TryAdd(name, worksheet))
                    throw new KnowledgeBaseExcelImportException($"Лист '{name}' встречается более одного раза.");
            }

            if (worksheets.Count == 0)
                throw new KnowledgeBaseExcelImportException("Excel-файл не содержит листов SpreadsheetML.");

            return worksheets;
        }

        private static List<string[]> ReadWorksheetRows(
            IReadOnlyDictionary<string, XElement> worksheets,
            string worksheetName,
            params string[] expectedHeaders)
        {
            if (!worksheets.TryGetValue(worksheetName, out var worksheet))
                throw new KnowledgeBaseExcelImportException($"В Excel-файле отсутствует обязательный лист '{worksheetName}'.");

            var table = worksheet.Element(SpreadsheetNamespace + "Table");
            if (table == null)
                throw new KnowledgeBaseExcelImportException($"Лист '{worksheetName}' не содержит таблицу данных.");

            var rows = table
                .Elements(SpreadsheetNamespace + "Row")
                .Select(ReadRowValues)
                .Select(TrimTrailingEmptyValues)
                .ToList();

            if (rows.Count == 0)
                throw new KnowledgeBaseExcelImportException($"Лист '{worksheetName}' не содержит строк.");

            string[] header = NormalizeRow(rows[0], expectedHeaders.Length, worksheetName, rowNumber: 1);
            if (!header.SequenceEqual(expectedHeaders))
            {
                throw new KnowledgeBaseExcelImportException(
                    $"Лист '{worksheetName}' имеет неожиданные заголовки. Ожидалось: {string.Join(", ", expectedHeaders)}.");
            }

            var dataRows = new List<string[]>();
            for (int index = 1; index < rows.Count; index++)
            {
                var row = TrimTrailingEmptyValues(rows[index]);
                if (row.Length == 0)
                    continue;

                dataRows.Add(NormalizeRow(row, expectedHeaders.Length, worksheetName, index + 1));
            }

            return dataRows;
        }

        private static string[] ReadRowValues(XElement row)
        {
            var values = new List<string>();
            int currentIndex = 1;

            foreach (var cell in row.Elements(SpreadsheetNamespace + "Cell"))
            {
                string? indexText = cell.Attribute(SpreadsheetNamespace + "Index")?.Value;
                if (!string.IsNullOrWhiteSpace(indexText))
                {
                    int requestedIndex = ParseInt(indexText, "Cell.Index");
                    while (currentIndex < requestedIndex)
                    {
                        values.Add(string.Empty);
                        currentIndex++;
                    }
                }

                values.Add(cell.Element(SpreadsheetNamespace + "Data")?.Value?.Trim() ?? string.Empty);
                currentIndex++;
            }

            return values.ToArray();
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

        private static string[] NormalizeRow(string[] values, int expectedLength, string worksheetName, int rowNumber)
        {
            if (values.Length > expectedLength)
            {
                throw new KnowledgeBaseExcelImportException(
                    $"Лист '{worksheetName}', строка {rowNumber}: обнаружены лишние значения после ожидаемых колонок.");
            }

            if (values.Length == expectedLength)
                return values;

            var normalized = new string[expectedLength];
            Array.Copy(values, normalized, values.Length);
            for (int index = values.Length; index < expectedLength; index++)
                normalized[index] = string.Empty;

            return normalized;
        }

        private static int ParseInt(string value, string context)
        {
            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                throw new KnowledgeBaseExcelImportException($"Не удалось прочитать целое число '{value}' в '{context}'.");

            return parsed;
        }
    }

    internal sealed record KnowledgeBaseSpreadsheetWorkbookData(
        List<string[]> MetaRows,
        List<string[]> LevelRows,
        List<string[]> WorkshopRows,
        List<string[]> NodeRows);
}
