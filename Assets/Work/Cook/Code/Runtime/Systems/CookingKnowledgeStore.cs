using System;
using System.Collections.Generic;
using UnityEngine;
using Work.Cook.Code.Data;
using Work.Cook.Code.Runtime.Core;
using Work.Cook.Code.Runtime.Events;
using Work.Cook.Code.Runtime.UI;
using Work.Core.EventBus;
using Work.NPC.Code.Data;
using Work.NPC.Code.Runtime;

namespace Work.Cook.Code.Runtime.Systems
{
    public sealed class CookingKnowledgeStore : MonoBehaviour
    {
        private const int CURRENT_SCHEMA_VERSION = 2;

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
        private readonly HashSet<string> _appliedCookingSessionIds =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, HashSet<string>> _knownRecipeTagIds =
            new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, KnownRecipeRecord> _recipeRecords =
            new Dictionary<string, KnownRecipeRecord>(StringComparer.OrdinalIgnoreCase);
        private readonly List<CookingKnowledgeUpdate> _pendingUpdates =
            new List<CookingKnowledgeUpdate>();
        private readonly CookingKnowledgePlayerPrefsRepository _playerPrefsRepository =
            new CookingKnowledgePlayerPrefsRepository();

        private bool _initialized;
        private string _changedVariantId;

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
            string recipeId = CookingKnowledgeKeyUtility.GetRecipeId(recipe);
            return string.IsNullOrWhiteSpace(recipeId) == false && _discoveredRecipeIds.Contains(recipeId);
        }

        public bool IsPreparationEffectKnown(IngredientSO ingredient, IngredientPreparationOption option)
        {
            EnsureInitialized();
            string key = CookingKnowledgeKeyUtility.BuildPreparationKey(ingredient, option);
            return string.IsNullOrWhiteSpace(key) == false && _knownPreparationEffectKeys.Contains(key);
        }

        public bool HasAttemptedRecipe(RecipeSO recipe)
        {
            EnsureInitialized();
            string recipeId = CookingKnowledgeKeyUtility.GetRecipeId(recipe);
            return string.IsNullOrWhiteSpace(recipeId) == false && _attemptedRecipeIds.Contains(recipeId);
        }

        public bool HasTriedIngredient(IngredientSO ingredient)
        {
            EnsureInitialized();
            string ingredientId = CookingKnowledgeKeyUtility.GetIngredientId(ingredient);
            return string.IsNullOrWhiteSpace(ingredientId) == false && _triedIngredientIds.Contains(ingredientId);
        }

        public bool HasTriedPreparation(IngredientSO ingredient, IngredientPreparationOption option)
        {
            EnsureInitialized();
            string key = CookingKnowledgeKeyUtility.BuildPreparationKey(ingredient, option);
            return string.IsNullOrWhiteSpace(key) == false && _triedPreparationKeys.Contains(key);
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
            if (string.IsNullOrWhiteSpace(recipeId)
                || _knownRecipeTagIds.TryGetValue(recipeId, out HashSet<string> tagIds) == false)
            {
                return Array.Empty<FoodTagSO>();
            }

            return ResolveTags(tagIds);
        }

        public CookingRecipeKnowledgeSnapshot GetRecipeKnowledge(RecipeSO recipe)
        {
            EnsureInitialized();
            string recipeId = CookingKnowledgeKeyUtility.GetRecipeId(recipe);
            bool discovered = string.IsNullOrWhiteSpace(recipeId) == false
                              && _discoveredRecipeIds.Contains(recipeId);
            if (string.IsNullOrWhiteSpace(recipeId)
                || _recipeRecords.TryGetValue(recipeId, out KnownRecipeRecord record) == false)
            {
                return new CookingRecipeKnowledgeSnapshot(
                    recipe,
                    discovered,
                    0,
                    DishCraftGrade.Bad,
                    Array.Empty<FoodTagSO>(),
                    Array.Empty<CookingRecipeVariantKnowledgeSnapshot>(),
                    Array.Empty<RecipeGuestSummarySnapshot>());
            }

            return BuildRecipeSnapshot(recipe, record, discovered);
        }

        public IReadOnlyList<CookingRecipeVariantKnowledgeSnapshot> GetDiscoveredVariants(RecipeSO recipe)
        {
            return GetRecipeKnowledge(recipe).Variants;
        }

        public bool TryGetDiscoveredVariant(
            RecipeSO recipe,
            string variantId,
            out CookingRecipeVariantKnowledgeSnapshot snapshot)
        {
            snapshot = null;
            if (string.IsNullOrWhiteSpace(variantId))
                return false;

            IReadOnlyList<CookingRecipeVariantKnowledgeSnapshot> variants = GetDiscoveredVariants(recipe);
            for (int i = 0; i < variants.Count; i++)
            {
                if (string.Equals(variants[i].VariantId, variantId, StringComparison.OrdinalIgnoreCase))
                {
                    snapshot = variants[i];
                    return true;
                }
            }

            return false;
        }

        public IReadOnlyList<string> GetDiscoveredVariantKeys(RecipeSO recipe)
        {
            IReadOnlyList<CookingRecipeVariantKnowledgeSnapshot> variants = GetDiscoveredVariants(recipe);
            List<string> ids = new List<string>(variants.Count);
            for (int i = 0; i < variants.Count; i++)
                ids.Add(variants[i].VariantId);
            return ids;
        }

        public bool DiscoverRecipe(RecipeSO recipe)
        {
            EnsureInitialized();
            bool changed = AddRecipe(recipe);
            if (changed)
                CommitChanges();
            return changed;
        }

        public bool LearnFromService(DishResult result, NpcDishMatchReport report)
        {
            EnsureInitialized();
            if (result == null || report == null)
                return false;

            if (string.IsNullOrWhiteSpace(result.CookingSessionId) == false
                && _appliedCookingSessionIds.Add(result.CookingSessionId) == false)
            {
                return false;
            }

            bool changed = false;
            if (result.TargetRecipe != null)
                changed |= AddAttemptedRecipe(result.TargetRecipe);

            if (result.IsRecipeMatched && result.BaseRecipe != null)
            {
                changed |= AddAttemptedRecipe(result.BaseRecipe);
                changed |= AddRecipe(result.BaseRecipe);
                IReadOnlyList<FoodTagSO> revealedTags = BuildRevealedResultTags(result, report);
                changed |= RecordRecipeCompletion(result, report, revealedTags);
                changed |= AddRecipeTags(result.BaseRecipe, revealedTags);
            }

            if (result.PreparedIngredients != null)
            {
                for (int i = 0; i < result.PreparedIngredients.Count; i++)
                {
                    PreparedIngredientState prepared = result.PreparedIngredients[i];
                    if (prepared == null)
                        continue;
                    changed |= AddTriedIngredient(prepared.Ingredient);
                    changed |= AddTriedPreparation(prepared.Ingredient, prepared.PreparationOption);
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
            ClearRuntimeData();
            if (saveToPlayerPrefs)
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
            int variantCount = 0;
            foreach (KnownRecipeRecord record in _recipeRecords.Values)
                variantCount += record.variants != null ? record.variants.Count : 0;
            return $"recipes={_discoveredRecipeIds.Count}, recipeTags={KnownRecipeTagCount}, " +
                   $"variants={variantCount}, preparationEffects={_knownPreparationEffectKeys.Count}, " +
                   $"triedPreparations={_triedPreparationKeys.Count}, triedIngredients={_triedIngredientIds.Count}, " +
                   $"pendingUpdates={_pendingUpdates.Count}, prefsKey={playerPrefsKey}, hasSavedPrefs={HasSavedPlayerPrefs}";
        }

        private void RebuildFromSeedData()
        {
            ClearRuntimeData();
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

        private void ClearRuntimeData()
        {
            _discoveredRecipeIds.Clear();
            _knownPreparationEffectKeys.Clear();
            _attemptedRecipeIds.Clear();
            _triedIngredientIds.Clear();
            _triedPreparationKeys.Clear();
            _appliedCookingSessionIds.Clear();
            _knownRecipeTagIds.Clear();
            _recipeRecords.Clear();
            _pendingUpdates.Clear();
            _changedVariantId = string.Empty;
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

            bool migrateLegacy = saveData.schemaVersion < CURRENT_SCHEMA_VERSION;
            if (migrateLegacy == false && saveData.recipeRecords != null)
            {
                for (int i = 0; i < saveData.recipeRecords.Count; i++)
                    LoadRecipeRecord(saveData.recipeRecords[i]);
            }

            LoadLegacyRecipeTags(saveData.knownRecipeTags);
            if (migrateLegacy)
            {
                MigrateLegacyVariants(saveData.knownRecipeVariants);
                foreach (string recipeId in _discoveredRecipeIds)
                    GetOrCreateRecipeRecord(recipeId);
                SaveToPlayerPrefs();
            }
        }

        private void LoadLegacyRecipeTags(IReadOnlyList<KnownRecipeTagSaveData> entries)
        {
            if (entries == null)
                return;
            for (int i = 0; i < entries.Count; i++)
            {
                KnownRecipeTagSaveData entry = entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.recipeId))
                    continue;
                HashSet<string> tags = GetOrCreateRecipeTagSet(entry.recipeId);
                AddIds(tags, entry.tagIds);
                KnownRecipeRecord record = GetOrCreateRecipeRecord(entry.recipeId);
                AddIds(record.knownTagIds, entry.tagIds);
            }
        }

        private void LoadRecipeRecord(KnownRecipeRecord record)
        {
            if (record == null || string.IsNullOrWhiteSpace(record.recipeId))
                return;
            NormalizeRecord(record);
            _recipeRecords[record.recipeId] = record;
            if (record.completionCount > 0)
                _discoveredRecipeIds.Add(record.recipeId);
            HashSet<string> tags = GetOrCreateRecipeTagSet(record.recipeId);
            AddIds(tags, record.knownTagIds);
        }

        private void SaveToPlayerPrefs()
        {
            if (saveToPlayerPrefs == false || string.IsNullOrWhiteSpace(playerPrefsKey))
                return;

            CookingKnowledgeSaveData saveData = new CookingKnowledgeSaveData
            {
                schemaVersion = CURRENT_SCHEMA_VERSION,
                recipeRecords = new List<KnownRecipeRecord>(_recipeRecords.Values),
                discoveredRecipeIds = new List<string>(_discoveredRecipeIds),
                knownPreparationEffectKeys = new List<string>(_knownPreparationEffectKeys),
                attemptedRecipeIds = new List<string>(_attemptedRecipeIds),
                triedIngredientIds = new List<string>(_triedIngredientIds),
                triedPreparationKeys = new List<string>(_triedPreparationKeys),
                knownRecipeTags = new List<KnownRecipeTagSaveData>(),
                knownRecipeVariants = new List<KnownRecipeVariantSaveData>()
            };
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
            if (string.IsNullOrWhiteSpace(recipeId))
                return false;
            GetOrCreateRecipeRecord(recipeId);
            if (_discoveredRecipeIds.Add(recipeId) == false)
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
            return string.IsNullOrWhiteSpace(recipeId) == false && _attemptedRecipeIds.Add(recipeId);
        }

        private bool RecordRecipeCompletion(
            DishResult result,
            NpcDishMatchReport report,
            IReadOnlyList<FoodTagSO> revealedTags)
        {
            string recipeId = CookingKnowledgeKeyUtility.GetRecipeId(result.BaseRecipe);
            if (string.IsNullOrWhiteSpace(recipeId))
                return false;

            KnownRecipeRecord recipeRecord = GetOrCreateRecipeRecord(recipeId);
            recipeRecord.completionCount++;
            if (result.CraftGrade > recipeRecord.bestCraftGrade)
                recipeRecord.bestCraftGrade = result.CraftGrade;
            UpdateGuestSummary(recipeRecord, report);

            if (result.IsVariant && result.VariantIdentity != null
                                 && string.IsNullOrWhiteSpace(result.VariantId) == false)
            {
                KnownRecipeVariantRecord variant = FindVariant(recipeRecord, result.VariantId);
                bool discovered = variant == null;
                if (discovered)
                {
                    variant = new KnownRecipeVariantRecord
                    {
                        variantId = CookingKnowledgeKeyUtility.NormalizeId(result.VariantId),
                        identityComponents = BuildComponentRecords(result.VariantIdentity.IdentityComponents),
                        replayComponents = BuildComponentRecords(result.VariantIdentity.ReplayComponents),
                        discoveryOrder = GetNextDiscoveryOrder(recipeRecord)
                    };
                    recipeRecord.variants.Add(variant);
                }

                variant.completionCount++;
                bool replaceReplay = variant.completionCount == 1 || result.CraftGrade > variant.bestCraftGrade;
                if (result.CraftGrade > variant.bestCraftGrade)
                    variant.bestCraftGrade = result.CraftGrade;
                if (replaceReplay)
                    variant.replayComponents = BuildComponentRecords(result.VariantIdentity.ReplayComponents);
                if (variant.identityComponents == null || variant.identityComponents.Count == 0)
                    variant.identityComponents = BuildComponentRecords(result.VariantIdentity.IdentityComponents);
                variant.hasBizarreObservation |= result.IsBizarre;
                variant.hasDangerousObservation |= result.IsDangerous;
                AddTagIds(variant.knownTagIds, revealedTags);
                _changedVariantId = variant.variantId;

                QueueUpdate(new CookingKnowledgeUpdate(
                    CookingKnowledgeUpdateType.RecipeVariantDiscovered,
                    discovered ? "변형 요리 발견" : "변형 요리 기록 갱신",
                    result.BaseRecipe.DisplayName,
                    result.BaseRecipe,
                    null,
                    null,
                    variant.variantId));
            }

            return true;
        }

        private bool AddRecipeTags(RecipeSO recipe, IReadOnlyList<FoodTagSO> tags)
        {
            string recipeId = CookingKnowledgeKeyUtility.GetRecipeId(recipe);
            if (string.IsNullOrWhiteSpace(recipeId) || tags == null)
                return false;

            HashSet<string> tagSet = GetOrCreateRecipeTagSet(recipeId);
            KnownRecipeRecord record = GetOrCreateRecipeRecord(recipeId);
            bool changed = false;
            for (int i = 0; i < tags.Count; i++)
            {
                string tagId = CookingKnowledgeKeyUtility.GetTagId(tags[i]);
                if (string.IsNullOrWhiteSpace(tagId))
                    continue;
                changed |= tagSet.Add(tagId);
                AddId(record.knownTagIds, tagId);
            }

            if (changed)
            {
                QueueUpdate(new CookingKnowledgeUpdate(
                    CookingKnowledgeUpdateType.RecipeTagsRevealed,
                    "유효 태그 기록",
                    recipe != null ? recipe.DisplayName : recipeId,
                    recipe,
                    null,
                    null,
                    _changedVariantId));
            }
            return changed;
        }

        private bool AddPreparationEffect(IngredientSO ingredient, IngredientPreparationOption option)
        {
            string key = CookingKnowledgeKeyUtility.BuildPreparationKey(ingredient, option);
            if (string.IsNullOrWhiteSpace(key) || _knownPreparationEffectKeys.Add(key) == false)
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
            return string.IsNullOrWhiteSpace(ingredientId) == false && _triedIngredientIds.Add(ingredientId);
        }

        private bool AddTriedPreparation(IngredientSO ingredient, IngredientPreparationOption option)
        {
            string key = CookingKnowledgeKeyUtility.BuildPreparationKey(ingredient, option);
            return string.IsNullOrWhiteSpace(key) == false && _triedPreparationKeys.Add(key);
        }

        private void UpdateGuestSummary(KnownRecipeRecord record, NpcDishMatchReport report)
        {
            string npcId = CookingKnowledgeKeyUtility.NormalizeId(report?.Order?.NpcId);
            if (string.IsNullOrWhiteSpace(npcId))
                return;
            NpcConversationResult result = report?.Evaluation?.Result ?? NpcConversationResult.Wrong;
            RecipeGuestSummaryRecord summary = null;
            for (int i = 0; i < record.guestSummaries.Count; i++)
            {
                if (string.Equals(record.guestSummaries[i].npcId, npcId, StringComparison.OrdinalIgnoreCase))
                {
                    summary = record.guestSummaries[i];
                    break;
                }
            }

            if (summary == null)
            {
                summary = new RecipeGuestSummaryRecord
                {
                    npcId = npcId,
                    bestResult = result,
                    lastResult = result
                };
                record.guestSummaries.Add(summary);
            }

            summary.serveCount++;
            if (GetReactionRank(result) > GetReactionRank(summary.bestResult))
                summary.bestResult = result;
            summary.lastResult = result;
        }

        private static int GetReactionRank(NpcConversationResult result)
        {
            switch (result)
            {
                case NpcConversationResult.Perfect: return 4;
                case NpcConversationResult.Correct: return 3;
                case NpcConversationResult.Similar: return 2;
                case NpcConversationResult.Wrong: return 1;
                case NpcConversationResult.Disgusting: return 0;
                default: return -1;
            }
        }

        private void MigrateLegacyVariants(IReadOnlyList<KnownRecipeVariantSaveData> entries)
        {
            if (entries == null)
                return;
            for (int entryIndex = 0; entryIndex < entries.Count; entryIndex++)
            {
                KnownRecipeVariantSaveData entry = entries[entryIndex];
                if (entry == null || string.IsNullOrWhiteSpace(entry.recipeId) || entry.variantKeys == null)
                    continue;
                RecipeSO recipe = FindRecipeById(entry.recipeId);
                KnownRecipeRecord record = GetOrCreateRecipeRecord(entry.recipeId);
                for (int keyIndex = 0; keyIndex < entry.variantKeys.Count; keyIndex++)
                {
                    string legacyKey = entry.variantKeys[keyIndex];
                    if (string.IsNullOrWhiteSpace(legacyKey))
                        continue;

                    KnownRecipeVariantRecord migrated;
                    if (TryResolveLegacyVariant(recipe, legacyKey, out migrated) == false)
                    {
                        migrated = new KnownRecipeVariantRecord
                        {
                            variantId = "legacy_" + ComputeStableHash(legacyKey),
                            legacyVariantKey = legacyKey,
                            discoveryOrder = GetNextDiscoveryOrder(record),
                            completionCount = 1
                        };
                    }

                    if (FindVariant(record, migrated.variantId) != null)
                        continue;
                    if (migrated.discoveryOrder <= 0)
                        migrated.discoveryOrder = GetNextDiscoveryOrder(record);
                    if (migrated.completionCount <= 0)
                        migrated.completionCount = 1;
                    record.variants.Add(migrated);
                    record.completionCount++;
                    _discoveredRecipeIds.Add(record.recipeId);
                }
            }
        }

        private bool TryResolveLegacyVariant(
            RecipeSO recipe,
            string legacyKey,
            out KnownRecipeVariantRecord record)
        {
            record = null;
            if (recipe == null || string.IsNullOrWhiteSpace(legacyKey))
                return false;
            string[] parts = legacyKey.Split('|');
            if (parts.Length < 2)
                return false;

            List<PreparedIngredientState> prepared = new List<PreparedIngredientState>();
            for (int i = 1; i < parts.Length; i++)
            {
                int separator = parts[i].LastIndexOf(':');
                if (separator <= 0)
                    return false;
                string ingredientId = parts[i].Substring(0, separator);
                string methodId = parts[i].Substring(separator + 1);
                IngredientSO ingredient = FindIngredientById(ingredientId);
                if (ingredient == null)
                    return false;

                IngredientPreparationOption option = null;
                if (string.Equals(methodId, "none", StringComparison.OrdinalIgnoreCase) == false)
                {
                    int optionMatches = 0;
                    for (int optionIndex = 0; optionIndex < ingredient.PreparationOptions.Count; optionIndex++)
                    {
                        IngredientPreparationOption candidate = ingredient.PreparationOptions[optionIndex];
                        if (candidate?.Method != null
                            && string.Equals(candidate.Method.MethodId, methodId, StringComparison.OrdinalIgnoreCase))
                        {
                            option = candidate;
                            optionMatches++;
                        }
                    }
                    if (optionMatches != 1)
                        return false;
                }
                prepared.Add(new PreparedIngredientState(ingredient, option));
            }

            RecipePreparedMatchResult match = recipe.MatchPreparedIngredients(prepared);
            if (match.Status != RecipeMatchStatus.Matched)
                return false;
            CookingVariantIdentity identity = CookingVariantIdentityBuilder.Build(recipe, match.Bindings);
            if (identity.IsVariant == false || string.IsNullOrWhiteSpace(identity.VariantId))
                return false;

            record = new KnownRecipeVariantRecord
            {
                variantId = identity.VariantId,
                identityComponents = BuildComponentRecords(identity.IdentityComponents),
                replayComponents = BuildComponentRecords(identity.ReplayComponents),
                completionCount = 1,
                bestCraftGrade = DishCraftGrade.Bad
            };
            return true;
        }

        private CookingRecipeKnowledgeSnapshot BuildRecipeSnapshot(
            RecipeSO recipe,
            KnownRecipeRecord record,
            bool discovered)
        {
            List<CookingRecipeVariantKnowledgeSnapshot> variants =
                new List<CookingRecipeVariantKnowledgeSnapshot>();
            for (int i = 0; i < record.variants.Count; i++)
            {
                KnownRecipeVariantRecord variant = record.variants[i];
                variants.Add(new CookingRecipeVariantKnowledgeSnapshot(
                    variant.variantId,
                    CopyComponents(variant.identityComponents),
                    CopyComponents(variant.replayComponents),
                    variant.completionCount,
                    variant.bestCraftGrade,
                    ResolveTags(variant.knownTagIds),
                    variant.discoveryOrder,
                    variant.hasBizarreObservation,
                    variant.hasDangerousObservation,
                    variant.legacyVariantKey));
            }
            variants.Sort((left, right) => right.DiscoveryOrder.CompareTo(left.DiscoveryOrder));

            List<RecipeGuestSummarySnapshot> guests = new List<RecipeGuestSummarySnapshot>();
            for (int i = 0; i < record.guestSummaries.Count; i++)
            {
                RecipeGuestSummaryRecord summary = record.guestSummaries[i];
                guests.Add(new RecipeGuestSummarySnapshot(
                    summary.npcId,
                    summary.serveCount,
                    summary.bestResult,
                    summary.lastResult));
            }
            guests.Sort((left, right) => string.Compare(left.NpcId, right.NpcId, StringComparison.OrdinalIgnoreCase));

            return new CookingRecipeKnowledgeSnapshot(
                recipe,
                discovered,
                record.completionCount,
                record.bestCraftGrade,
                ResolveTags(record.knownTagIds),
                variants,
                guests);
        }

        private void LoadRecordCollections(KnownRecipeRecord record)
        {
            if (record.knownTagIds == null)
                record.knownTagIds = new List<string>();
            if (record.variants == null)
                record.variants = new List<KnownRecipeVariantRecord>();
            if (record.guestSummaries == null)
                record.guestSummaries = new List<RecipeGuestSummaryRecord>();
        }

        private void NormalizeRecord(KnownRecipeRecord record)
        {
            record.recipeId = CookingKnowledgeKeyUtility.NormalizeId(record.recipeId);
            record.completionCount = Mathf.Max(0, record.completionCount);
            LoadRecordCollections(record);
            NormalizeIds(record.knownTagIds);
            for (int i = record.variants.Count - 1; i >= 0; i--)
            {
                KnownRecipeVariantRecord variant = record.variants[i];
                if (variant == null || string.IsNullOrWhiteSpace(variant.variantId))
                {
                    record.variants.RemoveAt(i);
                    continue;
                }
                variant.variantId = CookingKnowledgeKeyUtility.NormalizeId(variant.variantId);
                variant.completionCount = Mathf.Max(0, variant.completionCount);
                if (variant.identityComponents == null)
                    variant.identityComponents = new List<VariantComponentRecord>();
                if (variant.replayComponents == null)
                    variant.replayComponents = new List<VariantComponentRecord>();
                if (variant.knownTagIds == null)
                    variant.knownTagIds = new List<string>();
                NormalizeIds(variant.knownTagIds);
            }
            for (int i = record.guestSummaries.Count - 1; i >= 0; i--)
            {
                RecipeGuestSummaryRecord summary = record.guestSummaries[i];
                if (summary == null || string.IsNullOrWhiteSpace(summary.npcId))
                    record.guestSummaries.RemoveAt(i);
                else
                {
                    summary.npcId = CookingKnowledgeKeyUtility.NormalizeId(summary.npcId);
                    summary.serveCount = Mathf.Max(0, summary.serveCount);
                }
            }
        }

        private KnownRecipeRecord GetOrCreateRecipeRecord(string recipeId)
        {
            recipeId = CookingKnowledgeKeyUtility.NormalizeId(recipeId);
            if (_recipeRecords.TryGetValue(recipeId, out KnownRecipeRecord record))
                return record;
            record = new KnownRecipeRecord { recipeId = recipeId };
            _recipeRecords.Add(recipeId, record);
            return record;
        }

        private HashSet<string> GetOrCreateRecipeTagSet(string recipeId)
        {
            recipeId = CookingKnowledgeKeyUtility.NormalizeId(recipeId);
            if (_knownRecipeTagIds.TryGetValue(recipeId, out HashSet<string> tags))
                return tags;
            tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _knownRecipeTagIds.Add(recipeId, tags);
            return tags;
        }

        private static KnownRecipeVariantRecord FindVariant(KnownRecipeRecord record, string variantId)
        {
            if (record?.variants == null || string.IsNullOrWhiteSpace(variantId))
                return null;
            for (int i = 0; i < record.variants.Count; i++)
            {
                if (string.Equals(record.variants[i].variantId, variantId, StringComparison.OrdinalIgnoreCase))
                    return record.variants[i];
            }
            return null;
        }

        private static int GetNextDiscoveryOrder(KnownRecipeRecord record)
        {
            int max = 0;
            for (int i = 0; i < record.variants.Count; i++)
                max = Math.Max(max, record.variants[i].discoveryOrder);
            return max + 1;
        }

        private static List<VariantComponentRecord> BuildComponentRecords(
            IReadOnlyList<CookingVariantComponent> components)
        {
            List<VariantComponentRecord> records = new List<VariantComponentRecord>();
            if (components == null)
                return records;
            for (int i = 0; i < components.Count; i++)
            {
                CookingVariantComponent component = components[i];
                if (component == null)
                    continue;
                records.Add(new VariantComponentRecord
                {
                    requirementId = component.RequirementId,
                    ingredientId = component.Ingredient != null ? component.Ingredient.IngredientId : string.Empty,
                    preparationOptionId = component.PreparationOption != null
                        ? component.PreparationOption.PreparationOptionId
                        : string.Empty,
                    variantEffectId = component.VariantEffectId,
                    kind = component.Kind
                });
            }
            return records;
        }

        private static List<VariantComponentRecord> CopyComponents(IReadOnlyList<VariantComponentRecord> source)
        {
            List<VariantComponentRecord> copy = new List<VariantComponentRecord>();
            if (source == null)
                return copy;
            for (int i = 0; i < source.Count; i++)
            {
                VariantComponentRecord component = source[i];
                if (component == null)
                    continue;
                copy.Add(new VariantComponentRecord
                {
                    requirementId = component.requirementId,
                    ingredientId = component.ingredientId,
                    preparationOptionId = component.preparationOptionId,
                    variantEffectId = component.variantEffectId,
                    kind = component.kind
                });
            }
            return copy;
        }

        private static void AddTagIds(List<string> target, IReadOnlyList<FoodTagSO> tags)
        {
            if (target == null || tags == null)
                return;
            for (int i = 0; i < tags.Count; i++)
                AddId(target, CookingKnowledgeKeyUtility.GetTagId(tags[i]));
        }

        private static bool AddId(List<string> target, string value)
        {
            value = CookingKnowledgeKeyUtility.NormalizeId(value);
            if (target == null || string.IsNullOrWhiteSpace(value))
                return false;
            for (int i = 0; i < target.Count; i++)
            {
                if (string.Equals(target[i], value, StringComparison.OrdinalIgnoreCase))
                    return false;
            }
            target.Add(value);
            return true;
        }

        private static void NormalizeIds(List<string> ids)
        {
            if (ids == null)
                return;
            HashSet<string> normalized = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < ids.Count; i++)
            {
                string id = CookingKnowledgeKeyUtility.NormalizeId(ids[i]);
                if (string.IsNullOrWhiteSpace(id) == false)
                    normalized.Add(id);
            }
            ids.Clear();
            ids.AddRange(normalized);
        }

        private List<FoodTagSO> ResolveTags(IEnumerable<string> tagIds)
        {
            List<FoodTagSO> tags = new List<FoodTagSO>();
            if (tagIds == null)
                return tags;
            foreach (string tagId in tagIds)
            {
                FoodTagSO tag = FindTagById(tagId);
                if (tag != null)
                    tags.Add(tag);
            }
            tags.Sort((left, right) => string.Compare(left.DisplayName, right.DisplayName, StringComparison.CurrentCulture));
            return tags;
        }

        private RecipeSO FindRecipeById(string recipeId)
        {
            recipeId = CookingKnowledgeKeyUtility.NormalizeId(recipeId);
            if (catalog == null)
                return null;
            for (int i = 0; i < catalog.Recipes.Count; i++)
            {
                RecipeSO recipe = catalog.Recipes[i];
                if (recipe != null && string.Equals(recipe.RecipeId, recipeId, StringComparison.OrdinalIgnoreCase))
                    return recipe;
            }
            return null;
        }

        private IngredientSO FindIngredientById(string ingredientId)
        {
            ingredientId = CookingKnowledgeKeyUtility.NormalizeId(ingredientId);
            if (catalog == null)
                return null;
            for (int i = 0; i < catalog.Ingredients.Count; i++)
            {
                IngredientSO ingredient = catalog.Ingredients[i];
                if (ingredient != null
                    && string.Equals(ingredient.IngredientId, ingredientId, StringComparison.OrdinalIgnoreCase))
                    return ingredient;
            }
            return null;
        }

        private FoodTagSO FindTagById(string tagId)
        {
            tagId = CookingKnowledgeKeyUtility.NormalizeId(tagId);
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

        private static IReadOnlyList<FoodTagSO> BuildRevealedResultTags(
            DishResult result,
            NpcDishMatchReport report)
        {
            if (result == null || report == null)
                return Array.Empty<FoodTagSO>();
            HashSet<string> revealedTagIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            AddIds(revealedTagIds, report.MatchedRequiredTags);
            AddIds(revealedTagIds, report.MatchedPreferredTags);
            AddIds(revealedTagIds, report.MatchedAvoidTags);
            AddIds(revealedTagIds, report.MatchedDisgustingTags);

            List<FoodTagSO> tags = new List<FoodTagSO>();
            for (int i = 0; i < result.Tags.Count; i++)
            {
                FoodTagSO tag = result.Tags[i];
                string tagId = CookingKnowledgeKeyUtility.GetTagId(tag);
                if (string.IsNullOrWhiteSpace(tagId) == false && revealedTagIds.Contains(tagId))
                    tags.Add(tag);
            }
            return tags;
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
            Bus<CookingKnowledgeChangedEvent>.Raise(new CookingKnowledgeChangedEvent(this, _changedVariantId));
            _changedVariantId = string.Empty;
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

        private static void AddIds(List<string> target, IReadOnlyList<string> ids)
        {
            if (target == null || ids == null)
                return;
            for (int i = 0; i < ids.Count; i++)
                AddId(target, ids[i]);
        }

        private static string ComputeStableHash(string value)
        {
            unchecked
            {
                const ulong offset = 14695981039346656037UL;
                const ulong prime = 1099511628211UL;
                ulong hash = offset;
                string source = value ?? string.Empty;
                for (int i = 0; i < source.Length; i++)
                {
                    hash ^= char.ToLowerInvariant(source[i]);
                    hash *= prime;
                }
                return hash.ToString("x16");
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
