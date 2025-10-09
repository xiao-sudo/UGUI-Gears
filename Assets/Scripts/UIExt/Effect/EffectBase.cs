using System;
using UnityEngine;

namespace UIExt.Effect
{
    /// <summary>
    /// Base class for guide effects
    /// </summary>
    public abstract class EffectBase : MonoBehaviour, IEffect
    {
        [SerializeField]
        protected RectTransform m_Target;

        protected Action m_OnComplete;
        protected Action m_OnCancel;

        protected bool m_IsPlaying;
        protected bool m_IsPaused;

        /// <summary>
        /// Optional target finder function that will be called during Play() if m_Target is null
        /// </summary>
        protected Func<RectTransform> m_TargetFinder;

        public RectTransform Target
        {
            get => m_Target;
            set => m_Target = value;
        }

        public bool IsPlaying => m_IsPlaying;

        protected virtual bool NeedTarget => true;

        /// <summary>
        /// Set a dynamic target finder function
        /// </summary>
        public IEffect SetTargetFinder(Func<RectTransform> targetFinder)
        {
            m_TargetFinder = targetFinder;
            return this;
        }

        /// <summary>
        /// Override this method to provide custom target finding logic
        /// Called during Play() if m_Target is null and m_TargetFinder is also null
        /// </summary>
        protected virtual RectTransform FindTarget()
        {
            return null;
        }

        public virtual void Play()
        {
            // Try to resolve target if not set
            if (NeedTarget && m_Target == null)
            {
                // First try the target finder delegate
                if (m_TargetFinder != null)
                {
                    m_Target = m_TargetFinder();
                }
                // Then try the virtual FindTarget method
                else
                {
                    m_Target = FindTarget();
                }

                // If still null, log error and return
                if (m_Target == null)
                {
                    Debug.LogError($"[{GetType().Name}] Target is null and cannot be found, cannot play effect");
                    return;
                }
            }

            m_IsPlaying = true;
            m_IsPaused = false;
            OnPlay();
        }

        public virtual void Stop()
        {
            if (!m_IsPlaying)
                return;

            m_IsPlaying = false;
            m_IsPaused = false;
            OnStop();
        }

        public virtual void Pause()
        {
            if (!m_IsPlaying || m_IsPaused)
                return;

            m_IsPaused = true;
            OnPause();
        }

        public virtual void Resume()
        {
            if (!m_IsPlaying || !m_IsPaused)
                return;

            m_IsPaused = false;
            OnResume();
        }

        public IEffect OnComplete(Action onComplete)
        {
            m_OnComplete = onComplete;
            return this;
        }

        public IEffect OnCancel(Action onCancel)
        {
            m_OnCancel = onCancel;
            return this;
        }

        protected virtual void OnPlay()
        {
        }

        protected virtual void OnStop()
        {
        }

        protected virtual void OnPause()
        {
        }

        protected virtual void OnResume()
        {
        }

        protected virtual void InvokeComplete()
        {
            m_IsPlaying = false;
            m_OnComplete?.Invoke();
        }

        protected virtual void InvokeCancel()
        {
            m_IsPlaying = false;
            m_OnCancel?.Invoke();
        }

        protected virtual void OnDestroy()
        {
            Stop();
        }
    }
}