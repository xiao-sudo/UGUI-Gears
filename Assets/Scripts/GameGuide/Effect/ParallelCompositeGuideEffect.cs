using System;
using System.Collections.Generic;
using GameGuide.Core;
using UIExt.Effect;
using UnityEngine;

namespace GameGuide.Effect
{
    /// <summary>
    /// Parallel Composite Guide Effect - manages multiple guide effects as a single unit (Parallel Execution)
    /// All child effects play simultaneously
    /// </summary>
    public class ParallelCompositeGuideEffect : IGuideEffect, IResettableEffect
    {
        #region Private Fields

        /// <summary>
        /// List of child effects managed by this composite
        /// </summary>
        private List<IGuideEffect> m_ChildEffects = new List<IGuideEffect>();

        /// <summary>
        /// Track if the composite is currently playing
        /// </summary>
        private bool m_IsPlaying = false;

        /// <summary>
        /// Track if the composite is currently paused
        /// </summary>
        private bool m_IsPaused = false;

        /// <summary>
        /// Primary target (returns first child's target if available)
        /// </summary>
        private RectTransform m_Target;

        /// <summary>
        /// Complete callback
        /// </summary>
        private Action m_OnComplete;

        /// <summary>
        /// Cancel callback
        /// </summary>
        private Action m_OnCancel;

        // /// <summary>
        // /// Target finder function
        // /// </summary>
        // private Func<RectTransform> m_TargetFinder;

        #endregion

        #region IGuideEffect Events

        public event Action<IGuideEffect> OnGuideEffectCompleted;
        public event Action<IGuideEffect> OnGuideEffectStarted;
        public event Action<IGuideEffect> OnGuideEffectStopped;

        #endregion

        #region IEffect Properties

        /// <summary>
        /// Returns the first child's target or the explicitly set target
        /// </summary>
        public RectTransform Target
        {
            get
            {
                if (m_Target != null)
                    return m_Target;

                // Return first child's target
                if (m_ChildEffects.Count > 0 && m_ChildEffects[0] != null)
                    return m_ChildEffects[0].Target;

                return null;
            }
        }

        /// <summary>
        /// Returns true if any child effect is playing
        /// </summary>
        public bool IsPlaying => m_IsPlaying;

        #endregion

        #region Public Configuration Methods

        /// <summary>
        /// Add a child effect to the composite
        /// </summary>
        public ParallelCompositeGuideEffect AddEffect(IGuideEffect effect)
        {
            if (effect == null)
            {
                Debug.LogWarning("[CompositeGuideEffect] Cannot add null effect");
                return this;
            }

            if (ReferenceEquals(effect, this))
            {
                Debug.LogError("[CompositeGuideEffect] Cannot add self as child effect");
                return this;
            }

            if (!m_ChildEffects.Contains(effect))
            {
                m_ChildEffects.Add(effect);

                // Subscribe to child effect events
                SubscribeToChildEffect(effect);

                Debug.Log($"[CompositeGuideEffect] Added child effect. Total: {m_ChildEffects.Count}");
            }

            return this;
        }

        /// <summary>
        /// Remove a child effect from the composite
        /// </summary>
        public ParallelCompositeGuideEffect RemoveEffect(IGuideEffect effect)
        {
            if (effect == null)
                return this;

            if (m_ChildEffects.Remove(effect))
            {
                // Unsubscribe from child effect events
                UnsubscribeFromChildEffect(effect);

                Debug.Log($"[CompositeGuideEffect] Removed child effect. Remaining: {m_ChildEffects.Count}");
            }

            return this;
        }

        /// <summary>
        /// Clear all child effects
        /// </summary>
        public ParallelCompositeGuideEffect ClearEffects()
        {
            // Unsubscribe from all child effects
            foreach (var effect in m_ChildEffects)
            {
                UnsubscribeFromChildEffect(effect);
            }

            m_ChildEffects.Clear();
            Debug.Log("[CompositeGuideEffect] Cleared all child effects");

            return this;
        }

        /// <summary>
        /// Get all child effects (read-only)
        /// </summary>
        public IReadOnlyList<IGuideEffect> GetChildEffects()
        {
            return m_ChildEffects.AsReadOnly();
        }

        /// <summary>
        /// Get child effect count
        /// </summary>
        public int GetChildEffectCount()
        {
            return m_ChildEffects.Count;
        }

        #endregion

        #region IEffect Implementation

        /// <summary>
        /// Play all child effects simultaneously
        /// </summary>
        public void Play()
        {
            if (m_IsPlaying)
            {
                Debug.LogWarning("[CompositeGuideEffect] Already playing");
                return;
            }

            if (m_ChildEffects.Count == 0)
            {
                Debug.LogWarning("[CompositeGuideEffect] No child effects to play");
                return;
            }

            m_IsPlaying = true;
            m_IsPaused = false;

            Debug.Log($"[CompositeGuideEffect] Playing {m_ChildEffects.Count} effects in parallel mode");

            // Invoke started event
            OnGuideEffectStarted?.Invoke(this);

            // Play all effects simultaneously
            foreach (var effect in m_ChildEffects)
            {
                if (effect != null)
                {
                    effect.Play();
                }
            }
        }

        /// <summary>
        /// Stop all child effects
        /// </summary>
        public void Stop()
        {
            if (!m_IsPlaying)
                return;

            Debug.Log("[CompositeGuideEffect] Stopping all effects");

            // Stop all child effects
            foreach (var effect in m_ChildEffects)
            {
                if (effect != null && effect.IsPlaying)
                {
                    effect.Stop();
                }
            }

            m_IsPlaying = false;
            m_IsPaused = false;

            // Invoke stopped event
            OnGuideEffectStopped?.Invoke(this);
        }

        /// <summary>
        /// Pause all child effects
        /// </summary>
        public void Pause()
        {
            if (!m_IsPlaying || m_IsPaused)
                return;

            Debug.Log("[CompositeGuideEffect] Pausing all effects");

            foreach (var effect in m_ChildEffects)
            {
                if (effect != null && effect.IsPlaying)
                {
                    effect.Pause();
                }
            }

            m_IsPaused = true;
        }

        /// <summary>
        /// Resume all child effects
        /// </summary>
        public void Resume()
        {
            if (!m_IsPlaying || !m_IsPaused)
                return;

            Debug.Log("[CompositeGuideEffect] Resuming all effects");

            foreach (var effect in m_ChildEffects)
            {
                if (effect != null)
                {
                    effect.Resume();
                }
            }

            m_IsPaused = false;
        }

        /// <summary>
        /// Set complete callback
        /// </summary>
        public IEffect OnComplete(Action onComplete)
        {
            m_OnComplete = onComplete;
            return this;
        }

        /// <summary>
        /// Set cancel callback
        /// </summary>
        public IEffect OnCancel(Action onCancel)
        {
            m_OnCancel = onCancel;
            return this;
        }

        /// <summary>
        /// Set target finder function
        /// </summary>
        public IEffect SetTargetFinder(Func<RectTransform> targetFinder)
        {
            return this;
        }

        #endregion

        #region IResettableEffect Implementation

        /// <summary>
        /// Reset all child effects
        /// </summary>
        public void Reset()
        {
            Debug.Log("[CompositeGuideEffect] Resetting all effects");

            Stop();

            // Reset all resettable child effects
            foreach (var effect in m_ChildEffects)
            {
                if (effect is IResettableEffect resettableEffect)
                {
                    resettableEffect.Reset();
                }
            }
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Subscribe to child effect events
        /// </summary>
        private void SubscribeToChildEffect(IGuideEffect effect)
        {
            if (effect == null)
                return;

            effect.OnGuideEffectCompleted += OnChildEffectCompleted;
            effect.OnGuideEffectStarted += OnChildEffectStarted;
            effect.OnGuideEffectStopped += OnChildEffectStopped;
        }

        /// <summary>
        /// Unsubscribe from child effect events
        /// </summary>
        private void UnsubscribeFromChildEffect(IGuideEffect effect)
        {
            if (effect == null)
                return;

            effect.OnGuideEffectCompleted -= OnChildEffectCompleted;
            effect.OnGuideEffectStarted -= OnChildEffectStarted;
            effect.OnGuideEffectStopped -= OnChildEffectStopped;
        }

        #endregion

        #region Child Effect Event Handlers

        /// <summary>
        /// Called when a child effect completes
        /// </summary>
        private void OnChildEffectCompleted(IGuideEffect effect)
        {
            Debug.Log($"[CompositeGuideEffect] Child effect completed: {effect}");

            // Check if all effects are completed (parallel mode)
            bool allCompleted = true;
            foreach (var childEffect in m_ChildEffects)
            {
                if (childEffect != null && childEffect.IsPlaying)
                {
                    allCompleted = false;
                    break;
                }
            }

            if (allCompleted)
            {
                OnAllEffectsCompleted();
            }
        }

        /// <summary>
        /// Called when a child effect starts
        /// </summary>
        private void OnChildEffectStarted(IGuideEffect effect)
        {
            Debug.Log($"[CompositeGuideEffect] Child effect started: {effect}");
        }

        /// <summary>
        /// Called when a child effect stops
        /// </summary>
        private void OnChildEffectStopped(IGuideEffect effect)
        {
            Debug.Log($"[CompositeGuideEffect] Child effect stopped: {effect}");
        }

        /// <summary>
        /// Called when all child effects are completed
        /// </summary>
        private void OnAllEffectsCompleted()
        {
            Debug.Log("[CompositeGuideEffect] All effects completed");

            m_IsPlaying = false;
            m_IsPaused = false;

            // Invoke callbacks
            m_OnComplete?.Invoke();
            OnGuideEffectCompleted?.Invoke(this);
        }

        #endregion

        #region Debug

        public override string ToString()
        {
            return $"[CompositeGuideEffect] {m_ChildEffects.Count} effects (Parallel), Playing: {m_IsPlaying}";
        }

        #endregion
    }
}