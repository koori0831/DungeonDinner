using UnityEngine;
using Work.Combat.Code.Runtime;

namespace Work.Combat.Code.Test
{
    /// <summary>
    /// CombatTest 씬에서 피격과 사망 처리를 검증하는 테스트 실행 컴포넌트
    /// </summary>
    public sealed class CombatTestScenarioRunner : MonoBehaviour
    {
        [SerializeField]
        private PlayerAttackExecutor playerAttackExecutor;

        [SerializeField]
        private EnemyDeathHandler slashWeakEnemyDeathHandler;

        [SerializeField]
        private EnemyDeathHandler pierceWeakEnemyDeathHandler;

        [SerializeField]
        private bool executeOnStart = true;

        private void Start()
        {
            if (executeOnStart == false)
            {
                return;
            }

            RunAttackTest();
        }

        /// <summary>
        /// 테스트 공격 실행 후 결과 로그 출력
        /// </summary>
        [ContextMenu("Run Attack Test")]
        public void RunAttackTest()
        {
            if (playerAttackExecutor == null)
            {
                Debug.LogError($"{nameof(playerAttackExecutor)} is missing.", this);
                return;
            }

            playerAttackExecutor.ExecuteAttack();

            Debug.Log(
                $"CombatTest Result - HitSuccessCount: {playerAttackExecutor.LastHitSuccessCount}, " +
                $"KilledCount: {playerAttackExecutor.LastKilledCount}, " +
                $"LastResult: {playerAttackExecutor.LastHitResult.ResultType}, " +
                $"SlashEnemyDead: {GetIsDead(slashWeakEnemyDeathHandler)}, " +
                $"PierceEnemyDead: {GetIsDead(pierceWeakEnemyDeathHandler)}",
                this
            );
        }

        private static bool GetIsDead(EnemyDeathHandler deathHandler)
        {
            if (deathHandler == null)
            {
                return false;
            }

            return deathHandler.IsDead;
        }
    }
}
