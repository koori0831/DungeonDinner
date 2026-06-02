namespace Work.Enemy.Code
{
    /// <summary>
    /// 적의 전투 상태.
    /// </summary>
    public enum EnemyState
    {
        None = 0,
        Idle = 1,
        Patrol = 2,
        Attack = 3,
        Stunned = 4,
        GuardBroken = 5,
        Defensive = 6,
        Hidden = 7,
        Exposed = 8,
        Dead = 9,
        Chase = 10
    }
}
