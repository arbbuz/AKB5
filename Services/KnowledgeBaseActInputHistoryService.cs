using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public sealed class KnowledgeBaseActInputHistoryService
    {
        public List<KbActInputHistoryEntry> NormalizeEntries(
            IEnumerable<KbActInputHistoryEntry>? entries)
        {
            var normalizedByKey = new Dictionary<EntryKey, KbActInputHistoryEntry>(EntryKeyComparer.Instance);
            if (entries == null)
                return new List<KbActInputHistoryEntry>();

            foreach (KbActInputHistoryEntry? entry in entries)
            {
                if (entry == null || !IsSupportedField(entry.Field))
                    continue;

                string workshopName = NormalizeWorkshopName(entry.WorkshopName);
                string displayValue = NormalizeDisplayValue(entry.DisplayValue);
                if (string.IsNullOrWhiteSpace(workshopName) || string.IsNullOrWhiteSpace(displayValue))
                    continue;

                string normalizedValue = NormalizeValue(displayValue);
                var key = new EntryKey(workshopName, entry.Field, normalizedValue);
                var normalizedEntry = new KbActInputHistoryEntry
                {
                    WorkshopName = workshopName,
                    Field = entry.Field,
                    DisplayValue = displayValue,
                    NormalizedValue = normalizedValue,
                    UseOrder = Math.Max(0, entry.UseOrder)
                };

                if (!normalizedByKey.TryGetValue(key, out KbActInputHistoryEntry? existing) ||
                    normalizedEntry.UseOrder >= existing.UseOrder)
                {
                    normalizedByKey[key] = normalizedEntry;
                }
            }

            return normalizedByKey.Values
                .OrderBy(static entry => entry.WorkshopName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static entry => entry.Field)
                .ThenByDescending(static entry => entry.UseOrder)
                .ThenBy(static entry => entry.DisplayValue, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public List<KbActInputHistoryEntry> AddOrTouch(
            IEnumerable<KbActInputHistoryEntry>? entries,
            string? workshopName,
            KbActInputHistoryField field,
            string? value)
        {
            List<KbActInputHistoryEntry> normalizedEntries = NormalizeEntries(entries);
            string normalizedWorkshop = NormalizeWorkshopName(workshopName);
            string displayValue = NormalizeDisplayValue(value);
            if (!IsSupportedField(field) ||
                string.IsNullOrWhiteSpace(normalizedWorkshop) ||
                string.IsNullOrWhiteSpace(displayValue))
            {
                return normalizedEntries;
            }

            string normalizedValue = NormalizeValue(displayValue);
            normalizedEntries.RemoveAll(entry =>
                WorkshopNamesEqual(entry.WorkshopName, normalizedWorkshop) &&
                entry.Field == field &&
                string.Equals(entry.NormalizedValue, normalizedValue, StringComparison.OrdinalIgnoreCase));

            long highestUseOrder = normalizedEntries.Count == 0
                ? 0
                : normalizedEntries.Max(static entry => entry.UseOrder);
            normalizedEntries.Add(new KbActInputHistoryEntry
            {
                WorkshopName = normalizedWorkshop,
                Field = field,
                DisplayValue = displayValue,
                NormalizedValue = normalizedValue,
                UseOrder = highestUseOrder == long.MaxValue ? long.MaxValue : highestUseOrder + 1
            });

            return NormalizeEntries(normalizedEntries);
        }

        public List<KbActInputHistoryEntry> Delete(
            IEnumerable<KbActInputHistoryEntry>? entries,
            string? workshopName,
            KbActInputHistoryField field,
            string? value)
        {
            string normalizedWorkshop = NormalizeWorkshopName(workshopName);
            string normalizedValue = NormalizeValue(value);

            return NormalizeEntries(entries)
                .Where(entry =>
                    !WorkshopNamesEqual(entry.WorkshopName, normalizedWorkshop) ||
                    entry.Field != field ||
                    !string.Equals(entry.NormalizedValue, normalizedValue, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public IReadOnlyList<string> GetSuggestions(
            IEnumerable<KbActInputHistoryEntry>? entries,
            string? workshopName,
            KbActInputHistoryField field) =>
            NormalizeEntries(entries)
                .Where(entry =>
                    WorkshopNamesEqual(entry.WorkshopName, workshopName) &&
                    entry.Field == field)
                .OrderByDescending(static entry => entry.UseOrder)
                .ThenBy(static entry => entry.DisplayValue, StringComparer.OrdinalIgnoreCase)
                .Select(static entry => entry.DisplayValue)
                .ToList();

        public List<KbActInputHistoryEntry> RecordActValues(
            IEnumerable<KbActInputHistoryEntry>? entries,
            string? workshopName,
            KbAct act,
            IEnumerable<KbActExecutor>? executors)
        {
            List<KbActInputHistoryEntry> updated = NormalizeEntries(entries);
            KbActExecutor? executor = executors?
                .OrderBy(static item => item.SortOrder)
                .FirstOrDefault();

            updated = AddOrTouch(
                updated,
                workshopName,
                KbActInputHistoryField.ExecutorName,
                FormatExecutorName(executor));
            updated = AddOrTouch(
                updated,
                workshopName,
                KbActInputHistoryField.ExecutorPosition,
                executor?.Position);
            updated = AddOrTouch(
                updated,
                workshopName,
                KbActInputHistoryField.CustomerName,
                act.CustomerName);
            updated = AddOrTouch(
                updated,
                workshopName,
                KbActInputHistoryField.CustomerPosition,
                act.CustomerPosition);
            updated = AddOrTouch(
                updated,
                workshopName,
                KbActInputHistoryField.ApproverName,
                act.ApproverName);
            return AddOrTouch(
                updated,
                workshopName,
                KbActInputHistoryField.ApproverPosition,
                act.ApproverPosition);
        }

        public static string NormalizeDisplayValue(string? value) =>
            string.Join(
                " ",
                (value ?? string.Empty)
                    .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        public static string NormalizeValue(string? value) =>
            NormalizeDisplayValue(value).ToUpperInvariant();

        private static string NormalizeWorkshopName(string? workshopName) =>
            workshopName?.Trim() ?? string.Empty;

        private static bool WorkshopNamesEqual(string? left, string? right) =>
            string.Equals(
                NormalizeWorkshopName(left),
                NormalizeWorkshopName(right),
                StringComparison.OrdinalIgnoreCase);

        private static bool IsSupportedField(KbActInputHistoryField field) =>
            Enum.IsDefined(typeof(KbActInputHistoryField), field);

        private static string FormatExecutorName(KbActExecutor? executor)
        {
            if (executor == null)
                return string.Empty;

            return string.Join(
                " ",
                new[]
                {
                    executor.LastName,
                    executor.FirstName,
                    executor.MiddleName
                }.Where(static part => !string.IsNullOrWhiteSpace(part)));
        }

        private readonly record struct EntryKey(
            string WorkshopName,
            KbActInputHistoryField Field,
            string NormalizedValue);

        private sealed class EntryKeyComparer : IEqualityComparer<EntryKey>
        {
            public static EntryKeyComparer Instance { get; } = new();

            public bool Equals(EntryKey left, EntryKey right) =>
                left.Field == right.Field &&
                string.Equals(left.WorkshopName, right.WorkshopName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(left.NormalizedValue, right.NormalizedValue, StringComparison.OrdinalIgnoreCase);

            public int GetHashCode(EntryKey value) =>
                HashCode.Combine(
                    StringComparer.OrdinalIgnoreCase.GetHashCode(value.WorkshopName),
                    value.Field,
                    StringComparer.OrdinalIgnoreCase.GetHashCode(value.NormalizedValue));
        }
    }
}
