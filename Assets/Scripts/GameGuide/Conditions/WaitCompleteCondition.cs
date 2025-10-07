namespace GameGuide.Conditions
{
    public class WaitCompleteCondition : GuideConditionBase
    {
        private bool m_CompleteState = false;
        private bool m_LastCompleteState = false;

        public WaitCompleteCondition(string id, string description)
            : base(id, description)
        {
            m_LastCompleteState = m_CompleteState = false;
        }

        public void OnComplete()
        {
            m_CompleteState = true;
        }

        public override bool IsSatisfied()
        {
            return m_CompleteState;
        }

        protected override void OnStartListening()
        {
        }

        protected override void OnStopListening()
        {
        }

        public override bool NeedsStateChecking => true;

        public override void PerformStateCheck()
        {
            base.PerformStateCheck();

            if (m_LastCompleteState != CurrentState)
            {
                if (IsSatisfied())
                    TriggerConditionChanged();

                m_LastCompleteState = CurrentState;
            }
        }

        private bool CurrentState => m_CompleteState;
    }
}