using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace UIExt.Guide.Conditions
{
    /// <summary>
    /// Guide condition manager
    /// </summary>
    public class GuideConditionManager : MonoBehaviour
    {
        private static GuideConditionManager INSTANCE = null;
        public static GuideConditionManager Instance => INSTANCE;

        [SerializeField]
        private float m_CheckInterval = 0.1f; // Check interval

        [SerializeField]
        private bool m_EnableDebugLog = false;

        private Dictionary<string, IGuideCondition> m_ActiveConditions;
        private List<IGuideCondition> m_ConditionsToCheck;
        private Coroutine m_CheckCoroutine;
        private bool m_IsInitialized;

        /// <summary>
        /// Check interval
        /// </summary>
        public float CheckInterval
        {
            get => m_CheckInterval;
            set => m_CheckInterval = Mathf.Max(0.01f, value);
        }

        /// <summary>
        /// Whether to enable debug logging
        /// </summary>
        public bool EnableDebugLog
        {
            get => m_EnableDebugLog;
            set => m_EnableDebugLog = value;
        }

        /// <summary>
        /// Number of active conditions
        /// </summary>
        public int ActiveConditionCount => m_ActiveConditions?.Count ?? 0;

        private void Awake()
        {
            if (INSTANCE == null)
            {
                INSTANCE = this;
                Initialize();
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Initialize()
        {
            if (m_IsInitialized) return;

            m_ActiveConditions = new Dictionary<string, IGuideCondition>();
            m_ConditionsToCheck = new List<IGuideCondition>();
            m_IsInitialized = true;

            LogDebug("GuideConditionManager initialized");
        }

        private void Start()
        {
            StartConditionChecking();
        }

        private void OnDestroy()
        {
            StopConditionChecking();
            ClearAllConditions();
        }

        /// <summary>
        /// Register condition
        /// </summary>
        public void RegisterCondition(IGuideCondition condition)
        {
            if (condition == null || string.IsNullOrEmpty(condition.ConditionId)) return;

            Initialize();

            if (m_ActiveConditions.ContainsKey(condition.ConditionId))
            {
                LogDebug($"Condition {condition.ConditionId} already registered");
                return;
            }

            m_ActiveConditions[condition.ConditionId] = condition;

            // If condition needs periodic checking, add to check list
            if (ShouldCheckCondition(condition))
            {
                if (!m_ConditionsToCheck.Contains(condition))
                {
                    m_ConditionsToCheck.Add(condition);
                }
            }

            LogDebug($"Registered condition: {condition.ConditionId}");
        }

        /// <summary>
        /// Unregister condition
        /// </summary>
        public void UnregisterCondition(IGuideCondition condition)
        {
            if (condition == null || string.IsNullOrEmpty(condition.ConditionId)) return;

            if (m_ActiveConditions.Remove(condition.ConditionId))
            {
                m_ConditionsToCheck.Remove(condition);

                // Stop listening
                if (condition.IsListening)
                {
                    condition.StopListening();
                }

                LogDebug($"Unregistered condition: {condition.ConditionId}");
            }
        }

        /// <summary>
        /// Unregister condition by ID
        /// </summary>
        public void UnregisterCondition(string conditionId)
        {
            if (string.IsNullOrEmpty(conditionId)) return;

            if (m_ActiveConditions.TryGetValue(conditionId, out IGuideCondition condition))
            {
                UnregisterCondition(condition);
            }
        }

        /// <summary>
        /// Get condition
        /// </summary>
        public IGuideCondition GetCondition(string conditionId)
        {
            if (string.IsNullOrEmpty(conditionId)) return null;

            m_ActiveConditions.TryGetValue(conditionId, out IGuideCondition condition);
            return condition;
        }

        /// <summary>
        /// Check if condition exists
        /// </summary>
        public bool HasCondition(string conditionId)
        {
            return !string.IsNullOrEmpty(conditionId) && m_ActiveConditions.ContainsKey(conditionId);
        }

        /// <summary>
        /// Clear all conditions
        /// </summary>
        public void ClearAllConditions()
        {
            if (m_ActiveConditions == null) return;

            foreach (var condition in m_ActiveConditions.Values)
            {
                if (condition.IsListening)
                {
                    condition.StopListening();
                }
            }

            m_ActiveConditions.Clear();
            m_ConditionsToCheck.Clear();

            LogDebug("Cleared all conditions");
        }

        /// <summary>
        /// Start condition checking
        /// </summary>
        public void StartConditionChecking()
        {
            if (m_CheckCoroutine != null) return;

            m_CheckCoroutine = StartCoroutine(ConditionCheckCoroutine());
            LogDebug("Started condition checking");
        }

        /// <summary>
        /// Stop condition checking
        /// </summary>
        public void StopConditionChecking()
        {
            if (m_CheckCoroutine != null)
            {
                StopCoroutine(m_CheckCoroutine);
                m_CheckCoroutine = null;
                LogDebug("Stopped condition checking");
            }
        }

        private IEnumerator ConditionCheckCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(m_CheckInterval);

                CheckAllConditions();
            }
        }

        private void CheckAllConditions()
        {
            if (m_ConditionsToCheck == null) return;

            for (int i = m_ConditionsToCheck.Count - 1; i >= 0; i--)
            {
                var condition = m_ConditionsToCheck[i];

                if (condition == null)
                {
                    m_ConditionsToCheck.RemoveAt(i);
                    continue;
                }

                // Check specific condition types
                CheckSpecificCondition(condition);
            }
        }

        private void CheckSpecificCondition(IGuideCondition condition)
        {
            switch (condition)
            {
                case UIConditions.UIStateCondition uiStateCondition:
                    uiStateCondition.CheckStateChange();
                    break;

                // Can add more condition types that need periodic checking
            }
        }

        private bool ShouldCheckCondition(IGuideCondition condition)
        {
            // Determine if condition needs periodic checking
            return condition is UIConditions.UIStateCondition;
        }

        /// <summary>
        /// Force check all active conditions
        /// </summary>
        public void ForceCheckAllConditions()
        {
            if (m_ActiveConditions == null) return;

            foreach (var condition in m_ActiveConditions.Values)
            {
                CheckSpecificCondition(condition);
            }
        }

        /// <summary>
        /// Get descriptions of all active conditions
        /// </summary>
        public List<string> GetAllConditionDescriptions()
        {
            if (m_ActiveConditions == null) return new List<string>();

            return m_ActiveConditions.Values
                .Where(c => c != null)
                .Select(c => c.ToString())
                .ToList();
        }

        /// <summary>
        /// Get list of satisfied conditions
        /// </summary>
        public List<IGuideCondition> GetSatisfiedConditions()
        {
            if (m_ActiveConditions == null) return new List<IGuideCondition>();

            return m_ActiveConditions.Values
                .Where(c => c != null && c.IsSatisfied())
                .ToList();
        }

        /// <summary>
        /// Get list of unsatisfied conditions
        /// </summary>
        public List<IGuideCondition> GetUnsatisfiedConditions()
        {
            if (m_ActiveConditions == null) return new List<IGuideCondition>();

            return m_ActiveConditions.Values
                .Where(c => c != null && !c.IsSatisfied())
                .ToList();
        }

        private void LogDebug(string message)
        {
            if (m_EnableDebugLog)
            {
                Debug.Log($"[GuideConditionManager] {message}");
            }
        }

        /// <summary>
        /// Create condition builder (for chaining)
        /// </summary>
        public static ConditionBuilder CreateBuilder()
        {
            return new ConditionBuilder();
        }
    }

    /// <summary>
    /// Condition builder
    /// </summary>
    public class ConditionBuilder
    {
        private List<IGuideCondition> conditions;

        public ConditionBuilder()
        {
            conditions = new List<IGuideCondition>();
        }

        /// <summary>
        /// Add condition
        /// </summary>
        public ConditionBuilder AddCondition(IGuideCondition condition)
        {
            if (condition != null)
            {
                conditions.Add(condition);
            }

            return this;
        }

        /// <summary>
        /// Create AND composite condition
        /// </summary>
        public CompositeConditions.CompositeCondition BuildAndCondition()
        {
            return new CompositeConditions.CompositeCondition(CompositeConditions.CompositeLogicType.AND,
                conditions.ToArray());
        }

        /// <summary>
        /// Create OR composite condition
        /// </summary>
        public CompositeConditions.CompositeCondition BuildOrCondition()
        {
            return new CompositeConditions.CompositeCondition(CompositeConditions.CompositeLogicType.OR,
                conditions.ToArray());
        }

        /// <summary>
        /// Create XOR composite condition
        /// </summary>
        public CompositeConditions.CompositeCondition BuildXorCondition()
        {
            return new CompositeConditions.CompositeCondition(CompositeConditions.CompositeLogicType.XOR,
                conditions.ToArray());
        }

        /// <summary>
        /// Get condition列表
        /// </summary>
        public List<IGuideCondition> GetConditions()
        {
            return new List<IGuideCondition>(conditions);
        }
    }
}