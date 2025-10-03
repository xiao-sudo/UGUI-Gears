using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace UIExt.Guide.Conditions.EventConditions
{
    /// <summary>
    /// Custom event condition
    /// </summary>
    [Serializable]
    public class CustomEventCondition : GuideConditionBase
    {
        [SerializeField]
        private string m_EventKey;

        [SerializeField]
        private bool m_RequireExactMatch = true;

        private object m_EventValue;
        private bool m_IsSatisfied;
        private object m_LastEventValue;

        public string EventKey
        {
            get => m_EventKey;
            set => m_EventKey = value;
        }

        public object EventValue
        {
            get => m_EventValue;
            set => m_EventValue = value;
        }

        public bool RequireExactMatch
        {
            get => m_RequireExactMatch;
            set => m_RequireExactMatch = value;
        }

        public CustomEventCondition() : base()
        {
        }

        public CustomEventCondition(string key, object value, bool exactMatch = true)
            : base($"Custom_{key}", $"CustomEvent {key} = {value}")
        {
            m_EventKey = key;
            m_EventValue = value;
            m_RequireExactMatch = exactMatch;
        }

        public override bool IsSatisfied()
        {
            return m_IsSatisfied;
        }

        protected override void OnStartListening()
        {
            if (string.IsNullOrEmpty(m_EventKey)) return;

            m_IsSatisfied = false;

            // Register to custom event system
            CustomEventSystem.Instance?.RegisterEvent(m_EventKey, OnCustomEventTriggered);
        }

        protected override void OnStopListening()
        {
            if (string.IsNullOrEmpty(m_EventKey)) return;

            // Unregister from custom event system
            CustomEventSystem.Instance?.UnregisterEvent(m_EventKey, OnCustomEventTriggered);
        }

        private void OnCustomEventTriggered(object eventData)
        {
            if (!IsListening) return;

            // Check if event value matches
            bool valueMatches = true;
            if (m_EventValue != null)
            {
                if (m_RequireExactMatch)
                {
                    valueMatches = Equals(eventData, m_EventValue);
                }
                else
                {
                    valueMatches = eventData?.ToString() == m_EventValue?.ToString();
                }
            }

            if (valueMatches)
            {
                m_LastEventValue = eventData;
                m_IsSatisfied = true;
                TriggerConditionChanged();
            }
        }

        /// <summary>
        /// Reset condition state
        /// </summary>
        public void Reset()
        {
            m_IsSatisfied = false;
            m_LastEventValue = null;
            TriggerConditionChanged();
        }

        /// <summary>
        /// Get value from last triggered event
        /// </summary>
        public object GetLastEventValue()
        {
            return m_LastEventValue;
        }

        protected override string GetDefaultDescription()
        {
            if (string.IsNullOrEmpty(m_EventKey))
                return "Custom event condition";

            string valueDesc = m_EventValue != null ? $" = {m_EventValue}" : "";
            return $"Custom event {m_EventKey}{valueDesc}";
        }
    }

    /// <summary>
    /// Custom event system
    /// </summary>
    public class CustomEventSystem : MonoBehaviour
    {
        private static CustomEventSystem instance;
        public static CustomEventSystem Instance => instance;

        private System.Collections.Generic.Dictionary<string, Action<object>> eventHandlers;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                eventHandlers = new System.Collections.Generic.Dictionary<string, Action<object>>();
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void RegisterEvent(string eventKey, Action<object> handler)
        {
            if (string.IsNullOrEmpty(eventKey) || handler == null) return;

            if (!eventHandlers.ContainsKey(eventKey))
            {
                eventHandlers[eventKey] = handler;
            }
            else
            {
                eventHandlers[eventKey] += handler;
            }
        }

        public void UnregisterEvent(string eventKey, Action<object> handler)
        {
            if (string.IsNullOrEmpty(eventKey) || handler == null) return;

            if (eventHandlers.ContainsKey(eventKey))
            {
                eventHandlers[eventKey] -= handler;
                if (eventHandlers[eventKey] == null)
                {
                    eventHandlers.Remove(eventKey);
                }
            }
        }

        public void TriggerEvent(string eventKey, object eventValue = null)
        {
            if (string.IsNullOrEmpty(eventKey)) return;

            if (eventHandlers.ContainsKey(eventKey))
            {
                eventHandlers[eventKey]?.Invoke(eventValue);
            }
        }

        /// <summary>
        /// Set event value (for state tracking)
        /// </summary>
        public void SetEventValue(string eventKey, object eventValue)
        {
            TriggerEvent(eventKey, eventValue);
        }

        /// <summary>
        /// Check if event is registered
        /// </summary>
        public bool IsEventRegistered(string eventKey)
        {
            return !string.IsNullOrEmpty(eventKey) && eventHandlers.ContainsKey(eventKey);
        }
    }
}