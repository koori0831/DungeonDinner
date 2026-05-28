using UnityEngine;
using Work.Combat.Code.Core;

namespace Work.Combat.Code.Runtime
{
    /// <summary>
    /// 적의 피격 처리 담당 컴포넌트
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
        private EnemyKillConditionResolver killConditionResolver;

        /// <summary>
        /// 피격 반응 처리 후 사망 조건 검사
        /// </summary>
        /// <param name="hitContext">이번 피격 정보</param>
        /// <returns>피격 처리 결과</returns>
        public HitResult ReceiveHit(in HitContext hitContext)
        {
            if (deathHandler != null && deathHandler.IsDead == true)
            {
                return new HitResult(false, false, HitResultType.AlreadyDead);
            }

            if (isHitable == false)
            {
                return new HitResult(false, false, HitResultType.Ignored);
            }

            if (hitReaction != null)
            {
                hitReaction.PlayHitReaction(in hitContext);
            }

            if (killConditionResolver == null)
            {
                LogMissingKillConditionResolver();
                return new HitResult(true, false, HitResultType.InvalidConfiguration);
            }

            bool canKill = killConditionResolver.CanKill(in hitContext);

            if (canKill == true)
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

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void LogMissingKillConditionResolver()
        {
            Debug.LogError($"{nameof(EnemyKillConditionResolver)} is missing.", this);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void LogMissingDeathHandler()
        {
            Debug.LogError($"{nameof(EnemyDeathHandler)} is missing while kill condition is satisfied.", this);
        }
    }
}
