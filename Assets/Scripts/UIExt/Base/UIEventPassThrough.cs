using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace UIExt.Base
{
    [RequireComponent(typeof(Image))]
    public class UIEventPassThrough : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
    {
        public enum PassThroughType
        {
            /// <summary>
            /// Pass in target's rect
            /// </summary>
            PassAtTargetRect,

            /// <summary>
            /// Pass outside target's rect
            /// </summary>
            PassExcludeTargetRect,

            /// <summary>
            /// Pass always
            /// </summary>
            PassAlways,

            /// <summary>
            /// Pass Never
            /// </summary>
            PassNever
        }

        private readonly struct EventExecuteTarget
        {
            private readonly bool m_Executed;
            private readonly GameObject m_Target;

            public EventExecuteTarget(bool executed, GameObject target)
            {
                m_Executed = executed;
                m_Target = target;
            }

            public bool Executed => m_Executed;
            public GameObject Target => m_Target;
        }

        [SerializeField]
        private RectTransform m_PassThroughTarget;

        [SerializeField]
        private PassThroughType m_PassThroughType = PassThroughType.PassAtTargetRect;

        [SerializeField]
        private Color m_MaskImageColor = new(0, 0, 0, 0.66f);

        private GameObject m_CachedGo;
        private Image m_MaskImage;
        private Action<GameObject> m_PassThroughClickCallback;

        private bool IsSpecifiedTarget => null != m_PassThroughTarget;

        public RectTransform PassThroughTarget
        {
            get => m_PassThroughTarget;
            set => m_PassThroughTarget = value;
        }

        public PassThroughType PassThroughStyle
        {
            get => m_PassThroughType;
            set => m_PassThroughType = value;
        }

        public Action<GameObject> PassThroughClickCallback
        {
            get => m_PassThroughClickCallback;
            set => m_PassThroughClickCallback = value;
        }

        private void Awake()
        {
            m_CachedGo = gameObject;

            m_MaskImage = GetComponent<Image>();
            m_MaskImage.color = m_MaskImageColor;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            var executeTarget = PassEvent(eventData, ExecuteEvents.pointerClickHandler);

            if (executeTarget.Executed)
                m_PassThroughClickCallback.Invoke(executeTarget.Target);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            PassEvent(eventData, ExecuteEvents.pointerDownHandler);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            PassEvent(eventData, ExecuteEvents.pointerUpHandler);
        }

        private EventExecuteTarget PassEvent<T>(PointerEventData eventData, ExecuteEvents.EventFunction<T> handler)
            where T : IEventSystemHandler
        {
            if (!IsSpecifiedTarget)
                return new EventExecuteTarget(false, null);

            if (!CanEventPassThrough(eventData))
                return new EventExecuteTarget(false, null);

            var results = ListPool<RaycastResult>.Get();

            EventSystem.current.RaycastAll(eventData, results);

            bool executed = false;
            GameObject executeGo = null;

            foreach (var result in results)
            {
                var go = result.gameObject;

                executed = ExecuteExcludeSelf(go, eventData, handler);

                if (executed)
                {
                    executeGo = go;
                    break;
                }
            }

            ListPool<RaycastResult>.Release(results);

            return new EventExecuteTarget(executed, executeGo);
        }

        private bool CanEventPassThrough(PointerEventData eventData)
        {
            switch (m_PassThroughType)
            {
                case PassThroughType.PassAtTargetRect:
                    return RectTransformUtility.RectangleContainsScreenPoint(m_PassThroughTarget, eventData.position,
                        eventData.pressEventCamera);

                case PassThroughType.PassExcludeTargetRect:
                    return !RectTransformUtility.RectangleContainsScreenPoint(m_PassThroughTarget, eventData.position,
                        eventData.pressEventCamera);

                case PassThroughType.PassAlways:
                    return true;

                case PassThroughType.PassNever:
                    return false;
            }

            return false;
        }

        private bool ExecuteExcludeSelf<T>(GameObject go, PointerEventData eventData,
            ExecuteEvents.EventFunction<T> handler) where T : IEventSystemHandler
        {
            if (go != m_CachedGo)
                return ExecuteEvents.Execute(go, eventData, handler);

            return false;
        }
    }
}