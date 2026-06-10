using UnityEngine;
using UnityEngine.AI;
using Work.Entities.Code;

namespace Work.Enemy.Code.Slime
{
    /// <summary>
    /// 슬라임의 응축, 도약, 착지 리듬을 적용하는 NavMesh 기반 이동 모듈.
    /// </summary>
    public sealed class SlimeHopMovementModule : EnemyMovementModule
    {
        private const float MIN_DIRECTION_SQR_MAGNITUDE = 0.0001f;
        private const float MIN_RANGE = 0f;
        private const float NAV_MESH_SAMPLE_DISTANCE = 2f;

        [SerializeField]
        [Tooltip("이동 목적지가 없거나 목적지 도착 후 실제 대기 상태에서 유지할 시간.")]
        private float idleCooldown = 0.45f;

        [SerializeField]
        [Tooltip("아직 목적지가 남아 있을 때 착지 후 다음 점프까지 기다릴 시간.")]
        private float moveCooldown = 0.1f;

        [SerializeField]
        [Tooltip("응축 중 최신 목적지 기준으로 도약 방향을 다시 계산하는 간격.")]
        private float chargeRetargetInterval = 0.05f;

        [SerializeField]
        [Tooltip("착지 위치 보정 Warp를 수행할 최소 거리. 작을수록 착지 스냅을 더 자주 보정.")]
        private float landingWarpThreshold = 0.05f;

        [SerializeField]
        [Tooltip("응축 단계 지속 시간.")]
        private float chargeDuration = 0.16f;

        [SerializeField]
        [Tooltip("실제 좌표 이동이 발생하는 도약 단계 지속 시간.")]
        private float jumpDuration = 0.22f;

        [SerializeField]
        [Tooltip("착지 후 visual 회복 단계 지속 시간.")]
        private float landDuration = 0.2f;

        [SerializeField]
        [Tooltip("한 번 도약할 때 최대 이동 거리.")]
        private float hopDistance = 1.65f;

        [SerializeField]
        [Tooltip("목표까지 남은 거리가 이 값보다 짧으면 목표 지점까지 바로 도약.")]
        private float minHopDistance = 0.25f;

        [SerializeField]
        [Tooltip("피격 등 외부 충격 이동이 감쇠되는 속도.")]
        private float hopImpulseDamping = 8f;

        private NavMeshPath _path;
        private Vector3 _destination;
        private Vector3 _hopStart;
        private Vector3 _hopEnd;
        private Vector3 _hopDirection = Vector3.forward;
        private Vector3 _externalVelocity;
        private float _phaseStartTime;
        private float _phaseDuration;
        private float _nextChargeRetargetTime;
        private bool _hasDestination;

        /// <summary>
        /// 현재 슬라임 점프 이동 단계.
        /// </summary>
        public SlimeHopPhase CurrentPhase { get; private set; } = SlimeHopPhase.Idle;

        /// <summary>
        /// 현재 이동 단계의 0~1 진행도.
        /// </summary>
        public float PhaseNormalizedTime
        {
            get
            {
                if (_phaseDuration <= MIN_RANGE)
                {
                    return 1f;
                }

                return Mathf.Clamp01((Time.time - _phaseStartTime) / _phaseDuration);
            }
        }

        /// <summary>
        /// 현재 도약 방향.
        /// </summary>
        public Vector3 HopDirection => _hopDirection;

        /// <summary>
        /// 현재 이동 목적지 보유 여부.
        /// </summary>
        public bool HasDestination => _hasDestination;

        /// <summary>
        /// 현재 이동 목적지.
        /// </summary>
        public Vector3 Destination => _destination;

        /// <summary>
        /// 공격 상태 진입 가능 여부.
        /// </summary>
        public override bool CanEnterAttack => CurrentPhase == SlimeHopPhase.Idle;

        /// <summary>
        /// 모듈 소유자 초기화.
        /// </summary>
        /// <param name="entity">모듈 소유 엔티티.</param>
        public override void Initialize(Entity entity)
        {
            base.Initialize(entity);
            _path = new NavMeshPath();
            EnterPhase(SlimeHopPhase.Idle, 0f);
        }

        /// <summary>
        /// 지정 위치를 향한 다음 점프 목적지 갱신.
        /// </summary>
        /// <param name="targetPosition">최종 이동 목표 위치.</param>
        public override void MoveTo(Vector3 targetPosition)
        {
            if (TryGetNavMeshPosition(targetPosition, out Vector3 navMeshPosition) == false)
            {
                return;
            }

            bool hadDestination = _hasDestination;
            _destination = navMeshPosition;
            _hasDestination = true;

            if (CanUseAgent() == false)
            {
                return;
            }

            Agent.isStopped = false;

            if (hadDestination == false && CurrentPhase == SlimeHopPhase.Idle)
            {
                EnterPhase(SlimeHopPhase.Idle, moveCooldown);
            }
        }

        /// <summary>
        /// 지정 월드 방향으로 다음 점프 목적지 갱신.
        /// </summary>
        /// <param name="worldDirection">월드 기준 이동 방향.</param>
        public override void Move(Vector3 worldDirection)
        {
            worldDirection.y = 0f;

            if (worldDirection.sqrMagnitude <= MIN_DIRECTION_SQR_MAGNITUDE)
            {
                Stop();
                return;
            }

            MoveTo(transform.position + worldDirection.normalized * hopDistance);
        }

        /// <summary>
        /// 점프 이동 정지.
        /// </summary>
        public override void Stop()
        {
            _hasDestination = false;
            _externalVelocity = Vector3.zero;
            EnterPhase(SlimeHopPhase.Idle, 0f);

            if (CanUseAgent() == false)
            {
                return;
            }

            Agent.isStopped = true;
            Agent.ResetPath();
        }

        /// <summary>
        /// 외부 충격 기반 밀림 적용.
        /// </summary>
        /// <param name="direction">밀림 방향.</param>
        /// <param name="power">밀림 강도.</param>
        public override void ApplyImpulse(Vector3 direction, float power)
        {
            if (power <= MIN_RANGE)
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
        public override bool HasReached(Vector3 targetPosition)
        {
            return HasReached(targetPosition, StoppingDistance);
        }

        /// <summary>
        /// 지정 위치 도착 여부 반환.
        /// </summary>
        /// <param name="targetPosition">도착 확인 위치.</param>
        /// <param name="distance">도착 판정 거리.</param>
        /// <returns>도착 여부.</returns>
        public override bool HasReached(Vector3 targetPosition, float distance)
        {
            if (CurrentPhase != SlimeHopPhase.Idle)
            {
                return false;
            }

            return GetHorizontalSqrDistance(transform.position, targetPosition) <= distance * distance;
        }

        private void Update()
        {
            UpdateMovement();
        }

        protected override void UpdateMovement()
        {
            if (CanUseAgent() == false)
            {
                return;
            }

            UpdateHopCycle();
            ApplyExternalMovement();
        }

        private void OnValidate()
        {
            idleCooldown = Mathf.Max(MIN_RANGE, idleCooldown);
            moveCooldown = Mathf.Max(MIN_RANGE, moveCooldown);
            chargeRetargetInterval = Mathf.Max(MIN_RANGE, chargeRetargetInterval);
            landingWarpThreshold = Mathf.Max(MIN_RANGE, landingWarpThreshold);
            chargeDuration = Mathf.Max(MIN_RANGE, chargeDuration);
            jumpDuration = Mathf.Max(MIN_RANGE, jumpDuration);
            landDuration = Mathf.Max(MIN_RANGE, landDuration);
            hopDistance = Mathf.Max(MIN_RANGE, hopDistance);
            minHopDistance = Mathf.Max(MIN_RANGE, minHopDistance);
            hopImpulseDamping = Mathf.Max(MIN_RANGE, hopImpulseDamping);
        }

        private void UpdateHopCycle()
        {
            if (_hasDestination == false)
            {
                return;
            }

            switch (CurrentPhase)
            {
                case SlimeHopPhase.Idle:
                    UpdateIdle();
                    break;
                case SlimeHopPhase.Charge:
                    UpdateCharge();
                    break;
                case SlimeHopPhase.Jump:
                    UpdateJump();
                    break;
                case SlimeHopPhase.Land:
                    UpdateLand();
                    break;
            }
        }

        private void UpdateIdle()
        {
            if (IsNearDestination() == true)
            {
                _hasDestination = false;
                return;
            }

            if (IsPhaseComplete() == false)
            {
                return;
            }

            if (TryPrepareHop() == false)
            {
                EnterPhase(SlimeHopPhase.Idle, idleCooldown);
                return;
            }

            EnterPhase(SlimeHopPhase.Charge, chargeDuration);
        }

        private void UpdateCharge()
        {
            UpdateChargeTarget();
            RotateToDirection(_hopDirection);

            if (IsPhaseComplete() == false)
            {
                return;
            }

            EnterPhase(SlimeHopPhase.Jump, jumpDuration);
        }

        private void UpdateJump()
        {
            float progress = Mathf.SmoothStep(0f, 1f, PhaseNormalizedTime);
            Vector3 targetPosition = Vector3.Lerp(_hopStart, _hopEnd, progress);
            Vector3 delta = targetPosition - transform.position;
            delta.y = 0f;

            if (delta.sqrMagnitude > MIN_DIRECTION_SQR_MAGNITUDE)
            {
                Agent.Move(delta);
            }

            RotateToDirection(_hopDirection);

            if (IsPhaseComplete() == false)
            {
                return;
            }

            CorrectLandingPosition();
            EnterPhase(SlimeHopPhase.Land, landDuration);
        }

        private void UpdateLand()
        {
            if (IsPhaseComplete() == false)
            {
                return;
            }

            if (IsNearDestination() == true)
            {
                _hasDestination = false;
                EnterPhase(SlimeHopPhase.Idle, idleCooldown);
                return;
            }

            EnterPhase(SlimeHopPhase.Idle, moveCooldown);
        }

        private void UpdateChargeTarget()
        {
            if (_hasDestination == false || Time.time < _nextChargeRetargetTime)
            {
                return;
            }

            _nextChargeRetargetTime = Time.time + chargeRetargetInterval;
            TryPrepareHop();
        }

        private bool TryPrepareHop()
        {
            if (TryGetNextHopPoint(out Vector3 nextHopPoint) == false)
            {
                return false;
            }

            _hopStart = transform.position;
            _hopEnd = nextHopPoint;
            _hopDirection = _hopEnd - _hopStart;
            _hopDirection.y = 0f;

            if (_hopDirection.sqrMagnitude <= MIN_DIRECTION_SQR_MAGNITUDE)
            {
                return false;
            }

            _hopDirection.Normalize();
            RotateToDirection(_hopDirection);
            return true;
        }

        private bool TryGetNextHopPoint(out Vector3 nextHopPoint)
        {
            Vector3 startPosition = transform.position;
            float targetDistance = GetHorizontalDistance(startPosition, _destination);

            if (targetDistance <= StoppingDistance)
            {
                nextHopPoint = startPosition;
                return false;
            }

            float nextDistance = Mathf.Min(hopDistance, targetDistance);

            if (nextDistance < minHopDistance)
            {
                nextDistance = targetDistance;
            }

            if (NavMesh.CalculatePath(startPosition, _destination, NavMesh.AllAreas, GetPath()) == true && _path.status != NavMeshPathStatus.PathInvalid)
            {
                return TryGetPathPoint(startPosition, nextDistance, out nextHopPoint);
            }

            Vector3 direction = _destination - startPosition;
            direction.y = 0f;

            if (direction.sqrMagnitude <= MIN_DIRECTION_SQR_MAGNITUDE)
            {
                nextHopPoint = startPosition;
                return false;
            }

            Vector3 candidate = startPosition + direction.normalized * nextDistance;
            return TrySampleHopPoint(candidate, out nextHopPoint);
        }

        private bool TryGetPathPoint(Vector3 startPosition, float nextDistance, out Vector3 nextHopPoint)
        {
            Vector3[] corners = _path.corners;

            if (corners.Length <= 1)
            {
                return TrySampleHopPoint(_destination, out nextHopPoint);
            }

            Vector3 previousPoint = startPosition;
            float remainingDistance = nextDistance;

            for (int i = 1; i < corners.Length; i++)
            {
                Vector3 currentPoint = corners[i];
                Vector3 segment = currentPoint - previousPoint;
                segment.y = 0f;
                float segmentDistance = segment.magnitude;

                if (segmentDistance <= MIN_DIRECTION_SQR_MAGNITUDE)
                {
                    previousPoint = currentPoint;
                    continue;
                }

                if (remainingDistance <= segmentDistance)
                {
                    Vector3 candidate = previousPoint + segment.normalized * remainingDistance;
                    return TrySampleHopPoint(candidate, out nextHopPoint);
                }

                remainingDistance -= segmentDistance;
                previousPoint = currentPoint;
            }

            return TrySampleHopPoint(corners[corners.Length - 1], out nextHopPoint);
        }

        private bool TrySampleHopPoint(Vector3 candidate, out Vector3 hopPoint)
        {
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, NAV_MESH_SAMPLE_DISTANCE, NavMesh.AllAreas) == false)
            {
                hopPoint = candidate;
                return false;
            }

            hopPoint = hit.position;
            return GetHorizontalSqrDistance(transform.position, hopPoint) > StoppingDistance * StoppingDistance;
        }

        private void ApplyExternalMovement()
        {
            if (_externalVelocity.sqrMagnitude <= MIN_DIRECTION_SQR_MAGNITUDE)
            {
                _externalVelocity = Vector3.zero;
                return;
            }

            Vector3 currentVelocity = _externalVelocity;
            _externalVelocity = Vector3.Lerp(_externalVelocity, Vector3.zero, hopImpulseDamping * Time.deltaTime);
            Agent.Move(currentVelocity * Time.deltaTime);
        }

        private void CorrectLandingPosition()
        {
            Vector3 landingDelta = _hopEnd - transform.position;
            landingDelta.y = 0f;

            if (landingDelta.sqrMagnitude <= MIN_DIRECTION_SQR_MAGNITUDE)
            {
                return;
            }

            if (landingDelta.sqrMagnitude >= landingWarpThreshold * landingWarpThreshold)
            {
                Agent.Warp(_hopEnd);
                return;
            }

            Agent.Move(landingDelta);
        }

        private bool IsNearDestination()
        {
            return GetHorizontalSqrDistance(transform.position, _destination) <= StoppingDistance * StoppingDistance;
        }

        private bool IsPhaseComplete()
        {
            return Time.time >= _phaseStartTime + _phaseDuration;
        }

        private void EnterPhase(SlimeHopPhase phase, float duration)
        {
            CurrentPhase = phase;
            _phaseStartTime = Time.time;
            _phaseDuration = Mathf.Max(MIN_RANGE, duration);

            if (phase == SlimeHopPhase.Charge)
            {
                _nextChargeRetargetTime = 0f;
            }
        }

        private NavMeshPath GetPath()
        {
            if (_path == null)
            {
                _path = new NavMeshPath();
            }

            return _path;
        }

        private static float GetHorizontalDistance(Vector3 from, Vector3 to)
        {
            return Mathf.Sqrt(GetHorizontalSqrDistance(from, to));
        }

        private static float GetHorizontalSqrDistance(Vector3 from, Vector3 to)
        {
            from.y = 0f;
            to.y = 0f;
            return (to - from).sqrMagnitude;
        }
    }
}
