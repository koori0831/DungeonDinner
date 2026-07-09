using System.Collections.Generic;
using Work.Cook.Code.Data;

namespace Work.Cook.Code.Runtime.Core
{
    public sealed class PreparedIngredientState
    {
        private static readonly IReadOnlyList<FoodTagSO> EMPTY_TAGS = new List<FoodTagSO>();

        private readonly List<FoodTagSO> _addedTags = new List<FoodTagSO>();
        private readonly List<FoodTagSO> _removeTags = new List<FoodTagSO>();

        public IngredientSO Ingredient { get; }
        public IngredientPreparationOption PreparationOption { get; }
        public CookingMiniGameResult MiniGameResult { get; }
        public CookingMiniGameFeedbackRule MiniGameFeedbackRule { get; }
        public PreparationMethodSO Method => PreparationOption?.Method;
        public IReadOnlyList<FoodTagSO> AddedTags => _addedTags;
        public IReadOnlyList<FoodTagSO> RemoveTags => _removeTags;
        public bool CausesDisgusting => PreparationOption != null && PreparationOption.CausesDisgusting;
        public bool AddsPoison => PreparationOption != null && PreparationOption.AddsPoison;
        public int QualityDelta { get; }
        public int PreparationQualityDelta => PreparationOption?.QualityDelta ?? 0;
        public int MiniGameQualityDelta { get; }
        public string ResultNameModifier { get; }
        public bool HasMiniGameResult => MiniGameResult != null;
        public string MiniGameFeedbackText
        {
            get
            {
                if (MiniGameFeedbackRule != null && string.IsNullOrWhiteSpace(MiniGameFeedbackRule.FeedbackText) == false)
                    return MiniGameFeedbackRule.FeedbackText;

                return MiniGameResult != null ? MiniGameResult.FeedbackText : string.Empty;
            }
        }

        public PreparedIngredientState(IngredientSO ingredient, IngredientPreparationOption preparationOption)
            : this(ingredient, preparationOption, null)
        {
        }

        public PreparedIngredientState(
            IngredientSO ingredient,
            IngredientPreparationOption preparationOption,
            CookingMiniGameResult miniGameResult)
        {
            Ingredient = ingredient;
            PreparationOption = preparationOption;
            MiniGameResult = miniGameResult;
            MiniGameFeedbackRule = ResolveMiniGameFeedbackRule(preparationOption, miniGameResult);
            MiniGameQualityDelta = ResolveMiniGameQualityDelta(miniGameResult, MiniGameFeedbackRule);
            QualityDelta = PreparationQualityDelta + MiniGameQualityDelta;
            ResultNameModifier = BuildResultNameModifier(preparationOption, MiniGameFeedbackRule);

            AddTags(_addedTags, preparationOption?.AddTags);
            AddTags(_addedTags, MiniGameFeedbackRule?.AddTags);
            AddTags(_removeTags, preparationOption?.RemoveTags);
            AddTags(_removeTags, MiniGameFeedbackRule?.RemoveTags);
        }

        private static CookingMiniGameFeedbackRule ResolveMiniGameFeedbackRule(
            IngredientPreparationOption preparationOption,
            CookingMiniGameResult miniGameResult)
        {
            if (preparationOption == null || miniGameResult == null)
                return null;

            return preparationOption.FindMiniGameFeedbackRule(miniGameResult.Grade);
        }

        private static int ResolveMiniGameQualityDelta(
            CookingMiniGameResult miniGameResult,
            CookingMiniGameFeedbackRule feedbackRule)
        {
            if (feedbackRule != null)
                return feedbackRule.QualityDelta;

            return miniGameResult != null ? miniGameResult.QualityDelta : 0;
        }

        private static string BuildResultNameModifier(
            IngredientPreparationOption preparationOption,
            CookingMiniGameFeedbackRule feedbackRule)
        {
            string preparationModifier = preparationOption?.ResultNameModifier ?? string.Empty;
            string miniGameModifier = feedbackRule != null ? feedbackRule.ResultNameModifier : string.Empty;

            if (string.IsNullOrWhiteSpace(preparationModifier) == true)
                return miniGameModifier;

            if (string.IsNullOrWhiteSpace(miniGameModifier) == true)
                return preparationModifier;

            return $"{miniGameModifier} {preparationModifier}";
        }

        private static void AddTags(ICollection<FoodTagSO> target, IReadOnlyList<FoodTagSO> source)
        {
            if (target == null || source == null || source == EMPTY_TAGS)
                return;

            for (int i = 0; i < source.Count; i++)
            {
                FoodTagSO tag = source[i];
                if (tag != null && target.Contains(tag) == false)
                    target.Add(tag);
            }
        }
    }
}
