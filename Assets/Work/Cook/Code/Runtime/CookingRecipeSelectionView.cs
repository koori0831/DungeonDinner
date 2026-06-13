using System;
using System.Collections.Generic;
using UnityEngine;
using Work.Cook.Code.Data;
using Work.Cook.Code.Info;

namespace Work.Cook.Code.Runtime
{
    public sealed class CookingRecipeSelectionView : MonoBehaviour, ICookingRecipeSelectionView
    {
        [SerializeField] private CookingGamePanel gamePanel;
        [SerializeField] private InfoDictionaryPanel dictionaryPanel;
        [SerializeField] private CookingDataCatalogSO fallbackCatalog;
        [SerializeField] private CookingKnowledgeStore knowledgeStore;

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

        private CookingKnowledgeStore _subscribedKnowledgeStore;

        private void Awake()
        {
            EnsureReferences();
        }

        private void OnEnable()
        {
            EnsureReferences();
            SubscribeKnowledgeStore();

            if (refreshOnEnable)
                Refresh();
        }

        private void OnDisable()
        {
            UnsubscribeKnowledgeStore();
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

        public void Initialize(CookingGamePanel owner, CookingFlowRunner runner, CookingKnowledgeStore store)
        {
            gamePanel = owner;
            knowledgeStore = store;

            if (runner != null)
                SetFallbackCatalog(runner.Catalog);

            EnsureReferences();
            SubscribeKnowledgeStore();

            if (isActiveAndEnabled)
                Refresh();
        }

        public void SetGamePanel(CookingGamePanel value)
        {
            gamePanel = value;
        }

        public void SetFallbackCatalog(CookingDataCatalogSO value)
        {
            fallbackCatalog = value;
            knowledgeStore?.SetCatalog(fallbackCatalog);
        }

        private IReadOnlyList<InfoDictionaryCategoryData> BuildCategories()
        {
            List<InfoDictionaryCategoryData> categories = new List<InfoDictionaryCategoryData>();

            AddRecipeCategories(categories);

            if (includeDirectIngredientSelection)
            {
                EnsureDirectSelectionCategories(categories);
                AddDirectSelectionToEveryCategory(categories);
            }

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

        private void EnsureDirectSelectionCategories(IList<InfoDictionaryCategoryData> categories)
        {
            if (categories == null)
                return;

            IReadOnlyList<FoodCategorySO> sourceCategories = GetSourceCategories();
            for (int i = 0; i < sourceCategories.Count; i++)
            {
                FoodCategorySO category = sourceCategories[i];
                if (category == null || ContainsCategory(categories, category.DisplayName))
                    continue;

                categories.Add(new InfoDictionaryCategoryData(
                    category.DisplayName,
                    GetCategoryIcon(category),
                    MarkerEnum.Recipe,
                    recipeDisplayViewType,
                    new List<InfoDictionaryEntryData>()));
            }

            if (categories.Count == 0)
            {
                categories.Add(new InfoDictionaryCategoryData(
                    uncategorizedCategoryDisplayName,
                    defaultCategoryIcon,
                    MarkerEnum.Recipe,
                    recipeDisplayViewType,
                    new List<InfoDictionaryEntryData>()));
            }
        }

        private IReadOnlyList<RecipeSO> GetSourceRecipes()
        {
            if (gamePanel != null && gamePanel.FlowRunner != null)
                return gamePanel.FlowRunner.Recipes;

            return fallbackCatalog != null ? fallbackCatalog.Recipes : Array.Empty<RecipeSO>();
        }

        private IReadOnlyList<FoodCategorySO> GetSourceCategories()
        {
            if (fallbackCatalog != null)
                return fallbackCatalog.Categories;

            if (gamePanel != null
                && gamePanel.FlowRunner != null
                && gamePanel.FlowRunner.Catalog != null)
            {
                return gamePanel.FlowRunner.Catalog.Categories;
            }

            return Array.Empty<FoodCategorySO>();
        }

        private bool IsRecipeVisible(RecipeSO recipe)
        {
            if (recipe == null)
                return false;

            if (knowledgeStore != null)
                return knowledgeStore.IsRecipeDiscovered(recipe);

            return showAllRecipesUntilKnowledgeStoreExists || discoveredRecipes.Contains(recipe);
        }

        private IReadOnlyList<FoodTagSO> GetKnownEffectiveTags(RecipeSO recipe)
        {
            if (recipe == null)
                return Array.Empty<FoodTagSO>();

            if (knowledgeStore != null)
                return knowledgeStore.GetKnownEffectiveTags(recipe);

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

            if (knowledgeStore == null && gamePanel != null)
                knowledgeStore = gamePanel.KnowledgeStore;

            if (knowledgeStore == null)
                knowledgeStore = GetComponentInParent<CookingKnowledgeStore>();

            if (knowledgeStore != null)
            {
                if (fallbackCatalog != null)
                    knowledgeStore.SetCatalog(fallbackCatalog);
                else if (gamePanel != null && gamePanel.FlowRunner != null)
                    knowledgeStore.SetCatalog(gamePanel.FlowRunner.Catalog);
            }
        }

        private void SubscribeKnowledgeStore()
        {
            if (_subscribedKnowledgeStore == knowledgeStore)
                return;

            UnsubscribeKnowledgeStore();

            if (knowledgeStore == null)
                return;

            knowledgeStore.KnowledgeChanged += HandleKnowledgeChanged;
            _subscribedKnowledgeStore = knowledgeStore;
        }

        private void UnsubscribeKnowledgeStore()
        {
            if (_subscribedKnowledgeStore == null)
                return;

            _subscribedKnowledgeStore.KnowledgeChanged -= HandleKnowledgeChanged;
            _subscribedKnowledgeStore = null;
        }

        private void HandleKnowledgeChanged()
        {
            if (isActiveAndEnabled)
                Refresh();
        }

        private static bool ContainsCategory(IList<InfoDictionaryCategoryData> categories, string displayName)
        {
            if (categories == null)
                return false;

            for (int i = 0; i < categories.Count; i++)
            {
                InfoDictionaryCategoryData category = categories[i];
                if (category != null
                    && string.Equals(category.DisplayName, displayName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
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
