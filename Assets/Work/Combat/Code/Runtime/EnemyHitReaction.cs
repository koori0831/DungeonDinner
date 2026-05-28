using UnityEngine;
using Work.Combat.Code.Core;

namespace Work.Combat.Code.Runtime
{
    /// <summary>
    /// 적 피격 시 발생하는 넉백과 연출 처리 컴포넌트
    /// </summary>
    public sealed class EnemyHitReaction : MonoBehaviour
    {
        private const float MIN_DIRECTION_SQR_MAGNITUDE = 0.0001f;

        [SerializeField]
        private Rigidbody targetRigidbody;

        [SerializeField]
        private Animator animator;

        [SerializeField]
        private string hitTriggerName = "Hit";

        [SerializeField]
        private ParticleSystem hitEffect;

        [SerializeField]
        private AudioSource hitAudioSource;

        private void Reset()
        {
            targetRigidbody = GetComponent<Rigidbody>();
            animator = GetComponentInChildren<Animator>();
            hitAudioSource = GetComponent<AudioSource>();
        }

        /// <summary>
        /// 피격 반응 연출 실행
        /// </summary>
        /// <param name="hitContext">이번 피격 정보</param>
        public void PlayHitReaction(in HitContext hitContext)
        {
            ApplyKnockback(in hitContext);
            PlayHitAnimation();
            PlayHitEffect(in hitContext);
            PlayHitSound();
        }

        private void ApplyKnockback(in HitContext hitContext)
        {
            if (targetRigidbody == null)
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

            Vector3 force = hitContext.HitDirection.normalized * hitContext.KnockbackPower;
            targetRigidbody.AddForce(force, ForceMode.Impulse);
        }

        private void PlayHitAnimation()
        {
            if (animator == null || string.IsNullOrEmpty(hitTriggerName) == true)
            {
                return;
            }

            animator.SetTrigger(hitTriggerName);
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
