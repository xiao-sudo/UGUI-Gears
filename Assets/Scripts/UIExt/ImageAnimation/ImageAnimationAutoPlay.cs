using Animancer;
using UIExt.Base;
using UnityEngine;

namespace UIExt.ImageAnimation
{
    public class ImageAnimationAutoPlay : AnimationBase
    {
        [SerializeField]
        private AnimationClip m_Clip;

        private AnimancerComponent m_Animancer;
        private Animator m_Animator;
        private AnimancerState m_AnimancerState;
        private AnimancerEvent m_OneLoopEndEvent;

        public override bool IsPlaying => m_AnimancerState != null && m_AnimancerState.IsPlaying;

        public override float NormalizedTime
        {
            get
            {
                if (null != m_AnimancerState)
                    return m_AnimancerState.NormalizedTime;

                return 0;
            }
        }

        protected override void InitializeAnimation()
        {
            m_Animator = GetComponent<Animator>();
            if (null == m_Animator)
                m_Animator = gameObject.AddComponent<Animator>();

            m_Animancer = GetComponent<AnimancerComponent>();
            if (null == m_Animancer)
                m_Animancer = gameObject.AddComponent<AnimancerComponent>();

            m_Animancer.Animator = m_Animator;
            m_OneLoopEndEvent = new AnimancerEvent(AnimancerEvent.AlmostOne, OnAnimancerStateEnd);

            // Set duration from clip
            if (m_Clip != null)
            {
                m_Duration = m_Clip.length;
            }
        }

        protected override void OnPlayInternal()
        {
            if (m_AnimancerState != null)
            {
                m_AnimancerState.Play();
            }
            else
            {
                if (m_Clip != null)
                {
                    m_AnimancerState = m_Animancer.Play(m_Clip);
                    m_AnimancerState.Speed = m_Speed;

                    m_AnimancerState.OwnedEvents.Add(m_OneLoopEndEvent);
                }
            }
        }

        protected override void OnPauseInternal()
        {
            if (m_AnimancerState != null && m_AnimancerState.IsPlaying)
            {
                m_AnimancerState.Speed = 0;
            }
        }

        protected override void OnResumeInternal()
        {
            if (m_AnimancerState != null)
            {
                m_AnimancerState.Speed = m_Speed;
                if (!m_AnimancerState.IsPlaying)
                {
                    m_AnimancerState.Play();
                }
            }
        }

        protected override void OnStopInternal()
        {
            if (m_AnimancerState != null)
            {
                m_AnimancerState.OwnedEvents.Remove(m_OneLoopEndEvent);
                m_AnimancerState.Stop();
            }
        }

        /// <summary>
        /// Called when AnimancerState ends one loop
        /// </summary>
        private void OnAnimancerStateEnd()
        {
            if (m_Loop)
            {
                m_AnimancerState.Time = 0;
                m_AnimancerState.Play();
                return;
            }

            Stop();
            OnAnimationComplete();
        }

        protected override bool ValidateAnimation()
        {
            if (m_Clip == null)
            {
                Debug.LogError($"[{GetType().Name}] Animation clip is null");
                return false;
            }

            if (m_Animancer == null)
            {
                Debug.LogError($"[{GetType().Name}] Animancer component is null");
                return false;
            }

            if (m_Animator == null)
            {
                Debug.LogError($"[{GetType().Name}] Animator component is null");
                return false;
            }

            return true;
        }

        private void OnDestroy()
        {
            m_Animator = null;
            m_Animancer = null;
        }
    }
}