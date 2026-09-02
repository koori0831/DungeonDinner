using System;
using System.Collections.Generic;
using UnityEngine;
using Work.NPC.Code.Data;
using Work.NPC.Code.Runtime;

namespace Work.Dispatch.Code.Runtime
{
    /// <summary>
    /// 파견 UI와 검증 코드가 NPC 내부 저장 구조를 직접 읽지 않도록 하는 읽기 전용 어댑터입니다.
    /// </summary>
    public sealed class DispatchNpcQuery : MonoBehaviour
    {
        [SerializeField] private NpcEncounterDirector encounterDirector;

        private void Awake()
        {
            EnsureDirector();
        }

        public IReadOnlyList<NpcData> GetAllNpcs()
        {
            return EnsureDirector() != null
                ? encounterDirector.GetAllNpcData()
                : Array.Empty<NpcData>();
        }

        public bool TryGetNpc(string npcId, out NpcData npc)
        {
            if (EnsureDirector() == null)
            {
                npc = null;
                return false;
            }

            return encounterDirector.TryGetNpcData(npcId, out npc);
        }

        public DispatchNpcEligibility GetEligibility(string npcId)
        {
            if (EnsureDirector() == null || encounterDirector.TryGetNpcData(npcId, out _) == false)
            {
                return new DispatchNpcEligibility(false, 0, Array.Empty<string>());
            }

            return new DispatchNpcEligibility(
                true,
                encounterDirector.GetNpcAffinity(npcId),
                encounterDirector.GetNpcRegionIds(npcId));
        }

        private NpcEncounterDirector EnsureDirector()
        {
            if (encounterDirector == null)
            {
                encounterDirector = FindFirstObjectByType<NpcEncounterDirector>();
            }

            return encounterDirector;
        }
    }
}
