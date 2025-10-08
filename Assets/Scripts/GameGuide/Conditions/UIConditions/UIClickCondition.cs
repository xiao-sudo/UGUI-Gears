using System;
using UnityEngine;

namespace GameGuide.Conditions.UIConditions
{
    /// <summary>
    /// UI click condition
    /// </summary>
    [Serializable]
    public class UIClickCondition : GuideConditionBase
    {
        [SerializeField]
        private GameObject m_TargetObject;

        [SerializeField]
        private ClickHandler m_ClickHandler = new ClickHandler();

        [NonSerialized]
        private bool m_EventsSetup = false;

        public GameObject TargetObject
        {
            get => m_TargetObject;
            set => m_TargetObject = value;
        }

        public bool RequireExactTarget
        {
            get => m_ClickHandler.RequireExactTarget;
            set => m_ClickHandler.RequireExactTarget = value;
        }

        public int RequiredClickCount
        {
            get => m_ClickHandler.RequiredClickCount;
            set => m_ClickHandler.RequiredClickCount = value;
        }

        public float ClickTimeWindow
        {
            get => m_ClickHandler.ClickTimeWindow;
            set => m_ClickHandler.ClickTimeWindow = value;
        }

        private UIClickCondition()
        {
        }

        public UIClickCondition(GameObject target, int clickCount = 1, bool exactTarget = true)
            : base($"Click_{target?.name}_{clickCount}", $"Click {target?.name} {clickCount} Times")
        {
            m_TargetObject = target;
            m_ClickHandler = new ClickHandler
            {
                RequiredClickCount = clickCount,
                RequireExactTarget = exactTarget
            };
            SetupClickHandler();
        }

        public override bool IsSatisfied()
        {
            return m_ClickHandler.IsSatisfied;
        }

        private void SetupClickHandler()
        {
            if (m_EventsSetup) return;  // Prevent duplicate subscriptions
            
            m_ClickHandler.OnClickSatisfied += () => { TriggerConditionChanged(); };
            m_EventsSetup = true;
        }

        protected override void OnStartListening()
        {
            if (m_TargetObject == null) return;

            // Ensure event handler is set up (in case of deserialization)
            SetupClickHandler();

            // Reset state and attach listeners using ClickHandler
            m_ClickHandler.Reset();
            m_ClickHandler.AttachListeners(m_TargetObject);
        }

        protected override void OnStopListening()
        {
            // Remove event listeners using ClickHandler
            m_ClickHandler.DetachListeners();
        }

        /// <summary>
        /// Reset condition state
        /// </summary>
        public void Reset()
        {
            m_ClickHandler.Reset();
            TriggerConditionChanged();
        }

        protected override string GetDefaultDescription()
        {
            if (m_TargetObject == null)
                return $"Click condition: {RequiredClickCount} clicks";

            return $"Click {m_TargetObject.name} {RequiredClickCount} times" +
                   $" (Current: {m_ClickHandler.CurrentClickCount}/{RequiredClickCount})";
        }
    }
}