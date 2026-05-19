using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public enum KnowledgeBaseTreeMutationFailure
    {
        None,
        InvalidNodeName,
        DepthLimitExceeded,
        CatalogUnavailable,
        TemplateUnavailable,
        ClipboardUnavailable,
        DeleteFailed,
        MoveWouldCreateCycle,
        MoveFailed,
        RestoreFailed,
        NoChanges
    }

    public class KnowledgeBaseTreeMutationResult
    {
        public bool IsSuccess { get; init; }

        public KnowledgeBaseTreeMutationFailure Failure { get; init; }

        public string? ErrorMessage { get; init; }

        public string? StatusMessage { get; init; }

        public KbNode? AffectedNode { get; init; }

        public KnowledgeBaseSessionViewState ViewState { get; init; } = new();
    }

    /// <summary>
    /// Координирует mutating-операции над деревом, undo/redo и сохранение history snapshot.
    /// Не зависит от WinForms и может тестироваться отдельно от MainForm.
    /// </summary>
    public class KnowledgeBaseTreeMutationWorkflowService
    {
        private readonly KnowledgeBaseSessionService _session;
        private readonly KnowledgeBaseSessionWorkflowService _sessionWorkflowService;
        private readonly KnowledgeBaseTreeController _treeController;
        private readonly KnowledgeBaseCompositionTemplateService _compositionTemplateService = new();
        private readonly KnowledgeBaseObjectTemplateService _objectTemplateService = new();
        private readonly UndoRedoService _history;

        public KnowledgeBaseTreeMutationWorkflowService(
            KnowledgeBaseSessionService session,
            KnowledgeBaseSessionWorkflowService sessionWorkflowService,
            KnowledgeBaseTreeController treeController,
            UndoRedoService history)
        {
            _session = session;
            _sessionWorkflowService = sessionWorkflowService;
            _treeController = treeController;
            _history = history;
        }

        public bool HasClipboardNode => _treeController.HasClipboardNode;

        public bool CanUndo => _history.CanUndo;

        public bool CanRedo => _history.CanRedo;

        public bool CanAddNode(KbNode? parentNode) => _treeController.CanAddNode(parentNode);

        public bool CanCreateObjectFromCatalog(KbNode? parentNode) =>
            _session.EquipmentCatalogItems.Count > 0 &&
            _treeController.CanAddNode(parentNode);

        public bool CanAddNodeFromTemplate(KbNode? parentNode) =>
            _treeController.CanAddNode(parentNode) &&
            _compositionTemplateService.TryResolveTemplateChildType(parentNode, out _);

        public bool HasObjectTemplates => _session.ObjectTemplates.Count > 0;

        public bool CanCreateObjectFromTemplate(KbNode? parentNode) =>
            GetAvailableObjectTemplates(parentNode).Count > 0;

        public static bool CanSaveObjectAsTemplate(KbNode? sourceNode) =>
            sourceNode != null && sourceNode.NodeType != KbNodeType.WorkshopRoot;

        public bool CanPasteNode(KbNode? parentNode) => _treeController.CanPasteNode(parentNode);

        public void ClearClipboard() => _treeController.ClearClipboard();

        public void CopyNode(KbNode node) => _treeController.CopyNode(node);

        public IReadOnlyList<KbObjectTemplate> GetAvailableObjectTemplates(KbNode? parentNode) =>
            _session.ObjectTemplates
                .Where(template => CanAttachObjectTemplate(parentNode, template))
                .ToList();

        public KnowledgeBaseTreeMutationResult AddNode(
            string workshopName,
            KbNode? parentNode,
            string nodeName,
            List<KbNode> currentRoots)
        {
            string normalizedName = nodeName.Trim();
            if (string.IsNullOrWhiteSpace(normalizedName))
            {
                return Failure(
                    KnowledgeBaseTreeMutationFailure.InvalidNodeName,
                    "Название узла не должно быть пустым.");
            }

            if (!_treeController.CanAddNode(parentNode))
            {
                return Failure(
                    KnowledgeBaseTreeMutationFailure.DepthLimitExceeded,
                    $"Достигнута максимальная глубина ({_session.Config.MaxLevels}).");
            }

            string historySnapshot = CaptureHistorySnapshot(currentRoots);
            var newNode = _treeController.AddNode(workshopName, parentNode, normalizedName);
            PersistVirtualWorkshopWrapperIfNeeded(parentNode, currentRoots);
            _history.SaveState(historySnapshot);

            return Success($"➕ Добавлено: {newNode.Name}", newNode);
        }

        public KnowledgeBaseTreeMutationResult AddNodeFromTemplate(
            string workshopName,
            KbNode? parentNode,
            string nodeName,
            string templateId,
            List<KbNode> currentRoots)
        {
            string normalizedName = nodeName.Trim();
            if (string.IsNullOrWhiteSpace(normalizedName))
            {
                return Failure(
                    KnowledgeBaseTreeMutationFailure.InvalidNodeName,
                    "Название узла не должно быть пустым.");
            }

            if (!_treeController.CanAddNode(parentNode))
            {
                return Failure(
                    KnowledgeBaseTreeMutationFailure.DepthLimitExceeded,
                    $"Достигнута максимальная глубина ({_session.Config.MaxLevels}).");
            }

            if (!_compositionTemplateService.TryResolveTemplateChildType(parentNode, out var targetNodeType))
            {
                return Failure(
                    KnowledgeBaseTreeMutationFailure.TemplateUnavailable,
                    "Для выбранного родителя нет доступных шаблонов.");
            }

            var template = _compositionTemplateService.FindTemplate(templateId);
            if (template == null || template.TargetNodeType != targetNodeType)
            {
                return Failure(
                    KnowledgeBaseTreeMutationFailure.TemplateUnavailable,
                    "Выбранный шаблон недоступен для этого типа узла.");
            }

            string historySnapshot = CaptureHistorySnapshot(currentRoots);
            var newNode = _treeController.AddNode(
                workshopName,
                parentNode,
                new KbNode
                {
                    Name = normalizedName,
                    NodeType = targetNodeType,
                    Details = new KbNodeDetails
                    {
                        Location = KnowledgeBaseCompositionTemplateService.BuildInheritedLocation(parentNode)
                    }
                });

            var templateResult = _compositionTemplateService.ApplyTemplate(
                newNode,
                _session.CompositionRacks,
                _session.CompositionEntries,
                template.TemplateId);
            if (!templateResult.IsSuccess)
            {
                return Failure(
                    KnowledgeBaseTreeMutationFailure.TemplateUnavailable,
                    templateResult.ErrorMessage);
            }

            _session.ReplaceCompositionEntries(templateResult.CompositionEntries);
            _session.ReplaceCompositionRacks(templateResult.CompositionRacks);
            PersistVirtualWorkshopWrapperIfNeeded(parentNode, currentRoots);
            _history.SaveState(historySnapshot);

            return Success(
                $"Добавлено по шаблону: {newNode.Name}",
                newNode);
        }

        public KnowledgeBaseTreeMutationResult CreateObjectFromCatalog(
            string workshopName,
            KbNode? parentNode,
            KbEquipmentCatalogItem? catalogItem,
            List<KbNode> currentRoots)
        {
            List<KbEquipmentCatalogItem> normalizedCatalogItems =
                KnowledgeBaseDataService.NormalizeEquipmentCatalogItems(new[] { catalogItem });
            if (normalizedCatalogItems.Count == 0)
            {
                return Failure(
                    KnowledgeBaseTreeMutationFailure.CatalogUnavailable,
                    "Выберите запись каталога оборудования.");
            }

            if (!_treeController.CanAddNode(parentNode))
            {
                return Failure(
                    KnowledgeBaseTreeMutationFailure.DepthLimitExceeded,
                    $"Достигнута максимальная глубина ({_session.Config.MaxLevels}).");
            }

            KbEquipmentCatalogItem item = normalizedCatalogItems[0];
            string historySnapshot = CaptureHistorySnapshot(currentRoots);
            KbNode newNode = _treeController.AddNode(
                workshopName,
                parentNode,
                new KbNode
                {
                    Name = BuildCatalogObjectNodeName(item),
                    NodeType = ResolveCatalogObjectNodeType(item.DefaultNodeType),
                    Details = new KbNodeDetails
                    {
                        Description = item.Description
                    }
                });

            PersistVirtualWorkshopWrapperIfNeeded(parentNode, currentRoots);
            _history.SaveState(historySnapshot);

            return Success($"Создан объект из каталога: {newNode.Name}", newNode);
        }

        public KnowledgeBaseTreeMutationResult CreateObjectFromTemplate(
            string workshopName,
            KbNode? parentNode,
            string templateId,
            string rootNameOverride,
            List<KbNode> currentRoots)
        {
            var template = FindObjectTemplate(templateId);
            if (template == null)
            {
                return Failure(
                    KnowledgeBaseTreeMutationFailure.TemplateUnavailable,
                    "Шаблон объекта не найден.");
            }

            var instance = _objectTemplateService.CreateInstance(template, rootNameOverride);
            if (!instance.IsSuccess || instance.RootNode == null)
            {
                return Failure(
                    KnowledgeBaseTreeMutationFailure.TemplateUnavailable,
                    instance.ErrorMessage);
            }

            if (!_treeController.CanAttachSubtree(parentNode, instance.RootNode))
            {
                return Failure(
                    KnowledgeBaseTreeMutationFailure.DepthLimitExceeded,
                    $"Поддерево шаблона не помещается в глубину {_session.Config.MaxLevels}.");
            }

            string historySnapshot = CaptureHistorySnapshot(currentRoots);
            KbNode createdNode = _treeController.AddNode(workshopName, parentNode, instance.RootNode);

            if (instance.CompositionEntries.Count > 0)
                _session.ReplaceCompositionEntries(_session.CompositionEntries.Concat(instance.CompositionEntries));

            if (instance.CompositionRacks.Count > 0)
                _session.ReplaceCompositionRacks(_session.CompositionRacks.Concat(instance.CompositionRacks));

            if (instance.DocumentLinks.Count > 0)
                _session.ReplaceDocumentLinks(_session.DocumentLinks.Concat(instance.DocumentLinks));

            if (instance.SoftwareRecords.Count > 0)
                _session.ReplaceSoftwareRecords(_session.SoftwareRecords.Concat(instance.SoftwareRecords));

            if (instance.NetworkFileReferences.Count > 0)
                _session.ReplaceNetworkFileReferences(_session.NetworkFileReferences.Concat(instance.NetworkFileReferences));

            if (instance.MaintenanceScheduleProfiles.Count > 0)
                _session.ReplaceMaintenanceScheduleProfiles(
                    _session.MaintenanceScheduleProfiles.Concat(instance.MaintenanceScheduleProfiles));

            PersistVirtualWorkshopWrapperIfNeeded(parentNode, currentRoots);
            _history.SaveState(historySnapshot);

            return Success($"Создано из шаблона объекта: {createdNode.Name}", createdNode);
        }

        public KnowledgeBaseTreeMutationResult SaveObjectAsTemplate(
            KbNode sourceNode,
            string displayName,
            string category,
            string description,
            List<KbNode> currentRoots)
        {
            if (!CanSaveObjectAsTemplate(sourceNode))
            {
                return Failure(
                    KnowledgeBaseTreeMutationFailure.TemplateUnavailable,
                    "Выберите объект дерева, который можно сохранить как шаблон.");
            }

            var templateResult = _objectTemplateService.CreateTemplateFromExistingObject(
                sourceNode,
                displayName,
                category,
                description,
                _session.CompositionRacks,
                _session.CompositionEntries,
                _session.DocumentLinks,
                _session.SoftwareRecords,
                _session.NetworkFileReferences,
                _session.MaintenanceScheduleProfiles);
            if (!templateResult.IsSuccess || templateResult.Template == null)
            {
                return Failure(
                    KnowledgeBaseTreeMutationFailure.TemplateUnavailable,
                    templateResult.ErrorMessage);
            }

            string historySnapshot = CaptureHistorySnapshot(currentRoots);
            _session.ReplaceObjectTemplates(_session.ObjectTemplates.Concat(new[] { templateResult.Template }));
            _history.SaveState(historySnapshot);

            return Success($"Сохранён шаблон объекта: {templateResult.Template.DisplayName}", sourceNode);
        }

        public KnowledgeBaseObjectTemplateApplicationPlan PreviewApplyObjectTemplateToExistingObject(
            KbNode? targetNode,
            string templateId)
        {
            var template = FindObjectTemplate(templateId);
            return _objectTemplateService.BuildApplyToExistingObjectPlan(
                template,
                targetNode,
                _session.Config.MaxLevels,
                _session.CompositionRacks,
                _session.CompositionEntries,
                _session.DocumentLinks,
                _session.SoftwareRecords,
                _session.NetworkFileReferences,
                _session.MaintenanceScheduleProfiles);
        }

        public KnowledgeBaseTreeMutationResult ApplyObjectTemplateToExistingObject(
            string workshopName,
            KbNode targetNode,
            string templateId,
            List<KbNode> currentRoots)
        {
            var plan = PreviewApplyObjectTemplateToExistingObject(targetNode, templateId);
            if (!plan.IsSuccess)
            {
                return Failure(
                    KnowledgeBaseTreeMutationFailure.TemplateUnavailable,
                    plan.ErrorMessage);
            }

            if (!plan.HasChanges)
            {
                return Failure(
                    KnowledgeBaseTreeMutationFailure.NoChanges,
                    "Шаблон не добавляет новых данных к выбранному объекту.");
            }

            string historySnapshot = CaptureHistorySnapshot(currentRoots);
            foreach (KnowledgeBaseObjectTemplateDetailUpdate update in plan.DetailUpdates)
                ApplyDetailUpdate(update);

            foreach (KnowledgeBaseObjectTemplateNodeAddition addition in plan.NodeAdditions)
                _treeController.AddNode(workshopName, addition.ParentNode, addition.Node);

            if (plan.CompositionEntries.Count > 0)
                _session.ReplaceCompositionEntries(_session.CompositionEntries.Concat(plan.CompositionEntries));

            if (plan.CompositionRacks.Count > 0)
                _session.ReplaceCompositionRacks(_session.CompositionRacks.Concat(plan.CompositionRacks));

            if (plan.DocumentLinks.Count > 0)
                _session.ReplaceDocumentLinks(_session.DocumentLinks.Concat(plan.DocumentLinks));

            if (plan.SoftwareRecords.Count > 0)
                _session.ReplaceSoftwareRecords(_session.SoftwareRecords.Concat(plan.SoftwareRecords));

            if (plan.NetworkFileReferences.Count > 0)
                _session.ReplaceNetworkFileReferences(_session.NetworkFileReferences.Concat(plan.NetworkFileReferences));

            if (plan.MaintenanceScheduleProfiles.Count > 0)
                _session.ReplaceMaintenanceScheduleProfiles(
                    _session.MaintenanceScheduleProfiles.Concat(plan.MaintenanceScheduleProfiles));

            _history.SaveState(historySnapshot);

            return Success($"Шаблон применён к объекту: {targetNode.Name}", targetNode);
        }

        public KnowledgeBaseTreeMutationResult DeleteNode(
            string workshopName,
            KbNode nodeToRemove,
            List<KbNode> currentRoots)
        {
            string historySnapshot = CaptureHistorySnapshot(currentRoots);
            var removedNodeIds = CollectSubtreeNodeIds(nodeToRemove);
            if (!_treeController.DeleteNode(workshopName, nodeToRemove))
            {
                return Failure(
                    KnowledgeBaseTreeMutationFailure.DeleteFailed,
                    "Не удалось удалить выбранный узел.");
            }

            DeleteTypedDataForNodeIds(removedNodeIds);
            _history.SaveState(historySnapshot);
            return Success($"🗑 Удалено: {nodeToRemove.Name}");
        }

        public KnowledgeBaseTreeMutationResult PasteNode(KbNode parentNode, List<KbNode> currentRoots)
        {
            if (!_treeController.HasClipboardNode)
            {
                return Failure(
                    KnowledgeBaseTreeMutationFailure.ClipboardUnavailable,
                    "Буфер копирования пуст.");
            }

            if (!_treeController.CanPasteNode(parentNode))
            {
                return Failure(
                    KnowledgeBaseTreeMutationFailure.DepthLimitExceeded,
                    $"Поддерево не помещается в глубину {_session.Config.MaxLevels}.");
            }

            string historySnapshot = CaptureHistorySnapshot(currentRoots);
            var newNode = _treeController.PasteNode(parentNode);
            _history.SaveState(historySnapshot);

            return Success($"📌 Вставлено: {newNode.Name}", newNode);
        }

        public KnowledgeBaseTreeMutationResult RenameNode(
            KbNode node,
            string newName,
            List<KbNode> currentRoots)
        {
            string normalizedName = newName.Trim();
            if (string.IsNullOrWhiteSpace(normalizedName))
            {
                return Failure(
                    KnowledgeBaseTreeMutationFailure.InvalidNodeName,
                    "Название узла не должно быть пустым.");
            }

            if (string.Equals(node.Name, normalizedName, System.StringComparison.CurrentCulture))
                return Failure(KnowledgeBaseTreeMutationFailure.NoChanges, null);

            string historySnapshot = CaptureHistorySnapshot(currentRoots);
            _treeController.RenameNode(node, normalizedName);
            _history.SaveState(historySnapshot);

            return Success($"✏️ Переименовано в: {normalizedName}", node);
        }

        public KnowledgeBaseTreeMutationResult MoveNode(
            string workshopName,
            KbNode draggedNode,
            KbNode? oldParentNode,
            KbNode targetNode,
            List<KbNode> currentRoots)
        {
            if (_treeController.WouldCreateCycle(targetNode, draggedNode))
            {
                return Failure(
                    KnowledgeBaseTreeMutationFailure.MoveWouldCreateCycle,
                    "Нельзя переместить узел внутрь его потомка.");
            }

            if (!_treeController.CanMoveNode(targetNode, draggedNode))
            {
                return Failure(
                    KnowledgeBaseTreeMutationFailure.DepthLimitExceeded,
                    $"Поддерево не помещается в глубину {_session.Config.MaxLevels}.");
            }

            string historySnapshot = CaptureHistorySnapshot(currentRoots);
            if (!_treeController.MoveNode(workshopName, draggedNode, oldParentNode, targetNode))
            {
                return Failure(
                    KnowledgeBaseTreeMutationFailure.MoveFailed,
                    "Не удалось переместить узел в новую позицию.");
            }

            DeleteUnsupportedTypedDataForMovedSubtree(draggedNode, currentRoots);
            _history.SaveState(historySnapshot);
            return Success($"↕ Перемещено: {draggedNode.Name}", draggedNode);
        }

        public KnowledgeBaseTreeMutationResult Undo(List<KbNode> currentRoots)
        {
            var snapshot = _history.Undo(CaptureHistorySnapshot(currentRoots));
            return snapshot == null
                ? Failure(KnowledgeBaseTreeMutationFailure.NoChanges, null)
                : RestoreSnapshot(snapshot, "↩ Выполнена отмена");
        }

        public KnowledgeBaseTreeMutationResult Redo(List<KbNode> currentRoots)
        {
            var snapshot = _history.Redo(CaptureHistorySnapshot(currentRoots));
            return snapshot == null
                ? Failure(KnowledgeBaseTreeMutationFailure.NoChanges, null)
                : RestoreSnapshot(snapshot, "↪ Выполнен повтор");
        }

        private string CaptureHistorySnapshot(List<KbNode> currentRoots) =>
            _session.SerializeSnapshot(currentRoots, includeCurrentWorkshop: true);

        private void PersistVirtualWorkshopWrapperIfNeeded(
            KbNode? parentNode,
            List<KbNode> currentRoots)
        {
            if (parentNode == null || currentRoots.Count != 0)
                return;

            if (parentNode.NodeType != KbNodeType.WorkshopRoot)
                return;

            currentRoots.Add(parentNode);
            _session.SyncCurrentWorkshop(currentRoots);
        }

        private bool CanAttachObjectTemplate(KbNode? parentNode, KbObjectTemplate template)
        {
            if (template.RootNode == null || _session.Config.MaxLevels <= 0)
                return false;

            int newRootLevel = parentNode == null ? 0 : parentNode.LevelIndex + 1;
            return newRootLevel + GetObjectTemplateHeight(template.RootNode) <= _session.Config.MaxLevels;
        }

        private static int GetObjectTemplateHeight(KbObjectTemplateNode node)
        {
            if (node.Children.Count == 0)
                return 1;

            return 1 + node.Children.Max(GetObjectTemplateHeight);
        }

        private KbObjectTemplate? FindObjectTemplate(string? templateId)
        {
            string normalizedTemplateId = templateId?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalizedTemplateId))
                return null;

            return _session.ObjectTemplates.FirstOrDefault(template =>
                string.Equals(template.TemplateId, normalizedTemplateId, StringComparison.Ordinal));
        }

        private static void ApplyDetailUpdate(KnowledgeBaseObjectTemplateDetailUpdate update)
        {
            update.TargetNode.Details ??= new KbNodeDetails();
            switch (update.FieldKey)
            {
                case "description":
                    update.TargetNode.Details.Description = update.Value;
                    break;
                case "location":
                    update.TargetNode.Details.Location = update.Value;
                    break;
                case "inventory":
                    update.TargetNode.Details.InventoryNumber = update.Value;
                    break;
                case "photo":
                    update.TargetNode.Details.PhotoPath = update.Value;
                    break;
                case "ip":
                    update.TargetNode.Details.IpAddress = update.Value;
                    break;
                case "schema":
                    update.TargetNode.Details.SchemaLink = update.Value;
                    break;
            }
        }

        private KnowledgeBaseTreeMutationResult RestoreSnapshot(string snapshot, string statusText)
        {
            var restoreResult = _sessionWorkflowService.RestoreSnapshot(snapshot);
            if (!restoreResult.IsSuccess)
            {
                return Failure(
                    KnowledgeBaseTreeMutationFailure.RestoreFailed,
                    restoreResult.ErrorMessage);
            }

            return new KnowledgeBaseTreeMutationResult
            {
                IsSuccess = true,
                Failure = KnowledgeBaseTreeMutationFailure.None,
                StatusMessage = statusText,
                ViewState = restoreResult.ViewState
            };
        }

        private void DeleteTypedDataForNodeIds(ISet<string> removedNodeIds)
        {
            if (removedNodeIds.Count == 0)
                return;

            var remainingCompositionEntries = _session.CompositionEntries
                .Where(entry => !removedNodeIds.Contains(entry.ParentNodeId))
                .ToList();
            if (remainingCompositionEntries.Count != _session.CompositionEntries.Count)
                _session.ReplaceCompositionEntries(remainingCompositionEntries);

            var remainingCompositionRacks = _session.CompositionRacks
                .Where(rack => !removedNodeIds.Contains(rack.ParentNodeId))
                .ToList();
            if (remainingCompositionRacks.Count != _session.CompositionRacks.Count)
                _session.ReplaceCompositionRacks(remainingCompositionRacks);

            var remainingDocumentLinks = _session.DocumentLinks
                .Where(link => !removedNodeIds.Contains(link.OwnerNodeId))
                .ToList();
            if (remainingDocumentLinks.Count != _session.DocumentLinks.Count)
                _session.ReplaceDocumentLinks(remainingDocumentLinks);

            var remainingSoftwareRecords = _session.SoftwareRecords
                .Where(record => !removedNodeIds.Contains(record.OwnerNodeId))
                .ToList();
            if (remainingSoftwareRecords.Count != _session.SoftwareRecords.Count)
                _session.ReplaceSoftwareRecords(remainingSoftwareRecords);

            DeleteNetworkDataForNodeIds(removedNodeIds);

            var remainingMaintenanceScheduleProfiles = _session.MaintenanceScheduleProfiles
                .Where(profile => !removedNodeIds.Contains(profile.OwnerNodeId))
                .ToList();
            if (remainingMaintenanceScheduleProfiles.Count != _session.MaintenanceScheduleProfiles.Count)
                _session.ReplaceMaintenanceScheduleProfiles(remainingMaintenanceScheduleProfiles);
        }

        private void DeleteUnsupportedTypedDataForMovedSubtree(KbNode movedRoot, IReadOnlyList<KbNode> currentRoots)
        {
            Dictionary<string, int> visibleLevelByNodeId = BuildVisibleLevelByNodeId(currentRoots);
            DeleteCompositionDataForNodeIds(CollectMovedSubtreeUnsupportedTypedNodeIds(
                movedRoot,
                visibleLevelByNodeId,
                static (node, visibleLevel) => !KnowledgeBaseCompositionStateService.SupportsComposition(
                    node.NodeType,
                    visibleLevel)));
            DeleteDocsAndSoftwareDataForNodeIds(CollectMovedSubtreeUnsupportedTypedNodeIds(
                movedRoot,
                visibleLevelByNodeId,
                static (node, visibleLevel) => !KnowledgeBaseDocsAndSoftwareStateService.SupportsRecords(
                    node.NodeType,
                    visibleLevel)));
            DeleteNetworkDataForNodeIds(CollectMovedSubtreeUnsupportedTypedNodeIds(
                movedRoot,
                visibleLevelByNodeId,
                static (node, visibleLevel) => !KnowledgeBaseNetworkStateService.SupportsRecords(
                    node.NodeType,
                    visibleLevel)));
            DeleteMaintenanceDataForNodeIds(CollectMovedSubtreeUnsupportedTypedNodeIds(
                movedRoot,
                visibleLevelByNodeId,
                static (node, visibleLevel) => !KnowledgeBaseMaintenanceScheduleStateService.SupportsProfile(
                    node.NodeType,
                    visibleLevel)));
        }

        private void DeleteCompositionDataForNodeIds(ISet<string> removedNodeIds)
        {
            if (removedNodeIds.Count == 0)
                return;

            var remainingCompositionEntries = _session.CompositionEntries
                .Where(entry => !removedNodeIds.Contains(entry.ParentNodeId))
                .ToList();
            if (remainingCompositionEntries.Count != _session.CompositionEntries.Count)
                _session.ReplaceCompositionEntries(remainingCompositionEntries);

            var remainingCompositionRacks = _session.CompositionRacks
                .Where(rack => !removedNodeIds.Contains(rack.ParentNodeId))
                .ToList();
            if (remainingCompositionRacks.Count != _session.CompositionRacks.Count)
                _session.ReplaceCompositionRacks(remainingCompositionRacks);
        }

        private void DeleteDocsAndSoftwareDataForNodeIds(ISet<string> removedNodeIds)
        {
            if (removedNodeIds.Count == 0)
                return;

            var remainingDocumentLinks = _session.DocumentLinks
                .Where(link => !removedNodeIds.Contains(link.OwnerNodeId))
                .ToList();
            if (remainingDocumentLinks.Count != _session.DocumentLinks.Count)
                _session.ReplaceDocumentLinks(remainingDocumentLinks);

            var remainingSoftwareRecords = _session.SoftwareRecords
                .Where(record => !removedNodeIds.Contains(record.OwnerNodeId))
                .ToList();
            if (remainingSoftwareRecords.Count != _session.SoftwareRecords.Count)
                _session.ReplaceSoftwareRecords(remainingSoftwareRecords);
        }

        private void DeleteNetworkDataForNodeIds(ISet<string> removedNodeIds)
        {
            if (removedNodeIds.Count == 0)
                return;

            var remainingNetworkFileReferences = _session.NetworkFileReferences
                .Where(reference => !removedNodeIds.Contains(reference.OwnerNodeId))
                .ToList();
            if (remainingNetworkFileReferences.Count != _session.NetworkFileReferences.Count)
                _session.ReplaceNetworkFileReferences(remainingNetworkFileReferences);

            var removedNetworkDeviceIds = _session.NetworkDevices
                .Where(device =>
                    removedNodeIds.Contains(device.OwnerNodeId) ||
                    removedNodeIds.Contains(device.LinkedNodeId))
                .Select(device => device.NetworkDeviceId)
                .ToHashSet(StringComparer.Ordinal);

            if (removedNetworkDeviceIds.Count == 0)
                return;

            var remainingNetworkDevices = _session.NetworkDevices
                .Where(device => !removedNetworkDeviceIds.Contains(device.NetworkDeviceId))
                .ToList();
            if (remainingNetworkDevices.Count != _session.NetworkDevices.Count)
                _session.ReplaceNetworkDevices(remainingNetworkDevices);

            var removedNetworkInterfaceIds = _session.NetworkInterfaces
                .Where(networkInterface => removedNetworkDeviceIds.Contains(networkInterface.NetworkDeviceId))
                .Select(networkInterface => networkInterface.NetworkInterfaceId)
                .ToHashSet(StringComparer.Ordinal);

            var remainingNetworkInterfaces = _session.NetworkInterfaces
                .Where(networkInterface => !removedNetworkDeviceIds.Contains(networkInterface.NetworkDeviceId))
                .ToList();
            if (remainingNetworkInterfaces.Count != _session.NetworkInterfaces.Count)
                _session.ReplaceNetworkInterfaces(remainingNetworkInterfaces);

            if (removedNetworkInterfaceIds.Count == 0)
                return;

            var remainingNetworkConnections = _session.NetworkConnections
                .Where(connection =>
                    !removedNetworkInterfaceIds.Contains(connection.EndpointAInterfaceId) &&
                    !removedNetworkInterfaceIds.Contains(connection.EndpointBInterfaceId))
                .ToList();
            if (remainingNetworkConnections.Count != _session.NetworkConnections.Count)
                _session.ReplaceNetworkConnections(remainingNetworkConnections);
        }

        private void DeleteMaintenanceDataForNodeIds(ISet<string> removedNodeIds)
        {
            if (removedNodeIds.Count == 0)
                return;

            var remainingMaintenanceScheduleProfiles = _session.MaintenanceScheduleProfiles
                .Where(profile => !removedNodeIds.Contains(profile.OwnerNodeId))
                .ToList();
            if (remainingMaintenanceScheduleProfiles.Count != _session.MaintenanceScheduleProfiles.Count)
                _session.ReplaceMaintenanceScheduleProfiles(remainingMaintenanceScheduleProfiles);
        }

        private static HashSet<string> CollectMovedSubtreeUnsupportedTypedNodeIds(
            KbNode root,
            IReadOnlyDictionary<string, int> visibleLevelByNodeId,
            Func<KbNode, int, bool> isUnsupported)
        {
            var nodeIds = new HashSet<string>(StringComparer.Ordinal);
            CollectMovedSubtreeUnsupportedTypedNodeIdsRecursive(root, visibleLevelByNodeId, isUnsupported, nodeIds);
            return nodeIds;
        }

        private static void CollectMovedSubtreeUnsupportedTypedNodeIdsRecursive(
            KbNode node,
            IReadOnlyDictionary<string, int> visibleLevelByNodeId,
            Func<KbNode, int, bool> isUnsupported,
            ISet<string> nodeIds)
        {
            string nodeId = node.NodeId?.Trim() ?? string.Empty;
            int visibleLevel = visibleLevelByNodeId.TryGetValue(nodeId, out int value)
                ? value
                : Math.Max(1, node.LevelIndex);

            if (!string.IsNullOrWhiteSpace(nodeId) && isUnsupported(node, visibleLevel))
                nodeIds.Add(nodeId);

            foreach (KbNode child in node.Children)
                CollectMovedSubtreeUnsupportedTypedNodeIdsRecursive(child, visibleLevelByNodeId, isUnsupported, nodeIds);
        }

        private static Dictionary<string, int> BuildVisibleLevelByNodeId(IEnumerable<KbNode> roots)
        {
            var levels = new Dictionary<string, int>(StringComparer.Ordinal);
            CollectVisibleLevels(roots, visibleLevel: 1, levels);
            return levels;
        }

        private static void CollectVisibleLevels(
            IEnumerable<KbNode> nodes,
            int visibleLevel,
            IDictionary<string, int> levels)
        {
            foreach (KbNode node in nodes)
            {
                int currentVisibleLevel = GetEffectiveVisibleLevel(node, visibleLevel);
                string nodeId = node.NodeId?.Trim() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(nodeId))
                    levels[nodeId] = currentVisibleLevel;

                CollectVisibleLevels(node.Children, currentVisibleLevel + 1, levels);
            }
        }

        private static int GetEffectiveVisibleLevel(KbNode node, int visibleLevel)
        {
            if (node.NodeType == KbNodeType.WorkshopRoot && node.LevelIndex == 0)
                return Math.Max(0, visibleLevel - 1);

            return visibleLevel;
        }

        private static string BuildCatalogObjectNodeName(KbEquipmentCatalogItem item)
        {
            string[] primaryParts =
            {
                item.EquipmentKind?.Trim() ?? string.Empty,
                item.Manufacturer?.Trim() ?? string.Empty,
                item.Model?.Trim() ?? string.Empty
            };
            string name = string.Join(" ", primaryParts.Where(static part => !string.IsNullOrWhiteSpace(part)));
            if (!string.IsNullOrWhiteSpace(name))
                return name;

            string series = item.Series?.Trim() ?? string.Empty;
            return string.IsNullOrWhiteSpace(series) ? "Оборудование" : series;
        }

        private static KbNodeType ResolveCatalogObjectNodeType(KbNodeType defaultNodeType) => defaultNodeType switch
        {
            KbNodeType.System => KbNodeType.System,
            KbNodeType.Cabinet => KbNodeType.Cabinet,
            KbNodeType.Device => KbNodeType.Device,
            KbNodeType.Controller => KbNodeType.Controller,
            KbNodeType.Module => KbNodeType.Module,
            _ => KbNodeType.Device
        };

        private static HashSet<string> CollectSubtreeNodeIds(KbNode root)
        {
            var nodeIds = new HashSet<string>(StringComparer.Ordinal);
            CollectSubtreeNodeIdsRecursive(root, nodeIds);
            return nodeIds;
        }

        private static void CollectSubtreeNodeIdsRecursive(KbNode node, ISet<string> nodeIds)
        {
            string nodeId = node.NodeId?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(nodeId))
                nodeIds.Add(nodeId);

            foreach (var child in node.Children)
                CollectSubtreeNodeIdsRecursive(child, nodeIds);
        }

        private KnowledgeBaseTreeMutationResult Success(string statusMessage, KbNode? affectedNode = null) =>
            new()
            {
                IsSuccess = true,
                Failure = KnowledgeBaseTreeMutationFailure.None,
                StatusMessage = statusMessage,
                AffectedNode = affectedNode,
                ViewState = _sessionWorkflowService.BuildViewState()
            };

        private static KnowledgeBaseTreeMutationResult Failure(
            KnowledgeBaseTreeMutationFailure failure,
            string? errorMessage) =>
            new()
            {
                IsSuccess = false,
                Failure = failure,
                ErrorMessage = errorMessage
            };
    }
}
