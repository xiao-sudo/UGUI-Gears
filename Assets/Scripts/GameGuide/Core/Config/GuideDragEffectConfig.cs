using System;
using GameGuide.Core.Effect;
using UnityEngine;

namespace GameGuide.Core.Config
{
    public class GuideDragEffectConfig : IGuideEffectConfig
    {
        public Action OnDragComplete { get; set; }

        public float CompleteThreshold { get; set; }

        public float HintDuration { get; set; }

        public float HintLoopDelay { get; set; }

        public RectTransform DragHint { get; set; }

        public RectTransform Start { get; set; }

        public RectTransform End { get; set; }

        public RectTransform DragTarget { get; set; }

        public void Apply(GuideDragEffect dragEffect)
        {
            dragEffect.Target = DragTarget;
            dragEffect.OnComplete(OnDragComplete);
            dragEffect.SetHintAnimation(true, HintDuration, HintLoopDelay);
            dragEffect.SetDragHint(DragHint);
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