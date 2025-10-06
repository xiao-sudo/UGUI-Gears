using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GameGuide.Conditions;

namespace GameGuide.Core
{
    /// <summary>
    /// Guide manager - MonoBehaviour singleton that manages all guide groups
    /// </summary>
    public class GuideManager : MonoBehaviour, IGuideManager
    {
        #region Singleton

        private static GuideManager S_INSTANCE;

        public static GuideManager Instance
        {
            get
            {
                if (S_INSTANCE == null)
                {
                    S_INSTANCE = FindObjectOfType<GuideManager>();
                    if (S_INSTANCE == null)
                    {
                        var go = new GameObject("GuideManager");
                        S_INSTANCE = go.AddComponent<GuideManager>();
                        DontDestroyOnLoad(go);
                    }
                }

                return S_INSTANCE;
            }
        }

        #endregion

        #region Serialized Fields

        [SerializeField]
        private bool m_EnableDebugLog = true;

        [SerializeField]
        private bool m_AutoSave = true;

        [SerializeField]
        private float m_SaveInterval = 5f;

        [SerializeField]
        private bool m_PauseOnApplicationPause = true;

        [SerializeField]
        private bool m_ResumeOnApplicationFocus = true;

        #endregion

        #region Private Fields

        private Dictionary<string, IGuideGroup> m_Groups = new Dictionary<string, IGuideGroup>();
        private List<IGuideGroup> m_ActiveGroups = new List<IGuideGroup>();
        private List<IGuideGroup> m_CompletedGroups = new List<IGuideGroup>();
        private bool m_IsPaused = false;
        private bool m_IsInitialized = false;
        private float m_LastSaveTime = 0f;
        private GuideSaveData m_SaveData = new GuideSaveData();

        #endregion

        #region Public Properties

        public IReadOnlyDictionary<string, IGuideGroup> Groups => m_Groups;
        public IReadOnlyList<IGuideGroup> ActiveGroups => m_ActiveGroups.AsReadOnly();
        public IReadOnlyList<IGuideGroup> CompletedGroups => m_CompletedGroups.AsReadOnly();
        public int TotalGroups => m_Groups.Count;
        public int ActiveGroupsCount => m_ActiveGroups.Count;
        public int CompletedGroupsCount => m_CompletedGroups.Count;
        public bool IsPaused => m_IsPaused;
        public bool IsInitialized => m_IsInitialized;

        #endregion

        #region Events

        public event Action<IGuideGroup> OnGroupRegistered;
        public event Action<IGuideGroup> OnGroupUnregistered;

        public void ClearGuideProgress()
        {
            // Stop all guide groups
            StopAllGuides();
            
            // Clear all data
            m_ActiveGroups.Clear();
            m_CompletedGroups.Clear();
            
            if (m_EnableDebugLog)
            {
                Debug.Log("[GuideManager] Cleared all guide progress");
            }
        }

        public event Action<IGuideGroup> OnGroupStarted;
        public event Action<IGuideGroup> OnGroupCompleted;
        public event Action<IGuideGroup> OnGroupCancelled;
        public event Action<IGuideGroup> OnGroupFailed;
        public event Action<IGuideGroup> OnGroupPaused;
        public event Action<IGuideGroup> OnGroupResumed;
        public event Action OnAllGuidesPaused;
        public event Action OnAllGuidesResumed;
        public event Action OnAllGuidesCancelled;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (S_INSTANCE == null)
            {
                S_INSTANCE = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else if (S_INSTANCE != this)
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // Load saved data
            LoadSaveData();
        }

        private void Update()
        {
            if (!m_IsInitialized || m_IsPaused) return;

            // Update all active guide groups
            UpdateActiveGroups();

            // Auto-save
            if (m_AutoSave && Time.time - m_LastSaveTime >= m_SaveInterval)
            {
                SaveData();
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (m_PauseOnApplicationPause)
            {
                if (pauseStatus)
                {
                    PauseAllGuides();
                }
                else
                {
                    ResumeAllGuides();
                }
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (m_ResumeOnApplicationFocus)
            {
                if (hasFocus)
                {
                    ResumeAllGuides();
                }
            }
        }

        private void OnDestroy()
        {
            // Save data
            SaveData();

            // Cleanup all guide groups
            ClearAllGroups();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the guide manager
        /// </summary>
        private void Initialize()
        {
            if (m_IsInitialized) return;

            // Initialize condition manager
            if (GuideConditionManager.Instance == null)
            {
                Debug.LogError("[GuideManager] GuideConditionManager not found!");
                return;
            }

            m_IsInitialized = true;

            if (m_EnableDebugLog)
            {
                Debug.Log("[GuideManager] Initialized");
            }
        }

        #endregion

        #region Guide Group Management

        /// <summary>
        /// Register a guide group
        /// </summary>
        public void RegisterGroup(IGuideGroup group)
        {
            if (group == null)
            {
                Debug.LogWarning("[GuideManager] Cannot register null group");
                return;
            }

            if (m_Groups.ContainsKey(group.GroupId))
            {
                Debug.LogWarning($"[GuideManager] Group {group.GroupId} already registered");
                return;
            }

            // Initialize guide group
            group.Initialize();

            // Register to dictionary
            m_Groups[group.GroupId] = group;

            // Subscribe to group events
            SubscribeToGroupEvents(group);

            // Raise registered event
            OnGroupRegistered?.Invoke(group);

            if (m_EnableDebugLog)
            {
                Debug.Log($"[GuideManager] Registered group: {group.GroupId}");
            }
        }

        /// <summary>
        /// Unregister a guide group
        /// </summary>
        public void UnregisterGroup(IGuideGroup group)
        {
            if (group == null) return;

            if (!m_Groups.ContainsKey(group.GroupId))
            {
                Debug.LogWarning($"[GuideManager] Group {group.GroupId} not registered");
                return;
            }

            // Stop the group
            group.StopGroup();

            // Remove from active list
            m_ActiveGroups.Remove(group);
            m_CompletedGroups.Remove(group);

            // Unsubscribe from group events
            UnsubscribeFromGroupEvents(group);

            // Remove from dictionary
            m_Groups.Remove(group.GroupId);

            // Raise unregistered event
            OnGroupUnregistered?.Invoke(group);

            if (m_EnableDebugLog)
            {
                Debug.Log($"[GuideManager] Unregistered group: {group.GroupId}");
            }
        }

        /// <summary>
        /// Get a guide group by ID
        /// </summary>
        public IGuideGroup GetGroup(string groupId)
        {
            m_Groups.TryGetValue(groupId, out var group);
            return group;
        }

        /// <summary>
        /// Check whether a guide group is registered
        /// </summary>
        public bool IsGroupRegistered(string groupId)
        {
            return m_Groups.ContainsKey(groupId);
        }

        /// <summary>
        /// Clear all guide groups
        /// </summary>
        public void ClearAllGroups()
        {
            // Stop all groups
            StopAllGuides();

            // Cleanup all groups
            foreach (var group in m_Groups.Values)
            {
                UnsubscribeFromGroupEvents(group);
                group.Dispose();
            }

            m_Groups.Clear();
            m_ActiveGroups.Clear();
            m_CompletedGroups.Clear();

            if (m_EnableDebugLog)
            {
                Debug.Log("[GuideManager] Cleared all groups");
            }
        }

        #endregion

        #region Guide Group Control

        /// <summary>
        /// Start a guide group
        /// </summary>
        public void StartGroup(string groupId)
        {
            var group = GetGroup(groupId);
            if (group == null)
            {
                Debug.LogWarning($"[GuideManager] Group {groupId} not found");
                return;
            }

            StartGroup(group);
        }

        /// <summary>
        /// Start a guide group
        /// </summary>
        public void StartGroup(IGuideGroup group)
        {
            if (group == null)
            {
                Debug.LogWarning("[GuideManager] Cannot start null group");
                return;
            }

            if (!m_Groups.ContainsKey(group.GroupId))
            {
                Debug.LogWarning($"[GuideManager] Group {group.GroupId} not registered");
                return;
            }

            group.StartGroup();
        }

        /// <summary>
        /// Stop a guide group
        /// </summary>
        public void StopGroup(string groupId)
        {
            var group = GetGroup(groupId);
            if (group == null)
            {
                Debug.LogWarning($"[GuideManager] Group {groupId} not found");
                return;
            }

            StopGroup(group);
        }

        /// <summary>
        /// Stop a guide group
        /// </summary>
        public void StopGroup(IGuideGroup group)
        {
            if (group == null) return;

            group.StopGroup();
        }

        /// <summary>
        /// Pause a guide group
        /// </summary>
        public void PauseGroup(string groupId)
        {
            var group = GetGroup(groupId);
            if (group == null)
            {
                Debug.LogWarning($"[GuideManager] Group {groupId} not found");
                return;
            }

            PauseGroup(group);
        }

        /// <summary>
        /// Pause a guide group
        /// </summary>
        public void PauseGroup(IGuideGroup group)
        {
            if (group == null) return;

            group.PauseGroup();
        }

        /// <summary>
        /// Resume a guide group
        /// </summary>
        public void ResumeGroup(string groupId)
        {
            var group = GetGroup(groupId);
            if (group == null)
            {
                Debug.LogWarning($"[GuideManager] Group {groupId} not found");
                return;
            }

            ResumeGroup(group);
        }

        /// <summary>
        /// Resume a guide group
        /// </summary>
        public void ResumeGroup(IGuideGroup group)
        {
            if (group == null) return;

            group.ResumeGroup();
        }

        /// <summary>
        /// Reset a guide group
        /// </summary>
        public void ResetGroup(string groupId)
        {
            var group = GetGroup(groupId);
            if (group == null)
            {
                Debug.LogWarning($"[GuideManager] Group {groupId} not found");
                return;
            }

            ResetGroup(group);
        }

        /// <summary>
        /// Reset a guide group
        /// </summary>
        public void ResetGroup(IGuideGroup group)
        {
            if (group == null) return;

            group.ResetGroup();
        }

        #endregion

        #region Global Control

        public void RegisterGuideGroup(IGuideGroup group)
        {
            RegisterGroup(group);
        }

        public void UnregisterGuideGroup(string groupId)
        {
            var group = GetGroup(groupId);
            if (group != null)
            {
                UnregisterGroup(group);
            }
        }

        public IGuideGroup GetGuideGroup(string groupId)
        {
            return GetGroup(groupId);
        }

        public List<IGuideGroup> GetAllGuideGroups()
        {
            return m_Groups.Values.ToList();
        }

        public List<IGuideGroup> GetActiveGuideGroups()
        {
            return m_ActiveGroups.ToList();
        }

        public void StartGuideGroup(string groupId)
        {
            StartGroup(groupId);
        }

        public void StopGuideGroup(string groupId)
        {
            StopGroup(groupId);
        }

        /// <summary>
        /// Pause all guides
        /// </summary>
        public void PauseAllGuides()
        {
            if (m_IsPaused) return;

            m_IsPaused = true;

            // Pause all active guide groups
            foreach (var group in m_ActiveGroups.ToList())
            {
                group.PauseGroup();
            }

            // Raise paused event
            OnAllGuidesPaused?.Invoke();

            if (m_EnableDebugLog)
            {
                Debug.Log("[GuideManager] Paused all guides");
            }
        }

        /// <summary>
        /// Resume all guides
        /// </summary>
        public void ResumeAllGuides()
        {
            if (!m_IsPaused) return;

            m_IsPaused = false;

            // Resume all active guide groups
            foreach (var group in m_ActiveGroups.ToList())
            {
                group.ResumeGroup();
            }

            // Raise resumed event
            OnAllGuidesResumed?.Invoke();

            if (m_EnableDebugLog)
            {
                Debug.Log("[GuideManager] Resumed all guides");
            }
        }

        /// <summary>
        /// Stop all guides (cancel)
        /// </summary>
        public void StopAllGuides()
        {
            // Stop all guide groups
            foreach (var group in m_Groups.Values)
            {
                group.StopGroup();
            }

            // Raise cancelled event
            OnAllGuidesCancelled?.Invoke();

            if (m_EnableDebugLog)
            {
                Debug.Log("[GuideManager] Cancelled all guides");
            }
        }

        /// <summary>
        /// Fail all guides (system exception)
        /// </summary>
        public void FailAllGuides()
        {
            // Fail all guide groups
            foreach (var group in m_Groups.Values)
            {
                group.FailGroup();
            }

            if (m_EnableDebugLog)
            {
                Debug.Log("[GuideManager] Failed all guides");
            }
        }

        public bool IsGuideActive(string groupId)
        {
            var group = GetGroup(groupId);
            return group != null && group.IsRunning;
        }

        public GuideGroupState GetGroupState(string groupId)
        {
            var group = GetGroup(groupId);
            return group?.State ?? GuideGroupState.Inactive;
        }

        public bool HasGuideGroup(string groupId)
        {
            return IsGroupRegistered(groupId);
        }

        public void SaveGuideProgress()
        {
            SaveData();
        }

        public void LoadGuideProgress()
        {
            LoadSaveData();
        }

        #endregion

        #region State Queries

        /// <summary>
        /// Check if any guide is active
        /// </summary>
        public bool HasActiveGuides()
        {
            return m_ActiveGroups.Count > 0;
        }

        /// <summary>
        /// Check if a guide group is completed
        /// </summary>
        public bool IsGroupCompleted(string groupId)
        {
            var group = GetGroup(groupId);
            return group != null && group.IsCompleted;
        }

        /// <summary>
        /// Get all active guide group IDs
        /// </summary>
        public string[] GetActiveGroupIds()
        {
            return m_ActiveGroups.Select(g => g.GroupId).ToArray();
        }

        /// <summary>
        /// Get all completed guide group IDs
        /// </summary>
        public string[] GetCompletedGroupIds()
        {
            return m_CompletedGroups.Select(g => g.GroupId).ToArray();
        }

        #endregion

        #region Update Logic

        /// <summary>
        /// Update all active guide groups
        /// </summary>
        private void UpdateActiveGroups()
        {
            // Update all active guide groups
            foreach (var group in m_ActiveGroups.ToList())
            {
                group.Update();
            }
        }

        #endregion

        #region Event Handling

        /// <summary>
        /// Subscribe to guide group events
        /// </summary>
        private void SubscribeToGroupEvents(IGuideGroup group)
        {
            group.OnGroupStarted += OnEvtGroupStarted;
            group.OnGroupCompleted += OnEvtGroupCompleted;
            group.OnGroupPaused += OnEvtGroupPaused;
            group.OnGroupResumed += OnEvtGroupResumed;
            group.OnGroupCancelled += OnEvtGroupCancelled;
            group.OnGroupFailed += OnEvtGroupFailed;
        }

        /// <summary>
        /// Unsubscribe from guide group events
        /// </summary>
        private void UnsubscribeFromGroupEvents(IGuideGroup group)
        {
            group.OnGroupStarted -= OnEvtGroupStarted;
            group.OnGroupCompleted -= OnEvtGroupCompleted;
            group.OnGroupPaused -= OnEvtGroupPaused;
            group.OnGroupResumed -= OnEvtGroupResumed;
            group.OnGroupCancelled -= OnEvtGroupCancelled;
            group.OnGroupFailed -= OnEvtGroupFailed;
        }

        /// <summary>
        /// Guide group started callback
        /// </summary>
        private void OnEvtGroupStarted(IGuideGroup group)
        {
            // Add to active list
            if (!m_ActiveGroups.Contains(group))
            {
                m_ActiveGroups.Add(group);
            }

            // Remove from completed list
            m_CompletedGroups.Remove(group);

            // Raise event
            OnGroupStarted?.Invoke(group);

            if (m_EnableDebugLog)
            {
                Debug.Log($"[GuideManager] Group started: {group.GroupId}");
            }
        }

        /// <summary>
        /// Guide group completed callback
        /// </summary>
        private void OnEvtGroupCompleted(IGuideGroup group)
        {
            // Remove from active list
            m_ActiveGroups.Remove(group);

            // Add to completed list
            if (!m_CompletedGroups.Contains(group))
            {
                m_CompletedGroups.Add(group);
            }

            // Raise event
            OnGroupCompleted?.Invoke(group);

            if (m_EnableDebugLog)
            {
                Debug.Log($"[GuideManager] Group completed: {group.GroupId}");
            }
        }

        /// <summary>
        /// Guide group paused callback
        /// </summary>
        private void OnEvtGroupPaused(IGuideGroup group)
        {
            // Raise event
            OnGroupPaused?.Invoke(group);

            if (m_EnableDebugLog)
            {
                Debug.Log($"[GuideManager] Group paused: {group.GroupId}");
            }
        }

        /// <summary>
        /// Guide group resumed callback
        /// </summary>
        private void OnEvtGroupResumed(IGuideGroup group)
        {
            // Raise event
            OnGroupResumed?.Invoke(group);

            if (m_EnableDebugLog)
            {
                Debug.Log($"[GuideManager] Group resumed: {group.GroupId}");
            }
        }

        /// <summary>
        /// Guide group cancelled callback
        /// </summary>
        private void OnEvtGroupCancelled(IGuideGroup group)
        {
            // Remove from active list
            m_ActiveGroups.Remove(group);

            // Raise event
            OnGroupCancelled?.Invoke(group);

            if (m_EnableDebugLog)
            {
                Debug.Log($"[GuideManager] Group cancelled: {group.GroupId}");
            }
        }

        /// <summary>
        /// Guide group failed callback
        /// </summary>
        private void OnEvtGroupFailed(IGuideGroup group)
        {
            // Remove from active list
            m_ActiveGroups.Remove(group);

            // Raise event
            OnGroupFailed?.Invoke(group);

            if (m_EnableDebugLog)
            {
                Debug.Log($"[GuideManager] Group failed: {group.GroupId}");
            }
        }

        #endregion

        #region Data Persistence

        /// <summary>
        /// Save data
        /// </summary>
        public void SaveData()
        {
            try
            {
                // Collect data to save
                m_SaveData.Clear();

                foreach (var group in m_Groups.Values)
                {
                    var groupData = new GuideGroupSaveData
                    {
                        GroupId = group.GroupId,
                        State = group.State.ToString(),
                        CurrentItemIndex = group.CurrentItemIndex,
                    };

                    m_SaveData.Groups.Add(groupData);
                }

                // Save to PlayerPrefs
                var json = JsonUtility.ToJson(m_SaveData);
                PlayerPrefs.SetString("GuideSaveData", json);
                PlayerPrefs.Save();

                m_LastSaveTime = Time.time;

                if (m_EnableDebugLog)
                {
                    Debug.Log("[GuideManager] Data saved");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[GuideManager] Failed to save data: {e.Message}");
            }
        }

        /// <summary>
        /// Load data
        /// </summary>
        public void LoadSaveData()
        {
            try
            {
                if (!PlayerPrefs.HasKey("GuideSaveData"))
                {
                    if (m_EnableDebugLog)
                    {
                        Debug.Log("[GuideManager] No save data found");
                    }

                    return;
                }

                var json = PlayerPrefs.GetString("GuideSaveData");
                m_SaveData = JsonUtility.FromJson<GuideSaveData>(json);

                if (m_SaveData == null)
                {
                    m_SaveData = new GuideSaveData();
                    return;
                }

                // Restore group states
                foreach (var groupData in m_SaveData.Groups)
                {
                    var group = GetGroup(groupData.GroupId);
                    if (group != null)
                    {
                        // State restoration can be implemented as needed.
                        // Since GuideGroup is now a pure C# class, the restoration logic should be redesigned.
                    }
                }

                if (m_EnableDebugLog)
                {
                    Debug.Log("[GuideManager] Data loaded");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[GuideManager] Failed to load data: {e.Message}");
                m_SaveData = new GuideSaveData();
            }
        }

        #endregion

        #region Debugging & Logging

        /// <summary>
        /// Enable/disable debug logging
        /// </summary>
        public void SetDebugLogEnabled(bool enabled)
        {
            m_EnableDebugLog = enabled;
        }

        /// <summary>
        /// Print current status
        /// </summary>
        [ContextMenu("Print Status")]
        public void PrintStatus()
        {
            Debug.Log($"[GuideManager] Status:\n" +
                      $"Total Groups: {TotalGroups}\n" +
                      $"Active Groups: {ActiveGroupsCount}\n" +
                      $"Completed Groups: {CompletedGroupsCount}\n" +
                      $"Is Paused: {IsPaused}");
        }

        #endregion
    }

    #region Save Data Structures

    [Serializable]
    public class GuideSaveData
    {
        public List<GuideGroupSaveData> Groups = new List<GuideGroupSaveData>();

        public void Clear()
        {
            Groups.Clear();
        }
    }

    [Serializable]
    public class GuideGroupSaveData
    {
        public string GroupId;
        public string State;
        public int CurrentItemIndex;
    }

    #endregion
}