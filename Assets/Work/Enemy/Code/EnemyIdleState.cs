using UnityEngine;
using Work.Entities.Code;
using Work.FSM.Code;

namespace Work.Enemy.Code
{
    /// <summary>
    /// 적 대기 상태.
    /// </summary>
    public class EnemyIdleState : EnemyBehaviourState
    {
        private float _patrolStartTime;

        protected override EnemyState StateType => EnemyState.Idle;

        public EnemyIdleState(StateMachine stateMachine, Entity owner, int animationHash)
            : base(stateMachine, owner, animationHash)
        {
        }

        /// <summary>
        /// 대기 상태 진입 처리.
        /// </summary>
        public override void Enter()
        {
            base.Enter();

            if (_enemy == null)
            {
                return;
            }

            _enemy.StopMoving();
            _patrolStartTime = Time.time + _enemy.PatrolWaitTime;
        }

        /// <summary>
        /// 대기 상태 갱신.
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

            if (Time.time >= _patrolStartTime)
            {
                ChangeState(EnemyStateNames.PATROL);
            }
        }
    }
}
