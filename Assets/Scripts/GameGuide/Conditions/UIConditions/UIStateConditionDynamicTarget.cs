using System;
using UnityEngine;

namespace GameGuide.Conditions.UIConditions
{
    [Serializable]
    public class UIStateConditionDynamicTarget : UIStateCondition, IDynamicTarget
    {
        private DynamicTarget m_DynamicTarget;

        private UIStateConditionDynamicTarget()
        {
        }

        public UIStateConditionDynamicTarget(string id, UIStateType stateType, bool expectValue = true)
            : base(id, null, stateType, expectValue)
        {
            m_DynamicTarget = new DynamicTarget();
        }

        public override bool NeedsStateChecking => true;

        public string TargetRelPath
        {
            get => m_DynamicTarget.TargetRelPath;
            set => m_DynamicTarget.TargetRelPath = value;
        }

        public Func<RectTransform> RootGetter
        {
            get => m_DynamicTarget.RootGetter;
            set => m_DynamicTarget.RootGetter = value;
        }

        public GameObject GetTargetGameObject()
        {
            return GetTargetObject();
        }

        public void InvalidateCache()
        {
            m_DynamicTarget.InvalidateCache();
        }

        protected override GameObject GetTargetObject()
        {
            return m_DynamicTarget.GetTargetGameObject();
        }
    }
}