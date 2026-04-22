using System.Drawing.Drawing2D;

namespace AsutpKnowledgeBase.UiServices
{
    public static class KnowledgeBaseTreeNodeVisuals
    {
        private const string WorkshopKey = "workshop";
        private const string DepartmentKey = "department";
        private const string EquipmentKey = "equipment";
        private const string CabinetKey = "cabinet";
        private const string DeviceKey = "device";
        private const string ModuleKey = "module";
        private const string NoteKey = "note";

        public static ImageList CreateImageList()
        {
            var imageList = new ImageList
            {
                ColorDepth = ColorDepth.Depth32Bit,
                ImageSize = new Size(16, 16),
                TransparentColor = Color.Transparent
            };

            imageList.Images.Add(WorkshopKey, CreateWorkshopIcon());
            imageList.Images.Add(DepartmentKey, CreateDepartmentIcon());
            imageList.Images.Add(EquipmentKey, CreateEquipmentIcon());
            imageList.Images.Add(CabinetKey, CreateCabinetIcon());
            imageList.Images.Add(DeviceKey, CreateDeviceIcon());
            imageList.Images.Add(ModuleKey, CreateModuleIcon());
            imageList.Images.Add(NoteKey, CreateNoteIcon());

            return imageList;
        }

        public static string GetImageKey(int levelIndex) => levelIndex switch
        {
            <= 0 => WorkshopKey,
            1 => DepartmentKey,
            2 => EquipmentKey,
            3 => CabinetKey,
            4 => DeviceKey,
            5 => ModuleKey,
            _ => NoteKey
        };

        private static Bitmap CreateWorkshopIcon()
        {
            return CreateTileIcon(
                Color.FromArgb(217, 119, 6),
                graphics =>
                {
                    using SolidBrush brush = new(Color.White);
                    graphics.FillRectangle(brush, 3, 9, 10, 4);
                    graphics.FillRectangle(brush, 5, 6, 2, 3);
                    graphics.FillRectangle(brush, 9, 6, 2, 3);
                    graphics.FillRectangle(brush, 7, 3, 2, 10);
                });
        }

        private static Bitmap CreateDepartmentIcon()
        {
            return CreateTileIcon(
                Color.FromArgb(8, 145, 178),
                graphics =>
                {
                    using SolidBrush brush = new(Color.White);
                    graphics.FillRoundedRectangle(brush, new Rectangle(3, 3, 10, 3), 2);
                    graphics.FillRoundedRectangle(brush, new Rectangle(4, 7, 8, 3), 2);
                    graphics.FillRoundedRectangle(brush, new Rectangle(5, 11, 6, 2), 1);
                });
        }

        private static Bitmap CreateEquipmentIcon()
        {
            return CreateTileIcon(
                Color.FromArgb(5, 150, 105),
                graphics =>
                {
                    using Pen pen = new(Color.White, 1.6f)
                    {
                        StartCap = LineCap.Round,
                        EndCap = LineCap.Round
                    };

                    graphics.DrawEllipse(pen, 4.5f, 4.5f, 7f, 7f);
                    graphics.DrawLine(pen, 8f, 2.5f, 8f, 4.25f);
                    graphics.DrawLine(pen, 8f, 11.75f, 8f, 13.5f);
                    graphics.DrawLine(pen, 2.5f, 8f, 4.25f, 8f);
                    graphics.DrawLine(pen, 11.75f, 8f, 13.5f, 8f);
                });
        }

        private static Bitmap CreateCabinetIcon()
        {
            return CreateTileIcon(
                Color.FromArgb(30, 64, 175),
                graphics =>
                {
                    using Pen pen = new(Color.White, 1.4f);
                    using SolidBrush brush = new(Color.White);
                    graphics.DrawRoundedRectangle(pen, new Rectangle(4, 2, 8, 12), 2);
                    graphics.DrawLine(pen, 8, 2.5f, 8, 13.5f);
                    graphics.FillEllipse(brush, 9.25f, 7f, 1.5f, 1.5f);
                });
        }

        private static Bitmap CreateDeviceIcon()
        {
            return CreateTileIcon(
                Color.FromArgb(59, 130, 246),
                graphics =>
                {
                    using Pen pen = new(Color.White, 1.2f);
                    using SolidBrush brush = new(Color.White);
                    graphics.DrawRoundedRectangle(pen, new Rectangle(4, 4, 8, 8), 2);
                    graphics.FillRectangle(brush, 6, 6, 1.5f, 1.5f);
                    graphics.FillRectangle(brush, 8.25f, 6, 1.5f, 1.5f);
                    graphics.FillRectangle(brush, 6, 8.25f, 1.5f, 1.5f);
                    graphics.FillRectangle(brush, 8.25f, 8.25f, 1.5f, 1.5f);
                    graphics.DrawLine(pen, 2.5f, 6f, 4f, 6f);
                    graphics.DrawLine(pen, 2.5f, 10f, 4f, 10f);
                    graphics.DrawLine(pen, 12f, 6f, 13.5f, 6f);
                    graphics.DrawLine(pen, 12f, 10f, 13.5f, 10f);
                });
        }

        private static Bitmap CreateModuleIcon()
        {
            return CreateTileIcon(
                Color.FromArgb(71, 85, 105),
                graphics =>
                {
                    using SolidBrush brush = new(Color.White);
                    graphics.FillRoundedRectangle(brush, new Rectangle(3, 4, 10, 8), 2);
                    using SolidBrush accentBrush = new(Color.FromArgb(71, 85, 105));
                    graphics.FillRectangle(accentBrush, 5, 6, 1.5f, 4);
                    graphics.FillRectangle(accentBrush, 7.25f, 6, 1.5f, 4);
                    graphics.FillRectangle(accentBrush, 9.5f, 6, 1.5f, 4);
                });
        }

        private static Bitmap CreateNoteIcon()
        {
            return CreateTileIcon(
                Color.FromArgb(100, 116, 139),
                graphics =>
                {
                    using SolidBrush brush = new(Color.White);
                    PointF[] foldedCorner =
                    {
                        new(10.5f, 3f),
                        new(13f, 5.5f),
                        new(10.5f, 5.5f)
                    };

                    graphics.FillRectangle(brush, 4, 3, 7, 10);
                    graphics.FillPolygon(brush, foldedCorner);
                    using Pen pen = new(Color.FromArgb(100, 116, 139), 1.1f);
                    graphics.DrawLine(pen, 5.5f, 7f, 10.5f, 7f);
                    graphics.DrawLine(pen, 5.5f, 9.5f, 10.5f, 9.5f);
                });
        }

        private static Bitmap CreateTileIcon(Color accentColor, Action<Graphics> drawGlyph)
        {
            var bitmap = new Bitmap(16, 16);
            using Graphics graphics = Graphics.FromImage(bitmap);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.Clear(Color.Transparent);

            using GraphicsPath path = CreateRoundedRectanglePath(new Rectangle(1, 1, 14, 14), 4);
            using SolidBrush brush = new(accentColor);
            graphics.FillPath(brush, path);

            drawGlyph(graphics);
            return bitmap;
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
