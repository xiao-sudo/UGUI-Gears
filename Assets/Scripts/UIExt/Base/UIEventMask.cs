using System;
using UnityEngine;
using UnityEngine.UI;

namespace UIExt.Base
{
    [RequireComponent(typeof(UIEventPassThrough))]
    public class UIEventMask : Image
    {
        public enum MaskType
        {
            Rect,
            Circle
        }

        public enum PassThroughType
        {
            /// <summary>
            /// Pass in target's rect
            /// </summary>
            PassAtTargetRect,

            /// <summary>
            /// Pass outside target's rect
            /// </summary>
            PassExcludeTargetRect,

            /// <summary>
            /// Pass always
            /// </summary>
            PassAlways,

            /// <summary>
            /// Pass Never
            /// </summary>
            PassNever
        }

        [SerializeField]
        private UIEventPassThrough m_EventPassThrough;

        [SerializeField]
        private PassThroughType m_PassThroughType = PassThroughType.PassAtTargetRect;

        [SerializeField]
        private MaskType m_MaskType = MaskType.Rect;

        private GameObject m_MaskTarget;
        private RectTransform m_MaskTargetTransform;

        public PassThroughType PassThroughStyle
        {
            get => m_PassThroughType;
            set => m_PassThroughType = value;
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

        public void SetMask(MaskType maskType)
        {
        }

        public override bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            switch (m_PassThroughType)
            {
                case PassThroughType.PassAtTargetRect:
                    var r = RectTransformUtility.RectangleContainsScreenPoint(m_MaskTargetTransform, screenPoint,
                        eventCamera);

                    return !r;

                case PassThroughType.PassExcludeTargetRect:
                    return RectTransformUtility.RectangleContainsScreenPoint(m_MaskTargetTransform, screenPoint,
                        eventCamera);

                case PassThroughType.PassAlways:
                    return false;

                case PassThroughType.PassNever:
                    return true;

                default:
                    return true;
            }
        }

        public Rect HollowTargetRectOnScreenSpace
        {
            get
            {
                var rect = Utility.UIRect.GetRectInScreenSpace(m_MaskTargetTransform, canvas.GetComponent<Camera>());
                return rect;
            }
        }

        public RectTransform MaskTargetRectTransform => m_MaskTargetTransform;
    }
}