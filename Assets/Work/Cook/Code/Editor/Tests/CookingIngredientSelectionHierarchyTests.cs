using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DungeonDinner.Cook.EditorTests
{
    public sealed class CookingIngredientSelectionHierarchyTests
    {
        private const string CookScenePath = "Assets/Work/Cook/Scene/CookTestScene.unity";
        private const float LayoutTolerance = 0.5f;

        [TearDown]
        public void TearDown()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [Test]
        public void IngredientActionButtons_HaveHierarchyOwnedHeight_AndRemainInsideSelectionView()
        {
            Scene scene = EditorSceneManager.OpenScene(CookScenePath, OpenSceneMode.Single);
            Transform selectionView = FindTransform(scene, "TemporaryIngredientSelectionView");

            Assert.That(selectionView, Is.Not.Null, "The ingredient selection view is missing from CookTestScene.");

            Transform actionRow = selectionView.Find("ActionRow");
            Assert.That(actionRow, Is.Not.Null, "The ingredient selection action row is missing.");
            Assert.That(actionRow.childCount, Is.EqualTo(2), "The action row must contain clear and confirm buttons.");

            selectionView.gameObject.SetActive(true);
            RectTransform selectionRect = selectionView.GetComponent<RectTransform>();
            LayoutRebuilder.ForceRebuildLayoutImmediate(selectionRect);
            Canvas.ForceUpdateCanvases();

            for (int index = 0; index < actionRow.childCount; index++)
            {
                RectTransform buttonRect = actionRow.GetChild(index) as RectTransform;
                Assert.That(buttonRect, Is.Not.Null);

                LayoutElement buttonLayout = buttonRect.GetComponent<LayoutElement>();
                Assert.That(
                    buttonLayout,
                    Is.Not.Null,
                    $"{buttonRect.name} must declare its size in the scene hierarchy instead of relying on runtime layout code.");
                Assert.That(buttonLayout.ignoreLayout, Is.False);
                Assert.That(buttonLayout.preferredHeight, Is.GreaterThan(0f));
                Assert.That(buttonLayout.flexibleHeight, Is.EqualTo(0f));
                Assert.That(buttonRect.rect.height, Is.GreaterThan(0f));

                AssertRectIsContainedBy(buttonRect, selectionRect);
            }
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

