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
    public sealed class NpcQuestionUiGeometryPlayModeTests
    {
        private const string CategoryName = "NpcQuestionUiGeometry";
        private const float OverlapAreaEpsilon = 0.5f;
        private const float OverlapRatioEpsilon = 0.001f;
        private const int QuestionCount = 4;
        private const int SampleCount = 12;

        private static readonly string[] QuestionLabels =
        {
            "맛",
            "온도/식감",
            "몸 상태",
            "피하고 싶은 음식"
        };

        [UnityTest]
        [Category(CategoryName)]
        public IEnumerator CookTestScene_QuestionUiRegionsDoNotOverlap()
        {
            return MeasureScene("Assets/Work/Cook/Scene/CookTestScene.unity");
        }

        [UnityTest]
        [Category(CategoryName)]
        public IEnumerator AdventureTestScene_QuestionUiRegionsDoNotOverlap()
        {
            return MeasureScene("Assets/Work/Adventure/Scene/AdventureTestScene.unity");
        }

        [UnityTest]
        [Category(CategoryName)]
        public IEnumerator DungeonDinnerScene_QuestionUiRegionsDoNotOverlap()
        {
            return MeasureScene("Assets/Work/Integration/Scene/DungeonDinnerScene.unity");
        }

        [TearDown]
        public void RestoreLogAssertionState()
        {
            LogAssert.ignoreFailingMessages = false;
        }

        private static IEnumerator MeasureScene(string scenePath)
        {
            LogAssert.ignoreFailingMessages = true;

            int buildIndex = SceneUtility.GetBuildIndexByScenePath(scenePath);
            Assert.That(buildIndex, Is.GreaterThanOrEqualTo(0), scenePath + " is not enabled in Build Settings.");

            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(buildIndex, LoadSceneMode.Single);
            Assert.That(loadOperation, Is.Not.Null, "Could not start loading " + scenePath);
            while (loadOperation.isDone == false)
                yield return null;

            yield return null;
            yield return new WaitForSecondsRealtime(0.75f);

            Scene scene = SceneManager.GetActiveScene();
            MonoBehaviour questionPanel = FindBehaviour(scene, "Work.NPC.Code.Runtime.NpcQuestionPanel");
            Assert.That(questionPanel, Is.Not.Null, scenePath + " has no NpcQuestionPanel.");

            StopConversationPlayback(scene);

            RectTransform optionRoot = GetFieldValue<RectTransform>(questionPanel, "optionRoot");
            Assert.That(optionRoot, Is.Not.Null, scenePath + " has no optionRoot.");

            MonoBehaviour chatPanel = FindBehaviour(scene, "Work.Chat.Code.ChatPanel");
            Assert.That(chatPanel, Is.Not.Null, scenePath + " has no ChatPanel.");
            RectTransform chatContent = GetFieldValue<RectTransform>(chatPanel, "contentTrm");
            Assert.That(chatContent, Is.Not.Null, scenePath + " ChatPanel has no contentTrm.");

            BuildDeterministicConversation(chatPanel);
            InvokeQuestionOptions(questionPanel);

            yield return null;
            yield return null;
            Canvas.ForceUpdateCanvases();

            SceneGeometryResult result = CreateInitialResult(scenePath, scene, questionPanel, optionRoot, chatContent);
            for (int sampleIndex = 0; sampleIndex < SampleCount; sampleIndex++)
            {
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(optionRoot);
                UpdateMeasurements(result, scene, optionRoot, chatContent);
                yield return new WaitForSecondsRealtime(0.025f);
            }

            result.sampleCount = SampleCount;
            WriteResult(result);
            Debug.Log("NPC_QUESTION_UI_GEOMETRY " + JsonUtility.ToJson(result));

            bool passed = result.activeButtonCount == QuestionCount + 1
                          && result.hasVerticalLayoutGroup
                          && result.hasContentSizeFitter
                          && result.conversationContentRootAssigned
                          && result.maxOverlappingButtonPairs == 0
                          && result.maxButtonPairOverlapRatio <= OverlapRatioEpsilon
                          && result.maxButtonChatBubbleOverlapRatio <= OverlapRatioEpsilon
                          && result.buttonContainerViewportOverlapRatio <= OverlapRatioEpsilon
                          && result.maxButtonOutsideContainerRatio <= OverlapRatioEpsilon;

            Assert.That(
                passed,
                Is.True,
                $"{scenePath} UI geometry overlap: " +
                $"buttons={result.activeButtonCount}, " +
                $"overlappingPairs={result.maxOverlappingButtonPairs}/{result.buttonPairCount}, " +
                $"buttonOverlap={result.maxButtonPairOverlapCanvasUnits2:F2}u^2 " +
                $"({result.maxButtonPairOverlapRatio:P2}), " +
                $"chatBubbleOverlap={result.maxButtonChatBubbleOverlapCanvasUnits2:F2}u^2 " +
                $"({result.maxButtonChatBubbleOverlapRatio:P2}), " +
                $"chatViewportOverlap={result.buttonContainerViewportOverlapRatio:P2}, " +
                $"layout={result.hasVerticalLayoutGroup}/{result.hasContentSizeFitter}, " +
                $"conversationRoot={result.conversationContentRootAssigned}, " +
                $"outsideContainer={result.maxButtonOutsideContainerRatio:P2}.");
        }

        private static SceneGeometryResult CreateInitialResult(
            string scenePath,
            Scene scene,
            MonoBehaviour questionPanel,
            RectTransform optionRoot,
            RectTransform chatContent)
        {
            RectTransform conversationContentRoot = GetFieldValue<RectTransform>(
                questionPanel,
                "conversationContentRoot");
            Canvas canvas = optionRoot.GetComponentInParent<Canvas>();
            RectTransform canvasRect = canvas != null ? canvas.rootCanvas.transform as RectTransform : null;
            Rect rootRect = ToCanvasRect(optionRoot, canvasRect);
            Rect contentRect = ToCanvasRect(chatContent, canvasRect);
            ScrollRect scrollRect = chatContent.GetComponentInParent<ScrollRect>();
            RectTransform viewport = scrollRect != null
                ? (scrollRect.viewport != null ? scrollRect.viewport : scrollRect.transform as RectTransform)
                : null;

            return new SceneGeometryResult
            {
                scenePath = scenePath,
                sceneName = scene.name,
                canvasWidth = canvasRect != null ? canvasRect.rect.width : 0f,
                canvasHeight = canvasRect != null ? canvasRect.rect.height : 0f,
                buttonContainerWidth = rootRect.width,
                buttonContainerHeight = rootRect.height,
                chatContentWidth = contentRect.width,
                chatContentHeight = contentRect.height,
                hasVerticalLayoutGroup = optionRoot.GetComponent<VerticalLayoutGroup>() != null,
                hasContentSizeFitter = optionRoot.GetComponent<ContentSizeFitter>() != null,
                conversationContentRootAssigned = conversationContentRoot != null,
                buttonContainerViewportOverlapRatio = viewport != null
                    ? IntersectionRatio(rootRect, ToCanvasRect(viewport, canvasRect))
                    : 0f
            };
        }

        private static void UpdateMeasurements(
            SceneGeometryResult result,
            Scene scene,
            RectTransform optionRoot,
            RectTransform chatContent)
        {
            Canvas canvas = optionRoot.GetComponentInParent<Canvas>();
            RectTransform canvasRect = canvas != null ? canvas.rootCanvas.transform as RectTransform : null;
            Rect containerRect = ToCanvasRect(optionRoot, canvasRect);

            List<RectTransform> buttons = FindDirectActiveButtons(optionRoot);
            List<RectTransform> chatBubbles = FindBehaviourRects(scene, "Work.Chat.Code.ChatTextField");

            result.activeButtonCount = Mathf.Max(result.activeButtonCount, buttons.Count);
            result.chatBubbleCount = Mathf.Max(result.chatBubbleCount, chatBubbles.Count);
            result.buttonPairCount = Mathf.Max(result.buttonPairCount, buttons.Count * (buttons.Count - 1) / 2);

            int overlappingPairs = 0;
            float sumButtonHeights = 0f;
            for (int i = 0; i < buttons.Count; i++)
            {
                Rect buttonRect = ToCanvasRect(buttons[i], canvasRect);
                sumButtonHeights += buttonRect.height;

                float outsideRatio = OutsideRatio(buttonRect, containerRect);
                result.maxButtonOutsideContainerRatio = Mathf.Max(
                    result.maxButtonOutsideContainerRatio,
                    outsideRatio);

                for (int bubbleIndex = 0; bubbleIndex < chatBubbles.Count; bubbleIndex++)
                {
                    Rect bubbleRect = ToCanvasRect(chatBubbles[bubbleIndex], canvasRect);
                    float overlapArea = IntersectionArea(buttonRect, bubbleRect);
                    result.maxButtonChatBubbleOverlapCanvasUnits2 = Mathf.Max(
                        result.maxButtonChatBubbleOverlapCanvasUnits2,
                        overlapArea);
                    result.maxButtonChatBubbleOverlapRatio = Mathf.Max(
                        result.maxButtonChatBubbleOverlapRatio,
                        IntersectionRatio(buttonRect, bubbleRect));
                }

                for (int otherIndex = i + 1; otherIndex < buttons.Count; otherIndex++)
                {
                    Rect otherRect = ToCanvasRect(buttons[otherIndex], canvasRect);
                    float overlapArea = IntersectionArea(buttonRect, otherRect);
                    if (overlapArea > OverlapAreaEpsilon)
                        overlappingPairs++;

                    result.maxButtonPairOverlapCanvasUnits2 = Mathf.Max(
                        result.maxButtonPairOverlapCanvasUnits2,
                        overlapArea);
                    result.maxButtonPairOverlapRatio = Mathf.Max(
                        result.maxButtonPairOverlapRatio,
                        IntersectionRatio(buttonRect, otherRect));
                }
            }

            result.sumButtonHeights = Mathf.Max(result.sumButtonHeights, sumButtonHeights);
            result.maxOverlappingButtonPairs = Mathf.Max(result.maxOverlappingButtonPairs, overlappingPairs);
            result.chatContentHeight = Mathf.Max(
                result.chatContentHeight,
                ToCanvasRect(chatContent, canvasRect).height);
        }

        private static void StopConversationPlayback(Scene scene)
        {
            MonoBehaviour runner = FindBehaviour(scene, "Work.NPC.Code.Runtime.NpcConversationRunner");
            if (runner == null)
                return;

            runner.StopAllCoroutines();
            runner.enabled = false;
        }

        private static void BuildDeterministicConversation(MonoBehaviour chatPanel)
        {
            Type type = chatPanel.GetType();
            MethodInfo clearMethod = type.GetMethod("ClearChats", BindingFlags.Instance | BindingFlags.Public);
            MethodInfo addMethod = type.GetMethod("AddChat", BindingFlags.Instance | BindingFlags.Public);
            MethodInfo completeMethod = type.GetMethod("CompleteActiveTyping", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(addMethod, Is.Not.Null, "ChatPanel.AddChat could not be found.");

            clearMethod?.Invoke(chatPanel, null);
            for (int i = 0; i < 6; i++)
            {
                string message = i % 2 == 0
                    ? "손님이 주문과 취향을 설명하는 플레이모드 레이아웃 측정용 대화입니다."
                    : "플레이어가 내용을 확인하고 추가 질문을 이어가는 레이아웃 측정용 응답입니다.";
                addMethod.Invoke(chatPanel, new object[] { message, i % 2 != 0 });
                completeMethod?.Invoke(chatPanel, null);
            }
        }

        private static void InvokeQuestionOptions(MonoBehaviour questionPanel)
        {
            Type categoryType = Type.GetType("Work.NPC.Code.Data.QuestionCategoryData, Assembly-CSharp");
            Assert.That(categoryType, Is.Not.Null, "QuestionCategoryData type could not be loaded.");

            Type listType = typeof(List<>).MakeGenericType(categoryType);
            IList options = (IList)Activator.CreateInstance(listType);
            ConstructorInfo constructor = categoryType.GetConstructor(new[]
            {
                typeof(string),
                typeof(string),
                typeof(string)
            });
            Assert.That(constructor, Is.Not.Null, "QuestionCategoryData constructor could not be found.");

            for (int i = 0; i < QuestionLabels.Length; i++)
            {
                options.Add(constructor.Invoke(new object[]
                {
                    "Geometry" + i,
                    QuestionLabels[i],
                    "Question_Geometry" + i
                }));
            }

            MethodInfo method = questionPanel.GetType().GetMethod(
                "HandleQuestionOptionsUpdated",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, "NpcQuestionPanel.HandleQuestionOptionsUpdated could not be found.");
            method.Invoke(questionPanel, new object[] { options });
        }

        private static MonoBehaviour FindBehaviour(Scene scene, string fullTypeName)
        {
            MonoBehaviour[] behaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour != null
                    && behaviour.gameObject.scene == scene
                    && string.Equals(behaviour.GetType().FullName, fullTypeName, StringComparison.Ordinal))
                {
                    return behaviour;
                }
            }

            return null;
        }

        private static List<RectTransform> FindBehaviourRects(Scene scene, string fullTypeName)
        {
            List<RectTransform> results = new List<RectTransform>();
            MonoBehaviour[] behaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null
                    || behaviour.gameObject.scene != scene
                    || string.Equals(behaviour.GetType().FullName, fullTypeName, StringComparison.Ordinal) == false)
                {
                    continue;
                }

                if (behaviour.transform is RectTransform rectTransform)
                    results.Add(rectTransform);
            }

            return results;
        }

        private static List<RectTransform> FindDirectActiveButtons(RectTransform root)
        {
            List<RectTransform> results = new List<RectTransform>();
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.gameObject.activeInHierarchy == false || child.GetComponent<Button>() == null)
                    continue;
                if (child is RectTransform rectTransform)
                    results.Add(rectTransform);
            }

            return results;
        }

        private static T GetFieldValue<T>(object owner, string fieldName) where T : class
        {
            FieldInfo field = owner.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return field != null ? field.GetValue(owner) as T : null;
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
            float childArea = child.width * child.height;
            if (childArea <= 0f)
                return 0f;
            return Mathf.Clamp01(1f - IntersectionArea(child, parent) / childArea);
        }

        private static void WriteResult(SceneGeometryResult result)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            string outputDirectory = Path.Combine(projectRoot, "Temp", "NpcQuestionUiGeometry");
            Directory.CreateDirectory(outputDirectory);
            string outputPath = Path.Combine(outputDirectory, result.sceneName + ".json");
            File.WriteAllText(outputPath, JsonUtility.ToJson(result, true));
        }

        [Serializable]
        private sealed class SceneGeometryResult
        {
            public string scenePath;
            public string sceneName;
            public int sampleCount;
            public int activeButtonCount;
            public int chatBubbleCount;
            public int buttonPairCount;
            public int maxOverlappingButtonPairs;
            public float canvasWidth;
            public float canvasHeight;
            public float buttonContainerWidth;
            public float buttonContainerHeight;
            public float chatContentWidth;
            public float chatContentHeight;
            public float sumButtonHeights;
            public float maxButtonPairOverlapCanvasUnits2;
            public float maxButtonPairOverlapRatio;
            public float maxButtonChatBubbleOverlapCanvasUnits2;
            public float maxButtonChatBubbleOverlapRatio;
            public float maxButtonOutsideContainerRatio;
            public float buttonContainerViewportOverlapRatio;
            public bool hasVerticalLayoutGroup;
            public bool hasContentSizeFitter;
            public bool conversationContentRootAssigned;
        }
    }
}
