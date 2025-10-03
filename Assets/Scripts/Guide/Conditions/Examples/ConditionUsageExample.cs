using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UIExt.Guide.Conditions.UIConditions;
using UIExt.Guide.Conditions.EventConditions;
using UIExt.Guide.Conditions.CompositeConditions;
using UnityEngine.Serialization;

namespace UIExt.Guide.Conditions.Examples
{
    /// <summary>
    /// Condition usage example
    /// </summary>
    public class ConditionUsageExample : MonoBehaviour
    {
        [FormerlySerializedAs("testButton")]
        [Header("Test UI Objects")]
        [SerializeField]
        private GameObject testClick;

        [SerializeField]
        private GameObject testPanel;

        [SerializeField]
        private Animator testAnimator;

        [Header("Condition Testing")]
        [SerializeField]
        private bool runExample = false;

        private void Start()
        {
            if (runExample)
            {
                StartCoroutine(RunConditionExamples());
            }
        }

        private IEnumerator RunConditionExamples()
        {
            Debug.Log("=== 开始条件系统示例 ===");

            // 示例1：UI状态条件
            yield return StartCoroutine(ExampleUIStateCondition());

            // 示例2：UI点击条件
            yield return StartCoroutine(ExampleUIClickCondition());

            yield return StartCoroutine(ExampleUIDoubleClickCondition());

            // 示例4：复合条件
            yield return StartCoroutine(ExampleCompositeCondition());

            // 示例5：事件条件
            yield return StartCoroutine(ExampleEventCondition());

            Debug.Log("=== 条件系统示例完成 ===");
        }

        private IEnumerator ExampleUIStateCondition()
        {
            Debug.Log("--- 示例1：UI状态条件 ---");

            if (testPanel == null)
            {
                Debug.LogWarning("testPanel 未设置，跳过UI状态条件示例");
                yield break;
            }

            // 创建UI状态条件：面板必须激活
            var uiStateCondition =
                new UIStateCondition(testPanel, UIStateCondition.UIStateType.ActiveInHierarchy, true);

            // 注册到管理器
            GuideConditionManager.Instance.RegisterCondition(uiStateCondition);

            Debug.Log($"条件描述: {uiStateCondition.GetDescription()}");
            Debug.Log($"当前是否满足: {uiStateCondition.IsSatisfied()}");

            // 等待条件变化
            yield return new WaitForSeconds(1f);

            // 注销条件
            GuideConditionManager.Instance.UnregisterCondition(uiStateCondition);
        }

        private IEnumerator ExampleUIClickCondition()
        {
            Debug.Log("--- 示例2：UI点击条件 ---");

            if (testClick == null)
            {
                Debug.LogWarning("testButton 未设置，跳过UI点击条件示例");
                yield break;
            }

            // 创建UI点击条件：需要点击按钮1次
            var clickCondition = new UIClickCondition(testClick.gameObject, 1, true);

            // 注册到管理器
            GuideConditionManager.Instance.RegisterCondition(clickCondition);

            Debug.Log($"条件描述: {clickCondition.GetDescription()}");
            Debug.Log("请点击测试按钮来完成条件...");

            // 监听条件变化
            bool conditionMet = false;
            clickCondition.OnConditionChanged += (condition) =>
            {
                if (condition.IsSatisfied())
                {
                    Debug.Log("按钮点击条件已满足！");
                    conditionMet = true;
                }
            };

            clickCondition.StartListening();

            // 等待条件满足或超时
            float timeout = 10f;
            float elapsed = 0f;
            while (!conditionMet && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (!conditionMet)
            {
                Debug.Log("点击条件超时");
            }

            // 注销条件
            GuideConditionManager.Instance.UnregisterCondition(clickCondition);
        }

        private IEnumerator ExampleUIDoubleClickCondition()
        {
            Debug.Log("--- 示例3: UI双击条件 ---");

            if (testClick == null)
            {
                Debug.LogWarning("testButton 未设置，跳过UI点击条件示例");
                yield break;
            }

            // 创建UI点击条件：需要点击按钮1次
            var doubleClickCondition = new UIClickCondition(testClick.gameObject, 2, true)
            {
                ClickTimeWindow = 0.2f
            };

            // 注册到管理器
            GuideConditionManager.Instance.RegisterCondition(doubleClickCondition);

            Debug.Log($"条件描述: {doubleClickCondition.GetDescription()}");
            Debug.Log("请双击测试按钮来完成条件...");

            // 监听条件变化
            bool conditionMet = false;
            doubleClickCondition.OnConditionChanged += (condition) =>
            {
                if (condition.IsSatisfied())
                {
                    Debug.Log("按钮点击条件已满足！");
                    conditionMet = true;
                }
            };

            doubleClickCondition.StartListening();

            // 等待条件满足或超时
            float timeout = 10f;
            float elapsed = 0f;
            while (!conditionMet && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (!conditionMet)
            {
                Debug.Log("点击条件超时");
            }

            // 注销条件
            GuideConditionManager.Instance.UnregisterCondition(doubleClickCondition);
        }

        private IEnumerator ExampleCompositeCondition()
        {
            Debug.Log("--- 示例4：复合条件 ---");

            if (testPanel == null || testClick == null)
            {
                Debug.LogWarning("测试对象未设置，跳过复合条件示例");
                yield break;
            }

            // 创建子条件
            var panelActiveCondition =
                new UIStateCondition(testPanel, UIStateCondition.UIStateType.ActiveInHierarchy, true);
            var buttonInteractableCondition =
                new UIStateCondition(testClick.gameObject, UIStateCondition.UIStateType.Interactable, true);

            // 创建AND复合条件
            var compositeCondition = new CompositeCondition(CompositeLogicType.AND, panelActiveCondition,
                buttonInteractableCondition);

            // 注册到管理器
            GuideConditionManager.Instance.RegisterCondition(compositeCondition);

            Debug.Log($"复合条件描述: {compositeCondition.GetDescription()}");
            Debug.Log($"当前是否满足: {compositeCondition.IsSatisfied()}");

            // 监听条件变化
            compositeCondition.OnConditionChanged += (condition) =>
            {
                Debug.Log($"复合条件状态变化: {condition.IsSatisfied()}");
                Debug.Log(
                    $"满足的子条件数量: {((CompositeCondition)condition).GetSatisfiedConditionCount()}/{((CompositeCondition)condition).GetTotalConditionCount()}");
            };

            compositeCondition.StartListening();

            // 等待一段时间观察变化
            yield return new WaitForSeconds(2f);

            // 注销条件
            GuideConditionManager.Instance.UnregisterCondition(compositeCondition);
        }

        private IEnumerator ExampleEventCondition()
        {
            Debug.Log("--- 示例5：事件条件 ---");

            // 创建游戏事件条件
            var eventCondition = new GameEventCondition("TestEvent", "TestData", true);

            // 注册到管理器
            GuideConditionManager.Instance.RegisterCondition(eventCondition);

            Debug.Log($"条件描述: {eventCondition.GetDescription()}");

            // 监听条件变化
            bool conditionMet = false;
            eventCondition.OnConditionChanged += (condition) =>
            {
                if (condition.IsSatisfied())
                {
                    Debug.Log("事件条件已满足！");
                    Debug.Log($"事件数据: {eventCondition.GetLastEventData()}");
                    conditionMet = true;
                }
            };

            eventCondition.StartListening();

            // 等待1秒后触发事件
            yield return new WaitForSeconds(1f);

            Debug.Log("触发测试事件...");
            EventManager.Instance?.TriggerEvent("TestEvent", "TestData");

            // 等待条件满足
            float timeout = 5f;
            float elapsed = 0f;
            while (!conditionMet && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (!conditionMet)
            {
                Debug.Log("事件条件超时");
            }

            // 注销条件
            GuideConditionManager.Instance.UnregisterCondition(eventCondition);
        }

        [ContextMenu("运行所有示例")]
        private void RunAllExamples()
        {
            StartCoroutine(RunConditionExamples());
        }

        [ContextMenu("测试条件管理器状态")]
        private void TestConditionManagerStatus()
        {
            var manager = GuideConditionManager.Instance;
            if (manager == null)
            {
                Debug.Log("条件管理器未初始化");
                return;
            }

            Debug.Log($"活跃条件数量: {manager.ActiveConditionCount}");
            Debug.Log($"满足条件的数量: {manager.GetSatisfiedConditions().Count}");
            Debug.Log($"未满足条件的数量: {manager.GetUnsatisfiedConditions().Count}");

            var descriptions = manager.GetAllConditionDescriptions();
            Debug.Log("所有条件描述:");
            foreach (var desc in descriptions)
            {
                Debug.Log($"  - {desc}");
            }
        }
    }
}