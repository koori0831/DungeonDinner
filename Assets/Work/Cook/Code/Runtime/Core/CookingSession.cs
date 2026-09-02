using System;
using System.Collections.Generic;
using Work.Cook.Code.Data;

namespace Work.Cook.Code.Runtime.Core
{
    public sealed class CookingSession
    {
        private readonly List<IngredientSO> _selectedIngredients = new List<IngredientSO>();
        private readonly List<PreparedIngredientState> _preparedIngredients = new List<PreparedIngredientState>();

        public CookingMode Mode { get; }
        public string SessionId { get; }
        public RecipeSO SelectedRecipe { get; }
        public CookingRecipeStartPlan StartPlan { get; private set; }
        public IReadOnlyList<IngredientSO> SelectedIngredients => _selectedIngredients;
        public IReadOnlyList<PreparedIngredientState> PreparedIngredients => _preparedIngredients;

        private CookingSession(CookingMode mode, RecipeSO selectedRecipe)
        {
            SessionId = Guid.NewGuid().ToString("N");
            Mode = mode;
            SelectedRecipe = selectedRecipe;
        }

        public static CookingSession CreateForRecipe(RecipeSO recipe)
        {
            return CreateForRecipe(recipe, null);
        }

        public static CookingSession CreateForRecipe(RecipeSO recipe, IEnumerable<IngredientSO> selectedIngredients)
        {
            return CreateForRecipe(recipe, selectedIngredients, null);
        }

        public static CookingSession CreateForRecipe(
            RecipeSO recipe,
            IEnumerable<IngredientSO> selectedIngredients,
            CookingRecipeStartPlan startPlan)
        {
            CookingSession session = new CookingSession(CookingMode.Recipe, recipe);
            session.StartPlan = startPlan;
            if (recipe == null)
                return session;

            if (selectedIngredients != null)
            {
                foreach (IngredientSO ingredient in selectedIngredients)
                    session.AddIngredient(ingredient);
            }
            else
            {
                for (int i = 0; i < recipe.RequiredIngredients.Count; i++)
                {
                    RecipeIngredientRequirement requirement = recipe.RequiredIngredients[i];
                    if (requirement != null && requirement.Ingredient != null)
                        session.AddIngredient(requirement.Ingredient);
                }
            }

            return session;
        }

        public void SetStartPlan(CookingRecipeStartPlan startPlan)
        {
            StartPlan = startPlan;
        }

        public static CookingSession CreateForDirectIngredients(IEnumerable<IngredientSO> ingredients)
        {
            CookingSession session = new CookingSession(CookingMode.DirectIngredients, null);
            if (ingredients == null)
                return session;

            foreach (IngredientSO ingredient in ingredients)
                session.AddIngredient(ingredient);

            return session;
        }

        public void AddIngredient(IngredientSO ingredient)
        {
            if (ingredient == null)
                return;

            _selectedIngredients.Add(ingredient);
        }

        public bool RemoveIngredient(IngredientSO ingredient)
        {
            if (ingredient == null)
                return false;

            for (int i = _selectedIngredients.Count - 1; i >= 0; i--)
            {
                if (_selectedIngredients[i] != ingredient)
                    continue;

                _selectedIngredients.RemoveAt(i);
                TrimPreparationsToSelectedCount(ingredient);
                return true;
            }

            return false;
        }

        public void SelectPreparation(IngredientSO ingredient, IngredientPreparationOption preparationOption)
        {
            SelectPreparation(ingredient, preparationOption, null);
        }

        public void SelectPreparation(
            IngredientSO ingredient,
            IngredientPreparationOption preparationOption,
            CookingMiniGameResult miniGameResult)
        {
            if (ingredient == null)
                return;

            // One call prepares one selected occurrence. This preserves separate
            // preparation records when the same IngredientSO is selected twice.
            int selectedCount = CountSelectedOccurrences(ingredient);
            int preparedCount = CountPreparedOccurrences(ingredient);
            if (preparedCount >= selectedCount)
                RemoveLastPreparedOccurrence(ingredient);
            _preparedIngredients.Add(new PreparedIngredientState(ingredient, preparationOption, miniGameResult));
        }

        public void ClearPreparations()
        {
            _preparedIngredients.Clear();
        }

        public PreparedIngredientState GetPreparedIngredient(IngredientSO ingredient)
        {
            for (int i = 0; i < _preparedIngredients.Count; i++)
            {
                PreparedIngredientState state = _preparedIngredients[i];
                if (state.Ingredient == ingredient)
                    return state;
            }

            return null;
        }

        public bool IsEveryIngredientPrepared()
        {
            if (_selectedIngredients.Count == 0)
                return false;

            for (int i = 0; i < _selectedIngredients.Count; i++)
            {
                IngredientSO ingredient = _selectedIngredients[i];
                if (CountPreparedOccurrences(ingredient) < CountSelectedOccurrences(ingredient))
                    return false;
            }

            return true;
        }

        public bool IsOccurrencePrepared(int selectedIndex)
        {
            if (selectedIndex < 0 || selectedIndex >= _selectedIngredients.Count)
                return false;

            IngredientSO ingredient = _selectedIngredients[selectedIndex];
            int occurrence = 0;
            for (int i = 0; i <= selectedIndex; i++)
            {
                if (_selectedIngredients[i] == ingredient)
                    occurrence++;
            }

            return CountPreparedOccurrences(ingredient) >= occurrence;
        }

        private int CountSelectedOccurrences(IngredientSO ingredient)
        {
            int count = 0;
            for (int i = 0; i < _selectedIngredients.Count; i++)
            {
                if (_selectedIngredients[i] == ingredient)
                    count++;
            }

            return count;
        }

        private int CountPreparedOccurrences(IngredientSO ingredient)
        {
            int count = 0;
            for (int i = 0; i < _preparedIngredients.Count; i++)
            {
                if (_preparedIngredients[i]?.Ingredient == ingredient)
                    count++;
            }

            return count;
        }

        private void TrimPreparationsToSelectedCount(IngredientSO ingredient)
        {
            int selectedCount = CountSelectedOccurrences(ingredient);
            while (CountPreparedOccurrences(ingredient) > selectedCount)
                RemoveLastPreparedOccurrence(ingredient);
        }

        private void RemoveLastPreparedOccurrence(IngredientSO ingredient)
        {
            for (int i = _preparedIngredients.Count - 1; i >= 0; i--)
            {
                if (_preparedIngredients[i].Ingredient == ingredient)
                {
                    _preparedIngredients.RemoveAt(i);
                    return;
                }
            }
        }
    }
}
