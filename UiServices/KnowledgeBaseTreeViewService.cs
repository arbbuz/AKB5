using System.Collections;
using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.UiServices
{
    public sealed class KnowledgeBaseTreeSearchNavigationResult
    {
        public string StatusText { get; init; } = string.Empty;

        public bool HasActiveResult { get; init; }

        public KnowledgeBaseNodeWorkspaceTabKind PreferredTabKind { get; init; } =
            KnowledgeBaseNodeWorkspaceTabKind.Info;
    }

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

        public List<KbNode> GetVisibleTreeData(TreeView treeView) =>
            _currentProjection.VisibleRoots.ToList();

        public List<KbNode> GetPersistedTreeData(TreeView treeView) =>
            _currentProjection.CreatePersistedRootsSnapshot(_currentProjection.VisibleRoots);

        public KbNode? GetEffectiveParentForRootOperations() =>
            _currentProjection.GetEffectiveParentForRootOperations();

        public KbNode? ResolveActualParentNode(KbNode node, KbNode? visibleParentNode) =>
            _currentProjection.ResolveActualParent(node, visibleParentNode);

        public KnowledgeBaseTreeSearchNavigationResult PerformSearch(
            TreeView treeView,
            KbConfig config,
            string searchText,
            KnowledgeBaseSearchScope scope,
            IReadOnlyList<KbCompositionEntry>? compositionEntries,
            IReadOnlyList<KbDocumentLink>? documentLinks,
            IReadOnlyList<KbSoftwareRecord>? softwareRecords)
        {
            string normalizedSearch = searchText.Trim();
            if (string.IsNullOrEmpty(normalizedSearch))
            {
                return new KnowledgeBaseTreeSearchNavigationResult
                {
                    StatusText = ClearSearch()
                };
            }

            ResetSearchResults();

            var matches = _treeSearchService.FindMatches(
                GetVisibleTreeData(treeView),
                config,
                normalizedSearch,
                scope,
                compositionEntries,
                documentLinks,
                softwareRecords);

            foreach (var match in matches)
            {
                if (TryFindTreeNode(treeView.Nodes, match.Node, out var treeNode) && treeNode != null)
                    _searchResults.Add(new SearchNavigationItem(treeNode, match));
            }

            if (_searchResults.Count == 0)
            {
                return new KnowledgeBaseTreeSearchNavigationResult
                {
                    StatusText = $"Поиск [{GetScopeText(scope)}]: \"{normalizedSearch}\" | Совпадений не найдено"
                };
            }

            _currentSearchIndex = 0;
            return SelectSearchResult(treeView, _currentSearchIndex, scope);
        }

        public KnowledgeBaseTreeSearchNavigationResult? NavigateSearch(
            TreeView treeView,
            int direction,
            KnowledgeBaseSearchScope scope)
        {
            if (_searchResults.Count == 0)
                return null;

            _currentSearchIndex += direction;
            if (_currentSearchIndex >= _searchResults.Count)
                _currentSearchIndex = 0;
            if (_currentSearchIndex < 0)
                _currentSearchIndex = _searchResults.Count - 1;

            return SelectSearchResult(treeView, _currentSearchIndex, scope);
        }

        public void RefreshSearch(
            TreeView treeView,
            KbConfig config,
            string searchText,
            KnowledgeBaseSearchScope scope,
            IReadOnlyList<KbCompositionEntry>? compositionEntries,
            IReadOnlyList<KbDocumentLink>? documentLinks,
            IReadOnlyList<KbSoftwareRecord>? softwareRecords)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                ClearSearch();
                return;
            }

            PerformSearch(
                treeView,
                config,
                searchText,
                scope,
                compositionEntries,
                documentLinks,
                softwareRecords);
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

        private KnowledgeBaseTreeSearchNavigationResult SelectSearchResult(
            TreeView treeView,
            int index,
            KnowledgeBaseSearchScope scope)
        {
            if (index < 0 || index >= _searchResults.Count)
            {
                return new KnowledgeBaseTreeSearchNavigationResult
                {
                    StatusText = string.Empty
                };
            }

            var searchResult = _searchResults[index];
            var node = searchResult.TreeNode;
            ExpandToNode(node);
            treeView.SelectedNode = node;
            treeView.Focus();
            RefreshTreeViewVisuals(treeView);

            return new KnowledgeBaseTreeSearchNavigationResult
            {
                StatusText = BuildSearchStatusText(searchResult.Match, index, _searchResults.Count, scope),
                HasActiveResult = true,
                PreferredTabKind = searchResult.Match.PreferredTabKind
            };
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

        private static string BuildSearchStatusText(
            KnowledgeBaseTreeSearchMatch match,
            int index,
            int total,
            KnowledgeBaseSearchScope scope) =>
            $"Поиск [{GetScopeText(scope)}]: {index + 1}/{total} | {GetDomainText(match.Domain)} | {match.NodePath} | {match.MatchFieldLabel}: {match.MatchValue}";

        private static string GetScopeText(KnowledgeBaseSearchScope scope) =>
            scope switch
            {
                KnowledgeBaseSearchScope.All => "Все",
                KnowledgeBaseSearchScope.Tree => "Дерево",
                KnowledgeBaseSearchScope.Card => "Карточка",
                KnowledgeBaseSearchScope.Composition => "Состав",
                KnowledgeBaseSearchScope.DocsAndSoftware => "Документация и ПО",
                _ => "Все"
            };

        private static string GetDomainText(KnowledgeBaseSearchDomain domain) =>
            domain switch
            {
                KnowledgeBaseSearchDomain.Tree => "Дерево",
                KnowledgeBaseSearchDomain.Card => "Карточка",
                KnowledgeBaseSearchDomain.Composition => "Состав",
                KnowledgeBaseSearchDomain.DocsAndSoftware => "Документация и ПО",
                _ => "Дерево"
            };

        private sealed record SearchNavigationItem(TreeNode TreeNode, KnowledgeBaseTreeSearchMatch Match);

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
