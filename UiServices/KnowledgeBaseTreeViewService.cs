using System;
using System.Collections.Generic;
using System.Windows.Forms;
using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.UiServices
{
    /// <summary>
    /// Инкапсулирует WinForms-специфичную работу с TreeView:
    /// построение узлов, восстановление expanded-state и поиск.
    /// </summary>
    public class KnowledgeBaseTreeViewService
    {
        private readonly List<TreeNode> _searchResults = new();
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

            treeView.BeginUpdate();
            treeView.SelectedNode = null;
            treeView.Nodes.Clear();

            foreach (var node in viewState.CurrentRoots)
                treeView.Nodes.Add(BuildTreeNode(node, nodeToSelect, expandedNodes, ref selectedTreeNode));

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
        }

        public HashSet<KbNode> CaptureExpandedNodes(TreeView treeView)
        {
            var expandedNodes = new HashSet<KbNode>();
            CollectExpandedNodes(treeView.Nodes, expandedNodes);
            return expandedNodes;
        }

        public List<KbNode> GetCurrentTreeData(TreeView treeView)
        {
            var list = new List<KbNode>();
            foreach (TreeNode treeNode in treeView.Nodes)
            {
                if (treeNode.Tag is KbNode node)
                    list.Add(node);
            }

            return list;
        }

        public string PerformSearch(TreeView treeView, string searchText)
        {
            string normalizedSearch = searchText.Trim();
            if (string.IsNullOrEmpty(normalizedSearch))
                return ClearSearch();

            ResetSearchResults();
            FindAllMatches(treeView.Nodes, normalizedSearch);

            if (_searchResults.Count == 0)
            {
                treeView.SelectedNode = null;
                return $"❌ Не найдено: {normalizedSearch}";
            }

            _currentSearchIndex = 0;
            SelectSearchResult(treeView, _currentSearchIndex);
            return BuildSearchStatus();
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
            return BuildSearchStatus();
        }

        public void RefreshSearch(TreeView treeView, string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                ClearSearch();
                return;
            }

            PerformSearch(treeView, searchText);
        }

        public string ClearSearch()
        {
            ResetSearchResults();
            return "Готово";
        }

        private TreeNode BuildTreeNode(
            KbNode node,
            KbNode? nodeToSelect,
            ISet<KbNode>? expandedNodes,
            ref TreeNode? selectedTreeNode)
        {
            var treeNode = new TreeNode(node.Name) { Tag = node };

            if (ReferenceEquals(node, nodeToSelect))
                selectedTreeNode = treeNode;

            foreach (var child in node.Children)
                treeNode.Nodes.Add(BuildTreeNode(child, nodeToSelect, expandedNodes, ref selectedTreeNode));

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

        private void FindAllMatches(TreeNodeCollection nodes, string searchText)
        {
            foreach (TreeNode node in nodes)
            {
                if (node.Text.Contains(searchText, StringComparison.CurrentCultureIgnoreCase))
                    _searchResults.Add(node);

                FindAllMatches(node.Nodes, searchText);
            }
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

            var node = _searchResults[index];
            ExpandToNode(node);
            treeView.SelectedNode = node;
            treeView.Focus();
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

        private string BuildSearchStatus() =>
            $"🔍 Найдено: {_searchResults.Count} | Показан {_currentSearchIndex + 1} из {_searchResults.Count}";

        private void ResetSearchResults()
        {
            _searchResults.Clear();
            _currentSearchIndex = -1;
        }
    }
}
