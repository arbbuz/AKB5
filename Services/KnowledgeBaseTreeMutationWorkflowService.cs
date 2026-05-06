using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public enum KnowledgeBaseTreeMutationFailure
    {
        None,
        InvalidNodeName,
        DepthLimitExceeded,
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

        public bool CanAddNodeFromTemplate(KbNode? parentNode) =>
            _treeController.CanAddNode(parentNode) &&
            _compositionTemplateService.TryResolveTemplateChildType(parentNode, out _);

        public bool HasObjectTemplates => _session.ObjectTemplates.Count > 0;

        public bool CanCreateObjectFromTemplate(KbNode? parentNode) =>
            GetAvailableObjectTemplates(parentNode).Count > 0;

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
                    "РќР°Р·РІР°РЅРёРµ СѓР·Р»Р° РЅРµ РґРѕР»Р¶РЅРѕ Р±С‹С‚СЊ РїСѓСЃС‚С‹Рј.");
            }

            if (!_treeController.CanAddNode(parentNode))
            {
                return Failure(
                    KnowledgeBaseTreeMutationFailure.DepthLimitExceeded,
                    $"Р”РѕСЃС‚РёРіРЅСѓС‚Р° РјР°РєСЃРёРјР°Р»СЊРЅР°СЏ РіР»СѓР±РёРЅР° ({_session.Config.MaxLevels}).");
            }

            if (!_compositionTemplateService.TryResolveTemplateChildType(parentNode, out var targetNodeType))
            {
                return Failure(
                    KnowledgeBaseTreeMutationFailure.TemplateUnavailable,
                    "Р”Р»СЏ РІС‹Р±СЂР°РЅРЅРѕРіРѕ СЂРѕРґРёС‚РµР»СЏ РЅРµС‚ РґРѕСЃС‚СѓРїРЅС‹С… С€Р°Р±Р»РѕРЅРѕРІ.");
            }

            var template = _compositionTemplateService.FindTemplate(templateId);
            if (template == null || template.TargetNodeType != targetNodeType)
            {
                return Failure(
                    KnowledgeBaseTreeMutationFailure.TemplateUnavailable,
                    "Р’С‹Р±СЂР°РЅРЅС‹Р№ С€Р°Р±Р»РѕРЅ РЅРµРґРѕСЃС‚СѓРїРµРЅ РґР»СЏ СЌС‚РѕРіРѕ С‚РёРїР° СѓР·Р»Р°.");
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
                _session.CompositionEntries,
                template.TemplateId);
            if (!templateResult.IsSuccess)
            {
                return Failure(
                    KnowledgeBaseTreeMutationFailure.TemplateUnavailable,
                    templateResult.ErrorMessage);
            }

            _session.ReplaceCompositionEntries(templateResult.CompositionEntries);
            PersistVirtualWorkshopWrapperIfNeeded(parentNode, currentRoots);
            _history.SaveState(historySnapshot);

            return Success(
                $"вћ• Р”РѕР±Р°РІР»РµРЅРѕ РїРѕ С€Р°Р±Р»РѕРЅСѓ: {newNode.Name}",
                newNode);
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

            var remainingNetworkFileReferences = _session.NetworkFileReferences
                .Where(reference => !removedNodeIds.Contains(reference.OwnerNodeId))
                .ToList();
            if (remainingNetworkFileReferences.Count != _session.NetworkFileReferences.Count)
                _session.ReplaceNetworkFileReferences(remainingNetworkFileReferences);

            var remainingMaintenanceScheduleProfiles = _session.MaintenanceScheduleProfiles
                .Where(profile => !removedNodeIds.Contains(profile.OwnerNodeId))
                .ToList();
            if (remainingMaintenanceScheduleProfiles.Count != _session.MaintenanceScheduleProfiles.Count)
                _session.ReplaceMaintenanceScheduleProfiles(remainingMaintenanceScheduleProfiles);
        }

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
