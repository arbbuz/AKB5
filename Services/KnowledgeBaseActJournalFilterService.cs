namespace AsutpKnowledgeBase.Services
{
    public static class KnowledgeBaseActJournalFilterColumns
    {
        public const string ActDate = "ActDate";
        public const string ActNumber = "ActNumber";
        public const string Status = "Status";
        public const string ActType = "ActType";
        public const string Workshop = "Workshop";
        public const string Object = "Object";
        public const string Equipment = "Equipment";
        public const string OrderNumber = "OrderNumber";
        public const string DocumentState = "DocumentState";

        public static readonly IReadOnlyCollection<string> All = new[]
        {
            ActDate,
            ActNumber,
            Status,
            ActType,
            Workshop,
            Object,
            Equipment,
            OrderNumber,
            DocumentState
        };
    }

    public sealed class KnowledgeBaseActJournalFilterState
    {
        private readonly Dictionary<string, HashSet<string>> _selectedValuesByColumn =
            new(StringComparer.Ordinal);

        public bool HasFilters => _selectedValuesByColumn.Count > 0;

        internal IEnumerable<KeyValuePair<string, HashSet<string>>> ActiveFilters =>
            _selectedValuesByColumn;

        public bool HasFilter(string columnName) =>
            _selectedValuesByColumn.ContainsKey(NormalizeColumnName(columnName));

        public IReadOnlyCollection<string> GetSelectedValues(string columnName)
        {
            string normalizedColumnName = NormalizeColumnName(columnName);
            return _selectedValuesByColumn.TryGetValue(normalizedColumnName, out HashSet<string>? selectedValues)
                ? selectedValues
                    .OrderBy(static value => value, StringComparer.CurrentCultureIgnoreCase)
                    .ToArray()
                : Array.Empty<string>();
        }

        public void SetSelectedValues(string columnName, IEnumerable<string> selectedValues)
        {
            string normalizedColumnName = NormalizeColumnName(columnName);
            if (string.IsNullOrWhiteSpace(normalizedColumnName))
                return;

            _selectedValuesByColumn[normalizedColumnName] = selectedValues
                .Select(NormalizeValue)
                .Distinct(StringComparer.Ordinal)
                .ToHashSet(StringComparer.Ordinal);
        }

        public void ClearColumn(string columnName)
        {
            string normalizedColumnName = NormalizeColumnName(columnName);
            if (!string.IsNullOrWhiteSpace(normalizedColumnName))
                _selectedValuesByColumn.Remove(normalizedColumnName);
        }

        public void Clear() => _selectedValuesByColumn.Clear();

        private static string NormalizeColumnName(string? columnName) =>
            columnName?.Trim() ?? string.Empty;

        private static string NormalizeValue(string? value) =>
            value?.Trim() ?? string.Empty;
    }

    public sealed class KnowledgeBaseActJournalFilterService
    {
        public IReadOnlyList<KnowledgeBaseActJournalRow> Apply(
            IEnumerable<KnowledgeBaseActJournalRow>? rows,
            KnowledgeBaseActJournalFilterState? filterState,
            string? excludedColumnName = null)
        {
            if (rows == null)
                return Array.Empty<KnowledgeBaseActJournalRow>();

            List<KnowledgeBaseActJournalRow> normalizedRows = rows
                .Where(static row => row != null)
                .ToList();
            if (filterState == null || !filterState.HasFilters)
                return normalizedRows;

            string normalizedExcludedColumnName = excludedColumnName?.Trim() ?? string.Empty;
            return normalizedRows
                .Where(row => MatchesFilters(row, filterState, normalizedExcludedColumnName))
                .ToList();
        }

        public IReadOnlyList<string> GetDistinctValues(
            IEnumerable<KnowledgeBaseActJournalRow>? rows,
            string columnName)
        {
            if (rows == null || !IsSupportedColumn(columnName))
                return Array.Empty<string>();

            return rows
                .Where(static row => row != null)
                .Select(row => GetColumnValue(row, columnName))
                .Select(static value => value?.Trim() ?? string.Empty)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(static value => string.IsNullOrEmpty(value) ? 1 : 0)
                .ThenBy(static value => value, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }

        public static bool IsSupportedColumn(string? columnName)
        {
            string normalizedColumnName = columnName?.Trim() ?? string.Empty;
            return KnowledgeBaseActJournalFilterColumns.All.Contains(
                normalizedColumnName,
                StringComparer.Ordinal);
        }

        public static string GetColumnValue(KnowledgeBaseActJournalRow row, string columnName) =>
            columnName switch
            {
                KnowledgeBaseActJournalFilterColumns.ActDate => row.ActDateText,
                KnowledgeBaseActJournalFilterColumns.ActNumber => row.ActNumberText,
                KnowledgeBaseActJournalFilterColumns.Status => row.StatusText,
                KnowledgeBaseActJournalFilterColumns.ActType => row.ActTypeText,
                KnowledgeBaseActJournalFilterColumns.Workshop => row.WorkshopName,
                KnowledgeBaseActJournalFilterColumns.Object => row.ObjectName,
                KnowledgeBaseActJournalFilterColumns.Equipment => row.EquipmentName,
                KnowledgeBaseActJournalFilterColumns.OrderNumber => row.OrderNumber,
                KnowledgeBaseActJournalFilterColumns.DocumentState => row.DocumentStateText,
                _ => string.Empty
            };

        private static bool MatchesFilters(
            KnowledgeBaseActJournalRow row,
            KnowledgeBaseActJournalFilterState filterState,
            string excludedColumnName)
        {
            foreach (var filter in filterState.ActiveFilters)
            {
                if (string.Equals(filter.Key, excludedColumnName, StringComparison.Ordinal))
                    continue;

                string value = GetColumnValue(row, filter.Key).Trim();
                if (!filter.Value.Contains(value))
                    return false;
            }

            return true;
        }
    }
}
