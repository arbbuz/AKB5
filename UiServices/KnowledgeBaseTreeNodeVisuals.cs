using System.Drawing.Drawing2D;
using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.UiServices
{
    public static class KnowledgeBaseTreeNodeVisuals
    {
        private const int IconSize = 20;
        private const int ExpandGlyphSize = 12;
        private const string WorkshopKey = "workshop";
        private const string DepartmentKey = "department";
        private const string SystemKey = "system";
        private const string PanelKey = "panel";
        private const string DeviceKey = "device";

        private static readonly Rectangle TileBounds = new(1, 1, 18, 18);
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

        public static ImageList CreateExpandStateImageList()
        {
            var imageList = new ImageList
            {
                ColorDepth = ColorDepth.Depth32Bit,
                ImageSize = new Size(ExpandGlyphSize, ExpandGlyphSize),
                TransparentColor = Color.Transparent
            };

            imageList.Images.Add(CreateEmptyExpandStateGlyph());
            imageList.Images.Add(CreateExpandStateGlyph(expanded: false));
            imageList.Images.Add(CreateExpandStateGlyph(expanded: true));
            return imageList;
        }

        public static int GetExpandStateImageIndex(bool hasChildren, bool isExpanded) =>
            hasChildren
                ? isExpanded ? 2 : 1
                : 0;

        public static string GetImageKey(KbNodeType nodeType, bool hasChildren)
            => BuildVariantKey(GetBaseImageKey(nodeType), hasChildren);

        private static string GetBaseImageKey(KbNodeType nodeType) => nodeType switch
        {
            KbNodeType.WorkshopRoot => WorkshopKey,
            KbNodeType.Department => DepartmentKey,
            KbNodeType.System => SystemKey,
            KbNodeType.Cabinet => PanelKey,
            KbNodeType.Controller => DeviceKey,
            KbNodeType.Module => DeviceKey,
            KbNodeType.DocumentNode => SystemKey,
            KbNodeType.Unknown => DeviceKey,
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

                    graphics.DrawLine(pen, 4.25f, 14.25f, 15.75f, 14.25f);
                    graphics.DrawLine(pen, 4.25f, 14.25f, 4.25f, 11.5f);
                    graphics.DrawLine(pen, 4.25f, 11.5f, 6.6f, 8.8f);
                    graphics.DrawLine(pen, 6.6f, 8.8f, 8.7f, 11f);
                    graphics.DrawLine(pen, 8.7f, 11f, 10.8f, 8f);
                    graphics.DrawLine(pen, 10.8f, 8f, 12.9f, 10.2f);
                    graphics.DrawLine(pen, 12.9f, 10.2f, 15.75f, 7.2f);
                    graphics.DrawLine(pen, 15.75f, 7.2f, 15.75f, 14.25f);

                    graphics.DrawLine(pen, 5.75f, 6.25f, 5.75f, 10.8f);
                    graphics.DrawLine(pen, 5f, 6.25f, 6.5f, 6.25f);

                    graphics.FillRectangle(brush, 6.2f, 12f, 1.45f, 1.25f);
                    graphics.FillRectangle(brush, 8.65f, 12f, 1.45f, 1.25f);
                    graphics.FillRectangle(brush, 11.1f, 12f, 1.45f, 1.25f);
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
            return CloneIcon(baseIcon);
        }

        private static Bitmap CreateLeafVariant(Bitmap baseIcon)
        {
            return CloneIcon(baseIcon);
        }

        private static Bitmap CloneIcon(Bitmap baseIcon)
        {
            using (baseIcon)
            {
                return new Bitmap(baseIcon);
            }
        }

        private static Bitmap CreateExpandStateGlyph(bool expanded)
        {
            var bitmap = new Bitmap(ExpandGlyphSize, ExpandGlyphSize);
            using Graphics graphics = Graphics.FromImage(bitmap);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.Clear(Color.Transparent);

            using Pen pen = new(Color.FromArgb(71, 85, 105), 1.8f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            };

            PointF[] points = expanded
                ? new[]
                {
                    new PointF(2.25f, 4f),
                    new PointF(6f, 7.5f),
                    new PointF(9.75f, 4f)
                }
                : new[]
                {
                    new PointF(4f, 2.25f),
                    new PointF(7.5f, 6f),
                    new PointF(4f, 9.75f)
                };

            graphics.DrawLines(pen, points);
            return bitmap;
        }

        private static Bitmap CreateEmptyExpandStateGlyph()
        {
            var bitmap = new Bitmap(ExpandGlyphSize, ExpandGlyphSize);
            using Graphics graphics = Graphics.FromImage(bitmap);
            graphics.Clear(Color.Transparent);
            return bitmap;
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
