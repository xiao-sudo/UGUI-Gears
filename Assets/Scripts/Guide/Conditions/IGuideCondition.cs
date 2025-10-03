using System;

namespace UIExt.Guide.Conditions
{
    /// <summary>
    /// Guide condition interface
    /// </summary>
    public interface IGuideCondition
    {
        /// <summary>
        /// Condition ID for identification and debugging
        /// </summary>
        string ConditionId { get; }
        
        /// <summary>
        /// Check if the condition is satisfied
        /// </summary>
        /// <returns>true if condition is satisfied, false otherwise</returns>
        bool IsSatisfied();
        
        /// <summary>
        /// Event triggered when condition state changes
        /// </summary>
        event Action<IGuideCondition> OnConditionChanged;
        
        /// <summary>
        /// Start listening for condition changes
        /// </summary>
        void StartListening();
        
        /// <summary>
        /// Stop listening for condition changes
        /// </summary>
        void StopListening();
        
        /// <summary>
        /// Whether currently listening for changes
        /// </summary>
        bool IsListening { get; }
        
        /// <summary>
        /// Get description of the condition
        /// </summary>
        string GetDescription();
    }
}

