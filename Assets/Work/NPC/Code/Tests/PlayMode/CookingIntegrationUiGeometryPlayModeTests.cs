using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace DungeonDinner.Npc.PlayModeTests
{
    public sealed class CookingIntegrationUiGeometryPlayModeTests
    {
        private const string ScenePath = "Assets/Work/Integration/Scene/DungeonDinnerScene.unity";
        private const float RatioEpsilon = 0.001f;
        private const float RowTolerance = 1f;
        private readonly List<string> _runtimeErrors = new List<string>();

        [SetUp]
        public void BeginLogCapture()
        {
            _runtimeErrors.Clear();
            LogAssert.ignoreFailingMessages = true;
            Application.logMessageReceived += HandleLog;
        }

        [TearDown]
        public void EndLogCapture()
        {
            Application.logMessageReceived -= HandleLog;
            LogAssert.ignoreFailingMessages = false;
        }

        [UnityTest]
        [Category("CookingIntegrationUiGeometry")]
        public IEnumerator DungeonDinner_RecipeCardsAreCenteredAndCookingViewIsVisible()
        {
            int buildIndex = SceneUtility.GetBuildIndexByScenePath(ScenePath);
            Assert.That(buildIndex, Is.GreaterThanOrEqualTo(0), ScenePath + " is not enabled in Build Settings.");

            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(buildIndex, LoadSceneMode.Single);
            Assert.That(loadOperation, Is.Not.Null);
            while (loadOperation.isDone == false)
                yield return null;

            yield return null;
            yield return new WaitForSecondsRealtime(0.5f);

            Scene scene = SceneManager.GetActiveScene();
            DisableBehaviour(scene, "Work.Cook.Code.Runtime.Systems.CookingBusinessFlowController");
            DisableBehaviour(scene, "Work.NPC.Code.Runtime.NpcConversationRunner");

            MonoBehaviour gamePanel = FindBehaviour(scene, "Work.Cook.Code.Runtime.UI.CookingGamePanel");
            Assert.That(gamePanel, Is.Not.Null, "DungeonDinnerScene has no CookingGamePanel.");

            SeedDiscoveredRecipesWithoutSaving(gamePanel);

            InvokePublic(gamePanel, "OpenRecipeSelection");
            yield return null;
            yield return null;
            Canvas.ForceUpdateCanvases();

            GridLayoutGroup grid = FindVisibleRecipeGrid(scene);
            Assert.That(grid, Is.Not.Null, "Recipe selection did not create a visible recipe card grid.");
            List<RectTransform> cards = FindRecipeCards(grid);
            Assert.That(cards.Count, Is.GreaterThan(0), "Recipe selection created no visible recipe cards.");

            Canvas canvas = grid.GetComponentInParent<Canvas>();
            RectTransform canvasRect = canvas != null ? canvas.rootCanvas.transform as RectTransform : null;
            RectTransform gridRectTransform = grid.transform as RectTransform;
            ScrollRect scrollRect = grid.GetComponentInParent<ScrollRect>();
            RectTransform viewport = scrollRect != null
                ? (scrollRect.viewport != null ? scrollRect.viewport : scrollRect.transform as RectTransform)
                : null;
            Assert.That(gridRectTransform, Is.Not.Null);
            Assert.That(viewport, Is.Not.Null);

            GeometryResult result = MeasureRecipeGeometry(cards, gridRectTransform, viewport, canvasRect);
            result.sceneName = scene.name;
            result.cardCount = cards.Count;

            InvokePublic(gamePanel, "OpenPreparation");
            yield return null;
            yield return null;
            Canvas.ForceUpdateCanvases();

            GameObject preparationView = GetPublicProperty<GameObject>(gamePanel, "PreparationView");
            Assert.That(preparationView, Is.Not.Null, "CookingGamePanel.PreparationView is missing.");
            Assert.That(preparationView.activeInHierarchy, Is.True, "CookingViewRoot is not visible after OpenPreparation.");
            Assert.That(
                HasBehaviour(preparationView, "Work.Cook.Code.Runtime.UI.CookingView"),
                Is.True,
                "PreparationView does not use the latest CookingView.");

            RectTransform preparationRectTransform = preparationView.transform as RectTransform;
            Assert.That(preparationRectTransform, Is.Not.Null);
            Canvas preparationCanvas = preparationView.GetComponentInParent<Canvas>();
            RectTransform preparationCanvasRect =
                preparationCanvas != null ? preparationCanvas.rootCanvas.transform as RectTransform : null;
            Rect preparationRect = ToCanvasRect(preparationRectTransform, preparationCanvasRect);
            Rect rootCanvasRect = ToCanvasRect(preparationCanvasRect, preparationCanvasRect);
            result.preparationWidth = preparationRect.width;
            result.preparationHeight = preparationRect.height;
            result.preparationOutsideCanvasRatio = OutsideRatio(preparationRect, rootCanvasRect);
            result.legacyPreparationViewCount = CountBehaviours(
                scene,
                "Work.Cook.Code.Runtime.UI.CookingPreparationView");
            result.temporaryPreparationObjectCount = CountNamedObjects(scene, "TemporaryPreparationView");
            result.temporaryResultObjectCount = CountNamedObjects(scene, "TemporaryResultView");

            GameObject resultView = GetPublicProperty<GameObject>(gamePanel, "ResultView");
            result.resultViewInactiveDuringPreparation = resultView != null && resultView.activeInHierarchy == false;
            result.relevantRuntimeErrorCount = _runtimeErrors.Count;
            WriteResult(result);
            Debug.Log("COOKING_INTEGRATION_UI_GEOMETRY " + JsonUtility.ToJson(result));

            Assert.That(result.maxCardOverlapRatio, Is.LessThanOrEqualTo(RatioEpsilon));
            Assert.That(result.maxCardOutsideViewportRatio, Is.LessThanOrEqualTo(RatioEpsilon));
            Assert.That(result.maxRowCenterError, Is.LessThanOrEqualTo(RowTolerance));
            Assert.That(result.preparationWidth, Is.GreaterThan(0f));
            Assert.That(result.preparationHeight, Is.GreaterThan(0f));
            Assert.That(result.preparationOutsideCanvasRatio, Is.LessThanOrEqualTo(RatioEpsilon));
            Assert.That(result.legacyPreparationViewCount, Is.Zero);
            Assert.That(result.temporaryPreparationObjectCount, Is.Zero);
            Assert.That(result.temporaryResultObjectCount, Is.Zero);
            Assert.That(result.resultViewInactiveDuringPreparation, Is.True);
            Assert.That(_runtimeErrors, Is.Empty, string.Join("\n", _runtimeErrors));
        }

        [UnityTest]
        [Category("DispatchIntegrationSmoke")]
        public IEnumerator DungeonDinner_DispatchOpensClosesAndSharesGameTimeService()
        {
            int buildIndex = SceneUtility.GetBuildIndexByScenePath(ScenePath);
            Assert.That(buildIndex, Is.GreaterThanOrEqualTo(0), ScenePath + " is not enabled in Build Settings.");

            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(buildIndex, LoadSceneMode.Single);
            Assert.That(loadOperation, Is.Not.Null);
            while (loadOperation.isDone == false)
                yield return null;

            yield return null;
            yield return new WaitForSecondsRealtime(0.5f);

            Scene scene = SceneManager.GetActiveScene();
            DisableBehaviour(scene, "Work.Cook.Code.Runtime.Systems.CookingBusinessFlowController");
            DisableBehaviour(scene, "Work.NPC.Code.Runtime.NpcConversationRunner");

            GameObject dispatchRoot = GameObject.Find("DispatchUIRoot");
            Assert.That(dispatchRoot, Is.Not.Null, "DungeonDinnerScene has no DispatchUIRoot.");

            MonoBehaviour gameTime = FindBehaviour(dispatchRoot, "Work.TimeSystem.GameTimeService");
            MonoBehaviour presenter = FindBehaviour(dispatchRoot, "Work.Dispatch.Code.UI.DispatchScreenPresenter");
            MonoBehaviour preparation = FindBehaviour(scene, "Work.Adventure.Code.PreparationManager");
            MonoBehaviour businessFlow = FindBehaviour(
                scene,
                "Work.Cook.Code.Runtime.Systems.CookingBusinessFlowController");
            Assert.That(gameTime, Is.Not.Null, "DispatchUIRoot has no GameTimeService.");
            Assert.That(presenter, Is.Not.Null, "DispatchUIRoot has no DispatchScreenPresenter.");
            Assert.That(preparation, Is.Not.Null, "DungeonDinnerScene has no PreparationManager.");
            Assert.That(businessFlow, Is.Not.Null, "Latest cooking presentation has no business flow controller.");
            Assert.That(GetInstanceField(businessFlow, "gameTimeService"), Is.SameAs(gameTime));
            Assert.That(GetInstanceField(presenter, "gameTimeService"), Is.SameAs(gameTime));

            InvokePublic(preparation, "SelectDispatch");
            yield return null;
            Assert.That(GetPublicValue<bool>(presenter, "IsVisible"), Is.True, "Dispatch UI did not open.");

            InvokeInstance(presenter, "Close", BindingFlags.Instance | BindingFlags.NonPublic);
            yield return null;
            Assert.That(GetPublicValue<bool>(presenter, "IsVisible"), Is.False, "Dispatch UI did not close.");
            Assert.That(_runtimeErrors, Is.Empty, string.Join("\n", _runtimeErrors));

            DispatchSmokeResult result = new DispatchSmokeResult
            {
                sceneName = scene.name,
                opened = true,
                closed = true,
                sharedGameTimeService = true,
                relevantRuntimeErrorCount = _runtimeErrors.Count
            };
            WriteDispatchResult(result);
            Debug.Log("DISPATCH_INTEGRATION_SMOKE " + JsonUtility.ToJson(result));
        }

        private GeometryResult MeasureRecipeGeometry(
            IReadOnlyList<RectTransform> cards,
            RectTransform grid,
            RectTransform viewport,
            RectTransform canvasRect)
        {
            GeometryResult result = new GeometryResult();
            Rect gridRect = ToCanvasRect(grid, canvasRect);
            Rect viewportRect = ToCanvasRect(viewport, canvasRect);
            List<Rect> cardRects = new List<Rect>();
            for (int i = 0; i < cards.Count; i++)
            {
                Rect cardRect = ToCanvasRect(cards[i], canvasRect);
                cardRects.Add(cardRect);
                result.maxCardOutsideViewportRatio = Mathf.Max(
                    result.maxCardOutsideViewportRatio,
                    OutsideRatio(cardRect, viewportRect));
                for (int otherIndex = 0; otherIndex < i; otherIndex++)
                {
                    result.maxCardOverlapRatio = Mathf.Max(
                        result.maxCardOverlapRatio,
                        IntersectionRatio(cardRect, cardRects[otherIndex]));
                }
            }

            List<List<Rect>> rows = new List<List<Rect>>();
            for (int i = 0; i < cardRects.Count; i++)
            {
                Rect card = cardRects[i];
                List<Rect> row = null;
                for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
                {
                    if (Mathf.Abs(rows[rowIndex][0].center.y - card.center.y) <= RowTolerance)
                    {
                        row = rows[rowIndex];
                        break;
                    }
                }

                if (row == null)
                {
                    row = new List<Rect>();
                    rows.Add(row);
                }
                row.Add(card);
            }

            for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                float minX = float.PositiveInfinity;
                float maxX = float.NegativeInfinity;
                for (int cardIndex = 0; cardIndex < rows[rowIndex].Count; cardIndex++)
                {
                    minX = Mathf.Min(minX, rows[rowIndex][cardIndex].xMin);
                    maxX = Mathf.Max(maxX, rows[rowIndex][cardIndex].xMax);
                }
                result.maxRowCenterError = Mathf.Max(
                    result.maxRowCenterError,
                    Mathf.Abs((minX + maxX) * 0.5f - gridRect.center.x));
            }

            return result;
        }

        private static GridLayoutGroup FindVisibleRecipeGrid(Scene scene)
        {
            GridLayoutGroup[] grids = UnityEngine.Object.FindObjectsByType<GridLayoutGroup>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            GridLayoutGroup best = null;
            int bestCount = 0;
            for (int i = 0; i < grids.Length; i++)
            {
                if (grids[i] == null || grids[i].gameObject.scene != scene)
                    continue;
                int count = FindRecipeCards(grids[i]).Count;
                if (count > bestCount)
                {
                    best = grids[i];
                    bestCount = count;
                }
            }
            return best;
        }

        private static List<RectTransform> FindRecipeCards(GridLayoutGroup grid)
        {
            List<RectTransform> results = new List<RectTransform>();
            if (grid == null)
                return results;
            for (int i = 0; i < grid.transform.childCount; i++)
            {
                Transform child = grid.transform.GetChild(i);
                if (child.gameObject.activeInHierarchy
                    && HasBehaviour(child.gameObject, "Work.Cook.Code.Info.InfoSelectBtn")
                    && child is RectTransform rectTransform)
                {
                    results.Add(rectTransform);
                }
            }
            return results;
        }

        private static bool HasBehaviour(GameObject owner, string fullTypeName)
        {
            MonoBehaviour[] behaviours = owner.GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] != null
                    && string.Equals(behaviours[i].GetType().FullName, fullTypeName, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        private static MonoBehaviour FindBehaviour(Scene scene, string fullTypeName)
        {
            MonoBehaviour[] behaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] != null
                    && behaviours[i].gameObject.scene == scene
                    && string.Equals(behaviours[i].GetType().FullName, fullTypeName, StringComparison.Ordinal))
                {
                    return behaviours[i];
                }
            }
            return null;
        }

        private static MonoBehaviour FindBehaviour(GameObject owner, string fullTypeName)
        {
            if (owner == null)
                return null;
            MonoBehaviour[] behaviours = owner.GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] != null
                    && string.Equals(behaviours[i].GetType().FullName, fullTypeName, StringComparison.Ordinal))
                {
                    return behaviours[i];
                }
            }
            return null;
        }

        private static void DisableBehaviour(Scene scene, string fullTypeName)
        {
            MonoBehaviour behaviour = FindBehaviour(scene, fullTypeName);
            if (behaviour == null)
                return;
            behaviour.StopAllCoroutines();
            behaviour.enabled = false;
        }

        private static int CountBehaviours(Scene scene, string fullTypeName)
        {
            int count = 0;
            MonoBehaviour[] behaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] != null
                    && behaviours[i].gameObject.scene == scene
                    && string.Equals(behaviours[i].GetType().FullName, fullTypeName, StringComparison.Ordinal))
                {
                    count++;
                }
            }
            return count;
        }

        private static int CountNamedObjects(Scene scene, string objectName)
        {
            int count = 0;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform[] transforms = roots[i].GetComponentsInChildren<Transform>(true);
                for (int j = 0; j < transforms.Length; j++)
                {
                    if (string.Equals(transforms[j].name, objectName, StringComparison.Ordinal))
                        count++;
                }
            }
            return count;
        }

        private static void InvokePublic(object owner, string methodName)
        {
            InvokeInstance(owner, methodName, BindingFlags.Instance | BindingFlags.Public);
        }

        private static void InvokePublic(object owner, string methodName, object argument)
        {
            MethodInfo method = owner.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
            Assert.That(method, Is.Not.Null, owner.GetType().Name + "." + methodName + " was not found.");
            method.Invoke(owner, new[] { argument });
        }

        private static void InvokeInstance(object owner, string methodName, BindingFlags bindingFlags)
        {
            MethodInfo method = owner.GetType().GetMethod(methodName, bindingFlags);
            Assert.That(method, Is.Not.Null, owner.GetType().Name + "." + methodName + " was not found.");
            method.Invoke(owner, null);
        }

        private static T GetPublicProperty<T>(object owner, string propertyName) where T : class
        {
            PropertyInfo property = owner.GetType().GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public);
            return property != null ? property.GetValue(owner) as T : null;
        }

        private static T GetPublicValue<T>(object owner, string propertyName)
        {
            PropertyInfo property = owner.GetType().GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public);
            Assert.That(property, Is.Not.Null, owner.GetType().Name + "." + propertyName + " was not found.");
            return (T)property.GetValue(owner);
        }

        private static object GetInstanceField(object owner, string fieldName)
        {
            FieldInfo field = owner.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, owner.GetType().Name + "." + fieldName + " was not found.");
            return field.GetValue(owner);
        }

        private static void SetInstanceField(object owner, string fieldName, object value)
        {
            FieldInfo field = owner.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, owner.GetType().Name + "." + fieldName + " was not found.");
            field.SetValue(owner, value);
        }

        private static void SeedDiscoveredRecipesWithoutSaving(MonoBehaviour gamePanel)
        {
            MonoBehaviour flowRunner = GetPublicProperty<MonoBehaviour>(gamePanel, "FlowRunner");
            MonoBehaviour knowledgeStore = GetPublicProperty<MonoBehaviour>(gamePanel, "KnowledgeStore");
            Assert.That(flowRunner, Is.Not.Null, "CookingGamePanel.FlowRunner is missing.");
            Assert.That(knowledgeStore, Is.Not.Null, "CookingGamePanel.KnowledgeStore is missing.");

            SetInstanceField(knowledgeStore, "saveToPlayerPrefs", false);
            object recipesValue = GetPublicValue<object>(flowRunner, "Recipes");
            IEnumerable recipes = recipesValue as IEnumerable;
            Assert.That(recipes, Is.Not.Null, "CookingFlowRunner.Recipes is unavailable.");

            int discoveredCount = 0;
            foreach (object recipe in recipes)
            {
                if (recipe == null)
                    continue;
                InvokePublic(knowledgeStore, "DiscoverRecipe", recipe);
                discoveredCount++;
            }
            Assert.That(discoveredCount, Is.GreaterThan(0), "Cooking catalog contains no recipes to measure.");
        }

        private static Rect ToCanvasRect(RectTransform target, RectTransform canvasRect)
        {
            if (target == null)
                return new Rect();
            Vector3[] corners = new Vector3[4];
            target.GetWorldCorners(corners);
            Vector3 bottomLeft = canvasRect != null ? canvasRect.InverseTransformPoint(corners[0]) : corners[0];
            Vector3 topRight = canvasRect != null ? canvasRect.InverseTransformPoint(corners[2]) : corners[2];
            return Rect.MinMaxRect(
                Mathf.Min(bottomLeft.x, topRight.x),
                Mathf.Min(bottomLeft.y, topRight.y),
                Mathf.Max(bottomLeft.x, topRight.x),
                Mathf.Max(bottomLeft.y, topRight.y));
        }

        private static float IntersectionArea(Rect first, Rect second)
        {
            float width = Mathf.Max(0f, Mathf.Min(first.xMax, second.xMax) - Mathf.Max(first.xMin, second.xMin));
            float height = Mathf.Max(0f, Mathf.Min(first.yMax, second.yMax) - Mathf.Max(first.yMin, second.yMin));
            return width * height;
        }

        private static float IntersectionRatio(Rect first, Rect second)
        {
            float smallerArea = Mathf.Min(first.width * first.height, second.width * second.height);
            return smallerArea > 0f ? IntersectionArea(first, second) / smallerArea : 0f;
        }

        private static float OutsideRatio(Rect child, Rect parent)
        {
            float area = child.width * child.height;
            return area > 0f ? Mathf.Clamp01(1f - IntersectionArea(child, parent) / area) : 0f;
        }

        private void HandleLog(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)
                return;
            string text = (condition ?? string.Empty) + "\n" + (stackTrace ?? string.Empty);
            if (text.Contains("Work.Cook", StringComparison.Ordinal)
                || text.Contains("Work.NPC", StringComparison.Ordinal)
                || text.Contains("Work.Adventure", StringComparison.Ordinal)
                || text.Contains("Work.Dispatch", StringComparison.Ordinal)
                || text.Contains("Work.TimeSystem", StringComparison.Ordinal)
                || text.Contains("CookingGamePanel", StringComparison.Ordinal))
            {
                _runtimeErrors.Add("[" + type + "] " + condition + "\n" + stackTrace);
            }
        }

        private static void WriteResult(GeometryResult result)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            string outputDirectory = Path.Combine(projectRoot, "Temp", "CookingIntegrationUiGeometry");
            Directory.CreateDirectory(outputDirectory);
            File.WriteAllText(
                Path.Combine(outputDirectory, result.sceneName + ".json"),
                JsonUtility.ToJson(result, true));
        }

        private static void WriteDispatchResult(DispatchSmokeResult result)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            string outputDirectory = Path.Combine(projectRoot, "Temp", "DispatchIntegrationSmoke");
            Directory.CreateDirectory(outputDirectory);
            File.WriteAllText(
                Path.Combine(outputDirectory, result.sceneName + ".json"),
                JsonUtility.ToJson(result, true));
        }

        [Serializable]
        private sealed class GeometryResult
        {
            public string sceneName;
            public int cardCount;
            public float maxCardOverlapRatio;
            public float maxCardOutsideViewportRatio;
            public float maxRowCenterError;
            public float preparationWidth;
            public float preparationHeight;
            public float preparationOutsideCanvasRatio;
            public int legacyPreparationViewCount;
            public int temporaryPreparationObjectCount;
            public int temporaryResultObjectCount;
            public bool resultViewInactiveDuringPreparation;
            public int relevantRuntimeErrorCount;
        }

        [Serializable]
        private sealed class DispatchSmokeResult
        {
            public string sceneName;
            public bool opened;
            public bool closed;
            public bool sharedGameTimeService;
            public int relevantRuntimeErrorCount;
        }
    }
}
