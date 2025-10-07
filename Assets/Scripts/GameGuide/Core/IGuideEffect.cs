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
    }

    /// <summary>
    /// Resettable effect interface
    /// </summary>
    public interface IResettableEffect
    {
        void Reset();
    }
}
