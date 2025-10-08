using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameGuide.Conditions.UIConditions
{
    /// <summary>
    /// Click handler that manages click counting, time window, and event listening.
    /// This component can be reused by both UIClickCondition and UIClickConditionDynamicTarget.
    /// </summary>
    [Serializable]
    public class ClickHandler
    {
        #region Serialized Fields

        [SerializeField]
        private int m_RequiredClickCount = 1;

        [SerializeField]
        private bool m_RequireExactTarget = true;

        [SerializeField]
        private float m_ClickTimeWindow = 1f;

        #endregion

        #region Private Fields

        // Click state tracking
        private int m_CurrentClickCount;
        private float m_LastClickTime;
        private bool m_IsSatisfied;

        // Listener management
        private GameObject m_CurrentTargetObject;
        private Button m_TrackedButton;
        private EventTrigger.Entry m_TrackedEventTriggerEntry;

        #endregion

        #region Properties

        public int RequiredClickCount
        {
            get => m_RequiredClickCount;
            set => m_RequiredClickCount = value;
        }

        public bool RequireExactTarget
        {
            get => m_RequireExactTarget;
            set => m_RequireExactTarget = value;
        }

        public float ClickTimeWindow
        {
            get => m_ClickTimeWindow;
            set => m_ClickTimeWindow = value;
        }

        public int CurrentClickCount => m_CurrentClickCount;

        public bool IsSatisfied => m_IsSatisfied;

        public GameObject CurrentTargetObject => m_CurrentTargetObject;

        #endregion

        #region Events

        /// <summary>
        /// Event triggered when click condition is satisfied
        /// </summary>
        public event Action OnClickSatisfied;

        /// <summary>
        /// Event triggered on each valid click
        /// </summary>
        public event Action<int, int> OnClick; // (currentCount, requiredCount)

        #endregion

        #region Public Methods

        /// <summary>
        /// Reset click state
        /// </summary>
        public void Reset()
        {
            m_CurrentClickCount = 0;
            m_IsSatisfied = false;
            m_LastClickTime = 0f;
        }

        /// <summary>
        /// Attach click listeners to target object
        /// </summary>
        public void AttachListeners(GameObject target)
        {
            if (target == null) return;

            // Detach old listeners first if any
            if (m_CurrentTargetObject != null)
            {
                DetachListeners();
            }

            m_CurrentTargetObject = target;

            var button = target.GetComponent<Button>();
            var eventTrigger = target.GetComponent<EventTrigger>();

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
                eventTrigger = target.AddComponent<EventTrigger>();
                AddEventTriggerEntry(eventTrigger);
            }
        }

        /// <summary>
        /// Detach click listeners from current target
        /// </summary>
        public void DetachListeners()
        {
            // Remove Button listener
            if (m_TrackedButton != null)
            {
                m_TrackedButton.onClick.RemoveListener(OnTargetClicked);
                m_TrackedButton = null;
            }

            // Remove EventTrigger listener
            if (m_TrackedEventTriggerEntry != null && m_CurrentTargetObject != null)
            {
                var eventTrigger = m_CurrentTargetObject.GetComponent<EventTrigger>();
                if (eventTrigger != null)
                {
                    eventTrigger.triggers.Remove(m_TrackedEventTriggerEntry);
                }
                m_TrackedEventTriggerEntry = null;
            }

            m_CurrentTargetObject = null;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Add EventTrigger entry for pointer click
        /// </summary>
        private void AddEventTriggerEntry(EventTrigger eventTrigger)
        {
            m_TrackedEventTriggerEntry = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerClick
            };
            m_TrackedEventTriggerEntry.callback.AddListener(OnPointerClick);
            eventTrigger.triggers.Add(m_TrackedEventTriggerEntry);
        }

        /// <summary>
        /// Button click callback
        /// </summary>
        private void OnTargetClicked()
        {
            HandleClick(m_CurrentTargetObject);
        }

        /// <summary>
        /// EventTrigger pointer click callback
        /// </summary>
        private void OnPointerClick(BaseEventData data)
        {
            var eventData = data as PointerEventData;
            if (eventData != null)
            {
                HandleClick(eventData.pointerPress);
            }
        }

        /// <summary>
        /// Handle click event and check if conditions are met
        /// </summary>
        private void HandleClick(GameObject clickedObject)
        {
            if (clickedObject == null) return;

            // Check if target was clicked
            bool isTargetClicked = false;

            if (m_RequireExactTarget)
            {
                isTargetClicked = clickedObject == m_CurrentTargetObject;
            }
            else
            {
                // Check if it's the target or a child of the target
                isTargetClicked = clickedObject == m_CurrentTargetObject ||
                                  (m_CurrentTargetObject != null && 
                                   clickedObject.transform.IsChildOf(m_CurrentTargetObject.transform));
            }

            if (!isTargetClicked) return;

            // Check time window for consecutive clicks
            float currentTime = Time.time;
            if (currentTime - m_LastClickTime > m_ClickTimeWindow)
            {
                m_CurrentClickCount = 0; // Reset count if outside time window
            }

            m_CurrentClickCount++;
            m_LastClickTime = currentTime;

            // Trigger click event
            OnClick?.Invoke(m_CurrentClickCount, m_RequiredClickCount);

            // Check if click count requirement is met
            if (m_CurrentClickCount >= m_RequiredClickCount)
            {
                m_IsSatisfied = true;
                OnClickSatisfied?.Invoke();
            }
        }

        #endregion

        #region Debug

#if UNITY_EDITOR
        /// <summary>
        /// Get debug info string
        /// </summary>
        public string GetDebugInfo()
        {
            return $"ClickHandler: {m_CurrentClickCount}/{m_RequiredClickCount} clicks, " +
                   $"Satisfied: {m_IsSatisfied}, " +
                   $"Target: {m_CurrentTargetObject?.name ?? "null"}";
        }
#endif

        #endregion
    }
}

