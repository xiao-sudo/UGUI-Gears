using System;
using UIExt.Effect;

namespace GameGuide.Core.Effect
{
    /// <summary>
    /// Base class for guide effects
    /// </summary>
    public abstract class GuideEffectBase : EffectBase, IGuideEffect, IResettableEffect
    {
        #region Guide-specific Fields

        private IGuideItem m_GuideItem;
        private bool m_IsGuideActive = false;

        #endregion

        #region Guide-specific Events

        public event Action<IGuideEffect> OnGuideEffectCompleted;
        public event Action<IGuideEffect> OnGuideEffectStarted;
        public event Action<IGuideEffect> OnGuideEffectStopped;

        #endregion

        #region Guide-specific Properties

        public bool IsGuideActive => m_IsGuideActive;

        public bool IsPaused => m_IsPaused;

        #endregion

        #region Guide-specific Methods

        /// <summary>
        /// Set the guide item reference
        /// </summary>
        public void SetGuideItem(IGuideItem guideItem)
        {
            m_GuideItem = guideItem;
        }

        /// <summary>
        /// Reset effect state
        /// </summary>
        public virtual void Reset()
        {
            Stop();
            m_IsGuideActive = false;
            OnReset();
        }

        #endregion

        #region Overrides

        public override void Play()
        {
            base.Play();

            if (m_IsPlaying)
            {
                m_IsGuideActive = true;
                OnGuideEffectStarted?.Invoke(this);
            }
        }

        public override void Stop()
        {
            base.Stop();

            if (!m_IsPlaying)
            {
                m_IsGuideActive = false;
                OnGuideEffectStopped?.Invoke(this);
            }
        }

        protected override void InvokeComplete()
        {
            base.InvokeComplete();
            m_IsGuideActive = false;
            OnGuideEffectCompleted?.Invoke(this);
        }

        protected override void InvokeCancel()
        {
            base.InvokeCancel();
            m_IsGuideActive = false;
            OnGuideEffectStopped?.Invoke(this);
        }

        #endregion

        #region Virtual Methods (for subclasses)

        /// <summary>
        /// Handle reset
        /// </summary>
        protected virtual void OnReset()
        {
            // Subclasses can override to implement custom reset logic
        }

        #endregion
    }
}