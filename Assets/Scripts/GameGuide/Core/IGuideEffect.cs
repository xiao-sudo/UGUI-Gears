using System;
using UIExt.Effect;

namespace GameGuide.Core
{
    /// <summary>
    /// 引导效果接口扩展
    /// </summary>
    public interface IGuideEffect : IEffect
    {
        // 引导专用事件
        event Action<IGuideEffect> OnGuideEffectCompleted;
        event Action<IGuideEffect> OnGuideEffectStarted;
        event Action<IGuideEffect> OnGuideEffectStopped;
        
        // 设置引导项引用
        void SetGuideItem(IGuideItem guideItem);
        
        // 引导专用状态
        bool IsGuideActive { get; }
    }

    /// <summary>
    /// 可重置的效果接口
    /// </summary>
    public interface IResettableEffect
    {
        void Reset();
    }
}
