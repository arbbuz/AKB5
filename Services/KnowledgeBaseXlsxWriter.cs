using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using AsutpKnowledgeBase.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace AsutpKnowledgeBase.Services
{
    internal sealed class KnowledgeBaseXlsxWriter
    {
        private const string MetaSheetName = "Meta";
        private const string LevelsSheetName = "Levels";
        private const string WorkshopsSheetName = "Workshops";
        private const string WorkshopNodesSheetKind = "WorkshopNodes";
        private const int MaxWorksheetNameLength = 31;

        private static readonly char[] InvalidWorksheetNameCharacters = { ':', '\\', '/', '?', '*', '[', ']' };

        public byte[] BuildWorkbookPackage(SavedData data)
        {
            var normalizedConfig = KnowledgeBaseDataService.NormalizeConfig(data.Config);
            var normalizedWorkshops = KnowledgeBaseDataService.NormalizeWorkshops(data.Workshops);
            string lastWorkshop = KnowledgeBaseDataService.ResolveWorkshop(normalizedWorkshops, data.LastWorkshop);
            var workshopExports = BuildWorkshopExports(normalizedWorkshops, lastWorkshop);
            string lastWorkshopId = workshopExports
                .Single(workshop => string.Equals(workshop.WorkshopName, lastWorkshop, StringComparison.Ordinal))
                .WorkshopId;

            int nextNodeId = 1;
            var worksheets = new List<WorksheetDefinition>
            {
                new(
                    MetaSheetName,
                    BuildTabularRows(
                        headers: new[] { "Property", "Value" },
                        rows: BuildMetaRows(data.SchemaVersion, lastWorkshop, lastWorkshopId))),
                new(
                    LevelsSheetName,
                    BuildTabularRows(
                        headers: new[] { "LevelIndex", "LevelName" },
                        rows: BuildLevelRows(normalizedConfig))),
                new(
                    WorkshopsSheetName,
                    BuildTabularRows(
                        headers: new[] { "WorkshopOrder", "WorkshopId", "WorkshopName", "IsLastSelected", "NodesSheetKey" },
                        rows: BuildWorkshopRows(workshopExports)))
            };

            foreach (var workshop in workshopExports)
            {
                worksheets.Add(new WorksheetDefinition(
                    workshop.SheetTabName,
                    BuildWorkshopNodesSheetRows(normalizedConfig, workshop, ref nextNodeId)));
            }

            using var stream = new MemoryStream();
            using (var document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook, autoSave: true))
            {
                var workbookPart = document.AddWorkbookPart();
                workbookPart.Workbook = new Workbook();
                var sheets = workbookPart.Workbook.AppendChild(new Sheets());
                uint sheetId = 1;

                foreach (var worksheetDefinition in worksheets)
                {
                    var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                    worksheetPart.Worksheet = BuildWorksheet(worksheetDefinition.Rows);
                    worksheetPart.Worksheet.Save();

                    sheets.Append(new Sheet
                    {
                        Id = workbookPart.GetIdOfPart(worksheetPart),
                        SheetId = sheetId,
                        Name = worksheetDefinition.Name
                    });

                    sheetId++;
                }

                workbookPart.Workbook.Save();
            }

            return stream.ToArray();
        }

        private static IReadOnlyList<WorkshopExportRow> BuildWorkshopExports(
            IReadOnlyDictionary<string, List<KbNode>> workshops,
            string lastWorkshop)
        {
            var rows = new List<WorkshopExportRow>();
            var usedSheetNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int order = 1;

            foreach (var workshop in workshops)
            {
                rows.Add(new WorkshopExportRow(
                    WorkshopOrder: order,
                    WorkshopId: $"W{order}",
                    WorkshopName: workshop.Key,
                    IsLastSelected: string.Equals(workshop.Key, lastWorkshop, StringComparison.Ordinal),
                    NodesSheetKey: $"NS{order}",
                    SheetTabName: CreateUniqueWorkshopSheetName(workshop.Key, usedSheetNames),
                    RootNodes: workshop.Value));
                order++;
            }

            return rows;
        }

        private static IEnumerable<IReadOnlyList<WorksheetCell>> BuildMetaRows(
            int schemaVersion,
            string lastWorkshop,
            string lastWorkshopId)
        {
            yield return new[]
            {
                WorksheetCell.String("FormatId"),
                WorksheetCell.String(KnowledgeBaseExcelExchangeService.WorkbookFormatId)
            };
            yield return new[]
            {
                WorksheetCell.String("FormatVersion"),
                WorksheetCell.Number(KnowledgeBaseExcelExchangeService.WorkbookFormatVersion)
            };
            yield return new[]
            {
                WorksheetCell.String("SchemaVersion"),
                WorksheetCell.Number(schemaVersion)
            };
            yield return new[]
            {
                WorksheetCell.String("LastWorkshopId"),
                WorksheetCell.String(lastWorkshopId)
            };
            yield return new[]
            {
                WorksheetCell.String("LastWorkshop"),
                WorksheetCell.String(lastWorkshop)
            };
        }

        private static IEnumerable<IReadOnlyList<WorksheetCell>> BuildLevelRows(KbConfig config)
        {
            for (int index = 0; index < config.LevelNames.Count; index++)
            {
                yield return new[]
                {
                    WorksheetCell.Number(index),
                    WorksheetCell.String(config.LevelNames[index])
                };
            }
        }

        private static IEnumerable<IReadOnlyList<WorksheetCell>> BuildWorkshopRows(
            IEnumerable<WorkshopExportRow> workshops)
        {
            foreach (var workshop in workshops)
            {
                yield return new[]
                {
                    WorksheetCell.Number(workshop.WorkshopOrder),
                    WorksheetCell.String(workshop.WorkshopId),
                    WorksheetCell.String(workshop.WorkshopName),
                    WorksheetCell.Boolean(workshop.IsLastSelected),
                    WorksheetCell.String(workshop.NodesSheetKey)
                };
            }
        }

        private static IReadOnlyList<IReadOnlyList<WorksheetCell>> BuildWorkshopNodesSheetRows(
            KbConfig config,
            WorkshopExportRow workshop,
            ref int nextNodeId)
        {
            var rows = new List<IReadOnlyList<WorksheetCell>>
            {
                new[]
                {
                    WorksheetCell.String("Property"),
                    WorksheetCell.String("Value")
                },
                new[]
                {
                    WorksheetCell.String("SheetKind"),
                    WorksheetCell.String(WorkshopNodesSheetKind)
                },
                new[]
                {
                    WorksheetCell.String("WorkshopId"),
                    WorksheetCell.String(workshop.WorkshopId)
                },
                new[]
                {
                    WorksheetCell.String("NodesSheetKey"),
                    WorksheetCell.String(workshop.NodesSheetKey)
                },
                Array.Empty<WorksheetCell>(),
                new[]
                {
                    WorksheetCell.String("NodeId"),
                    WorksheetCell.String("ParentNodeId"),
                    WorksheetCell.String("SiblingOrder"),
                    WorksheetCell.String("LevelIndex"),
                    WorksheetCell.String("LevelName"),
                    WorksheetCell.String("NodeName"),
                    WorksheetCell.String("Path")
                }
            };

            for (int index = 0; index < workshop.RootNodes.Count; index++)
            {
                FlattenNode(
                    rows,
                    config,
                    workshop.RootNodes[index],
                    parentNodeId: null,
                    siblingOrder: index + 1,
                    currentPath: workshop.RootNodes[index].Name,
                    nextNodeId: ref nextNodeId);
            }

            return rows;
        }

        private static void FlattenNode(
            ICollection<IReadOnlyList<WorksheetCell>> rows,
            KbConfig config,
            KbNode node,
            int? parentNodeId,
            int siblingOrder,
            string currentPath,
            ref int nextNodeId)
        {
            int nodeId = nextNodeId;
            nextNodeId++;

            rows.Add(new[]
            {
                WorksheetCell.Number(nodeId),
                parentNodeId.HasValue ? WorksheetCell.Number(parentNodeId.Value) : WorksheetCell.String(string.Empty),
                WorksheetCell.Number(siblingOrder),
                WorksheetCell.Number(node.LevelIndex),
                WorksheetCell.String(GetLevelName(config, node.LevelIndex)),
                WorksheetCell.String(node.Name),
                WorksheetCell.String(currentPath)
            });

            for (int childIndex = 0; childIndex < node.Children.Count; childIndex++)
            {
                var child = node.Children[childIndex];
                FlattenNode(
                    rows,
                    config,
                    child,
                    nodeId,
                    childIndex + 1,
                    $"{currentPath} / {child.Name}",
                    ref nextNodeId);
            }
        }

        private static Worksheet BuildWorksheet(IReadOnlyList<IReadOnlyList<WorksheetCell>> rows)
        {
            var sheetData = new SheetData();

            for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                var row = new Row { RowIndex = (uint)(rowIndex + 1) };
                var cells = rows[rowIndex];

                for (int columnIndex = 0; columnIndex < cells.Count; columnIndex++)
                {
                    row.Append(CreateCell(cells[columnIndex], rowIndex + 1, columnIndex + 1));
                }

                sheetData.Append(row);
            }

            return new Worksheet(sheetData);
        }

        private static Cell CreateCell(WorksheetCell cell, int rowIndex, int columnIndex)
        {
            string cellReference = GetCellReference(rowIndex, columnIndex);

            return cell.Kind switch
            {
                WorksheetCellKind.Number => new Cell
                {
                    CellReference = cellReference,
                    CellValue = new CellValue(cell.Value)
                },
                WorksheetCellKind.Boolean => new Cell
                {
                    CellReference = cellReference,
                    DataType = CellValues.Boolean,
                    CellValue = new CellValue(cell.Value)
                },
                _ => CreateInlineStringCell(cellReference, cell.Value)
            };
        }

        private static Cell CreateInlineStringCell(string cellReference, string value)
        {
            var text = new Text(value);
            if (value.Length != value.Trim().Length)
                text.Space = SpaceProcessingModeValues.Preserve;

            return new Cell
            {
                CellReference = cellReference,
                DataType = CellValues.InlineString,
                InlineString = new InlineString(text)
            };
        }

        private static List<IReadOnlyList<WorksheetCell>> BuildTabularRows(
            IReadOnlyList<string> headers,
            IEnumerable<IReadOnlyList<WorksheetCell>> rows)
        {
            var allRows = new List<IReadOnlyList<WorksheetCell>>
            {
                headers.Select(WorksheetCell.String).ToArray()
            };
            allRows.AddRange(rows);
            return allRows;
        }

        private static string GetLevelName(KbConfig config, int levelIndex) =>
            config.LevelNames.Count > levelIndex
                ? config.LevelNames[levelIndex]
                : $"Ур. {levelIndex + 1}";

        private static string CreateUniqueWorkshopSheetName(string workshopName, ISet<string> usedNames)
        {
            string baseName = SanitizeWorksheetName($"Узлы - {workshopName}");
            if (string.IsNullOrWhiteSpace(baseName))
                baseName = "Узлы";

            if (usedNames.Add(baseName))
                return baseName;

            for (int attempt = 2; attempt < 1000; attempt++)
            {
                string suffix = $" ({attempt})";
                int maxBaseLength = MaxWorksheetNameLength - suffix.Length;
                string candidate = $"{TrimToLength(baseName, maxBaseLength)}{suffix}";
                if (usedNames.Add(candidate))
                    return candidate;
            }

            throw new InvalidOperationException("Не удалось подобрать уникальное имя worksheet для цеха.");
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

            return TrimToLength(normalized, MaxWorksheetNameLength);
        }

        private static string TrimToLength(string value, int maxLength)
        {
            if (maxLength <= 0)
                return string.Empty;

            return value.Length <= maxLength
                ? value
                : value[..maxLength].TrimEnd();
        }

        private static string GetCellReference(int rowIndex, int columnIndex) =>
            $"{GetColumnName(columnIndex)}{rowIndex.ToString(CultureInfo.InvariantCulture)}";

        private static string GetColumnName(int columnIndex)
        {
            if (columnIndex <= 0)
                throw new ArgumentOutOfRangeException(nameof(columnIndex));

            var chars = new Stack<char>();
            int current = columnIndex;
            while (current > 0)
            {
                current--;
                chars.Push((char)('A' + (current % 26)));
                current /= 26;
            }

            return new string(chars.ToArray());
        }

        private sealed record WorksheetDefinition(
            string Name,
            IReadOnlyList<IReadOnlyList<WorksheetCell>> Rows);

        private sealed record WorkshopExportRow(
            int WorkshopOrder,
            string WorkshopId,
            string WorkshopName,
            bool IsLastSelected,
            string NodesSheetKey,
            string SheetTabName,
            List<KbNode> RootNodes);

        private enum WorksheetCellKind
        {
            String,
            Number,
            Boolean
        }

        private readonly record struct WorksheetCell(WorksheetCellKind Kind, string Value)
        {
            public static WorksheetCell String(string value) => new(WorksheetCellKind.String, value);

            public static WorksheetCell Number(int value) =>
                new(WorksheetCellKind.Number, value.ToString(CultureInfo.InvariantCulture));

            public static WorksheetCell Boolean(bool value) => new(WorksheetCellKind.Boolean, value ? "1" : "0");
        }
    }
}
