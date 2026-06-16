using System;
using UnityEngine;
using Work.Combat.Code.Conditions;
using Work.Combat.Code.Core;

namespace Work.Enemy.Code.Drops
{
    /// <summary>
    /// 피격 조건과 드랍 테이블을 연결하는 적 드랍 규칙
    /// </summary>
    [Serializable]
    public sealed class EnemyDropRule
    {
        [SerializeField]
        private string ruleId;

        [SerializeField]
        private CombatConditionSO[] dropConditions;

        [SerializeField]
        private EnemyDropTableSO dropTable;

        /// <summary>
        /// 드랍 규칙 식별자
        /// </summary>
        public string RuleId => ruleId;

        /// <summary>
        /// 규칙에 연결된 드랍 테이블
        /// </summary>
        public EnemyDropTableSO DropTable => dropTable;

        /// <summary>
        /// 피격 정보 기반 드랍 규칙 충족 여부 반환
        /// </summary>
        /// <param name="hitContext">이번 피격 정보</param>
        /// <param name="conditionContext">피격 대상 조건 평가 정보</param>
        /// <returns>드랍 규칙 충족 여부</returns>
        public bool CanDrop(in HitContext hitContext, in CombatConditionContext conditionContext)
        {
            if (dropTable == null)
            {
                return false;
            }

            if (dropConditions == null || dropConditions.Length == 0)
            {
                return true;
            }

            for (int i = 0; i < dropConditions.Length; i++)
            {
                CombatConditionSO dropCondition = dropConditions[i];

                if (dropCondition == null)
                {
                    continue;
                }

                if (dropCondition.Evaluate(in hitContext, in conditionContext) == false)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
