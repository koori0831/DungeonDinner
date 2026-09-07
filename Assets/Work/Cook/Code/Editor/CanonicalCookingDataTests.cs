#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Work.Cook.Code.Data;
using Work.Cook.Code.Runtime.Core;
using Work.Cook.Code.Runtime.Systems;
using Work.NPC.Code.Data;
using Work.NPC.Code.Runtime;

namespace Work.Cook.Code.Editor.Tests
{
    public sealed class CanonicalCookingDataTests
    {
        private const string CatalogPath = "Assets/Work/Cook/SO/CookingDataCatalog.asset";
        private readonly List<UnityEngine.Object> created = new List<UnityEngine.Object>();
        private readonly List<string> saveKeys = new List<string>();

        [TearDown]
        public void TearDown()
        {
            for (int i = created.Count - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(created[i]);
            foreach (string key in saveKeys)
                PlayerPrefs.DeleteKey(key);
        }

        [Test]
        public void Catalog_ContainsOnlyThreeCanonicalMenusAndSixRealIngredients()
        {
            CookingDataCatalogSO catalog = LoadCatalog();
            Assert.That(catalog.Recipes.Select(r => r.RecipeId), Is.EquivalentTo(new[]
            {
                "mushroom_soup_pane", "slime_nucleus_dango", "corn_cheese_fondue"
            }));
            Assert.That(catalog.Ingredients.Select(i => i.IngredientId), Is.EquivalentTo(new[]
            {
                "mushroom_cap", "flat_mushroom", "slime_mucus", "slime_nucleus", "rock_salt", "corn_cheese"
            }));
            var report = new CookingDataValidationService().ValidateCatalog(catalog);
            Assert.That(report.HasErrors, Is.False, string.Join("\n", report.Issues.Select(i => i.Message)));
        }

        [Test]
        public void Fondue_RequiresMeltedCheeseMushroomPiecesAndBoiledSlime()
        {
            CookingDataCatalogSO catalog = LoadCatalog();
            RecipeSO recipe = catalog.Recipes.FirstOrDefault(r => r.RecipeId == "corn_cheese_fondue");
            Assert.That(recipe, Is.Not.Null, "The authored fondue recipe must be registered.");
            var prepared = new List<PreparedIngredientState>
            {
                Prepare(catalog, "corn_cheese", "roast"),
                Prepare(catalog, "flat_mushroom", "slicing"),
                Prepare(catalog, "slime_mucus", "boil_water")
            };
            Assert.That(recipe.MatchesPreparedIngredients(prepared), Is.True);
            Assert.That(catalog.Recipes.Count(r => r.MatchesPreparedIngredients(prepared)), Is.EqualTo(1));
            for (int i = 0; i < prepared.Count; i++)
            {
                var missing = new List<PreparedIngredientState>(prepared);
                missing.RemoveAt(i);
                Assert.That(recipe.MatchesPreparedIngredients(missing), Is.False, "All three ingredients are defining.");
                var raw = new List<PreparedIngredientState>(prepared);
                raw[i] = Prepare(catalog, prepared[i].Ingredient.IngredientId, "just");
                Assert.That(recipe.MatchesPreparedIngredients(raw), Is.False, "The recipe must require the authored preparation.");
            }
        }

        [Test]
        public void NpcOrders_ReferenceCanonicalRecipesCategoriesAndTags()
        {
            CookingDataCatalogSO catalog = LoadCatalog();
            NpcConversationDatabase database = NpcConversationDatabase.LoadFromResources("NPCData");
            var report = NpcDataValidator.Validate(database);
            Assert.That(report.HasErrors, Is.False, report.ToString());
            var recipeIds = new HashSet<string>(catalog.Recipes.Select(r => r.RecipeId));
            var tags = new HashSet<string>(catalog.Tags.Select(t => t.TagId));
            var categories = new HashSet<string>(catalog.Categories.Select(c => c.CategoryId));
            foreach (var order in database.VisitEvents.Values)
            {
                if (!string.IsNullOrWhiteSpace(order.CorrectRecipeId))
                    Assert.That(recipeIds.Contains(order.CorrectRecipeId), Is.True, order.EventId);
                foreach (string id in order.RequiredTags.Concat(order.PreferredTags).Concat(order.AvoidTags).Concat(order.DisgustingTags))
                    Assert.That(tags.Contains(id), Is.True, order.EventId + ": " + id);
                foreach (string id in order.AllowedFoodTypes)
                    Assert.That(categories.Contains(id), Is.True, order.EventId + ": " + id);
            }
        }

        [Test]
        public void OdinRepeatOrders_CoverEachCanonicalMenu()
        {
            NpcConversationDatabase database = NpcConversationDatabase.LoadFromResources("NPCData");
            var expectedRecipesByEvent = new Dictionary<string, string>
            {
                ["Odin_Normal_DangoCraving"] = "slime_nucleus_dango",
                ["Odin_Normal_MushroomSoup"] = "mushroom_soup_pane",
                ["Odin_Normal_WarmComfortSoup"] = "corn_cheese_fondue"
            };

            foreach (var expectation in expectedRecipesByEvent)
            {
                Assert.That(database.TryGetVisitEvent(expectation.Key, out var visitEvent), Is.True);
                Assert.That(visitEvent.RepeatMode, Is.EqualTo(VisitEventRepeatMode.Cycle));
                Assert.That(visitEvent.CorrectRecipeId, Is.EqualTo(expectation.Value));
            }

            Assert.That(expectedRecipesByEvent.Values.Distinct().ToArray(), Has.Length.EqualTo(3));
        }

        [Test]
        public void RenamedSoup_LoadsOldCompletionTagsGuestsAndRekeysVariant()
        {
            CookingDataCatalogSO source = LoadCatalog();
            RecipeSO oldRecipe = Clone(AssetDatabase.LoadAssetAtPath<RecipeSO>(
                AssetDatabase.GUIDToAssetPath("e96f7cf5c4f6b4d44b472112d8b9cc83")));
            Set(oldRecipe, "recipeId", "NewRecipe");
            RecipeSO recipe = Clone(oldRecipe);
            Set(recipe, "recipeId", "mushroom_soup_pane");
            var prepared = new[]
            {
                Prepare(source, "mushroom_cap", "just"), Prepare(source, "flat_mushroom", "slicing"),
                Prepare(source, "slime_mucus", "boil_water")
            };
            var oldIdentity = CookingVariantIdentityBuilder.Build(oldRecipe, oldRecipe.MatchPreparedIngredients(prepared).Bindings);
            var identity = CookingVariantIdentityBuilder.Build(recipe, recipe.MatchPreparedIngredients(prepared).Bindings);
            Assert.That(oldIdentity.IsVariant, Is.True);
            Assert.That(oldIdentity.VariantId, Is.Not.EqualTo(identity.VariantId));
            var record = new KnownRecipeRecord
            {
                recipeId = "NewRecipe", completionCount = 4, bestCraftGrade = DishCraftGrade.Perfect,
                knownTagIds = new List<string> { "warm" },
                guestSummaries = new List<RecipeGuestSummaryRecord> { new RecipeGuestSummaryRecord { npcId = "Odin", serveCount = 4 } },
                variants = new List<KnownRecipeVariantRecord> { new KnownRecipeVariantRecord
                {
                    variantId = oldIdentity.VariantId, completionCount = 3, discoveryOrder = 1,
                    identityComponents = Components(oldIdentity.IdentityComponents),
                    replayComponents = Components(oldIdentity.ReplayComponents)
                } }
            };
            string key = NewSaveKey();
            PlayerPrefs.SetString(key, JsonUtility.ToJson(new SaveFixture { recipeRecords = new List<KnownRecipeRecord> { record } }));
            CookingDataCatalogSO catalog = Clone(source);
            Set(catalog, "recipes", new List<RecipeSO> { recipe });
            CookingKnowledgeStore store = CreateStore(catalog, key);
            var knowledge = store.GetRecipeKnowledge(recipe);
            Assert.That(knowledge.IsDiscovered, Is.True);
            Assert.That(knowledge.CompletionCount, Is.EqualTo(4));
            Assert.That(knowledge.KnownTags.Select(t => t.TagId), Does.Contain("warm"));
            Assert.That(knowledge.GuestSummaries.Single().ServeCount, Is.EqualTo(4));
            Assert.That(knowledge.Variants.Single().VariantId, Is.EqualTo(identity.VariantId));
            Assert.That(knowledge.Variants.Single().CompletionCount, Is.EqualTo(3));
            Assert.That(knowledge.Variants.Single().ReplayComponents, Has.Count.EqualTo(3));
            typeof(CookingKnowledgeStore).GetMethod("SaveToPlayerPrefs", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(store, null);
            Assert.That(PlayerPrefs.GetString(key), Does.Not.Contain("NewRecipe"));
            Assert.That(CreateStore(catalog, key).GetRecipeKnowledge(recipe).Variants.Single().VariantId, Is.EqualTo(identity.VariantId));
        }

        [Test]
        public void RenamedSoup_LoadsVersionlessDiscoveredAndAttemptedIds()
        {
            CookingDataCatalogSO source = LoadCatalog();
            RecipeSO recipe = Clone(AssetDatabase.LoadAssetAtPath<RecipeSO>(AssetDatabase.GUIDToAssetPath("e96f7cf5c4f6b4d44b472112d8b9cc83")));
            Set(recipe, "recipeId", "mushroom_soup_pane");
            CookingDataCatalogSO catalog = Clone(source);
            Set(catalog, "recipes", new List<RecipeSO> { recipe });
            string key = NewSaveKey();
            PlayerPrefs.SetString(key, "{\"discoveredRecipeIds\":[\"NewRecipe\"],\"attemptedRecipeIds\":[\"NewRecipe\"],\"knownRecipeTags\":[{\"recipeId\":\"NewRecipe\",\"tagIds\":[\"warm\"]}]}");
            var knowledge = CreateStore(catalog, key).GetRecipeKnowledge(recipe);
            Assert.That(knowledge.IsDiscovered, Is.True);
            Assert.That(knowledge.KnownTags.Select(t => t.TagId), Does.Contain("warm"));
            Assert.That(PlayerPrefs.GetString(key), Does.Not.Contain("NewRecipe"));
        }

        private CookingKnowledgeStore CreateStore(CookingDataCatalogSO catalog, string key)
        {
            var go = new GameObject("CanonicalCookingDataTests");
            go.SetActive(false);
            created.Add(go);
            var store = go.AddComponent<CookingKnowledgeStore>();
            Set(store, "playerPrefsKey", key);
            store.Initialize(catalog);
            return store;
        }

        private string NewSaveKey()
        {
            string key = "DungeonDinner.Tests.CanonicalData." + Guid.NewGuid().ToString("N");
            saveKeys.Add(key);
            return key;
        }

        private T Clone<T>(T asset) where T : UnityEngine.Object
        {
            T clone = UnityEngine.Object.Instantiate(asset);
            created.Add(clone);
            return clone;
        }

        private static CookingDataCatalogSO LoadCatalog() => AssetDatabase.LoadAssetAtPath<CookingDataCatalogSO>(CatalogPath);
        private static PreparedIngredientState Prepare(CookingDataCatalogSO catalog, string id, string optionId)
        {
            IngredientSO ingredient = catalog.Ingredients.Single(i => i.IngredientId == id);
            return new PreparedIngredientState(ingredient, ingredient.PreparationOptions.Single(o => o.PreparationOptionId == optionId));
        }

        private static List<VariantComponentRecord> Components(IReadOnlyList<CookingVariantComponent> components) => components.Select(c =>
            new VariantComponentRecord { requirementId = c.RequirementId, ingredientId = c.Ingredient.IngredientId,
                preparationOptionId = c.PreparationOption?.PreparationOptionId ?? string.Empty, variantEffectId = c.VariantEffectId, kind = c.Kind }).ToList();
        private static void Set(object target, string field, object value) => target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);

        [Serializable]
        private sealed class SaveFixture
        {
            public int schemaVersion = 2;
            public List<KnownRecipeRecord> recipeRecords;
        }
    }
}
#endif
