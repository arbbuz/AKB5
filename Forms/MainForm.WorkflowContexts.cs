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
                GetPersistedTreeData = GetPersistedTreeData,
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
                UpdateUi = () => UpdateUI(),
                SetStatusText = SetLastActionText,
                RememberDatabasePath = path => RememberDatabasePath(path, showErrorMessage: false)
            };

        private KnowledgeBaseWorkshopUiWorkflowContext CreateWorkshopUiWorkflowContext() =>
            new()
            {
                Owner = this,
                GetPersistedTreeData = GetPersistedTreeData,
                ApplySessionView = viewState => ApplySessionView(viewState, clearSearch: false),
                OfferProtectiveSnapshotBeforeDangerousOperation = OfferProtectiveSnapshotBeforeDangerousOperation,
                RefreshSearchAfterMutation = RefreshSearchAfterMutation,
                UpdateDirtyState = UpdateDirtyState,
                UpdateUi = () => UpdateUI(),
                SetStatusText = SetLastActionText
            };

        private KnowledgeBaseTreeMutationUiWorkflowContext CreateTreeMutationUiWorkflowContext() =>
            new()
            {
                Owner = this,
                TreeView = tvTree,
                CurrentWorkshop = _currentWorkshop,
                GetPersistedTreeData = GetPersistedTreeData,
                GetEffectiveParentForRootOperations = GetEffectiveParentForRootOperations,
                ResolveActualParentNode = ResolveActualParentNode,
                GetEquipmentCatalogItems = () => _session.EquipmentCatalogItems,
                CaptureExpandedNodes = CaptureExpandedNodes,
                GetDeleteImpact = BuildDeleteImpact,
                OfferProtectiveSnapshotBeforeDangerousOperation = OfferProtectiveSnapshotBeforeDangerousOperation,
                ApplySessionView = ApplySessionView,
                RefreshSearchAfterMutation = RefreshSearchAfterMutation,
                UpdateDirtyState = UpdateDirtyState,
                UpdateUi = () => UpdateUI(),
                SetStatusText = SetLastActionText
            };

        private bool OfferProtectiveSnapshotBeforeDangerousOperation(
            string operationDescription,
            string snapshotNote) =>
            _fileUiWorkflowService.OfferProtectiveSnapshotBeforeDangerousOperation(
                CreateFileUiWorkflowContext(),
                operationDescription,
                snapshotNote);

        private void BindWorkshops(IReadOnlyList<string> workshopNames, string selectedWorkshop)
        {
            _isBindingWorkshops = true;
            try
            {
                _treeViewService.BindWorkshops(cmbWorkshops, workshopNames, selectedWorkshop);
            }
            finally
            {
                _isBindingWorkshops = false;
            }

            RefreshWorkshopSelectorLayout();
        }

        private void ResetTransientUiStateAfterLoad()
        {
            _history.Clear();
            _treeMutationWorkflowService.ClearClipboard();
        }

        private void UpdateDirtyState() =>
            _session.RefreshDirtyState(GetPersistedTreeData());

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
            _session.SyncCurrentWorkshop(GetPersistedTreeData());

        private List<KbNode> GetVisibleTreeData()
            => _treeViewService.GetVisibleTreeData(tvTree);

        private List<KbNode> GetPersistedTreeData()
            => _treeViewService.GetPersistedTreeData(tvTree);

        private KbNode? GetEffectiveParentForRootOperations() =>
            _treeViewService.GetEffectiveParentForRootOperations();

        private KbNode? ResolveActualParentNode(KbNode node, KbNode? visibleParentNode) =>
            _treeViewService.ResolveActualParentNode(node, visibleParentNode);

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
            _treeViewService.RefreshSearch(
                tvTree,
                _config,
                txtSearch.Text,
                GetSelectedSearchScope(),
                _session.CompositionEntries,
                _session.DocumentLinks,
                _session.SoftwareRecords,
                _session.MaintenanceScheduleProfiles);
            UpdateSearchButtons();
        }

        private HashSet<KbNode> CaptureExpandedNodes() =>
            _treeViewService.CaptureExpandedNodes(tvTree);

        private KnowledgeBaseTreeDeleteImpact BuildDeleteImpact(KbNode node)
        {
            var nodeIds = new HashSet<string>(StringComparer.Ordinal);
            CollectNodeIds(node, nodeIds);

            return new KnowledgeBaseTreeDeleteImpact
            {
                ChildNodeCount = Math.Max(0, nodeIds.Count - 1),
                CompositionEntryCount =
                    _session.CompositionEntries.Count(entry => nodeIds.Contains(entry.ParentNodeId)) +
                    _session.CompositionRacks.Count(rack => nodeIds.Contains(rack.ParentNodeId)),
                DocumentLinkCount = _session.DocumentLinks.Count(link => nodeIds.Contains(link.OwnerNodeId)),
                SoftwareRecordCount = _session.SoftwareRecords.Count(record => nodeIds.Contains(record.OwnerNodeId)),
                MaintenanceProfileCount = _session.MaintenanceScheduleProfiles.Count(profile => nodeIds.Contains(profile.OwnerNodeId))
            };
        }

        private static void CollectNodeIds(KbNode node, ISet<string> nodeIds)
        {
            if (!string.IsNullOrWhiteSpace(node.NodeId))
                nodeIds.Add(node.NodeId);

            foreach (KbNode child in node.Children)
                CollectNodeIds(child, nodeIds);
        }
    }
}
