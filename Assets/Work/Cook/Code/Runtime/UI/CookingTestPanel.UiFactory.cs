using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
using UnityEngine.UI;

namespace Work.Cook.Code.Runtime.UI
{
    public sealed partial class CookingTestPanel
    {
        private void SetTitle(string text)
        {
            if (_titleText != null)
                _titleText.text = text;
        }

        private void ClearContent()
        {
            if (_contentRoot == null)
                return;

            for (int i = _contentRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = _contentRoot.GetChild(i);
                child.gameObject.SetActive(false);

                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }

        private Canvas CreateCanvas()
        {
            GameObject canvasObject = new GameObject("CookingTestCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        private RectTransform CreatePanel(Transform parent)
        {
            GameObject panelObject = new GameObject("CookingTestPanel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            panelObject.transform.SetParent(parent, false);

            RectTransform rect = panelObject.transform as RectTransform;
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-18f, -62f);
            rect.sizeDelta = GetEffectivePanelSize();

            Image image = panelObject.GetComponent<Image>();
            image.color = new Color(0.08f, 0.09f, 0.11f, 0.96f);

            VerticalLayoutGroup layout = panelObject.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 14, 14);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            return rect;
        }

        private RectTransform CreateScrollContent(Transform parent)
        {
            GameObject scrollObject = new GameObject("ScrollView", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(LayoutElement));
            scrollObject.transform.SetParent(parent, false);

            Image scrollImage = scrollObject.GetComponent<Image>();
            scrollImage.color = new Color(0f, 0f, 0f, 0f);
            scrollImage.raycastTarget = true;

            LayoutElement scrollLayout = scrollObject.GetComponent<LayoutElement>();
            scrollLayout.flexibleWidth = 1f;
            scrollLayout.flexibleHeight = 1f;

            GameObject viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            viewportObject.transform.SetParent(scrollObject.transform, false);

            RectTransform viewportRect = viewportObject.transform as RectTransform;
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = new Vector2(-12f, 0f);

            Image viewportImage = viewportObject.GetComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.001f);
            viewportImage.raycastTarget = true;

            Scrollbar scrollbar = CreateVerticalScrollbar(scrollObject.transform);

            GameObject contentObject = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentObject.transform.SetParent(viewportObject.transform, false);

            RectTransform contentRect = contentObject.transform as RectTransform;
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;

            VerticalLayoutGroup contentLayout = contentObject.GetComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(0, 8, 0, 8);
            contentLayout.spacing = 8f;
            contentLayout.childAlignment = TextAnchor.UpperLeft;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = scrollObject.GetComponent<ScrollRect>();
            scroll.viewport = viewportRect;
            scroll.content = contentRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 32f;
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

            return contentRect;
        }

        private Vector2 GetEffectivePanelSize()
        {
            float width = Mathf.Clamp(panelSize.x, 540f, 680f);
            float height = Mathf.Clamp(panelSize.y, 560f, 860f);
            return new Vector2(width, height);
        }

        private Scrollbar CreateVerticalScrollbar(Transform parent)
        {
            GameObject scrollbarObject = new GameObject("VerticalScrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
            scrollbarObject.transform.SetParent(parent, false);

            RectTransform scrollbarRect = scrollbarObject.transform as RectTransform;
            scrollbarRect.anchorMin = new Vector2(1f, 0f);
            scrollbarRect.anchorMax = new Vector2(1f, 1f);
            scrollbarRect.pivot = new Vector2(1f, 0.5f);
            scrollbarRect.sizeDelta = new Vector2(10f, 0f);
            scrollbarRect.anchoredPosition = Vector2.zero;

            Image background = scrollbarObject.GetComponent<Image>();
            background.color = new Color(0.04f, 0.05f, 0.07f, 0.85f);

            GameObject slidingAreaObject = new GameObject("Sliding Area", typeof(RectTransform));
            slidingAreaObject.transform.SetParent(scrollbarObject.transform, false);
            RectTransform slidingArea = slidingAreaObject.transform as RectTransform;
            slidingArea.anchorMin = Vector2.zero;
            slidingArea.anchorMax = Vector2.one;
            slidingArea.offsetMin = new Vector2(1f, 1f);
            slidingArea.offsetMax = new Vector2(-1f, -1f);

            GameObject handleObject = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handleObject.transform.SetParent(slidingAreaObject.transform, false);
            RectTransform handleRect = handleObject.transform as RectTransform;
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.one;
            handleRect.offsetMin = Vector2.zero;
            handleRect.offsetMax = Vector2.zero;

            Image handleImage = handleObject.GetComponent<Image>();
            handleImage.color = new Color(0.36f, 0.46f, 0.60f, 1f);

            Scrollbar scrollbar = scrollbarObject.GetComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.handleRect = handleRect;
            scrollbar.targetGraphic = handleImage;
            scrollbar.size = 0.2f;

            ColorBlock colors = scrollbar.colors;
            colors.normalColor = handleImage.color;
            colors.highlightedColor = new Color(0.55f, 0.68f, 0.86f, 1f);
            colors.pressedColor = new Color(0.28f, 0.36f, 0.48f, 1f);
            colors.selectedColor = colors.highlightedColor;
            scrollbar.colors = colors;

            return scrollbar;
        }

        private RectTransform CreateRow(Transform parent, string name, float height)
        {
            GameObject rowObject = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            rowObject.transform.SetParent(parent, false);

            HorizontalLayoutGroup layout = rowObject.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            LayoutElement layoutElement = rowObject.GetComponent<LayoutElement>();
            layoutElement.preferredHeight = height;
            layoutElement.minHeight = height;
            layoutElement.flexibleWidth = 1f;

            return rowObject.transform as RectTransform;
        }

        private void MakeDragHandle(RectTransform handle, RectTransform target, Canvas canvas)
        {
            Image image = handle.gameObject.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0f);
            image.raycastTarget = true;

            CookingTestDragHandle dragHandle = handle.gameObject.AddComponent<CookingTestDragHandle>();
            dragHandle.Initialize(target, canvas);
        }

        private void CreateSectionLabel(Transform parent, string text)
        {
            TextMeshProUGUI label = CreateText(parent, $"Section_{text}", text, 16f, TextAlignmentOptions.Left);
            label.color = new Color(0.78f, 0.86f, 1f, 1f);

            LayoutElement layoutElement = label.GetComponent<LayoutElement>();
            layoutElement.preferredHeight = 22f;
            layoutElement.minHeight = 22f;
        }

        private Button CreateButton(
            Transform parent,
            string label,
            UnityAction onClick,
            Vector2? size = null,
            float height = 34f,
            ButtonTone tone = ButtonTone.Default)
        {
            GameObject buttonObject = new GameObject($"Button_{SanitizeName(label)}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonObject.transform.SetParent(parent, false);

            Image image = buttonObject.GetComponent<Image>();
            image.color = GetButtonColor(tone);

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);

            ColorBlock colors = button.colors;
            colors.normalColor = image.color;
            colors.highlightedColor = GetButtonHighlightColor(tone);
            colors.pressedColor = new Color(0.12f, 0.16f, 0.22f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.16f, 0.16f, 0.17f, 0.62f);
            button.colors = colors;

            LayoutElement layoutElement = buttonObject.GetComponent<LayoutElement>();
            layoutElement.minHeight = size?.y ?? height;
            layoutElement.preferredHeight = size?.y ?? height;
            layoutElement.preferredWidth = size?.x ?? 0f;
            layoutElement.flexibleWidth = size.HasValue ? 0f : 1f;

            TextMeshProUGUI text = CreateText(buttonObject.transform, "Label", label, height > 48f ? 14f : 16f, TextAlignmentOptions.Center);
            text.textWrappingMode = height > 48f ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Truncate;

            RectTransform textRect = text.transform as RectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8f, 4f);
            textRect.offsetMax = new Vector2(-8f, -4f);
            DestroyComponent(text.GetComponent<LayoutElement>());

            return button;
        }

        private TextMeshProUGUI CreateInfoBox(Transform parent, string name, string text, float height, float fontSize)
        {
            TextMeshProUGUI box = CreateTextBox(parent, name, "Text", height, fontSize);
            box.text = text;
            return box;
        }

        private TextMeshProUGUI CreateTextBox(
            Transform parent,
            string boxName,
            string textName,
            float height,
            float fontSize)
        {
            GameObject boxObject = new GameObject(boxName, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            boxObject.transform.SetParent(parent, false);

            Image image = boxObject.GetComponent<Image>();
            image.color = new Color(0.04f, 0.05f, 0.07f, 0.96f);

            LayoutElement layoutElement = boxObject.GetComponent<LayoutElement>();
            layoutElement.minHeight = height;
            layoutElement.preferredHeight = height;
            layoutElement.flexibleWidth = 1f;

            TextMeshProUGUI text = CreateText(boxObject.transform, textName, string.Empty, fontSize, TextAlignmentOptions.TopLeft);
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Truncate;

            RectTransform textRect = text.transform as RectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10f, 8f);
            textRect.offsetMax = new Vector2(-10f, -8f);
            DestroyComponent(text.GetComponent<LayoutElement>());

            return text;
        }

        private TextMeshProUGUI CreateText(
            Transform parent,
            string name,
            string text,
            float fontSize,
            TextAlignmentOptions alignment)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            textObject.transform.SetParent(parent, false);

            TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.color = Color.white;
            label.alignment = alignment;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;

            if (fontAsset != null)
                label.font = fontAsset;

            LayoutElement layoutElement = textObject.GetComponent<LayoutElement>();
            layoutElement.minHeight = 24f;
            layoutElement.preferredHeight = 24f;
            layoutElement.flexibleWidth = 1f;

            return label;
        }

        private static Color GetButtonColor(ButtonTone tone)
        {
            switch (tone)
            {
                case ButtonTone.Primary:
                    return new Color(0.20f, 0.36f, 0.52f, 0.98f);
                case ButtonTone.Selected:
                    return new Color(0.22f, 0.44f, 0.32f, 0.98f);
                case ButtonTone.Warning:
                    return new Color(0.45f, 0.34f, 0.16f, 0.98f);
                case ButtonTone.Danger:
                    return new Color(0.48f, 0.20f, 0.22f, 0.98f);
                case ButtonTone.Default:
                default:
                    return new Color(0.19f, 0.24f, 0.31f, 0.98f);
            }
        }

        private static Color GetButtonHighlightColor(ButtonTone tone)
        {
            switch (tone)
            {
                case ButtonTone.Primary:
                    return new Color(0.28f, 0.48f, 0.68f, 1f);
                case ButtonTone.Selected:
                    return new Color(0.30f, 0.56f, 0.42f, 1f);
                case ButtonTone.Warning:
                    return new Color(0.60f, 0.45f, 0.22f, 1f);
                case ButtonTone.Danger:
                    return new Color(0.65f, 0.28f, 0.30f, 1f);
                case ButtonTone.Default:
                default:
                    return new Color(0.26f, 0.33f, 0.43f, 1f);
            }
        }

        private static void SetButtonLabel(Button button, string label)
        {
            TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (text != null)
                text.text = label;
        }

        private static string SanitizeName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Empty";

            return value.Replace('\n', ' ').Replace('\r', ' ');
        }

        private static void DestroyComponent(Component component)
        {
            if (component == null)
                return;

            if (Application.isPlaying)
                Destroy(component);
            else
                DestroyImmediate(component);
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
                return;

            GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
#else
            eventSystemObject.AddComponent<StandaloneInputModule>();
#endif
        }
    }
}