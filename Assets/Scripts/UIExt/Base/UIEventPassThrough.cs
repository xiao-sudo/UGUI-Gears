using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Pool;

namespace UIExt.Base
{
    public class UIEventPassThrough : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
    {
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
        private GameObject m_PassThroughTarget;

        private Action<GameObject> m_PassThroughClickCallback;

        private bool IsSpecifiedTarget => null != m_PassThroughTarget;

        public GameObject PassThroughTarget
        {
            get => m_PassThroughTarget;
            set => m_PassThroughTarget = value;
        }

        public Action<GameObject> PassThroughClickCallback
        {
            get => m_PassThroughClickCallback;
            set => m_PassThroughClickCallback = value;
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

            var results = ListPool<RaycastResult>.Get();

            EventSystem.current.RaycastAll(eventData, results);

            bool executed = false;
            GameObject executeGo = null;

            foreach (var result in results)
            {
                var go = result.gameObject;

                executed = ExecuteOnlyOnSpecifiedTarget(go, eventData, handler);

                if (executed)
                {
                    executeGo = go;
                    break;
                }
            }

            ListPool<RaycastResult>.Release(results);

            return new EventExecuteTarget(executed, executeGo);
        }

        private bool ExecuteOnlyOnSpecifiedTarget<T>(GameObject go, PointerEventData eventData,
            ExecuteEvents.EventFunction<T> handler) where T : IEventSystemHandler
        {
            if (go == m_PassThroughTarget)
                return ExecuteEvents.Execute(go, eventData, handler);

            return false;
        }
    }
}