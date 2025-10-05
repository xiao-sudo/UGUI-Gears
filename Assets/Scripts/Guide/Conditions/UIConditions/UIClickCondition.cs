using System;
using UIExt.Guide.Conditions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Guide.Conditions.UIConditions
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
        private bool m_RequireExactTarget = true;

        [SerializeField]
        private int m_RequiredClickCount = 1;

        [SerializeField]
        private float m_ClickTimeWindow = 1f; // Click time window

        private int m_CurrentClickCount;
        private float m_LastClickTime;
        private bool m_IsSatisfied;

        // Track listeners we added
        private Button m_TrackedButton;
        private EventTrigger.Entry m_TrackedEventTriggerEntry;

        public GameObject TargetObject
        {
            get => m_TargetObject;
            set => m_TargetObject = value;
        }

        public bool RequireExactTarget
        {
            get => m_RequireExactTarget;
            set => m_RequireExactTarget = value;
        }

        public int RequiredClickCount
        {
            get => m_RequiredClickCount;
            set => m_RequiredClickCount = value;
        }

        public float ClickTimeWindow
        {
            get => m_ClickTimeWindow;
            set => m_ClickTimeWindow = value;
        }

        public UIClickCondition() : base()
        {
        }

        public UIClickCondition(GameObject target, int clickCount = 1, bool exactTarget = true)
            : base($"Click_{target?.name}_{clickCount}", $"Click {target?.name} {clickCount} Times")
        {
            m_TargetObject = target;
            m_RequiredClickCount = clickCount;
            m_RequireExactTarget = exactTarget;
        }

        public override bool IsSatisfied()
        {
            return m_IsSatisfied;
        }

        protected override void OnStartListening()
        {
            if (m_TargetObject == null) return;

            // Reset state
            m_CurrentClickCount = 0;
            m_IsSatisfied = false;

            // Add event listeners
            AddClickListeners();
        }

        protected override void OnStopListening()
        {
            // Remove event listeners
            RemoveClickListeners();
        }

        private void AddClickListeners()
        {
            if (m_TargetObject == null) return;

            var button = m_TargetObject.GetComponent<Button>();
            var eventTrigger = m_TargetObject.GetComponent<EventTrigger>();

            if (button != null)
            {
                // Prefer Button component for better performance
                m_TrackedButton = button;
                button.onClick.AddListener(OnTargetClicked);
            }
            else if (eventTrigger != null)
            {
                // Use existing EventTrigger
                AddEventTriggerEntry(eventTrigger);
            }
            else
            {
                // Create new EventTrigger
                eventTrigger = m_TargetObject.AddComponent<EventTrigger>();
                AddEventTriggerEntry(eventTrigger);
            }
        }

        private void AddEventTriggerEntry(EventTrigger eventTrigger)
        {
            m_TrackedEventTriggerEntry = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerClick
            };
            m_TrackedEventTriggerEntry.callback.AddListener(OnPointerClick);
            eventTrigger.triggers.Add(m_TrackedEventTriggerEntry);
        }

        private void RemoveClickListeners()
        {
            if (m_TargetObject == null) return;

            // Remove Button listener
            if (m_TrackedButton != null)
            {
                m_TrackedButton.onClick.RemoveListener(OnTargetClicked);
                m_TrackedButton = null;
            }

            // Remove EventTrigger listener
            if (m_TrackedEventTriggerEntry != null)
            {
                var eventTrigger = m_TargetObject.GetComponent<EventTrigger>();
                if (eventTrigger != null)
                {
                    eventTrigger.triggers.Remove(m_TrackedEventTriggerEntry);
                }

                m_TrackedEventTriggerEntry = null;
            }
        }

        private void OnTargetClicked()
        {
            HandleClick(m_TargetObject);
        }

        private void OnPointerClick(BaseEventData data)
        {
            var eventData = data as PointerEventData;
            if (eventData != null)
            {
                HandleClick(eventData.pointerPress);
            }
        }

        private void HandleClick(GameObject clickedObject)
        {
            // Check if target object was clicked
            bool isTargetClicked = false;

            if (m_RequireExactTarget)
            {
                isTargetClicked = clickedObject == m_TargetObject;
            }
            else
            {
                // Check if it's a child object of the target
                isTargetClicked = clickedObject == m_TargetObject ||
                                  clickedObject.transform.IsChildOf(m_TargetObject.transform);
            }

            if (!isTargetClicked) return;

            // Check time window
            float currentTime = Time.time;
            if (currentTime - m_LastClickTime > m_ClickTimeWindow)
            {
                m_CurrentClickCount = 0; // Reset count
            }

            m_CurrentClickCount++;
            m_LastClickTime = currentTime;

            // Check if click count requirement is met
            if (m_CurrentClickCount >= m_RequiredClickCount)
            {
                m_IsSatisfied = true;
                TriggerConditionChanged();
            }
        }

        /// <summary>
        /// Reset condition state
        /// </summary>
        public void Reset()
        {
            m_CurrentClickCount = 0;
            m_IsSatisfied = false;
            m_TrackedButton = null;
            m_TrackedEventTriggerEntry = null;
            TriggerConditionChanged();
        }

        protected override string GetDefaultDescription()
        {
            if (m_TargetObject == null)
                return $"Click condition: {m_RequiredClickCount} clicks";

            return $"Click {m_TargetObject.name} {m_RequiredClickCount} times";
        }
    }
}