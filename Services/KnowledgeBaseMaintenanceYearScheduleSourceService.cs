using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public sealed class KnowledgeBaseMaintenanceYearScheduleSourceRow
    {
        public string OwnerNodeId { get; init; } = string.Empty;

        public string Path { get; init; } = string.Empty;

        public string NodeName { get; init; } = string.Empty;

        public string InventoryNumber { get; init; } = string.Empty;

        public bool IsIncludedInSchedule { get; init; }

        public List<KbMaintenanceYearScheduleEntry> YearScheduleEntries { get; init; } = new();

        public int TreeOrder { get; init; }

        public uint SourceRowNumber { get; init; }

        public bool HasManualSchedule => YearScheduleEntries.Count > 0;
    }

    public sealed class KnowledgeBaseMaintenanceYearScheduleSourceApplyResult
    {
        public bool IsSuccess { get; init; }

        public string ErrorMessage { get; init; } = string.Empty;

        public List<KbMaintenanceScheduleProfile> MaintenanceScheduleProfiles { get; init; } = new();

        public int EditedRowCount { get; init; }

        public int UpdatedProfileCount { get; init; }

        public int ClearedProfileCount { get; init; }

        public int UnchangedProfileCount { get; init; }

        public List<string> UnresolvedRows { get; init; } = new();
    }

    public sealed class KnowledgeBaseMaintenanceYearScheduleSourceService
    {
        public List<KnowledgeBaseMaintenanceYearScheduleSourceRow> BuildRows(
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

            Dictionary<string, KbMaintenanceScheduleProfile> profilesByOwnerNodeId = CloneProfiles(maintenanceScheduleProfiles)
                .Where(static profile => !string.IsNullOrWhiteSpace(profile.OwnerNodeId))
                .GroupBy(static profile => profile.OwnerNodeId.Trim(), StringComparer.Ordinal)
                .ToDictionary(
                    static group => group.Key,
                    static group => group.OrderBy(static profile => profile.MaintenanceProfileId, StringComparer.Ordinal).First(),
                    StringComparer.Ordinal);

            var rows = new List<KnowledgeBaseMaintenanceYearScheduleSourceRow>();
            foreach (OwnerNodeContext context in ownerNodeContexts.Values.OrderBy(static context => context.TreeOrder))
            {
                if (!profilesByOwnerNodeId.TryGetValue(context.OwnerNodeId, out KbMaintenanceScheduleProfile? profile))
                    continue;

                rows.Add(new KnowledgeBaseMaintenanceYearScheduleSourceRow
                {
                    OwnerNodeId = context.OwnerNodeId,
                    Path = context.Path,
                    NodeName = context.NodeName,
                    InventoryNumber = context.InventoryNumber,
                    IsIncludedInSchedule = profile.IsIncludedInSchedule,
                    YearScheduleEntries = CloneYearScheduleEntries(profile.YearScheduleEntries),
                    TreeOrder = context.TreeOrder
                });
            }

            return rows;
        }

        public KnowledgeBaseMaintenanceYearScheduleSourceApplyResult ApplyRows(
            IReadOnlyList<KnowledgeBaseMaintenanceYearScheduleSourceRow>? rows,
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

            List<KbMaintenanceScheduleProfile> updatedProfiles = CloneProfiles(maintenanceScheduleProfiles);
            Dictionary<string, KbMaintenanceScheduleProfile> profilesByOwnerNodeId = updatedProfiles
                .Where(static profile => !string.IsNullOrWhiteSpace(profile.OwnerNodeId))
                .GroupBy(static profile => profile.OwnerNodeId.Trim(), StringComparer.Ordinal)
                .ToDictionary(
                    static group => group.Key,
                    static group => group.OrderBy(static profile => profile.MaintenanceProfileId, StringComparer.Ordinal).First(),
                    StringComparer.Ordinal);

            var unresolvedRows = new List<string>();
            int editedRowCount = 0;
            int updatedProfileCount = 0;
            int clearedProfileCount = 0;
            int unchangedProfileCount = 0;

            foreach (KnowledgeBaseMaintenanceYearScheduleSourceRow row in rows ?? Array.Empty<KnowledgeBaseMaintenanceYearScheduleSourceRow>())
            {
                string ownerNodeId = row.OwnerNodeId?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(ownerNodeId))
                    continue;

                editedRowCount++;
                if (!ownerNodeContexts.ContainsKey(ownerNodeId))
                {
                    unresolvedRows.Add($"{FormatRowReference(row)}: узел OwnerNodeId '{ownerNodeId}' не найден в текущем цехе.");
                    continue;
                }

                if (!profilesByOwnerNodeId.TryGetValue(ownerNodeId, out KbMaintenanceScheduleProfile? profile))
                {
                    unresolvedRows.Add($"{FormatRowReference(row)}: профиль ТО для OwnerNodeId '{ownerNodeId}' не настроен.");
                    continue;
                }

                if (!TryNormalizeYearScheduleEntries(row.YearScheduleEntries, out List<KbMaintenanceYearScheduleEntry> entries, out string errorMessage))
                    return Failure(errorMessage);

                if (YearScheduleEquals(profile.YearScheduleEntries, entries))
                {
                    unchangedProfileCount++;
                    continue;
                }

                bool wasManual = profile.YearScheduleEntries?.Count > 0;
                profile.YearScheduleEntries = CloneYearScheduleEntries(entries);
                if (wasManual && profile.YearScheduleEntries.Count == 0)
                    clearedProfileCount++;
                else
                    updatedProfileCount++;
            }

            return new KnowledgeBaseMaintenanceYearScheduleSourceApplyResult
            {
                IsSuccess = true,
                MaintenanceScheduleProfiles = updatedProfiles,
                EditedRowCount = editedRowCount,
                UpdatedProfileCount = updatedProfileCount,
                ClearedProfileCount = clearedProfileCount,
                UnchangedProfileCount = unchangedProfileCount,
                UnresolvedRows = unresolvedRows
            };
        }

        public static List<KbMaintenanceScheduleProfile> CloneProfiles(
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

        public static List<KbMaintenanceYearScheduleEntry> CloneYearScheduleEntries(
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
                    WorkKind = entry.WorkKind,
                    Hours = entry.Hours
                });
            }

            return clones;
        }

        public static bool YearScheduleEquals(
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
                    leftEntries[index].WorkKind != rightEntries[index].WorkKind ||
                    leftEntries[index].Hours != rightEntries[index].Hours)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TryNormalizeYearScheduleEntries(
            IReadOnlyList<KbMaintenanceYearScheduleEntry>? source,
            out List<KbMaintenanceYearScheduleEntry> entries,
            out string errorMessage)
        {
            entries = new List<KbMaintenanceYearScheduleEntry>();
            errorMessage = string.Empty;

            var usedMonths = new HashSet<int>();
            foreach (KbMaintenanceYearScheduleEntry entry in source ?? Array.Empty<KbMaintenanceYearScheduleEntry>())
            {
                if (entry == null)
                    continue;

                if (entry.Month < 1 || entry.Month > 12)
                {
                    errorMessage = "Месяц в источнике годового графика ТО должен быть в диапазоне от 1 до 12.";
                    return false;
                }

                if (!Enum.IsDefined(typeof(KbMaintenanceWorkKind), entry.WorkKind))
                {
                    errorMessage = "В источнике годового графика ТО найден неизвестный тип работ.";
                    return false;
                }

                if (!usedMonths.Add(entry.Month))
                {
                    errorMessage = "В источнике годового графика ТО не должно быть дублей одного месяца.";
                    return false;
                }

                entries.Add(new KbMaintenanceYearScheduleEntry
                {
                    Month = entry.Month,
                    WorkKind = entry.WorkKind,
                    Hours = Math.Max(0, entry.Hours)
                });
            }

            entries = entries.OrderBy(static entry => entry.Month).ToList();
            return true;
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

        private static string FormatRowReference(KnowledgeBaseMaintenanceYearScheduleSourceRow row) =>
            row.SourceRowNumber > 0
                ? $"Строка {row.SourceRowNumber}"
                : "Строка источника";

        private static KnowledgeBaseMaintenanceYearScheduleSourceApplyResult Failure(string errorMessage) =>
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
    }
}
