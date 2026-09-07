using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DungeonDinner.Cook.EditorTests
{
    public sealed class CookingUiPresentationIconTests
    {
        private const string SettingsPath = "Assets/Work/Cook/SO/CookingUiPresentationSettings.asset";
        private const string IconsPath = "Assets/Work/Cook/Graphics/UIPresentation/Icons/";
        private const string FeedbackPath = "Assets/Work/Cook/Graphics/UIAsset/Feedback/";

        private static readonly IReadOnlyDictionary<string, string> ExpectedIconPaths =
            new Dictionary<string, string>
            {
                ["qualityVisuals.Array.data[0].icon"] = FeedbackPath + "ui_quality_perfect.png",
                ["qualityVisuals.Array.data[1].icon"] = FeedbackPath + "ui_quality_normal.png",
                ["qualityVisuals.Array.data[2].icon"] = FeedbackPath + "ui_quality_good.png",
                ["qualityVisuals.Array.data[3].icon"] = FeedbackPath + "ui_quality_poor.png",
                ["reactionVisuals.Array.data[0].icon"] = FeedbackPath + "ui_reaction_delighted.png",
                ["reactionVisuals.Array.data[1].icon"] = FeedbackPath + "ui_reaction_satisfied.png",
                ["reactionVisuals.Array.data[2].icon"] = FeedbackPath + "ui_reaction_interested.png",
                ["reactionVisuals.Array.data[3].icon"] = FeedbackPath + "ui_reaction_disappointed.png",
                ["reactionVisuals.Array.data[4].icon"] = FeedbackPath + "ui_reaction_repulsed.png",
                ["tagVisuals.Array.data[0].icon"] = FeedbackPath + "ui_tag_required.png",
                ["tagVisuals.Array.data[1].icon"] = FeedbackPath + "ui_tag_preferred.png",
                ["tagVisuals.Array.data[2].icon"] = FeedbackPath + "ui_tag_avoid.png",
                ["tagVisuals.Array.data[3].icon"] = FeedbackPath + "ui_tag_danger.png",
                ["rewardIcon"] = FeedbackPath + "ui_reward_coin.png",
                ["npcPlaceholderIcon"] = FeedbackPath + "ui_npc_placeholder.png",
                ["cookingSparkleIcon"] = FeedbackPath + "ui_cooking_sparkle.png"
            };

        [Test]
        public void PresentationSettings_UseGeneratedSingleSpriteIcons()
        {
            Object settings = AssetDatabase.LoadAssetAtPath<Object>(SettingsPath);
            Assert.That(settings, Is.Not.Null);
            SerializedObject serializedSettings = new SerializedObject(settings);

            foreach (KeyValuePair<string, string> expected in ExpectedIconPaths)
            {
                SerializedProperty property = serializedSettings.FindProperty(expected.Key);
                Assert.That(property, Is.Not.Null, $"Missing serialized icon field: {expected.Key}");
                Assert.That(property.objectReferenceValue, Is.Not.Null, $"Unassigned icon: {expected.Key}");
                Assert.That(AssetDatabase.GetAssetPath(property.objectReferenceValue), Is.EqualTo(expected.Value));
            }
        }

        [Test]
        public void GeneratedIcons_UseUiSpriteImportSettings()
        {
            foreach (string iconPath in ExpectedIconPaths.Values)
            {
                TextureImporter importer = AssetImporter.GetAtPath(iconPath) as TextureImporter;
                Assert.That(importer, Is.Not.Null, $"Missing texture importer: {iconPath}");
                Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite));
                Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single));
                int expectedMaxSize = iconPath.Contains("/Tags128/") ? 128 : 256;
                Assert.That(importer.maxTextureSize, Is.EqualTo(expectedMaxSize));
                Assert.That(importer.mipmapEnabled, Is.False);
                Assert.That(importer.alphaIsTransparency, Is.True);

                TextureImporterSettings settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                Assert.That(settings.spriteMeshType, Is.EqualTo(SpriteMeshType.FullRect));
                Assert.That(settings.spritePivot, Is.EqualTo(new Vector2(0.5f, 0.5f)));
            }
        }
    }
}
