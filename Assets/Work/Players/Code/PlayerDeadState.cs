using Work.Core.EventBus;
using Work.Entities.Code;
using Work.FSM.Code;
using static Work.Input.Code.InputEvents;

namespace Work.Players.Code
{
    public class PlayerDeadState : PlayerBaseState
    {
        public PlayerDeadState(StateMachine stateMachine, Entity owner, int animationHash) : base(stateMachine, owner, animationHash)
        {
        }

        public override void Enter()
        {
            base.Enter();
            _animator?.SetApplyRootMotion(true);
            Bus<PlayerInputEnableEvent>.Raise(new(false));
        }
    }
}