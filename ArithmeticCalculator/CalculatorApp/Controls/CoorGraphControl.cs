using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using CalculatorAPP.Models;
using CalculatorAPP.Forms;

namespace CalculatorAPP.Controls
{
    public class CoorGraphControl : Control
    {
        private CoordinateSystem coordinateSystem;
        private List<FunctionPlot> functions = new List<FunctionPlot>();
        private Point? lastMousePosition;

        private CoorMainForm parentForm;

        public CoorGraphControl()
        {
            DoubleBuffered = true;
            coordinateSystem = new CoordinateSystem(Size);

            // 设置样式
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.UserPaint |
                    ControlStyles.DoubleBuffer |
                    ControlStyles.ResizeRedraw, true);
        }

        public void SetMainForm(CoorMainForm form)
        {
            parentForm = form;
        }

        // 添加调试信息
        private void AddDebugInfo(string message)
        {
            parentForm.AddDebugInfo(message);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            coordinateSystem.ViewportSize = Size;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.White);

            DrawCoordinateSystem(g);
            DrawFunctions(g);
        }


        // ========================= 坐标系网格绘制 =========================


        private void DrawCoordinateSystem(Graphics g)
        {
            var cs = coordinateSystem;
            var pen = new Pen(cs.AxisColor, 1.5f);
            var gridPen = new Pen(cs.GridColor, 0.5f) { DashStyle = DashStyle.Dash };

            // 绘制网格
            if (cs.ShowGrid)
            {
                DrawGrid(g, gridPen);
            }

            // 绘制坐标轴
            DrawAxes(g, pen);

            // 绘制刻度
            DrawTicks(g, pen);
        }

        private void DrawGrid(Graphics g, Pen pen)
        {
            var cs = coordinateSystem;
            
            // 使用与刻度相同的间隔
            float gridInterval = CalculateTickInterval(cs.Scale);
            
            // 计算世界坐标范围
            PointF worldTopLeft = cs.ScreenToWorld(new PointF(0, 0));
            PointF worldBottomRight = cs.ScreenToWorld(new PointF(Width, Height));
            
            float minWorldX = worldTopLeft.X;
            float maxWorldX = worldBottomRight.X;
            float minWorldY = worldBottomRight.Y;
            float maxWorldY = worldTopLeft.Y;
            
            // 绘制垂直网格线
            float firstGridX = (float)Math.Ceiling(minWorldX / gridInterval) * gridInterval;
            float lastGridX = (float)Math.Floor(maxWorldX / gridInterval) * gridInterval;
            
            for (float worldX = firstGridX; worldX <= lastGridX; worldX += gridInterval)
            {
                PointF screenPoint = cs.WorldToScreen(new PointF(worldX, 0));
                g.DrawLine(pen, screenPoint.X, 0, screenPoint.X, Height);
            }
            
            // 绘制水平网格线
            float firstGridY = (float)Math.Ceiling(minWorldY / gridInterval) * gridInterval;
            float lastGridY = (float)Math.Floor(maxWorldY / gridInterval) * gridInterval;
            
            for (float worldY = firstGridY; worldY <= lastGridY; worldY += gridInterval)
            {
                PointF screenPoint = cs.WorldToScreen(new PointF(0, worldY));
                g.DrawLine(pen, 0, screenPoint.Y, Width, screenPoint.Y);
            }
        }

        private void DrawAxes(Graphics g, Pen pen)
        {
            var cs = coordinateSystem;
            
            // X轴
            g.DrawLine(pen, 0, cs.Origin.Y, Width, cs.Origin.Y);
            // Y轴
            g.DrawLine(pen, cs.Origin.X, 0, cs.Origin.X, Height);
        }

        private void DrawTicks(Graphics g, Pen pen)
        {
            var cs = coordinateSystem;
            float tickSize = 5;
            var font = new Font("Arial", 14);
            var brush = Brushes.Black;

            // 根据当前缩放级别计算合适的刻度间隔
            float tickInterval = CalculateTickInterval(cs.Scale);

            // 计算当前视图的世界坐标范围
            PointF worldTopLeft = cs.ScreenToWorld(new PointF(0, 0));
            PointF worldBottomRight = cs.ScreenToWorld(new PointF(Width, Height));

            float minWorldX = worldTopLeft.X;
            float maxWorldX = worldBottomRight.X;
            float minWorldY = worldBottomRight.Y; // Y坐标是反的
            float maxWorldY = worldTopLeft.Y;

            // 绘制X轴刻度
            DrawXAxisTicks(g, pen, font, brush, cs, tickInterval, minWorldX, maxWorldX, tickSize);

            // 绘制Y轴刻度
            DrawYAxisTicks(g, pen, font, brush, cs, tickInterval, minWorldY, maxWorldY, tickSize);
        }

        private float CalculateTickInterval(float scale)
        {
            float pixelsPerUnit = scale;
            
            // 理想的像素间距范围
            float minPixelSpacing = 40.0f;  // 最小40像素
            
            float idealInterval = minPixelSpacing / pixelsPerUnit;
            
            return FindOptimalTickInterval(idealInterval);
        }

        private float FindOptimalTickInterval(float idealInterval)
        {
            if (idealInterval <= 0) return 1.0f;
            
            // 计算数量级
            float magnitude = (float)Math.Pow(10, Math.Floor(Math.Log10(idealInterval)));
            float normalized = idealInterval / magnitude;
            
            // 在 1, 2, 5 序列中选择最接近的值
            float[] factors = { 1.0f, 2.0f, 5.0f };
            float bestFactor = 1.0f;
            float minDiff = float.MaxValue;
            
            foreach (float factor in factors)
            {
                float diff = Math.Abs(normalized - factor);
                if (diff < minDiff)
                {
                    minDiff = diff;
                    bestFactor = factor;
                }
            }
            
            float interval = bestFactor * magnitude;
            
            if (interval < 1e-10f) interval = 1e-10f; // 防止过小
            if (interval > 1e10f) interval = 1e10f;   // 防止过大
            
            return interval;
        }

        private void DrawXAxisTicks(Graphics g, Pen pen, Font font, Brush brush,
                           CoordinateSystem cs, float tickInterval,
                           float minWorldX, float maxWorldX, float tickSize)
        {
            // 计算第一个和最后一个刻度位置（对齐到刻度间隔）
            float firstTickX = (float)Math.Ceiling(minWorldX / tickInterval) * tickInterval;
            float lastTickX = (float)Math.Floor(maxWorldX / tickInterval) * tickInterval;

            // 限制刻度数量
            int maxTicks = 100;
            float actualTickInterval = tickInterval;
            int tickCount = (int)((lastTickX - firstTickX) / tickInterval) + 1;

            if (tickCount > maxTicks)
            {
                // 如果刻度太多，自动调整间隔
                actualTickInterval = AdjustTickIntervalForDensity(tickInterval, tickCount, maxTicks);
                firstTickX = (float)Math.Ceiling(minWorldX / actualTickInterval) * actualTickInterval;
                lastTickX = (float)Math.Floor(maxWorldX / actualTickInterval) * actualTickInterval;
            }

            for (float worldX = firstTickX; worldX <= lastTickX; worldX += actualTickInterval)
            {
                PointF screenPoint = cs.WorldToScreen(new PointF(worldX, 0));

                // 绘制刻度标签
                var label = FormatTickLabel(worldX, actualTickInterval);
                float newSize = font.Size * label.rescale;   
                Font tmpFont = new Font(font.FontFamily, newSize, font.Style);
                var size = g.MeasureString(label.format, tmpFont);

                // 绘制刻度线
                g.DrawLine(pen, screenPoint.X, cs.Origin.Y - tickSize, screenPoint.X, cs.Origin.Y + tickSize);

                // 确保标签不会超出边界
                float labelX = screenPoint.X - size.Width / 2;
                float labelY = cs.Origin.Y + tickSize + 2;

                if (labelX >= 0 && labelX + size.Width <= Width)
                {
                    g.DrawString(label.format, tmpFont, brush, labelX, labelY);
                }
                
            }
        }

        private void DrawYAxisTicks(Graphics g, Pen pen, Font font, Brush brush,
                           CoordinateSystem cs, float tickInterval,
                           float minWorldY, float maxWorldY, float tickSize)
        {
            // 计算第一个和最后一个刻度位置
            float firstTickY = (float)Math.Ceiling(minWorldY / tickInterval) * tickInterval;
            float lastTickY = (float)Math.Floor(maxWorldY / tickInterval) * tickInterval;

            // 限制刻度数量
            int maxTicks = 50;
            float actualTickInterval = tickInterval;
            int tickCount = (int)((lastTickY - firstTickY) / tickInterval) + 1;

            if (tickCount > maxTicks)
            {
                actualTickInterval = AdjustTickIntervalForDensity(tickInterval, tickCount, maxTicks);
                firstTickY = (float)Math.Ceiling(minWorldY / actualTickInterval) * actualTickInterval;
                lastTickY = (float)Math.Floor(maxWorldY / actualTickInterval) * actualTickInterval;
            }

            for (float worldY = firstTickY; worldY <= lastTickY; worldY += actualTickInterval)
            {
                // // 跳过太接近0的点，避免与原点标签重叠
                // if (Math.Abs(worldY) < actualTickInterval * 0.1f) continue;

                PointF screenPoint = cs.WorldToScreen(new PointF(0, worldY));

                // 绘制刻度标签
                var label = FormatTickLabel(worldY, actualTickInterval);
                float newSize = font.Size * label.rescale;   
                Font tmpFont = new Font(font.FontFamily, newSize, font.Style);
                var size = g.MeasureString(label.format, tmpFont);


                // 绘制刻度线
                g.DrawLine(pen, cs.Origin.X - tickSize, screenPoint.Y, cs.Origin.X + tickSize, screenPoint.Y);

                // 确保标签不会超出边界
                float labelX = cs.Origin.X - tickSize - 2 - size.Width;
                float labelY = screenPoint.Y - size.Height / 2;

                if (labelY >= 0 && labelY + size.Height <= Height)
                {
                    g.DrawString(label.format, tmpFont, brush, labelX, labelY);
                }
                
            }
        }

        private float AdjustTickIntervalForDensity(float originalInterval, int currentCount, int maxCount)
        {
            // 根据当前刻度数量调整间隔
            float factor = (float)currentCount / maxCount;

            if (factor > 4) return originalInterval * 5;
            else if (factor > 2) return originalInterval * 2;
            else return originalInterval;
        }

        private (string format, float rescale) FormatTickLabel(float value, float interval)
        {
            // 处理特殊值
            if (float.IsNaN(value) || float.IsInfinity(value)) return ("∞", 1.0f);

            float absValue = Math.Abs(value);
            float absInterval = Math.Abs(interval);

            return FormatSmartDecimal(absValue,absInterval);
        }

        private (string format, float rescale) FormatSmartDecimal(float value, float absInterval)
        {
            // 计算合适的小数位数
            int decimalPlaces = (int)Math.Max(0, -Math.Floor(Math.Log10(absInterval)) + 1);

            // 限制小数位数在合理范围内(可以解除限制)
            decimalPlaces = Math.Min(Math.Max(decimalPlaces, 0), 8);

            string format = decimalPlaces == 0 ? "0" : "0." + new string('#', decimalPlaces);

            string formattedValue = value.ToString(format, System.Globalization.CultureInfo.InvariantCulture);
            return (formattedValue, 1 - 0.05f * decimalPlaces);

        }



        // ========================= 函数绘制 =========================
        
        class AsymptoteAnalysis
        {
            public bool IsAsymptotic;
            public int ExtremeJumpCount;
        }

        // 检测函数是否存在渐近线，以优化性能
        private AsymptoteAnalysis AnalyzeFunctionBehavior(
            FunctionPlot function,
            CoordinateSystem cs,
            float worldStartX,
            float worldEndX)
        {
            int samples = 500;
            int jumpCount = 0;

            PointF? last = null;

            for (int i = 0; i <= samples; i++)
            {
                float x = worldStartX + (worldEndX - worldStartX) * i / samples;
                var p = TryGetPoint(function, cs, x);

                if (!p.HasValue)
                {
                    jumpCount++;
                    last = null;
                    continue;
                }

                if (last.HasValue)
                {
                    float dx = Math.Abs(p.Value.X - last.Value.X);
                    float dy = Math.Abs(p.Value.Y - last.Value.Y);

                    if (dy > Height * 0.8f)
                        jumpCount++;
                }

                last = p;
            }

            return new AsymptoteAnalysis
            {
                IsAsymptotic = jumpCount > samples * 0.001f,
                ExtremeJumpCount = jumpCount
            };
        }

        private readonly Dictionary<string, AsymptoteAnalysis> _asymptoteCache = new Dictionary<string, AsymptoteAnalysis>();

        private AsymptoteAnalysis GetAsymptoteAnalysis(
            FunctionPlot function,
            float worldStartX,
            float worldEndX)
        {
            string key = $"{function.Expression}:{worldStartX:F2}:{worldEndX:F2}";

            if (_asymptoteCache.TryGetValue(key, out var cached))
                return cached;

            var analysis = AnalyzeFunctionBehavior(function, coordinateSystem, worldStartX, worldEndX);
            _asymptoteCache[key] = analysis;
            return analysis;
        }

        private void DrawFunctions(Graphics g)
        {
            foreach (var function in functions)
            {
                DrawFunction(g, function);
            }
        }

        private void DrawFunction(Graphics g, FunctionPlot function)
        {
            if (function.Function == null) return;

            var cs = coordinateSystem;

            PointF worldTopLeft = cs.ScreenToWorld(new PointF(0, 0));
            PointF worldBottomRight = cs.ScreenToWorld(new PointF(Width, Height));

            float worldStartX = Math.Min(worldTopLeft.X, worldBottomRight.X);
            float worldEndX   = Math.Max(worldTopLeft.X, worldBottomRight.X);

            // ===== 渐近线函数检测 =====
            var analysis = GetAsymptoteAnalysis(function, worldStartX, worldEndX);
            bool isAsymptotic = analysis.IsAsymptotic;

            int baseSamples = Math.Max(Width, 1000);
            int samples = isAsymptotic ? baseSamples * 20 : baseSamples;

            var segments = new List<List<PointF>>();
            var currentSegment = new List<PointF>();

            PointF? lastPoint = null;

            for (int i = 0; i <= samples; i++)
            {
                float x = worldStartX + (worldEndX - worldStartX) * i / samples;
                var p = TryGetPoint(function, cs, x);

                if (!p.HasValue)
                {
                    FlushSegment(segments, currentSegment);
                    lastPoint = null;
                    continue;
                }

                if (lastPoint.HasValue)
                {
                    if (!IsContinuous(lastPoint.Value, p.Value))
                    {
                        FlushSegment(segments, currentSegment);
                    }
                }

                currentSegment.Add(p.Value);
                lastPoint = p;
            }

            FlushSegment(segments, currentSegment);

            foreach (var seg in segments)
            {
                if (seg.Count >= 2)
                {
                    using var pen = new Pen(function.Color, 2);
                    g.DrawLines(pen, seg.ToArray());
                }
            }
        }

        private PointF? TryGetPoint(
            FunctionPlot function,
            CoordinateSystem cs,
            float worldX)
        {
            try
            {
                double y = function.Function(worldX);
                if (double.IsNaN(y) || double.IsInfinity(y))
                    return null;

                PointF p = cs.WorldToScreen(new PointF(worldX, (float)y));

                // 越界直接判无效
                if (p.Y < -Height || p.Y > Height)
                    return null;

                return p;
            }
            catch
            {
                return null;
            }
        }


        private bool IsContinuous(PointF a, PointF b)
        {
            const float MaxDeltaY = 300f;
            return Math.Abs(a.Y - b.Y) <= MaxDeltaY;
        }


        private void FlushSegment(
            List<List<PointF>> segments,
            List<PointF> currentSegment)
        {
            if (currentSegment.Count > 1)
                segments.Add(new List<PointF>(currentSegment));

            currentSegment.Clear();
        }

        
        
        // ========================= 控制 =========================

        // 鼠标事件处理
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                lastMousePosition = e.Location;
                coordinateSystem.StartDrag(e.Location);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            
            if (e.Button == MouseButtons.Left && lastMousePosition.HasValue)
            {
                coordinateSystem.HandleDrag(e.Location);
                lastMousePosition = e.Location;
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left)
            {
                coordinateSystem.EndDrag();
                lastMousePosition = null;
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            float zoomFactor = e.Delta > 0 ? 1.1f : 0.9f;
            coordinateSystem.Zoom(zoomFactor, e.Location);
            Invalidate();
        }

        // 公共方法
        public void AddFunction(string expression, Color color)
        {
            var parser = new FunctionParser(this.AddDebugInfo);
            try
            {
                var function = parser.ParseFunction(expression);
                functions.Add(new FunctionPlot { Expression = expression, Function = function, Color = color });
                Invalidate();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public void ClearFunctions()
        {
            functions.Clear();
            Invalidate();
        }

        public void ResetView()
        {
            coordinateSystem = new CoordinateSystem(Size)
            {
                Scale = 50.0f,
                Origin = new PointF(Width / 2, Height / 2)
            };
            Invalidate();
        }
    }

    public class FunctionPlot
    {
        public string Expression { get; set; }
        public Func<double, double> Function { get; set; }
        public Color Color { get; set; } = Color.Red;
    }
}