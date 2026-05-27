namespace Work.Combat
{
    /// <summary>
    /// 피격 처리 결과 타입
    /// </summary>
    public enum HitResultType
    {
        None = 0,
        Hit,
        HitButNotKilled,
        Killed,
        Ignored,
        AlreadyDead,
        InvalidTarget
    }

    /// <summary>
    /// 피격 처리 결과
    /// </summary>
    /// <param name="IsHit">실제 피격 처리 여부</param>
    /// <param name="IsKilled">이번 피격으로 인한 사망 여부</param>
    /// <param name="ResultType">상세 결과 타입</param>
    public readonly record struct HitResult(
        bool IsHit,
        bool IsKilled,
        HitResultType ResultType
    );
}
