using UnityEngine;

namespace Work.Combat.Code.Core
{
    /// <summary>
    /// 공격 실행에 필요한 런타임 요청 정보
    /// </summary>
    /// <param name="Attacker">실제 공격 판정을 발생시킨 오브젝트</param>
    /// <param name="Owner">공격의 소유자</param>
    /// <param name="AttackData">이번 공격에 사용할 공격 데이터</param>
    /// <param name="Origin">공격 판정 시작 위치</param>
    /// <param name="Direction">공격 판정 방향</param>
    /// <param name="TargetLayerMask">공격 대상 레이어 마스크</param>
    public readonly record struct AttackExecutionRequest(
        GameObject Attacker,
        GameObject Owner,
        AttackDataSO AttackData,
        Vector3 Origin,
        Vector3 Direction,
        LayerMask TargetLayerMask
    );
}
