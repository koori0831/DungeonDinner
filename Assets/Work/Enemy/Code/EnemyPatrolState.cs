using UnityEngine;
using Work.Entities.Code;
using Work.FSM.Code;

namespace Work.Enemy.Code
{
    /// <summary>
    /// 적 순찰 상태.
    /// </summary>
    public class EnemyPatrolState : EnemyBehaviourState
    {
        private Vector3 _patrolPoint;

        protected override EnemyState StateType => EnemyState.Patrol;

        public EnemyPatrolState(StateMachine stateMachine, Entity owner, int animationHash)
            : base(stateMachine, owner, animationHash)
        {
        }

        /// <summary>
        /// 순찰 상태 진입 처리.
        /// </summary>
        public override void Enter()
        {
            base.Enter();

            if (_enemy == null)
            {
                return;
            }

            _patrolPoint = _enemy.GetNextPatrolPoint();
            _enemy.MoveTo(_patrolPoint);
        }

        /// <summary>
        /// 순찰 상태 갱신.
        /// </summary>
        public override void Update()
        {
            if (CanUpdateEnemy() == false)
            {
                return;
            }

            if (_enemy.TryAcquireTarget() == true && _enemy.IsTargetInDetectionRange() == true)
            {
                ChangeState(EnemyStateNames.CHASE);
                return;
            }

            if (_enemy.HasReached(_patrolPoint) == true)
            {
                ChangeState(EnemyStateNames.IDLE);
                return;
            }

            _enemy.MoveTo(_patrolPoint);
        }
    }
}
