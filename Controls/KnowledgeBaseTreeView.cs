using System.Drawing.Drawing2D;

namespace AsutpKnowledgeBase
{
    public class KnowledgeBaseTreeView : TreeView
    {
        private static readonly Color HoverBackColor = Color.FromArgb(241, 245, 249);
        private static readonly Color SelectedBackColor = Color.FromArgb(37, 99, 235);
        private static readonly Color SelectedInactiveBackColor = Color.FromArgb(219, 234, 254);
        private static readonly Color SelectedBorderColor = Color.FromArgb(29, 78, 216);
        private static readonly Color TextColor = Color.FromArgb(31, 41, 55);
        private static readonly Color SelectedTextColor = Color.White;
        private static readonly Color SelectedInactiveTextColor = Color.FromArgb(30, 64, 175);

        private TreeNode? _hoveredNode;
        private TreeNode? _lastSelectedNode;

        public KnowledgeBaseTreeView()
        {
            DrawMode = TreeViewDrawMode.OwnerDrawAll;
            BorderStyle = BorderStyle.None;
            FullRowSelect = true;
            HideSelection = false;
            ShowLines = false;
            ShowPlusMinus = false;
            ShowRootLines = false;
            HotTracking = false;
            Indent = 22;
            ItemHeight = 28;
            BackColor = Color.White;
            ForeColor = TextColor;
            DoubleBuffered = true;
            SetStyle(
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.ResizeRedraw,
                true);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            UpdateHoveredNode(GetNodeAt(e.Location));
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            UpdateHoveredNode(null);
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            TreeNode? clickedNode = GetNodeAt(e.Location);
            bool shouldToggle = e.Button == MouseButtons.Left &&
                clickedNode != null &&
                clickedNode.Nodes.Count > 0 &&
                GetNodeIconBounds(clickedNode, GetRowBounds(clickedNode.Bounds)).Contains(e.Location);

            base.OnMouseDown(e);

            if (!shouldToggle || clickedNode == null)
                return;

            if (clickedNode.IsExpanded)
                clickedNode.Collapse();
            else
                clickedNode.Expand();
        }

        protected override void OnDrawNode(DrawTreeNodeEventArgs e)
        {
            if (e.Node == null)
                return;

            e.DrawDefault = false;

            TreeNode node = e.Node;
            Rectangle rowBounds = GetRowBounds(e.Bounds);
            Rectangle iconBounds = GetNodeIconBounds(node, rowBounds);
            Rectangle textBounds = GetTextBounds(e.Bounds, rowBounds);
            Rectangle contentBounds = new(
                Math.Max(2, iconBounds.Left - 6),
                rowBounds.Top + 2,
                Math.Max(0, ClientSize.Width - Math.Max(2, iconBounds.Left - 6) - 6),
                Math.Max(0, rowBounds.Height - 4));

            bool isSelected = (e.State & TreeNodeStates.Selected) == TreeNodeStates.Selected;
            bool isFocusedSelection = isSelected && Focused;
            bool isHovered = !isSelected && ReferenceEquals(_hoveredNode, node);

            using Region previousClip = e.Graphics.Clip.Clone();
            e.Graphics.SetClip(rowBounds);

            using SolidBrush clearBrush = new(BackColor);
            e.Graphics.FillRectangle(clearBrush, rowBounds);

            DrawNodeBackground(e.Graphics, contentBounds, isFocusedSelection, isSelected, isHovered);
            DrawNodeIcon(e.Graphics, node, iconBounds);
            DrawNodeText(e.Graphics, node, textBounds, isFocusedSelection, isSelected);

            e.Graphics.SetClip(previousClip, CombineMode.Replace);
        }

        protected override void OnAfterSelect(TreeViewEventArgs e)
        {
            InvalidateNode(_lastSelectedNode);
            _lastSelectedNode = e.Node;
            InvalidateNode(_lastSelectedNode);
            base.OnAfterSelect(e);
        }

        protected override void OnAfterCollapse(TreeViewEventArgs e)
        {
            base.OnAfterCollapse(e);
            UpdateHoveredNode(null);
            Invalidate();
        }

        protected override void OnAfterExpand(TreeViewEventArgs e)
        {
            base.OnAfterExpand(e);
            Invalidate();
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            InvalidateNode(SelectedNode);
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            InvalidateNode(SelectedNode);
        }

        private void UpdateHoveredNode(TreeNode? node)
        {
            if (ReferenceEquals(_hoveredNode, node))
                return;

            TreeNode? previousNode = _hoveredNode;
            _hoveredNode = node;

            InvalidateNode(previousNode);
            InvalidateNode(_hoveredNode);
        }

        private void InvalidateNode(TreeNode? node)
        {
            if (node == null || IsDisposed)
                return;

            Rectangle rowBounds = GetRowBounds(node.Bounds);
            if (!rowBounds.IsEmpty)
                Invalidate(rowBounds);
        }

        private void DrawNodeBackground(
            Graphics graphics,
            Rectangle bounds,
            bool isFocusedSelection,
            bool isSelected,
            bool isHovered)
        {
            if (!isSelected && !isHovered)
                return;

            Color fillColor = isFocusedSelection
                ? SelectedBackColor
                : isSelected
                    ? SelectedInactiveBackColor
                    : HoverBackColor;
            Color? borderColor = isFocusedSelection ? SelectedBorderColor : null;

            SmoothingMode previousSmoothingMode = graphics.SmoothingMode;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using GraphicsPath path = CreateRoundedRectanglePath(bounds, 7);
            using SolidBrush fillBrush = new(fillColor);
            graphics.FillPath(fillBrush, path);

            if (borderColor.HasValue)
            {
                using Pen borderPen = new(borderColor.Value, 1);
                graphics.DrawPath(borderPen, path);
            }

            graphics.SmoothingMode = previousSmoothingMode;
        }

        private void DrawNodeIcon(Graphics graphics, TreeNode node, Rectangle iconBounds)
        {
            Image? image = ResolveNodeImage(node);
            if (image == null)
                return;

            graphics.DrawImage(image, iconBounds);
        }

        private void DrawNodeText(
            Graphics graphics,
            TreeNode node,
            Rectangle textBounds,
            bool isFocusedSelection,
            bool isSelected)
        {
            Color textColor = isFocusedSelection
                ? SelectedTextColor
                : isSelected
                    ? SelectedInactiveTextColor
                    : TextColor;

            TextRenderer.DrawText(
                graphics,
                node.Text,
                node.NodeFont ?? Font,
                textBounds,
                textColor,
                TextFormatFlags.Left |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis |
                TextFormatFlags.NoPrefix |
                TextFormatFlags.PreserveGraphicsClipping);
        }

        private Image? ResolveNodeImage(TreeNode node)
        {
            if (ImageList == null)
                return null;

            string key = Focused && !string.IsNullOrWhiteSpace(node.SelectedImageKey)
                ? node.SelectedImageKey
                : node.ImageKey;

            if (!string.IsNullOrWhiteSpace(key) && ImageList.Images.ContainsKey(key))
                return ImageList.Images[key];

            int imageIndex = node.ImageIndex;
            if (imageIndex >= 0 && imageIndex < ImageList.Images.Count)
                return ImageList.Images[imageIndex];

            return null;
        }

        private Rectangle GetNodeIconBounds(TreeNode node, Rectangle rowBounds)
        {
            Size imageSize = ImageList?.ImageSize ?? new Size(16, 16);
            int left = Math.Max(6, node.Bounds.Left - imageSize.Width - 4);
            int top = rowBounds.Top + Math.Max(0, (rowBounds.Height - imageSize.Height) / 2);
            return new Rectangle(left, top, imageSize.Width, imageSize.Height);
        }

        private Rectangle GetRowBounds(Rectangle nodeBounds)
        {
            int height = Math.Max(1, nodeBounds.Height);
            return new Rectangle(0, nodeBounds.Top, ClientSize.Width, height);
        }

        private Rectangle GetTextBounds(Rectangle nodeBounds, Rectangle rowBounds)
        {
            return new Rectangle(
                nodeBounds.Left,
                rowBounds.Top,
                Math.Max(0, ClientSize.Width - nodeBounds.Left - 8),
                rowBounds.Height);
        }

        private static GraphicsPath CreateRoundedRectanglePath(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            var path = new GraphicsPath();

            if (bounds.Width <= 0 || bounds.Height <= 0)
                return path;

            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
