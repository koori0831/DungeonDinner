using UnityEngine;
using Work.Entities.Code;
using Work.FSM.Code;

namespace Work.Enemy.Code
{
    /// <summary>
    /// 적 엔티티 초기화와 상태용 컨텍스트 facade 담당 클래스.
    /// </summary>
    public class EnemyBase : Entity
    {
        [Header("Initialize")]
        [SerializeField]
        private bool initializeOnAwake = true;

        [Header("References")]
        [SerializeField]
        private EnemyStateController stateController;

        private EnemyMovementModule _movementModule;
        private EntityStateModule _stateModule;
        private EnemyTerritoryModule _territoryModule;
        private EnemyTargetingModule _targetingModule;
        private EnemyCombatModule _combatModule;
        private bool _isInitialized;
        private bool _isDead;

        /// <summary>
        /// 현재 추적 대상.
        /// </summary>
        public Transform Target => GetTargetingModule()?.Target;

        /// <summary>
        /// 활동 범위 중심 위치.
        /// </summary>
        public Vector3 ActivityCenter => GetTerritoryModule() != null ? GetTerritoryModule().ActivityCenter : transform.position;

        /// <summary>
        /// 활동 반경.
        /// </summary>
        public float ActivityRadius => GetTerritoryModule() != null ? GetTerritoryModule().ActivityRadius : 0f;

        /// <summary>
        /// 감지 반경.
        /// </summary>
        public float DetectionRadius => GetTargetingModule() != null ? GetTargetingModule().DetectionRadius : 0f;

        /// <summary>
        /// 공격 거리.
        /// </summary>
        public float AttackDistance => GetCombatModule() != null ? GetCombatModule().AttackDistance : 0f;

        /// <summary>
        /// 공격 상태 진입 허용 각도.
        /// </summary>
        public float AttackEnterAngle => GetCombatModule() != null ? GetCombatModule().AttackEnterAngle : 0f;

        /// <summary>
        /// 순찰 대기 시간.
        /// </summary>
        public float PatrolWaitTime => GetTerritoryModule() != null ? GetTerritoryModule().PatrolWaitTime : 0f;

        /// <summary>
        /// 순찰 지점 주변 체류 시간.
        /// </summary>
        public float PatrolPointStayTime => GetTerritoryModule() != null ? GetTerritoryModule().PatrolPointStayTime : 0f;

        /// <summary>
        /// 순찰 지점 주변 다음 이동점 선택 간격.
        /// </summary>
        public float PatrolPointMoveInterval => GetTerritoryModule() != null ? GetTerritoryModule().PatrolPointMoveInterval : 0f;

        /// <summary>
        /// 공격 쿨타임.
        /// </summary>
        public float AttackCooldown => GetCombatModule() != null ? GetCombatModule().AttackCooldown : 0f;

        /// <summary>
        /// 활동 범위 이탈 후 복귀 전환까지 대기 시간.
        /// </summary>
        public float ChaseReturnDelay => GetTerritoryModule() != null ? GetTerritoryModule().ChaseReturnDelay : 0f;

        /// <summary>
        /// 사망 여부.
        /// </summary>
        public bool IsDead => _isDead;

        /// <summary>
        /// 공격 가능 여부.
        /// </summary>
        public bool CanExecuteAttack => GetCombatModule() != null && GetCombatModule().CanExecuteAttack == true;

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

            EnemyTargetingModule targetingModule = GetTargetingModule();

            if (targetingModule == null)
            {
                return false;
            }

            Transform previousTarget = targetingModule.Target;
            bool isAcquired = targetingModule.TryAcquireTarget();
            Transform currentTarget = targetingModule.Target;

            InvokeTargetChangeEvents(previousTarget, currentTarget);
            return isAcquired;
        }

        /// <summary>
        /// 현재 대상 제거.
        /// </summary>
        public virtual void ClearTarget()
        {
            EnemyTargetingModule targetingModule = GetTargetingModule();

            if (targetingModule == null || targetingModule.Target == null)
            {
                return;
            }

            Transform previousTarget = targetingModule.Target;
            targetingModule.ClearTarget();
            InvokeTargetChangeEvents(previousTarget, null);
        }

        /// <summary>
        /// 현재 대상의 감지 범위 포함 여부 반환.
        /// </summary>
        /// <returns>감지 범위 포함 여부.</returns>
        public virtual bool IsTargetInDetectionRange()
        {
            EnemyTargetingModule targetingModule = GetTargetingModule();
            return targetingModule != null && targetingModule.IsTargetInDetectionRange() == true;
        }

        /// <summary>
        /// 현재 대상의 활동 범위 포함 여부 반환.
        /// </summary>
        /// <returns>활동 범위 포함 여부.</returns>
        public virtual bool IsTargetInActivityRange()
        {
            EnemyTargetingModule targetingModule = GetTargetingModule();
            return targetingModule != null && targetingModule.IsTargetInActivityRange() == true;
        }

        /// <summary>
        /// 현재 대상의 공격 범위 포함 여부 반환.
        /// </summary>
        /// <returns>공격 범위 포함 여부.</returns>
        public virtual bool IsTargetInAttackRange()
        {
            EnemyCombatModule combatModule = GetCombatModule();
            return combatModule != null && combatModule.IsTargetInAttackRange(Target, GetTerritoryModule()) == true;
        }

        /// <summary>
        /// 복귀 완료 영역 안쪽 포함 여부 반환.
        /// </summary>
        /// <returns>복귀 완료 영역 포함 여부.</returns>
        public virtual bool IsInsideReturnArea()
        {
            EnemyTerritoryModule territoryModule = GetTerritoryModule();
            return territoryModule == null || territoryModule.IsInsideReturnArea() == true;
        }

        /// <summary>
        /// 현재 타겟을 지정 각도 이내로 바라보는지 반환.
        /// </summary>
        /// <param name="maxAngle">허용 각도.</param>
        /// <returns>타겟 방향 정렬 여부.</returns>
        public virtual bool IsFacingTarget(float maxAngle)
        {
            EnemyCombatModule combatModule = GetCombatModule();
            return combatModule != null && combatModule.IsFacingTarget(Target, maxAngle) == true;
        }

        /// <summary>
        /// 활동 범위 내 다음 순찰 위치 반환.
        /// </summary>
        /// <returns>순찰 위치.</returns>
        public virtual Vector3 GetNextPatrolPoint()
        {
            EnemyTerritoryModule territoryModule = GetTerritoryModule();
            return territoryModule != null ? territoryModule.GetNextPatrolPoint() : transform.position;
        }

        /// <summary>
        /// 순찰 위치 주변의 다음 세부 이동 위치 반환.
        /// </summary>
        /// <param name="patrolPoint">기준 순찰 위치.</param>
        /// <returns>세부 이동 위치.</returns>
        public virtual Vector3 GetNextPatrolMovePoint(Vector3 patrolPoint)
        {
            EnemyTerritoryModule territoryModule = GetTerritoryModule();
            return territoryModule != null ? territoryModule.GetNextPatrolMovePoint(patrolPoint) : patrolPoint;
        }

        /// <summary>
        /// 복귀 목표 위치 반환.
        /// </summary>
        /// <returns>복귀 목표 위치.</returns>
        public virtual Vector3 GetReturnPoint()
        {
            EnemyTerritoryModule territoryModule = GetTerritoryModule();
            return territoryModule != null ? territoryModule.GetReturnPoint() : transform.position;
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

            Vector3 currentPosition = transform.position;
            currentPosition.y = 0f;
            targetPosition.y = 0f;
            float sqrDistance = (targetPosition - currentPosition).sqrMagnitude;
            return sqrDistance <= 0.01f;
        }

        /// <summary>
        /// 현재 대상을 바라보도록 회전.
        /// </summary>
        public virtual void FaceTarget()
        {
            Transform target = Target;

            if (target == null)
            {
                return;
            }

            EnemyMovementModule movementModule = GetMovementModule();
            movementModule?.FaceTowards(target.position);
        }

        /// <summary>
        /// 현재 공격 실행.
        /// </summary>
        public virtual void ExecuteAttack()
        {
            EnemyCombatModule combatModule = GetCombatModule();

            if (_isDead == true || combatModule == null || combatModule.CanExecuteAttack == false)
            {
                return;
            }

            OnBeforeAttack();

            if (combatModule.ExecuteAttack() == true)
            {
                OnAfterAttack();
            }
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

        private void ResolveSceneReferences()
        {
            if (stateController == null)
            {
                stateController = GetComponent<EnemyStateController>();
            }
        }

        private void ResolveModules()
        {
            TryGetModule<EnemyMovementModule>(out _movementModule, true);
            TryGetModule<EntityStateModule>(out _stateModule, true);
            TryGetModule<EnemyTerritoryModule>(out _territoryModule, true);
            TryGetModule<EnemyTargetingModule>(out _targetingModule, true);
            TryGetModule<EnemyCombatModule>(out _combatModule, true);
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

        private EnemyTerritoryModule GetTerritoryModule()
        {
            if (_territoryModule == null)
            {
                TryGetModule<EnemyTerritoryModule>(out _territoryModule, true);
            }

            return _territoryModule;
        }

        private EnemyTargetingModule GetTargetingModule()
        {
            if (_targetingModule == null)
            {
                TryGetModule<EnemyTargetingModule>(out _targetingModule, true);
            }

            return _targetingModule;
        }

        private EnemyCombatModule GetCombatModule()
        {
            if (_combatModule == null)
            {
                TryGetModule<EnemyCombatModule>(out _combatModule, true);
            }

            return _combatModule;
        }

        private void InvokeTargetChangeEvents(Transform previousTarget, Transform currentTarget)
        {
            if (previousTarget == currentTarget)
            {
                return;
            }

            if (previousTarget != null)
            {
                OnTargetLost();
            }

            if (currentTarget != null)
            {
                OnTargetAcquired(currentTarget);
            }
        }
    }
}
