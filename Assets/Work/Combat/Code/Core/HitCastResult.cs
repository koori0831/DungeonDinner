using UnityEngine;

namespace Work.Combat.Code.Core
{
    /// <summary>
    /// 타격 범위 탐색으로 확인된 대상별 피격 정보
    /// </summary>
    /// <param name="Hitable">피격 가능 대상</param>
    /// <param name="TargetCollider">피격 판정에 감지된 Collider</param>
    /// <param name="HitPoint">대상 Collider 표면에 가까운 피격 위치</param>
    /// <param name="HitDirection">공격 소유자에서 대상 방향으로 계산된 피격 방향</param>
    public readonly record struct HitCastResult(
        IHitable Hitable,
        Collider TargetCollider,
        Vector3 HitPoint,
        Vector3 HitDirection
    );
}
