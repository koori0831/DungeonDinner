using UnityEngine;
using Work.Entities.Code;

namespace Work.Enemy.Code
{
    /// <summary>
    /// 적 활동 영역, 순찰 지점, 복귀 지점 계산 담당 모듈.
    /// </summary>
    public sealed class EnemyTerritoryModule : MonoBehaviour, IEntityModule
    {
        private const int PATROL_MOVE_POINT_SAMPLE_COUNT = 6;
        private const float MIN_RANGE = 0f;

        [SerializeField]
        private float activityRadius = 8f;

        [SerializeField]
        private float returnInsideMargin = 1.5f;

        [SerializeField]
        private float patrolRadius = 5f;

        [SerializeField]
        private float patrolPointMoveRadius = 1f;

        [SerializeField]
        private float patrolWaitTime = 1.5f;

        [SerializeField]
        private float patrolPointStayTime = 3f;

        [SerializeField]
        private float patrolPointMoveInterval = 0.6f;

        [SerializeField]
        private float chaseReturnDelay = 1.5f;

        private Entity _owner;
        private EnemyMovementModule _movementModule;
        private Vector3 _activityCenter;
        private bool _hasActivityCenter;

        /// <summary>
        /// 활동 범위 중심 위치.
        /// </summary>
        public Vector3 ActivityCenter => _activityCenter;

        /// <summary>
        /// 활동 반경.
        /// </summary>
        public float ActivityRadius => activityRadius;

        /// <summary>
        /// 순찰 대기 시간.
        /// </summary>
        public float PatrolWaitTime => patrolWaitTime;

        /// <summary>
        /// 순찰 지점 주변 체류 시간.
        /// </summary>
        public float PatrolPointStayTime => patrolPointStayTime;

        /// <summary>
        /// 순찰 지점 주변 다음 이동점 선택 간격.
        /// </summary>
        public float PatrolPointMoveInterval => patrolPointMoveInterval;

        /// <summary>
        /// 활동 범위 이탈 후 복귀 전환까지 대기 시간.
        /// </summary>
        public float ChaseReturnDelay => chaseReturnDelay;

        /// <summary>
        /// 모듈 소유자 초기화.
        /// </summary>
        /// <param name="entity">모듈 소유 엔티티.</param>
        public void Initialize(Entity entity)
        {
            _owner = entity;
            _activityCenter = entity.transform.position;
            _hasActivityCenter = true;
            entity.TryGetModule<EnemyMovementModule>(out _movementModule, true);
        }

        /// <summary>
        /// 지정 위치의 활동 범위 포함 여부 반환.
        /// </summary>
        /// <param name="position">검사 위치.</param>
        /// <returns>활동 범위 포함 여부.</returns>
        public bool IsPositionInActivityRange(Vector3 position)
        {
            float sqrDistance = GetHorizontalSqrDistance(GetActivityCenter(), position);
            return sqrDistance <= activityRadius * activityRadius;
        }

        /// <summary>
        /// 복귀 완료 영역 안쪽 포함 여부 반환.
        /// </summary>
        /// <returns>복귀 완료 영역 포함 여부.</returns>
        public bool IsInsideReturnArea()
        {
            float insideRadius = Mathf.Max(MIN_RANGE, activityRadius - returnInsideMargin);
            float sqrDistance = GetHorizontalSqrDistance(GetActivityCenter(), transform.position);
            return sqrDistance <= insideRadius * insideRadius;
        }

        /// <summary>
        /// 활동 범위 내 다음 순찰 위치 반환.
        /// </summary>
        /// <returns>순찰 위치.</returns>
        public Vector3 GetNextPatrolPoint()
        {
            float radius = Mathf.Min(activityRadius, patrolRadius);

            if (radius <= MIN_RANGE)
            {
                return GetActivityCenter();
            }

            Vector2 offset = Random.insideUnitCircle * radius;
            return GetActivityCenter() + new Vector3(offset.x, 0f, offset.y);
        }

        /// <summary>
        /// 순찰 위치 주변의 다음 세부 이동 위치 반환.
        /// </summary>
        /// <param name="patrolPoint">기준 순찰 위치.</param>
        /// <returns>세부 이동 위치.</returns>
        public Vector3 GetNextPatrolMovePoint(Vector3 patrolPoint)
        {
            float radius = Mathf.Min(activityRadius, patrolPointMoveRadius);

            if (radius <= MIN_RANGE)
            {
                return ClampToActivityRange(patrolPoint);
            }

            float minimumSqrDistance = GetPatrolMoveMinimumSqrDistance();
            Vector3 selectedPoint = ClampToActivityRange(patrolPoint);
            float selectedSqrDistance = GetHorizontalSqrDistance(transform.position, selectedPoint);

            for (int i = 0; i < PATROL_MOVE_POINT_SAMPLE_COUNT; i++)
            {
                Vector2 offset = Random.insideUnitCircle * radius;
                Vector3 nextPoint = ClampToActivityRange(patrolPoint + new Vector3(offset.x, 0f, offset.y));
                float sqrDistance = GetHorizontalSqrDistance(transform.position, nextPoint);

                if (sqrDistance >= minimumSqrDistance)
                {
                    return nextPoint;
                }

                if (sqrDistance > selectedSqrDistance)
                {
                    selectedPoint = nextPoint;
                    selectedSqrDistance = sqrDistance;
                }
            }

            return selectedPoint;
        }

        /// <summary>
        /// 복귀 목표 위치 반환.
        /// </summary>
        /// <returns>복귀 목표 위치.</returns>
        public Vector3 GetReturnPoint()
        {
            return GetActivityCenter();
        }

        private void OnValidate()
        {
            activityRadius = Mathf.Max(MIN_RANGE, activityRadius);
            returnInsideMargin = Mathf.Max(MIN_RANGE, returnInsideMargin);
            patrolRadius = Mathf.Max(MIN_RANGE, patrolRadius);
            patrolPointMoveRadius = Mathf.Max(MIN_RANGE, patrolPointMoveRadius);
            patrolWaitTime = Mathf.Max(MIN_RANGE, patrolWaitTime);
            patrolPointStayTime = Mathf.Max(MIN_RANGE, patrolPointStayTime);
            patrolPointMoveInterval = Mathf.Max(MIN_RANGE, patrolPointMoveInterval);
            chaseReturnDelay = Mathf.Max(MIN_RANGE, chaseReturnDelay);
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 center = Application.isPlaying == true && _hasActivityCenter == true ? _activityCenter : transform.position;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(center, activityRadius);
        }

        private Vector3 GetActivityCenter()
        {
            if (_hasActivityCenter == true || _owner == null)
            {
                return _activityCenter;
            }

            _activityCenter = _owner.transform.position;
            _hasActivityCenter = true;
            return _activityCenter;
        }

        private float GetPatrolMoveMinimumSqrDistance()
        {
            if (_movementModule == null && _owner != null)
            {
                _owner.TryGetModule<EnemyMovementModule>(out _movementModule, true);
            }

            float minimumDistance = _movementModule != null ? _movementModule.StoppingDistance * 2f : MIN_RANGE;
            return minimumDistance * minimumDistance;
        }

        private Vector3 ClampToActivityRange(Vector3 position)
        {
            Vector3 center = GetActivityCenter();
            Vector3 offset = position - center;
            offset.y = 0f;

            if (activityRadius <= MIN_RANGE || offset.sqrMagnitude <= activityRadius * activityRadius)
            {
                position.y = center.y;
                return position;
            }

            Vector3 clampedPosition = center + offset.normalized * activityRadius;
            clampedPosition.y = center.y;
            return clampedPosition;
        }

        private static float GetHorizontalSqrDistance(Vector3 from, Vector3 to)
        {
            from.y = 0f;
            to.y = 0f;
            return (to - from).sqrMagnitude;
        }
    }
}
