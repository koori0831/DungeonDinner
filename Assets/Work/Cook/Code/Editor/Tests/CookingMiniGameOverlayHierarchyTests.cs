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

        [Test]
        public void OverlayPrefab_HasReadableActionFeedbackHierarchyAndBindings()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.That(prefab, Is.Not.Null, "The cooking presentation prefab is missing.");

            Transform overlay = FindDeep(prefab.transform, "CookingMiniGameOverlayRoot");
            Assert.That(overlay, Is.Not.Null, "The mini-game overlay root is missing.");

            RectTransform targetFrame = FindDeep(overlay, "TargetFrame") as RectTransform;
            Assert.That(targetFrame, Is.Not.Null);
            Assert.That(targetFrame.sizeDelta.x, Is.GreaterThanOrEqualTo(520f));
            Assert.That(targetFrame.sizeDelta.y, Is.GreaterThanOrEqualTo(320f));

            RectTransform actionHud = FindDeep(overlay, "LocalActionHUD") as RectTransform;
            Assert.That(actionHud, Is.Not.Null);
            Assert.That(actionHud.sizeDelta.y, Is.GreaterThanOrEqualTo(104f));
            Assert.That(FindDeep(overlay, "GestureGuide"), Is.Not.Null);
            Assert.That(FindDeep(overlay, "ProgressGauge"), Is.Not.Null);
            Assert.That(FindDeep(overlay, "ProgressFill"), Is.Not.Null);
            Assert.That(FindDeep(overlay, "TargetBand"), Is.Not.Null);
            Assert.That(FindDeep(overlay, "TargetMarker"), Is.Not.Null);
            Assert.That(FindDeep(overlay, "TimerGauge"), Is.Not.Null);
            Assert.That(FindDeep(overlay, "MistakeToast"), Is.Not.Null);
            Assert.That(FindDeep(overlay, "ResultBadge/Score"), Is.Not.Null);
            Assert.That(FindDeep(overlay, "ResultBadge/Reason"), Is.Not.Null);

            MonoBehaviour host = overlay.GetComponents<MonoBehaviour>()
                .FirstOrDefault(component => component != null && component.GetType().Name == "CookingMiniGameOverlayHost");
            Assert.That(host, Is.Not.Null, "CookingMiniGameOverlayHost is missing.");

            SerializedObject serializedHost = new SerializedObject(host);
            AssertBound(serializedHost, "actionHudRoot");
            AssertBound(serializedHost, "progressFill");
            AssertBound(serializedHost, "targetBand");
            AssertBound(serializedHost, "targetMarker");
            AssertBound(serializedHost, "progressField");
            AssertBound(serializedHost, "gestureField");
            AssertBound(serializedHost, "timerFill");
            AssertBound(serializedHost, "mistakeCanvasGroup");
            AssertBound(serializedHost, "mistakeField");
            AssertBound(serializedHost, "resultScoreField");
            AssertBound(serializedHost, "resultReasonField");
            AssertBound(serializedHost, "synchronizedWorkbenchView");
            AssertBound(serializedHost, "synchronizedHandView");
            AssertBound(serializedHost, "synchronizedActiveSlotView");
            Assert.That(serializedHost.FindProperty("actionHudGap").floatValue, Is.GreaterThanOrEqualTo(24f));
            Assert.That(serializedHost.FindProperty("mistakeDisplayDuration").floatValue, Is.GreaterThanOrEqualTo(1.1f));
            Assert.That(serializedHost.FindProperty("useTemporaryFeedbackAudio").boolValue, Is.True);

            RectTransform knifeGuide = FindDeep(overlay, "KnifeGuide") as RectTransform;
            Assert.That(knifeGuide, Is.Not.Null);
            Assert.That(knifeGuide.sizeDelta.x, Is.GreaterThanOrEqualTo(36f));
            Assert.That(knifeGuide.sizeDelta.y, Is.GreaterThanOrEqualTo(96f));
            Assert.That(knifeGuide.parent.parent, Is.SameAs(targetFrame),
                "Slicing guides must render above the ingredient alpha mask.");

            for (int index = 1; index <= 3; index++)
            {
                RectTransform cutLine = FindDeep(overlay, $"CutLine{index}") as RectTransform;
                Assert.That(cutLine, Is.Not.Null);
                Assert.That(cutLine.sizeDelta.x, Is.GreaterThanOrEqualTo(12f));
                Assert.That(Mathf.Abs(cutLine.anchoredPosition.x), Is.LessThanOrEqualTo(80f));
                Assert.That(cutLine.sizeDelta.y, Is.LessThanOrEqualTo(180f));
                Assert.That(cutLine.GetComponent<Image>(), Is.Not.Null);
            }

            Assert.That(FindDeep(overlay, "IngredientClickGuide"), Is.Not.Null,
                "Roasting must keep a visible click/flip guide over the stationary ingredient.");
            Assert.That(FindDeep(overlay, "PlateZone"), Is.Null,
                "Transport destination zones must not remain after click interaction migration.");
            Assert.That(FindDeep(overlay, "LadleGuide"), Is.Null,
                "The boiling drag tool must not remain after click interaction migration.");

            MonoBehaviour[] temporaryLabels = overlay.GetComponentsInChildren<MonoBehaviour>(true)
                .Where(component => component != null
                    && component.name == "TemporaryLabel"
                    && component.GetType().Name == "TextMeshProUGUI")
                .ToArray();
            Assert.That(temporaryLabels.All(HasNonEmptyText), Is.True);
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
