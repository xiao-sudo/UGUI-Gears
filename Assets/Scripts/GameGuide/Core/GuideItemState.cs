namespace GameGuide.Core
{
    /// <summary>
    /// 引导项状态枚举
    /// </summary>
    public enum GuideItemState
    {
        Inactive,       // 未激活
        Waiting,        // 等待触发条件满足
        Active,         // 激活中，效果正在播放
        Completed,      // 已完成
        Cancelled,      // 已取消
        Failed          // 执行失败
    }

    /// <summary>
    /// 引导项优先级
    /// </summary>
    public enum GuideItemPriority
    {
        Low = 0,
        Normal = 1,
        High = 2,
        Critical = 3
    }

    /// <summary>
    /// 引导组状态枚举
    /// </summary>
    public enum GuideGroupState
    {
        Inactive,       // 未激活
        Waiting,        // 等待执行
        Running,        // 运行中
        Paused,         // 已暂停
        Completed,      // 已完成（正常结束）
        Cancelled,      // 已取消（用户主动取消）
        Failed          // 执行失败（异常结束）
    }
}
