using Work.Entities.Code;
using Work.FSM.Code;

namespace Work.Players.Code
{
    public class PlayerIdleState : PlayerBaseState
    {
        public PlayerIdleState(StateMachine stateMachine, Entity owner, int animationHash) : base(stateMachine, owner, animationHash)
        {
        }

        public override void Enter()
        {
            base.Enter();
            _animator?.SetParam(SpeedHash, 0f);
            Player.MovementModule?.MoveStop();
        }

        public override void Update()
        {
            if (Player.InputContainer.MoveVector.sqrMagnitude > MoveInputThreshold)
            {
                _stateMachine.ChangeState(MoveStateName);
            }
        }
    }
}
