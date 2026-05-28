using UnityEngine;

namespace Work.Combat
{
    /// <summary>
    /// 적의 현재 상태를 평가하는 조건 SO
    /// </summary>
    [CreateAssetMenu(menuName = "Combat/Condition/Enemy State")]
    public sealed class EnemyStateConditionSO : CombatConditionSO
    {
        [SerializeField]
        private EnemyState targetState;

        [SerializeField]
        private ConditionCompareType compareType = ConditionCompareType.Equal;

        /// <summary>
        /// 적 상태 조건 충족 여부 반환
        /// </summary>
        /// <param name="hitContext">이번 피격 정보</param>
        /// <param name="conditionContext">피격 대상 런타임 정보</param>
        /// <returns>조건 충족 여부</returns>
        public override bool Evaluate(in HitContext hitContext, in CombatConditionContext conditionContext)
        {
            EnemyStateController stateController = conditionContext.StateController;

            if (stateController == null)
            {
                return false;
            }

            if (compareType == ConditionCompareType.Equal)
            {
                return stateController.CurrentState == targetState;
            }

            if (compareType == ConditionCompareType.NotEqual)
            {
                return stateController.CurrentState != targetState;
            }

            return false;
        }
    }
}
