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

                AddWorkItemIfDue(workItems, node, profile, month, KbMaintenanceWorkKind.To1, profile.To1Hours, isDue: true);
                AddWorkItemIfDue(
                    workItems,
                    node,
                    profile,
                    month,
                    KbMaintenanceWorkKind.To2,
                    profile.To2Hours,
                    isDue: IsSemiAnnualDue(ownerNodeId, month));
                AddWorkItemIfDue(
                    workItems,
                    node,
                    profile,
                    month,
                    KbMaintenanceWorkKind.To3,
                    profile.To3Hours,
                    isDue: IsAnnualDue(ownerNodeId, month));
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

        private static bool IsSemiAnnualDue(string ownerNodeId, int month)
        {
            int firstMonth = 1 + ComputeStableOffset(ownerNodeId, "TO2") % 6;
            return month == firstMonth || month == firstMonth + 6;
        }

        private static bool IsAnnualDue(string ownerNodeId, int month)
        {
            int annualMonth = 1 + ComputeStableOffset(ownerNodeId, "TO3") % 12;
            return month == annualMonth;
        }

        private static int ComputeStableOffset(string ownerNodeId, string salt)
        {
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{ownerNodeId}|{salt}"));
            int value = BitConverter.ToInt32(bytes, startIndex: 0);
            return Math.Abs(value == int.MinValue ? int.MaxValue : value);
        }
    }
}
