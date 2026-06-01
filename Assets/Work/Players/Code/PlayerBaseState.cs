using UnityEngine;
using Work.FSM.Code;
using Work.Entities.Code;

namespace Work.Players.Code
{
    public abstract class PlayerBaseState : State
    {
        protected const string IdleStateName = "Idle";
        protected const string MoveStateName = "Move";
        protected const float MoveInputThreshold = 0.1f;
        protected const float SpeedStopThreshold = 0.03f;
        protected const float SpeedAcceleration = 4f;
        protected const float SpeedDeceleration = 2f;
        protected static readonly int SpeedHash = Animator.StringToHash("Speed");

        protected Player Player;

        protected PlayerBaseState(StateMachine stateMachine, Entity owner, int animationHash) : base(stateMachine, owner, animationHash)
        {
            Player = owner as Player;
            Debug.Assert(Player != null, "Owner is not a Player");
        }
    }
}
