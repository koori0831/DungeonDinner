#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Work.Cook.Code.Data;
using Work.Cook.Code.Runtime.Core;
using Work.Cook.Code.Runtime.Systems;
using Work.Cook.Code.Runtime.UI;
using Work.NPC.Code.Data;
using Work.NPC.Code.Runtime;

namespace Work.Cook.Code.Editor.Tests
{
    public sealed class CookingDomainRuleTests
    {
        private readonly List<UnityEngine.Object> _createdObjects = new List<UnityEngine.Object>();
        private readonly List<string> _playerPrefsKeys = new List<string>();

        [TearDown]
        public void TearDown()
        {
            for (int i = _createdObjects.Count - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(_createdObjects[i]);
            _createdObjects.Clear();
            for (int i = 0; i < _playerPrefsKeys.Count; i++)
                PlayerPrefs.DeleteKey(_playerPrefsKeys[i]);
            if (_playerPrefsKeys.Count > 0)
                PlayerPrefs.Save();
            _playerPrefsKeys.Clear();
        }

        [Test]
        public void SelectedRecipe_DoesNotOverrideActualPreparedIngredients()
        {
            IngredientSO required = CreateIngredient("required");
            IngredientSO wrong = CreateIngredient("wrong");
            RecipeSO recipe = CreateRecipe("recipe", Requirement(required, true));
            CookingSession session = CookingSession.CreateForRecipe(recipe, new[] { wrong });
            session.SelectPreparation(wrong, null);

            RecipeMatchResult result = new RecipeMatcher(new StubDataProvider(recipe)).Match(session);

            Assert.That(result.IsMatched, Is.False);
            Assert.That(result.TargetRecipe, Is.SameAs(recipe));
        }

        [Test]
        public void IngredientOutsideAuthoredSlots_MakesRecipeUnformed()
        {
            IngredientSO required = CreateIngredient("required");
            IngredientSO extra = CreateIngredient("extra");
            RecipeSO recipe = CreateRecipe("recipe", Requirement(required, true));
            CookingSession session = CookingSession.CreateForDirectIngredients(new[] { required, extra });
            session.SelectPreparation(required, null);
            session.SelectPreparation(extra, null);

            RecipeMatchResult result = new RecipeMatcher(new StubDataProvider(recipe)).Match(session);

            Assert.That(result.IsMatched, Is.False);
        }

        [Test]
        public void OptionalAuthoredSlot_AcceptsNarrowAdditionalIngredient()
        {
            IngredientSO required = CreateIngredient("required");
            IngredientSO optional = CreateIngredient("optional");
            RecipeSO recipe = CreateRecipe(
                "recipe",
                Requirement(required, true),
                Requirement(optional, false));
            CookingSession session = CookingSession.CreateForDirectIngredients(new[] { required, optional });
            session.SelectPreparation(required, null);
            session.SelectPreparation(optional, null);

            RecipeMatchResult result = new RecipeMatcher(new StubDataProvider(recipe)).Match(session);

            Assert.That(result.Recipe, Is.SameAs(recipe));
            Assert.That(result.IsVariant, Is.True);
        }

        [Test]
        public void DuplicateIngredientOccurrences_KeepSeparatePreparationRecords()
        {
            IngredientSO ingredient = CreateIngredient("duplicate");
            CookingSession session = CookingSession.CreateForDirectIngredients(new[] { ingredient, ingredient });

            session.SelectPreparation(ingredient, null);
            Assert.That(session.IsEveryIngredientPrepared(), Is.False);
            session.SelectPreparation(ingredient, null);

            Assert.That(session.PreparedIngredients.Count, Is.EqualTo(2));
            Assert.That(session.IsEveryIngredientPrepared(), Is.True);
        }

        [Test]
        public void EqualHighestScoreRecipes_AreAmbiguous()
        {
            IngredientSO ingredient = CreateIngredient("shared");
            RecipeSO first = CreateRecipe("first", Requirement(ingredient, true, "slot_first"));
            RecipeSO second = CreateRecipe("second", Requirement(ingredient, true, "slot_second"));
            CookingSession session = CookingSession.CreateForDirectIngredients(new[] { ingredient });
            session.SelectPreparation(ingredient, null);

            RecipeMatchResult result = new RecipeMatcher(new StubDataProvider(first, second)).Match(session);

            Assert.That(result.Status, Is.EqualTo(RecipeMatchStatus.Ambiguous));
            Assert.That(result.Recipe, Is.Null);
        }

        [Test]
        public void DifferentMeaningfulSlotBindings_AreAmbiguous()
        {
            PreparationMethodSO firstMethod = CreateMethod("first_method");
            PreparationMethodSO secondMethod = CreateMethod("second_method");
            IngredientPreparationOption firstOption = CreateOption("first_option", firstMethod, qualityDelta: 1);
            IngredientPreparationOption secondOption = CreateOption("second_option", secondMethod, qualityDelta: 2);
            IngredientSO ingredient = CreateIngredient("shared", firstOption, secondOption);
            RecipeIngredientRequirement firstSlot = Requirement(ingredient, true, "slot_a");
            RecipeIngredientRequirement secondSlot = Requirement(ingredient, true, "slot_b");
            SetField(firstSlot, "requiredPreparationMethods", new List<PreparationMethodSO> { firstMethod, secondMethod });
            SetField(secondSlot, "requiredPreparationMethods", new List<PreparationMethodSO> { firstMethod, secondMethod });
            RecipeSO recipe = CreateRecipe("ambiguous_slots", firstSlot, secondSlot);

            RecipePreparedMatchResult match = recipe.MatchPreparedIngredients(new[]
            {
                new PreparedIngredientState(ingredient, firstOption),
                new PreparedIngredientState(ingredient, secondOption)
            });

            Assert.That(match.Status, Is.EqualTo(RecipeMatchStatus.Ambiguous));
        }

        [Test]
        public void QualityOnlyPreparation_DoesNotSplitAlternativeVariantIdentity()
        {
            PreparationMethodSO firstMethod = CreateMethod("low_quality");
            PreparationMethodSO secondMethod = CreateMethod("high_quality");
            IngredientPreparationOption firstOption = CreateOption("low_quality_option", firstMethod, qualityDelta: -1);
            IngredientPreparationOption secondOption = CreateOption("high_quality_option", secondMethod, qualityDelta: 2);
            IngredientSO canonical = CreateIngredient("canonical");
            IngredientSO alternative = CreateIngredient("alternative", firstOption, secondOption);
            RecipeIngredientRequirement slot = Requirement(canonical, true, "slot_main");
            SetField(slot, "alternatives", new List<IngredientSO> { alternative });
            RecipeSO recipe = CreateRecipe("quality_variant", slot);

            CookingVariantIdentity first = BuildVariantIdentity(recipe, alternative, firstOption, null);
            CookingVariantIdentity second = BuildVariantIdentity(recipe, alternative, secondOption, null);

            Assert.That(first.IsVariant, Is.True);
            Assert.That(second.VariantId, Is.EqualTo(first.VariantId));
            Assert.That(first.IdentityComponents[0].PreparationOption, Is.Null);
            Assert.That(first.ReplayComponents[0].PreparationOption, Is.SameAs(firstOption));
            Assert.That(second.ReplayComponents[0].PreparationOption, Is.SameAs(secondOption));
        }

        [Test]
        public void SameAuthoredFeedbackEffectAcrossGrades_UsesOneVariantIdentity()
        {
            CookingMiniGameFeedbackRule goodRule = CreateFeedbackRule(
                CookingMiniGameGrade.Good,
                "effect_precise",
                "정교한");
            CookingMiniGameFeedbackRule perfectRule = CreateFeedbackRule(
                CookingMiniGameGrade.Perfect,
                "effect_precise",
                "정교한");
            PreparationMethodSO method = CreateMethod("slice", CookingMiniGameType.Slicing);
            IngredientPreparationOption option = CreateOption(
                "slice_option",
                method,
                feedbackRules: new[] { goodRule, perfectRule });
            IngredientSO ingredient = CreateIngredient("ingredient", option);
            RecipeSO recipe = CreateRecipe("feedback_variant", Requirement(ingredient, true, "slot_main"));

            CookingVariantIdentity good = BuildVariantIdentity(
                recipe,
                ingredient,
                option,
                new CookingMiniGameResult(CookingMiniGameType.Slicing, CookingMiniGameGrade.Good, 0.8f, 1, string.Empty));
            CookingVariantIdentity perfect = BuildVariantIdentity(
                recipe,
                ingredient,
                option,
                new CookingMiniGameResult(CookingMiniGameType.Slicing, CookingMiniGameGrade.Perfect, 1f, 3, string.Empty));

            Assert.That(good.IsVariant, Is.True);
            Assert.That(perfect.VariantId, Is.EqualTo(good.VariantId));
            Assert.That(good.IdentityComponents[0].VariantEffectId, Is.EqualTo("effect_precise"));
        }

        [Test]
        public void BaseStartPlan_DoesNotAutoSelectAlternativeWhenCanonicalIsMissing()
        {
            IngredientSO canonical = CreateIngredient("canonical");
            IngredientSO alternative = CreateIngredient("alternative");
            RecipeIngredientRequirement slot = Requirement(canonical, true, "slot_main");
            SetField(slot, "alternatives", new List<IngredientSO> { alternative });
            RecipeSO recipe = CreateRecipe("recipe", slot);
            CookingRecipeStartPlanBuilder builder = new CookingRecipeStartPlanBuilder(
                new[] { canonical, alternative },
                ingredient => ingredient == alternative ? 2 : 0,
                null);

            CookingRecipeStartPlan plan = builder.BuildBase(recipe);

            Assert.That(plan.Candidates, Does.Contain(canonical));
            Assert.That(plan.Candidates, Does.Contain(alternative));
            Assert.That(plan.PresetIngredients, Is.Empty);
            Assert.That(plan.Shortages.Count, Is.EqualTo(1));
            Assert.That(plan.Shortages[0].Ingredient, Is.SameAs(canonical));
            Assert.That(plan.IsSelectionValid(new[] { alternative }, _ => 2, out _), Is.True);
        }

        [Test]
        public void StartPlan_RejectsSelectionThatExceedsOwnedDuplicateQuantity()
        {
            IngredientSO ingredient = CreateIngredient("duplicate");
            RecipeIngredientRequirement slot = Requirement(ingredient, true, "slot_main", 2, 2);
            RecipeSO recipe = CreateRecipe("duplicate_recipe", slot);
            CookingRecipeStartPlan plan = new CookingRecipeStartPlanBuilder(
                new[] { ingredient },
                _ => 1,
                null).BuildBase(recipe);

            bool valid = plan.IsSelectionValid(
                new[] { ingredient, ingredient },
                _ => 1,
                out string reason);

            Assert.That(valid, Is.False);
            Assert.That(reason, Does.Contain("보유 1"));
        }

        [Test]
        public void VariantStartPlan_KeepsMissingOptionalReplayComponentRequired()
        {
            IngredientSO canonical = CreateIngredient("canonical");
            IngredientSO garnish = CreateIngredient("garnish");
            RecipeSO recipe = CreateRecipe(
                "variant_recipe",
                Requirement(canonical, true, "slot_main"),
                Requirement(garnish, false, "slot_garnish"));
            CookingRecipeVariantKnowledgeSnapshot variant = new CookingRecipeVariantKnowledgeSnapshot(
                "variant_optional",
                new[]
                {
                    new VariantComponentRecord
                    {
                        requirementId = "slot_garnish",
                        ingredientId = garnish.IngredientId,
                        kind = VariantComponentKind.Optional
                    }
                },
                new[]
                {
                    new VariantComponentRecord
                    {
                        requirementId = "slot_main",
                        ingredientId = canonical.IngredientId,
                        kind = VariantComponentKind.Canonical
                    },
                    new VariantComponentRecord
                    {
                        requirementId = "slot_garnish",
                        ingredientId = garnish.IngredientId,
                        kind = VariantComponentKind.Optional
                    }
                },
                1,
                DishCraftGrade.Good,
                Array.Empty<FoodTagSO>(),
                1,
                false,
                false,
                string.Empty);
            CookingRecipeStartPlan plan = new CookingRecipeStartPlanBuilder(
                new[] { canonical, garnish },
                ingredient => ingredient == canonical ? 1 : 0,
                null).BuildVariant(recipe, variant);

            Assert.That(plan.GetRequiredQuantity(garnish), Is.EqualTo(1));
            Assert.That(plan.Shortages, Has.Count.EqualTo(1));
            Assert.That(plan.IsSelectionValid(new[] { canonical }, _ => 1, out string reason), Is.False);
            Assert.That(reason, Does.Contain("1개 부족"));
        }

        [Test]
        public void KnowledgeStore_AccumulatesOncePerSessionAndKeepsVariantBestReplay()
        {
            FoodTagSO tag = CreateTag("savory");
            PreparationMethodSO firstMethod = CreateMethod("rough");
            PreparationMethodSO bestMethod = CreateMethod("precise");
            IngredientPreparationOption firstOption = CreateOption("rough_option", firstMethod, qualityDelta: -1);
            IngredientPreparationOption bestOption = CreateOption("precise_option", bestMethod, qualityDelta: 2);
            IngredientSO canonical = CreateIngredient("canonical");
            IngredientSO alternative = CreateIngredient("alternative", firstOption, bestOption);
            RecipeIngredientRequirement slot = Requirement(canonical, true, "slot_main");
            SetField(slot, "alternatives", new List<IngredientSO> { alternative });
            RecipeSO recipe = CreateRecipe("recipe", slot);
            CookingDataCatalogSO catalog = CreateCatalog(
                new[] { canonical, alternative },
                new[] { recipe },
                new[] { tag },
                new[] { firstMethod, bestMethod });
            CookingKnowledgeStore store = CreateKnowledgeStore(catalog, false, false, string.Empty);

            CookingVariantIdentity firstIdentity = BuildVariantIdentity(recipe, alternative, firstOption, null);
            NpcDishMatchReport report = CreateMatchReport(recipe.RecipeId, "guest", tag.TagId, DishCraftGrade.Good);
            DishResult firstResult = CreateDishResult(
                recipe,
                tag,
                firstIdentity,
                firstOption,
                "session_one",
                DishCraftGrade.Good,
                DishOddity.Bizarre);

            Assert.That(store.LearnFromService(firstResult, report), Is.True);
            Assert.That(store.LearnFromService(firstResult, report), Is.False);

            CookingVariantIdentity bestIdentity = BuildVariantIdentity(recipe, alternative, bestOption, null);
            Assert.That(bestIdentity.VariantId, Is.EqualTo(firstIdentity.VariantId));
            DishResult bestResult = CreateDishResult(
                recipe,
                tag,
                bestIdentity,
                bestOption,
                "session_two",
                DishCraftGrade.Perfect,
                DishOddity.Normal);
            Assert.That(store.LearnFromService(bestResult, report), Is.True);

            CookingRecipeKnowledgeSnapshot knowledge = store.GetRecipeKnowledge(recipe);
            Assert.That(knowledge.CompletionCount, Is.EqualTo(2));
            Assert.That(knowledge.BestCraftGrade, Is.EqualTo(DishCraftGrade.Perfect));
            Assert.That(knowledge.KnownTags, Has.Count.EqualTo(1));
            Assert.That(knowledge.GuestSummaries, Has.Count.EqualTo(1));
            Assert.That(knowledge.GuestSummaries[0].ServeCount, Is.EqualTo(2));
            Assert.That(knowledge.Variants, Has.Count.EqualTo(1));
            Assert.That(knowledge.Variants[0].CompletionCount, Is.EqualTo(2));
            Assert.That(knowledge.Variants[0].BestCraftGrade, Is.EqualTo(DishCraftGrade.Perfect));
            Assert.That(knowledge.Variants[0].HasBizarreObservation, Is.True);
            Assert.That(knowledge.Variants[0].ReplayComponents[0].preparationOptionId, Is.EqualTo("precise_option"));
        }

        [Test]
        public void VersionlessKnowledge_MigratesResolvableAndUnresolvableVariantsToV2()
        {
            PreparationMethodSO method = CreateMethod("cut");
            IngredientPreparationOption option = CreateOption("cut_option", method, resultNameModifier: "썬");
            IngredientSO canonical = CreateIngredient("canonical");
            IngredientSO alternative = CreateIngredient("alternative", option);
            RecipeIngredientRequirement slot = Requirement(canonical, true, "slot_main");
            SetField(slot, "alternatives", new List<IngredientSO> { alternative });
            RecipeSO recipe = CreateRecipe("recipe", slot);
            CookingDataCatalogSO catalog = CreateCatalog(
                new[] { canonical, alternative },
                new[] { recipe },
                preparationMethods: new[] { method });
            string key = "DungeonDinner.Tests.CookingKnowledge." + Guid.NewGuid().ToString("N");
            _playerPrefsKeys.Add(key);
            PlayerPrefs.SetString(
                key,
                "{\"discoveredRecipeIds\":[\"recipe\"],\"knownRecipeVariants\":[{\"recipeId\":\"recipe\",\"variantKeys\":[\"recipe|alternative:cut\",\"recipe|missing:cut\"]}]}" );
            PlayerPrefs.Save();

            CookingKnowledgeStore store = CreateKnowledgeStore(catalog, true, true, key);
            CookingRecipeKnowledgeSnapshot knowledge = store.GetRecipeKnowledge(recipe);

            Assert.That(knowledge.IsDiscovered, Is.True);
            Assert.That(knowledge.Variants, Has.Count.EqualTo(2));
            Assert.That(CountReplayable(knowledge.Variants), Is.EqualTo(1));
            Assert.That(CountLegacyUnreplayable(knowledge.Variants), Is.EqualTo(1));
            Assert.That(PlayerPrefs.GetString(key), Does.Contain("\"schemaVersion\":2"));
        }

        [Test]
        public void AuthoredCookingCatalog_HasNoValidationErrors()
        {
            CookingDataCatalogSO catalog = AssetDatabase.LoadAssetAtPath<CookingDataCatalogSO>(
                "Assets/Work/Cook/SO/CookingDataCatalog.asset");

            CookingDataValidationReport report = new CookingDataValidationService().ValidateCatalog(catalog);

            Assert.That(catalog, Is.Not.Null);
            Assert.That(report.HasErrors, Is.False, BuildValidationFailure(report));
        }

        [Test]
        public void Validator_ReportsMissingSlotAndVariantEffectIds()
        {
            CookingMiniGameFeedbackRule feedback = CreateFeedbackRule(
                CookingMiniGameGrade.Good,
                string.Empty,
                "변형");
            PreparationMethodSO method = CreateMethod("cut", CookingMiniGameType.Slicing);
            IngredientPreparationOption option = CreateOption(
                "cut_option",
                method,
                feedbackRules: new[] { feedback });
            IngredientSO ingredient = CreateIngredient("ingredient", option);
            RecipeIngredientRequirement requirement = Requirement(ingredient, true, string.Empty);
            RecipeSO recipe = CreateRecipe("recipe", requirement);
            CookingDataCatalogSO catalog = CreateCatalog(
                new[] { ingredient },
                new[] { recipe },
                preparationMethods: new[] { method });

            CookingDataValidationReport report = new CookingDataValidationService().ValidateCatalog(catalog);

            Assert.That(HasIssueCode(report, "REQUIREMENT_ID_MISSING"), Is.True);
            Assert.That(HasIssueCode(report, "VARIANT_EFFECT_ID_MISSING"), Is.True);
        }

        [TestCase("Assets/Work/Cook/Scene/CookTestScene.unity")]
        [TestCase("Assets/Work/Adventure/Scene/AdventureTestScene.unity")]
        public void CookingScenes_LoadWithRequiredFlowComponentsAndNoMissingScripts(string scenePath)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            int missingScripts = 0;
            int gamePanels = 0;
            int knowledgeStores = 0;
            int flowRunners = 0;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform[] transforms = roots[i].GetComponentsInChildren<Transform>(true);
                for (int objectIndex = 0; objectIndex < transforms.Length; objectIndex++)
                    missingScripts += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transforms[objectIndex].gameObject);
                gamePanels += roots[i].GetComponentsInChildren<CookingGamePanel>(true).Length;
                knowledgeStores += roots[i].GetComponentsInChildren<CookingKnowledgeStore>(true).Length;
                flowRunners += roots[i].GetComponentsInChildren<CookingFlowRunner>(true).Length;
            }

            Assert.That(scene.isLoaded, Is.True);
            Assert.That(missingScripts, Is.Zero, scenePath + " contains missing scripts.");
            Assert.That(gamePanels, Is.GreaterThan(0), scenePath + " has no CookingGamePanel.");
            Assert.That(knowledgeStores, Is.GreaterThan(0), scenePath + " has no CookingKnowledgeStore.");
            Assert.That(flowRunners, Is.GreaterThan(0), scenePath + " has no CookingFlowRunner.");
        }

        [UnityTest]
        public IEnumerator CookTestScene_PlayModeCookingFlowInitializes()
        {
            return VerifyCookingSceneInPlayMode("Assets/Work/Cook/Scene/CookTestScene.unity");
        }

        [UnityTest]
        public IEnumerator AdventureTestScene_PlayModeCookingFlowInitializes()
        {
            return VerifyCookingSceneInPlayMode("Assets/Work/Adventure/Scene/AdventureTestScene.unity");
        }

        [Test]
        public void BizarreDish_IsNotAnAutomaticFailure()
        {
            VisitEventData visit = CreateVisitEvent();
            NpcDishSubmission dish = new NpcDishSubmission(
                "recipe",
                "soup",
                Array.Empty<string>(),
                NpcDishFormationStatus.Formed,
                NpcDishOddity.Bizarre,
                NpcDishSafety.Safe,
                NpcDishCraftGrade.Perfect);

            NpcDishEvaluation evaluation = NpcDishResultEvaluator.Evaluate(visit, dish);

            Assert.That(evaluation.Result, Is.EqualTo(NpcConversationResult.Perfect));
        }

        [Test]
        public void DangerousDish_IsAlwaysWrong()
        {
            VisitEventData visit = CreateVisitEvent();
            NpcDishSubmission dish = new NpcDishSubmission(
                "recipe",
                "soup",
                Array.Empty<string>(),
                NpcDishFormationStatus.Formed,
                NpcDishOddity.Normal,
                NpcDishSafety.Dangerous,
                NpcDishCraftGrade.Perfect);

            NpcDishEvaluation evaluation = NpcDishResultEvaluator.Evaluate(visit, dish);

            Assert.That(evaluation.Result, Is.EqualTo(NpcConversationResult.Wrong));
        }

        private IngredientSO CreateIngredient(string id, params IngredientPreparationOption[] options)
        {
            IngredientSO ingredient = ScriptableObject.CreateInstance<IngredientSO>();
            _createdObjects.Add(ingredient);
            SetField(ingredient, "ingredientId", id);
            SetField(ingredient, "displayName", id);
            SetField(ingredient, "preparationOptions", new List<IngredientPreparationOption>(options ?? Array.Empty<IngredientPreparationOption>()));
            return ingredient;
        }

        private RecipeSO CreateRecipe(string id, params RecipeIngredientRequirement[] requirements)
        {
            RecipeSO recipe = ScriptableObject.CreateInstance<RecipeSO>();
            _createdObjects.Add(recipe);
            SetField(recipe, "recipeId", id);
            SetField(recipe, "displayName", id);
            SetField(recipe, "requiredIngredients", new List<RecipeIngredientRequirement>(requirements));
            return recipe;
        }

        private static RecipeIngredientRequirement Requirement(
            IngredientSO ingredient,
            bool recipeDefining,
            string requirementId = "slot_main",
            int minCount = 1,
            int maxCount = 1)
        {
            RecipeIngredientRequirement requirement = new RecipeIngredientRequirement();
            SetField(requirement, "requirementId", requirementId);
            SetField(requirement, "ingredient", ingredient);
            SetField(requirement, "minCount", minCount);
            SetField(requirement, "maxCount", maxCount);
            SetField(requirement, "recipeDefining", recipeDefining);
            return requirement;
        }

        private PreparationMethodSO CreateMethod(
            string id,
            CookingMiniGameType miniGameType = CookingMiniGameType.None)
        {
            PreparationMethodSO method = ScriptableObject.CreateInstance<PreparationMethodSO>();
            _createdObjects.Add(method);
            SetField(method, "methodId", id);
            SetField(method, "displayName", id);
            SetField(method, "miniGameType", miniGameType);
            return method;
        }

        private static IngredientPreparationOption CreateOption(
            string id,
            PreparationMethodSO method,
            int qualityDelta = 0,
            string resultNameModifier = "",
            IReadOnlyList<CookingMiniGameFeedbackRule> feedbackRules = null)
        {
            IngredientPreparationOption option = new IngredientPreparationOption();
            SetField(option, "preparationOptionId", id);
            SetField(option, "method", method);
            SetField(option, "qualityDelta", qualityDelta);
            SetField(option, "resultNameModifier", resultNameModifier);
            SetField(option, "miniGameFeedbackRules", new List<CookingMiniGameFeedbackRule>(
                feedbackRules ?? Array.Empty<CookingMiniGameFeedbackRule>()));
            return option;
        }

        private static CookingMiniGameFeedbackRule CreateFeedbackRule(
            CookingMiniGameGrade grade,
            string effectId,
            string resultNameModifier)
        {
            CookingMiniGameFeedbackRule rule = new CookingMiniGameFeedbackRule();
            SetField(rule, "grade", grade);
            SetField(rule, "variantEffectId", effectId);
            SetField(rule, "resultNameModifier", resultNameModifier);
            return rule;
        }

        private FoodTagSO CreateTag(string id)
        {
            FoodTagSO tag = ScriptableObject.CreateInstance<FoodTagSO>();
            _createdObjects.Add(tag);
            SetField(tag, "tagId", id);
            SetField(tag, "displayName", id);
            return tag;
        }

        private CookingDataCatalogSO CreateCatalog(
            IReadOnlyList<IngredientSO> ingredients,
            IReadOnlyList<RecipeSO> recipes,
            IReadOnlyList<FoodTagSO> tags = null,
            IReadOnlyList<PreparationMethodSO> preparationMethods = null)
        {
            CookingDataCatalogSO catalog = ScriptableObject.CreateInstance<CookingDataCatalogSO>();
            _createdObjects.Add(catalog);
            SetField(catalog, "ingredients", new List<IngredientSO>(ingredients ?? Array.Empty<IngredientSO>()));
            SetField(catalog, "recipes", new List<RecipeSO>(recipes ?? Array.Empty<RecipeSO>()));
            SetField(catalog, "tags", new List<FoodTagSO>(tags ?? Array.Empty<FoodTagSO>()));
            SetField(catalog, "preparationMethods", new List<PreparationMethodSO>(
                preparationMethods ?? Array.Empty<PreparationMethodSO>()));
            return catalog;
        }

        private CookingKnowledgeStore CreateKnowledgeStore(
            CookingDataCatalogSO catalog,
            bool load,
            bool save,
            string key)
        {
            GameObject owner = new GameObject("CookingKnowledgeStoreTests");
            _createdObjects.Add(owner);
            CookingKnowledgeStore store = owner.AddComponent<CookingKnowledgeStore>();
            SetField(store, "catalog", catalog);
            SetField(store, "loadFromPlayerPrefsOnAwake", load);
            SetField(store, "saveToPlayerPrefs", save);
            SetField(store, "playerPrefsKey", key);
            store.Initialize(catalog);
            return store;
        }

        private static CookingVariantIdentity BuildVariantIdentity(
            RecipeSO recipe,
            IngredientSO ingredient,
            IngredientPreparationOption option,
            CookingMiniGameResult miniGameResult)
        {
            PreparedIngredientState prepared = new PreparedIngredientState(ingredient, option, miniGameResult);
            RecipePreparedMatchResult match = recipe.MatchPreparedIngredients(new[] { prepared });
            Assert.That(match.Status, Is.EqualTo(RecipeMatchStatus.Matched));
            return CookingVariantIdentityBuilder.Build(recipe, match.Bindings);
        }

        private static DishResult CreateDishResult(
            RecipeSO recipe,
            FoodTagSO tag,
            CookingVariantIdentity identity,
            IngredientPreparationOption option,
            string sessionId,
            DishCraftGrade grade,
            DishOddity oddity)
        {
            IngredientSO ingredient = identity.ReplayComponents[0].Ingredient;
            return new DishResult(
                recipe.DisplayName,
                recipe,
                null,
                new[] { tag },
                DishFormationStatus.Formed,
                DishVariantStatus.Variant,
                oddity,
                DishSafety.Safe,
                grade,
                0,
                sessionId,
                recipe,
                true,
                identity,
                new[] { new PreparedIngredientState(ingredient, option) },
                Array.Empty<string>());
        }

        private static NpcDishMatchReport CreateMatchReport(
            string recipeId,
            string npcId,
            string tagId,
            DishCraftGrade grade)
        {
            VisitEventData visit = CreateVisitEvent(recipeId, npcId, new[] { tagId });
            NpcOrderContext order = NpcOrderContext.FromVisitEvent(visit, 0, 0);
            NpcDishSubmission dish = new NpcDishSubmission(
                recipeId,
                "soup",
                new[] { tagId },
                NpcDishFormationStatus.Formed,
                NpcDishOddity.Normal,
                NpcDishSafety.Safe,
                (NpcDishCraftGrade)grade);
            return NpcDishResultEvaluator.BuildMatchReport(order, dish);
        }

        private static int CountReplayable(IReadOnlyList<CookingRecipeVariantKnowledgeSnapshot> variants)
        {
            int count = 0;
            for (int i = 0; i < variants.Count; i++)
            {
                if (variants[i].CanReplay)
                    count++;
            }
            return count;
        }

        private static int CountLegacyUnreplayable(IReadOnlyList<CookingRecipeVariantKnowledgeSnapshot> variants)
        {
            int count = 0;
            for (int i = 0; i < variants.Count; i++)
            {
                if (variants[i].CanReplay == false && string.IsNullOrWhiteSpace(variants[i].LegacyVariantKey) == false)
                    count++;
            }
            return count;
        }

        private static string BuildValidationFailure(CookingDataValidationReport report)
        {
            if (report == null || report.Issues == null)
                return "Validation report is missing.";
            List<string> errors = new List<string>();
            for (int i = 0; i < report.Issues.Count; i++)
            {
                if (report.Issues[i].Severity == CookingDataValidationSeverity.Error)
                    errors.Add(report.Issues[i].ToString());
            }
            return string.Join("\n", errors);
        }

        private static bool HasIssueCode(CookingDataValidationReport report, string code)
        {
            if (report?.Issues == null)
                return false;
            for (int i = 0; i < report.Issues.Count; i++)
            {
                if (string.Equals(report.Issues[i].Code, code, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        private static IEnumerator VerifyCookingSceneInPlayMode(string scenePath)
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            yield return new EnterPlayMode();
            yield return null;

            CookingGamePanel[] panels = UnityEngine.Object.FindObjectsByType<CookingGamePanel>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            bool hasReadyPanel = false;
            for (int i = 0; i < panels.Length; i++)
            {
                if (panels[i] != null && panels[i].FlowRunner != null && panels[i].KnowledgeStore != null)
                {
                    hasReadyPanel = true;
                    break;
                }
            }

            yield return new ExitPlayMode();
            Assert.That(hasReadyPanel, Is.True, scenePath + " did not initialize the cooking flow in Play Mode.");
        }

        private static VisitEventData CreateVisitEvent(
            string recipeId = "recipe",
            string npcId = "npc",
            IReadOnlyList<string> requiredTags = null)
        {
            return new VisitEventData(
                "event",
                npcId,
                string.Empty,
                Array.Empty<string>(),
                0,
                Array.Empty<string>(),
                VisitEventType.Normal,
                0,
                VisitEventRepeatMode.Once,
                0,
                0,
                0,
                0,
                string.Empty,
                Array.Empty<string>(),
                string.Empty,
                string.Empty,
                string.Empty,
                Array.Empty<string>(),
                string.Empty,
                string.Empty,
                0,
                recipeId,
                new[] { "soup" },
                requiredTags ?? Array.Empty<string>(),
                Array.Empty<string>(),
                Array.Empty<string>(),
                Array.Empty<string>());
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field: {target.GetType().Name}.{fieldName}");
            field.SetValue(target, value);
        }

        private sealed class StubDataProvider : ICookingDataProvider
        {
            private readonly IReadOnlyList<RecipeSO> _recipes;

            public StubDataProvider(params RecipeSO[] recipes)
            {
                _recipes = recipes ?? Array.Empty<RecipeSO>();
            }

            public IReadOnlyList<RecipeSO> GetRecipes() => _recipes;
            public IReadOnlyList<IngredientSO> GetIngredients() => Array.Empty<IngredientSO>();
            public IReadOnlyList<IngredientPreparationOption> GetPreparationOptions(IngredientSO ingredient) =>
                Array.Empty<IngredientPreparationOption>();
            public RecipeSO FindRecipeByIngredients(IReadOnlyList<IngredientSO> ingredients) => null;
            public RecipeSO FindRecipeByPreparedIngredients(IReadOnlyList<PreparedIngredientState> ingredients) => null;
        }
    }
}
#endif
