using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using AsutpKnowledgeBase.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace AsutpKnowledgeBase.Services
{
    public sealed class KnowledgeBaseMaintenanceScheduleNormImportResult
    {
        public bool IsSuccess { get; init; }

        public string ErrorMessage { get; init; } = string.Empty;

        public List<KbMaintenanceScheduleProfile> MaintenanceScheduleProfiles { get; init; } = new();

        public int ImportedEquipmentCount { get; init; }

        public int CreatedProfileCount { get; init; }

        public int UpdatedProfileCount { get; init; }

        public int UnchangedProfileCount { get; init; }

        public int MatchedByInventoryCount { get; init; }

        public int MatchedByNameCount { get; init; }

        public List<string> UnresolvedEntries { get; init; } = new();
    }

    public sealed class KnowledgeBaseMaintenanceScheduleNormImportService
    {
        private const int FirstDataRowIndex = 16;
        private const int EquipmentNameColumnIndex = 2;
        private const int EquipmentInventoryColumnIndex = 4;
        private const int PlanFactColumnIndex = 5;
        private const int FirstDayColumnIndex = 6;
        private const int LastDayColumnIndex = 36;
        private static readonly Regex WorkCellRegex = new(
            @"^\s*(ТО[123])\s*/\s*(\d+(?:[.,]\d+)?)\s*$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
        private static readonly Regex MonthSheetNameRegex = new(
            @"\(\s*(?<month>\d{1,2})\s*\)\s*$",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);

        public KnowledgeBaseMaintenanceScheduleNormImportResult ImportWorkbook(
            byte[]? workbookPackage,
            IReadOnlyList<KbNode>? roots,
            IReadOnlyList<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles)
        {
            if (workbookPackage == null || workbookPackage.Length == 0)
                return Failure("Файл Excel с нормами ТО не был передан.");

            try
            {
                List<ImportedNormEntry> importedEntries = ParseWorkbook(workbookPackage);
                if (importedEntries.Count == 0)
                {
                    return Failure(
                        "В книге не найдены строки плана с нормами ТО1/ТО2/ТО3. " +
                        "Ожидается monthly workbook формата 123.xlsx.");
                }

                List<OwnerNodeCandidate> candidates = BuildOwnerNodeCandidates(roots);
                if (candidates.Count == 0)
                    return Failure("В текущем цехе не найдено инженерных узлов для сопоставления норм ТО.");

                List<KbMaintenanceScheduleProfile> updatedProfiles = CloneProfiles(maintenanceScheduleProfiles);
                Dictionary<string, KbMaintenanceScheduleProfile> profilesByOwnerNodeId = updatedProfiles
                    .Where(static profile => !string.IsNullOrWhiteSpace(profile.OwnerNodeId))
                    .ToDictionary(profile => profile.OwnerNodeId, StringComparer.Ordinal);

                var unresolvedEntries = new List<string>();
                int createdProfileCount = 0;
                int updatedProfileCount = 0;
                int unchangedProfileCount = 0;
                int matchedByInventoryCount = 0;
                int matchedByNameCount = 0;

                foreach (ImportedNormEntry importedEntry in importedEntries)
                {
                    MatchResolution resolution = ResolveOwnerNode(importedEntry, candidates);
                    if (!resolution.IsResolved || resolution.Candidate == null)
                    {
                        unresolvedEntries.Add(BuildUnresolvedEntryText(importedEntry, resolution.IsAmbiguous));
                        continue;
                    }

                    if (resolution.MatchKind == MatchKind.Inventory)
                        matchedByInventoryCount++;
                    else
                        matchedByNameCount++;

                    string ownerNodeId = resolution.Candidate.OwnerNode.NodeId;
                    if (!profilesByOwnerNodeId.TryGetValue(ownerNodeId, out KbMaintenanceScheduleProfile? existingProfile))
                    {
                        var createdProfile = new KbMaintenanceScheduleProfile
                        {
                            OwnerNodeId = ownerNodeId,
                            IsIncludedInSchedule = true,
                            To1Hours = importedEntry.To1Hours,
                            To2Hours = importedEntry.To2Hours,
                            To3Hours = importedEntry.To3Hours
                        };

                        updatedProfiles.Add(createdProfile);
                        profilesByOwnerNodeId[ownerNodeId] = createdProfile;
                        createdProfileCount++;
                        continue;
                    }

                    bool hasChanges =
                        existingProfile.To1Hours != importedEntry.To1Hours ||
                        existingProfile.To2Hours != importedEntry.To2Hours ||
                        existingProfile.To3Hours != importedEntry.To3Hours;

                    if (!hasChanges)
                    {
                        unchangedProfileCount++;
                        continue;
                    }

                    existingProfile.To1Hours = importedEntry.To1Hours;
                    existingProfile.To2Hours = importedEntry.To2Hours;
                    existingProfile.To3Hours = importedEntry.To3Hours;
                    updatedProfileCount++;
                }

                return new KnowledgeBaseMaintenanceScheduleNormImportResult
                {
                    IsSuccess = true,
                    MaintenanceScheduleProfiles = updatedProfiles,
                    ImportedEquipmentCount = importedEntries.Count,
                    CreatedProfileCount = createdProfileCount,
                    UpdatedProfileCount = updatedProfileCount,
                    UnchangedProfileCount = unchangedProfileCount,
                    MatchedByInventoryCount = matchedByInventoryCount,
                    MatchedByNameCount = matchedByNameCount,
                    UnresolvedEntries = unresolvedEntries
                };
            }
            catch (Exception ex)
            {
                return Failure($"Не удалось импортировать нормы ТО из Excel: {ex.Message}");
            }
        }

        private static List<ImportedNormEntry> ParseWorkbook(byte[] workbookPackage)
        {
            using var stream = new MemoryStream(workbookPackage, writable: false);
            using SpreadsheetDocument document = SpreadsheetDocument.Open(stream, false);

            WorkbookPart workbookPart = document.WorkbookPart
                ?? throw new InvalidOperationException("Файл XLSX не содержит workbook part.");
            List<string> sharedStrings = ReadSharedStrings(workbookPart.SharedStringTablePart).ToList();
            var aggregatedEntries = new Dictionary<string, ImportedNormAccumulator>(StringComparer.Ordinal);

            foreach (Sheet sheet in workbookPart.Workbook.Sheets?.Elements<Sheet>() ?? Enumerable.Empty<Sheet>())
            {
                string sheetName = sheet.Name?.Value?.Trim() ?? string.Empty;
                if (!TryParseMonthSheetName(sheetName, out _))
                    continue;

                string relationshipId = sheet.Id?.Value?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(relationshipId))
                    continue;

                if (workbookPart.GetPartById(relationshipId) is not WorksheetPart worksheetPart)
                    continue;

                ParseWorksheet(sheetName, worksheetPart, sharedStrings, aggregatedEntries);
            }

            return aggregatedEntries.Values
                .Select(static accumulator => accumulator.ToEntry())
                .OrderBy(static entry => entry.SystemName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static entry => entry.EquipmentName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static void ParseWorksheet(
            string sheetName,
            WorksheetPart worksheetPart,
            IReadOnlyList<string> sharedStrings,
            IDictionary<string, ImportedNormAccumulator> aggregatedEntries)
        {
            SheetData sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>()
                ?? throw new InvalidOperationException($"Лист '{sheetName}' не содержит sheetData.");

            string currentSystemName = string.Empty;
            string currentSystemInventory = string.Empty;

            foreach (Row row in sheetData.Elements<Row>())
            {
                uint rowIndex = row.RowIndex?.Value ?? 0;
                if (rowIndex < FirstDataRowIndex)
                    continue;

                Dictionary<int, string> values = ReadRowValues(row, sharedStrings);
                if (values.Count == 0)
                    continue;

                if (TryParsePlanRow(sheetName, rowIndex, values, currentSystemName, currentSystemInventory, out ImportedNormEntry? importedEntry) &&
                    importedEntry != null)
                {
                    string aggregateKey = BuildAggregateKey(importedEntry);
                    if (!aggregatedEntries.TryGetValue(aggregateKey, out ImportedNormAccumulator? accumulator))
                    {
                        accumulator = ImportedNormAccumulator.Create(importedEntry);
                        aggregatedEntries[aggregateKey] = accumulator;
                    }
                    else
                    {
                        accumulator.Absorb(importedEntry);
                    }

                    continue;
                }

                if (TryParseSystemHeaderRow(values, out string systemName, out string systemInventory))
                {
                    currentSystemName = systemName;
                    currentSystemInventory = systemInventory;
                }
            }
        }

        private static bool TryParsePlanRow(
            string sheetName,
            uint rowIndex,
            IReadOnlyDictionary<int, string> values,
            string currentSystemName,
            string currentSystemInventory,
            out ImportedNormEntry? importedEntry)
        {
            importedEntry = null;

            string planFactValue = GetCellValue(values, PlanFactColumnIndex);
            if (!string.Equals(planFactValue.Trim(), "план", StringComparison.OrdinalIgnoreCase))
                return false;

            string equipmentName = GetCellValue(values, EquipmentNameColumnIndex).Trim();
            if (string.IsNullOrWhiteSpace(equipmentName))
                return false;

            int to1Hours = 0;
            int to2Hours = 0;
            int to3Hours = 0;

            for (int columnIndex = FirstDayColumnIndex; columnIndex <= LastDayColumnIndex; columnIndex++)
            {
                string cellValue = GetCellValue(values, columnIndex);
                if (!TryParseWorkCell(cellValue, out KbMaintenanceWorkKind workKind, out int hours))
                    continue;

                switch (workKind)
                {
                    case KbMaintenanceWorkKind.To1:
                        to1Hours += hours;
                        break;
                    case KbMaintenanceWorkKind.To2:
                        to2Hours += hours;
                        break;
                    case KbMaintenanceWorkKind.To3:
                        to3Hours += hours;
                        break;
                }
            }

            if (to1Hours <= 0 && to2Hours <= 0 && to3Hours <= 0)
                return false;

            importedEntry = new ImportedNormEntry(
                sheetName,
                rowIndex,
                equipmentName,
                GetCellValue(values, EquipmentInventoryColumnIndex).Trim(),
                currentSystemName.Trim(),
                currentSystemInventory.Trim(),
                to1Hours,
                to2Hours,
                to3Hours);
            return true;
        }

        private static bool TryParseSystemHeaderRow(
            IReadOnlyDictionary<int, string> values,
            out string systemName,
            out string systemInventory)
        {
            systemName = string.Empty;
            systemInventory = string.Empty;

            string planFactValue = GetCellValue(values, PlanFactColumnIndex).Trim();
            if (!string.IsNullOrWhiteSpace(planFactValue))
                return false;

            bool containsWorkMarkers = false;
            for (int columnIndex = FirstDayColumnIndex; columnIndex <= LastDayColumnIndex; columnIndex++)
            {
                if (TryParseWorkCell(GetCellValue(values, columnIndex), out _, out _))
                {
                    containsWorkMarkers = true;
                    break;
                }
            }

            if (containsWorkMarkers)
                return false;

            string name = GetCellValue(values, EquipmentNameColumnIndex).Trim();
            string numbering = GetCellValue(values, 1).Trim();
            string inventory = GetCellValue(values, EquipmentInventoryColumnIndex).Trim();
            if (string.IsNullOrWhiteSpace(name))
                return false;

            if (string.IsNullOrWhiteSpace(numbering) && string.IsNullOrWhiteSpace(inventory))
                return false;

            systemName = name;
            systemInventory = inventory;
            return true;
        }

        private static bool TryParseWorkCell(
            string? rawValue,
            out KbMaintenanceWorkKind workKind,
            out int hours)
        {
            workKind = KbMaintenanceWorkKind.To1;
            hours = 0;

            string normalizedValue = rawValue?.Trim() ?? string.Empty;
            if (normalizedValue.Length == 0)
                return false;

            Match match = WorkCellRegex.Match(normalizedValue);
            if (!match.Success)
                return false;

            workKind = match.Groups[1].Value.Trim().ToUpperInvariant() switch
            {
                "ТО1" => KbMaintenanceWorkKind.To1,
                "ТО2" => KbMaintenanceWorkKind.To2,
                "ТО3" => KbMaintenanceWorkKind.To3,
                _ => KbMaintenanceWorkKind.To1
            };

            string hoursText = match.Groups[2].Value.Replace(',', '.');
            if (!decimal.TryParse(hoursText, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal parsedHours))
                return false;

            hours = decimal.ToInt32(decimal.Round(parsedHours, MidpointRounding.AwayFromZero));
            return hours > 0;
        }

        private static List<OwnerNodeCandidate> BuildOwnerNodeCandidates(IReadOnlyList<KbNode>? roots)
        {
            var candidates = new List<OwnerNodeCandidate>();
            foreach (KbNode root in roots ?? Array.Empty<KbNode>())
                CollectOwnerNodeCandidates(candidates, root, visibleLevel: 1, parentSystemName: string.Empty, parentSystemInventory: string.Empty);

            return candidates;
        }

        private static void CollectOwnerNodeCandidates(
            ICollection<OwnerNodeCandidate> candidates,
            KbNode node,
            int visibleLevel,
            string parentSystemName,
            string parentSystemInventory)
        {
            int currentVisibleLevel = GetEffectiveVisibleLevel(node, visibleLevel);
            string currentSystemName = parentSystemName;
            string currentSystemInventory = parentSystemInventory;
            if (currentVisibleLevel == 2)
            {
                currentSystemName = node.Name?.Trim() ?? string.Empty;
                currentSystemInventory = node.Details?.InventoryNumber?.Trim() ?? string.Empty;
            }

            if (KnowledgeBaseMaintenanceScheduleStateService.SupportsProfile(node.NodeType, currentVisibleLevel))
            {
                candidates.Add(new OwnerNodeCandidate(
                    node,
                    currentVisibleLevel,
                    node.Name?.Trim() ?? string.Empty,
                    node.Details?.InventoryNumber?.Trim() ?? string.Empty,
                    currentSystemName,
                    currentSystemInventory));
            }

            foreach (KbNode child in node.Children ?? Enumerable.Empty<KbNode>())
            {
                CollectOwnerNodeCandidates(
                    candidates,
                    child,
                    currentVisibleLevel + 1,
                    currentSystemName,
                    currentSystemInventory);
            }
        }

        private static MatchResolution ResolveOwnerNode(
            ImportedNormEntry importedEntry,
            IReadOnlyList<OwnerNodeCandidate> candidates)
        {
            if (importedEntry.EquipmentInventoryKey.Length > 0)
            {
                IEnumerable<OwnerNodeCandidate> scopedByEquipmentInventory = candidates.Where(candidate =>
                    string.Equals(candidate.EquipmentInventoryKey, importedEntry.EquipmentInventoryKey, StringComparison.Ordinal));

                if (importedEntry.SystemInventoryKey.Length > 0)
                {
                    MatchResolution scopedResolution = ResolveUniqueCandidate(
                        scopedByEquipmentInventory.Where(candidate =>
                            string.Equals(candidate.SystemInventoryKey, importedEntry.SystemInventoryKey, StringComparison.Ordinal)),
                        MatchKind.Inventory);
                    if (scopedResolution.IsResolved || scopedResolution.IsAmbiguous)
                        return scopedResolution;
                }

                MatchResolution inventoryResolution = ResolveUniqueCandidate(scopedByEquipmentInventory, MatchKind.Inventory);
                if (inventoryResolution.IsResolved || inventoryResolution.IsAmbiguous)
                    return inventoryResolution;
            }

            if (importedEntry.EquipmentNameKey.Length == 0)
                return MatchResolution.NotFound;

            if (importedEntry.SystemInventoryKey.Length > 0)
            {
                MatchResolution systemInventoryResolution = ResolveUniqueCandidate(
                    candidates.Where(candidate =>
                        string.Equals(candidate.SystemInventoryKey, importedEntry.SystemInventoryKey, StringComparison.Ordinal) &&
                        HasMatchingNameKey(candidate.EquipmentNameKeys, importedEntry.EquipmentNameKeys)),
                    MatchKind.Name);
                if (systemInventoryResolution.IsResolved || systemInventoryResolution.IsAmbiguous)
                    return systemInventoryResolution;
            }

            if (importedEntry.SystemNameKey.Length > 0)
            {
                MatchResolution systemNameResolution = ResolveUniqueCandidate(
                    candidates.Where(candidate =>
                        HasMatchingNameKey(candidate.SystemNameKeys, importedEntry.SystemNameKeys) &&
                        HasMatchingNameKey(candidate.EquipmentNameKeys, importedEntry.EquipmentNameKeys)),
                    MatchKind.Name);
                if (systemNameResolution.IsResolved || systemNameResolution.IsAmbiguous)
                    return systemNameResolution;
            }

            return ResolveUniqueCandidate(
                candidates.Where(candidate =>
                    HasMatchingNameKey(candidate.EquipmentNameKeys, importedEntry.EquipmentNameKeys)),
                MatchKind.Name);
        }

        private static MatchResolution ResolveUniqueCandidate(
            IEnumerable<OwnerNodeCandidate> candidates,
            MatchKind matchKind)
        {
            OwnerNodeCandidate[] candidateArray = candidates.Take(2).ToArray();
            return candidateArray.Length switch
            {
                0 => MatchResolution.NotFound,
                1 => new MatchResolution(true, false, matchKind, candidateArray[0]),
                _ => new MatchResolution(false, true, MatchKind.None, null)
            };
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

        private static bool TryParseMonthSheetName(string sheetName, out int month)
        {
            month = 0;
            Match match = MonthSheetNameRegex.Match(sheetName ?? string.Empty);
            if (!match.Success)
                return false;

            return int.TryParse(match.Groups["month"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out month) &&
                   month is >= 1 and <= 12;
        }

        private static string GetCellValue(IReadOnlyDictionary<int, string> values, int columnIndex) =>
            values.TryGetValue(columnIndex, out string? value) ? value ?? string.Empty : string.Empty;

        private static string BuildAggregateKey(ImportedNormEntry entry)
        {
            if (entry.EquipmentInventoryKey.Length > 0)
                return $"eqinv:{entry.EquipmentInventoryKey}";

            if (entry.SystemInventoryKey.Length > 0)
                return $"sysinv:{entry.SystemInventoryKey}|name:{entry.EquipmentNameKey}";

            if (entry.SystemNameKey.Length > 0)
                return $"sys:{entry.SystemNameKey}|name:{entry.EquipmentNameKey}";

            return $"name:{entry.EquipmentNameKey}";
        }

        private static string BuildUnresolvedEntryText(ImportedNormEntry entry, bool isAmbiguous)
        {
            string systemText = entry.SystemName.Length > 0
                ? $"{entry.SystemName} / "
                : string.Empty;
            string inventoryText = entry.EquipmentInventory.Length > 0
                ? $" [инв. {entry.EquipmentInventory}]"
                : entry.SystemInventory.Length > 0
                    ? $" [система {entry.SystemInventory}]"
                    : string.Empty;
            string suffix = isAmbiguous ? " - найдено несколько совпадений" : " - совпадение не найдено";
            return $"{systemText}{entry.EquipmentName}{inventoryText}{suffix}";
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
                         .Where(static entry => entry != null)
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

        private static string NormalizeTextKey(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var builder = new StringBuilder(value.Length);
            bool pendingSeparator = false;
            foreach (char character in value.Trim().ToUpperInvariant())
            {
                if (char.IsLetterOrDigit(character))
                {
                    if (pendingSeparator && builder.Length > 0)
                        builder.Append(' ');

                    builder.Append(character);
                    pendingSeparator = false;
                    continue;
                }

                pendingSeparator = true;
            }

            return builder.ToString();
        }

        private static string NormalizeCompactTextKey(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var builder = new StringBuilder(value.Length);
            foreach (char character in value.Trim().ToUpperInvariant())
            {
                if (char.IsLetterOrDigit(character))
                    builder.Append(character);
            }

            return builder.ToString();
        }

        private static string NormalizeInventoryKey(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var builder = new StringBuilder(value.Length);
            foreach (char character in value.Trim().ToUpperInvariant())
            {
                if (char.IsLetterOrDigit(character))
                    builder.Append(character);
            }

            return builder.ToString();
        }

        private static int GetEffectiveVisibleLevel(KbNode node, int visibleLevel)
        {
            if (node.NodeType == KbNodeType.WorkshopRoot && node.LevelIndex == 0)
                return Math.Max(0, visibleLevel - 1);

            return visibleLevel;
        }

        private static bool HasMatchingNameKey(
            IReadOnlyCollection<string> left,
            IReadOnlyCollection<string> right)
        {
            if (left.Count == 0 || right.Count == 0)
                return false;

            foreach (string leftKey in left)
            {
                if (right.Contains(leftKey))
                    return true;
            }

            return false;
        }

        private static string[] BuildNameMatchKeys(string? value, string? systemContext = null)
        {
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (string variant in ExpandNameVariants(value, systemContext))
            {
                string normalizedKey = NormalizeTextKey(variant);
                if (normalizedKey.Length > 0)
                    keys.Add(normalizedKey);

                string compactKey = NormalizeCompactTextKey(variant);
                if (compactKey.Length > 0)
                    keys.Add(compactKey);
            }

            return keys.ToArray();
        }

        private static IEnumerable<string> ExpandNameVariants(string? value, string? systemContext = null)
        {
            string trimmedValue = value?.Trim() ?? string.Empty;
            if (trimmedValue.Length == 0)
                yield break;

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string variant in ExpandNameVariantsCore(trimmedValue, systemContext))
            {
                string trimmedVariant = variant.Trim();
                if (trimmedVariant.Length == 0 || !seen.Add(trimmedVariant))
                    continue;

                yield return trimmedVariant;
            }
        }

        private static IEnumerable<string> ExpandNameVariantsCore(string value, string? systemContext)
        {
            yield return value;

            foreach (string trimmedBySuffix in TrimTrailingSystemContext(value, systemContext))
                yield return trimmedBySuffix;

            foreach (string trimmedByDots in TrimDotSeparatedSuffixes(value))
                yield return trimmedByDots;

            foreach (string trimmedBySuffix in TrimTrailingSystemContext(value, systemContext))
            {
                foreach (string trimmedByDots in TrimDotSeparatedSuffixes(trimmedBySuffix))
                    yield return trimmedByDots;
            }
        }

        private static IEnumerable<string> TrimDotSeparatedSuffixes(string value)
        {
            string[] segments = value
                .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (segments.Length <= 1)
                yield break;

            for (int count = segments.Length - 1; count >= 1; count--)
                yield return string.Join(" ", segments.Take(count));
        }

        private static IEnumerable<string> TrimTrailingSystemContext(string value, string? systemContext)
        {
            foreach (string suffix in BuildSystemContextSuffixes(systemContext))
            {
                if (TryTrimTrailingContext(value, suffix, out string trimmed))
                    yield return trimmed;
            }
        }

        private static IEnumerable<string> BuildSystemContextSuffixes(string? systemContext)
        {
            string trimmedContext = systemContext?.Trim() ?? string.Empty;
            if (trimmedContext.Length == 0)
                yield break;

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string candidate in ExpandSystemContextSuffixes(trimmedContext))
            {
                string trimmedCandidate = candidate.Trim().Trim('.', ' ', '-', '–', '—', ',', ';', ':');
                if (trimmedCandidate.Length == 0 || !seen.Add(trimmedCandidate))
                    continue;

                yield return trimmedCandidate;
            }
        }

        private static IEnumerable<string> ExpandSystemContextSuffixes(string systemContext)
        {
            yield return systemContext;

            if (systemContext.StartsWith("АСУТП ", StringComparison.OrdinalIgnoreCase))
                yield return systemContext["АСУТП ".Length..];

            if (systemContext.StartsWith("АСУ ", StringComparison.OrdinalIgnoreCase))
                yield return systemContext["АСУ ".Length..];

            if (systemContext.StartsWith("СУ ", StringComparison.OrdinalIgnoreCase))
                yield return systemContext["СУ ".Length..];

            if (systemContext.StartsWith("ЛИНИИ ", StringComparison.OrdinalIgnoreCase))
                yield return "ЛИНИЯ " + systemContext["ЛИНИИ ".Length..];
        }

        private static bool TryTrimTrailingContext(string value, string suffix, out string trimmed)
        {
            trimmed = string.Empty;

            string normalizedValue = value.Trim().Trim('.', ' ', '-', '–', '—', ',', ';', ':');
            string normalizedSuffix = suffix.Trim().Trim('.', ' ', '-', '–', '—', ',', ';', ':');
            if (normalizedValue.Length == 0 || normalizedSuffix.Length == 0)
                return false;

            if (!normalizedValue.EndsWith(normalizedSuffix, StringComparison.OrdinalIgnoreCase))
                return false;

            trimmed = normalizedValue[..^normalizedSuffix.Length]
                .Trim()
                .Trim('.', ' ', '-', '–', '—', ',', ';', ':');
            return trimmed.Length > 0;
        }

        private static KnowledgeBaseMaintenanceScheduleNormImportResult Failure(string errorMessage) =>
            new()
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };

        private sealed record ImportedNormEntry(
            string SheetName,
            uint RowIndex,
            string EquipmentName,
            string EquipmentInventory,
            string SystemName,
            string SystemInventory,
            int To1Hours,
            int To2Hours,
            int To3Hours)
        {
            public string EquipmentNameKey { get; } = NormalizeTextKey(EquipmentName);

            public string[] EquipmentNameKeys { get; } = BuildNameMatchKeys(EquipmentName, SystemName);

            public string EquipmentInventoryKey { get; } = NormalizeInventoryKey(EquipmentInventory);

            public string SystemNameKey { get; } = NormalizeTextKey(SystemName);

            public string[] SystemNameKeys { get; } = BuildNameMatchKeys(SystemName);

            public string SystemInventoryKey { get; } = NormalizeInventoryKey(SystemInventory);
        }

        private sealed class ImportedNormAccumulator
        {
            public ImportedNormAccumulator(ImportedNormEntry source)
            {
                SheetName = source.SheetName;
                RowIndex = source.RowIndex;
                EquipmentName = source.EquipmentName;
                EquipmentInventory = source.EquipmentInventory;
                SystemName = source.SystemName;
                SystemInventory = source.SystemInventory;
                To1Hours = source.To1Hours;
                To2Hours = source.To2Hours;
                To3Hours = source.To3Hours;
            }

            public string SheetName { get; }

            public uint RowIndex { get; }

            public string EquipmentName { get; }

            public string EquipmentInventory { get; }

            public string SystemName { get; }

            public string SystemInventory { get; }

            public int To1Hours { get; private set; }

            public int To2Hours { get; private set; }

            public int To3Hours { get; private set; }

            public static ImportedNormAccumulator Create(ImportedNormEntry entry) => new(entry);

            public void Absorb(ImportedNormEntry entry)
            {
                To1Hours = Math.Max(To1Hours, entry.To1Hours);
                To2Hours = Math.Max(To2Hours, entry.To2Hours);
                To3Hours = Math.Max(To3Hours, entry.To3Hours);
            }

            public ImportedNormEntry ToEntry() => new(
                SheetName,
                RowIndex,
                EquipmentName,
                EquipmentInventory,
                SystemName,
                SystemInventory,
                To1Hours,
                To2Hours,
                To3Hours);
        }

        private sealed record OwnerNodeCandidate(
            KbNode OwnerNode,
            int VisibleLevel,
            string EquipmentName,
            string EquipmentInventory,
            string SystemName,
            string SystemInventory)
        {
            public string EquipmentNameKey { get; } = NormalizeTextKey(EquipmentName);

            public string[] EquipmentNameKeys { get; } = BuildNameMatchKeys(EquipmentName, SystemName);

            public string EquipmentInventoryKey { get; } = NormalizeInventoryKey(EquipmentInventory);

            public string SystemNameKey { get; } = NormalizeTextKey(SystemName);

            public string[] SystemNameKeys { get; } = BuildNameMatchKeys(SystemName);

            public string SystemInventoryKey { get; } = NormalizeInventoryKey(SystemInventory);
        }

        private readonly record struct MatchResolution(
            bool IsResolved,
            bool IsAmbiguous,
            MatchKind MatchKind,
            OwnerNodeCandidate? Candidate)
        {
            public static MatchResolution NotFound => new(false, false, MatchKind.None, null);
        }

        private enum MatchKind
        {
            None,
            Inventory,
            Name
        }
    }
}
