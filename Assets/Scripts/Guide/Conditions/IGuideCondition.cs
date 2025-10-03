using System;

namespace UIExt.Guide.Conditions
{
    /// <summary>
    /// Condition cleanup strategy
    /// </summary>
    public enum ConditionCleanupStrategy
    {
        /// <summary>
        /// Manual cleanup - requires explicit unregister call
        /// </summary>
        Manual,
        
        /// <summary>
        /// Auto cleanup when condition is satisfied
        /// </summary>
        AutoOnSatisfied,
        
        /// <summary>
        /// Auto cleanup when timeout is reached
        /// </summary>
        AutoOnTimeout,
        
        /// <summary>
        /// Auto cleanup when condition is satisfied OR timeout is reached (whichever comes first)
        /// </summary>
        AutoOnSatisfiedOrTimeout,
        
        /// <summary>
        /// Persistent condition - never auto cleanup
        /// </summary>
        Persistent
    }

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
        
        /// <summary>
        /// Cleanup strategy for this condition
        /// </summary>
        ConditionCleanupStrategy CleanupStrategy { get; set; }
        
        /// <summary>
        /// Timeout in seconds for AutoOnTimeout strategy (0 = no timeout)
        /// </summary>
        float TimeoutSeconds { get; set; }
        
        /// <summary>
        /// Time when condition was registered (for timeout calculation)
        /// </summary>
        float RegistrationTime { get; set; }
        
        /// <summary>
        /// Whether this condition needs periodic state checking
        /// </summary>
        bool NeedsStateChecking { get; }
        
        /// <summary>
        /// Perform state checking if needed
        /// </summary>
        void PerformStateCheck();
    }
}

