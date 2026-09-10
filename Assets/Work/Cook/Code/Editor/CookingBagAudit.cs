using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Work.Cook.Code.Info;
using Work.Cook.Code.Runtime.UI;
using Object = UnityEngine.Object;

namespace Work.Cook.Code.Editor
{
    public static class CookingBagAudit
    {
        [InitializeOnLoadMethod]
        private static void CheckRequest()
        {
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists("Temp/CookingBagAudit.request") || EditorApplication.isPlayingOrWillChangePlaymode) return;
                File.Delete("Temp/CookingBagAudit.request");
                Run();
            };
        }

        [MenuItem("Tools/Dungeon Dinner/Validate Bag Layout")]
        public static void Run()
        {
            var scene = EditorSceneManager.OpenPreviewScene("Assets/Work/Adventure/Scene/AdventureTestScene.unity");
            var oldTarget = RenderTexture.active;
            RenderTexture target = null;
            Texture2D capture = null;
            GameObject canvasObject = null;
            GameObject cameraObject = null;
            try
            {
                var source = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<CookingIngredientSelectionView>(true)).First();
                canvasObject = new GameObject("Bag Audit", typeof(RectTransform), typeof(Canvas));
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(canvasObject, scene);
                var canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                ((RectTransform)canvas.transform).sizeDelta = new Vector2(720, 760);
                canvas.transform.localScale = Vector3.one * 0.01f;
                var view = Object.Instantiate(source, canvas.transform);
                view.gameObject.SetActive(true);
                var rect = (RectTransform)view.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(520, 680);
                foreach (var root in scene.GetRootGameObjects()) if (root != canvasObject) root.SetActive(false);
                var apply = typeof(CookingIngredientSelectionView).GetMethod("ApplyBagPresentation", BindingFlags.NonPublic | BindingFlags.Instance);
                apply.Invoke(view, null);
                var bag = view.transform.Find("Body/BagSection/AvailableIngredientsViewport/AvailableIngredients");
                var selected = view.transform.Find("Body/SelectedSection/SelectedIngredientsViewport/SelectedIngredients");
                foreach (var parent in new[] { bag, selected })
                    for (int i = parent.childCount - 1; i >= 0; i--) Object.DestroyImmediate(parent.GetChild(i).gameObject);
                view.transform.Find("Body/BagSection/EmptyAvailable").gameObject.SetActive(false);
                view.transform.Find("Body/SelectedSection/EmptySelected").gameObject.SetActive(false);
                var entries = Resources.Load<FieldGuideCatalogSO>("DungeonFieldGuide").BuildCategories()[0].Entries;
                var prefab = AssetDatabase.LoadAssetAtPath<CookingIngredientButtonView>("Assets/Work/Cook/Prefabs/UI/CookingIngredientButton.prefab");
                for (int i = 0; i < entries.Count; i++)
                    Object.Instantiate(prefab, bag).Bind(entries[i].DisplayName + " x20", entries[i].Icon, false, true, null, null, null);
                Object.Instantiate(prefab, selected).Bind(entries[0].DisplayName + " x1", entries[0].Icon, true, true, null, null, null);
                view.transform.Find("Body/SelectedSection/SectionTitle").GetComponent<TMPro.TextMeshProUGUI>().text = "선택한 재료 1";
                view.transform.Find("Body/BagSection/AvailableSummary").GetComponent<TMPro.TextMeshProUGUI>().text = "가방 7종";
                foreach (int width in new[] { 460, 520, 680 })
                {
                    rect.sizeDelta = new Vector2(width, 680);
                    LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
                    Canvas.ForceUpdateCanvases();
                    foreach (var path in new[] { "ActionRow", "Body/BagSection/IngredientSearchField", "Body/SelectedSection/SelectedIngredientsViewport" })
                    {
                        var child = (RectTransform)view.transform.Find(path);
                        var corners = new Vector3[4];
                        child.GetWorldCorners(corners);
                        if (child.rect.height < 20 || corners.Any(c => !rect.rect.Contains(rect.InverseTransformPoint(c))))
                            throw new InvalidOperationException("Layout outside bag at width " + width + ": " + path);
                    }
                }
                rect.sizeDelta = new Vector2(520, 680);
                LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
                Canvas.ForceUpdateCanvases();
                foreach (var listRoot in new[] { bag, selected })
                {
                    var scroll = listRoot.GetComponentInParent<ScrollRect>();
                    if (scroll == null) continue;
                    scroll.verticalNormalizedPosition = 1;
                    if (scroll.verticalScrollbar != null)
                    {
                        float height = scroll.content.rect.height;
                        scroll.verticalScrollbar.gameObject.SetActive(height > scroll.viewport.rect.height);
                        scroll.verticalScrollbar.size = height > 0 ? Mathf.Clamp01(scroll.viewport.rect.height / height) : 1;
                        scroll.verticalScrollbar.SetValueWithoutNotify(1);
                    }
                }
                cameraObject = new GameObject("Bag Camera", typeof(Camera));
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject, scene);
                var camera = cameraObject.GetComponent<Camera>();
                camera.scene = scene;
                camera.transform.position = new Vector3(0, 0, -10);
                camera.orthographic = true;
                camera.orthographicSize = 3.8f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.055f, 0.04f, 0.03f);
                canvas.worldCamera = camera;
                target = new RenderTexture(720, 760, 24);
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                capture = new Texture2D(720, 760, TextureFormat.RGB24, false);
                capture.ReadPixels(new Rect(0, 0, 720, 760), 0, 0);
                capture.Apply();
                File.WriteAllBytes("Temp/CookingBagAudit.png", capture.EncodeToPNG());
                File.WriteAllText("Temp/CookingBagAudit.txt", "PASS: bag search, list viewport and actions fit at widths 460/520/680.");
            }
            catch (Exception error) { File.WriteAllText("Temp/CookingBagAudit.txt", "FAIL: " + error); Debug.LogException(error); }
            finally
            {
                RenderTexture.active = oldTarget;
                EditorSceneManager.ClosePreviewScene(scene);
                if (target != null) Object.DestroyImmediate(target);
                if (capture != null) Object.DestroyImmediate(capture);
            }
        }
    }
}
