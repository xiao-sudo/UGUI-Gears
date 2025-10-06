using System;
using GameGuide.Conditions;
using UIExt.Effect;

namespace GameGuide.Core
{
    /// <summary>
    /// 引导项接口
    /// </summary>
    public interface IGuideItem
    {
        // 基础属性
        string ItemId { get; }
        string Description { get; }
        GuideItemPriority Priority { get; }
        GuideItemState State { get; }
        
        // 条件系统
        IGuideCondition TriggerCondition { get; }
        IGuideCondition CompletionCondition { get; }
        
        // 效果系统
        IGuideEffect GuideEffect { get; }
        
        // 时间管理
        float StartTime { get; }
        float Duration { get; }
        float RunningTimeoutSeconds { get; }
        
        // 状态查询
        bool IsActive { get; }
        bool IsCompleted { get; }
        bool IsWaiting { get; }
        
        // 生命周期控制
        void Initialize();
        // 进入等待态（由 GuideGroup 切换到该项时调用，不直接运行）
        void Enter();
        void StartItem();
        void CompleteItem();
        void CancelItem();
        void ResetItem();
        void Dispose();
        
        // 效果控制
        void PauseEffect();
        void ResumeEffect();
        
        // 更新方法（由外部调用）
        void Update();
        
        // 事件回调
        event Action<IGuideItem> OnItemStarted;
        event Action<IGuideItem> OnItemCompleted;
        event Action<IGuideItem> OnItemCancelled;
        event Action<IGuideItem> OnItemFailed;
        event Action<IGuideItem> OnStateChanged;
    }
}
