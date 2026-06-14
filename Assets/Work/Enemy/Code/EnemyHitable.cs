using UnityEngine;
using Work.Combat.Code.Core;
using Work.Enemy.Code.Drops;
using Work.Entities.Code;

namespace Work.Enemy.Code
{
    /// <summary>
    /// 적의 피격 처리 담당 컴포넌트.
    /// </summary>
    public sealed class EnemyHitable : MonoBehaviour, IHitable
    {
        [SerializeField]
        private bool isHitable = true;

        [SerializeField]
        private EnemyHitReaction hitReaction;

        [SerializeField]
        private EnemyDeathHandler deathHandler;

        [SerializeField]
        private EntityHealthModule healthModule;

        [SerializeField]
        private EnemyDropResolver dropResolver;

        private void Awake()
        {
            ResolveSceneReferences();
        }

        /// <summary>
        /// 피격 1회 처리 후 드랍과 체력 기반 사망 처리
        /// </summary>
        /// <param name="hitContext">이번 피격 정보.</param>
        /// <returns>피격 처리 결과.</returns>
        public HitResult ReceiveHit(in HitContext hitContext)
        {
            ResolveSceneReferences();

            if (IsAlreadyDead() == true)
            {
                return new HitResult(false, false, HitResultType.AlreadyDead);
            }

            if (isHitable == false)
            {
                return new HitResult(false, false, HitResultType.Ignored);
            }

            if (healthModule == null)
            {
                LogMissingHealthModule();
                return new HitResult(false, false, HitResultType.InvalidConfiguration);
            }

            if (healthModule.TryApplyHit(out bool isKilled) == false)
            {
                return new HitResult(false, false, HitResultType.AlreadyDead);
            }

            if (hitReaction != null)
            {
                hitReaction.PlayHitReaction(in hitContext);
            }

            if (dropResolver != null)
            {
                dropResolver.ResolveDrops(in hitContext);
            }
            else
            {
                LogMissingDropResolver();
            }

            if (isKilled == true)
            {
                if (deathHandler == null)
                {
                    LogMissingDeathHandler();
                    return new HitResult(true, false, HitResultType.InvalidConfiguration);
                }

                deathHandler.Die(in hitContext);
                return new HitResult(true, true, HitResultType.Killed);
            }

            return new HitResult(true, false, HitResultType.HitButNotKilled);
        }

        private void ResolveSceneReferences()
        {
            if (healthModule == null)
            {
                healthModule = GetComponentInParent<EntityHealthModule>();
            }

            if (dropResolver == null)
            {
                dropResolver = GetComponent<EnemyDropResolver>();
            }
        }

        private bool IsAlreadyDead()
        {
            if (healthModule != null && healthModule.IsDead == true)
            {
                return true;
            }

            return deathHandler != null && deathHandler.IsDead == true;
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void LogMissingHealthModule()
        {
            Debug.LogError($"{nameof(EntityHealthModule)} is missing. Enemy hit stopped.", this);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void LogMissingDropResolver()
        {
            Debug.LogWarning($"{nameof(EnemyDropResolver)} is missing. Drop log skipped.", this);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void LogMissingDeathHandler()
        {
            Debug.LogError($"{nameof(EnemyDeathHandler)} is missing while health is depleted.", this);
        }
    }
}
