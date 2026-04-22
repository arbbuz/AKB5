using System.Drawing.Drawing2D;

namespace AsutpKnowledgeBase.UiServices
{
    public static class KnowledgeBaseTreeNodeVisuals
    {
        private const int IconSize = 20;
        private const string WorkshopKey = "workshop";
        private const string DepartmentKey = "department";
        private const string SystemKey = "system";
        private const string PanelKey = "panel";
        private const string DeviceKey = "device";

        private static readonly Rectangle TileBounds = new(1, 1, 18, 18);
        private static readonly RectangleF VariantBadgeBounds = new(11.75f, 11.75f, 7f, 7f);

        public static ImageList CreateImageList()
        {
            var imageList = new ImageList
            {
                ColorDepth = ColorDepth.Depth32Bit,
                ImageSize = new Size(IconSize, IconSize),
                TransparentColor = Color.Transparent
            };

            AddNodeTypeIcons(imageList, WorkshopKey, CreateWorkshopIcon);
            AddNodeTypeIcons(imageList, DepartmentKey, CreateDepartmentIcon);
            AddNodeTypeIcons(imageList, SystemKey, CreateSystemIcon);
            AddNodeTypeIcons(imageList, PanelKey, CreatePanelIcon);
            AddNodeTypeIcons(imageList, DeviceKey, CreateDeviceIcon);

            return imageList;
        }

        public static string GetImageKey(int levelIndex, bool hasChildren)
            => BuildVariantKey(GetBaseImageKey(levelIndex), hasChildren);

        private static string GetBaseImageKey(int levelIndex) => levelIndex switch
        {
            <= 0 => WorkshopKey,
            1 => DepartmentKey,
            2 => SystemKey,
            3 => PanelKey,
            _ => DeviceKey
        };

        private static void AddNodeTypeIcons(
            ImageList imageList,
            string baseKey,
            Func<Bitmap> createBaseIcon)
        {
            imageList.Images.Add(BuildVariantKey(baseKey, hasChildren: true), CreateContainerVariant(createBaseIcon()));
            imageList.Images.Add(BuildVariantKey(baseKey, hasChildren: false), CreateLeafVariant(createBaseIcon()));
        }

        private static string BuildVariantKey(string baseKey, bool hasChildren)
            => $"{baseKey}-{(hasChildren ? "container" : "leaf")}";

        private static Bitmap CreateWorkshopIcon()
        {
            return CreateTileIcon(
                Color.FromArgb(217, 119, 6),
                graphics =>
                {
                    using SolidBrush brush = new(Color.White);
                    graphics.FillRectangle(brush, 4, 13, 12, 3);
                    graphics.FillRectangle(brush, 5, 9, 3, 4);
                    graphics.FillRectangle(brush, 9, 7, 3, 6);
                    graphics.FillRectangle(brush, 13, 5, 2, 8);
                });
        }

        private static Bitmap CreateDepartmentIcon()
        {
            return CreateTileIcon(
                Color.FromArgb(13, 148, 136),
                graphics =>
                {
                    using Pen pen = CreateGlyphPen(1.45f);
                    using SolidBrush brush = new(Color.White);

                    graphics.DrawRoundedRectangle(pen, new Rectangle(4, 4, 12, 11), 2);
                    graphics.DrawLine(pen, 10f, 4.75f, 10f, 14.25f);
                    graphics.DrawLine(pen, 4.75f, 9.5f, 10f, 9.5f);
                    graphics.FillRectangle(brush, 6f, 6f, 2f, 1.75f);
                    graphics.FillRectangle(brush, 11.75f, 6f, 2f, 1.75f);
                });
        }

        private static Bitmap CreateSystemIcon()
        {
            return CreateTileIcon(
                Color.FromArgb(37, 99, 235),
                graphics =>
                {
                    using Pen pen = CreateGlyphPen(1.55f);
                    using SolidBrush brush = new(Color.White);

                    graphics.DrawLine(pen, 6.5f, 6.75f, 10f, 6.75f);
                    graphics.DrawLine(pen, 10f, 6.75f, 13.5f, 10.25f);
                    graphics.DrawLine(pen, 10f, 6.75f, 10f, 13.25f);
                    FillNodeCircle(graphics, brush, 4.75f, 5f);
                    FillNodeCircle(graphics, brush, 8.25f, 5f);
                    FillNodeCircle(graphics, brush, 11.75f, 8.5f);
                    FillNodeCircle(graphics, brush, 8.25f, 12f);
                });
        }

        private static Bitmap CreatePanelIcon()
        {
            return CreateTileIcon(
                Color.FromArgb(51, 65, 85),
                graphics =>
                {
                    using Pen pen = CreateGlyphPen(1.4f);
                    using SolidBrush brush = new(Color.White);

                    graphics.DrawRoundedRectangle(pen, new Rectangle(5, 3, 10, 14), 2);
                    graphics.DrawLine(pen, 10.5f, 3.75f, 10.5f, 16.25f);
                    graphics.FillEllipse(brush, 12f, 10.25f, 1.75f, 1.75f);
                    graphics.FillEllipse(brush, 7f, 6.25f, 1.75f, 1.75f);
                    graphics.FillEllipse(brush, 7f, 9.5f, 1.75f, 1.75f);
                });
        }

        private static Bitmap CreateDeviceIcon()
        {
            return CreateTileIcon(
                Color.FromArgb(5, 150, 105),
                graphics =>
                {
                    using Pen pen = CreateGlyphPen(1.35f);
                    using SolidBrush brush = new(Color.White);

                    graphics.DrawRoundedRectangle(pen, new Rectangle(4, 5, 12, 9), 2);
                    graphics.DrawRectangle(pen, 6.25f, 7f, 4.5f, 2.75f);
                    graphics.FillEllipse(brush, 12.5f, 8f, 1.75f, 1.75f);
                    graphics.DrawLine(pen, 6.5f, 14f, 6.5f, 16.25f);
                    graphics.DrawLine(pen, 10f, 14f, 10f, 16.25f);
                    graphics.DrawLine(pen, 13.5f, 14f, 13.5f, 16.25f);
                    graphics.DrawLine(pen, 4f, 9.5f, 2.25f, 9.5f);
                    graphics.DrawLine(pen, 16f, 9.5f, 17.75f, 9.5f);
                });
        }

        private static Bitmap CreateContainerVariant(Bitmap baseIcon)
        {
            return ApplyBadge(
                baseIcon,
                VariantBadgeBounds,
                drawBadge: (graphics, badgeBounds) =>
                {
                    using SolidBrush badgeBrush = new(Color.FromArgb(15, 23, 42));
                    using Pen borderPen = new(Color.White, 0.95f);
                    Rectangle badgeRectangle = Rectangle.Round(badgeBounds);

                    graphics.FillRoundedRectangle(badgeBrush, badgeRectangle, 2);
                    graphics.DrawRoundedRectangle(borderPen, badgeRectangle, 2);
                });
        }

        private static Bitmap CreateLeafVariant(Bitmap baseIcon)
        {
            return ApplyBadge(
                baseIcon,
                VariantBadgeBounds,
                drawBadge: (graphics, badgeBounds) =>
                {
                    using SolidBrush badgeBrush = new(Color.White);
                    using SolidBrush centerBrush = new(Color.FromArgb(15, 23, 42));
                    using Pen borderPen = new(Color.FromArgb(15, 23, 42), 0.95f);

                    graphics.FillEllipse(badgeBrush, badgeBounds);
                    graphics.DrawEllipse(borderPen, badgeBounds);
                    graphics.FillEllipse(centerBrush, badgeBounds.Left + 2.3f, badgeBounds.Top + 2.3f, 2.4f, 2.4f);
                });
        }

        private static Bitmap ApplyBadge(
            Bitmap baseIcon,
            RectangleF badgeBounds,
            Action<Graphics, RectangleF> drawBadge)
        {
            using (baseIcon)
            {
                var bitmap = new Bitmap(baseIcon);
                using Graphics graphics = Graphics.FromImage(bitmap);
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using SolidBrush backdropBrush = new(Color.White);
                graphics.FillEllipse(backdropBrush, badgeBounds);
                drawBadge(graphics, badgeBounds);
                return bitmap;
            }
        }

        private static Bitmap CreateTileIcon(Color accentColor, Action<Graphics> drawGlyph)
        {
            var bitmap = new Bitmap(IconSize, IconSize);
            using Graphics graphics = Graphics.FromImage(bitmap);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.Clear(Color.Transparent);

            using GraphicsPath path = CreateRoundedRectanglePath(TileBounds, 5);
            using SolidBrush brush = new(accentColor);
            using Pen borderPen = new(Color.FromArgb(90, 15, 23, 42), 1f);
            graphics.FillPath(brush, path);
            graphics.DrawPath(borderPen, path);

            drawGlyph(graphics);
            return bitmap;
        }

        private static Pen CreateGlyphPen(float width)
        {
            return new Pen(Color.White, width)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            };
        }

        private static void FillNodeCircle(Graphics graphics, Brush brush, float left, float top)
        {
            graphics.FillEllipse(brush, left, top, 3.5f, 3.5f);
        }

        private static GraphicsPath CreateRoundedRectanglePath(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        private static void FillRoundedRectangle(this Graphics graphics, Brush brush, Rectangle bounds, int radius)
        {
            using GraphicsPath path = CreateRoundedRectanglePath(bounds, radius);
            graphics.FillPath(brush, path);
        }

        private static void DrawRoundedRectangle(this Graphics graphics, Pen pen, Rectangle bounds, int radius)
        {
            using GraphicsPath path = CreateRoundedRectanglePath(bounds, radius);
            graphics.DrawPath(pen, path);
        }
    }
}
