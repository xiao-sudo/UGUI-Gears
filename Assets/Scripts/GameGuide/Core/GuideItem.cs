using System;
using UnityEngine;
using GameGuide.Conditions;
using GameGuide.Core.Config;

namespace GameGuide.Core
{
    /// <summary>
    /// Guide item implementation - pure C# class, not inheriting from MonoBehaviour
    /// </summary>
    [Serializable]
    public class GuideItem : IGuideItem
    {
        #region Serialized Fields

        [SerializeField]
        private string m_ItemId;

        [SerializeField]
        private string m_Description;

        [SerializeField]
        private GuideItemPriority m_Priority = GuideItemPriority.Normal;

        // Timeout configuration: waiting phase and running phase configured separately
        [SerializeField]
        private float m_WaitingTimeoutSeconds = 0f; // 0 means waiting timeout is disabled

        [SerializeField]
        private float m_RunningTimeoutSeconds = 30f; // Backward compatibility: as running timeout

        [SerializeField]
        private bool m_AutoStart = true;

        [SerializeField]
        private bool m_AutoComplete = true;

        #endregion

        #region Private Fields

        private GuideItemState m_State = GuideItemState.Inactive;
        private float m_EnterTime = 0f;
        private float m_Duration = 0f;
        private IGuideCondition m_TriggerCondition;
        private IGuideCondition m_CompletionCondition;

        private IGuideEffect m_GuideEffect;
        private IGuideEffectConfig m_GuideEffectConfig;

        private bool m_IsInitialized = false;
        private bool m_EffectIsPlaying = false;
        private float m_WaitingStartTime = 0f;
        private float m_RunningStartTime = 0f;

        #endregion

        #region Public Properties

        public string ItemId => m_ItemId;
        public string Description => m_Description;
        public GuideItemPriority Priority => m_Priority;
        public GuideItemState State => m_State;
        public float EnterTime => m_EnterTime;

        public float Duration => m_Duration;

        public float RunningTimeoutSeconds => m_RunningTimeoutSeconds;
        public float WaitingTimeoutSeconds => m_WaitingTimeoutSeconds;

        public IGuideCondition TriggerCondition => m_TriggerCondition;
        public IGuideCondition CompletionCondition => m_CompletionCondition;
        public IGuideEffect GuideEffect => m_GuideEffect;

        public bool IsActive => m_State == GuideItemState.Active;
        public bool IsCompleted => m_State == GuideItemState.Completed;
        public bool IsWaiting => m_State == GuideItemState.Waiting;

        #endregion

        #region Events

        public event Action<IGuideItem> OnItemStarted;
        public event Action<IGuideItem> OnItemCompleted;
        public event Action<IGuideItem> OnItemCancelled;
        public event Action<IGuideItem> OnItemFailed;
        public event Action<IGuideItem> OnStateChanged;

        #endregion

        #region Constructors

        public GuideItem()
        {
            m_ItemId = Guid.NewGuid().ToString();
            m_Description = string.Empty;
        }

        public GuideItem(string itemId, string description = "")
        {
            m_ItemId = string.IsNullOrEmpty(itemId) ? Guid.NewGuid().ToString() : itemId;
            m_Description = description ?? string.Empty;
        }

        #endregion

        #region Configuration Methods (Fluent)

        /// <summary>
        /// Set guide item ID
        /// </summary>
        public GuideItem SetItemId(string itemId)
        {
            m_ItemId = itemId;
            return this;
        }

        /// <summary>
        /// Set description
        /// </summary>
        public GuideItem SetDescription(string description)
        {
            m_Description = description;
            return this;
        }

        /// <summary>
        /// Set priority
        /// </summary>
        public GuideItem SetPriority(GuideItemPriority priority)
        {
            m_Priority = priority;
            return this;
        }

        /// <summary>
        /// Set timeout
        /// </summary>
        public GuideItem SetTimeout(float timeoutSeconds)
        {
            m_RunningTimeoutSeconds = Mathf.Max(0f, timeoutSeconds);
            return this;
        }

        /// <summary>
        /// Set waiting timeout (Waiting)
        /// </summary>
        public GuideItem SetWaitingTimeout(float timeoutSeconds)
        {
            m_WaitingTimeoutSeconds = Mathf.Max(0f, timeoutSeconds);
            return this;
        }

        /// <summary>
        /// Set running timeout (Running)
        /// </summary>
        public GuideItem SetRunningTimeout(float timeoutSeconds)
        {
            m_RunningTimeoutSeconds = Mathf.Max(0f, timeoutSeconds);
            return this;
        }

        /// <summary>
        /// Set whether to auto start
        /// </summary>
        public GuideItem SetAutoStart(bool autoStart)
        {
            m_AutoStart = autoStart;
            return this;
        }

        /// <summary>
        /// Set whether to auto complete
        /// </summary>
        public GuideItem SetAutoComplete(bool autoComplete)
        {
            m_AutoComplete = autoComplete;
            return this;
        }

        /// <summary>
        /// Set trigger condition
        /// </summary>
        public GuideItem SetTriggerCondition(IGuideCondition condition)
        {
            if (m_TriggerCondition != null)
            {
                m_TriggerCondition.OnConditionChanged -= OnTriggerConditionChanged;
            }

            m_TriggerCondition = condition;

            if (m_TriggerCondition != null)
            {
                m_TriggerCondition.OnConditionChanged += OnTriggerConditionChanged;
            }

            return this;
        }

        /// <summary>
        /// Set completion condition
        /// </summary>
        public GuideItem SetCompletionCondition(IGuideCondition condition)
        {
            if (m_CompletionCondition != null)
            {
                m_CompletionCondition.OnConditionChanged -= OnCompletionConditionChanged;
            }

            m_CompletionCondition = condition;

            if (m_CompletionCondition != null)
            {
                m_CompletionCondition.OnConditionChanged += OnCompletionConditionChanged;
            }

            return this;
        }

        /// <summary>
        /// Set guide effect
        /// </summary>
        public GuideItem SetGuideEffect(IGuideEffect effect, IGuideEffectConfig config)
        {
            m_GuideEffect = effect;
            m_GuideEffectConfig = config;

            return this;
        }

        #endregion

        #region Lifecycle Control

        /// <summary>
        /// Initialize guide item
        /// </summary>
        public void Initialize()
        {
            if (m_IsInitialized) return;

            // Register conditions to manager
            RegisterConditions();

            // Initialize effect (do not play)
            InitializeEffect();

            // Set initial state (keep Inactive, do not auto-enter Waiting)
            m_IsInitialized = true;
            SetState(GuideItemState.Inactive);

            Debug.Log($"[GuideItem] Initialized: {ItemId}");
        }

        /// <summary>
        /// Enter Waiting: start listening to trigger condition and check immediately once
        /// </summary>
        public void Enter()
        {
            if (!m_IsInitialized)
            {
                Initialize();
            }

            // Only allow entering Waiting from Inactive or after Reset
            if (m_State != GuideItemState.Inactive && m_State != GuideItemState.Waiting)
            {
                Debug.LogWarning($"[GuideItem] Cannot enter waiting from state {m_State} for item {ItemId}");
                return;
            }


            SetState(GuideItemState.Waiting);

            m_EnterTime = Time.time;
            // Start waiting timer (for Waiting timeout)
            m_WaitingStartTime = Time.time;

            // Immediately check trigger condition to avoid missing an already satisfied case
            if (m_TriggerCondition != null)
            {
                if (m_TriggerCondition.IsSatisfied() && m_AutoStart)
                {
                    StartItem();
                }
            }
        }

        /// <summary>
        /// Start the guide item
        /// </summary>
        public void StartItem()
        {
            if (m_State != GuideItemState.Waiting)
            {
                Debug.LogWarning($"[GuideItem] Cannot start item {ItemId} in state {m_State}");
                return;
            }

            m_RunningStartTime = Time.time;

            // Update state
            SetState(GuideItemState.Active);

            // Play guide effect
            StartEffect();

            // Raise started event
            OnItemStarted?.Invoke(this);

            Debug.Log($"[GuideItem] Started: {ItemId}");
        }

        /// <summary>
        /// Complete the guide item
        /// </summary>
        public void CompleteItem()
        {
            if (m_State != GuideItemState.Active)
            {
                Debug.LogWarning($"[GuideItem] Cannot complete item {ItemId} in state {m_State}");
                return;
            }

            // Record duration
            m_Duration = Time.time - m_EnterTime;

            // Stop guide effect
            StopEffect();

            // Update state
            SetState(GuideItemState.Completed);

            // Raise completed event
            OnItemCompleted?.Invoke(this);

            Debug.Log($"[GuideItem] Completed: {ItemId} (Duration: {m_Duration:F2}s)");
        }

        /// <summary>
        /// Cancel the guide item
        /// </summary>
        public void CancelItem()
        {
            if (m_State == GuideItemState.Completed || m_State == GuideItemState.Cancelled)
            {
                return;
            }

            // Stop guide effect
            StopEffect();

            // Update state
            SetState(GuideItemState.Cancelled);

            // Raise cancelled event
            OnItemCancelled?.Invoke(this);

            Debug.Log($"[GuideItem] Cancelled: {ItemId}");
        }

        /// <summary>
        /// Reset the guide item
        /// </summary>
        public void ResetItem()
        {
            // Cancel current state first
            CancelItem();

            // Reset time records
            m_EnterTime = 0f;
            m_Duration = 0f;
            m_RunningStartTime = 0f;
            m_WaitingStartTime = 0f;

            // Reset effect state
            ResetEffect();

            // Reset to Inactive; wait for GuideGroup to call Enter()
            SetState(GuideItemState.Inactive);

            Debug.Log($"[GuideItem] Reset: {ItemId}");
        }

        #endregion

        #region Update Methods (called externally)

        /// <summary>
        /// Update the guide item (called by GuideManager)
        /// </summary>
        public void Update()
        {
            // Only handle timeouts in Waiting/Active
            if (m_State == GuideItemState.Waiting || m_State == GuideItemState.Active)
            {
                UpdateTimeout();
            }
        }

        /// <summary>
        /// Update timeout checks
        /// </summary>
        private void UpdateTimeout()
        {
            if (m_State == GuideItemState.Waiting)
            {
                if (m_WaitingTimeoutSeconds > 0f && Time.time - m_WaitingStartTime >= m_WaitingTimeoutSeconds)
                {
                    OnWaitingTimeout();
                }

                return;
            }

            if (m_State == GuideItemState.Active)
            {
                if (m_RunningTimeoutSeconds > 0f && Time.time - m_RunningStartTime >= m_RunningTimeoutSeconds)
                {
                    OnRunningTimeout();
                }
            }
        }

        #endregion

        #region Effect Lifecycle Management

        /// <summary>
        /// Initialize effect (do not play)
        /// </summary>
        private void InitializeEffect()
        {
            if (m_GuideEffect == null) return;

            if (null == m_GuideEffectConfig)
                return;

            m_GuideEffectConfig.Apply(m_GuideEffect);

            Debug.Log($"[GuideItem] Effect initialized: {ItemId}");
        }

        /// <summary>
        /// Start playing effect
        /// </summary>
        private void StartEffect()
        {
            if (m_GuideEffect == null)
            {
                Debug.LogWarning($"[GuideItem] No effect to play for item: {ItemId}");
                return;
            }

            if (m_EffectIsPlaying)
            {
                Debug.LogWarning($"[GuideItem] Effect already playing for item: {ItemId}");
                return;
            }

            try
            {
                // Play effect
                m_GuideEffect.Play();
                m_EffectIsPlaying = true;

                Debug.Log($"[GuideItem] Effect started: {ItemId}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[GuideItem] Failed to start effect for item {ItemId}: {e.Message}");
                m_EffectIsPlaying = false;
            }
        }

        /// <summary>
        /// Stop playing effect
        /// </summary>
        private void StopEffect()
        {
            if (m_GuideEffect == null || !m_EffectIsPlaying)
            {
                return;
            }

            try
            {
                // Stop effect
                m_GuideEffect.Stop();
                m_EffectIsPlaying = false;

                Debug.Log($"[GuideItem] Effect stopped: {ItemId}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[GuideItem] Failed to stop effect for item {ItemId}: {e.Message}");
            }
        }

        /// <summary>
        /// Pause effect
        /// </summary>
        public void PauseEffect()
        {
            if (m_GuideEffect == null || !m_EffectIsPlaying)
            {
                return;
            }

            try
            {
                m_GuideEffect.Pause();
                Debug.Log($"[GuideItem] Effect paused: {ItemId}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[GuideItem] Failed to pause effect for item {ItemId}: {e.Message}");
            }
        }

        /// <summary>
        /// Resume effect
        /// </summary>
        public void ResumeEffect()
        {
            if (m_GuideEffect == null || !m_EffectIsPlaying)
            {
                return;
            }

            try
            {
                m_GuideEffect.Resume();
                Debug.Log($"[GuideItem] Effect resumed: {ItemId}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[GuideItem] Failed to resume effect for item {ItemId}: {e.Message}");
            }
        }

        /// <summary>
        /// Reset effect state
        /// </summary>
        private void ResetEffect()
        {
            if (m_GuideEffect == null) return;

            try
            {
                // Stop effect
                m_GuideEffect.Stop();
                m_EffectIsPlaying = false;

                // Reset effect state (if the effect supports resetting)
                if (m_GuideEffect is IResettableEffect resettableEffect)
                {
                    resettableEffect.Reset();
                }

                Debug.Log($"[GuideItem] Effect reset: {ItemId}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[GuideItem] Failed to reset effect for item {ItemId}: {e.Message}");
            }
        }

        #endregion

        #region Condition Event Handling

        /// <summary>
        /// Trigger condition changed callback
        /// </summary>
        private void OnTriggerConditionChanged(IGuideCondition condition)
        {
            if (m_State == GuideItemState.Waiting && condition.IsSatisfied() && m_AutoStart)
            {
                StartItem();
            }
        }

        /// <summary>
        /// Completion condition changed callback
        /// </summary>
        private void OnCompletionConditionChanged(IGuideCondition condition)
        {
            if (m_State == GuideItemState.Active && condition.IsSatisfied() && m_AutoComplete)
            {
                CompleteItem();
            }
        }

        #endregion

        #region Timeout Management

        /// <summary>
        /// Timeout handling
        /// </summary>
        private void OnWaitingTimeout()
        {
            Debug.LogWarning($"[GuideItem] Waiting timeout: {ItemId}");

            // Waiting timeout usually means the step can't start → Fail
            StopEffect();
            SetState(GuideItemState.Failed);
            OnItemFailed?.Invoke(this);
        }

        private void OnRunningTimeout()
        {
            Debug.LogWarning($"[GuideItem] Running timeout: {ItemId}");

            // Running timeout → Fail
            StopEffect();
            SetState(GuideItemState.Failed);
            OnItemFailed?.Invoke(this);
        }

        #endregion

        #region Condition Management

        /// <summary>
        /// Register conditions
        /// </summary>
        private void RegisterConditions()
        {
            if (m_TriggerCondition != null)
            {
                GuideConditionManager.Instance.RegisterCondition(m_TriggerCondition);
            }

            if (m_CompletionCondition != null)
            {
                GuideConditionManager.Instance.RegisterCondition(m_CompletionCondition);
            }
        }

        /// <summary>
        /// Unregister conditions
        /// </summary>
        private void UnregisterConditions()
        {
            if (m_TriggerCondition != null)
            {
                GuideConditionManager.Instance.UnregisterCondition(m_TriggerCondition);
            }

            if (m_CompletionCondition != null)
            {
                GuideConditionManager.Instance.UnregisterCondition(m_CompletionCondition);
            }
        }

        #endregion

        #region State Management

        /// <summary>
        /// Set state
        /// </summary>
        private void SetState(GuideItemState newState)
        {
            if (m_State == newState) return;

            var oldState = m_State;
            m_State = newState;

            // Raise state changed event
            OnStateChanged?.Invoke(this);

            Debug.Log($"[GuideItem] State changed: {ItemId} {oldState} -> {newState}");
        }

        #endregion

        #region Cleanup and Disposal

        /// <summary>
        /// Cleanup resources
        /// </summary>
        public void Dispose()
        {
            // Stop effect
            StopEffect();

            // Unsubscribe from events
            UnsubscribeFromEvents();

            // Unregister conditions
            UnregisterConditions();

            Debug.Log($"[GuideItem] Disposed: {ItemId}");
        }

        /// <summary>
        /// Unsubscribe from events
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            if (m_TriggerCondition != null)
            {
                m_TriggerCondition.OnConditionChanged -= OnTriggerConditionChanged;
            }

            if (m_CompletionCondition != null)
            {
                m_CompletionCondition.OnConditionChanged -= OnCompletionConditionChanged;
            }
        }

        #endregion

        #region Overrides

        public override string ToString()
        {
            return $"[GuideItem] {ItemId}: {Description} (State: {State}, Priority: {Priority})";
        }

        #endregion
    }
}