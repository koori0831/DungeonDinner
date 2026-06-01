using Work.Entities.Code;
using Work.FSM.Code;

namespace Work.Enemy.Code
{
    /// <summary>
    /// 적 추격 상태.
    /// </summary>
    public class EnemyChaseState : EnemyBehaviourState
    {
        protected override EnemyState StateType => EnemyState.Chase;

        public EnemyChaseState(StateMachine stateMachine, Entity owner, int animationHash)
            : base(stateMachine, owner, animationHash)
        {
        }

        /// <summary>
        /// 추격 상태 갱신.
        /// </summary>
        public override void Update()
        {
            if (CanUpdateEnemy() == false)
            {
                return;
            }

            if (_enemy.Target == null && _enemy.TryAcquireTarget() == false)
            {
                ChangeState(EnemyStateNames.IDLE);
                return;
            }

            if (_enemy.IsTargetInActivityRange() == false)
            {
                _enemy.ClearTarget();
                ChangeState(EnemyStateNames.IDLE);
                return;
            }

            if (_enemy.IsTargetInAttackRange() == true)
            {
                ChangeState(EnemyStateNames.ATTACK);
                return;
            }

            _enemy.MoveTo(_enemy.Target.position);
        }
    }
}
