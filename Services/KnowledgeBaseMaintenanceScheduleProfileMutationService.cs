using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public sealed class KnowledgeBaseMaintenanceScheduleProfileMutationResult
    {
        public bool IsSuccess { get; init; }

        public string ErrorMessage { get; init; } = string.Empty;

        public List<KbMaintenanceScheduleProfile> MaintenanceScheduleProfiles { get; init; } = new();
    }

    public class KnowledgeBaseMaintenanceScheduleProfileMutationService
    {
        public KnowledgeBaseMaintenanceScheduleProfileMutationResult UpsertMaintenanceScheduleProfile(
            KbNode? ownerNode,
            IReadOnlyList<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles,
            KbMaintenanceScheduleProfile? draftProfile,
            int visibleLevel = 0)
        {
            if (!TryValidateOwnerNode(ownerNode, visibleLevel, out var ownerNodeId, out var errorMessage))
                return Failure(errorMessage);

            if (draftProfile == null)
                return Failure("Черновик профиля ТО не был передан.");

            if (!TryValidateHours(draftProfile.To1Hours, "ТО1", out errorMessage) ||
                !TryValidateHours(draftProfile.To2Hours, "ТО2", out errorMessage) ||
                !TryValidateHours(draftProfile.To3Hours, "ТО3", out errorMessage))
            {
                return Failure(errorMessage);
            }

            if (!TryValidateYearScheduleEntries(draftProfile.YearScheduleEntries, out errorMessage))
                return Failure(errorMessage);

            var updatedProfiles = CloneProfiles(maintenanceScheduleProfiles);
            int existingIndex = ResolveExistingProfileIndex(updatedProfiles, ownerNodeId, draftProfile.MaintenanceProfileId);
            if (existingIndex >= 0 &&
                !string.Equals(updatedProfiles[existingIndex].OwnerNodeId, ownerNodeId, StringComparison.Ordinal))
            {
                return Failure("Нельзя перенести профиль ТО на другой узел через редактирование.");
            }

            var normalizedDraft = new KbMaintenanceScheduleProfile
            {
                MaintenanceProfileId = draftProfile.MaintenanceProfileId?.Trim() ?? string.Empty,
                OwnerNodeId = ownerNodeId,
                IsIncludedInSchedule = draftProfile.IsIncludedInSchedule,
                To1Hours = draftProfile.To1Hours,
                To2Hours = draftProfile.To2Hours,
                To3Hours = draftProfile.To3Hours,
                YearScheduleEntries = CloneYearScheduleEntries(draftProfile.YearScheduleEntries)
            };

            if (existingIndex >= 0)
                updatedProfiles[existingIndex] = normalizedDraft;
            else
                updatedProfiles.Add(normalizedDraft);

            return Success(updatedProfiles);
        }

        public KnowledgeBaseMaintenanceScheduleProfileMutationResult DeleteMaintenanceScheduleProfile(
            KbNode? ownerNode,
            IReadOnlyList<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles,
            string? maintenanceProfileId,
            int visibleLevel = 0)
        {
            if (!TryValidateOwnerNode(ownerNode, visibleLevel, out var ownerNodeId, out var errorMessage))
                return Failure(errorMessage);

            string normalizedProfileId = maintenanceProfileId?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalizedProfileId))
                return Failure("Профиль ТО не выбран.");

            var updatedProfiles = CloneProfiles(maintenanceScheduleProfiles);
            int removedCount = updatedProfiles.RemoveAll(profile =>
                string.Equals(profile.MaintenanceProfileId, normalizedProfileId, StringComparison.Ordinal) &&
                string.Equals(profile.OwnerNodeId, ownerNodeId, StringComparison.Ordinal));

            return removedCount == 0
                ? Failure("Не удалось найти выбранный профиль ТО.")
                : Success(updatedProfiles);
        }

        private static int ResolveExistingProfileIndex(
            List<KbMaintenanceScheduleProfile> maintenanceScheduleProfiles,
            string ownerNodeId,
            string? maintenanceProfileId)
        {
            string normalizedProfileId = maintenanceProfileId?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(normalizedProfileId))
            {
                return maintenanceScheduleProfiles.FindIndex(profile =>
                    string.Equals(profile.MaintenanceProfileId, normalizedProfileId, StringComparison.Ordinal));
            }

            return maintenanceScheduleProfiles.FindIndex(profile =>
                string.Equals(profile.OwnerNodeId, ownerNodeId, StringComparison.Ordinal));
        }

        private static bool TryValidateOwnerNode(
            KbNode? ownerNode,
            int visibleLevel,
            out string ownerNodeId,
            out string errorMessage)
        {
            if (ownerNode == null)
            {
                ownerNodeId = string.Empty;
                errorMessage = "Не выбран узел для редактирования профиля ТО.";
                return false;
            }

            if (!KnowledgeBaseMaintenanceScheduleStateService.SupportsProfile(ownerNode.NodeType, visibleLevel))
            {
                ownerNodeId = string.Empty;
                errorMessage = "Для выбранного узла вкладка \"График ТО\" недоступна.";
                return false;
            }

            ownerNodeId = ownerNode.NodeId?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(ownerNodeId))
            {
                errorMessage = "У выбранного узла отсутствует NodeId.";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        private static bool TryValidateHours(int hours, string workType, out string errorMessage)
        {
            if (hours < 0)
            {
                errorMessage = $"Норма часов для {workType} не может быть отрицательной.";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        private static List<KbMaintenanceScheduleProfile> CloneProfiles(
            IReadOnlyList<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles)
        {
            var clones = new List<KbMaintenanceScheduleProfile>();
            if (maintenanceScheduleProfiles == null)
                return clones;

            foreach (var profile in maintenanceScheduleProfiles)
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

        private static bool TryValidateYearScheduleEntries(
            IReadOnlyList<KbMaintenanceYearScheduleEntry>? entries,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            if (entries == null)
                return true;

            var usedMonths = new HashSet<int>();
            foreach (KbMaintenanceYearScheduleEntry entry in entries)
            {
                if (entry == null)
                    continue;

                if (entry.Month < 1 || entry.Month > 12)
                {
                    errorMessage = "Месяц в годовом размещении ТО должен быть в диапазоне от 1 до 12.";
                    return false;
                }

                if (!Enum.IsDefined(typeof(KbMaintenanceWorkKind), entry.WorkKind))
                {
                    errorMessage = "В годовом размещении ТО найден неизвестный тип работ.";
                    return false;
                }

                if (!usedMonths.Add(entry.Month))
                {
                    errorMessage = "В годовом размещении ТО не должно быть дублей одного месяца.";
                    return false;
                }
            }

            return true;
        }

        private static List<KbMaintenanceYearScheduleEntry> CloneYearScheduleEntries(
            IReadOnlyList<KbMaintenanceYearScheduleEntry>? entries)
        {
            var clones = new List<KbMaintenanceYearScheduleEntry>();
            if (entries == null)
                return clones;

            foreach (KbMaintenanceYearScheduleEntry entry in entries
                         .Where(static entry => entry != null)
                         .OrderBy(static entry => entry.Month))
            {
                clones.Add(new KbMaintenanceYearScheduleEntry
                {
                    Month = entry.Month,
                    WorkKind = entry.WorkKind
                });
            }

            return clones;
        }

        private static KnowledgeBaseMaintenanceScheduleProfileMutationResult Success(
            List<KbMaintenanceScheduleProfile> maintenanceScheduleProfiles) =>
            new()
            {
                IsSuccess = true,
                MaintenanceScheduleProfiles = maintenanceScheduleProfiles
            };

        private static KnowledgeBaseMaintenanceScheduleProfileMutationResult Failure(string errorMessage) =>
            new()
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };
    }
}
