using System;
using UnityEngine;

namespace UIExt.Base
{
    /// <summary>
    /// Abstract base class for animations
    /// Provides common implementation for IAnimation interface
    /// </summary>
    public abstract class AnimationBase : MonoBehaviour, IAnimation
    {
        [Header("Animation Settings")]
        [SerializeField]
        [Tooltip("Animation speed multiplier")]
        protected float m_Speed = 1f;

        [SerializeField]
        [Tooltip("Whether to play automatically on enable")]
        protected bool m_AutoPlay = false;

        [SerializeField]
        [Tooltip("Whether to loop the animation")]
        protected bool m_Loop = false;

        protected bool m_Initialized = false;
        protected bool m_IsPlaying;
        protected bool m_IsPaused;
        protected float m_Duration;
        protected float m_NormalizedTime;

        // Events
        protected Action m_OnStart;
        protected Action m_OnComplete;
        protected Action m_OnPause;
        protected Action m_OnResume;
        protected Action m_OnStop;

        #region IAnimation Properties

        public virtual bool IsPlaying => m_IsPlaying;
        public virtual bool IsPaused => m_IsPaused;

        public virtual float Duration => m_Duration;
        public virtual float NormalizedTime => m_NormalizedTime;

        public float Speed
        {
            get => m_Speed;
            set => m_Speed = value;
        }

        #endregion

        #region IAnimation Methods

        public void Initialize()
        {
            if (!m_Initialized)
                InitializeAnimation();

            m_Initialized = true;
        }

        public virtual void Play()
        {
            if (m_IsPlaying && !m_IsPaused)
                return;

            if (!ValidateAnimation())
            {
                Debug.LogError($"[{GetType().Name}] Animation validation failed, cannot play");
                return;
            }

            m_IsPlaying = true;
            m_IsPaused = false;
            m_OnStart?.Invoke();
            OnPlayInternal();
        }

        public virtual void Pause()
        {
            if (!m_IsPlaying || m_IsPaused)
                return;

            m_IsPaused = true;
            m_OnPause?.Invoke();
            OnPauseInternal();
        }

        public virtual void Resume()
        {
            if (!m_IsPlaying || !m_IsPaused)
                return;

            m_IsPaused = false;
            m_OnResume?.Invoke();
            OnResumeInternal();
        }

        public virtual void Stop()
        {
            if (!m_IsPlaying)
                return;

            m_IsPlaying = false;
            m_IsPaused = false;
            m_OnStop?.Invoke();
            OnStopInternal();
        }

        public IAnimation OnComplete(Action onComplete)
        {
            m_OnComplete = onComplete;
            return this;
        }

        public IAnimation OnStart(Action onStart)
        {
            m_OnStart = onStart;
            return this;
        }

        public IAnimation OnPause(Action onPause)
        {
            m_OnPause = onPause;
            return this;
        }

        public IAnimation OnResume(Action onResume)
        {
            m_OnResume = onResume;
            return this;
        }

        public IAnimation OnStop(Action onStop)
        {
            m_OnStop = onStop;
            return this;
        }

        #endregion

        #region Unity Lifecycle

        protected virtual void Awake()
        {
            Initialize();
        }

        protected virtual void OnEnable()
        {
            if (m_AutoPlay)
            {
                Play();
            }
        }

        protected virtual void OnDisable()
        {
            Stop();
        }

        #endregion

        #region Abstract Methods

        /// <summary>
        /// Initialize the animation
        /// </summary>
        protected abstract void InitializeAnimation();

        /// <summary>
        /// Internal play implementation
        /// </summary>
        protected abstract void OnPlayInternal();

        /// <summary>
        /// Internal pause implementation
        /// </summary>
        protected abstract void OnPauseInternal();

        /// <summary>
        /// Internal resume implementation
        /// </summary>
        protected abstract void OnResumeInternal();

        /// <summary>
        /// Internal stop implementation
        /// </summary>
        protected abstract void OnStopInternal();

        /// <summary>
        /// Validate animation before playing
        /// Override to add custom validation logic
        /// </summary>
        protected virtual bool ValidateAnimation()
        {
            return true;
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Call this when animation completes naturally
        /// </summary>
        protected void OnAnimationComplete()
        {
            if (m_Loop)
            {
                // Reset and play again
                m_NormalizedTime = 0f;
                OnPlayInternal();
            }
            else
            {
                m_IsPlaying = false;
                m_IsPaused = false;
                m_OnComplete?.Invoke();
            }
        }

        /// <summary>
        /// Update normalized time based on delta time
        /// </summary>
        protected void UpdateNormalizedTime()
        {
            if (m_Duration > 0)
            {
                m_NormalizedTime += Time.deltaTime * m_Speed / m_Duration;

                if (m_NormalizedTime >= 1f)
                {
                    m_NormalizedTime = 1f;
                    OnAnimationComplete();
                }
            }
        }

        #endregion
    }
}