using System;
using UIExt.Effect;
using UnityEngine;

namespace GameGuide.Core.Effect
{
    /// <summary>
    /// Guide drag effect - built on top of DragEffect
    /// </summary>
    public class GuideDragEffect : GuideEffectBase
    {
        #region Private Fields
        
        private DragEffect m_DragEffect;
        private Action<Vector2, Vector2> m_OnDragComplete;
        private Action<Vector2, Vector2> m_OnDragStart;
        private RectTransform m_StartPosition;
        private RectTransform m_EndPosition;
        
        #endregion

        #region Properties
        
        private DragEffect DragEffect
        {
            get
            {
                if (m_DragEffect == null)
                {
                    m_DragEffect = GetComponent<DragEffect>();
                    if (m_DragEffect == null)
                    {
                        m_DragEffect = gameObject.AddComponent<DragEffect>();
                    }
                }
                return m_DragEffect;
            }
        }
        
        #endregion

        #region Configuration Methods (Fluent)
        
        /// <summary>
        /// Set drag positions
        /// </summary>
        public GuideDragEffect SetPositions(RectTransform startPos, RectTransform endPos)
        {
            m_StartPosition = startPos;
            m_EndPosition = endPos;
            return this;
        }

        /// <summary>
        /// Set completion threshold
        /// </summary>
        public GuideDragEffect SetCompleteThreshold(float threshold)
        {
            // Note: DragEffect threshold configuration may need other APIs.
            // Keeping the interface; implementation depends on DragEffect API.
            return this;
        }

        /// <summary>
        /// Set drag hint
        /// </summary>
        public GuideDragEffect SetDragHint(RectTransform dragHint)
        {
            // Note: DragEffect hint configuration may need other APIs.
            // Keeping the interface; implementation depends on DragEffect API.
            return this;
        }

        /// <summary>
        /// Set hint animation
        /// </summary>
        public GuideDragEffect SetHintAnimation(bool autoPlay, float duration, float loopDelay)
        {
            // Note: DragEffect animation configuration may need other APIs.
            // Keeping the interface; implementation depends on DragEffect API.
            return this;
        }

        /// <summary>
        /// Set trajectory display
        /// </summary>
        public GuideDragEffect SetShowTrajectory(bool show, LineRenderer lineRenderer = null)
        {
            // Note: DragEffect trajectory configuration may need other APIs.
            // Keeping the interface; implementation depends on DragEffect API.
            return this;
        }

        /// <summary>
        /// Set drag start callback
        /// </summary>
        public GuideDragEffect OnDragStart(Action<Vector2, Vector2> onDragStart)
        {
            m_OnDragStart = onDragStart;
            return this;
        }

        /// <summary>
        /// Set drag complete callback
        /// </summary>
        public GuideDragEffect OnDragComplete(Action<Vector2, Vector2> onDragComplete)
        {
            m_OnDragComplete = onDragComplete;
            return this;
        }
        
        #endregion

        #region Overrides
        
        protected override void OnPlay()
        {
            base.OnPlay();

            if (m_StartPosition == null || m_EndPosition == null)
            {
                Debug.LogError($"[GuideDragEffect] Start or end position is null");
                return;
            }

            // Set DragEffect target (use start position as target)
            DragEffect.Target = m_StartPosition;

            // Set drag positions (adjust according to DragEffect's API)
            // DragEffect.SetPositions(m_StartPosition, m_EndPosition);

            // Set completion callback
            DragEffect.OnComplete(OnDragEffectComplete);

            // Play DragEffect
            DragEffect.Play();
        }

        protected override void OnStop()
        {
            base.OnStop();

            // Stop DragEffect
            DragEffect.Stop();
        }

        protected override void OnPause()
        {
            base.OnPause();

            // Pause DragEffect
            DragEffect.Pause();
        }

        protected override void OnResume()
        {
            base.OnResume();

            // Resume DragEffect
            DragEffect.Resume();
        }

        protected override void OnReset()
        {
            base.OnReset();
            
            // Reset DragEffect
            DragEffect.Stop();
        }
        
        #endregion

        #region Event Handling

        /// <summary>
        /// DragEffect completed callback
        /// </summary>
        private void OnDragEffectComplete()
        {
            // Invoke user callback
            if (m_StartPosition != null && m_EndPosition != null)
            {
                var startPos = RectTransformUtility.WorldToScreenPoint(null, m_StartPosition.position);
                var endPos = RectTransformUtility.WorldToScreenPoint(null, m_EndPosition.position);
                m_OnDragComplete?.Invoke(startPos, endPos);
            }

            // Auto-complete the guide effect
            InvokeComplete();
        }
        
        #endregion
    }
}
