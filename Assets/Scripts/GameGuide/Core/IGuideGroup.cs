using System;
using System.Collections.Generic;

namespace GameGuide.Core
{
    /// <summary>
    /// Guide group interface
    /// </summary>
    public interface IGuideGroup
    {
        // Basic properties
        string GroupId { get; }
        string GroupName { get; }
        GuideGroupState State { get; }

        // Guide item management
        List<IGuideItem> GuideItems { get; }
        IGuideItem CurrentItem { get; }
        int CurrentItemIndex { get; }

        // State queries
        bool IsRunning { get; }
        bool IsCompleted { get; }
        bool IsPaused { get; }

        // Execution control
        void StartGroup();
        void PauseGroup();
        void ResumeGroup();
        void StopGroup();
        void FailGroup();
        void ResetGroup();

        // Guide item management
        void AddGuideItem(IGuideItem item);
        void RemoveGuideItem(string itemId);
        void ClearGuideItems();

        void Initialize();
        void Dispose();

        void Update();

        // Event callbacks
        event Action<IGuideGroup> OnGroupStarted;
        event Action<IGuideGroup> OnGroupCompleted;
        event Action<IGuideGroup> OnGroupPaused;
        event Action<IGuideGroup> OnGroupResumed;
        event Action<IGuideGroup> OnGroupCancelled;
        event Action<IGuideGroup> OnGroupFailed;
        event Action<IGuideGroup, IGuideItem> OnCurrentItemChanged;
        event Action<IGuideGroup> OnStateChanged;
    }
}