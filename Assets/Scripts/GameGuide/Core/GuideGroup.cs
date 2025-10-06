using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GameGuide.Conditions;

namespace GameGuide.Core
{
    /// <summary>
    /// 引导组实现类 - 纯C#类，不继承MonoBehaviour
    /// </summary>
    [Serializable]
    public class GuideGroup : IGuideGroup
    {
        #region 序列化字段

        [SerializeField]
        private string m_GroupId;

        [SerializeField]
        private string m_GroupName;

        [SerializeField]
        private string m_Description;

        [SerializeField]
        private bool m_AutoStart = true;

        [SerializeField]
        private bool m_AutoComplete = true;

        [SerializeField]
        private bool m_CanPause = true;

        [SerializeField]
        private bool m_CanResume = true;

        #endregion

        #region 私有字段

        private GuideGroupState m_State = GuideGroupState.Inactive;
        private List<IGuideItem> m_Items = new();
        private int m_CurrentItemIndex = -1;
        private IGuideItem m_CurrentItem;
        private float m_StartTime = 0f;
        private float m_Duration = 0f;
        private bool m_IsInitialized = false;

        #endregion

        #region 公共属性

        public string GroupId => m_GroupId;
        public string GroupName => m_GroupName;
        public string Description => m_Description;
        public GuideGroupState State => m_State;
        public List<IGuideItem> GuideItems { get; }
        public float StartTime => m_StartTime;
        public float Duration => m_Duration;

        public IReadOnlyList<IGuideItem> Items => m_Items.AsReadOnly();
        public IGuideItem CurrentItem => m_CurrentItem;
        public int CurrentItemIndex => m_CurrentItemIndex;
        public int TotalItems => m_Items.Count;
        public int CompletedItems => m_Items.Count(item => item.IsCompleted);
        public int RemainingItems => m_Items.Count(item => !item.IsCompleted);

        public bool IsRunning => m_State == GuideGroupState.Running;
        public bool IsCompleted => m_State == GuideGroupState.Completed;
        public bool IsPaused => m_State == GuideGroupState.Paused;
        public bool IsEmpty => m_Items.Count == 0;

        #endregion

        #region 事件

        public void ClearGuideItems()
        {
            ClearItems();
        }

        public event Action<IGuideGroup> OnGroupStarted;
        public event Action<IGuideGroup> OnGroupCompleted;
        public event Action<IGuideGroup> OnGroupPaused;
        public event Action<IGuideGroup> OnGroupResumed;
        public event Action<IGuideGroup> OnGroupCancelled;
        public event Action<IGuideGroup> OnGroupFailed;
        public event Action<IGuideGroup, IGuideItem> OnCurrentItemChanged;
        public event Action<IGuideGroup> OnStateChanged;

        #endregion

        #region 构造函数

        public GuideGroup()
        {
            m_GroupId = Guid.NewGuid().ToString();
            m_GroupName = string.Empty;
            m_Description = string.Empty;
        }

        public GuideGroup(string groupId, string groupName = "", string description = "")
        {
            m_GroupId = string.IsNullOrEmpty(groupId) ? Guid.NewGuid().ToString() : groupId;
            m_GroupName = groupName ?? string.Empty;
            m_Description = description ?? string.Empty;
        }

        #endregion

        #region 配置方法（链式调用）

        /// <summary>
        /// 设置组ID
        /// </summary>
        public GuideGroup SetGroupId(string groupId)
        {
            m_GroupId = groupId;
            return this;
        }

        /// <summary>
        /// 设置组名称
        /// </summary>
        public GuideGroup SetGroupName(string groupName)
        {
            m_GroupName = groupName;
            return this;
        }

        /// <summary>
        /// 设置描述
        /// </summary>
        public GuideGroup SetDescription(string description)
        {
            m_Description = description;
            return this;
        }

        /// <summary>
        /// 设置是否自动开始
        /// </summary>
        public GuideGroup SetAutoStart(bool autoStart)
        {
            m_AutoStart = autoStart;
            return this;
        }

        /// <summary>
        /// 设置是否自动完成
        /// </summary>
        public GuideGroup SetAutoComplete(bool autoComplete)
        {
            m_AutoComplete = autoComplete;
            return this;
        }

        /// <summary>
        /// 设置是否可以暂停
        /// </summary>
        public GuideGroup SetCanPause(bool canPause)
        {
            m_CanPause = canPause;
            return this;
        }

        /// <summary>
        /// 设置是否可以恢复
        /// </summary>
        public GuideGroup SetCanResume(bool canResume)
        {
            m_CanResume = canResume;
            return this;
        }

        #endregion

        #region 引导项管理

        /// <summary>
        /// 添加引导项
        /// </summary>
        public GuideGroup AddItem(IGuideItem item)
        {
            if (item == null)
            {
                Debug.LogWarning("[GuideGroup] Cannot add null item");
                return this;
            }

            if (m_Items.Contains(item))
            {
                Debug.LogWarning($"[GuideGroup] Item {item.ItemId} already exists in group {GroupId}");
                return this;
            }

            m_Items.Add(item);

            // 订阅引导项事件
            SubscribeToItemEvents(item);

            Debug.Log($"[GuideGroup] Added item {item.ItemId} to group {GroupId}");
            return this;
        }

        /// <summary>
        /// 移除引导项
        /// </summary>
        public GuideGroup RemoveItem(IGuideItem item)
        {
            if (item == null) return this;

            if (!m_Items.Contains(item))
            {
                Debug.LogWarning($"[GuideGroup] Item {item.ItemId} not found in group {GroupId}");
                return this;
            }

            // 取消事件订阅
            UnsubscribeFromItemEvents(item);

            // 如果是要移除的当前项，先停止它
            if (m_CurrentItem == item)
            {
                StopCurrentItem();
            }

            m_Items.Remove(item);

            Debug.Log($"[GuideGroup] Removed item {item.ItemId} from group {GroupId}");
            return this;
        }

        /// <summary>
        /// 移除所有引导项
        /// </summary>
        public GuideGroup ClearItems()
        {
            // 停止当前项
            StopCurrentItem();

            // 取消所有事件订阅
            foreach (var item in m_Items)
            {
                UnsubscribeFromItemEvents(item);
            }

            m_Items.Clear();
            m_CurrentItemIndex = -1;
            m_CurrentItem = null;

            Debug.Log($"[GuideGroup] Cleared all items from group {GroupId}");
            return this;
        }

        /// <summary>
        /// 根据ID查找引导项
        /// </summary>
        public IGuideItem FindItem(string itemId)
        {
            return m_Items.FirstOrDefault(item => item.ItemId == itemId);
        }

        /// <summary>
        /// 根据索引获取引导项
        /// </summary>
        public IGuideItem GetItem(int index)
        {
            if (index < 0 || index >= m_Items.Count)
            {
                return null;
            }

            return m_Items[index];
        }

        #endregion

        #region 生命周期控制

        /// <summary>
        /// 初始化引导组
        /// </summary>
        public void Initialize()
        {
            if (m_IsInitialized) return;

            // 初始化所有引导项
            foreach (var item in m_Items)
            {
                item.Initialize();
            }

            m_IsInitialized = true;
            SetState(GuideGroupState.Waiting);

            Debug.Log($"[GuideGroup] Initialized: {GroupId} with {m_Items.Count} items");
        }

        /// <summary>
        /// 开始引导组
        /// </summary>
        public void StartGroup()
        {
            if (m_State != GuideGroupState.Waiting)
            {
                Debug.LogWarning($"[GuideGroup] Cannot start group {GroupId} in state {m_State}");
                return;
            }

            if (m_Items.Count == 0)
            {
                Debug.LogWarning($"[GuideGroup] Cannot start empty group {GroupId}");
                return;
            }

            // 记录开始时间
            m_StartTime = Time.time;

            // 更新状态
            SetState(GuideGroupState.Running);

            // 开始第一个引导项
            StartNextItem();

            // 触发开始事件
            OnGroupStarted?.Invoke(this);

            Debug.Log($"[GuideGroup] Started: {GroupId}");
        }

        /// <summary>
        /// 暂停引导组
        /// </summary>
        public void PauseGroup()
        {
            if (!m_CanPause)
            {
                Debug.LogWarning($"[GuideGroup] Group {GroupId} cannot be paused");
                return;
            }

            if (m_State != GuideGroupState.Running)
            {
                Debug.LogWarning($"[GuideGroup] Cannot pause group {GroupId} in state {m_State}");
                return;
            }

            // 暂停当前引导项
            if (m_CurrentItem != null)
            {
                m_CurrentItem.PauseEffect();
            }

            // 更新状态
            SetState(GuideGroupState.Paused);

            // 触发暂停事件
            OnGroupPaused?.Invoke(this);

            Debug.Log($"[GuideGroup] Paused: {GroupId}");
        }

        /// <summary>
        /// 恢复引导组
        /// </summary>
        public void ResumeGroup()
        {
            if (!m_CanResume)
            {
                Debug.LogWarning($"[GuideGroup] Group {GroupId} cannot be resumed");
                return;
            }

            if (m_State != GuideGroupState.Paused)
            {
                Debug.LogWarning($"[GuideGroup] Cannot resume group {GroupId} in state {m_State}");
                return;
            }

            // 恢复当前引导项
            if (m_CurrentItem != null)
            {
                m_CurrentItem.ResumeEffect();
            }

            // 更新状态
            SetState(GuideGroupState.Running);

            // 触发恢复事件
            OnGroupResumed?.Invoke(this);

            Debug.Log($"[GuideGroup] Resumed: {GroupId}");
        }

        /// <summary>
        /// 停止引导组（用户主动取消）
        /// </summary>
        public void StopGroup()
        {
            if (m_State == GuideGroupState.Inactive || m_State == GuideGroupState.Completed)
            {
                return;
            }

            // 停止当前引导项
            StopCurrentItem();

            // 更新状态为取消
            SetState(GuideGroupState.Cancelled);

            // 触发取消事件
            OnGroupCancelled?.Invoke(this);

            Debug.Log($"[GuideGroup] Cancelled: {GroupId}");
        }

        /// <summary>
        /// 标记引导组为失败状态（系统异常或错误）
        /// </summary>
        public void FailGroup()
        {
            if (m_State == GuideGroupState.Inactive || m_State == GuideGroupState.Completed)
            {
                return;
            }

            // 停止当前引导项
            StopCurrentItem();

            // 更新状态为失败
            SetState(GuideGroupState.Failed);

            // 触发失败事件
            OnGroupFailed?.Invoke(this);

            Debug.Log($"[GuideGroup] Failed: {GroupId}");
        }

        /// <summary>
        /// 重置引导组
        /// </summary>
        public void ResetGroup()
        {
            // 停止当前组
            StopGroup();

            // 重置所有引导项
            foreach (var item in m_Items)
            {
                item.ResetItem();
            }

            // 重置状态
            m_CurrentItemIndex = -1;
            m_CurrentItem = null;
            m_StartTime = 0f;
            m_Duration = 0f;

            SetState(GuideGroupState.Inactive);

            Debug.Log($"[GuideGroup] Reset: {GroupId}");
        }

        public void AddGuideItem(IGuideItem item)
        {
            AddItem(item);
        }

        public void RemoveGuideItem(string itemId)
        {
            var item = FindItem(itemId);
            if (item != null)
            {
                RemoveItem(item);
            }
        }

        #endregion

        #region 更新方法（由外部调用）

        /// <summary>
        /// 更新引导组（由GuideManager调用）
        /// </summary>
        public void Update()
        {
            if (m_State != GuideGroupState.Running) return;

            // 更新当前引导项
            if (m_CurrentItem != null)
            {
                m_CurrentItem.Update();
            }
        }

        #endregion

        #region 引导项执行逻辑

        /// <summary>
        /// 开始下一个引导项
        /// </summary>
        private void StartNextItem()
        {
            try
            {
                // 根据策略获取下一个引导项
                var nextItem = GetNextItem();

                if (nextItem == null)
                {
                    // 没有更多引导项，完成组
                    CompleteGroup();
                    return;
                }

                // 设置当前引导项
                SetCurrentItem(nextItem);

                // 开始引导项
                // 先进入等待态，不直接运行；由条件满足后自动转为 Running
                nextItem.Enter();

                Debug.Log($"[GuideGroup] Started next item: {nextItem.ItemId} in group {GroupId}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[GuideGroup] Failed to start next item in group {GroupId}: {e.Message}");
                // 如果启动失败，尝试继续下一个项
                var nextItem = GetNextItem();
                if (nextItem != null)
                {
                    StartNextItem();
                }
                else
                {
                    CompleteGroup();
                }
            }
        }

        /// <summary>
        /// 根据策略获取下一个引导项
        /// </summary>
        private IGuideItem GetNextItem()
        {
            return GetNextSequentialItem();
        }

        /// <summary>
        /// 获取下一个顺序引导项
        /// </summary>
        private IGuideItem GetNextSequentialItem()
        {
            for (int i = m_CurrentItemIndex + 1; i < m_Items.Count; i++)
            {
                var item = m_Items[i];
                if (!item.IsCompleted)
                {
                    return item;
                }
            }

            return null;
        }

        /// <summary>
        /// 获取下一个优先级引导项
        /// </summary>
        private IGuideItem GetNextPriorityItem()
        {
            var uncompletedItems = m_Items.Where(item => !item.IsCompleted).ToList();

            if (uncompletedItems.Count == 0)
            {
                return null;
            }

            // 按优先级排序（高优先级在前）
            uncompletedItems.Sort((a, b) => b.Priority.CompareTo(a.Priority));

            return uncompletedItems[0];
        }

        /// <summary>
        /// 获取下一个并行引导项
        /// </summary>
        private IGuideItem GetNextParallelItem()
        {
            // 并行策略：同时执行所有未完成的引导项
            // 这里返回第一个未完成的项，实际执行时应该同时启动所有项
            return m_Items.FirstOrDefault(item => !item.IsCompleted);
        }

        /// <summary>
        /// 设置当前引导项
        /// </summary>
        private void SetCurrentItem(IGuideItem item)
        {
            if (m_CurrentItem == item) return;

            var oldItem = m_CurrentItem;
            m_CurrentItem = item;

            // 更新当前项索引
            m_CurrentItemIndex = m_Items.IndexOf(item);

            // 触发当前项变化事件
            OnCurrentItemChanged?.Invoke(this, item);

            Debug.Log($"[GuideGroup] Current item changed: {oldItem?.ItemId} -> {item?.ItemId} in group {GroupId}");
        }

        /// <summary>
        /// 停止当前引导项
        /// </summary>
        private void StopCurrentItem()
        {
            if (m_CurrentItem == null) return;

            m_CurrentItem.CancelItem();
            m_CurrentItem = null;
            m_CurrentItemIndex = -1;
        }

        #endregion

        #region 引导项事件处理

        /// <summary>
        /// 订阅引导项事件
        /// </summary>
        private void SubscribeToItemEvents(IGuideItem item)
        {
            item.OnItemCompleted += OnItemCompleted;
            item.OnItemCancelled += OnItemCancelled;
            item.OnItemFailed += OnItemFailed;
        }

        /// <summary>
        /// 取消引导项事件订阅
        /// </summary>
        private void UnsubscribeFromItemEvents(IGuideItem item)
        {
            item.OnItemCompleted -= OnItemCompleted;
            item.OnItemCancelled -= OnItemCancelled;
            item.OnItemFailed -= OnItemFailed;
        }

        /// <summary>
        /// 引导项完成回调
        /// </summary>
        private void OnItemCompleted(IGuideItem item)
        {
            Debug.Log($"[GuideGroup] Item completed: {item.ItemId} in group {GroupId}");
            StartNextItem();
        }

        /// <summary>
        /// 引导项取消回调
        /// </summary>
        private void OnItemCancelled(IGuideItem item)
        {
            Debug.Log($"[GuideGroup] Item cancelled: {item.ItemId} in group {GroupId}");

            // 如果是当前项被取消，开始下一个项
            if (m_CurrentItem == item)
            {
                StartNextItem();
            }
        }

        /// <summary>
        /// 引导项失败回调
        /// </summary>
        private void OnItemFailed(IGuideItem item)
        {
            Debug.LogWarning($"[GuideGroup] Item failed: {item.ItemId} in group {GroupId}");

            // 如果是当前项失败，继续下一个项
            if (m_CurrentItem == item)
            {
                StartNextItem();
            }
        }

        #endregion

        #region 完成处理

        /// <summary>
        /// 完成引导组
        /// </summary>
        private void CompleteGroup()
        {
            // 记录持续时间
            m_Duration = Time.time - m_StartTime;

            // 停止当前引导项
            StopCurrentItem();

            // 更新状态
            SetState(GuideGroupState.Completed);

            // 触发完成事件
            OnGroupCompleted?.Invoke(this);

            Debug.Log($"[GuideGroup] Completed: {GroupId} (Duration: {m_Duration:F2}s)");
        }

        #endregion

        #region 排序逻辑

        /// <summary>
        /// 根据策略排序引导项
        /// </summary>
        private void SortItemsByStrategy()
        {
        }

        #endregion

        #region 状态管理

        /// <summary>
        /// 设置状态
        /// </summary>
        private void SetState(GuideGroupState newState)
        {
            if (m_State == newState) return;

            var oldState = m_State;
            m_State = newState;

            // 触发状态变化事件
            OnStateChanged?.Invoke(this);

            Debug.Log($"[GuideGroup] State changed: {GroupId} {oldState} -> {newState}");
        }

        #endregion

        #region 清理和销毁

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Dispose()
        {
            // 停止组
            StopGroup();

            // 清理所有引导项
            foreach (var item in m_Items)
            {
                UnsubscribeFromItemEvents(item);
                item.Dispose();
            }

            m_Items.Clear();
            m_CurrentItem = null;
            m_CurrentItemIndex = -1;

            Debug.Log($"[GuideGroup] Disposed: {GroupId}");
        }

        #endregion

        #region 重写方法

        public override string ToString()
        {
            return
                $"[GuideGroup] {GroupId}: {GroupName} (State: {State}, Items: {m_Items.Count}, Current: {m_CurrentItem?.ItemId})";
        }

        #endregion
    }
}