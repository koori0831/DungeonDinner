using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DungeonDinner.Adventure.Ui.PlayModeTests
{
    [Category("AdventureTooltipLayout")]
    public sealed class AdventureTooltipLayoutTests
    {
        [UnityTest]
        public IEnumerator Tooltip_FollowsPointerAndStaysInsideViewport_AcrossResolutions()
        {
            int originalWidth = Screen.width;
            int originalHeight = Screen.height;
            FullScreenMode originalMode = Screen.fullScreenMode;
            Mouse originalMouse = Mouse.current;
            Mouse mouse = InputSystem.AddDevice<Mouse>("TooltipLayoutMouse");
            GameObject canvasObject = null;
            try
            {
                Type tooltipType = Type.GetType("Work.Adventure.Code.UI.TooltipUI, Assembly-CSharp", true);
                Type showEventType = Type.GetType("Work.Adventure.Code.UI.OnEnableTooltipEvent, Assembly-CSharp", true);
                MethodInfo show = tooltipType.GetMethod("HandleEnableEvent", BindingFlags.Instance | BindingFlags.NonPublic);

                canvasObject = new GameObject("Tooltip Layout Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
                Canvas canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0f;

                // Match AdventureCanvas's fixed-size, centered tooltip container and top-right panel pivot.
                RectTransform parent = new GameObject("Tooltip", typeof(RectTransform)).GetComponent<RectTransform>();
                parent.SetParent(canvas.transform, false);
                parent.sizeDelta = new Vector2(1920, 1080);
                RectTransform panel = new GameObject("TooltipPanel", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                panel.SetParent(parent, false);
                panel.anchorMin = panel.anchorMax = Vector2.zero;
                panel.pivot = Vector2.one;
                panel.sizeDelta = new Vector2(183, 55.1f);
                panel.GetComponent<Image>().raycastTarget = false;
                TextMeshProUGUI label = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
                label.transform.SetParent(panel, false);
                label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(0, 0.5f);
                label.rectTransform.pivot = new Vector2(0, 0.5f);
                label.rectTransform.sizeDelta = new Vector2(48, 56);
                label.fontSize = 20.83f;
                label.color = Color.black;
                label.alignment = TextAlignmentOptions.MidlineLeft;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.margin = new Vector4(15, 0, 0, 0);
                label.raycastTarget = false;

                panel.gameObject.SetActive(false);
                Component tooltip = panel.gameObject.AddComponent(tooltipType);
                tooltipType.GetField("root", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(tooltip, panel);
                tooltipType.GetField("text", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(tooltip, label);
                tooltipType.GetField("offset", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(tooltip, 45.8f);
                panel.gameObject.SetActive(true);

                Vector2Int[] resolutions = {
                    new Vector2Int(1024, 768), new Vector2Int(1280, 720),
                    new Vector2Int(1920, 1080), new Vector2Int(2560, 1440)
                };
                foreach (Vector2Int resolution in resolutions)
                {
                    if (!Application.isEditor)
                    {
                        Screen.SetResolution(resolution.x, resolution.y, FullScreenMode.Windowed);
                        for (int frame = 0; frame < 120; frame++)
                        {
                            yield return null;
                            if (Screen.width == resolution.x && Screen.height == resolution.y)
                                break;
                        }
                        Assert.That(new Vector2Int(Screen.width, Screen.height), Is.EqualTo(resolution));
                    }
                    else
                    {
                        // Editor Game View does not implement Screen.SetResolution; exercise equivalent UI scaling.
                        scaler.enabled = false;
                        canvas.scaleFactor = resolution.x / 1920f;
                    }
                    yield return null;
                    Canvas.ForceUpdateCanvases();
                    Rect viewport = canvas.pixelRect;

                    foreach (Vector2 parentOffset in new[] { Vector2.zero, new Vector2(113, -47) })
                    {
                        parent.anchoredPosition = parentOffset;
                        Vector2 pointer = new Vector2(viewport.xMax - 60, viewport.yMin + viewport.height * 0.65f);
                        SetPointer(mouse, pointer);
                        show.Invoke(tooltip, new[] { Activator.CreateInstance(showEventType, "Requires a knife.") });
                        // Must be correct immediately on hover, before a later frame can repair the position.
                        Assert.That(Vector2.Distance(RectTransformUtility.WorldToScreenPoint(null, panel.position),
                            pointer + new Vector2(20, -20)), Is.LessThan(1f), "Tooltip drifted away from the pointer at " + resolution);
                        AssertVisible(panel, viewport);

                        foreach (Vector2 edgePointer in new[] {
                            new Vector2(viewport.xMin + 1, viewport.yMin + 1),
                            new Vector2(viewport.xMin + 1, viewport.yMax - 1),
                            new Vector2(viewport.xMax - 1, viewport.yMin + 1),
                            new Vector2(viewport.xMax - 1, viewport.yMax - 1) })
                        {
                            SetPointer(mouse, edgePointer);
                            yield return null;
                            AssertVisible(panel, viewport);
                        }

                        SetPointer(mouse, new Vector2(viewport.xMin + 1, viewport.yMin + 1));
                        show.Invoke(tooltip, new[] { Activator.CreateInstance(showEventType, "Requires a knife. One item is consumed on selection.") });
                        AssertVisible(panel, viewport);
                    }
                    TestContext.Progress.WriteLine($"PASS {Screen.width}x{Screen.height}; scale={canvas.scaleFactor:F3}; immediate hover, four screen edges, long text and offset parent.");
                }
            }
            finally
            {
                if (canvasObject != null) Object.DestroyImmediate(canvasObject);
                InputSystem.RemoveDevice(mouse);
                originalMouse?.MakeCurrent();
                if (!Application.isEditor) Screen.SetResolution(originalWidth, originalHeight, originalMode);
            }
        }

        private static void SetPointer(Mouse mouse, Vector2 position)
        {
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position });
            InputSystem.Update();
            mouse.MakeCurrent();
        }

        private static void AssertVisible(RectTransform panel, Rect viewport)
        {
            var corners = new Vector3[4];
            panel.GetWorldCorners(corners);
            foreach (Vector3 corner in corners)
            {
                Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, corner);
                Assert.That(screen.x, Is.InRange(viewport.xMin + 7f, viewport.xMax - 7f), "Tooltip is clipped horizontally.");
                Assert.That(screen.y, Is.InRange(viewport.yMin + 7f, viewport.yMax - 7f), "Tooltip is clipped vertically.");
            }
        }
    }
}
