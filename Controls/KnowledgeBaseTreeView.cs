using AsutpKnowledgeBase.UiServices;

namespace AsutpKnowledgeBase
{
    public class KnowledgeBaseTreeView : TreeView
    {
        private static readonly Color TextColor = Color.FromArgb(31, 41, 55);

        public KnowledgeBaseTreeView()
        {
            BorderStyle = BorderStyle.None;
            FullRowSelect = true;
            HideSelection = false;
            ShowLines = false;
            ShowPlusMinus = false;
            ShowRootLines = false;
            HotTracking = false;
            Indent = 22;
            ItemHeight = 24;
            BackColor = Color.White;
            ForeColor = TextColor;
            StateImageList = KnowledgeBaseTreeNodeVisuals.CreateExpandStateImageList();
        }

        public void RefreshTreeVisuals()
        {
            if (IsDisposed || !IsHandleCreated)
                return;

            RefreshExpandStateImages(Nodes);
            Invalidate();
        }

        protected override void OnAfterExpand(TreeViewEventArgs e)
        {
            base.OnAfterExpand(e);
            if (e.Node != null)
                ApplyExpandStateImage(e.Node);
        }

        protected override void OnAfterCollapse(TreeViewEventArgs e)
        {
            base.OnAfterCollapse(e);
            if (e.Node != null)
                ApplyExpandStateImage(e.Node);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            TreeViewHitTestInfo hitTest = HitTest(e.Location);

            base.OnMouseDown(e);

            if (e.Button != MouseButtons.Left ||
                hitTest.Location != TreeViewHitTestLocations.StateImage ||
                hitTest.Node == null ||
                hitTest.Node.Nodes.Count == 0)
            {
                return;
            }

            if (hitTest.Node.IsExpanded)
                hitTest.Node.Collapse();
            else
                hitTest.Node.Expand();
        }

        private static void RefreshExpandStateImages(TreeNodeCollection nodes)
        {
            foreach (TreeNode node in nodes)
            {
                ApplyExpandStateImage(node);
                RefreshExpandStateImages(node.Nodes);
            }
        }

        private static void ApplyExpandStateImage(TreeNode node)
        {
            node.StateImageIndex = KnowledgeBaseTreeNodeVisuals.GetExpandStateImageIndex(
                hasChildren: node.Nodes.Count > 0,
                isExpanded: node.IsExpanded);
        }
    }
}
