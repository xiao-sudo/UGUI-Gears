using System;
using UnityEngine;

namespace UIExt.Base.UIHollow
{
    // ==================== 枚举定义 ====================

    /// <summary>
    /// 材质类型枚举
    /// </summary>
    public enum MaterialType
    {
        Rect,
        Circle
    }

    /// <summary>
    /// 镂空尺寸模式
    /// 定义如何确定镂空区域的大小
    /// </summary>
    [Serializable]
    public enum HollowSizeMode
    {
        /// <summary>
        /// 使用 RectTransform 的实际尺寸
        /// 镂空区域完全匹配目标 UI 元素的大小
        /// </summary>
        UseRectTransformSize,

        /// <summary>
        /// 自定义矩形尺寸（宽高）
        /// 以 RectTransform 中心为中心点，使用自定义的宽高
        /// </summary>
        CustomRectSize,

        /// <summary>
        /// 自定义圆形尺寸（半径）
        /// 以 RectTransform 中心为圆心，使用自定义的半径
        /// </summary>
        CustomCircleRadius
    }

    // ==================== 配置类 ====================

    /// <summary>
    /// 基于 RectTransform 中心点的镂空配置
    /// 统一使用 RectTransform 作为中心，支持三种尺寸模式
    /// </summary>
    [Serializable]
    public class RectTransformHollowConfig
    {
        [Header("Target")]
        [SerializeField]
        [Tooltip("镂空的中心目标（必需）")]
        private RectTransform m_Target;

        [Header("Size Mode")]
        [SerializeField]
        [Tooltip("尺寸模式")]
        private HollowSizeMode m_SizeMode = HollowSizeMode.UseRectTransformSize;

        [Header("Custom Size Settings")]
        [SerializeField]
        [Tooltip("自定义矩形尺寸（当模式为 CustomRectSize 时使用）")]
        private Vector2 m_CustomSize = new Vector2(200, 200);

        [SerializeField]
        [Tooltip("自定义圆形半径（当模式为 CustomCircleRadius 时使用）")]
        private float m_CustomRadius = 100f;

        // Shape 实例缓存（不序列化）
        // 为每种 Shape 类型缓存一个实例，最多 3 个对象（对应 3 种 SizeMode）
        // 创建后永久复用，通过 UpdateTarget/Size/Radius 更新参数
        [System.NonSerialized]
        private RectHollowShape m_RectShapeInstance;

        [System.NonSerialized]
        private CustomRectHollowShape m_CustomRectShapeInstance;

        [System.NonSerialized]
        private CustomCircleHollowShape m_CustomCircleShapeInstance;

        // 属性
        public RectTransform Target
        {
            get => m_Target;
            set => m_Target = value;
        }

        public HollowSizeMode SizeMode
        {
            get => m_SizeMode;
            set => m_SizeMode = value;
        }

        public Vector2 CustomSize
        {
            get => m_CustomSize;
            set => m_CustomSize = value;
        }

        public float CustomRadius
        {
            get => m_CustomRadius;
            set => m_CustomRadius = value;
        }

        /// <summary>
        /// 创建对应的镂空形状（使用实例缓存）
        /// 每种类型缓存一个实例，创建后永久复用（只更新参数）
        /// 最多缓存 3 个对象（对应 3 种 SizeMode）
        /// </summary>
        public IHollowShape CreateShape()
        {
            if (!IsValid())
                return null;

            switch (m_SizeMode)
            {
                case HollowSizeMode.UseRectTransformSize:
                    // 获取或创建 RectHollowShape 实例
                    if (m_RectShapeInstance == null)
                    {
                        m_RectShapeInstance = new RectHollowShape(m_Target);
                    }
                    else
                    {
                        m_RectShapeInstance.UpdateTarget(m_Target);
                    }
                    return m_RectShapeInstance;

                case HollowSizeMode.CustomRectSize:
                    // 获取或创建 CustomRectHollowShape 实例
                    if (m_CustomRectShapeInstance == null)
                    {
                        m_CustomRectShapeInstance = new CustomRectHollowShape(m_Target, m_CustomSize);
                    }
                    else
                    {
                        m_CustomRectShapeInstance.UpdateTargetAndSize(m_Target, m_CustomSize);
                    }
                    return m_CustomRectShapeInstance;

                case HollowSizeMode.CustomCircleRadius:
                    // 获取或创建 CustomCircleHollowShape 实例
                    if (m_CustomCircleShapeInstance == null)
                    {
                        m_CustomCircleShapeInstance = new CustomCircleHollowShape(m_Target, m_CustomRadius);
                    }
                    else
                    {
                        m_CustomCircleShapeInstance.UpdateTargetAndRadius(m_Target, m_CustomRadius);
                    }
                    return m_CustomCircleShapeInstance;

                default:
                    return null;
            }
        }

        /// <summary>
        /// 清除所有缓存实例（强制重新创建所有 Shape）
        /// </summary>
        public void ClearInstances()
        {
            m_RectShapeInstance = null;
            m_CustomRectShapeInstance = null;
            m_CustomCircleShapeInstance = null;
        }

        /// <summary>
        /// 获取推荐的材质类型（用于视觉效果）
        /// </summary>
        public MaterialType GetMaterialType()
        {
            return m_SizeMode == HollowSizeMode.CustomCircleRadius
                ? MaterialType.Circle
                : MaterialType.Rect;
        }

        /// <summary>
        /// 验证配置是否有效
        /// </summary>
        public bool IsValid()
        {
            if (m_Target == null)
                return false;

            switch (m_SizeMode)
            {
                case HollowSizeMode.CustomRectSize:
                    return m_CustomSize.x > 0 && m_CustomSize.y > 0;

                case HollowSizeMode.CustomCircleRadius:
                    return m_CustomRadius > 0;

                default:
                    return true;
            }
        }

        /// <summary>
        /// 尝试更新现有 Shape（性能优化，避免重新创建）
        /// </summary>
        /// <returns>如果成功更新返回 true，否则返回 false（需要重新创建）</returns>
        public bool TryUpdateShape(IHollowShape existingShape)
        {
            // 支持动态更新自定义尺寸
            if (m_SizeMode == HollowSizeMode.CustomRectSize
                && existingShape is CustomRectHollowShape rectShape)
            {
                rectShape.UpdateSize(m_CustomSize);
                return true;
            }

            if (m_SizeMode == HollowSizeMode.CustomCircleRadius
                && existingShape is CustomCircleHollowShape circleShape)
            {
                circleShape.UpdateRadius(m_CustomRadius);
                return true;
            }

            return false;
        }
    }
}
