using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameGuide.Conditions.UIConditions
{
    /// <summary>
    /// UI state condition
    /// </summary>
    [Serializable]
    public class UIStateCondition : GuideConditionBase
    {
        public enum UIStateType
        {
            ActiveInHierarchy, // Object is active in hierarchy
            Interactable, // Interactable
            Visible, // Visible
            Enabled, // Enabled
            Selected, // Selected
        }

        [SerializeField]
        private GameObject m_TargetObject;

        [SerializeField]
        private UIStateType m_StateType;

        [SerializeField]
        private bool m_ExpectedValue = true;

        private Selectable m_Selectable;
        private bool m_LastStateValue;

        public UIStateType StateType
        {
            get => m_StateType;
            set => m_StateType = value;
        }

        public bool ExpectedValue
        {
            get => m_ExpectedValue;
            set => m_ExpectedValue = value;
        }

        public UIStateCondition() : base()
        {
        }

        public UIStateCondition(GameObject target, UIStateType type, bool expected = true)
            : base($"UI_{type}_{target?.name}", $"UI object {target?.name} {type} state is {expected}")
        {
            m_TargetObject = target;
            m_StateType = type;
            m_ExpectedValue = expected;
            m_LastStateValue = false;
        }

        /// <summary>
        /// UI state conditions need periodic state checking
        /// </summary>
        public override bool NeedsStateChecking => true;

        /// <summary>
        /// Perform state checking for UI state conditions
        /// </summary>
        public override void PerformStateCheck()
        {
            CheckStateChange();
        }

        public override bool IsSatisfied()
        {
            var targetObject = GetTargetObject();
            if (targetObject == null) return false;

            bool currentStateValue = GetCurrentStateValue(targetObject);
            return currentStateValue == m_ExpectedValue;
        }

        private bool GetCurrentStateValue(GameObject targetObject)
        {
            if (targetObject == null) return false;

            switch (m_StateType)
            {
                case UIStateType.ActiveInHierarchy:
                    return targetObject.activeInHierarchy;

                case UIStateType.Interactable:
                    // First check if the object is active in hierarchy
                    if (!targetObject.activeInHierarchy)
                        return false;

                    if (m_Selectable == null || m_Selectable.gameObject != targetObject)
                        m_Selectable = targetObject.GetComponent<Selectable>();
                    return m_Selectable != null && m_Selectable.interactable;

                case UIStateType.Visible:
                    // First check if the object is active in hierarchy (highest priority)
                    if (!targetObject.activeInHierarchy)
                        return false;

                    // Check if the object is enabled
                    var visibleComponent = targetObject.GetComponent<MonoBehaviour>();
                    if (visibleComponent != null && !visibleComponent.enabled)
                        return false;

                    // Check CanvasGroup alpha and interactable state
                    var canvasGroup = targetObject.GetComponent<CanvasGroup>();
                    if (canvasGroup != null)
                        return canvasGroup.alpha > 0.01f && canvasGroup.interactable;

                    // Check Image alpha
                    var image = targetObject.GetComponent<Image>();
                    if (image != null)
                        return image.color.a > 0.01f;

                    // If no specific visibility components, consider it visible if active
                    return true;

                case UIStateType.Enabled:
                    // First check if the object is active in hierarchy
                    if (!targetObject.activeInHierarchy)
                        return false;

                    var enableComponent = targetObject.GetComponent<MonoBehaviour>();
                    return enableComponent != null && enableComponent.enabled;

                case UIStateType.Selected:
                    // First check if the object is active in hierarchy
                    if (!targetObject.activeInHierarchy)
                        return false;

                    if (m_Selectable == null || m_Selectable.gameObject != targetObject)
                        m_Selectable = targetObject.GetComponent<Selectable>();
                    return m_Selectable != null &&
                           m_Selectable.gameObject == EventSystem.current?.currentSelectedGameObject;

                default:
                    return false;
            }
        }

        protected override void OnStartListening()
        {
            var targetObject = GetTargetObject();
            if (targetObject == null) return;

            m_Selectable = targetObject.GetComponent<Selectable>();

            CheckStateChange();
        }

        protected override void OnStopListening()
        {
            // Clean up event listeners
        }

        protected override string GetDefaultDescription()
        {
            var targetObject = GetTargetObject();
            if (targetObject == null)
                return $"UI object state check: {m_StateType} = {m_ExpectedValue}";

            return $"UI object {targetObject.name} {m_StateType} state is {m_ExpectedValue}";
        }

        /// <summary>
        /// Check state changes periodically (called externally)
        /// </summary>
        public void CheckStateChange()
        {
            if (!IsListening) return;

            var targetObject = GetTargetObject();

            if (null == targetObject)
                return;

            bool currentState = GetCurrentStateValue(targetObject);
            if (currentState != m_LastStateValue)
            {
                m_LastStateValue = currentState;
                TriggerConditionChanged();
            }
        }

        protected virtual GameObject GetTargetObject()
        {
            return m_TargetObject;
        }
    }
}