using GameGuide.Core;

namespace GameGuide.Effect.Config
{
    public interface IGuideEffectConfig
    {
        void Apply(IGuideEffect guideEffect);
    }
}