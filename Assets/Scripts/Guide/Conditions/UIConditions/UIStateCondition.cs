using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UIExt.Guide.Conditions.UIConditions
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

        public GameObject TargetObject
        {
            get => m_TargetObject;
            set => m_TargetObject = value;
        }

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
        }

        public override bool IsSatisfied()
        {
            if (m_TargetObject == null) return false;

            bool currentStateValue = GetCurrentStateValue();
            return currentStateValue == m_ExpectedValue;
        }

        private bool GetCurrentStateValue()
        {
            switch (m_StateType)
            {
                case UIStateType.ActiveInHierarchy:
                    return m_TargetObject.activeInHierarchy;

                case UIStateType.Interactable:
                    if (m_Selectable == null)
                        m_Selectable = m_TargetObject.GetComponent<Selectable>();
                    return m_Selectable != null && m_Selectable.interactable;

                case UIStateType.Visible:
                    var canvasGroup = m_TargetObject.GetComponent<CanvasGroup>();
                    if (canvasGroup != null)
                        return canvasGroup.alpha > 0.01f && canvasGroup.interactable;

                    var image = m_TargetObject.GetComponent<Image>();
                    if (image != null)
                        return image.color.a > 0.01f;

                    return m_TargetObject.activeInHierarchy;

                case UIStateType.Enabled:
                    var component = m_TargetObject.GetComponent<MonoBehaviour>();
                    return component != null && component.enabled;

                case UIStateType.Selected:
                    if (m_Selectable == null)
                        m_Selectable = m_TargetObject.GetComponent<Selectable>();
                    return m_Selectable != null &&
                           m_Selectable.gameObject == EventSystem.current?.currentSelectedGameObject;

                default:
                    return false;
            }
        }

        protected override void OnStartListening()
        {
            if (m_TargetObject == null) return;

            m_Selectable = m_TargetObject.GetComponent<Selectable>();
            m_LastStateValue = GetCurrentStateValue();

            // For certain state types, special event listening is required
            switch (m_StateType)
            {
                case UIStateType.ActiveInHierarchy:
                    // Can be checked periodically through coroutines
                    break;

                case UIStateType.Interactable:
                    // Listen to Selectable state changes
                    break;

                case UIStateType.Selected:
                    // Listen to EventSystem selection state changes
                    break;
            }
        }

        protected override void OnStopListening()
        {
            // Clean up event listeners
        }

        protected override string GetDefaultDescription()
        {
            if (m_TargetObject == null)
                return $"UI object state check: {m_StateType} = {m_ExpectedValue}";

            return $"UI object {m_TargetObject.name} {m_StateType} state is {m_ExpectedValue}";
        }

        /// <summary>
        /// Check state changes periodically (called externally)
        /// </summary>
        public void CheckStateChange()
        {
            if (!IsListening || m_TargetObject == null) return;

            bool currentState = GetCurrentStateValue();
            if (currentState != m_LastStateValue)
            {
                m_LastStateValue = currentState;
                TriggerConditionChanged();
            }
        }
    }
}