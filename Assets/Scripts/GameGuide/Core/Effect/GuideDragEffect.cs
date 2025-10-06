using System;
using UIExt.Effect;
using UnityEngine;

namespace GameGuide.Core.Effect
{
    /// <summary>
    /// 引导拖拽效果 - 基于DragEffect实现
    /// </summary>
    public class GuideDragEffect : GuideEffectBase
    {
        #region 私有字段
        
        private DragEffect m_DragEffect;
        private Action<Vector2, Vector2> m_OnDragComplete;
        private Action<Vector2, Vector2> m_OnDragStart;
        private RectTransform m_StartPosition;
        private RectTransform m_EndPosition;
        
        #endregion

        #region 属性
        
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

        #region 配置方法（链式调用）
        
        /// <summary>
        /// 设置拖拽位置
        /// </summary>
        public GuideDragEffect SetPositions(RectTransform startPos, RectTransform endPos)
        {
            m_StartPosition = startPos;
            m_EndPosition = endPos;
            return this;
        }

        /// <summary>
        /// 设置完成阈值
        /// </summary>
        public GuideDragEffect SetCompleteThreshold(float threshold)
        {
            // 注意：DragEffect的阈值设置可能需要通过其他方式实现
            // 这里先保留接口，具体实现可能需要根据DragEffect的API调整
            return this;
        }

        /// <summary>
        /// 设置拖拽提示
        /// </summary>
        public GuideDragEffect SetDragHint(RectTransform dragHint)
        {
            // 注意：DragEffect的提示设置可能需要通过其他方式实现
            // 这里先保留接口，具体实现可能需要根据DragEffect的API调整
            return this;
        }

        /// <summary>
        /// 设置提示动画
        /// </summary>
        public GuideDragEffect SetHintAnimation(bool autoPlay, float duration, float loopDelay)
        {
            // 注意：DragEffect的动画设置可能需要通过其他方式实现
            // 这里先保留接口，具体实现可能需要根据DragEffect的API调整
            return this;
        }

        /// <summary>
        /// 设置轨迹显示
        /// </summary>
        public GuideDragEffect SetShowTrajectory(bool show, LineRenderer lineRenderer = null)
        {
            // 注意：DragEffect的轨迹设置可能需要通过其他方式实现
            // 这里先保留接口，具体实现可能需要根据DragEffect的API调整
            return this;
        }

        /// <summary>
        /// 设置拖拽开始回调
        /// </summary>
        public GuideDragEffect OnDragStart(Action<Vector2, Vector2> onDragStart)
        {
            m_OnDragStart = onDragStart;
            return this;
        }

        /// <summary>
        /// 设置拖拽完成回调
        /// </summary>
        public GuideDragEffect OnDragComplete(Action<Vector2, Vector2> onDragComplete)
        {
            m_OnDragComplete = onDragComplete;
            return this;
        }
        
        #endregion

        #region 重写基类方法
        
        protected override void OnPlay()
        {
            base.OnPlay();

            if (m_StartPosition == null || m_EndPosition == null)
            {
                Debug.LogError($"[GuideDragEffect] Start or end position is null");
                return;
            }

            // 设置DragEffect的目标（使用起始位置作为目标）
            DragEffect.Target = m_StartPosition;

            // 设置拖拽位置（需要根据DragEffect的实际API调整）
            // DragEffect.SetPositions(m_StartPosition, m_EndPosition);

            // 设置完成回调
            DragEffect.OnComplete(OnDragEffectComplete);

            // 播放DragEffect
            DragEffect.Play();
        }

        protected override void OnStop()
        {
            base.OnStop();

            // 停止DragEffect
            DragEffect.Stop();
        }

        protected override void OnPause()
        {
            base.OnPause();

            // 暂停DragEffect
            DragEffect.Pause();
        }

        protected override void OnResume()
        {
            base.OnResume();

            // 恢复DragEffect
            DragEffect.Resume();
        }

        protected override void OnReset()
        {
            base.OnReset();
            
            // 重置DragEffect
            DragEffect.Stop();
        }
        
        #endregion

        #region 事件处理

        /// <summary>
        /// DragEffect完成回调
        /// </summary>
        private void OnDragEffectComplete()
        {
            // 调用用户设置的回调
            if (m_StartPosition != null && m_EndPosition != null)
            {
                var startPos = RectTransformUtility.WorldToScreenPoint(null, m_StartPosition.position);
                var endPos = RectTransformUtility.WorldToScreenPoint(null, m_EndPosition.position);
                m_OnDragComplete?.Invoke(startPos, endPos);
            }

            // 自动完成引导效果
            InvokeComplete();
        }
        
        #endregion
    }
}
