using System;
using UnityEngine;

namespace UIExt.Guide.Conditions
{
    /// <summary>
    /// Base class for guide conditions
    /// </summary>
    [Serializable]
    public abstract class GuideConditionBase : IGuideCondition
    {
        [SerializeField]
        protected string m_ConditionId;

        [SerializeField]
        protected string m_Description;

        public string ConditionId => m_ConditionId;
        public bool IsListening { get; protected set; }

        public event Action<IGuideCondition> OnConditionChanged;

        protected GuideConditionBase()
        {
            m_ConditionId = "-1";
            m_Description = string.Empty;
        }

        protected GuideConditionBase(string id, string desc = "")
        {
            m_ConditionId = string.IsNullOrEmpty(id) ? $"{GetType().Name}-{id}" : id;
            m_Description = desc;
        }

        public abstract bool IsSatisfied();

        public virtual void StartListening()
        {
            if (IsListening) return;

            IsListening = true;
            OnStartListening();
        }

        public virtual void StopListening()
        {
            if (!IsListening) return;

            IsListening = false;
            OnStopListening();
        }

        public virtual string GetDescription()
        {
            return string.IsNullOrEmpty(m_Description) ? GetDefaultDescription() : m_Description;
        }

        protected virtual string GetDefaultDescription()
        {
            return GetType().Name;
        }

        /// <summary>
        /// Implementation for starting to listen
        /// </summary>
        protected abstract void OnStartListening();

        /// <summary>
        /// Implementation for stopping to listen
        /// </summary>
        protected abstract void OnStopListening();

        /// <summary>
        /// Trigger condition changed event
        /// </summary>
        protected virtual void TriggerConditionChanged()
        {
            OnConditionChanged?.Invoke(this);
        }

        public override string ToString()
        {
            return $"[{GetType().Name}] {ConditionId}: {GetDescription()}";
        }
    }
}