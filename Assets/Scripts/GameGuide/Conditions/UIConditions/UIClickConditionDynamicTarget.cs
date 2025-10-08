using System;
using UnityEngine;

namespace GameGuide.Conditions.UIConditions
{
    /// <summary>
    /// UI click condition with dynamic target finding.
    /// Standalone implementation that directly inherits from GuideConditionBase.
    /// </summary>
    [Serializable]
    public class UIClickConditionDynamicTarget : GuideConditionBase
    {
        #region Serialized Fields

        [SerializeField]
        private ClickHandler m_ClickHandler = new ClickHandler();

        #endregion

        #region Private Fields

        private DynamicTarget m_DynamicTarget;
        private bool m_IsListenerAttached;

        #endregion

        #region Properties

        public int RequiredClickCount
        {
            get => m_ClickHandler.RequiredClickCount;
            set => m_ClickHandler.RequiredClickCount = value;
        }

        public bool RequireExactTarget
        {
            get => m_ClickHandler.RequireExactTarget;
            set => m_ClickHandler.RequireExactTarget = value;
        }

        public float ClickTimeWindow
        {
            get => m_ClickHandler.ClickTimeWindow;
            set => m_ClickHandler.ClickTimeWindow = value;
        }

        /// <summary>
        /// Get cached target object (for external access)
        /// </summary>
        public GameObject CachedTargetObject => m_DynamicTarget?.GetTargetGameObject();

        #endregion

        #region Constructors

        private UIClickConditionDynamicTarget()
        {
        }

        public UIClickConditionDynamicTarget(
            string targetPath,
            Func<RectTransform> rootGetter,
            int clickCount = 1,
            bool exactTarget = true)
            : base($"DynamicClick_{targetPath}_{clickCount}",
                $"Dynamic click {targetPath} {clickCount} times")
        {
            m_DynamicTarget = new DynamicTarget
            {
                TargetRelPath = targetPath,
                RootGetter = rootGetter
            };

            m_ClickHandler = new ClickHandler
            {
                RequiredClickCount = clickCount,
                RequireExactTarget = exactTarget
            };

            SetupClickHandler();
        }

        private void SetupClickHandler()
        {
            m_ClickHandler.OnClickSatisfied += () =>
            {
                TriggerConditionChanged();
                Debug.Log($"[UIClickConditionDynamicTarget] Condition satisfied: {ConditionId}");
            };

            m_ClickHandler.OnClick += (current, required) =>
            {
                Debug.Log($"[UIClickConditionDynamicTarget] Click {current}/{required}: {ConditionId}");
            };
        }

        #endregion

        #region GuideConditionBase Implementation

        /// <summary>
        /// Needs periodic state checking to manage listener lifecycle
        /// </summary>
        public override bool NeedsStateChecking => true;

        /// <summary>
        /// Check if condition is satisfied
        /// </summary>
        public override bool IsSatisfied()
        {
            return m_ClickHandler.IsSatisfied;
        }

        /// <summary>
        /// Perform state check - manage listener attachment/detachment dynamically
        /// </summary>
        public override void PerformStateCheck()
        {
            if (!IsListening) return;

            GameObject targetObject = m_DynamicTarget.GetTargetGameObject();
            GameObject currentTarget = m_ClickHandler.CurrentTargetObject;

            // Scenario 1: Object disappeared (destroyed or hidden)
            if (targetObject == null && m_IsListenerAttached)
            {
                m_ClickHandler.DetachListeners();
                m_IsListenerAttached = false;

                Debug.Log($"[UIClickConditionDynamicTarget] Target lost, listeners detached: {ConditionId}");
            }
            // Scenario 2: Object appeared, but listeners not attached yet
            else if (targetObject != null && !m_IsListenerAttached)
            {
                m_ClickHandler.AttachListeners(targetObject);
                m_IsListenerAttached = true;

                Debug.Log($"[UIClickConditionDynamicTarget] Target found, listeners attached: {ConditionId}");
            }
            // Scenario 3: Object was replaced (different instance)
            else if (targetObject != null && currentTarget != null &&
                     targetObject != currentTarget)
            {
                // ClickHandler will handle detaching old and attaching new
                m_ClickHandler.AttachListeners(targetObject);

                Debug.Log($"[UIClickConditionDynamicTarget] Target replaced, listeners reattached: {ConditionId}");
            }
        }

        protected override void OnStartListening()
        {
            // Reset click state using ClickHandler
            m_ClickHandler.Reset();

            // Reset listener state
            m_IsListenerAttached = false;

            // Immediately try to find object and attach listeners
            PerformStateCheck();
        }

        protected override void OnStopListening()
        {
            // Remove listeners if attached using ClickHandler
            if (m_IsListenerAttached)
            {
                m_ClickHandler.DetachListeners();
                m_IsListenerAttached = false;
            }
        }

        protected override string GetDefaultDescription()
        {
            string path = m_DynamicTarget?.TargetRelPath ?? "null";
            var target = m_DynamicTarget?.GetTargetGameObject();
            string status = target != null ? "✓" : "✗";

            return $"Dynamic Click [{status}] {path} {RequiredClickCount} times" +
                   $" (Current: {m_ClickHandler.CurrentClickCount}/{RequiredClickCount})";
        }

        #endregion

        #region Dynamic Target Configuration

        /// <summary>
        /// Set dynamic target configuration
        /// </summary>
        public void SetDynamicTarget(string path, Func<RectTransform> rootGetter)
        {
            if (m_DynamicTarget == null)
                m_DynamicTarget = new DynamicTarget();

            m_DynamicTarget.TargetRelPath = path;
            m_DynamicTarget.RootGetter = rootGetter;

            // Clear current listeners and reset state using ClickHandler
            if (m_IsListenerAttached)
            {
                m_ClickHandler.DetachListeners();
                m_IsListenerAttached = false;
            }
        }

        /// <summary>
        /// Invalidate cache and force re-find target object
        /// </summary>
        public void InvalidateCache()
        {
            m_DynamicTarget?.InvalidateCache();

            // Remove listeners and wait for re-attachment using ClickHandler
            if (m_IsListenerAttached)
            {
                m_ClickHandler.DetachListeners();
                m_IsListenerAttached = false;
            }
        }

        /// <summary>
        /// Reset condition state (click count and satisfied flag)
        /// </summary>
        public void Reset()
        {
            m_ClickHandler.Reset();

            if (m_IsListenerAttached)
            {
                m_ClickHandler.DetachListeners();
                m_IsListenerAttached = false;
            }

            TriggerConditionChanged();
        }

        #endregion

        #region Debug and Editor Support

#if UNITY_EDITOR
        /// <summary>
        /// Debug: Print condition info
        /// </summary>
        [ContextMenu("Debug Info")]
        private void DebugInfo()
        {
            Debug.Log($"=== UIClickConditionDynamicTarget Debug Info ===");
            Debug.Log($"  ConditionId: {ConditionId}");
            Debug.Log($"  Path: {m_DynamicTarget?.TargetRelPath ?? "null"}");
            Debug.Log($"  IsListening: {IsListening}");
            Debug.Log($"  IsListenerAttached: {m_IsListenerAttached}");
            Debug.Log($"  CurrentTarget: {m_ClickHandler.CurrentTargetObject?.name ?? "null"}");
            Debug.Log($"  ClickCount: {m_ClickHandler.CurrentClickCount}/{RequiredClickCount}");
            Debug.Log($"  IsSatisfied: {IsSatisfied()}");
            Debug.Log($"  Description: {GetDescription()}");
            Debug.Log($"  ClickHandler: {m_ClickHandler.GetDebugInfo()}");
        }
#endif

        #endregion
    }
}