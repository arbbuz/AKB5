using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase
{
    public partial class MainForm
    {
        private void ApplyWorkspaceState(KnowledgeBaseSelectedNodeState selectedNodeState)
        {
            if (!selectedNodeState.HasSelection)
            {
                pnlSelectedNodeInfoScreen.Visible = false;
                tabSelectedNodeWorkspace.Visible = false;
                return;
            }

            if (selectedNodeState.Workspace.UseTabHost)
            {
                MoveSelectedNodeCard(tabSelectedNodeInfo);
                ConfigureWorkspaceTabs(selectedNodeState.Workspace);
                pnlSelectedNodeInfoScreen.Visible = false;
                tabSelectedNodeWorkspace.Visible = true;
                return;
            }

            MoveSelectedNodeCard(pnlSelectedNodeInfoScreen);
            pnlSelectedNodeInfoScreen.Visible = true;
            tabSelectedNodeWorkspace.Visible = false;
        }

        private void MoveSelectedNodeCard(Control parent)
        {
            if (ReferenceEquals(tblSelectedNodeCard.Parent, parent))
                return;

            tblSelectedNodeCard.Parent?.Controls.Remove(tblSelectedNodeCard);
            parent.Controls.Add(tblSelectedNodeCard);
            tblSelectedNodeCard.Dock = DockStyle.Fill;
        }

        private void ConfigureWorkspaceTabs(KnowledgeBaseNodeWorkspaceState workspace)
        {
            KnowledgeBaseNodeWorkspaceTabKind? preferredTab =
                tabSelectedNodeWorkspace.SelectedTab?.Tag is KnowledgeBaseNodeWorkspaceTabKind tabKind
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

                if (tabSelectedNodeWorkspace.TabPages.Count > 0)
                    tabSelectedNodeWorkspace.SelectedIndex = 0;
            }
            finally
            {
                tabSelectedNodeWorkspace.ResumeLayout();
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
                case KnowledgeBaseNodeWorkspaceTabKind.Composition:
                    lblSelectedNodeCompositionPlaceholder.Text = tab.Description;
                    break;
                case KnowledgeBaseNodeWorkspaceTabKind.DocsAndSoftware:
                    lblSelectedNodeDocsPlaceholder.Text = tab.Description;
                    break;
                case KnowledgeBaseNodeWorkspaceTabKind.Network:
                    lblSelectedNodeNetworkPlaceholder.Text = tab.Description;
                    break;
            }
        }
    }
}
