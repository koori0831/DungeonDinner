using UnityEngine;
using Work.Core.EventBus;

namespace Work.Players.Code
{
    /// <summary>
    /// 플레이어 타겟 등록 상태 변경 이벤트
    /// </summary>
    /// <param name="Target">변경된 플레이어 Transform</param>
    /// <param name="IsRegistered">등록 여부</param>
    public readonly record struct PlayerTargetChangedEvent(Transform Target, bool IsRegistered) : IEvent;
}    