using UnityEngine;
using Work.Entities.Code;
using Work.FSM.Code;

namespace Work.Enemy.Code
{
    /// <summary>
    /// 적 추격 상태.
    /// </summary>
    public class EnemyChaseState : EnemyBehaviourState
    {
        private const float DESTINATION_UPDATE_INTERVAL = 0.2f;

        private float _returnEndTime;
        private float _nextDestinationUpdateTime;
        private bool _isReturnTimerRunning;

        protected override EnemyState StateType => EnemyState.Chase;

        public EnemyChaseState(StateMachine stateMachine, Entity owner, int animationHash)
            : base(stateMachine, owner, animationHash)
        {
        }

        /// <summary>
        /// 추격 상태 진입 처리.
        /// </summary>
        public override void Enter()
        {
            base.Enter();
            ResetReturnTimer();
            _nextDestinationUpdateTime = 0f;
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
                ChangeState(EnemyStateNames.RETURN);
                return;
            }

            if (_enemy.IsTargetInActivityRange() == false)
            {
                UpdateReturnTimer();

                if (Time.time >= _returnEndTime)
                {
                    _enemy.ClearTarget();
                    ChangeState(EnemyStateNames.RETURN);
                    return;
                }

                UpdateDestination();
                return;
            }

            ResetReturnTimer();

            if (_enemy.IsTargetInAttackRange() == true && _enemy.CanEnterAttack == true)
            {
                _enemy.StopMoving();
                _enemy.FaceTarget();

                if (_enemy.IsFacingTarget(_enemy.AttackEnterAngle) == true)
                {
                    ChangeState(EnemyStateNames.ATTACK);
                }

                return;
            }

            UpdateDestination();
        }

        private void UpdateDestination()
        {
            if (_enemy.Target == null || Time.time < _nextDestinationUpdateTime)
            {
                return;
            }

            _nextDestinationUpdateTime = Time.time + DESTINATION_UPDATE_INTERVAL;
            _enemy.MoveTo(_enemy.Target.position);
        }

        private void UpdateReturnTimer()
        {
            if (_isReturnTimerRunning == true)
            {
                return;
            }

            _returnEndTime = Time.time + _enemy.ChaseReturnDelay;
            _isReturnTimerRunning = true;
        }

        private void ResetReturnTimer()
        {
            _returnEndTime = 0f;
            _isReturnTimerRunning = false;
        }
    }
}
