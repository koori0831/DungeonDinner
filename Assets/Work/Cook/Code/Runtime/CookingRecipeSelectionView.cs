using System;
using System.Collections.Generic;
using UnityEngine;
using Work.Cook.Code.Data;
using Work.Cook.Code.Info;

namespace Work.Cook.Code.Runtime
{
    public sealed class CookingRecipeSelectionView : MonoBehaviour
    {
        [SerializeField] private CookingGamePanel gamePanel;
        [SerializeField] private InfoDictionaryPanel dictionaryPanel;
        [SerializeField] private CookingDataCatalogSO fallbackCatalog;

        [Header("Dictionary Category")]
        [SerializeField] private string uncategorizedCategoryDisplayName = "기타";
        [SerializeField] private Sprite defaultCategoryIcon;
        [SerializeField] private ViewHaveInfoEnum recipeDisplayViewType =
            ViewHaveInfoEnum.Name | ViewHaveInfoEnum.Image | ViewHaveInfoEnum.Description;

        [Header("Recipe Discovery")]
        [SerializeField] private bool showAllRecipesUntilKnowledgeStoreExists = true;
        [SerializeField] private bool showBaseTagsAsKnownForTesting;
        [SerializeField] private List<RecipeSO> discoveredRecipes = new List<RecipeSO>();
        [SerializeField] private List<KnownRecipeTagEntry> knownRecipeTags = new List<KnownRecipeTagEntry>();

        [Header("Direct Selection Entry")]
        [SerializeField] private bool includeDirectIngredientSelection = true;
        [SerializeField] private string directSelectionDisplayName = "재료 직접 선택";
        [SerializeField, TextArea] private string directSelectionDescription =
            "가방에서 재료를 직접 골라 알려진 레시피에 없는 조합을 시도합니다.";
        [SerializeField] private Sprite directSelectionIcon;

        [Header("Build")]
        [SerializeField] private bool refreshOnEnable = true;

        private void Awake()
        {
            EnsureReferences();
        }

        private void OnEnable()
        {
            if (refreshOnEnable)
                Refresh();
        }

        public void Refresh()
        {
            EnsureReferences();

            if (dictionaryPanel == null)
            {
                Debug.LogWarning("CookingRecipeSelectionView needs an InfoDictionaryPanel before it can show recipes.", this);
                return;
            }

            dictionaryPanel.Initialize(BuildCategories());
        }

        public void SetGamePanel(CookingGamePanel value)
        {
            gamePanel = value;
        }

        public void SetFallbackCatalog(CookingDataCatalogSO value)
        {
            fallbackCatalog = value;
        }

        private IReadOnlyList<InfoDictionaryCategoryData> BuildCategories()
        {
            List<InfoDictionaryCategoryData> categories = new List<InfoDictionaryCategoryData>();

            AddRecipeCategories(categories);

            if (includeDirectIngredientSelection)
                AddDirectSelectionToEveryCategory(categories);

            return categories;
        }

        private void AddRecipeCategories(ICollection<InfoDictionaryCategoryData> categories)
        {
            List<FoodCategorySO> orderedCategories = new List<FoodCategorySO>();
            Dictionary<FoodCategorySO, List<InfoDictionaryEntryData>> entriesByCategory =
                new Dictionary<FoodCategorySO, List<InfoDictionaryEntryData>>();
            List<InfoDictionaryEntryData> uncategorizedEntries = new List<InfoDictionaryEntryData>();

            IReadOnlyList<RecipeSO> sourceRecipes = GetSourceRecipes();
            for (int i = 0; i < sourceRecipes.Count; i++)
            {
                RecipeSO recipe = sourceRecipes[i];
                if (recipe == null || IsRecipeVisible(recipe) == false)
                    continue;

                CookingRecipeEntryData entry = new CookingRecipeEntryData(
                    recipe,
                    null,
                    GetKnownEffectiveTags(recipe));

                FoodCategorySO category = recipe.Category;
                if (category == null)
                {
                    uncategorizedEntries.Add(entry);
                    continue;
                }

                if (entriesByCategory.TryGetValue(category, out List<InfoDictionaryEntryData> entries) == false)
                {
                    entries = new List<InfoDictionaryEntryData>();
                    entriesByCategory.Add(category, entries);
                    orderedCategories.Add(category);
                }

                entries.Add(entry);
            }

            for (int i = 0; i < orderedCategories.Count; i++)
            {
                FoodCategorySO category = orderedCategories[i];
                categories.Add(new InfoDictionaryCategoryData(
                    category.DisplayName,
                    GetCategoryIcon(category),
                    MarkerEnum.Recipe,
                    recipeDisplayViewType,
                    entriesByCategory[category]));
            }

            if (uncategorizedEntries.Count > 0)
            {
                categories.Add(new InfoDictionaryCategoryData(
                    uncategorizedCategoryDisplayName,
                    defaultCategoryIcon,
                    MarkerEnum.Recipe,
                    recipeDisplayViewType,
                    uncategorizedEntries));
            }
        }

        private CookingRecipeEntryData CreateDirectSelectionEntry()
        {
            return new CookingRecipeEntryData(
                directSelectionDisplayName,
                directSelectionIcon,
                directSelectionDescription,
                true);
        }

        private Sprite GetCategoryIcon(FoodCategorySO category)
        {
            if (category == null)
                return defaultCategoryIcon;

            return category.Icon != null ? category.Icon : defaultCategoryIcon;
        }

        private void AddDirectSelectionToEveryCategory(IList<InfoDictionaryCategoryData> categories)
        {
            if (categories == null || categories.Count == 0)
                return;

            for (int i = 0; i < categories.Count; i++)
            {
                InfoDictionaryCategoryData category = categories[i];
                if (category == null)
                    continue;

                List<InfoDictionaryEntryData> entries = new List<InfoDictionaryEntryData>
                {
                    CreateDirectSelectionEntry()
                };

                if (category.Entries != null)
                    entries.AddRange(category.Entries);

                categories[i] = new InfoDictionaryCategoryData(
                    category.DisplayName,
                    category.MarkIcon,
                    category.Marker,
                    category.ViewType,
                    entries);
            }
        }

        private IReadOnlyList<RecipeSO> GetSourceRecipes()
        {
            if (gamePanel != null && gamePanel.FlowRunner != null)
                return gamePanel.FlowRunner.Recipes;

            return fallbackCatalog != null ? fallbackCatalog.Recipes : Array.Empty<RecipeSO>();
        }

        private bool IsRecipeVisible(RecipeSO recipe)
        {
            if (recipe == null)
                return false;

            return showAllRecipesUntilKnowledgeStoreExists || discoveredRecipes.Contains(recipe);
        }

        private IReadOnlyList<FoodTagSO> GetKnownEffectiveTags(RecipeSO recipe)
        {
            if (recipe == null)
                return Array.Empty<FoodTagSO>();

            if (showBaseTagsAsKnownForTesting)
                return recipe.BaseTags;

            for (int i = 0; i < knownRecipeTags.Count; i++)
            {
                KnownRecipeTagEntry entry = knownRecipeTags[i];
                if (entry != null && entry.Recipe == recipe)
                    return entry.KnownEffectiveTags;
            }

            return Array.Empty<FoodTagSO>();
        }

        private void EnsureReferences()
        {
            if (gamePanel == null)
                gamePanel = GetComponentInParent<CookingGamePanel>();

            if (dictionaryPanel == null)
                dictionaryPanel = GetComponentInChildren<InfoDictionaryPanel>(true);
        }

        [Serializable]
        private sealed class KnownRecipeTagEntry
        {
            [SerializeField] private RecipeSO recipe;
            [SerializeField] private List<FoodTagSO> knownEffectiveTags = new List<FoodTagSO>();

            public RecipeSO Recipe => recipe;
            public IReadOnlyList<FoodTagSO> KnownEffectiveTags => knownEffectiveTags;
        }
    }
}
