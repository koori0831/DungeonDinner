namespace Work.Combat.Code.Core
{
    /// <summary>
    /// 지정 범위 안에서 피격 가능한 대상을 탐색하는 인터페이스
    /// </summary>
    public interface IHitCaster
    {
        /// <summary>
        /// 범위 안의 피격 가능 대상별 판정 결과를 배열에 저장
        /// </summary>
        /// <param name="request">타격 범위 탐색 요청</param>
        /// <param name="results">피격 가능 대상별 판정 결과 배열</param>
        /// <returns>결과 배열에 저장된 대상 수</returns>
        int Cast(in HitCastRequest request, HitCastResult[] results);
    }
}
