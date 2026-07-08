using System.Collections.Generic;
using Work.Cook.Code.Data;

namespace Work.Cook.Code.Runtime
{
    public sealed class CookingSession
    {
        private readonly List<IngredientSO> _selectedIngredients = new List<IngredientSO>();
        private readonly List<PreparedIngredientState> _preparedIngredients = new List<PreparedIngredientState>();

        public CookingMode Mode { get; }
        public RecipeSO SelectedRecipe { get; }
        public IReadOnlyList<IngredientSO> SelectedIngredients => _selectedIngredients;
        public IReadOnlyList<PreparedIngredientState> PreparedIngredients => _preparedIngredients;

        private CookingSession(CookingMode mode, RecipeSO selectedRecipe)
        {
            Mode = mode;
            SelectedRecipe = selectedRecipe;
        }

        public static CookingSession CreateForRecipe(RecipeSO recipe)
        {
            return CreateForRecipe(recipe, null);
        }

        public static CookingSession CreateForRecipe(RecipeSO recipe, IEnumerable<IngredientSO> selectedIngredients)
        {
            CookingSession session = new CookingSession(CookingMode.Recipe, recipe);
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
                RemovePreparationIfIngredientIsGone(ingredient);
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

            for (int i = _preparedIngredients.Count - 1; i >= 0; i--)
            {
                if (_preparedIngredients[i].Ingredient == ingredient)
                    _preparedIngredients.RemoveAt(i);
            }

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
                if (GetPreparedIngredient(_selectedIngredients[i]) == null)
                    return false;
            }

            return true;
        }

        private void RemovePreparationIfIngredientIsGone(IngredientSO ingredient)
        {
            for (int i = 0; i < _selectedIngredients.Count; i++)
            {
                if (_selectedIngredients[i] == ingredient)
                    return;
            }

            for (int i = _preparedIngredients.Count - 1; i >= 0; i--)
            {
                if (_preparedIngredients[i].Ingredient == ingredient)
                    _preparedIngredients.RemoveAt(i);
            }
        }
    }
}
