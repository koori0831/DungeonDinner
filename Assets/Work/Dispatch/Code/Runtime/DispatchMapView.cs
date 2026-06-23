using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Work.Dispatch.Code.Data;

namespace Work.Dispatch.Code.Runtime
{
    /// <summary>
    /// 파견 지도와 방문 가능한 포인트를 표시하는 Temp UI
    /// </summary>
    public sealed class DispatchMapView : MonoBehaviour
    {
        [Header("Layout References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image overlayImage;
        [SerializeField] private RectTransform pointRoot;
        [SerializeField] private Button closeButton;
        [SerializeField] private TextMeshProUGUI titleField;
        [SerializeField] private TextMeshProUGUI descriptionField;
        [SerializeField] private RectTransform pointInfoPanel;
        [SerializeField] private TextMeshProUGUI pointInfoTitleField;
        [SerializeField] private TextMeshProUGUI pointInfoDescriptionField;
        [SerializeField] private TextMeshProUGUI pointInfoRewardField;
        [SerializeField] private TextMeshProUGUI emptyField;

        [Header("Default Layout")]
        [SerializeField] private bool buildDefaultLayoutWhenMissing = true;
        [SerializeField] private TMP_FontAsset fontAsset;
        [SerializeField] private Sprite panelSprite;
        [SerializeField] private Sprite labelSprite;
        [SerializeField] private Sprite buttonSprite;
        [SerializeField] private Color overlayColor = new Color(0f, 0f, 0f, 0.42f);
        [SerializeField] private Color panelColor = new Color(0.09f, 0.075f, 0.055f, 0.97f);
        [SerializeField] private Color mapColor = new Color(0.18f, 0.13f, 0.085f, 1f);
        [SerializeField] private Color infoPanelColor = new Color(0.13f, 0.10f, 0.075f, 0.98f);
        [SerializeField] private Color pointButtonColor = new Color(0.56f, 0.40f, 0.23f, 1f);
        [SerializeField] private Color pointIconColor = new Color(0.94f, 0.76f, 0.42f, 1f);
        [SerializeField] private Color closeButtonColor = new Color(0.36f, 0.20f, 0.16f, 1f);

        [Header("Text")]
        [SerializeField] private string fallbackTitleText = "파견 지도";
        [SerializeField] private string fallbackDescriptionText = "방문할 포인트를 선택하면 재료를 수급합니다.";
        [SerializeField] private string emptyPointText = "방문 가능한 포인트가 없습니다.";
        [SerializeField] private string defaultPointInfoTitleText = "파견지 정보";
        [SerializeField] private string defaultPointInfoDescriptionText = "지도 위 아이콘에 마우스를 올리면 획득 가능한 재료와 수량을 확인할 수 있습니다.";
        [SerializeField] private string closeButtonText = "닫기";

        private DispatchController _dispatchController;
        private DispatchMapSO _dispatchMap;
        private bool _isGeneratedLayoutBuilt;

        /// <summary>
        /// 파견 지도 UI 표시
        /// </summary>
        /// <param name="controller">파견 흐름 컨트롤러</param>
        /// <param name="map">표시할 파견 지도 데이터</param>
        /// <param name="defaultFontAsset">기본 UI 폰트</param>
        public void Show(DispatchController controller, DispatchMapSO map, TMP_FontAsset defaultFontAsset)
        {
            _dispatchController = controller;
            _dispatchMap = map;

            if (defaultFontAsset != null)
            {
                SetFontAsset(defaultFontAsset);
            }

            EnsureLayout();
            gameObject.SetActive(true);
            transform.SetAsLastSibling();

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            Refresh();
        }

        /// <summary>
        /// 지도 UI 숨김
        /// </summary>
        public void Hide()
        {
            EnsureLayout();

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            gameObject.SetActive(false);
        }

        /// <summary>
        /// 지도 UI 새로고침
        /// </summary>
        public void Refresh()
        {
            EnsureLayout();
            BindText();
            BindDefaultPointInfo();
            RebuildPoints();
        }

        /// <summary>
        /// 기본 폰트 지정
        /// </summary>
        /// <param name="value">적용할 TMP 폰트</param>
        public void SetFontAsset(TMP_FontAsset value)
        {
            if (value == null)
            {
                return;
            }

            fontAsset = value;
            ApplyFont(titleField);
            ApplyFont(descriptionField);
            ApplyFont(pointInfoTitleField);
            ApplyFont(pointInfoDescriptionField);
            ApplyFont(pointInfoRewardField);
            ApplyFont(emptyField);
        }

        /// <summary>
        /// 기본 생성 UI에 사용할 스프라이트 지정
        /// </summary>
        /// <param name="panel">패널 배경 스프라이트</param>
        /// <param name="label">정보 라벨 배경 스프라이트</param>
        /// <param name="button">버튼 배경 스프라이트</param>
        public void SetUiSprites(Sprite panel, Sprite label, Sprite button)
        {
            bool isChanged = panelSprite != panel || labelSprite != label || buttonSprite != button;
            panelSprite = panel;
            labelSprite = label;
            buttonSprite = button;

            if (_isGeneratedLayoutBuilt == false || isChanged == false)
            {
                return;
            }

            ResetGeneratedLayoutReferences();
            EnsureLayout();
            BindText();
            BindDefaultPointInfo();
            RebuildPoints();
        }

        private void Awake()
        {
            EnsureLayout();
            Hide();
        }

        private void EnsureLayout()
        {
            if (buildDefaultLayoutWhenMissing == false)
            {
                return;
            }

            if (canvasGroup != null
                && overlayImage != null
                && pointRoot != null
                && closeButton != null
                && titleField != null
                && descriptionField != null
                && pointInfoPanel != null
                && pointInfoTitleField != null
                && pointInfoDescriptionField != null
                && pointInfoRewardField != null
                && emptyField != null)
            {
                return;
            }

            DispatchDefaultUiUtility.ClearChildren(transform);
            DispatchDefaultUiUtility.EnsureStretchRect(gameObject);
            canvasGroup = DispatchDefaultUiUtility.GetOrAdd<CanvasGroup>(gameObject);

            overlayImage = DispatchDefaultUiUtility.GetOrAdd<Image>(gameObject);
            DispatchDefaultUiUtility.ApplyGeneratedSprite(overlayImage);
            overlayImage.color = overlayColor;
            overlayImage.raycastTarget = true;

            RectTransform panel = CreatePanel();
            CreateHeader(panel);

            descriptionField = DispatchDefaultUiUtility.CreateText(
                panel,
                "Description",
                fallbackDescriptionText,
                15f,
                fontAsset,
                TextAlignmentOptions.Center,
                new Color(0.82f, 0.76f, 0.66f, 1f),
                true);
            DispatchDefaultUiUtility.AddLayoutElement(descriptionField.gameObject, -1f, 44f, -1f, 0f);

            pointRoot = CreateMapArea(panel);
            pointInfoPanel = CreatePointInfoPanel(panel);
            emptyField = DispatchDefaultUiUtility.CreateText(
                panel,
                "Empty",
                string.Empty,
                15f,
                fontAsset,
                TextAlignmentOptions.Center,
                new Color(0.76f, 0.70f, 0.62f, 1f),
                true);
            DispatchDefaultUiUtility.AddLayoutElement(emptyField.gameObject, -1f, 28f, -1f, 0f);
            _isGeneratedLayoutBuilt = true;
        }

        private void ResetGeneratedLayoutReferences()
        {
            canvasGroup = null;
            overlayImage = null;
            pointRoot = null;
            closeButton = null;
            titleField = null;
            descriptionField = null;
            pointInfoPanel = null;
            pointInfoTitleField = null;
            pointInfoDescriptionField = null;
            pointInfoRewardField = null;
            emptyField = null;
            _isGeneratedLayoutBuilt = false;
        }

        private RectTransform CreatePanel()
        {
            GameObject panelObject = new GameObject("MapPanel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            panelObject.transform.SetParent(transform, false);

            RectTransform panel = panelObject.GetComponent<RectTransform>();
            panel.anchorMin = new Vector2(0.5f, 0.5f);
            panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.anchoredPosition = Vector2.zero;
            panel.sizeDelta = new Vector2(820f, 560f);

            Image panelImage = panelObject.GetComponent<Image>();
            ApplyUiSprite(panelImage, panelSprite, panelColor);

            VerticalLayoutGroup layoutGroup = panelObject.GetComponent<VerticalLayoutGroup>();
            layoutGroup.padding = new RectOffset(24, 24, 20, 20);
            layoutGroup.spacing = 10f;
            layoutGroup.childAlignment = TextAnchor.UpperCenter;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;
            return panel;
        }

        private void CreateHeader(Transform parent)
        {
            GameObject headerObject = new GameObject("Header", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            headerObject.transform.SetParent(parent, false);
            DispatchDefaultUiUtility.AddLayoutElement(headerObject, -1f, 48f, -1f, 0f);

            HorizontalLayoutGroup layoutGroup = headerObject.GetComponent<HorizontalLayoutGroup>();
            layoutGroup.spacing = 12f;
            layoutGroup.childAlignment = TextAnchor.MiddleCenter;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = true;

            titleField = DispatchDefaultUiUtility.CreateText(
                headerObject.transform,
                "Title",
                fallbackTitleText,
                25f,
                fontAsset,
                TextAlignmentOptions.MidlineLeft,
                Color.white,
                false);
            DispatchDefaultUiUtility.AddLayoutElement(titleField.gameObject, 0f, -1f, 1f, -1f);

            closeButton = DispatchDefaultUiUtility.CreateButton(
                headerObject.transform,
                "CloseButton",
                closeButtonText,
                closeButtonColor,
                fontAsset,
                HandleCloseClicked);
            ApplyButtonSprite(closeButton, buttonSprite, closeButtonColor);
            DispatchDefaultUiUtility.AddLayoutElement(closeButton.gameObject, 96f, -1f, 0f, -1f);
        }

        private RectTransform CreateMapArea(Transform parent)
        {
            GameObject mapObject = new GameObject("MapArea", typeof(RectTransform), typeof(Image));
            mapObject.transform.SetParent(parent, false);
            DispatchDefaultUiUtility.AddLayoutElement(mapObject, -1f, 0f, -1f, 1f);

            Image image = mapObject.GetComponent<Image>();
            ApplyUiSprite(image, panelSprite, mapColor);
            return mapObject.GetComponent<RectTransform>();
        }

        private RectTransform CreatePointInfoPanel(Transform parent)
        {
            GameObject panelObject = new GameObject("PointInfoPanel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            panelObject.transform.SetParent(parent, false);
            DispatchDefaultUiUtility.AddLayoutElement(panelObject, -1f, 104f, -1f, 0f);

            Image image = panelObject.GetComponent<Image>();
            ApplyUiSprite(image, labelSprite != null ? labelSprite : panelSprite, infoPanelColor);

            VerticalLayoutGroup layoutGroup = panelObject.GetComponent<VerticalLayoutGroup>();
            layoutGroup.padding = new RectOffset(14, 14, 10, 10);
            layoutGroup.spacing = 4f;
            layoutGroup.childAlignment = TextAnchor.UpperLeft;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;

            pointInfoTitleField = DispatchDefaultUiUtility.CreateText(
                panelObject.transform,
                "PointInfoTitle",
                defaultPointInfoTitleText,
                17f,
                fontAsset,
                TextAlignmentOptions.MidlineLeft,
                Color.white,
                false);
            DispatchDefaultUiUtility.AddLayoutElement(pointInfoTitleField.gameObject, -1f, 24f, -1f, 0f);

            pointInfoDescriptionField = DispatchDefaultUiUtility.CreateText(
                panelObject.transform,
                "PointInfoDescription",
                defaultPointInfoDescriptionText,
                13f,
                fontAsset,
                TextAlignmentOptions.TopLeft,
                new Color(0.82f, 0.76f, 0.66f, 1f),
                true);
            DispatchDefaultUiUtility.AddLayoutElement(pointInfoDescriptionField.gameObject, -1f, 36f, -1f, 0f);

            pointInfoRewardField = DispatchDefaultUiUtility.CreateText(
                panelObject.transform,
                "PointInfoRewards",
                string.Empty,
                14f,
                fontAsset,
                TextAlignmentOptions.TopLeft,
                new Color(0.96f, 0.82f, 0.48f, 1f),
                true);
            DispatchDefaultUiUtility.AddLayoutElement(pointInfoRewardField.gameObject, -1f, 24f, -1f, 0f);
            return panelObject.GetComponent<RectTransform>();
        }

        private void BindText()
        {
            string title = _dispatchMap != null ? _dispatchMap.DisplayName : fallbackTitleText;
            string description = _dispatchMap != null && string.IsNullOrWhiteSpace(_dispatchMap.Description) == false
                ? _dispatchMap.Description
                : fallbackDescriptionText;

            SetText(titleField, title);
            SetText(descriptionField, description);
        }

        private void RebuildPoints()
        {
            DispatchDefaultUiUtility.ClearChildren(pointRoot);

            IReadOnlyList<DispatchPointSO> points = _dispatchMap != null ? _dispatchMap.Points : null;
            if (pointRoot == null || points == null || points.Count == 0)
            {
                SetText(emptyField, emptyPointText);
                return;
            }

            int visiblePointCount = 0;
            for (int i = 0; i < points.Count; i++)
            {
                DispatchPointSO point = points[i];
                if (point == null)
                {
                    continue;
                }

                CreatePointButton(point);
                visiblePointCount++;
            }

            SetText(emptyField, visiblePointCount > 0 ? string.Empty : emptyPointText);
        }

        private void CreatePointButton(DispatchPointSO point)
        {
            DispatchPointSO capturedPoint = point;
            Button button = DispatchDefaultUiUtility.CreateButton(
                pointRoot,
                $"Point_{point.PointId}",
                BuildPointButtonLabel(point),
                pointButtonColor,
                fontAsset,
                () => HandlePointClicked(capturedPoint));
            ApplyButtonSprite(button, buttonSprite, pointButtonColor);
            ConfigurePointButtonVisual(button, point);
            AddPointHoverEvents(button.gameObject, capturedPoint);

            RectTransform rectTransform = button.GetComponent<RectTransform>();
            Vector2 normalizedPosition = point.NormalizedMapPosition;
            rectTransform.anchorMin = normalizedPosition;
            rectTransform.anchorMax = normalizedPosition;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(76f, 76f);
            button.interactable = point.HasValidReward == true
                                  && (_dispatchController == null || _dispatchController.IsDispatching == false);
        }

        private string BuildPointButtonLabel(DispatchPointSO point)
        {
            if (point == null)
            {
                return string.Empty;
            }

            string displayName = point.DisplayName;
            if (string.IsNullOrWhiteSpace(displayName) == true)
            {
                return "?";
            }

            return displayName.Substring(0, 1);
        }

        private void ConfigurePointButtonVisual(Button button, DispatchPointSO point)
        {
            if (button == null || point == null)
            {
                return;
            }

            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null && point.Icon == null)
            {
                label.fontSize = 24f;
                label.color = pointIconColor;
            }

            Image pointImage = button.GetComponent<Image>();
            if (pointImage == null || point.Icon == null)
            {
                return;
            }

            if (label != null)
            {
                label.gameObject.SetActive(false);
            }

            pointImage.sprite = point.Icon;
            pointImage.type = Image.Type.Simple;
            pointImage.preserveAspect = true;
            pointImage.color = Color.white;
        }

        private void AddPointHoverEvents(GameObject target, DispatchPointSO point)
        {
            if (target == null)
            {
                return;
            }

            EventTrigger trigger = target.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = target.AddComponent<EventTrigger>();
            }

            trigger.triggers.Clear();
            AddEventTrigger(trigger, EventTriggerType.PointerEnter, _ => BindPointInfo(point));
            AddEventTrigger(trigger, EventTriggerType.PointerExit, _ => BindDefaultPointInfo());
        }

        private static void AddEventTrigger(
            EventTrigger trigger,
            EventTriggerType eventType,
            UnityEngine.Events.UnityAction<BaseEventData> action)
        {
            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = eventType;
            entry.callback.AddListener(action);
            trigger.triggers.Add(entry);
        }

        private void BindPointInfo(DispatchPointSO point)
        {
            if (point == null)
            {
                BindDefaultPointInfo();
                return;
            }

            SetText(pointInfoTitleField, point.DisplayName);
            SetText(
                pointInfoDescriptionField,
                string.IsNullOrWhiteSpace(point.Description) == false ? point.Description : "설명이 없습니다.");
            SetText(pointInfoRewardField, $"획득 가능: {point.BuildRewardSummaryText()}");
        }

        private void BindDefaultPointInfo()
        {
            SetText(pointInfoTitleField, defaultPointInfoTitleText);
            SetText(pointInfoDescriptionField, defaultPointInfoDescriptionText);
            SetText(pointInfoRewardField, string.Empty);
        }

        private void HandlePointClicked(DispatchPointSO point)
        {
            if (_dispatchController == null || point == null)
            {
                return;
            }

            _dispatchController.StartDispatch(point);
        }

        private void HandleCloseClicked()
        {
            if (_dispatchController != null)
            {
                _dispatchController.CloseMap();
                return;
            }

            Hide();
        }

        private void SetText(TextMeshProUGUI field, string text)
        {
            if (field == null)
            {
                return;
            }

            field.text = text ?? string.Empty;
        }

        private void ApplyFont(TextMeshProUGUI field)
        {
            if (field == null || fontAsset == null)
            {
                return;
            }

            field.font = fontAsset;
        }

        private static void ApplyUiSprite(Image image, Sprite sprite, Color fallbackColor)
        {
            if (image == null)
            {
                return;
            }

            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Sliced;
                image.color = Color.white;
                return;
            }

            DispatchDefaultUiUtility.ApplyGeneratedSprite(image);
            image.color = fallbackColor;
        }

        private static void ApplyButtonSprite(Button button, Sprite sprite, Color fallbackColor)
        {
            if (button == null)
            {
                return;
            }

            Image image = button.GetComponent<Image>();
            ApplyUiSprite(image, sprite, fallbackColor);

            Color visualColor = sprite != null ? Color.white : fallbackColor;
            ColorBlock colors = button.colors;
            colors.normalColor = visualColor;
            colors.highlightedColor = Color.Lerp(visualColor, Color.white, 0.15f);
            colors.pressedColor = Color.Lerp(visualColor, Color.black, 0.18f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(visualColor.r * 0.45f, visualColor.g * 0.45f, visualColor.b * 0.45f, 0.65f);
            button.colors = colors;
        }
    }
}
