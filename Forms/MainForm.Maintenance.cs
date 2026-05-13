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

        private void ExportMaintenanceMonthWorkbook(object? sender, EventArgs e)
        {
            SaveCurrentWorkshopState();
            _maintenanceWorkbookUiWorkflowService.Export(
                this,
                _currentWorkshop,
                GetVisibleTreeData(),
                _session.MaintenanceScheduleProfiles,
                _session.Config.ProductionCalendarYears,
                CurrentDataPath,
                SetLastActionText);
        }

        private void ExportMaintenanceYearWorkbook(object? sender, EventArgs e)
        {
            SaveCurrentWorkshopState();
            _maintenanceWorkbookUiWorkflowService.ExportYear(
                this,
                _currentWorkshop,
                GetVisibleTreeData(),
                _session.MaintenanceScheduleProfiles,
                _session.Config.ProductionCalendarYears,
                CurrentDataPath,
                SetLastActionText);
        }

        private void ExportMaintenanceYearMonthlyWorkbook(object? sender, EventArgs e)
        {
            SaveCurrentWorkshopState();
            _maintenanceWorkbookUiWorkflowService.ExportYearMonthly(
                this,
                _currentWorkshop,
                GetVisibleTreeData(),
                _session.MaintenanceScheduleProfiles,
                _session.Config.ProductionCalendarYears,
                CurrentDataPath,
                SetLastActionText);
        }

        private void RecalculateMaintenanceYearWorkbookToDecember(object? sender, EventArgs e)
        {
            SaveCurrentWorkshopState();
            _maintenanceWorkbookUiWorkflowService.RecalculateYearToDecember(
                this,
                _currentWorkshop,
                GetVisibleTreeData(),
                _session.MaintenanceScheduleProfiles,
                _session.Config.ProductionCalendarYears,
                CurrentDataPath,
                SetLastActionText);
        }

        private void ImportMaintenanceScheduleNorms(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_currentWorkshop))
            {
                MessageBox.Show(
                    this,
                    "Сначала выберите цех для импорта норм ТО.",
                    "График ТО",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            SaveCurrentWorkshopState();

            using var dialog = new OpenFileDialog
            {
                Title = "Импортировать нормы ТО из Excel",
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

            int manuallyDisabledMissingProfileCount = OfferDisableMissingAnnualProfiles(importResult);
            if (manuallyDisabledMissingProfileCount < 0)
                return;

            if (!OfferProtectiveSnapshotBeforeDangerousOperation(
                    "импортом норм ТО",
                    $"Перед импортом норм ТО: {Path.GetFileName(dialog.FileName)}"))
            {
                return;
            }

            _session.ReplaceMaintenanceScheduleProfiles(importResult.MaintenanceScheduleProfiles);
            UpdateDirtyState();
            UpdateUI();

            string summaryText = BuildMaintenanceNormImportSummary(importResult, manuallyDisabledMissingProfileCount);
            MessageBox.Show(
                this,
                summaryText,
                "График ТО",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            SetLastActionText(
                $"Импортированы нормы ТО: {importResult.CreatedProfileCount + importResult.UpdatedProfileCount} проф.");
        }

        private int OfferDisableMissingAnnualProfiles(KnowledgeBaseMaintenanceScheduleNormImportResult importResult)
        {
            if (importResult.MissingIncludedProfiles.Count == 0 ||
                importResult.DisabledMissingProfileCount > 0)
            {
                return 0;
            }

            var lines = new List<string>
            {
                "В базе есть включённые профили ТО, которых нет в годовом Excel-файле.",
                string.Empty
            };

            if (importResult.UnresolvedEntries.Count > 0)
            {
                lines.Add(
                    "Автоотключение не выполнено, потому что в файле есть несопоставленные строки. " +
                    "Можно отключить отсутствующие профили сейчас или оставить их включёнными для ручной проверки.");
                lines.Add(string.Empty);
            }

            lines.Add($"Отсутствующих включённых профилей: {importResult.MissingIncludedProfiles.Count}");
            foreach (KnowledgeBaseMaintenanceScheduleMissingProfile missingProfile in importResult.MissingIncludedProfiles.Take(12))
                lines.Add($"- {missingProfile.DisplayText}");

            if (importResult.MissingIncludedProfiles.Count > 12)
                lines.Add($"- ... ещё {importResult.MissingIncludedProfiles.Count - 12}");

            lines.Add(string.Empty);
            lines.Add("Отключить эти профили при импорте?");

            DialogResult decision = MessageBox.Show(
                this,
                string.Join(Environment.NewLine, lines),
                "График ТО",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (decision == DialogResult.Cancel)
                return -1;

            if (decision != DialogResult.Yes)
                return 0;

            HashSet<string> ownerNodeIds = importResult.MissingIncludedProfiles
                .Select(static profile => profile.OwnerNodeId)
                .Where(static ownerNodeId => !string.IsNullOrWhiteSpace(ownerNodeId))
                .ToHashSet(StringComparer.Ordinal);
            int disabledCount = 0;
            foreach (KbMaintenanceScheduleProfile profile in importResult.MaintenanceScheduleProfiles)
            {
                string ownerNodeId = profile.OwnerNodeId?.Trim() ?? string.Empty;
                if (profile.IsIncludedInSchedule && ownerNodeIds.Contains(ownerNodeId))
                {
                    profile.IsIncludedInSchedule = false;
                    disabledCount++;
                }
            }

            return disabledCount;
        }

        private void EditMaintenanceYearScheduleSource(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_currentWorkshop))
            {
                MessageBox.Show(
                    this,
                    "Сначала выберите цех для редактирования источника годового графика ТО.",
                    "График ТО",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            SaveCurrentWorkshopState();

            List<KnowledgeBaseMaintenanceYearScheduleSourceRow> rows =
                _maintenanceYearScheduleSourceService.BuildRows(
                    GetPersistedTreeData(),
                    _session.MaintenanceScheduleProfiles);
            if (rows.Count == 0)
            {
                MessageBox.Show(
                    this,
                    "В текущем цехе нет настроенных профилей ТО для массового редактирования.",
                    "График ТО",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            using var dialog = new KnowledgeBaseMaintenanceYearScheduleSourceDialog(_currentWorkshop, rows);
            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            KnowledgeBaseMaintenanceYearScheduleSourceApplyResult applyResult =
                _maintenanceYearScheduleSourceService.ApplyRows(
                    dialog.ResultRows,
                    GetPersistedTreeData(),
                    _session.MaintenanceScheduleProfiles);
            if (!applyResult.IsSuccess)
            {
                MessageBox.Show(
                    this,
                    applyResult.ErrorMessage,
                    "График ТО",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                SetLastActionText($"Ошибка редактирования источника годового графика ТО: {applyResult.ErrorMessage}");
                return;
            }

            _session.ReplaceMaintenanceScheduleProfiles(applyResult.MaintenanceScheduleProfiles);
            UpdateDirtyState();
            UpdateUI();

            MessageBox.Show(
                this,
                BuildMaintenanceYearScheduleSourceEditSummary(applyResult),
                "График ТО",
                MessageBoxButtons.OK,
                applyResult.UnresolvedRows.Count > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
            SetLastActionText(
                $"Обновлён источник годового графика ТО: {applyResult.UpdatedProfileCount + applyResult.ClearedProfileCount} изм.");
        }

        private void ExportMaintenanceYearScheduleSource(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_currentWorkshop))
            {
                MessageBox.Show(
                    this,
                    "Сначала выберите цех для экспорта источника годового графика ТО.",
                    "График ТО",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            SaveCurrentWorkshopState();

            using var dialog = new SaveFileDialog
            {
                Title = "Экспортировать источник годового графика ТО",
                Filter = "Книги Excel (*.xlsx)|*.xlsx|Все файлы (*.*)|*.*",
                FileName = $"Источник годового графика ТО - {BuildSafeFileNamePart(_currentWorkshop)}.xlsx",
                OverwritePrompt = true
            };

            string? directory = Path.GetDirectoryName(CurrentDataPath);
            if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
                dialog.InitialDirectory = directory;

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            KnowledgeBaseMaintenanceYearScheduleSourceExportResult exportResult =
                _maintenanceYearScheduleSourceExchangeService.ExportWorkbook(
                    GetPersistedTreeData(),
                    _session.MaintenanceScheduleProfiles);

            if (!exportResult.IsSuccess)
            {
                MessageBox.Show(
                    this,
                    exportResult.ErrorMessage,
                    "График ТО",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                SetLastActionText($"Ошибка экспорта источника годового графика ТО: {exportResult.ErrorMessage}");
                return;
            }

            try
            {
                string? targetDirectory = Path.GetDirectoryName(dialog.FileName);
                if (!string.IsNullOrWhiteSpace(targetDirectory))
                    Directory.CreateDirectory(targetDirectory);

                File.WriteAllBytes(dialog.FileName, exportResult.WorkbookPackage);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    $"Ошибка записи Excel-файла: {ex.Message}",
                    "График ТО",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                SetLastActionText($"Ошибка экспорта источника годового графика ТО: {ex.Message}");
                return;
            }

            MessageBox.Show(
                this,
                BuildMaintenanceYearScheduleSourceExportSummary(exportResult),
                "График ТО",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            SetLastActionText($"Экспортирован источник годового графика ТО: {exportResult.ExportedProfileCount} проф.");
        }

        private void ImportMaintenanceYearScheduleSource(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_currentWorkshop))
            {
                MessageBox.Show(
                    this,
                    "Сначала выберите цех для импорта источника годового графика ТО.",
                    "График ТО",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            SaveCurrentWorkshopState();

            using var dialog = new OpenFileDialog
            {
                Title = "Импортировать источник годового графика ТО из Excel",
                Filter = "Книги Excel (*.xlsx)|*.xlsx|Все файлы (*.*)|*.*",
                CheckFileExists = true
            };

            string? directory = Path.GetDirectoryName(CurrentDataPath);
            if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
                dialog.InitialDirectory = directory;

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            KnowledgeBaseMaintenanceYearScheduleSourceImportResult importResult;
            try
            {
                byte[] packageBytes;
                using (var stream = new FileStream(dialog.FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                using (var memory = new MemoryStream())
                {
                    stream.CopyTo(memory);
                    packageBytes = memory.ToArray();
                }

                importResult = _maintenanceYearScheduleSourceExchangeService.ImportWorkbook(
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
                SetLastActionText($"Ошибка импорта источника годового графика ТО: {ex.Message}");
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
                SetLastActionText($"Ошибка импорта источника годового графика ТО: {importResult.ErrorMessage}");
                return;
            }

            _session.ReplaceMaintenanceScheduleProfiles(importResult.MaintenanceScheduleProfiles);
            UpdateDirtyState();
            UpdateUI();

            MessageBox.Show(
                this,
                BuildMaintenanceYearScheduleSourceImportSummary(importResult),
                "График ТО",
                MessageBoxButtons.OK,
                importResult.UnresolvedRows.Count > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
            SetLastActionText(
                $"Импортирован источник годового графика ТО: {importResult.UpdatedProfileCount + importResult.ClearedProfileCount} изм.");
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
                To3Hours = profile.To3Hours,
                YearScheduleEntries = profile.YearScheduleEntries
                    .Select(static entry => new KbMaintenanceYearScheduleEntry
                    {
                        Month = entry.Month,
                        WorkKind = entry.WorkKind,
                        Hours = entry.Hours
                    })
                    .ToList()
            };

        private static string BuildMaintenanceNormImportSummary(
            KnowledgeBaseMaintenanceScheduleNormImportResult result,
            int manuallyDisabledMissingProfileCount = 0)
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

            if (result.YearScheduleAppliedProfileCount > 0)
                lines.Add($"Годовая раскладка обновлена: {result.YearScheduleAppliedProfileCount}");

            if (result.WorkbookWarnings.Count > 0)
            {
                lines.Add(string.Empty);
                lines.Add($"Предупреждения по Excel-итогам: {result.WorkbookWarnings.Count}");
                foreach (string warning in result.WorkbookWarnings.Take(10))
                    lines.Add($"- {warning}");

                if (result.WorkbookWarnings.Count > 10)
                    lines.Add($"- ... ещё {result.WorkbookWarnings.Count - 10}");
            }

            int totalDisabledMissingProfileCount =
                result.DisabledMissingProfileCount + Math.Max(0, manuallyDisabledMissingProfileCount);
            if (result.MissingIncludedProfiles.Count > 0)
            {
                lines.Add(string.Empty);
                if (totalDisabledMissingProfileCount > 0)
                {
                    lines.Add($"Отключено профилей, отсутствующих в годовом файле: {totalDisabledMissingProfileCount}");
                }
                else
                {
                    lines.Add(
                        $"Включены в базе, но отсутствуют в годовом файле: {result.MissingIncludedProfiles.Count}");
                }

                foreach (KnowledgeBaseMaintenanceScheduleMissingProfile missingProfile in result.MissingIncludedProfiles.Take(10))
                    lines.Add($"- {missingProfile.DisplayText}");

                if (result.MissingIncludedProfiles.Count > 10)
                    lines.Add($"- ... ещё {result.MissingIncludedProfiles.Count - 10}");
            }

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

        private static string BuildMaintenanceYearScheduleSourceEditSummary(
            KnowledgeBaseMaintenanceYearScheduleSourceApplyResult result)
        {
            var lines = new List<string>
            {
                "Редактирование источника годового графика ТО завершено.",
                $"Строк обработано: {result.EditedRowCount}",
                $"Обновлено ручных раскладок: {result.UpdatedProfileCount}",
                $"Очищено до автоматического fallback: {result.ClearedProfileCount}",
                $"Без изменений: {result.UnchangedProfileCount}"
            };

            if (result.UnresolvedRows.Count > 0)
            {
                lines.Add(string.Empty);
                lines.Add($"Не сопоставлено: {result.UnresolvedRows.Count}");
                foreach (string unresolvedRow in result.UnresolvedRows.Take(10))
                    lines.Add($"- {unresolvedRow}");

                if (result.UnresolvedRows.Count > 10)
                    lines.Add($"- ... ещё {result.UnresolvedRows.Count - 10}");
            }

            return string.Join(Environment.NewLine, lines);
        }

        private static string BuildMaintenanceYearScheduleSourceExportSummary(
            KnowledgeBaseMaintenanceYearScheduleSourceExportResult result)
        {
            var lines = new List<string>
            {
                "Экспорт источника годового графика ТО завершён.",
                $"Профилей выгружено: {result.ExportedProfileCount}",
                $"С ручной годовой раскладкой: {result.ManualScheduleProfileCount}",
                $"С автоматическим fallback: {result.AutomaticFallbackProfileCount}"
            };

            return string.Join(Environment.NewLine, lines);
        }

        private static string BuildMaintenanceYearScheduleSourceImportSummary(
            KnowledgeBaseMaintenanceYearScheduleSourceImportResult result)
        {
            var lines = new List<string>
            {
                "Импорт источника годового графика ТО завершён.",
                $"Строк обработано: {result.ImportedRowCount}",
                $"Обновлено ручных раскладок: {result.UpdatedProfileCount}",
                $"Очищено до автоматического fallback: {result.ClearedProfileCount}",
                $"Без изменений: {result.UnchangedProfileCount}"
            };

            if (result.UnresolvedRows.Count > 0)
            {
                lines.Add(string.Empty);
                lines.Add($"Не сопоставлено: {result.UnresolvedRows.Count}");
                foreach (string unresolvedRow in result.UnresolvedRows.Take(10))
                    lines.Add($"- {unresolvedRow}");

                if (result.UnresolvedRows.Count > 10)
                    lines.Add($"- ... ещё {result.UnresolvedRows.Count - 10}");
            }

            return string.Join(Environment.NewLine, lines);
        }

        private static string BuildSafeFileNamePart(string value)
        {
            string normalized = value?.Trim() ?? string.Empty;
            foreach (char invalidChar in Path.GetInvalidFileNameChars())
                normalized = normalized.Replace(invalidChar, ' ');

            normalized = string.Join(" ", normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries));
            return string.IsNullOrWhiteSpace(normalized) ? "цех" : normalized;
        }
    }
}
