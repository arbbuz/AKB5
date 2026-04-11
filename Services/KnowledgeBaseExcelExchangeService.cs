using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
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

    /// <summary>
    /// Экспортирует и импортирует базу знаний в Excel-compatible SpreadsheetML 2003 workbook
    /// без внешних зависимостей. Контракт книги фиксирован:
    /// листы Meta, Levels, Workshops и Nodes.
    /// </summary>
    public class KnowledgeBaseExcelExchangeService
    {
        public const string WorkbookFormatId = "AKB5.ExcelExchange";
        public const int WorkbookFormatVersion = 1;

        private static readonly XNamespace SpreadsheetNamespace = "urn:schemas-microsoft-com:office:spreadsheet";

        public string BuildWorkbookXml(SavedData data)
        {
            var normalizedConfig = KnowledgeBaseDataService.NormalizeConfig(data.Config);
            var normalizedWorkshops = KnowledgeBaseDataService.NormalizeWorkshops(data.Workshops);
            string lastWorkshop = KnowledgeBaseDataService.ResolveWorkshop(normalizedWorkshops, data.LastWorkshop);

            XNamespace ns = SpreadsheetNamespace;
            XNamespace officeNs = "urn:schemas-microsoft-com:office:office";
            XNamespace excelNs = "urn:schemas-microsoft-com:office:excel";
            XNamespace htmlNs = "http://www.w3.org/TR/REC-html40";

            var workbook = new XElement(
                ns + "Workbook",
                new XAttribute(XNamespace.Xmlns + "o", officeNs),
                new XAttribute(XNamespace.Xmlns + "x", excelNs),
                new XAttribute(XNamespace.Xmlns + "ss", ns),
                new XAttribute(XNamespace.Xmlns + "html", htmlNs),
                BuildStyles(ns),
                CreateWorksheet(
                    ns,
                    "Meta",
                    new[] { "Property", "Value" },
                    BuildMetaRows(data.SchemaVersion, lastWorkshop)),
                CreateWorksheet(
                    ns,
                    "Levels",
                    new[] { "LevelIndex", "LevelName" },
                    BuildLevelRows(normalizedConfig)),
                CreateWorksheet(
                    ns,
                    "Workshops",
                    new[] { "WorkshopOrder", "WorkshopName", "IsLastSelected" },
                    BuildWorkshopRows(normalizedWorkshops, lastWorkshop)),
                CreateWorksheet(
                    ns,
                    "Nodes",
                    new[]
                    {
                        "NodeId",
                        "WorkshopName",
                        "ParentNodeId",
                        "SiblingOrder",
                        "LevelIndex",
                        "LevelName",
                        "NodeName",
                        "Path"
                    },
                    BuildNodeRows(normalizedConfig, normalizedWorkshops)));

            var document = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XProcessingInstruction("mso-application", "progid=\"Excel.Sheet\""),
                workbook);

            using var writer = new Utf8StringWriter();
            document.Save(writer);
            return writer.ToString();
        }

        public KnowledgeBaseExcelExportResult Export(SavedData data, string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return new KnowledgeBaseExcelExportResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Не указан путь для экспорта."
                };
            }

            try
            {
                string xml = BuildWorkbookXml(data);
                string? directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(directory))
                    Directory.CreateDirectory(directory);

                File.WriteAllText(path, xml, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                return new KnowledgeBaseExcelExportResult { IsSuccess = true };
            }
            catch (Exception ex)
            {
                return new KnowledgeBaseExcelExportResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public KnowledgeBaseExcelImportResult Import(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return new KnowledgeBaseExcelImportResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Не указан путь для импорта."
                };
            }

            if (!File.Exists(path))
            {
                return new KnowledgeBaseExcelImportResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Файл '{path}' не найден."
                };
            }

            try
            {
                return ImportFromXml(File.ReadAllText(path));
            }
            catch (Exception ex)
            {
                return new KnowledgeBaseExcelImportResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Ошибка чтения Excel-файла: {ex.Message}"
                };
            }
        }

        public KnowledgeBaseExcelImportResult ImportFromXml(string xml)
        {
            if (string.IsNullOrWhiteSpace(xml))
            {
                return new KnowledgeBaseExcelImportResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Файл импорта пустой."
                };
            }

            try
            {
                var document = XDocument.Parse(xml);
                var data = ParseWorkbook(document);

                return new KnowledgeBaseExcelImportResult
                {
                    IsSuccess = true,
                    Data = data
                };
            }
            catch (ExcelImportException ex)
            {
                return new KnowledgeBaseExcelImportResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
            catch (Exception ex)
            {
                return new KnowledgeBaseExcelImportResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Ошибка разбора Excel-файла: {ex.Message}"
                };
            }
        }

        private static IEnumerable<IReadOnlyList<string>> BuildMetaRows(int schemaVersion, string lastWorkshop)
        {
            yield return new[] { "FormatId", WorkbookFormatId };
            yield return new[] { "FormatVersion", WorkbookFormatVersion.ToString(CultureInfo.InvariantCulture) };
            yield return new[] { "SchemaVersion", schemaVersion.ToString(CultureInfo.InvariantCulture) };
            yield return new[] { "LastWorkshop", lastWorkshop };
        }

        private static IEnumerable<IReadOnlyList<string>> BuildLevelRows(KbConfig config)
        {
            for (int index = 0; index < config.LevelNames.Count; index++)
            {
                yield return new[]
                {
                    index.ToString(CultureInfo.InvariantCulture),
                    config.LevelNames[index]
                };
            }
        }

        private static IEnumerable<IReadOnlyList<string>> BuildWorkshopRows(
            IReadOnlyDictionary<string, List<KbNode>> workshops,
            string lastWorkshop)
        {
            int order = 1;
            foreach (var workshop in workshops.Keys)
            {
                yield return new[]
                {
                    order.ToString(CultureInfo.InvariantCulture),
                    workshop,
                    string.Equals(workshop, lastWorkshop, StringComparison.Ordinal) ? "TRUE" : "FALSE"
                };
                order++;
            }
        }

        private static IEnumerable<IReadOnlyList<string>> BuildNodeRows(
            KbConfig config,
            IReadOnlyDictionary<string, List<KbNode>> workshops)
        {
            var rows = new List<IReadOnlyList<string>>();
            int nextNodeId = 1;

            foreach (var workshop in workshops)
            {
                for (int index = 0; index < workshop.Value.Count; index++)
                {
                    FlattenNode(
                        rows,
                        config,
                        workshop.Key,
                        workshop.Value[index],
                        parentNodeId: string.Empty,
                        siblingOrder: index + 1,
                        currentPath: workshop.Value[index].Name,
                        nextNodeId: ref nextNodeId);
                }
            }

            return rows;
        }

        private static void FlattenNode(
            ICollection<IReadOnlyList<string>> rows,
            KbConfig config,
            string workshopName,
            KbNode node,
            string parentNodeId,
            int siblingOrder,
            string currentPath,
            ref int nextNodeId)
        {
            string nodeId = nextNodeId.ToString(CultureInfo.InvariantCulture);
            nextNodeId++;

            rows.Add(new[]
            {
                nodeId,
                workshopName,
                parentNodeId,
                siblingOrder.ToString(CultureInfo.InvariantCulture),
                node.LevelIndex.ToString(CultureInfo.InvariantCulture),
                GetLevelName(config, node.LevelIndex),
                node.Name,
                $"{workshopName} / {currentPath}"
            });

            for (int childIndex = 0; childIndex < node.Children.Count; childIndex++)
            {
                var child = node.Children[childIndex];
                FlattenNode(
                    rows,
                    config,
                    workshopName,
                    child,
                    nodeId,
                    childIndex + 1,
                    $"{currentPath} / {child.Name}",
                    ref nextNodeId);
            }
        }

        private static SavedData ParseWorkbook(XDocument document)
        {
            var worksheets = GetWorksheets(document);
            var metaRows = ReadWorksheetRows(worksheets, "Meta", "Property", "Value");
            var levelRows = ReadWorksheetRows(worksheets, "Levels", "LevelIndex", "LevelName");
            var workshopRows = ReadWorksheetRows(worksheets, "Workshops", "WorkshopOrder", "WorkshopName", "IsLastSelected");
            var nodeRows = ReadWorksheetRows(
                worksheets,
                "Nodes",
                "NodeId",
                "WorkshopName",
                "ParentNodeId",
                "SiblingOrder",
                "LevelIndex",
                "LevelName",
                "NodeName",
                "Path");

            var meta = ParseMeta(metaRows);
            var config = ParseConfig(levelRows);
            var workshops = ParseWorkshops(workshopRows, meta.LastWorkshop);
            var roots = ParseNodes(config, workshops, nodeRows);

            return new SavedData
            {
                SchemaVersion = meta.SchemaVersion,
                Config = config,
                Workshops = roots,
                LastWorkshop = workshops.LastWorkshop
            };
        }

        private static Dictionary<string, XElement> GetWorksheets(XDocument document)
        {
            var worksheets = new Dictionary<string, XElement>(StringComparer.Ordinal);

            foreach (var worksheet in document.Descendants(SpreadsheetNamespace + "Worksheet"))
            {
                string name = worksheet.Attribute(SpreadsheetNamespace + "Name")?.Value?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(name))
                    throw new ExcelImportException("Обнаружен лист Excel без имени.");

                if (!worksheets.TryAdd(name, worksheet))
                    throw new ExcelImportException($"Лист '{name}' встречается более одного раза.");
            }

            if (worksheets.Count == 0)
                throw new ExcelImportException("Excel-файл не содержит листов SpreadsheetML.");

            return worksheets;
        }

        private static List<string[]> ReadWorksheetRows(
            IReadOnlyDictionary<string, XElement> worksheets,
            string worksheetName,
            params string[] expectedHeaders)
        {
            if (!worksheets.TryGetValue(worksheetName, out var worksheet))
                throw new ExcelImportException($"В Excel-файле отсутствует обязательный лист '{worksheetName}'.");

            var table = worksheet.Element(SpreadsheetNamespace + "Table");
            if (table == null)
                throw new ExcelImportException($"Лист '{worksheetName}' не содержит таблицу данных.");

            var rows = table
                .Elements(SpreadsheetNamespace + "Row")
                .Select(ReadRowValues)
                .Select(TrimTrailingEmptyValues)
                .ToList();

            if (rows.Count == 0)
                throw new ExcelImportException($"Лист '{worksheetName}' не содержит строк.");

            string[] header = NormalizeRow(rows[0], expectedHeaders.Length, worksheetName, rowNumber: 1);
            if (!header.SequenceEqual(expectedHeaders))
            {
                throw new ExcelImportException(
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
                throw new ExcelImportException(
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

        private static ParsedMeta ParseMeta(IEnumerable<string[]> rows)
        {
            var values = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var row in rows)
            {
                string propertyName = RequireValue(row[0], "Meta", "Property");
                if (!values.TryAdd(propertyName, row[1].Trim()))
                    throw new ExcelImportException($"Лист 'Meta' содержит дублирующее свойство '{propertyName}'.");
            }

            string formatId = RequireMetaValue(values, "FormatId");
            if (!string.Equals(formatId, WorkbookFormatId, StringComparison.Ordinal))
            {
                throw new ExcelImportException(
                    $"Неподдерживаемый FormatId '{formatId}'. Ожидалось '{WorkbookFormatId}'.");
            }

            int formatVersion = ParseInt(RequireMetaValue(values, "FormatVersion"), "Meta.FormatVersion");
            if (formatVersion != WorkbookFormatVersion)
            {
                throw new ExcelImportException(
                    $"Неподдерживаемая версия Excel exchange: {formatVersion}. Ожидалось {WorkbookFormatVersion}.");
            }

            int schemaVersion = ParseInt(RequireMetaValue(values, "SchemaVersion"), "Meta.SchemaVersion");
            if (schemaVersion < 1)
                throw new ExcelImportException($"Неподдерживаемая SchemaVersion: {schemaVersion}.");

            string lastWorkshop = values.TryGetValue("LastWorkshop", out var storedLastWorkshop)
                ? storedLastWorkshop.Trim()
                : string.Empty;

            return new ParsedMeta(schemaVersion, lastWorkshop);
        }

        private static KbConfig ParseConfig(IEnumerable<string[]> rows)
        {
            var parsedLevels = rows
                .Select(row => new ParsedLevelRow(
                    ParseInt(row[0], "Levels.LevelIndex"),
                    RequireValue(row[1], "Levels", "LevelName")))
                .OrderBy(row => row.LevelIndex)
                .ToList();

            if (parsedLevels.Count == 0)
                throw new ExcelImportException("Лист 'Levels' не содержит уровней.");

            for (int index = 0; index < parsedLevels.Count; index++)
            {
                if (parsedLevels[index].LevelIndex != index)
                {
                    throw new ExcelImportException(
                        $"Лист 'Levels' должен содержать последовательные LevelIndex от 0. Ошибка на индексе {parsedLevels[index].LevelIndex}.");
                }
            }

            return new KbConfig
            {
                MaxLevels = parsedLevels.Count,
                LevelNames = parsedLevels.Select(level => level.LevelName).ToList()
            };
        }

        private static ParsedWorkshops ParseWorkshops(IEnumerable<string[]> rows, string metaLastWorkshop)
        {
            var parsedRows = rows
                .Select(row => new ParsedWorkshopRow(
                    ParsePositiveInt(row[0], "Workshops.WorkshopOrder"),
                    RequireValue(row[1], "Workshops", "WorkshopName"),
                    ParseBoolean(row[2], "Workshops.IsLastSelected")))
                .OrderBy(row => row.WorkshopOrder)
                .ToList();

            if (parsedRows.Count == 0)
                throw new ExcelImportException("Лист 'Workshops' не содержит цехов.");

            var workshopNames = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);
            var workshopOrders = new HashSet<int>();
            foreach (var row in parsedRows)
            {
                if (!workshopOrders.Add(row.WorkshopOrder))
                {
                    throw new ExcelImportException(
                        $"Лист 'Workshops' содержит дублирующий WorkshopOrder '{row.WorkshopOrder}'.");
                }

                if (!workshopNames.Add(row.WorkshopName))
                {
                    throw new ExcelImportException(
                        $"Лист 'Workshops' содержит дублирующий цех '{row.WorkshopName}'.");
                }
            }

            var selectedRows = parsedRows.Where(row => row.IsLastSelected).ToList();
            if (selectedRows.Count > 1)
                throw new ExcelImportException("Лист 'Workshops' содержит более одного выбранного цеха.");

            string selectedFromRows = selectedRows.SingleOrDefault()?.WorkshopName ?? string.Empty;
            string lastWorkshop = metaLastWorkshop;

            if (!string.IsNullOrWhiteSpace(metaLastWorkshop) &&
                !string.IsNullOrWhiteSpace(selectedFromRows) &&
                !string.Equals(metaLastWorkshop, selectedFromRows, StringComparison.Ordinal))
            {
                throw new ExcelImportException(
                    $"Meta.LastWorkshop ('{metaLastWorkshop}') не совпадает с выбранным цехом на листе 'Workshops' ('{selectedFromRows}').");
            }

            if (string.IsNullOrWhiteSpace(lastWorkshop))
                lastWorkshop = selectedFromRows;

            if (string.IsNullOrWhiteSpace(lastWorkshop))
                lastWorkshop = parsedRows[0].WorkshopName;

            if (!parsedRows.Any(row => string.Equals(row.WorkshopName, lastWorkshop, StringComparison.Ordinal)))
                throw new ExcelImportException($"Выбранный цех '{lastWorkshop}' отсутствует на листе 'Workshops'.");

            return new ParsedWorkshops(parsedRows.Select(row => row.WorkshopName).ToList(), lastWorkshop);
        }

        private static Dictionary<string, List<KbNode>> ParseNodes(
            KbConfig config,
            ParsedWorkshops workshops,
            IEnumerable<string[]> rows)
        {
            var parsedRows = rows
                .Select(row => new ParsedNodeRow(
                    NodeId: RequireValue(row[0], "Nodes", "NodeId"),
                    WorkshopName: RequireValue(row[1], "Nodes", "WorkshopName"),
                    ParentNodeId: row[2].Trim(),
                    SiblingOrder: ParsePositiveInt(row[3], "Nodes.SiblingOrder"),
                    LevelIndex: ParseNonNegativeInt(row[4], "Nodes.LevelIndex"),
                    LevelName: RequireValue(row[5], "Nodes", "LevelName"),
                    NodeName: RequireValue(row[6], "Nodes", "NodeName"),
                    Path: RequireValue(row[7], "Nodes", "Path")))
                .ToList();

            var knownWorkshops = new HashSet<string>(workshops.OrderedWorkshopNames, StringComparer.Ordinal);
            var parsedById = new Dictionary<string, ParsedNodeRow>(StringComparer.Ordinal);

            foreach (var row in parsedRows)
            {
                if (!knownWorkshops.Contains(row.WorkshopName))
                    throw new ExcelImportException($"Узел '{row.NodeName}' ссылается на неизвестный цех '{row.WorkshopName}'.");

                if (row.LevelIndex >= config.MaxLevels)
                {
                    throw new ExcelImportException(
                        $"Узел '{row.NodeName}' имеет LevelIndex {row.LevelIndex}, выходящий за пределы конфигурации уровней.");
                }

                string expectedLevelName = GetLevelName(config, row.LevelIndex);
                if (!string.Equals(row.LevelName, expectedLevelName, StringComparison.Ordinal))
                {
                    throw new ExcelImportException(
                        $"Узел '{row.NodeName}' имеет LevelName '{row.LevelName}', но ожидалось '{expectedLevelName}'.");
                }

                if (!parsedById.TryAdd(row.NodeId, row))
                    throw new ExcelImportException($"Лист 'Nodes' содержит дублирующий NodeId '{row.NodeId}'.");
            }

            foreach (var group in parsedRows.GroupBy(row => (row.WorkshopName, row.ParentNodeId)))
            {
                var siblingOrders = new HashSet<int>();
                foreach (var row in group)
                {
                    if (!siblingOrders.Add(row.SiblingOrder))
                    {
                        throw new ExcelImportException(
                            $"Для parent '{row.ParentNodeId}' в цехе '{row.WorkshopName}' обнаружен дублирующий SiblingOrder '{row.SiblingOrder}'.");
                    }
                }
            }

            foreach (var row in parsedRows.Where(row => !string.IsNullOrWhiteSpace(row.ParentNodeId)))
            {
                if (!parsedById.TryGetValue(row.ParentNodeId, out var parentRow))
                {
                    throw new ExcelImportException(
                        $"Узел '{row.NodeName}' ссылается на отсутствующий ParentNodeId '{row.ParentNodeId}'.");
                }

                if (!string.Equals(row.WorkshopName, parentRow.WorkshopName, StringComparison.Ordinal))
                {
                    throw new ExcelImportException(
                        $"Узел '{row.NodeName}' не может ссылаться на родителя из другого цеха.");
                }
            }

            var childrenByParent = parsedRows
                .Where(row => !string.IsNullOrWhiteSpace(row.ParentNodeId))
                .GroupBy(row => row.ParentNodeId, StringComparer.Ordinal)
                .ToDictionary(
                    group => group.Key,
                    group => group.OrderBy(row => row.SiblingOrder).ToList(),
                    StringComparer.Ordinal);

            var rootsByWorkshop = workshops.OrderedWorkshopNames.ToDictionary(
                workshopName => workshopName,
                _ => new List<KbNode>(),
                StringComparer.Ordinal);

            var visitedNodeIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (string workshopName in workshops.OrderedWorkshopNames)
            {
                var rootRows = parsedRows
                    .Where(row => string.Equals(row.WorkshopName, workshopName, StringComparison.Ordinal) &&
                                  string.IsNullOrWhiteSpace(row.ParentNodeId))
                    .OrderBy(row => row.SiblingOrder)
                    .ToList();

                foreach (var rootRow in rootRows)
                {
                    if (rootRow.LevelIndex != 0)
                    {
                        throw new ExcelImportException(
                            $"Корневой узел '{rootRow.NodeName}' должен иметь LevelIndex 0.");
                    }

                    rootsByWorkshop[workshopName].Add(BuildNode(
                        rootRow,
                        workshopName,
                        $"{workshopName} / {rootRow.NodeName}",
                        childrenByParent,
                        visitedNodeIds,
                        new HashSet<string>(StringComparer.Ordinal)));
                }
            }

            if (visitedNodeIds.Count != parsedRows.Count)
            {
                var orphanNode = parsedRows.First(row => !visitedNodeIds.Contains(row.NodeId));
                throw new ExcelImportException(
                    $"Узел '{orphanNode.NodeName}' недостижим из корневых узлов и не может быть импортирован.");
            }

            return rootsByWorkshop;
        }

        private static KbNode BuildNode(
            ParsedNodeRow row,
            string workshopName,
            string expectedPath,
            IReadOnlyDictionary<string, List<ParsedNodeRow>> childrenByParent,
            ISet<string> visitedNodeIds,
            ISet<string> recursionStack)
        {
            if (!recursionStack.Add(row.NodeId))
                throw new ExcelImportException($"Обнаружен цикл в иерархии узлов около NodeId '{row.NodeId}'.");

            if (!string.Equals(row.Path, expectedPath, StringComparison.Ordinal))
            {
                throw new ExcelImportException(
                    $"Узел '{row.NodeName}' имеет некорректный Path '{row.Path}'. Ожидалось '{expectedPath}'.");
            }

            var node = new KbNode
            {
                Name = row.NodeName,
                LevelIndex = row.LevelIndex
            };

            if (childrenByParent.TryGetValue(row.NodeId, out var children))
            {
                foreach (var childRow in children)
                {
                    if (!string.Equals(childRow.WorkshopName, workshopName, StringComparison.Ordinal))
                    {
                        throw new ExcelImportException(
                            $"Узел '{childRow.NodeName}' не может находиться в другом цехе, чем его родитель.");
                    }

                    if (childRow.LevelIndex != row.LevelIndex + 1)
                    {
                        throw new ExcelImportException(
                            $"Узел '{childRow.NodeName}' должен иметь LevelIndex {row.LevelIndex + 1}, но найден {childRow.LevelIndex}.");
                    }

                    node.Children.Add(BuildNode(
                        childRow,
                        workshopName,
                        $"{expectedPath} / {childRow.NodeName}",
                        childrenByParent,
                        visitedNodeIds,
                        recursionStack));
                }
            }

            visitedNodeIds.Add(row.NodeId);
            recursionStack.Remove(row.NodeId);
            return node;
        }

        private static string RequireMetaValue(IReadOnlyDictionary<string, string> values, string propertyName)
        {
            if (!values.TryGetValue(propertyName, out var value) || string.IsNullOrWhiteSpace(value))
                throw new ExcelImportException($"Лист 'Meta' не содержит обязательного свойства '{propertyName}'.");

            return value.Trim();
        }

        private static string RequireValue(string value, string worksheetName, string columnName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ExcelImportException($"Лист '{worksheetName}' содержит пустое значение в колонке '{columnName}'.");

            return value.Trim();
        }

        private static int ParseInt(string value, string context)
        {
            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                throw new ExcelImportException($"Не удалось прочитать целое число '{value}' в '{context}'.");

            return parsed;
        }

        private static int ParsePositiveInt(string value, string context)
        {
            int parsed = ParseInt(value, context);
            if (parsed <= 0)
                throw new ExcelImportException($"Значение '{context}' должно быть положительным, получено '{parsed}'.");

            return parsed;
        }

        private static int ParseNonNegativeInt(string value, string context)
        {
            int parsed = ParseInt(value, context);
            if (parsed < 0)
                throw new ExcelImportException($"Значение '{context}' не может быть отрицательным, получено '{parsed}'.");

            return parsed;
        }

        private static bool ParseBoolean(string value, string context)
        {
            string normalized = value.Trim();
            return normalized.ToUpperInvariant() switch
            {
                "TRUE" => true,
                "FALSE" => false,
                "1" => true,
                "0" => false,
                _ => throw new ExcelImportException(
                    $"Значение '{context}' должно быть TRUE/FALSE или 1/0, получено '{value}'.")
            };
        }

        private static string GetLevelName(KbConfig config, int levelIndex) =>
            config.LevelNames.Count > levelIndex
                ? config.LevelNames[levelIndex]
                : $"Ур. {levelIndex + 1}";

        private static XElement BuildStyles(XNamespace ns) =>
            new(
                ns + "Styles",
                new XElement(
                    ns + "Style",
                    new XAttribute(ns + "ID", "Default"),
                    new XAttribute(ns + "Name", "Normal")),
                new XElement(
                    ns + "Style",
                    new XAttribute(ns + "ID", "Header"),
                    new XElement(
                        ns + "Font",
                        new XAttribute(ns + "Bold", "1"))));

        private static XElement CreateWorksheet(
            XNamespace ns,
            string worksheetName,
            IReadOnlyList<string> headers,
            IEnumerable<IReadOnlyList<string>> rows)
        {
            return new XElement(
                ns + "Worksheet",
                new XAttribute(ns + "Name", worksheetName),
                new XElement(
                    ns + "Table",
                    CreateRow(ns, headers, isHeader: true),
                    rows.Select(row => CreateRow(ns, row, isHeader: false))));
        }

        private static XElement CreateRow(XNamespace ns, IEnumerable<string> values, bool isHeader)
        {
            return new XElement(
                ns + "Row",
                values.Select(value =>
                {
                    var cell = new XElement(
                        ns + "Cell",
                        new XElement(
                            ns + "Data",
                            new XAttribute(ns + "Type", "String"),
                            value));

                    if (isHeader)
                        cell.Add(new XAttribute(ns + "StyleID", "Header"));

                    return cell;
                }));
        }

        private sealed class Utf8StringWriter : StringWriter
        {
            public override Encoding Encoding => new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        }

        private sealed class ExcelImportException : Exception
        {
            public ExcelImportException(string message)
                : base(message)
            {
            }
        }

        private sealed record ParsedMeta(int SchemaVersion, string LastWorkshop);

        private sealed record ParsedLevelRow(int LevelIndex, string LevelName);

        private sealed record ParsedWorkshopRow(int WorkshopOrder, string WorkshopName, bool IsLastSelected);

        private sealed record ParsedWorkshops(IReadOnlyList<string> OrderedWorkshopNames, string LastWorkshop);

        private sealed record ParsedNodeRow(
            string NodeId,
            string WorkshopName,
            string ParentNodeId,
            int SiblingOrder,
            int LevelIndex,
            string LevelName,
            string NodeName,
            string Path);
    }
}
