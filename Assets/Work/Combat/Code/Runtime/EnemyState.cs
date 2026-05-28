namespace Work.Combat.Code.Runtime
{
    /// <summary>
    /// 적의 전투 상태
    /// </summary>
    public enum EnemyState
    {
        None = 0,
        Idle,
        Patrol,
        Attack,
        Stunned,
        GuardBroken,
        Defensive,
        Hidden,
        Exposed,
        Dead
    }
}
