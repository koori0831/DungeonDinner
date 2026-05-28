using UnityEngine;

namespace Work.Combat.Code.Core
{
    /// <summary>
    /// 공격 1회 또는 피격 1회에 필요한 읽기 전용 정보
    /// </summary>
    /// <param name="Attacker">실제 공격 판정을 발생시킨 오브젝트</param>
    /// <param name="Owner">공격의 소유자</param>
    /// <param name="AttackType">공격 타입</param>
    /// <param name="HitPoint">피격 위치</param>
    /// <param name="HitDirection">피격 방향</param>
    /// <param name="KnockbackPower">넉백 강도</param>
    /// <param name="AttackId">공격 식별자</param>
    public readonly record struct HitContext(
        GameObject Attacker,
        GameObject Owner,
        AttackType AttackType,
        Vector3 HitPoint,
        Vector3 HitDirection,
        float KnockbackPower,
        string AttackId
    );
}
