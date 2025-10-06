using System;
using GameGuide.Conditions;
using GameGuide.Conditions.UIConditions;
using UnityEngine;
using GameGuide.Core.Effect;
using Unity.VisualScripting;

namespace GameGuide.Core
{
    /// <summary>
    /// Guide system factory - provides convenient methods to create guide components
    /// </summary>
    public static class GuideFactory
    {
        #region Create Guide Item

        /// <summary>
        /// Create a guide item
        /// </summary>
        public static GuideItem CreateGuideItem(string itemId = null, string description = "")
        {
            return new GuideItem(itemId, description);
        }

        /// <summary>
        /// Create a guide item (with configuration)
        /// </summary>
        public static GuideItem CreateGuideItem(Action<GuideItem> configure)
        {
            var item = new GuideItem();
            configure?.Invoke(item);
            return item;
        }

        #endregion

        #region Create Guide Group

        /// <summary>
        /// Create a guide group
        /// </summary>
        public static GuideGroup CreateGuideGroup(string groupId = null, string groupName = "", string description = "")
        {
            return new GuideGroup(groupId, groupName, description);
        }

        /// <summary>
        /// Create a guide group (with configuration)
        /// </summary>
        public static GuideGroup CreateGuideGroup(Action<GuideGroup> configure)
        {
            var group = new GuideGroup();
            configure?.Invoke(group);
            return group;
        }

        #endregion

        #region Create Guide Effects

        /// <summary>
        /// Create a guide lock effect
        /// </summary>
        public static GuideLockEffect CreateGuideLockEffect(GameObject root, GameObject target,
            Action<GuideLockEffect> configure = null)
        {
            if (target == null)
            {
                Debug.LogError("[GuideFactory] Target is null for GuideLockEffect");
                return null;
            }

            var effect = root.GetOrAddComponent<GuideLockEffect>();

            effect.Target = target.GetComponent<RectTransform>();
            configure?.Invoke(effect);
            return effect;
        }

        /// <summary>
        /// Create a guide drag effect
        /// </summary>
        public static GuideDragEffect CreateGuideDragEffect(GameObject target, RectTransform startPos,
            RectTransform endPos, Action<GuideDragEffect> configure = null)
        {
            if (target == null)
            {
                Debug.LogError("[GuideFactory] Target is null for GuideDragEffect");
                return null;
            }

            var effect = target.GetOrAddComponent<GuideDragEffect>();
            effect.Target = target.GetComponent<RectTransform>();
            effect.SetPositions(startPos, endPos);
            configure?.Invoke(effect);
            return effect;
        }

        #endregion

        #region Create Guide Conditions

        /// <summary>
        /// Create an always-true condition
        /// </summary>
        public static AlwaysTrueCondition CreateAlwaysTrueCondition(string conditionId = null)
        {
            return new AlwaysTrueCondition(conditionId);
        }

        /// <summary>
        /// Create a UI state condition
        /// </summary>
        public static UIStateCondition CreateUIStateCondition(GameObject target, UIStateCondition.UIStateType stateType,
            string conditionId = null)
        {
            if (target == null)
            {
                Debug.LogError("[GuideFactory] Target is null for UIStateCondition");
                return null;
            }

            return new UIStateCondition(target, stateType);
        }

        /// <summary>
        /// Create a UI click condition
        /// </summary>
        public static UIClickCondition CreateUIClickCondition(GameObject target, int requiredClickCount = 1,
            string conditionId = null)
        {
            if (target == null)
            {
                Debug.LogError("[GuideFactory] Target is null for UIClickCondition");
                return null;
            }

            return new UIClickCondition(target, requiredClickCount);
        }

        /// <summary>
        /// Create a guide item completed condition
        /// </summary>
        public static GuideItemCompletedCondition CreateGuideItemCompletedCondition(string targetItemId,
            string conditionId = null)
        {
            return new GuideItemCompletedCondition(targetItemId);
        }

        #endregion

        #region Create Complete Guide Group (Convenience)

        // /// <summary>
        // /// Create a simple click guide group
        // /// </summary>
        // public static GuideGroup CreateClickGuideGroup(string groupId, GameObject target, string description = "")
        // {
        //     var group = CreateGuideGroup(groupId, $"Click Guide: {target.name}", description);
        //
        //     // Create UI click condition
        //     var clickCondition = CreateUIClickCondition(target, 1);
        //
        //     // Create lock effect
        //     var lockEffect = CreateGuideLockEffect(target, effect =>
        //     {
        //         effect.SetMaskType(UIEventMask.MaskType.Rect)
        //             .SetFocusFrame(target.GetComponent<RectTransform>())
        //             .SetAllowClick(true);
        //     });
        //
        //     // Create guide item
        //     var item = CreateGuideItem($"click_{target.name}", $"Click on {target.name}")
        //         .SetTriggerCondition(clickCondition)
        //         .SetGuideEffect(lockEffect)
        //         .SetAutoStart(true)
        //         .SetAutoComplete(true);
        //
        //     group.AddItem(item);
        //     return group;
        // }

        /// <summary>
        /// Create a drag guide group
        /// </summary>
        public static GuideGroup CreateDragGuideGroup(string groupId, GameObject target, RectTransform startPos,
            RectTransform endPos, string description = "")
        {
            var group = CreateGuideGroup(groupId, $"Drag Guide: {target.name}", description);

            // Create drag effect
            var dragEffect = CreateGuideDragEffect(target, startPos, endPos, effect => { });

            // Create guide item
            var item = CreateGuideItem($"drag_{target.name}", $"Drag from {startPos.name} to {endPos.name}")
                .SetGuideEffect(dragEffect)
                .SetAutoStart(true)
                .SetAutoComplete(true);

            group.AddItem(item);
            return group;
        }

        /// <summary>
        /// Create a multi-step guide group
        /// </summary>
        public static GuideGroup CreateMultiStepGuideGroup(string groupId, GuideStep[] steps, string description = "")
        {
            var group = CreateGuideGroup(groupId, $"Multi-step Guide", description);

            foreach (var step in steps)
            {
                var item = CreateGuideItem(step.ItemId, step.Description);

                // Set trigger condition
                if (step.TriggerCondition != null)
                {
                    item.SetTriggerCondition(step.TriggerCondition);
                }

                // Set completion condition
                if (step.CompletionCondition != null)
                {
                    item.SetCompletionCondition(step.CompletionCondition);
                }

                // Set guide effect
                if (step.GuideEffect != null)
                {
                    item.SetGuideEffect(step.GuideEffect);
                }

                // Set other properties
                item.SetPriority(step.Priority)
                    .SetTimeout(step.TimeoutSeconds)
                    .SetAutoStart(step.AutoStart)
                    .SetAutoComplete(step.AutoComplete);

                group.AddItem(item);
            }

            return group;
        }

        #endregion

        #region Helper Data Structures

        /// <summary>
        /// Guide step data
        /// </summary>
        [Serializable]
        public struct GuideStep
        {
            public string ItemId;
            public string Description;
            public GuideItemPriority Priority;
            public float TimeoutSeconds;
            public bool AutoStart;
            public bool AutoComplete;
            public IGuideCondition TriggerCondition;
            public IGuideCondition CompletionCondition;
            public IGuideEffect GuideEffect;
        }

        #endregion
    }
}