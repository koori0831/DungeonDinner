using UnityEngine;
using Work.Combat.Code.Core;

namespace Work.Combat.Code.Test
{
    /// <summary>
    /// 실제 적을 죽이지 않고 피격 요청 내용을 기록하는 테스트용 피격 대상.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CombatHitTestTarget : MonoBehaviour, IHitable
    {
        [SerializeField]
        private bool isHitable = true;

        [SerializeField]
        private HitResultType hitResultType = HitResultType.HitButNotKilled;

        [SerializeField]
        private bool logHits = true;

        [Header("Last Hit")]
        [SerializeField]
        private int hitCount;

        [SerializeField]
        private GameObject lastAttacker;

        [SerializeField]
        private GameObject lastOwner;

        [SerializeField]
        private AttackType lastAttackType;

        [SerializeField]
        private string lastAttackId;

        [SerializeField]
        private Vector3 lastHitPoint;

        [SerializeField]
        private Vector3 lastHitDirection;

        [SerializeField]
        private float lastKnockbackPower;

        [SerializeField]
        private HitResultType lastResultType;

        /// <summary>
        /// 마지막 피격 정보.
        /// </summary>
        public HitContext LastHitContext { get; private set; }

        /// <summary>
        /// 마지막 피격 처리 결과.
        /// </summary>
        public HitResult LastHitResult { get; private set; }

        /// <summary>
        /// 누적 피격 횟수.
        /// </summary>
        public int HitCount => hitCount;

        /// <summary>
        /// 피격 요청을 기록하고 설정된 결과 반환.
        /// </summary>
        /// <param name="hitContext">이번 피격 정보.</param>
        /// <returns>테스트용 피격 결과.</returns>
        public HitResult ReceiveHit(in HitContext hitContext)
        {
            LastHitContext = hitContext;
            hitCount++;
            lastAttacker = hitContext.Attacker;
            lastOwner = hitContext.Owner;
            lastAttackType = hitContext.AttackType;
            lastAttackId = hitContext.AttackId;
            lastHitPoint = hitContext.HitPoint;
            lastHitDirection = hitContext.HitDirection;
            lastKnockbackPower = hitContext.KnockbackPower;

            if (isHitable == false)
            {
                LastHitResult = new HitResult(false, false, HitResultType.Ignored);
                lastResultType = LastHitResult.ResultType;
                LogHit();
                return LastHitResult;
            }

            bool isHit = IsHitResultType(hitResultType);
            bool isKilled = hitResultType == HitResultType.Killed;
            LastHitResult = new HitResult(isHit, isKilled, hitResultType);
            lastResultType = LastHitResult.ResultType;
            LogHit();
            return LastHitResult;
        }

        /// <summary>
        /// 누적 피격 기록 초기화.
        /// </summary>
        [ContextMenu("Clear Hit History")]
        public void ClearHitHistory()
        {
            hitCount = 0;
            lastAttacker = null;
            lastOwner = null;
            lastAttackType = AttackType.None;
            lastAttackId = string.Empty;
            lastHitPoint = Vector3.zero;
            lastHitDirection = Vector3.zero;
            lastKnockbackPower = 0f;
            lastResultType = HitResultType.None;
            LastHitContext = new HitContext(null, null, AttackType.None, Vector3.zero, Vector3.zero, 0f, string.Empty);
            LastHitResult = new HitResult(false, false, HitResultType.None);
        }

        private static bool IsHitResultType(HitResultType resultType)
        {
            return resultType != HitResultType.None
                   && resultType != HitResultType.Ignored
                   && resultType != HitResultType.AlreadyDead
                   && resultType != HitResultType.InvalidTarget;
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void LogHit()
        {
            if (logHits == false)
            {
                return;
            }

            Debug.Log($"{nameof(CombatHitTestTarget)} received hit. count={hitCount}, result={lastResultType}, attackId={lastAttackId}", this);
        }
    }
}
