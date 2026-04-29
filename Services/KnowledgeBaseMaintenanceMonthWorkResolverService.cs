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
            foreach (KbNode node in EnumerateNodesPreOrder(roots))
            {
                string ownerNodeId = node.NodeId?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(ownerNodeId))
                    continue;

                if (!profileByOwnerNodeId.TryGetValue(ownerNodeId, out KbMaintenanceScheduleProfile? profile))
                    continue;

                if (!profile.IsIncludedInSchedule || !KnowledgeBaseMaintenanceScheduleStateService.SupportsProfile(node.NodeType))
                    continue;

                // TO2 includes TO1, and TO3 includes both TO1 and TO2.
                // The yearly schedule therefore uses one maintenance slot per month:
                // quarterly slot months get TO2, one of those quarters gets TO3 instead, and the rest get TO1.
                // For a full profile (TO1 + TO2 + TO3) this yields 8x TO1, 3x TO2, and 1x TO3 per year.
                int quarterlySlotPosition = ComputeQuarterlySlotPosition(ownerNodeId);
                int annualMonth = ComputeAnnualMonth(ownerNodeId, quarterlySlotPosition);
                bool isAnnualDue = profile.To3Hours > 0 && month == annualMonth;
                bool isQuarterlyDue = profile.To2Hours > 0 && IsQuarterlySlotMonth(month, quarterlySlotPosition) && !isAnnualDue;
                bool isMonthlyDue = profile.To1Hours > 0 && !isQuarterlyDue && !isAnnualDue;

                AddWorkItemIfDue(workItems, node, profile, month, KbMaintenanceWorkKind.To3, profile.To3Hours, isAnnualDue);
                AddWorkItemIfDue(workItems, node, profile, month, KbMaintenanceWorkKind.To2, profile.To2Hours, isQuarterlyDue);
                AddWorkItemIfDue(workItems, node, profile, month, KbMaintenanceWorkKind.To1, profile.To1Hours, isMonthlyDue);
            }

            return workItems;
        }

        private static IEnumerable<KbNode> EnumerateNodesPreOrder(IEnumerable<KbNode> roots)
        {
            foreach (KbNode node in roots)
            {
                yield return node;

                foreach (KbNode child in EnumerateNodesPreOrder(node.Children))
                    yield return child;
            }
        }

        private static void AddWorkItemIfDue(
            ICollection<KbMaintenanceMonthWorkItem> workItems,
            KbNode node,
            KbMaintenanceScheduleProfile profile,
            int month,
            KbMaintenanceWorkKind workKind,
            int hours,
            bool isDue)
        {
            if (!isDue || hours <= 0)
                return;

            workItems.Add(new KbMaintenanceMonthWorkItem
            {
                OwnerNodeId = profile.OwnerNodeId?.Trim() ?? string.Empty,
                NodeName = node.Name?.Trim() ?? string.Empty,
                WorkKind = workKind,
                Hours = hours
            });
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
