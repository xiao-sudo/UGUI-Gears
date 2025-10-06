using System;
using UIExt.Base;
using UIExt.Effect;
using UnityEngine;

namespace GameGuide.Core.Effect
{
    /// <summary>
    /// Guide lock effect - built on top of LockEffect
    /// </summary>
    [RequireComponent(typeof(UIEventMask))]
    public class GuideLockEffect : GuideEffectBase
    {
        #region Private Fields
        
        private LockEffect m_LockEffect;
        private Action<GameObject> m_OnTargetClick;
        
        #endregion

        #region Properties
        
        private LockEffect LockEffect
        {
            get
            {
                if (m_LockEffect == null)
                {
                    m_LockEffect = GetComponent<LockEffect>();
                    if (m_LockEffect == null)
                    {
                        m_LockEffect = gameObject.AddComponent<LockEffect>();
                    }
                }
                return m_LockEffect;
            }
        }
        
        #endregion

        #region Configuration Methods (Fluent)

        /// <summary>
        /// Set mask type
        /// </summary>
        public GuideLockEffect SetMaskType(UIEventMask.MaskType maskType)
        {
            LockEffect.SetMaskType(maskType);
            return this;
        }

        /// <summary>
        /// Set whether clicking is allowed
        /// </summary>
        public GuideLockEffect SetAllowClick(bool allowClick)
        {
            LockEffect.SetAllowClick(allowClick);
            return this;
        }

        /// <summary>
        /// Set focus frame
        /// </summary>
        public GuideLockEffect SetFocusFrame(RectTransform focusFrame)
        {
            LockEffect.SetFocusFrame(focusFrame);
            return this;
        }

        /// <summary>
        /// Set focus frame offset
        /// </summary>
        public GuideLockEffect SetFocusFrameOffset(Vector2 centerOffset, Vector2 sizeIncrease)
        {
            LockEffect.SetFocusFrameOffset(centerOffset, sizeIncrease);
            return this;
        }

        /// <summary>
        /// Set focus frame offset
        /// </summary>
        public GuideLockEffect SetFocusFrameOffset(float offsetX, float offsetY, float widthIncrease, float heightIncrease)
        {
            LockEffect.SetFocusFrameOffset(offsetX, offsetY, widthIncrease, heightIncrease);
            return this;
        }

        /// <summary>
        /// Set focus animation
        /// </summary>
        public GuideLockEffect SetFocusAnimation(IAnimation imageAnimation)
        {
            LockEffect.SetFocusAnimation(imageAnimation);
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

        #endregion

        #region Overrides

        protected override void OnPlay()
        {
            base.OnPlay();

            if (m_Target == null)
            {
                Debug.LogError($"[GuideLockEffect] Target is null, cannot play effect");
                return;
            }

            // Set LockEffect target
            LockEffect.Target = m_Target;

            // Set click callback
            LockEffect.OnTargetClick(OnLockEffectTargetClick);

            // Play LockEffect
            LockEffect.Play();
        }

        protected override void OnStop()
        {
            base.OnStop();

            // Stop LockEffect
            LockEffect.Stop();
        }

        protected override void OnPause()
        {
            base.OnPause();

            // Pause LockEffect
            LockEffect.Pause();
        }

        protected override void OnResume()
        {
            base.OnResume();

            // Resume LockEffect
            LockEffect.Resume();
        }

        protected override void OnReset()
        {
            base.OnReset();
            
            // Reset LockEffect
            LockEffect.Stop();
        }

        #endregion

        #region Event Handling

        /// <summary>
        /// LockEffect target clicked callback
        /// </summary>
        private void OnLockEffectTargetClick(GameObject clickedObject)
        {
            // Invoke user callback
            m_OnTargetClick?.Invoke(clickedObject);

            // Auto-complete guide effect
            InvokeComplete();
        }

        #endregion
    }
}