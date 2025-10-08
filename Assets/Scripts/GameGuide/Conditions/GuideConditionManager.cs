using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GameGuide.Conditions
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
        private List<IGuideCondition> m_ConditionsToCheck; // Conditions that need state checking
        private List<IGuideCondition> m_ConditionsWithTimeout; // Conditions with timeout settings
        private bool m_IsInitialized;

        // Time control for Update method
        private float m_ElapsedTime = 0f;
        private bool m_IsCheckingEnabled = true;

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
            m_ConditionsToCheck = new List<IGuideCondition>(); // Conditions that need state checking
            m_ConditionsWithTimeout = new List<IGuideCondition>(); // Conditions with timeout settings
            m_IsInitialized = true;

            LogDebug("GuideConditionManager initialized");
        }

        private void Update()
        {
            // Early exit for performance optimization
            if (!m_IsCheckingEnabled) return;

            m_ElapsedTime += Time.deltaTime;
            if (m_ElapsedTime >= m_CheckInterval)
            {
                m_ElapsedTime = 0f;

                // Handle state checking and timeout checking separately
                CheckConditionStates();
                CheckTimeoutConditions();
            }
        }

        private void OnDestroy()
        {
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

            // Set registration time for timeout calculation
            condition.RegistrationTime = Time.time;

            m_ActiveConditions[condition.ConditionId] = condition;

            // Subscribe to condition changes for auto cleanup
            condition.OnConditionChanged += OnConditionStateChanged;


            // If condition needs periodic state checking, add to state check list
            if (ShouldCheckCondition(condition))
            {
                if (!m_ConditionsToCheck.Contains(condition))
                {
                    m_ConditionsToCheck.Add(condition);
                }
            }

            // If condition has timeout, add to timeout check list
            if (HasTimeout(condition))
            {
                if (!m_ConditionsWithTimeout.Contains(condition))
                {
                    m_ConditionsWithTimeout.Add(condition);
                }
            }

            LogDebug($"Registered condition: {condition.ConditionId} with strategy: {condition.CleanupStrategy}");

            // Start listening to the condition
            condition.StartListening();
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
                m_ConditionsWithTimeout.Remove(condition);

                // Unsubscribe from condition changes
                condition.OnConditionChanged -= OnConditionStateChanged;

                // Stop listening
                if (condition.IsListening)
                {
                    condition.StopListening();
                }

                LogDebug($"Unregistered condition: {condition.ConditionId}");
            }
        }

        /// <summary>
        /// Enable/disable condition checking
        /// </summary>
        public void SetCheckingEnabled(bool checkEnable)
        {
            m_IsCheckingEnabled = checkEnable;
            LogDebug($"Condition checking {(checkEnable ? "enabled" : "disabled")}");
        }

        /// <summary>
        /// Force check all conditions immediately
        /// </summary>
        public void ForceCheckAllConditions()
        {
            CheckConditionStates();
            CheckTimeoutConditions();
        }

        /// <summary>
        /// Check condition states (for conditions that need periodic state checking)
        /// </summary>
        private void CheckConditionStates()
        {
            if (m_ConditionsToCheck == null || m_ConditionsToCheck.Count == 0) return;

            for (int i = m_ConditionsToCheck.Count - 1; i >= 0; i--)
            {
                var condition = m_ConditionsToCheck[i];

                if (condition == null)
                {
                    m_ConditionsToCheck.RemoveAt(i);
                    continue;
                }

                // Check specific condition types that need state monitoring
                CheckSpecificCondition(condition);
            }
        }

        private void CheckSpecificCondition(IGuideCondition condition)
        {
            // Use the condition's own state checking method
            condition.PerformStateCheck();
        }

        /// <summary>
        /// Check timeout conditions (for conditions with timeout settings)
        /// </summary>
        private void CheckTimeoutConditions()
        {
            if (m_ConditionsWithTimeout == null || m_ConditionsWithTimeout.Count == 0) return;

            var currentTime = Time.time;
            var conditionsToRemove = new List<IGuideCondition>();

            for (int i = m_ConditionsWithTimeout.Count - 1; i >= 0; i--)
            {
                var condition = m_ConditionsWithTimeout[i];

                if (condition == null)
                {
                    m_ConditionsWithTimeout.RemoveAt(i);
                    continue;
                }

                // Check if condition should time out
                if ((condition.CleanupStrategy == ConditionCleanupStrategy.AutoOnTimeout ||
                     condition.CleanupStrategy == ConditionCleanupStrategy.AutoOnSatisfiedOrTimeout) &&
                    condition.TimeoutSeconds > 0f)
                {
                    var elapsedTime = currentTime - condition.RegistrationTime;
                    if (elapsedTime >= condition.TimeoutSeconds)
                    {
                        LogDebug(
                            $"Auto cleaning up timeout condition: {condition.ConditionId} (elapsed: {elapsedTime:F2}s)");
                        conditionsToRemove.Add(condition);
                    }
                }
            }

            // Batch remove to avoid modifying collection during iteration
            foreach (var condition in conditionsToRemove)
            {
                UnregisterCondition(condition);
            }
        }

        private void OnConditionStateChanged(IGuideCondition condition)
        {
            if (condition == null) return;

            if (condition.IsSatisfied())
            {
                if (0 != (condition.CleanupStrategy & ConditionCleanupStrategy.AutoOnSatisfied))
                {
                    LogDebug($"Auto cleaning up satisfied condition: {condition.ConditionId}");
                    UnregisterCondition(condition);
                }
            }
        }

        /// <summary>
        /// Check if condition needs periodic state checking
        /// </summary>
        private bool ShouldCheckCondition(IGuideCondition condition)
        {
            return condition.NeedsStateChecking;
        }

        /// <summary>
        /// Check if condition has timeout settings
        /// </summary>
        private bool HasTimeout(IGuideCondition condition)
        {
            return 0 != (condition.CleanupStrategy & ConditionCleanupStrategy.AutoOnTimeout) &&
                   condition.TimeoutSeconds > 0f;
        }

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
            m_ConditionsWithTimeout.Clear();

            LogDebug("Cleared all conditions");
        }

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

        public bool HasCondition(string conditionId)
        {
            return !string.IsNullOrEmpty(conditionId) && m_ActiveConditions.ContainsKey(conditionId);
        }

        public IGuideCondition GetCondition(string conditionId)
        {
            if (string.IsNullOrEmpty(conditionId)) return null;
            m_ActiveConditions.TryGetValue(conditionId, out IGuideCondition condition);
            return condition;
        }

        private void LogDebug(string message)
        {
            if (m_EnableDebugLog)
            {
                Debug.Log($"[GuideConditionManager] {message}");
            }
        }
    }
}