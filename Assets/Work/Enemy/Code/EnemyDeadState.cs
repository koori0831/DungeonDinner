using Work.Entities.Code;
using Work.FSM.Code;

namespace Work.Enemy.Code
{
    /// <summary>
    /// 적 사망 상태.
    /// </summary>
    public class EnemyDeadState : EnemyBehaviourState
    {
        protected override EnemyState StateType => EnemyState.Dead;

        public EnemyDeadState(StateMachine stateMachine, Entity owner, int animationHash)
            : base(stateMachine, owner, animationHash)
        {
        }

        /// <summary>
        /// 사망 상태 진입 처리.
        /// </summary>
        public override void Enter()
        {
            base.Enter();

            if (_enemy == null)
            {
                return;
            }

            _enemy.StopMoving();
            _enemy.ClearTarget();
        }
    }
}
