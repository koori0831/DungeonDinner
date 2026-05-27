using System.Collections.Generic;
using UnityEngine;

namespace Work.Combat
{
    /// <summary>
    /// 적에게 등록된 사망 조건 검사 담당 컴포넌트
    /// </summary>
    public sealed class EnemyKillConditionResolver : MonoBehaviour
    {
        [SerializeField]
        private List<MonoBehaviour> killConditionBehaviours;

        private List<IKillCondition> _killConditions;

        private void Awake()
        {
            CacheKillConditions();
        }

        /// <summary>
        /// 모든 사망 조건의 충족 여부 반환
        /// </summary>
        /// <param name="hitContext">이번 피격 정보</param>
        /// <returns>사망 가능 여부</returns>
        public bool CanKill(in HitContext hitContext)
        {
            if (_killConditions == null || _killConditions.Count == 0)
            {
                return true;
            }

            for (int i = 0; i < _killConditions.Count; i++)
            {
                IKillCondition killCondition = _killConditions[i];

                if (killCondition == null)
                {
                    continue;
                }

                if (killCondition.CanKill(in hitContext) == false)
                {
                    return false;
                }
            }

            return true;
        }

        private void CacheKillConditions()
        {
            if (killConditionBehaviours == null || killConditionBehaviours.Count == 0)
            {
                _killConditions = new List<IKillCondition>();
                return;
            }

            _killConditions = new List<IKillCondition>(killConditionBehaviours.Count);

            for (int i = 0; i < killConditionBehaviours.Count; i++)
            {
                MonoBehaviour conditionBehaviour = killConditionBehaviours[i];

                if (conditionBehaviour == null)
                {
                    continue;
                }

                IKillCondition killCondition = conditionBehaviour as IKillCondition;
                _killConditions[i] = killCondition;

                if (killCondition == null)
                {
                    Debug.LogWarning($"{conditionBehaviour.name} 컴포넌트가 IKillCondition을 구현하지 않음", conditionBehaviour);
                }
            }
        }
    }
}
