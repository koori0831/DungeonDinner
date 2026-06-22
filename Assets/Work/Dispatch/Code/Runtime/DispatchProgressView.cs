using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Work.Dispatch.Code.Data;

namespace Work.Dispatch.Code.Runtime
{
    /// <summary>
    /// 파견 진행 중 입력을 차단하고 진행률을 표시하는 Temp UI
    /// </summary>
    public sealed class DispatchProgressView : MonoBehaviour
    {
        [Header("Layout References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image overlayImage;
        [SerializeField] private Image pointIconImage;
        [SerializeField] private Image progressFillImage;
        [SerializeField] private TextMeshProUGUI titleField;
        [SerializeField] private TextMeshProUGUI rewardField;
        [SerializeField] private TextMeshProUGUI progressField;

        [Header("Default Layout")]
        [SerializeField] private bool buildDefaultLayoutWhenMissing = true;
        [SerializeField] private TMP_FontAsset fontAsset;
        [SerializeField] private Color overlayColor = new Color(0f, 0f, 0f, 0.58f);
        [SerializeField] private Color panelColor = new Color(0.08f, 0.07f, 0.06f, 0.96f);
        [SerializeField] private Color iconColor = new Color(0.24f, 0.18f, 0.12f, 1f);
        [SerializeField] private Color progressBackgroundColor = new Color(0.20f, 0.16f, 0.11f, 1f);
        [SerializeField] private Color progressFillColor = new Color(0.78f, 0.55f, 0.28f, 1f);

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
            ApplyFont(rewardField);
            ApplyFont(progressField);
        }

        /// <summary>
        /// 파견 진행 UI 표시
        /// </summary>
        /// <param name="point">진행 중인 파견 포인트</param>
        public void Show(DispatchPointSO point)
        {
            EnsureLayout();
            gameObject.SetActive(true);
            transform.SetAsLastSibling();

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            string pointName = point != null ? point.DisplayName : "파견";
            SetText(titleField, $"{pointName} 파견 중");
            SetText(rewardField, point != null ? point.BuildRewardSummaryText() : string.Empty);

            if (pointIconImage != null)
            {
                pointIconImage.sprite = point != null ? point.Icon : null;
                pointIconImage.color = pointIconImage.sprite != null ? Color.white : iconColor;
            }

            SetProgress(0f, point != null ? point.DurationSeconds : 0f);
        }

        /// <summary>
        /// 진행률 표시 갱신
        /// </summary>
        /// <param name="normalizedProgress">0~1 진행률</param>
        /// <param name="remainingSeconds">남은 시간</param>
        public void SetProgress(float normalizedProgress, float remainingSeconds)
        {
            float progress = Mathf.Clamp01(normalizedProgress);

            if (progressFillImage != null)
            {
                progressFillImage.fillAmount = progress;
            }

            int percent = Mathf.RoundToInt(progress * 100f);
            float safeRemainingSeconds = Mathf.Max(0f, remainingSeconds);
            SetText(progressField, $"{percent}%  /  {safeRemainingSeconds:0.0}s");
        }

        /// <summary>
        /// 파견 진행 UI 숨김
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
                && pointIconImage != null
                && progressFillImage != null
                && titleField != null
                && rewardField != null
                && progressField != null)
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
            pointIconImage = CreateIcon(panel);
            titleField = DispatchDefaultUiUtility.CreateText(panel, "Title", "파견 중", 24f, fontAsset, TextAlignmentOptions.Center, Color.white, true);
            DispatchDefaultUiUtility.AddLayoutElement(titleField.gameObject, -1f, 38f, -1f, 0f);

            rewardField = DispatchDefaultUiUtility.CreateText(panel, "Rewards", string.Empty, 16f, fontAsset, TextAlignmentOptions.Center, new Color(0.88f, 0.76f, 0.58f, 1f), true);
            DispatchDefaultUiUtility.AddLayoutElement(rewardField.gameObject, -1f, 48f, -1f, 0f);

            CreateProgressBar(panel);
            progressField = DispatchDefaultUiUtility.CreateText(panel, "ProgressText", string.Empty, 16f, fontAsset, TextAlignmentOptions.Center, Color.white, false);
            DispatchDefaultUiUtility.AddLayoutElement(progressField.gameObject, -1f, 28f, -1f, 0f);
        }

        private RectTransform CreatePanel()
        {
            GameObject panelObject = new GameObject("ProgressPanel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            panelObject.transform.SetParent(transform, false);

            RectTransform panel = panelObject.GetComponent<RectTransform>();
            panel.anchorMin = new Vector2(0.5f, 0.5f);
            panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.anchoredPosition = Vector2.zero;
            panel.sizeDelta = new Vector2(520f, 320f);

            Image panelImage = panelObject.GetComponent<Image>();
            DispatchDefaultUiUtility.ApplyGeneratedSprite(panelImage);
            panelImage.color = panelColor;

            VerticalLayoutGroup layoutGroup = panelObject.GetComponent<VerticalLayoutGroup>();
            layoutGroup.padding = new RectOffset(24, 24, 24, 24);
            layoutGroup.spacing = 12f;
            layoutGroup.childAlignment = TextAnchor.MiddleCenter;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;
            return panel;
        }

        private Image CreateIcon(Transform parent)
        {
            GameObject iconObject = new GameObject("PointIcon", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(parent, false);
            DispatchDefaultUiUtility.AddLayoutElement(iconObject, 82f, 82f, 0f, 0f);

            Image image = iconObject.GetComponent<Image>();
            DispatchDefaultUiUtility.ApplyGeneratedSprite(image);
            image.color = iconColor;
            image.preserveAspect = true;
            return image;
        }

        private void CreateProgressBar(Transform parent)
        {
            GameObject barObject = new GameObject("ProgressBar", typeof(RectTransform), typeof(Image));
            barObject.transform.SetParent(parent, false);
            DispatchDefaultUiUtility.AddLayoutElement(barObject, -1f, 32f, -1f, 0f);

            Image barImage = barObject.GetComponent<Image>();
            DispatchDefaultUiUtility.ApplyGeneratedSprite(barImage);
            barImage.color = progressBackgroundColor;

            GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillObject.transform.SetParent(barObject.transform, false);
            RectTransform fillRect = fillObject.GetComponent<RectTransform>();
            DispatchDefaultUiUtility.StretchToParent(fillRect);

            progressFillImage = fillObject.GetComponent<Image>();
            DispatchDefaultUiUtility.ApplyGeneratedSprite(progressFillImage);
            progressFillImage.color = progressFillColor;
            progressFillImage.type = Image.Type.Filled;
            progressFillImage.fillMethod = Image.FillMethod.Horizontal;
            progressFillImage.fillOrigin = 0;
            progressFillImage.fillAmount = 0f;
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
