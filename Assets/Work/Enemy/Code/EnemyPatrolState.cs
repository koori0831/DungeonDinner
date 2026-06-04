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
        private Vector3 _movePoint;
        private float _patrolEndTime;
        private float _nextMovePointTime;
        private bool _isMovingAroundPatrolPoint;

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
            _movePoint = _patrolPoint;
            _patrolEndTime = 0f;
            _nextMovePointTime = 0f;
            _isMovingAroundPatrolPoint = false;
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

            if (_isMovingAroundPatrolPoint == false)
            {
                if (_enemy.HasReached(_patrolPoint) == true)
                {
                    StartMovingAroundPatrolPoint();
                    return;
                }

                _enemy.MoveTo(_patrolPoint);
                return;
            }

            if (Time.time >= _patrolEndTime)
            {
                ChangeState(EnemyStateNames.IDLE);
                return;
            }

            if (_enemy.HasReached(_movePoint) == true && Time.time >= _nextMovePointTime)
            {
                SelectNextMovePoint();
            }

            _enemy.MoveTo(_movePoint);
        }

        private void StartMovingAroundPatrolPoint()
        {
            _isMovingAroundPatrolPoint = true;
            _patrolEndTime = Time.time + _enemy.PatrolPointStayTime;
            SelectNextMovePoint();
        }

        private void SelectNextMovePoint()
        {
            _movePoint = _enemy.GetNextPatrolMovePoint(_patrolPoint);
            _nextMovePointTime = Time.time + _enemy.PatrolPointMoveInterval;
            _enemy.MoveTo(_movePoint);
        }
    }
}
