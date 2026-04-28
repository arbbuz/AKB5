using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase
{
    public partial class MainForm
    {
        private void ApplyWorkspaceState(KnowledgeBaseSelectedNodeState selectedNodeState)
        {
            string? currentNodeId = (tvTree.SelectedNode?.Tag as Models.KbNode)?.NodeId;
            bool resetToInfoTab = !string.Equals(_lastSelectedWorkspaceNodeId, currentNodeId, StringComparison.Ordinal);
            _lastSelectedWorkspaceNodeId = currentNodeId;

            if (!selectedNodeState.HasSelection)
            {
                pnlSelectedNodeInfoScreen.Visible = false;
                tabSelectedNodeWorkspace.Visible = false;
                return;
            }

            if (selectedNodeState.Workspace.UseTabHost)
            {
                MoveSelectedNodeInfoScreen(tabSelectedNodeInfo);
                ConfigureWorkspaceTabs(selectedNodeState.Workspace, resetToInfoTab);
                pnlSelectedNodeInfoScreen.Visible = false;
                tabSelectedNodeWorkspace.Visible = true;
                return;
            }

            MoveSelectedNodeInfoScreen(pnlSelectedNodeInfoScreen);
            pnlSelectedNodeInfoScreen.Visible = true;
            tabSelectedNodeWorkspace.Visible = false;
        }

        private void MoveSelectedNodeInfoScreen(Control parent)
        {
            if (ReferenceEquals(selectedNodeInfoScreen.Parent, parent))
                return;

            selectedNodeInfoScreen.Parent?.Controls.Remove(selectedNodeInfoScreen);
            parent.Controls.Add(selectedNodeInfoScreen);
            selectedNodeInfoScreen.Dock = DockStyle.Fill;
        }

        private void ConfigureWorkspaceTabs(KnowledgeBaseNodeWorkspaceState workspace, bool resetToInfoTab)
        {
            KnowledgeBaseNodeWorkspaceTabKind? preferredTab =
                !resetToInfoTab && tabSelectedNodeWorkspace.SelectedTab?.Tag is KnowledgeBaseNodeWorkspaceTabKind tabKind
                    ? tabKind
                    : null;

            tabSelectedNodeWorkspace.SuspendLayout();
            try
            {
                tabSelectedNodeWorkspace.TabPages.Clear();
                foreach (var tab in workspace.Tabs)
                {
                    var tabPage = GetWorkspaceTabPage(tab.Kind);
                    tabPage.Text = tab.Title;
                    UpdateWorkspacePlaceholder(tab);
                    tabSelectedNodeWorkspace.TabPages.Add(tabPage);
                }

                if (preferredTab.HasValue)
                {
                    foreach (TabPage tabPage in tabSelectedNodeWorkspace.TabPages)
                    {
                        if (tabPage.Tag is KnowledgeBaseNodeWorkspaceTabKind currentTabKind && currentTabKind == preferredTab.Value)
                        {
                            tabSelectedNodeWorkspace.SelectedTab = tabPage;
                            return;
                        }
                    }
                }

                SelectDefaultWorkspaceTab();
            }
            finally
            {
                tabSelectedNodeWorkspace.ResumeLayout();
            }
        }

        private void SelectDefaultWorkspaceTab()
        {
            foreach (TabPage tabPage in tabSelectedNodeWorkspace.TabPages)
            {
                if (tabPage.Tag is KnowledgeBaseNodeWorkspaceTabKind tabKind &&
                    tabKind == KnowledgeBaseNodeWorkspaceTabKind.Info)
                {
                    tabSelectedNodeWorkspace.SelectedTab = tabPage;
                    return;
                }
            }

            if (tabSelectedNodeWorkspace.TabPages.Count > 0)
                tabSelectedNodeWorkspace.SelectedIndex = 0;
        }

        private void SelectWorkspaceTab(KnowledgeBaseNodeWorkspaceTabKind tabKind)
        {
            foreach (TabPage tabPage in tabSelectedNodeWorkspace.TabPages)
            {
                if (tabPage.Tag is KnowledgeBaseNodeWorkspaceTabKind currentTabKind &&
                    currentTabKind == tabKind)
                {
                    tabSelectedNodeWorkspace.SelectedTab = tabPage;
                    return;
                }
            }
        }

        private TabPage GetWorkspaceTabPage(KnowledgeBaseNodeWorkspaceTabKind tabKind) => tabKind switch
        {
            KnowledgeBaseNodeWorkspaceTabKind.Info => tabSelectedNodeInfo,
            KnowledgeBaseNodeWorkspaceTabKind.Composition => tabSelectedNodeComposition,
            KnowledgeBaseNodeWorkspaceTabKind.DocsAndSoftware => tabSelectedNodeDocsAndSoftware,
            KnowledgeBaseNodeWorkspaceTabKind.Network => tabSelectedNodeNetwork,
            _ => tabSelectedNodeInfo
        };

        private void UpdateWorkspacePlaceholder(KnowledgeBaseNodeWorkspaceTabState tab)
        {
            switch (tab.Kind)
            {
                case KnowledgeBaseNodeWorkspaceTabKind.DocsAndSoftware:
                    lblSelectedNodeDocsPlaceholder.Text = tab.Description;
                    break;
            }
        }
    }
}
