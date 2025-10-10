using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UIExt.Effect
{
    /// <summary>
    /// drag effect - guides user to drag an element from start to end position
    /// </summary>
    public class DragEffect : EffectBase, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private enum EndpointType
        {
            None = 0,
            ScreenPoint,
            WorldPosition,
            StaticRect,
            FollowRect
        }

        private struct DragEndpoint
        {
            public EndpointType Type;
            public RectTransform Rect;
            public Vector2 ScreenPos;
            public Vector3 WorldPos;
            public Camera WorldCamera;

            public Vector2 GetScreenPosition(Canvas rootCanvas)
            {
                switch (Type)
                {
                    case EndpointType.ScreenPoint:
                        return ScreenPos;

                    case EndpointType.WorldPosition:
                    {
                        var cam = WorldCamera != null
                            ? WorldCamera
                            : (rootCanvas != null ? rootCanvas.worldCamera : Camera.main);
                        if (cam == null)
                            return ScreenPos; // fallback
                        var sp = cam.WorldToScreenPoint(WorldPos);
                        return new Vector2(sp.x, sp.y);
                    }

                    case EndpointType.StaticRect:
                    case EndpointType.FollowRect:
                    {
                        if (Rect == null)
                            return ScreenPos;
                        var cam = rootCanvas != null ? rootCanvas.worldCamera : Camera.main;
                        var world = Rect.position;
                        var sp = RectTransformUtility.WorldToScreenPoint(cam, world);
                        return new Vector2(sp.x, sp.y);
                    }

                    default:
                        return ScreenPos;
                }
            }
        }

        private class DragValidator
        {
            private readonly DragEffect m_Owner;

            // Settings
            private float m_Timeout;
            private Func<Vector2, bool> m_ValidationFunc;
            private Action m_OnTimeout;

            // State
            private bool m_IsWaiting;
            private Coroutine m_Coroutine;

            public bool IsWaiting => m_IsWaiting;

            public DragValidator(DragEffect owner)
            {
                m_Owner = owner;
            }

            public void SetTimeout(float seconds)
            {
                m_Timeout = Mathf.Max(0, seconds);
            }

            public void SetValidationFunc(Func<Vector2, bool> func)
            {
                m_ValidationFunc = func;
            }

            public void SetTimeoutCallback(Action callback)
            {
                m_OnTimeout = callback;
            }

            public bool HasValidation()
            {
                return m_ValidationFunc != null;
            }

            public void StartValidation(Vector2 pos)
            {
                m_IsWaiting = true;
            }

            public void SetCoroutine(Coroutine coroutine)
            {
                m_Coroutine = coroutine;
            }

            public System.Collections.IEnumerator ValidationCoroutine(Vector2 dragEndPos)
            {
                float timeoutTimer = 0f;

                // Wait one frame to handle execution order issues
                yield return null;
                timeoutTimer += Time.deltaTime;

                if (m_Timeout > 0 && timeoutTimer >= m_Timeout)
                {
                    HandleTimeout();
                    yield break;
                }

                // Call validation function
                bool isValid = false;
                try
                {
                    isValid = m_ValidationFunc.Invoke(dragEndPos);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[DragEffect] Validation function threw exception: {e.Message}");
                    isValid = false;
                }

                // Handle result
                if (isValid)
                {
                    ConfirmComplete();
                }
                else
                {
                    ConfirmFailed();
                }
            }

            public void ConfirmComplete()
            {
                if (!m_IsWaiting)
                    return;

                m_IsWaiting = false;

                if (m_Coroutine != null)
                {
                    m_Owner.StopCoroutine(m_Coroutine);
                    m_Coroutine = null;
                }

                m_Owner.InvokeComplete();
            }

            public void ConfirmFailed()
            {
                if (!m_IsWaiting)
                    return;

                m_IsWaiting = false;

                if (m_Coroutine != null)
                {
                    m_Owner.StopCoroutine(m_Coroutine);
                    m_Coroutine = null;
                }

                m_Owner.HandleDragFailed();
            }

            public void Cancel()
            {
                if (!m_IsWaiting)
                    return;

                m_IsWaiting = false;

                if (m_Coroutine != null)
                {
                    m_Owner.StopCoroutine(m_Coroutine);
                    m_Coroutine = null;
                }

                m_Owner.HandleDragFailed();
            }

            public void Cleanup()
            {
                if (m_Coroutine != null)
                {
                    m_Owner.StopCoroutine(m_Coroutine);
                    m_Coroutine = null;
                }

                m_IsWaiting = false;
            }

            private void HandleTimeout()
            {
                m_IsWaiting = false;
                m_Coroutine = null;

                m_OnTimeout?.Invoke();
                m_Owner.HandleDragFailed();
            }
        }

        private DragEndpoint m_StartEndpoint;
        private DragEndpoint m_EndEndpoint;

        [SerializeField]
        [Tooltip("Drag hint object (like a hand icon)")]
        private RectTransform m_DragHint;

        [SerializeField]
        [Tooltip("Static indicator at start position")]
        private RectTransform m_FromHint;

        [SerializeField]
        [Tooltip("Static indicator at end position")]
        private RectTransform m_ToHint;

        [SerializeField]
        [Tooltip("Auto play hint animation")]
        private bool m_AutoPlayHint = true;

        [SerializeField]
        [Tooltip("Hint animation duration")]
        private float m_HintDuration = 1.5f;

        [SerializeField]
        [Tooltip("Hint animation loop delay")]
        private float m_HintLoopDelay = 0.5f;

        [SerializeField]
        [Tooltip("Drag threshold to complete")]
        private float m_CompleteThreshold = 50f;

        private bool m_IsDragging;
        private float m_HintTimer;
        private bool m_DragHintPlaying;
        private Vector3 m_HintStartPos;
        private Vector3 m_HintEndPos;

        private Canvas m_Canvas;
        private Image m_DragImage;
        private Action<Vector2> m_OnDrag;
        private Action m_OnDragStart;
        private Action m_OnDragEnd;

        private DragValidator m_Validator;

        protected override bool NeedTarget => false;

        /// <summary>
        /// Is currently waiting for validation to complete
        /// </summary>
        public bool IsWaitingValidation => m_Validator?.IsWaiting ?? false;

        private DragValidator Validator
        {
            get
            {
                if (m_Validator == null)
                {
                    m_Validator = new DragValidator(this);
                }

                return m_Validator;
            }
        }

        private Canvas RootCanvas
        {
            get
            {
                if (m_Canvas == null)
                {
                    m_Canvas = GetComponentInParent<Canvas>();
                    if (m_Canvas != null)
                        m_Canvas = m_Canvas.rootCanvas;
                }

                return m_Canvas;
            }
        }

        /// <summary>
        /// Set from hint indicator
        /// </summary>
        public DragEffect SetFromHint(RectTransform hint)
        {
            m_FromHint = hint;
            return this;
        }

        /// <summary>
        /// Set to hint indicator
        /// </summary>
        public DragEffect SetToHint(RectTransform hint)
        {
            m_ToHint = hint;
            return this;
        }

        /// <summary>
        /// Set start and end positions (Auto Follow). Equivalent to SetPositionsFollow.
        /// </summary>
        public DragEffect SetPositions(RectTransform start, RectTransform end)
        {
            return SetPositionsFollow(start, end);
        }

        /// <summary>
        /// Set start and end screen positions in pixels.
        /// </summary>
        public DragEffect SetPositionsScreen(Vector2 startScreen, Vector2 endScreen)
        {
            m_StartEndpoint = new DragEndpoint { Type = EndpointType.ScreenPoint, ScreenPos = startScreen };
            m_EndEndpoint = new DragEndpoint { Type = EndpointType.ScreenPoint, ScreenPos = endScreen };
            return this;
        }

        /// <summary>
        /// Set start and end world positions, will be projected to screen using camera.
        /// </summary>
        public DragEffect SetPositionsWorld(Vector3 startWorld, Vector3 endWorld, Camera camera = null)
        {
            m_StartEndpoint = new DragEndpoint
                { Type = EndpointType.WorldPosition, WorldPos = startWorld, WorldCamera = camera };
            m_EndEndpoint = new DragEndpoint
                { Type = EndpointType.WorldPosition, WorldPos = endWorld, WorldCamera = camera };
            return this;
        }

        /// <summary>
        /// Set start and end using RectTransform but sampled once when play (Static sampling).
        /// </summary>
        public DragEffect SetPositionsStatic(RectTransform start, RectTransform end)
        {
            m_StartEndpoint = new DragEndpoint { Type = EndpointType.StaticRect, Rect = start };
            m_EndEndpoint = new DragEndpoint { Type = EndpointType.StaticRect, Rect = end };
            return this;
        }

        /// <summary>
        /// Set start and end using RectTransform and follow dynamically (Auto Follow).
        /// </summary>
        public DragEffect SetPositionsFollow(RectTransform start, RectTransform end)
        {
            m_StartEndpoint = new DragEndpoint { Type = EndpointType.FollowRect, Rect = start };
            m_EndEndpoint = new DragEndpoint { Type = EndpointType.FollowRect, Rect = end };
            return this;
        }

        /// <summary>
        /// Set drag hint
        /// </summary>
        public DragEffect SetDragHint(RectTransform hint)
        {
            m_DragHint = hint;
            return this;
        }

        /// <summary>
        /// Set hint animation settings
        /// </summary>
        public DragEffect SetHintAnimation(bool autoPlay, float duration = 1.5f, float loopDelay = 0.5f)
        {
            m_AutoPlayHint = autoPlay;
            m_HintDuration = duration;
            m_HintLoopDelay = loopDelay;
            return this;
        }

        /// <summary>
        /// Set complete threshold
        /// </summary>
        public DragEffect SetCompleteThreshold(float threshold)
        {
            m_CompleteThreshold = threshold;
            return this;
        }

        /// <summary>
        /// Set drag callbacks
        /// </summary>
        public DragEffect OnDragStart(Action onDragStart)
        {
            m_OnDragStart = onDragStart;
            return this;
        }

        public DragEffect OnDrag(Action<Vector2> onDrag)
        {
            m_OnDrag = onDrag;
            return this;
        }

        public DragEffect OnDragEnd(Action onDragEnd)
        {
            m_OnDragEnd = onDragEnd;
            return this;
        }

        /// <summary>
        /// Set validation timeout. If validation doesn't complete within this time, it will auto-fail.
        /// Set to 0 to disable timeout.
        /// </summary>
        public DragEffect SetValidationTimeout(float seconds)
        {
            Validator.SetTimeout(seconds);
            return this;
        }

        /// <summary>
        /// Set validation callback. The function will be called to validate the drag end position.
        /// Return true for success, false for failure.
        /// The validation will be executed with a one-frame delay by default to handle execution order issues.
        /// </summary>
        public DragEffect OnDragEndValidation(Func<Vector2, bool> validationFunc)
        {
            Validator.SetValidationFunc(validationFunc);
            return this;
        }

        /// <summary>
        /// Set callback for when validation times out
        /// </summary>
        public DragEffect OnValidationTimeout(Action onTimeout)
        {
            Validator.SetTimeoutCallback(onTimeout);
            return this;
        }

        protected override void OnPlay()
        {
            base.OnPlay();

            // Validate endpoints
            bool hasEndpoints = m_StartEndpoint.Type != EndpointType.None && m_EndEndpoint.Type != EndpointType.None;

            if (!hasEndpoints)
            {
                Debug.LogError("[GuideDragEffect] Start or End position is not configured");
                InvokeCancel();
                return;
            }

            gameObject.SetActive(true);

            // Setup drag target
            if (m_Target != null)
            {
                SetLocalPositionFromScreenPoint(m_Target, GetStartScreenPos());
            }

            // Ensure Image component for raycasting
            if (m_DragImage == null)
            {
                m_DragImage = GetComponent<Image>();
                if (m_DragImage == null)
                {
                    m_DragImage = gameObject.AddComponent<Image>();
                    m_DragImage.color = new Color(0, 0, 0, 0.01f); // Nearly transparent
                }

                SetLocalPositionFromScreenPoint(m_DragImage.rectTransform, GetStartScreenPos());
            }

            // Setup hint
            if (m_DragHint != null && m_AutoPlayHint)
            {
                // m_DragHint.gameObject.SetActive(true);
                ShowHint(m_DragHint);
                var startScreen = GetStartScreenPos();
                var endScreen = GetEndScreenPos();
                m_HintStartPos = startScreen;
                m_HintEndPos = endScreen;
                m_HintTimer = 0f;
                m_DragHintPlaying = true;
            }

            if (null != m_FromHint)
            {
                SetLocalPositionFromScreenPoint(m_FromHint, GetStartScreenPos());
                ShowHint(m_FromHint);
            }

            if (null != m_ToHint)
            {
                SetLocalPositionFromScreenPoint(m_ToHint, GetEndScreenPos());
                ShowHint(m_ToHint);
            }
        }

        protected override void OnStop()
        {
            base.OnStop();

            HideHint(m_DragHint);
            HideHint(m_FromHint);
            HideHint(m_ToHint);

            m_DragHintPlaying = false;
            m_IsDragging = false;

            // Clean up validation state
            Validator.Cleanup();

            gameObject.SetActive(false);
        }

        protected override void OnPause()
        {
            base.OnPause();
            m_DragHintPlaying = false;
        }

        protected override void OnResume()
        {
            base.OnResume();
            if (m_AutoPlayHint && m_DragHint != null)
            {
                m_DragHintPlaying = true;
            }
        }

        private void Update()
        {
            if (!m_IsPlaying || m_IsPaused)
                return;

            UpdateFromAndToHints();

            if (m_DragHintPlaying && !m_IsDragging)
            {
                // Refresh hint endpoints for follow/world modes
                if (m_StartEndpoint.Type == EndpointType.FollowRect ||
                    m_StartEndpoint.Type == EndpointType.WorldPosition)
                    m_HintStartPos = GetStartScreenPos();
                if (m_EndEndpoint.Type == EndpointType.FollowRect || m_EndEndpoint.Type == EndpointType.WorldPosition)
                    m_HintEndPos = GetEndScreenPos();

                UpdateDragHintAnimation();
            }
        }

        private void UpdateFromAndToHints()
        {
            if (m_FromHint != null &&
                (m_StartEndpoint.Type == EndpointType.FollowRect ||
                 m_StartEndpoint.Type == EndpointType.WorldPosition))
            {
                SetLocalPositionFromScreenPoint(m_FromHint, GetStartScreenPos());
            }

            if (m_ToHint != null &&
                (m_EndEndpoint.Type == EndpointType.FollowRect ||
                 m_EndEndpoint.Type == EndpointType.WorldPosition))
            {
                SetLocalPositionFromScreenPoint(m_ToHint, GetEndScreenPos());
            }
        }

        private void UpdateDragHintAnimation()
        {
            m_HintTimer += Time.deltaTime;

            float totalDuration = m_HintDuration + m_HintLoopDelay;
            float normalizedTime = (m_HintTimer % totalDuration) / totalDuration;

            Vector3 screenPos;
            if (normalizedTime <= m_HintDuration / totalDuration)
            {
                float t = (m_HintTimer % totalDuration) / m_HintDuration;
                t = Mathf.SmoothStep(0, 1, t);
                screenPos = Vector3.Lerp(m_HintStartPos, m_HintEndPos, t);
            }
            else
            {
                screenPos = m_HintStartPos;
            }

            // Convert screen position to local position relative to m_DragHint's parent
            SetLocalPositionFromScreenPoint(m_DragHint, screenPos);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!m_IsPlaying || m_IsPaused)
                return;

            m_IsDragging = true;

            HideHint(m_FromHint);
            HideHint(m_DragHint);
            m_DragHintPlaying = false;

            m_OnDragStart?.Invoke();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!m_IsPlaying || m_IsPaused || !m_IsDragging)
                return;

            if (m_Target != null)
            {
                SetLocalPositionFromScreenPoint(m_Target, eventData.position);
            }

            m_OnDrag?.Invoke(eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!m_IsPlaying || m_IsPaused || !m_IsDragging)
                return;

            m_IsDragging = false;

            // Check if drag reached the end position (threshold check)
            float distance = Vector2.Distance(eventData.position, GetEndScreenPos());

            if (distance > m_CompleteThreshold)
            {
                // Distance check failed, reset immediately
                HandleDragFailed();
                return;
            }

            // Move target to end position
            if (m_Target != null)
            {
                SetLocalPositionFromScreenPoint(m_Target, GetEndScreenPos());
            }

            // Trigger OnDragEnd callback
            m_OnDragEnd?.Invoke();

            // Start validation process
            if (Validator.HasValidation())
            {
                // Validation mode with delay
                Validator.StartValidation(eventData.position);
                var coroutine = StartCoroutine(Validator.ValidationCoroutine(eventData.position));
                Validator.SetCoroutine(coroutine);
            }
            else
            {
                // No validation, complete immediately
                InvokeComplete();
            }
        }

        private Vector2 GetStartScreenPos()
        {
            return m_StartEndpoint.GetScreenPosition(RootCanvas);
        }

        private Vector2 GetEndScreenPos()
        {
            return m_EndEndpoint.GetScreenPosition(RootCanvas);
        }

        /// <summary>
        /// Helper method to set RectTransform's local position from screen position.
        /// Automatically converts screen position to local position relative to the target's parent.
        /// </summary>
        private void SetLocalPositionFromScreenPoint(RectTransform target, Vector2 screenPos)
        {
            if (target == null)
                return;

            var parent = target.parent as RectTransform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parent,
                screenPos,
                RootCanvas.worldCamera,
                out var localPoint);
            target.localPosition = localPoint;
        }

        /// <summary>
        /// Confirm that the drag was successful. Call this from external validation logic.
        /// </summary>
        public void ConfirmComplete()
        {
            Validator.ConfirmComplete();
        }

        /// <summary>
        /// Confirm that the drag failed validation. Call this from external validation logic.
        /// </summary>
        public void ConfirmFailed()
        {
            Validator.ConfirmFailed();
        }

        /// <summary>
        /// Cancel ongoing validation
        /// </summary>
        public void CancelValidation()
        {
            Validator.Cancel();
        }

        /// <summary>
        /// Handle drag failed - reset to start position and restart hint
        /// </summary>
        internal void HandleDragFailed()
        {
            // Reset to start position
            if (m_Target != null)
            {
                SetLocalPositionFromScreenPoint(m_Target, GetStartScreenPos());
            }

            // Restart hint animation
            if (m_AutoPlayHint && m_DragHint != null)
            {
                ShowHint(m_DragHint);
                m_HintTimer = 0f;
                m_DragHintPlaying = true;
            }

            ShowHint(m_FromHint);
            ShowHint(m_ToHint);
        }

        private void ShowHint(RectTransform hint)
        {
            if (null != hint)
                hint.gameObject.SetActive(true);
        }

        private void HideHint(RectTransform hint)
        {
            if (null != hint)
                hint.gameObject.SetActive(false);
        }
    }
}