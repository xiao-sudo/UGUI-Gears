using System;
using UnityEngine;

namespace UIExt.Effect
{
    /// <summary>
    /// effect interface
    /// </summary>
    public interface IEffect
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
        IEffect OnComplete(Action onComplete);

        /// <summary>
        /// Set cancel callback
        /// </summary>
        IEffect OnCancel(Action onCancel);
    }
}

