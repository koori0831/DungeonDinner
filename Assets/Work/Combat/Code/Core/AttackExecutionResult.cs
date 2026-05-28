namespace Work.Combat.Code.Core
{
    /// <summary>
    /// 공격 실행 후 수집된 피격 처리 결과
    /// </summary>
    /// <param name="HitSuccessCount">실제 피격 성공 수</param>
    /// <param name="KilledCount">이번 공격으로 처치한 대상 수</param>
    /// <param name="LastHitResult">마지막 피격 처리 결과</param>
    /// <param name="HasAnyHit">하나 이상의 피격 성공 여부</param>
    public readonly record struct AttackExecutionResult(
        int HitSuccessCount,
        int KilledCount,
        HitResult LastHitResult,
        bool HasAnyHit
    );
}
