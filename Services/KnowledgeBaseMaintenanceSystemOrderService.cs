using System.Globalization;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace AsutpKnowledgeBase.Services
{
    public sealed record KnowledgeBaseMaintenanceSystemOrderEntry(
        int Rank,
        int SequenceNumber,
        string SystemName,
        string InventoryNumber);

    public sealed class KnowledgeBaseMaintenanceSystemOrderService
    {
        private readonly KnowledgeBaseMaintenanceWorkbookTemplateService _templateService;
        private IReadOnlyList<KnowledgeBaseMaintenanceSystemOrderEntry>? _annualTemplateOrder;

        public KnowledgeBaseMaintenanceSystemOrderService(
            KnowledgeBaseMaintenanceWorkbookTemplateService? templateService = null)
        {
            _templateService = templateService ?? new KnowledgeBaseMaintenanceWorkbookTemplateService();
        }

        public IReadOnlyList<KnowledgeBaseMaintenanceSystemOrderEntry> GetAnnualTemplateOrder() =>
            _annualTemplateOrder ??= ReadAnnualTemplateOrder();

        public int GetNextAppendedSequenceNumber(IReadOnlyList<KnowledgeBaseMaintenanceSystemOrderEntry> templateOrder) =>
            templateOrder.Count == 0
                ? 1
                : templateOrder.Max(static entry => entry.SequenceNumber) + 1;

        public KnowledgeBaseMaintenanceSystemOrderEntry? ResolveTemplateEntry(
            IReadOnlyList<KnowledgeBaseMaintenanceSystemOrderEntry> templateOrder,
            string systemName,
            string inventoryNumber)
        {
            string systemNameKey = BuildSystemOrderKey(systemName);
            string inventoryNumberKey = BuildInventoryNumberKey(inventoryNumber);

            if (!string.IsNullOrWhiteSpace(systemNameKey) && !string.IsNullOrWhiteSpace(inventoryNumberKey))
            {
                KnowledgeBaseMaintenanceSystemOrderEntry? exactEntry = templateOrder.FirstOrDefault(entry =>
                    string.Equals(BuildSystemOrderKey(entry.SystemName), systemNameKey, StringComparison.Ordinal) &&
                    string.Equals(BuildInventoryNumberKey(entry.InventoryNumber), inventoryNumberKey, StringComparison.Ordinal));
                if (exactEntry != null)
                    return exactEntry;
            }

            if (!string.IsNullOrWhiteSpace(inventoryNumberKey))
            {
                List<KnowledgeBaseMaintenanceSystemOrderEntry> inventoryMatches = templateOrder
                    .Where(entry => string.Equals(BuildInventoryNumberKey(entry.InventoryNumber), inventoryNumberKey, StringComparison.Ordinal))
                    .Take(2)
                    .ToList();
                if (inventoryMatches.Count == 1)
                    return inventoryMatches[0];
            }

            if (!string.IsNullOrWhiteSpace(systemNameKey))
            {
                KnowledgeBaseMaintenanceSystemOrderEntry? nameMatch = templateOrder.FirstOrDefault(entry =>
                    string.Equals(BuildSystemOrderKey(entry.SystemName), systemNameKey, StringComparison.Ordinal));
                if (nameMatch != null)
                    return nameMatch;
            }

            return ResolveTemplateNameMatch(templateOrder, systemName);
        }

        public string BuildSystemKey(string systemNodeId, string systemName, string inventoryNumber)
        {
            string normalizedSystemNodeId = systemNodeId?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(normalizedSystemNodeId))
                return normalizedSystemNodeId;

            return $"{BuildNameMatchKey(systemName)}|{BuildInventoryNumberKey(inventoryNumber)}";
        }

        private IReadOnlyList<KnowledgeBaseMaintenanceSystemOrderEntry> ReadAnnualTemplateOrder()
        {
            byte[] templateBytes = _templateService.GetAnnualTemplatePackage();
            using var stream = new MemoryStream(templateBytes, writable: false);
            using SpreadsheetDocument document = SpreadsheetDocument.Open(stream, false);
            WorkbookPart workbookPart = document.WorkbookPart
                ?? throw new InvalidOperationException("Шаблон годового графика ТО повреждён: отсутствует workbook part.");
            Sheet sheet = workbookPart.Workbook.Sheets?.Elements<Sheet>().FirstOrDefault()
                ?? throw new InvalidOperationException("Шаблон годового графика ТО повреждён: отсутствует первый лист.");
            WorksheetPart worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id?.Value ?? string.Empty);
            SheetData sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>()
                ?? throw new InvalidOperationException("Шаблон годового графика ТО повреждён: отсутствует sheetData.");
            IReadOnlyList<string> sharedStrings = ReadSharedStrings(workbookPart.SharedStringTablePart);

            var order = new List<KnowledgeBaseMaintenanceSystemOrderEntry>();
            foreach (Row row in sheetData.Elements<Row>())
            {
                string sequenceText = ReadCellText(row, 1, sharedStrings);
                string systemName = ReadCellText(row, 2, sharedStrings);
                string inventoryNumber = ReadCellText(row, 4, sharedStrings);
                if (!int.TryParse(sequenceText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int sequenceNumber) ||
                    string.IsNullOrWhiteSpace(systemName))
                {
                    continue;
                }

                if (IsTemplateColumnNumberRow(sequenceNumber, systemName, inventoryNumber))
                    continue;

                order.Add(new KnowledgeBaseMaintenanceSystemOrderEntry(
                    Rank: order.Count,
                    SequenceNumber: sequenceNumber,
                    SystemName: systemName.Trim(),
                    InventoryNumber: inventoryNumber.Trim()));
            }

            return order;
        }

        private static KnowledgeBaseMaintenanceSystemOrderEntry? ResolveTemplateNameMatch(
            IReadOnlyList<KnowledgeBaseMaintenanceSystemOrderEntry> templateOrder,
            string systemName)
        {
            string systemNameMatchKey = BuildNameMatchKey(systemName);
            if (string.IsNullOrWhiteSpace(systemNameMatchKey))
                return null;

            KnowledgeBaseMaintenanceSystemOrderEntry? exactMatch = FindUnique(
                templateOrder.Where(entry => string.Equals(BuildNameMatchKey(entry.SystemName), systemNameMatchKey, StringComparison.Ordinal)));
            if (exactMatch != null)
                return exactMatch;

            return templateOrder
                .Select(entry => new
                {
                    Entry = entry,
                    Score = CalculateNameMatchScore(entry.SystemName, BuildNameMatchKey(entry.SystemName), systemName, systemNameMatchKey)
                })
                .Where(static item => item.Score > 0)
                .OrderByDescending(static item => item.Score)
                .ThenBy(static item => item.Entry.Rank)
                .Select(static item => item.Entry)
                .FirstOrDefault();
        }

        private static T? FindUnique<T>(IEnumerable<T> items)
            where T : class
        {
            List<T> matches = items.Take(2).ToList();
            return matches.Count == 1 ? matches[0] : null;
        }

        private static int CalculateNameMatchScore(
            string templateName,
            string templateMatchKey,
            string candidateName,
            string candidateMatchKey)
        {
            if (string.IsNullOrWhiteSpace(templateMatchKey) || string.IsNullOrWhiteSpace(candidateMatchKey))
                return 0;

            if (string.Equals(templateMatchKey, candidateMatchKey, StringComparison.Ordinal))
                return 10_000;

            if (templateMatchKey.Contains(candidateMatchKey, StringComparison.Ordinal) ||
                candidateMatchKey.Contains(templateMatchKey, StringComparison.Ordinal))
            {
                int shorterLength = Math.Min(templateMatchKey.Length, candidateMatchKey.Length);
                int longerLength = Math.Max(templateMatchKey.Length, candidateMatchKey.Length);
                return 6_000 + (shorterLength * 1_000 / Math.Max(1, longerLength));
            }

            HashSet<string> templateTokens = BuildNameTokens(templateName);
            HashSet<string> candidateTokens = BuildNameTokens(candidateName);
            if (templateTokens.Count == 0 || candidateTokens.Count == 0)
                return 0;

            int overlap = candidateTokens.Count(templateTokens.Contains);
            if (overlap == 0 ||
                (candidateTokens.Count <= 2 && overlap < candidateTokens.Count) ||
                (candidateTokens.Count > 2 && overlap < 2))
            {
                return 0;
            }

            return 1_000 + (overlap * 100) + (overlap * 100 / candidateTokens.Count) + (overlap * 100 / templateTokens.Count);
        }

        private static bool IsTemplateColumnNumberRow(int sequenceNumber, string systemName, string inventoryNumber) =>
            sequenceNumber == 1 &&
            string.Equals(BuildSystemOrderKey(systemName), "2", StringComparison.Ordinal) &&
            string.Equals(BuildInventoryNumberKey(inventoryNumber), "4", StringComparison.Ordinal);

        private static string BuildSystemOrderKey(string? systemName)
        {
            string normalized = Regex.Replace(systemName?.Trim() ?? string.Empty, @"\s+", " ");
            return normalized.ToUpperInvariant();
        }

        private static string BuildInventoryNumberKey(string? inventoryNumber)
        {
            string normalized = Regex.Replace(inventoryNumber?.Trim() ?? string.Empty, @"\s+", string.Empty);
            return normalized.ToUpperInvariant();
        }

        private static string BuildNameMatchKey(string? value)
        {
            string normalized = NormalizeNameForMatching(value);
            return Regex.Replace(normalized, @"[^\p{L}\p{Nd}]+", string.Empty).ToUpperInvariant();
        }

        private static HashSet<string> BuildNameTokens(string? value)
        {
            string normalized = NormalizeNameForMatching(value);
            return Regex.Matches(normalized.ToUpperInvariant(), @"[\p{L}\p{Nd}]+")
                .Select(static match => match.Value)
                .Where(static token => token.Length > 0)
                .ToHashSet(StringComparer.Ordinal);
        }

        private static string NormalizeNameForMatching(string? value)
        {
            string normalized = (value ?? string.Empty)
                .Replace('Ё', 'Е')
                .Replace('ё', 'е')
                .Replace('№', ' ');
            normalized = Regex.Replace(normalized, @"\bАСУТП\b", "АСУ", RegexOptions.IgnoreCase);
            normalized = Regex.Replace(normalized, @"\bМО\b", "медного отделения", RegexOptions.IgnoreCase);
            normalized = Regex.Replace(normalized, @"\bНО\b", "никелевого отделения", RegexOptions.IgnoreCase);
            normalized = Regex.Replace(normalized, @"\bФП\b", "фильтр пресс", RegexOptions.IgnoreCase);
            return normalized;
        }

        private static IReadOnlyList<string> ReadSharedStrings(SharedStringTablePart? part)
        {
            if (part?.SharedStringTable == null)
                return Array.Empty<string>();

            return part.SharedStringTable
                .Elements<SharedStringItem>()
                .Select(static item => item.InnerText ?? string.Empty)
                .ToArray();
        }

        private static string ReadCellText(Row row, int columnIndex, IReadOnlyList<string> sharedStrings)
        {
            Cell? cell = row.Elements<Cell>()
                .FirstOrDefault(candidate =>
                    string.Equals(
                        Regex.Replace(candidate.CellReference?.Value ?? string.Empty, @"\d", string.Empty),
                        GetColumnName(columnIndex),
                        StringComparison.Ordinal));

            return cell == null
                ? string.Empty
                : ReadCellText(cell, sharedStrings);
        }

        private static string ReadCellText(Cell cell, IReadOnlyList<string> sharedStrings)
        {
            if (cell.DataType?.Value == CellValues.SharedString)
            {
                if (cell.CellValue == null || !int.TryParse(cell.CellValue.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int index))
                    return string.Empty;

                return index >= 0 && index < sharedStrings.Count
                    ? sharedStrings[index]
                    : string.Empty;
            }

            return cell.CellValue?.Text ?? cell.InnerText ?? string.Empty;
        }

        private static string GetColumnName(int columnIndex)
        {
            var columnName = string.Empty;
            int dividend = columnIndex;
            while (dividend > 0)
            {
                int modulo = (dividend - 1) % 26;
                columnName = Convert.ToChar('A' + modulo, CultureInfo.InvariantCulture) + columnName;
                dividend = (dividend - modulo) / 26;
            }

            return columnName;
        }
    }
}
