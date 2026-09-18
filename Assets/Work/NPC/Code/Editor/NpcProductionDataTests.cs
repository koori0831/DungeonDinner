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
            var catalog = AssetDatabase.LoadAssetAtPath<CookingDataCatalogSO>("Assets/Work/Cook/SO/CookingDataCatalog.asset");
            var orders = CsvTableParser.Parse(Resources.Load<TextAsset>("NPCData/VisitEvents"))
                .Select(VisitEventData.FromRow).Where(o => o.CorrectRecipeId == recipeId).ToArray();
            Assert.That(orders, Is.Not.Empty);
            var chosen = new List<(IngredientSO ingredient, IngredientPreparationOption option)>();
            var builder = CookingServiceFactory.CreateDishResultBuilder(catalog);
            bool Search(int index)
            {
                if (index == catalog.Ingredients.Count)
                {
                    if (chosen.Count == 0) return false;
                    var session = CookingSession.CreateForDirectIngredients(chosen.Select(p => p.ingredient).ToArray());
                    foreach (var pair in chosen) session.SelectPreparation(pair.ingredient, pair.option);
                    var dish = builder.Build(session);
                    if (!dish.IsRecipeMatched || dish.BaseRecipe.RecipeId != recipeId) return false;
                    var submission = CookingNpcDishAdapter.ToNpcDishSubmission(dish);
                    return orders.All(order =>
                    {
                        var outcome = NpcDishResultEvaluator.Evaluate(order, submission);
                        return (outcome.Result == NpcConversationResult.Correct || outcome.Result == NpcConversationResult.Perfect)
                            && order.PreferredTags.All(submission.Tags.Contains);
                    });
                }
                if (Search(index + 1)) return true;
                var ingredient = catalog.Ingredients[index];
                foreach (var option in ingredient.PreparationOptions)
                {
                    chosen.Add((ingredient, option));
                    if (Search(index + 1)) return true;
                    chosen.RemoveAt(chosen.Count - 1);
                }
                return false;
            }
            Assert.That(Search(0), Is.True, recipeId + " must satisfy the shipped NPC sensory requirements using real preparations.");
        }

        [Test]
        public void ProductionDailyOrders_AreRepeatableUnderRuntimeRules()
        {
            var events = CsvTableParser.Parse(Resources.Load<TextAsset>("NPCData/VisitEvents")).Select(VisitEventData.FromRow).ToArray();
            var daily = events.Where(e => e.RepeatMode == VisitEventRepeatMode.Cycle).ToArray();
            Assert.That(daily.Length, Is.GreaterThanOrEqualTo(4));
            foreach (var order in daily)
            {
                Assert.That(NpcVisitEventRules.IsOneShotEvent(order), Is.False, order.EventId);
                Assert.That(NpcVisitEventRules.RequiresCookingStep(order), Is.True, order.EventId);
                Assert.That(order.RequiredLastResult, Is.Empty, order.EventId);
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

        [Test]
        public void PortraitCatalog_MapsEveryProductionNpcToTransparentSingleSprite()
        {
            const string catalogPath = "Assets/Resources/NPCData/NpcPortraitCatalog.asset";
            var catalog = AssetDatabase.LoadAssetAtPath<NpcPortraitCatalogSO>(catalogPath);
            Assert.That(catalog, Is.Not.Null, catalogPath);

            var expectedPaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Odin"] = "Assets/Work/Cook/Graphics/UIAsset/NPCPortraits/npc_portrait_odin.png",
                ["Nari"] = "Assets/Work/Cook/Graphics/UIAsset/NPCPortraits/npc_portrait_nari.png",
                ["Boram"] = "Assets/Work/Cook/Graphics/UIAsset/NPCPortraits/npc_portrait_boram.png",
                ["Rook"] = "Assets/Work/Cook/Graphics/UIAsset/NPCPortraits/npc_portrait_rook.png"
            };

            foreach (KeyValuePair<string, string> expected in expectedPaths)
            {
                Sprite portrait = catalog.GetPortrait(expected.Key.ToLowerInvariant());
                Assert.That(portrait, Is.Not.Null, expected.Key);
                Assert.That(AssetDatabase.GetAssetPath(portrait), Is.EqualTo(expected.Value));

                TextureImporter importer = AssetImporter.GetAtPath(expected.Value) as TextureImporter;
                Assert.That(importer, Is.Not.Null, expected.Value);
                Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite));
                Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single));
                Assert.That(importer.maxTextureSize, Is.EqualTo(512));
                Assert.That(importer.mipmapEnabled, Is.False);
                Assert.That(importer.alphaIsTransparency, Is.True);
                Assert.That(importer.DoesSourceTextureHaveAlpha(), Is.True, expected.Value);

                TextureImporterSettings settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                Assert.That(settings.spriteMeshType, Is.EqualTo(SpriteMeshType.FullRect));
                Assert.That(settings.spritePivot, Is.EqualTo(new Vector2(0.5f, 0.5f)));
            }

            Assert.That(catalog.GetPortrait("missing-npc"), Is.EqualTo(catalog.FallbackPortrait));
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
