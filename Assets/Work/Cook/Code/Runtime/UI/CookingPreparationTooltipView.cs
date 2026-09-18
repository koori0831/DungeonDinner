using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Work.Cook.Code.Data;

namespace Work.Cook.Code.Runtime.UI
{
    [DisallowMultipleComponent]
    public sealed class CookingPreparationTooltipView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform visualRoot;
        [SerializeField] private TextMeshProUGUI titleField;
        [SerializeField] private TextMeshProUGUI descriptionField;
        [SerializeField] private TextMeshProUGUI effectField;
        [SerializeField] private CookingUiPresentationSettingsSO settings;

        private Tween _activeTween;
        private CookingPreparationOptionCardView _owner;

        private void Awake()
        {
            HideImmediate();
        }

        private void OnDisable()
        {
            _activeTween?.Kill();
            _activeTween = null;
            _owner = null;
            HideImmediate();
        }

        public void SetSettings(CookingUiPresentationSettingsSO value)
        {
            settings = value;
        }

        public void Show(
            CookingPreparationOptionCardView owner,
            string title,
            string description,
            string effect)
        {
            if (owner == null || canvasGroup == null)
                return;

            _owner = owner;
            SetText(titleField, title);
            SetText(descriptionField, description);
            SetText(effectField, effect);
            ApplyFont();

            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            Canvas.ForceUpdateCanvases();
            if (visualRoot != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(visualRoot);
            PositionBesideOwner(owner);
            _activeTween?.Kill();
            canvasGroup.alpha = 0f;
            if (visualRoot != null)
                visualRoot.localScale = new Vector3(0.96f, 0.96f, 1f);

            Sequence sequence = DOTween.Sequence().SetUpdate(true);
            sequence.Join(canvasGroup.DOFade(1f, 0.12f));
            if (visualRoot != null)
                sequence.Join(visualRoot.DOScale(1f, 0.14f).SetEase(Ease.OutQuad));
            _activeTween = sequence;
        }

        public void Hide(CookingPreparationOptionCardView owner)
        {
            if (owner != null && _owner != null && owner != _owner)
                return;

            _owner = null;
            _activeTween?.Kill();
            _activeTween = null;
            HideImmediate();
        }

        private void HideImmediate()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
        }

        private void ApplyFont()
        {
            if (settings?.FontAsset == null)
                return;

            if (titleField != null)
                titleField.font = settings.FontAsset;
            if (descriptionField != null)
                descriptionField.font = settings.FontAsset;
            if (effectField != null)
                effectField.font = settings.FontAsset;
        }

        private void PositionBesideOwner(CookingPreparationOptionCardView owner)
        {
            RectTransform tooltipRect = transform as RectTransform;
            RectTransform parentRect = tooltipRect != null ? tooltipRect.parent as RectTransform : null;
            RectTransform cardRect = owner != null ? owner.LayoutRoot : null;
            if (tooltipRect == null || parentRect == null || cardRect == null)
                return;

            Vector3[] cardCorners = new Vector3[4];
            cardRect.GetWorldCorners(cardCorners);
            Vector3 cardCenterWorld = (cardCorners[0] + cardCorners[2]) * 0.5f;
            Vector2 cardCenter = parentRect.InverseTransformPoint(cardCenterWorld);
            Vector2 cardLeft = parentRect.InverseTransformPoint(cardCorners[0]);
            Vector2 cardRight = parentRect.InverseTransformPoint(cardCorners[3]);
            float cardHalfWidth = Mathf.Max(1f, Mathf.Abs(cardRight.x - cardLeft.x) * 0.5f);
            float tooltipHalfWidth = Mathf.Max(1f, tooltipRect.rect.width * 0.5f);
            float tooltipHalfHeight = Mathf.Max(1f, tooltipRect.rect.height * 0.5f);
            float direction = cardCenter.x <= parentRect.rect.center.x ? 1f : -1f;

            Vector2 target = new Vector2(
                cardCenter.x + direction * (cardHalfWidth + tooltipHalfWidth + 18f),
                cardCenter.y);
            Rect bounds = parentRect.rect;
            target.x = Mathf.Clamp(target.x, bounds.xMin + tooltipHalfWidth, bounds.xMax - tooltipHalfWidth);
            target.y = Mathf.Clamp(target.y, bounds.yMin + tooltipHalfHeight, bounds.yMax - tooltipHalfHeight);
            tooltipRect.localPosition = new Vector3(target.x, target.y, tooltipRect.localPosition.z);
        }

        private static void SetText(TextMeshProUGUI field, string value)
        {
            if (field != null)
                field.text = value ?? string.Empty;
        }
    }
}
