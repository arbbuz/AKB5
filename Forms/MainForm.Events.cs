using AsutpKnowledgeBase.UiServices;

namespace AsutpKnowledgeBase
{
    public partial class MainForm
    {
        private void InitializeEvents()
        {
            tvTree.ItemDrag += TvTree_ItemDrag;
            tvTree.DragEnter += TvTree_DragEnter;
            tvTree.DragDrop += TvTree_DragDrop;
            tvTree.AfterSelect += TvTree_AfterSelect;
            cmbWorkshops.SelectedIndexChanged += CmbWorkshops_SelectedIndexChanged;

            btnSearch.Click += (s, e) => PerformSearch();
            btnSearchPrev.Click += (s, e) => NavigateSearch(-1);
            btnSearchNext.Click += (s, e) => NavigateSearch(1);

            btnUndo.Click += (s, e) => UndoAction();
            btnRedo.Click += (s, e) => RedoAction();

            KeyPreview = true;
            KeyDown += (s, e) =>
            {
                if (e.Control && e.KeyCode == Keys.Z) { e.SuppressKeyPress = true; UndoAction(); }
                if (e.Control && e.KeyCode == Keys.Y) { e.SuppressKeyPress = true; RedoAction(); }
                if (e.Control && e.KeyCode == Keys.C) { e.SuppressKeyPress = true; CopyNode(); }
                if (e.Control && e.KeyCode == Keys.V) { e.SuppressKeyPress = true; PasteNode(); }
                if (e.KeyCode == Keys.F2) { e.SuppressKeyPress = true; RenameNode(); }
                if (e.KeyCode == Keys.Insert) { e.SuppressKeyPress = true; AddNode(); }
                if (e.Control && e.KeyCode == Keys.F) { e.SuppressKeyPress = true; txtSearch.Focus(); }
                if (e.KeyCode == Keys.Enter && txtSearch.Focused) { e.SuppressKeyPress = true; PerformSearch(); }
                if (e.KeyCode == Keys.Escape && txtSearch.Focused) { e.SuppressKeyPress = true; txtSearch.Clear(); ClearSearch(); UpdateUI(); }
            };
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
            var data = _session.CreateSaveData(GetCurrentTreeData());
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

        private void TvTree_DragEnter(object? sender, DragEventArgs e) => e.Effect = DragDropEffects.Move;

        private void TvTree_DragDrop(object? sender, DragEventArgs e)
            => _treeMutationUiWorkflowService.HandleDragDrop(CreateTreeMutationUiWorkflowContext(), e);

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

        private void BtnSetup_Click(object? sender, EventArgs e)
            => _workshopUiWorkflowService.ConfigureLevels(CreateWorkshopUiWorkflowContext());

        private void TvTree_AfterSelect(object? sender, TreeViewEventArgs e) => UpdateUI();

        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
            => _fileUiWorkflowService.HandleFormClosing(CreateFileUiWorkflowContext(), e);
    }
}
