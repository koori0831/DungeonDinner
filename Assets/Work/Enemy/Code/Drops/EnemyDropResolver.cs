using System.Text;
using UnityEngine;
using Work.Combat.Code.Conditions;
using Work.Combat.Code.Core;
using Work.Entities.Code;

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

            // TODO: 아이템 시스템 구현 후 로그 대신 월드 드랍 또는 인벤토리 지급으로 연결
            LogDropResult();
            return lastDropCount;
        }

        private void ResolveSceneReferences(Entity entity)
        {
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
                EnemyDropItemSO item = result.Item;
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
