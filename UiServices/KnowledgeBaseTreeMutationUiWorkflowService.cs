using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.UiServices
{
    public class KnowledgeBaseTreeMutationUiWorkflowContext
    {
        public IWin32Window Owner { get; init; } = null!;

        public TreeView TreeView { get; init; } = null!;

        public string CurrentWorkshop { get; init; } = string.Empty;

        public Func<List<KbNode>> GetPersistedTreeData { get; init; } = null!;

        public Func<KbNode?> GetEffectiveParentForRootOperations { get; init; } = null!;

        public Func<KbNode, KbNode?, KbNode?> ResolveActualParentNode { get; init; } = null!;

        public Func<HashSet<KbNode>> CaptureExpandedNodes { get; init; } = null!;

        public Action<KnowledgeBaseSessionViewState, bool, KbNode?, ISet<KbNode>?> ApplySessionView { get; init; } = null!;

        public Action RefreshSearchAfterMutation { get; init; } = null!;

        public Action UpdateDirtyState { get; init; } = null!;

        public Action UpdateUi { get; init; } = null!;

        public Action<string> SetStatusText { get; init; } = null!;
    }

    /// <summary>
    /// Координирует WinForms-специфичные tree-mutation сценарии:
    /// диалоги, drag-and-drop feedback и undo/redo orchestration поверх core workflow.
    /// </summary>
    public class KnowledgeBaseTreeMutationUiWorkflowService
    {
        private readonly KnowledgeBaseTreeMutationWorkflowService _treeMutationWorkflowService;
        private readonly KnowledgeBaseCompositionTemplateService _compositionTemplateService = new();

        public KnowledgeBaseTreeMutationUiWorkflowService(
            KnowledgeBaseTreeMutationWorkflowService treeMutationWorkflowService)
        {
            _treeMutationWorkflowService = treeMutationWorkflowService;
        }

        public void AddNode(KnowledgeBaseTreeMutationUiWorkflowContext context) =>
            AddNodeWithParent(
                context,
                context.GetEffectiveParentForRootOperations(),
                "Введите название нового объекта:");

        public void AddChildNode(KnowledgeBaseTreeMutationUiWorkflowContext context)
        {
            if (context.TreeView.SelectedNode?.Tag is not KbNode selectedNode)
            {
                MessageBox.Show(
                    context.Owner,
                    "Выберите узел, в который нужно добавить дочерний объект.",
                    "Внимание",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            AddNodeWithParent(
                context,
                selectedNode,
                "Введите название нового дочернего объекта:");
        }

        public void AddChildNodeFromTemplate(KnowledgeBaseTreeMutationUiWorkflowContext context)
        {
            if (context.TreeView.SelectedNode?.Tag is not KbNode selectedNode)
            {
                MessageBox.Show(
                    context.Owner,
                    "Р’С‹Р±РµСЂРёС‚Рµ СЂРѕРґРёС‚РµР»СЊСЃРєРёР№ СѓР·РµР», РІ РєРѕС‚РѕСЂС‹Р№ РЅСѓР¶РЅРѕ РґРѕР±Р°РІРёС‚СЊ СѓР·РµР» РїРѕ С€Р°Р±Р»РѕРЅСѓ.",
                    "Р’РЅРёРјР°РЅРёРµ",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            AddNodeFromTemplateWithParent(context, selectedNode);
        }

        public void DeleteNode(KnowledgeBaseTreeMutationUiWorkflowContext context)
        {
            if (context.TreeView.SelectedNode?.Tag is not KbNode node)
            {
                MessageBox.Show(
                    context.Owner,
                    "Выберите узел для удаления.",
                    "Внимание",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show(
                    context.Owner,
                    $"Удалить '{node.Name}' и все вложенные элементы?",
                    "Подтверждение",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return;
            }

            var nextSelectedNode = context.TreeView.SelectedNode?.Parent?.Tag as KbNode;
            var expandedNodes = context.CaptureExpandedNodes();
            var result = _treeMutationWorkflowService.DeleteNode(
                context.CurrentWorkshop,
                node,
                context.GetPersistedTreeData());

            if (!result.IsSuccess)
            {
                ShowMutationFailure(context.Owner, result, "Ошибка удаления");
                return;
            }

            ApplySuccessfulMutation(context, result, nextSelectedNode, expandedNodes);
        }

        public void CopyNode(KnowledgeBaseTreeMutationUiWorkflowContext context)
        {
            if (context.TreeView.SelectedNode?.Tag is not KbNode node)
                return;

            _treeMutationWorkflowService.CopyNode(node);
            context.UpdateUi();
            context.SetStatusText($"📋 Скопировано: {node.Name}");
        }

        public void PasteNode(KnowledgeBaseTreeMutationUiWorkflowContext context)
        {
            if (!_treeMutationWorkflowService.HasClipboardNode ||
                context.TreeView.SelectedNode?.Tag is not KbNode parentNode)
            {
                return;
            }

            var expandedNodes = context.CaptureExpandedNodes();
            var result = _treeMutationWorkflowService.PasteNode(parentNode, context.GetPersistedTreeData());
            if (!result.IsSuccess)
            {
                ShowMutationFailure(context.Owner, result, "Ошибка вставки");
                return;
            }

            ApplySuccessfulMutation(context, result, result.AffectedNode, expandedNodes);
        }

        public void RenameNode(KnowledgeBaseTreeMutationUiWorkflowContext context)
        {
            if (context.TreeView.SelectedNode?.Tag is not KbNode node)
                return;

            using var dialog = new InputDialog("Новое название:", node.Name);
            if (dialog.ShowDialog(context.Owner) != DialogResult.OK || string.IsNullOrWhiteSpace(dialog.Result))
                return;

            var expandedNodes = context.CaptureExpandedNodes();
            var result = _treeMutationWorkflowService.RenameNode(node, dialog.Result, context.GetPersistedTreeData());
            if (!result.IsSuccess)
            {
                if (result.Failure != KnowledgeBaseTreeMutationFailure.NoChanges)
                    ShowMutationFailure(context.Owner, result, "Переименование");

                return;
            }

            ApplySuccessfulMutation(context, result, result.AffectedNode, expandedNodes);
        }

        public void HandleDragDrop(KnowledgeBaseTreeMutationUiWorkflowContext context, DragEventArgs e)
        {
            Point point = context.TreeView.PointToClient(new Point(e.X, e.Y));
            TreeNode? targetNode = context.TreeView.GetNodeAt(point);
            TreeNode? draggedNode = e.Data?.GetData(typeof(TreeNode)) as TreeNode;

            if (draggedNode == null || targetNode == null || draggedNode == targetNode)
                return;

            if (targetNode.Tag is not KbNode targetData || draggedNode.Tag is not KbNode draggedData)
                return;

            var expandedNodes = context.CaptureExpandedNodes();
            var result = _treeMutationWorkflowService.MoveNode(
                context.CurrentWorkshop,
                draggedData,
                context.ResolveActualParentNode(draggedData, draggedNode.Parent?.Tag as KbNode),
                targetData,
                context.GetPersistedTreeData());

            if (!result.IsSuccess)
            {
                ShowMutationFailure(context.Owner, result, "Ошибка перемещения");
                return;
            }

            ApplySuccessfulMutation(context, result, result.AffectedNode, expandedNodes);
        }

        public void Undo(KnowledgeBaseTreeMutationUiWorkflowContext context)
        {
            var result = _treeMutationWorkflowService.Undo(context.GetPersistedTreeData());
            if (!result.IsSuccess)
            {
                if (result.Failure != KnowledgeBaseTreeMutationFailure.NoChanges)
                    ShowMutationFailure(context.Owner, result, "Undo/Redo");

                return;
            }

            context.ApplySessionView(result.ViewState, true, null, null);
            context.UpdateDirtyState();
            context.UpdateUi();
            context.SetStatusText(result.StatusMessage ?? "↩ Выполнена отмена");
        }

        public void Redo(KnowledgeBaseTreeMutationUiWorkflowContext context)
        {
            var result = _treeMutationWorkflowService.Redo(context.GetPersistedTreeData());
            if (!result.IsSuccess)
            {
                if (result.Failure != KnowledgeBaseTreeMutationFailure.NoChanges)
                    ShowMutationFailure(context.Owner, result, "Undo/Redo");

                return;
            }

            context.ApplySessionView(result.ViewState, true, null, null);
            context.UpdateDirtyState();
            context.UpdateUi();
            context.SetStatusText(result.StatusMessage ?? "↪ Выполнен повтор");
        }

        private void AddNodeWithParent(
            KnowledgeBaseTreeMutationUiWorkflowContext context,
            KbNode? parentNode,
            string prompt)
        {
            using var dialog = new InputDialog(prompt);
            if (dialog.ShowDialog(context.Owner) != DialogResult.OK || string.IsNullOrWhiteSpace(dialog.Result))
                return;

            var expandedNodes = context.CaptureExpandedNodes();
            var result = _treeMutationWorkflowService.AddNode(
                context.CurrentWorkshop,
                parentNode,
                dialog.Result,
                context.GetPersistedTreeData());

            if (!result.IsSuccess)
            {
                ShowMutationFailure(context.Owner, result, "Невозможно добавить");
                return;
            }

            ApplySuccessfulMutation(context, result, result.AffectedNode, expandedNodes);
        }

        private void AddNodeFromTemplateWithParent(
            KnowledgeBaseTreeMutationUiWorkflowContext context,
            KbNode parentNode)
        {
            if (!_treeMutationWorkflowService.CanAddNodeFromTemplate(parentNode))
            {
                MessageBox.Show(
                    context.Owner,
                    "Р”Р»СЏ РІС‹Р±СЂР°РЅРЅРѕРіРѕ СѓР·Р»Р° РЅРµС‚ РґРѕСЃС‚СѓРїРЅС‹С… С€Р°Р±Р»РѕРЅРѕРІ РґРѕС‡РµСЂРЅРёС… РѕР±СЉРµРєС‚РѕРІ.",
                    "Composition template",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            var templates = _compositionTemplateService.GetChildTemplates(parentNode);
            using var dialog = new KnowledgeBaseCompositionTemplateDialog(
                "Р”РѕР±Р°РІРёС‚СЊ РёР· С€Р°Р±Р»РѕРЅР°",
                "Р’С‹Р±РµСЂРёС‚Рµ С€Р°Р±Р»РѕРЅ Рё РёРјСЏ РЅРѕРІРѕРіРѕ СѓР·Р»Р°:",
                templates,
                collectNodeName: true,
                inheritedLocation: KnowledgeBaseCompositionTemplateService.BuildInheritedLocation(parentNode));
            if (dialog.ShowDialog(context.Owner) != DialogResult.OK)
                return;

            var expandedNodes = context.CaptureExpandedNodes();
            var result = _treeMutationWorkflowService.AddNodeFromTemplate(
                context.CurrentWorkshop,
                parentNode,
                dialog.NodeName,
                dialog.SelectedTemplateId,
                context.GetPersistedTreeData());
            if (!result.IsSuccess)
            {
                ShowMutationFailure(context.Owner, result, "РќРµРІРѕР·РјРѕР¶РЅРѕ РґРѕР±Р°РІРёС‚СЊ РёР· С€Р°Р±Р»РѕРЅР°");
                return;
            }

            ApplySuccessfulMutation(context, result, result.AffectedNode, expandedNodes);
        }

        private static void ApplySuccessfulMutation(
            KnowledgeBaseTreeMutationUiWorkflowContext context,
            KnowledgeBaseTreeMutationResult result,
            KbNode? nodeToSelect,
            ISet<KbNode> expandedNodes)
        {
            context.ApplySessionView(
                result.ViewState,
                false,
                nodeToSelect,
                expandedNodes);
            context.RefreshSearchAfterMutation();
            context.UpdateDirtyState();
            context.UpdateUi();

            if (!string.IsNullOrWhiteSpace(result.StatusMessage))
                context.SetStatusText(result.StatusMessage);
        }

        private static void ShowMutationFailure(
            IWin32Window owner,
            KnowledgeBaseTreeMutationResult result,
            string title)
        {
            if (string.IsNullOrWhiteSpace(result.ErrorMessage))
                return;

            MessageBoxIcon icon = result.Failure switch
            {
                KnowledgeBaseTreeMutationFailure.DeleteFailed => MessageBoxIcon.Error,
                KnowledgeBaseTreeMutationFailure.MoveFailed => MessageBoxIcon.Error,
                KnowledgeBaseTreeMutationFailure.RestoreFailed => MessageBoxIcon.Error,
                _ => MessageBoxIcon.Warning
            };

            MessageBox.Show(
                owner,
                result.ErrorMessage,
                title,
                MessageBoxButtons.OK,
                icon);
        }
    }
}
