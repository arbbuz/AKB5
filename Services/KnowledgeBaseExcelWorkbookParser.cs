using System.Globalization;
using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    internal sealed class KnowledgeBaseExcelWorkbookParser
    {
        private const string MetaSheetName = "Meta";
        private const string LevelsSheetName = "Levels";
        private const string WorkshopsSheetName = "Workshops";
        private const string WorkshopNodesSheetKind = "WorkshopNodes";
        private const int MaxWorksheetNameLength = 31;

        private static readonly HashSet<string> GlobalWorksheetNames = new(StringComparer.Ordinal)
        {
            MetaSheetName,
            LevelsSheetName,
            WorkshopsSheetName
        };

        private static readonly string[] PropertyValueHeaders = { "Property", "Value" };
        private static readonly string[] LevelHeaders = { "LevelIndex", "LevelName" };
        private static readonly string[] WorkshopHeaders = { "WorkshopOrder", "WorkshopId", "WorkshopName", "IsLastSelected", "NodesSheetKey" };
        private static readonly string[] RequiredNodeHeaders =
        {
            "NodeId",
            "ParentNodeId",
            "SiblingOrder",
            "LevelIndex",
            "LevelName",
            "NodeName",
            "Path"
        };

        private static readonly char[] InvalidWorksheetNameCharacters = { ':', '\\', '/', '?', '*', '[', ']' };

        public SavedData ParseWorkbook(KnowledgeBaseSpreadsheetWorkbookData workbook)
        {
            var meta = ParseMeta(ParseStrictTable(RequireWorksheet(workbook, MetaSheetName), PropertyValueHeaders));
            var config = ParseConfig(ParseStrictTable(RequireWorksheet(workbook, LevelsSheetName), LevelHeaders));
            var workshops = ParseWorkshops(meta, ParseStrictTable(RequireWorksheet(workbook, WorkshopsSheetName), WorkshopHeaders));

            ValidateStableSheetNameProjection(workshops.Rows);

            var nodeSheets = ParseWorkshopNodeSheets(workbook);
            var roots = BuildNodeTree(config, workshops, ParseNodes(nodeSheets, workshops));

            return new SavedData
            {
                SchemaVersion = meta.SchemaVersion,
                Config = config,
                Workshops = roots,
                LastWorkshop = workshops.LastWorkshop
            };
        }

        private static ParsedMeta ParseMeta(WorksheetTable table)
        {
            var values = ParseStrictPropertyRows(table);

            string formatId = RequireMetaValue(values, "FormatId");
            if (!string.Equals(formatId, KnowledgeBaseExcelExchangeService.WorkbookFormatId, StringComparison.Ordinal))
            {
                throw new KnowledgeBaseExcelImportException(
                    $"Неподдерживаемый FormatId '{formatId}'. Ожидалось '{KnowledgeBaseExcelExchangeService.WorkbookFormatId}'.");
            }

            int formatVersion = ParseInt(RequireMetaValue(values, "FormatVersion"), "Meta.FormatVersion");
            if (formatVersion != KnowledgeBaseExcelExchangeService.WorkbookFormatVersion)
            {
                if (formatVersion is 1 or 2)
                {
                    throw new KnowledgeBaseExcelImportException(
                        $"Excel exchange форматы v1/v2 больше не поддерживаются. " +
                        $"Ожидался WorkbookFormatVersion = {KnowledgeBaseExcelExchangeService.WorkbookFormatVersion}, получено {formatVersion}.");
                }

                throw new KnowledgeBaseExcelImportException(
                    $"Неподдерживаемая версия Excel exchange: {formatVersion}. " +
                    $"Ожидалась версия {KnowledgeBaseExcelExchangeService.WorkbookFormatVersion}.");
            }

            int schemaVersion = ParseInt(RequireMetaValue(values, "SchemaVersion"), "Meta.SchemaVersion");
            if (schemaVersion < 1)
                throw new KnowledgeBaseExcelImportException($"Неподдерживаемая SchemaVersion: {schemaVersion}.");

            string lastWorkshopId = values.TryGetValue("LastWorkshopId", out var storedLastWorkshopId)
                ? storedLastWorkshopId.Trim()
                : string.Empty;
            string lastWorkshop = values.TryGetValue("LastWorkshop", out var storedLastWorkshop)
                ? storedLastWorkshop.Trim()
                : string.Empty;

            return new ParsedMeta(schemaVersion, lastWorkshopId, lastWorkshop);
        }

        private static KbConfig ParseConfig(WorksheetTable table)
        {
            var levelNames = new List<string>();

            foreach (var row in table.Rows)
            {
                int expectedLevelIndex = levelNames.Count;
                int parsedLevelIndex = ParseInt(ReadRequiredCell(row, table, "LevelIndex"), "Levels.LevelIndex");
                if (parsedLevelIndex != expectedLevelIndex)
                {
                    throw new KnowledgeBaseExcelImportException(
                        $"Лист 'Levels', строка {row.RowNumber}: техническая колонка 'LevelIndex' должна быть равна {expectedLevelIndex}, получено {parsedLevelIndex}.");
                }

                levelNames.Add(ReadRequiredCell(row, table, "LevelName"));
            }

            if (levelNames.Count == 0)
                throw new KnowledgeBaseExcelImportException("Лист 'Levels' не содержит уровней.");

            return new KbConfig
            {
                MaxLevels = levelNames.Count,
                LevelNames = levelNames
            };
        }

        private static ParsedWorkshops ParseWorkshops(ParsedMeta meta, WorksheetTable table)
        {
            var rows = new List<ParsedWorkshopRow>();
            var workshopIds = new HashSet<string>(StringComparer.Ordinal);
            var workshopNames = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);
            var nodesSheetKeys = new HashSet<string>(StringComparer.Ordinal);

            foreach (var row in table.Rows)
            {
                int expectedOrder = rows.Count + 1;
                int parsedOrder = ParsePositiveInt(ReadRequiredCell(row, table, "WorkshopOrder"), "Workshops.WorkshopOrder");
                if (parsedOrder != expectedOrder)
                {
                    throw new KnowledgeBaseExcelImportException(
                        $"Лист 'Workshops', строка {row.RowNumber}: техническая колонка 'WorkshopOrder' должна быть равна {expectedOrder}, получено {parsedOrder}.");
                }

                string workshopId = ReadRequiredCell(row, table, "WorkshopId");
                string workshopName = ReadRequiredCell(row, table, "WorkshopName");
                bool isLastSelected = ParseBoolean(ReadRequiredCell(row, table, "IsLastSelected"), "Workshops.IsLastSelected");
                string nodesSheetKey = ReadRequiredCell(row, table, "NodesSheetKey");

                if (!workshopIds.Add(workshopId))
                    throw new KnowledgeBaseExcelImportException($"Лист 'Workshops' содержит дублирующий WorkshopId '{workshopId}'.");

                if (!workshopNames.Add(workshopName))
                    throw new KnowledgeBaseExcelImportException($"Лист 'Workshops' содержит дублирующий цех '{workshopName}'.");

                if (!nodesSheetKeys.Add(nodesSheetKey))
                    throw new KnowledgeBaseExcelImportException($"Лист 'Workshops' содержит дублирующий NodesSheetKey '{nodesSheetKey}'.");

                rows.Add(new ParsedWorkshopRow(
                    WorkshopOrder: parsedOrder,
                    WorkshopId: workshopId,
                    WorkshopName: workshopName,
                    IsLastSelected: isLastSelected,
                    NodesSheetKey: nodesSheetKey));
            }

            if (rows.Count == 0)
                throw new KnowledgeBaseExcelImportException("Лист 'Workshops' не содержит цехов.");

            var selectedRows = rows.Where(row => row.IsLastSelected).ToList();
            if (selectedRows.Count > 1)
                throw new KnowledgeBaseExcelImportException("Лист 'Workshops' содержит более одного выбранного цеха.");

            return new ParsedWorkshops(
                rows,
                ResolveLastWorkshop(rows, meta));
        }

        private static string ResolveLastWorkshop(IReadOnlyList<ParsedWorkshopRow> rows, ParsedMeta meta)
        {
            string selectedWorkshop = rows.SingleOrDefault(row => row.IsLastSelected)?.WorkshopName ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(selectedWorkshop))
                return selectedWorkshop;

            if (!string.IsNullOrWhiteSpace(meta.LastWorkshopId))
            {
                string? workshopById = rows
                    .SingleOrDefault(row => string.Equals(row.WorkshopId, meta.LastWorkshopId, StringComparison.Ordinal))
                    ?.WorkshopName;
                if (!string.IsNullOrWhiteSpace(workshopById))
                    return workshopById;
            }

            if (!string.IsNullOrWhiteSpace(meta.LastWorkshop))
            {
                string? workshopByName = rows
                    .SingleOrDefault(row => string.Equals(row.WorkshopName, meta.LastWorkshop, StringComparison.Ordinal))
                    ?.WorkshopName;
                if (!string.IsNullOrWhiteSpace(workshopByName))
                    return workshopByName;
            }

            return rows[0].WorkshopName;
        }

        private static IReadOnlyList<ParsedWorkshopNodeSheet> ParseWorkshopNodeSheets(
            KnowledgeBaseSpreadsheetWorkbookData workbook)
        {
            var nodeSheets = workbook.Worksheets
                .Where(sheet => !GlobalWorksheetNames.Contains(sheet.SheetName))
                .Select(TryParseWorkshopNodeSheet)
                .Where(sheet => sheet != null)
                .Cast<ParsedWorkshopNodeSheet>()
                .ToList();

            if (nodeSheets.Count == 0)
                throw new KnowledgeBaseExcelImportException("Формат Excel v3 не содержит листов узлов по цехам.");

            return nodeSheets;
        }

        private static IReadOnlyList<ParsedNodeRow> ParseNodes(
            IReadOnlyList<ParsedWorkshopNodeSheet> nodeSheets,
            ParsedWorkshops workshops)
        {
            var workshopRowsById = workshops.Rows.ToDictionary(row => row.WorkshopId, row => row, StringComparer.Ordinal);
            var nodeSheetsByKey = new Dictionary<string, ParsedWorkshopNodeSheet>(StringComparer.Ordinal);
            var nodeSheetsByWorkshopId = new Dictionary<string, ParsedWorkshopNodeSheet>(StringComparer.Ordinal);

            foreach (var nodeSheet in nodeSheets)
            {
                if (!workshopRowsById.ContainsKey(nodeSheet.WorkshopId))
                {
                    throw new KnowledgeBaseExcelImportException(
                        $"Лист узлов '{nodeSheet.SheetName}' ссылается на неизвестный WorkshopId '{nodeSheet.WorkshopId}'.");
                }

                if (!nodeSheetsByKey.TryAdd(nodeSheet.NodesSheetKey, nodeSheet))
                {
                    throw new KnowledgeBaseExcelImportException(
                        $"В книге обнаружено более одного листа узлов с NodesSheetKey '{nodeSheet.NodesSheetKey}'.");
                }

                if (!nodeSheetsByWorkshopId.TryAdd(nodeSheet.WorkshopId, nodeSheet))
                {
                    throw new KnowledgeBaseExcelImportException(
                        $"В книге обнаружено более одного листа узлов для WorkshopId '{nodeSheet.WorkshopId}'.");
                }
            }

            var parsedRows = new List<ParsedNodeRow>();
            var referencedSheetKeys = new HashSet<string>(StringComparer.Ordinal);

            foreach (var workshop in workshops.Rows)
            {
                if (!nodeSheetsByKey.TryGetValue(workshop.NodesSheetKey, out var nodeSheet))
                {
                    throw new KnowledgeBaseExcelImportException(
                        $"Для цеха '{workshop.WorkshopName}' отсутствует лист узлов с ключом '{workshop.NodesSheetKey}'.");
                }

                if (!string.Equals(nodeSheet.WorkshopId, workshop.WorkshopId, StringComparison.Ordinal))
                {
                    throw new KnowledgeBaseExcelImportException(
                        $"Лист узлов '{nodeSheet.SheetName}' не соответствует WorkshopId '{workshop.WorkshopId}' из листа 'Workshops'.");
                }

                referencedSheetKeys.Add(workshop.NodesSheetKey);
                parsedRows.AddRange(ParseNodeRows(nodeSheet.NodeTable, workshop));
            }

            ParsedWorkshopNodeSheet? danglingSheet = nodeSheets
                .FirstOrDefault(sheet => !referencedSheetKeys.Contains(sheet.NodesSheetKey));
            if (danglingSheet != null)
            {
                throw new KnowledgeBaseExcelImportException(
                    $"Лист узлов '{danglingSheet.SheetName}' не связан ни с одной строкой листа 'Workshops'.");
            }

            return parsedRows;
        }

        private static ParsedWorkshopNodeSheet? TryParseWorkshopNodeSheet(KnowledgeBaseSpreadsheetWorksheetData worksheet)
        {
            var rows = EnumerateMeaningfulRows(worksheet.Rows).ToList();
            if (rows.Count == 0)
                return null;

            Dictionary<string, int>? propertyHeaderMap = TryBuildHeaderMap(rows[0].Values);
            if (propertyHeaderMap == null)
            {
                if (rows[0].Values.Any(value => string.Equals(value.Trim(), "Property", StringComparison.Ordinal) ||
                                                string.Equals(value.Trim(), "Value", StringComparison.Ordinal)))
                {
                    throw new KnowledgeBaseExcelImportException(
                        $"Лист '{worksheet.SheetName}', строка {rows[0].RowNumber} содержит дублирующиеся заголовки колонок.");
                }

                return null;
            }

            if (!propertyHeaderMap.ContainsKey("Property") || !propertyHeaderMap.ContainsKey("Value"))
                return null;

            var metaRows = new List<WorksheetRow>();
            WorksheetTable? nodeTable = null;

            for (int index = 1; index < rows.Count; index++)
            {
                var row = rows[index];
                Dictionary<string, int>? nodeHeaderMap = TryBuildHeaderMap(row.Values);
                if (nodeHeaderMap != null && ContainsRequiredHeaders(nodeHeaderMap, RequiredNodeHeaders))
                {
                    nodeTable = new WorksheetTable(
                        worksheet.SheetName,
                        nodeHeaderMap,
                        rows.Skip(index + 1).ToList());
                    break;
                }

                metaRows.Add(row);
            }

            var metaCandidate = ParseLenientPropertyRows(worksheet.SheetName, propertyHeaderMap, metaRows);
            bool looksLikeAkb5NodeSheet =
                metaCandidate.Values.ContainsKey("SheetKind") ||
                metaCandidate.Values.ContainsKey("WorkshopId") ||
                metaCandidate.Values.ContainsKey("NodesSheetKey");

            if (!looksLikeAkb5NodeSheet)
                return null;

            if (metaCandidate.ErrorMessage != null)
                throw new KnowledgeBaseExcelImportException(metaCandidate.ErrorMessage);

            string sheetKind = RequireWorksheetMetaValue(worksheet.SheetName, metaCandidate.Values, "SheetKind");
            if (!string.Equals(sheetKind, WorkshopNodesSheetKind, StringComparison.Ordinal))
            {
                throw new KnowledgeBaseExcelImportException(
                    $"Лист '{worksheet.SheetName}' имеет неподдерживаемый SheetKind '{sheetKind}'.");
            }

            string workshopId = RequireWorksheetMetaValue(worksheet.SheetName, metaCandidate.Values, "WorkshopId");
            string nodesSheetKey = RequireWorksheetMetaValue(worksheet.SheetName, metaCandidate.Values, "NodesSheetKey");

            if (nodeTable == null)
            {
                throw new KnowledgeBaseExcelImportException(
                    $"Лист '{worksheet.SheetName}' не содержит табличную часть узлов с обязательными заголовками AKB5.");
            }

            EnsureRequiredHeaders(nodeTable.HeaderMap, RequiredNodeHeaders, worksheet.SheetName, rows[0].RowNumber);
            return new ParsedWorkshopNodeSheet(worksheet.SheetName, workshopId, nodesSheetKey, nodeTable);
        }

        private static IEnumerable<ParsedNodeRow> ParseNodeRows(WorksheetTable nodeTable, ParsedWorkshopRow workshop)
        {
            foreach (var row in nodeTable.Rows)
            {
                yield return new ParsedNodeRow(
                    NodeId: ReadRequiredCell(row, nodeTable, "NodeId"),
                    WorkshopId: workshop.WorkshopId,
                    WorkshopName: workshop.WorkshopName,
                    ParentNodeId: ReadOptionalCell(row, nodeTable, "ParentNodeId"),
                    SiblingOrder: ParsePositiveInt(ReadRequiredCell(row, nodeTable, "SiblingOrder"), "Nodes.SiblingOrder"),
                    LevelIndex: ParseNonNegativeInt(ReadRequiredCell(row, nodeTable, "LevelIndex"), "Nodes.LevelIndex"),
                    NodeName: ReadRequiredCell(row, nodeTable, "NodeName"),
                    Description: ReadOptionalCell(row, nodeTable, "Description"),
                    Location: ReadOptionalCell(row, nodeTable, "Location"),
                    PhotoPath: ReadOptionalCell(row, nodeTable, "PhotoPath"),
                    IpAddress: ReadOptionalCell(row, nodeTable, "IpAddress"),
                    SchemaLink: ReadOptionalCell(row, nodeTable, "SchemaLink"));

                _ = ReadOptionalCell(row, nodeTable, "LevelName");
                _ = ReadOptionalCell(row, nodeTable, "Path");
            }
        }

        private static Dictionary<string, List<KbNode>> BuildNodeTree(
            KbConfig config,
            ParsedWorkshops workshops,
            IReadOnlyList<ParsedNodeRow> rows)
        {
            var parsedById = new Dictionary<string, ParsedNodeRow>(StringComparer.Ordinal);
            var workshopRowsById = workshops.Rows.ToDictionary(row => row.WorkshopId, row => row, StringComparer.Ordinal);

            foreach (var row in rows)
            {
                if (!workshopRowsById.ContainsKey(row.WorkshopId))
                {
                    throw new KnowledgeBaseExcelImportException(
                        $"Узел '{row.NodeName}' ссылается на неизвестный WorkshopId '{row.WorkshopId}'.");
                }

                if (row.LevelIndex >= config.MaxLevels)
                {
                    throw new KnowledgeBaseExcelImportException(
                        $"Узел '{row.NodeName}' имеет LevelIndex {row.LevelIndex}, выходящий за пределы конфигурации уровней.");
                }

                if (!parsedById.TryAdd(row.NodeId, row))
                {
                    throw new KnowledgeBaseExcelImportException(
                        $"Листы узлов содержат дублирующий NodeId '{row.NodeId}'.");
                }
            }

            ValidateParentLinks(rows, parsedById);
            ValidateNoCycles(parsedById);
            ValidateSiblingOrders(rows, workshopRowsById);

            var childrenByParent = rows
                .Where(row => !string.IsNullOrWhiteSpace(row.ParentNodeId))
                .GroupBy(row => row.ParentNodeId, StringComparer.Ordinal)
                .ToDictionary(
                    group => group.Key,
                    group => group.OrderBy(row => row.SiblingOrder).ToList(),
                    StringComparer.Ordinal);

            var rootsByWorkshop = workshops.Rows.ToDictionary(
                workshop => workshop.WorkshopName,
                _ => new List<KbNode>(),
                StringComparer.Ordinal);

            var visitedNodeIds = new HashSet<string>(StringComparer.Ordinal);

            foreach (var workshop in workshops.Rows)
            {
                var rootRows = rows
                    .Where(row => string.Equals(row.WorkshopId, workshop.WorkshopId, StringComparison.Ordinal) &&
                                  string.IsNullOrWhiteSpace(row.ParentNodeId))
                    .OrderBy(row => row.SiblingOrder)
                    .ToList();

                foreach (var rootRow in rootRows)
                {
                    if (rootRow.LevelIndex != 0)
                    {
                        throw new KnowledgeBaseExcelImportException(
                            $"Корневой узел '{rootRow.NodeName}' должен иметь LevelIndex 0.");
                    }

                    rootsByWorkshop[workshop.WorkshopName].Add(BuildNode(
                        rootRow,
                        workshop,
                        childrenByParent,
                        visitedNodeIds,
                        new HashSet<string>(StringComparer.Ordinal)));
                }
            }

            if (visitedNodeIds.Count != rows.Count)
            {
                ParsedNodeRow orphanNode = rows.First(row => !visitedNodeIds.Contains(row.NodeId));
                throw new KnowledgeBaseExcelImportException(
                    $"Узел '{orphanNode.NodeName}' недостижим из корневых узлов и не может быть импортирован.");
            }

            return rootsByWorkshop;
        }

        private static void ValidateParentLinks(
            IEnumerable<ParsedNodeRow> rows,
            IReadOnlyDictionary<string, ParsedNodeRow> parsedById)
        {
            foreach (var row in rows.Where(row => !string.IsNullOrWhiteSpace(row.ParentNodeId)))
            {
                if (!parsedById.TryGetValue(row.ParentNodeId, out var parentRow))
                {
                    throw new KnowledgeBaseExcelImportException(
                        $"Узел '{row.NodeName}' ссылается на отсутствующий ParentNodeId '{row.ParentNodeId}'.");
                }

                if (!string.Equals(row.WorkshopId, parentRow.WorkshopId, StringComparison.Ordinal))
                {
                    throw new KnowledgeBaseExcelImportException(
                        $"Узел '{row.NodeName}' не может ссылаться на родителя из другого цеха.");
                }
            }
        }

        private static void ValidateNoCycles(IReadOnlyDictionary<string, ParsedNodeRow> parsedById)
        {
            var states = new Dictionary<string, NodeVisitState>(StringComparer.Ordinal);

            foreach (var row in parsedById.Values)
            {
                VisitNode(row, parsedById, states);
            }
        }

        private static void VisitNode(
            ParsedNodeRow row,
            IReadOnlyDictionary<string, ParsedNodeRow> parsedById,
            IDictionary<string, NodeVisitState> states)
        {
            if (states.TryGetValue(row.NodeId, out var existingState))
            {
                if (existingState == NodeVisitState.Visited)
                    return;

                if (existingState == NodeVisitState.Visiting)
                {
                    throw new KnowledgeBaseExcelImportException(
                        $"Обнаружен цикл в иерархии узлов около NodeId '{row.NodeId}' ('{row.NodeName}').");
                }
            }

            states[row.NodeId] = NodeVisitState.Visiting;

            if (!string.IsNullOrWhiteSpace(row.ParentNodeId) &&
                parsedById.TryGetValue(row.ParentNodeId, out var parentRow))
            {
                VisitNode(parentRow, parsedById, states);
            }

            states[row.NodeId] = NodeVisitState.Visited;
        }

        private static void ValidateSiblingOrders(
            IEnumerable<ParsedNodeRow> rows,
            IReadOnlyDictionary<string, ParsedWorkshopRow> workshopsById)
        {
            foreach (var group in rows.GroupBy(row => (row.WorkshopId, row.ParentNodeId)))
            {
                var siblingOrders = new HashSet<int>();
                foreach (var row in group)
                {
                    if (!siblingOrders.Add(row.SiblingOrder))
                    {
                        string workshopName = workshopsById.TryGetValue(row.WorkshopId, out var workshop)
                            ? workshop.WorkshopName
                            : row.WorkshopId;
                        throw new KnowledgeBaseExcelImportException(
                            $"Для parent '{row.ParentNodeId}' в цехе '{workshopName}' обнаружен дублирующий SiblingOrder '{row.SiblingOrder}'.");
                    }
                }
            }
        }

        private static KbNode BuildNode(
            ParsedNodeRow row,
            ParsedWorkshopRow workshop,
            IReadOnlyDictionary<string, List<ParsedNodeRow>> childrenByParent,
            ISet<string> visitedNodeIds,
            ISet<string> recursionStack)
        {
            if (!recursionStack.Add(row.NodeId))
            {
                throw new KnowledgeBaseExcelImportException(
                    $"Обнаружен цикл в иерархии узлов около NodeId '{row.NodeId}' ('{row.NodeName}').");
            }

            var node = new KbNode
            {
                Name = row.NodeName,
                LevelIndex = row.LevelIndex,
                Details = new KbNodeDetails
                {
                    Description = row.Description,
                    Location = row.Location,
                    PhotoPath = row.PhotoPath,
                    IpAddress = row.LevelIndex >= 2 ? row.IpAddress : string.Empty,
                    SchemaLink = row.LevelIndex >= 2 ? row.SchemaLink : string.Empty
                }
            };

            if (childrenByParent.TryGetValue(row.NodeId, out var children))
            {
                foreach (var childRow in children)
                {
                    if (!string.Equals(childRow.WorkshopId, workshop.WorkshopId, StringComparison.Ordinal))
                    {
                        throw new KnowledgeBaseExcelImportException(
                            $"Узел '{childRow.NodeName}' не может находиться в другом цехе, чем его родитель.");
                    }

                    if (childRow.LevelIndex != row.LevelIndex + 1)
                    {
                        throw new KnowledgeBaseExcelImportException(
                            $"Узел '{childRow.NodeName}' должен иметь LevelIndex {row.LevelIndex + 1}, но найден {childRow.LevelIndex}.");
                    }

                    node.Children.Add(BuildNode(
                        childRow,
                        workshop,
                        childrenByParent,
                        visitedNodeIds,
                        recursionStack));
                }
            }

            visitedNodeIds.Add(row.NodeId);
            recursionStack.Remove(row.NodeId);
            return node;
        }

        private static void ValidateStableSheetNameProjection(IReadOnlyList<ParsedWorkshopRow> workshops)
        {
            var projectedNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var workshop in workshops)
            {
                string projectedSheetName = BuildProjectedWorkshopSheetName(workshop.WorkshopName);
                if (!projectedNames.TryAdd(projectedSheetName, workshop.WorkshopName))
                {
                    throw new KnowledgeBaseExcelImportException(
                        $"Названия цехов '{projectedNames[projectedSheetName]}' и '{workshop.WorkshopName}' " +
                        $"после sanitization/truncation дают один и тот же tab name '{projectedSheetName}'. " +
                        "Импорт остановлен во избежание опасной неоднозначности.");
                }
            }
        }

        private static string BuildProjectedWorkshopSheetName(string workshopName)
        {
            string baseName = SanitizeWorksheetName($"Узлы - {workshopName}");
            return string.IsNullOrWhiteSpace(baseName) ? "Узлы" : baseName;
        }

        private static string SanitizeWorksheetName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var chars = value
                .Select(symbol =>
                {
                    if (char.IsControl(symbol) || InvalidWorksheetNameCharacters.Contains(symbol))
                        return ' ';

                    return symbol;
                })
                .ToArray();

            string normalized = string.Join(
                " ",
                new string(chars)
                    .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                .Trim();

            normalized = normalized.Trim('\'');
            if (string.IsNullOrWhiteSpace(normalized))
                return string.Empty;

            return normalized.Length <= MaxWorksheetNameLength
                ? normalized
                : normalized[..MaxWorksheetNameLength].TrimEnd();
        }

        private static KnowledgeBaseSpreadsheetWorksheetData RequireWorksheet(
            KnowledgeBaseSpreadsheetWorkbookData workbook,
            string sheetName)
        {
            var matches = workbook.Worksheets
                .Where(sheet => string.Equals(sheet.SheetName, sheetName, StringComparison.Ordinal))
                .ToList();

            return matches.Count switch
            {
                1 => matches[0],
                0 => throw new KnowledgeBaseExcelImportException($"В Excel-файле отсутствует обязательный лист '{sheetName}'."),
                _ => throw new KnowledgeBaseExcelImportException($"Лист '{sheetName}' встречается более одного раза.")
            };
        }

        private static WorksheetTable ParseStrictTable(
            KnowledgeBaseSpreadsheetWorksheetData worksheet,
            IReadOnlyCollection<string> requiredHeaders)
        {
            var rows = EnumerateMeaningfulRows(worksheet.Rows).ToList();
            if (rows.Count == 0)
                throw new KnowledgeBaseExcelImportException($"Лист '{worksheet.SheetName}' не содержит строк.");

            Dictionary<string, int> headerMap = BuildHeaderMap(rows[0].Values, worksheet.SheetName, rows[0].RowNumber);
            EnsureRequiredHeaders(headerMap, requiredHeaders, worksheet.SheetName, rows[0].RowNumber);

            return new WorksheetTable(
                worksheet.SheetName,
                headerMap,
                rows.Skip(1).ToList());
        }

        private static IEnumerable<WorksheetRow> EnumerateMeaningfulRows(IReadOnlyList<string[]> rows)
        {
            for (int index = 0; index < rows.Count; index++)
            {
                string[] row = rows[index];
                if (row.Length == 0)
                    continue;

                if (row.All(string.IsNullOrWhiteSpace))
                    continue;

                yield return new WorksheetRow(index + 1, row);
            }
        }

        private static Dictionary<string, string> ParseStrictPropertyRows(WorksheetTable table)
        {
            var values = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var row in table.Rows)
            {
                string propertyName = ReadRequiredCell(row, table, "Property");
                string propertyValue = ReadRequiredCell(row, table, "Value");

                if (!values.TryAdd(propertyName, propertyValue))
                {
                    throw new KnowledgeBaseExcelImportException(
                        $"Лист '{table.SheetName}' содержит дублирующее свойство '{propertyName}'.");
                }
            }

            return values;
        }

        private static ParsedPropertyRows ParseLenientPropertyRows(
            string sheetName,
            IReadOnlyDictionary<string, int> headerMap,
            IEnumerable<WorksheetRow> rows)
        {
            var values = new Dictionary<string, string>(StringComparer.Ordinal);
            string? errorMessage = null;

            foreach (var row in rows)
            {
                string propertyName = ReadCell(row, headerMap, "Property");
                string propertyValue = ReadCell(row, headerMap, "Value");

                if (string.IsNullOrWhiteSpace(propertyName) && string.IsNullOrWhiteSpace(propertyValue))
                    continue;

                if (string.IsNullOrWhiteSpace(propertyName))
                {
                    errorMessage ??=
                        $"Лист '{sheetName}', строка {row.RowNumber}: обнаружено значение в колонке 'Value' без имени свойства.";
                    continue;
                }

                if (!values.TryAdd(propertyName, propertyValue) && errorMessage == null)
                {
                    errorMessage =
                        $"Лист '{sheetName}' содержит дублирующее свойство '{propertyName}'.";
                }
            }

            return new ParsedPropertyRows(values, errorMessage);
        }

        private static Dictionary<string, int> BuildHeaderMap(
            string[] values,
            string sheetName,
            int rowNumber)
        {
            Dictionary<string, int>? headerMap = TryBuildHeaderMap(values);
            if (headerMap == null)
            {
                throw new KnowledgeBaseExcelImportException(
                    $"Лист '{sheetName}', строка {rowNumber} содержит дублирующиеся заголовки колонок.");
            }

            return headerMap;
        }

        private static Dictionary<string, int>? TryBuildHeaderMap(string[] values)
        {
            var headerMap = new Dictionary<string, int>(StringComparer.Ordinal);

            for (int index = 0; index < values.Length; index++)
            {
                string headerName = values[index].Trim();
                if (string.IsNullOrWhiteSpace(headerName))
                    continue;

                if (!headerMap.TryAdd(headerName, index))
                    return null;
            }

            return headerMap;
        }

        private static bool ContainsRequiredHeaders(
            IReadOnlyDictionary<string, int> headerMap,
            IEnumerable<string> requiredHeaders) =>
            requiredHeaders.All(headerMap.ContainsKey);

        private static void EnsureRequiredHeaders(
            IReadOnlyDictionary<string, int> headerMap,
            IEnumerable<string> requiredHeaders,
            string sheetName,
            int rowNumber)
        {
            foreach (string requiredHeader in requiredHeaders)
            {
                if (!headerMap.ContainsKey(requiredHeader))
                {
                    throw new KnowledgeBaseExcelImportException(
                        $"Лист '{sheetName}', строка {rowNumber} не содержит обязательную колонку '{requiredHeader}'.");
                }
            }
        }

        private static string ReadRequiredCell(
            WorksheetRow row,
            WorksheetTable table,
            string headerName)
        {
            string value = ReadCell(row, table.HeaderMap, headerName);
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new KnowledgeBaseExcelImportException(
                    $"Лист '{table.SheetName}', строка {row.RowNumber} содержит пустое значение в колонке '{headerName}'.");
            }

            return value.Trim();
        }

        private static string ReadOptionalCell(
            WorksheetRow row,
            WorksheetTable table,
            string headerName) =>
            ReadCell(row, table.HeaderMap, headerName);

        private static string ReadCell(
            WorksheetRow row,
            IReadOnlyDictionary<string, int> headerMap,
            string headerName)
        {
            if (!headerMap.TryGetValue(headerName, out int columnIndex))
                return string.Empty;

            if (columnIndex < 0 || columnIndex >= row.Values.Length)
                return string.Empty;

            return row.Values[columnIndex].Trim();
        }

        private static string RequireMetaValue(IReadOnlyDictionary<string, string> values, string propertyName)
        {
            if (!values.TryGetValue(propertyName, out var value) || string.IsNullOrWhiteSpace(value))
                throw new KnowledgeBaseExcelImportException($"Лист 'Meta' не содержит обязательного свойства '{propertyName}'.");

            return value.Trim();
        }

        private static string RequireWorksheetMetaValue(
            string sheetName,
            IReadOnlyDictionary<string, string> values,
            string propertyName)
        {
            if (!values.TryGetValue(propertyName, out var value) || string.IsNullOrWhiteSpace(value))
            {
                throw new KnowledgeBaseExcelImportException(
                    $"Лист '{sheetName}' не содержит обязательного свойства '{propertyName}'.");
            }

            return value.Trim();
        }

        private static int ParseInt(string value, string context)
        {
            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                throw new KnowledgeBaseExcelImportException($"Не удалось прочитать целое число '{value}' в '{context}'.");

            return parsed;
        }

        private static int ParsePositiveInt(string value, string context)
        {
            int parsed = ParseInt(value, context);
            if (parsed <= 0)
                throw new KnowledgeBaseExcelImportException($"Значение '{context}' должно быть положительным, получено '{parsed}'.");

            return parsed;
        }

        private static int ParseNonNegativeInt(string value, string context)
        {
            int parsed = ParseInt(value, context);
            if (parsed < 0)
                throw new KnowledgeBaseExcelImportException($"Значение '{context}' не может быть отрицательным, получено '{parsed}'.");

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
                _ => throw new KnowledgeBaseExcelImportException(
                    $"Значение '{context}' должно быть TRUE/FALSE или 1/0, получено '{value}'.")
            };
        }

        private sealed record ParsedMeta(int SchemaVersion, string LastWorkshopId, string LastWorkshop);

        private sealed record ParsedWorkshopRow(
            int WorkshopOrder,
            string WorkshopId,
            string WorkshopName,
            bool IsLastSelected,
            string NodesSheetKey);

        private sealed record ParsedWorkshops(
            IReadOnlyList<ParsedWorkshopRow> Rows,
            string LastWorkshop);

        private sealed record ParsedWorkshopNodeSheet(
            string SheetName,
            string WorkshopId,
            string NodesSheetKey,
            WorksheetTable NodeTable);

        private sealed record ParsedPropertyRows(
            IReadOnlyDictionary<string, string> Values,
            string? ErrorMessage);

        private sealed record ParsedNodeRow(
            string NodeId,
            string WorkshopId,
            string WorkshopName,
            string ParentNodeId,
            int SiblingOrder,
            int LevelIndex,
            string NodeName,
            string Description,
            string Location,
            string PhotoPath,
            string IpAddress,
            string SchemaLink);

        private sealed record WorksheetTable(
            string SheetName,
            IReadOnlyDictionary<string, int> HeaderMap,
            IReadOnlyList<WorksheetRow> Rows);

        private sealed record WorksheetRow(int RowNumber, string[] Values);

        private enum NodeVisitState
        {
            Visiting,
            Visited
        }
    }

    internal sealed class KnowledgeBaseExcelImportException : Exception
    {
        public KnowledgeBaseExcelImportException(string message)
            : base(message)
        {
        }
    }
}
