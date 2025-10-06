using System;
using UIExt.Effect;

namespace GameGuide.Core
{
    /// <summary>
    /// Guide effect interface extension
    /// </summary>
    public interface IGuideEffect : IEffect
    {
        // Guide-specific events
        event Action<IGuideEffect> OnGuideEffectCompleted;
        event Action<IGuideEffect> OnGuideEffectStarted;
        event Action<IGuideEffect> OnGuideEffectStopped;
        
        // Set guide item reference
        void SetGuideItem(IGuideItem guideItem);
        
        // Guide-specific state
        bool IsGuideActive { get; }
    }

    /// <summary>
    /// Resettable effect interface
    /// </summary>
    public interface IResettableEffect
    {
        void Reset();
    }
}
