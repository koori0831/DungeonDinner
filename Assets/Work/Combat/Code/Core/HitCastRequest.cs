using UnityEngine;

namespace Work.Combat.Code.Core
{
    /// <summary>
    /// 타격 범위 탐색에 필요한 읽기 전용 정보
    /// </summary>
    /// <param name="Owner">공격의 소유자</param>
    /// <param name="Origin">탐색 시작 위치</param>
    /// <param name="Direction">탐색 방향</param>
    /// <param name="Range">탐색 거리</param>
    /// <param name="Radius">탐색 반지름</param>
    /// <param name="TargetLayerMask">탐색 대상 레이어 마스크</param>
    public readonly record struct HitCastRequest(
        GameObject Owner,
        Vector3 Origin,
        Vector3 Direction,
        float Range,
        float Radius,
        LayerMask TargetLayerMask
    );
}
