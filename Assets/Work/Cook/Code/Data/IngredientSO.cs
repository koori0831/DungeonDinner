using System.Collections.Generic;
using UnityEngine;

namespace Work.Cook.Code.Data
{
    [CreateAssetMenu(fileName = "Ingredient", menuName = "Dungeon Dinner/Cooking/Ingredient")]
    public sealed class IngredientSO : ScriptableObject
    {
        [SerializeField] private string ingredientId;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [SerializeField] private IngredientCategorySO category;
        [SerializeField] private GameObject modelPrefab;
        [SerializeField] private List<FoodTagSO> baseTags = new List<FoodTagSO>();
        [SerializeField] private List<IngredientPreparationOption> preparationOptions = new List<IngredientPreparationOption>();

        public string IngredientId => ingredientId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? ingredientId : displayName;
        public string Description => description;
        public IngredientCategorySO Category => category;
        public GameObject ModelPrefab => modelPrefab;
        public IReadOnlyList<FoodTagSO> BaseTags => baseTags;
        public IReadOnlyList<IngredientPreparationOption> PreparationOptions => preparationOptions;

        public IngredientPreparationOption FindPreparationOption(PreparationMethodSO method)
        {
            if (method == null)
                return null;

            for (int i = 0; i < preparationOptions.Count; i++)
            {
                IngredientPreparationOption option = preparationOptions[i];
                if (option != null && option.Method == method)
                    return option;
            }

            return null;
        }
    }
}
