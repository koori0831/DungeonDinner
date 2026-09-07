#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Work.Cook.Code.Data;
using Work.Cook.Code.Runtime.Core;
using Work.Cook.Code.Runtime.Integration;
using Work.NPC.Code.Data;
using Work.NPC.Code.Runtime;

namespace Work.NPC.Code.Editor.Tests
{
    public sealed class NpcProductionDataTests
    {
        [TestCase("mushroom_soup_pane")]
        [TestCase("slime_nucleus_dango")]
        [TestCase("corn_cheese_fondue")]
        public void OrderProfile_CanBeSatisfiedWithRealPreparations(string recipeId)
        {
            string json = File.ReadAllText("Docs/NPCSystem/Production/OrderProfiles.json");
            Profile profile = JsonUtility.FromJson<ProfileFile>(json).profiles.Single(p => p.recipeId == recipeId);
            var catalog = AssetDatabase.LoadAssetAtPath<CookingDataCatalogSO>("Assets/Work/Cook/SO/CookingDataCatalog.asset");
            var ingredients = profile.preparations.Select(p => catalog.Ingredients.Single(i => i.IngredientId == p.ingredientId)).ToArray();
            CookingSession session = CookingSession.CreateForDirectIngredients(ingredients);
            for (int i = 0; i < ingredients.Length; i++)
            {
                var option = ingredients[i].PreparationOptions.Single(o => o.PreparationOptionId == profile.preparations[i].optionId);
                session.SelectPreparation(ingredients[i], option);
            }

            DishResult result = CookingServiceFactory.CreateDishResultBuilder(catalog).Build(session);
            Assert.That(result.FormationStatus, Is.EqualTo(DishFormationStatus.Formed));
            Assert.That(result.BaseRecipe.RecipeId, Is.EqualTo(recipeId));
            var order = VisitEventData.FromRow(new Dictionary<string, string>
            {
                ["EventId"] = "ProductionProfileCheck", ["NpcId"] = "ProfileCheck",
                ["CorrectRecipeId"] = recipeId, ["AllowedFoodTypes"] = profile.foodType,
                ["RequiredTags"] = profile.requiredTags, ["PreferredTags"] = profile.preferredTags,
                ["AvoidTags"] = profile.avoidTags, ["DisgustingTags"] = profile.disgustingTags
            });
            var submission = CookingNpcDishAdapter.ToNpcDishSubmission(result);
            var evaluation = NpcDishResultEvaluator.Evaluate(order, submission);
            Assert.That(evaluation.Result, Is.EqualTo(NpcConversationResult.Correct).Or.EqualTo(NpcConversationResult.Perfect));
            Assert.That(order.PreferredTags.All(submission.Tags.Contains), Is.True,
                "The example preparation must also be able to satisfy the preferred tags.");
        }

        [Test]
        public void StarterTemplate_DailyOrdersAreRepeatableUnderRuntimeRules()
        {
            string csv = File.ReadAllText("Docs/NPCSystem/Production/Templates/NpcPackage/VisitEvents.csv")
                .Replace("{{NPC_ID}}", "TemplateVisitor");
            var asset = new TextAsset(csv);
            try
            {
                var events = CsvTableParser.Parse(asset).Select(VisitEventData.FromRow).ToArray();
                var dailyOrders = events.Where(e => e.RepeatMode == VisitEventRepeatMode.Cycle).ToArray();
                Assert.That(dailyOrders.Length, Is.EqualTo(2));
                foreach (var order in dailyOrders)
                {
                    Assert.That(NpcVisitEventRules.IsOneShotEvent(order), Is.False, order.EventId);
                    Assert.That(NpcVisitEventRules.RequiresCookingStep(order), Is.True, order.EventId);
                    Assert.That(order.RequiredLastResult, Is.Empty);
                    Assert.That(order.RequiredCorrectCount, Is.Zero);
                    Assert.That(order.RequiredAffinity, Is.Zero);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void MossCave_HasFourDayOneCookingCustomersForThreeCustomerBusinessDay()
        {
            NpcConversationDatabase database = NpcConversationDatabase.LoadFromResources("NPCData");
            string[] expectedNpcIds = { "Odin", "Nari", "Boram", "Rook" };
            var poolEntries = database.GetRegionPoolEntries("MossCave")
                .Where(entry => entry.MinDay <= 1)
                .ToArray();

            Assert.That(poolEntries.Select(entry => entry.NpcId),
                Is.EquivalentTo(expectedNpcIds));

            foreach (string npcId in expectedNpcIds)
            {
                VisitEventData[] events = database.GetVisitEvents("MossCave", npcId).ToArray();
                Assert.That(events, Is.Not.Empty, npcId);
                Assert.That(events.All(NpcVisitEventRules.RequiresCookingStep), Is.True, npcId);
                Assert.That(events.Any(visitEvent => visitEvent.RequiredNpcVisits == 0
                                                    && visitEvent.RequiredEventIds.Count == 0),
                    Is.True,
                    npcId + " must be available for a new save.");
            }
        }

        [Serializable]
        private sealed class ProfileFile { public Profile[] profiles; }
        [Serializable]
        private sealed class Profile
        {
            public string recipeId, foodType, requiredTags, preferredTags, avoidTags, disgustingTags;
            public Preparation[] preparations;
        }
        [Serializable]
        private sealed class Preparation { public string ingredientId, optionId; }
    }
}
#endif
