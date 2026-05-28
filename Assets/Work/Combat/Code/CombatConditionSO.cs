using UnityEngine;

namespace Work.Combat
{
    /// <summary>
    /// 전투 조건 평가용 ScriptableObject 기반 모듈
    /// </summary>
    public abstract class CombatConditionSO : ScriptableObject
    {
        /// <summary>
        /// 피격 정보와 대상 상태 기반 조건 충족 여부 반환
        /// </summary>
        /// <param name="hitContext">이번 피격 정보</param>
        /// <param name="conditionContext">피격 대상 런타임 정보</param>
        /// <returns>조건 충족 여부</returns>
        public abstract bool Evaluate(in HitContext hitContext, in CombatConditionContext conditionContext);
    }
}
