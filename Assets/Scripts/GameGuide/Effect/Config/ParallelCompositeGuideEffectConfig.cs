using System;
using System.Collections.Generic;
using GameGuide.Core;
using UnityEngine;

namespace GameGuide.Effect.Config
{
    /// <summary>
    /// Configuration for ParallelCompositeGuideEffect
    /// Manages multiple effect-config pairs for parallel execution
    /// </summary>
    [Serializable]
    public class ParallelCompositeGuideEffectConfig : IGuideEffectConfig
    {
        #region Nested Class

        /// <summary>
        /// Represents a single effect-config pair
        /// </summary>
        [Serializable]
        public class EffectConfigPair
        {
            /// <summary>
            /// The effect instance
            /// </summary>
            public IGuideEffect Effect;

            /// <summary>
            /// The configuration for the effect
            /// </summary>
            public IGuideEffectConfig Config;

            public EffectConfigPair(IGuideEffect effect, IGuideEffectConfig config)
            {
                Effect = effect;
                Config = config;
            }
        }

        #endregion

        #region Private Fields

        /// <summary>
        /// List of effect-config pairs
        /// </summary>
        private List<EffectConfigPair> m_EffectConfigPairs = new();

        #endregion

        #region Public Configuration Methods

        /// <summary>
        /// Add an effect-config pair to the composite
        /// </summary>
        /// <param name="effect">The guide effect instance</param>
        /// <param name="config">The configuration for the effect</param>
        public ParallelCompositeGuideEffectConfig AddEffectConfig(IGuideEffect effect, IGuideEffectConfig config)
        {
            if (effect == null)
            {
                Debug.LogWarning("[CompositeGuideEffectConfig] Cannot add null effect");
                return this;
            }

            m_EffectConfigPairs.Add(new EffectConfigPair(effect, config));
            Debug.Log($"[CompositeGuideEffectConfig] Added effect-config pair. Total: {m_EffectConfigPairs.Count}");

            return this;
        }

        /// <summary>
        /// Add multiple effect-config pairs at once
        /// </summary>
        public ParallelCompositeGuideEffectConfig AddEffectConfigs(
            params (IGuideEffect effect, IGuideEffectConfig config)[] pairs)
        {
            foreach (var pair in pairs)
            {
                AddEffectConfig(pair.effect, pair.config);
            }

            return this;
        }

        /// <summary>
        /// Remove an effect-config pair
        /// </summary>
        public ParallelCompositeGuideEffectConfig RemoveEffectConfig(IGuideEffect effect)
        {
            if (effect == null)
                return this;

            m_EffectConfigPairs.RemoveAll(pair => pair.Effect == effect);
            Debug.Log(
                $"[CompositeGuideEffectConfig] Removed effect-config pair. Remaining: {m_EffectConfigPairs.Count}");

            return this;
        }

        /// <summary>
        /// Clear all effect-config pairs
        /// </summary>
        public ParallelCompositeGuideEffectConfig ClearEffectConfigs()
        {
            m_EffectConfigPairs.Clear();
            Debug.Log("[CompositeGuideEffectConfig] Cleared all effect-config pairs");
            return this;
        }

        /// <summary>
        /// Get all effect-config pairs (read-only)
        /// </summary>
        public IReadOnlyList<EffectConfigPair> GetEffectConfigPairs()
        {
            return m_EffectConfigPairs.AsReadOnly();
        }

        /// <summary>
        /// Get effect count
        /// </summary>
        public int GetEffectCount()
        {
            return m_EffectConfigPairs.Count;
        }

        #endregion

        #region IGuideEffectConfig Implementation

        /// <summary>
        /// Apply configuration to the composite guide effect
        /// </summary>
        /// <param name="guideEffect">The guide effect to configure (must be CompositeGuideEffect)</param>
        public void Apply(IGuideEffect guideEffect)
        {
            if (guideEffect == null)
            {
                Debug.LogError("[CompositeGuideEffectConfig] Cannot apply config to null effect");
                return;
            }

            if (!(guideEffect is ParallelCompositeGuideEffect composite))
            {
                Debug.LogError(
                    $"[CompositeGuideEffectConfig] Effect must be CompositeGuideEffect, but got: {guideEffect.GetType().Name}");
                return;
            }

            Apply(composite);
        }

        /// <summary>
        /// Apply configuration to the composite guide effect (strongly typed)
        /// </summary>
        public void Apply(ParallelCompositeGuideEffect parallelComposite)
        {
            if (parallelComposite == null)
            {
                Debug.LogError("[CompositeGuideEffectConfig] Cannot apply config to null composite effect");
                return;
            }

            Debug.Log(
                $"[CompositeGuideEffectConfig] Applying config to composite with {m_EffectConfigPairs.Count} effects");

            // Clear existing child effects first
            parallelComposite.ClearEffects();

            // Apply each effect-config pair
            int appliedCount = 0;
            foreach (var pair in m_EffectConfigPairs)
            {
                if (pair.Effect == null)
                {
                    Debug.LogWarning("[CompositeGuideEffectConfig] Skipping null effect in pair");
                    continue;
                }

                try
                {
                    // Step 1: Apply configuration to the individual effect
                    if (pair.Config != null)
                    {
                        pair.Config.Apply(pair.Effect);
                        Debug.Log(
                            $"[CompositeGuideEffectConfig] Applied config to effect: {pair.Effect.GetType().Name}");
                    }
                    else
                    {
                        Debug.LogWarning(
                            $"[CompositeGuideEffectConfig] No config provided for effect: {pair.Effect.GetType().Name}");
                    }

                    // Step 2: Add the configured effect to the composite
                    parallelComposite.AddEffect(pair.Effect);
                    appliedCount++;
                }
                catch (Exception ex)
                {
                    Debug.LogError(
                        $"[CompositeGuideEffectConfig] Failed to apply config for effect {pair.Effect.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
                }
            }

            Debug.Log(
                $"[CompositeGuideEffectConfig] Successfully applied {appliedCount}/{m_EffectConfigPairs.Count} effects to composite");
        }

        #endregion

        #region Validation

        /// <summary>
        /// Validate the configuration
        /// </summary>
        public bool Validate(out string errorMessage)
        {
            if (m_EffectConfigPairs.Count == 0)
            {
                errorMessage = "No effect-config pairs added";
                return false;
            }

            for (int i = 0; i < m_EffectConfigPairs.Count; i++)
            {
                var pair = m_EffectConfigPairs[i];
                if (pair.Effect == null)
                {
                    errorMessage = $"Effect at index {i} is null";
                    return false;
                }
            }

            errorMessage = string.Empty;
            return true;
        }

        #endregion

        #region Debug

        public override string ToString()
        {
            return $"[CompositeGuideEffectConfig] {m_EffectConfigPairs.Count} effects (Parallel)";
        }

        #endregion
    }
}