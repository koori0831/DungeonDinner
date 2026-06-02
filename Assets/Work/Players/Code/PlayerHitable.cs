using UnityEngine;
using Work.Combat.Code.Core;

namespace Work.Players.Code
{
    /// <summary>
    /// 플레이어의 기본 피격 판정 처리 컴포넌트.
    /// </summary>
    public sealed class PlayerHitable : MonoBehaviour, IHitable
    {
        [SerializeField]
        private bool isHitable = true;

        /// <summary>
        /// 마지막 피격 정보.
        /// </summary>
        public HitContext LastHitContext { get; private set; }

        /// <summary>
        /// 마지막 피격 처리 결과.
        /// </summary>
        public HitResult LastHitResult { get; private set; }

        /// <summary>
        /// 피격 정보 수신.
        /// </summary>
        /// <param name="hitContext">이번 피격 정보.</param>
        /// <returns>피격 처리 결과.</returns>
        public HitResult ReceiveHit(in HitContext hitContext)
        {
            LastHitContext = hitContext;

            if (isHitable == false)
            {
                LastHitResult = new HitResult(false, false, HitResultType.Ignored);
                return LastHitResult;
            }

            LastHitResult = new HitResult(true, false, HitResultType.HitButNotKilled);
            return LastHitResult;
        }
    }
}
