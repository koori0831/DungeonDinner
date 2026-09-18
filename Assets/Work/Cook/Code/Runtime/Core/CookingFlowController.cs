using System;
using System.Collections.Generic;
using Work.Cook.Code.Data;

namespace Work.Cook.Code.Runtime.Core
{
    public sealed class CookingFlowController
    {
        private static readonly IReadOnlyList<RecipeSO> EmptyRecipes = Array.Empty<RecipeSO>();
        private static readonly IReadOnlyList<IngredientSO> EmptyIngredients = Array.Empty<IngredientSO>();
        private static readonly IReadOnlyList<IngredientPreparationOption> EmptyPreparationOptions =
            Array.Empty<IngredientPreparationOption>();
        private static readonly IReadOnlyList<PreparedIngredientState> EmptyPreparedIngredients =
            Array.Empty<PreparedIngredientState>();

        private readonly ICookingDataProvider _dataProvider;
        private readonly IDishResultBuilder _resultBuilder;

        private CookingSession _session;

        public event Action StateChanged;

        public CookingFlowState State { get; private set; } = CookingFlowState.Idle;
        public CookingSession CurrentSession => _session;
        public DishResult LastResult { get; private set; }
        public IReadOnlyList<RecipeSO> Recipes => _dataProvider?.GetRecipes() ?? EmptyRecipes;
        public IReadOnlyList<IngredientSO> Ingredients => _dataProvider?.GetIngredients() ?? EmptyIngredients;
        public IReadOnlyList<IngredientSO> SelectedIngredients => _session?.SelectedIngredients ?? EmptyIngredients;
        public IReadOnlyList<PreparedIngredientState> PreparedIngredients =>
            _session?.PreparedIngredients ?? EmptyPreparedIngredients;

        public CookingFlowController(ICookingDataProvider dataProvider, IDishResultBuilder resultBuilder)
        {
            _dataProvider = dataProvider;
            _resultBuilder = resultBuilder;
        }

        public bool BeginRecipeCooking(RecipeSO recipe)
        {
            if (recipe == null)
                return false;

            _session = CookingSession.CreateForRecipe(recipe);
            LastResult = null;
            SetState(_session.SelectedIngredients.Count > 0
                ? CookingFlowState.PreparingIngredients
                : CookingFlowState.ReadyToComplete);
            return true;
        }

        public bool BeginRecipeIngredientSelection(RecipeSO recipe)
        {
            return BeginRecipeIngredientSelection(recipe, null);
        }

        public bool BeginRecipeIngredientSelection(RecipeSO recipe, CookingRecipeStartPlan startPlan)
        {
            if (recipe == null)
                return false;

            _session = CookingSession.CreateForRecipe(recipe, Array.Empty<IngredientSO>(), startPlan);
            LastResult = null;
            SetState(CookingFlowState.SelectingIngredients);
            return true;
        }

        public void BeginDirectSelection()
        {
            _session = CookingSession.CreateForDirectIngredients(null);
            LastResult = null;
            SetState(CookingFlowState.SelectingIngredients);
        }

        public bool BeginDirectCooking(IEnumerable<IngredientSO> ingredients)
        {
            _session = CookingSession.CreateForDirectIngredients(ingredients);
            LastResult = null;

            if (_session.SelectedIngredients.Count == 0)
            {
                SetState(CookingFlowState.SelectingIngredients);
                return false;
            }

            SetState(CookingFlowState.PreparingIngredients);
            return true;
        }

        public bool AddDirectIngredient(IngredientSO ingredient)
        {
            if (ingredient == null)
                return false;

            EnsureDirectSelectionSession();
            _session.AddIngredient(ingredient);
            LastResult = null;
            SetState(CookingFlowState.SelectingIngredients);
            return true;
        }

        public bool AddRecipeIngredient(IngredientSO ingredient)
        {
            if (ingredient == null || _session == null || _session.Mode != CookingMode.Recipe)
                return false;

            _session.AddIngredient(ingredient);
            LastResult = null;
            SetState(CookingFlowState.SelectingIngredients);
            return true;
        }

        public bool RemoveDirectIngredient(IngredientSO ingredient)
        {
            if (_session == null
                || (_session.Mode != CookingMode.DirectIngredients && _session.Mode != CookingMode.Recipe))
                return false;

            bool removed = _session.RemoveIngredient(ingredient);
            if (removed == false)
                return false;

            LastResult = null;
            SetState(CookingFlowState.SelectingIngredients);
            return true;
        }

        public bool ConfirmDirectIngredients()
        {
            if (_session == null
                || (_session.Mode != CookingMode.DirectIngredients && _session.Mode != CookingMode.Recipe))
                return false;

            if (_session.SelectedIngredients.Count == 0)
                return false;

            if (_session.Mode == CookingMode.DirectIngredients)
                _session.ClearPreparations();

            LastResult = null;
            SetState(CookingFlowState.PreparingIngredients);
            return true;
        }

        public IReadOnlyList<IngredientPreparationOption> GetPreparationOptions(IngredientSO ingredient)
        {
            return _dataProvider?.GetPreparationOptions(ingredient) ?? EmptyPreparationOptions;
        }

        public IngredientSO GetNextUnpreparedIngredient()
        {
            return GetNextUnpreparedOccurrence()?.Ingredient;
        }

        public CookingIngredientOccurrence GetNextUnpreparedOccurrence()
        {
            if (_session == null)
                return null;

            for (int i = 0; i < _session.SelectedIngredients.Count; i++)
            {
                IngredientSO ingredient = _session.SelectedIngredients[i];
                if (_session.IsOccurrencePrepared(i) == false)
                {
                    int occurrence = 0;
                    for (int previous = 0; previous < i; previous++)
                    {
                        if (_session.SelectedIngredients[previous] == ingredient)
                            occurrence++;
                    }
                    return new CookingIngredientOccurrence(i, occurrence, ingredient);
                }
            }

            return null;
        }

        public PlannedPreparation GetCurrentPreparationRecommendation()
        {
            CookingIngredientOccurrence occurrence = GetNextUnpreparedOccurrence();
            return occurrence != null && _session?.StartPlan != null
                ? _session.StartPlan.GetPreparationRecommendation(
                    _session.SelectedIngredients,
                    occurrence.SelectedIndex)
                : null;
        }

        public bool IsCurrentPreparationAllowed(IngredientPreparationOption option)
        {
            CookingIngredientOccurrence occurrence = GetNextUnpreparedOccurrence();
            return occurrence == null || _session?.StartPlan == null
                || _session.StartPlan.IsPreparationAllowed(
                    _session.SelectedIngredients,
                    occurrence.SelectedIndex,
                    option);
        }

        public bool SelectPreparation(IngredientSO ingredient, IngredientPreparationOption preparationOption)
        {
            return SelectPreparation(ingredient, preparationOption, null);
        }

        public bool SelectPreparation(
            IngredientSO ingredient,
            IngredientPreparationOption preparationOption,
            CookingMiniGameResult miniGameResult)
        {
            if (_session == null || ingredient == null)
                return false;

            if (IsSelectedIngredient(ingredient) == false)
                return false;

            _session.SelectPreparation(ingredient, preparationOption, miniGameResult);
            LastResult = null;
            SetState(_session.IsEveryIngredientPrepared()
                ? CookingFlowState.ReadyToComplete
                : CookingFlowState.PreparingIngredients);
            return true;
        }

        public bool TryCompleteCooking(out DishResult result)
        {
            result = null;
            if (CanCompleteCooking() == false)
                return false;

            result = _resultBuilder.Build(_session);
            LastResult = result;
            SetState(CookingFlowState.Completed);
            return true;
        }

        public bool TryPreviewCookingResult(out DishResult result)
        {
            result = null;
            if (CanCompleteCooking() == false)
                return false;

            result = _resultBuilder.Build(_session);
            return result != null;
        }

        public bool CanCompleteCooking()
        {
            return _session != null
                   && _resultBuilder != null
                   && _session.SelectedIngredients.Count > 0
                   && _session.IsEveryIngredientPrepared();
        }

        public void Reset()
        {
            _session = null;
            LastResult = null;
            SetState(CookingFlowState.Idle);
        }

        private void EnsureDirectSelectionSession()
        {
            if (_session != null && _session.Mode == CookingMode.DirectIngredients)
                return;

            _session = CookingSession.CreateForDirectIngredients(null);
        }

        private bool IsSelectedIngredient(IngredientSO ingredient)
        {
            for (int i = 0; i < _session.SelectedIngredients.Count; i++)
            {
                if (_session.SelectedIngredients[i] == ingredient)
                    return true;
            }

            return false;
        }

        private void SetState(CookingFlowState state)
        {
            State = state;
            StateChanged?.Invoke();
        }
    }
}
