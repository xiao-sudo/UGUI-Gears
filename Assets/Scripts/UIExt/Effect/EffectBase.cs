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

        public RectTransform Target
        {
            get => m_Target;
            set => m_Target = value;
        }

        public bool IsPlaying => m_IsPlaying;

        public virtual void Play()
        {
            if (m_Target == null)
            {
                Debug.LogError($"[{GetType().Name}] Target is null, cannot play effect");
                return;
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

