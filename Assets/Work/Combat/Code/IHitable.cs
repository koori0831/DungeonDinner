namespace Work.Combat
{
    /// <summary>
    /// 피격 가능한 오브젝트가 구현하는 인터페이스
    /// </summary>
    public interface IHitable
    {
        /// <summary>
        /// 피격 정보에 따른 피격 처리 실행
        /// </summary>
        /// <param name="hitContext">이번 피격 정보</param>
        /// <returns>피격 처리 결과</returns>
        HitResult ReceiveHit(in HitContext hitContext);
    }
}
