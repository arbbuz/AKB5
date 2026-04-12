using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using AsutpKnowledgeBase.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace AsutpKnowledgeBase.Services
{
    internal sealed class KnowledgeBaseXlsxReader
    {
        private readonly KnowledgeBaseExcelWorkbookParser _parser = new();

        public SavedData ParseWorkbookPackage(byte[] packageBytes)
        {
            using var stream = new MemoryStream(packageBytes, writable: false);
            using var document = SpreadsheetDocument.Open(stream, false);

            var workbookPart = document.WorkbookPart
                ?? throw new KnowledgeBaseExcelImportException("Файл XLSX не содержит workbook part.");
            var sheets = workbookPart.Workbook.Sheets?.Elements<Sheet>().ToList()
                ?? new List<Sheet>();
            if (sheets.Count == 0)
                throw new KnowledgeBaseExcelImportException("Файл XLSX не содержит листов workbook.");

            var sharedStrings = ReadSharedStrings(workbookPart.SharedStringTablePart);
            var worksheets = new List<KnowledgeBaseSpreadsheetWorksheetData>();

            foreach (var sheet in sheets)
            {
                string sheetName = sheet.Name?.Value?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(sheetName))
                    throw new KnowledgeBaseExcelImportException("Обнаружен лист XLSX без имени.");

                string relationshipId = sheet.Id?.Value?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(relationshipId))
                    throw new KnowledgeBaseExcelImportException($"Для листа '{sheetName}' отсутствует relationship id.");

                if (workbookPart.GetPartById(relationshipId) is not WorksheetPart worksheetPart)
                {
                    throw new KnowledgeBaseExcelImportException(
                        $"Для листа '{sheetName}' отсутствует связанная worksheet part.");
                }

                worksheets.Add(new KnowledgeBaseSpreadsheetWorksheetData(
                    sheetName,
                    ReadWorksheetRows(worksheetPart, sharedStrings)));
            }

            return _parser.ParseWorkbook(new KnowledgeBaseSpreadsheetWorkbookData(worksheets));
        }

        private static IReadOnlyList<string> ReadSharedStrings(SharedStringTablePart? part)
        {
            if (part?.SharedStringTable == null)
                return Array.Empty<string>();

            return part.SharedStringTable
                .Elements<SharedStringItem>()
                .Select(ReadSharedStringItem)
                .ToList();
        }

        private static string ReadSharedStringItem(SharedStringItem item) =>
            string.Concat(item.Descendants<Text>().Select(text => text.Text)).Trim();

        private static List<string[]> ReadWorksheetRows(
            WorksheetPart worksheetPart,
            IReadOnlyList<string> sharedStrings)
        {
            var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>()
                ?? throw new KnowledgeBaseExcelImportException("Лист XLSX не содержит 'sheetData'.");

            var rows = new List<string[]>();
            foreach (var row in sheetData.Elements<Row>())
            {
                rows.Add(TrimTrailingEmptyValues(ReadRowValues(row, sharedStrings).ToArray()));
            }

            return rows;
        }

        private static List<string> ReadRowValues(Row row, IReadOnlyList<string> sharedStrings)
        {
            var values = new List<string>();
            int currentIndex = 1;

            foreach (var cell in row.Elements<Cell>())
            {
                int requestedIndex = currentIndex;
                string? reference = cell.CellReference?.Value;
                if (!string.IsNullOrWhiteSpace(reference))
                    requestedIndex = GetColumnIndex(reference);

                while (currentIndex < requestedIndex)
                {
                    values.Add(string.Empty);
                    currentIndex++;
                }

                values.Add(ReadCellValue(cell, sharedStrings));
                currentIndex++;
            }

            return values;
        }

        private static string ReadCellValue(Cell cell, IReadOnlyList<string> sharedStrings)
        {
            if (cell.DataType?.Value == CellValues.SharedString)
                return ReadSharedString(cell, sharedStrings);

            if (cell.DataType?.Value == CellValues.Boolean)
                return ReadBoolean(cell);

            if (cell.DataType?.Value == CellValues.InlineString)
            {
                return string.Concat(
                        (cell.InlineString?.Descendants<Text>() ?? Enumerable.Empty<Text>())
                            .Select(text => text.Text))
                    .Trim();
            }

            return (cell.CellValue?.InnerText ?? string.Empty).Trim();
        }

        private static string ReadSharedString(Cell cell, IReadOnlyList<string> sharedStrings)
        {
            string rawIndex = cell.CellValue?.InnerText?.Trim() ?? string.Empty;
            if (!int.TryParse(rawIndex, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index))
            {
                throw new KnowledgeBaseExcelImportException(
                    $"Не удалось прочитать индекс shared string '{rawIndex}'.");
            }

            if (index < 0 || index >= sharedStrings.Count)
                throw new KnowledgeBaseExcelImportException($"Shared string index '{index}' выходит за границы таблицы строк.");

            return sharedStrings[index];
        }

        private static string ReadBoolean(Cell cell)
        {
            string rawValue = cell.CellValue?.InnerText?.Trim() ?? string.Empty;
            return rawValue switch
            {
                "1" => "TRUE",
                "0" => "FALSE",
                _ => rawValue
            };
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

            if (index <= 0)
                throw new KnowledgeBaseExcelImportException($"Некорректная ссылка на ячейку '{cellReference}'.");

            return index;
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
    }
}
