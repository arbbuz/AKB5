using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase
{
    public partial class MainForm
    {
        private void AddSlottedCompositionEntry(object? sender, EventArgs e)
        {
            if (!TryGetCompositionParentNode(out var parentNode))
                return;

            EditCompositionEntryCore(
                parentNode,
                new KbCompositionEntry
                {
                    ParentNodeId = parentNode.NodeId,
                    SlotNumber = GetNextSlotNumber(parentNode.NodeId),
                    PositionOrder = 0
                },
                "Добавить слот состава",
                "Позиция в слоте добавлена.");
        }

        private void AddAuxiliaryCompositionEntry(object? sender, EventArgs e)
        {
            if (!TryGetCompositionParentNode(out var parentNode))
                return;

            EditCompositionEntryCore(
                parentNode,
                new KbCompositionEntry
                {
                    ParentNodeId = parentNode.NodeId,
                    SlotNumber = null,
                    PositionOrder = GetNextAuxiliaryOrder(parentNode.NodeId)
                },
                "Добавить оборудование",
                "Оборудование добавлено в состав.");
        }

        private void ApplyCompositionTemplate(object? sender, EventArgs e)
        {
            if (!TryGetCompositionParentNode(out var parentNode))
                return;

            var templates = _compositionTemplateService.GetTemplates(parentNode.NodeType);
            if (templates.Count == 0)
            {
                MessageBox.Show(
                    this,
                    "Для выбранного типа узла нет доступных шаблонов состава.",
                    "Состав",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            using var dialog = new KnowledgeBaseCompositionTemplateDialog(
                "Применить шаблон состава",
                "Выберите шаблон для текущего объекта:",
                templates,
                collectNodeName: false);
            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            if (!ConfirmReplaceCompositionEntries(parentNode))
                return;

            ApplyCompositionTransfer(
                _compositionTemplateService.ApplyTemplate(parentNode, _session.CompositionEntries, dialog.SelectedTemplateId),
                "Состав заполнен по шаблону.");
        }

        private void CopyCompositionFromExistingObject(object? sender, EventArgs e)
        {
            if (!TryGetCompositionParentNode(out var parentNode))
                return;

            var options = BuildCopySourceOptions(parentNode);
            if (options.Count == 0)
            {
                MessageBox.Show(
                    this,
                    "В текущем цехе нет подходящих объектов с заполненным составом.",
                    "Состав",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            using var dialog = new KnowledgeBaseCompositionCopySourceDialog(options);
            if (dialog.ShowDialog(this) != DialogResult.OK || dialog.SelectedSourceNode == null)
                return;

            if (!ConfirmReplaceCompositionEntries(parentNode))
                return;

            ApplyCompositionTransfer(
                _compositionTemplateService.CopyComposition(
                    parentNode,
                    _session.CompositionEntries,
                    dialog.SelectedSourceNode),
                "Состав скопирован из выбранного объекта.");
        }

        private void EditSelectedCompositionEntry(object? sender, EventArgs e)
        {
            if (!TryGetCompositionParentNode(out var parentNode))
                return;

            var selectedEntry = FindSelectedCompositionEntry(parentNode);
            if (selectedEntry == null)
            {
                MessageBox.Show(
                    this,
                    "Выберите запись состава для изменения.",
                    "Состав",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            EditCompositionEntryCore(
                parentNode,
                CloneCompositionEntry(selectedEntry),
                "Изменить запись состава",
                "Запись состава обновлена.");
        }

        private void DeleteSelectedCompositionEntry(object? sender, EventArgs e)
        {
            if (!TryGetCompositionParentNode(out var parentNode))
                return;

            var selectedEntry = FindSelectedCompositionEntry(parentNode);
            if (selectedEntry == null)
            {
                MessageBox.Show(
                    this,
                    "Выберите запись состава для удаления.",
                    "Состав",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            var confirmResult = MessageBox.Show(
                this,
                $"Удалить запись состава \"{GetCompositionEntryDisplayName(selectedEntry)}\"?",
                "Состав",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Warning);
            if (confirmResult != DialogResult.OK)
                return;

            ApplyCompositionMutation(
                _compositionMutationService.DeleteEntry(parentNode, _session.CompositionEntries, selectedEntry.EntryId),
                "Запись состава удалена.");
        }

        private void EditCompositionEntryCore(
            KbNode parentNode,
            KbCompositionEntry draftEntry,
            string dialogTitle,
            string successStatusText)
        {
            using var dialog = new KnowledgeBaseCompositionEntryDialog(dialogTitle, draftEntry);
            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            ApplyCompositionMutation(
                _compositionMutationService.UpsertEntry(parentNode, _session.CompositionEntries, dialog.Result),
                successStatusText);
        }

        private void ApplyCompositionMutation(
            KnowledgeBaseCompositionMutationResult result,
            string successStatusText)
        {
            if (!result.IsSuccess)
            {
                MessageBox.Show(
                    this,
                    result.ErrorMessage,
                    "Состав",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            _session.ReplaceCompositionEntries(result.CompositionEntries);
            UpdateDirtyState();
            UpdateUI();
            SetLastActionText(successStatusText);
        }

        private void ApplyCompositionTransfer(
            KnowledgeBaseCompositionTransferResult result,
            string successStatusText)
        {
            if (!result.IsSuccess)
            {
                MessageBox.Show(
                    this,
                    result.ErrorMessage,
                    "Состав",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            _session.ReplaceCompositionEntries(result.CompositionEntries);
            UpdateDirtyState();
            UpdateUI();
            SetLastActionText(successStatusText);
        }

        private bool ConfirmReplaceCompositionEntries(KbNode parentNode)
        {
            if (CountTypedCompositionEntries(parentNode.NodeId) == 0)
                return true;

            return MessageBox.Show(
                this,
                "Текущий состав будет заменен. Продолжить?",
                "Состав",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Warning) == DialogResult.OK;
        }

        private bool TryGetCompositionParentNode(out KbNode parentNode)
        {
            parentNode = tvTree.SelectedNode?.Tag as KbNode ?? new KbNode();
            if (tvTree.SelectedNode?.Tag is KbNode selectedNode &&
                KnowledgeBaseCompositionStateService.SupportsComposition(selectedNode.NodeType))
            {
                parentNode = selectedNode;
                return true;
            }

            MessageBox.Show(
                this,
                "Вкладка \"Состав\" доступна только для шкафов, устройств, контроллеров и модулей.",
                "Состав",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return false;
        }

        private KbCompositionEntry? FindSelectedCompositionEntry(KbNode parentNode)
        {
            string selectedEntryId = selectedNodeCompositionScreen.SelectedEntryId;
            if (string.IsNullOrWhiteSpace(selectedEntryId))
                return null;

            return _session.CompositionEntries.FirstOrDefault(entry =>
                string.Equals(entry.EntryId, selectedEntryId, StringComparison.Ordinal) &&
                string.Equals(entry.ParentNodeId, parentNode.NodeId, StringComparison.Ordinal));
        }

        private IReadOnlyList<KnowledgeBaseCompositionCopySourceOption> BuildCopySourceOptions(KbNode targetNode)
        {
            var options = new List<KnowledgeBaseCompositionCopySourceOption>();
            var roots = GetVisibleTreeData();
            CollectCopySourceOptions(roots, roots, targetNode, options);

            return options
                .OrderBy(option => option.DisplayText, KnowledgeBaseNaturalStringComparer.Instance)
                .ToList();
        }

        private void CollectCopySourceOptions(
            IReadOnlyList<KbNode> roots,
            IEnumerable<KbNode> nodes,
            KbNode targetNode,
            ICollection<KnowledgeBaseCompositionCopySourceOption> options)
        {
            foreach (var node in nodes)
            {
                if (!ReferenceEquals(node, targetNode) &&
                    node.NodeType == targetNode.NodeType &&
                    CountTypedCompositionEntries(node.NodeId) > 0)
                {
                    int entryCount = CountTypedCompositionEntries(node.NodeId);
                    string path = _nodePresentationService.BuildNodePath(roots, node);
                    options.Add(new KnowledgeBaseCompositionCopySourceOption
                    {
                        Node = node,
                        DisplayText = path,
                        Description =
                            $"Путь: {path}{Environment.NewLine}" +
                            $"Тип узла: {node.NodeType}{Environment.NewLine}" +
                            $"Записей состава: {entryCount}"
                    });
                }

                CollectCopySourceOptions(roots, node.Children, targetNode, options);
            }
        }

        private int CountTypedCompositionEntries(string parentNodeId) =>
            _session.CompositionEntries.Count(entry =>
                string.Equals(entry.ParentNodeId, parentNodeId, StringComparison.Ordinal));

        private int GetNextSlotNumber(string parentNodeId)
        {
            int? maxSlot = _session.CompositionEntries
                .Where(entry =>
                    string.Equals(entry.ParentNodeId, parentNodeId, StringComparison.Ordinal) &&
                    entry.SlotNumber.HasValue)
                .Select(entry => entry.SlotNumber)
                .Max();

            return (maxSlot ?? 0) + 1;
        }

        private int GetNextAuxiliaryOrder(string parentNodeId)
        {
            int? maxOrder = _session.CompositionEntries
                .Where(entry =>
                    string.Equals(entry.ParentNodeId, parentNodeId, StringComparison.Ordinal) &&
                    !entry.SlotNumber.HasValue)
                .Select(entry => (int?)entry.PositionOrder)
                .Max();

            return (maxOrder ?? -1) + 1;
        }

        private static string GetCompositionEntryDisplayName(KbCompositionEntry entry)
        {
            string model = entry.Model?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(model))
                return model;

            string componentType = entry.ComponentType?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(componentType))
                return componentType;

            return "без названия";
        }

        private static KbCompositionEntry CloneCompositionEntry(KbCompositionEntry entry) =>
            new()
            {
                EntryId = entry.EntryId,
                ParentNodeId = entry.ParentNodeId,
                SlotNumber = entry.SlotNumber,
                PositionOrder = entry.PositionOrder,
                ComponentType = entry.ComponentType,
                Model = entry.Model,
                IpAddress = entry.IpAddress,
                LastCalibrationAt = entry.LastCalibrationAt,
                NextCalibrationAt = entry.NextCalibrationAt,
                Notes = entry.Notes
            };
    }
}
