namespace GameGuide.Core
{
    /// <summary>
    /// Guide item state enum
    /// </summary>
    public enum GuideItemState
    {
        Inactive,       // Not active
        Waiting,        // Waiting for trigger condition
        Active,         // Active, effect is playing
        Completed,      // Completed
        Cancelled,      // Cancelled
        Failed          // Failed to execute
    }

    /// <summary>
    /// Guide item priority
    /// </summary>
    public enum GuideItemPriority
    {
        Low = 0,
        Normal = 1,
        High = 2,
        Critical = 3
    }

    /// <summary>
    /// Guide group state enum
    /// </summary>
    public enum GuideGroupState
    {
        Inactive,       // Not active
        Waiting,        // Waiting to run
        Running,        // Running
        Paused,         // Paused
        Completed,      // Completed (normal finish)
        Cancelled,      // Cancelled (user cancelled)
        Failed          // Failed (abnormal finish)
    }
}
