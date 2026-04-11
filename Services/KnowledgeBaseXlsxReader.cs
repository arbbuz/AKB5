using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    internal sealed class KnowledgeBaseXlsxReader
    {
        private static readonly XNamespace SpreadsheetNamespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        private static readonly XNamespace OfficeDocumentRelationshipsNamespace = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        private static readonly XNamespace PackageRelationshipsNamespace = "http://schemas.openxmlformats.org/package/2006/relationships";

        private readonly KnowledgeBaseExcelWorkbookParser _parser = new();

        public SavedData ParseWorkbookPackage(byte[] packageBytes)
        {
            using var stream = new MemoryStream(packageBytes, writable: false);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);

            var workbookEntry = archive.GetEntry("xl/workbook.xml")
                ?? throw new KnowledgeBaseExcelImportException("Файл XLSX не содержит 'xl/workbook.xml'.");
            var workbookRelationshipsEntry = archive.GetEntry("xl/_rels/workbook.xml.rels")
                ?? throw new KnowledgeBaseExcelImportException("Файл XLSX не содержит 'xl/_rels/workbook.xml.rels'.");

            var workbookDocument = LoadDocument(workbookEntry);
            var workbookRelationships = ReadWorkbookRelationships(LoadDocument(workbookRelationshipsEntry));
            var sharedStrings = ReadSharedStrings(archive);
            var worksheets = ReadWorksheets(archive, workbookDocument, workbookRelationships, sharedStrings);

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

        private static Dictionary<string, string> ReadWorkbookRelationships(XDocument relationshipsDocument)
        {
            var relationships = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var relationship in relationshipsDocument.Root?.Elements(PackageRelationshipsNamespace + "Relationship")
                         ?? Enumerable.Empty<XElement>())
            {
                string id = relationship.Attribute("Id")?.Value?.Trim() ?? string.Empty;
                string target = relationship.Attribute("Target")?.Value?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(target))
                    continue;

                relationships[id] = NormalizePartPath("xl/workbook.xml", target);
            }

            return relationships;
        }

        private static IReadOnlyList<string> ReadSharedStrings(ZipArchive archive)
        {
            var entry = archive.GetEntry("xl/sharedStrings.xml");
            if (entry == null)
                return Array.Empty<string>();

            var document = LoadDocument(entry);
            return document
                .Descendants(SpreadsheetNamespace + "si")
                .Select(ReadSharedStringItem)
                .ToList();
        }

        private static string ReadSharedStringItem(XElement item)
        {
            var runs = item.Elements(SpreadsheetNamespace + "r").ToList();
            if (runs.Count > 0)
            {
                return string.Concat(runs
                    .Select(run => run.Element(SpreadsheetNamespace + "t")?.Value ?? string.Empty))
                    .Trim();
            }

            return (item.Element(SpreadsheetNamespace + "t")?.Value ?? string.Empty).Trim();
        }

        private static Dictionary<string, XElement> ReadWorksheets(
            ZipArchive archive,
            XDocument workbookDocument,
            IReadOnlyDictionary<string, string> workbookRelationships,
            IReadOnlyList<string> sharedStrings)
        {
            var worksheets = new Dictionary<string, XElement>(StringComparer.Ordinal);

            foreach (var sheet in workbookDocument.Descendants(SpreadsheetNamespace + "sheet"))
            {
                string name = sheet.Attribute("name")?.Value?.Trim() ?? string.Empty;
                string relationshipId = sheet.Attribute(OfficeDocumentRelationshipsNamespace + "id")?.Value?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(name))
                    throw new KnowledgeBaseExcelImportException("Обнаружен лист XLSX без имени.");

                if (string.IsNullOrWhiteSpace(relationshipId) || !workbookRelationships.TryGetValue(relationshipId, out var partPath))
                {
                    throw new KnowledgeBaseExcelImportException(
                        $"Для листа '{name}' отсутствует связь workbook relationship '{relationshipId}'.");
                }

                var entry = archive.GetEntry(partPath)
                    ?? throw new KnowledgeBaseExcelImportException(
                        $"Лист '{name}' ссылается на отсутствующую часть '{partPath}'.");

                var worksheetDocument = LoadDocument(entry);
                var worksheetRoot = worksheetDocument.Root
                    ?? throw new KnowledgeBaseExcelImportException($"Лист '{name}' не содержит XML-корень.");

                if (!worksheets.TryAdd(name, worksheetRoot))
                    throw new KnowledgeBaseExcelImportException($"Лист '{name}' встречается более одного раза.");
            }

            if (worksheets.Count == 0)
                throw new KnowledgeBaseExcelImportException("Файл XLSX не содержит листов workbook.");

            return worksheets.ToDictionary(
                pair => pair.Key,
                pair => MaterializeWorksheetRows(pair.Value, sharedStrings),
                StringComparer.Ordinal);
        }

        private static XElement MaterializeWorksheetRows(XElement worksheet, IReadOnlyList<string> sharedStrings)
        {
            var sheetData = worksheet.Element(SpreadsheetNamespace + "sheetData")
                ?? throw new KnowledgeBaseExcelImportException("Лист XLSX не содержит 'sheetData'.");

            return new XElement(
                SpreadsheetNamespace + "worksheet",
                new XElement(
                    SpreadsheetNamespace + "sheetData",
                    sheetData.Elements(SpreadsheetNamespace + "row").Select(row =>
                        new XElement(
                            SpreadsheetNamespace + "row",
                            ReadRowValues(row, sharedStrings).Select(value =>
                                new XElement(
                                    SpreadsheetNamespace + "c",
                                    new XElement(SpreadsheetNamespace + "v", value)))))));
        }

        private static List<string[]> ReadWorksheetRows(
            IReadOnlyDictionary<string, XElement> worksheets,
            string worksheetName,
            params string[] expectedHeaders)
        {
            if (!worksheets.TryGetValue(worksheetName, out var worksheet))
                throw new KnowledgeBaseExcelImportException($"В Excel-файле отсутствует обязательный лист '{worksheetName}'.");

            var rows = worksheet
                .Descendants(SpreadsheetNamespace + "row")
                .Select(row => row
                    .Elements(SpreadsheetNamespace + "c")
                    .Select(cell => cell.Element(SpreadsheetNamespace + "v")?.Value ?? string.Empty)
                    .ToArray())
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

        private static string[] ReadRowValues(XElement row, IReadOnlyList<string> sharedStrings)
        {
            var values = new List<string>();
            int currentIndex = 1;

            foreach (var cell in row.Elements(SpreadsheetNamespace + "c"))
            {
                int requestedIndex = currentIndex;
                string? reference = cell.Attribute("r")?.Value;
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

            return values.ToArray();
        }

        private static string ReadCellValue(XElement cell, IReadOnlyList<string> sharedStrings)
        {
            string cellType = cell.Attribute("t")?.Value?.Trim() ?? string.Empty;

            return cellType switch
            {
                "inlineStr" => string.Concat(
                    cell.Descendants(SpreadsheetNamespace + "t")
                        .Select(text => text.Value))
                    .Trim(),
                "s" => ReadSharedString(cell, sharedStrings),
                "b" => ReadBoolean(cell),
                _ => (cell.Element(SpreadsheetNamespace + "v")?.Value ?? string.Empty).Trim()
            };
        }

        private static string ReadSharedString(XElement cell, IReadOnlyList<string> sharedStrings)
        {
            string rawIndex = cell.Element(SpreadsheetNamespace + "v")?.Value?.Trim() ?? string.Empty;
            if (!int.TryParse(rawIndex, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index))
            {
                throw new KnowledgeBaseExcelImportException(
                    $"Не удалось прочитать индекс shared string '{rawIndex}'.");
            }

            if (index < 0 || index >= sharedStrings.Count)
                throw new KnowledgeBaseExcelImportException($"Shared string index '{index}' выходит за границы таблицы строк.");

            return sharedStrings[index];
        }

        private static string ReadBoolean(XElement cell)
        {
            string rawValue = cell.Element(SpreadsheetNamespace + "v")?.Value?.Trim() ?? string.Empty;
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

        private static string NormalizePartPath(string basePartPath, string target)
        {
            string baseDirectory = Path.GetDirectoryName(basePartPath)?.Replace('\\', '/') ?? string.Empty;
            string combined = string.IsNullOrWhiteSpace(baseDirectory)
                ? target
                : $"{baseDirectory}/{target}";

            var segments = new Stack<string>();
            foreach (var segment in combined.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries))
            {
                if (segment == ".")
                    continue;

                if (segment == "..")
                {
                    if (segments.Count > 0)
                        segments.Pop();

                    continue;
                }

                segments.Push(segment);
            }

            return string.Join("/", segments.Reverse());
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

        private static XDocument LoadDocument(ZipArchiveEntry entry)
        {
            using var stream = entry.Open();
            return XDocument.Load(stream);
        }
    }
}
