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
        [SerializeField] private RectTransform pointInfoPanel;
        [SerializeField] private TextMeshProUGUI pointInfoTitleField;
        [SerializeField] private TextMeshProUGUI pointInfoDescriptionField;
        [SerializeField] private TextMeshProUGUI pointInfoRewardField;
        [SerializeField] private TextMeshProUGUI emptyField;

        [Header("Prefabs")]
        [SerializeField] private DispatchPointButtonView pointButtonPrefab;

        [Header("View Settings")]
        [SerializeField] private TMP_FontAsset fontAsset;
        [SerializeField] private Sprite panelSprite;
        [SerializeField] private Sprite labelSprite;
        [SerializeField] private Sprite buttonSprite;

        [Header("Text")]
        [SerializeField] private string fallbackTitleText = "파견 지도";
        [SerializeField] private string fallbackDescriptionText = "방문할 포인트를 선택하면 재료를 수급합니다.";
        [SerializeField] private string emptyPointText = "방문 가능한 포인트가 없습니다.";
        [SerializeField] private string defaultPointInfoTitleText = "파견지 정보";
        [SerializeField] private string defaultPointInfoDescriptionText = "지도 위 아이콘에 마우스를 올리면 획득 가능한 재료와 수량을 확인할 수 있습니다.";

        private DispatchController _dispatchController;
        private DispatchMapSO _dispatchMap;

        /// <summary>
        /// 파견 지도 UI 표시
        /// </summary>
        /// <param name="controller">파견 흐름 컨트롤러</param>
        /// <param name="map">표시할 파견 지도 데이터</param>
        public void Show(DispatchController controller, DispatchMapSO map)
        {
            _dispatchController = controller;
            _dispatchMap = map;

            EnsureLayout();
            gameObject.SetActive(true);

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
            panelSprite = panel;
            labelSprite = label;
            buttonSprite = button;
        }

        private void Awake()
        {
            EnsureLayout();
            Hide();
        }

        private void EnsureLayout()
        {
            if (HasRequiredLayoutReferences() == true)
            {
                return;
            }

            Debug.LogError("DispatchMapView is missing inspector layout references or pointButtonPrefab. Assign a prefab/inspector based map view.", this);
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
            if (pointButtonPrefab != null)
            {
                DispatchPointButtonView view = Instantiate(pointButtonPrefab, pointRoot);
                Button prefabButton = view.Button;
                view.Bind(
                    BuildPointButtonLabel(point),
                    point.Icon,
                    point.HasValidReward == true && (_dispatchController == null || _dispatchController.IsDispatching == false),
                    () => HandlePointClicked(capturedPoint),
                    () => BindPointInfo(capturedPoint),
                    BindDefaultPointInfo);
                ConfigurePointButtonRect(view.transform as RectTransform, point);
                if (prefabButton != null)
                {
                    prefabButton.interactable = point.HasValidReward == true
                                                && (_dispatchController == null || _dispatchController.IsDispatching == false);
                }

                return;
            }

            Debug.LogError("DispatchMapView pointButtonPrefab is missing. Assign a point button prefab.", this);
        }

        private void ConfigurePointButtonRect(RectTransform rectTransform, DispatchPointSO point)
        {
            if (rectTransform == null || point == null)
            {
                return;
            }

            Vector2 normalizedPosition = point.NormalizedMapPosition;
            rectTransform.anchorMin = normalizedPosition;
            rectTransform.anchorMax = normalizedPosition;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(76f, 76f);
        }

        private bool HasRequiredLayoutReferences()
        {
            return canvasGroup != null
                   && overlayImage != null
                   && pointRoot != null
                   && closeButton != null
                   && titleField != null
                   && descriptionField != null
                   && pointInfoPanel != null
                   && pointInfoTitleField != null
                   && pointInfoDescriptionField != null
                   && pointInfoRewardField != null
                   && emptyField != null
                   && pointButtonPrefab != null;
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

    }
}
