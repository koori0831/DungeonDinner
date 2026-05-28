using UnityEngine;

namespace Work.Combat
{
    /// <summary>
    /// 적의 사망 상태 전환과 사망 연출 처리 컴포넌트
    /// </summary>
    public sealed class EnemyDeathHandler : MonoBehaviour
    {
        [SerializeField]
        private Collider[] collidersToDisable;

        [SerializeField]
        private MonoBehaviour[] behavioursToDisable;

        [SerializeField]
        private Animator animator;

        [SerializeField]
        private string deathTriggerName = "Death";

        /// <summary>
        /// 사망 처리 완료 여부
        /// </summary>
        public bool IsDead { get; private set; }

        private void Reset()
        {
            collidersToDisable = GetComponentsInChildren<Collider>();
            animator = GetComponentInChildren<Animator>();
        }

        /// <summary>
        /// 피격 정보에 따른 사망 처리 실행
        /// </summary>
        /// <param name="hitContext">이번 피격 정보</param>
        public void Die(in HitContext hitContext)
        {
            if (IsDead == true)
            {
                return;
            }

            IsDead = true;

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
