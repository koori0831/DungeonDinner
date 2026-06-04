using UnityEngine;
using Work.Entities.Code;

namespace Work.Enemy.Code
{
    /// <summary>
    /// 적 AI의 월드 기준 이동 처리 모듈.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class EnemyMovementModule : MonoBehaviour, IEntityModule
    {
        private const float MIN_DIRECTION_SQR_MAGNITUDE = 0.0001f;

        [SerializeField]
        private float moveSpeed = 3f;

        [SerializeField]
        private float rotationSpeed = 540f;

        [SerializeField]
        private float stoppingDistance = 0.15f;

        [SerializeField]
        private float impulseDamping = 8f;

        private Entity _owner;
        private CharacterController _controller;
        private Vector3 _moveTarget;
        private Vector3 _manualMoveDirection;
        private Vector3 _externalVelocity;
        private float _verticalVelocity;
        private bool _hasMoveTarget;
        private bool _hasManualMoveDirection;

        /// <summary>
        /// 이동 도착 판정 거리.
        /// </summary>
        public float StoppingDistance => stoppingDistance;

        /// <summary>
        /// 모듈 소유자 초기화.
        /// </summary>
        /// <param name="entity">모듈 소유 엔티티.</param>
        public void Initialize(Entity entity)
        {
            _owner = entity;
            _controller = GetComponent<CharacterController>();
        }

        /// <summary>
        /// 지정 위치로 이동 시작.
        /// </summary>
        /// <param name="targetPosition">이동 목표 위치.</param>
        public void MoveTo(Vector3 targetPosition)
        {
            _moveTarget = targetPosition;
            _hasMoveTarget = true;
            _hasManualMoveDirection = false;
        }

        /// <summary>
        /// 지정 월드 방향으로 이동 시작.
        /// </summary>
        /// <param name="worldDirection">월드 기준 이동 방향.</param>
        public void Move(Vector3 worldDirection)
        {
            worldDirection.y = 0f;

            if (worldDirection.sqrMagnitude <= MIN_DIRECTION_SQR_MAGNITUDE)
            {
                Stop();
                return;
            }

            _manualMoveDirection = worldDirection.normalized;
            _hasManualMoveDirection = true;
            _hasMoveTarget = false;
        }

        /// <summary>
        /// 이동 정지.
        /// </summary>
        public void Stop()
        {
            _hasMoveTarget = false;
            _hasManualMoveDirection = false;
            _manualMoveDirection = Vector3.zero;
        }

        /// <summary>
        /// 외부 충격 기반 밀림 적용.
        /// </summary>
        /// <param name="direction">밀림 방향.</param>
        /// <param name="power">밀림 강도.</param>
        public void ApplyImpulse(Vector3 direction, float power)
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
        public bool HasReached(Vector3 targetPosition)
        {
            return HasReached(targetPosition, stoppingDistance);
        }

        /// <summary>
        /// 지정 위치 도착 여부 반환.
        /// </summary>
        /// <param name="targetPosition">도착 확인 위치.</param>
        /// <param name="distance">도착 판정 거리.</param>
        /// <returns>도착 여부.</returns>
        public bool HasReached(Vector3 targetPosition, float distance)
        {
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
        public void FaceTowards(Vector3 targetPosition)
        {
            Vector3 direction = targetPosition - transform.position;
            direction.y = 0f;
            RotateToDirection(direction);
        }

        private void Update()
        {
            if (_controller == null)
            {
                return;
            }

            Vector3 horizontalVelocity = GetHorizontalVelocity();
            Vector3 externalVelocity = GetExternalVelocity();
            ApplyGravity();

            Vector3 totalVelocity = horizontalVelocity + externalVelocity + new Vector3(0f, _verticalVelocity, 0f);
            _controller.Move(totalVelocity * Time.deltaTime);
        }

        private Vector3 GetHorizontalVelocity()
        {
            if (_hasMoveTarget == true)
            {
                Vector3 direction = _moveTarget - transform.position;
                direction.y = 0f;

                if (direction.sqrMagnitude <= stoppingDistance * stoppingDistance)
                {
                    _hasMoveTarget = false;
                    return Vector3.zero;
                }

                Vector3 normalizedDirection = direction.normalized;
                RotateToDirection(normalizedDirection);
                return normalizedDirection * moveSpeed;
            }

            if (_hasManualMoveDirection == true)
            {
                RotateToDirection(_manualMoveDirection);
                return _manualMoveDirection * moveSpeed;
            }

            return Vector3.zero;
        }

        private void ApplyGravity()
        {
            if (_controller.isGrounded == true && _verticalVelocity < 0f)
            {
                _verticalVelocity = 0f;
                return;
            }

            _verticalVelocity += Physics.gravity.y * Time.deltaTime;
        }

        private Vector3 GetExternalVelocity()
        {
            if (_externalVelocity.sqrMagnitude <= MIN_DIRECTION_SQR_MAGNITUDE)
            {
                _externalVelocity = Vector3.zero;
                return Vector3.zero;
            }

            Vector3 currentVelocity = _externalVelocity;
            _externalVelocity = Vector3.Lerp(_externalVelocity, Vector3.zero, impulseDamping * Time.deltaTime);

            return currentVelocity;
        }

        private void RotateToDirection(Vector3 direction)
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
    }
}
