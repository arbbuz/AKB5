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

        public int YearScheduleAppliedProfileCount { get; init; }

        public int DisabledMissingProfileCount { get; init; }

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
        private static readonly string[] AutomationNamePrefixes = { "АСУТП ", "АСУ ТП ", "АСУ ", "СУ " };
        private static readonly Regex WorkCellRegex = new(
            @"^\s*(ТО[123])\s*/\s*(\d+(?:[.,]\d+)?)\s*$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
        private static readonly Regex MonthSheetNameRegex = new(
            @"\(\s*(?<month>\d{1,2})\s*\)\s*$",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);
        private static readonly Regex ParentheticalContentRegex = new(
            @"\((?<value>[^()]*)\)",
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
                        "Ожидается XLSX с годовой структурой норм ТО как в 456.xlsx или " +
                        "помесячной структурой как в 123.xlsx; имя файла не используется.");
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
                int yearScheduleAppliedProfileCount = 0;
                int disabledMissingProfileCount = 0;
                bool isAnnualSource = importedEntries.Any(static entry => entry.YearScheduleEntries.Count > 0);

                var resolvedEntriesByOwnerNodeId = new Dictionary<string, ImportedNormAccumulator>(StringComparer.Ordinal);
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
                    if (!resolvedEntriesByOwnerNodeId.TryGetValue(ownerNodeId, out ImportedNormAccumulator? accumulator))
                    {
                        resolvedEntriesByOwnerNodeId[ownerNodeId] = ImportedNormAccumulator.Create(importedEntry);
                    }
                    else
                    {
                        accumulator.AbsorbResolvedOwnerEntry(importedEntry);
                    }
                }

                foreach ((string ownerNodeId, ImportedNormAccumulator accumulator) in resolvedEntriesByOwnerNodeId)
                {
                    ImportedNormEntry importedEntry = accumulator.ToEntry();
                    if (!profilesByOwnerNodeId.TryGetValue(ownerNodeId, out KbMaintenanceScheduleProfile? existingProfile))
                    {
                        var createdProfile = new KbMaintenanceScheduleProfile
                        {
                            OwnerNodeId = ownerNodeId,
                            IsIncludedInSchedule = true,
                            To1Hours = importedEntry.To1Hours,
                            To2Hours = importedEntry.To2Hours,
                            To3Hours = importedEntry.To3Hours,
                            YearScheduleEntries = CloneYearScheduleEntries(importedEntry.YearScheduleEntries)
                        };

                        updatedProfiles.Add(createdProfile);
                        profilesByOwnerNodeId[ownerNodeId] = createdProfile;
                        createdProfileCount++;
                        if (importedEntry.YearScheduleEntries.Count > 0)
                            yearScheduleAppliedProfileCount++;
                        continue;
                    }

                    bool hasYearScheduleChanges =
                        importedEntry.YearScheduleEntries.Count > 0 &&
                        !YearScheduleEquals(existingProfile.YearScheduleEntries, importedEntry.YearScheduleEntries);
                    bool hasInclusionChanges = isAnnualSource && !existingProfile.IsIncludedInSchedule;
                    bool hasChanges =
                        existingProfile.To1Hours != importedEntry.To1Hours ||
                        existingProfile.To2Hours != importedEntry.To2Hours ||
                        existingProfile.To3Hours != importedEntry.To3Hours ||
                        hasYearScheduleChanges ||
                        hasInclusionChanges;

                    if (!hasChanges)
                    {
                        unchangedProfileCount++;
                        continue;
                    }

                    if (hasInclusionChanges)
                        existingProfile.IsIncludedInSchedule = true;

                    existingProfile.To1Hours = importedEntry.To1Hours;
                    existingProfile.To2Hours = importedEntry.To2Hours;
                    existingProfile.To3Hours = importedEntry.To3Hours;
                    if (hasYearScheduleChanges)
                    {
                        existingProfile.YearScheduleEntries = CloneYearScheduleEntries(importedEntry.YearScheduleEntries);
                        yearScheduleAppliedProfileCount++;
                    }

                    updatedProfileCount++;
                }

                if (isAnnualSource && unresolvedEntries.Count == 0)
                {
                    HashSet<string> currentWorkshopOwnerNodeIds = candidates
                        .Select(static candidate => candidate.OwnerNode.NodeId)
                        .Where(static ownerNodeId => !string.IsNullOrWhiteSpace(ownerNodeId))
                        .ToHashSet(StringComparer.Ordinal);

                    foreach (KbMaintenanceScheduleProfile profile in updatedProfiles)
                    {
                        string ownerNodeId = profile.OwnerNodeId?.Trim() ?? string.Empty;
                        if (profile.IsIncludedInSchedule &&
                            currentWorkshopOwnerNodeIds.Contains(ownerNodeId) &&
                            !resolvedEntriesByOwnerNodeId.ContainsKey(ownerNodeId))
                        {
                            profile.IsIncludedInSchedule = false;
                            disabledMissingProfileCount++;
                        }
                    }
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
                    YearScheduleAppliedProfileCount = yearScheduleAppliedProfileCount,
                    DisabledMissingProfileCount = disabledMissingProfileCount,
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
                string relationshipId = sheet.Id?.Value?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(relationshipId))
                    continue;

                if (workbookPart.GetPartById(relationshipId) is not WorksheetPart worksheetPart)
                    continue;

                ParseAnnualWorksheet(sheetName, worksheetPart, sharedStrings, aggregatedEntries);
            }

            if (aggregatedEntries.Count > 0)
            {
                return aggregatedEntries.Values
                    .Select(static accumulator => accumulator.ToEntry())
                    .OrderBy(static entry => entry.SystemName, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(static entry => entry.EquipmentName, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

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

        private static void ParseAnnualWorksheet(
            string sheetName,
            WorksheetPart worksheetPart,
            IReadOnlyList<string> sharedStrings,
            IDictionary<string, ImportedNormAccumulator> aggregatedEntries)
        {
            SheetData sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>()
                ?? throw new InvalidOperationException($"Лист '{sheetName}' не содержит sheetData.");

            List<Row> rows = sheetData.Elements<Row>().ToList();
            if (!TryFindAnnualPlanColumns(rows, sharedStrings, out uint planHeaderRowIndex, out Dictionary<int, int> monthByPlanColumn))
                return;

            string currentSystemName = string.Empty;
            string currentSystemInventory = string.Empty;

            foreach (Row row in rows)
            {
                uint rowIndex = row.RowIndex?.Value ?? 0;
                if (rowIndex <= planHeaderRowIndex)
                    continue;

                if (IsHiddenRow(row))
                    continue;

                Dictionary<int, string> values = ReadRowValues(row, sharedStrings);
                if (values.Count == 0)
                    continue;

                if (TryParseAnnualPlanRow(
                        sheetName,
                        rowIndex,
                        values,
                        monthByPlanColumn,
                        currentSystemName,
                        currentSystemInventory,
                        out ImportedNormEntry? importedEntry) &&
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

                if (TryParseAnnualSystemHeaderRow(values, monthByPlanColumn.Keys, out string systemName, out string systemInventory))
                {
                    currentSystemName = systemName;
                    currentSystemInventory = systemInventory;
                }
            }
        }

        private static bool TryFindAnnualPlanColumns(
            IReadOnlyList<Row> rows,
            IReadOnlyList<string> sharedStrings,
            out uint planHeaderRowIndex,
            out Dictionary<int, int> monthByPlanColumn)
        {
            planHeaderRowIndex = 0;
            monthByPlanColumn = new Dictionary<int, int>();

            foreach (Row row in rows)
            {
                Dictionary<int, string> values = ReadRowValues(row, sharedStrings);
                int[] planColumns = values
                    .Where(static pair => string.Equals(pair.Value.Trim(), "план", StringComparison.OrdinalIgnoreCase))
                    .Select(static pair => pair.Key)
                    .Order()
                    .ToArray();

                if (planColumns.Length < 12)
                    continue;

                int month = 1;
                foreach (int columnIndex in planColumns.Take(12))
                    monthByPlanColumn[columnIndex] = month++;

                planHeaderRowIndex = row.RowIndex?.Value ?? 0;
                return true;
            }

            return false;
        }

        private static bool IsHiddenRow(Row row) => row.Hidden?.Value == true;

        private static bool TryParseAnnualPlanRow(
            string sheetName,
            uint rowIndex,
            IReadOnlyDictionary<int, string> values,
            IReadOnlyDictionary<int, int> monthByPlanColumn,
            string currentSystemName,
            string currentSystemInventory,
            out ImportedNormEntry? importedEntry)
        {
            importedEntry = null;

            string equipmentName = GetCellValue(values, EquipmentNameColumnIndex).Trim();
            if (string.IsNullOrWhiteSpace(equipmentName) ||
                string.Equals(equipmentName, "2", StringComparison.OrdinalIgnoreCase) ||
                equipmentName.Contains("Наименование", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            int to1Hours = 0;
            int to2Hours = 0;
            int to3Hours = 0;
            var yearScheduleEntries = new List<KbMaintenanceYearScheduleEntry>();
            var monthWorkEntries = new List<ImportedNormMonthWorkEntry>();

            foreach ((int columnIndex, int month) in monthByPlanColumn.OrderBy(static pair => pair.Value))
            {
                string cellValue = GetCellValue(values, columnIndex);
                if (!TryParseWorkCell(cellValue, out KbMaintenanceWorkKind workKind, out int hours))
                    continue;

                switch (workKind)
                {
                    case KbMaintenanceWorkKind.To1:
                        to1Hours = Math.Max(to1Hours, hours);
                        break;
                    case KbMaintenanceWorkKind.To2:
                        to2Hours = Math.Max(to2Hours, hours);
                        break;
                    case KbMaintenanceWorkKind.To3:
                        to3Hours = Math.Max(to3Hours, hours);
                        break;
                }

                yearScheduleEntries.Add(new KbMaintenanceYearScheduleEntry
                {
                    Month = month,
                    WorkKind = workKind,
                    Hours = hours
                });
                monthWorkEntries.Add(new ImportedNormMonthWorkEntry(month, workKind, hours));
            }

            if (to1Hours <= 0 && to2Hours <= 0 && to3Hours <= 0)
                return false;

            importedEntry = new ImportedNormEntry(
                sheetName,
                rowIndex,
                equipmentName.Trim().TrimEnd('.'),
                GetCellValue(values, EquipmentInventoryColumnIndex).Trim(),
                currentSystemName.Trim(),
                currentSystemInventory.Trim(),
                to1Hours,
                to2Hours,
                to3Hours,
                monthWorkEntries,
                yearScheduleEntries);
            return true;
        }

        private static bool TryParseAnnualSystemHeaderRow(
            IReadOnlyDictionary<int, string> values,
            IEnumerable<int> planColumns,
            out string systemName,
            out string systemInventory)
        {
            systemName = string.Empty;
            systemInventory = string.Empty;

            foreach (int columnIndex in planColumns)
            {
                if (TryParseWorkCell(GetCellValue(values, columnIndex), out _, out _))
                    return false;
            }

            string name = GetCellValue(values, EquipmentNameColumnIndex).Trim();
            string numbering = GetCellValue(values, 1).Trim();
            string inventory = GetCellValue(values, EquipmentInventoryColumnIndex).Trim();
            if (string.IsNullOrWhiteSpace(name))
                return false;

            if (string.IsNullOrWhiteSpace(numbering) && string.IsNullOrWhiteSpace(inventory))
                return false;

            if (string.Equals(name, "2", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Наименование", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            systemName = name;
            systemInventory = inventory;
            return true;
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
                to3Hours,
                new List<ImportedNormMonthWorkEntry>(),
                new List<KbMaintenanceYearScheduleEntry>());
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
                    HasMatchingKey(candidate.EquipmentInventoryKeys, importedEntry.EquipmentInventoryKeys));

                if (importedEntry.SystemInventoryKey.Length > 0)
                {
                    MatchResolution scopedResolution = ResolveUniqueCandidate(
                        scopedByEquipmentInventory.Where(candidate =>
                            HasMatchingKey(candidate.SystemInventoryKeys, importedEntry.SystemInventoryKeys)),
                        MatchKind.Inventory);
                    if (scopedResolution.IsResolved || scopedResolution.IsAmbiguous)
                        return scopedResolution;
                }

                MatchResolution inventoryResolution = ResolveUniqueCandidate(scopedByEquipmentInventory, MatchKind.Inventory);
                if (inventoryResolution.IsResolved || inventoryResolution.IsAmbiguous)
                    return inventoryResolution;

                MatchResolution scopedSystemOwnerResolution = ResolveUniqueCandidate(
                    candidates.Where(candidate =>
                        HasMatchingKey(candidate.SystemInventoryKeys, importedEntry.EquipmentInventoryKeys) &&
                        HasMatchingNameKey(candidate.SystemNameKeys, importedEntry.EquipmentNameKeys)),
                    MatchKind.Inventory);
                if (scopedSystemOwnerResolution.IsResolved || scopedSystemOwnerResolution.IsAmbiguous)
                    return scopedSystemOwnerResolution;

                MatchResolution singleSystemOwnerResolution = ResolveUniqueCandidate(
                    candidates.Where(candidate =>
                        HasMatchingKey(candidate.SystemInventoryKeys, importedEntry.EquipmentInventoryKeys)),
                    MatchKind.Inventory);
                if (singleSystemOwnerResolution.IsResolved || singleSystemOwnerResolution.IsAmbiguous)
                    return singleSystemOwnerResolution;
            }

            if (importedEntry.EquipmentInventoryKey.Length > 0)
            {
                MatchResolution systemOwnerByNameResolution = ResolveUniqueCandidate(
                    candidates.Where(candidate =>
                        HasMatchingNameKey(candidate.SystemNameKeys, importedEntry.EquipmentNameKeys)),
                    MatchKind.Name);
                if (systemOwnerByNameResolution.IsResolved || systemOwnerByNameResolution.IsAmbiguous)
                    return systemOwnerByNameResolution;
            }

            if (importedEntry.EquipmentNameKey.Length == 0)
                return MatchResolution.NotFound;

            if (importedEntry.SystemInventoryKey.Length > 0)
            {
                OwnerNodeCandidate[] systemInventoryCandidates = candidates
                    .Where(candidate => HasMatchingKey(candidate.SystemInventoryKeys, importedEntry.SystemInventoryKeys))
                    .ToArray();
                if (importedEntry.SystemNameKey.Length > 0)
                {
                    MatchResolution scopedSystemInventoryResolution = ResolveBestSystemScopedNameCandidate(
                        systemInventoryCandidates,
                        importedEntry);
                    if (scopedSystemInventoryResolution.IsResolved || scopedSystemInventoryResolution.IsAmbiguous)
                        return scopedSystemInventoryResolution;
                }

                if (importedEntry.SystemNameKey.Length == 0)
                {
                    MatchResolution systemInventoryResolution = ResolveBestNameCandidate(
                        systemInventoryCandidates,
                        importedEntry);
                    if (systemInventoryResolution.IsResolved || systemInventoryResolution.IsAmbiguous)
                        return systemInventoryResolution;
                }
            }

            if (importedEntry.SystemNameKey.Length > 0)
            {
                MatchResolution systemNameResolution = ResolveBestSystemScopedNameCandidate(
                    candidates,
                    importedEntry);
                if (systemNameResolution.IsResolved || systemNameResolution.IsAmbiguous)
                    return systemNameResolution;
            }

            return ResolveBestNameCandidate(candidates, importedEntry);
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

        private static MatchResolution ResolveBestNameCandidate(
            IEnumerable<OwnerNodeCandidate> candidates,
            ImportedNormEntry importedEntry)
        {
            var matchedCandidates = candidates
                .Select(candidate => new
                {
                    Candidate = candidate,
                    Score = GetEquipmentNameMatchScore(importedEntry, candidate)
                })
                .Where(static match => match.Score > 0)
                .ToArray();
            if (matchedCandidates.Length == 0)
                return MatchResolution.NotFound;

            int bestScore = matchedCandidates.Max(static match => match.Score);
            OwnerNodeCandidate[] bestCandidates = matchedCandidates
                .Where(match => match.Score == bestScore)
                .Select(static match => match.Candidate)
                .Take(2)
                .ToArray();

            return bestCandidates.Length switch
            {
                1 => new MatchResolution(true, false, MatchKind.Name, bestCandidates[0]),
                _ => new MatchResolution(false, true, MatchKind.None, null)
            };
        }

        private static int GetEquipmentNameMatchScore(
            ImportedNormEntry importedEntry,
            OwnerNodeCandidate candidate)
        {
            if (importedEntry.EquipmentNameKey.Length == 0 || candidate.EquipmentNameKey.Length == 0)
                return 0;

            if (string.Equals(importedEntry.EquipmentNameKey, candidate.EquipmentNameKey, StringComparison.Ordinal))
                return 400;

            if (candidate.EquipmentNameKeys.Contains(importedEntry.EquipmentNameKey, StringComparer.Ordinal))
                return 300 + Math.Min(importedEntry.EquipmentNameKey.Length, 50);

            if (importedEntry.EquipmentNameKeys.Contains(candidate.EquipmentNameKey, StringComparer.Ordinal))
                return 200 + Math.Min(candidate.EquipmentNameKey.Length, 50);

            return HasMatchingNameKey(candidate.EquipmentNameKeys, importedEntry.EquipmentNameKeys) ? 100 : 0;
        }

        private static MatchResolution ResolveBestSystemScopedNameCandidate(
            IEnumerable<OwnerNodeCandidate> candidates,
            ImportedNormEntry importedEntry)
        {
            var matchedCandidates = candidates
                .Select(candidate => new
                {
                    Candidate = candidate,
                    SystemScore = GetSystemNameMatchScore(importedEntry, candidate),
                    EquipmentScore = GetEquipmentNameMatchScore(importedEntry, candidate)
                })
                .Where(static match => match.SystemScore > 0 && match.EquipmentScore > 0)
                .ToArray();
            if (matchedCandidates.Length == 0)
                return MatchResolution.NotFound;

            int bestSystemScore = matchedCandidates.Max(static match => match.SystemScore);
            int bestEquipmentScore = matchedCandidates
                .Where(match => match.SystemScore == bestSystemScore)
                .Max(static match => match.EquipmentScore);
            OwnerNodeCandidate[] bestCandidates = matchedCandidates
                .Where(match => match.SystemScore == bestSystemScore && match.EquipmentScore == bestEquipmentScore)
                .Select(static match => match.Candidate)
                .Take(2)
                .ToArray();

            return bestCandidates.Length switch
            {
                1 => new MatchResolution(true, false, MatchKind.Name, bestCandidates[0]),
                _ => new MatchResolution(false, true, MatchKind.None, null)
            };
        }

        private static int GetSystemNameMatchScore(
            ImportedNormEntry importedEntry,
            OwnerNodeCandidate candidate)
        {
            if (importedEntry.SystemNameKey.Length == 0 || candidate.SystemNameKey.Length == 0)
                return 0;

            if (string.Equals(importedEntry.SystemNameKey, candidate.SystemNameKey, StringComparison.Ordinal))
                return 400;

            if (candidate.SystemNameKeys.Contains(importedEntry.SystemNameKey, StringComparer.Ordinal))
                return 300 + Math.Min(importedEntry.SystemNameKey.Length, 50);

            if (importedEntry.SystemNameKeys.Contains(candidate.SystemNameKey, StringComparer.Ordinal))
                return 200 + Math.Min(candidate.SystemNameKey.Length, 50);

            if (HasMatchingNameKey(candidate.SystemNameKeys, importedEntry.SystemNameKeys))
                return 100;

            return GetSystemTokenSubsetMatchScore(importedEntry.SystemNameKeys, candidate.SystemNameKeys);
        }

        private static int GetSystemTokenSubsetMatchScore(
            IReadOnlyCollection<string> importedKeys,
            IReadOnlyCollection<string> candidateKeys)
        {
            foreach (string importedKey in importedKeys)
            {
                HashSet<string> importedTokens = BuildSignificantSystemTokens(importedKey);
                if (importedTokens.Count < 2)
                    continue;

                foreach (string candidateKey in candidateKeys)
                {
                    HashSet<string> candidateTokens = BuildSignificantSystemTokens(candidateKey);
                    if (candidateTokens.Count < 2)
                        continue;

                    if (candidateTokens.All(importedTokens.Contains))
                        return 150 + Math.Min(candidateTokens.Count, 30);

                    if (importedTokens.All(candidateTokens.Contains))
                        return 140 + Math.Min(importedTokens.Count, 30);
                }
            }

            return 0;
        }

        private static HashSet<string> BuildSignificantSystemTokens(string key)
        {
            var tokens = new HashSet<string>(StringComparer.Ordinal);
            foreach (string token in key.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (token is "АСУ" or "АСУТП" or "СУ" or "СИСТЕМА" or "СИСТЕМЫ")
                    continue;

                tokens.Add(token);
            }

            return tokens;
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
                return $"eqinv:{entry.EquipmentInventoryCanonicalKey}";

            if (entry.SystemInventoryKey.Length > 0)
            {
                string systemNamePart = entry.SystemNameKey.Length > 0
                    ? $"|sys:{entry.SystemNameKey}"
                    : string.Empty;
                return $"sysinv:{entry.SystemInventoryKey}{systemNamePart}|name:{entry.EquipmentNameKey}";
            }

            if (entry.SystemNameKey.Length > 0)
                return $"sys:{entry.SystemNameKey}|name:{entry.EquipmentNameKey}";

            return $"name:{entry.EquipmentNameKey}";
        }

        private static string BuildUnresolvedEntryText(ImportedNormEntry entry, bool isAmbiguous)
        {
            string rowText = entry.SheetName.Length > 0
                ? $"{entry.SheetName}, строка {entry.RowIndex}: "
                : string.Empty;
            string systemText = entry.SystemName.Length > 0
                ? $"{entry.SystemName} / "
                : string.Empty;
            string inventoryText = entry.EquipmentInventory.Length > 0
                ? $" [инв. {entry.EquipmentInventory}]"
                : entry.SystemInventory.Length > 0
                    ? $" [система {entry.SystemInventory}]"
                    : string.Empty;
            string suffix = isAmbiguous ? " - найдено несколько совпадений" : " - совпадение не найдено";
            return $"{rowText}{systemText}{entry.EquipmentName}{inventoryText}{suffix}";
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
                    WorkKind = entry.WorkKind,
                    Hours = entry.Hours
                });
            }

            return clones;
        }

        private static List<ImportedNormMonthWorkEntry> CloneMonthWorkEntries(
            IReadOnlyList<ImportedNormMonthWorkEntry>? entries)
        {
            var clones = new List<ImportedNormMonthWorkEntry>();
            if (entries == null)
                return clones;

            foreach (ImportedNormMonthWorkEntry entry in entries)
                clones.Add(entry);

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

            for (int i = 0; i < leftEntries.Count; i++)
            {
                if (leftEntries[i].Month != rightEntries[i].Month ||
                    leftEntries[i].WorkKind != rightEntries[i].WorkKind ||
                    leftEntries[i].Hours != rightEntries[i].Hours)
                {
                    return false;
                }
            }

            return true;
        }

        private static string NormalizeTextKey(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var tokens = new List<string>();
            var builder = new StringBuilder(value.Length);
            bool pendingSeparator = false;
            foreach (char sourceCharacter in value.Trim())
            {
                char character = NormalizeComparableCharacter(sourceCharacter);
                if (char.IsLetterOrDigit(character))
                {
                    if (pendingSeparator && builder.Length > 0)
                    {
                        tokens.Add(NormalizeComparableToken(builder.ToString()));
                        builder.Clear();
                    }

                    builder.Append(character);
                    pendingSeparator = false;
                    continue;
                }

                pendingSeparator = true;
            }

            if (builder.Length > 0)
                tokens.Add(NormalizeComparableToken(builder.ToString()));

            return string.Join(" ", tokens.Where(static token => token.Length > 0));
        }

        private static string NormalizeCompactTextKey(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var tokens = new List<string>();
            var builder = new StringBuilder(value.Length);
            foreach (char sourceCharacter in value.Trim())
            {
                char character = NormalizeComparableCharacter(sourceCharacter);
                if (char.IsLetterOrDigit(character))
                {
                    builder.Append(character);
                    continue;
                }

                if (builder.Length > 0)
                {
                    tokens.Add(NormalizeComparableToken(builder.ToString()));
                    builder.Clear();
                }
            }

            if (builder.Length > 0)
                tokens.Add(NormalizeComparableToken(builder.ToString()));

            return string.Concat(tokens);
        }

        private static string NormalizeInventoryKey(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var builder = new StringBuilder(value.Length);
            foreach (char sourceCharacter in value.Trim())
            {
                char character = NormalizeComparableCharacter(sourceCharacter);
                if (char.IsLetterOrDigit(character))
                    builder.Append(character);
            }

            return builder.ToString();
        }

        private static char NormalizeComparableCharacter(char character)
        {
            char upper = char.ToUpperInvariant(character);
            return upper == 'Ё' ? 'Е' : upper;
        }

        private static string NormalizeComparableToken(string token)
        {
            return token switch
            {
                "ПРЕССА" or "ПРЕССОМ" or "ПРЕССАМИ" => "ПРЕСС",
                "ФП" => "ФИЛЬТРПРЕСС",
                "СТАДИИ" or "СТАДИЕЙ" => "СТАДИЯ",
                "Й" or "ЕЙ" => "Я",
                "В" => string.Empty,
                _ => token
            };
        }

        private static string NormalizeInventoryAggregateKey(string? value)
        {
            string key = NormalizeInventoryKey(value);
            return NormalizeNumericInventoryKey(key);
        }

        private static string[] BuildInventoryMatchKeys(string? value)
        {
            string key = NormalizeInventoryKey(value);
            if (key.Length == 0)
                return Array.Empty<string>();

            var keys = new HashSet<string>(StringComparer.Ordinal) { key };
            string numericKey = NormalizeNumericInventoryKey(key);
            if (numericKey.Length > 0)
                keys.Add(numericKey);

            return keys.ToArray();
        }

        private static string NormalizeNumericInventoryKey(string key)
        {
            if (key.Length == 0 || key.Any(static character => !char.IsDigit(character)))
                return key;

            string trimmed = key.TrimStart('0');
            return trimmed.Length == 0 ? "0" : trimmed;
        }

        private static int GetEffectiveVisibleLevel(KbNode node, int visibleLevel)
        {
            if (node.NodeType == KbNodeType.WorkshopRoot && node.LevelIndex == 0)
                return Math.Max(0, visibleLevel - 1);

            return visibleLevel;
        }

        private static bool HasMatchingNameKey(
            IReadOnlyCollection<string> left,
            IReadOnlyCollection<string> right) =>
            HasMatchingKey(left, right);

        private static bool HasMatchingKey(
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
            foreach (string baseVariant in ExpandStructuralNameVariants(value))
            {
                foreach (string variant in ExpandContextualNameVariants(baseVariant, systemContext))
                    yield return variant;
            }
        }

        private static IEnumerable<string> ExpandContextualNameVariants(string value, string? systemContext)
        {
            yield return value;

            foreach (string trimmedByModelCode in TrimTrailingLatinModelCodes(value))
                yield return trimmedByModelCode;

            foreach (string trimmedBySuffix in TrimTrailingSystemContext(value, systemContext))
            {
                yield return trimmedBySuffix;

                foreach (string trimmedByModelCode in TrimTrailingLatinModelCodes(trimmedBySuffix))
                    yield return trimmedByModelCode;
            }

            foreach (string trimmedByDots in TrimDotSeparatedSuffixes(value))
            {
                yield return trimmedByDots;

                foreach (string trimmedByModelCode in TrimTrailingLatinModelCodes(trimmedByDots))
                    yield return trimmedByModelCode;
            }

            foreach (string trimmedBySuffix in TrimTrailingSystemContext(value, systemContext))
            {
                foreach (string trimmedByDots in TrimDotSeparatedSuffixes(trimmedBySuffix))
                {
                    yield return trimmedByDots;

                    foreach (string trimmedByModelCode in TrimTrailingLatinModelCodes(trimmedByDots))
                        yield return trimmedByModelCode;
                }
            }
        }

        private static IEnumerable<string> ExpandStructuralNameVariants(string value)
        {
            yield return value;

            foreach (string leadingAutomationVariant in ExpandLeadingAutomationVariants(value))
                yield return leadingAutomationVariant;

            if (TryStripLeadingAutomationPrefix(value, out string strippedPrefix))
                yield return strippedPrefix;

            string withoutParentheses = ParentheticalContentRegex
                .Replace(value, " ")
                .Trim();
            if (withoutParentheses.Length > 0 && !string.Equals(withoutParentheses, value, StringComparison.Ordinal))
            {
                yield return withoutParentheses;
                foreach (string leadingAutomationVariant in ExpandLeadingAutomationVariants(withoutParentheses))
                    yield return leadingAutomationVariant;

                if (TryStripLeadingAutomationPrefix(withoutParentheses, out string strippedWithoutParenthesesPrefix))
                    yield return strippedWithoutParenthesesPrefix;
            }

            foreach (Match match in ParentheticalContentRegex.Matches(value))
            {
                string parentheticalValue = match.Groups["value"].Value.Trim();
                if (parentheticalValue.Length == 0)
                    continue;

                yield return parentheticalValue;
                if (TryStripLeadingAutomationPrefix(parentheticalValue, out string strippedParentheticalPrefix))
                    yield return strippedParentheticalPrefix;
            }

            foreach (string trimmedByEmbeddedContext in TrimEmbeddedAutomationContext(value))
                yield return trimmedByEmbeddedContext;

            foreach (string withoutInlineAutomation in RemoveInlineAutomationWords(value))
                yield return withoutInlineAutomation;

            foreach (string withoutKnownContextPhrase in RemoveKnownContextPhrases(value))
                yield return withoutKnownContextPhrase;

            if (TryTrimTrailingHyphenNumber(value, out string withoutTrailingHyphenNumber))
                yield return withoutTrailingHyphenNumber;
        }

        private static IEnumerable<string> ExpandLeadingAutomationVariants(string value)
        {
            string trimmed = value.Trim();
            if (trimmed.StartsWith("АСУТП ", StringComparison.OrdinalIgnoreCase))
            {
                string tail = trimmed["АСУТП ".Length..].Trim();
                if (tail.Length > 0)
                    yield return "АСУ " + tail;
            }

            if (trimmed.StartsWith("Системы ", StringComparison.OrdinalIgnoreCase))
            {
                string tail = trimmed["Системы ".Length..].Trim();
                if (tail.Length > 0)
                    yield return "АСУ " + tail;
            }
        }

        private static bool TryStripLeadingAutomationPrefix(string value, out string stripped)
        {
            stripped = string.Empty;
            string trimmed = value.Trim();
            foreach (string prefix in AutomationNamePrefixes)
            {
                if (!trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    continue;

                stripped = trimmed[prefix.Length..].Trim();
                return stripped.Length > 0;
            }

            return false;
        }

        private static IEnumerable<string> TrimDotSeparatedSuffixes(string value)
        {
            string[] segments = value
                .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (segments.Length <= 1)
                yield break;

            for (int count = segments.Length - 1; count >= 1; count--)
                yield return string.Join(" ", segments.Take(count));

            for (int startIndex = 1; startIndex < segments.Length; startIndex++)
                yield return string.Join(" ", segments.Skip(startIndex));
        }

        private static IEnumerable<string> TrimEmbeddedAutomationContext(string value)
        {
            string[] markers =
            {
                " АСУТП ",
                " АСУ ",
                " СУ "
            };

            foreach (string marker in markers)
            {
                int markerIndex = value.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                if (markerIndex <= 0)
                    continue;

                string trimmed = value[..markerIndex].Trim();
                if (IsUsefulTrimmedNameVariant(trimmed))
                {
                    yield return trimmed;
                    if (TryTrimTrailingHyphenNumber(trimmed, out string withoutTrailingHyphenNumber))
                        yield return withoutTrailingHyphenNumber;
                }
            }
        }

        private static bool IsUsefulTrimmedNameVariant(string value)
        {
            string key = NormalizeTextKey(value);
            return key.Length > 1 && key is not "АСУ" and not "АСУТП" and not "СУ";
        }

        private static IEnumerable<string> RemoveInlineAutomationWords(string value)
        {
            string[] phrases =
            {
                " АСУ ТП ",
                " АСУТП ",
                " АСУ "
            };

            string padded = $" {value.Trim()} ";
            foreach (string phrase in phrases)
            {
                if (!padded.Contains(phrase, StringComparison.OrdinalIgnoreCase))
                    continue;

                string normalized = padded.Replace(phrase, " ", StringComparison.OrdinalIgnoreCase).Trim();
                if (normalized.Length > 0 && !string.Equals(normalized, value.Trim(), StringComparison.Ordinal))
                    yield return normalized;
            }
        }

        private static IEnumerable<string> RemoveKnownContextPhrases(string value)
        {
            string[] phrases =
            {
                " участка антисептика",
                " участок антисептика"
            };

            foreach (string phrase in phrases)
            {
                if (!value.Contains(phrase, StringComparison.OrdinalIgnoreCase))
                    continue;

                string normalized = value.Replace(phrase, string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
                if (normalized.Length > 0 && !string.Equals(normalized, value.Trim(), StringComparison.Ordinal))
                    yield return normalized;
            }
        }

        private static bool TryTrimTrailingHyphenNumber(string value, out string trimmed)
        {
            trimmed = string.Empty;
            string normalized = value.Trim();
            int hyphenIndex = normalized.LastIndexOf('-');
            if (hyphenIndex <= 0 || hyphenIndex == normalized.Length - 1)
                return false;

            if (normalized[(hyphenIndex + 1)..].Any(static character => !char.IsDigit(character)))
                return false;

            trimmed = normalized[..hyphenIndex].Trim();
            return trimmed.Length > 0;
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
            foreach (string candidate in ExpandSystemContextBaseSuffixes(systemContext))
            {
                yield return candidate;

                if (candidate.StartsWith("ЛИНИИ ", StringComparison.OrdinalIgnoreCase))
                    yield return "ЛИНИЯ " + candidate["ЛИНИИ ".Length..];

                if (candidate.StartsWith("СИСТЕМЫ ", StringComparison.OrdinalIgnoreCase))
                    yield return "СИСТЕМА " + candidate["СИСТЕМЫ ".Length..];
            }
        }

        private static IEnumerable<string> ExpandSystemContextBaseSuffixes(string systemContext)
        {
            yield return systemContext;

            if (systemContext.StartsWith("АСУТП ", StringComparison.OrdinalIgnoreCase))
                yield return systemContext["АСУТП ".Length..];

            if (systemContext.StartsWith("АСУ ", StringComparison.OrdinalIgnoreCase))
                yield return systemContext["АСУ ".Length..];

            if (systemContext.StartsWith("СУ ", StringComparison.OrdinalIgnoreCase))
                yield return systemContext["СУ ".Length..];
        }

        private static IEnumerable<string> TrimTrailingLatinModelCodes(string value)
        {
            string normalized = value.Trim().Trim('.', ' ', '-', '–', '—', ',', ';', ':');
            int lastSpaceIndex = normalized.LastIndexOf(' ');
            if (lastSpaceIndex <= 0 || lastSpaceIndex == normalized.Length - 1)
                yield break;

            string tail = normalized[(lastSpaceIndex + 1)..];
            if (!IsLatinModelCode(tail))
                yield break;

            string trimmed = normalized[..lastSpaceIndex].Trim().Trim('.', ' ', '-', '–', '—', ',', ';', ':');
            if (trimmed.Length > 0)
                yield return trimmed;
        }

        private static bool IsLatinModelCode(string value)
        {
            bool hasLatinLetter = false;
            bool hasDigit = false;
            foreach (char character in value)
            {
                if (character is >= 'A' and <= 'Z' or >= 'a' and <= 'z')
                {
                    hasLatinLetter = true;
                    continue;
                }

                if (char.IsDigit(character))
                {
                    hasDigit = true;
                    continue;
                }

                if (character is '-' or '_')
                    continue;

                return false;
            }

            return hasLatinLetter && hasDigit;
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
            int To3Hours,
            List<ImportedNormMonthWorkEntry> MonthWorkEntries,
            List<KbMaintenanceYearScheduleEntry> YearScheduleEntries)
        {
            public string EquipmentNameKey { get; } = NormalizeTextKey(EquipmentName);

            public string[] EquipmentNameKeys { get; } = BuildNameMatchKeys(EquipmentName, SystemName);

            public string EquipmentInventoryKey { get; } = NormalizeInventoryKey(EquipmentInventory);

            public string EquipmentInventoryCanonicalKey { get; } = NormalizeInventoryAggregateKey(EquipmentInventory);

            public string[] EquipmentInventoryKeys { get; } = BuildInventoryMatchKeys(EquipmentInventory);

            public string SystemNameKey { get; } = NormalizeTextKey(SystemName);

            public string[] SystemNameKeys { get; } = BuildNameMatchKeys(SystemName);

            public string SystemInventoryKey { get; } = NormalizeInventoryKey(SystemInventory);

            public string[] SystemInventoryKeys { get; } = BuildInventoryMatchKeys(SystemInventory);
        }

        private sealed record ImportedNormMonthWorkEntry(
            int Month,
            KbMaintenanceWorkKind WorkKind,
            int Hours);

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
                MonthWorkEntries = CloneMonthWorkEntries(source.MonthWorkEntries);
                YearScheduleEntries = CloneYearScheduleEntries(source.YearScheduleEntries);
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

            public List<ImportedNormMonthWorkEntry> MonthWorkEntries { get; private set; }

            public List<KbMaintenanceYearScheduleEntry> YearScheduleEntries { get; private set; }

            public static ImportedNormAccumulator Create(ImportedNormEntry entry) => new(entry);

            public void Absorb(ImportedNormEntry entry)
            {
                To1Hours = Math.Max(To1Hours, entry.To1Hours);
                To2Hours = Math.Max(To2Hours, entry.To2Hours);
                To3Hours = Math.Max(To3Hours, entry.To3Hours);
                if (entry.MonthWorkEntries.Count > 0)
                    MonthWorkEntries = CloneMonthWorkEntries(entry.MonthWorkEntries);

                if (entry.YearScheduleEntries.Count > 0)
                    YearScheduleEntries = CloneYearScheduleEntries(entry.YearScheduleEntries);
            }

            public void AbsorbResolvedOwnerEntry(ImportedNormEntry entry)
            {
                if (MonthWorkEntries.Count > 0 || entry.MonthWorkEntries.Count > 0)
                {
                    MonthWorkEntries.AddRange(CloneMonthWorkEntries(entry.MonthWorkEntries));
                    ApplyAggregatedMonthWorkEntries();
                    return;
                }

                Absorb(entry);
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
                To3Hours,
                CloneMonthWorkEntries(MonthWorkEntries),
                CloneYearScheduleEntries(YearScheduleEntries));

            private void ApplyAggregatedMonthWorkEntries()
            {
                To1Hours = 0;
                To2Hours = 0;
                To3Hours = 0;
                var yearScheduleEntries = new List<KbMaintenanceYearScheduleEntry>();

                foreach (var monthGroup in MonthWorkEntries
                             .Where(static entry => entry.Month is >= 1 and <= 12 && entry.Hours > 0)
                             .GroupBy(static entry => entry.Month)
                             .OrderBy(static group => group.Key))
                {
                    KbMaintenanceWorkKind workKind = monthGroup.Max(static entry => entry.WorkKind);
                    int hours = monthGroup.Sum(static entry => entry.Hours);
                    switch (workKind)
                    {
                        case KbMaintenanceWorkKind.To1:
                            To1Hours = Math.Max(To1Hours, hours);
                            break;
                        case KbMaintenanceWorkKind.To2:
                            To2Hours = Math.Max(To2Hours, hours);
                            break;
                        case KbMaintenanceWorkKind.To3:
                            To3Hours = Math.Max(To3Hours, hours);
                            break;
                    }

                    yearScheduleEntries.Add(new KbMaintenanceYearScheduleEntry
                    {
                        Month = monthGroup.Key,
                        WorkKind = workKind,
                        Hours = hours
                    });
                }

                YearScheduleEntries = yearScheduleEntries;
            }
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

            public string[] EquipmentInventoryKeys { get; } = BuildInventoryMatchKeys(EquipmentInventory);

            public string SystemNameKey { get; } = NormalizeTextKey(SystemName);

            public string[] SystemNameKeys { get; } = BuildNameMatchKeys(SystemName);

            public string SystemInventoryKey { get; } = NormalizeInventoryKey(SystemInventory);

            public string[] SystemInventoryKeys { get; } = BuildInventoryMatchKeys(SystemInventory);
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
