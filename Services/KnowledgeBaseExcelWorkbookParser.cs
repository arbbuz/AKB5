using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    internal sealed class KnowledgeBaseExcelWorkbookParser
    {
        public SavedData ParseWorkbook(KnowledgeBaseSpreadsheetWorkbookData workbook)
        {
            var meta = ParseMeta(workbook.MetaRows);
            var config = ParseConfig(workbook.LevelRows);
            var workshops = ParseWorkshops(workbook.WorkshopRows, meta.LastWorkshop);
            var roots = ParseNodes(config, workshops, workbook.NodeRows);

            return new SavedData
            {
                SchemaVersion = meta.SchemaVersion,
                Config = config,
                Workshops = roots,
                LastWorkshop = workshops.LastWorkshop
            };
        }

        private static ParsedMeta ParseMeta(IEnumerable<string[]> rows)
        {
            var values = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var row in rows)
            {
                string propertyName = RequireValue(row[0], "Meta", "Property");
                if (!values.TryAdd(propertyName, row[1].Trim()))
                    throw new KnowledgeBaseExcelImportException($"Лист 'Meta' содержит дублирующее свойство '{propertyName}'.");
            }

            string formatId = RequireMetaValue(values, "FormatId");
            if (!string.Equals(formatId, KnowledgeBaseExcelExchangeService.WorkbookFormatId, StringComparison.Ordinal))
            {
                throw new KnowledgeBaseExcelImportException(
                    $"Неподдерживаемый FormatId '{formatId}'. Ожидалось '{KnowledgeBaseExcelExchangeService.WorkbookFormatId}'.");
            }

            int formatVersion = ParseInt(RequireMetaValue(values, "FormatVersion"), "Meta.FormatVersion");
            if (formatVersion != KnowledgeBaseExcelExchangeService.WorkbookFormatVersion)
            {
                throw new KnowledgeBaseExcelImportException(
                    $"Неподдерживаемая версия Excel exchange: {formatVersion}. Ожидалось {KnowledgeBaseExcelExchangeService.WorkbookFormatVersion}.");
            }

            int schemaVersion = ParseInt(RequireMetaValue(values, "SchemaVersion"), "Meta.SchemaVersion");
            if (schemaVersion < 1)
                throw new KnowledgeBaseExcelImportException($"Неподдерживаемая SchemaVersion: {schemaVersion}.");

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
                throw new KnowledgeBaseExcelImportException("Лист 'Levels' не содержит уровней.");

            for (int index = 0; index < parsedLevels.Count; index++)
            {
                if (parsedLevels[index].LevelIndex != index)
                {
                    throw new KnowledgeBaseExcelImportException(
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
                throw new KnowledgeBaseExcelImportException("Лист 'Workshops' не содержит цехов.");

            var workshopNames = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);
            var workshopOrders = new HashSet<int>();
            foreach (var row in parsedRows)
            {
                if (!workshopOrders.Add(row.WorkshopOrder))
                {
                    throw new KnowledgeBaseExcelImportException(
                        $"Лист 'Workshops' содержит дублирующий WorkshopOrder '{row.WorkshopOrder}'.");
                }

                if (!workshopNames.Add(row.WorkshopName))
                {
                    throw new KnowledgeBaseExcelImportException(
                        $"Лист 'Workshops' содержит дублирующий цех '{row.WorkshopName}'.");
                }
            }

            var selectedRows = parsedRows.Where(row => row.IsLastSelected).ToList();
            if (selectedRows.Count > 1)
                throw new KnowledgeBaseExcelImportException("Лист 'Workshops' содержит более одного выбранного цеха.");

            string selectedFromRows = selectedRows.SingleOrDefault()?.WorkshopName ?? string.Empty;
            string lastWorkshop = !string.IsNullOrWhiteSpace(selectedFromRows)
                ? selectedFromRows
                : metaLastWorkshop;

            if (string.IsNullOrWhiteSpace(lastWorkshop) ||
                !parsedRows.Any(row => string.Equals(row.WorkshopName, lastWorkshop, StringComparison.Ordinal)))
            {
                lastWorkshop = parsedRows[0].WorkshopName;
            }

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
                    NodeName: RequireValue(row[6], "Nodes", "NodeName")))
                .ToList();

            parsedRows = RemapNodeWorkshopNames(workshops, parsedRows);

            var knownWorkshops = new HashSet<string>(workshops.OrderedWorkshopNames, StringComparer.Ordinal);
            var parsedById = new Dictionary<string, ParsedNodeRow>(StringComparer.Ordinal);

            foreach (var row in parsedRows)
            {
                if (!knownWorkshops.Contains(row.WorkshopName))
                    throw new KnowledgeBaseExcelImportException($"Узел '{row.NodeName}' ссылается на неизвестный цех '{row.WorkshopName}'.");

                if (row.LevelIndex >= config.MaxLevels)
                {
                    throw new KnowledgeBaseExcelImportException(
                        $"Узел '{row.NodeName}' имеет LevelIndex {row.LevelIndex}, выходящий за пределы конфигурации уровней.");
                }

                if (!parsedById.TryAdd(row.NodeId, row))
                    throw new KnowledgeBaseExcelImportException($"Лист 'Nodes' содержит дублирующий NodeId '{row.NodeId}'.");
            }

            foreach (var group in parsedRows.GroupBy(row => (row.WorkshopName, row.ParentNodeId)))
            {
                var siblingOrders = new HashSet<int>();
                foreach (var row in group)
                {
                    if (!siblingOrders.Add(row.SiblingOrder))
                    {
                        throw new KnowledgeBaseExcelImportException(
                            $"Для parent '{row.ParentNodeId}' в цехе '{row.WorkshopName}' обнаружен дублирующий SiblingOrder '{row.SiblingOrder}'.");
                    }
                }
            }

            foreach (var row in parsedRows.Where(row => !string.IsNullOrWhiteSpace(row.ParentNodeId)))
            {
                if (!parsedById.TryGetValue(row.ParentNodeId, out var parentRow))
                {
                    throw new KnowledgeBaseExcelImportException(
                        $"Узел '{row.NodeName}' ссылается на отсутствующий ParentNodeId '{row.ParentNodeId}'.");
                }

                if (!string.Equals(row.WorkshopName, parentRow.WorkshopName, StringComparison.Ordinal))
                {
                    throw new KnowledgeBaseExcelImportException(
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
                        throw new KnowledgeBaseExcelImportException(
                            $"Корневой узел '{rootRow.NodeName}' должен иметь LevelIndex 0.");
                    }

                    rootsByWorkshop[workshopName].Add(BuildNode(
                        rootRow,
                        workshopName,
                        childrenByParent,
                        visitedNodeIds,
                        new HashSet<string>(StringComparer.Ordinal)));
                }
            }

            if (visitedNodeIds.Count != parsedRows.Count)
            {
                var orphanNode = parsedRows.First(row => !visitedNodeIds.Contains(row.NodeId));
                throw new KnowledgeBaseExcelImportException(
                    $"Узел '{orphanNode.NodeName}' недостижим из корневых узлов и не может быть импортирован.");
            }

            return rootsByWorkshop;
        }

        private static List<ParsedNodeRow> RemapNodeWorkshopNames(
            ParsedWorkshops workshops,
            List<ParsedNodeRow> parsedRows)
        {
            var sourceWorkshopNames = parsedRows
                .Select(row => row.WorkshopName)
                .Distinct(StringComparer.Ordinal)
                .ToList();

            if (sourceWorkshopNames.Count == 0)
                return parsedRows;

            var targetWorkshopNames = workshops.OrderedWorkshopNames.ToList();
            var targetWorkshopSet = new HashSet<string>(targetWorkshopNames, StringComparer.Ordinal);
            if (sourceWorkshopNames.All(targetWorkshopSet.Contains))
                return parsedRows;

            var anchors = FindWorkshopAnchors(sourceWorkshopNames, targetWorkshopNames);
            var mapping = BuildWorkshopNameMapping(sourceWorkshopNames, targetWorkshopNames, anchors);

            return parsedRows
                .Select(row => row with { WorkshopName = mapping[row.WorkshopName] })
                .ToList();
        }

        private static IReadOnlyList<WorkshopAnchor> FindWorkshopAnchors(
            IReadOnlyList<string> sourceWorkshopNames,
            IReadOnlyList<string> targetWorkshopNames)
        {
            var lcsLengths = new int[sourceWorkshopNames.Count + 1, targetWorkshopNames.Count + 1];
            for (int sourceIndex = sourceWorkshopNames.Count - 1; sourceIndex >= 0; sourceIndex--)
            {
                for (int targetIndex = targetWorkshopNames.Count - 1; targetIndex >= 0; targetIndex--)
                {
                    lcsLengths[sourceIndex, targetIndex] = string.Equals(
                        sourceWorkshopNames[sourceIndex],
                        targetWorkshopNames[targetIndex],
                        StringComparison.Ordinal)
                        ? lcsLengths[sourceIndex + 1, targetIndex + 1] + 1
                        : Math.Max(
                            lcsLengths[sourceIndex + 1, targetIndex],
                            lcsLengths[sourceIndex, targetIndex + 1]);
                }
            }

            var anchors = new List<WorkshopAnchor>();
            int sourceCursor = 0;
            int targetCursor = 0;
            while (sourceCursor < sourceWorkshopNames.Count && targetCursor < targetWorkshopNames.Count)
            {
                if (string.Equals(sourceWorkshopNames[sourceCursor], targetWorkshopNames[targetCursor], StringComparison.Ordinal))
                {
                    anchors.Add(new WorkshopAnchor(sourceCursor, targetCursor));
                    sourceCursor++;
                    targetCursor++;
                    continue;
                }

                if (lcsLengths[sourceCursor + 1, targetCursor] >= lcsLengths[sourceCursor, targetCursor + 1])
                {
                    sourceCursor++;
                }
                else
                {
                    targetCursor++;
                }
            }

            return anchors;
        }

        private static Dictionary<string, string> BuildWorkshopNameMapping(
            IReadOnlyList<string> sourceWorkshopNames,
            IReadOnlyList<string> targetWorkshopNames,
            IReadOnlyList<WorkshopAnchor> anchors)
        {
            var mapping = new Dictionary<string, string>(StringComparer.Ordinal);
            int previousSourceIndex = -1;
            int previousTargetIndex = -1;

            for (int anchorIndex = 0; anchorIndex <= anchors.Count; anchorIndex++)
            {
                bool isSentinel = anchorIndex == anchors.Count;
                int sourceBoundary = isSentinel ? sourceWorkshopNames.Count : anchors[anchorIndex].SourceIndex;
                int targetBoundary = isSentinel ? targetWorkshopNames.Count : anchors[anchorIndex].TargetIndex;

                int sourceGapStart = previousSourceIndex + 1;
                int sourceGapCount = sourceBoundary - sourceGapStart;
                int targetGapStart = previousTargetIndex + 1;
                int targetGapCount = targetBoundary - targetGapStart;

                if (sourceGapCount > 0)
                {
                    if (sourceGapCount != targetGapCount)
                    {
                        throw CreateWorkshopRemapException(
                            sourceWorkshopNames.Skip(sourceGapStart).Take(sourceGapCount),
                            targetWorkshopNames.Skip(targetGapStart).Take(targetGapCount));
                    }

                    for (int gapIndex = 0; gapIndex < sourceGapCount; gapIndex++)
                    {
                        mapping[sourceWorkshopNames[sourceGapStart + gapIndex]] =
                            targetWorkshopNames[targetGapStart + gapIndex];
                    }
                }

                if (isSentinel)
                    continue;

                string workshopName = sourceWorkshopNames[anchors[anchorIndex].SourceIndex];
                mapping[workshopName] = targetWorkshopNames[anchors[anchorIndex].TargetIndex];
                previousSourceIndex = anchors[anchorIndex].SourceIndex;
                previousTargetIndex = anchors[anchorIndex].TargetIndex;
            }

            return mapping;
        }

        private static KnowledgeBaseExcelImportException CreateWorkshopRemapException(
            IEnumerable<string> sourceGap,
            IEnumerable<string> targetGap)
        {
            string sourceNames = FormatWorkshopNames(sourceGap);
            string targetNames = FormatWorkshopNames(targetGap);

            return new KnowledgeBaseExcelImportException(
                $"Не удалось однозначно сопоставить имена цехов между листами 'Nodes' и 'Workshops'. " +
                $"Сегмент Nodes: {sourceNames}. Сегмент Workshops: {targetNames}. " +
                "Проверьте переименования цехов и порядок строк на листе 'Workshops'.");
        }

        private static string FormatWorkshopNames(IEnumerable<string> workshopNames)
        {
            var names = workshopNames.ToList();
            if (names.Count == 0)
                return "(пусто)";

            return string.Join(", ", names.Select(name => $"'{name}'"));
        }

        private static KbNode BuildNode(
            ParsedNodeRow row,
            string workshopName,
            IReadOnlyDictionary<string, List<ParsedNodeRow>> childrenByParent,
            ISet<string> visitedNodeIds,
            ISet<string> recursionStack)
        {
            if (!recursionStack.Add(row.NodeId))
                throw new KnowledgeBaseExcelImportException($"Обнаружен цикл в иерархии узлов около NodeId '{row.NodeId}'.");

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
                        workshopName,
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
                throw new KnowledgeBaseExcelImportException($"Лист 'Meta' не содержит обязательного свойства '{propertyName}'.");

            return value.Trim();
        }

        private static string RequireValue(string value, string worksheetName, string columnName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new KnowledgeBaseExcelImportException($"Лист '{worksheetName}' содержит пустое значение в колонке '{columnName}'.");

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

        private sealed record ParsedMeta(int SchemaVersion, string LastWorkshop);

        private sealed record ParsedLevelRow(int LevelIndex, string LevelName);

        private sealed record ParsedWorkshopRow(int WorkshopOrder, string WorkshopName, bool IsLastSelected);

        private sealed record ParsedWorkshops(IReadOnlyList<string> OrderedWorkshopNames, string LastWorkshop);

        private sealed record WorkshopAnchor(int SourceIndex, int TargetIndex);

        private sealed record ParsedNodeRow(
            string NodeId,
            string WorkshopName,
            string ParentNodeId,
            int SiblingOrder,
            int LevelIndex,
            string NodeName);
    }

    internal sealed class KnowledgeBaseExcelImportException : Exception
    {
        public KnowledgeBaseExcelImportException(string message)
            : base(message)
        {
        }
    }
}
