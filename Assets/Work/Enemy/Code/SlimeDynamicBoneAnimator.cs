using UnityEngine;

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
        private float idlePulseSpeed = 2.5f;

        [SerializeField]
        private float movePulseSpeed = 7f;

        [SerializeField]
        private float attackPulseSpeed = 10f;

        [SerializeField]
        private float idleSquashAmount = 0.035f;

        [SerializeField]
        private float moveSquashAmount = 0.13f;

        [SerializeField]
        private float attackSquashAmount = 0.2f;

        [SerializeField]
        private float moveForce = 0.45f;

        [SerializeField]
        private float bounceForce = 0.16f;

        [SerializeField]
        private float poseLerpSpeed = 12f;

        [SerializeField]
        private float forceLerpSpeed = 10f;

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

        private float GetMotionWeight(EnemyState currentState, Vector3 frameMovement)
        {
            if (currentState == EnemyState.Dead)
            {
                return 0f;
            }

            if (currentState == EnemyState.Patrol || currentState == EnemyState.Chase)
            {
                return 1f;
            }

            if (frameMovement.sqrMagnitude > MIN_MOVE_SQR_MAGNITUDE)
            {
                return 0.75f;
            }

            return 0f;
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
                targetForce -= moveDirection * moveForce * motionWeight;
            }

            targetForce += Vector3.up * pulse * bounceForce;

            float forceLerp = 1f - Mathf.Exp(-forceLerpSpeed * Time.deltaTime);
            _currentDynamicForce = Vector3.Lerp(_currentDynamicForce, targetForce, forceLerp);
            dynamicBone.m_Force = _currentDynamicForce;
        }
    }
}
