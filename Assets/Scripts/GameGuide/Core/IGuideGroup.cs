using System;
using System.Collections.Generic;

namespace GameGuide.Core
{
    /// <summary>
    /// 引导组接口
    /// </summary>
    public interface IGuideGroup
    {
        // 基础属性
        string GroupId { get; }
        string GroupName { get; }
        GuideGroupState State { get; }

        // 引导项管理
        List<IGuideItem> GuideItems { get; }
        IGuideItem CurrentItem { get; }
        int CurrentItemIndex { get; }

        // 状态查询
        bool IsRunning { get; }
        bool IsCompleted { get; }
        bool IsPaused { get; }

        // 执行控制
        void StartGroup();
        void PauseGroup();
        void ResumeGroup();
        void StopGroup();
        void FailGroup();
        void ResetGroup();

        // 引导项管理
        void AddGuideItem(IGuideItem item);
        void RemoveGuideItem(string itemId);
        void ClearGuideItems();

        void Initialize();
        void Dispose();

        void Update();

        // 事件回调
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