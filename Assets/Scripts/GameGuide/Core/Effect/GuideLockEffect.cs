using System;
using UIExt.Base;
using UIExt.Effect;
using UnityEngine;

namespace GameGuide.Core.Effect
{
    /// <summary>
    /// Guide lock effect - highlights and locks a UI element with guide-specific events
    /// Supports dynamic target finding by path
    /// </summary>
    [RequireComponent(typeof(UIEventMask))]
    public class GuideLockEffect : LockEffect, IGuideEffect, IResettableEffect
    {
        #region Dynamic Target Finding

        [Header("Dynamic Target Finding")]
        [SerializeField]
        [Tooltip("Target path relative to SearchRoot (e.g. 'Panel/Buttons/ConfirmButton')")]
        private string m_TargetPath;

        [SerializeField]
        [Tooltip("Search root for path-based finding (optional, uses Canvas if null)")]
        private Transform m_SearchRoot;

        /// <summary>
        /// Optional search root finder function
        /// </summary>
        private System.Func<Transform> m_SearchRootFinder;

        /// <summary>
        /// Set target path for dynamic finding
        /// </summary>
        public GuideLockEffect SetTargetPath(string targetPath)
        {
            m_TargetPath = targetPath;
            return this;
        }

        /// <summary>
        /// Set search root directly
        /// </summary>
        public GuideLockEffect SetSearchRoot(Transform searchRoot)
        {
            m_SearchRoot = searchRoot;
            return this;
        }

        /// <summary>
        /// Set search root finder function
        /// </summary>
        public GuideLockEffect SetSearchRootFinder(System.Func<Transform> searchRootFinder)
        {
            m_SearchRootFinder = searchRootFinder;
            return this;
        }

        /// <summary>
        /// Get search root, resolving dynamically if needed
        /// </summary>
        protected Transform GetSearchRoot()
        {
            // If already set, return it
            if (m_SearchRoot != null)
            {
                return m_SearchRoot;
            }

            // Try the search root finder delegate
            if (m_SearchRootFinder != null)
            {
                m_SearchRoot = m_SearchRootFinder();
                if (m_SearchRoot != null)
                {
                    Debug.Log($"[{GetType().Name}] Search root found via finder: {m_SearchRoot.name}");
                }
            }

            // Try the virtual method
            if (m_SearchRoot == null)
            {
                m_SearchRoot = FindSearchRoot();
                if (m_SearchRoot != null)
                {
                    Debug.Log($"[{GetType().Name}] Search root found via FindSearchRoot: {m_SearchRoot.name}");
                }
            }

            return m_SearchRoot;
        }

        /// <summary>
        /// Override this to provide custom search root finding logic
        /// </summary>
        protected virtual Transform FindSearchRoot()
        {
            return null;
        }

        /// <summary>
        /// Find target by path relative to search root
        /// </summary>
        protected override RectTransform FindTarget()
        {
            // If no path specified, let base class handle it
            if (string.IsNullOrEmpty(m_TargetPath))
            {
                return base.FindTarget();
            }

            Transform root = GetSearchRoot();

            if (root == null)
            {
                // Fallback to Canvas
                var canvas = GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    root = canvas.transform;
                }
                else
                {
                    Debug.LogWarning($"[{GetType().Name}] No Canvas found and no search root specified!");
                    return null;
                }
            }

            var target = root.Find(m_TargetPath);
            if (target != null)
            {
                Debug.Log($"[{GetType().Name}] Found target by path: {m_TargetPath}");
                return target.GetComponent<RectTransform>();
            }

            Debug.LogWarning($"[{GetType().Name}] Could not find target at path: {m_TargetPath}");
            return null;
        }

        #endregion

        #region IGuideEffect Implementation

        public event Action<IGuideEffect> OnGuideEffectCompleted;
        public event Action<IGuideEffect> OnGuideEffectStarted;
        public event Action<IGuideEffect> OnGuideEffectStopped;

        #endregion

        #region Overrides

        public override void Play()
        {
            base.Play();

            if (m_IsPlaying)
            {
                OnGuideEffectStarted?.Invoke(this);
            }
        }

        public override void Stop()
        {
            base.Stop();

            if (!m_IsPlaying)
            {
                OnGuideEffectStopped?.Invoke(this);
            }
        }

        protected override void InvokeComplete()
        {
            base.InvokeComplete();
            OnGuideEffectCompleted?.Invoke(this);
        }

        protected override void InvokeCancel()
        {
            base.InvokeCancel();
            OnGuideEffectStopped?.Invoke(this);
        }

        #endregion

        #region IResettableEffect Implementation

        public virtual void Reset()
        {
            Stop();
        }

        #endregion
    }
}