using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using Work.Adventure.Code;
using Work.Dispatch.Code.Data;
using Work.Dispatch.Code.Runtime;
using Work.Dispatch.Code.UI;
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
        public void AdventureScene_ContainsDispatchPrefabInstance()
        {
            EditorSceneManager.OpenScene(
                "Assets/Work/Adventure/Scene/AdventureTestScene.unity",
                OpenSceneMode.Single);

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

        private static void AssertSerializedReference(Object target, string propertyName)
        {
            Assert.That(target, Is.Not.Null, $"{propertyName} 참조를 검사할 대상이 없습니다.");
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            Assert.That(property, Is.Not.Null, $"{target.GetType().Name}.{propertyName} 필드를 찾을 수 없습니다.");
            Assert.That(property.objectReferenceValue, Is.Not.Null,
                $"{target.GetType().Name}.{propertyName} 참조가 연결되지 않았습니다.");
        }
    }
}
