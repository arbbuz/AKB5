using AsutpKnowledgeBase.UiServices;

namespace AsutpKnowledgeBase
{
    public partial class MainForm
    {
        private void InitializeEvents()
        {
            splitMain.SplitterMoved += SplitMain_SplitterMoved;
            tvTree.ItemDrag += TvTree_ItemDrag;
            tvTree.MouseDown += TvTree_MouseDown;
            tvTree.DragEnter += TvTree_DragEnter;
            tvTree.DragDrop += TvTree_DragDrop;
            tvTree.AfterSelect += TvTree_AfterSelect;
            cmbWorkshops.SelectedIndexChanged += CmbWorkshops_SelectedIndexChanged;

            btnSearch.Click += (s, e) => PerformSearch();
            btnSearchPrev.Click += (s, e) => NavigateSearch(-1);
            btnSearchNext.Click += (s, e) => NavigateSearch(1);
            txtSearch.TextBox.PreviewKeyDown += TxtSearch_PreviewKeyDown;
            txtSearch.TextBox.KeyDown += TxtSearch_KeyDown;
            selectedNodeInfoScreen.BrowsePhotoRequested += BtnBrowsePhoto_Click;
            selectedNodeInfoScreen.OpenPhotoRequested += BtnOpenPhoto_Click;
            selectedNodeCompositionScreen.AddSlottedRequested += AddSlottedCompositionEntry;
            selectedNodeCompositionScreen.AddAuxiliaryRequested += AddAuxiliaryCompositionEntry;
            selectedNodeCompositionScreen.EditSelectedRequested += EditSelectedCompositionEntry;
            selectedNodeCompositionScreen.DeleteSelectedRequested += DeleteSelectedCompositionEntry;

            btnUndo.Click += (s, e) => UndoAction();
            btnRedo.Click += (s, e) => RedoAction();
            btnCollapseTree.Click += (s, e) => CollapseTreeToRoots();

            selectedNodeInfoScreen.DescriptionChangedByUser +=
                (s, e) => HandleNodeDetailsChanged(details => details.Description = selectedNodeInfoScreen.DescriptionText);
            selectedNodeInfoScreen.LocationChangedByUser +=
                (s, e) => HandleNodeDetailsChanged(details => details.Location = selectedNodeInfoScreen.LocationText);
            selectedNodeInfoScreen.PhotoPathChangedByUser +=
                (s, e) => HandleNodeDetailsChanged(details => details.PhotoPath = selectedNodeInfoScreen.PhotoPathText);
            selectedNodeInfoScreen.IpAddressChangedByUser +=
                (s, e) => HandleNodeDetailsChanged(details => details.IpAddress = selectedNodeInfoScreen.IpAddressText);
            selectedNodeInfoScreen.SchemaLinkChangedByUser +=
                (s, e) => HandleNodeDetailsChanged(details => details.SchemaLink = selectedNodeInfoScreen.SchemaLinkText);

            KeyPreview = true;
            KeyDown += (s, e) =>
            {
                if (ShouldBypassGlobalShortcutForTextInput(e))
                    return;

                if (e.Control && e.KeyCode == Keys.Z) { e.SuppressKeyPress = true; UndoAction(); }
                if (e.Control && e.KeyCode == Keys.Y) { e.SuppressKeyPress = true; RedoAction(); }
                if (e.Control && e.KeyCode == Keys.C) { e.SuppressKeyPress = true; CopyNode(); }
                if (e.Control && e.KeyCode == Keys.V) { e.SuppressKeyPress = true; PasteNode(); }
                if (e.KeyCode == Keys.F2) { e.SuppressKeyPress = true; RenameNode(); }
                if (e.KeyCode == Keys.Insert) { e.SuppressKeyPress = true; AddNode(); }
                if (e.Control && e.KeyCode == Keys.F) { e.SuppressKeyPress = true; txtSearch.Focus(); }
            };
        }

        private bool ShouldBypassGlobalShortcutForTextInput(KeyEventArgs e)
        {
            if (!e.Control || e.KeyCode is not (Keys.C or Keys.V or Keys.Z or Keys.Y))
                return false;

            if (txtSearch.Focused)
                return true;

            Control? focusedControl = ActiveControl;
            while (focusedControl is ContainerControl container && container.ActiveControl != null)
                focusedControl = container.ActiveControl;

            return focusedControl is TextBoxBase;
        }

        private static void TxtSearch_PreviewKeyDown(object? sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
                e.IsInputKey = true;
        }

        private void TxtSearch_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                PerformSearch();
                return;
            }

            if (e.KeyCode == Keys.Escape)
            {
                e.SuppressKeyPress = true;
                ClearSearchInput();
            }
        }

        private void ClearSearchInput()
        {
            txtSearch.Clear();
            ClearSearch();
            UpdateUI();
            txtSearch.TextBox.Focus();
        }

        private void BtnOpen_Click(object? sender, EventArgs e)
            => _fileUiWorkflowService.OpenDatabase(CreateFileUiWorkflowContext());

        private void BtnLoad_Click(object? sender, EventArgs e)
            => _fileUiWorkflowService.ReloadDatabase(CreateFileUiWorkflowContext());

        private void BtnSave_Click(object? sender, EventArgs e)
            => _fileUiWorkflowService.SaveCurrentDatabase(CreateFileUiWorkflowContext());

        private void BtnSaveAs_Click(object? sender, EventArgs e)
            => _fileUiWorkflowService.SaveDatabaseAs(CreateFileUiWorkflowContext());

        private void BtnImportExcel_Click(object? sender, EventArgs e)
        {
            var fileContext = CreateFileUiWorkflowContext();
            _excelUiWorkflowService.Import(new KnowledgeBaseExcelImportUiWorkflowContext
            {
                Owner = this,
                CurrentDataPath = CurrentDataPath,
                ConfirmContinueBeforeImport = actionDescription =>
                    _fileUiWorkflowService.ConfirmContinueBeforeReplace(fileContext, actionDescription),
                ReplaceAllData = data => _fileUiWorkflowService.ReplaceAllData(fileContext, data),
                SetStatusText = SetLastActionText
            });
        }

        private void BtnExportExcel_Click(object? sender, EventArgs e)
        {
            SaveCurrentWorkshopState();
            var data = _session.CreateSaveData(GetPersistedTreeData());
            _excelUiWorkflowService.Export(
                this,
                data,
                CurrentDataPath,
                SetLastActionText);
        }

        private void PerformSearch()
        {
            SetLastActionText(_treeViewService.PerformSearch(tvTree, _config, txtSearch.Text));
            UpdateSearchButtons();
        }

        private void NavigateSearch(int direction)
        {
            var statusText = _treeViewService.NavigateSearch(tvTree, direction);
            UpdateSearchButtons();
            if (!string.IsNullOrWhiteSpace(statusText))
                SetLastActionText(statusText);
        }

        private void AddNode()
            => _treeMutationUiWorkflowService.AddNode(CreateTreeMutationUiWorkflowContext());

        private void AddChildNode()
            => _treeMutationUiWorkflowService.AddChildNode(CreateTreeMutationUiWorkflowContext());

        private void DeleteNode()
            => _treeMutationUiWorkflowService.DeleteNode(CreateTreeMutationUiWorkflowContext());

        private void CopyNode()
            => _treeMutationUiWorkflowService.CopyNode(CreateTreeMutationUiWorkflowContext());

        private void PasteNode()
            => _treeMutationUiWorkflowService.PasteNode(CreateTreeMutationUiWorkflowContext());

        private void RenameNode()
            => _treeMutationUiWorkflowService.RenameNode(CreateTreeMutationUiWorkflowContext());

        private void TvTree_ItemDrag(object? sender, ItemDragEventArgs e)
        {
            if (e.Button == MouseButtons.Left && e.Item != null)
                DoDragDrop(e.Item, DragDropEffects.Move);
        }

        private void TvTree_MouseDown(object? sender, MouseEventArgs e)
        {
            TreeNode? clickedNode = tvTree.GetNodeAt(e.Location);
            if (clickedNode != null)
            {
                if (!ReferenceEquals(tvTree.SelectedNode, clickedNode))
                    tvTree.SelectedNode = clickedNode;

                return;
            }

            if (tvTree.SelectedNode != null)
            {
                tvTree.SelectedNode = null;
                UpdateUI();
            }
        }

        private void TvTree_DragEnter(object? sender, DragEventArgs e) => e.Effect = DragDropEffects.Move;

        private void TvTree_DragDrop(object? sender, DragEventArgs e)
            => _treeMutationUiWorkflowService.HandleDragDrop(CreateTreeMutationUiWorkflowContext(), e);

        private void SplitMain_SplitterMoved(object? sender, SplitterEventArgs e)
        {
            if (_isApplyingDeferredLayout)
                return;

            SaveCurrentSplitterDistance();
        }

        private void UndoAction()
            => _treeMutationUiWorkflowService.Undo(CreateTreeMutationUiWorkflowContext());

        private void RedoAction()
            => _treeMutationUiWorkflowService.Redo(CreateTreeMutationUiWorkflowContext());

        private void CmbWorkshops_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_isBindingWorkshops)
                return;

            _workshopUiWorkflowService.SelectWorkshop(
                CreateWorkshopUiWorkflowContext(),
                cmbWorkshops.SelectedItem as string);
        }

        private void BtnAddWorkshop_Click(object? sender, EventArgs e)
            => _workshopUiWorkflowService.AddWorkshop(CreateWorkshopUiWorkflowContext());

        private void BtnDeleteWorkshop_Click(object? sender, EventArgs e)
            => _workshopUiWorkflowService.DeleteCurrentWorkshop(CreateWorkshopUiWorkflowContext());

        private void BtnRenameWorkshop_Click(object? sender, EventArgs e)
            => _workshopUiWorkflowService.RenameCurrentWorkshop(CreateWorkshopUiWorkflowContext());

        private void TvTree_AfterSelect(object? sender, TreeViewEventArgs e)
        {
            bool keepTreeFocus = tvTree.ContainsFocus;
            UpdateUI();

            if (keepTreeFocus && tvTree.CanFocus && !tvTree.Focused)
                tvTree.Focus();
        }

        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            _fileUiWorkflowService.HandleFormClosing(CreateFileUiWorkflowContext(), e);
            if (e.Cancel)
                return;

            SaveCurrentSplitterDistance();
            SaveCurrentWindowLayout();
        }
    }
}
