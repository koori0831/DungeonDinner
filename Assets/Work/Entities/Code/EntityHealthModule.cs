using UnityEngine;

namespace Work.Entities.Code
{
    /// <summary>
    /// 엔티티의 정수 체력과 피격 횟수 기반 사망 상태 관리 모듈
    /// </summary>
    public sealed class EntityHealthModule : MonoBehaviour, IEntityModule
    {
        private const int MIN_HEALTH = 1;

        [SerializeField]
        [Min(MIN_HEALTH)]
        private int maxHealth = 3;

        private int _currentHealth;
        private bool _isInitialized;

        /// <summary>
        /// 최대 체력
        /// </summary>
        public int MaxHealth => maxHealth;

        /// <summary>
        /// 현재 체력
        /// </summary>
        public int CurrentHealth => _currentHealth;

        /// <summary>
        /// 사망 여부
        /// </summary>
        public bool IsDead => _currentHealth <= 0;

        private void Awake()
        {
            EnsureInitialized();
        }

        /// <summary>
        /// 모듈 소유자 초기화
        /// </summary>
        /// <param name="entity">모듈 소유 엔티티</param>
        public void Initialize(Entity entity)
        {
            EnsureInitialized();
        }

        /// <summary>
        /// 체력을 최대 체력으로 초기화
        /// </summary>
        public void ResetHealth()
        {
            _currentHealth = Mathf.Max(MIN_HEALTH, maxHealth);
            _isInitialized = true;
        }

        /// <summary>
        /// 유효 피격 1회를 적용하고 사망 여부 반환
        /// </summary>
        /// <param name="isKilled">이번 피격으로 인한 사망 여부</param>
        /// <returns>피격 적용 여부</returns>
        public bool TryApplyHit(out bool isKilled)
        {
            EnsureInitialized();

            if (IsDead == true)
            {
                isKilled = false;
                return false;
            }

            _currentHealth = Mathf.Max(0, _currentHealth - 1);
            isKilled = IsDead;
            return true;
        }

        private void OnValidate()
        {
            maxHealth = Mathf.Max(MIN_HEALTH, maxHealth);
        }

        private void EnsureInitialized()
        {
            if (_isInitialized == true)
            {
                return;
            }

            ResetHealth();
        }
    }
}
