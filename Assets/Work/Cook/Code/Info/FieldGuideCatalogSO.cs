using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Work.Cook.Code.Data;
using Work.Cook.Code.Runtime.UI;

namespace Work.Cook.Code.Info
{
    [CreateAssetMenu(menuName = "Dungeon Dinner/Field Guide")]
    public sealed class FieldGuideCatalogSO : ScriptableObject
    {
        [SerializeField] private List<IngredientRecord> ingredients = new List<IngredientRecord>();
        [SerializeField] private List<InfoDictionaryCategoryData> categories = new List<InfoDictionaryCategoryData>();
        [SerializeField] private CookingDataCatalogSO cookingCatalog;

        public IReadOnlyList<InfoDictionaryCategoryData> BuildCategories()
        {
            var result = new List<InfoDictionaryCategoryData>();
            var entries = new List<InfoDictionaryEntryData>();
            var seen = new HashSet<IngredientSO>();
            foreach (var record in ingredients)
            {
                if (record == null || record.ingredient == null || !seen.Add(record.ingredient))
                    continue;
                var ingredient = record.ingredient;
                var body = new StringBuilder(ingredient.Description);
                Section(body, "채집 노트", record.source);
                Section(body, "재료 분류", ingredient.Category != null ? ingredient.Category.DisplayName : "몬스터 재료");
                var methods = new List<string>();
                foreach (var option in ingredient.PreparationOptions)
                    if (option != null && !string.IsNullOrWhiteSpace(option.DisplayName))
                        methods.Add("• " + option.DisplayName);
                Section(body, "손질 방법", string.Join("\n", methods));
                Section(body, "요리사의 메모", record.notes);
                entries.Add(new InfoDictionaryEntryData(ingredient.DisplayName, ingredient.IconSprite, body.ToString()));
            }
            if (entries.Count > 0)
                result.Add(new InfoDictionaryCategoryData("재료", entries[0].Icon, MarkerEnum.Ingredient,
                    ViewHaveInfoEnum.Name | ViewHaveInfoEnum.Image | ViewHaveInfoEnum.Description, entries));
            result.AddRange(categories);
            var dishes = new List<InfoDictionaryEntryData>();
            var seenRecipes = new HashSet<RecipeSO>();
            if (cookingCatalog != null)
            foreach (var recipe in cookingCatalog.Recipes)
            {
                if (recipe == null || !seenRecipes.Add(recipe))
                    continue;
                var body = new StringBuilder(recipe.Description);
                Section(body, "요리 분류", recipe.Category != null ? recipe.Category.DisplayName : "던전 요리");
                var requirements = new List<string>();
                foreach (var requirement in recipe.RequiredIngredients)
                {
                    var text = CookingRecipeDisplayPanel.BuildRequirementText(requirement);
                    if (!string.IsNullOrWhiteSpace(text)) requirements.Add("• " + text);
                }
                Section(body, "재료와 손질", string.Join("\n", requirements));
                Section(body, "조리 메모", recipe.HintDescription);
                dishes.Add(new InfoDictionaryEntryData(recipe.DisplayName, recipe.IconSprite, body.ToString()));
            }
            if (dishes.Count > 0)
                result.Add(new InfoDictionaryCategoryData("요리", dishes[0].Icon, MarkerEnum.Recipe,
                    ViewHaveInfoEnum.Name | ViewHaveInfoEnum.Image | ViewHaveInfoEnum.Description, dishes));
            return result;
        }

        private static void Section(StringBuilder body, string title, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                body.Append("\n\n<b><color=#81552F>").Append(title).Append("</color></b>\n").Append(value);
        }

        [Serializable]
        private sealed class IngredientRecord
        {
            public IngredientSO ingredient = null;
            [TextArea] public string source = string.Empty;
            [TextArea] public string notes = string.Empty;
        }
    }
}
