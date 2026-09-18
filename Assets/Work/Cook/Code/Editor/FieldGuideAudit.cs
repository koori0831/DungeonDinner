using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Work.Cook.Code.Info;
using Object = UnityEngine.Object;

namespace Work.Cook.Code.Editor
{
    // An explicit request file permits unattended validation in an already open editor.
    public static class FieldGuideAudit
    {
        [InitializeOnLoadMethod]
        private static void CheckRequest()
        {
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists("Temp/FieldGuideAudit.request") || EditorApplication.isPlayingOrWillChangePlaymode)
                    return;
                File.Delete("Temp/FieldGuideAudit.request");
                Run();
            };
        }

        [MenuItem("Tools/Dungeon Dinner/Validate Field Guide")]
        public static void Run()
        {
            Scene preview = default;
            RenderTexture target = null;
            Texture2D capture = null;
            var previousTarget = RenderTexture.active;
            try
            {
                var catalog = Resources.Load<FieldGuideCatalogSO>("DungeonFieldGuide");
                Require(catalog != null, "Shared guide asset is missing.");
                var categories = catalog.BuildCategories();
                Require(categories.Count == 4, "Expected ingredient, tool, monster and dish categories.");
                int count = 0;
                var names = new HashSet<string>();
                foreach (var category in categories)
                foreach (var entry in category.Entries)
                {
                    Require(entry.Icon == null && entry.DisplayName == "???", "Undiscovered entry leaked: " + entry.EntryId);
                    Require(!string.IsNullOrWhiteSpace(entry.Description), "Missing description: " + entry.DisplayName);
                    Require(names.Add(entry.EntryId), "Duplicate entry: " + entry.DisplayName);
                    count++;
                }
                Require(count > 0, "The production guide is empty.");

                preview = EditorSceneManager.NewPreviewScene();
                var cameraObject = new GameObject("Guide Audit Camera", typeof(Camera));
                SceneManager.MoveGameObjectToScene(cameraObject, preview);
                var camera = cameraObject.GetComponent<Camera>();
                camera.scene = preview;
                camera.transform.position = new Vector3(0, 0, -10);
                camera.orthographic = true;
                camera.orthographicSize = 3.6f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.19f, 0.16f, 0.13f);
                target = new RenderTexture(960, 720, 24);
                camera.targetTexture = target;
                var canvasObject = new GameObject("Guide Audit Canvas", typeof(RectTransform), typeof(Canvas));
                SceneManager.MoveGameObjectToScene(canvasObject, preview);
                var canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = camera;
                ((RectTransform)canvas.transform).sizeDelta = new Vector2(960, 720);
                canvas.transform.localScale = Vector3.one * 0.01f;
                var listRoot = MakePage(canvas.transform, "List Page", -222);
                var detailRoot = MakePage(canvas.transform, "Detail Page", 222);
                var listPrefab = AssetDatabase.LoadAssetAtPath<InfoDictionaryScrollViewField>(
                    "Assets/Work/Cook/Prefabs/UI/Scroll View.prefab");
                var detailPrefab = AssetDatabase.LoadAssetAtPath<InfoDisplayPanel>(
                    "Assets/Work/Cook/Prefabs/UI/InfoDisplayPanel.prefab");
                var list = Object.Instantiate(listPrefab, listRoot);
                var detail = Object.Instantiate(detailPrefab, detailRoot);
                detail.InitializeDisplay(() => { });
                list.InitializeField(categories[0].Entries, detail.Enable);
                list.SetCategoryHeading("재료", categories[0].Entries.Count);
                detail.Enable(categories[0].Entries[0]);
                detail.SetSiblingNavigation(null, () => detail.Enable(categories[0].Entries[1]), false, true);
                var next = detail.transform.Find("NextEntry").GetComponent<Button>();
                next.onClick.Invoke();
                var readingArea = detail.GetComponentInChildren<ScrollRect>();
                Require(readingArea.content.rect.height <= readingArea.viewport.rect.height
                    || readingArea.verticalNormalizedPosition >= 0.99f,
                    "Changing entry must reset the reading position.");
                foreach (int width in new[] { 320, 398, 560 })
                {
                    listRoot.sizeDelta = new Vector2(width, 640);
                    Canvas.ForceUpdateCanvases();
                    list.InitializeField(categories[0].Entries, detail.Enable);
                    var grid = list.GetComponentInChildren<GridLayoutGroup>();
                    float required = grid.constraintCount * grid.cellSize.x
                        + (grid.constraintCount - 1) * grid.spacing.x + grid.padding.horizontal;
                    Require(required <= width, "Grid exceeds viewport at width " + width);
                    Require(list.GetComponentsInChildren<InfoSelectBtn>().Length == 7, "List entry count changed.");
                    foreach (var category in categories)
                    {
                        list.InitializeField(category.Entries, detail.Enable);
                        Canvas.ForceUpdateCanvases();
                        foreach (var button in list.GetComponentsInChildren<InfoSelectBtn>())
                            AssertLabelFits(button.GetComponentInChildren<TextMeshProUGUI>(), (RectTransform)button.transform);
                        foreach (var entry in category.Entries)
                        {
                            detailRoot.sizeDelta = new Vector2(width, 640);
                            detail.Enable(entry);
                            Canvas.ForceUpdateCanvases();
                            var title = detail.transform.Find("NameField").GetComponent<TextMeshProUGUI>();
                            AssertLabelFits(title, (RectTransform)detail.transform);
                        }
                    }
                }
                listRoot.sizeDelta = new Vector2(398, 640);
                detailRoot.sizeDelta = new Vector2(398, 640);
                Canvas.ForceUpdateCanvases();
                list.InitializeField(categories[3].Entries, detail.Enable);
                list.transform.Find("CategoryHeading").GetComponent<TextMeshProUGUI>().text = "요리  <size=70%>3종</size>";
                detail.Enable(categories[3].Entries[0]);
                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active = target;
                capture = new Texture2D(960, 720, TextureFormat.RGB24, false);
                capture.ReadPixels(new Rect(0, 0, 960, 720), 0, 0);
                capture.Apply();
                Directory.CreateDirectory("Temp/FieldGuideAudit");
                File.WriteAllBytes("Temp/FieldGuideAudit/guide.png", capture.EncodeToPNG());
                File.WriteAllText("Temp/FieldGuideAudit/result.txt", "PASS: undiscovered entries redacted, unique IDs, navigation, scroll reset, all list/title glyph bounds at widths 320/398/560.\n");
                Debug.Log("Field guide audit passed. Preview: Temp/FieldGuideAudit/guide.png");
            }
            catch (Exception exception)
            {
                Directory.CreateDirectory("Temp/FieldGuideAudit");
                File.WriteAllText("Temp/FieldGuideAudit/result.txt", "FAIL: " + exception);
                Debug.LogException(exception);
            }
            finally
            {
                RenderTexture.active = previousTarget;
                if (preview.IsValid()) EditorSceneManager.ClosePreviewScene(preview);
                if (capture != null) Object.DestroyImmediate(capture);
                if (target != null) Object.DestroyImmediate(target);
            }
        }

        private static RectTransform MakePage(Transform parent, string name, float x)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(398, 640);
            rect.anchoredPosition = new Vector2(x, 0);
            go.GetComponent<Image>().color = new Color(0.97f, 0.94f, 0.86f);
            return rect;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static void AssertLabelFits(TextMeshProUGUI text, RectTransform container)
        {
            text.ForceMeshUpdate(true, true);
            Require(!text.isTextTruncated, "Truncated label: " + text.text);
            var bounds = container.rect;
            for (int i = 0; i < text.textInfo.characterCount; i++)
            {
                var character = text.textInfo.characterInfo[i];
                if (!character.isVisible) continue;
                foreach (var corner in new[] { character.bottomLeft, character.topRight })
                {
                    var position = container.InverseTransformPoint(text.transform.TransformPoint(corner));
                    Require(position.x >= bounds.xMin - 0.5f && position.x <= bounds.xMax + 0.5f
                        && position.y >= bounds.yMin - 0.5f && position.y <= bounds.yMax + 0.5f,
                        "Glyph outside card: " + text.text + " / " + character.character);
                }
            }
            Require(text.textBounds.size.y <= text.rectTransform.rect.height + 1,
                "Label exceeds reserved height: " + text.text);
            Require(text.textBounds.size.x <= text.rectTransform.rect.width + 1,
                "Label exceeds reserved width: " + text.text);
        }
    }
}
