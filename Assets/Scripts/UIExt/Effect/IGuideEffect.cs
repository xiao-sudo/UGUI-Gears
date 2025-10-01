using System;
using UnityEngine;

namespace UIExt.Effect
{
    /// <summary>
    /// Guide effect interface
    /// </summary>
    public interface IGuideEffect
    {
        /// <summary>
        /// Effect target
        /// </summary>
        RectTransform Target { get; }

        /// <summary>
        /// Is effect playing
        /// </summary>
        bool IsPlaying { get; }

        /// <summary>
        /// Play effect
        /// </summary>
        void Play();

        /// <summary>
        /// Stop effect
        /// </summary>
        void Stop();

        /// <summary>
        /// Pause effect
        /// </summary>
        void Pause();

        /// <summary>
        /// Resume effect
        /// </summary>
        void Resume();

        /// <summary>
        /// Set complete callback
        /// </summary>
        IGuideEffect OnComplete(Action onComplete);

        /// <summary>
        /// Set cancel callback
        /// </summary>
        IGuideEffect OnCancel(Action onCancel);
    }
}

