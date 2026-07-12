using System;
using System.Collections.Generic;
using UnityEngine;
using Work.Cook.Code.Data;
using Work.Cook.Code.Runtime.Core;
using Work.Cook.Code.Runtime.Events;
using Work.Cook.Code.Runtime.Integration;
using Work.Cook.Code.Runtime.UI;
using Work.Core.EventBus;

namespace Work.Cook.Code.Runtime.Systems
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
        private readonly HashSet<string> _attemptedRecipeIds =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _triedIngredientIds =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _triedPreparationKeys =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly List<CookingKnowledgeUpdate> _pendingUpdates =
            new List<CookingKnowledgeUpdate>();
        private readonly Dictionary<string, HashSet<string>> _knownRecipeTagIds =
            new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        private readonly CookingKnowledgePlayerPrefsRepository _playerPrefsRepository =
            new CookingKnowledgePlayerPrefsRepository();

        private bool _initialized;

        public CookingDataCatalogSO Catalog => catalog;
        public string PlayerPrefsKey => playerPrefsKey;
        public bool HasSavedPlayerPrefs => _playerPrefsRepository.HasSave(playerPrefsKey);
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
        public int PendingKnowledgeUpdateCount
        {
            get
            {
                EnsureInitialized();
                return _pendingUpdates.Count;
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

            if (_initialized == true)
                return;

            RebuildFromSeedData();

            if (loadFromPlayerPrefsOnAwake == true)
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

            string recipeId = CookingKnowledgeKeyUtility.GetRecipeId(recipe);
            return string.IsNullOrWhiteSpace(recipeId) == false
                   && _discoveredRecipeIds.Contains(recipeId);
        }

        public bool IsPreparationEffectKnown(IngredientSO ingredient, IngredientPreparationOption option)
        {
            EnsureInitialized();

            string key = CookingKnowledgeKeyUtility.BuildPreparationKey(ingredient, option);
            return string.IsNullOrWhiteSpace(key) == false
                   && _knownPreparationEffectKeys.Contains(key);
        }

        public bool HasAttemptedRecipe(RecipeSO recipe)
        {
            EnsureInitialized();

            string recipeId = CookingKnowledgeKeyUtility.GetRecipeId(recipe);
            return string.IsNullOrWhiteSpace(recipeId) == false
                   && _attemptedRecipeIds.Contains(recipeId);
        }

        public bool HasTriedIngredient(IngredientSO ingredient)
        {
            EnsureInitialized();

            string ingredientId = CookingKnowledgeKeyUtility.GetIngredientId(ingredient);
            return string.IsNullOrWhiteSpace(ingredientId) == false
                   && _triedIngredientIds.Contains(ingredientId);
        }

        public bool HasTriedPreparation(IngredientSO ingredient, IngredientPreparationOption option)
        {
            EnsureInitialized();

            string key = CookingKnowledgeKeyUtility.BuildPreparationKey(ingredient, option);
            return string.IsNullOrWhiteSpace(key) == false
                   && _triedPreparationKeys.Contains(key);
        }

        public IReadOnlyList<CookingKnowledgeUpdate> PeekPendingKnowledgeUpdates()
        {
            EnsureInitialized();
            return _pendingUpdates;
        }

        public List<CookingKnowledgeUpdate> ConsumePendingKnowledgeUpdates()
        {
            EnsureInitialized();

            List<CookingKnowledgeUpdate> updates = new List<CookingKnowledgeUpdate>(_pendingUpdates);
            _pendingUpdates.Clear();
            return updates;
        }

        public IReadOnlyList<FoodTagSO> GetKnownEffectiveTags(RecipeSO recipe)
        {
            EnsureInitialized();

            string recipeId = CookingKnowledgeKeyUtility.GetRecipeId(recipe);
            if (string.IsNullOrWhiteSpace(recipeId) == true
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
            if (changed == true)
                CommitChanges();

            return changed;
        }

        public bool LearnPreparationEffect(IngredientSO ingredient, IngredientPreparationOption option)
        {
            EnsureInitialized();

            bool changed = AddTriedIngredient(ingredient);
            changed |= AddTriedPreparation(ingredient, option);
            changed |= AddPreparationEffect(ingredient, option);
            if (changed == true)
                CommitChanges();

            return changed;
        }

        public bool LearnSelectedIngredients(IReadOnlyList<IngredientSO> ingredients)
        {
            EnsureInitialized();

            if (ingredients == null)
                return false;

            bool changed = false;
            for (int i = 0; i < ingredients.Count; i++)
                changed |= AddTriedIngredient(ingredients[i]);

            if (changed == true)
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
                changed |= AddAttemptedRecipe(result.BaseRecipe);
                changed |= AddRecipe(result.BaseRecipe);
                changed |= AddRecipeTags(result.BaseRecipe, result.Tags);
            }

            if (result.PreparedIngredients != null)
            {
                for (int i = 0; i < result.PreparedIngredients.Count; i++)
                {
                    PreparedIngredientState prepared = result.PreparedIngredients[i];
                    if (prepared != null)
                    {
                        changed |= AddTriedIngredient(prepared.Ingredient);
                        changed |= AddTriedPreparation(prepared.Ingredient, prepared.PreparationOption);
                        changed |= AddPreparationEffect(prepared.Ingredient, prepared.PreparationOption);
                    }
                }
            }

            if (changed == true)
                CommitChanges();

            return changed;
        }

        public void ClearKnowledgeForDebug()
        {
            EnsureInitialized();

            _discoveredRecipeIds.Clear();
            _knownPreparationEffectKeys.Clear();
            _attemptedRecipeIds.Clear();
            _triedIngredientIds.Clear();
            _triedPreparationKeys.Clear();
            _pendingUpdates.Clear();
            _knownRecipeTagIds.Clear();

            if (saveToPlayerPrefs == true)
                _playerPrefsRepository.Delete(playerPrefsKey);

            NotifyKnowledgeChanged();
        }

        public void ResetToSeedDataForDebug()
        {
            EnsureInitialized();
            RebuildFromSeedData();
            SaveToPlayerPrefs();
            NotifyKnowledgeChanged();
        }

        public string BuildDebugSummary()
        {
            EnsureInitialized();

            return $"recipes={_discoveredRecipeIds.Count}, recipeTags={KnownRecipeTagCount}, " +
                   $"preparationEffects={_knownPreparationEffectKeys.Count}, triedPreparations={_triedPreparationKeys.Count}, " +
                   $"triedIngredients={_triedIngredientIds.Count}, pendingUpdates={_pendingUpdates.Count}, prefsKey={playerPrefsKey}, " +
                   $"hasSavedPrefs={HasSavedPlayerPrefs}";
        }

        private void RebuildFromSeedData()
        {
            _discoveredRecipeIds.Clear();
            _knownPreparationEffectKeys.Clear();
            _attemptedRecipeIds.Clear();
            _triedIngredientIds.Clear();
            _triedPreparationKeys.Clear();
            _pendingUpdates.Clear();
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
            CookingKnowledgeSaveData saveData = _playerPrefsRepository.Load(playerPrefsKey, this);
            if (saveData == null)
                return;

            AddIds(_discoveredRecipeIds, saveData.discoveredRecipeIds);
            AddIds(_knownPreparationEffectKeys, saveData.knownPreparationEffectKeys);
            AddIds(_attemptedRecipeIds, saveData.attemptedRecipeIds);
            AddIds(_triedIngredientIds, saveData.triedIngredientIds);
            AddIds(_triedPreparationKeys, saveData.triedPreparationKeys);

            if (saveData.knownRecipeTags == null)
                return;

            for (int i = 0; i < saveData.knownRecipeTags.Count; i++)
            {
                KnownRecipeTagSaveData entry = saveData.knownRecipeTags[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.recipeId) == true)
                    continue;

                HashSet<string> tags = GetOrCreateRecipeTagSet(entry.recipeId);
                AddIds(tags, entry.tagIds);
            }
        }

        private void SaveToPlayerPrefs()
        {
            if (saveToPlayerPrefs == false || string.IsNullOrWhiteSpace(playerPrefsKey) == true)
                return;

            CookingKnowledgeSaveData saveData = new CookingKnowledgeSaveData
            {
                discoveredRecipeIds = new List<string>(_discoveredRecipeIds),
                knownPreparationEffectKeys = new List<string>(_knownPreparationEffectKeys),
                attemptedRecipeIds = new List<string>(_attemptedRecipeIds),
                triedIngredientIds = new List<string>(_triedIngredientIds),
                triedPreparationKeys = new List<string>(_triedPreparationKeys),
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

            _playerPrefsRepository.Save(playerPrefsKey, saveData);
        }

        private void CommitChanges()
        {
            SaveToPlayerPrefs();
            NotifyKnowledgeChanged();
        }

        private bool AddRecipe(RecipeSO recipe)
        {
            string recipeId = CookingKnowledgeKeyUtility.GetRecipeId(recipe);
            if (string.IsNullOrWhiteSpace(recipeId) == true || _discoveredRecipeIds.Add(recipeId) == false)
                return false;

            QueueUpdate(new CookingKnowledgeUpdate(
                CookingKnowledgeUpdateType.RecipeDiscovered,
                "레시피 발견",
                recipe != null ? recipe.DisplayName : recipeId,
                recipe));
            return true;
        }

        private bool AddAttemptedRecipe(RecipeSO recipe)
        {
            string recipeId = CookingKnowledgeKeyUtility.GetRecipeId(recipe);
            if (string.IsNullOrWhiteSpace(recipeId) == true || _attemptedRecipeIds.Add(recipeId) == false)
                return false;
            return true;

        }

        private bool AddRecipeTags(RecipeSO recipe, IReadOnlyList<FoodTagSO> tags)
        {
            string recipeId = CookingKnowledgeKeyUtility.GetRecipeId(recipe);
            if (string.IsNullOrWhiteSpace(recipeId) == true || tags == null)
                return false;

            HashSet<string> tagSet = GetOrCreateRecipeTagSet(recipeId);
            bool changed = false;

            for (int i = 0; i < tags.Count; i++)
            {
                string tagId = CookingKnowledgeKeyUtility.GetTagId(tags[i]);
                if (string.IsNullOrWhiteSpace(tagId) == false)
                    changed |= tagSet.Add(tagId);
            }

            if (changed == true)
            {
                QueueUpdate(new CookingKnowledgeUpdate(
                    CookingKnowledgeUpdateType.RecipeTagsRevealed,
                    "유효 태그 기록",
                    recipe != null ? recipe.DisplayName : recipeId,
                    recipe));
            }

            return changed;
        }

        private bool AddPreparationEffect(IngredientSO ingredient, IngredientPreparationOption option)
        {
            string key = CookingKnowledgeKeyUtility.BuildPreparationKey(ingredient, option);
            if (string.IsNullOrWhiteSpace(key) == true || _knownPreparationEffectKeys.Add(key) == false)
                return false;

            QueueUpdate(new CookingKnowledgeUpdate(
                CookingKnowledgeUpdateType.PreparationEffectRevealed,
                "손질 효과 기록",
                CookingKnowledgeKeyUtility.BuildPreparationUpdateBody(ingredient, option),
                null,
                ingredient,
                option != null ? option.Method : null));
            return true;
        }

        private bool AddTriedIngredient(IngredientSO ingredient)
        {
            string ingredientId = CookingKnowledgeKeyUtility.GetIngredientId(ingredient);
            if (string.IsNullOrWhiteSpace(ingredientId) == true || _triedIngredientIds.Add(ingredientId) == false)
                return false;
            return true;

        }

        private bool AddTriedPreparation(IngredientSO ingredient, IngredientPreparationOption option)
        {
            string key = CookingKnowledgeKeyUtility.BuildPreparationKey(ingredient, option);
            if (string.IsNullOrWhiteSpace(key) == true || _triedPreparationKeys.Add(key) == false)
                return false;
            return true;

        }

        private void QueueUpdate(CookingKnowledgeUpdate update)
        {
            if (update == null)
                return;

            _pendingUpdates.Add(update);
            Bus<CookingKnowledgeUpdateQueuedEvent>.Raise(new CookingKnowledgeUpdateQueuedEvent(this, update));
        }

        private void NotifyKnowledgeChanged()
        {
            Bus<CookingKnowledgeChangedEvent>.Raise(new CookingKnowledgeChangedEvent(this));
        }

        private HashSet<string> GetOrCreateRecipeTagSet(string recipeId)
        {
            recipeId = CookingKnowledgeKeyUtility.NormalizeId(recipeId);
            if (_knownRecipeTagIds.TryGetValue(recipeId, out HashSet<string> tagSet) == true)
                return tagSet;

            tagSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _knownRecipeTagIds.Add(recipeId, tagSet);
            return tagSet;
        }

        private FoodTagSO FindTagById(string tagId)
        {
            tagId = CookingKnowledgeKeyUtility.NormalizeId(tagId);
            if (string.IsNullOrWhiteSpace(tagId) == true || catalog == null)
                return null;

            for (int i = 0; i < catalog.Tags.Count; i++)
            {
                FoodTagSO tag = catalog.Tags[i];
                if (tag != null && string.Equals(tag.TagId, tagId, StringComparison.OrdinalIgnoreCase) == true)
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
                string id = CookingKnowledgeKeyUtility.NormalizeId(ids[i]);
                if (string.IsNullOrWhiteSpace(id) == false)
                    target.Add(id);
            }
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
    }
}
