using UnityEngine;
using UnityEngine.AI;
using Work.Entities.Code;

namespace Work.Enemy.Code
{
    /// <summary>
    /// 적 AI의 월드 기준 이동 처리 모듈.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyMovementModule : MonoBehaviour, IEntityModule
    {
        private const float MIN_DIRECTION_SQR_MAGNITUDE = 0.0001f;
        private const float MIN_RANGE = 0f;
        private const float NAV_MESH_SAMPLE_DISTANCE = 2f;

        [SerializeField]
        private float moveSpeed = 3f;

        [SerializeField]
        private float rotationSpeed = 540f;

        [SerializeField]
        private float stoppingDistance = 0.15f;

        [SerializeField]
        private float impulseDamping = 8f;

        private Entity _owner;
        private NavMeshAgent _agent;
        private Vector3 _manualMoveDirection;
        private Vector3 _externalVelocity;
        private bool _hasManualMoveDirection;

        /// <summary>
        /// 이동 도착 판정 거리.
        /// </summary>
        public float StoppingDistance => stoppingDistance;

        /// <summary>
        /// 공격 상태 진입 가능 여부.
        /// </summary>
        public virtual bool CanEnterAttack => true;

        protected NavMeshAgent Agent => _agent;

        protected float MoveSpeed => moveSpeed;

        protected float RotationSpeed => rotationSpeed;

        /// <summary>
        /// 모듈 소유자 초기화.
        /// </summary>
        /// <param name="entity">모듈 소유 엔티티.</param>
        public virtual void Initialize(Entity entity)
        {
            _owner = entity;
            _agent = GetComponent<NavMeshAgent>();
            ConfigureAgent();
            TryPlaceAgentOnNavMesh();
        }

        /// <summary>
        /// 지정 위치로 이동 시작.
        /// </summary>
        /// <param name="targetPosition">이동 목표 위치.</param>
        public virtual void MoveTo(Vector3 targetPosition)
        {
            if (TryGetNavMeshPosition(targetPosition, out Vector3 navMeshPosition) == false)
            {
                return;
            }

            _hasManualMoveDirection = false;

            if (CanUseAgent() == false)
            {
                return;
            }

            _agent.isStopped = false;
            _agent.SetDestination(navMeshPosition);
        }

        /// <summary>
        /// 지정 월드 방향으로 이동 시작.
        /// </summary>
        /// <param name="worldDirection">월드 기준 이동 방향.</param>
        public virtual void Move(Vector3 worldDirection)
        {
            worldDirection.y = 0f;

            if (worldDirection.sqrMagnitude <= MIN_DIRECTION_SQR_MAGNITUDE)
            {
                Stop();
                return;
            }

            _manualMoveDirection = worldDirection.normalized;
            _hasManualMoveDirection = true;

            if (CanUseAgent() == true)
            {
                _agent.isStopped = false;
                _agent.ResetPath();
            }
        }

        /// <summary>
        /// 이동 정지.
        /// </summary>
        public virtual void Stop()
        {
            _hasManualMoveDirection = false;
            _manualMoveDirection = Vector3.zero;

            if (CanUseAgent() == false)
            {
                return;
            }

            _agent.isStopped = true;
            _agent.ResetPath();
        }

        /// <summary>
        /// 외부 충격 기반 밀림 적용.
        /// </summary>
        /// <param name="direction">밀림 방향.</param>
        /// <param name="power">밀림 강도.</param>
        public virtual void ApplyImpulse(Vector3 direction, float power)
        {
            if (power <= 0f)
            {
                return;
            }

            direction.y = 0f;

            if (direction.sqrMagnitude <= MIN_DIRECTION_SQR_MAGNITUDE)
            {
                return;
            }

            _externalVelocity += direction.normalized * power;
        }

        /// <summary>
        /// 지정 위치 도착 여부 반환.
        /// </summary>
        /// <param name="targetPosition">도착 확인 위치.</param>
        /// <returns>도착 여부.</returns>
        public virtual bool HasReached(Vector3 targetPosition)
        {
            return HasReached(targetPosition, stoppingDistance);
        }

        /// <summary>
        /// 지정 위치 도착 여부 반환.
        /// </summary>
        /// <param name="targetPosition">도착 확인 위치.</param>
        /// <param name="distance">도착 판정 거리.</param>
        /// <returns>도착 여부.</returns>
        public virtual bool HasReached(Vector3 targetPosition, float distance)
        {
            if (CanUseAgent() == true)
            {
                if (_agent.pathPending == true)
                {
                    return false;
                }

                if (_agent.pathStatus != NavMeshPathStatus.PathInvalid)
                {
                    float reachDistance = Mathf.Max(distance, _agent.stoppingDistance);

                    if (_agent.remainingDistance <= reachDistance)
                    {
                        return true;
                    }
                }
            }

            Vector3 currentPosition = transform.position;
            currentPosition.y = 0f;
            targetPosition.y = 0f;

            float sqrDistance = (targetPosition - currentPosition).sqrMagnitude;
            return sqrDistance <= distance * distance;
        }

        /// <summary>
        /// 지정 위치를 향해 회전.
        /// </summary>
        /// <param name="targetPosition">바라볼 위치.</param>
        public virtual void FaceTowards(Vector3 targetPosition)
        {
            Vector3 direction = targetPosition - transform.position;
            direction.y = 0f;
            RotateToDirection(direction);
        }

        private void Update()
        {
            UpdateMovement();
        }

        protected virtual void UpdateMovement()
        {
            if (CanUseAgent() == false)
            {
                return;
            }

            UpdateManualMove();
            UpdateAgentRotation();
            ApplyExternalMovement();
        }

        private void OnValidate()
        {
            moveSpeed = Mathf.Max(MIN_RANGE, moveSpeed);
            rotationSpeed = Mathf.Max(MIN_RANGE, rotationSpeed);
            stoppingDistance = Mathf.Max(MIN_RANGE, stoppingDistance);
            impulseDamping = Mathf.Max(MIN_RANGE, impulseDamping);

            if (_agent == null)
            {
                _agent = GetComponent<NavMeshAgent>();
            }

            ConfigureAgent();
        }

        protected virtual void ConfigureAgent()
        {
            if (_agent == null)
            {
                return;
            }

            _agent.speed = moveSpeed;
            _agent.angularSpeed = rotationSpeed;
            _agent.stoppingDistance = stoppingDistance;
            _agent.updateRotation = false;
            _agent.updateUpAxis = true;
        }

        private void UpdateManualMove()
        {
            if (_hasManualMoveDirection == false)
            {
                return;
            }

            RotateToDirection(_manualMoveDirection);
            _agent.Move(_manualMoveDirection * (moveSpeed * Time.deltaTime));
        }

        private void UpdateAgentRotation()
        {
            if (_hasManualMoveDirection == true)
            {
                return;
            }

            Vector3 velocity = _agent.desiredVelocity;
            velocity.y = 0f;

            if (velocity.sqrMagnitude <= MIN_DIRECTION_SQR_MAGNITUDE)
            {
                return;
            }

            RotateToDirection(velocity);
        }

        private void ApplyExternalMovement()
        {
            if (_externalVelocity.sqrMagnitude <= MIN_DIRECTION_SQR_MAGNITUDE)
            {
                _externalVelocity = Vector3.zero;
                return;
            }

            Vector3 currentVelocity = _externalVelocity;
            _externalVelocity = Vector3.Lerp(_externalVelocity, Vector3.zero, impulseDamping * Time.deltaTime);
            _agent.Move(currentVelocity * Time.deltaTime);
        }

        protected void RotateToDirection(Vector3 direction)
        {
            if (_owner == null || direction.sqrMagnitude <= MIN_DIRECTION_SQR_MAGNITUDE)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            _owner.transform.rotation = Quaternion.RotateTowards(
                _owner.transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }

        protected bool CanUseAgent()
        {
            if (_agent == null || _agent.enabled == false || gameObject.activeInHierarchy == false)
            {
                return false;
            }

            return TryPlaceAgentOnNavMesh();
        }

        protected bool TryPlaceAgentOnNavMesh()
        {
            if (_agent == null || _agent.isOnNavMesh == true)
            {
                return _agent != null;
            }

            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, NAV_MESH_SAMPLE_DISTANCE, NavMesh.AllAreas) == false)
            {
                return false;
            }

            return _agent.Warp(hit.position);
        }

        protected static bool TryGetNavMeshPosition(Vector3 position, out Vector3 navMeshPosition)
        {
            if (NavMesh.SamplePosition(position, out NavMeshHit hit, NAV_MESH_SAMPLE_DISTANCE, NavMesh.AllAreas) == true)
            {
                navMeshPosition = hit.position;
                return true;
            }

            navMeshPosition = position;
            return false;
        }
    }
}
