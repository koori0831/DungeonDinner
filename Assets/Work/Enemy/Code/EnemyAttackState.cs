using UnityEngine;
using Work.Entities.Code;
using Work.FSM.Code;

namespace Work.Enemy.Code
{
    /// <summary>
    /// 적 공격 상태.
    /// </summary>
    public class EnemyAttackState : EnemyBehaviourState
    {
        private EnemyAttackPhase _phase;
        private float _phaseEndTime;
        private bool _hasExecutedAttack;

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
            BeginWindup();
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

            if (ValidateAttackTarget() == false)
            {
                return;
            }

            UpdateAttackPhase();
        }

        private bool ValidateAttackTarget()
        {
            if (_enemy.Target == null && _enemy.TryAcquireTarget() == false)
            {
                ChangeState(EnemyStateNames.RETURN);
                return false;
            }

            if (_enemy.IsTargetInAttackRange() == false)
            {
                ChangeState(EnemyStateNames.CHASE);
                return false;
            }

            if (_enemy.IsFacingTarget(_enemy.AttackEnterAngle) == false)
            {
                ChangeState(EnemyStateNames.CHASE);
                return false;
            }

            return true;
        }

        private void BeginWindup()
        {
            if (_enemy == null || _enemy.CanExecuteAttack == false)
            {
                ChangeState(EnemyStateNames.CHASE);
                return;
            }

            _phase = EnemyAttackPhase.Windup;
            _hasExecutedAttack = false;

            float windupTime = Mathf.Max(0f, _enemy.AttackWindupTime);
            _phaseEndTime = Time.time + windupTime;

            if (windupTime <= 0f)
            {
                UpdateAttackPhase();
            }
        }

        private void UpdateAttackPhase()
        {
            if (_phase == EnemyAttackPhase.Windup)
            {
                if (Time.time < _phaseEndTime)
                {
                    return;
                }

                ExecuteAttackOnce();
                BeginRecovery();
                return;
            }

            if (_phase == EnemyAttackPhase.Recovery)
            {
                if (Time.time < _phaseEndTime)
                {
                    return;
                }

                ChangeState(EnemyStateNames.CHASE);
            }
        }

        private void ExecuteAttackOnce()
        {
            if (_enemy == null || _hasExecutedAttack == true)
            {
                return;
            }

            _phase = EnemyAttackPhase.Execute;
            _hasExecutedAttack = true;

            _enemy.ExecuteAttack();
        }

        private void BeginRecovery()
        {
            _phase = EnemyAttackPhase.Recovery;

            float recoveryTime = Mathf.Max(0f, _enemy.AttackRecoveryTime);
            _phaseEndTime = Time.time + recoveryTime;

            if (recoveryTime <= 0f)
            {
                ChangeState(EnemyStateNames.CHASE);
            }
        }

        private enum EnemyAttackPhase
        {
            Windup,
            Execute,
            Recovery
        }
    }
}
