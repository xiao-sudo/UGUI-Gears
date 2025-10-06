using System;
using UIExt.Effect;

namespace GameGuide.Core.Effect
{
    /// <summary>
    /// 引导效果基类
    /// </summary>
    public abstract class GuideEffectBase : EffectBase, IGuideEffect, IResettableEffect
    {
        #region 引导专用字段

        private IGuideItem m_GuideItem;
        private bool m_IsGuideActive = false;

        #endregion

        #region 引导专用事件

        public event Action<IGuideEffect> OnGuideEffectCompleted;
        public event Action<IGuideEffect> OnGuideEffectStarted;
        public event Action<IGuideEffect> OnGuideEffectStopped;

        #endregion

        #region 引导专用属性

        public bool IsGuideActive => m_IsGuideActive;

        public bool IsPaused => m_IsPaused;

        #endregion

        #region 引导专用方法

        /// <summary>
        /// 设置引导项引用
        /// </summary>
        public void SetGuideItem(IGuideItem guideItem)
        {
            m_GuideItem = guideItem;
        }

        /// <summary>
        /// 重置效果状态
        /// </summary>
        public virtual void Reset()
        {
            Stop();
            m_IsGuideActive = false;
            OnReset();
        }

        #endregion

        #region 重写基类方法

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

        #region 虚方法（供子类重写）

        /// <summary>
        /// 重置时的处理
        /// </summary>
        protected virtual void OnReset()
        {
            // 子类可以重写此方法来实现自定义重置逻辑
        }

        #endregion
    }
}