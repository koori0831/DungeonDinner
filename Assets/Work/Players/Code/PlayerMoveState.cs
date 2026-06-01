using Work.Entities.Code;
using Work.FSM.Code;
using UnityEngine;

namespace Work.Players.Code
{
    public class PlayerMoveState : PlayerBaseState
    {
        private float _currentSpeed;

        public PlayerMoveState(StateMachine stateMachine, Entity owner, int animationHash) : base(stateMachine, owner, animationHash)
        {
        }

        public override void Enter()
        {
            base.Enter();
            _animator?.SetApplyRootMotion(true);
        }

        public override void Update()
        {
            var moveVector = Player.InputContainer.MoveVector;
            bool hasMoveInput = moveVector.sqrMagnitude > MoveInputThreshold;
            float targetSpeed = hasMoveInput ? Mathf.Clamp01(moveVector.magnitude) : 0f;
            float speedChangeRate = hasMoveInput ? SpeedAcceleration : SpeedDeceleration;

            _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, speedChangeRate * Time.deltaTime);
            _animator?.SetParam(SpeedHash, _currentSpeed);

            if (hasMoveInput)
            {
                Player.MovementModule?.Move(moveVector, true);
                return;
            }

            if (_currentSpeed <= SpeedStopThreshold)
            {
                _currentSpeed = 0f;
                _animator?.SetParam(SpeedHash, _currentSpeed);
                Player.MovementModule?.MoveStop();
                _stateMachine.ChangeState(IdleStateName);
            }
        }

        public override void Exit()
        {
            Player.MovementModule?.MoveStop();
            _currentSpeed = 0f;
            _animator?.SetParam(SpeedHash, 0f);
            _animator?.SetApplyRootMotion(false);
            base.Exit();
        }
    }
}
