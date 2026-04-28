using System.Globalization;
using AsutpKnowledgeBase.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace AsutpKnowledgeBase.Services
{
    internal sealed class KnowledgeBaseXlsxWriter
    {
        private const string InstructionsSheetName = "Инструкция";
        private const string MetaSheetName = "Meta";
        private const string LevelsSheetName = "Levels";
        private const string WorkshopsSheetName = "Workshops";
        private const string WorkshopNodesSheetKind = "WorkshopNodes";
        private const int MaxWorksheetNameLength = 31;

        private const uint DefaultLockedStyleIndex = 0;
        private const uint HeaderStyleIndex = 1;
        private const uint EditableStyleIndex = 2;
        private const uint ReadOnlyStyleIndex = 3;
        private const uint WrappedReadOnlyStyleIndex = 4;

        private static readonly char[] InvalidWorksheetNameCharacters = { ':', '\\', '/', '?', '*', '[', ']' };

        public byte[] BuildWorkbookPackage(SavedData data)
        {
            var normalizedData = KnowledgeBaseDataService.NormalizeSavedData(new SavedData
            {
                SchemaVersion = data.SchemaVersion,
                Config = data.Config,
                Workshops = CloneWorkshops(data.Workshops),
                CompositionEntries = data.CompositionEntries,
                LastWorkshop = data.LastWorkshop
            });
            var normalizedConfig = normalizedData.Config;
            var normalizedWorkshops = normalizedData.Workshops;
            string lastWorkshop = normalizedData.LastWorkshop;
            var workshopExports = BuildWorkshopExports(normalizedWorkshops, lastWorkshop);
            string lastWorkshopId = workshopExports
                .Single(workshop => string.Equals(workshop.WorkshopName, lastWorkshop, StringComparison.Ordinal))
                .WorkshopId;
            var worksheets = new List<WorksheetDefinition>
            {
                BuildInstructionsWorksheet(),
                new(
                    MetaSheetName,
                    BuildTabularRows(
                        headers: new[] { "Property", "Value" },
                        rows: BuildMetaRows(normalizedData.SchemaVersion, lastWorkshop, lastWorkshopId)),
                    BuildMetaColumns(),
                    1,
                    true),
                new(
                    LevelsSheetName,
                    BuildTabularRows(
                        headers: new[] { "LevelIndex", "LevelName" },
                        rows: BuildLevelRows(normalizedConfig)),
                    BuildLevelColumns(),
                    1,
                    true),
                new(
                    WorkshopsSheetName,
                    BuildTabularRows(
                        headers: new[] { "WorkshopOrder", "WorkshopId", "WorkshopName", "IsLastSelected", "NodesSheetKey" },
                        rows: BuildWorkshopRows(workshopExports)),
                    BuildWorkshopColumns(),
                    1,
                    true)
            };

            foreach (var workshop in workshopExports)
            {
                worksheets.Add(new WorksheetDefinition(
                    workshop.SheetTabName,
                    BuildWorkshopNodesSheetRows(normalizedConfig, workshop),
                    BuildWorkshopNodeColumns(),
                    6,
                    true));
            }

            using var stream = new MemoryStream();
            using (var document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook, autoSave: true))
            {
                var workbookPart = document.AddWorkbookPart();
                workbookPart.Workbook = new Workbook();

                var stylesPart = workbookPart.AddNewPart<WorkbookStylesPart>();
                stylesPart.Stylesheet = BuildStylesheet();
                stylesPart.Stylesheet.Save();

                var sheets = workbookPart.Workbook.AppendChild(new Sheets());
                uint sheetId = 1;

                foreach (var worksheetDefinition in worksheets)
                {
                    var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                    worksheetPart.Worksheet = BuildWorksheet(worksheetDefinition);
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

        private static WorksheetDefinition BuildInstructionsWorksheet()
        {
            var rows = BuildTabularRows(
                headers: new[] { "Раздел", "Инструкция" },
                rows: BuildInstructionsRows());

            return new WorksheetDefinition(
                InstructionsSheetName,
                rows,
                BuildInstructionsColumns(),
                1,
                true);
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

        private static Dictionary<string, List<KbNode>> CloneWorkshops(Dictionary<string, List<KbNode>>? workshops)
        {
            var clone = new Dictionary<string, List<KbNode>>();
            if (workshops == null)
                return clone;

            foreach (var workshop in workshops)
                clone[workshop.Key] = (workshop.Value ?? new List<KbNode>()).Select(CloneNode).ToList();

            return clone;
        }

        private static KbNode CloneNode(KbNode source)
        {
            var details = source.Details ?? new KbNodeDetails();
            return new KbNode
            {
                NodeId = source.NodeId,
                Name = source.Name,
                LevelIndex = source.LevelIndex,
                NodeType = source.NodeType,
                Details = new KbNodeDetails
                {
                    Description = details.Description,
                    Location = details.Location,
                    PhotoPath = details.PhotoPath,
                    IpAddress = details.IpAddress,
                    SchemaLink = details.SchemaLink
                },
                Children = (source.Children ?? new List<KbNode>()).Select(CloneNode).ToList()
            };
        }

        private static IEnumerable<IReadOnlyList<WorksheetCell>> BuildInstructionsRows()
        {
            yield return new[]
            {
                WorksheetCell.String("Книга v3", ReadOnlyStyleIndex),
                WorksheetCell.String("Эта книга нужна для ручной правки обменного формата. JSON остаётся основным источником данных.", WrappedReadOnlyStyleIndex)
            };
            yield return new[]
            {
                WorksheetCell.String("Можно редактировать", ReadOnlyStyleIndex),
                WorksheetCell.String(
                    "Workshops.WorkshopName, Workshops.IsLastSelected и поля узлов NodeName, Description, Location, PhotoPath, IpAddress, SchemaLink. Лист Levels сохранён как слой совместимости и обычно не требует правки.",
                    WrappedReadOnlyStyleIndex)
            };
            yield return new[]
            {
                WorksheetCell.String("Скрытые техполя", ReadOnlyStyleIndex),
                WorksheetCell.String("WorkshopOrder, WorkshopId, NodesSheetKey, NodeId, ParentNodeId, SiblingOrder и LevelIndex скрыты и защищены от случайной правки.", WrappedReadOnlyStyleIndex)
            };
            yield return new[]
            {
                WorksheetCell.String("Колонки только для чтения", ReadOnlyStyleIndex),
                WorksheetCell.String("Meta.*, Nodes.LevelName и Nodes.Path нужны для контекста и не редактируются.", WrappedReadOnlyStyleIndex)
            };
            yield return new[]
            {
                WorksheetCell.String("Фото объекта", ReadOnlyStyleIndex),
                WorksheetCell.String("PhotoPath хранит локальный или сетевой путь к изображению. Приложение пытается показать превью, если файл доступен.", WrappedReadOnlyStyleIndex)
            };
            yield return new[]
            {
                WorksheetCell.String("Не ломайте структуру", ReadOnlyStyleIndex),
                WorksheetCell.String("Не удаляйте обязательные листы и заголовки, не меняйте FormatId/FormatVersion и связи WorkshopId / NodesSheetKey.", WrappedReadOnlyStyleIndex)
            };
        }

        private static IEnumerable<IReadOnlyList<WorksheetCell>> BuildMetaRows(
            int schemaVersion,
            string lastWorkshop,
            string lastWorkshopId)
        {
            yield return BuildReadOnlyPair("FormatId", KnowledgeBaseExcelExchangeService.WorkbookFormatId);
            yield return BuildReadOnlyPair("FormatVersion", KnowledgeBaseExcelExchangeService.WorkbookFormatVersion.ToString(CultureInfo.InvariantCulture));
            yield return BuildReadOnlyPair("SchemaVersion", schemaVersion.ToString(CultureInfo.InvariantCulture));
            yield return BuildReadOnlyPair("LastWorkshopId", lastWorkshopId);
            yield return BuildReadOnlyPair("LastWorkshop", lastWorkshop);
        }

        private static IEnumerable<IReadOnlyList<WorksheetCell>> BuildLevelRows(KbConfig config)
        {
            for (int index = 0; index < config.LevelNames.Count; index++)
            {
                yield return new[]
                {
                    WorksheetCell.Number(index, ReadOnlyStyleIndex),
                    WorksheetCell.String(config.LevelNames[index], EditableStyleIndex)
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
                    WorksheetCell.Number(workshop.WorkshopOrder, ReadOnlyStyleIndex),
                    WorksheetCell.String(workshop.WorkshopId, ReadOnlyStyleIndex),
                    WorksheetCell.String(workshop.WorkshopName, EditableStyleIndex),
                    WorksheetCell.Boolean(workshop.IsLastSelected, EditableStyleIndex),
                    WorksheetCell.String(workshop.NodesSheetKey, ReadOnlyStyleIndex)
                };
            }
        }

        private static IReadOnlyList<IReadOnlyList<WorksheetCell>> BuildWorkshopNodesSheetRows(
            KbConfig config,
            WorkshopExportRow workshop)
        {
            var rows = new List<IReadOnlyList<WorksheetCell>>
            {
                new[]
                {
                    WorksheetCell.String("Property", HeaderStyleIndex),
                    WorksheetCell.String("Value", HeaderStyleIndex)
                },
                BuildReadOnlyPair("SheetKind", WorkshopNodesSheetKind),
                BuildReadOnlyPair("WorkshopId", workshop.WorkshopId),
                BuildReadOnlyPair("NodesSheetKey", workshop.NodesSheetKey),
                Array.Empty<WorksheetCell>(),
                new[]
                {
                    WorksheetCell.String("NodeId", HeaderStyleIndex),
                    WorksheetCell.String("ParentNodeId", HeaderStyleIndex),
                    WorksheetCell.String("SiblingOrder", HeaderStyleIndex),
                    WorksheetCell.String("LevelIndex", HeaderStyleIndex),
                    WorksheetCell.String("LevelName", HeaderStyleIndex),
                    WorksheetCell.String("NodeType", HeaderStyleIndex),
                    WorksheetCell.String("NodeName", HeaderStyleIndex),
                    WorksheetCell.String("Description", HeaderStyleIndex),
                    WorksheetCell.String("Location", HeaderStyleIndex),
                    WorksheetCell.String("PhotoPath", HeaderStyleIndex),
                    WorksheetCell.String("IpAddress", HeaderStyleIndex),
                    WorksheetCell.String("SchemaLink", HeaderStyleIndex),
                    WorksheetCell.String("Path", HeaderStyleIndex)
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
                    currentPath: workshop.RootNodes[index].Name);
            }

            return rows;
        }

        private static void FlattenNode(
            ICollection<IReadOnlyList<WorksheetCell>> rows,
            KbConfig config,
            KbNode node,
            string? parentNodeId,
            int siblingOrder,
            string currentPath)
        {
            var details = node.Details ?? new KbNodeDetails();
            string ipAddress = KnowledgeBaseNodeMetadataService.SupportsTechnicalFields(node.NodeType) ? details.IpAddress : string.Empty;
            string schemaLink = KnowledgeBaseNodeMetadataService.SupportsTechnicalFields(node.NodeType) ? details.SchemaLink : string.Empty;

            rows.Add(new[]
            {
                WorksheetCell.String(node.NodeId, ReadOnlyStyleIndex),
                !string.IsNullOrWhiteSpace(parentNodeId)
                    ? WorksheetCell.String(parentNodeId, ReadOnlyStyleIndex)
                    : WorksheetCell.String(string.Empty, ReadOnlyStyleIndex),
                WorksheetCell.Number(siblingOrder, ReadOnlyStyleIndex),
                WorksheetCell.Number(node.LevelIndex, ReadOnlyStyleIndex),
                WorksheetCell.String(GetLevelName(config, node.LevelIndex), ReadOnlyStyleIndex),
                WorksheetCell.String(node.NodeType.ToString(), ReadOnlyStyleIndex),
                WorksheetCell.String(node.Name, EditableStyleIndex),
                WorksheetCell.String(details.Description, EditableStyleIndex),
                WorksheetCell.String(details.Location, EditableStyleIndex),
                WorksheetCell.String(details.PhotoPath, EditableStyleIndex),
                WorksheetCell.String(ipAddress, EditableStyleIndex),
                WorksheetCell.String(schemaLink, EditableStyleIndex),
                WorksheetCell.String(currentPath, WrappedReadOnlyStyleIndex)
            });

            for (int childIndex = 0; childIndex < node.Children.Count; childIndex++)
            {
                var child = node.Children[childIndex];
                FlattenNode(
                    rows,
                    config,
                    child,
                    node.NodeId,
                    childIndex + 1,
                    $"{currentPath} / {child.Name}");
            }
        }

        private static Worksheet BuildWorksheet(WorksheetDefinition definition)
        {
            var worksheet = new Worksheet();

            if (definition.FrozenRowCount > 0)
                worksheet.Append(CreateSheetViews(definition.FrozenRowCount));

            if (definition.Columns.Count > 0)
                worksheet.Append(BuildColumns(definition.Columns));

            worksheet.Append(BuildSheetData(definition.Rows));

            if (definition.ProtectSheet)
                worksheet.Append(CreateSheetProtection());

            return worksheet;
        }

        private static SheetViews CreateSheetViews(uint frozenRowCount)
        {
            string topLeftCell = $"A{frozenRowCount + 1}";

            var sheetView = new SheetView { WorkbookViewId = 0U };
            sheetView.Append(new Pane
            {
                VerticalSplit = frozenRowCount,
                TopLeftCell = topLeftCell,
                ActivePane = PaneValues.BottomLeft,
                State = PaneStateValues.Frozen
            });
            sheetView.Append(new Selection
            {
                Pane = PaneValues.BottomLeft,
                ActiveCell = topLeftCell,
                SequenceOfReferences = new ListValue<StringValue> { InnerText = topLeftCell }
            });

            return new SheetViews(sheetView);
        }

        private static Columns BuildColumns(IEnumerable<WorksheetColumnDefinition> columns)
        {
            var worksheetColumns = new Columns();
            foreach (var column in columns)
            {
                worksheetColumns.Append(new Column
                {
                    Min = column.Min,
                    Max = column.Max,
                    Width = column.Width,
                    Hidden = column.Hidden,
                    BestFit = !column.Hidden,
                    CustomWidth = true
                });
            }

            return worksheetColumns;
        }

        private static SheetData BuildSheetData(IReadOnlyList<IReadOnlyList<WorksheetCell>> rows)
        {
            var sheetData = new SheetData();

            for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                var row = new Row { RowIndex = (uint)(rowIndex + 1) };
                var cells = rows[rowIndex];

                for (int columnIndex = 0; columnIndex < cells.Count; columnIndex++)
                    row.Append(CreateCell(cells[columnIndex], rowIndex + 1, columnIndex + 1));

                sheetData.Append(row);
            }

            return sheetData;
        }

        private static SheetProtection CreateSheetProtection() =>
            new()
            {
                Sheet = true,
                Objects = true,
                Scenarios = true,
                FormatCells = false,
                FormatColumns = false,
                FormatRows = false,
                InsertColumns = false,
                InsertRows = false,
                InsertHyperlinks = false,
                DeleteColumns = false,
                DeleteRows = false,
                Sort = false,
                AutoFilter = false,
                PivotTables = false,
                SelectLockedCells = true,
                SelectUnlockedCells = true
            };

        private static Stylesheet BuildStylesheet()
        {
            var fonts = new Fonts(
                new Font(
                    new FontSize { Val = 11D },
                    new Color { Theme = 1U },
                    new FontName { Val = "Calibri" },
                    new FontFamilyNumbering { Val = 2 },
                    new FontScheme { Val = FontSchemeValues.Minor }),
                new Font(
                    new Bold(),
                    new FontSize { Val = 11D },
                    new Color { Theme = 1U },
                    new FontName { Val = "Calibri" },
                    new FontFamilyNumbering { Val = 2 },
                    new FontScheme { Val = FontSchemeValues.Minor }))
            {
                Count = 2U
            };

            var fills = new Fills(
                new Fill(new PatternFill { PatternType = PatternValues.None }),
                new Fill(new PatternFill { PatternType = PatternValues.Gray125 }),
                CreateSolidFill("FFFFF2CC"),
                CreateSolidFill("FFEDEDED"),
                CreateSolidFill("FFD9EAF7"))
            {
                Count = 5U
            };

            var borders = new Borders(new Border()) { Count = 1U };

            var cellStyleFormats = new CellStyleFormats(new CellFormat()) { Count = 1U };

            var cellFormats = new CellFormats(
                new CellFormat
                {
                    FontId = 0U,
                    FillId = 0U,
                    BorderId = 0U
                },
                new CellFormat
                {
                    FontId = 1U,
                    FillId = 4U,
                    BorderId = 0U,
                    ApplyFont = true,
                    ApplyFill = true,
                    ApplyAlignment = true,
                    Alignment = new Alignment
                    {
                        Horizontal = HorizontalAlignmentValues.Center,
                        Vertical = VerticalAlignmentValues.Center,
                        WrapText = true
                    }
                },
                new CellFormat
                {
                    FontId = 0U,
                    FillId = 2U,
                    BorderId = 0U,
                    ApplyFill = true,
                    ApplyAlignment = true,
                    ApplyProtection = true,
                    Alignment = new Alignment
                    {
                        Vertical = VerticalAlignmentValues.Top
                    },
                    Protection = new Protection
                    {
                        Locked = false
                    }
                },
                new CellFormat
                {
                    FontId = 0U,
                    FillId = 3U,
                    BorderId = 0U,
                    ApplyFill = true,
                    ApplyAlignment = true,
                    Alignment = new Alignment
                    {
                        Vertical = VerticalAlignmentValues.Top
                    }
                },
                new CellFormat
                {
                    FontId = 0U,
                    FillId = 3U,
                    BorderId = 0U,
                    ApplyFill = true,
                    ApplyAlignment = true,
                    Alignment = new Alignment
                    {
                        Vertical = VerticalAlignmentValues.Top,
                        WrapText = true
                    }
                })
            {
                Count = 5U
            };

            return new Stylesheet(
                fonts,
                fills,
                borders,
                cellStyleFormats,
                cellFormats,
                new CellStyles(new CellStyle { Name = "Normal", FormatId = 0U, BuiltinId = 0U }) { Count = 1U },
                new DifferentialFormats { Count = 0U },
                new TableStyles
                {
                    Count = 0U,
                    DefaultTableStyle = "TableStyleMedium2",
                    DefaultPivotStyle = "PivotStyleLight16"
                });
        }

        private static Fill CreateSolidFill(string rgb) =>
            new(new PatternFill(
                    new ForegroundColor { Rgb = HexBinaryValue.FromString(rgb) },
                    new BackgroundColor { Indexed = 64U })
            {
                PatternType = PatternValues.Solid
            });

        private static Cell CreateCell(WorksheetCell cell, int rowIndex, int columnIndex)
        {
            string cellReference = GetCellReference(rowIndex, columnIndex);

            return cell.Kind switch
            {
                WorksheetCellKind.Number => new Cell
                {
                    CellReference = cellReference,
                    StyleIndex = cell.StyleIndex,
                    CellValue = new CellValue(cell.Value)
                },
                WorksheetCellKind.Boolean => new Cell
                {
                    CellReference = cellReference,
                    StyleIndex = cell.StyleIndex,
                    DataType = CellValues.Boolean,
                    CellValue = new CellValue(cell.Value)
                },
                _ => CreateInlineStringCell(cellReference, cell.Value, cell.StyleIndex)
            };
        }

        private static Cell CreateInlineStringCell(string cellReference, string value, uint styleIndex)
        {
            var text = new Text(value);
            if (value.Length != value.Trim().Length)
                text.Space = SpaceProcessingModeValues.Preserve;

            return new Cell
            {
                CellReference = cellReference,
                StyleIndex = styleIndex,
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
                headers.Select(header => WorksheetCell.String(header, HeaderStyleIndex)).ToArray()
            };
            allRows.AddRange(rows);
            return allRows;
        }

        private static IReadOnlyList<WorksheetCell> BuildReadOnlyPair(string propertyName, string value) =>
            new[]
            {
                WorksheetCell.String(propertyName, ReadOnlyStyleIndex),
                WorksheetCell.String(value, WrappedReadOnlyStyleIndex)
            };

        private static IReadOnlyList<WorksheetColumnDefinition> BuildInstructionsColumns() =>
            new[]
            {
                new WorksheetColumnDefinition(1U, 1U, 24D, false),
                new WorksheetColumnDefinition(2U, 2U, 96D, false)
            };

        private static IReadOnlyList<WorksheetColumnDefinition> BuildMetaColumns() =>
            new[]
            {
                new WorksheetColumnDefinition(1U, 1U, 24D, false),
                new WorksheetColumnDefinition(2U, 2U, 40D, false)
            };

        private static IReadOnlyList<WorksheetColumnDefinition> BuildLevelColumns() =>
            new[]
            {
                new WorksheetColumnDefinition(1U, 1U, 12D, true),
                new WorksheetColumnDefinition(2U, 2U, 28D, false)
            };

        private static IReadOnlyList<WorksheetColumnDefinition> BuildWorkshopColumns() =>
            new[]
            {
                new WorksheetColumnDefinition(1U, 1U, 12D, true),
                new WorksheetColumnDefinition(2U, 2U, 12D, true),
                new WorksheetColumnDefinition(3U, 3U, 30D, false),
                new WorksheetColumnDefinition(4U, 4U, 18D, false),
                new WorksheetColumnDefinition(5U, 5U, 14D, true)
            };

        private static IReadOnlyList<WorksheetColumnDefinition> BuildWorkshopNodeColumns() =>
            new[]
            {
                new WorksheetColumnDefinition(1U, 1U, 12D, true),
                new WorksheetColumnDefinition(2U, 2U, 14D, true),
                new WorksheetColumnDefinition(3U, 3U, 12D, true),
                new WorksheetColumnDefinition(4U, 4U, 12D, true),
                new WorksheetColumnDefinition(5U, 5U, 18D, false),
                new WorksheetColumnDefinition(6U, 6U, 18D, false),
                new WorksheetColumnDefinition(7U, 7U, 30D, false),
                new WorksheetColumnDefinition(8U, 8U, 42D, false),
                new WorksheetColumnDefinition(9U, 9U, 28D, false),
                new WorksheetColumnDefinition(10U, 10U, 38D, false),
                new WorksheetColumnDefinition(11U, 11U, 20D, false),
                new WorksheetColumnDefinition(12U, 12U, 28D, false),
                new WorksheetColumnDefinition(13U, 13U, 48D, false)
            };

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

            throw new InvalidOperationException("Не удалось подобрать уникальное имя листа для цеха.");
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
            IReadOnlyList<IReadOnlyList<WorksheetCell>> Rows,
            IReadOnlyList<WorksheetColumnDefinition> Columns,
            uint FrozenRowCount,
            bool ProtectSheet);

        private sealed record WorksheetColumnDefinition(
            uint Min,
            uint Max,
            double Width,
            bool Hidden);

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

        private readonly record struct WorksheetCell(WorksheetCellKind Kind, string Value, uint StyleIndex)
        {
            public static WorksheetCell String(string value, uint styleIndex = DefaultLockedStyleIndex) =>
                new(WorksheetCellKind.String, value, styleIndex);

            public static WorksheetCell Number(int value, uint styleIndex = DefaultLockedStyleIndex) =>
                new(WorksheetCellKind.Number, value.ToString(CultureInfo.InvariantCulture), styleIndex);

            public static WorksheetCell Boolean(bool value, uint styleIndex = DefaultLockedStyleIndex) =>
                new(WorksheetCellKind.Boolean, value ? "1" : "0", styleIndex);
        }
    }
}
