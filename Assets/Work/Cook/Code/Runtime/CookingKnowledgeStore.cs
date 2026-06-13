using System;
using System.Collections.Generic;
using UnityEngine;
using Work.Cook.Code.Data;

namespace Work.Cook.Code.Runtime
{
    public sealed class CookingKnowledgeStore : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private CookingDataCatalogSO catalog;
        [SerializeField] private List<RecipeSO> initialDiscoveredRecipes = new List<RecipeSO>();
        [SerializeField] private List<KnownRecipeTagSeedEntry> initialKnownRecipeTags = new List<KnownRecipeTagSeedEntry>();
        [SerializeField] private List<KnownPreparationEffectSeedEntry> initialKnownPreparationEffects =
            new List<KnownPreparationEffectSeedEntry>();

        [Header("Persistence")]
        [SerializeField] private bool loadFromPlayerPrefsOnAwake = true;
        [SerializeField] private bool saveToPlayerPrefs = true;
        [SerializeField] private string playerPrefsKey = "DungeonDinner.CookingKnowledge";

        private readonly HashSet<string> _discoveredRecipeIds =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _knownPreparationEffectKeys =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, HashSet<string>> _knownRecipeTagIds =
            new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        private bool _initialized;

        public event Action KnowledgeChanged;

        public CookingDataCatalogSO Catalog => catalog;
        public string PlayerPrefsKey => playerPrefsKey;
        public bool HasSavedPlayerPrefs => string.IsNullOrWhiteSpace(playerPrefsKey) == false
                                           && PlayerPrefs.HasKey(playerPrefsKey);
        public int DiscoveredRecipeCount
        {
            get
            {
                EnsureInitialized();
                return _discoveredRecipeIds.Count;
            }
        }
        public int KnownPreparationEffectCount
        {
            get
            {
                EnsureInitialized();
                return _knownPreparationEffectKeys.Count;
            }
        }
        public int KnownRecipeTagCount
        {
            get
            {
                EnsureInitialized();

                int count = 0;
                foreach (KeyValuePair<string, HashSet<string>> pair in _knownRecipeTagIds)
                    count += pair.Value.Count;

                return count;
            }
        }

        private void Awake()
        {
            Initialize(catalog);
        }

        public void Initialize(CookingDataCatalogSO defaultCatalog = null)
        {
            if (defaultCatalog != null)
                catalog = defaultCatalog;

            if (_initialized)
                return;

            RebuildFromSeedData();

            if (loadFromPlayerPrefsOnAwake)
                LoadFromPlayerPrefs();

            _initialized = true;
        }

        public void SetCatalog(CookingDataCatalogSO value)
        {
            if (value != null)
                catalog = value;

            EnsureInitialized();
        }

        public bool IsRecipeDiscovered(RecipeSO recipe)
        {
            EnsureInitialized();

            string recipeId = GetRecipeId(recipe);
            return string.IsNullOrWhiteSpace(recipeId) == false
                   && _discoveredRecipeIds.Contains(recipeId);
        }

        public bool IsPreparationEffectKnown(IngredientSO ingredient, IngredientPreparationOption option)
        {
            EnsureInitialized();

            string key = BuildPreparationKey(ingredient, option);
            return string.IsNullOrWhiteSpace(key) == false
                   && _knownPreparationEffectKeys.Contains(key);
        }

        public IReadOnlyList<FoodTagSO> GetKnownEffectiveTags(RecipeSO recipe)
        {
            EnsureInitialized();

            string recipeId = GetRecipeId(recipe);
            if (string.IsNullOrWhiteSpace(recipeId)
                || _knownRecipeTagIds.TryGetValue(recipeId, out HashSet<string> tagIds) == false)
            {
                return Array.Empty<FoodTagSO>();
            }

            List<FoodTagSO> tags = new List<FoodTagSO>();
            foreach (string tagId in tagIds)
            {
                FoodTagSO tag = FindTagById(tagId);
                if (tag != null)
                    tags.Add(tag);
            }

            return tags;
        }

        public bool DiscoverRecipe(RecipeSO recipe)
        {
            EnsureInitialized();

            bool changed = AddRecipe(recipe);
            if (changed)
                CommitChanges();

            return changed;
        }

        public bool LearnPreparationEffect(IngredientSO ingredient, IngredientPreparationOption option)
        {
            EnsureInitialized();

            bool changed = AddPreparationEffect(ingredient, option);
            if (changed)
                CommitChanges();

            return changed;
        }

        public bool LearnFromResult(DishResult result)
        {
            EnsureInitialized();

            if (result == null)
                return false;

            bool changed = false;

            if (result.BaseRecipe != null)
            {
                changed |= AddRecipe(result.BaseRecipe);
                changed |= AddRecipeTags(result.BaseRecipe, result.Tags);
            }

            if (result.PreparedIngredients != null)
            {
                for (int i = 0; i < result.PreparedIngredients.Count; i++)
                {
                    PreparedIngredientState prepared = result.PreparedIngredients[i];
                    if (prepared != null)
                        changed |= AddPreparationEffect(prepared.Ingredient, prepared.PreparationOption);
                }
            }

            if (changed)
                CommitChanges();

            return changed;
        }

        public void ClearKnowledgeForDebug()
        {
            EnsureInitialized();

            _discoveredRecipeIds.Clear();
            _knownPreparationEffectKeys.Clear();
            _knownRecipeTagIds.Clear();

            if (saveToPlayerPrefs && string.IsNullOrWhiteSpace(playerPrefsKey) == false)
            {
                PlayerPrefs.DeleteKey(playerPrefsKey);
                PlayerPrefs.Save();
            }

            KnowledgeChanged?.Invoke();
        }

        public void ResetToSeedDataForDebug()
        {
            EnsureInitialized();
            RebuildFromSeedData();
            SaveToPlayerPrefs();
            KnowledgeChanged?.Invoke();
        }

        public string BuildDebugSummary()
        {
            EnsureInitialized();

            return $"recipes={_discoveredRecipeIds.Count}, recipeTags={KnownRecipeTagCount}, " +
                   $"preparationEffects={_knownPreparationEffectKeys.Count}, prefsKey={playerPrefsKey}, " +
                   $"hasSavedPrefs={HasSavedPlayerPrefs}";
        }

        private void RebuildFromSeedData()
        {
            _discoveredRecipeIds.Clear();
            _knownPreparationEffectKeys.Clear();
            _knownRecipeTagIds.Clear();

            for (int i = 0; i < initialDiscoveredRecipes.Count; i++)
                AddRecipe(initialDiscoveredRecipes[i]);

            for (int i = 0; i < initialKnownRecipeTags.Count; i++)
            {
                KnownRecipeTagSeedEntry entry = initialKnownRecipeTags[i];
                if (entry != null)
                    AddRecipeTags(entry.Recipe, entry.KnownEffectiveTags);
            }

            for (int i = 0; i < initialKnownPreparationEffects.Count; i++)
            {
                KnownPreparationEffectSeedEntry entry = initialKnownPreparationEffects[i];
                if (entry != null)
                    AddPreparationEffect(entry.Ingredient, entry.PreparationOption);
            }
        }

        private void LoadFromPlayerPrefs()
        {
            if (string.IsNullOrWhiteSpace(playerPrefsKey) || PlayerPrefs.HasKey(playerPrefsKey) == false)
                return;

            string json = PlayerPrefs.GetString(playerPrefsKey);
            if (string.IsNullOrWhiteSpace(json))
                return;

            CookingKnowledgeSaveData saveData;
            try
            {
                saveData = JsonUtility.FromJson<CookingKnowledgeSaveData>(json);
            }
            catch (ArgumentException exception)
            {
                Debug.LogWarning($"Failed to load cooking knowledge data. {exception.Message}", this);
                return;
            }

            if (saveData == null)
                return;

            AddIds(_discoveredRecipeIds, saveData.discoveredRecipeIds);
            AddIds(_knownPreparationEffectKeys, saveData.knownPreparationEffectKeys);

            if (saveData.knownRecipeTags == null)
                return;

            for (int i = 0; i < saveData.knownRecipeTags.Count; i++)
            {
                KnownRecipeTagSaveData entry = saveData.knownRecipeTags[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.recipeId))
                    continue;

                HashSet<string> tags = GetOrCreateRecipeTagSet(entry.recipeId);
                AddIds(tags, entry.tagIds);
            }
        }

        private void SaveToPlayerPrefs()
        {
            if (saveToPlayerPrefs == false || string.IsNullOrWhiteSpace(playerPrefsKey))
                return;

            CookingKnowledgeSaveData saveData = new CookingKnowledgeSaveData
            {
                discoveredRecipeIds = new List<string>(_discoveredRecipeIds),
                knownPreparationEffectKeys = new List<string>(_knownPreparationEffectKeys),
                knownRecipeTags = new List<KnownRecipeTagSaveData>()
            };

            foreach (KeyValuePair<string, HashSet<string>> pair in _knownRecipeTagIds)
            {
                saveData.knownRecipeTags.Add(new KnownRecipeTagSaveData
                {
                    recipeId = pair.Key,
                    tagIds = new List<string>(pair.Value)
                });
            }

            PlayerPrefs.SetString(playerPrefsKey, JsonUtility.ToJson(saveData));
            PlayerPrefs.Save();
        }

        private void CommitChanges()
        {
            SaveToPlayerPrefs();
            KnowledgeChanged?.Invoke();
        }

        private bool AddRecipe(RecipeSO recipe)
        {
            string recipeId = GetRecipeId(recipe);
            return string.IsNullOrWhiteSpace(recipeId) == false
                   && _discoveredRecipeIds.Add(recipeId);
        }

        private bool AddRecipeTags(RecipeSO recipe, IReadOnlyList<FoodTagSO> tags)
        {
            string recipeId = GetRecipeId(recipe);
            if (string.IsNullOrWhiteSpace(recipeId) || tags == null)
                return false;

            HashSet<string> tagSet = GetOrCreateRecipeTagSet(recipeId);
            bool changed = false;

            for (int i = 0; i < tags.Count; i++)
            {
                string tagId = GetTagId(tags[i]);
                if (string.IsNullOrWhiteSpace(tagId) == false)
                    changed |= tagSet.Add(tagId);
            }

            return changed;
        }

        private bool AddPreparationEffect(IngredientSO ingredient, IngredientPreparationOption option)
        {
            string key = BuildPreparationKey(ingredient, option);
            return string.IsNullOrWhiteSpace(key) == false
                   && _knownPreparationEffectKeys.Add(key);
        }

        private HashSet<string> GetOrCreateRecipeTagSet(string recipeId)
        {
            recipeId = NormalizeId(recipeId);
            if (_knownRecipeTagIds.TryGetValue(recipeId, out HashSet<string> tagSet))
                return tagSet;

            tagSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _knownRecipeTagIds.Add(recipeId, tagSet);
            return tagSet;
        }

        private FoodTagSO FindTagById(string tagId)
        {
            tagId = NormalizeId(tagId);
            if (string.IsNullOrWhiteSpace(tagId) || catalog == null)
                return null;

            for (int i = 0; i < catalog.Tags.Count; i++)
            {
                FoodTagSO tag = catalog.Tags[i];
                if (tag != null && string.Equals(tag.TagId, tagId, StringComparison.OrdinalIgnoreCase))
                    return tag;
            }

            return null;
        }

        private void EnsureInitialized()
        {
            if (_initialized == false)
                Initialize(catalog);
        }

        private static void AddIds(HashSet<string> target, IReadOnlyList<string> ids)
        {
            if (target == null || ids == null)
                return;

            for (int i = 0; i < ids.Count; i++)
            {
                string id = NormalizeId(ids[i]);
                if (string.IsNullOrWhiteSpace(id) == false)
                    target.Add(id);
            }
        }

        private static string BuildPreparationKey(IngredientSO ingredient, IngredientPreparationOption option)
        {
            if (ingredient == null || option == null)
                return string.Empty;

            string ingredientId = GetIngredientId(ingredient);
            string methodId = option.Method != null ? option.Method.MethodId : option.DisplayName;
            methodId = NormalizeId(methodId);

            if (string.IsNullOrWhiteSpace(ingredientId) || string.IsNullOrWhiteSpace(methodId))
                return string.Empty;

            return $"{ingredientId}:{methodId}";
        }

        private static string GetRecipeId(RecipeSO recipe)
        {
            if (recipe == null)
                return string.Empty;

            string recipeId = NormalizeId(recipe.RecipeId);
            return string.IsNullOrWhiteSpace(recipeId) ? NormalizeId(recipe.DisplayName) : recipeId;
        }

        private static string GetIngredientId(IngredientSO ingredient)
        {
            if (ingredient == null)
                return string.Empty;

            string ingredientId = NormalizeId(ingredient.IngredientId);
            return string.IsNullOrWhiteSpace(ingredientId) ? NormalizeId(ingredient.DisplayName) : ingredientId;
        }

        private static string GetTagId(FoodTagSO tag)
        {
            if (tag == null)
                return string.Empty;

            string tagId = NormalizeId(tag.TagId);
            return string.IsNullOrWhiteSpace(tagId) ? NormalizeId(tag.DisplayName) : tagId;
        }

        private static string NormalizeId(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        [Serializable]
        private sealed class KnownRecipeTagSeedEntry
        {
            [SerializeField] private RecipeSO recipe;
            [SerializeField] private List<FoodTagSO> knownEffectiveTags = new List<FoodTagSO>();

            public RecipeSO Recipe => recipe;
            public IReadOnlyList<FoodTagSO> KnownEffectiveTags => knownEffectiveTags;
        }

        [Serializable]
        private sealed class KnownPreparationEffectSeedEntry
        {
            [SerializeField] private IngredientSO ingredient;
            [SerializeField] private IngredientPreparationOption preparationOption;

            public IngredientSO Ingredient => ingredient;
            public IngredientPreparationOption PreparationOption => preparationOption;
        }

        [Serializable]
        private sealed class CookingKnowledgeSaveData
        {
            public List<string> discoveredRecipeIds = new List<string>();
            public List<string> knownPreparationEffectKeys = new List<string>();
            public List<KnownRecipeTagSaveData> knownRecipeTags = new List<KnownRecipeTagSaveData>();
        }

        [Serializable]
        private sealed class KnownRecipeTagSaveData
        {
            public string recipeId;
            public List<string> tagIds = new List<string>();
        }
    }
}
