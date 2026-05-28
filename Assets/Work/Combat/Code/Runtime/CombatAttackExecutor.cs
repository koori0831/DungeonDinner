using UnityEngine;
using Work.Combat.Code.Core;

namespace Work.Combat.Code.Runtime
{
    /// <summary>
    /// 전투 주체의 공격 실행과 피격 대상 호출 담당 컴포넌트
    /// </summary>
    public sealed class CombatAttackExecutor : MonoBehaviour
    {
        [SerializeField]
        private AttackDataSO attackData;

        [SerializeField]
        private MonoBehaviour hitCasterBehaviour;

        [SerializeField]
        private Transform attackOrigin;

        [SerializeField]
        private LayerMask targetLayerMask;

        private readonly HitCastResult[] HIT_RESULTS = new HitCastResult[16];

        private IHitCaster _hitCaster;

        /// <summary>
        /// 마지막 공격의 실제 피격 성공 수
        /// </summary>
        public int LastHitSuccessCount { get; private set; }

        /// <summary>
        /// 마지막 공격의 처치 수
        /// </summary>
        public int LastKilledCount { get; private set; }

        /// <summary>
        /// 마지막 공격의 마지막 피격 처리 결과
        /// </summary>
        public HitResult LastHitResult { get; private set; }

        /// <summary>
        /// 마지막 공격에서 하나라도 피격 성공했는지 여부
        /// </summary>
        public bool HasAnyHit { get; private set; }

        private void Awake()
        {
            _hitCaster = hitCasterBehaviour as IHitCaster;

            if (_hitCaster == null)
            {
                LogInvalidHitCaster();
            }
        }

        /// <summary>
        /// 현재 설정된 공격 데이터 기반 공격 실행
        /// </summary>
        /// <returns>공격 실행 결과</returns>
        public AttackExecutionResult ExecuteAttack()
        {
            AttackExecutionRequest request = CreateDefaultRequest(attackData);
            return ExecuteAttack(in request);
        }

        /// <summary>
        /// 지정 공격 데이터 기반 공격 실행
        /// </summary>
        /// <param name="attackData">이번 공격에 사용할 공격 데이터</param>
        /// <returns>공격 실행 결과</returns>
        public AttackExecutionResult ExecuteAttack(AttackDataSO attackData)
        {
            AttackExecutionRequest request = CreateDefaultRequest(attackData);
            return ExecuteAttack(in request);
        }

        /// <summary>
        /// 지정 공격 실행 요청 기반 공격 실행
        /// </summary>
        /// <param name="request">공격 실행 요청 정보</param>
        /// <returns>공격 실행 결과</returns>
        public AttackExecutionResult ExecuteAttack(in AttackExecutionRequest request)
        {
            ResetLastAttackResult();

            if (_hitCaster == null)
            {
                LogMissingHitCaster();
                return CreateLastAttackResult();
            }

            if (request.AttackData == null)
            {
                LogMissingAttackData();
                return CreateLastAttackResult();
            }

            HitCastRequest hitCastRequest = new HitCastRequest(
                request.Owner,
                request.Origin,
                request.Direction,
                request.AttackData.Range,
                request.AttackData.Radius,
                request.TargetLayerMask
            );

            int hitCount = _hitCaster.Cast(in hitCastRequest, HIT_RESULTS);

            for (int i = 0; i < hitCount; i++)
            {
                HitCastResult hitCastResult = HIT_RESULTS[i];
                IHitable hitable = hitCastResult.Hitable;

                if (hitable == null)
                {
                    continue;
                }

                HitContext hitContext = CreateHitContext(in request, in hitCastResult);
                HitResult hitResult = hitable.ReceiveHit(in hitContext);
                CollectHitResult(hitResult);
            }

            return CreateLastAttackResult();
        }

        /// <summary>
        /// 현재 기본 공격 데이터 변경
        /// </summary>
        /// <param name="attackData">기본 공격 데이터</param>
        public void SetAttackData(AttackDataSO attackData)
        {
            this.attackData = attackData;
        }

        /// <summary>
        /// 공격 판정 기준 Transform 변경
        /// </summary>
        /// <param name="attackOrigin">공격 기준 Transform</param>
        public void SetAttackOrigin(Transform attackOrigin)
        {
            this.attackOrigin = attackOrigin;
        }

        private AttackExecutionRequest CreateDefaultRequest(AttackDataSO requestAttackData)
        {
            Transform originTransform = attackOrigin != null ? attackOrigin : transform;

            return new AttackExecutionRequest(
                gameObject,
                gameObject,
                requestAttackData,
                originTransform.position,
                originTransform.forward,
                targetLayerMask
            );
        }

        private HitContext CreateHitContext(in AttackExecutionRequest request, in HitCastResult hitCastResult)
        {
            return new HitContext(
                request.Attacker,
                request.Owner,
                request.AttackData.AttackType,
                hitCastResult.HitPoint,
                hitCastResult.HitDirection,
                request.AttackData.KnockbackPower,
                request.AttackData.AttackId
            );
        }

        private void CollectHitResult(HitResult hitResult)
        {
            LastHitResult = hitResult;

            if (hitResult.IsHit == true)
            {
                LastHitSuccessCount++;
                HasAnyHit = true;
            }

            if (hitResult.IsKilled == true)
            {
                LastKilledCount++;
            }
        }

        private void ResetLastAttackResult()
        {
            LastHitSuccessCount = 0;
            LastKilledCount = 0;
            LastHitResult = new HitResult(false, false, HitResultType.None);
            HasAnyHit = false;
        }

        private AttackExecutionResult CreateLastAttackResult()
        {
            return new AttackExecutionResult(
                LastHitSuccessCount,
                LastKilledCount,
                LastHitResult,
                HasAnyHit
            );
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void LogInvalidHitCaster()
        {
            Debug.LogError($"{nameof(hitCasterBehaviour)} must implement {nameof(IHitCaster)}.", this);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void LogMissingHitCaster()
        {
            Debug.LogError($"{nameof(IHitCaster)} is missing. Attack execution stopped.", this);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void LogMissingAttackData()
        {
            Debug.LogError($"{nameof(attackData)} is missing. Attack execution stopped.", this);
        }
    }
}
