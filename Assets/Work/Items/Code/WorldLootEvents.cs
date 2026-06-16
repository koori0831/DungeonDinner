using UnityEngine;
using Work.Core.EventBus;

namespace Work.Items.Code
{
    /// <summary>
    /// 월드 루팅 아이템 감지 이벤트 모음
    /// </summary>
    public static class WorldLootEvents
    {
        /// <summary>
        /// 루팅 아이템 감지 범위에 수집 주체가 들어왔을 때 발생하는 이벤트
        /// </summary>
        /// <param name="LootItem">감지된 월드 루팅 아이템</param>
        /// <param name="CollectorController">감지된 수집 주체 캐릭터 컨트롤러</param>
        public readonly record struct WorldLootDetectedEvent(
            WorldLootItem LootItem,
            CharacterController CollectorController
        ) : IEvent;

        /// <summary>
        /// 루팅 아이템 감지 범위에서 수집 주체가 벗어났을 때 발생하는 이벤트
        /// </summary>
        /// <param name="LootItem">감지 범위에서 벗어난 월드 루팅 아이템</param>
        /// <param name="CollectorController">감지 범위에서 벗어난 수집 주체 캐릭터 컨트롤러</param>
        public readonly record struct WorldLootLostEvent(
            WorldLootItem LootItem,
            CharacterController CollectorController
        ) : IEvent;
    }
}
