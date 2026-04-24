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
                "Добавлена слотовая позиция в Composition.");
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
                "Добавить вспомогательное оборудование",
                "Добавлена вспомогательная позиция в Composition.");
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
                    "Выберите typed composition entry для изменения.",
                    "Composition",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            EditCompositionEntryCore(
                parentNode,
                CloneCompositionEntry(selectedEntry),
                "Изменить запись состава",
                "Запись Composition обновлена.");
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
                    "Выберите typed composition entry для удаления.",
                    "Composition",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            var confirmResult = MessageBox.Show(
                this,
                $"Удалить запись состава \"{selectedEntry.Model}\"?",
                "Composition",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Warning);
            if (confirmResult != DialogResult.OK)
                return;

            ApplyCompositionMutation(
                _compositionMutationService.DeleteEntry(parentNode, _session.CompositionEntries, selectedEntry.EntryId),
                "Запись Composition удалена.");
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
                    "Composition",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            _session.ReplaceCompositionEntries(result.CompositionEntries);
            UpdateDirtyState();
            UpdateUI();
            SetLastActionText(successStatusText);
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
                "Composition доступен только для шкафов и typed engineering узлов.",
                "Composition",
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
