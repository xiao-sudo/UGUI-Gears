using System;
using System.Collections;
using UnityEngine;
using GameGuide.Conditions;
using UIExt.Effect;
using UnityEngine.Serialization;

namespace GameGuide.Core
{
    /// <summary>
    /// 引导项实现类 - 纯C#类，不继承MonoBehaviour
    /// </summary>
    [Serializable]
    public class GuideItem : IGuideItem
    {
        #region 序列化字段

        [SerializeField]
        private string m_ItemId;

        [SerializeField]
        private string m_Description;

        [SerializeField]
        private GuideItemPriority m_Priority = GuideItemPriority.Normal;

        // 超时配置：等待期与执行期分别配置
        [SerializeField]
        private float m_WaitingTimeoutSeconds = 0f; // 0 表示不启用等待期超时

        [SerializeField]
        private float m_RunningTimeoutSeconds = 30f; // 保持兼容：作为执行期超时（Running）

        [SerializeField]
        private bool m_AutoStart = true;

        [SerializeField]
        private bool m_AutoComplete = true;

        #endregion

        #region 私有字段

        private GuideItemState m_State = GuideItemState.Inactive;
        private float m_StartTime = 0f;
        private float m_Duration = 0f;
        private IGuideCondition m_TriggerCondition;
        private IGuideCondition m_CompletionCondition;
        private IGuideEffect m_GuideEffect;
        private bool m_IsInitialized = false;
        private bool m_EffectIsPlaying = false;
        private float m_WaitingStartTime = 0f;
        private float m_RunningStartTime = 0f;

        #endregion

        #region 公共属性

        public string ItemId => m_ItemId;
        public string Description => m_Description;
        public GuideItemPriority Priority => m_Priority;
        public GuideItemState State => m_State;
        public float StartTime => m_StartTime;

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

        #region 事件

        public event Action<IGuideItem> OnItemStarted;
        public event Action<IGuideItem> OnItemCompleted;
        public event Action<IGuideItem> OnItemCancelled;
        public event Action<IGuideItem> OnItemFailed;
        public event Action<IGuideItem> OnStateChanged;

        #endregion

        #region 构造函数

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

        #region 配置方法（链式调用）

        /// <summary>
        /// 设置引导项ID
        /// </summary>
        public GuideItem SetItemId(string itemId)
        {
            m_ItemId = itemId;
            return this;
        }

        /// <summary>
        /// 设置描述
        /// </summary>
        public GuideItem SetDescription(string description)
        {
            m_Description = description;
            return this;
        }

        /// <summary>
        /// 设置优先级
        /// </summary>
        public GuideItem SetPriority(GuideItemPriority priority)
        {
            m_Priority = priority;
            return this;
        }

        /// <summary>
        /// 设置超时时间
        /// </summary>
        public GuideItem SetTimeout(float timeoutSeconds)
        {
            m_RunningTimeoutSeconds = Mathf.Max(0f, timeoutSeconds);
            return this;
        }

        /// <summary>
        /// 设置等待期超时（Waiting）
        /// </summary>
        public GuideItem SetWaitingTimeout(float timeoutSeconds)
        {
            m_WaitingTimeoutSeconds = Mathf.Max(0f, timeoutSeconds);
            return this;
        }

        /// <summary>
        /// 设置执行期超时（Running）
        /// </summary>
        public GuideItem SetRunningTimeout(float timeoutSeconds)
        {
            m_RunningTimeoutSeconds = Mathf.Max(0f, timeoutSeconds);
            return this;
        }

        /// <summary>
        /// 设置是否自动开始
        /// </summary>
        public GuideItem SetAutoStart(bool autoStart)
        {
            m_AutoStart = autoStart;
            return this;
        }

        /// <summary>
        /// 设置是否自动完成
        /// </summary>
        public GuideItem SetAutoComplete(bool autoComplete)
        {
            m_AutoComplete = autoComplete;
            return this;
        }

        /// <summary>
        /// 设置触发条件
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
        /// 设置完成条件
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
        /// 设置引导效果
        /// </summary>
        public GuideItem SetGuideEffect(IGuideEffect effect)
        {
            if (m_GuideEffect != null)
            {
                m_GuideEffect.OnGuideEffectCompleted -= OnGuideEffectCompleted;
            }

            m_GuideEffect = effect;

            if (m_GuideEffect != null)
            {
                m_GuideEffect.OnGuideEffectCompleted += OnGuideEffectCompleted;
            }

            return this;
        }

        #endregion

        #region 生命周期控制

        /// <summary>
        /// 初始化引导项
        /// </summary>
        public void Initialize()
        {
            if (m_IsInitialized) return;

            // 注册条件到管理器
            RegisterConditions();

            // 初始化效果（但不播放）
            InitializeEffect();

            // 设置初始状态（保持 Inactive，不自动进入 Waiting）
            m_IsInitialized = true;
            SetState(GuideItemState.Inactive);

            Debug.Log($"[GuideItem] Initialized: {ItemId}");
        }

        /// <summary>
        /// 进入等待态：开始监听触发条件并做一次立即检查
        /// </summary>
        public void Enter()
        {
            if (!m_IsInitialized)
            {
                Initialize();
            }

            // 仅允许从 Inactive 或 Reset 后进入 Waiting
            if (m_State != GuideItemState.Inactive && m_State != GuideItemState.Waiting)
            {
                Debug.LogWarning($"[GuideItem] Cannot enter waiting from state {m_State} for item {ItemId}");
                return;
            }

            SetState(GuideItemState.Waiting);

            // 开始等待计时（用于 Waiting 期超时）
            m_WaitingStartTime = Time.time;

            // 立即检查触发条件，避免错过已满足的情况
            if (m_TriggerCondition != null)
            {
                if (m_TriggerCondition.IsSatisfied() && m_AutoStart)
                {
                    StartItem();
                }
            }
        }

        /// <summary>
        /// 开始引导项
        /// </summary>
        public void StartItem()
        {
            if (m_State != GuideItemState.Waiting)
            {
                Debug.LogWarning($"[GuideItem] Cannot start item {ItemId} in state {m_State}");
                return;
            }

            // 记录开始时间
            m_StartTime = Time.time;
            m_RunningStartTime = Time.time;

            // 更新状态
            SetState(GuideItemState.Active);

            // 播放引导效果
            StartEffect();

            // 触发开始事件
            OnItemStarted?.Invoke(this);

            Debug.Log($"[GuideItem] Started: {ItemId}");
        }

        /// <summary>
        /// 完成引导项
        /// </summary>
        public void CompleteItem()
        {
            if (m_State != GuideItemState.Active)
            {
                Debug.LogWarning($"[GuideItem] Cannot complete item {ItemId} in state {m_State}");
                return;
            }

            // 记录持续时间
            m_Duration = Time.time - m_StartTime;

            // 停止引导效果
            StopEffect();

            // 更新状态
            SetState(GuideItemState.Completed);

            // 触发完成事件
            OnItemCompleted?.Invoke(this);

            Debug.Log($"[GuideItem] Completed: {ItemId} (Duration: {m_Duration:F2}s)");
        }

        /// <summary>
        /// 取消引导项
        /// </summary>
        public void CancelItem()
        {
            if (m_State == GuideItemState.Completed || m_State == GuideItemState.Cancelled)
            {
                return;
            }

            // 停止引导效果
            StopEffect();

            // 更新状态
            SetState(GuideItemState.Cancelled);

            // 触发取消事件
            OnItemCancelled?.Invoke(this);

            Debug.Log($"[GuideItem] Cancelled: {ItemId}");
        }

        /// <summary>
        /// 重置引导项
        /// </summary>
        public void ResetItem()
        {
            // 先取消当前状态
            CancelItem();

            // 重置时间记录
            m_StartTime = 0f;
            m_Duration = 0f;
            m_RunningStartTime = 0f;
            m_WaitingStartTime = 0f;

            // 重置效果状态
            ResetEffect();

            // 重置为未激活，等待 GuideGroup 调用 Enter()
            SetState(GuideItemState.Inactive);

            Debug.Log($"[GuideItem] Reset: {ItemId}");
        }

        #endregion

        #region 更新方法（由外部调用）

        /// <summary>
        /// 更新引导项（由GuideManager调用）
        /// </summary>
        public void Update()
        {
            // 仅处理 Waiting/Active 的超时
            if (m_State == GuideItemState.Waiting || m_State == GuideItemState.Active)
            {
                UpdateTimeout();
            }
        }

        /// <summary>
        /// 更新超时检查
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

        #region 效果生命周期管理

        /// <summary>
        /// 初始化效果（不播放）
        /// </summary>
        private void InitializeEffect()
        {
            if (m_GuideEffect == null) return;

            // 订阅效果完成事件
            m_GuideEffect.OnGuideEffectCompleted += OnGuideEffectCompleted;

            Debug.Log($"[GuideItem] Effect initialized: {ItemId}");
        }

        /// <summary>
        /// 开始播放效果
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
                // 播放效果
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
        /// 停止播放效果
        /// </summary>
        private void StopEffect()
        {
            if (m_GuideEffect == null || !m_EffectIsPlaying)
            {
                return;
            }

            try
            {
                // 停止效果
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
        /// 暂停效果
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
        /// 恢复效果
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
        /// 重置效果状态
        /// </summary>
        private void ResetEffect()
        {
            if (m_GuideEffect == null) return;

            try
            {
                // 停止效果
                m_GuideEffect.Stop();
                m_EffectIsPlaying = false;

                // 重置效果状态（如果效果支持重置）
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

        #region 效果事件处理

        /// <summary>
        /// 效果完成回调
        /// </summary>
        private void OnGuideEffectCompleted(IGuideEffect effect)
        {
            if (effect != m_GuideEffect) return;

            m_EffectIsPlaying = false;

            // 如果启用了自动完成，则完成引导项
            if (m_AutoComplete && m_State == GuideItemState.Active)
            {
                CompleteItem();
            }

            Debug.Log($"[GuideItem] Effect completed: {ItemId}");
        }

        #endregion

        #region 条件事件处理

        /// <summary>
        /// 触发条件变化回调
        /// </summary>
        private void OnTriggerConditionChanged(IGuideCondition condition)
        {
            if (m_State == GuideItemState.Waiting && condition.IsSatisfied() && m_AutoStart)
            {
                StartItem();
            }
        }

        /// <summary>
        /// 完成条件变化回调
        /// </summary>
        private void OnCompletionConditionChanged(IGuideCondition condition)
        {
            if (m_State == GuideItemState.Active && condition.IsSatisfied() && m_AutoComplete)
            {
                CompleteItem();
            }
        }

        #endregion

        #region 超时管理

        /// <summary>
        /// 超时处理
        /// </summary>
        private void OnWaitingTimeout()
        {
            Debug.LogWarning($"[GuideItem] Waiting timeout: {ItemId}");

            // 等待期超时通常认为当前步骤无法开始 → 失败
            StopEffect();
            SetState(GuideItemState.Failed);
            OnItemFailed?.Invoke(this);
        }

        private void OnRunningTimeout()
        {
            Debug.LogWarning($"[GuideItem] Running timeout: {ItemId}");

            // 执行期超时 → 失败
            StopEffect();
            SetState(GuideItemState.Failed);
            OnItemFailed?.Invoke(this);
        }

        #endregion

        #region 条件管理

        /// <summary>
        /// 注册条件
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
        /// 注销条件
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

        #region 状态管理

        /// <summary>
        /// 设置状态
        /// </summary>
        private void SetState(GuideItemState newState)
        {
            if (m_State == newState) return;

            var oldState = m_State;
            m_State = newState;

            // 触发状态变化事件
            OnStateChanged?.Invoke(this);

            Debug.Log($"[GuideItem] State changed: {ItemId} {oldState} -> {newState}");
        }

        #endregion

        #region 清理和销毁

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Dispose()
        {
            // 停止效果
            StopEffect();

            // 取消事件订阅
            UnsubscribeFromEvents();

            // 注销条件
            UnregisterConditions();

            Debug.Log($"[GuideItem] Disposed: {ItemId}");
        }

        /// <summary>
        /// 取消事件订阅
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

            if (m_GuideEffect != null)
            {
                m_GuideEffect.OnGuideEffectCompleted -= OnGuideEffectCompleted;
            }
        }

        #endregion

        #region 重写方法

        public override string ToString()
        {
            return $"[GuideItem] {ItemId}: {Description} (State: {State}, Priority: {Priority})";
        }

        #endregion
    }
}