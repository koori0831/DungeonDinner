using System.Text;
using UnityEngine;
using Work.Combat.Code.Conditions;
using Work.Combat.Code.Core;
using Work.Entities.Code;
using Work.Items.Code;

namespace Work.Enemy.Code.Drops
{
    /// <summary>
    /// 적 피격 정보 기반 드랍 규칙 평가와 로그 출력 담당 컴포넌트
    /// </summary>
    public sealed class EnemyDropResolver : MonoBehaviour, IEntityModule
    {
        private const int MAX_DROP_RESULT_COUNT = 8;

        [SerializeField]
        private EnemyStateController stateController;

        [SerializeField]
        private EnemyDropWorldSpawner worldDropSpawner;

        [SerializeField]
        private EnemyDropRule[] dropRules;

        [SerializeField]
        private bool logDrops = true;

        [Header("Last Drop")]
        [SerializeField]
        private AttackType lastAttackType;

        [SerializeField]
        private int lastDropCount;

        [SerializeField]
        private string lastDropSummary;

        private readonly EnemyDropResult[] DROP_RESULTS = new EnemyDropResult[MAX_DROP_RESULT_COUNT];

        /// <summary>
        /// 마지막 드랍 계산 공격 타입
        /// </summary>
        public AttackType LastAttackType => lastAttackType;

        /// <summary>
        /// 마지막 드랍 결과 수
        /// </summary>
        public int LastDropCount => lastDropCount;

        /// <summary>
        /// 마지막 드랍 로그 요약
        /// </summary>
        public string LastDropSummary => lastDropSummary;

        /// <summary>
        /// 마지막 드랍 결과 조회
        /// </summary>
        /// <param name="index">조회할 드랍 결과 인덱스</param>
        /// <returns>드랍 결과</returns>
        public EnemyDropResult GetLastDropResult(int index)
        {
            if (index < 0 || index >= lastDropCount)
            {
                return default;
            }

            return DROP_RESULTS[index];
        }

        /// <summary>
        /// 마지막 드랍 결과를 외부 버퍼에 복사
        /// </summary>
        /// <param name="results">복사 대상 결과 버퍼</param>
        /// <param name="startIndex">복사를 시작할 인덱스</param>
        /// <returns>복사된 드랍 결과 수</returns>
        public int CopyLastDropResults(EnemyDropResult[] results, int startIndex)
        {
            if (results == null || startIndex < 0 || startIndex >= results.Length)
            {
                return 0;
            }

            int copyCount = Mathf.Min(lastDropCount, results.Length - startIndex);

            for (int i = 0; i < copyCount; i++)
            {
                results[startIndex + i] = DROP_RESULTS[i];
            }

            return copyCount;
        }

        private void Awake()
        {
            ResolveSceneReferences(null);
        }

        /// <summary>
        /// 모듈 소유자 초기화
        /// </summary>
        /// <param name="entity">모듈 소유 엔티티</param>
        public void Initialize(Entity entity)
        {
            ResolveSceneReferences(entity);
        }

        /// <summary>
        /// 피격 정보 기반 드랍 처리
        /// </summary>
        /// <param name="hitContext">이번 피격 정보</param>
        /// <returns>드랍 결과 수</returns>
        public int ResolveDrops(in HitContext hitContext)
        {
            ResetLastDrop(hitContext.AttackType);

            if (IsSingleAttackType(hitContext.AttackType) == false)
            {
                LogInvalidAttackType(hitContext.AttackType);
                return 0;
            }

            if (dropRules == null || dropRules.Length == 0)
            {
                UpdateDropSummary();
                LogDropResult();
                return 0;
            }

            CombatConditionContext conditionContext = new CombatConditionContext(gameObject, stateController);

            for (int i = 0; i < dropRules.Length; i++)
            {
                EnemyDropRule dropRule = dropRules[i];

                if (dropRule == null)
                {
                    continue;
                }

                if (dropRule.CanDrop(in hitContext, in conditionContext) == false)
                {
                    continue;
                }

                EnemyDropTableSO dropTable = dropRule.DropTable;

                if (dropTable == null)
                {
                    continue;
                }

                lastDropCount += dropTable.RollDrops(DROP_RESULTS, lastDropCount);
                break;
            }

            UpdateDropSummary();

            if (lastDropCount > 0)
            {
                SpawnLastDrops(in hitContext);
            }

            LogDropResult();
            return lastDropCount;
        }

        private void ResolveSceneReferences(Entity entity)
        {
            ResolveWorldDropSpawner();

            if (stateController != null)
            {
                return;
            }

            if (entity != null)
            {
                stateController = entity.GetComponent<EnemyStateController>();

                if (stateController != null)
                {
                    return;
                }
            }

            stateController = GetComponentInParent<EnemyStateController>();
        }

        private void ResolveWorldDropSpawner()
        {
            if (worldDropSpawner != null)
            {
                return;
            }

            worldDropSpawner = GetComponent<EnemyDropWorldSpawner>();
        }

        private void SpawnLastDrops(in HitContext hitContext)
        {
            ResolveWorldDropSpawner();

            if (worldDropSpawner == null)
            {
                return;
            }

            worldDropSpawner.SpawnDrops(DROP_RESULTS, lastDropCount, in hitContext);
        }

        private void ResetLastDrop(AttackType attackType)
        {
            lastAttackType = attackType;
            lastDropCount = 0;
            lastDropSummary = string.Empty;

            for (int i = 0; i < DROP_RESULTS.Length; i++)
            {
                DROP_RESULTS[i] = default;
            }
        }

        private void UpdateDropSummary()
        {
            if (lastDropCount <= 0)
            {
                lastDropSummary = $"No drop. attackType={lastAttackType}";
                return;
            }

            StringBuilder builder = new StringBuilder();
            builder.Append("attackType=");
            builder.Append(lastAttackType);
            builder.Append(", drops=");

            for (int i = 0; i < lastDropCount; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }

                EnemyDropResult result = DROP_RESULTS[i];
                ItemDataSO item = result.Item;
                builder.Append(item != null ? item.DisplayName : "Missing Item");
                builder.Append(" x");
                builder.Append(result.Amount);
            }

            lastDropSummary = builder.ToString();
        }

        private static bool IsSingleAttackType(AttackType attackType)
        {
            int attackTypeValue = (int)attackType;
            return attackTypeValue != 0 && (attackTypeValue & (attackTypeValue - 1)) == 0;
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void LogDropResult()
        {
            if (logDrops == false)
            {
                return;
            }

            Debug.Log($"{nameof(EnemyDropResolver)} resolved drop. {lastDropSummary}", this);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void LogInvalidAttackType(AttackType attackType)
        {
            Debug.LogWarning($"{nameof(EnemyDropResolver)} requires a single non-empty {nameof(AttackType)}. attackType={attackType}", this);
        }
    }
}
