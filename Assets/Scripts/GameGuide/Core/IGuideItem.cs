using System;
using GameGuide.Conditions;

namespace GameGuide.Core
{
    /// <summary>
    /// Guide item interface
    /// </summary>
    public interface IGuideItem
    {
        // Basic properties
        string ItemId { get; }
        string Description { get; }
        GuideItemPriority Priority { get; }
        GuideItemState State { get; }

        // Condition system
        IGuideCondition TriggerCondition { get; }
        IGuideCondition CompletionCondition { get; }

        // Effect system
        IGuideEffect GuideEffect { get; }

        // Time management
        float EnterTime { get; }
        float Duration { get; }
        float RunningTimeoutSeconds { get; }

        // State queries
        bool IsActive { get; }
        bool IsCompleted { get; }
        bool IsWaiting { get; }

        void Initialize();

        // Enter Waiting (called when GuideGroup switches to this item; does not run immediately)
        void Enter();
        void StartItem();
        void CompleteItem();
        void CancelItem();
        void ResetItem();
        void Dispose();

        // Effect control
        void PauseEffect();
        void ResumeEffect();

        // Update method (called externally)
        void Update();

        // Event callbacks
        event Action<IGuideItem> OnItemStarted;
        event Action<IGuideItem> OnItemCompleted;
        event Action<IGuideItem> OnItemCancelled;
        event Action<IGuideItem> OnItemFailed;
        event Action<IGuideItem> OnStateChanged;
    }
}