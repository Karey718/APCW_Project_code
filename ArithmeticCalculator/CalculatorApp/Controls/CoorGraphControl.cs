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

            // Set Style
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.UserPaint |
                    ControlStyles.DoubleBuffer |
                    ControlStyles.ResizeRedraw, true);
        }

        public void SetMainForm(CoorMainForm form)
        {
            parentForm = form;
        }

        // Add debugging information
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


        // ========================= Coordinate System Grid Axes Ticks Drawing =========================


        private void DrawCoordinateSystem(Graphics g)
        {
            var cs = coordinateSystem;
            var pen = new Pen(cs.AxisColor, 1.5f);
            var gridPen = new Pen(cs.GridColor, 0.5f) { DashStyle = DashStyle.Dash };

            // Draw Grid
            if (cs.ShowGrid)
            {
                DrawGrid(g, gridPen);
            }

            // Draw Axes
            DrawAxes(g, pen);

            // Draw Ticks
            DrawTicks(g, pen);
        }

        private void DrawGrid(Graphics g, Pen pen)
        {
            var cs = coordinateSystem;
            
            // Using intervals identical to the scale
            float gridInterval = CalculateTickInterval(cs.Scale);
            
            // Calculate the world coordinate range
            PointF worldTopLeft = cs.ScreenToWorld(new PointF(0, 0));
            PointF worldBottomRight = cs.ScreenToWorld(new PointF(Width, Height));
            
            float minWorldX = worldTopLeft.X;
            float maxWorldX = worldBottomRight.X;
            float minWorldY = worldBottomRight.Y;
            float maxWorldY = worldTopLeft.Y;
            
            // Draw vertical grid lines
            float firstGridX = (float)Math.Ceiling(minWorldX / gridInterval) * gridInterval;
            float lastGridX = (float)Math.Floor(maxWorldX / gridInterval) * gridInterval;
            
            for (float worldX = firstGridX; worldX <= lastGridX; worldX += gridInterval)
            {
                PointF screenPoint = cs.WorldToScreen(new PointF(worldX, 0));
                g.DrawLine(pen, screenPoint.X, 0, screenPoint.X, Height);
            }
            
            // Draw horizontal grid lines
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
            
            // X Axes
            g.DrawLine(pen, 0, cs.Origin.Y, Width, cs.Origin.Y);
            // Y Axes
            g.DrawLine(pen, cs.Origin.X, 0, cs.Origin.X, Height);
        }

        private void DrawTicks(Graphics g, Pen pen)
        {
            var cs = coordinateSystem;
            float tickSize = 5;
            var font = new Font("Arial", 14);
            var brush = Brushes.Black;

            // Calculate the appropriate tick interval based on the current zoom level.
            float tickInterval = CalculateTickInterval(cs.Scale);

            // Calculate the world coordinate range of the current view
            PointF worldTopLeft = cs.ScreenToWorld(new PointF(0, 0));
            PointF worldBottomRight = cs.ScreenToWorld(new PointF(Width, Height));

            float minWorldX = worldTopLeft.X;
            float maxWorldX = worldBottomRight.X;
            float minWorldY = worldBottomRight.Y; // The Y-coordinate is inverted.
            float maxWorldY = worldTopLeft.Y;

            // Plot the X-axis scale
            DrawXAxisTicks(g, pen, font, brush, cs, tickInterval, minWorldX, maxWorldX, tickSize);

            // Plot the Y-axis scale
            DrawYAxisTicks(g, pen, font, brush, cs, tickInterval, minWorldY, maxWorldY, tickSize);
        }

        private float CalculateTickInterval(float scale)
        {
            float pixelsPerUnit = scale;
            
            // Ideal pixel pitch range
            float minPixelSpacing = 60.0f;  
            
            float idealInterval = minPixelSpacing / pixelsPerUnit;
            
            return FindOptimalTickInterval(idealInterval);
        }

        private float FindOptimalTickInterval(float idealInterval)
        {
            if (idealInterval <= 0) return 1.0f;
            
            // Calculate the order of magnitude
            float magnitude = (float)Math.Pow(10, Math.Floor(Math.Log10(idealInterval)));
            float normalized = idealInterval / magnitude;
            
            // Select the closest value from the sequence 1, 2, 5
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
            
            if (interval < 1e-10f) interval = 1e-10f; 
            if (interval > 1e10f) interval = 1e10f;  
            
            return interval;
        }

        private void DrawXAxisTicks(Graphics g, Pen pen, Font font, Brush brush,
                           CoordinateSystem cs, float tickInterval,
                           float minWorldX, float maxWorldX, float tickSize)
        {
            // Calculate the positions of the first and last graduations (aligned to the graduation interval)
            float firstTickX = (float)Math.Ceiling(minWorldX / tickInterval) * tickInterval;
            float lastTickX = (float)Math.Floor(maxWorldX / tickInterval) * tickInterval;

            // Limit the number of graduations
            int maxTicks = 100;
            float actualTickInterval = tickInterval;
            int tickCount = (int)((lastTickX - firstTickX) / tickInterval) + 1;

            if (tickCount > maxTicks)
            {
                // If there are too many graduations, the intervals will be adjusted automatically.
                actualTickInterval = AdjustTickIntervalForDensity(tickInterval, tickCount, maxTicks);
                firstTickX = (float)Math.Ceiling(minWorldX / actualTickInterval) * actualTickInterval;
                lastTickX = (float)Math.Floor(maxWorldX / actualTickInterval) * actualTickInterval;
            }

            for (float worldX = firstTickX; worldX <= lastTickX; worldX += actualTickInterval)
            {
                PointF screenPoint = cs.WorldToScreen(new PointF(worldX, 0));

                // Drawing scale labels
                var label = FormatTickLabel(worldX, actualTickInterval);
                float newSize = font.Size * label.rescale;   
                Font tmpFont = new Font(font.FontFamily, newSize, font.Style);
                var size = g.MeasureString(label.format, tmpFont);

                // Drawing scale lines
                g.DrawLine(pen, screenPoint.X, cs.Origin.Y - tickSize, screenPoint.X, cs.Origin.Y + tickSize);

                // Ensure labels do not extend beyond the boundaries.
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
            // Calculate the positions of the first and last graduations
            float firstTickY = (float)Math.Ceiling(minWorldY / tickInterval) * tickInterval;
            float lastTickY = (float)Math.Floor(maxWorldY / tickInterval) * tickInterval;

            // Limit the number of graduations
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
                // Skip points too close to zero to avoid overlapping with the origin label.
                if (Math.Abs(worldY) < actualTickInterval * 0.01f) continue;

                PointF screenPoint = cs.WorldToScreen(new PointF(0, worldY));

                // Drawing scale labels
                var label = FormatTickLabel(worldY, actualTickInterval);
                float newSize = font.Size * label.rescale;   
                Font tmpFont = new Font(font.FontFamily, newSize, font.Style);
                var size = g.MeasureString(label.format, tmpFont);


                // Drawing scale lines
                g.DrawLine(pen, cs.Origin.X - tickSize, screenPoint.Y, cs.Origin.X + tickSize, screenPoint.Y);

                // Ensure labels do not extend beyond the boundaries.
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
            // Adjust the interval according to the current number of graduations.
            float factor = (float)currentCount / maxCount;

            if (factor > 4) return originalInterval * 5;
            else if (factor > 2) return originalInterval * 2;
            else return originalInterval;
        }

        private (string format, float rescale) FormatTickLabel(float value, float interval)
        {
            // Handling special values
            if (float.IsNaN(value) || float.IsInfinity(value)) return ("∞", 1.0f);

            float absValue = Math.Abs(value);
            float absInterval = Math.Abs(interval);

            return FormatSmartDecimal(absValue,absInterval);
        }

        private (string format, float rescale) FormatSmartDecimal(float value, float absInterval)
        {
            // Calculate the appropriate number of decimal places
            int decimalPlaces = (int)Math.Max(0, -Math.Floor(Math.Log10(absInterval)) + 1);

            // Restrict the number of decimal places to a reasonable range (this restriction may be lifted)
            decimalPlaces = Math.Min(Math.Max(decimalPlaces, 0), 8);

            string format = decimalPlaces == 0 ? "0" : "0." + new string('#', decimalPlaces);

            string formattedValue = value.ToString(format, System.Globalization.CultureInfo.InvariantCulture);
            return (formattedValue, 1 - 0.05f * decimalPlaces);

        }



        // ========================= Function plotting =========================
        
        class AsymptoteAnalysis
        {
            public bool IsAsymptotic;
            public int ExtremeJumpCount;
        }

        // Detect whether a function possesses an asymptote to optimise performance
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

            // ===== Asymptotic Function Detection =====
            var analysis = GetAsymptoteAnalysis(function, worldStartX, worldEndX);
            bool isAsymptotic = analysis.IsAsymptotic;

            int baseSamples = Math.Max(Width, 1000);
            int samples = isAsymptotic ? baseSamples * 10 : baseSamples;

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
                // Invoke the F# computation module
                double y = function.Function(worldX);
                if (double.IsNaN(y) || double.IsInfinity(y))
                    return null;

                PointF p = cs.WorldToScreen(new PointF(worldX, (float)y));

                // Crossing the boundary shall be deemed invalid.
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

        
        
        // ========================= Control =========================

        // Mouse Event Handling
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

        // Public methods
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