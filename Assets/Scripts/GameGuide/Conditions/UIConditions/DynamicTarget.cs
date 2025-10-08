using System;
using UnityEngine;

namespace GameGuide.Conditions.UIConditions
{
    public interface IDynamicTarget
    {
        /// <summary>
        /// Target GameObject Path relative to Root Transform
        /// </summary>
        string TargetRelPath { get; set; }

        /// <summary>
        /// Root Transform Getter
        /// </summary>
        Func<RectTransform> RootGetter { get; set; }

        /// <summary>
        /// Get Target Object, return null when either Root or Target is not prepared
        /// </summary>
        /// <returns></returns>
        public GameObject GetTargetGameObject();

        /// <summary>
        /// Invalidate Cache, used when dynamic change UI name
        /// </summary>
        public void InvalidateCache();
    }

    public class DynamicTarget : IDynamicTarget
    {
        private RectTransform m_Root;
        private GameObject m_TargetGo;
        private string m_TargetRelPath;

        public string TargetRelPath
        {
            get => m_TargetRelPath;
            set => m_TargetRelPath = value;
        }

        public Func<RectTransform> RootGetter { get; set; }

        public GameObject GetTargetGameObject()
        {
            if (null == m_TargetGo)
            {
                if (null == m_Root)
                {
                    if (null != RootGetter)
                        m_Root = RootGetter();
                }

                if (null != m_Root)
                {
                    var targetTransform = m_Root.Find(m_TargetRelPath);

                    if (null != targetTransform)
                        m_TargetGo = targetTransform.gameObject;
                }
            }

            return m_TargetGo;
        }

        public void InvalidateCache()
        {
            m_Root = null;
            m_TargetGo = null;
        }
    }
}