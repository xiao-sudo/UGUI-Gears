using System;
using UnityEngine;
using UnityEngine.UI;

namespace UIExt.Base.UIHollow
{
    /// <summary>
    /// UI 镂空遮罩组件
    /// 基于 ICanvasRaycastFilter 实现真正的镂空效果
    /// 在指定区域内不接收 Raycast，事件可以穿透到下层系统（如 EasyTouch）
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class UIHollowMask : MonoBehaviour, ICanvasRaycastFilter
    {
        [Serializable]
        public enum HollowShapeType
        {
            /// <summary>
            /// 无镂空
            /// </summary>
            None,

            /// <summary>
            /// 矩形镂空
            /// </summary>
            Rect,

            /// <summary>
            /// 圆形镂空
            /// </summary>
            Circle
        }

        [Serializable]
        public enum HollowMode
        {
            /// <summary>
            /// 镂空区域内穿透，外部阻挡
            /// </summary>
            HollowInside,

            /// <summary>
            /// 镂空区域外穿透，内部阻挡
            /// </summary>
            HollowOutside
        }

        [Header("Hollow Configuration")]
        [SerializeField]
        private RectTransformHollowConfig m_InputConfig = new RectTransformHollowConfig();

        [SerializeField]
        private HollowMode m_HollowMode = HollowMode.HollowInside;

        [SerializeField]
        private bool m_EnableVisualMask = true;

        [Header("Materials")]
        [SerializeField]
        private Material m_RectMaskMaterial;

        [SerializeField]
        private Material m_CircleMaskMaterial;

        [Header("Debug")]
        [SerializeField]
        private bool m_ShowDebugInfo = false;

        // 私有字段
        private IHollowShape m_CurrentShape;
        private Image m_MaskImage;
        private Canvas m_RootCanvas;

        // 事件
        public event Action<Vector2> OnRaycastFiltered;

        // 属性（只读，通过 SetHollowTargetXXX 方法修改）
        public RectTransformHollowConfig InputConfig => m_InputConfig;

        public HollowMode Mode
        {
            get => m_HollowMode;
            set => m_HollowMode = value;
        }

        public bool EnableVisualMask
        {
            get => m_EnableVisualMask;
            set
            {
                if (m_EnableVisualMask != value)
                {
                    m_EnableVisualMask = value;
                    UpdateVisualMask();
                }
            }
        }

        #region ICanvasRaycastFilter Implementation

        /// <summary>
        /// Unity 在 Raycast 阶段调用此方法
        /// 返回 false 表示此 UI 元素不参与 Raycast，事件会穿透到下层
        /// </summary>
        public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
        {
            // 无镂空形状，正常接收所有事件
            if (m_CurrentShape == null)
            {
                return true;
            }

            bool isInsideHollow = m_CurrentShape.IsPointInside(sp, eventCamera);

            bool shouldBlockRaycast;
            switch (m_HollowMode)
            {
                case HollowMode.HollowInside:
                    // 内部穿透：在镂空区域内返回 false（穿透），外部返回 true（阻挡）
                    shouldBlockRaycast = !isInsideHollow;
                    break;

                case HollowMode.HollowOutside:
                    // 外部穿透：在镂空区域外返回 false（穿透），内部返回 true（阻挡）
                    shouldBlockRaycast = isInsideHollow;
                    break;

                default:
                    shouldBlockRaycast = true;
                    break;
            }

            // 调试信息
            if (m_ShowDebugInfo && !shouldBlockRaycast)
            {
                OnRaycastFiltered?.Invoke(sp);
                Debug.Log(
                    $"[UIHollowMask] Raycast filtered at {sp}, inside hollow: {isInsideHollow}, mode: {m_HollowMode}, input mode: {m_InputConfig.GetType().Name}");
            }

            return shouldBlockRaycast;
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeComponents();
        }

        private void Start()
        {
            UpdateHollowShape();
        }

        private void LateUpdate()
        {
            // 每帧更新视觉效果（处理目标移动）
            if (m_EnableVisualMask && m_CurrentShape != null)
                UpdateVisualMask();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 清除镂空目标
        /// </summary>
        public void ClearHollowTarget()
        {
            m_CurrentShape = null;
            UpdateVisualMask();
        }

        #endregion

        #region New RectTransform-Centered API

        /// <summary>
        /// 模式 1：使用 RectTransform 的实际尺寸作为镂空区域
        /// 镂空区域完全匹配目标 UI 元素的大小
        /// </summary>
        /// <param name="target">目标 RectTransform（作为镂空中心和尺寸来源）</param>
        public void SetHollowTargetWithOriginalSize(RectTransform target)
        {
            m_InputConfig.Target = target;
            m_InputConfig.SizeMode = HollowSizeMode.UseRectTransformSize;
            RecreateShape();
        }

        /// <summary>
        /// 模式 2：以 RectTransform 为中心，指定自定义宽高作为镂空区域
        /// 创建一个以目标 UI 元素中心为中心的自定义大小矩形镂空
        /// </summary>
        /// <param name="target">目标 RectTransform（作为镂空中心）</param>
        /// <param name="customSize">自定义矩形尺寸（Canvas 单位）</param>
        public void SetHollowTargetWithCustomSize(RectTransform target, Vector2 customSize)
        {
            m_InputConfig.Target = target;
            m_InputConfig.SizeMode = HollowSizeMode.CustomRectSize;
            m_InputConfig.CustomSize = customSize;
            RecreateShape();
        }

        /// <summary>
        /// 模式 3：以 RectTransform 为中心，指定半径作为镂空区域
        /// 创建一个以目标 UI 元素中心为圆心的圆形镂空
        /// </summary>
        /// <param name="target">目标 RectTransform（作为镂空中心）</param>
        /// <param name="radius">圆形半径（Canvas 单位）</param>
        public void SetHollowTargetWithRadius(RectTransform target, float radius)
        {
            m_InputConfig.Target = target;
            m_InputConfig.SizeMode = HollowSizeMode.CustomCircleRadius;
            m_InputConfig.CustomRadius = radius;
            RecreateShape();
        }

        /// <summary>
        /// 动态更新自定义矩形尺寸（性能优化，避免重新创建 Shape）
        /// 仅在当前配置为 CustomRectSize 模式时有效
        /// </summary>
        /// <param name="newSize">新的矩形尺寸</param>
        public void UpdateCustomSize(Vector2 newSize)
        {
            if (m_InputConfig.SizeMode == HollowSizeMode.CustomRectSize)
            {
                m_InputConfig.CustomSize = newSize;

                if (m_CurrentShape != null && m_InputConfig.TryUpdateShape(m_CurrentShape))
                {
                    UpdateVisualMask();
                    return;
                }
            }

            Debug.LogWarning("[UIHollowMask] UpdateCustomSize failed: Not in CustomRectSize mode");
        }

        /// <summary>
        /// 动态更新圆形半径（性能优化，避免重新创建 Shape）
        /// 仅在当前配置为 CustomCircleRadius 模式时有效
        /// </summary>
        /// <param name="newRadius">新的半径</param>
        public void UpdateRadius(float newRadius)
        {
            if (m_InputConfig.SizeMode == HollowSizeMode.CustomCircleRadius)
            {
                m_InputConfig.CustomRadius = newRadius;

                if (m_CurrentShape != null && m_InputConfig.TryUpdateShape(m_CurrentShape))
                {
                    UpdateVisualMask();
                    return;
                }
            }

            Debug.LogWarning("[UIHollowMask] UpdateRadius failed: Not in CustomCircleRadius mode");
        }

        #endregion

        #region Private Methods

        private void InitializeComponents()
        {
            m_MaskImage = GetComponent<Image>();

            // 确保 Image 可以作为 Raycast Target
            m_MaskImage.raycastTarget = true;

            // 获取根画布
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
                m_RootCanvas = canvas.rootCanvas;
        }

        /// <summary>
        /// 更新镂空形状（带优化：尝试更新现有 Shape）
        /// 用于 Inspector OnValidate 等场景
        /// </summary>
        private void UpdateHollowShape()
        {
            if (m_InputConfig == null || !m_InputConfig.IsValid())
            {
                m_CurrentShape = null;
                UpdateVisualMask();
                return;
            }

            // 尝试更新现有 Shape（性能优化）
            if (m_CurrentShape != null && m_InputConfig.TryUpdateShape(m_CurrentShape))
            {
                UpdateVisualMask();
                return;
            }

            // 无法更新，重新创建
            RecreateShape();
        }

        /// <summary>
        /// 重新创建镂空形状（智能缓存：SizeMode 相同时复用 Shape）
        /// 用于 SetHollowTargetWithXXX 等明确改变模式的场景
        /// </summary>
        private void RecreateShape()
        {
            if (m_InputConfig == null || !m_InputConfig.IsValid())
            {
                m_CurrentShape = null;
                UpdateVisualMask();
                return;
            }

            // 创建 Shape（内部会智能判断是否复用）
            m_CurrentShape = m_InputConfig.CreateShape();

            // 设置材质
            if (m_EnableVisualMask && m_CurrentShape != null)
            {
                var materialType = m_InputConfig.GetMaterialType();
                var material = materialType == MaterialType.Circle
                    ? m_CircleMaskMaterial
                    : m_RectMaskMaterial;
                SetupVisualMask(material);
            }

            UpdateVisualMask();
        }


        private void SetupVisualMask(Material material)
        {
            if (m_MaskImage == null || material == null)
                return;

            m_MaskImage.material = material;
        }

        private void UpdateVisualMask()
        {
            if (!m_EnableVisualMask || m_CurrentShape == null || m_MaskImage == null || m_MaskImage.material == null)
            {
                // 禁用视觉效果
                if (m_MaskImage != null && m_MaskImage.material != null)
                {
                    // 可以设置一个默认材质或者移除材质
                    m_MaskImage.material = null;
                }

                return;
            }

            // 更新 Shader 参数
            m_CurrentShape.UpdateShaderProperties(m_MaskImage.material, m_RootCanvas);
        }

        #endregion

        #region Editor Support

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                UpdateHollowShape();
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (m_CurrentShape == null)
                return;

            Gizmos.color = m_HollowMode == HollowMode.HollowInside ? Color.red : Color.green;
            UnityEditor.Handles.color = Gizmos.color;

            // 绘制镂空区域
            DrawHollowGizmos();
        }

        private void DrawHollowGizmos()
        {
            if (m_InputConfig == null || m_InputConfig.Target == null)
                return;

            var worldCorners = new Vector3[4];
            m_InputConfig.Target.GetWorldCorners(worldCorners);
            var center = (worldCorners[0] + worldCorners[2]) / 2f;

            switch (m_InputConfig.SizeMode)
            {
                case HollowSizeMode.UseRectTransformSize:
                    // 绘制原始尺寸矩形
                    Gizmos.DrawWireCube(center, worldCorners[2] - worldCorners[0]);
                    UnityEditor.Handles.Label(center, "Original Size");
                    break;

                case HollowSizeMode.CustomRectSize:
                    // 绘制自定义尺寸矩形
                    var canvasScale = m_RootCanvas != null ? m_RootCanvas.transform.lossyScale.x : 1f;
                    var screenSize = m_InputConfig.CustomSize * canvasScale;
                    Gizmos.DrawWireCube(center, new Vector3(screenSize.x, screenSize.y, 0));
                    UnityEditor.Handles.Label(center,
                        $"Custom Rect\n{m_InputConfig.CustomSize.x} x {m_InputConfig.CustomSize.y}");
                    break;

                case HollowSizeMode.CustomCircleRadius:
                    // 绘制自定义半径圆形
                    var scale = m_RootCanvas != null ? m_RootCanvas.transform.lossyScale.x : 1f;
                    var screenRadius = m_InputConfig.CustomRadius * scale;
                    UnityEditor.Handles.DrawWireDisc(center, Vector3.forward, screenRadius);
                    UnityEditor.Handles.Label(center, $"Custom Circle\nRadius: {m_InputConfig.CustomRadius}");
                    break;
            }
        }
#endif

        #endregion
    }
}