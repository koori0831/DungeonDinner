using UnityEngine;
using UnityEngine.Serialization;
using Work.Combat.Code.Core;
using Work.Entities.Code;

namespace Work.Players.Code
{
    /// <summary>
    /// 플레이어의 기본 피격 판정 처리 컴포넌트.
    /// </summary>
    public sealed class PlayerHittable : MonoBehaviour, IHitable
    {
        [SerializeField]
        private bool isHittable = true;

        [SerializeField]
        private EntityHealthModule healthModule;

        /// <summary>
        /// 마지막 피격 정보.
        /// </summary>
        public HitContext LastHitContext { get; private set; }

        /// <summary>
        /// 마지막 피격 처리 결과.
        /// </summary>
        public HitResult LastHitResult { get; private set; }

        private void Awake()
        {
            ResolveSceneReferences();
        }

        /// <summary>
        /// 피격 정보 수신.
        /// </summary>
        /// <param name="hitContext">이번 피격 정보.</param>
        /// <returns>피격 처리 결과.</returns>
        public HitResult ReceiveHit(in HitContext hitContext)
        {
            ResolveSceneReferences();
            LastHitContext = hitContext;

            if (healthModule != null && healthModule.IsDead == true)
            {
                LastHitResult = new HitResult(false, false, HitResultType.AlreadyDead);
                return LastHitResult;
            }

            if (isHittable == false)
            {
                LastHitResult = new HitResult(false, false, HitResultType.Ignored);
                return LastHitResult;
            }

            if (healthModule == null)
            {
                LogMissingHealthModule();
                LastHitResult = new HitResult(false, false, HitResultType.InvalidConfiguration);
                return LastHitResult;
            }

            if (healthModule.TryApplyHit(out bool isKilled) == false)
            {
                LastHitResult = new HitResult(false, false, HitResultType.AlreadyDead);
                return LastHitResult;
            }

            LastHitResult = isKilled == true
                ? new HitResult(true, true, HitResultType.Killed)
                : new HitResult(true, false, HitResultType.HitButNotKilled);

            return LastHitResult;
        }

        private void ResolveSceneReferences()
        {
            if (healthModule != null)
            {
                return;
            }

            healthModule = GetComponentInParent<EntityHealthModule>();
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void LogMissingHealthModule()
        {
            Debug.LogError($"{nameof(EntityHealthModule)} is missing. Player hit stopped.", this);
        }
    }
}
