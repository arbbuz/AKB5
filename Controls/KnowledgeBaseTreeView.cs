using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace AsutpKnowledgeBase
{
    public class KnowledgeBaseTreeView : TreeView
    {
        private const int TvmFirst = 0x1100;
        private const int TvmSetExtendedStyle = TvmFirst + 44;
        private const int TvsExDoubleBuffer = 0x0004;
        private const int ExpandGlyphSize = 9;
        private const int ExpandGlyphGap = 3;
        private const int ContentGap = 5;

        private static readonly Color HoverBackColor = Color.FromArgb(241, 245, 249);
        private static readonly Color SelectedBackColor = Color.FromArgb(37, 99, 235);
        private static readonly Color SelectedInactiveBackColor = Color.FromArgb(219, 234, 254);
        private static readonly Color SelectedBorderColor = Color.FromArgb(29, 78, 216);
        private static readonly Color TextColor = Color.FromArgb(31, 41, 55);
        private static readonly Color GlyphColor = Color.FromArgb(71, 85, 105);
        private static readonly Color SelectedTextColor = Color.White;
        private static readonly Color SelectedInactiveTextColor = Color.FromArgb(30, 64, 175);

        private TreeNode? _hoveredNode;
        private TreeNode? _lastSelectedNode;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

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
            ItemHeight = 30;
            BackColor = Color.White;
            ForeColor = TextColor;
            DoubleBuffered = true;
            SetStyle(
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.ResizeRedraw,
                true);
        }

        public void RefreshTreeVisuals()
        {
            if (IsDisposed || !IsHandleCreated)
                return;

            Invalidate();
            Update();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            SendMessage(
                Handle,
                TvmSetExtendedStyle,
                (IntPtr)TvsExDoubleBuffer,
                (IntPtr)TvsExDoubleBuffer);
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
                GetExpandGlyphHitBounds(clickedNode).Contains(e.Location);

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
            Rectangle labelBounds = GetLabelBounds(node, e.Bounds);
            Rectangle rowBounds = GetRowBounds(labelBounds);
            if (rowBounds.IsEmpty)
                return;

            Rectangle iconBounds = GetNodeIconBounds(labelBounds);
            Rectangle expandGlyphBounds = GetExpandGlyphBounds(node, labelBounds);
            Rectangle textBounds = GetTextBounds(labelBounds, rowBounds, iconBounds, expandGlyphBounds);
            Rectangle contentBounds = GetContentBounds(rowBounds, iconBounds);

            bool isSelected = (e.State & TreeNodeStates.Selected) == TreeNodeStates.Selected;
            bool isFocusedSelection = isSelected && Focused;
            bool isHovered = !isSelected && ReferenceEquals(_hoveredNode, node);

            using Region previousClip = e.Graphics.Clip.Clone();
            using SolidBrush clearBrush = new(BackColor);

            e.Graphics.SetClip(rowBounds);
            e.Graphics.FillRectangle(clearBrush, rowBounds);
            DrawNodeBackground(e.Graphics, contentBounds, isFocusedSelection, isSelected, isHovered);
            DrawNodeIcon(e.Graphics, node, iconBounds, isSelected);
            DrawExpandGlyph(e.Graphics, node, expandGlyphBounds, isFocusedSelection, isSelected);
            DrawNodeText(e.Graphics, node, textBounds, isFocusedSelection, isSelected);
            e.Graphics.SetClip(previousClip, CombineMode.Replace);
        }

        protected override void OnAfterSelect(TreeViewEventArgs e)
        {
            TreeNode? previousSelectedNode = _lastSelectedNode;
            _lastSelectedNode = e.Node;

            base.OnAfterSelect(e);

            InvalidateNode(previousSelectedNode);
            InvalidateNode(_lastSelectedNode);

            if (IsHandleCreated)
                Update();
        }

        protected override void OnAfterCollapse(TreeViewEventArgs e)
        {
            base.OnAfterCollapse(e);
            UpdateHoveredNode(null);
            RefreshTreeVisuals();
        }

        protected override void OnAfterExpand(TreeViewEventArgs e)
        {
            base.OnAfterExpand(e);
            RefreshTreeVisuals();
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            InvalidateNode(SelectedNode);

            if (IsHandleCreated)
                Update();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            InvalidateNode(SelectedNode);

            if (IsHandleCreated)
                Update();
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
            if (node == null || IsDisposed || node.TreeView != this)
                return;

            Rectangle rowBounds = GetRowBounds(node.Bounds);
            if (!rowBounds.IsEmpty)
                Invalidate(rowBounds);
        }

        private static void DrawNodeBackground(
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

        private void DrawNodeIcon(Graphics graphics, TreeNode node, Rectangle iconBounds, bool isSelected)
        {
            Image? image = ResolveNodeImage(node, isSelected);
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
                TextFormatFlags.SingleLine |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis |
                TextFormatFlags.NoPrefix |
                TextFormatFlags.PreserveGraphicsClipping);
        }

        private static void DrawExpandGlyph(
            Graphics graphics,
            TreeNode node,
            Rectangle glyphBounds,
            bool isFocusedSelection,
            bool isSelected)
        {
            if (glyphBounds.IsEmpty || node.Nodes.Count == 0)
                return;

            Color glyphColor = isFocusedSelection
                ? SelectedTextColor
                : isSelected
                    ? SelectedInactiveTextColor
                    : GlyphColor;

            PointF[] points = node.IsExpanded
                ? new[]
                {
                    new PointF(glyphBounds.Left + 1.5f, glyphBounds.Top + 2.5f),
                    new PointF(glyphBounds.Left + glyphBounds.Width / 2f, glyphBounds.Bottom - 2.0f),
                    new PointF(glyphBounds.Right - 1.5f, glyphBounds.Top + 2.5f)
                }
                : new[]
                {
                    new PointF(glyphBounds.Left + 2.5f, glyphBounds.Top + 1.5f),
                    new PointF(glyphBounds.Right - 2.0f, glyphBounds.Top + glyphBounds.Height / 2f),
                    new PointF(glyphBounds.Left + 2.5f, glyphBounds.Bottom - 1.5f)
                };

            SmoothingMode previousSmoothingMode = graphics.SmoothingMode;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using Pen pen = new(glyphColor, 1.8f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            };
            graphics.DrawLines(pen, points);

            graphics.SmoothingMode = previousSmoothingMode;
        }

        private Image? ResolveNodeImage(TreeNode node, bool isSelected)
        {
            if (ImageList == null)
                return null;

            string key = isSelected && !string.IsNullOrWhiteSpace(node.SelectedImageKey)
                ? node.SelectedImageKey
                : node.ImageKey;

            if (!string.IsNullOrWhiteSpace(key) && ImageList.Images.ContainsKey(key))
                return ImageList.Images[key];

            int imageIndex = isSelected && node.SelectedImageIndex >= 0
                ? node.SelectedImageIndex
                : node.ImageIndex;
            if (imageIndex >= 0 && imageIndex < ImageList.Images.Count)
                return ImageList.Images[imageIndex];

            return null;
        }

        private Rectangle GetNodeIconBounds(TreeNode node)
        {
            return BuildIconBounds(node.Bounds.Left, node.Bounds.Top, node.Bounds.Height);
        }

        private Rectangle GetNodeIconBounds(Rectangle labelBounds)
        {
            return BuildIconBounds(labelBounds.Left, labelBounds.Top, labelBounds.Height);
        }

        private Rectangle BuildIconBounds(int textLeft, int top, int height)
        {
            Size imageSize = ImageList?.ImageSize ?? new Size(20, 20);
            int left = Math.Max(6, textLeft - imageSize.Width - 4);
            int iconTop = top + Math.Max(0, (Math.Max(height, ItemHeight) - imageSize.Height) / 2);
            return new Rectangle(left, iconTop, imageSize.Width, imageSize.Height);
        }

        private Rectangle GetExpandGlyphBounds(TreeNode node)
        {
            Rectangle labelBounds = GetLabelBounds(node, node.Bounds);
            return GetExpandGlyphBounds(node, labelBounds);
        }

        private Rectangle GetExpandGlyphBounds(TreeNode node, Rectangle labelBounds)
        {
            if (node.Nodes.Count == 0)
                return Rectangle.Empty;

            Rectangle iconBounds = GetNodeIconBounds(labelBounds);
            int left = iconBounds.Right + ExpandGlyphGap;
            int top = labelBounds.Top + Math.Max(0, (Math.Max(labelBounds.Height, ItemHeight) - ExpandGlyphSize) / 2);
            return new Rectangle(left, top, ExpandGlyphSize, ExpandGlyphSize);
        }

        private Rectangle GetExpandGlyphHitBounds(TreeNode node)
        {
            Rectangle bounds = GetExpandGlyphBounds(node);
            if (bounds.IsEmpty)
                return Rectangle.Empty;

            bounds.Inflate(4, 4);
            return bounds;
        }

        private static Rectangle GetLabelBounds(TreeNode node, Rectangle fallbackBounds)
        {
            Rectangle labelBounds = node.Bounds;
            return labelBounds.IsEmpty ? fallbackBounds : labelBounds;
        }

        private Rectangle GetRowBounds(Rectangle bounds)
        {
            if (bounds.IsEmpty)
                return Rectangle.Empty;

            return new Rectangle(0, bounds.Top, ClientSize.Width, Math.Max(1, Math.Max(bounds.Height, ItemHeight)));
        }

        private Rectangle GetTextBounds(
            Rectangle labelBounds,
            Rectangle rowBounds,
            Rectangle iconBounds,
            Rectangle expandGlyphBounds)
        {
            int left = Math.Max(labelBounds.Left, iconBounds.Right + ContentGap);
            if (!expandGlyphBounds.IsEmpty)
                left = Math.Max(left, expandGlyphBounds.Right + ContentGap);

            return new Rectangle(
                left,
                rowBounds.Top,
                Math.Max(0, ClientSize.Width - left - 8),
                rowBounds.Height);
        }

        private static Rectangle GetContentBounds(Rectangle rowBounds, Rectangle iconBounds)
        {
            int left = Math.Max(2, iconBounds.Left - 6);
            return new Rectangle(
                left,
                rowBounds.Top + 2,
                Math.Max(0, rowBounds.Width - left - 6),
                Math.Max(0, rowBounds.Height - 4));
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
