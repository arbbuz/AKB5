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

        public Func<KbNode, KnowledgeBaseTreeDeleteImpact> GetDeleteImpact { get; init; } = null!;

        public Func<string, string, bool> OfferProtectiveSnapshotBeforeDangerousOperation { get; init; } = null!;

        public Action RefreshSearchAfterMutation { get; init; } = null!;

        public Action UpdateDirtyState { get; init; } = null!;

        public Action UpdateUi { get; init; } = null!;

        public Action<string> SetStatusText { get; init; } = null!;
    }

    public class KnowledgeBaseTreeDeleteImpact
    {
        public int ChildNodeCount { get; init; }

        public int CompositionEntryCount { get; init; }

        public int DocumentLinkCount { get; init; }

        public int SoftwareRecordCount { get; init; }

        public int NetworkFileReferenceCount { get; init; }

        public int MaintenanceProfileCount { get; init; }

        public bool HasImpact =>
            ChildNodeCount > 0 ||
            CompositionEntryCount > 0 ||
            DocumentLinkCount > 0 ||
            SoftwareRecordCount > 0 ||
            NetworkFileReferenceCount > 0 ||
            MaintenanceProfileCount > 0;
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
                    "Выберите родительский узел, в который нужно добавить узел по шаблону.",
                    "Внимание",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            AddNodeFromTemplateWithParent(context, selectedNode);
        }

        public void CreateObjectFromTemplate(KnowledgeBaseTreeMutationUiWorkflowContext context)
        {
            KbNode? parentNode = context.TreeView.SelectedNode?.Tag as KbNode ??
                                 context.GetEffectiveParentForRootOperations();
            var templates = _treeMutationWorkflowService.GetAvailableObjectTemplates(parentNode);
            if (templates.Count == 0)
            {
                string message = _treeMutationWorkflowService.HasObjectTemplates
                    ? "Для выбранного места нет шаблонов объектов, которые помещаются в глубину дерева."
                    : "В базе нет сохранённых шаблонов объектов.";
                MessageBox.Show(
                    context.Owner,
                    message,
                    "Шаблоны объектов",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            using var dialog = new KnowledgeBaseObjectTemplateCreateDialog(
                templates,
                BuildObjectTemplateParentText(parentNode));
            if (dialog.ShowDialog(context.Owner) != DialogResult.OK)
                return;

            var expandedNodes = context.CaptureExpandedNodes();
            var result = _treeMutationWorkflowService.CreateObjectFromTemplate(
                context.CurrentWorkshop,
                parentNode,
                dialog.SelectedTemplateId,
                dialog.RootNodeName,
                context.GetPersistedTreeData());
            if (!result.IsSuccess)
            {
                ShowMutationFailure(context.Owner, result, "Создание из шаблона объекта");
                return;
            }

            ApplySuccessfulMutation(context, result, result.AffectedNode, expandedNodes);
        }

        public void SaveObjectAsTemplate(KnowledgeBaseTreeMutationUiWorkflowContext context)
        {
            if (context.TreeView.SelectedNode?.Tag is not KbNode selectedNode)
            {
                MessageBox.Show(
                    context.Owner,
                    "Выберите объект, который нужно сохранить как шаблон.",
                    "Шаблоны объектов",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            if (!KnowledgeBaseTreeMutationWorkflowService.CanSaveObjectAsTemplate(selectedNode))
            {
                MessageBox.Show(
                    context.Owner,
                    "Этот узел нельзя сохранить как шаблон объекта.",
                    "Шаблоны объектов",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            using var dialog = new KnowledgeBaseObjectTemplateSaveDialog(selectedNode);
            if (dialog.ShowDialog(context.Owner) != DialogResult.OK)
                return;

            var expandedNodes = context.CaptureExpandedNodes();
            var result = _treeMutationWorkflowService.SaveObjectAsTemplate(
                selectedNode,
                dialog.DisplayName,
                dialog.Category,
                dialog.Description,
                context.GetPersistedTreeData());
            if (!result.IsSuccess)
            {
                ShowMutationFailure(context.Owner, result, "Сохранение шаблона объекта");
                return;
            }

            ApplySuccessfulMutation(context, result, selectedNode, expandedNodes);
        }

        public void ApplyObjectTemplateToExistingObject(KnowledgeBaseTreeMutationUiWorkflowContext context)
        {
            if (context.TreeView.SelectedNode?.Tag is not KbNode selectedNode)
            {
                MessageBox.Show(
                    context.Owner,
                    "Выберите объект, к которому нужно применить шаблон.",
                    "Шаблоны объектов",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            var templates = _treeMutationWorkflowService.GetApplicableObjectTemplates(selectedNode);
            if (templates.Count == 0)
            {
                MessageBox.Show(
                    context.Owner,
                    _treeMutationWorkflowService.HasObjectTemplates
                        ? "Для типа выбранного объекта нет подходящих шаблонов."
                        : "В базе нет сохранённых шаблонов объектов.",
                    "Шаблоны объектов",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            using var dialog = new KnowledgeBaseObjectTemplateApplyPreviewDialog(
                selectedNode,
                templates,
                templateId => _treeMutationWorkflowService.PreviewApplyObjectTemplateToExistingObject(
                    selectedNode,
                    templateId));
            if (dialog.ShowDialog(context.Owner) != DialogResult.OK)
                return;

            KnowledgeBaseObjectTemplateApplicationPlan plan =
                _treeMutationWorkflowService.PreviewApplyObjectTemplateToExistingObject(
                    selectedNode,
                    dialog.SelectedTemplateId);
            if (RequiresProtectiveSnapshotBeforeApplyTemplate(plan) &&
                !context.OfferProtectiveSnapshotBeforeDangerousOperation(
                    "массовым применением шаблона к объекту",
                    $"Перед применением шаблона \"{plan.TemplateDisplayName}\" к объекту \"{selectedNode.Name}\""))
            {
                return;
            }

            var expandedNodes = context.CaptureExpandedNodes();
            var result = _treeMutationWorkflowService.ApplyObjectTemplateToExistingObject(
                context.CurrentWorkshop,
                selectedNode,
                dialog.SelectedTemplateId,
                context.GetPersistedTreeData());
            if (!result.IsSuccess)
            {
                ShowMutationFailure(context.Owner, result, "Применение шаблона объекта");
                return;
            }

            ApplySuccessfulMutation(context, result, selectedNode, expandedNodes);
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

            KnowledgeBaseTreeDeleteImpact impact = context.GetDeleteImpact(node);
            if (MessageBox.Show(
                    context.Owner,
                    BuildDeleteConfirmationText(node, impact),
                    impact.HasImpact ? "Подтверждение опасного удаления" : "Подтверждение",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2) != DialogResult.Yes)
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

        private static string BuildDeleteConfirmationText(KbNode node, KnowledgeBaseTreeDeleteImpact impact)
        {
            var lines = new List<string>
            {
                $"Удалить \"{node.Name}\"?"
            };

            if (impact.HasImpact)
            {
                lines.Add(string.Empty);
                lines.Add("Будут удалены связанные данные:");
                if (impact.ChildNodeCount > 0)
                    lines.Add($"- дочерних объектов: {impact.ChildNodeCount}");
                if (impact.CompositionEntryCount > 0)
                    lines.Add($"- записей состава: {impact.CompositionEntryCount}");
                if (impact.DocumentLinkCount > 0)
                    lines.Add($"- документов: {impact.DocumentLinkCount}");
                if (impact.SoftwareRecordCount > 0)
                    lines.Add($"- записей ПО: {impact.SoftwareRecordCount}");
                if (impact.NetworkFileReferenceCount > 0)
                    lines.Add($"- сетевых файлов: {impact.NetworkFileReferenceCount}");
                if (impact.MaintenanceProfileCount > 0)
                    lines.Add($"- профилей ТО: {impact.MaintenanceProfileCount}");
            }
            else
            {
                lines.Add("Связанные данные не найдены.");
            }

            lines.Add(string.Empty);
            lines.Add("Продолжить?");
            return string.Join(Environment.NewLine, lines);
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

            DialogResult confirmation = MessageBox.Show(
                context.Owner,
                BuildMoveConfirmationText(
                    context,
                    draggedData,
                    draggedNode.Parent?.Tag as KbNode,
                    targetData,
                    draggedNode.Level + 1,
                    targetNode.Level + 2),
                "Подтверждение перемещения",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);
            if (confirmation != DialogResult.Yes)
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
                    ShowMutationFailure(context.Owner, result, "Отмена и повтор");

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
                    ShowMutationFailure(context.Owner, result, "Отмена и повтор");

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
                    "Для выбранного узла нет доступных шаблонов дочерних объектов.",
                    "Шаблон состава",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            var templates = _compositionTemplateService.GetChildTemplates(parentNode);
            using var dialog = new KnowledgeBaseCompositionTemplateDialog(
                "Добавить из шаблона",
                "Выберите шаблон и имя нового узла:",
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
                ShowMutationFailure(context.Owner, result, "Невозможно добавить из шаблона");
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

        private static string BuildObjectTemplateParentText(KbNode? parentNode)
        {
            if (parentNode == null || parentNode.NodeType == KbNodeType.WorkshopRoot)
                return "Корень текущего цеха";

            string name = parentNode.Name?.Trim() ?? string.Empty;
            return string.IsNullOrWhiteSpace(name)
                ? "Выбранный объект"
                : name;
        }

        private static bool RequiresProtectiveSnapshotBeforeApplyTemplate(
            KnowledgeBaseObjectTemplateApplicationPlan plan) =>
            plan.IsSuccess &&
            (plan.NodeAdditions.Count > 1 ||
             plan.CompositionEntries.Count > 0 ||
             plan.DocumentLinks.Count > 0 ||
             plan.SoftwareRecords.Count > 0 ||
             plan.NetworkFileReferences.Count > 0 ||
             plan.MaintenanceScheduleProfiles.Count > 0);

        private static string BuildMoveConfirmationText(
            KnowledgeBaseTreeMutationUiWorkflowContext context,
            KbNode draggedData,
            KbNode? visibleOldParentNode,
            KbNode targetData,
            int oldVisibleLevel,
            int newVisibleLevel)
        {
            KbNode? actualOldParent = context.ResolveActualParentNode(draggedData, visibleOldParentNode);
            var lines = new List<string>
            {
                $"Переместить объект \"{draggedData.Name}\"?",
                string.Empty,
                "Было:",
                $"Родитель: {BuildMoveParentText(actualOldParent, context.CurrentWorkshop)}",
                $"Уровень: Lvl{oldVisibleLevel}",
                string.Empty,
                "Станет:",
                $"Родитель: {BuildMoveParentText(targetData, context.CurrentWorkshop)}",
                $"Уровень: Lvl{newVisibleLevel}"
            };

            if (oldVisibleLevel != newVisibleLevel)
            {
                lines.Add(string.Empty);
                lines.Add($"Уровень изменится: Lvl{oldVisibleLevel} -> Lvl{newVisibleLevel}.");
                lines.Add("При смене уровня объект примет свойства нового уровня.");
            }

            lines.Add(string.Empty);
            lines.Add("Продолжить?");
            return string.Join(Environment.NewLine, lines);
        }

        private static string BuildMoveParentText(KbNode? parentNode, string currentWorkshop)
        {
            if (parentNode == null)
                return BuildWorkshopRootText(currentWorkshop);

            if (parentNode.NodeType == KbNodeType.WorkshopRoot)
                return BuildWorkshopRootText(parentNode.Name);

            string name = parentNode.Name?.Trim() ?? string.Empty;
            return string.IsNullOrWhiteSpace(name)
                ? "Выбранный объект"
                : name;
        }

        private static string BuildWorkshopRootText(string workshopName)
        {
            string name = workshopName?.Trim() ?? string.Empty;
            return string.IsNullOrWhiteSpace(name)
                ? "Корень цеха"
                : $"Корень цеха: {name}";
        }
    }
}
