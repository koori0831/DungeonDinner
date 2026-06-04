using UnityEngine;
using Work.Combat.Code.Runtime;
using Work.Entities.Code;
using Work.FSM.Code;
using Work.Players.Code;

namespace Work.Enemy.Code
{
    /// <summary>
    /// 기본 적 행동과 FSM 연동을 담당하는 적 엔티티 기반 클래스.
    /// </summary>
    public class EnemyBase : Entity
    {
        private const int MAX_TARGET_COLLIDER_COUNT = 16;
        private const float MIN_RANGE = 0f;

        [Header("Initialize")]
        [SerializeField]
        private bool initializeOnAwake = true;

        [Header("References")]
        [SerializeField]
        private EnemyStateController stateController;

        [SerializeField]
        private CombatAttackExecutor attackExecutor;

        [Header("Range")]
        [SerializeField]
        private float activityRadius = 8f;

        [SerializeField]
        private float detectionRadius = 5f;

        [SerializeField]
        private float attackDistance = 1.5f;

        [SerializeField]
        private float patrolRadius = 5f;

        [SerializeField]
        private float patrolPointMoveRadius = 1f;

        [Header("Timing")]
        [SerializeField]
        private float patrolWaitTime = 1.5f;

        [SerializeField]
        private float patrolPointStayTime = 3f;

        [SerializeField]
        private float patrolPointMoveInterval = 0.6f;

        [SerializeField]
        private float attackCooldown = 1.25f;

        [Header("Detection")]
        [SerializeField]
        private LayerMask targetLayerMask = ~0;

        [SerializeField]
        private QueryTriggerInteraction targetQueryTriggerInteraction = QueryTriggerInteraction.Ignore;

        private Collider[] _targetColliders = new Collider[MAX_TARGET_COLLIDER_COUNT];
        private EnemyMovementModule _movementModule;
        private EntityStateModule _stateModule;
        private Transform _target;
        private Vector3 _activityCenter;
        private float _nextAttackTime;
        private bool _isInitialized;
        private bool _isDead;

        /// <summary>
        /// 현재 추적 대상.
        /// </summary>
        public Transform Target => _target;

        /// <summary>
        /// 활동 범위 중심 위치.
        /// </summary>
        public Vector3 ActivityCenter => _activityCenter;

        /// <summary>
        /// 활동 반경.
        /// </summary>
        public float ActivityRadius => activityRadius;

        /// <summary>
        /// 감지 반경.
        /// </summary>
        public float DetectionRadius => detectionRadius;

        /// <summary>
        /// 공격 거리.
        /// </summary>
        public float AttackDistance => attackDistance;

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
        /// 공격 쿨타임.
        /// </summary>
        public float AttackCooldown => attackCooldown;

        /// <summary>
        /// 사망 여부.
        /// </summary>
        public bool IsDead => _isDead;

        /// <summary>
        /// 공격 가능 여부.
        /// </summary>
        public bool CanExecuteAttack => Time.time >= _nextAttackTime;

        protected virtual void Awake()
        {
            if (initializeOnAwake == true)
            {
                Init();
            }
        }

        /// <summary>
        /// 적 엔티티 초기화.
        /// </summary>
        public override void Init()
        {
            if (_isInitialized == true)
            {
                return;
            }

            _activityCenter = transform.position;
            ResolveSceneReferences();

            base.Init();
            ResolveModules();

            _isInitialized = true;
        }

        /// <summary>
        /// 감지 범위의 플레이어 대상 확보.
        /// </summary>
        /// <returns>대상 확보 여부.</returns>
        public virtual bool TryAcquireTarget()
        {
            if (_isDead == true)
            {
                return false;
            }

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
            OnTargetAcquired(_target);
            return true;
        }

        /// <summary>
        /// 현재 대상 제거.
        /// </summary>
        public virtual void ClearTarget()
        {
            if (_target == null)
            {
                return;
            }

            _target = null;
            OnTargetLost();
        }

        /// <summary>
        /// 현재 대상의 감지 범위 포함 여부 반환.
        /// </summary>
        /// <returns>감지 범위 포함 여부.</returns>
        public virtual bool IsTargetInDetectionRange()
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
        public virtual bool IsTargetInActivityRange()
        {
            if (_target == null)
            {
                return false;
            }

            return IsPositionInActivityRange(_target.position);
        }

        /// <summary>
        /// 현재 대상의 공격 범위 포함 여부 반환.
        /// </summary>
        /// <returns>공격 범위 포함 여부.</returns>
        public virtual bool IsTargetInAttackRange()
        {
            if (_target == null)
            {
                return false;
            }

            float sqrDistance = GetHorizontalSqrDistance(transform.position, _target.position);
            return sqrDistance <= attackDistance * attackDistance && IsTargetInActivityRange() == true;
        }

        /// <summary>
        /// 활동 범위 내 다음 순찰 위치 반환.
        /// </summary>
        /// <returns>순찰 위치.</returns>
        public virtual Vector3 GetNextPatrolPoint()
        {
            float radius = Mathf.Min(activityRadius, patrolRadius);

            if (radius <= MIN_RANGE)
            {
                return _activityCenter;
            }

            Vector2 offset = Random.insideUnitCircle * radius;
            return _activityCenter + new Vector3(offset.x, 0f, offset.y);
        }

        /// <summary>
        /// 순찰 위치 주변의 다음 세부 이동 위치 반환.
        /// </summary>
        /// <param name="patrolPoint">기준 순찰 위치.</param>
        /// <returns>세부 이동 위치.</returns>
        public virtual Vector3 GetNextPatrolMovePoint(Vector3 patrolPoint)
        {
            float radius = Mathf.Min(activityRadius, patrolPointMoveRadius);

            if (radius <= MIN_RANGE)
            {
                return ClampToActivityRange(patrolPoint);
            }

            Vector2 offset = Random.insideUnitCircle * radius;
            Vector3 nextPoint = patrolPoint + new Vector3(offset.x, 0f, offset.y);
            return ClampToActivityRange(nextPoint);
        }

        /// <summary>
        /// 지정 위치로 이동.
        /// </summary>
        /// <param name="targetPosition">이동 목표 위치.</param>
        public virtual void MoveTo(Vector3 targetPosition)
        {
            if (_isDead == true)
            {
                return;
            }

            EnemyMovementModule movementModule = GetMovementModule();
            movementModule?.MoveTo(targetPosition);
        }

        /// <summary>
        /// 이동 정지.
        /// </summary>
        public virtual void StopMoving()
        {
            EnemyMovementModule movementModule = GetMovementModule();
            movementModule?.Stop();
        }

        /// <summary>
        /// 지정 위치 도착 여부 반환.
        /// </summary>
        /// <param name="targetPosition">도착 확인 위치.</param>
        /// <returns>도착 여부.</returns>
        public virtual bool HasReached(Vector3 targetPosition)
        {
            EnemyMovementModule movementModule = GetMovementModule();

            if (movementModule != null)
            {
                return movementModule.HasReached(targetPosition);
            }

            float sqrDistance = GetHorizontalSqrDistance(transform.position, targetPosition);
            return sqrDistance <= 0.01f;
        }

        /// <summary>
        /// 현재 대상을 바라보도록 회전.
        /// </summary>
        public virtual void FaceTarget()
        {
            if (_target == null)
            {
                return;
            }

            EnemyMovementModule movementModule = GetMovementModule();
            movementModule?.FaceTowards(_target.position);
        }

        /// <summary>
        /// 현재 공격 실행.
        /// </summary>
        public virtual void ExecuteAttack()
        {
            if (_isDead == true || CanExecuteAttack == false)
            {
                return;
            }

            _nextAttackTime = Time.time + attackCooldown;
            FaceTarget();
            OnBeforeAttack();

            if (attackExecutor == null)
            {
                ResolveSceneReferences();
            }

            if (attackExecutor == null)
            {
                LogMissingAttackExecutor();
                return;
            }

            attackExecutor.ExecuteAttack();
            OnAfterAttack();
        }

        /// <summary>
        /// 현재 전투 상태 변경.
        /// </summary>
        /// <param name="state">변경할 전투 상태.</param>
        public virtual void SetEnemyState(EnemyState state)
        {
            if (stateController == null)
            {
                ResolveSceneReferences();
            }

            stateController?.SetState(state);
        }

        /// <summary>
        /// 사망 상태 전환.
        /// </summary>
        public virtual void Die()
        {
            if (_isDead == true)
            {
                return;
            }

            _isDead = true;
            StopMoving();
            ClearTarget();
            SetEnemyState(EnemyState.Dead);

            EntityStateModule stateModule = GetStateModule();
            stateModule?.StateMachine.TryChangeState(EnemyStateNames.DEAD);
        }

        /// <summary>
        /// 지정 위치의 활동 범위 포함 여부 반환.
        /// </summary>
        /// <param name="position">검사 위치.</param>
        /// <returns>활동 범위 포함 여부.</returns>
        protected virtual bool IsPositionInActivityRange(Vector3 position)
        {
            float sqrDistance = GetHorizontalSqrDistance(_activityCenter, position);
            return sqrDistance <= activityRadius * activityRadius;
        }

        /// <summary>
        /// 감지 범위 내 대상 탐색.
        /// </summary>
        /// <param name="target">탐색된 대상.</param>
        /// <returns>탐색 성공 여부.</returns>
        protected virtual bool TryFindTargetInDetectionRange(out Transform target)
        {
            target = null;
            int colliderCount = Physics.OverlapSphereNonAlloc(
                transform.position,
                detectionRadius,
                _targetColliders,
                targetLayerMask,
                targetQueryTriggerInteraction
            );

            float nearestSqrDistance = float.MaxValue;

            for (int i = 0; i < colliderCount; i++)
            {
                Collider targetCollider = _targetColliders[i];

                if (targetCollider == null)
                {
                    continue;
                }

                Player player = targetCollider.GetComponentInParent<Player>();

                if (player == null)
                {
                    continue;
                }

                Transform playerTransform = player.transform;

                if (IsPositionInActivityRange(playerTransform.position) == false)
                {
                    continue;
                }

                float sqrDistance = GetHorizontalSqrDistance(transform.position, playerTransform.position);

                if (sqrDistance >= nearestSqrDistance)
                {
                    continue;
                }

                nearestSqrDistance = sqrDistance;
                target = playerTransform;
            }

            return target != null;
        }

        /// <summary>
        /// 대상 확보 후 확장 지점.
        /// </summary>
        /// <param name="target">확보된 대상.</param>
        protected virtual void OnTargetAcquired(Transform target)
        {
        }

        /// <summary>
        /// 대상 상실 후 확장 지점.
        /// </summary>
        protected virtual void OnTargetLost()
        {
        }

        /// <summary>
        /// 공격 실행 전 확장 지점.
        /// </summary>
        protected virtual void OnBeforeAttack()
        {
        }

        /// <summary>
        /// 공격 실행 후 확장 지점.
        /// </summary>
        protected virtual void OnAfterAttack()
        {
        }

        protected virtual void OnValidate()
        {
            activityRadius = Mathf.Max(MIN_RANGE, activityRadius);
            detectionRadius = Mathf.Max(MIN_RANGE, detectionRadius);
            attackDistance = Mathf.Max(MIN_RANGE, attackDistance);
            patrolRadius = Mathf.Max(MIN_RANGE, patrolRadius);
            patrolPointMoveRadius = Mathf.Max(MIN_RANGE, patrolPointMoveRadius);
            patrolWaitTime = Mathf.Max(MIN_RANGE, patrolWaitTime);
            patrolPointStayTime = Mathf.Max(MIN_RANGE, patrolPointStayTime);
            patrolPointMoveInterval = Mathf.Max(MIN_RANGE, patrolPointMoveInterval);
            attackCooldown = Mathf.Max(MIN_RANGE, attackCooldown);
        }

        protected virtual void OnDrawGizmosSelected()
        {
            Vector3 center = Application.isPlaying == true ? _activityCenter : transform.position;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(center, activityRadius);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackDistance);
        }

        private void ResolveSceneReferences()
        {
            if (stateController == null)
            {
                stateController = GetComponent<EnemyStateController>();
            }

            if (attackExecutor == null)
            {
                attackExecutor = GetComponent<CombatAttackExecutor>();
            }
        }

        private void ResolveModules()
        {
            TryGetModule<EnemyMovementModule>(out _movementModule, true);
            TryGetModule<EntityStateModule>(out _stateModule, true);
        }

        private EnemyMovementModule GetMovementModule()
        {
            if (_movementModule == null)
            {
                TryGetModule<EnemyMovementModule>(out _movementModule, true);
            }

            return _movementModule;
        }

        private EntityStateModule GetStateModule()
        {
            if (_stateModule == null)
            {
                TryGetModule<EntityStateModule>(out _stateModule, true);
            }

            return _stateModule;
        }

        private Vector3 ClampToActivityRange(Vector3 position)
        {
            Vector3 offset = position - _activityCenter;
            offset.y = 0f;

            if (activityRadius <= MIN_RANGE || offset.sqrMagnitude <= activityRadius * activityRadius)
            {
                position.y = _activityCenter.y;
                return position;
            }

            Vector3 clampedPosition = _activityCenter + offset.normalized * activityRadius;
            clampedPosition.y = _activityCenter.y;
            return clampedPosition;
        }

        private static float GetHorizontalSqrDistance(Vector3 from, Vector3 to)
        {
            from.y = 0f;
            to.y = 0f;
            return (to - from).sqrMagnitude;
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void LogMissingAttackExecutor()
        {
            Debug.LogError($"{nameof(CombatAttackExecutor)} is missing.", this);
        }
    }
}
