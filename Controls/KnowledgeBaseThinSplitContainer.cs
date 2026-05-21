namespace AsutpKnowledgeBase
{
    public sealed class KnowledgeBaseThinSplitContainer : SplitContainer
    {
        public KnowledgeBaseThinSplitContainer()
        {
            BorderStyle = BorderStyle.None;
            SplitterWidth = 6;
            BackColor = Color.White;
            DoubleBuffered = true;
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);
        }

        public Color SplitterFillColor { get; set; } = Color.White;

        public Color SplitterLineColor { get; set; } = Color.FromArgb(198, 205, 214);

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Rectangle splitterBounds = SplitterRectangle;
            if (splitterBounds.Width <= 0 || splitterBounds.Height <= 0)
                return;

            using var fillBrush = new SolidBrush(SplitterFillColor);
            e.Graphics.FillRectangle(fillBrush, splitterBounds);

            using var linePen = new Pen(SplitterLineColor);
            if (Orientation == Orientation.Vertical)
            {
                int x = splitterBounds.Left + splitterBounds.Width / 2;
                e.Graphics.DrawLine(linePen, x, splitterBounds.Top, x, splitterBounds.Bottom - 1);
            }
            else
            {
                int y = splitterBounds.Top + splitterBounds.Height / 2;
                e.Graphics.DrawLine(linePen, splitterBounds.Left, y, splitterBounds.Right - 1, y);
            }
        }
    }
}
