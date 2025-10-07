using System;
using UIExt.Effect;

namespace GameGuide.Core.Effect
{
    /// <summary>
    /// Base class for guide effects
    /// </summary>
    public abstract class GuideEffectBase : EffectBase, IGuideEffect, IResettableEffect
    {
        #region Guide-specific Events

        public event Action<IGuideEffect> OnGuideEffectCompleted;
        public event Action<IGuideEffect> OnGuideEffectStarted;
        public event Action<IGuideEffect> OnGuideEffectStopped;

        #endregion

        #region Guide-specific Methods

        /// <summary>
        /// Reset effect state
        /// </summary>
        public virtual void Reset()
        {
            Stop();
            OnReset();
        }

        #endregion

        #region Overrides

        public override void Play()
        {
            base.Play();

            if (m_IsPlaying)
            {
                OnGuideEffectStarted?.Invoke(this);
            }
        }

        public override void Stop()
        {
            base.Stop();

            if (!m_IsPlaying)
            {
                OnGuideEffectStopped?.Invoke(this);
            }
        }

        protected override void InvokeComplete()
        {
            base.InvokeComplete();
            OnGuideEffectCompleted?.Invoke(this);
        }

        protected override void InvokeCancel()
        {
            base.InvokeCancel();
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