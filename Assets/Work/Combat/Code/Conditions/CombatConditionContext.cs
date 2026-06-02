using UnityEngine;
using Work.Enemy.Code;

namespace Work.Combat.Code.Conditions
{
    /// <summary>
    /// 조건 평가에 필요한 피격 대상 런타임 정보.
    /// </summary>
    /// <param name="Target">피격 대상 오브젝트.</param>
    /// <param name="StateController">피격 대상 상태 컨트롤러.</param>
    public readonly record struct CombatConditionContext(
        GameObject Target,
        EnemyStateController StateController
    );
}
