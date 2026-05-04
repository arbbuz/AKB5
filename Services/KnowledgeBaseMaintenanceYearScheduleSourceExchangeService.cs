using System.Globalization;
using AsutpKnowledgeBase.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace AsutpKnowledgeBase.Services
{
    public sealed class KnowledgeBaseMaintenanceYearScheduleSourceExportResult
    {
        public bool IsSuccess { get; init; }

        public string ErrorMessage { get; init; } = string.Empty;

        public byte[] WorkbookPackage { get; init; } = Array.Empty<byte>();

        public int ExportedProfileCount { get; init; }

        public int ManualScheduleProfileCount { get; init; }

        public int AutomaticFallbackProfileCount { get; init; }
    }

    public sealed class KnowledgeBaseMaintenanceYearScheduleSourceImportResult
    {
        public bool IsSuccess { get; init; }

        public string ErrorMessage { get; init; } = string.Empty;

        public List<KbMaintenanceScheduleProfile> MaintenanceScheduleProfiles { get; init; } = new();

        public int ImportedRowCount { get; init; }

        public int UpdatedProfileCount { get; init; }

        public int ClearedProfileCount { get; init; }

        public int UnchangedProfileCount { get; init; }

        public List<string> UnresolvedRows { get; init; } = new();
    }

    public sealed class KnowledgeBaseMaintenanceYearScheduleSourceExchangeService
    {
        private const string InstructionsSheetName = "Инструкция";
        private const string SourceSheetName = "YearScheduleSource";
        private const string OwnerNodeIdHeader = "OwnerNodeId";
        private const string PathHeader = "Path";
        private const string NodeNameHeader = "NodeName";
        private const string InventoryNumberHeader = "InventoryNumber";
        private const string IncludedHeader = "IsIncludedInSchedule";
        private const string SourceModeHeader = "ScheduleSource";

        private static readonly string[] MonthHeaders = Enumerable
            .Range(1, 12)
            .Select(static month => $"M{month:00}")
            .ToArray();

        private static readonly string[] SourceHeaders = new[]
        {
            OwnerNodeIdHeader,
            PathHeader,
            NodeNameHeader,
            InventoryNumberHeader,
            IncludedHeader,
            SourceModeHeader
        }.Concat(MonthHeaders).ToArray();

        public KnowledgeBaseMaintenanceYearScheduleSourceExportResult ExportWorkbook(
            IReadOnlyList<KbNode>? roots,
            IReadOnlyList<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles)
        {
            try
            {
                List<YearScheduleSourceExportRow> rows = BuildExportRows(roots, maintenanceScheduleProfiles);
                return new KnowledgeBaseMaintenanceYearScheduleSourceExportResult
                {
                    IsSuccess = true,
                    WorkbookPackage = BuildWorkbookPackage(rows),
                    ExportedProfileCount = rows.Count,
                    ManualScheduleProfileCount = rows.Count(static row => row.HasManualSchedule),
                    AutomaticFallbackProfileCount = rows.Count(static row => !row.HasManualSchedule)
                };
            }
            catch (Exception ex)
            {
                return new KnowledgeBaseMaintenanceYearScheduleSourceExportResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Не удалось экспортировать источник годовой раскладки ТО: {ex.Message}"
                };
            }
        }

        public KnowledgeBaseMaintenanceYearScheduleSourceImportResult ImportWorkbook(
            byte[]? workbookPackage,
            IReadOnlyList<KbNode>? roots,
            IReadOnlyList<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles)
        {
            if (workbookPackage == null || workbookPackage.Length == 0)
                return ImportFailure("Файл Excel с источником годовой раскладки ТО не был передан.");

            try
            {
                List<ImportedYearScheduleSourceRow> importedRows = ParseWorkbook(workbookPackage);
                Dictionary<string, OwnerNodeContext> ownerNodeContexts = BuildOwnerNodeContexts(roots)
                    .Where(static context => !string.IsNullOrWhiteSpace(context.OwnerNodeId))
                    .GroupBy(static context => context.OwnerNodeId, StringComparer.Ordinal)
                    .ToDictionary(
                        static group => group.Key,
                        static group => group.OrderBy(static context => context.TreeOrder).First(),
                        StringComparer.Ordinal);

                List<KbMaintenanceScheduleProfile> updatedProfiles = CloneProfiles(maintenanceScheduleProfiles);
                Dictionary<string, KbMaintenanceScheduleProfile> profilesByOwnerNodeId = updatedProfiles
                    .Where(static profile => !string.IsNullOrWhiteSpace(profile.OwnerNodeId))
                    .GroupBy(static profile => profile.OwnerNodeId.Trim(), StringComparer.Ordinal)
                    .ToDictionary(
                        static group => group.Key,
                        static group => group.OrderBy(static profile => profile.MaintenanceProfileId, StringComparer.Ordinal).First(),
                        StringComparer.Ordinal);

                var unresolvedRows = new List<string>();
                int importedRowCount = 0;
                int updatedProfileCount = 0;
                int clearedProfileCount = 0;
                int unchangedProfileCount = 0;

                foreach (ImportedYearScheduleSourceRow row in importedRows)
                {
                    string ownerNodeId = row.OwnerNodeId.Trim();
                    if (string.IsNullOrWhiteSpace(ownerNodeId))
                        continue;

                    importedRowCount++;
                    if (!ownerNodeContexts.ContainsKey(ownerNodeId))
                    {
                        unresolvedRows.Add($"Строка {row.RowNumber}: узел OwnerNodeId '{ownerNodeId}' не найден в текущем цехе.");
                        continue;
                    }

                    if (!profilesByOwnerNodeId.TryGetValue(ownerNodeId, out KbMaintenanceScheduleProfile? profile))
                    {
                        unresolvedRows.Add($"Строка {row.RowNumber}: профиль ТО для OwnerNodeId '{ownerNodeId}' не настроен.");
                        continue;
                    }

                    if (!TryParseYearScheduleEntries(row, out List<KbMaintenanceYearScheduleEntry> importedEntries, out string errorMessage))
                        return ImportFailure(errorMessage);

                    if (YearScheduleEquals(profile.YearScheduleEntries, importedEntries))
                    {
                        unchangedProfileCount++;
                        continue;
                    }

                    bool wasManual = profile.YearScheduleEntries?.Count > 0;
                    profile.YearScheduleEntries = CloneYearScheduleEntries(importedEntries);
                    if (wasManual && profile.YearScheduleEntries.Count == 0)
                        clearedProfileCount++;
                    else
                        updatedProfileCount++;
                }

                return new KnowledgeBaseMaintenanceYearScheduleSourceImportResult
                {
                    IsSuccess = true,
                    MaintenanceScheduleProfiles = updatedProfiles,
                    ImportedRowCount = importedRowCount,
                    UpdatedProfileCount = updatedProfileCount,
                    ClearedProfileCount = clearedProfileCount,
                    UnchangedProfileCount = unchangedProfileCount,
                    UnresolvedRows = unresolvedRows
                };
            }
            catch (Exception ex)
            {
                return ImportFailure($"Не удалось импортировать источник годовой раскладки ТО: {ex.Message}");
            }
        }

        private static List<YearScheduleSourceExportRow> BuildExportRows(
            IReadOnlyList<KbNode>? roots,
            IReadOnlyList<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles)
        {
            Dictionary<string, OwnerNodeContext> ownerNodeContexts = BuildOwnerNodeContexts(roots)
                .Where(static context => !string.IsNullOrWhiteSpace(context.OwnerNodeId))
                .GroupBy(static context => context.OwnerNodeId, StringComparer.Ordinal)
                .ToDictionary(
                    static group => group.Key,
                    static group => group.OrderBy(static context => context.TreeOrder).First(),
                    StringComparer.Ordinal);

            var rows = new List<YearScheduleSourceExportRow>();
            foreach (KbMaintenanceScheduleProfile profile in CloneProfiles(maintenanceScheduleProfiles)
                         .Where(static profile => !string.IsNullOrWhiteSpace(profile.OwnerNodeId)))
            {
                string ownerNodeId = profile.OwnerNodeId.Trim();
                ownerNodeContexts.TryGetValue(ownerNodeId, out OwnerNodeContext? context);
                rows.Add(new YearScheduleSourceExportRow(
                    OwnerNodeId: ownerNodeId,
                    Path: context?.Path ?? string.Empty,
                    NodeName: context?.NodeName ?? string.Empty,
                    InventoryNumber: context?.InventoryNumber ?? string.Empty,
                    IsIncludedInSchedule: profile.IsIncludedInSchedule,
                    YearScheduleEntries: CloneYearScheduleEntries(profile.YearScheduleEntries),
                    TreeOrder: context?.TreeOrder ?? int.MaxValue));
            }

            return rows
                .OrderBy(static row => row.TreeOrder)
                .ThenBy(static row => row.Path, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static row => row.OwnerNodeId, StringComparer.Ordinal)
                .ToList();
        }

        private static byte[] BuildWorkbookPackage(IReadOnlyList<YearScheduleSourceExportRow> rows)
        {
            using var stream = new MemoryStream();
            using (var document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook, autoSave: true))
            {
                var workbookPart = document.AddWorkbookPart();
                workbookPart.Workbook = new Workbook();
                var sheets = workbookPart.Workbook.AppendChild(new Sheets());

                AppendWorksheet(
                    workbookPart,
                    sheets,
                    sheetId: 1,
                    InstructionsSheetName,
                    BuildInstructionRows());
                AppendWorksheet(
                    workbookPart,
                    sheets,
                    sheetId: 2,
                    SourceSheetName,
                    BuildSourceRows(rows));

                workbookPart.Workbook.Save();
            }

            return stream.ToArray();
        }

        private static IEnumerable<string[]> BuildInstructionRows()
        {
            yield return new[] { "Раздел", "Инструкция" };
            yield return new[]
            {
                "Назначение",
                "Это источник годовой раскладки ТО. Итоговый годовой график остаётся отчётом, а эти данные импортируются обратно в JSON-профили."
            };
            yield return new[]
            {
                "Что редактировать",
                "Заполняйте только колонки M01-M12 значениями ТО1, ТО2, ТО3 или оставляйте ячейку пустой для автоматического fallback."
            };
            yield return new[]
            {
                "Что не менять",
                "OwnerNodeId нужен для устойчивого сопоставления с деревом. Импорт не меняет нормы часов и признак участия в графике."
            };
        }

        private static IEnumerable<string[]> BuildSourceRows(IReadOnlyList<YearScheduleSourceExportRow> rows)
        {
            yield return SourceHeaders;
            foreach (YearScheduleSourceExportRow row in rows)
            {
                Dictionary<int, KbMaintenanceWorkKind> entriesByMonth = row.YearScheduleEntries
                    .Where(static entry => entry != null && entry.Month is >= 1 and <= 12)
                    .GroupBy(static entry => entry.Month)
                    .ToDictionary(
                        static group => group.Key,
                        static group => group.OrderByDescending(static entry => entry.WorkKind).First().WorkKind);

                var values = new List<string>
                {
                    row.OwnerNodeId,
                    row.Path,
                    row.NodeName,
                    row.InventoryNumber,
                    row.IsIncludedInSchedule ? "TRUE" : "FALSE",
                    row.HasManualSchedule ? "Ручной" : "Авто"
                };

                for (int month = 1; month <= 12; month++)
                {
                    values.Add(entriesByMonth.TryGetValue(month, out KbMaintenanceWorkKind workKind)
                        ? FormatWorkKind(workKind)
                        : string.Empty);
                }

                yield return values.ToArray();
            }
        }

        private static void AppendWorksheet(
            WorkbookPart workbookPart,
            Sheets sheets,
            uint sheetId,
            string sheetName,
            IEnumerable<string[]> rows)
        {
            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            var sheetData = new SheetData();
            uint rowIndex = 1;
            foreach (string[] rowValues in rows)
            {
                var row = new Row { RowIndex = rowIndex };
                for (int columnIndex = 1; columnIndex <= rowValues.Length; columnIndex++)
                {
                    row.Append(CreateInlineStringCell(
                        columnIndex,
                        rowIndex,
                        rowValues[columnIndex - 1] ?? string.Empty));
                }

                sheetData.Append(row);
                rowIndex++;
            }

            worksheetPart.Worksheet = new Worksheet(sheetData);
            worksheetPart.Worksheet.Save();

            sheets.Append(new Sheet
            {
                Id = workbookPart.GetIdOfPart(worksheetPart),
                SheetId = sheetId,
                Name = sheetName
            });
        }

        private static Cell CreateInlineStringCell(int columnIndex, uint rowIndex, string value) =>
            new()
            {
                CellReference = $"{GetColumnName(columnIndex)}{rowIndex.ToString(CultureInfo.InvariantCulture)}",
                DataType = CellValues.InlineString,
                InlineString = new InlineString(new Text(value ?? string.Empty))
            };

        private static List<ImportedYearScheduleSourceRow> ParseWorkbook(byte[] workbookPackage)
        {
            using var stream = new MemoryStream(workbookPackage, writable: false);
            using SpreadsheetDocument document = SpreadsheetDocument.Open(stream, false);
            WorkbookPart workbookPart = document.WorkbookPart
                ?? throw new InvalidOperationException("Файл XLSX не содержит workbook part.");

            Sheet sheet = workbookPart.Workbook.Sheets?.Elements<Sheet>()
                .FirstOrDefault(sheet => string.Equals(sheet.Name?.Value?.Trim(), SourceSheetName, StringComparison.Ordinal))
                ?? throw new InvalidOperationException($"Файл XLSX не содержит лист '{SourceSheetName}'.");

            string relationshipId = sheet.Id?.Value?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(relationshipId))
                throw new InvalidOperationException($"Для листа '{SourceSheetName}' отсутствует relationship id.");

            if (workbookPart.GetPartById(relationshipId) is not WorksheetPart worksheetPart)
                throw new InvalidOperationException($"Для листа '{SourceSheetName}' отсутствует worksheet part.");

            IReadOnlyList<string> sharedStrings = ReadSharedStrings(workbookPart.SharedStringTablePart);
            List<Row> rows = worksheetPart.Worksheet.GetFirstChild<SheetData>()?.Elements<Row>().ToList()
                ?? new List<Row>();
            if (rows.Count == 0)
                throw new InvalidOperationException($"Лист '{SourceSheetName}' не содержит строк.");

            Dictionary<int, string> headerValues = ReadRowValues(rows[0], sharedStrings);
            Dictionary<string, int> headerMap = BuildHeaderMap(headerValues, SourceSheetName, rows[0].RowIndex?.Value ?? 1);
            EnsureRequiredHeaders(headerMap, SourceHeaders, SourceSheetName, rows[0].RowIndex?.Value ?? 1);

            var importedRows = new List<ImportedYearScheduleSourceRow>();
            foreach (Row row in rows.Skip(1))
            {
                Dictionary<int, string> values = ReadRowValues(row, sharedStrings);
                if (values.Count == 0 || values.Values.All(string.IsNullOrWhiteSpace))
                    continue;

                var monthValues = new Dictionary<int, string>();
                for (int month = 1; month <= 12; month++)
                    monthValues[month] = ReadCell(values, headerMap, MonthHeaders[month - 1]);

                importedRows.Add(new ImportedYearScheduleSourceRow(
                    RowNumber: row.RowIndex?.Value ?? 0,
                    OwnerNodeId: ReadCell(values, headerMap, OwnerNodeIdHeader),
                    MonthValues: monthValues));
            }

            return importedRows;
        }

        private static bool TryParseYearScheduleEntries(
            ImportedYearScheduleSourceRow row,
            out List<KbMaintenanceYearScheduleEntry> entries,
            out string errorMessage)
        {
            entries = new List<KbMaintenanceYearScheduleEntry>();
            errorMessage = string.Empty;

            for (int month = 1; month <= 12; month++)
            {
                string value = row.MonthValues.TryGetValue(month, out string? rawValue)
                    ? rawValue?.Trim() ?? string.Empty
                    : string.Empty;
                if (string.IsNullOrWhiteSpace(value) || value == "-")
                    continue;

                if (!TryParseWorkKind(value, out KbMaintenanceWorkKind workKind))
                {
                    errorMessage =
                        $"Лист '{SourceSheetName}', строка {row.RowNumber}, колонка {MonthHeaders[month - 1]}: " +
                        $"ожидается ТО1, ТО2, ТО3 или пустое значение.";
                    return false;
                }

                entries.Add(new KbMaintenanceYearScheduleEntry
                {
                    Month = month,
                    WorkKind = workKind
                });
            }

            return true;
        }

        private static IReadOnlyList<string> ReadSharedStrings(SharedStringTablePart? part)
        {
            if (part?.SharedStringTable == null)
                return Array.Empty<string>();

            return part.SharedStringTable
                .Elements<SharedStringItem>()
                .Select(static item => string.Concat(item.Descendants<Text>().Select(text => text.Text)).Trim())
                .ToList();
        }

        private static Dictionary<int, string> ReadRowValues(Row row, IReadOnlyList<string> sharedStrings)
        {
            var values = new Dictionary<int, string>();
            foreach (Cell cell in row.Elements<Cell>())
            {
                int columnIndex = GetColumnIndex(cell.CellReference?.Value ?? string.Empty);
                if (columnIndex <= 0)
                    continue;

                values[columnIndex] = ReadCellValue(cell, sharedStrings);
            }

            return values;
        }

        private static string ReadCellValue(Cell cell, IReadOnlyList<string> sharedStrings)
        {
            if (cell.DataType?.Value == CellValues.SharedString)
            {
                string rawIndex = cell.CellValue?.InnerText?.Trim() ?? string.Empty;
                return int.TryParse(rawIndex, NumberStyles.Integer, CultureInfo.InvariantCulture, out int index) &&
                       index >= 0 &&
                       index < sharedStrings.Count
                    ? sharedStrings[index]
                    : string.Empty;
            }

            if (cell.DataType?.Value == CellValues.InlineString)
            {
                return string.Concat(
                        (cell.InlineString?.Descendants<Text>() ?? Enumerable.Empty<Text>())
                            .Select(text => text.Text))
                    .Trim();
            }

            return (cell.CellValue?.InnerText ?? string.Empty).Trim();
        }

        private static Dictionary<string, int> BuildHeaderMap(
            IReadOnlyDictionary<int, string> headerValues,
            string sheetName,
            uint rowNumber)
        {
            var headerMap = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var pair in headerValues)
            {
                string header = pair.Value.Trim();
                if (string.IsNullOrWhiteSpace(header))
                    continue;

                if (!headerMap.TryAdd(header, pair.Key))
                    throw new InvalidOperationException($"Лист '{sheetName}', строка {rowNumber}: найден дублирующийся заголовок '{header}'.");
            }

            return headerMap;
        }

        private static void EnsureRequiredHeaders(
            IReadOnlyDictionary<string, int> headerMap,
            IEnumerable<string> requiredHeaders,
            string sheetName,
            uint rowNumber)
        {
            foreach (string requiredHeader in requiredHeaders)
            {
                if (!headerMap.ContainsKey(requiredHeader))
                    throw new InvalidOperationException($"Лист '{sheetName}', строка {rowNumber}: отсутствует колонка '{requiredHeader}'.");
            }
        }

        private static string ReadCell(
            IReadOnlyDictionary<int, string> values,
            IReadOnlyDictionary<string, int> headerMap,
            string header)
        {
            if (!headerMap.TryGetValue(header, out int columnIndex))
                return string.Empty;

            return values.TryGetValue(columnIndex, out string? value)
                ? value?.Trim() ?? string.Empty
                : string.Empty;
        }

        private static List<OwnerNodeContext> BuildOwnerNodeContexts(IReadOnlyList<KbNode>? roots)
        {
            var contexts = new List<OwnerNodeContext>();
            int treeOrder = 0;
            foreach (KbNode root in roots ?? Array.Empty<KbNode>())
                CollectOwnerNodeContexts(contexts, root, visibleLevel: 1, parentPath: string.Empty, ref treeOrder);

            return contexts;
        }

        private static void CollectOwnerNodeContexts(
            ICollection<OwnerNodeContext> contexts,
            KbNode node,
            int visibleLevel,
            string parentPath,
            ref int treeOrder)
        {
            int currentVisibleLevel = GetEffectiveVisibleLevel(node, visibleLevel);
            string nodeName = node.Name?.Trim() ?? string.Empty;
            string path = string.IsNullOrWhiteSpace(parentPath)
                ? nodeName
                : $"{parentPath} / {nodeName}";

            if (KnowledgeBaseMaintenanceScheduleStateService.SupportsProfile(node.NodeType, currentVisibleLevel))
            {
                contexts.Add(new OwnerNodeContext(
                    OwnerNodeId: node.NodeId?.Trim() ?? string.Empty,
                    NodeName: nodeName,
                    InventoryNumber: node.Details?.InventoryNumber?.Trim() ?? string.Empty,
                    Path: path,
                    TreeOrder: treeOrder));
            }

            treeOrder++;
            foreach (KbNode child in node.Children ?? Enumerable.Empty<KbNode>())
                CollectOwnerNodeContexts(contexts, child, currentVisibleLevel + 1, path, ref treeOrder);
        }

        private static int GetEffectiveVisibleLevel(KbNode node, int visibleLevel)
        {
            if (node.NodeType == KbNodeType.WorkshopRoot && node.LevelIndex == 0)
                return Math.Max(0, visibleLevel - 1);

            return visibleLevel;
        }

        private static List<KbMaintenanceScheduleProfile> CloneProfiles(
            IReadOnlyList<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles)
        {
            var clones = new List<KbMaintenanceScheduleProfile>();
            foreach (KbMaintenanceScheduleProfile profile in maintenanceScheduleProfiles ?? Array.Empty<KbMaintenanceScheduleProfile>())
            {
                clones.Add(new KbMaintenanceScheduleProfile
                {
                    MaintenanceProfileId = profile.MaintenanceProfileId,
                    OwnerNodeId = profile.OwnerNodeId,
                    IsIncludedInSchedule = profile.IsIncludedInSchedule,
                    To1Hours = profile.To1Hours,
                    To2Hours = profile.To2Hours,
                    To3Hours = profile.To3Hours,
                    YearScheduleEntries = CloneYearScheduleEntries(profile.YearScheduleEntries)
                });
            }

            return clones;
        }

        private static List<KbMaintenanceYearScheduleEntry> CloneYearScheduleEntries(
            IReadOnlyList<KbMaintenanceYearScheduleEntry>? entries)
        {
            var clones = new List<KbMaintenanceYearScheduleEntry>();
            if (entries == null)
                return clones;

            foreach (KbMaintenanceYearScheduleEntry entry in entries
                         .Where(static entry => entry != null && entry.Month is >= 1 and <= 12)
                         .OrderBy(static entry => entry.Month))
            {
                clones.Add(new KbMaintenanceYearScheduleEntry
                {
                    Month = entry.Month,
                    WorkKind = entry.WorkKind
                });
            }

            return clones;
        }

        private static bool YearScheduleEquals(
            IReadOnlyList<KbMaintenanceYearScheduleEntry>? left,
            IReadOnlyList<KbMaintenanceYearScheduleEntry>? right)
        {
            List<KbMaintenanceYearScheduleEntry> leftEntries = CloneYearScheduleEntries(left);
            List<KbMaintenanceYearScheduleEntry> rightEntries = CloneYearScheduleEntries(right);
            if (leftEntries.Count != rightEntries.Count)
                return false;

            for (int index = 0; index < leftEntries.Count; index++)
            {
                if (leftEntries[index].Month != rightEntries[index].Month ||
                    leftEntries[index].WorkKind != rightEntries[index].WorkKind)
                {
                    return false;
                }
            }

            return true;
        }

        private static string FormatWorkKind(KbMaintenanceWorkKind workKind) => workKind switch
        {
            KbMaintenanceWorkKind.To1 => "ТО1",
            KbMaintenanceWorkKind.To2 => "ТО2",
            KbMaintenanceWorkKind.To3 => "ТО3",
            _ => string.Empty
        };

        private static bool TryParseWorkKind(string value, out KbMaintenanceWorkKind workKind)
        {
            string normalized = new string(
                value.Trim()
                    .ToUpperInvariant()
                    .Where(static symbol => char.IsLetterOrDigit(symbol))
                    .ToArray());

            workKind = normalized switch
            {
                "ТО1" or "TO1" or "1" => KbMaintenanceWorkKind.To1,
                "ТО2" or "TO2" or "2" => KbMaintenanceWorkKind.To2,
                "ТО3" or "TO3" or "3" => KbMaintenanceWorkKind.To3,
                _ => KbMaintenanceWorkKind.To1
            };

            return normalized is "ТО1" or "TO1" or "1" or "ТО2" or "TO2" or "2" or "ТО3" or "TO3" or "3";
        }

        private static string GetColumnName(int columnIndex)
        {
            var name = string.Empty;
            int index = columnIndex;
            while (index > 0)
            {
                int remainder = (index - 1) % 26;
                name = (char)('A' + remainder) + name;
                index = (index - remainder - 1) / 26;
            }

            return name;
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

            return index;
        }

        private static KnowledgeBaseMaintenanceYearScheduleSourceImportResult ImportFailure(string errorMessage) =>
            new()
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };

        private sealed record OwnerNodeContext(
            string OwnerNodeId,
            string NodeName,
            string InventoryNumber,
            string Path,
            int TreeOrder);

        private sealed record YearScheduleSourceExportRow(
            string OwnerNodeId,
            string Path,
            string NodeName,
            string InventoryNumber,
            bool IsIncludedInSchedule,
            List<KbMaintenanceYearScheduleEntry> YearScheduleEntries,
            int TreeOrder)
        {
            public bool HasManualSchedule => YearScheduleEntries.Count > 0;
        }

        private sealed record ImportedYearScheduleSourceRow(
            uint RowNumber,
            string OwnerNodeId,
            IReadOnlyDictionary<int, string> MonthValues);
    }
}
