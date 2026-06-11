using UnityEngine;
using Work.Entities.Code;
using Work.FSM.Code;

namespace Work.Enemy.Code
{
    /// <summary>
    /// 적 활동 영역 복귀 상태.
    /// </summary>
    public class EnemyReturnState : EnemyBehaviourState
    {
        private Vector3 _returnPoint;

        protected override EnemyState StateType => EnemyState.Return;

        public EnemyReturnState(StateMachine stateMachine, Entity owner, int animationHash)
            : base(stateMachine, owner, animationHash)
        {
        }

        /// <summary>
        /// 복귀 상태 진입 처리.
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
            _returnPoint = _enemy.GetReturnPoint();
            _enemy.MoveTo(_returnPoint);
        }

        /// <summary>
        /// 복귀 상태 갱신.
        /// </summary>
        public override void Update()
        {
            if (CanUpdateEnemy() == false)
            {
                return;
            }

            if (_enemy.IsInsideReturnArea() == true)
            {
                ChangeState(EnemyStateNames.PATROL);
                return;
            }

            _enemy.MoveTo(_returnPoint);
        }
    }
}
