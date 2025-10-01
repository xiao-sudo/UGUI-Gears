using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UIExt.Effect
{
    /// <summary>
    /// Guide drag effect - guides user to drag an element from start to end position
    /// </summary>
    public class GuideDragEffect : GuideEffectBase, IBeginDragHandler, IDragHandler, IEndDragHandler
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

        private DragEndpoint m_StartEndpoint;
        private DragEndpoint m_EndEndpoint;

        // [Header("Drag Settings")]
        // [SerializeField]
        // [Tooltip("Start position for the drag (Follow when using RectTransform API)")]
        // private RectTransform m_StartPosition;
        //
        // [SerializeField]
        // [Tooltip("End position for the drag (Follow when using RectTransform API)")]
        // private RectTransform m_EndPosition;

        [SerializeField]
        [Tooltip("Drag hint object (like a hand icon)")]
        private RectTransform m_DragHint;

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
        private bool m_HintPlaying;
        private Vector3 m_HintStartPos;
        private Vector3 m_HintEndPos;

        private Canvas m_Canvas;
        private Image m_DragImage;
        private Action<Vector2> m_OnDrag;
        private Action m_OnDragStart;
        private Action m_OnDragEnd;

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
        /// Set start and end positions (Auto Follow). Equivalent to SetPositionsFollow.
        /// </summary>
        public GuideDragEffect SetPositions(RectTransform start, RectTransform end)
        {
            return SetPositionsFollow(start, end);
        }

        /// <summary>
        /// Set start and end screen positions in pixels.
        /// </summary>
        public GuideDragEffect SetPositionsScreen(Vector2 startScreen, Vector2 endScreen)
        {
            m_StartEndpoint = new DragEndpoint { Type = EndpointType.ScreenPoint, ScreenPos = startScreen };
            m_EndEndpoint = new DragEndpoint { Type = EndpointType.ScreenPoint, ScreenPos = endScreen };
            return this;
        }

        /// <summary>
        /// Set start and end world positions, will be projected to screen using camera.
        /// </summary>
        public GuideDragEffect SetPositionsWorld(Vector3 startWorld, Vector3 endWorld, Camera camera = null)
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
        public GuideDragEffect SetPositionsStatic(RectTransform start, RectTransform end)
        {
            m_StartEndpoint = new DragEndpoint { Type = EndpointType.StaticRect, Rect = start };
            m_EndEndpoint = new DragEndpoint { Type = EndpointType.StaticRect, Rect = end };
            return this;
        }

        /// <summary>
        /// Set start and end using RectTransform and follow dynamically (Auto Follow).
        /// </summary>
        public GuideDragEffect SetPositionsFollow(RectTransform start, RectTransform end)
        {
            m_StartEndpoint = new DragEndpoint { Type = EndpointType.FollowRect, Rect = start };
            m_EndEndpoint = new DragEndpoint { Type = EndpointType.FollowRect, Rect = end };
            return this;
        }

        /// <summary>
        /// Set drag hint
        /// </summary>
        public GuideDragEffect SetDragHint(RectTransform hint)
        {
            m_DragHint = hint;
            return this;
        }

        /// <summary>
        /// Set hint animation settings
        /// </summary>
        public GuideDragEffect SetHintAnimation(bool autoPlay, float duration = 1.5f, float loopDelay = 0.5f)
        {
            m_AutoPlayHint = autoPlay;
            m_HintDuration = duration;
            m_HintLoopDelay = loopDelay;
            return this;
        }

        /// <summary>
        /// Set complete threshold
        /// </summary>
        public GuideDragEffect SetCompleteThreshold(float threshold)
        {
            m_CompleteThreshold = threshold;
            return this;
        }

        /// <summary>
        /// Set drag callbacks
        /// </summary>
        public GuideDragEffect OnDragStart(Action onDragStart)
        {
            m_OnDragStart = onDragStart;
            return this;
        }

        public GuideDragEffect OnDrag(Action<Vector2> onDrag)
        {
            m_OnDrag = onDrag;
            return this;
        }

        public GuideDragEffect OnDragEnd(Action onDragEnd)
        {
            m_OnDragEnd = onDragEnd;
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
                var startScreen = GetStartScreenPos();
                Vector2 localPoint;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    RootCanvas.transform as RectTransform,
                    startScreen,
                    RootCanvas.worldCamera,
                    out localPoint);
                m_Target.localPosition = localPoint;
            }

            // Setup hint
            if (m_DragHint != null && m_AutoPlayHint)
            {
                m_DragHint.gameObject.SetActive(true);
                var startScreen = GetStartScreenPos();
                var endScreen = GetEndScreenPos();
                m_HintStartPos = startScreen;
                m_HintEndPos = endScreen;
                m_HintTimer = 0f;
                m_HintPlaying = true;
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
            }
        }

        protected override void OnStop()
        {
            base.OnStop();

            if (m_DragHint != null)
            {
                m_DragHint.gameObject.SetActive(false);
            }

            m_HintPlaying = false;
            m_IsDragging = false;

            gameObject.SetActive(false);
        }

        protected override void OnPause()
        {
            base.OnPause();
            m_HintPlaying = false;
        }

        protected override void OnResume()
        {
            base.OnResume();
            if (m_AutoPlayHint && m_DragHint != null)
            {
                m_HintPlaying = true;
            }
        }

        private void Update()
        {
            if (!m_IsPlaying || m_IsPaused)
                return;

            if (m_HintPlaying && !m_IsDragging)
            {
                // Refresh hint endpoints for follow/world modes
                if (m_StartEndpoint.Type == EndpointType.FollowRect ||
                    m_StartEndpoint.Type == EndpointType.WorldPosition)
                    m_HintStartPos = GetStartScreenPos();
                if (m_EndEndpoint.Type == EndpointType.FollowRect || m_EndEndpoint.Type == EndpointType.WorldPosition)
                    m_HintEndPos = GetEndScreenPos();

                UpdateHintAnimation();
            }
        }

        private void UpdateHintAnimation()
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

            RectTransformUtility.ScreenPointToLocalPointInRectangle(RootCanvas.transform as RectTransform, screenPos,
                RootCanvas.worldCamera, out var localPoint);

            m_DragHint.localPosition = localPoint;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!m_IsPlaying || m_IsPaused)
                return;

            m_IsDragging = true;

            if (m_DragHint != null)
            {
                m_DragHint.gameObject.SetActive(false);
                m_HintPlaying = false;
            }

            m_OnDragStart?.Invoke();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!m_IsPlaying || m_IsPaused || !m_IsDragging)
                return;

            if (m_Target != null)
            {
                Vector2 localPoint;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    RootCanvas.transform as RectTransform,
                    eventData.position,
                    RootCanvas.worldCamera,
                    out localPoint);

                m_Target.localPosition = localPoint;
            }

            m_OnDrag?.Invoke(eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!m_IsPlaying || m_IsPaused || !m_IsDragging)
                return;

            m_IsDragging = false;

            // Check if drag reached the end position
            float distance = Vector2.Distance(eventData.position, GetEndScreenPos());

            if (distance <= m_CompleteThreshold)
            {
                // Drag completed successfully
                if (m_Target != null)
                {
                    var endScreen = GetEndScreenPos();
                    Vector2 localPoint;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        RootCanvas.transform as RectTransform,
                        endScreen,
                        RootCanvas.worldCamera,
                        out localPoint);
                    m_Target.localPosition = localPoint;
                }

                m_OnDragEnd?.Invoke();
                InvokeComplete();
            }
            else
            {
                // Drag failed, reset to current start screen position
                if (m_Target != null)
                {
                    var startScreen = GetStartScreenPos();
                    Vector2 localPoint;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        RootCanvas.transform as RectTransform,
                        startScreen,
                        RootCanvas.worldCamera,
                        out localPoint);
                    m_Target.localPosition = localPoint;
                }

                // Restart hint animation
                if (m_AutoPlayHint && m_DragHint != null)
                {
                    m_DragHint.gameObject.SetActive(true);
                    m_HintTimer = 0f;
                    m_HintPlaying = true;
                }
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
    }
}