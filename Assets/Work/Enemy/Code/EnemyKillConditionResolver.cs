using UnityEngine;
using Work.Combat.Code.Conditions;
using Work.Combat.Code.Core;

namespace Work.Enemy.Code
{
    /// <summary>
    /// 적에게 등록된 사망 조건 SO 검사 담당 컴포넌트.
    /// </summary>
    public sealed class EnemyKillConditionResolver : MonoBehaviour
    {
        [SerializeField]
        private EnemyStateController stateController;

        [SerializeField]
        private CombatConditionSO[] killConditions;

        private void Awake()
        {
            if (stateController == null)
            {
                stateController = GetComponent<EnemyStateController>();
            }

            ValidateKillConditions();
        }

        /// <summary>
        /// 모든 사망 조건의 충족 여부 반환.
        /// </summary>
        /// <param name="hitContext">이번 피격 정보.</param>
        /// <returns>사망 가능 여부.</returns>
        public bool CanKill(in HitContext hitContext)
        {
            if (killConditions == null || killConditions.Length == 0)
            {
                return true;
            }

            CombatConditionContext conditionContext = new CombatConditionContext(gameObject, stateController);

            for (int i = 0; i < killConditions.Length; i++)
            {
                CombatConditionSO killCondition = killConditions[i];

                if (killCondition == null)
                {
                    continue;
                }

                if (killCondition.Evaluate(in hitContext, in conditionContext) == false)
                {
                    return false;
                }
            }

            return true;
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void ValidateKillConditions()
        {
            if (killConditions == null)
            {
                return;
            }

            for (int i = 0; i < killConditions.Length; i++)
            {
                if (killConditions[i] != null)
                {
                    continue;
                }

                Debug.LogWarning($"{nameof(killConditions)} has null condition at index {i}.", this);
            }
        }
    }
}
