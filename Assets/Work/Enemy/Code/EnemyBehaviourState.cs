using UnityEngine;
using Work.Entities.Code;
using Work.FSM.Code;

namespace Work.Enemy.Code
{
    /// <summary>
    /// 적 FSM 상태 공통 기반 클래스.
    /// </summary>
    public abstract class EnemyBehaviourState : State
    {
        protected EnemyBase _enemy;

        /// <summary>
        /// 상태 진입 시 반영할 적 전투 상태.
        /// </summary>
        protected abstract EnemyState StateType { get; }

        protected EnemyBehaviourState(StateMachine stateMachine, Entity owner, int animationHash)
            : base(stateMachine, owner, animationHash)
        {
            _enemy = owner as EnemyBase;
        }

        /// <summary>
        /// 상태 진입 처리.
        /// </summary>
        public override void Enter()
        {
            base.Enter();

            if (_enemy == null)
            {
                LogInvalidOwner();
                return;
            }

            _enemy.SetEnemyState(StateType);
        }

        /// <summary>
        /// 적 상태 갱신 가능 여부 반환.
        /// </summary>
        /// <returns>갱신 가능 여부.</returns>
        protected bool CanUpdateEnemy()
        {
            return _enemy != null && _enemy.IsDead == false;
        }

        /// <summary>
        /// FSM 상태 변경.
        /// </summary>
        /// <param name="stateName">변경할 상태 이름.</param>
        protected void ChangeState(string stateName)
        {
            _stateMachine.ChangeState(stateName);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void LogInvalidOwner()
        {
            Debug.LogError($"{nameof(EnemyBehaviourState)} owner must be {nameof(EnemyBase)}.");
        }
    }
}
