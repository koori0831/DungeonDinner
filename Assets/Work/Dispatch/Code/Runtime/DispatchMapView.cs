using System.Collections.Generic;
using TMPro;
using UnityEngine;
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
        [SerializeField] private TextMeshProUGUI emptyField;

        [Header("Default Layout")]
        [SerializeField] private bool buildDefaultLayoutWhenMissing = true;
        [SerializeField] private TMP_FontAsset fontAsset;
        [SerializeField] private Color overlayColor = new Color(0f, 0f, 0f, 0.42f);
        [SerializeField] private Color panelColor = new Color(0.09f, 0.075f, 0.055f, 0.97f);
        [SerializeField] private Color mapColor = new Color(0.18f, 0.13f, 0.085f, 1f);
        [SerializeField] private Color pointButtonColor = new Color(0.56f, 0.40f, 0.23f, 1f);
        [SerializeField] private Color closeButtonColor = new Color(0.36f, 0.20f, 0.16f, 1f);

        [Header("Text")]
        [SerializeField] private string fallbackTitleText = "파견 지도";
        [SerializeField] private string fallbackDescriptionText = "방문할 포인트를 선택하면 재료를 수급합니다.";
        [SerializeField] private string emptyPointText = "방문 가능한 포인트가 없습니다.";
        [SerializeField] private string closeButtonText = "닫기";

        private DispatchController _dispatchController;
        private DispatchMapSO _dispatchMap;

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
            ApplyFont(emptyField);
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
            DispatchDefaultUiUtility.ApplyGeneratedSprite(panelImage);
            panelImage.color = panelColor;

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
            DispatchDefaultUiUtility.AddLayoutElement(closeButton.gameObject, 96f, -1f, 0f, -1f);
        }

        private RectTransform CreateMapArea(Transform parent)
        {
            GameObject mapObject = new GameObject("MapArea", typeof(RectTransform), typeof(Image));
            mapObject.transform.SetParent(parent, false);
            DispatchDefaultUiUtility.AddLayoutElement(mapObject, -1f, 0f, -1f, 1f);

            Image image = mapObject.GetComponent<Image>();
            DispatchDefaultUiUtility.ApplyGeneratedSprite(image);
            image.color = mapColor;
            return mapObject.GetComponent<RectTransform>();
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
                BuildPointButtonText(point),
                pointButtonColor,
                fontAsset,
                () => HandlePointClicked(capturedPoint));

            RectTransform rectTransform = button.GetComponent<RectTransform>();
            Vector2 normalizedPosition = point.NormalizedMapPosition;
            rectTransform.anchorMin = normalizedPosition;
            rectTransform.anchorMax = normalizedPosition;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(180f, 78f);
            button.interactable = point.HasValidReward == true
                                  && (_dispatchController == null || _dispatchController.IsDispatching == false);
        }

        private string BuildPointButtonText(DispatchPointSO point)
        {
            if (point == null)
            {
                return string.Empty;
            }

            return $"{point.DisplayName}\n{point.BuildRewardSummaryText()}";
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
    }
}
