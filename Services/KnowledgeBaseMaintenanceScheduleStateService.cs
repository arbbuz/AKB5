using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public sealed class KnowledgeBaseMaintenanceScheduleState
    {
        public bool SupportsEditing { get; init; }

        public string SourceText { get; init; } = string.Empty;

        public string SummaryText { get; init; } = string.Empty;

        public string EmptyStateText { get; init; } = string.Empty;

        public bool HasProfile { get; init; }

        public string MaintenanceProfileId { get; init; } = string.Empty;

        public bool IsIncludedInSchedule { get; init; }

        public string InclusionText { get; init; } = string.Empty;

        public string To1HoursText { get; init; } = "-";

        public string To2HoursText { get; init; } = "-";

        public string To3HoursText { get; init; } = "-";
    }

    public class KnowledgeBaseMaintenanceScheduleStateService
    {
        public KnowledgeBaseMaintenanceScheduleState Build(
            KbNode? selectedNode,
            IReadOnlyList<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles,
            int visibleLevel = 0)
        {
            if (selectedNode == null || !SupportsProfile(selectedNode.NodeType, visibleLevel))
            {
                return new KnowledgeBaseMaintenanceScheduleState
                {
                    EmptyStateText = "Вкладка \"График ТО\" недоступна для выбранного узла."
                };
            }

            string ownerNodeId = selectedNode.NodeId?.Trim() ?? string.Empty;
            KbMaintenanceScheduleProfile? profile = FindOwnedProfile(ownerNodeId, maintenanceScheduleProfiles);
            if (profile == null)
            {
                return new KnowledgeBaseMaintenanceScheduleState
                {
                    SupportsEditing = true,
                    SourceText = "Настройте участие узла в графике ТО и задайте нормы часов для ТО1, ТО2 и ТО3.",
                    SummaryText = "Профиль ТО для этого узла ещё не настроен.",
                    EmptyStateText = "Профиль ТО для этого узла ещё не настроен."
                };
            }

            return new KnowledgeBaseMaintenanceScheduleState
            {
                SupportsEditing = true,
                SourceText = "Профиль ТО задаёт участие узла в месячном планировании и нормы часов для ТО1, ТО2 и ТО3.",
                SummaryText = profile.IsIncludedInSchedule
                    ? "Узел включён в график ТО."
                    : "Узел исключён из графика ТО.",
                EmptyStateText = "Профиль ТО для этого узла ещё не настроен.",
                HasProfile = true,
                MaintenanceProfileId = profile.MaintenanceProfileId,
                IsIncludedInSchedule = profile.IsIncludedInSchedule,
                InclusionText = profile.IsIncludedInSchedule ? "Да" : "Нет",
                To1HoursText = FormatHours(profile.To1Hours),
                To2HoursText = FormatHours(profile.To2Hours),
                To3HoursText = FormatHours(profile.To3Hours)
            };
        }

        public static bool SupportsProfile(KbNodeType nodeType, int visibleLevel = 0) =>
            KnowledgeBaseEngineeringNodeSupportService.SupportsEngineeringWorkspace(nodeType, visibleLevel);

        private static KbMaintenanceScheduleProfile? FindOwnedProfile(
            string ownerNodeId,
            IReadOnlyList<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles)
        {
            if (string.IsNullOrWhiteSpace(ownerNodeId) || maintenanceScheduleProfiles == null)
                return null;

            return maintenanceScheduleProfiles
                .Where(profile => string.Equals(profile.OwnerNodeId, ownerNodeId, StringComparison.Ordinal))
                .OrderBy(profile => profile.MaintenanceProfileId, StringComparer.Ordinal)
                .FirstOrDefault();
        }

        private static string FormatHours(int hours) => $"{Math.Max(0, hours)} ч";
    }
}
