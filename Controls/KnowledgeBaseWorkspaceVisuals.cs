using System.Drawing.Drawing2D;

namespace AsutpKnowledgeBase
{
    internal static class KnowledgeBaseWorkspaceVisuals
    {
        public static readonly Color SurfaceColor = Color.White;
        public static readonly Color PanelColor = Color.FromArgb(251, 253, 254);
        public static readonly Color HeaderColor = Color.FromArgb(243, 246, 249);
        public static readonly Color HairlineColor = Color.FromArgb(76, 119, 138, 156);
        public static readonly Color HairlineStrongColor = Color.FromArgb(112, 119, 138, 156);
        public static readonly Color GridLineColor = Color.FromArgb(214, 220, 225);
        public static readonly Color TextColor = Color.FromArgb(28, 38, 49);
        public static readonly Color TitleColor = Color.FromArgb(51, 68, 85);
        public static readonly Color MutedTextColor = Color.FromArgb(102, 119, 137);
        public static readonly Color AccentSoftColor = Color.FromArgb(232, 246, 247);
        public static readonly Color AccentTextColor = Color.FromArgb(6, 76, 80);

        public static SectionPanel CreateSectionPanel(string title) =>
            new()
            {
                Text = title,
                Dock = DockStyle.Fill
            };

        public static BorderPanel CreateBorderPanel() =>
            new()
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(1)
            };

        public static Label CreateEmptyStateLabel(string text) =>
            new EmptyStateLabel
            {
                Dock = DockStyle.Fill,
                Text = text,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = MutedTextColor,
                Padding = new Padding(24),
                Visible = false
            };

        public static Button CreateActionButton(string text, bool primary = false) =>
            CreateButton(text, autoSize: true, primary: primary);

        public static Button CreateSquareActionButton(string text, bool primary = false) =>
            CreateButton(text, autoSize: false, primary: primary);

        public static void ConfigureListView(ListView listView)
        {
            listView.BackColor = SurfaceColor;
            listView.BorderStyle = BorderStyle.None;
            listView.ForeColor = TextColor;
            listView.GridLines = false;
            listView.HideSelection = false;
        }

        public static void ConfigureGrid(DataGridView grid)
        {
            grid.BackgroundColor = SurfaceColor;
            grid.BorderStyle = BorderStyle.None;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.Single;
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            grid.EnableHeadersVisualStyles = false;
            grid.GridColor = GridLineColor;
            grid.DefaultCellStyle.BackColor = SurfaceColor;
            grid.DefaultCellStyle.ForeColor = TextColor;
            grid.DefaultCellStyle.SelectionBackColor = AccentSoftColor;
            grid.DefaultCellStyle.SelectionForeColor = AccentTextColor;
            grid.ColumnHeadersDefaultCellStyle.BackColor = HeaderColor;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(65, 84, 102);
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = HeaderColor;
            grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.FromArgb(65, 84, 102);
        }

        public static void ApplyCaptionLabel(Label label)
        {
            label.ForeColor = MutedTextColor;
        }

        private static Button CreateButton(string text, bool autoSize, bool primary)
        {
            var button = new ActionButton
            {
                Text = text,
                AutoSize = autoSize,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Primary = primary,
                UseVisualStyleBackColor = false,
                Margin = new Padding(0, 0, 8, 8),
                Padding = autoSize ? new Padding(8, 2, 8, 2) : new Padding(0),
                MinimumSize = autoSize ? new Size(0, 28) : new Size(32, 28),
                Size = autoSize ? Size.Empty : new Size(32, 28)
            };
            return button;
        }

        private static GraphicsPath CreateRoundedRectanglePath(Rectangle rectangle, int radius)
        {
            var path = new GraphicsPath();
            int diameter = radius * 2;
            var arc = new Rectangle(rectangle.Location, new Size(diameter, diameter));

            path.AddArc(arc, 180, 90);
            arc.X = rectangle.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = rectangle.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = rectangle.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }

        private sealed class ActionButton : Button
        {
            private bool _hovered;
            private bool _pressed;

            public ActionButton()
            {
                SetStyle(
                    ControlStyles.UserPaint |
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.ResizeRedraw,
                    true);
                FlatStyle = FlatStyle.Flat;
                FlatAppearance.BorderSize = 0;
                BackColor = SurfaceColor;
                ForeColor = Color.FromArgb(37, 55, 71);
            }

            public bool Primary { get; init; }

            protected override bool ShowFocusCues => false;

            protected override void OnEnabledChanged(EventArgs e)
            {
                base.OnEnabledChanged(e);
                Invalidate();
            }

            protected override void OnMouseEnter(EventArgs e)
            {
                _hovered = true;
                base.OnMouseEnter(e);
                Invalidate();
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                _hovered = false;
                _pressed = false;
                base.OnMouseLeave(e);
                Invalidate();
            }

            protected override void OnMouseDown(MouseEventArgs mevent)
            {
                if (Enabled && mevent.Button == MouseButtons.Left)
                    _pressed = true;

                base.OnMouseDown(mevent);
                Invalidate();
            }

            protected override void OnMouseUp(MouseEventArgs mevent)
            {
                _pressed = false;
                base.OnMouseUp(mevent);
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                var bounds = ClientRectangle;
                if (bounds.Width <= 0 || bounds.Height <= 0)
                    return;

                e.Graphics.Clear(Parent?.BackColor ?? SystemColors.Control);

                var contentBounds = bounds;
                contentBounds.Width -= 1;
                contentBounds.Height -= 1;
                if (contentBounds.Width <= 0 || contentBounds.Height <= 0)
                    return;

                using var backBrush = new SolidBrush(GetBackColor());
                e.Graphics.FillRectangle(backBrush, contentBounds);

                using var pen = new Pen(GetBorderColor(), 1F);
                e.Graphics.DrawRectangle(pen, contentBounds);

                TextRenderer.DrawText(
                    e.Graphics,
                    Text,
                    Font,
                    contentBounds,
                    GetTextColor(),
                    TextFormatFlags.HorizontalCenter |
                    TextFormatFlags.VerticalCenter |
                    TextFormatFlags.EndEllipsis |
                    TextFormatFlags.NoPrefix);
            }

            private Color GetBackColor()
            {
                if (!Enabled)
                    return Color.FromArgb(245, 247, 249);

                if (_pressed)
                    return Color.FromArgb(221, 238, 239);

                if (_hovered)
                    return Primary
                        ? Color.FromArgb(218, 240, 241)
                        : Color.FromArgb(239, 246, 250);

                return Primary ? AccentSoftColor : SurfaceColor;
            }

            private Color GetBorderColor()
            {
                if (!Enabled)
                    return Color.FromArgb(120, 190, 202, 214);

                return Primary
                    ? Color.FromArgb(120, 15, 139, 143)
                    : HairlineStrongColor;
            }

            private Color GetTextColor()
            {
                if (!Enabled)
                    return Color.FromArgb(154, 165, 175);

                return Primary ? AccentTextColor : Color.FromArgb(37, 55, 71);
            }
        }

        internal sealed class SectionPanel : Panel
        {
            private const int BorderTop = 12;
            private const int TitleTop = -1;
            private const int TitleHeight = 18;

            public SectionPanel()
            {
                DoubleBuffered = true;
                BackColor = PanelColor;
                Padding = new Padding(10, 20, 10, 10);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);

                var bounds = ClientRectangle;
                bounds.X += 1;
                bounds.Y += BorderTop;
                bounds.Width -= 2;
                bounds.Height -= BorderTop + 1;
                if (bounds.Width <= 0 || bounds.Height <= 0)
                    return;

                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = CreateRoundedRectanglePath(bounds, 6);
                using var pen = new Pen(HairlineColor, 0.25F);
                e.Graphics.DrawPath(pen, path);

                if (string.IsNullOrWhiteSpace(Text))
                    return;

                var titleSize = TextRenderer.MeasureText(Text, Font);
                var titleBounds = new Rectangle(14, TitleTop, titleSize.Width + 8, TitleHeight);
                using var titleBrush = new SolidBrush(PanelColor);
                e.Graphics.FillRectangle(titleBrush, titleBounds);
                TextRenderer.DrawText(
                    e.Graphics,
                    Text,
                    Font,
                    new Point(18, TitleTop),
                    TitleColor,
                    TextFormatFlags.NoPrefix);
            }
        }

        internal sealed class BorderPanel : Panel
        {
            public BorderPanel()
            {
                DoubleBuffered = true;
                BackColor = SurfaceColor;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);

                var bounds = ClientRectangle;
                bounds.Width -= 1;
                bounds.Height -= 1;
                if (bounds.Width <= 0 || bounds.Height <= 0)
                    return;

                using var pen = new Pen(HairlineColor, 0.25F);
                e.Graphics.DrawRectangle(pen, bounds);
            }
        }

        private sealed class EmptyStateLabel : Label
        {
            public EmptyStateLabel()
            {
                DoubleBuffered = true;
                BackColor = Color.FromArgb(250, 252, 253);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);

                var bounds = ClientRectangle;
                bounds.Width -= 1;
                bounds.Height -= 1;
                if (bounds.Width <= 0 || bounds.Height <= 0)
                    return;

                using var pen = new Pen(HairlineStrongColor, 1F)
                {
                    DashStyle = DashStyle.Dash
                };
                e.Graphics.DrawRectangle(pen, bounds);
            }
        }
    }
}
