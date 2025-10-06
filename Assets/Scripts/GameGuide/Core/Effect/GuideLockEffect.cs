using System;
using UIExt.Base;
using UIExt.Effect;
using UnityEngine;

namespace GameGuide.Core.Effect
{
    /// <summary>
    /// 引导锁定效果 - 基于LockEffect实现
    /// </summary>
    [RequireComponent(typeof(UIEventMask))]
    public class GuideLockEffect : GuideEffectBase
    {
        #region 私有字段
        
        private LockEffect m_LockEffect;
        private Action<GameObject> m_OnTargetClick;
        
        #endregion

        #region 属性
        
        private LockEffect LockEffect
        {
            get
            {
                if (m_LockEffect == null)
                {
                    m_LockEffect = GetComponent<LockEffect>();
                    if (m_LockEffect == null)
                    {
                        m_LockEffect = gameObject.AddComponent<LockEffect>();
                    }
                }
                return m_LockEffect;
            }
        }
        
        #endregion

        #region 配置方法（链式调用）

        /// <summary>
        /// 设置遮罩类型
        /// </summary>
        public GuideLockEffect SetMaskType(UIEventMask.MaskType maskType)
        {
            LockEffect.SetMaskType(maskType);
            return this;
        }

        /// <summary>
        /// 设置是否允许点击
        /// </summary>
        public GuideLockEffect SetAllowClick(bool allowClick)
        {
            LockEffect.SetAllowClick(allowClick);
            return this;
        }

        /// <summary>
        /// 设置聚焦框
        /// </summary>
        public GuideLockEffect SetFocusFrame(RectTransform focusFrame)
        {
            LockEffect.SetFocusFrame(focusFrame);
            return this;
        }

        /// <summary>
        /// 设置聚焦框偏移
        /// </summary>
        public GuideLockEffect SetFocusFrameOffset(Vector2 centerOffset, Vector2 sizeIncrease)
        {
            LockEffect.SetFocusFrameOffset(centerOffset, sizeIncrease);
            return this;
        }

        /// <summary>
        /// 设置聚焦框偏移
        /// </summary>
        public GuideLockEffect SetFocusFrameOffset(float offsetX, float offsetY, float widthIncrease, float heightIncrease)
        {
            LockEffect.SetFocusFrameOffset(offsetX, offsetY, widthIncrease, heightIncrease);
            return this;
        }

        /// <summary>
        /// 设置聚焦动画
        /// </summary>
        public GuideLockEffect SetFocusAnimation(IAnimation imageAnimation)
        {
            LockEffect.SetFocusAnimation(imageAnimation);
            return this;
        }

        /// <summary>
        /// 设置目标点击回调
        /// </summary>
        public GuideLockEffect OnTargetClick(Action<GameObject> onTargetClick)
        {
            m_OnTargetClick = onTargetClick;
            return this;
        }

        #endregion

        #region 重写基类方法

        protected override void OnPlay()
        {
            base.OnPlay();

            if (m_Target == null)
            {
                Debug.LogError($"[GuideLockEffect] Target is null, cannot play effect");
                return;
            }

            // 设置LockEffect的目标
            LockEffect.Target = m_Target;

            // 设置点击回调
            LockEffect.OnTargetClick(OnLockEffectTargetClick);

            // 播放LockEffect
            LockEffect.Play();
        }

        protected override void OnStop()
        {
            base.OnStop();

            // 停止LockEffect
            LockEffect.Stop();
        }

        protected override void OnPause()
        {
            base.OnPause();

            // 暂停LockEffect
            LockEffect.Pause();
        }

        protected override void OnResume()
        {
            base.OnResume();

            // 恢复LockEffect
            LockEffect.Resume();
        }

        protected override void OnReset()
        {
            base.OnReset();
            
            // 重置LockEffect
            LockEffect.Stop();
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// LockEffect目标点击回调
        /// </summary>
        private void OnLockEffectTargetClick(GameObject clickedObject)
        {
            // 调用用户设置的回调
            m_OnTargetClick?.Invoke(clickedObject);

            // 自动完成引导效果
            InvokeComplete();
        }

        #endregion
    }
}