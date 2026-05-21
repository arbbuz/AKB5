using System.Drawing.Drawing2D;
using System.Globalization;
using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.UiServices
{
    public static class KnowledgeBaseTreeNodeVisuals
    {
        private const int IconSize = 30;
        private const int ExpandGlyphSize = 12;
        private const float MaterialSymbolViewBoxSize = 960F;
        private const string WorkshopKey = "workshop";
        private const string DepartmentKey = "department";
        private const string SystemKey = "system";
        private const string PanelKey = "panel";
        private const string DeviceKey = "device";

        // Google Material Symbols SVG paths, Apache-2.0:
        // https://github.com/google/material-design-icons/tree/master/symbols/web
        private const string DepartmentIconPath = "M80-180v-600q0-24.75 17.63-42.38Q115.25-840 140-840h270q24.75 0 42.38 17.62Q470-804.75 470-780v105h350q24.75 0 42.38 17.62Q880-639.75 880-615v435q0 24.75-17.62 42.37Q844.75-120 820-120H140q-24.75 0-42.37-17.63Q80-155.25 80-180Zm60 0h105v-105H140v105Zm0-165h105v-105H140v105Zm0-165h105v-105H140v105Zm0-165h105v-105H140v105Zm165 495h105v-105H305v105Zm0-165h105v-105H305v105Zm0-165h105v-105H305v105Zm0-165h105v-105H305v105Zm165 495h350v-435H470v105h80v60h-80v105h80v60h-80v105Zm185-270v-60h60v60h-60Zm0 165v-60h60v60h-60Z";
        private const string SystemIconPath = "M213-120q-20 0-33.5-13.5T166-167q0-20 13.5-33.5T213-214h80L187-576q-32-15-50-40.5T119-684q0-47 34.5-81.5T235-800q44 0 73 23.5t39 62.5h146v-59q0-12 9-21t21-9q11 0 18.5 8.5T549-775l75-72q8-8 20.5-10.5T670-854l158 76q9 5 12.5 14t-1.5 19q-5 10-14.5 12t-18.5-3l-155-75-98 99v52l98 103 155-76q10-5 19-2.5t14 12.5q5 10 1.5 20T827-588l-153 72q-14 7-27 6.5T624-520l-75-72q0 14-7.5 21t-18.5 7q-12 0-21-9t-9-21v-60H345q0 12-6.5 24.5T323-609l205 395h111q20 0 33.5 13.5T686-167q0 20-13.5 33.5T639-120H213Zm22-508q24 0 40-16t16-40q0-24-16-40t-40-16q-24 0-40 16t-16 40q0 24 16 40t40 16Zm124 414h102L272-581q-3 2-10 4t-11 3l108 360Zm102 0Z";
        private const string PanelIconPath = "M286.88-717q-20.88 0-35.38 14.62-14.5 14.62-14.5 35.5 0 20.88 14.62 35.38 14.62 14.5 35.5 14.5 20.88 0 35.38-14.62 14.5-14.62 14.5-35.5 0-20.88-14.62-35.38-14.62-14.5-35.5-14.5Zm0 414q-20.88 0-35.38 14.62-14.5 14.62-14.5 35.5 0 20.88 14.62 35.38 14.62 14.5 35.5 14.5 20.88 0 35.38-14.62 14.5-14.62 14.5-35.5 0-20.88-14.62-35.38-14.62-14.5-35.5-14.5ZM154-839h651q16 0 25.5 9.5t9.5 25.81V-535q0 17.42-9.5 29.21T805-494H154q-15 0-24.5-11.79T120-535v-268.69q0-16.31 9.5-25.81T154-839Zm26 60v225h600v-225H180Zm-26 353h647q15 0 27 12.5t12 28.53V-121q0 20-12 30.5T801-80H159q-16 0-27.5-10.5T120-121v-263.97q0-16.03 9.5-28.53T154-426Zm26 60v226h600v-226H180Zm0-413v225-225Zm0 413v226-226Z";

        private static readonly Rectangle TileBounds = new(1, 1, IconSize - 2, IconSize - 2);
        private static readonly RectangleF MaterialSymbolBounds = new(4.25f, 4.25f, 21.5f, 21.5f);
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

        public static string GetImageKey(KbNode node, int hierarchyLevel, bool hasChildren) =>
            BuildVariantKey(GetBaseImageKey(GetIconNodeType(node, hierarchyLevel)), hasChildren);

        public static string GetImageKey(KbNodeType nodeType, bool hasChildren)
            => BuildVariantKey(GetBaseImageKey(nodeType), hasChildren);

        public static Bitmap CreateNodeIcon(KbNodeType nodeType, int hierarchyLevel, bool hasChildren)
        {
            var baseKey = GetBaseImageKey(GetIconNodeType(nodeType, hierarchyLevel));
            return baseKey switch
            {
                WorkshopKey => hasChildren ? CreateContainerVariant(CreateWorkshopIcon()) : CreateLeafVariant(CreateWorkshopIcon()),
                DepartmentKey => hasChildren ? CreateContainerVariant(CreateDepartmentIcon()) : CreateLeafVariant(CreateDepartmentIcon()),
                SystemKey => hasChildren ? CreateContainerVariant(CreateSystemIcon()) : CreateLeafVariant(CreateSystemIcon()),
                PanelKey => hasChildren ? CreateContainerVariant(CreatePanelIcon()) : CreateLeafVariant(CreatePanelIcon()),
                _ => hasChildren ? CreateContainerVariant(CreateDeviceIcon()) : CreateLeafVariant(CreateDeviceIcon())
            };
        }

        private static KbNodeType GetIconNodeType(KbNode node, int hierarchyLevel)
        {
            if (node.NodeType == KbNodeType.WorkshopRoot)
                return KbNodeType.WorkshopRoot;

            return GetIconNodeType(node.NodeType, hierarchyLevel);
        }

        private static KbNodeType GetIconNodeType(KbNodeType nodeType, int hierarchyLevel)
        {
            if (nodeType == KbNodeType.WorkshopRoot)
                return KbNodeType.WorkshopRoot;

            return hierarchyLevel switch
            {
                0 => KbNodeType.Department,
                1 => KbNodeType.System,
                2 => KbNodeType.Cabinet,
                _ => nodeType
            };
        }

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
            return CreateScaledLegacyTileIcon(
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
            return CreateMaterialSymbolIcon(Color.FromArgb(13, 148, 136), DepartmentIconPath);
        }

        private static Bitmap CreateSystemIcon()
        {
            return CreateMaterialSymbolIcon(Color.FromArgb(37, 99, 235), SystemIconPath);
        }

        private static Bitmap CreatePanelIcon()
        {
            return CreateMaterialSymbolIcon(Color.FromArgb(51, 65, 85), PanelIconPath);
        }

        private static Bitmap CreateDeviceIcon()
        {
            return CreateScaledLegacyTileIcon(
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

        private static Bitmap CreateMaterialSymbolIcon(Color accentColor, string svgPathData)
        {
            return CreateTileIcon(
                accentColor,
                graphics =>
                {
                    using GraphicsPath glyphPath = SvgPathParser.Parse(svgPathData);
                    float scale = MaterialSymbolBounds.Width / MaterialSymbolViewBoxSize;
                    using Matrix matrix = new(
                        scale,
                        0F,
                        0F,
                        scale,
                        MaterialSymbolBounds.Left,
                        MaterialSymbolBounds.Top + MaterialSymbolBounds.Height);
                    glyphPath.Transform(matrix);

                    using SolidBrush brush = new(Color.White);
                    graphics.FillPath(brush, glyphPath);
                });
        }

        private static Bitmap CreateScaledLegacyTileIcon(Color accentColor, Action<Graphics> drawGlyph)
        {
            return CreateTileIcon(
                accentColor,
                graphics =>
                {
                    GraphicsState state = graphics.Save();
                    graphics.ScaleTransform(1.5f, 1.5f);
                    drawGlyph(graphics);
                    graphics.Restore(state);
                });
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

        private sealed class SvgPathParser
        {
            private readonly string _data;
            private int _index;
            private char _command;
            private PointF _currentPoint;
            private PointF _figureStartPoint;
            private PointF _lastQuadraticControlPoint;
            private bool _hasLastQuadraticControlPoint;

            private SvgPathParser(string data)
            {
                _data = data;
            }

            public static GraphicsPath Parse(string data)
            {
                var parser = new SvgPathParser(data);
                return parser.ParsePath();
            }

            private GraphicsPath ParsePath()
            {
                var path = new GraphicsPath(FillMode.Winding);
                while (MoveNextCommandIfPresent())
                {
                    switch (_command)
                    {
                        case 'M':
                        case 'm':
                            ParseMove(path, char.IsLower(_command));
                            break;
                        case 'L':
                        case 'l':
                            ParseLines(path, char.IsLower(_command));
                            break;
                        case 'H':
                        case 'h':
                            ParseHorizontalLines(path, char.IsLower(_command));
                            break;
                        case 'V':
                        case 'v':
                            ParseVerticalLines(path, char.IsLower(_command));
                            break;
                        case 'Q':
                        case 'q':
                            ParseQuadraticCurves(path, char.IsLower(_command));
                            break;
                        case 'T':
                        case 't':
                            ParseSmoothQuadraticCurves(path, char.IsLower(_command));
                            break;
                        case 'Z':
                        case 'z':
                            path.CloseFigure();
                            _currentPoint = _figureStartPoint;
                            ResetCurveMemory();
                            break;
                        default:
                            throw new NotSupportedException($"SVG path command '{_command}' is not supported.");
                    }
                }

                return path;
            }

            private bool MoveNextCommandIfPresent()
            {
                SkipSeparators();
                if (_index >= _data.Length)
                    return false;

                if (IsCommand(_data[_index]))
                {
                    _command = _data[_index++];
                    return true;
                }

                if (_command == default)
                    throw new FormatException("SVG path data started without a command.");

                return true;
            }

            private void ParseMove(GraphicsPath path, bool isRelative)
            {
                bool isFirstPoint = true;
                while (HasNumber())
                {
                    var point = ReadPoint(isRelative);
                    if (isFirstPoint)
                    {
                        path.StartFigure();
                        _currentPoint = point;
                        _figureStartPoint = point;
                        isFirstPoint = false;
                    }
                    else
                    {
                        AddLine(path, point);
                    }
                }

                _command = isRelative ? 'l' : 'L';
                ResetCurveMemory();
            }

            private void ParseLines(GraphicsPath path, bool isRelative)
            {
                while (HasNumber())
                    AddLine(path, ReadPoint(isRelative));

                ResetCurveMemory();
            }

            private void ParseHorizontalLines(GraphicsPath path, bool isRelative)
            {
                while (HasNumber())
                {
                    float x = ReadNumber();
                    if (isRelative)
                        x += _currentPoint.X;

                    AddLine(path, new PointF(x, _currentPoint.Y));
                }

                ResetCurveMemory();
            }

            private void ParseVerticalLines(GraphicsPath path, bool isRelative)
            {
                while (HasNumber())
                {
                    float y = ReadNumber();
                    if (isRelative)
                        y += _currentPoint.Y;

                    AddLine(path, new PointF(_currentPoint.X, y));
                }

                ResetCurveMemory();
            }

            private void ParseQuadraticCurves(GraphicsPath path, bool isRelative)
            {
                while (HasNumber())
                {
                    PointF controlPoint = ReadPoint(isRelative);
                    PointF endPoint = ReadPoint(isRelative);
                    AddQuadraticCurve(path, controlPoint, endPoint);
                }
            }

            private void ParseSmoothQuadraticCurves(GraphicsPath path, bool isRelative)
            {
                while (HasNumber())
                {
                    PointF controlPoint = _hasLastQuadraticControlPoint
                        ? new PointF(
                            (2F * _currentPoint.X) - _lastQuadraticControlPoint.X,
                            (2F * _currentPoint.Y) - _lastQuadraticControlPoint.Y)
                        : _currentPoint;
                    PointF endPoint = ReadPoint(isRelative);
                    AddQuadraticCurve(path, controlPoint, endPoint);
                }
            }

            private void AddLine(GraphicsPath path, PointF endPoint)
            {
                path.AddLine(_currentPoint, endPoint);
                _currentPoint = endPoint;
            }

            private void AddQuadraticCurve(GraphicsPath path, PointF controlPoint, PointF endPoint)
            {
                PointF firstControlPoint = new(
                    _currentPoint.X + ((2F / 3F) * (controlPoint.X - _currentPoint.X)),
                    _currentPoint.Y + ((2F / 3F) * (controlPoint.Y - _currentPoint.Y)));
                PointF secondControlPoint = new(
                    endPoint.X + ((2F / 3F) * (controlPoint.X - endPoint.X)),
                    endPoint.Y + ((2F / 3F) * (controlPoint.Y - endPoint.Y)));
                path.AddBezier(_currentPoint, firstControlPoint, secondControlPoint, endPoint);
                _currentPoint = endPoint;
                _lastQuadraticControlPoint = controlPoint;
                _hasLastQuadraticControlPoint = true;
            }

            private PointF ReadPoint(bool isRelative)
            {
                float x = ReadNumber();
                float y = ReadNumber();
                if (isRelative)
                {
                    x += _currentPoint.X;
                    y += _currentPoint.Y;
                }

                return new PointF(x, y);
            }

            private bool HasNumber()
            {
                SkipSeparators();
                return _index < _data.Length &&
                    (char.IsDigit(_data[_index]) || _data[_index] == '-' || _data[_index] == '+' || _data[_index] == '.');
            }

            private float ReadNumber()
            {
                SkipSeparators();
                int start = _index;

                if (_index < _data.Length && (_data[_index] == '-' || _data[_index] == '+'))
                    _index++;

                while (_index < _data.Length && char.IsDigit(_data[_index]))
                    _index++;

                if (_index < _data.Length && _data[_index] == '.')
                {
                    _index++;
                    while (_index < _data.Length && char.IsDigit(_data[_index]))
                        _index++;
                }

                if (_index < _data.Length && (_data[_index] == 'e' || _data[_index] == 'E'))
                {
                    _index++;
                    if (_index < _data.Length && (_data[_index] == '-' || _data[_index] == '+'))
                        _index++;

                    while (_index < _data.Length && char.IsDigit(_data[_index]))
                        _index++;
                }

                return float.Parse(_data[start.._index], CultureInfo.InvariantCulture);
            }

            private void SkipSeparators()
            {
                while (_index < _data.Length && (char.IsWhiteSpace(_data[_index]) || _data[_index] == ','))
                    _index++;
            }

            private void ResetCurveMemory()
            {
                _hasLastQuadraticControlPoint = false;
            }

            private static bool IsCommand(char value) =>
                (value >= 'A' && value <= 'Z') || (value >= 'a' && value <= 'z');
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
