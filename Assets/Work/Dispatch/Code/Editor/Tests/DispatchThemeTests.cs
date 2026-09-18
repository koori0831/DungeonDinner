using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Work.Cook.Code.Data;
using Work.Dispatch.Code.Data;
using Work.Dispatch.Code.UI;
using Object = UnityEngine.Object;

namespace Work.Dispatch.Code.Editor.Tests
{
    public sealed class DispatchThemeTests
    {
        private const string UiPath = "Assets/Work/Dispatch/UI/";
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameObject _host;
        private DispatchScreenPresenter _presenter;
        private CookingUiPresentationSettingsSO _theme;

        [SetUp]
        public void SetUp()
        {
            _theme = AssetDatabase.LoadAssetAtPath<CookingUiPresentationSettingsSO>(
                "Assets/Work/Cook/SO/CookingUiPresentationSettings.asset");
            Assert.That(_theme, Is.Not.Null);
            _host = new GameObject("Dispatch theme test");
            _host.SetActive(false);
            _presenter = _host.AddComponent<DispatchScreenPresenter>();
            SetField("presentationTheme", _theme);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_host);
        }

        [Test]
        public void ShippedPrefab_ReferencesTheSharedCookingTheme()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Work/Dispatch/Prefabs/DispatchUIRoot.prefab");
            Assert.That(prefab, Is.Not.Null);
            var settings = new SerializedObject(prefab.GetComponent<DispatchScreenPresenter>());
            Assert.That(settings.FindProperty("presentationTheme").objectReferenceValue, Is.SameAs(_theme));
        }

        [TestCase("DispatchNpcRow.uxml", "npc-row")]
        [TestCase("DispatchRegionRow.uxml", "region-row")]
        [TestCase("DispatchMaterialRow.uxml", "material-row")]
        [TestCase("DispatchReportRow.uxml", "report-row")]
        public void DynamicRow_UsesTheStyledRowAsItsBindingRoot(string templateName, string rowName)
        {
            VisualElement row = CreateRow(templateName);
            Assert.That(row, Is.Not.InstanceOf<TemplateContainer>(),
                "Selection classes must reach the .list-row element, not its unstyled template wrapper.");
            Assert.That(row.name, Is.EqualTo(rowName));
            Assert.That(row.ClassListContains("list-row"), Is.True);
        }

        [TestCase("DispatchMaterialRow.uxml", "decrease-button")]
        [TestCase("DispatchMaterialRow.uxml", "increase-button")]
        [TestCase("DispatchReportRow.uxml", "claim-button")]
        public void ButtonsCreatedAfterInitialThemeApplication_ReceiveSharedArtwork(string templateName, string buttonName)
        {
            SetField("_root", CreateScreen());
            Invoke("ApplySharedTheme");
            Button button = CreateRow(templateName).Q<Button>(buttonName);
            Assert.That(button, Is.Not.Null);
            Sprite sprite = button.style.backgroundImage.value.sprite;
            Assert.That(sprite, Is.Not.Null, "A virtualized list button retained its default Unity appearance.");
            Assert.That(sprite == _theme.PrimaryButtonSprite || sprite == _theme.SecondaryButtonSprite, Is.True);
        }

        [Test]
        public void ReboundNpcRow_AppliesAndClearsSelectionAndLockOnTheVisibleRow()
        {
            VisualElement row = CreateRow("DispatchNpcRow.uxml");
            SetField("_selectedNpcId", "npc-selected");
            Invoke("BindNpcRow", row, Model("NpcRowModel", "npc-selected", "동료", 3, 2, true));
            Assert.That(row.ClassListContains("is-selected"), Is.True);
            Assert.That(row.ClassListContains("is-locked"), Is.False);

            Invoke("BindNpcRow", row, Model("NpcRowModel", "npc-locked", "잠긴 동료", 0, 2, false));
            Assert.That(row.ClassListContains("is-selected"), Is.False);
            Assert.That(row.ClassListContains("is-locked"), Is.True);

            Invoke("BindNpcRow", row, Model("NpcRowModel", "npc-selected", "동료", 3, 2, true));
            Assert.That(row.ClassListContains("is-selected"), Is.True);
            Assert.That(row.ClassListContains("is-locked"), Is.False);
        }

        [Test]
        public void ReboundRegionAndMaterialRows_ClearTheirPreviousSelection()
        {
            var region = AssetDatabase.LoadAssetAtPath<DispatchRegionSO>(
                "Assets/Work/Dispatch/Data/Regions/MossCaveDispatch.asset");
            Assert.That(region, Is.Not.Null);
            VisualElement regionRow = CreateRow("DispatchRegionRow.uxml");
            SetField("_selectedRegionId", region.RegionId);
            Invoke("BindRegionRow", regionRow, Model("RegionRowModel", region));
            Assert.That(regionRow.ClassListContains("is-selected"), Is.True);
            SetField("_selectedRegionId", null);
            Invoke("BindRegionRow", regionRow, Model("RegionRowModel", region));
            Assert.That(regionRow.ClassListContains("is-selected"), Is.False);

            Assert.That(region.Materials.Count, Is.GreaterThan(0));
            VisualElement materialRow = CreateRow("DispatchMaterialRow.uxml");
            Invoke("BindMaterialRow", materialRow, Model("MaterialRowModel", region.Materials[0], 1));
            Assert.That(materialRow.ClassListContains("is-selected"), Is.True);
            Invoke("BindMaterialRow", materialRow, Model("MaterialRowModel", region.Materials[0], 0));
            Assert.That(materialRow.ClassListContains("is-selected"), Is.False);
        }

        [TestCase("modal-card")]
        [TestCase("status-card")]
        [TestCase("report-panel")]
        [TestCase("summary-slip")]
        public void SecondaryPagesAndConfirmation_ReceiveTheSharedPanelArtwork(string className)
        {
            VisualElement root = CreateScreen();
            SetField("_root", root);
            Invoke("ApplySharedTheme");
            VisualElement panel = root.Q<VisualElement>(className: className);
            Assert.That(panel, Is.Not.Null);
            Assert.That(ContainsSharedPanelSprite(panel), Is.True,
                className + " was omitted from the common theme targets.");
        }

        private bool ContainsSharedPanelSprite(VisualElement element)
        {
            Sprite sprite = element.style.backgroundImage.value.sprite;
            if (sprite != null && (sprite == _theme.PanelSprite || sprite == _theme.ReceiptSprite)) return true;
            foreach (VisualElement child in element.Children())
                if (ContainsSharedPanelSprite(child)) return true;
            return false;
        }

        private VisualElement CreateRow(string templateName)
        {
            var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPath + templateName);
            Assert.That(template, Is.Not.Null);
            return (VisualElement)Invoke("CreateThemedRow", template);
        }

        private static VisualElement CreateScreen()
        {
            var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPath + "DispatchScreen.uxml");
            Assert.That(template, Is.Not.Null);
            return template.CloneTree().Q<VisualElement>("dispatch-root");
        }

        private static object Model(string name, params object[] arguments)
        {
            Type type = typeof(DispatchScreenPresenter).GetNestedType(name, BindingFlags.NonPublic);
            Assert.That(type, Is.Not.Null, name);
            return Activator.CreateInstance(type, arguments);
        }

        private void SetField(string name, object value)
        {
            FieldInfo field = typeof(DispatchScreenPresenter).GetField(name, PrivateInstance);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(_presenter, value);
        }

        private object Invoke(string name, params object[] arguments)
        {
            MethodInfo method = typeof(DispatchScreenPresenter).GetMethod(name, PrivateInstance);
            Assert.That(method, Is.Not.Null, name);
            return method.Invoke(_presenter, arguments);
        }
    }
}
