using UnityEngine;
using Work.NPC.Code.Data;
using Work.NPC.Code.Runtime;
using Work.Cook.Code.Runtime.Core;
using Work.Cook.Code.Runtime.Integration;
using Work.Cook.Code.Runtime.Systems;
using Work.Cook.Code.Runtime.UI;

namespace Work.Cook.Code.Runtime.Systems
{
    public sealed class CookingRewardCalculator : MonoBehaviour
    {
        [Header("Result Rewards")]
        [SerializeField, Min(0)] private int disgustingReward;
        [SerializeField, Min(0)] private int wrongReward;
        [SerializeField, Min(0)] private int similarReward = 8;
        [SerializeField, Min(0)] private int correctReward = 18;
        [SerializeField, Min(0)] private int perfectReward = 30;

        [Header("Dish Quality Bonus")]
        [SerializeField, Min(0)] private int perfectDishBonus = 5;
        [SerializeField, Min(0)] private int alteredDishBonus;
        [SerializeField, Min(0)] private int normalDishBonus;
        [SerializeField, Min(0)] private int qualityScoreBonusPerPoint = 2;
        [SerializeField, Min(0)] private int qualityScorePenaltyPerPoint = 2;

        public int CalculateAmount(NpcDishMatchReport matchReport, DishResult dishResult)
        {
            if (matchReport == null)
                return 0;

            int amount = GetBaseReward(matchReport.Evaluation?.Result ?? NpcConversationResult.Wrong);
            amount += GetDishQualityBonus(dishResult);
            amount += GetQualityScoreRewardDelta(dishResult);
            return Mathf.Max(0, amount);
        }

        private int GetBaseReward(NpcConversationResult result)
        {
            switch (result)
            {
                case NpcConversationResult.Perfect:
                    return perfectReward;
                case NpcConversationResult.Correct:
                    return correctReward;
                case NpcConversationResult.Similar:
                    return similarReward;
                case NpcConversationResult.Disgusting:
                case NpcConversationResult.Wrong:
                default:
                    return wrongReward;
            }
        }

        private int GetDishQualityBonus(DishResult dishResult)
        {
            if (dishResult == null)
                return 0;

            switch (dishResult.Quality)
            {
                case DishQuality.Perfect:
                    return perfectDishBonus;
                case DishQuality.Altered:
                    return alteredDishBonus;
                case DishQuality.Normal:
                    return normalDishBonus;
                case DishQuality.Disgusting:
                default:
                    return 0;
            }
        }

        private int GetQualityScoreRewardDelta(DishResult dishResult)
        {
            if (dishResult == null || dishResult.QualityScore == 0)
                return 0;

            if (dishResult.QualityScore > 0)
                return dishResult.QualityScore * qualityScoreBonusPerPoint;

            return dishResult.QualityScore * qualityScorePenaltyPerPoint;
        }
    }

    public sealed class CookingRewardGrant
    {
        public DishResult DishResult { get; }
        public NpcDishMatchReport MatchReport { get; }
        public int Amount { get; }
        public int BalanceAfter { get; }

        public NpcConversationResult Result => MatchReport?.Evaluation?.Result ?? NpcConversationResult.Wrong;
        public string NpcId => MatchReport?.Order?.NpcId ?? string.Empty;
        public string EventId => MatchReport?.Order?.EventId ?? string.Empty;

        public CookingRewardGrant(
            DishResult dishResult,
            NpcDishMatchReport matchReport,
            int amount,
            int balanceAfter)
        {
            DishResult = dishResult;
            MatchReport = matchReport;
            Amount = Mathf.Max(0, amount);
            BalanceAfter = Mathf.Max(0, balanceAfter);
        }

        public string BuildDebugSummary()
        {
            string dishName = DishResult != null ? DishResult.DisplayName : "None";
            int matchScore = MatchReport != null ? MatchReport.MatchScore : 0;
            int maxMatchScore = MatchReport != null ? MatchReport.MaxMatchScore : 0;
            return
                $"npc={ValueOrNone(NpcId)}, event={ValueOrNone(EventId)}, result={Result}, " +
                $"dish={dishName}, match={matchScore}/{maxMatchScore}, reward={Amount}, balance={BalanceAfter}";
        }

        private static string ValueOrNone(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "None" : value;
        }
    }
}
