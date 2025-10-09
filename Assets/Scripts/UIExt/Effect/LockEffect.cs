using System;
using UIExt.Base;
using UnityEngine;

namespace UIExt.Effect
{
    /// <summary>
    /// lock effect - highlights and locks a UI element
    /// Supports mask, focus frame, and animation
    /// </summary>
    [RequireComponent(typeof(UIEventMask))]
    public class LockEffect : EffectBase
    {
        [Serializable]
        private class LockFocus
        {
            [SerializeField]
            [Tooltip("the Rect is the root of the focus effect")]
            private RectTransform m_FocusRect;

            [SerializeField]
            [Tooltip("Offset for the focus rect (x,y: center offset, z,w: width/height increase)")]
            private Vector4 m_FocusOffset = new Vector4(0, 0, 0, 0);

            private IAnimation m_FocusAnimation;

            private bool m_Initialized;

            public bool IsValid => null != m_FocusRect;

            public Vector4 FocusOffset
            {
                get => m_FocusOffset;
                set => m_FocusOffset = value;
            }

            public void Initialize()
            {
                if (m_Initialized)
                    return;

                ForceInitialize();

                m_Initialized = true;
            }

            public void ForceInitialize()
            {
                if (null != m_FocusRect)
                {
                    m_FocusAnimation = m_FocusRect.GetComponent<IAnimation>();

                    if (null != m_FocusAnimation)
                        m_FocusAnimation.Initialize();
                }
            }

            public void Focus(RectTransform target)
            {
                if (null == target)
                    return;

                if (IsValid)
                {
                    Initialize();

                    m_FocusRect.gameObject.SetActive(true);

                    Utility.UIRect.SetTargetRectBySource(m_FocusRect, target, m_FocusOffset);

                    if (null != m_FocusAnimation)
                    {
                        m_FocusAnimation.Play();
                    }
                }
            }

            public void Update(RectTransform target)
            {
                if (null == target)
                    return;

                if (IsValid)
                {
                    Utility.UIRect.SetTargetRectBySource(m_FocusRect, target, m_FocusOffset);
                }
            }

            public void Stop()
            {
                if (null != m_FocusAnimation)
                    m_FocusAnimation.Stop();

                if (null != m_FocusRect)
                    m_FocusRect.gameObject.SetActive(false);
            }

            public void Pause()
            {
                if (null != m_FocusAnimation)
                    m_FocusAnimation.Pause();
            }

            public void Resume()
            {
                if (null != m_FocusAnimation)
                    m_FocusAnimation.Resume();
            }
        }

        [Header("Lock Settings")]
        [SerializeField]
        [Tooltip("Mask type for the lock effect")]
        private UIEventMask.MaskType m_MaskType = UIEventMask.MaskType.Rect;

        [SerializeField]
        [Tooltip("Whether the target can be clicked")]
        private bool m_AllowClick = true;

        [SerializeField]
        private LockFocus m_LockFocus = new();

        private UIEventMask m_EventMask;

        private UIEventMask EventMask
        {
            get
            {
                if (m_EventMask == null)
                    m_EventMask = GetComponent<UIEventMask>();
                return m_EventMask;
            }
        }

        #region Unity Functions

        private void Awake()
        {
            m_LockFocus.Initialize();
        }

        #endregion

        /// <summary>
        /// Set mask type
        /// </summary>
        public LockEffect SetMaskType(UIEventMask.MaskType maskType)
        {
            m_MaskType = maskType;
            return this;
        }

        /// <summary>
        /// Set whether target can be clicked
        /// </summary>
        public LockEffect SetAllowClick(bool allowClick)
        {
            m_AllowClick = allowClick;
            return this;
        }


        /// <summary>
        /// Set focus frame offset
        /// </summary>
        /// <param name="centerOffset">Center position offset (x, y)</param>
        /// <param name="sizeIncrease">Size increase (width, height)</param>
        public LockEffect SetFocusFrameOffset(Vector2 centerOffset, Vector2 sizeIncrease)
        {
            m_LockFocus.FocusOffset = new Vector4(centerOffset.x, centerOffset.y, sizeIncrease.x, sizeIncrease.y);
            return this;
        }

        /// <summary>
        /// Set focus frame offset
        /// </summary>
        /// <param name="offsetX">Center X offset</param>
        /// <param name="offsetY">Center Y offset</param>
        /// <param name="widthIncrease">Width increase</param>
        /// <param name="heightIncrease">Height increase</param>
        public LockEffect SetFocusFrameOffset(float offsetX, float offsetY, float widthIncrease,
            float heightIncrease)
        {
            m_LockFocus.FocusOffset = new Vector4(offsetX, offsetY, widthIncrease, heightIncrease);
            return this;
        }

        protected override void OnPlay()
        {
            base.OnPlay();

            if (null == m_Target)
            {
                Debug.LogWarning("Target is null in LockEffect.OnPlay", this);
                return;
            }

            // Setup mask
            EventMask.SetUIMaskTarget(m_Target, m_MaskType);
            EventMask.PassThroughStyle = m_AllowClick
                ? UIEventPassThrough.PassThroughType.PassAtTargetRect
                : UIEventPassThrough.PassThroughType.PassNever;

            // Setup focus
            if (m_LockFocus.IsValid)
                m_LockFocus.Focus(m_Target);

            gameObject.SetActive(true);
        }

        protected override void OnStop()
        {
            base.OnStop();

            EventMask.PassThroughClickCallback = null;

            if (m_LockFocus.IsValid)
                m_LockFocus.Stop();

            gameObject.SetActive(false);
        }

        protected override void OnPause()
        {
            base.OnPause();

            if (m_LockFocus.IsValid)
                m_LockFocus.Pause();
        }

        protected override void OnResume()
        {
            base.OnResume();

            if (m_LockFocus.IsValid)
                m_LockFocus.Resume();
        }

        private void Update()
        {
            if (!m_IsPlaying || m_IsPaused)
                return;

            if (m_LockFocus.IsValid)
                m_LockFocus.Update(m_Target);
        }
    }
}