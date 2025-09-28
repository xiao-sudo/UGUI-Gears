using System;
using UnityEngine;

namespace UIExt.Base
{
    [RequireComponent(typeof(UIEventPassThrough))]
    public class UIEventMask : MonoBehaviour
    {
        public enum MaskType
        {
            Rect,
            Circle
        }

        [SerializeField]
        private UIEventPassThrough m_EventPassThrough;

        [SerializeField]
        private UIEventPassThrough.PassThroughType m_PassThroughType =
            UIEventPassThrough.PassThroughType.PassAtTargetRect;

        [SerializeField]
        private MaskType m_MaskType = MaskType.Rect;

        private GameObject m_MaskTarget;
        private RectTransform m_MaskTargetTransform;

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

        public void SetMaskTarget(GameObject target)
        {
            if (null == target)
            {
                Debug.LogError("Hollow Target is null");
                return;
            }

            m_MaskTarget = target;
            m_MaskTargetTransform = m_MaskTarget.GetComponent<RectTransform>();

            EventPassThrough.PassThroughTarget = m_MaskTarget;
        }

        private void Awake()
        {
            EventPassThrough.PassThroughStyle = PassThroughStyle;
        }

        public void SetMask(MaskType maskType)
        {
        }

        public RectTransform MaskTargetRectTransform => m_MaskTargetTransform;
    }
}