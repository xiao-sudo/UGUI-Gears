using System;
using UIExt.Effect;

namespace GameGuide.Core.Effect
{
    /// <summary>
    /// Guide drag effect - guides user to drag an element with guide-specific events
    /// </summary>
    public class GuideDragEffect : DragEffect, IGuideEffect, IResettableEffect
    {
        #region IGuideEffect Implementation

        public event Action<IGuideEffect> OnGuideEffectCompleted;
        public event Action<IGuideEffect> OnGuideEffectStarted;
        public event Action<IGuideEffect> OnGuideEffectStopped;

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

        #region IResettableEffect Implementation

        public virtual void Reset()
        {
            Stop();
        }

        #endregion
    }
}