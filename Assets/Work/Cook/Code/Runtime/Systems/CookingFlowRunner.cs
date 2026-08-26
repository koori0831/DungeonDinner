using System.Collections.Generic;
using UnityEngine;
using Work.Cook.Code.Data;
using Work.Cook.Code.Runtime.Core;
using Work.Cook.Code.Runtime.Events;
using Work.Cook.Code.Runtime.Integration;
using Work.Cook.Code.Runtime.Systems;
using Work.Cook.Code.Runtime.UI;
using Work.Core.EventBus;

namespace Work.Cook.Code.Runtime.Systems
{
    public sealed class CookingFlowRunner : MonoBehaviour
    {
        [SerializeField] private CookingDataCatalogSO catalog;

        private CookingFlowController _controller;

        public CookingFlowController Controller
        {
            get
            {
                EnsureController();
                return _controller;
            }
        }

        public CookingDataCatalogSO Catalog => catalog;
        public CookingFlowState State => Controller.State;
        public DishResult LastResult => Controller.LastResult;
        public IReadOnlyList<RecipeSO> Recipes => Controller.Recipes;
        public IReadOnlyList<IngredientSO> Ingredients => Controller.Ingredients;
        public IReadOnlyList<IngredientSO> SelectedIngredients => Controller.SelectedIngredients;
        public IReadOnlyList<PreparedIngredientState> PreparedIngredients => Controller.PreparedIngredients;

        private void Awake()
        {
            EnsureController();
        }

        public void SetCatalog(CookingDataCatalogSO value)
        {
            if (catalog == value)
                return;

            catalog = value;
            RebuildController();
        }

        public bool BeginRecipeCooking(RecipeSO recipe)
        {
            return Controller.BeginRecipeCooking(recipe);
        }

        public bool BeginRecipeIngredientSelection(RecipeSO recipe)
        {
            return Controller.BeginRecipeIngredientSelection(recipe);
        }

        public void BeginDirectSelection()
        {
            Controller.BeginDirectSelection();
        }

        public bool BeginDirectCooking(IEnumerable<IngredientSO> ingredients)
        {
            return Controller.BeginDirectCooking(ingredients);
        }

        public bool AddDirectIngredient(IngredientSO ingredient)
        {
            return Controller.AddDirectIngredient(ingredient);
        }

        public bool AddRecipeIngredient(IngredientSO ingredient)
        {
            return Controller.AddRecipeIngredient(ingredient);
        }

        public bool RemoveDirectIngredient(IngredientSO ingredient)
        {
            return Controller.RemoveDirectIngredient(ingredient);
        }

        public bool ConfirmDirectIngredients()
        {
            return Controller.ConfirmDirectIngredients();
        }

        public IReadOnlyList<IngredientPreparationOption> GetPreparationOptions(IngredientSO ingredient)
        {
            return Controller.GetPreparationOptions(ingredient);
        }

        public IngredientSO GetNextUnpreparedIngredient()
        {
            return Controller.GetNextUnpreparedIngredient();
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
            return Controller.SelectPreparation(ingredient, preparationOption, miniGameResult);
        }

        public bool SelectPreparationByMethod(IngredientSO ingredient, PreparationMethodSO method)
        {
            if (ingredient == null || method == null)
                return false;

            return SelectPreparation(ingredient, ingredient.FindPreparationOption(method));
        }

        public bool TryCompleteCooking(out DishResult result)
        {
            if (Controller.TryCompleteCooking(out result) == false)
                return false;

            Bus<CookingFlowCompletedEvent>.Raise(new CookingFlowCompletedEvent(this, result));
            return true;
        }

        public bool TryPreviewCookingResult(out DishResult result)
        {
            return Controller.TryPreviewCookingResult(out result);
        }

        public void ResetFlow()
        {
            Controller.Reset();
        }

        private void EnsureController()
        {
            if (_controller != null)
                return;

            RebuildController();
        }

        private void RebuildController()
        {
            if (_controller != null)
                _controller.StateChanged -= HandleStateChanged;

            _controller = CookingServiceFactory.CreateFlowController(catalog);
            _controller.StateChanged += HandleStateChanged;
            HandleStateChanged();
        }

        private void HandleStateChanged()
        {
            Bus<CookingFlowStateChangedEvent>.Raise(new CookingFlowStateChangedEvent(this, _controller.State));
        }
    }
}
