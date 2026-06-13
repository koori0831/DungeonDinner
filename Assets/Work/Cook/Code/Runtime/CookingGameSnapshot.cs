using System;
using System.Collections.Generic;
using Work.Cook.Code.Data;
using Work.NPC.Code.Runtime;

namespace Work.Cook.Code.Runtime
{
    [Serializable]
    public sealed class CookingGameSnapshot
    {
        public CookingGameScreenState Screen { get; }
        public CookingFlowState FlowState { get; }
        public CookingMode? Mode { get; }
        public RecipeSO SelectedRecipe { get; }
        public IngredientSO CurrentIngredient { get; }
        public DishResult CurrentResult { get; }
        public int SelectedIngredientCount { get; }
        public int PreparedIngredientCount { get; }
        public int KnownRecipeCount { get; }
        public int KnownPreparationEffectCount { get; }
        public int RewardBalance { get; }
        public int PreviewRewardAmount { get; }
        public NpcDishMatchReport CurrentNpcMatchReport { get; }
        public IReadOnlyList<IngredientSO> SelectedIngredients { get; }
        public IReadOnlyList<PreparedIngredientState> PreparedIngredients { get; }

        public bool HasSelectedIngredients => SelectedIngredientCount > 0;
        public bool HasCurrentIngredient => CurrentIngredient != null;
        public bool HasCurrentResult => CurrentResult != null;
        public bool CanHandResultToNpc { get; }
        public bool HasNpcMatchReport => CurrentNpcMatchReport != null;
        public bool IsEveryIngredientPrepared => SelectedIngredientCount > 0
                                                 && SelectedIngredientCount == PreparedIngredientCount;

        public CookingGameSnapshot(
            CookingGameScreenState screen,
            CookingFlowState flowState,
            CookingMode? mode,
            RecipeSO selectedRecipe,
            IngredientSO currentIngredient,
            DishResult currentResult,
            IReadOnlyList<IngredientSO> selectedIngredients,
            IReadOnlyList<PreparedIngredientState> preparedIngredients,
            int knownRecipeCount,
            int knownPreparationEffectCount,
            int rewardBalance,
            int previewRewardAmount = 0,
            NpcDishMatchReport currentNpcMatchReport = null,
            bool canHandResultToNpc = false)
        {
            Screen = screen;
            FlowState = flowState;
            Mode = mode;
            SelectedRecipe = selectedRecipe;
            CurrentIngredient = currentIngredient;
            CurrentResult = currentResult;
            SelectedIngredients = CopyList(selectedIngredients);
            PreparedIngredients = CopyList(preparedIngredients);
            SelectedIngredientCount = SelectedIngredients.Count;
            PreparedIngredientCount = PreparedIngredients.Count;
            KnownRecipeCount = knownRecipeCount;
            KnownPreparationEffectCount = knownPreparationEffectCount;
            RewardBalance = rewardBalance;
            PreviewRewardAmount = Math.Max(0, previewRewardAmount);
            CurrentNpcMatchReport = currentNpcMatchReport;
            CanHandResultToNpc = canHandResultToNpc;
        }

        public string BuildDebugSummary()
        {
            string recipeName = SelectedRecipe != null ? SelectedRecipe.DisplayName : "None";
            string ingredientName = CurrentIngredient != null ? CurrentIngredient.DisplayName : "None";
            string resultName = CurrentResult != null ? CurrentResult.DisplayName : "None";

            return $"screen={Screen}, flow={FlowState}, mode={Mode?.ToString() ?? "None"}, " +
                   $"recipe={recipeName}, currentIngredient={ingredientName}, result={resultName}, " +
                   $"selected={SelectedIngredientCount}, prepared={PreparedIngredientCount}, " +
                   $"knownRecipes={KnownRecipeCount}, knownPrepEffects={KnownPreparationEffectCount}, " +
                   $"rewardBalance={RewardBalance}, rewardPreview={PreviewRewardAmount}, " +
                   $"canHandResult={CanHandResultToNpc}, hasNpcMatch={HasNpcMatchReport}";
        }

        private static IReadOnlyList<T> CopyList<T>(IReadOnlyList<T> source)
        {
            if (source == null || source.Count == 0)
                return new List<T>();

            List<T> copy = new List<T>(source.Count);
            for (int i = 0; i < source.Count; i++)
                copy.Add(source[i]);

            return copy;
        }
    }
}
