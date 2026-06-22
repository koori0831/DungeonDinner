using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Work.Dispatch.Code.Data;

namespace Work.Dispatch.Code.Runtime
{
    /// <summary>
    /// 인스펙터에서 구성한 파견 지도 UI에 포인트 데이터 바인딩
    /// </summary>
    public sealed class DispatchMapView : MonoBehaviour
    {
        [Header("Layout References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform pointRoot;
        [SerializeField] private DispatchPointButtonView pointButtonPrefab;
        [SerializeField] private Button closeButton;
        [SerializeField] private TextMeshProUGUI titleField;
        [SerializeField] private TextMeshProUGUI descriptionField;
        [SerializeField] private TextMeshProUGUI emptyField;

        [Header("Text")]
        [SerializeField] private string fallbackTitleText = "파견 지도";
        [SerializeField] private string fallbackDescriptionText = "방문할 포인트를 선택하면 재료를 수급합니다.";
        [SerializeField] private string emptyPointText = "방문 가능한 포인트가 없습니다.";

        private DispatchController _dispatchController;
        private DispatchMapSO _dispatchMap;
        private bool _loggedMissingPointPrefab;

        /// <summary>
        /// 파견 지도 UI 표시
        /// </summary>
        /// <param name="controller">파견 흐름 컨트롤러</param>
        /// <param name="map">표시할 파견 지도 데이터</param>
        public void Show(DispatchController controller, DispatchMapSO map)
        {
            _dispatchController = controller;
            _dispatchMap = map;
            BindCloseButton();

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
            BindText();
            RebuildPoints();
        }

        private void Awake()
        {
            BindCloseButton();
            Hide();
        }

        private void OnEnable()
        {
            BindCloseButton();
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
                SetActive(emptyField != null ? emptyField.gameObject : null, true);
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

            bool hasVisiblePoint = visiblePointCount > 0;
            SetText(emptyField, hasVisiblePoint == true ? string.Empty : emptyPointText);
            SetActive(emptyField != null ? emptyField.gameObject : null, hasVisiblePoint == false);
        }

        private void CreatePointButton(DispatchPointSO point)
        {
            if (pointRoot == null)
            {
                return;
            }

            if (pointButtonPrefab == null)
            {
                if (_loggedMissingPointPrefab == false)
                {
                    Debug.LogWarning("DispatchMapView needs a point button prefab before it can build map points.", this);
                    _loggedMissingPointPrefab = true;
                }

                return;
            }

            DispatchPointSO capturedPoint = point;
            DispatchPointButtonView pointButton = Instantiate(pointButtonPrefab, pointRoot);
            pointButton.name = $"Point_{point.PointId}";
            bool interactable = point.HasValidReward == true
                                && (_dispatchController == null || _dispatchController.IsDispatching == false);
            pointButton.Bind(point, () => HandlePointClicked(capturedPoint), interactable);
            ApplyPointPosition(pointButton.transform as RectTransform, point);
        }

        private static void ApplyPointPosition(RectTransform rectTransform, DispatchPointSO point)
        {
            if (rectTransform == null || point == null)
            {
                return;
            }

            Vector2 normalizedPosition = point.NormalizedMapPosition;
            rectTransform.anchorMin = normalizedPosition;
            rectTransform.anchorMax = normalizedPosition;
            rectTransform.anchoredPosition = Vector2.zero;
        }

        private void HandlePointClicked(DispatchPointSO point)
        {
            if (_dispatchController == null || point == null)
            {
                return;
            }

            _dispatchController.StartDispatch(point);
        }

        private void BindCloseButton()
        {
            if (closeButton == null)
            {
                return;
            }

            closeButton.onClick.RemoveListener(HandleCloseClicked);
            closeButton.onClick.AddListener(HandleCloseClicked);
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

        private static void SetText(TextMeshProUGUI field, string text)
        {
            if (field != null)
            {
                field.text = text ?? string.Empty;
            }
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
            {
                target.SetActive(active);
            }
        }
    }
}
