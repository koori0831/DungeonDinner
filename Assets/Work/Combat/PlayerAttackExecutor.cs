using UnityEngine;

namespace Work.Combat
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

        private readonly IHitable[] _HIT_RESULTS = new IHitable[16];

        private IHitCaster _hitCaster;

        private void Awake()
        {
            _hitCaster = hitCasterBehaviour as IHitCaster;
        }

        /// <summary>
        /// 현재 무기 데이터 기반 공격 실행
        /// </summary>
        public void ExecuteAttack()
        {
            if (_hitCaster == null || currentWeaponData == null)
            {
                return;
            }

            HitContext hitContext = new HitContext(
                gameObject,
                gameObject,
                currentWeaponData.AttackType,
                transform.position,
                transform.forward,
                currentWeaponData.KnockbackPower,
                currentWeaponData.WeaponId
            );

            HitCastRequest request = new HitCastRequest(
                gameObject,
                transform.position,
                transform.forward,
                currentWeaponData.Range,
                currentWeaponData.Radius,
                enemyLayerMask
            );

            int hitCount = _hitCaster.Cast(in request, _HIT_RESULTS);

            for (int i = 0; i < hitCount; i++)
            {
                IHitable hitable = _HIT_RESULTS[i];

                if (hitable == null)
                {
                    continue;
                }

                _ = hitable.ReceiveHit(in hitContext);
            }
        }
    }
}
