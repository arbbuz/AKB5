using System.Security.Cryptography;
using System.Text;
using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public sealed class KnowledgeBaseMaintenanceMonthWorkResolverService
    {
        public IReadOnlyList<KbMaintenanceMonthWorkItem> ResolveMonthWorkItems(
            int year,
            int month,
            IReadOnlyList<KbNode> roots,
            IReadOnlyList<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles)
        {
            if (year <= 0)
                throw new ArgumentOutOfRangeException(nameof(year), year, "Год должен быть положительным.");

            if (month < 1 || month > 12)
                throw new ArgumentOutOfRangeException(nameof(month), month, "Месяц должен быть в диапазоне от 1 до 12.");

            if (roots == null || roots.Count == 0 || maintenanceScheduleProfiles == null || maintenanceScheduleProfiles.Count == 0)
                return Array.Empty<KbMaintenanceMonthWorkItem>();

            var profileByOwnerNodeId = maintenanceScheduleProfiles
                .Where(static profile => profile != null && !string.IsNullOrWhiteSpace(profile.OwnerNodeId))
                .GroupBy(profile => profile.OwnerNodeId.Trim(), StringComparer.Ordinal)
                .ToDictionary(
                    static group => group.Key,
                    static group => group.OrderBy(profile => profile.MaintenanceProfileId, StringComparer.Ordinal).First(),
                    StringComparer.Ordinal);

            var workItems = new List<KbMaintenanceMonthWorkItem>();
            foreach (NodeContext context in BuildNodeContexts(roots))
            {
                KbNode node = context.Node;
                string ownerNodeId = node.NodeId?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(ownerNodeId))
                    continue;

                if (!profileByOwnerNodeId.TryGetValue(ownerNodeId, out KbMaintenanceScheduleProfile? profile))
                    continue;

                if (!profile.IsIncludedInSchedule ||
                    !KnowledgeBaseMaintenanceScheduleStateService.SupportsProfile(node.NodeType, context.VisibleLevel))
                {
                    continue;
                }

                KbMaintenanceYearScheduleEntry? explicitEntry = ResolveExplicitYearScheduleEntry(profile.YearScheduleEntries, month);
                KbMaintenanceWorkKind dueKind = explicitEntry?.WorkKind ?? ResolveDueWorkKind(profile, ownerNodeId, month);

                AddWorkItemIfDue(workItems, context, profile, KbMaintenanceWorkKind.To3, ResolveDueHours(profile, explicitEntry, KbMaintenanceWorkKind.To3), dueKind == KbMaintenanceWorkKind.To3);
                AddWorkItemIfDue(workItems, context, profile, KbMaintenanceWorkKind.To2, ResolveDueHours(profile, explicitEntry, KbMaintenanceWorkKind.To2), dueKind == KbMaintenanceWorkKind.To2);
                AddWorkItemIfDue(workItems, context, profile, KbMaintenanceWorkKind.To1, ResolveDueHours(profile, explicitEntry, KbMaintenanceWorkKind.To1), dueKind == KbMaintenanceWorkKind.To1);
            }

            return workItems;
        }

        private static List<NodeContext> BuildNodeContexts(IEnumerable<KbNode> roots)
        {
            var contexts = new List<NodeContext>();
            int preorderIndex = 0;
            AddNodeContexts(
                roots,
                visibleLevel: 1,
                systemNodeId: string.Empty,
                systemPreorderIndex: int.MaxValue,
                systemLevel3NodeCount: 0,
                contexts,
                ref preorderIndex);
            return contexts;
        }

        private static void AddNodeContexts(
            IEnumerable<KbNode> nodes,
            int visibleLevel,
            string systemNodeId,
            int systemPreorderIndex,
            int systemLevel3NodeCount,
            ICollection<NodeContext> contexts,
            ref int preorderIndex)
        {
            foreach (KbNode node in nodes)
            {
                int currentVisibleLevel = GetEffectiveVisibleLevel(node, visibleLevel);
                int currentPreorderIndex = preorderIndex++;
                string currentSystemNodeId = systemNodeId;
                int currentSystemPreorderIndex = systemPreorderIndex;
                int currentSystemLevel3NodeCount = systemLevel3NodeCount;
                string nodeId = node.NodeId?.Trim() ?? string.Empty;
                if (currentVisibleLevel == 2)
                {
                    currentSystemNodeId = nodeId;
                    currentSystemPreorderIndex = currentPreorderIndex;
                    currentSystemLevel3NodeCount = CountVisibleLevel3Children(node, currentVisibleLevel);
                }

                contexts.Add(new NodeContext(
                    node,
                    currentVisibleLevel,
                    currentPreorderIndex,
                    currentSystemNodeId,
                    currentSystemPreorderIndex,
                    currentSystemLevel3NodeCount));

                AddNodeContexts(
                    node.Children,
                    currentVisibleLevel + 1,
                    currentSystemNodeId,
                    currentSystemPreorderIndex,
                    currentSystemLevel3NodeCount,
                    contexts,
                    ref preorderIndex);
            }
        }

        private static int CountVisibleLevel3Children(KbNode node, int systemVisibleLevel)
        {
            int childVisibleLevel = systemVisibleLevel + 1;
            int count = 0;
            foreach (KbNode child in node.Children ?? new List<KbNode>())
            {
                if (GetEffectiveVisibleLevel(child, childVisibleLevel) == 3)
                    count++;
            }

            return count;
        }

        private static int GetEffectiveVisibleLevel(KbNode node, int visibleLevel)
        {
            if (node.NodeType == KbNodeType.WorkshopRoot && node.LevelIndex == 0)
                return Math.Max(0, visibleLevel - 1);

            return visibleLevel;
        }

        private static void AddWorkItemIfDue(
            ICollection<KbMaintenanceMonthWorkItem> workItems,
            NodeContext context,
            KbMaintenanceScheduleProfile profile,
            KbMaintenanceWorkKind workKind,
            int hours,
            bool isDue)
        {
            if (!isDue || hours <= 0)
                return;

            workItems.Add(new KbMaintenanceMonthWorkItem
            {
                OwnerNodeId = profile.OwnerNodeId?.Trim() ?? string.Empty,
                NodeName = context.Node.Name?.Trim() ?? string.Empty,
                SystemNodeId = context.SystemNodeId,
                SystemPreorderIndex = context.SystemPreorderIndex,
                OwnerPreorderIndex = context.PreorderIndex,
                SystemLevel3NodeCount = context.SystemLevel3NodeCount,
                WorkKind = workKind,
                Hours = hours
            });
        }

        private sealed record NodeContext(
            KbNode Node,
            int VisibleLevel,
            int PreorderIndex,
            string SystemNodeId,
            int SystemPreorderIndex,
            int SystemLevel3NodeCount);

        private static KbMaintenanceWorkKind ResolveDueWorkKind(
            KbMaintenanceScheduleProfile profile,
            string ownerNodeId,
            int month)
        {
            // TO2 includes TO1, and TO3 includes both TO1 and TO2.
            // Without a manual yearly source the planner keeps deterministic month
            // placement for compatibility with existing profiles.
            int quarterlySlotPosition = ComputeQuarterlySlotPosition(ownerNodeId);
            int annualMonth = ComputeAnnualMonth(ownerNodeId, quarterlySlotPosition);
            if (profile.To3Hours > 0 && month == annualMonth)
                return KbMaintenanceWorkKind.To3;

            if (profile.To2Hours > 0 && IsQuarterlySlotMonth(month, quarterlySlotPosition))
                return KbMaintenanceWorkKind.To2;

            return KbMaintenanceWorkKind.To1;
        }

        private static int ResolveDueHours(
            KbMaintenanceScheduleProfile profile,
            KbMaintenanceYearScheduleEntry? explicitEntry,
            KbMaintenanceWorkKind workKind)
        {
            if (explicitEntry?.WorkKind == workKind && explicitEntry.Hours > 0)
                return explicitEntry.Hours;

            return workKind switch
            {
                KbMaintenanceWorkKind.To1 => profile.To1Hours,
                KbMaintenanceWorkKind.To2 => profile.To2Hours,
                KbMaintenanceWorkKind.To3 => profile.To3Hours,
                _ => 0
            };
        }

        private static KbMaintenanceYearScheduleEntry? ResolveExplicitYearScheduleEntry(
            IReadOnlyList<KbMaintenanceYearScheduleEntry>? yearScheduleEntries,
            int month)
        {
            if (yearScheduleEntries == null || yearScheduleEntries.Count == 0)
                return null;

            return yearScheduleEntries
                .Where(entry => entry != null &&
                                entry.Month == month &&
                                Enum.IsDefined(typeof(KbMaintenanceWorkKind), entry.WorkKind))
                .OrderByDescending(static entry => entry.WorkKind)
                .ThenByDescending(static entry => entry.Hours)
                .FirstOrDefault();
        }

        private static bool IsQuarterlySlotMonth(int month, int quarterlySlotPosition)
        {
            return ((month - 1) % 3) == quarterlySlotPosition;
        }

        private static int ComputeQuarterlySlotPosition(string ownerNodeId)
        {
            return ComputeStableOffset(ownerNodeId, "TO2") % 3;
        }

        private static int ComputeAnnualMonth(string ownerNodeId, int quarterlySlotPosition)
        {
            int annualQuarterIndex = ComputeStableOffset(ownerNodeId, "TO3") % 4;
            return 1 + (annualQuarterIndex * 3) + quarterlySlotPosition;
        }

        private static int ComputeStableOffset(string ownerNodeId, string salt)
        {
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{ownerNodeId}|{salt}"));
            int value = BitConverter.ToInt32(bytes, startIndex: 0);
            return Math.Abs(value == int.MinValue ? int.MaxValue : value);
        }
    }
}
