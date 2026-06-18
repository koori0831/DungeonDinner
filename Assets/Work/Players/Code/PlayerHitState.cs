using Work.Entities.Code;
using Work.FSM.Code;

namespace Work.Players.Code
{
    public class PlayerHitState : PlayerBaseState
    {
        public PlayerHitState(StateMachine stateMachine, Entity owner, int animationHash) : base(stateMachine, owner, animationHash)
        {
        }

        public override void Enter()
        {
            base.Enter();
            _animator?.SetApplyRootMotion(true);
        }

        public override void OnTriggerEnter(AnimationEventType eventType)
        {
            base.OnTriggerEnter(eventType);

            if (eventType == AnimationEventType.End)
            {
                _animator?.SetApplyRootMotion(false);
                _stateMachine.ChangeState("Idle");
            }
        }
    }
}