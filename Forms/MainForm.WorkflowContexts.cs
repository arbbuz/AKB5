using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;
using AsutpKnowledgeBase.UiServices;

namespace AsutpKnowledgeBase
{
    public partial class MainForm
    {
        private KnowledgeBaseFileUiWorkflowContext CreateFileUiWorkflowContext() =>
            new()
            {
                Owner = this,
                GetCurrentTreeData = GetCurrentTreeData,
                SaveCurrentWorkshopState = SaveCurrentWorkshopState,
                UpdateDirtyState = UpdateDirtyState,
                GetUiState = () => new KnowledgeBaseFileUiState
                {
                    IsDirty = _isDirty,
                    RequiresSave = _requiresSave,
                    CurrentWorkshop = _currentWorkshop,
                    LastSavedWorkshop = _lastSavedWorkshop
                },
                ResetTransientUiStateAfterLoad = ResetTransientUiStateAfterLoad,
                ApplyLoadedSessionView = viewState => ApplySessionView(viewState, clearSearch: true),
                UpdateUi = UpdateUI,
                SetStatusText = SetLastActionText
            };

        private KnowledgeBaseWorkshopUiWorkflowContext CreateWorkshopUiWorkflowContext() =>
            new()
            {
                Owner = this,
                GetCurrentTreeData = GetCurrentTreeData,
                ApplySessionView = viewState => ApplySessionView(viewState, clearSearch: false),
                RefreshSearchAfterMutation = RefreshSearchAfterMutation,
                UpdateDirtyState = UpdateDirtyState,
                UpdateUi = UpdateUI,
                SetStatusText = SetLastActionText
            };

        private KnowledgeBaseTreeMutationUiWorkflowContext CreateTreeMutationUiWorkflowContext() =>
            new()
            {
                Owner = this,
                TreeView = tvTree,
                CurrentWorkshop = _currentWorkshop,
                GetCurrentTreeData = GetCurrentTreeData,
                CaptureExpandedNodes = CaptureExpandedNodes,
                ApplySessionView = ApplySessionView,
                RefreshSearchAfterMutation = RefreshSearchAfterMutation,
                UpdateDirtyState = UpdateDirtyState,
                UpdateUi = UpdateUI,
                SetStatusText = SetLastActionText
            };

        private void BindWorkshops(IReadOnlyList<string> workshopNames, string selectedWorkshop)
        {
            _isBindingWorkshops = true;
            _treeViewService.BindWorkshops(cmbWorkshops, workshopNames, selectedWorkshop);
            _isBindingWorkshops = false;
        }

        private void ResetTransientUiStateAfterLoad()
        {
            _history.Clear();
            _treeMutationWorkflowService.ClearClipboard();
        }

        private void UpdateDirtyState() =>
            _session.RefreshDirtyState(GetCurrentTreeData());

        private void ApplySessionView(
            KnowledgeBaseSessionViewState viewState,
            bool clearSearch,
            KbNode? nodeToSelect = null,
            ISet<KbNode>? expandedNodes = null)
        {
            BindWorkshops(viewState.WorkshopNames, viewState.CurrentWorkshop);
            _treeViewService.ApplySessionView(tvTree, viewState, clearSearch, nodeToSelect, expandedNodes);
            UpdateSearchButtons();
        }

        private void SaveCurrentWorkshopState() =>
            _session.SyncCurrentWorkshop(GetCurrentTreeData());

        private List<KbNode> GetCurrentTreeData()
            => _treeViewService.GetCurrentTreeData(tvTree);

        private void UpdateSearchButtons()
        {
            bool canNavigate = _treeViewService.CanNavigateSearch;
            btnSearchPrev.Enabled = canNavigate;
            btnSearchNext.Enabled = canNavigate;
        }

        private void ClearSearch()
        {
            SetLastActionText(_treeViewService.ClearSearch());
            UpdateSearchButtons();
        }

        private void RefreshSearchAfterMutation()
        {
            _treeViewService.RefreshSearch(tvTree, _config, txtSearch.Text);
            UpdateSearchButtons();
        }

        private HashSet<KbNode> CaptureExpandedNodes() =>
            _treeViewService.CaptureExpandedNodes(tvTree);
    }
}
