using UnityEngine;
using Work.Combat.Code.Core;

namespace Work.Combat.Code.Conditions
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
            if (TryEvaluateConditions(in hitContext, in conditionContext, out bool result) == false)
            {
                return false;
            }

            if (invertResult == true)
            {
                return result == false;
            }

            return result;
        }

        private bool TryEvaluateConditions(in HitContext hitContext, in CombatConditionContext conditionContext, out bool result)
        {
            if (conditions == null || conditions.Length == 0)
            {
                result = false;
                return false;
            }

            if (groupType == CombatConditionGroupType.Any)
            {
                return TryEvaluateAny(in hitContext, in conditionContext, out result);
            }

            return TryEvaluateAll(in hitContext, in conditionContext, out result);
        }

        private bool TryEvaluateAll(in HitContext hitContext, in CombatConditionContext conditionContext, out bool result)
        {
            for (int i = 0; i < conditions.Length; i++)
            {
                CombatConditionSO condition = conditions[i];

                if (condition == null)
                {
                    result = false;
                    return false;
                }

                if (condition.Evaluate(in hitContext, in conditionContext) == false)
                {
                    result = false;
                    return true;
                }
            }

            result = true;
            return true;
        }

        private bool TryEvaluateAny(in HitContext hitContext, in CombatConditionContext conditionContext, out bool result)
        {
            bool hasEvaluableCondition = false;

            for (int i = 0; i < conditions.Length; i++)
            {
                CombatConditionSO condition = conditions[i];

                if (condition == null)
                {
                    continue;
                }

                hasEvaluableCondition = true;

                if (condition.Evaluate(in hitContext, in conditionContext) == true)
                {
                    result = true;
                    return true;
                }
            }

            result = false;
            return hasEvaluableCondition;
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
