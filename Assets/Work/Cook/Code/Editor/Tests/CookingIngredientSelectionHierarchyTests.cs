using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DungeonDinner.Cook.EditorTests
{
    public sealed class CookingIngredientSelectionHierarchyTests
    {
        private const string CookScenePath = "Assets/Work/Cook/Scene/CookTestScene.unity";
        private const string RecipeScrollPrefabPath = "Assets/Work/Cook/Prefabs/UI/Scroll View.prefab";
        private const float LayoutTolerance = 0.5f;

        [TearDown]
        public void TearDown()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [Test]
        public void IngredientBag_HasIndependentFourColumnPopupAndReachableControls()
        {
            Scene scene = EditorSceneManager.OpenScene(CookScenePath, OpenSceneMode.Single);
            Transform selectionView = FindTransform(scene, "TemporaryIngredientSelectionView");
            Assert.That(selectionView, Is.Not.Null);
            Assert.That(selectionView.parent.GetComponent<Canvas>(), Is.Not.Null, "Bag must be independent of the dictionary.");
            selectionView.gameObject.SetActive(true);
            var selectionRect = (RectTransform)selectionView;
            Assert.That(selectionRect.sizeDelta, Is.EqualTo(new Vector2(720,680)));
            Assert.That(selectionView.Find("TitleBar"), Is.Not.Null);
            foreach (string name in new[] { "Collapse", "Close" })
                Assert.That(selectionView.Find("TitleBar/" + name).GetComponent<Button>(), Is.Not.Null);
            foreach (var button in selectionView.GetComponentsInChildren<Button>(true))
            {
                Assert.That(((RectTransform)button.transform).rect.height, Is.GreaterThanOrEqualTo(40));
                AssertRectIsContainedBy((RectTransform)button.transform, selectionRect);
            }
            var grids = selectionView.GetComponentsInChildren<GridLayoutGroup>(true);
            Assert.That(grids.Length, Is.EqualTo(2));
            foreach (var grid in grids)
            {
                Assert.That(grid.constraintCount, Is.EqualTo(4));
                Assert.That(grid.cellSize, Is.EqualTo(new Vector2(150,112)));
                Assert.That(grid.GetComponentInParent<ScrollRect>(true), Is.Not.Null);
            }
            Assert.That(selectionView.parent.Find("IngredientBagButton").GetComponent<Button>(), Is.Not.Null);
        }

        [Test]
        public void RecipeScrollPrefab_CentersIncompleteThreeColumnRows()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RecipeScrollPrefabPath);
            Assert.That(prefab, Is.Not.Null);

            GridLayoutGroup grid = prefab.GetComponentInChildren<GridLayoutGroup>(true);
            Assert.That(grid, Is.Not.Null);
            Assert.That(grid.childAlignment, Is.EqualTo(TextAnchor.UpperCenter));
            Assert.That(grid.padding.left, Is.EqualTo(18));
            Assert.That(grid.padding.right, Is.EqualTo(18));

            MonoBehaviour field = null;
            foreach (MonoBehaviour candidate in prefab.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (candidate != null && candidate.GetType().Name == "InfoDictionaryScrollViewField")
                {
                    field = candidate;
                    break;
                }
            }

            Assert.That(field, Is.Not.Null);
            SerializedObject serializedField = new SerializedObject(field);
            Assert.That(serializedField.FindProperty("columnsPerRow").intValue, Is.EqualTo(3));
            Assert.That(serializedField.FindProperty("centeredHorizontalPadding").intValue, Is.EqualTo(18));
        }

        private static Transform FindTransform(Scene scene, string objectName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform found = FindTransform(root.transform, objectName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static Transform FindTransform(Transform current, string objectName)
        {
            if (current.name == objectName)
            {
                return current;
            }

            for (int index = 0; index < current.childCount; index++)
            {
                Transform found = FindTransform(current.GetChild(index), objectName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static void AssertRectIsContainedBy(RectTransform child, RectTransform parent)
        {
            Vector3[] worldCorners = new Vector3[4];
            child.GetWorldCorners(worldCorners);
            Rect parentRect = parent.rect;

            foreach (Vector3 worldCorner in worldCorners)
            {
                Vector3 localCorner = parent.InverseTransformPoint(worldCorner);
                bool isContained = localCorner.x >= parentRect.xMin - LayoutTolerance
                    && localCorner.x <= parentRect.xMax + LayoutTolerance
                    && localCorner.y >= parentRect.yMin - LayoutTolerance
                    && localCorner.y <= parentRect.yMax + LayoutTolerance;

                Assert.That(
                    isContained,
                    Is.True,
                    $"{child.name} extends outside {parent.name}. Corner: {localCorner}, bounds: {parentRect}.");
            }
        }
    }
}
