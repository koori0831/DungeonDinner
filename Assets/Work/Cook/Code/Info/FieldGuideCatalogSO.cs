using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using UnityEngine;
using Work.Cook.Code.Data;
using Work.Cook.Code.Runtime.UI;
using Work.Cook.Code.Runtime.Systems;

namespace Work.Cook.Code.Info
{
    [CreateAssetMenu(menuName = "Dungeon Dinner/Field Guide")]
    public sealed class FieldGuideCatalogSO : ScriptableObject
    {
        [SerializeField] private List<IngredientRecord> ingredients = new List<IngredientRecord>();
        [SerializeField] private List<InfoDictionaryCategoryData> categories = new List<InfoDictionaryCategoryData>();
        [SerializeField] private CookingDataCatalogSO cookingCatalog;

        public IReadOnlyList<InfoDictionaryCategoryData> BuildCategories(CookingKnowledgeStore knowledge = null)
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

                Section(body, "재료 분류", ingredient.Category != null ? ingredient.Category.DisplayName : "몬스터 재료");
                var methods = new List<string>();
                foreach (var option in ingredient.PreparationOptions)
                    if (option != null && knowledge != null && knowledge.IsPreparationEffectKnown(ingredient, option) && !string.IsNullOrWhiteSpace(option.DisplayName))
                        {
                            var effects = new List<string>();
                            if (option.AddTags.Count > 0) effects.Add("더해진 맛: " + string.Join(", ", option.AddTags.Where(t => t != null).Select(t => t.DisplayName)));
                            if (option.RemoveTags.Count > 0) effects.Add("줄어든 맛: " + string.Join(", ", option.RemoveTags.Where(t => t != null).Select(t => t.DisplayName)));
                            if (option.AddsPoison) effects.Add("독성 관찰");
                            if (option.CausesDisgusting) effects.Add("불쾌한 풍미 관찰");
                            methods.Add("• " + option.DisplayName + (effects.Count > 0 ? "\n" + string.Join(" · ", effects) : ""));
                        }
                Section(body, "손질 방법", string.Join("\n", methods));

                entries.Add(new InfoDictionaryEntryData(ingredient.DisplayName, ingredient.IconSprite, body.ToString(),
                    CookingKnowledgeStore.IngredientEntryId(ingredient), knowledge != null && knowledge.IsEntryDiscovered(CookingKnowledgeStore.IngredientEntryId(ingredient))));
            }
            if (entries.Count > 0)
                result.Add(new InfoDictionaryCategoryData("재료", entries[0].Icon, MarkerEnum.Ingredient,
                    ViewHaveInfoEnum.Name | ViewHaveInfoEnum.Image | ViewHaveInfoEnum.Description, entries));
            foreach (var category in categories)
            {
                var observed = new List<InfoDictionaryEntryData>();
                foreach (var entry in category.Entries)
                    observed.Add(new InfoDictionaryEntryData(entry.DisplayName, entry.Icon, entry.Description, entry.EntryId,
                        knowledge != null && knowledge.IsEntryDiscovered(entry.EntryId)));
                result.Add(new InfoDictionaryCategoryData(category.DisplayName, category.MarkIcon, category.Marker, category.ViewType, observed));
            }
            var dishes = new List<InfoDictionaryEntryData>();
            var seenRecipes = new HashSet<RecipeSO>();
            if (cookingCatalog != null)
            foreach (var recipe in cookingCatalog.Recipes)
            {
                if (recipe == null || !seenRecipes.Add(recipe))
                    continue;
                var body = new StringBuilder(recipe.Description);
                Section(body, "요리 분류", recipe.Category != null ? recipe.Category.DisplayName : "던전 요리");
                if (knowledge != null && knowledge.IsRecipeDiscovered(recipe))
                {
                    var history = new CookingRecipeKnowledgePresentationBuilder(cookingCatalog).Build(knowledge.GetRecipeKnowledge(recipe));
                    Section(body, "완성 기록", history.CompletionSummary);
                    Section(body, "발견한 맛", history.KnownTags);
                    Section(body, "손님 반응", history.GuestSummaries);
                    foreach (var variant in history.Variants)
                        Section(body, variant.DisplayName, variant.Summary + "\n" + variant.Details);
                }
                dishes.Add(new InfoDictionaryEntryData(recipe.DisplayName, recipe.IconSprite, body.ToString(),
                    CookingKnowledgeStore.RecipeEntryId(recipe), knowledge != null && knowledge.IsRecipeDiscovered(recipe)));
            }
            var incomplete = IncompleteDishDefinitionSO.Instance;
            if (incomplete != null)
                dishes.Add(new InfoDictionaryEntryData(incomplete.DisplayName, incomplete.Icon, incomplete.Description,
                    IncompleteDishDefinitionSO.EntryId, knowledge != null && knowledge.IsEntryDiscovered(IncompleteDishDefinitionSO.EntryId)));
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
