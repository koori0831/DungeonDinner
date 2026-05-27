using System;

namespace Work.Combat
{
    /// <summary>
    /// 공격의 물리적 전투 타입
    /// </summary>
    [Flags]
    public enum AttackType
    {
        None = 0,
        Slash = 1 << 0,
        Pierce = 1 << 1,
        Blunt = 1 << 2
    }
}
