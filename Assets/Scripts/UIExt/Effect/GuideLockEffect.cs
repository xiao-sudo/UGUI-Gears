using System;
using UIExt.Base;
using UIExt.ImageAnimation;
using UnityEngine;

namespace UIExt.Effect
{
    /// <summary>
    /// Guide lock effect - highlights and locks a UI element
    /// Supports mask, focus frame, and animation
    /// </summary>
    [RequireComponent(typeof(UIEventMask))]
    public class GuideLockEffect : GuideEffectBase
    {
        [Header("Lock Settings")]
        [SerializeField]
        [Tooltip("Mask type for the lock effect")]
        private UIEventMask.MaskType m_MaskType = UIEventMask.MaskType.Rect;

        [SerializeField]
        [Tooltip("Whether the target can be clicked")]
        private bool m_AllowClick = true;

        [Header("Focus Frame")]
        [SerializeField]
        [Tooltip("Focus frame to highlight the target")]
        private RectTransform m_FocusFrame;

        [SerializeField]
        [Tooltip("Offset for the focus frame (x,y: center offset, z,w: width/height increase)")]
        private Vector4 m_FocusOffset = new Vector4(0, 0, 0, 0);

        private IAnimation m_FocusAnimation;

        private UIEventMask m_EventMask;
        private Action<GameObject> m_OnTargetClick;

        private UIEventMask EventMask
        {
            get
            {
                if (m_EventMask == null)
                    m_EventMask = GetComponent<UIEventMask>();
                return m_EventMask;
            }
        }

        /// <summary>
        /// Set mask type
        /// </summary>
        public GuideLockEffect SetMaskType(UIEventMask.MaskType maskType)
        {
            m_MaskType = maskType;
            return this;
        }

        /// <summary>
        /// Set whether target can be clicked
        /// </summary>
        public GuideLockEffect SetAllowClick(bool allowClick)
        {
            m_AllowClick = allowClick;
            return this;
        }

        /// <summary>
        /// Set focus frame
        /// </summary>
        public GuideLockEffect SetFocusFrame(RectTransform focusFrame)
        {
            m_FocusFrame = focusFrame;
            return this;
        }

        /// <summary>
        /// Set focus frame with offset
        /// </summary>
        /// <param name="focusFrame">Focus frame RectTransform</param>
        /// <param name="offset">Offset (x,y: center offset, z,w: width/height increase)</param>
        public GuideLockEffect SetFocusFrame(RectTransform focusFrame, Vector4 offset)
        {
            m_FocusFrame = focusFrame;
            m_FocusOffset = offset;
            return this;
        }

        /// <summary>
        /// Set focus frame offset
        /// </summary>
        /// <param name="centerOffset">Center position offset (x, y)</param>
        /// <param name="sizeIncrease">Size increase (width, height)</param>
        public GuideLockEffect SetFocusFrameOffset(Vector2 centerOffset, Vector2 sizeIncrease)
        {
            m_FocusOffset = new Vector4(centerOffset.x, centerOffset.y, sizeIncrease.x, sizeIncrease.y);
            return this;
        }

        /// <summary>
        /// Set focus frame offset
        /// </summary>
        /// <param name="offsetX">Center X offset</param>
        /// <param name="offsetY">Center Y offset</param>
        /// <param name="widthIncrease">Width increase</param>
        /// <param name="heightIncrease">Height increase</param>
        public GuideLockEffect SetFocusFrameOffset(float offsetX, float offsetY, float widthIncrease,
            float heightIncrease)
        {
            m_FocusOffset = new Vector4(offsetX, offsetY, widthIncrease, heightIncrease);
            return this;
        }

        /// <summary>
        /// Set focus animation
        /// </summary>
        public GuideLockEffect SetFocusAnimation(IAnimation imageAnimation)
        {
            m_FocusAnimation = imageAnimation;
            return this;
        }

        /// <summary>
        /// Set target click callback
        /// </summary>
        public GuideLockEffect OnTargetClick(Action<GameObject> onTargetClick)
        {
            m_OnTargetClick = onTargetClick;
            return this;
        }

        protected override void OnPlay()
        {
            base.OnPlay();

            // Setup mask
            EventMask.SetUIMaskTarget(m_Target, m_MaskType);
            EventMask.PassThroughStyle = m_AllowClick
                ? UIEventPassThrough.PassThroughType.PassAtTargetRect
                : UIEventPassThrough.PassThroughType.PassNever;

            // Setup click callback
            EventMask.PassThroughClickCallback = OnMaskTargetClick;

            // Setup focus frame
            if (m_FocusFrame != null)
            {
                m_FocusFrame.gameObject.SetActive(true);
                Utility.UIRect.SetTargetRectBySource(m_FocusFrame, m_Target, m_FocusOffset);

                // Play animation if exists
                if (m_FocusAnimation != null)
                {
                    m_FocusAnimation.Initialize();
                    m_FocusAnimation.Play();
                }
            }

            gameObject.SetActive(true);
        }

        protected override void OnStop()
        {
            base.OnStop();

            EventMask.PassThroughClickCallback = null;

            if (m_FocusFrame != null)
            {
                if (m_FocusAnimation != null)
                {
                    m_FocusAnimation.Stop();
                }

                m_FocusFrame.gameObject.SetActive(false);
            }

            gameObject.SetActive(false);
        }

        protected override void OnPause()
        {
            base.OnPause();

            if (m_FocusAnimation != null && m_FocusAnimation.IsPlaying)
            {
                m_FocusAnimation.Pause();
            }
        }

        protected override void OnResume()
        {
            base.OnResume();

            if (m_FocusAnimation != null)
            {
                m_FocusAnimation.Resume();
            }
        }

        private void OnMaskTargetClick(GameObject clickedObject)
        {
            m_OnTargetClick?.Invoke(clickedObject);
        }

        private void Update()
        {
            if (!m_IsPlaying || m_IsPaused)
                return;

            // Update focus frame position if target moves
            if (m_FocusFrame != null && m_Target != null)
            {
                UpdateFocusFrame();
            }
        }

        private void UpdateFocusFrame()
        {
            if (Utility.UIRect.AreRectEquals(m_FocusFrame, m_Target))
                return;

            Utility.UIRect.SetTargetRectBySource(m_FocusFrame, m_Target, m_FocusOffset);
        }
    }
}