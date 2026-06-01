using UnityEngine;

namespace Work.Enemy.Code
{
    /// <summary>
    /// 적의 현재 전투 상태 보관 컴포넌트.
    /// </summary>
    public sealed class EnemyStateController : MonoBehaviour
    {
        [SerializeField]
        private EnemyState currentState;

        /// <summary>
        /// 현재 적 상태.
        /// </summary>
        public EnemyState CurrentState => currentState;

        /// <summary>
        /// 현재 적 상태 변경.
        /// </summary>
        /// <param name="state">변경할 적 상태.</param>
        public void SetState(EnemyState state)
        {
            currentState = state;
        }
    }
}
