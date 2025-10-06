using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GameGuide.Conditions;

namespace GameGuide.Core
{
    /// <summary>
    /// Guide group implementation - pure C# class, not inheriting from MonoBehaviour
    /// </summary>
    [Serializable]
    public class GuideGroup : IGuideGroup
    {
        #region Serialized Fields

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

        #region Private Fields

        private GuideGroupState m_State = GuideGroupState.Inactive;
        private List<IGuideItem> m_Items = new();
        private int m_CurrentItemIndex = -1;
        private IGuideItem m_CurrentItem;
        private float m_StartTime = 0f;
        private float m_Duration = 0f;
        private bool m_IsInitialized = false;

        #endregion

        #region Public Properties

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

        #region Events

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

        #region Constructors

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

        #region Configuration Methods (Fluent)

        /// <summary>
        /// Set group ID
        /// </summary>
        public GuideGroup SetGroupId(string groupId)
        {
            m_GroupId = groupId;
            return this;
        }

        /// <summary>
        /// Set group name
        /// </summary>
        public GuideGroup SetGroupName(string groupName)
        {
            m_GroupName = groupName;
            return this;
        }

        /// <summary>
        /// Set description
        /// </summary>
        public GuideGroup SetDescription(string description)
        {
            m_Description = description;
            return this;
        }

        /// <summary>
        /// Set whether to auto start
        /// </summary>
        public GuideGroup SetAutoStart(bool autoStart)
        {
            m_AutoStart = autoStart;
            return this;
        }

        /// <summary>
        /// Set whether to auto complete
        /// </summary>
        public GuideGroup SetAutoComplete(bool autoComplete)
        {
            m_AutoComplete = autoComplete;
            return this;
        }

        /// <summary>
        /// Set whether the group can be paused
        /// </summary>
        public GuideGroup SetCanPause(bool canPause)
        {
            m_CanPause = canPause;
            return this;
        }

        /// <summary>
        /// Set whether the group can be resumed
        /// </summary>
        public GuideGroup SetCanResume(bool canResume)
        {
            m_CanResume = canResume;
            return this;
        }

        #endregion

        #region Guide Item Management

        /// <summary>
        /// Add a guide item
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

            // Subscribe to guide item events
            SubscribeToItemEvents(item);

            Debug.Log($"[GuideGroup] Added item {item.ItemId} to group {GroupId}");
            return this;
        }

        /// <summary>
        /// Remove a guide item
        /// </summary>
        public GuideGroup RemoveItem(IGuideItem item)
        {
            if (item == null) return this;

            if (!m_Items.Contains(item))
            {
                Debug.LogWarning($"[GuideGroup] Item {item.ItemId} not found in group {GroupId}");
                return this;
            }

            // Unsubscribe from item events
            UnsubscribeFromItemEvents(item);

            // If removing the current item, stop it first
            if (m_CurrentItem == item)
            {
                StopCurrentItem();
            }

            m_Items.Remove(item);

            Debug.Log($"[GuideGroup] Removed item {item.ItemId} from group {GroupId}");
            return this;
        }

        /// <summary>
        /// Remove all guide items
        /// </summary>
        public GuideGroup ClearItems()
        {
            // Stop current item
            StopCurrentItem();

            // Unsubscribe all item events
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
        /// Find a guide item by ID
        /// </summary>
        public IGuideItem FindItem(string itemId)
        {
            return m_Items.FirstOrDefault(item => item.ItemId == itemId);
        }

        /// <summary>
        /// Get a guide item by index
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

        #region Lifecycle Control

        /// <summary>
        /// Initialize the guide group
        /// </summary>
        public void Initialize()
        {
            if (m_IsInitialized) return;

            m_IsInitialized = true;
            SetState(GuideGroupState.Waiting);

            Debug.Log($"[GuideGroup] Initialized: {GroupId} with {m_Items.Count} items");
        }

        /// <summary>
        /// Start the guide group
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

            // Record start time
            m_StartTime = Time.time;

            // Update state
            SetState(GuideGroupState.Running);

            // Start first guide item
            StartNextItem();

            // Raise started event
            OnGroupStarted?.Invoke(this);

            Debug.Log($"[GuideGroup] Started: {GroupId}");
        }

        /// <summary>
        /// Pause the guide group
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

            // Pause current guide item
            if (m_CurrentItem != null)
            {
                m_CurrentItem.PauseEffect();
            }

            // Update state
            SetState(GuideGroupState.Paused);

            // Raise paused event
            OnGroupPaused?.Invoke(this);

            Debug.Log($"[GuideGroup] Paused: {GroupId}");
        }

        /// <summary>
        /// Resume the guide group
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

            // Resume current guide item
            if (m_CurrentItem != null)
            {
                m_CurrentItem.ResumeEffect();
            }

            // Update state
            SetState(GuideGroupState.Running);

            // Raise resumed event
            OnGroupResumed?.Invoke(this);

            Debug.Log($"[GuideGroup] Resumed: {GroupId}");
        }

        /// <summary>
        /// Stop the guide group (user cancelled)
        /// </summary>
        public void StopGroup()
        {
            if (m_State == GuideGroupState.Inactive || m_State == GuideGroupState.Completed)
            {
                return;
            }

            // Stop current guide item
            StopCurrentItem();

            // Update state to Cancelled
            SetState(GuideGroupState.Cancelled);

            // Raise cancelled event
            OnGroupCancelled?.Invoke(this);

            Debug.Log($"[GuideGroup] Cancelled: {GroupId}");
        }

        /// <summary>
        /// Mark the guide group as Failed (system exception or error)
        /// </summary>
        public void FailGroup()
        {
            if (m_State == GuideGroupState.Inactive || m_State == GuideGroupState.Completed)
            {
                return;
            }

            // Stop current guide item
            StopCurrentItem();

            // Update state to Failed
            SetState(GuideGroupState.Failed);

            // Raise failed event
            OnGroupFailed?.Invoke(this);

            Debug.Log($"[GuideGroup] Failed: {GroupId}");
        }

        /// <summary>
        /// Reset the guide group
        /// </summary>
        public void ResetGroup()
        {
            // Stop current group
            StopGroup();

            // Reset all guide items
            foreach (var item in m_Items)
            {
                item.ResetItem();
            }

            // Reset state
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

        #region Update Methods (called externally)

        /// <summary>
        /// Update the guide group (called by GuideManager)
        /// </summary>
        public void Update()
        {
            if (m_State != GuideGroupState.Running) return;

            // Update current guide item
            if (m_CurrentItem != null)
            {
                m_CurrentItem.Update();
            }
        }

        #endregion

        #region Guide Item Execution Logic

        /// <summary>
        /// Start the next guide item
        /// </summary>
        private void StartNextItem()
        {
            try
            {
                // Get next item according to strategy
                var nextItem = GetNextItem();

                if (nextItem == null)
                {
                    // No more items, complete the group
                    CompleteGroup();
                    return;
                }

                // Set current item
                SetCurrentItem(nextItem);

                // Start guide item
                // Enter Waiting first; it will switch to Running automatically when conditions are met
                nextItem.Enter();

                Debug.Log($"[GuideGroup] Started next item: {nextItem.ItemId} in group {GroupId}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[GuideGroup] Failed to start next item in group {GroupId}: {e.Message}");
                // If starting fails, try to continue with the next item
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
        /// Get the next guide item based on strategy
        /// </summary>
        private IGuideItem GetNextItem()
        {
            return GetNextSequentialItem();
        }

        /// <summary>
        /// Get the next sequential guide item
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
        /// Get the next priority-based guide item
        /// </summary>
        private IGuideItem GetNextPriorityItem()
        {
            var uncompletedItems = m_Items.Where(item => !item.IsCompleted).ToList();

            if (uncompletedItems.Count == 0)
            {
                return null;
            }

            // Sort by priority (higher priority first)
            uncompletedItems.Sort((a, b) => b.Priority.CompareTo(a.Priority));

            return uncompletedItems[0];
        }

        /// <summary>
        /// Get the next parallel guide item
        /// </summary>
        private IGuideItem GetNextParallelItem()
        {
            // Parallel strategy: execute all uncompleted items simultaneously
            // Here we return the first uncompleted item; in practice all should start
            return m_Items.FirstOrDefault(item => !item.IsCompleted);
        }

        /// <summary>
        /// Set the current guide item
        /// </summary>
        private void SetCurrentItem(IGuideItem item)
        {
            if (m_CurrentItem == item) return;

            var oldItem = m_CurrentItem;
            m_CurrentItem = item;

            // Update current index
            m_CurrentItemIndex = m_Items.IndexOf(item);

            // Raise current item changed event
            OnCurrentItemChanged?.Invoke(this, item);

            Debug.Log($"[GuideGroup] Current item changed: {oldItem?.ItemId} -> {item?.ItemId} in group {GroupId}");
        }

        /// <summary>
        /// Stop the current guide item
        /// </summary>
        private void StopCurrentItem()
        {
            if (m_CurrentItem == null) return;

            m_CurrentItem.CancelItem();
            m_CurrentItem = null;
            m_CurrentItemIndex = -1;
        }

        #endregion

        #region Guide Item Event Handling

        /// <summary>
        /// Subscribe to guide item events
        /// </summary>
        private void SubscribeToItemEvents(IGuideItem item)
        {
            item.OnItemCompleted += OnItemCompleted;
            item.OnItemCancelled += OnItemCancelled;
            item.OnItemFailed += OnItemFailed;
        }

        /// <summary>
        /// Unsubscribe from guide item events
        /// </summary>
        private void UnsubscribeFromItemEvents(IGuideItem item)
        {
            item.OnItemCompleted -= OnItemCompleted;
            item.OnItemCancelled -= OnItemCancelled;
            item.OnItemFailed -= OnItemFailed;
        }

        /// <summary>
        /// Guide item completed callback
        /// </summary>
        private void OnItemCompleted(IGuideItem item)
        {
            Debug.Log($"[GuideGroup] Item completed: {item.ItemId} in group {GroupId}");
            StartNextItem();
        }

        /// <summary>
        /// Guide item cancelled callback
        /// </summary>
        private void OnItemCancelled(IGuideItem item)
        {
            Debug.Log($"[GuideGroup] Item cancelled: {item.ItemId} in group {GroupId}");

            // If the current item was cancelled, start the next one
            if (m_CurrentItem == item)
            {
                StartNextItem();
            }
        }

        /// <summary>
        /// Guide item failed callback
        /// </summary>
        private void OnItemFailed(IGuideItem item)
        {
            Debug.LogWarning($"[GuideGroup] Item failed: {item.ItemId} in group {GroupId}");

            // If the current item failed, continue with the next one
            if (m_CurrentItem == item)
            {
                StartNextItem();
            }
        }

        #endregion

        #region Completion Handling

        /// <summary>
        /// Complete the guide group
        /// </summary>
        private void CompleteGroup()
        {
            // Record duration
            m_Duration = Time.time - m_StartTime;

            // Stop current item
            StopCurrentItem();

            // Update state
            SetState(GuideGroupState.Completed);

            // Raise completed event
            OnGroupCompleted?.Invoke(this);

            Debug.Log($"[GuideGroup] Completed: {GroupId} (Duration: {m_Duration:F2}s)");
        }

        #endregion

        #region Sorting Logic

        /// <summary>
        /// Sort guide items according to strategy
        /// </summary>
        private void SortItemsByStrategy()
        {
        }

        #endregion

        #region State Management

        /// <summary>
        /// Set state
        /// </summary>
        private void SetState(GuideGroupState newState)
        {
            if (m_State == newState) return;

            var oldState = m_State;
            m_State = newState;

            // Raise state changed event
            OnStateChanged?.Invoke(this);

            Debug.Log($"[GuideGroup] State changed: {GroupId} {oldState} -> {newState}");
        }

        #endregion

        #region Cleanup and Disposal

        /// <summary>
        /// Cleanup resources
        /// </summary>
        public void Dispose()
        {
            // Stop group
            StopGroup();

            // Cleanup all guide items
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

        #region Overrides

        public override string ToString()
        {
            return
                $"[GuideGroup] {GroupId}: {GroupName} (State: {State}, Items: {m_Items.Count}, Current: {m_CurrentItem?.ItemId})";
        }

        #endregion
    }
}