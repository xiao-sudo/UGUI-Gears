using UIExt.Utility;
using UnityEngine;

namespace UIExt.Base.UIHollow
{
    // ==================== 基于 RectTransform 原始尺寸 ====================

    /// <summary>
    /// 矩形镂空形状实现
    /// 使用 RectTransform 的实际尺寸作为镂空区域
    /// </summary>
    public class RectHollowShape : IHollowShape
    {
        private RectTransform m_Target;
        private Rect m_CachedBounds;

        public RectHollowShape(RectTransform target)
        {
            m_Target = target;
            UpdateBounds();
        }

        public void UpdateTarget(RectTransform target)
        {
            m_Target = target;
        }

        public bool IsPointInside(Vector2 screenPoint, Camera eventCamera)
        {
            if (m_Target == null)
                return false;

            return RectTransformUtility.RectangleContainsScreenPoint(
                m_Target, screenPoint, eventCamera);
        }

        public void UpdateShaderProperties(Material material, Canvas rootCanvas)
        {
            if (material == null || rootCanvas == null || m_Target == null)
                return;

            // 使用现有的 UIRect 工具计算归一化坐标
            var rect = UIRect.GetNormalizedRectInScreenSpace(m_Target, rootCanvas);
            material.SetVector("_Rect", new Vector4(rect.x, rect.y, rect.width, rect.height));
        }

        public Rect GetBounds()
        {
            UpdateBounds();
            return m_CachedBounds;
        }

        private void UpdateBounds()
        {
            if (m_Target != null)
            {
                m_CachedBounds = m_Target.rect;
            }
        }
    }

    // ==================== 基于 RectTransform 中心的自定义尺寸 ====================

    /// <summary>
    /// 自定义矩形尺寸的镂空形状
    /// 使用 RectTransform 中心，但使用自定义宽高
    /// </summary>
    public class CustomRectHollowShape : IHollowShape
    {
        private RectTransform m_Target;
        private Vector2 m_CustomSize;

        public CustomRectHollowShape(RectTransform target, Vector2 customSize)
        {
            m_Target = target;
            m_CustomSize = customSize;
        }

        /// <summary>
        /// 动态更新目标
        /// </summary>
        public void UpdateTarget(RectTransform target)
        {
            m_Target = target;
        }

        /// <summary>
        /// 动态更新自定义尺寸
        /// </summary>
        public void UpdateSize(Vector2 size)
        {
            m_CustomSize = size;
        }

        /// <summary>
        /// 同时更新目标和尺寸
        /// </summary>
        public void UpdateTargetAndSize(RectTransform target, Vector2 size)
        {
            m_Target = target;
            m_CustomSize = size;
        }

        public bool IsPointInside(Vector2 screenPoint, Camera eventCamera)
        {
            if (m_Target == null)
                return false;

            // 将屏幕坐标转换为目标的本地坐标
            Vector2 localPoint;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    m_Target, screenPoint, eventCamera, out localPoint))
            {
                return false;
            }

            // 使用自定义尺寸创建的矩形（中心对齐）
            Rect customRect = new Rect(
                -m_CustomSize.x / 2f,
                -m_CustomSize.y / 2f,
                m_CustomSize.x,
                m_CustomSize.y
            );

            return customRect.Contains(localPoint);
        }

        public void UpdateShaderProperties(Material material, Canvas rootCanvas)
        {
            if (material == null || rootCanvas == null || m_Target == null)
                return;

            // 获取 RectTransform 在屏幕空间的中心位置
            Vector3[] worldCorners = new Vector3[4];
            m_Target.GetWorldCorners(worldCorners);
            Vector3 worldCenter = (worldCorners[0] + worldCorners[2]) / 2f;

            // 转换为屏幕空间
            Camera camera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : rootCanvas.worldCamera;

            Vector2 screenCenter;
            if (camera != null)
            {
                screenCenter = RectTransformUtility.WorldToScreenPoint(camera, worldCenter);
            }
            else
            {
                screenCenter = worldCenter;
            }

            // 计算自定义尺寸在屏幕空间的大小
            // 需要考虑 Canvas 的缩放
            float canvasScale = rootCanvas.transform.lossyScale.x;
            Vector2 screenSize = m_CustomSize * canvasScale;

            // 归一化坐标
            Vector2 normalizedCenter = new Vector2(
                screenCenter.x / Screen.width,
                screenCenter.y / Screen.height
            );

            Vector2 normalizedSize = new Vector2(
                screenSize.x / Screen.width,
                screenSize.y / Screen.height
            );

            // 设置 Shader 参数
            material.SetVector("_Rect", new Vector4(
                normalizedCenter.x - normalizedSize.x / 2f,
                normalizedCenter.y - normalizedSize.y / 2f,
                normalizedSize.x,
                normalizedSize.y
            ));
        }

        public Rect GetBounds()
        {
            return new Rect(
                -m_CustomSize.x / 2f,
                -m_CustomSize.y / 2f,
                m_CustomSize.x,
                m_CustomSize.y
            );
        }
    }

    /// <summary>
    /// 自定义圆形半径的镂空形状
    /// 使用 RectTransform 中心，但使用自定义半径
    /// </summary>
    public class CustomCircleHollowShape : IHollowShape
    {
        private RectTransform m_Target;
        private float m_CustomRadius;

        public CustomCircleHollowShape(RectTransform target, float customRadius)
        {
            m_Target = target;
            m_CustomRadius = customRadius;
        }

        /// <summary>
        /// 动态更新目标
        /// </summary>
        public void UpdateTarget(RectTransform target)
        {
            m_Target = target;
        }

        /// <summary>
        /// 动态更新自定义半径
        /// </summary>
        public void UpdateRadius(float radius)
        {
            m_CustomRadius = radius;
        }

        /// <summary>
        /// 同时更新目标和半径
        /// </summary>
        public void UpdateTargetAndRadius(RectTransform target, float radius)
        {
            m_Target = target;
            m_CustomRadius = radius;
        }

        public bool IsPointInside(Vector2 screenPoint, Camera eventCamera)
        {
            if (m_Target == null)
                return false;

            // 将屏幕坐标转换为目标的本地坐标
            Vector2 localPoint;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    m_Target, screenPoint, eventCamera, out localPoint))
            {
                return false;
            }

            // 计算到中心的距离（RectTransform 的本地中心通常是 (0,0)）
            float distance = localPoint.magnitude;

            return distance <= m_CustomRadius;
        }

        public void UpdateShaderProperties(Material material, Canvas rootCanvas)
        {
            if (material == null || rootCanvas == null || m_Target == null)
                return;

            // 获取 RectTransform 在屏幕空间的中心位置
            Vector3[] worldCorners = new Vector3[4];
            m_Target.GetWorldCorners(worldCorners);
            Vector3 worldCenter = (worldCorners[0] + worldCorners[2]) / 2f;

            // 转换为屏幕空间
            Camera camera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : rootCanvas.worldCamera;

            Vector2 screenCenter;
            if (camera != null)
            {
                screenCenter = RectTransformUtility.WorldToScreenPoint(camera, worldCenter);
            }
            else
            {
                screenCenter = worldCenter;
            }

            // 计算自定义半径在屏幕空间的大小
            float canvasScale = rootCanvas.transform.lossyScale.x;
            float screenRadius = m_CustomRadius * canvasScale;

            // 归一化坐标
            Vector2 normalizedCenter = new Vector2(
                screenCenter.x / Screen.width,
                screenCenter.y / Screen.height
            );

            float normalizedRadius = screenRadius / Screen.width;

            // 设置 Shader 参数
            material.SetVector("_Circle",
                new Vector4(normalizedCenter.x, normalizedCenter.y, normalizedRadius, 0));
        }

        public Rect GetBounds()
        {
            float diameter = m_CustomRadius * 2f;
            return new Rect(
                -m_CustomRadius,
                -m_CustomRadius,
                diameter,
                diameter
            );
        }
    }
}