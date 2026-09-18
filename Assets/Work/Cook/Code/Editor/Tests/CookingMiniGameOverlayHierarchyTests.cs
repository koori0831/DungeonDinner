using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonDinner.Cook.EditorTests
{
    public sealed class CookingMiniGameOverlayHierarchyTests
    {
        private const string PrefabPath = "Assets/Work/Cook/Prefabs/UI/CookingPresentationRoot.prefab";
        private const string StandaloneOverlayPrefabPath =
            "Assets/Work/Cook/Prefabs/UI/CookingMiniGameOverlayRoot.prefab";
        private const string IntegrationScenePath =
            "Assets/Work/Integration/Scene/DungeonDinnerScene.unity";
        private const string SettingsPath = "Assets/Work/Cook/SO/CookingMiniGameOverlaySettings.asset";
        private const string MiniGameAssetFolder = "Assets/Work/Cook/Graphics/UIAsset/CookingMiniGame";

        [Test]
        public void OverlayPrefab_HasGraphicCuesAndUnmaskedInteractionTargets()
        {
            foreach (string path in new[] { PrefabPath, StandaloneOverlayPrefabPath })
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Assert.That(prefab, Is.Not.Null);
                Transform overlay = prefab.name == "CookingMiniGameOverlayRoot" ? prefab.transform : FindDeep(prefab.transform, "CookingMiniGameOverlayRoot");
                MonoBehaviour host = overlay.GetComponents<MonoBehaviour>().First(c => c != null && c.GetType().Name == "CookingMiniGameOverlayHost");
                var serializedHost = new SerializedObject(host);
                foreach (string field in new[] { "actionHudRoot", "instructionField", "progressFill", "targetBand", "targetMarker", "progressField", "timerFill", "gestureGraphic", "resultScoreField", "resultReasonField" })
                    AssertBound(serializedHost, field);
                foreach (string removed in new[] { "titleField", "statusField", "gestureField", "mistakeField" })
                    Assert.That(serializedHost.FindProperty(removed), Is.Null);
                var result = ((Component)serializedHost.FindProperty("resultCanvasGroup").objectReferenceValue).transform;
                var progress = (Component)serializedHost.FindProperty("progressField").objectReferenceValue;
                var instruction = (Component)serializedHost.FindProperty("instructionField").objectReferenceValue;
                var hud = (Component)serializedHost.FindProperty("actionHudRoot").objectReferenceValue;
                Assert.That(instruction.transform.parent, Is.EqualTo(hud.transform));
                Assert.That(instruction.GetComponentInParent<Mask>(), Is.Null, "Instructions must remain outside the ingredient mask.");
                var paper = hud.transform.Find("PaperBackground");
                Assert.That(paper, Is.Not.Null, path + ": instruction panel background is missing");
                Assert.That(paper.GetSiblingIndex(), Is.LessThan(instruction.transform.GetSiblingIndex()),
                    path + ": opaque paper must render before the instruction text");
                foreach (var text in overlay.GetComponentsInChildren<MonoBehaviour>(true).Where(c => c != null && c.GetType().Name == "TextMeshProUGUI"))
                    Assert.That(text == progress || text == instruction || text.transform.IsChildOf(result), Is.True, path + ": unexpected in-game label " + text.name);
                foreach (string name in new[] { "KnifeGuide", "PestleGuide", "BrushGuide", "ActionGuide", "WasteZone", "StrikeTarget1", "CutLine1", "PourZone" })
                {
                    var target = FindDeep(overlay, name);
                    Assert.That(target, Is.Not.Null, name);
                    Assert.That(target.GetComponentInParent<Mask>(), Is.Null, name + " is clipped by the ingredient mask");
                }
                var waste = FindDeep(overlay, "WasteZone").GetComponent<Image>();
                Assert.That(waste.color.r, Is.GreaterThan(waste.color.g));
                Assert.That(waste.color.a, Is.GreaterThan(0.1f));
                Assert.That(FindDeep(waste.transform, "DestinationDrawing"), Is.Not.Null);
            }
        }

        [Test]
        public void BoilingScore_UsesOnlyCookingTiming()
        {
            System.Type scoringType = System.Type.GetType(
                "Work.Cook.Code.Runtime.UI.CookingMiniGameScoring, Assembly-CSharp");
            Assert.That(scoringType, Is.Not.Null);

            System.Reflection.MethodInfo method = scoringType.GetMethod(
                "ScoreBoiling",
                new[] { typeof(float), typeof(float), typeof(float) });
            Assert.That(method, Is.Not.Null,
                "Boiling scoring must expose the three-argument timing-only signature.");

            float early = (float)method.Invoke(null, new object[] { 0.2f, 0.52f, 0.7f });
            float centered = (float)method.Invoke(null, new object[] { 0.61f, 0.52f, 0.7f });
            float late = (float)method.Invoke(null, new object[] { 0.95f, 0.52f, 0.7f });
            Assert.That(centered, Is.GreaterThan(early));
            Assert.That(centered, Is.GreaterThan(late));
        }

        [TestCase(PrefabPath)]
        [TestCase(StandaloneOverlayPrefabPath)]
        public void ClickInteractionPrefabs_DoNotKeepTransportDragObjects(string prefabPath)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.That(prefab, Is.Not.Null, $"The cooking UI prefab is missing: {prefabPath}");

            Transform overlay = prefab.name == "CookingMiniGameOverlayRoot"
                ? prefab.transform
                : FindDeep(prefab.transform, "CookingMiniGameOverlayRoot");
            Assert.That(overlay, Is.Not.Null);
            Assert.That(FindDeep(overlay, "IngredientClickGuide"), Is.Not.Null);
            Assert.That(FindDeep(overlay, "PlateZone"), Is.Null);
            Assert.That(FindDeep(overlay, "LadleGuide"), Is.Null);
            Assert.That(FindDeep(overlay, "FlipAndDragGuide"), Is.Null);
        }

        [Test]
        public void DungeonDinnerScene_UsesClickInteractionPresentationPrefab()
        {
            SceneAsset scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(IntegrationScenePath);
            Assert.That(scene, Is.Not.Null, "The integration scene is missing.");

            string[] dependencies = AssetDatabase.GetDependencies(IntegrationScenePath, true);
            Assert.That(dependencies, Does.Contain(PrefabPath),
                "DungeonDinnerScene must use the updated cooking presentation prefab.");

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Transform overlay = FindDeep(prefab.transform, "CookingMiniGameOverlayRoot");
            Assert.That(FindDeep(overlay, "IngredientClickGuide"), Is.Not.Null);
            Assert.That(FindDeep(overlay, "PlateZone"), Is.Null);
            Assert.That(FindDeep(overlay, "LadleGuide"), Is.Null);
        }

        [Test]
        public void OverlaySettings_KeepResultVisibleLongEnoughToRead()
        {
            Object settings = AssetDatabase.LoadAssetAtPath<Object>(SettingsPath);
            Assert.That(settings, Is.Not.Null, "The cooking overlay settings asset is missing.");

            SerializedProperty duration = new SerializedObject(settings).FindProperty("resultDisplayDuration");
            Assert.That(duration, Is.Not.Null);
            Assert.That(duration.floatValue, Is.GreaterThanOrEqualTo(2f));

            SerializedProperty dimColor = new SerializedObject(settings).FindProperty("focusDimColor");
            Assert.That(dimColor, Is.Not.Null);
            Assert.That(dimColor.colorValue.a, Is.GreaterThanOrEqualTo(0.45f));
        }

        [Test]
        public void OverlaySettings_HasEveryGeneratedMiniGameSpriteAssigned()
        {
            Object settings = AssetDatabase.LoadAssetAtPath<Object>(SettingsPath);
            Assert.That(settings, Is.Not.Null, "The cooking overlay settings asset is missing.");

            SerializedObject serializedSettings = new SerializedObject(settings);
            string[] spriteProperties =
            {
                "knifeSprite",
                "brushSprite",
                "panSprite",
                "plateSprite",
                "pestleSprite",
                "pitcherSprite",
                "mortarSprite",
                "frostSprite",
                "flipSprite",
                "foamDiscardSprite"
            };

            foreach (string propertyName in spriteProperties)
                AssertBound(serializedSettings, propertyName);
        }

        [Test]
        public void Workbench_UsesGeneratedCuttingBoardBehindIngredient()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.That(prefab, Is.Not.Null);

            Transform workbench = FindDeep(prefab.transform, "Workbench");
            Assert.That(workbench, Is.Not.Null);
            Image board = workbench.GetComponent<Image>();
            Assert.That(board, Is.Not.Null);
            Assert.That(board.sprite, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(board.sprite),
                Is.EqualTo($"{MiniGameAssetFolder}/cook_tool_cutting_board.png"));
            Assert.That(board.preserveAspect, Is.True);

            Transform ingredientAnchor = FindDeep(workbench, "IngredientAnchor");
            Assert.That(ingredientAnchor, Is.Not.Null);
            Assert.That(ingredientAnchor.parent, Is.SameAs(workbench),
                "The ingredient must stay a child rendered above the workbench's board image.");
        }

        [Test]
        public void GeneratedMiniGameTextures_AreSingleTransparentSprites()
        {
            string[] filenames =
            {
                "cook_tool_knife.png",
                "cook_tool_brush.png",
                "cook_tool_pan.png",
                "cook_tool_plate.png",
                "cook_tool_pestle.png",
                "cook_tool_pitcher.png",
                "cook_tool_mortar.png",
                "cook_interaction_frost.png",
                "cook_interaction_flip.png",
                "cook_interaction_foam_discard.png",
                "cook_tool_cutting_board.png"
            };

            foreach (string filename in filenames)
            {
                string path = $"{MiniGameAssetFolder}/{filename}";
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Assert.That(importer, Is.Not.Null, $"Missing texture importer for {path}");
                Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite));
                Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single));
                SerializedProperty meshType = new SerializedObject(importer).FindProperty("m_SpriteMeshType");
                Assert.That(meshType, Is.Not.Null);
                Assert.That(meshType.intValue, Is.EqualTo((int)SpriteMeshType.FullRect));
                Assert.That(importer.alphaIsTransparency, Is.True);
                Assert.That(importer.mipmapEnabled, Is.False);
            }
        }

        [Test]
        public void PreparationHand_MiniGameState_IsVisuallyIsolated()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            Assert.That(instance, Is.Not.Null);

            try
            {
                Transform hand = FindDeep(instance.transform, "PreparationHand");
                Assert.That(hand, Is.Not.Null);
                MonoBehaviour handView = hand.GetComponents<MonoBehaviour>()
                    .FirstOrDefault(component => component != null
                        && component.GetType().Name == "CookingPreparationHandView");
                Assert.That(handView, Is.Not.Null);

                handView.GetType().GetMethod("ShowMiniGameState")?.Invoke(handView, null);
                CanvasGroup group = hand.GetComponent<CanvasGroup>();
                Assert.That(group, Is.Not.Null);
                Assert.That(group.alpha, Is.LessThanOrEqualTo(0.1f));
                Assert.That(group.interactable, Is.False);
                Assert.That(group.blocksRaycasts, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        private static void AssertBound(SerializedObject serializedObject, string propertyName)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            Assert.That(property, Is.Not.Null, $"Serialized property '{propertyName}' is missing.");
            Assert.That(property.objectReferenceValue, Is.Not.Null, $"Serialized property '{propertyName}' is not bound.");
        }

        private static bool HasNonEmptyText(MonoBehaviour component)
        {
            SerializedProperty text = new SerializedObject(component).FindProperty("m_text");
            return text != null && string.IsNullOrWhiteSpace(text.stringValue) == false;
        }

        private static Transform FindDeep(Transform root, string pathOrName)
        {
            if (root == null)
                return null;

            Transform pathMatch = root.Find(pathOrName);
            if (pathMatch != null)
                return pathMatch;
            if (root.name == pathOrName)
                return root;

            for (int index = 0; index < root.childCount; index++)
            {
                Transform found = FindDeep(root.GetChild(index), pathOrName);
                if (found != null)
                    return found;
            }

            return null;
        }
    }
}
