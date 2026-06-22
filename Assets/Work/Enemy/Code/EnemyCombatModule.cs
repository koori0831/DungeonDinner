using UnityEngine;
using Work.Combat.Code.Runtime;
using Work.Entities.Code;

namespace Work.Enemy.Code
{
    /// <summary>
    /// 적 공격 거리, 방향 정렬, 쿨타임, 공격 실행 담당 모듈.
    /// </summary>
    public sealed class EnemyCombatModule : MonoBehaviour, IEntityModule
    {
        private const float MIN_DIRECTION_SQR_MAGNITUDE = 0.0001f;
        private const float MIN_RANGE = 0f;

        [SerializeField]
        private CombatAttackExecutor attackExecutor;

        [SerializeField]
        private float attackDistance = 1.5f;

        [SerializeField]
        private float attackEnterAngle = 12f;

        [SerializeField]
        private float attackCooldown = 1.25f;

        [SerializeField]
        private float attackWindupTime = 0.25f;

        [SerializeField]
        private float attackRecoveryTime = 0.35f;

        private Entity _ownerEntity;
        private float _nextAttackTime;

        /// <summary>
        /// 공격 거리.
        /// </summary>
        public float AttackDistance => attackDistance;

        /// <summary>
        /// 공격 상태 진입 허용 각도.
        /// </summary>
        public float AttackEnterAngle => attackEnterAngle;

        /// <summary>
        /// 공격 쿨타임.
        /// </summary>
        public float AttackCooldown => attackCooldown;

        /// <summary>
        /// 공격 판정 전 준비 시간.
        /// </summary>
        public float AttackWindupTime => attackWindupTime;

        /// <summary>
        /// 공격 판정 후 회복 시간.
        /// </summary>
        public float AttackRecoveryTime => attackRecoveryTime;

        /// <summary>
        /// 공격 가능 여부.
        /// </summary>
        public bool CanExecuteAttack => Time.time >= _nextAttackTime;

        /// <summary>
        /// 모듈 소유자 초기화.
        /// </summary>
        /// <param name="entity">모듈 소유 엔티티.</param>
        public void Initialize(Entity entity)
        {
            _ownerEntity = entity;
            ResolveSceneReferences(entity);
        }

        /// <summary>
        /// 현재 대상의 공격 범위 포함 여부 반환.
        /// </summary>
        /// <param name="target">공격 대상.</param>
        /// <param name="territoryModule">활동 영역 모듈.</param>
        /// <returns>공격 범위 포함 여부.</returns>
        public bool IsTargetInAttackRange(Transform target, EnemyTerritoryModule territoryModule)
        {
            if (target == null)
            {
                return false;
            }

            float sqrDistance = GetHorizontalSqrDistance(transform.position, target.position);
            bool isInActivityRange = territoryModule == null || territoryModule.IsPositionInActivityRange(target.position) == true;
            return sqrDistance <= attackDistance * attackDistance && isInActivityRange == true;
        }

        /// <summary>
        /// 현재 타겟을 지정 각도 이내로 바라보는지 반환.
        /// </summary>
        /// <param name="target">확인할 타겟.</param>
        /// <param name="maxAngle">허용 각도.</param>
        /// <returns>타겟 방향 정렬 여부.</returns>
        public bool IsFacingTarget(Transform target, float maxAngle)
        {
            if (target == null)
            {
                return false;
            }

            Vector3 targetDirection = target.position - transform.position;
            targetDirection.y = 0f;
            float targetSqrMagnitude = targetDirection.sqrMagnitude;

            if (targetSqrMagnitude <= MIN_DIRECTION_SQR_MAGNITUDE)
            {
                return true;
            }

            Vector3 forward = transform.forward;
            forward.y = 0f;
            float forwardSqrMagnitude = forward.sqrMagnitude;

            if (forwardSqrMagnitude <= MIN_DIRECTION_SQR_MAGNITUDE)
            {
                return true;
            }

            if (maxAngle < 0f)
            {
                return false;
            }

            if (maxAngle >= 180f)
            {
                return true;
            }

            float dot = Vector3.Dot(forward, targetDirection);
            float magnitude = Mathf.Sqrt(forwardSqrMagnitude * targetSqrMagnitude);
            float angleThreshold = Mathf.Cos(maxAngle * Mathf.Deg2Rad);
            return dot >= magnitude * angleThreshold;
        }

        /// <summary>
        /// 현재 공격 실행.
        /// </summary>
        /// <returns>공격 실행 여부.</returns>
        public bool ExecuteAttack()
        {
            if (CanExecuteAttack == false)
            {
                return false;
            }

            ResolveSceneReferences(_ownerEntity);

            if (attackExecutor == null)
            {
                _nextAttackTime = Time.time + attackCooldown;
                LogMissingAttackExecutor();
                return false;
            }

            attackExecutor.ExecuteAttack();
            _nextAttackTime = Time.time + attackCooldown;
            return true;
        }

        private void OnValidate()
        {
            attackDistance = Mathf.Max(MIN_RANGE, attackDistance);
            attackEnterAngle = Mathf.Max(MIN_RANGE, attackEnterAngle);
            attackCooldown = Mathf.Max(MIN_RANGE, attackCooldown);
            attackWindupTime = Mathf.Max(MIN_RANGE, attackWindupTime);
            attackRecoveryTime = Mathf.Max(MIN_RANGE, attackRecoveryTime);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackDistance);
        }

        private void ResolveSceneReferences(Entity entity)
        {
            if (attackExecutor != null)
            {
                return;
            }

            if (entity != null && entity.TryGetModule<CombatAttackExecutor>(out attackExecutor, true) == true)
            {
                return;
            }

            attackExecutor = GetComponentInParent<CombatAttackExecutor>();
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
