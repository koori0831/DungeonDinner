using UnityEngine;
using Work.Combat.Code.Core;
using Work.Entities.Code;

namespace Work.Enemy.Code
{
    /// <summary>
    /// 적의 사망 상태 전환과 사망 연출 처리 컴포넌트.
    /// </summary>
    public sealed class EnemyDeathHandler : MonoBehaviour, IEntityModule
    {
        [SerializeField]
        private EnemyBase enemy;

        [SerializeField]
        private EnemyStateController stateController;

        [SerializeField]
        private Collider[] collidersToDisable;

        [SerializeField]
        private MonoBehaviour[] behavioursToDisable;

        [SerializeField]
        private Animator animator;

        [SerializeField]
        private string deathTriggerName = "Death";

        /// <summary>
        /// 사망 처리 완료 여부.
        /// </summary>
        public bool IsDead { get; private set; }

        private void Reset()
        {
            enemy = GetComponent<EnemyBase>();
            stateController = GetComponent<EnemyStateController>();
            collidersToDisable = GetComponentsInChildren<Collider>();
            animator = GetComponentInChildren<Animator>();
        }

        private void Awake()
        {
            ResolveSceneReferences(null);
        }

        /// <summary>
        /// 모듈 소유자 초기화.
        /// </summary>
        /// <param name="entity">모듈 소유 엔티티.</param>
        public void Initialize(Entity entity)
        {
            ResolveSceneReferences(entity);
        }

        private void ResolveSceneReferences(Entity entity)
        {
            if (enemy == null)
            {
                enemy = ResolveEnemy(entity);
            }

            if (stateController == null)
            {
                stateController = ResolveStateController(entity);
            }
        }

        private EnemyBase ResolveEnemy(Entity entity)
        {
            EnemyBase enemyBase = entity as EnemyBase;

            if (enemyBase != null)
            {
                return enemyBase;
            }

            if (entity != null)
            {
                enemyBase = entity.GetComponent<EnemyBase>();

                if (enemyBase != null)
                {
                    return enemyBase;
                }
            }

            return GetComponentInParent<EnemyBase>();
        }

        private EnemyStateController ResolveStateController(Entity entity)
        {
            EnemyStateController resolvedStateController = null;

            if (entity != null)
            {
                resolvedStateController = entity.GetComponent<EnemyStateController>();

                if (resolvedStateController != null)
                {
                    return resolvedStateController;
                }
            }

            return GetComponentInParent<EnemyStateController>();
        }

        /// <summary>
        /// 피격 정보에 따른 사망 처리 실행.
        /// </summary>
        /// <param name="hitContext">이번 피격 정보.</param>
        public void Die(in HitContext hitContext)
        {
            if (IsDead == true)
            {
                return;
            }

            IsDead = true;

            stateController?.SetState(EnemyState.Dead);
            enemy?.Die();
            DisableColliders();
            DisableBehaviours();
            PlayDeathAnimation();
        }

        private void DisableColliders()
        {
            if (collidersToDisable == null)
            {
                return;
            }

            for (int i = 0; i < collidersToDisable.Length; i++)
            {
                Collider targetCollider = collidersToDisable[i];

                if (targetCollider == null)
                {
                    continue;
                }

                targetCollider.enabled = false;
            }
        }

        private void DisableBehaviours()
        {
            if (behavioursToDisable == null)
            {
                return;
            }

            for (int i = 0; i < behavioursToDisable.Length; i++)
            {
                MonoBehaviour targetBehaviour = behavioursToDisable[i];

                if (targetBehaviour == null || targetBehaviour == this)
                {
                    continue;
                }

                targetBehaviour.enabled = false;
            }
        }

        private void PlayDeathAnimation()
        {
            if (animator == null || string.IsNullOrEmpty(deathTriggerName) == true)
            {
                return;
            }

            animator.SetTrigger(deathTriggerName);
        }
    }
}
