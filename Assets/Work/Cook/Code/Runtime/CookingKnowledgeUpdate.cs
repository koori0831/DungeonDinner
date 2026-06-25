using System;
using UnityEngine;
using Work.Cook.Code.Data;

namespace Work.Cook.Code.Runtime
{
    public enum CookingKnowledgeUpdateType
    {
        RecipeAttempted,
        RecipeDiscovered,
        IngredientTried,
        PreparationTried,
        PreparationEffectRevealed,
        RecipeTagsRevealed
    }

    [Serializable]
    public sealed class CookingKnowledgeUpdate
    {
        [SerializeField] private CookingKnowledgeUpdateType updateType;
        [SerializeField] private string title;
        [SerializeField, TextArea] private string body;
        [SerializeField] private RecipeSO recipe;
        [SerializeField] private IngredientSO ingredient;
        [SerializeField] private PreparationMethodSO preparationMethod;

        public CookingKnowledgeUpdateType UpdateType => updateType;
        public string Title => title;
        public string Body => body;
        public RecipeSO Recipe => recipe;
        public IngredientSO Ingredient => ingredient;
        public PreparationMethodSO PreparationMethod => preparationMethod;

        public CookingKnowledgeUpdate(
            CookingKnowledgeUpdateType updateType,
            string title,
            string body,
            RecipeSO recipe = null,
            IngredientSO ingredient = null,
            PreparationMethodSO preparationMethod = null)
        {
            this.updateType = updateType;
            this.title = title;
            this.body = body;
            this.recipe = recipe;
            this.ingredient = ingredient;
            this.preparationMethod = preparationMethod;
        }
    }
}
