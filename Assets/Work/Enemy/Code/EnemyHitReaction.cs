using UnityEngine;
using Work.Combat.Code.Core;

namespace Work.Enemy.Code
{
    /// <summary>
    /// 적 피격 시 발생하는 넉백과 연출 처리 컴포넌트.
    /// </summary>
    public sealed class EnemyHitReaction : MonoBehaviour
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
            if (movementModule == null)
            {
                movementModule = GetComponent<EnemyMovementModule>();
            }
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
