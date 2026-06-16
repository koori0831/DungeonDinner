using UnityEngine;
using Work.Combat.Code.Core;
using Work.Entities.Code;

namespace Work.Enemy.Code
{
    /// <summary>
    /// 적 피격 시 발생하는 넉백과 연출 처리 컴포넌트.
    /// </summary>
    public sealed class EnemyHitReaction : MonoBehaviour, IEntityModule
    {
        private const float MIN_DIRECTION_SQR_MAGNITUDE = 0.0001f;

        [SerializeField]
        private EnemyMovementModule movementModule;

        [SerializeField]
        private ParticleSystem hitEffect;

        [SerializeField]
        private AudioSource hitAudioSource;

        private void Reset()
        {
            movementModule = GetComponent<EnemyMovementModule>();
            hitAudioSource = GetComponent<AudioSource>();
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
            if (movementModule == null)
            {
                ResolveMovementModule(entity);
            }
        }

        private void ResolveMovementModule(Entity entity)
        {
            if (entity != null && entity.TryGetModule<EnemyMovementModule>(out movementModule, true) == true)
            {
                return;
            }

            movementModule = GetComponentInParent<EnemyMovementModule>();
        }

        /// <summary>
        /// 피격 반응 연출 실행.
        /// </summary>
        /// <param name="hitContext">이번 피격 정보.</param>
        public void PlayHitReaction(in HitContext hitContext)
        {
            ApplyKnockback(in hitContext);
            PlayHitEffect(in hitContext);
            PlayHitSound();
        }

        private void ApplyKnockback(in HitContext hitContext)
        {
            if (movementModule == null)
            {
                return;
            }

            if (hitContext.KnockbackPower <= 0f)
            {
                return;
            }

            if (hitContext.HitDirection.sqrMagnitude <= MIN_DIRECTION_SQR_MAGNITUDE)
            {
                return;
            }

            movementModule.ApplyImpulse(hitContext.HitDirection, hitContext.KnockbackPower);
        }

        private void PlayHitEffect(in HitContext hitContext)
        {
            if (hitEffect == null)
            {
                return;
            }

            hitEffect.transform.position = hitContext.HitPoint;
            hitEffect.Play();
        }

        private void PlayHitSound()
        {
            if (hitAudioSource == null)
            {
                return;
            }

            hitAudioSource.Play();
        }
    }
}
