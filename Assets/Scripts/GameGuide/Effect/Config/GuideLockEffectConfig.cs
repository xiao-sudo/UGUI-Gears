using System;
using GameGuide.Core;
using UIExt.Base;
using UnityEngine;

namespace GameGuide.Effect.Config
{
    public class GuideLockEffectConfig : IGuideEffectConfig
    {
        public UIEventMask.MaskType MaskType { get; set; }
        public RectTransform Target { get; set; }
        public RectTransform FocusFrame { get; set; }
        
        public string TargetPath { get; set; }
        
        public Func<RectTransform> RootFinder { get; set; }

        public void Apply(IGuideEffect guideEffect)
        {
            if (guideEffect is GuideLockEffect guideLockEffect)
                Apply(guideLockEffect);
        }

        public void Apply(GuideLockEffect guideLockEffect)
        {
            guideLockEffect.Target = Target;
            guideLockEffect.SetMaskType(MaskType);
            guideLockEffect.SetFocusFrame(FocusFrame);
            guideLockEffect.SetTargetPath(TargetPath);
            guideLockEffect.SetSearchRootFinder(RootFinder);
        }
    }
}