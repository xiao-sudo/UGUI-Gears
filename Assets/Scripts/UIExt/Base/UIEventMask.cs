using System;
using UIExt.Utility;
using UnityEngine;
using UnityEngine.UI;

namespace UIExt.Base
{
    [RequireComponent(typeof(UIEventPassThrough))]
    public class UIEventMask : MonoBehaviour
    {
        public enum MaskType
        {
            None,
            Rect,
            Circle
        }

        [SerializeField]
        private UIEventPassThrough m_EventPassThrough;

        [SerializeField]
        private UIEventPassThrough.PassThroughType m_PassThroughType =
            UIEventPassThrough.PassThroughType.PassAtTargetRect;

        [SerializeField]
        private Material m_RectMaskMaterial;

        private MaskType m_MaskType = MaskType.None;

        private Canvas m_Canvas;

        private RectTransform m_UIMaskTarget;
        private Image m_MaskImage;
        private Material m_CurrentMaskMaterial;

        public UIEventPassThrough.PassThroughType PassThroughStyle
        {
            get => m_PassThroughType;
            set
            {
                m_PassThroughType = value;
                EventPassThrough.PassThroughStyle = value;
            }
        }

        public MaskType MaskStyle
        {
            get => m_MaskType;
            set => m_MaskType = value;
        }

        /// <summary>
        /// Passed click event is executed
        /// </summary>
        public Action<GameObject> PassThroughClickCallback
        {
            set => EventPassThrough.PassThroughClickCallback = value;
        }

        private UIEventPassThrough EventPassThrough
        {
            get
            {
                if (null == m_EventPassThrough)
                    m_EventPassThrough = GetComponent<UIEventPassThrough>();

                return m_EventPassThrough;
            }
        }

        public void SetUIMaskTarget(RectTransform target, MaskType maskType = MaskType.Rect)
        {
            if (null == target)
            {
                Debug.LogError("Hollow Target is null");
                return;
            }

            if (m_UIMaskTarget == target)
                return;

            m_UIMaskTarget = target;

            EventPassThrough.PassThroughTarget = m_UIMaskTarget;

            if (m_MaskType != maskType)
                SetMaskMaterial(maskType);

            m_MaskType = maskType;
            UpdateMaskMaterial();
        }

        private void Awake()
        {
            EventPassThrough.PassThroughStyle = PassThroughStyle;
        }

        public RectTransform UIMaskTarget => m_UIMaskTarget;

        private Image MaskImage
        {
            get
            {
                if (null == m_MaskImage)
                    m_MaskImage = GetComponent<Image>();

                return m_MaskImage;
            }
        }

        private Canvas RootCanvas
        {
            get
            {
                if (null == m_Canvas)
                {
                    m_Canvas = GetComponentInParent<Canvas>();
                }

                return m_Canvas.rootCanvas;
            }
        }

        private void SetMaskMaterial(MaskType maskType)
        {
            m_CurrentMaskMaterial = GetMaskMaterial(maskType);
            MaskImage.material = m_CurrentMaskMaterial;
        }

        private Material GetMaskMaterial(MaskType maskType)
        {
            switch (maskType)
            {
                case MaskType.Rect:
                    return m_RectMaskMaterial;

                default:
                    return m_RectMaskMaterial;
            }
        }

        private void UpdateMaskMaterial()
        {
            switch (m_MaskType)
            {
                case MaskType.Rect:
                    UpdateRectMaskMaterial();
                    break;

                default:
                    UpdateRectMaskMaterial();
                    break;
            }
        }

        private static readonly int RECT_ID = Shader.PropertyToID("_Rect");

        private void UpdateRectMaskMaterial()
        {
            var rect = UIRect.GetNormalizedRectInScreenSpace(m_UIMaskTarget, RootCanvas);
            m_CurrentMaskMaterial.SetVector(RECT_ID, new Vector4(rect.x, rect.y, rect.width, rect.height));
        }
    }
}