using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase
{
    public partial class MainForm
    {
        private void ConfigureMaintenanceScheduleProfile(object? sender, EventArgs e)
        {
            if (!TryGetMaintenanceOwnerNode(out var ownerNode))
                return;

            var draftProfile = FindMaintenanceScheduleProfile(ownerNode) ??
                new KbMaintenanceScheduleProfile
                {
                    OwnerNodeId = ownerNode.NodeId,
                    IsIncludedInSchedule = true
                };

            EditMaintenanceScheduleProfileCore(
                ownerNode,
                CloneMaintenanceScheduleProfile(draftProfile),
                "Настроить профиль ТО",
                draftProfile.MaintenanceProfileId.Length == 0
                    ? "Профиль ТО сохранён."
                    : "Профиль ТО обновлён.");
        }

        private void DeleteMaintenanceScheduleProfile(object? sender, EventArgs e)
        {
            if (!TryGetMaintenanceOwnerNode(out var ownerNode))
                return;

            var profile = FindMaintenanceScheduleProfile(ownerNode);
            if (profile == null)
            {
                MessageBox.Show(
                    this,
                    "Для выбранного узла профиль ТО ещё не настроен.",
                    "График ТО",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            var confirmResult = MessageBox.Show(
                this,
                $"Удалить профиль ТО для узла \"{ownerNode.Name}\"?",
                "График ТО",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Warning);
            if (confirmResult != DialogResult.OK)
                return;

            ApplyMaintenanceScheduleProfileMutation(
                _maintenanceScheduleProfileMutationService.DeleteMaintenanceScheduleProfile(
                    ownerNode,
                    _session.MaintenanceScheduleProfiles,
                    profile.MaintenanceProfileId,
                    GetVisibleLevelForNode(ownerNode)),
                "Профиль ТО удалён.");
        }

        private void EditMaintenanceScheduleProfileCore(
            KbNode ownerNode,
            KbMaintenanceScheduleProfile draftProfile,
            string dialogTitle,
            string successStatusText)
        {
            using var dialog = new KnowledgeBaseMaintenanceScheduleProfileDialog(dialogTitle, draftProfile);
            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            ApplyMaintenanceScheduleProfileMutation(
                _maintenanceScheduleProfileMutationService.UpsertMaintenanceScheduleProfile(
                    ownerNode,
                    _session.MaintenanceScheduleProfiles,
                    dialog.Result,
                    GetVisibleLevelForNode(ownerNode)),
                successStatusText);
        }

        private void ApplyMaintenanceScheduleProfileMutation(
            KnowledgeBaseMaintenanceScheduleProfileMutationResult result,
            string successStatusText)
        {
            if (!result.IsSuccess)
            {
                MessageBox.Show(
                    this,
                    result.ErrorMessage,
                    "График ТО",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            _session.ReplaceMaintenanceScheduleProfiles(result.MaintenanceScheduleProfiles);
            UpdateDirtyState();
            UpdateUI();
            SetLastActionText(successStatusText);
        }

        private bool TryGetMaintenanceOwnerNode(out KbNode ownerNode)
        {
            ownerNode = new KbNode();
            if (TryGetSelectedTreeNode(out KbNode selectedNode) &&
                KnowledgeBaseMaintenanceScheduleStateService.SupportsProfile(
                    selectedNode.NodeType,
                    GetVisibleLevelForNode(selectedNode)))
            {
                ownerNode = selectedNode;
                return true;
            }

            MessageBox.Show(
                this,
                "График ТО доступен только для инженерных узлов.",
                "График ТО",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return false;
        }

        private KbMaintenanceScheduleProfile? FindMaintenanceScheduleProfile(KbNode ownerNode)
        {
            return _session.MaintenanceScheduleProfiles
                .Where(profile => string.Equals(profile.OwnerNodeId, ownerNode.NodeId, StringComparison.Ordinal))
                .OrderBy(profile => profile.MaintenanceProfileId, StringComparer.Ordinal)
                .FirstOrDefault();
        }

        private static KbMaintenanceScheduleProfile CloneMaintenanceScheduleProfile(
            KbMaintenanceScheduleProfile profile) =>
            new()
            {
                MaintenanceProfileId = profile.MaintenanceProfileId,
                OwnerNodeId = profile.OwnerNodeId,
                IsIncludedInSchedule = profile.IsIncludedInSchedule,
                To1Hours = profile.To1Hours,
                To2Hours = profile.To2Hours,
                To3Hours = profile.To3Hours
            };
    }
}
