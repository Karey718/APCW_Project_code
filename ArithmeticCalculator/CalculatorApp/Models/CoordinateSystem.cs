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

        // 世界坐标到屏幕坐标转换
        public PointF WorldToScreen(PointF worldPoint)
        {
            return new PointF(
                Origin.X + worldPoint.X * Scale,
                Origin.Y - worldPoint.Y * Scale
            );
        }

        // 屏幕坐标到世界坐标转换
        public PointF ScreenToWorld(PointF screenPoint)
        {
            return new PointF(
                (screenPoint.X - Origin.X) / Scale,
                (Origin.Y - screenPoint.Y) / Scale
            );
        }

        // 开始拖动
        public void StartDrag(PointF screenPoint)
        {
            lastMousePosition = screenPoint;
            isDragging = true;
        }

        // 处理拖动
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

        // 缩放
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

        // 获取当前可见的世界坐标范围
        public (float minX, float maxX, float minY, float maxY) GetVisibleWorldRange()
        {
            PointF topLeft = ScreenToWorld(new PointF(0, 0));
            PointF bottomRight = ScreenToWorld(new PointF(ViewportSize.Width, ViewportSize.Height));

            return (topLeft.X, bottomRight.X, bottomRight.Y, topLeft.Y);
        }

        // 检查世界坐标点是否在可见范围内
        public bool IsWorldPointVisible(PointF worldPoint)
        {
            var range = GetVisibleWorldRange();
            return worldPoint.X >= range.minX && worldPoint.X <= range.maxX &&
                worldPoint.Y >= range.minY && worldPoint.Y <= range.maxY;
        }
    }
    
}