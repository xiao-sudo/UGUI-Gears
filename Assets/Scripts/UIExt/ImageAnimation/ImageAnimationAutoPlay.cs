using Animancer;
using UnityEngine;

namespace UIExt.ImageAnimation
{
    public class ImageAnimationAutoPlay : MonoBehaviour
    {
        [SerializeField]
        private AnimationClip m_Clip;

        private AnimancerComponent m_Animancer;
        private Animator m_Animator;
        private AnimancerState m_AnimancerState;

        public bool IsPlaying => m_AnimancerState != null && m_AnimancerState.IsPlaying;

        private void Awake()
        {
            m_Animator = GetComponent<Animator>();
            if (null == m_Animator)
                m_Animator = gameObject.AddComponent<Animator>();

            m_Animancer = GetComponent<AnimancerComponent>();
            if (null == m_Animancer)
                m_Animancer = gameObject.AddComponent<AnimancerComponent>();

            m_Animancer.Animator = m_Animator;
        }

        private void OnEnable()
        {
            PlayImpl();
        }

        private void OnDisable()
        {
            StopImpl();
        }

        private void OnDestroy()
        {
            m_Animator = null;
            m_Animancer = null;
        }

        /// <summary>
        /// Manually play the animation
        /// </summary>
        public void Play()
        {
            PlayImpl();
        }

        /// <summary>
        /// Manually stop the animation
        /// </summary>
        public void Stop()
        {
            StopImpl();
        }

        /// <summary>
        /// Pause the animation
        /// </summary>
        public void Pause()
        {
            if (null != m_AnimancerState && m_AnimancerState.IsPlaying)
            {
                m_AnimancerState.Speed = 0;
            }
        }

        /// <summary>
        /// Resume the animation
        /// </summary>
        public void Resume()
        {
            if (null != m_AnimancerState)
            {
                m_AnimancerState.Speed = 1;
                if (!m_AnimancerState.IsPlaying)
                {
                    m_AnimancerState.Play();
                }
            }
        }

        private void PlayImpl()
        {
            if (null != m_AnimancerState)
                m_AnimancerState.Play();
            else
            {
                if (null != m_Clip)
                    m_AnimancerState = m_Animancer.Play(m_Clip);
            }
        }

        private void StopImpl()
        {
            if (null != m_AnimancerState)
                m_AnimancerState.Stop();
        }
    }
}