using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.UiServices
{
    public class KnowledgeBaseTreeMutationUiWorkflowContext
    {
        public IWin32Window Owner { get; init; } = null!;

        public TreeView TreeView { get; init; } = null!;

        public string CurrentWorkshop { get; init; } = string.Empty;

        public Func<List<KbNode>> GetCurrentTreeData { get; init; } = null!;

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

        public KnowledgeBaseTreeMutationUiWorkflowService(
            KnowledgeBaseTreeMutationWorkflowService treeMutationWorkflowService)
        {
            _treeMutationWorkflowService = treeMutationWorkflowService;
        }

        public void AddNode(KnowledgeBaseTreeMutationUiWorkflowContext context)
        {
            using var dialog = new InputDialog("Введите название нового объекта:");
            if (dialog.ShowDialog(context.Owner) != DialogResult.OK || string.IsNullOrWhiteSpace(dialog.Result))
                return;

            var selectedNode = context.TreeView.SelectedNode?.Tag as KbNode;
            var expandedNodes = context.CaptureExpandedNodes();
            var result = _treeMutationWorkflowService.AddNode(
                context.CurrentWorkshop,
                selectedNode,
                dialog.Result,
                context.GetCurrentTreeData());

            if (!result.IsSuccess)
            {
                ShowMutationFailure(context.Owner, result, "Невозможно добавить");
                return;
            }

            ApplySuccessfulMutation(context, result, result.AffectedNode, expandedNodes);
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
                context.GetCurrentTreeData());

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
            var result = _treeMutationWorkflowService.PasteNode(parentNode, context.GetCurrentTreeData());
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
            var result = _treeMutationWorkflowService.RenameNode(node, dialog.Result, context.GetCurrentTreeData());
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
                draggedNode.Parent?.Tag as KbNode,
                targetData,
                context.GetCurrentTreeData());

            if (!result.IsSuccess)
            {
                ShowMutationFailure(context.Owner, result, "Ошибка перемещения");
                return;
            }

            ApplySuccessfulMutation(context, result, result.AffectedNode, expandedNodes);
        }

        public void Undo(KnowledgeBaseTreeMutationUiWorkflowContext context)
        {
            var result = _treeMutationWorkflowService.Undo(context.GetCurrentTreeData());
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
            var result = _treeMutationWorkflowService.Redo(context.GetCurrentTreeData());
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

        private void ApplySuccessfulMutation(
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
