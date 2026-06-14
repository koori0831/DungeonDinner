using UnityEngine;
using Work.Enemy.Code.Slime;

namespace Work.Enemy.Code
{
    /// <summary>
    /// DynamicBone 힘과 루트 변형으로 슬라임 움직임을 연출하는 애니메이터.
    /// </summary>
    public sealed class SlimeDynamicBoneAnimator : MonoBehaviour
    {
        private const float MIN_DELTA_TIME = 0.0001f;
        private const float MIN_MOVE_SQR_MAGNITUDE = 0.0001f;

        [SerializeField]
        private Transform visualRoot;

        [SerializeField]
        private DynamicBone dynamicBone;

        [SerializeField]
        private EnemyStateController stateController;

        [SerializeField]
        private SlimeHopMovementModule slimeMovementModule;

        [SerializeField]
        [Tooltip("Idle 상태에서 기본적으로 살짝 출렁이는 속도.")]
        private float idlePulseSpeed = 2.5f;

        [SerializeField]
        [Tooltip("일반 이동 상태에서 출렁이는 속도. SlimeHopMovementModule의 hop phase가 없을 때 사용.")]
        private float movePulseSpeed = 7f;

        [SerializeField]
        [Tooltip("공격 상태에서 출렁이는 속도.")]
        private float attackPulseSpeed = 10f;

        [SerializeField]
        [Tooltip("Idle 상태의 기본 squash/stretch 크기.")]
        private float idleSquashAmount = 0.035f;

        [SerializeField]
        [Tooltip("일반 이동 상태의 squash/stretch 크기. SlimeHopMovementModule의 hop phase가 없을 때 사용.")]
        private float moveSquashAmount = 0.13f;

        [SerializeField]
        [Tooltip("공격 상태의 squash/stretch 크기.")]
        private float attackSquashAmount = 0.2f;

        [SerializeField]
        [Tooltip("이동 방향 반대로 DynamicBone에 주는 힘. 클수록 뒤로 더 크게 젖힘.")]
        private float moveForce = 0.45f;

        [SerializeField]
        [Tooltip("pulse에 따라 위아래로 주는 DynamicBone 힘.")]
        private float bounceForce = 0.16f;

        [SerializeField]
        [Tooltip("visualRoot scale/position이 목표 포즈로 따라가는 속도.")]
        private float poseLerpSpeed = 12f;

        [SerializeField]
        [Tooltip("DynamicBone force가 목표 힘으로 따라가는 속도.")]
        private float forceLerpSpeed = 10f;

        [SerializeField]
        [Tooltip("Charge 단계에서 납작하게 눌리는 크기. 클수록 도약 전 더 넓고 낮아짐.")]
        private float chargeSquashAmount = 0.12f;

        [SerializeField]
        [Tooltip("Jump 단계에서 진행 방향으로 길게 늘어나는 크기.")]
        private float jumpStretchAmount = 0.14f;

        [SerializeField]
        [Tooltip("Jump 단계에서 visualRoot만 위로 살짝 뜨는 높이. 실제 좌표 이동 높이는 아님.")]
        private float jumpLiftAmount = 0.08f;

        [SerializeField]
        [Tooltip("Land 단계에서 착지 충격으로 옆으로 퍼지는 크기.")]
        private float landSquashAmount = 0.14f;

        [SerializeField]
        [Tooltip("Charge/Jump 중 DynamicBone에 주는 이동 방향 반대 힘.")]
        private float hopDynamicForce = 0.25f;

        [SerializeField]
        [Tooltip("Land 중 DynamicBone에 주는 착지 반동 힘.")]
        private float landDynamicForce = 0.18f;

        private Vector3 _baseLocalScale;
        private Vector3 _baseLocalPosition;
        private Vector3 _previousPosition;
        private Vector3 _currentDynamicForce;
        private bool _hasBasePose;

        private void Awake()
        {
            ResolveReferences();
            CacheBasePose();
        }

        private void OnEnable()
        {
            _previousPosition = transform.position;
        }

        private void Update()
        {
            if (_hasBasePose == false)
            {
                CacheBasePose();
            }

            if (visualRoot == null)
            {
                return;
            }

            Vector3 frameMovement = transform.position - _previousPosition;
            _previousPosition = transform.position;
            frameMovement.y = 0f;

            EnemyState currentState = stateController != null ? stateController.CurrentState : EnemyState.None;

            if (TryApplyHopPose(currentState) == true)
            {
                return;
            }

            float motionWeight = GetMotionWeight(currentState, frameMovement);
            float pulseSpeed = GetPulseSpeed(currentState, motionWeight);
            float squashAmount = GetSquashAmount(currentState, motionWeight);
            float pulse = Mathf.Sin(Time.time * pulseSpeed) * squashAmount;

            Vector3 targetScale = GetTargetScale(currentState, pulse);
            Vector3 targetPosition = _baseLocalPosition + new Vector3(0f, Mathf.Abs(pulse) * 0.08f, 0f);

            float poseLerp = 1f - Mathf.Exp(-poseLerpSpeed * Time.deltaTime);
            visualRoot.localScale = Vector3.Lerp(visualRoot.localScale, targetScale, poseLerp);
            visualRoot.localPosition = Vector3.Lerp(visualRoot.localPosition, targetPosition, poseLerp);

            ApplyDynamicBoneForce(frameMovement, motionWeight, pulse);
        }

        private void OnDisable()
        {
            if (dynamicBone != null)
            {
                dynamicBone.m_Force = Vector3.zero;
            }
        }

        private void ResolveReferences()
        {
            if (stateController == null)
            {
                stateController = GetComponent<EnemyStateController>();
            }

            if (slimeMovementModule == null)
            {
                slimeMovementModule = GetComponentInParent<SlimeHopMovementModule>(true);
            }

            if (dynamicBone == null)
            {
                dynamicBone = GetComponentInChildren<DynamicBone>(true);
            }

            if (visualRoot == null && dynamicBone != null)
            {
                visualRoot = dynamicBone.m_ReferenceObject != null ? dynamicBone.m_ReferenceObject : dynamicBone.transform;
            }
        }

        private void CacheBasePose()
        {
            if (visualRoot == null)
            {
                return;
            }

            _baseLocalScale = visualRoot.localScale;
            _baseLocalPosition = visualRoot.localPosition;
            _hasBasePose = true;
        }

        private bool TryApplyHopPose(EnemyState currentState)
        {
            if (slimeMovementModule == null || currentState == EnemyState.Dead || currentState == EnemyState.Attack)
            {
                return false;
            }

            if (slimeMovementModule.CurrentPhase == SlimeHopPhase.Idle)
            {
                return false;
            }

            float phaseTime = slimeMovementModule.PhaseNormalizedTime;
            Vector3 targetScale = _baseLocalScale;
            Vector3 targetPosition = _baseLocalPosition;
            Vector3 targetForce = Vector3.zero;

            switch (slimeMovementModule.CurrentPhase)
            {
                case SlimeHopPhase.Charge:
                    GetChargePose(phaseTime, out targetScale, out targetPosition, out targetForce);
                    break;
                case SlimeHopPhase.Jump:
                    GetJumpPose(phaseTime, out targetScale, out targetPosition, out targetForce);
                    break;
                case SlimeHopPhase.Land:
                    GetLandPose(phaseTime, out targetScale, out targetPosition, out targetForce);
                    break;
            }

            float poseLerp = 1f - Mathf.Exp(-poseLerpSpeed * Time.deltaTime);
            visualRoot.localScale = Vector3.Lerp(visualRoot.localScale, targetScale, poseLerp);
            visualRoot.localPosition = Vector3.Lerp(visualRoot.localPosition, targetPosition, poseLerp);

            if (dynamicBone != null)
            {
                float forceLerp = 1f - Mathf.Exp(-forceLerpSpeed * Time.deltaTime);
                _currentDynamicForce = Vector3.Lerp(_currentDynamicForce, targetForce, forceLerp);
                dynamicBone.m_Force = _currentDynamicForce;
            }

            return true;
        }

        private void GetChargePose(float phaseTime, out Vector3 targetScale, out Vector3 targetPosition, out Vector3 targetForce)
        {
            float amount = Mathf.SmoothStep(0f, 1f, phaseTime) * chargeSquashAmount;
            targetScale = new Vector3(
                _baseLocalScale.x * (1f + amount),
                _baseLocalScale.y * (1f - amount),
                _baseLocalScale.z * (1f + amount)
            );
            targetPosition = _baseLocalPosition + Vector3.down * (amount * 0.06f);
            targetForce = -slimeMovementModule.HopDirection * (hopDynamicForce * amount);
        }

        private void GetJumpPose(float phaseTime, out Vector3 targetScale, out Vector3 targetPosition, out Vector3 targetForce)
        {
            float arc = Mathf.Sin(phaseTime * Mathf.PI);
            float stretch = Mathf.Lerp(jumpStretchAmount * 0.6f, jumpStretchAmount, arc);
            targetScale = new Vector3(
                _baseLocalScale.x * (1f - stretch * 0.2f),
                _baseLocalScale.y * (1f + stretch * 0.1f),
                _baseLocalScale.z * (1f + stretch)
            );
            targetPosition = _baseLocalPosition + Vector3.up * (arc * jumpLiftAmount);
            targetForce = -slimeMovementModule.HopDirection * hopDynamicForce + Vector3.up * (arc * hopDynamicForce * 0.35f);
        }

        private void GetLandPose(float phaseTime, out Vector3 targetScale, out Vector3 targetPosition, out Vector3 targetForce)
        {
            float recovery = 1f - Mathf.SmoothStep(0f, 1f, phaseTime);
            float amount = recovery * landSquashAmount;
            targetScale = new Vector3(
                _baseLocalScale.x * (1f + amount),
                _baseLocalScale.y * (1f - amount),
                _baseLocalScale.z * (1f + amount)
            );
            targetPosition = _baseLocalPosition;
            targetForce = slimeMovementModule.HopDirection * (landDynamicForce * recovery) + Vector3.down * (landDynamicForce * recovery);
        }

        private float GetMotionWeight(EnemyState currentState, Vector3 frameMovement)
        {
            if (currentState == EnemyState.Dead)
            {
                return 0f;
            }

            if (frameMovement.sqrMagnitude <= MIN_MOVE_SQR_MAGNITUDE)
            {
                return 0f;
            }

            if (currentState == EnemyState.Patrol || currentState == EnemyState.Chase || currentState == EnemyState.Return)
            {
                return 1f;
            }

            return 0.75f;
        }

        private float GetPulseSpeed(EnemyState currentState, float motionWeight)
        {
            if (currentState == EnemyState.Attack)
            {
                return attackPulseSpeed;
            }

            return motionWeight > 0f ? movePulseSpeed : idlePulseSpeed;
        }

        private float GetSquashAmount(EnemyState currentState, float motionWeight)
        {
            if (currentState == EnemyState.Attack)
            {
                return attackSquashAmount;
            }

            return motionWeight > 0f ? moveSquashAmount : idleSquashAmount;
        }

        private Vector3 GetTargetScale(EnemyState currentState, float pulse)
        {
            if (currentState == EnemyState.Dead)
            {
                return new Vector3(_baseLocalScale.x * 1.25f, _baseLocalScale.y * 0.5f, _baseLocalScale.z * 1.25f);
            }

            float horizontalScale = 1f + Mathf.Abs(pulse);
            float verticalScale = 1f - pulse;
            return new Vector3(
                _baseLocalScale.x * horizontalScale,
                _baseLocalScale.y * verticalScale,
                _baseLocalScale.z * horizontalScale
            );
        }

        private void ApplyDynamicBoneForce(Vector3 frameMovement, float motionWeight, float pulse)
        {
            if (dynamicBone == null)
            {
                return;
            }

            Vector3 targetForce = Vector3.zero;

            if (Time.deltaTime > MIN_DELTA_TIME && frameMovement.sqrMagnitude > MIN_MOVE_SQR_MAGNITUDE)
            {
                Vector3 moveDirection = frameMovement.normalized;
                targetForce -= moveDirection * (moveForce * motionWeight);
            }

            targetForce += Vector3.up * (pulse * bounceForce);

            float forceLerp = 1f - Mathf.Exp(-forceLerpSpeed * Time.deltaTime);
            _currentDynamicForce = Vector3.Lerp(_currentDynamicForce, targetForce, forceLerp);
            dynamicBone.m_Force = _currentDynamicForce;
        }
    }
}
