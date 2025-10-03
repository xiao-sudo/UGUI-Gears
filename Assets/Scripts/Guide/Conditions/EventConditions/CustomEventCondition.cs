using System;
using UnityEngine;

namespace UIExt.Guide.Conditions.EventConditions
{
    /// <summary>
    /// Custom event condition
    /// </summary>
    public class CustomEventCondition : GuideConditionBase
    {
        [SerializeField] private string eventKey;
        [SerializeField] private object eventValue;
        [SerializeField] private bool requireExactMatch = true;
        
        private bool isSatisfied;
        private object lastEventValue;
        
        public string EventKey
        {
            get => eventKey;
            set => eventKey = value;
        }
        
        public object EventValue
        {
            get => eventValue;
            set => eventValue = value;
        }
        
        public bool RequireExactMatch
        {
            get => requireExactMatch;
            set => requireExactMatch = value;
        }
        
        public CustomEventCondition() : base()
        {
        }
        
        public CustomEventCondition(string key, object value, bool exactMatch = true) 
            : base($"Custom_{key}", $"自定义事件 {key} = {value}")
        {
            eventKey = key;
            eventValue = value;
            requireExactMatch = exactMatch;
        }
        
        public override bool IsSatisfied()
        {
            return isSatisfied;
        }
        
        protected override void OnStartListening()
        {
            if (string.IsNullOrEmpty(eventKey)) return;
            
            isSatisfied = false;
            
            // Register to custom event system
            CustomEventSystem.Instance?.RegisterEvent(eventKey, OnCustomEventTriggered);
        }
        
        protected override void OnStopListening()
        {
            if (string.IsNullOrEmpty(eventKey)) return;
            
            // Unregister from custom event system
            CustomEventSystem.Instance?.UnregisterEvent(eventKey, OnCustomEventTriggered);
        }
        
        private void OnCustomEventTriggered(object eventData)
        {
            if (!IsListening) return;
            
            // Check if event value matches
            bool valueMatches = true;
            if (eventValue != null)
            {
                if (requireExactMatch)
                {
                    valueMatches = Equals(eventData, eventValue);
                }
                else
                {
                    valueMatches = eventData?.ToString() == eventValue?.ToString();
                }
            }
            
            if (valueMatches)
            {
                lastEventValue = eventData;
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
            lastEventValue = null;
            TriggerConditionChanged();
        }
        
        /// <summary>
        /// Get value from last triggered event
        /// </summary>
        public object GetLastEventValue()
        {
            return lastEventValue;
        }
        
        protected override string GetDefaultDescription()
        {
            if (string.IsNullOrEmpty(eventKey))
                return "Custom event condition";

            string valueDesc = eventValue != null ? $" = {eventValue}" : "";
            return $"Custom event {eventKey}{valueDesc}";
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

