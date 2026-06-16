using UnityEngine;
using Work.Entities.Code;
using Work.Players.Code;

namespace Work.Enemy.Code
{
    /// <summary>
    /// 확보한 타겟을 유지할 범위 기준.
    /// </summary>
    public enum EnemyTargetRetentionMode
    {
        DetectionRange,
        ActivityRange
    }

    /// <summary>
    /// 적 타겟 감지와 보관 담당 모듈.
    /// </summary>
    public sealed class EnemyTargetingModule : MonoBehaviour, IEntityModule
    {
        private const float MIN_RANGE = 0f;

        [SerializeField]
        private float detectionRadius = 5f;

        [SerializeField]
        private EnemyTargetRetentionMode targetRetentionMode = EnemyTargetRetentionMode.ActivityRange;

        private Entity _owner;
        private EnemyTerritoryModule _territoryModule;
        private Transform _knownTarget;
        private Transform _target;

        /// <summary>
        /// 현재 추적 대상.
        /// </summary>
        public Transform Target => IsValidTarget(_target) == true ? _target : null;

        /// <summary>
        /// 감지 반경.
        /// </summary>
        public float DetectionRadius => detectionRadius;

        /// <summary>
        /// 모듈 소유자 초기화.
        /// </summary>
        /// <param name="entity">모듈 소유 엔티티.</param>
        public void Initialize(Entity entity)
        {
            _owner = entity;

            if (entity != null)
            {
                entity.TryGetModule<EnemyTerritoryModule>(out _territoryModule, true);
            }

            ResolveKnownTarget();
        }

        /// <summary>
        /// 감지 범위의 대상 확보.
        /// </summary>
        /// <returns>대상 확보 여부.</returns>
        public bool TryAcquireTarget()
        {
            if (IsValidTarget(_target) == true)
            {
                if (ShouldRetainTarget(_target) == true)
                {
                    return true;
                }

                ClearTarget();
            }

            if (IsValidTarget(_knownTarget) == false)
            {
                ClearKnownTarget();
                return false;
            }

            if (IsTransformInDetectionRange(_knownTarget) == false)
            {
                return false;
            }

            if (IsTransformInActivityRange(_knownTarget) == false)
            {
                return false;
            }

            _target = _knownTarget;
            return true;
        }

        /// <summary>
        /// 현재 대상 제거.
        /// </summary>
        public void ClearTarget()
        {
            _target = null;
        }

        /// <summary>
        /// 현재 대상의 감지 범위 포함 여부 반환.
        /// </summary>
        /// <returns>감지 범위 포함 여부.</returns>
        public bool IsTargetInDetectionRange()
        {
            if (IsValidTarget(_target) == false)
            {
                return false;
            }

            return IsTransformInDetectionRange(_target) == true && IsTargetInActivityRange() == true;
        }

        /// <summary>
        /// 현재 대상의 활동 범위 포함 여부 반환.
        /// </summary>
        /// <returns>활동 범위 포함 여부.</returns>
        public bool IsTargetInActivityRange()
        {
            if (IsValidTarget(_target) == false)
            {
                return false;
            }

            return IsTransformInActivityRange(_target);
        }

        private void OnValidate()
        {
            detectionRadius = Mathf.Max(MIN_RANGE, detectionRadius);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
        }

        private void ResolveKnownTarget()
        {
            if (PlayerTargetProvider.TryGetTarget(out Transform target) == false)
            {
                ClearKnownTarget();
                return;
            }

            _knownTarget = target;
        }

        private void ClearKnownTarget()
        {
            _knownTarget = null;
        }

        private bool IsTransformInDetectionRange(Transform target)
        {
            if (IsValidTarget(target) == false)
            {
                return false;
            }

            float sqrDistance = GetHorizontalSqrDistance(transform.position, target.position);
            return sqrDistance <= detectionRadius * detectionRadius;
        }

        private bool IsTransformInActivityRange(Transform target)
        {
            if (IsValidTarget(target) == false)
            {
                return false;
            }

            EnemyTerritoryModule territoryModule = GetTerritoryModule();
            return territoryModule == null || territoryModule.IsPositionInActivityRange(target.position) == true;
        }

        private bool ShouldRetainTarget(Transform target)
        {
            if (targetRetentionMode == EnemyTargetRetentionMode.DetectionRange)
            {
                return IsTransformInDetectionRange(target) == true && IsTransformInActivityRange(target) == true;
            }

            return IsTransformInActivityRange(target);
        }

        private static bool IsValidTarget(Transform target)
        {
            return target != null && target.gameObject.activeInHierarchy == true;
        }

        private EnemyTerritoryModule GetTerritoryModule()
        {
            if (_territoryModule == null && _owner != null)
            {
                _owner.TryGetModule<EnemyTerritoryModule>(out _territoryModule, true);
            }

            return _territoryModule;
        }

        private static float GetHorizontalSqrDistance(Vector3 from, Vector3 to)
        {
            from.y = 0f;
            to.y = 0f;
            return (to - from).sqrMagnitude;
        }
    }
}
