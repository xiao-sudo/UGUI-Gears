using System;
using UnityEngine;

namespace GameGuide.Conditions
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

        [SerializeField]
        protected ConditionCleanupStrategy m_CleanupStrategy = ConditionCleanupStrategy.AutoOnSatisfiedOrTimeout;

        [SerializeField]
        protected float m_TimeoutSeconds = 0f;

        [SerializeField]
        protected float m_RegistrationTime = 0f;

        public string ConditionId => m_ConditionId;
        public bool IsListening { get; protected set; }

        public ConditionCleanupStrategy CleanupStrategy
        {
            get => m_CleanupStrategy;
            set => m_CleanupStrategy = value;
        }

        public float TimeoutSeconds
        {
            get => m_TimeoutSeconds;
            set => m_TimeoutSeconds = Mathf.Max(0f, value);
        }

        public float RegistrationTime
        {
            get => m_RegistrationTime;
            set => m_RegistrationTime = value;
        }

        /// <summary>
        /// Whether this condition needs periodic state checking
        /// Default implementation returns false
        /// </summary>
        public virtual bool NeedsStateChecking => false;

        /// <summary>
        /// Perform state checking if needed
        /// Default implementation does nothing
        /// </summary>
        public virtual void PerformStateCheck()
        {
            // Default implementation - do nothing
        }

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