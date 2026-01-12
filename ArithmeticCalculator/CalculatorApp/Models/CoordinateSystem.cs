using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace CalculatorAPP.Models
{
    public class CoordinateSystem
    {
        public float Scale = 50.0f;
        public PointF Origin = new PointF(0, 0);
        public Size ViewportSize;
        public Color AxisColor = Color.Black;
        public Color GridColor = Color.LightGray;
        public bool ShowGrid = true;

        private PointF lastMousePosition;
        private bool isDragging = false;

        public CoordinateSystem(Size viewportSize)
        {
            ViewportSize = viewportSize;
            Origin = new PointF(viewportSize.Width / 2, viewportSize.Height / 2);
        }

        // World Coordinate to Screen Coordinate Conversion
        public PointF WorldToScreen(PointF worldPoint)
        {
            return new PointF(
                Origin.X + worldPoint.X * Scale,
                Origin.Y - worldPoint.Y * Scale
            );
        }

        // Screen coordinate to world coordinate conversion
        public PointF ScreenToWorld(PointF screenPoint)
        {
            return new PointF(
                (screenPoint.X - Origin.X) / Scale,
                (Origin.Y - screenPoint.Y) / Scale
            );
        }

        public void StartDrag(PointF screenPoint)
        {
            lastMousePosition = screenPoint;
            isDragging = true;
        }

        public void HandleDrag(PointF currentScreenPoint)
        {
            if (!isDragging) return;

            float deltaX = currentScreenPoint.X - lastMousePosition.X;
            float deltaY = currentScreenPoint.Y - lastMousePosition.Y;

            Origin = new PointF(Origin.X + deltaX, Origin.Y + deltaY);
            lastMousePosition = currentScreenPoint;
        }

        public void EndDrag()
        {
            isDragging = false;
        }

        public void Zoom(float factor, PointF centerScreenPoint)
        {
            PointF worldCenterBefore = ScreenToWorld(centerScreenPoint);

            Scale *= factor;
            // Scale = Math.Max(0.000001f, Math.Min(Scale, 500.0f)); // 调整缩放范围

            PointF worldCenterAfter = ScreenToWorld(centerScreenPoint);

            float deltaWorldX = worldCenterAfter.X - worldCenterBefore.X;
            float deltaWorldY = worldCenterAfter.Y - worldCenterBefore.Y;

            Origin = new PointF(
                Origin.X + deltaWorldX * Scale,
                Origin.Y - deltaWorldY * Scale
            );
        }

        // Retrieve the currently visible world coordinate range
        public (float minX, float maxX, float minY, float maxY) GetVisibleWorldRange()
        {
            PointF topLeft = ScreenToWorld(new PointF(0, 0));
            PointF bottomRight = ScreenToWorld(new PointF(ViewportSize.Width, ViewportSize.Height));

            return (topLeft.X, bottomRight.X, bottomRight.Y, topLeft.Y);
        }

        // Check whether the world coordinate point is within the visible range
        public bool IsWorldPointVisible(PointF worldPoint)
        {
            var range = GetVisibleWorldRange();
            return worldPoint.X >= range.minX && worldPoint.X <= range.maxX &&
                worldPoint.Y >= range.minY && worldPoint.Y <= range.maxY;
        }
    }
    
}