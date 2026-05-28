using UnityEngine;

namespace Work.Combat
{
    /// <summary>
    /// 여러 전투 조건 SO를 하나의 조건으로 조합하는 그룹 SO
    /// </summary>
    [CreateAssetMenu(menuName = "Combat/Condition/Condition Group")]
    public sealed class CombatConditionGroupSO : CombatConditionSO
    {
        [SerializeField]
        private CombatConditionGroupType groupType = CombatConditionGroupType.All;

        [SerializeField]
        private bool invertResult;

        [SerializeField]
        private CombatConditionSO[] conditions;

        private void OnValidate()
        {
            ValidateConditions();
        }

        /// <summary>
        /// 조건 그룹 평가 결과 반환
        /// </summary>
        /// <param name="hitContext">이번 피격 정보</param>
        /// <param name="conditionContext">피격 대상 런타임 정보</param>
        /// <returns>조건 충족 여부</returns>
        public override bool Evaluate(in HitContext hitContext, in CombatConditionContext conditionContext)
        {
            bool result = EvaluateConditions(in hitContext, in conditionContext);

            if (invertResult == true)
            {
                return result == false;
            }

            return result;
        }

        private bool EvaluateConditions(in HitContext hitContext, in CombatConditionContext conditionContext)
        {
            if (conditions == null || conditions.Length == 0)
            {
                return groupType == CombatConditionGroupType.All;
            }

            if (groupType == CombatConditionGroupType.Any)
            {
                return EvaluateAny(in hitContext, in conditionContext);
            }

            return EvaluateAll(in hitContext, in conditionContext);
        }

        private bool EvaluateAll(in HitContext hitContext, in CombatConditionContext conditionContext)
        {
            for (int i = 0; i < conditions.Length; i++)
            {
                CombatConditionSO condition = conditions[i];

                if (condition == null)
                {
                    continue;
                }

                if (condition.Evaluate(in hitContext, in conditionContext) == false)
                {
                    return false;
                }
            }

            return true;
        }

        private bool EvaluateAny(in HitContext hitContext, in CombatConditionContext conditionContext)
        {
            for (int i = 0; i < conditions.Length; i++)
            {
                CombatConditionSO condition = conditions[i];

                if (condition == null)
                {
                    continue;
                }

                if (condition.Evaluate(in hitContext, in conditionContext) == true)
                {
                    return true;
                }
            }

            return false;
        }

        private void ValidateConditions()
        {
            if (conditions == null)
            {
                return;
            }

            for (int i = 0; i < conditions.Length; i++)
            {
                if (conditions[i] != null)
                {
                    continue;
                }

                Debug.LogWarning($"{nameof(conditions)} has null condition at index {i}.", this);
            }
        }
    }
}
