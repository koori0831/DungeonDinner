using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Work.Dispatch.Code.Data;

namespace Work.Dispatch.Code.Runtime
{
    /// <summary>
    /// 인스펙터에서 구성한 파견 진행 UI에 진행 상태 바인딩
    /// </summary>
    public sealed class DispatchProgressView : MonoBehaviour
    {
        [Header("Layout References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image pointIconImage;
        [SerializeField] private Image progressFillImage;
        [SerializeField] private TextMeshProUGUI titleField;
        [SerializeField] private TextMeshProUGUI rewardField;
        [SerializeField] private TextMeshProUGUI progressField;

        [Header("Text")]
        [SerializeField] private string fallbackTitleText = "파견";

        /// <summary>
        /// 파견 진행 UI 표시
        /// </summary>
        /// <param name="point">진행 중인 파견 포인트</param>
        public void Show(DispatchPointSO point)
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            string pointName = point != null ? point.DisplayName : fallbackTitleText;
            SetText(titleField, $"{pointName} 파견 중");
            SetText(rewardField, point != null ? point.BuildRewardSummaryText() : string.Empty);

            if (pointIconImage != null)
            {
                pointIconImage.sprite = point != null ? point.Icon : null;
                pointIconImage.gameObject.SetActive(pointIconImage.sprite != null);
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
            Hide();
        }

        private static void SetText(TextMeshProUGUI field, string text)
        {
            if (field != null)
            {
                field.text = text ?? string.Empty;
            }
        }
    }
}
