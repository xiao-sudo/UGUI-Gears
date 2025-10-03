using System;
using UnityEngine;

namespace UIExt.Guide.Conditions.EventConditions
{
    /// <summary>
    /// Game event condition
    /// </summary>
    public class GameEventCondition : GuideConditionBase
    {
        [SerializeField] private string eventName;
        [SerializeField] private object expectedData;
        [SerializeField] private bool requireExactMatch = true;
        
        private bool isSatisfied;
        private object lastEventData;
        
        public string EventName
        {
            get => eventName;
            set => eventName = value;
        }
        
        public object ExpectedData
        {
            get => expectedData;
            set => expectedData = value;
        }
        
        public bool RequireExactMatch
        {
            get => requireExactMatch;
            set => requireExactMatch = value;
        }
        
        public GameEventCondition() : base()
        {
        }
        
        public GameEventCondition(string eventName, object expectedData = null, bool exactMatch = true) 
            : base($"Event_{eventName}", $"游戏事件 {eventName} 触发")
        {
            this.eventName = eventName;
            this.expectedData = expectedData;
            this.requireExactMatch = exactMatch;
        }
        
        public override bool IsSatisfied()
        {
            return isSatisfied;
        }
        
        protected override void OnStartListening()
        {
            if (string.IsNullOrEmpty(eventName)) return;
            
            isSatisfied = false;
            
            // Register to event system
            EventManager.Instance?.RegisterEvent(eventName, OnEventTriggered);
        }
        
        protected override void OnStopListening()
        {
            if (string.IsNullOrEmpty(eventName)) return;
            
            // Unregister from event system
            EventManager.Instance?.UnregisterEvent(eventName, OnEventTriggered);
        }
        
        private void OnEventTriggered(object eventData)
        {
            if (!IsListening) return;
            
            // Check if event data matches
            bool dataMatches = true;
            if (expectedData != null)
            {
                if (requireExactMatch)
                {
                    dataMatches = Equals(eventData, expectedData);
                }
                else
                {
                    dataMatches = eventData?.ToString() == expectedData?.ToString();
                }
            }
            
            if (dataMatches)
            {
                lastEventData = eventData;
                isSatisfied = true;
                TriggerConditionChanged();
            }
        }
        
        /// <summary>
        /// Reset condition state
        /// </summary>
        public void Reset()
        {
            isSatisfied = false;
            lastEventData = null;
            TriggerConditionChanged();
        }
        
        /// <summary>
        /// Get data from last triggered event
        /// </summary>
        public object GetLastEventData()
        {
            return lastEventData;
        }
        
        protected override string GetDefaultDescription()
        {
            if (string.IsNullOrEmpty(eventName))
                return "Game event condition";

            string dataDesc = expectedData != null ? $" (data: {expectedData})" : "";
            return $"Game event {eventName} triggered{dataDesc}";
        }
    }
    
    /// <summary>
    /// Simple event manager (example implementation)
    /// </summary>
    public class EventManager : MonoBehaviour
    {
        private static EventManager instance;
        public static EventManager Instance => instance;
        
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
        
        public void RegisterEvent(string eventName, Action<object> handler)
        {
            if (string.IsNullOrEmpty(eventName) || handler == null) return;
            
            if (!eventHandlers.ContainsKey(eventName))
            {
                eventHandlers[eventName] = handler;
            }
            else
            {
                eventHandlers[eventName] += handler;
            }
        }
        
        public void UnregisterEvent(string eventName, Action<object> handler)
        {
            if (string.IsNullOrEmpty(eventName) || handler == null) return;
            
            if (eventHandlers.ContainsKey(eventName))
            {
                eventHandlers[eventName] -= handler;
                if (eventHandlers[eventName] == null)
                {
                    eventHandlers.Remove(eventName);
                }
            }
        }
        
        public void TriggerEvent(string eventName, object eventData = null)
        {
            if (string.IsNullOrEmpty(eventName)) return;
            
            if (eventHandlers.ContainsKey(eventName))
            {
                eventHandlers[eventName]?.Invoke(eventData);
            }
        }
    }
}

