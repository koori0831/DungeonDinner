using System.Collections.Generic;
using UnityEngine;
using Work.Entities.Code;
using Work.Players.Code;

namespace Work.Enemy.Code
{
    /// <summary>
    /// 적 타겟 감지와 보관 담당 모듈.
    /// </summary>
    public sealed class EnemyTargetingModule : MonoBehaviour, IEntityModule
    {
        private const int MAX_TARGET_COLLIDER_COUNT = 16;
        private const float MIN_RANGE = 0f;

        [SerializeField]
        private float detectionRadius = 5f;

        [SerializeField]
        private LayerMask targetLayerMask = ~0;

        [SerializeField]
        private QueryTriggerInteraction targetQueryTriggerInteraction = QueryTriggerInteraction.Ignore;

        private readonly Collider[] _targetColliders = new Collider[MAX_TARGET_COLLIDER_COUNT];
        private readonly Dictionary<int, Player> PLAYER_BY_COLLIDER_ID = new Dictionary<int, Player>();
        private Entity _owner;
        private EnemyTerritoryModule _territoryModule;
        private Transform _target;

        /// <summary>
        /// 현재 추적 대상.
        /// </summary>
        public Transform Target => _target;

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
            entity.TryGetModule<EnemyTerritoryModule>(out _territoryModule, true);
        }

        /// <summary>
        /// 감지 범위의 플레이어 대상 확보.
        /// </summary>
        /// <returns>대상 확보 여부.</returns>
        public bool TryAcquireTarget()
        {
            if (_target != null)
            {
                if (IsTargetInActivityRange() == true)
                {
                    return true;
                }

                ClearTarget();
            }

            Transform foundTarget;

            if (TryFindTargetInDetectionRange(out foundTarget) == false)
            {
                return false;
            }

            _target = foundTarget;
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
            if (_target == null)
            {
                return false;
            }

            float sqrDistance = GetHorizontalSqrDistance(transform.position, _target.position);
            return sqrDistance <= detectionRadius * detectionRadius && IsTargetInActivityRange() == true;
        }

        /// <summary>
        /// 현재 대상의 활동 범위 포함 여부 반환.
        /// </summary>
        /// <returns>활동 범위 포함 여부.</returns>
        public bool IsTargetInActivityRange()
        {
            if (_target == null)
            {
                return false;
            }

            EnemyTerritoryModule territoryModule = GetTerritoryModule();
            return territoryModule == null || territoryModule.IsPositionInActivityRange(_target.position) == true;
        }

        private void OnValidate()
        {
            detectionRadius = Mathf.Max(MIN_RANGE, detectionRadius);
        }

        private void OnDisable()
        {
            PLAYER_BY_COLLIDER_ID.Clear();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
        }

        private bool TryFindTargetInDetectionRange(out Transform target)
        {
            target = null;
            Vector3 ownerPosition = transform.position;
            int colliderCount = Physics.OverlapSphereNonAlloc(
                ownerPosition,
                detectionRadius,
                _targetColliders,
                targetLayerMask,
                targetQueryTriggerInteraction
            );

            float nearestSqrDistance = float.MaxValue;
            EnemyTerritoryModule territoryModule = GetTerritoryModule();

            for (int i = 0; i < colliderCount; i++)
            {
                Collider targetCollider = _targetColliders[i];

                if (targetCollider == null)
                {
                    continue;
                }

                Player player = GetCachedPlayer(targetCollider);

                if (player == null)
                {
                    continue;
                }

                Transform playerTransform = player.transform;

                if (territoryModule != null && territoryModule.IsPositionInActivityRange(playerTransform.position) == false)
                {
                    continue;
                }

                float sqrDistance = GetHorizontalSqrDistance(ownerPosition, playerTransform.position);

                if (sqrDistance >= nearestSqrDistance)
                {
                    continue;
                }

                nearestSqrDistance = sqrDistance;
                target = playerTransform;
            }

            return target != null;
        }

        private Player GetCachedPlayer(Collider targetCollider)
        {
            int colliderId = targetCollider.GetInstanceID();

            if (PLAYER_BY_COLLIDER_ID.TryGetValue(colliderId, out Player cachedPlayer) == true)
            {
                return cachedPlayer;
            }

            Player player = targetCollider.GetComponentInParent<Player>();
            PLAYER_BY_COLLIDER_ID.Add(colliderId, player);
            return player;
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
