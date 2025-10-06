using System;
using System.Collections.Generic;

namespace GameGuide.Core
{
    /// <summary>
    /// Guide manager interface
    /// </summary>
    public interface IGuideManager
    {
        // Guide group management
        void RegisterGuideGroup(IGuideGroup group);
        void UnregisterGuideGroup(string groupId);
        IGuideGroup GetGuideGroup(string groupId);
        List<IGuideGroup> GetAllGuideGroups();
        List<IGuideGroup> GetActiveGuideGroups();
        
        // Execution control
        void StartGuideGroup(string groupId);
        void StopGuideGroup(string groupId);
        void PauseAllGuides();
        void ResumeAllGuides();
        void StopAllGuides();
        void FailAllGuides();
        
        // State queries
        bool IsGuideActive(string groupId);
        GuideGroupState GetGroupState(string groupId);
        bool HasGuideGroup(string groupId);
        
        // Persistence
        void SaveGuideProgress();
        void LoadGuideProgress();
        void ClearGuideProgress();
        
        // Event callbacks
        event Action<IGuideGroup> OnGroupStarted;
        event Action<IGuideGroup> OnGroupCompleted;
        event Action<IGuideGroup> OnGroupCancelled;
        event Action<IGuideGroup> OnGroupFailed;
        event Action<IGuideGroup> OnGroupPaused;
        event Action<IGuideGroup> OnGroupResumed;
        event Action OnAllGuidesPaused;
        event Action OnAllGuidesResumed;
        event Action OnAllGuidesCancelled;
    }
}
