using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase
{
    public partial class MainForm
    {
        private void AddCompositionRack(object? sender, EventArgs e)
        {
            if (!TryGetCompositionParentNode(out var parentNode))
                return;

            var draftRack = _compositionRackMutationService.CreateAddDraft(
                parentNode,
                _session.CompositionRacks,
                _session.CompositionEntries);
            using var dialog = new KnowledgeBaseCompositionRackDialog("Добавить Rack", draftRack);
            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            ApplyCompositionRackMutation(
                _compositionRackMutationService.UpsertRack(
                    parentNode,
                    _session.CompositionRacks,
                    dialog.Result,
                    GetVisibleLevelForNode(parentNode)),
                "Rack добавлен.");
        }

        private void EditSelectedCompositionRack(object? sender, EventArgs e)
        {
            if (!TryGetCompositionParentNode(out var parentNode))
                return;

            var selectedRack = BuildSelectedRackDraft(parentNode);
            if (selectedRack == null)
            {
                MessageBox.Show(
                    this,
                    "Выберите Rack для изменения.",
                    "Состав",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            using var dialog = new KnowledgeBaseCompositionRackDialog("Изменить Rack", selectedRack);
            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            ApplyCompositionRackMutation(
                _compositionRackMutationService.UpsertRack(
                    parentNode,
                    _session.CompositionRacks,
                    dialog.Result,
                    GetVisibleLevelForNode(parentNode)),
                "Rack обновлён.");
        }

        private void DeleteSelectedCompositionRack(object? sender, EventArgs e)
        {
            if (!TryGetCompositionParentNode(out var parentNode))
                return;

            int rackNumber = selectedNodeCompositionScreen.SelectedRackNumber;
            var confirmResult = MessageBox.Show(
                this,
                $"Удалить пустой {KnowledgeBaseCompositionRackSlotRulesService.FormatRackText(rackNumber)}?",
                "Состав",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Warning);
            if (confirmResult != DialogResult.OK)
                return;

            ApplyCompositionRackMutation(
                _compositionRackMutationService.DeleteRack(
                    parentNode,
                    _session.CompositionRacks,
                    _session.CompositionEntries,
                    rackNumber,
                    GetVisibleLevelForNode(parentNode)),
                "Rack удалён.");
        }

        private void AddSlottedCompositionEntry(object? sender, EventArgs e)
        {
            if (!TryGetCompositionParentNode(out var parentNode))
                return;

            EditCompositionEntryCore(
                parentNode,
                new KbCompositionEntry
                {
                    ParentNodeId = parentNode.NodeId,
                    RackNumber = selectedNodeCompositionScreen.SelectedRackNumber,
                    SlotNumber = ResolveInitialSlotNumber(
                        parentNode.NodeId,
                        selectedNodeCompositionScreen.SelectedRackNumber,
                        selectedNodeCompositionScreen.SelectedSlotNumber),
                    PositionOrder = 0
                },
                "Добавить слот",
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
                    RackNumber = 0,
                    SlotNumber = null,
                    PositionOrder = GetNextAuxiliaryOrder(parentNode.NodeId)
                },
                "Добавить оборудование",
                "Оборудование добавлено в доп. оборудование.");
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
                    _session.CompositionRacks,
                    _session.CompositionEntries,
                    dialog.SelectedSourceNode),
                "Состав скопирован из выбранного объекта.");
        }

        private void EditSelectedCompositionEntry(object? sender, EventArgs e)
        {
            if (!TryGetCompositionParentNode(out var parentNode))
                return;

            var selectedEntry = FindSelectedCompositionEntry(
                parentNode,
                selectedNodeCompositionScreen.SelectedEntryId,
                requireSlotted: true);
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

        private void EditSelectedAuxiliaryCompositionEntry(object? sender, EventArgs e)
        {
            if (!TryGetCompositionParentNode(out var parentNode))
                return;

            var selectedEntry = FindSelectedCompositionEntry(
                parentNode,
                selectedNodeAdditionalEquipmentScreen.SelectedEntryId,
                requireSlotted: false);
            if (selectedEntry == null)
            {
                MessageBox.Show(
                    this,
                    "Выберите доп. оборудование для изменения.",
                    "Доп. оборудование",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            EditCompositionEntryCore(
                parentNode,
                CloneCompositionEntry(selectedEntry),
                "Изменить доп. оборудование",
                "Доп. оборудование обновлено.");
        }

        private void DeleteSelectedCompositionEntry(object? sender, EventArgs e)
        {
            if (!TryGetCompositionParentNode(out var parentNode))
                return;

            var selectedEntry = FindSelectedCompositionEntry(
                parentNode,
                selectedNodeCompositionScreen.SelectedEntryId,
                requireSlotted: true);
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
                _compositionMutationService.DeleteEntry(
                    parentNode,
                    _session.CompositionEntries,
                    selectedEntry.EntryId,
                    GetVisibleLevelForNode(parentNode)),
                "Запись состава удалена.");
        }

        private void DeleteSelectedAuxiliaryCompositionEntry(object? sender, EventArgs e)
        {
            if (!TryGetCompositionParentNode(out var parentNode))
                return;

            var selectedEntry = FindSelectedCompositionEntry(
                parentNode,
                selectedNodeAdditionalEquipmentScreen.SelectedEntryId,
                requireSlotted: false);
            if (selectedEntry == null)
            {
                MessageBox.Show(
                    this,
                    "Выберите доп. оборудование для удаления.",
                    "Доп. оборудование",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            var confirmResult = MessageBox.Show(
                this,
                $"Удалить доп. оборудование \"{GetCompositionEntryDisplayName(selectedEntry)}\"?",
                "Доп. оборудование",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Warning);
            if (confirmResult != DialogResult.OK)
                return;

            ApplyCompositionMutation(
                _compositionMutationService.DeleteEntry(
                    parentNode,
                    _session.CompositionEntries,
                    selectedEntry.EntryId,
                    GetVisibleLevelForNode(parentNode)),
                "Доп. оборудование удалено.");
        }

        private void EditCompositionEntryCore(
            KbNode parentNode,
            KbCompositionEntry draftEntry,
            string dialogTitle,
            string successStatusText)
        {
            using var dialog = new KnowledgeBaseCompositionEntryDialog(
                dialogTitle,
                draftEntry,
                _session.EquipmentCatalogItems);
            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            ApplyCompositionMutation(
                _compositionMutationService.UpsertEntry(
                    parentNode,
                    _session.CompositionEntries,
                    dialog.Result,
                    GetVisibleLevelForNode(parentNode)),
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

        private void ApplyCompositionRackMutation(
            KnowledgeBaseCompositionRackMutationResult result,
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

            _session.ReplaceCompositionRacks(result.CompositionRacks);
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
            _session.ReplaceCompositionRacks(result.CompositionRacks);
            UpdateDirtyState();
            UpdateUI();
            SetLastActionText(successStatusText);
        }

        private bool ConfirmReplaceCompositionEntries(KbNode parentNode)
        {
            if (CountTypedCompositionRecords(parentNode.NodeId) == 0)
                return true;

            return MessageBox.Show(
                this,
                "Текущие Rack и записи состава будут заменены. Продолжить?",
                "Состав",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Warning) == DialogResult.OK;
        }

        private bool TryGetCompositionParentNode(out KbNode parentNode)
        {
            parentNode = new KbNode();
            if (TryGetSelectedTreeNode(out KbNode selectedNode) &&
                KnowledgeBaseCompositionStateService.SupportsComposition(
                    selectedNode.NodeType,
                    GetVisibleLevelForNode(selectedNode)))
            {
                parentNode = selectedNode;
                return true;
            }

            MessageBox.Show(
                this,
                "Вкладки \"Состав\" и \"Доп. оборудование\" доступны только для шкафов, устройств, контроллеров и модулей.",
                "Состав",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return false;
        }

        private KbCompositionEntry? FindSelectedCompositionEntry(
            KbNode parentNode,
            string selectedEntryId,
            bool requireSlotted)
        {
            if (string.IsNullOrWhiteSpace(selectedEntryId))
                return null;

            return _session.CompositionEntries.FirstOrDefault(entry =>
                string.Equals(entry.EntryId, selectedEntryId, StringComparison.Ordinal) &&
                string.Equals(entry.ParentNodeId, parentNode.NodeId, StringComparison.Ordinal) &&
                entry.SlotNumber.HasValue == requireSlotted);
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
                    CountTypedCompositionRecords(node.NodeId) > 0)
                {
                    int entryCount = CountTypedCompositionEntries(node.NodeId);
                    int rackCount = CountTypedCompositionRacks(node.NodeId);
                    string path = _nodePresentationService.BuildNodePath(roots, node);
                    options.Add(new KnowledgeBaseCompositionCopySourceOption
                    {
                        Node = node,
                        DisplayText = path,
                        Description =
                            $"Путь: {path}{Environment.NewLine}" +
                            $"Тип узла: {node.NodeType}{Environment.NewLine}" +
                            $"Rack: {rackCount}{Environment.NewLine}" +
                            $"Записей состава: {entryCount}"
                    });
                }

                CollectCopySourceOptions(roots, node.Children, targetNode, options);
            }
        }

        private int CountTypedCompositionEntries(string parentNodeId) =>
            _session.CompositionEntries.Count(entry =>
                string.Equals(entry.ParentNodeId, parentNodeId, StringComparison.Ordinal));

        private int CountTypedCompositionRacks(string parentNodeId) =>
            _session.CompositionRacks.Count(rack =>
                string.Equals(rack.ParentNodeId, parentNodeId, StringComparison.Ordinal));

        private int CountTypedCompositionRecords(string parentNodeId) =>
            CountTypedCompositionEntries(parentNodeId) + CountTypedCompositionRacks(parentNodeId);

        private KbCompositionRack? BuildSelectedRackDraft(KbNode parentNode)
        {
            int rackNumber = selectedNodeCompositionScreen.SelectedRackNumber;
            KbCompositionRack? savedRack = _session.CompositionRacks.FirstOrDefault(rack =>
                string.Equals(rack.ParentNodeId, parentNode.NodeId, StringComparison.Ordinal) &&
                KnowledgeBaseCompositionRackSlotRulesService.NormalizeRackNumber(rack.RackNumber) == rackNumber);
            if (savedRack != null)
            {
                return new KbCompositionRack
                {
                    RackId = savedRack.RackId,
                    ParentNodeId = savedRack.ParentNodeId,
                    RackNumber = savedRack.RackNumber,
                    SortOrder = savedRack.SortOrder,
                    RackType = savedRack.RackType,
                    Label = savedRack.Label,
                    Notes = savedRack.Notes,
                    Properties = savedRack.Properties
                        .Select(static property => new KbCompositionRackProperty
                        {
                            Name = property.Name,
                            Value = property.Value
                        })
                        .ToList()
                };
            }

            var rackState = selectedNodeCompositionScreen.SelectedRackState;
            return rackState == null
                ? null
                : new KbCompositionRack
                {
                    ParentNodeId = parentNode.NodeId,
                    RackNumber = rackState.RackNumber,
                    SortOrder = rackState.RackNumber,
                    RackType = rackState.RackTypeText,
                    Label = rackState.LabelText,
                    Notes = rackState.NotesText
                };
        }

        private int ResolveInitialSlotNumber(string parentNodeId, int rackNumber, int? selectedSlotNumber)
        {
            int normalizedRackNumber = KnowledgeBaseCompositionRackSlotRulesService.NormalizeRackNumber(rackNumber);
            if (selectedSlotNumber.HasValue &&
                !_session.CompositionEntries.Any(entry =>
                    string.Equals(entry.ParentNodeId, parentNodeId, StringComparison.Ordinal) &&
                    entry.RackNumber == normalizedRackNumber &&
                    entry.SlotNumber == selectedSlotNumber.Value))
            {
                return selectedSlotNumber.Value;
            }

            return GetNextSlotNumber(parentNodeId, normalizedRackNumber);
        }

        private int GetNextSlotNumber(string parentNodeId, int rackNumber)
        {
            int? maxSlot = _session.CompositionEntries
                .Where(entry =>
                    string.Equals(entry.ParentNodeId, parentNodeId, StringComparison.Ordinal) &&
                    entry.RackNumber == rackNumber &&
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
                RackNumber = entry.RackNumber,
                SlotNumber = entry.SlotNumber,
                PositionOrder = entry.PositionOrder,
                ComponentType = entry.ComponentType,
                Model = entry.Model,
                OrderNumber = entry.OrderNumber,
                Firmware = entry.Firmware,
                MpiDpPnAddress = entry.MpiDpPnAddress,
                InputAddress = entry.InputAddress,
                OutputAddress = entry.OutputAddress,
                Comment = entry.Comment,
                InterfaceRows = entry.InterfaceRows,
                IpAddress = entry.IpAddress,
                LastCalibrationAt = entry.LastCalibrationAt,
                NextCalibrationAt = entry.NextCalibrationAt,
                Notes = entry.Notes
            };
    }
}
