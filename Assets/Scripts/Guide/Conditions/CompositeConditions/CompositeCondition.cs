using System.Collections.Generic;
using System.Linq;
using UIExt.Guide.Conditions;
using UnityEngine;

namespace Guide.Conditions.CompositeConditions
{
    /// <summary>
    /// Composite condition logic type
    /// </summary>
    public enum CompositeLogicType
    {
        AND,    // All conditions must be satisfied
        OR,     // Any condition satisfies
        XOR,    // Exactly one condition satisfies
        NOT     // Condition is not satisfied
    }
    
    /// <summary>
    /// Composite condition
    /// </summary>
    public class CompositeCondition : GuideConditionBase
    {
        [SerializeField] private CompositeLogicType logicType = CompositeLogicType.AND;
        [SerializeField] private List<IGuideCondition> subConditions = new List<IGuideCondition>();
        [SerializeField] private bool autoListenSubConditions = true;
        
        private bool isSatisfied;
        
        public CompositeLogicType LogicType
        {
            get => logicType;
            set => logicType = value;
        }
        
        public List<IGuideCondition> SubConditions
        {
            get => subConditions;
            set => subConditions = value ?? new List<IGuideCondition>();
        }
        
        public bool AutoListenSubConditions
        {
            get => autoListenSubConditions;
            set => autoListenSubConditions = value;
        }
        
        public CompositeCondition() : base()
        {
        }
        
        public CompositeCondition(CompositeLogicType logic, params IGuideCondition[] conditions) 
            : base($"Composite_{logic}", $"复合条件 ({logic})")
        {
            logicType = logic;
            subConditions = conditions?.ToList() ?? new List<IGuideCondition>();
        }
        
        public override bool IsSatisfied()
        {
            return isSatisfied;
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
            if (subConditions == null || subConditions.Count == 0) return;
            
            // Clean up null conditions
            subConditions.RemoveAll(c => c == null);
            
            if (subConditions.Count == 0) return;
            
            // Listen to sub-condition changes
            if (autoListenSubConditions)
            {
                foreach (var condition in subConditions)
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
            if (subConditions == null) return;
            
            // Stop listening to sub-conditions
            if (autoListenSubConditions)
            {
                foreach (var condition in subConditions)
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
            if (subConditions == null || subConditions.Count == 0)
            {
                isSatisfied = false;
                return;
            }
            
            bool newSatisfied = EvaluateConditions();
            
            if (newSatisfied != isSatisfied)
            {
                isSatisfied = newSatisfied;
                TriggerConditionChanged();
            }
        }
        
        private bool EvaluateConditions()
        {
            var validConditions = subConditions.Where(c => c != null).ToList();
            
            if (validConditions.Count == 0) return false;
            
            switch (logicType)
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
            
            if (!subConditions.Contains(condition))
            {
                subConditions.Add(condition);
                
                if (IsListening && autoListenSubConditions)
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
            
            if (subConditions.Contains(condition))
            {
                if (IsListening && autoListenSubConditions)
                {
                    condition.OnConditionChanged -= OnSubConditionChanged;
                    condition.StopListening();
                }
                
                subConditions.Remove(condition);
                CheckSatisfaction();
            }
        }
        
        /// <summary>
        /// Clear all sub-conditions
        /// </summary>
        public void ClearSubConditions()
        {
            if (IsListening && autoListenSubConditions)
            {
                foreach (var condition in subConditions)
                {
                    if (condition != null)
                    {
                        condition.OnConditionChanged -= OnSubConditionChanged;
                        condition.StopListening();
                    }
                }
            }
            
            subConditions.Clear();
            isSatisfied = false;
            TriggerConditionChanged();
        }
        
        /// <summary>
        /// Get count of satisfied sub-conditions
        /// </summary>
        public int GetSatisfiedConditionCount()
        {
            if (subConditions == null) return 0;
            return subConditions.Count(c => c != null && c.IsSatisfied());
        }
        
        /// <summary>
        /// Get total sub-condition count
        /// </summary>
        public int GetTotalConditionCount()
        {
            return subConditions?.Count ?? 0;
        }
        
        protected override string GetDefaultDescription()
        {
            if (subConditions == null || subConditions.Count == 0)
                return $"Composite condition ({logicType}) - no sub-conditions";

            string conditionDesc = string.Join(", ", subConditions
                .Where(c => c != null)
                .Select(c => c.GetDescription()));

            return $"Composite condition ({logicType}): {conditionDesc}";
        }
    }
}

