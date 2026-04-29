using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase
{
    public partial class MainForm
    {
        private void ConfigureMaintenanceScheduleProfile(object? sender, EventArgs e)
        {
            if (!TryGetMaintenanceOwnerNode(out KbNode ownerNode))
                return;

            KbMaintenanceScheduleProfile draftProfile = FindMaintenanceScheduleProfile(ownerNode) ??
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
            if (!TryGetMaintenanceOwnerNode(out KbNode ownerNode))
                return;

            KbMaintenanceScheduleProfile? profile = FindMaintenanceScheduleProfile(ownerNode);
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

            DialogResult confirmResult = MessageBox.Show(
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

        private void ExportMaintenanceWorkbook(object? sender, EventArgs e)
        {
            SaveCurrentWorkshopState();
            _maintenanceWorkbookUiWorkflowService.Export(
                this,
                _currentWorkshop,
                GetVisibleTreeData(),
                _session.MaintenanceScheduleProfiles,
                CurrentDataPath,
                SetLastActionText);
        }

        private void ImportMaintenanceScheduleNorms(object? sender, EventArgs e)
        {
            if (!TryGetMaintenanceOwnerNode(out _))
                return;

            SaveCurrentWorkshopState();

            using var dialog = new OpenFileDialog
            {
                Title = "Импортировать нормы ТО из monthly workbook",
                Filter = "Книги Excel (*.xlsx)|*.xlsx|Все файлы (*.*)|*.*",
                CheckFileExists = true
            };

            string? directory = Path.GetDirectoryName(CurrentDataPath);
            if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
                dialog.InitialDirectory = directory;

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            KnowledgeBaseMaintenanceScheduleNormImportResult importResult;
            try
            {
                byte[] packageBytes;
                using (var stream = new FileStream(dialog.FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                using (var memory = new MemoryStream())
                {
                    stream.CopyTo(memory);
                    packageBytes = memory.ToArray();
                }

                importResult = _maintenanceScheduleNormImportService.ImportWorkbook(
                    packageBytes,
                    GetPersistedTreeData(),
                    _session.MaintenanceScheduleProfiles);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    $"Ошибка чтения Excel-файла: {ex.Message}",
                    "График ТО",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                SetLastActionText($"Ошибка импорта норм ТО: {ex.Message}");
                return;
            }

            if (!importResult.IsSuccess)
            {
                MessageBox.Show(
                    this,
                    importResult.ErrorMessage,
                    "График ТО",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                SetLastActionText($"Ошибка импорта норм ТО: {importResult.ErrorMessage}");
                return;
            }

            _session.ReplaceMaintenanceScheduleProfiles(importResult.MaintenanceScheduleProfiles);
            UpdateDirtyState();
            UpdateUI();

            string summaryText = BuildMaintenanceNormImportSummary(importResult);
            MessageBox.Show(
                this,
                summaryText,
                "График ТО",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            SetLastActionText(
                $"Импортированы нормы ТО: {importResult.CreatedProfileCount + importResult.UpdatedProfileCount} проф.");
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

        private static string BuildMaintenanceNormImportSummary(
            KnowledgeBaseMaintenanceScheduleNormImportResult result)
        {
            var lines = new List<string>
            {
                "Импорт норм ТО завершён.",
                $"Распознано строк оборудования: {result.ImportedEquipmentCount}",
                $"Создано профилей: {result.CreatedProfileCount}",
                $"Обновлено профилей: {result.UpdatedProfileCount}",
                $"Без изменений: {result.UnchangedProfileCount}",
                $"Совпадения по инв. номеру: {result.MatchedByInventoryCount}",
                $"Совпадения по названию: {result.MatchedByNameCount}"
            };

            if (result.UnresolvedEntries.Count > 0)
            {
                lines.Add(string.Empty);
                lines.Add($"Не сопоставлено: {result.UnresolvedEntries.Count}");
                foreach (string unresolvedEntry in result.UnresolvedEntries.Take(10))
                    lines.Add($"- {unresolvedEntry}");

                if (result.UnresolvedEntries.Count > 10)
                    lines.Add($"- ... ещё {result.UnresolvedEntries.Count - 10}");
            }

            return string.Join(Environment.NewLine, lines);
        }
    }
}
