using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UIExt.Guide.Conditions.CompositeConditions
{
    /// <summary>
    /// Composite condition logic type
    /// </summary>
    public enum CompositeLogicType
    {
        AND, // All conditions must be satisfied
        OR, // Any condition satisfies
        XOR, // Exactly one condition satisfies
        NOT // Condition is not satisfied
    }

    /// <summary>
    /// Composite condition
    /// </summary>
    [Serializable]
    public class CompositeCondition : GuideConditionBase
    {
        [SerializeField]
        private CompositeLogicType m_LogicType = CompositeLogicType.AND;

        [SerializeField]
        private bool m_AutoListenSubConditions = true;

        private List<IGuideCondition> m_SubConditions = new List<IGuideCondition>();
        private bool m_IsSatisfied;

        public CompositeLogicType LogicType
        {
            get => m_LogicType;
            set => m_LogicType = value;
        }

        public List<IGuideCondition> SubConditions
        {
            get => m_SubConditions;
            set => m_SubConditions = value ?? new List<IGuideCondition>();
        }

        public bool AutoListenSubConditions
        {
            get => m_AutoListenSubConditions;
            set => m_AutoListenSubConditions = value;
        }

        public CompositeCondition() : base()
        {
        }

        public CompositeCondition(CompositeLogicType logic, params IGuideCondition[] conditions)
            : base($"Composite_{logic}", $"Composite ({logic})")
        {
            m_LogicType = logic;
            m_SubConditions = conditions?.ToList() ?? new List<IGuideCondition>();
        }

        public override bool IsSatisfied()
        {
            return m_IsSatisfied;
        }

        /// <summary>
        /// Composite conditions need periodic state checking to update their satisfaction status
        /// </summary>
        public override bool NeedsStateChecking => true;

        /// <summary>
        /// Perform state checking for composite conditions
        /// </summary>
        public override void PerformStateCheck()
        {
            CheckSatisfaction();
        }

        protected override void OnStartListening()
        {
            if (m_SubConditions == null || m_SubConditions.Count == 0) return;

            // Clean up null conditions
            m_SubConditions.RemoveAll(c => c == null);

            if (m_SubConditions.Count == 0) return;

            // Listen to sub-condition changes
            if (m_AutoListenSubConditions)
            {
                foreach (var condition in m_SubConditions)
                {
                    if (condition != null)
                    {
                        condition.OnConditionChanged += OnSubConditionChanged;
                        condition.StartListening();
                    }
                }
            }

            // Initial check
            CheckSatisfaction();
        }

        protected override void OnStopListening()
        {
            if (m_SubConditions == null) return;

            // Stop listening to sub-conditions
            if (m_AutoListenSubConditions)
            {
                foreach (var condition in m_SubConditions)
                {
                    if (condition != null)
                    {
                        condition.OnConditionChanged -= OnSubConditionChanged;
                        condition.StopListening();
                    }
                }
            }
        }

        private void OnSubConditionChanged(IGuideCondition changedCondition)
        {
            CheckSatisfaction();
        }

        private void CheckSatisfaction()
        {
            if (m_SubConditions == null || m_SubConditions.Count == 0)
            {
                m_IsSatisfied = false;
                return;
            }

            bool newSatisfied = EvaluateConditions();

            if (newSatisfied != m_IsSatisfied)
            {
                m_IsSatisfied = newSatisfied;
                TriggerConditionChanged();
            }
        }

        private bool EvaluateConditions()
        {
            var validConditions = m_SubConditions.Where(c => c != null).ToList();

            if (validConditions.Count == 0) return false;

            switch (m_LogicType)
            {
                case CompositeLogicType.AND:
                    return validConditions.All(c => c.IsSatisfied());

                case CompositeLogicType.OR:
                    return validConditions.Any(c => c.IsSatisfied());

                case CompositeLogicType.XOR:
                    int satisfiedCount = validConditions.Count(c => c.IsSatisfied());
                    return satisfiedCount == 1;

                case CompositeLogicType.NOT:
                    // NOT logic only applies to single condition
                    if (validConditions.Count == 1)
                        return !validConditions[0].IsSatisfied();
                    return false;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Add sub-condition
        /// </summary>
        public void AddSubCondition(IGuideCondition condition)
        {
            if (condition == null) return;

            if (!m_SubConditions.Contains(condition))
            {
                m_SubConditions.Add(condition);

                if (IsListening && m_AutoListenSubConditions)
                {
                    condition.OnConditionChanged += OnSubConditionChanged;
                    condition.StartListening();
                }

                CheckSatisfaction();
            }
        }

        /// <summary>
        /// Remove sub-condition
        /// </summary>
        public void RemoveSubCondition(IGuideCondition condition)
        {
            if (condition == null) return;

            if (m_SubConditions.Contains(condition))
            {
                if (IsListening && m_AutoListenSubConditions)
                {
                    condition.OnConditionChanged -= OnSubConditionChanged;
                    condition.StopListening();
                }

                m_SubConditions.Remove(condition);
                CheckSatisfaction();
            }
        }

        /// <summary>
        /// Clear all sub-conditions
        /// </summary>
        public void ClearSubConditions()
        {
            if (IsListening && m_AutoListenSubConditions)
            {
                foreach (var condition in m_SubConditions)
                {
                    if (condition != null)
                    {
                        condition.OnConditionChanged -= OnSubConditionChanged;
                        condition.StopListening();
                    }
                }
            }

            m_SubConditions.Clear();
            m_IsSatisfied = false;
            TriggerConditionChanged();
        }

        /// <summary>
        /// Get count of satisfied sub-conditions
        /// </summary>
        public int GetSatisfiedConditionCount()
        {
            if (m_SubConditions == null) return 0;
            return m_SubConditions.Count(c => c != null && c.IsSatisfied());
        }

        /// <summary>
        /// Get total sub-condition count
        /// </summary>
        public int GetTotalConditionCount()
        {
            return m_SubConditions?.Count ?? 0;
        }

        protected override string GetDefaultDescription()
        {
            if (m_SubConditions == null || m_SubConditions.Count == 0)
                return $"Composite condition ({m_LogicType}) - no sub-conditions";

            string conditionDesc = string.Join(", ", m_SubConditions
                .Where(c => c != null)
                .Select(c => c.GetDescription()));

            return $"Composite condition ({m_LogicType}): {conditionDesc}";
        }
    }
}