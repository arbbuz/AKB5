using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public sealed class KnowledgeBaseMaintenanceMonthDemandSummary
    {
        public int To1ItemCount { get; init; }

        public int To1Hours { get; init; }

        public int To2ItemCount { get; init; }

        public int To2Hours { get; init; }

        public int To3ItemCount { get; init; }

        public int To3Hours { get; init; }

        public int TotalItemCount { get; init; }

        public int TotalHours { get; init; }
    }

    public sealed class KnowledgeBaseMaintenanceMonthDemandSummaryService
    {
        private readonly KnowledgeBaseMaintenanceMonthWorkResolverService _workResolverService;

        public KnowledgeBaseMaintenanceMonthDemandSummaryService(
            KnowledgeBaseMaintenanceMonthWorkResolverService? workResolverService = null)
        {
            _workResolverService = workResolverService ?? new KnowledgeBaseMaintenanceMonthWorkResolverService();
        }

        public KnowledgeBaseMaintenanceMonthDemandSummary Build(
            int year,
            int month,
            IReadOnlyList<KbNode> roots,
            IReadOnlyList<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles)
        {
            IReadOnlyList<KbMaintenanceMonthWorkItem> workItems =
                _workResolverService.ResolveMonthWorkItems(year, month, roots, maintenanceScheduleProfiles);

            int to1Hours = workItems
                .Where(static item => item.WorkKind == KbMaintenanceWorkKind.To1)
                .Sum(static item => Math.Max(0, item.Hours));
            int to2Hours = workItems
                .Where(static item => item.WorkKind == KbMaintenanceWorkKind.To2)
                .Sum(static item => Math.Max(0, item.Hours));
            int to3Hours = workItems
                .Where(static item => item.WorkKind == KbMaintenanceWorkKind.To3)
                .Sum(static item => Math.Max(0, item.Hours));

            return new KnowledgeBaseMaintenanceMonthDemandSummary
            {
                To1ItemCount = workItems.Count(static item => item.WorkKind == KbMaintenanceWorkKind.To1),
                To1Hours = to1Hours,
                To2ItemCount = workItems.Count(static item => item.WorkKind == KbMaintenanceWorkKind.To2),
                To2Hours = to2Hours,
                To3ItemCount = workItems.Count(static item => item.WorkKind == KbMaintenanceWorkKind.To3),
                To3Hours = to3Hours,
                TotalItemCount = workItems.Count,
                TotalHours = to1Hours + to2Hours + to3Hours
            };
        }
    }
}
