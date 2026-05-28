using UnityEngine;

namespace Work.Combat
{
    /// <summary>
    /// 지정 공격 타입을 맞았는지 평가하는 조건 SO
    /// </summary>
    [CreateAssetMenu(menuName = "Combat/Condition/Attack Type")]
    public sealed class AttackTypeConditionSO : CombatConditionSO
    {
        [SerializeField]
        private AttackType requiredAttackTypes;

        /// <summary>
        /// 공격 타입 조건 충족 여부 반환
        /// </summary>
        /// <param name="hitContext">이번 피격 정보</param>
        /// <param name="conditionContext">피격 대상 런타임 정보</param>
        /// <returns>조건 충족 여부</returns>
        public override bool Evaluate(in HitContext hitContext, in CombatConditionContext conditionContext)
        {
            return (requiredAttackTypes & hitContext.AttackType) != 0;
        }
    }
}
