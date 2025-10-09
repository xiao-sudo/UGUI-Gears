using System;
using GameGuide.Core;
using UnityEngine;

namespace GameGuide.Effect.Config
{
    public class GuideDragEffectConfig : IGuideEffectConfig
    {
        public Action OnDragComplete { get; set; }

        public float CompleteThreshold { get; set; }

        public float HintDuration { get; set; }

        public float HintLoopDelay { get; set; }

        public RectTransform Start { get; set; }

        public RectTransform End { get; set; }


        public void Apply(GuideDragEffect dragEffect)
        {
            dragEffect.OnComplete(OnDragComplete);
            dragEffect.SetHintAnimation(true, HintDuration, HintLoopDelay);
            dragEffect.SetPositions(Start, End);
            dragEffect.SetCompleteThreshold(CompleteThreshold);
        }

        public void Apply(IGuideEffect guideEffect)
        {
            if (guideEffect is GuideDragEffect dragEffect)
                Apply(dragEffect);
        }
    }
}