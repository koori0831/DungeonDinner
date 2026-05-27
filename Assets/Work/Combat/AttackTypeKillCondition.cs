using UnityEngine;

namespace Work.Combat
{
    /// <summary>
    /// 지정 공격 타입을 맞았을 때만 사망 가능하게 하는 조건 컴포넌트
    /// </summary>
    public sealed class AttackTypeKillCondition : MonoBehaviour, IKillCondition
    {
        [SerializeField]
        private AttackType requiredAttackTypes;

        /// <summary>
        /// 공격 타입 조건 충족 여부 반환
        /// </summary>
        /// <param name="hitContext">이번 피격 정보</param>
        /// <returns>사망 가능 여부</returns>
        public bool CanKill(in HitContext hitContext)
        {
            return (requiredAttackTypes & hitContext.AttackType) != 0;
        }
    }
}
