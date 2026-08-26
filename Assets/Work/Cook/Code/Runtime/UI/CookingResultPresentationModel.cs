using System.Collections.Generic;
using Work.Cook.Code.Data;
using Work.Cook.Code.Runtime.Core;
using Work.NPC.Code.Data;

namespace Work.Cook.Code.Runtime.UI
{
    public sealed class CookingTagChipModel
    {
        public string DisplayName { get; }
        public CookingTagPresentationKind Kind { get; }
        public CookingTagPresentationStatus Status { get; }

        public CookingTagChipModel(
            string displayName,
            CookingTagPresentationKind kind,
            CookingTagPresentationStatus status)
        {
            DisplayName = displayName ?? string.Empty;
            Kind = kind;
            Status = status;
        }
    }

    public sealed class CookingPreparedIngredientPresentationModel
    {
        public string IngredientName { get; }
        public string MethodName { get; }
        public string GradeName { get; }
        public CookingMiniGameGrade? Grade { get; }
        public int QualityDelta { get; }
        public string Feedback { get; }
        public IReadOnlyList<string> EffectLabels { get; }
        public PreparedIngredientState Source { get; }

        public CookingPreparedIngredientPresentationModel(
            string ingredientName,
            string methodName,
            string gradeName,
            CookingMiniGameGrade? grade,
            int qualityDelta,
            string feedback,
            IReadOnlyList<string> effectLabels,
            PreparedIngredientState source)
        {
            IngredientName = ingredientName ?? string.Empty;
            MethodName = methodName ?? string.Empty;
            GradeName = gradeName ?? string.Empty;
            Grade = grade;
            QualityDelta = qualityDelta;
            Feedback = feedback ?? string.Empty;
            EffectLabels = effectLabels ?? new List<string>();
            Source = source;
        }
    }

    public sealed class CookingResultPresentationModel
    {
        public string DishName { get; }
        public string RecipeName { get; }
        public string CategoryName { get; }
        public DishQuality Quality { get; }
        public string QualityName { get; }
        public int QualityScore { get; }
        public IReadOnlyList<string> RepresentativeTags { get; }
        public string NpcName { get; }
        public NpcConversationResult Reaction { get; }
        public string ReactionName { get; }
        public string ReactionSummary { get; }
        public int MatchScore { get; }
        public int MaxMatchScore { get; }
        public int MatchPercent { get; }
        public int PreviewReward { get; }
        public bool HasNpcReport { get; }
        public bool CanHandToNpc { get; }
        public IReadOnlyList<CookingTagChipModel> TagComparisons { get; }
        public IReadOnlyList<CookingPreparedIngredientPresentationModel> PreparedIngredients { get; }
        public IReadOnlyList<string> Reasons { get; }
        public DishResult Source { get; }

        public CookingResultPresentationModel(
            string dishName,
            string recipeName,
            string categoryName,
            DishQuality quality,
            string qualityName,
            int qualityScore,
            IReadOnlyList<string> representativeTags,
            string npcName,
            NpcConversationResult reaction,
            string reactionName,
            string reactionSummary,
            int matchScore,
            int maxMatchScore,
            int matchPercent,
            int previewReward,
            bool hasNpcReport,
            bool canHandToNpc,
            IReadOnlyList<CookingTagChipModel> tagComparisons,
            IReadOnlyList<CookingPreparedIngredientPresentationModel> preparedIngredients,
            IReadOnlyList<string> reasons,
            DishResult source)
        {
            DishName = dishName ?? string.Empty;
            RecipeName = recipeName ?? string.Empty;
            CategoryName = categoryName ?? string.Empty;
            Quality = quality;
            QualityName = qualityName ?? string.Empty;
            QualityScore = qualityScore;
            RepresentativeTags = representativeTags ?? new List<string>();
            NpcName = npcName ?? string.Empty;
            Reaction = reaction;
            ReactionName = reactionName ?? string.Empty;
            ReactionSummary = reactionSummary ?? string.Empty;
            MatchScore = matchScore;
            MaxMatchScore = maxMatchScore;
            MatchPercent = matchPercent;
            PreviewReward = previewReward;
            HasNpcReport = hasNpcReport;
            CanHandToNpc = canHandToNpc;
            TagComparisons = tagComparisons ?? new List<CookingTagChipModel>();
            PreparedIngredients = preparedIngredients ?? new List<CookingPreparedIngredientPresentationModel>();
            Reasons = reasons ?? new List<string>();
            Source = source;
        }
    }

    public sealed class CookingOrderPresentationModel
    {
        public bool HasOrder { get; }
        public string NpcName { get; }
        public string RecipeName { get; }
        public IReadOnlyList<CookingTagChipModel> Tags { get; }
        public int PreparedCount { get; }
        public int IngredientCount { get; }
        public string EmptyMessage { get; }

        public CookingOrderPresentationModel(
            bool hasOrder,
            string npcName,
            string recipeName,
            IReadOnlyList<CookingTagChipModel> tags,
            int preparedCount,
            int ingredientCount,
            string emptyMessage)
        {
            HasOrder = hasOrder;
            NpcName = npcName ?? string.Empty;
            RecipeName = recipeName ?? string.Empty;
            Tags = tags ?? new List<CookingTagChipModel>();
            PreparedCount = preparedCount;
            IngredientCount = ingredientCount;
            EmptyMessage = emptyMessage ?? string.Empty;
        }
    }
}
