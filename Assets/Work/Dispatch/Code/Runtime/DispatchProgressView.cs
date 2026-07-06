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

        [Header("View Settings")]
        [SerializeField] private TMP_FontAsset fontAsset;
        [SerializeField] private Color iconColor = new Color(0.24f, 0.18f, 0.12f, 1f);

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

            Debug.LogError("DispatchProgressView is missing canvasGroup/overlayImage/pointIconImage/progressFillImage/titleField/rewardField/progressField references. Assign a prefab/inspector based progress view.", this);
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
