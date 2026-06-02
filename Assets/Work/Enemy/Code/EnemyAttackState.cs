using Work.Entities.Code;
using Work.FSM.Code;

namespace Work.Enemy.Code
{
    /// <summary>
    /// 적 공격 상태.
    /// </summary>
    public class EnemyAttackState : EnemyBehaviourState
    {
        protected override EnemyState StateType => EnemyState.Attack;

        public EnemyAttackState(StateMachine stateMachine, Entity owner, int animationHash)
            : base(stateMachine, owner, animationHash)
        {
        }

        /// <summary>
        /// 공격 상태 진입 처리.
        /// </summary>
        public override void Enter()
        {
            base.Enter();

            if (_enemy == null)
            {
                return;
            }

            _enemy.StopMoving();
            TryExecuteAttack();
        }

        /// <summary>
        /// 공격 상태 갱신.
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

            if (_enemy.IsTargetInAttackRange() == false)
            {
                ChangeState(EnemyStateNames.CHASE);
                return;
            }

            _enemy.StopMoving();
            _enemy.FaceTarget();
            TryExecuteAttack();
        }

        private void TryExecuteAttack()
        {
            if (_enemy == null || _enemy.CanExecuteAttack == false)
            {
                return;
            }

            _enemy.ExecuteAttack();
        }
    }
}
