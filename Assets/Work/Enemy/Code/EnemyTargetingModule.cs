using UnityEngine;
using Work.Entities.Code;

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
        private const int DEFAULT_MAX_RESOLVE_COLLIDER_COUNT = 8;
        private const float MIN_RANGE = 0f;

        [SerializeField]
        private float detectionRadius = 5f;

        [SerializeField]
        private LayerMask targetLayerMask = ~0;

        [SerializeField]
        private EnemyTargetRetentionMode targetRetentionMode = EnemyTargetRetentionMode.ActivityRange;

        [SerializeField]
        [Min(0f)]
        [Tooltip("타겟 참조를 찾기 위해 검사할 최대 반경.")]
        private float targetResolveRadius = 1000f;

        [SerializeField]
        [Min(1)]
        [Tooltip("타겟 참조 검색 시 한 번에 받을 Collider 최대 수.")]
        private int maxResolveColliderCount = DEFAULT_MAX_RESOLVE_COLLIDER_COUNT;

        [SerializeField]
        private QueryTriggerInteraction targetQueryTriggerInteraction = QueryTriggerInteraction.Ignore;

        [SerializeField]
        [Min(0f)]
        [Tooltip("타겟 참조가 없을 때 다음 검색까지 대기할 시간.")]
        private float resolveInterval = 1f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("타겟 참조 검색 실패가 반복될 때 적용할 최대 재검색 대기 시간.")]
        private float maxResolveInterval = 5f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("여러 적의 타겟 참조 검색이 한 프레임에 몰리지 않도록 더하는 무작위 시간.")]
        private float resolveIntervalJitter = 0.2f;

        private Entity _owner;
        private EnemyTerritoryModule _territoryModule;
        private Collider[] _resolveColliders;
        private Transform _knownTarget;
        private Transform _target;
        private float _nextResolveTime;
        private int _resolveFailureCount;

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

            EnsureResolveBuffer();
            _nextResolveTime = Time.time + GetResolveIntervalJitter();
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

                if (TryResolveKnownTarget() == false)
                {
                    return false;
                }
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
            targetResolveRadius = Mathf.Max(MIN_RANGE, targetResolveRadius);
            maxResolveColliderCount = Mathf.Max(1, maxResolveColliderCount);
            resolveInterval = Mathf.Max(MIN_RANGE, resolveInterval);
            maxResolveInterval = Mathf.Max(resolveInterval, maxResolveInterval);
            resolveIntervalJitter = Mathf.Max(MIN_RANGE, resolveIntervalJitter);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
        }

        private bool TryResolveKnownTarget()
        {
            if (Time.time < _nextResolveTime)
            {
                return false;
            }

            EnsureResolveBuffer();

            Vector3 ownerPosition = transform.position;
            int colliderCount = Physics.OverlapSphereNonAlloc(
                ownerPosition,
                targetResolveRadius,
                _resolveColliders,
                targetLayerMask,
                targetQueryTriggerInteraction
            );

            Transform nearestTarget = null;
            float nearestSqrDistance = float.MaxValue;

            for (int i = 0; i < colliderCount; i++)
            {
                Collider targetCollider = _resolveColliders[i];

                if (targetCollider == null)
                {
                    continue;
                }

                Transform targetRoot = GetTargetRoot(targetCollider);

                if (IsValidTarget(targetRoot) == false || IsSelfTarget(targetRoot) == true)
                {
                    continue;
                }

                float sqrDistance = GetHorizontalSqrDistance(ownerPosition, targetRoot.position);

                if (sqrDistance >= nearestSqrDistance)
                {
                    continue;
                }

                nearestSqrDistance = sqrDistance;
                nearestTarget = targetRoot;
            }

            _knownTarget = nearestTarget;

            if (_knownTarget != null)
            {
                _resolveFailureCount = 0;
                ScheduleNextResolveTime();
                return true;
            }

            _resolveFailureCount++;
            ScheduleNextResolveTime();
            return false;
        }

        private void EnsureResolveBuffer()
        {
            int capacity = Mathf.Max(1, maxResolveColliderCount);

            if (_resolveColliders != null && _resolveColliders.Length == capacity)
            {
                return;
            }

            _resolveColliders = new Collider[capacity];
        }

        private void ScheduleNextResolveTime()
        {
            _nextResolveTime = Time.time + GetResolveInterval() + GetResolveIntervalJitter();
        }

        private float GetResolveInterval()
        {
            if (resolveInterval <= MIN_RANGE)
            {
                return 0f;
            }

            float multiplier = Mathf.Pow(2f, _resolveFailureCount);
            return Mathf.Min(resolveInterval * multiplier, maxResolveInterval);
        }

        private float GetResolveIntervalJitter()
        {
            if (resolveIntervalJitter <= MIN_RANGE)
            {
                return 0f;
            }

            return Random.Range(0f, resolveIntervalJitter);
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

        private bool IsSelfTarget(Transform target)
        {
            return target == transform || target.IsChildOf(transform) == true;
        }

        private static Transform GetTargetRoot(Collider targetCollider)
        {
            CharacterController characterController = targetCollider.GetComponentInParent<CharacterController>();

            if (characterController != null)
            {
                return characterController.transform;
            }

            return targetCollider.transform;
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
