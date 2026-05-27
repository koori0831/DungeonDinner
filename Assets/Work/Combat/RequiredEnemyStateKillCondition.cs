using UnityEngine;

namespace Work.Combat
{
    /// <summary>
    /// 적이 지정 상태일 때만 사망 가능하게 하는 조건 컴포넌트
    /// </summary>
    public sealed class RequiredEnemyStateKillCondition : MonoBehaviour, IKillCondition
    {
        [SerializeField]
        private EnemyStateController stateController;

        [SerializeField]
        private EnemyState requiredState;

        /// <summary>
        /// 필수 적 상태 조건 충족 여부 반환
        /// </summary>
        /// <param name="hitContext">이번 피격 정보</param>
        /// <returns>사망 가능 여부</returns>
        public bool CanKill(in HitContext hitContext)
        {
            if (stateController == null)
            {
                return false;
            }

            return stateController.CurrentState == requiredState;
        }
    }
}
