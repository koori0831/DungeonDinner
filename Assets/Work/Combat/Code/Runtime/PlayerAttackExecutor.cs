using UnityEngine;
using Work.Combat.Code.Core;

namespace Work.Combat.Code.Runtime
{
    /// <summary>
    /// 플레이어 공격 실행과 피격 대상 호출 담당 컴포넌트
    /// </summary>
    public sealed class PlayerAttackExecutor : MonoBehaviour
    {
        [SerializeField]
        private WeaponDataSO currentWeaponData;

        [SerializeField]
        private MonoBehaviour hitCasterBehaviour;

        [SerializeField]
        private LayerMask enemyLayerMask;

        private readonly HitCastResult[] HIT_RESULTS = new HitCastResult[16];

        private IHitCaster _hitCaster;

        /// <summary>
        /// 마지막 공격의 실제 피격 성공 수
        /// </summary>
        public int LastHitSuccessCount { get; private set; }

        /// <summary>
        /// 마지막 공격의 처치 수
        /// </summary>
        public int LastKilledCount { get; private set; }

        /// <summary>
        /// 마지막 공격의 마지막 피격 처리 결과
        /// </summary>
        public HitResult LastHitResult { get; private set; }

        /// <summary>
        /// 마지막 공격에서 하나라도 피격 성공했는지 여부
        /// </summary>
        public bool HasAnyHit { get; private set; }

        private void Awake()
        {
            _hitCaster = hitCasterBehaviour as IHitCaster;

            if (_hitCaster == null)
            {
                LogInvalidHitCaster();
            }
        }

        /// <summary>
        /// 현재 무기 데이터 기반 공격 실행
        /// </summary>
        public void ExecuteAttack()
        {
            ResetLastAttackResult();

            if (_hitCaster == null)
            {
                LogMissingHitCaster();
                return;
            }

            if (currentWeaponData == null)
            {
                LogMissingWeaponData();
                return;
            }

            HitCastRequest request = new HitCastRequest(
                gameObject,
                transform.position,
                transform.forward,
                currentWeaponData.Range,
                currentWeaponData.Radius,
                enemyLayerMask
            );

            int hitCount = _hitCaster.Cast(in request, HIT_RESULTS);

            for (int i = 0; i < hitCount; i++)
            {
                HitCastResult hitCastResult = HIT_RESULTS[i];
                IHitable hitable = hitCastResult.Hitable;

                if (hitable == null)
                {
                    continue;
                }

                HitContext hitContext = CreateHitContext(in hitCastResult);
                HitResult hitResult = hitable.ReceiveHit(in hitContext);
                CollectHitResult(hitResult);
            }
        }

        private HitContext CreateHitContext(in HitCastResult hitCastResult)
        {
            return new HitContext(
                gameObject,
                gameObject,
                currentWeaponData.AttackType,
                hitCastResult.HitPoint,
                hitCastResult.HitDirection,
                currentWeaponData.KnockbackPower,
                currentWeaponData.WeaponId
            );
        }

        private void CollectHitResult(HitResult hitResult)
        {
            LastHitResult = hitResult;

            if (hitResult.IsHit == true)
            {
                LastHitSuccessCount++;
                HasAnyHit = true;
            }

            if (hitResult.IsKilled == true)
            {
                LastKilledCount++;
            }
        }

        private void ResetLastAttackResult()
        {
            LastHitSuccessCount = 0;
            LastKilledCount = 0;
            LastHitResult = new HitResult(false, false, HitResultType.None);
            HasAnyHit = false;
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void LogInvalidHitCaster()
        {
            Debug.LogError($"{nameof(hitCasterBehaviour)} must implement {nameof(IHitCaster)}.", this);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void LogMissingHitCaster()
        {
            Debug.LogError($"{nameof(IHitCaster)} is missing. Attack execution stopped.", this);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void LogMissingWeaponData()
        {
            Debug.LogError($"{nameof(currentWeaponData)} is missing. Attack execution stopped.", this);
        }
    }
}
