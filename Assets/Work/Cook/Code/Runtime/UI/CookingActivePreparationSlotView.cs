using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Work.Cook.Code.Data;
using Work.Cook.Code.Runtime.Core;

namespace Work.Cook.Code.Runtime.UI
{
    public enum CookingActivePreparationState
    {
        Empty,
        Selected,
        InProgress,
        Completed
    }

    /// <summary>
    /// 현재 작업 슬롯에 놓인 손질 카드와 상태를 표시한다.
    /// </summary>
    public sealed class CookingActivePreparationSlotView : MonoBehaviour
    {
        [SerializeField] private RectTransform visualRoot;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image iconImage;
        [SerializeField] private Image stateAccentImage;
        [SerializeField] private Image gradeIconImage;
        [SerializeField] private TextMeshProUGUI gradeLabelField;
        [SerializeField] private TextMeshProUGUI titleField;
        [SerializeField] private TextMeshProUGUI descriptionField;
        [SerializeField] private TextMeshProUGUI stateField;
        [SerializeField] private CookingUiPresentationSettingsSO presentationSettings;
        [SerializeField] private string emptyTitleText = "작업 슬롯";
        [SerializeField] private string emptyDescriptionText = "하단 카드에서 손질법을 선택하세요.";

        private Tween _activeTween;

        public CookingActivePreparationState State { get; private set; } = CookingActivePreparationState.Empty;

        private void Awake()
        {
            EnsureReferences();
        }

        private void OnDisable()
        {
            KillTween();
            ResetVisualTransform();
        }

        public void SetPresentationSettings(CookingUiPresentationSettingsSO value)
        {
            presentationSettings = value;
            ApplyFont();
        }

        public void SetFontAsset(TMP_FontAsset value)
        {
            if (value == null)
                return;

            if (titleField != null)
                titleField.font = value;
            if (descriptionField != null)
                descriptionField.font = value;
            if (stateField != null)
                stateField.font = value;
            if (gradeLabelField != null)
                gradeLabelField.font = value;
        }

        public void Clear()
        {
            State = CookingActivePreparationState.Empty;
            KillTween();
            BindIcon(null);
            BindGrade(null);
            SetText(titleField, emptyTitleText);
            SetText(descriptionField, emptyDescriptionText);
            SetText(stateField, "대기");
            SetAccent(new Color(0.38f, 0.28f, 0.18f, 0.75f));
            ResetVisualTransform();
        }

        public void Bind(IngredientPreparationOption option)
        {
            State = CookingActivePreparationState.Selected;
            KillTween();
            BindIcon(option);
            BindGrade(null);
            string optionName = option != null ? option.DisplayName : "그대로 사용";
            SetText(titleField, optionName);
            SetText(descriptionField, "중앙 조리대를 눌러 손질을 시작하세요.");
            SetText(stateField, "준비됨 · 재료를 클릭하세요");
            SetAccent(presentationSettings != null ? presentationSettings.ParchmentColor : new Color(0.9f, 0.7f, 0.3f, 1f));
            PlayCommitAnimation();
        }

        public void BindInProgress(IngredientPreparationOption option)
        {
            State = CookingActivePreparationState.InProgress;
            KillTween();
            BindIcon(option);
            BindGrade(null);
            string optionName = option != null ? option.DisplayName : "그대로 사용";
            SetText(titleField, optionName);
            SetText(descriptionField, "재료 위의 안내를 따라 직접 조작하세요.");
            SetText(stateField, "진행 중");
            SetAccent(new Color(0.9f, 0.48f, 0.16f, 1f));
            PlayProgressPulse();
        }

        public void BindResult(IngredientPreparationOption option, PreparedIngredientState prepared = null)
        {
            State = CookingActivePreparationState.Completed;
            KillTween();
            BindIcon(option);
            string optionName = option != null ? option.DisplayName : "그대로 사용";
            string gradeText = prepared?.MiniGameResult != null
                ? BuildGradeText(prepared.MiniGameResult.Grade)
                : "완료";
            string deltaText = prepared != null && prepared.QualityDelta != 0
                ? $" · 품질 {prepared.QualityDelta:+#;-#;0}"
                : string.Empty;
            SetText(titleField, optionName);
            SetText(descriptionField, $"{gradeText}{deltaText}");
            SetText(stateField, "완료");
            BindGrade(prepared);
            SetAccent(presentationSettings != null ? presentationSettings.PositiveColor : new Color(0.95f, 0.75f, 0.25f, 1f));
            PlayCompleteAnimation();
        }

        public void BindResultPreview(IngredientPreparationOption option, CookingMiniGameResult result)
        {
            State = CookingActivePreparationState.Completed;
            KillTween();
            BindIcon(option);
            string optionName = option != null ? option.DisplayName : "그대로 사용";
            SetText(titleField, optionName);
            SetText(descriptionField, result != null ? BuildGradeText(result.Grade) : "완료");
            SetText(stateField, "판정 완료");
            BindGradeResult(result);
            SetAccent(result != null
                ? GetGradeColor(result.Grade)
                : presentationSettings != null
                    ? presentationSettings.PositiveColor
                    : new Color(0.95f, 0.75f, 0.25f, 1f));
            PlayCompleteAnimation();
        }

        private void BindIcon(IngredientPreparationOption option)
        {
            if (iconImage == null)
                return;

            Sprite icon = option?.Method != null ? option.Method.IconSprite : null;
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
            iconImage.preserveAspect = true;
        }

        private void BindGrade(PreparedIngredientState prepared)
        {
            BindGradeResult(prepared?.MiniGameResult);
        }

        private void BindGradeResult(CookingMiniGameResult result)
        {
            if (gradeIconImage != null)
            {
                gradeIconImage.sprite = null;
                gradeIconImage.enabled = result != null;
                if (result != null)
                    gradeIconImage.color = GetGradeColor(result.Grade);
            }

            if (gradeLabelField != null)
            {
                gradeLabelField.gameObject.SetActive(result != null);
                gradeLabelField.text = result != null ? BuildGradeLabel(result.Grade) : string.Empty;
                if (result != null)
                    gradeLabelField.color = Color.white;
            }
        }

        private void PlayCommitAnimation()
        {
            if (visualRoot == null)
                return;

            visualRoot.localScale = new Vector3(0.9f, 0.9f, 1f);
            if (canvasGroup != null)
                canvasGroup.alpha = 0.35f;

            Sequence sequence = DOTween.Sequence().SetUpdate(true);
            sequence.Join(visualRoot.DOScale(1f, 0.22f).SetEase(Ease.OutBack));
            if (canvasGroup != null)
                sequence.Join(canvasGroup.DOFade(1f, 0.16f));
            _activeTween = sequence;
        }

        private void PlayProgressPulse()
        {
            if (stateAccentImage == null)
                return;

            Color baseColor = stateAccentImage.color;
            Color pulseColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0.4f);
            _activeTween = stateAccentImage.DOColor(pulseColor, 0.58f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetUpdate(true);
        }

        private void PlayCompleteAnimation()
        {
            if (visualRoot == null)
                return;

            visualRoot.localScale = Vector3.one;
            Sequence sequence = DOTween.Sequence().SetUpdate(true);
            sequence.Append(visualRoot.DOScale(1.07f, 0.16f).SetEase(Ease.OutQuad));
            sequence.Append(visualRoot.DOScale(1f, 0.2f).SetEase(Ease.OutBack));
            _activeTween = sequence;
        }

        private void SetAccent(Color color)
        {
            if (stateAccentImage != null)
                stateAccentImage.color = color;
        }

        private void EnsureReferences()
        {
            if (visualRoot == null)
                visualRoot = transform as RectTransform;
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
            if (gradeLabelField == null)
            {
                Transform found = transform.Find("GradeSeal/GradeLabel");
                if (found != null)
                    gradeLabelField = found.GetComponent<TextMeshProUGUI>();
            }
        }

        private void ApplyFont()
        {
            if (presentationSettings?.FontAsset != null)
                SetFontAsset(presentationSettings.FontAsset);
        }

        private void KillTween()
        {
            _activeTween?.Kill();
            _activeTween = null;
        }

        private void ResetVisualTransform()
        {
            if (visualRoot != null)
                visualRoot.localScale = Vector3.one;
            if (canvasGroup != null)
                canvasGroup.alpha = 1f;
        }

        private static string BuildGradeText(CookingMiniGameGrade grade)
        {
            switch (grade)
            {
                case CookingMiniGameGrade.Perfect:
                    return "완벽";
                case CookingMiniGameGrade.Good:
                    return "좋음";
                case CookingMiniGameGrade.Normal:
                    return "보통";
                default:
                    return "아쉬움";
            }
        }

        private static string BuildGradeLabel(CookingMiniGameGrade grade)
        {
            switch (grade)
            {
                case CookingMiniGameGrade.Perfect:
                    return "S";
                case CookingMiniGameGrade.Good:
                    return "A";
                case CookingMiniGameGrade.Normal:
                    return "B";
                default:
                    return "C";
            }
        }

        private static Color GetGradeColor(CookingMiniGameGrade grade)
        {
            switch (grade)
            {
                case CookingMiniGameGrade.Perfect:
                    return new Color(1f, 0.78f, 0.24f, 1f);
                case CookingMiniGameGrade.Good:
                    return new Color(0.45f, 0.78f, 0.4f, 1f);
                case CookingMiniGameGrade.Normal:
                    return new Color(0.75f, 0.68f, 0.55f, 1f);
                default:
                    return new Color(0.76f, 0.3f, 0.22f, 1f);
            }
        }

        private static void SetText(TextMeshProUGUI field, string text)
        {
            if (field != null)
                field.text = text ?? string.Empty;
        }
    }
}
