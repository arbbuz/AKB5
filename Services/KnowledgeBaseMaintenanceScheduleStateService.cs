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

        public string YearScheduleText { get; init; } = "-";
    }

    public class KnowledgeBaseMaintenanceScheduleStateService
    {
        private const string UnavailableText = "Вкладка \"График ТО\" недоступна для выбранного узла.";
        private const string EmptyProfileText = "Профиль ТО для этого узла ещё не настроен.";
        private const string NewProfileSourceText = "Настройте участие узла в графике ТО и задайте нормы часов для ТО1, ТО2 и ТО3. ТО2 включает ТО1, а ТО3 включает ТО1 и ТО2.";
        private const string ExistingProfileSourceText = "Профиль ТО задаёт участие узла в месячном планировании и нормы часов для ТО1, ТО2 и ТО3. ТО2 включает ТО1, а ТО3 включает ТО1 и ТО2.";

        public KnowledgeBaseMaintenanceScheduleState Build(
            KbNode? selectedNode,
            IReadOnlyList<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles,
            int visibleLevel = 0)
        {
            if (selectedNode == null || !SupportsProfile(selectedNode.NodeType, visibleLevel))
            {
                return new KnowledgeBaseMaintenanceScheduleState
                {
                    EmptyStateText = UnavailableText
                };
            }

            string ownerNodeId = selectedNode.NodeId?.Trim() ?? string.Empty;
            KbMaintenanceScheduleProfile? profile = FindOwnedProfile(ownerNodeId, maintenanceScheduleProfiles);
            if (profile == null)
            {
                return new KnowledgeBaseMaintenanceScheduleState
                {
                    SupportsEditing = true,
                    SourceText = NewProfileSourceText,
                    SummaryText = EmptyProfileText,
                    EmptyStateText = EmptyProfileText
                };
            }

            return new KnowledgeBaseMaintenanceScheduleState
            {
                SupportsEditing = true,
                SourceText = ExistingProfileSourceText,
                SummaryText = profile.IsIncludedInSchedule
                    ? "Узел включён в график ТО."
                    : "Узел исключён из графика ТО.",
                EmptyStateText = EmptyProfileText,
                HasProfile = true,
                MaintenanceProfileId = profile.MaintenanceProfileId,
                IsIncludedInSchedule = profile.IsIncludedInSchedule,
                InclusionText = profile.IsIncludedInSchedule ? "Да" : "Нет",
                To1HoursText = FormatHours(profile.To1Hours),
                To2HoursText = FormatHours(profile.To2Hours),
                To3HoursText = FormatHours(profile.To3Hours),
                YearScheduleText = FormatYearSchedule(profile.YearScheduleEntries)
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

        private static string FormatYearSchedule(IReadOnlyList<KbMaintenanceYearScheduleEntry>? entries)
        {
            if (entries == null || entries.Count == 0)
                return "Автоматический годовой план";

            KbMaintenanceYearScheduleEntry[] validEntries = entries
                .Where(static entry => entry != null &&
                                       entry.Month is >= 1 and <= 12 &&
                                       Enum.IsDefined(typeof(KbMaintenanceWorkKind), entry.WorkKind))
                .GroupBy(static entry => entry.Month)
                .Select(static group => group.Last())
                .ToArray();
            if (validEntries.Length == 0)
                return "Автоматический годовой план";

            int to1Count = validEntries.Count(static entry => entry.WorkKind == KbMaintenanceWorkKind.To1);
            int to2Count = validEntries.Count(static entry => entry.WorkKind == KbMaintenanceWorkKind.To2);
            int to3Count = validEntries.Count(static entry => entry.WorkKind == KbMaintenanceWorkKind.To3);
            int hoursCount = validEntries.Count(static entry => entry.Hours > 0);
            string hoursText = hoursCount > 0
                ? $", часы заданы: {hoursCount}"
                : ", часы из норм профиля";

            return $"Годовой план вручную: {validEntries.Length} мес.; ТО1 - {to1Count}, ТО2 - {to2Count}, ТО3 - {to3Count}{hoursText}";
        }
    }
}
