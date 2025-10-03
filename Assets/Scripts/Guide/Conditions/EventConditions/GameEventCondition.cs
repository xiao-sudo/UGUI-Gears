using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace UIExt.Guide.Conditions.EventConditions
{
    /// <summary>
    /// Game event condition
    /// </summary>
    [Serializable]
    public class GameEventCondition : GuideConditionBase
    {
        [SerializeField]
        private string m_EventName;

        [SerializeField]
        private bool m_RequireExactMatch = true;

        private object m_ExpectedData;
        private bool m_IsSatisfied;
        private object m_LastEventData;

        public string EventName
        {
            get => m_EventName;
            set => m_EventName = value;
        }

        public object ExpectedData
        {
            get => m_ExpectedData;
            set => m_ExpectedData = value;
        }

        public bool RequireExactMatch
        {
            get => m_RequireExactMatch;
            set => m_RequireExactMatch = value;
        }

        public GameEventCondition() : base()
        {
        }

        public GameEventCondition(string eventName, object expectedData = null, bool exactMatch = true)
            : base($"Event_{eventName}", $"GameEvent {eventName}")
        {
            m_EventName = eventName;
            m_ExpectedData = expectedData;
            m_RequireExactMatch = exactMatch;
        }

        public override bool IsSatisfied()
        {
            return m_IsSatisfied;
        }

        protected override void OnStartListening()
        {
            if (string.IsNullOrEmpty(m_EventName)) return;

            m_IsSatisfied = false;

            // Register to event system
            EventManager.Instance?.RegisterEvent(m_EventName, OnEventTriggered);
        }

        protected override void OnStopListening()
        {
            if (string.IsNullOrEmpty(m_EventName)) return;

            // Unregister from event system
            EventManager.Instance?.UnregisterEvent(m_EventName, OnEventTriggered);
        }

        private void OnEventTriggered(object eventData)
        {
            if (!IsListening) return;

            // Check if event data matches
            bool dataMatches = true;
            if (m_ExpectedData != null)
            {
                if (m_RequireExactMatch)
                {
                    dataMatches = Equals(eventData, m_ExpectedData);
                }
                else
                {
                    dataMatches = eventData?.ToString() == m_ExpectedData?.ToString();
                }
            }

            if (dataMatches)
            {
                m_LastEventData = eventData;
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
            m_LastEventData = null;
            TriggerConditionChanged();
        }

        /// <summary>
        /// Get data from last triggered event
        /// </summary>
        public object GetLastEventData()
        {
            return m_LastEventData;
        }

        protected override string GetDefaultDescription()
        {
            if (string.IsNullOrEmpty(m_EventName))
                return "Game event condition";

            string dataDesc = m_ExpectedData != null ? $" (data: {m_ExpectedData})" : "";
            return $"Game event {m_EventName} triggered{dataDesc}";
        }
    }

    /// <summary>
    /// Simple event manager (example implementation)
    /// </summary>
    public class EventManager : MonoBehaviour
    {
        private static EventManager INSTANCE;
        public static EventManager Instance => INSTANCE;

        private System.Collections.Generic.Dictionary<string, Action<object>> m_EventHandlers;

        private void Awake()
        {
            if (INSTANCE == null)
            {
                INSTANCE = this;
                m_EventHandlers = new System.Collections.Generic.Dictionary<string, Action<object>>();
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void RegisterEvent(string eventName, Action<object> handler)
        {
            if (string.IsNullOrEmpty(eventName) || handler == null) return;

            if (!m_EventHandlers.ContainsKey(eventName))
            {
                m_EventHandlers[eventName] = handler;
            }
            else
            {
                m_EventHandlers[eventName] += handler;
            }
        }

        public void UnregisterEvent(string eventName, Action<object> handler)
        {
            if (string.IsNullOrEmpty(eventName) || handler == null) return;

            if (m_EventHandlers.ContainsKey(eventName))
            {
                m_EventHandlers[eventName] -= handler;
                if (m_EventHandlers[eventName] == null)
                {
                    m_EventHandlers.Remove(eventName);
                }
            }
        }

        public void TriggerEvent(string eventName, object eventData = null)
        {
            if (string.IsNullOrEmpty(eventName)) return;

            if (m_EventHandlers.ContainsKey(eventName))
            {
                m_EventHandlers[eventName]?.Invoke(eventData);
            }
        }
    }
}