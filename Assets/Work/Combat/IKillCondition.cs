namespace Work.Combat
{
    /// <summary>
    /// 피격 대상이 이번 공격으로 사망 가능한지 판단하는 인터페이스
    /// </summary>
    public interface IKillCondition
    {
        /// <summary>
        /// 피격 정보에 따른 사망 가능 여부 반환
        /// </summary>
        /// <param name="hitContext">이번 피격 정보</param>
        /// <returns>사망 가능 여부</returns>
        bool CanKill(in HitContext hitContext);
    }
}
