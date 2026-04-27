using System.Collections;
using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.UiServices
{
    /// <summary>
    /// Инкапсулирует WinForms-специфичную работу с TreeView:
    /// построение узлов, восстановление expanded-state и навигацию по search-results.
    /// </summary>
    public class KnowledgeBaseTreeViewService
    {
        private static readonly IComparer TreeNodeDisplayComparer = new KnowledgeBaseTreeNodeDisplayComparer();
        private readonly KnowledgeBaseTreeSearchService _treeSearchService = new();
        private readonly List<SearchNavigationItem> _searchResults = new();
        private KnowledgeBaseWorkshopTreeProjection _currentProjection =
            KnowledgeBaseWorkshopTreeProjection.Create(string.Empty, Array.Empty<KbNode>());
        private int _currentSearchIndex = -1;

        public bool CanNavigateSearch => _searchResults.Count > 1;

        public void BindWorkshops(ComboBox comboBox, IReadOnlyList<string> workshopNames, string selectedWorkshop)
        {
            comboBox.BeginUpdate();
            comboBox.Items.Clear();

            foreach (var workshop in workshopNames)
                comboBox.Items.Add(workshop);

            if (comboBox.Items.Count > 0)
                comboBox.SelectedItem = selectedWorkshop;

            comboBox.EndUpdate();
        }

        public void ApplySessionView(
            TreeView treeView,
            KnowledgeBaseSessionViewState viewState,
            bool clearSearch,
            KbNode? nodeToSelect = null,
            ISet<KbNode>? expandedNodes = null)
        {
            TreeNode? selectedTreeNode = null;
            _currentProjection = KnowledgeBaseWorkshopTreeProjection.Create(
                viewState.CurrentWorkshop,
                viewState.CurrentRoots);
            treeView.TreeViewNodeSorter ??= TreeNodeDisplayComparer;

            treeView.BeginUpdate();
            treeView.SelectedNode = null;
            treeView.Nodes.Clear();

            foreach (var node in _currentProjection.VisibleRoots)
            {
                bool visibleRootIsWorkshopRoot = node.NodeType == KbNodeType.WorkshopRoot;
                treeView.Nodes.Add(
                    BuildTreeNode(
                        node,
                        nodeToSelect,
                        expandedNodes,
                        visibleDepth: 0,
                        visibleRootIsWorkshopRoot,
                        ref selectedTreeNode));
            }

            treeView.Sort();
            treeView.EndUpdate();

            if (expandedNodes == null)
                ExpandTreeToLevel(treeView.Nodes, targetLevelIndex: 1);

            if (clearSearch)
                ClearSearch();
            else
                ResetSearchResults();

            if (selectedTreeNode != null)
            {
                ExpandToNode(selectedTreeNode);
                treeView.SelectedNode = selectedTreeNode;
            }

            RefreshTreeViewVisuals(treeView);
        }

        public HashSet<KbNode> CaptureExpandedNodes(TreeView treeView)
        {
            var expandedNodes = new HashSet<KbNode>();
            CollectExpandedNodes(treeView.Nodes, expandedNodes);
            return expandedNodes;
        }

        public List<KbNode> GetVisibleTreeData(TreeView treeView)
            => _currentProjection.VisibleRoots.ToList();

        public List<KbNode> GetPersistedTreeData(TreeView treeView) =>
            _currentProjection.CreatePersistedRootsSnapshot(_currentProjection.VisibleRoots);

        public KbNode? GetEffectiveParentForRootOperations() =>
            _currentProjection.GetEffectiveParentForRootOperations();

        public KbNode? ResolveActualParentNode(KbNode node, KbNode? visibleParentNode) =>
            _currentProjection.ResolveActualParent(node, visibleParentNode);

        public string PerformSearch(TreeView treeView, KbConfig config, string searchText)
        {
            string normalizedSearch = searchText.Trim();
            if (string.IsNullOrEmpty(normalizedSearch))
                return ClearSearch();

            ResetSearchResults();

            var matches = _treeSearchService.FindMatches(GetVisibleTreeData(treeView), config, normalizedSearch);
            foreach (var match in matches)
            {
                if (TryFindTreeNode(treeView.Nodes, match.Node, out var treeNode) && treeNode != null)
                    _searchResults.Add(new SearchNavigationItem(treeNode));
            }

            if (_searchResults.Count == 0)
                return $"Поиск: \"{normalizedSearch}\" | Совпадений не найдено";

            _currentSearchIndex = 0;
            SelectSearchResult(treeView, _currentSearchIndex);
            return string.Empty;
        }

        public string? NavigateSearch(TreeView treeView, int direction)
        {
            if (_searchResults.Count == 0)
                return null;

            _currentSearchIndex += direction;
            if (_currentSearchIndex >= _searchResults.Count)
                _currentSearchIndex = 0;
            if (_currentSearchIndex < 0)
                _currentSearchIndex = _searchResults.Count - 1;

            SelectSearchResult(treeView, _currentSearchIndex);
            return string.Empty;
        }

        public void RefreshSearch(TreeView treeView, KbConfig config, string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                ClearSearch();
                return;
            }

            PerformSearch(treeView, config, searchText);
        }

        public string ClearSearch()
        {
            ResetSearchResults();
            return "Поиск очищен";
        }

        public static void RefreshTreeViewVisuals(TreeView treeView)
        {
            if (treeView.IsDisposed)
                return;

            if (treeView is KnowledgeBaseTreeView knowledgeBaseTreeView)
            {
                knowledgeBaseTreeView.RefreshTreeVisuals();
                return;
            }

            treeView.Invalidate();
        }

        private TreeNode BuildTreeNode(
            KbNode node,
            KbNode? nodeToSelect,
            ISet<KbNode>? expandedNodes,
            int visibleDepth,
            bool visibleRootIsWorkshopRoot,
            ref TreeNode? selectedTreeNode)
        {
            bool hasChildren = node.Children.Count > 0;
            int hierarchyLevel = visibleRootIsWorkshopRoot ? visibleDepth - 1 : visibleDepth;
            string imageKey = KnowledgeBaseTreeNodeVisuals.GetImageKey(node, hierarchyLevel, hasChildren);
            var treeNode = new TreeNode(node.Name)
            {
                Tag = node,
                ImageKey = imageKey,
                SelectedImageKey = imageKey,
                StateImageIndex = KnowledgeBaseTreeNodeVisuals.GetExpandStateImageIndex(
                    hasChildren,
                    isExpanded: false)
            };

            if (ReferenceEquals(node, nodeToSelect))
                selectedTreeNode = treeNode;

            foreach (var child in node.Children)
            {
                treeNode.Nodes.Add(
                    BuildTreeNode(
                        child,
                        nodeToSelect,
                        expandedNodes,
                        visibleDepth + 1,
                        visibleRootIsWorkshopRoot,
                        ref selectedTreeNode));
            }

            if (expandedNodes?.Contains(node) == true)
                treeNode.Expand();

            return treeNode;
        }

        private static void ExpandTreeToLevel(TreeNodeCollection nodes, int targetLevelIndex)
        {
            foreach (TreeNode node in nodes)
                ExpandNodeRecursive(node, targetLevelIndex);
        }

        private static void ExpandNodeRecursive(TreeNode node, int targetLevelIndex)
        {
            if (node.Tag is not KbNode kbNode || kbNode.LevelIndex >= targetLevelIndex)
                return;

            node.Expand();
            foreach (TreeNode child in node.Nodes)
                ExpandNodeRecursive(child, targetLevelIndex);
        }

        private static bool TryFindTreeNode(TreeNodeCollection nodes, KbNode targetNode, out TreeNode? matchedNode)
        {
            foreach (TreeNode node in nodes)
            {
                if (ReferenceEquals(node.Tag, targetNode))
                {
                    matchedNode = node;
                    return true;
                }

                if (TryFindTreeNode(node.Nodes, targetNode, out matchedNode))
                    return true;
            }

            matchedNode = null;
            return false;
        }

        private static void ExpandToNode(TreeNode node)
        {
            TreeNode? current = node.Parent;
            while (current != null)
            {
                current.Expand();
                current = current.Parent;
            }

            node.Expand();
        }

        private void SelectSearchResult(TreeView treeView, int index)
        {
            if (index < 0 || index >= _searchResults.Count)
                return;

            var node = _searchResults[index].TreeNode;
            ExpandToNode(node);
            treeView.SelectedNode = node;
            treeView.Focus();
            RefreshTreeViewVisuals(treeView);
        }

        private void CollectExpandedNodes(TreeNodeCollection nodes, ISet<KbNode> expandedNodes)
        {
            foreach (TreeNode node in nodes)
            {
                if (node.IsExpanded && node.Tag is KbNode kbNode)
                    expandedNodes.Add(kbNode);

                CollectExpandedNodes(node.Nodes, expandedNodes);
            }
        }

        private void ResetSearchResults()
        {
            _searchResults.Clear();
            _currentSearchIndex = -1;
        }

        private sealed record SearchNavigationItem(TreeNode TreeNode);

        private sealed class KnowledgeBaseTreeNodeDisplayComparer : IComparer
        {
            public int Compare(object? x, object? y)
            {
                var left = x as TreeNode;
                var right = y as TreeNode;

                if (ReferenceEquals(left, right))
                    return 0;
                if (left is null)
                    return -1;
                if (right is null)
                    return 1;

                return KnowledgeBaseNaturalStringComparer.Instance.Compare(left.Text, right.Text);
            }
        }
    }
}
