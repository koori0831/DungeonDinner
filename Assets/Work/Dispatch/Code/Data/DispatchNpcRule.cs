using System;
using UnityEngine;

namespace Work.Dispatch.Code.Data
{
    [Serializable]
    public sealed class DispatchNpcRule
    {
        [SerializeField] private string npcId;
        [SerializeField, Min(0)] private int requiredAffinity;
        [SerializeField, Range(50, 200)] private int timeMultiplierPercent = 100;

        public string NpcId => npcId;
        public int RequiredAffinity => Mathf.Max(0, requiredAffinity);
        public int TimeMultiplierPercent => Mathf.Clamp(timeMultiplierPercent, 50, 200);
    }
}
