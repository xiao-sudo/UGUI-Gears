using System;

namespace UIExt.Base
{
    public interface IAnimation
    {
        /// <summary>
        /// Animation is playing or not
        /// </summary>
        bool IsPlaying { get; }
        
        /// <summary>
        /// Animation is paused or not
        /// </summary>
        bool IsPaused { get; }
        
        /// <summary>
        /// Animation duration in seconds
        /// </summary>
        float Duration { get; }
        
        /// <summary>
        /// Current normalized time (0-1)
        /// </summary>
        float NormalizedTime { get; }
        
        /// <summary>
        /// Animation speed multiplier
        /// </summary>
        float Speed { get; set; }
        
        
        /// <summary>
        /// Initialize the IAnimation
        /// </summary>
        void Initialize();
        
        /// <summary>
        /// Play animation
        /// </summary>
        void Play();
        
        /// <summary>
        /// Pause Animation
        /// </summary>
        void Pause();
        
        /// <summary>
        /// Resume Animation
        /// </summary>
        void Resume();
        
        /// <summary>
        /// Stop Animation
        /// </summary>
        void Stop();
        
        /// <summary>
        /// Set animation completion callback
        /// </summary>
        IAnimation OnComplete(Action onComplete);
        
        /// <summary>
        /// Set animation start callback
        /// </summary>
        IAnimation OnStart(Action onStart);
        
        /// <summary>
        /// Set animation pause callback
        /// </summary>
        IAnimation OnPause(Action onPause);
        
        /// <summary>
        /// Set animation resume callback
        /// </summary>
        IAnimation OnResume(Action onResume);
        
        /// <summary>
        /// Set animation stop callback
        /// </summary>
        IAnimation OnStop(Action onStop);
    }
}