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
            KbMaintenanceScheduleProfile? draftProfile)
        {
            if (!TryValidateOwnerNode(ownerNode, out var ownerNodeId, out var errorMessage))
                return Failure(errorMessage);

            if (draftProfile == null)
                return Failure("Черновик профиля ТО не был передан.");

            if (!TryValidateHours(draftProfile.To1Hours, "ТО1", out errorMessage) ||
                !TryValidateHours(draftProfile.To2Hours, "ТО2", out errorMessage) ||
                !TryValidateHours(draftProfile.To3Hours, "ТО3", out errorMessage))
            {
                return Failure(errorMessage);
            }

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
                To3Hours = draftProfile.To3Hours
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
            string? maintenanceProfileId)
        {
            if (!TryValidateOwnerNode(ownerNode, out var ownerNodeId, out var errorMessage))
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
            out string ownerNodeId,
            out string errorMessage)
        {
            if (ownerNode == null)
            {
                ownerNodeId = string.Empty;
                errorMessage = "Не выбран узел для редактирования профиля ТО.";
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
            if (hours is < 0 or > 8)
            {
                errorMessage = $"Норма часов для {workType} должна быть в диапазоне от 0 до 8.";
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
                    To3Hours = profile.To3Hours
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
