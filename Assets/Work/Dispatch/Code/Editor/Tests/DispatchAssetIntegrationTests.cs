using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Work.Adventure.Code;
using Work.Adventure.Code.UI;
using Work.Cook.Code.Runtime.Systems;
using Work.Cook.Code.Runtime.UI;
using Work.Dispatch.Code.Data;
using Work.Dispatch.Code.Runtime;
using Work.Dispatch.Code.UI;
using Work.NPC.Code.Runtime;
using Work.TimeSystem;

namespace Work.Dispatch.Code.Editor.Tests
{
    public sealed class DispatchAssetIntegrationTests
    {
        [Test]
        public void Catalog_HasNoValidationErrors()
        {
            DispatchCatalogSO catalog = AssetDatabase.LoadAssetAtPath<DispatchCatalogSO>(
                "Assets/Work/Dispatch/Data/DispatchCatalog.asset");

            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.BuildValidationMessages(), Is.Empty);
            Assert.That(catalog.ItemCatalog, Is.Not.Null);
            Assert.That(catalog.ItemCatalog.BuildValidationMessages(), Is.Empty);
        }

        [Test]
        public void ScreenUxml_ContainsAllStaticPagesAndModal()
        {
            VisualTreeAsset screen = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/Work/Dispatch/UI/DispatchScreen.uxml");

            Assert.That(screen, Is.Not.Null);
            TemplateContainer tree = screen.CloneTree();
            Assert.That(tree.Q<VisualElement>("dispatch-root"), Is.Not.Null);
            Assert.That(tree.Q<VisualElement>("request-page"), Is.Not.Null);
            Assert.That(tree.Q<VisualElement>("active-page"), Is.Not.Null);
            Assert.That(tree.Q<VisualElement>("report-page"), Is.Not.Null);
            Assert.That(tree.Q<VisualElement>("confirmation-modal"), Is.Not.Null);
        }

        [Test]
        public void Prefab_HasActualUidocumentAndRuntimeComponents()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Work/Dispatch/Prefabs/DispatchUIRoot.prefab");

            Assert.That(prefab, Is.Not.Null);
            UIDocument document = prefab.GetComponent<UIDocument>();
            Assert.That(document, Is.Not.Null);
            Assert.That(document.panelSettings, Is.Not.Null);
            Assert.That(document.visualTreeAsset, Is.Not.Null);
            Assert.That(prefab.GetComponent<GameTimeService>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<DispatchManager>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<DispatchNpcQuery>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<DispatchScreenPresenter>(), Is.Not.Null);

            GameObject adventureCanvasPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Work/Adventure/Prefabs/AdventureCanvas.prefab");
            Assert.That(adventureCanvasPrefab, Is.Not.Null);
            Canvas adventureCanvas = adventureCanvasPrefab.GetComponent<Canvas>();
            Assert.That(adventureCanvas, Is.Not.Null);
            Assert.That(document.sortingOrder, Is.LessThan(adventureCanvas.sortingOrder),
                "파견 UI가 기존 페이드 Canvas보다 앞에 오면 화면 전환이 가려집니다.");
        }

        [Test]
        public void AdventureMapButtons_HaveAllRequiredSerializedReferences()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Work/Adventure/Prefabs/AdventureCanvas.prefab");
            Assert.That(prefab, Is.Not.Null);

            MapSelectButton[] mapButtons = prefab.GetComponentsInChildren<MapSelectButton>(true);
            Assert.That(mapButtons, Is.Not.Empty);
            for (int i = 0; i < mapButtons.Length; i++)
            {
                AssertSerializedReference(mapButtons[i], "buttonImage");
                AssertSerializedReference(mapButtons[i], "button");
                AssertSerializedReference(mapButtons[i], "buttonText");
            }
        }

        [TestCase(false, 0, "가능")]
        [TestCase(true, 0, "진행 중")]
        [TestCase(false, 2, "보고서 2건")]
        [TestCase(true, 2, "진행 중 · 보고서 2건")]
        public void PreparationDispatchStatus_FormatsAllRuntimeStates(
            bool hasActiveJob,
            int reportCount,
            string expected)
        {
            MethodInfo formatter = typeof(PreparationManager).GetMethod(
                "FormatDispatchStatus",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(formatter, Is.Not.Null);
            Assert.That(formatter.Invoke(null, new object[] { hasActiveJob, reportCount }), Is.EqualTo(expected));
        }

        [Test]
        public void CookingBusinessActionPanel_IsAboveResultActionsAndDoesNotBlockBackgroundRaycasts()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Work/Cook/Prefabs/UI/CookingPresentationRoot.prefab");
            Assert.That(prefab, Is.Not.Null);

            RectTransform businessPanel = FindNamedTransform(prefab, "BusinessActionPanel") as RectTransform;
            RectTransform resultActions = FindNamedTransform(prefab, "ResultActions") as RectTransform;
            Assert.That(businessPanel, Is.Not.Null);
            Assert.That(resultActions, Is.Not.Null);
            Assert.That(businessPanel.anchoredPosition.y, Is.EqualTo(126f).Within(0.01f));
            Assert.That(businessPanel.sizeDelta.y, Is.EqualTo(66f).Within(0.01f));
            Assert.That(
                businessPanel.anchoredPosition.y - resultActions.anchoredPosition.y - resultActions.sizeDelta.y,
                Is.GreaterThanOrEqualTo(16f));

            UnityEngine.UI.Image background = businessPanel.GetComponent<UnityEngine.UI.Image>();
            Assert.That(background, Is.Not.Null);
            Assert.That(background.raycastTarget, Is.False);
        }

        [TestCase("Assets/Work/Adventure/Scene/AdventureTestScene.unity")]
        [TestCase(AdventureSceneIntegrationSetup.IntegrationScenePath)]
        public void IntegratedScene_ContainsDispatchPrefabInstance(string scenePath)
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            GameObject root = GameObject.Find("DispatchUIRoot");
            Assert.That(root, Is.Not.Null);
            Assert.That(root.GetComponent<UIDocument>(), Is.Not.Null);
            DispatchManager manager = root.GetComponent<DispatchManager>();
            Assert.That(manager, Is.Not.Null);

            AssertSerializedReference(manager, "catalog");
            AssertSerializedReference(manager, "gameTimeService");
            AssertSerializedReference(manager, "playerInventory");
            AssertSerializedReference(root.GetComponent<DispatchNpcQuery>(), "encounterDirector");

            PreparationManager preparationManager = Object.FindFirstObjectByType<PreparationManager>();
            Assert.That(preparationManager, Is.Not.Null);
            AssertSerializedReference(preparationManager, "dispatchScreen");
        }

        [Test]
        public void RuntimeIntegrationScene_UsesLatestCookingPresentationWithoutLegacyViews()
        {
            Scene scene = EditorSceneManager.OpenScene(
                AdventureSceneIntegrationSetup.IntegrationScenePath,
                OpenSceneMode.Single);

            GameObject[] presentationRoots = FindPrefabInstanceRoots(
                scene,
                AdventureSceneIntegrationSetup.CookingPresentationPrefabPath);
            Assert.That(presentationRoots, Has.Length.EqualTo(1));
            GameObject presentationRoot = presentationRoots[0];

            CookingGamePanel[] panels = FindSceneComponents<CookingGamePanel>(scene);
            Assert.That(panels, Has.Length.EqualTo(1));
            CookingGamePanel panel = panels[0];
            Assert.That(panel.PreparationView, Is.Not.Null);
            Assert.That(panel.PreparationView.GetComponent<CookingView>(), Is.Not.Null);
            AssertReferenceInside(panel, "preparationView", presentationRoot);
            AssertReferenceInside(panel, "miniGameView", presentationRoot);
            AssertReferenceInside(panel, "resultView", presentationRoot);
            AssertReferenceInside(panel, "knowledgeUpdateView", presentationRoot);
            AssertReferenceInside(panel, "rewardView", presentationRoot);
            AssertReferenceInside(panel, "businessFlowController", presentationRoot);

            Assert.That(FindSceneComponents<CookingPreparationView>(scene), Is.Empty);
            Assert.That(FindSceneComponents<CookingBusinessFlowController>(scene), Has.Length.EqualTo(1));
            Assert.That(FindSceneComponents<AdventureManager>(scene), Has.Length.EqualTo(1));
            Assert.That(FindSceneComponents<PreparationManager>(scene), Has.Length.EqualTo(1));
            Assert.That(FindSceneComponents<GameTimeService>(scene), Has.Length.EqualTo(1));

            NpcConversationRunner[] runners = FindSceneComponents<NpcConversationRunner>(scene);
            Assert.That(runners, Has.Length.EqualTo(1));
            AssertReferenceInside(runners[0], "orderSlipPanel", presentationRoot);

            CookingBusinessFlowController businessFlow =
                FindSceneComponents<CookingBusinessFlowController>(scene)[0];
            GameObject dispatchRoot = GameObject.Find("DispatchUIRoot");
            Assert.That(dispatchRoot, Is.Not.Null);
            AssertReferenceEquals(
                businessFlow,
                "gameTimeService",
                dispatchRoot.GetComponent<GameTimeService>());
        }

        [Test]
        public void RuntimeIntegrationScene_HasExactlyOneActiveAudioListener()
        {
            Scene scene = EditorSceneManager.OpenScene(
                AdventureSceneIntegrationSetup.IntegrationScenePath,
                OpenSceneMode.Single);
            AudioListener[] listeners = FindSceneComponents<AudioListener>(scene);
            int activeCount = 0;
            for (int i = 0; i < listeners.Length; i++)
            {
                if (listeners[i].isActiveAndEnabled)
                    activeCount++;
            }

            Assert.That(activeCount, Is.EqualTo(1));
        }

        private static void AssertSerializedReference(Object target, string propertyName)
        {
            Assert.That(target, Is.Not.Null, $"{propertyName} 참조를 검사할 대상이 없습니다.");
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            Assert.That(property, Is.Not.Null, $"{target.GetType().Name}.{propertyName} 필드를 찾을 수 없습니다.");
            Assert.That(property.objectReferenceValue, Is.Not.Null,
                $"{target.GetType().Name}.{propertyName} 참조가 연결되지 않았습니다.");
        }

        private static void AssertReferenceInside(Object target, string propertyName, GameObject expectedRoot)
        {
            Object value = GetSerializedReference(target, propertyName);
            GameObject valueObject = value as GameObject;
            if (valueObject == null && value is Component component)
                valueObject = component.gameObject;

            Assert.That(valueObject, Is.Not.Null, $"{target.GetType().Name}.{propertyName} is missing.");
            Assert.That(
                valueObject == expectedRoot || valueObject.transform.IsChildOf(expectedRoot.transform),
                Is.True,
                $"{target.GetType().Name}.{propertyName} is outside CookingPresentationRoot.");
        }

        private static void AssertReferenceEquals(Object target, string propertyName, Object expected)
        {
            Assert.That(GetSerializedReference(target, propertyName), Is.SameAs(expected));
        }

        private static Object GetSerializedReference(Object target, string propertyName)
        {
            Assert.That(target, Is.Not.Null);
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            Assert.That(property, Is.Not.Null);
            return property.objectReferenceValue;
        }

        private static T[] FindSceneComponents<T>(Scene scene) where T : Component
        {
            System.Collections.Generic.List<T> results = new System.Collections.Generic.List<T>();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
                results.AddRange(roots[i].GetComponentsInChildren<T>(true));
            return results.ToArray();
        }

        private static Transform FindNamedTransform(GameObject root, string objectName)
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                if (string.Equals(transforms[i].name, objectName, System.StringComparison.Ordinal))
                    return transforms[i];
            }
            return null;
        }

        private static GameObject[] FindPrefabInstanceRoots(Scene scene, string prefabPath)
        {
            System.Collections.Generic.List<GameObject> results =
                new System.Collections.Generic.List<GameObject>();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                Transform[] transforms = roots[rootIndex].GetComponentsInChildren<Transform>(true);
                for (int transformIndex = 0; transformIndex < transforms.Length; transformIndex++)
                {
                    GameObject candidate = transforms[transformIndex].gameObject;
                    if (PrefabUtility.IsAnyPrefabInstanceRoot(candidate) == false)
                        continue;
                    if (string.Equals(
                            PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(candidate),
                            prefabPath,
                            System.StringComparison.OrdinalIgnoreCase))
                    {
                        results.Add(candidate);
                    }
                }
            }
            return results.ToArray();
        }
    }
}
