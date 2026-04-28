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

        public bool CanPasteNode(KbNode? parentNode) => _treeController.CanPasteNode(parentNode);

        public void ClearClipboard() => _treeController.ClearClipboard();

        public void CopyNode(KbNode node) => _treeController.CopyNode(node);

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
