using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Work.Cook.Code.Data;
using Work.Cook.Code.Runtime.Events;
using Work.Cook.Code.Runtime.Systems;
using Work.Core.EventBus;
using Work.NPC.Code.Data;

namespace Work.Cook.Code.Runtime.UI
{
    public sealed class CookingRewardToastView : MonoBehaviour, ICookingRewardView
    {
        [Header("Flow")]
        [SerializeField] private CookingGamePanel gamePanel;
        [SerializeField] private CookingRewardWallet rewardWallet;

        [Header("Layout References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform visualRoot;
        [SerializeField] private Image rewardIconImage;
        [SerializeField] private TextMeshProUGUI titleField;
        [SerializeField] private TextMeshProUGUI rewardField;
        [SerializeField] private TextMeshProUGUI balanceField;

        [Header("View Settings")]
        [SerializeField] private CookingUiPresentationSettingsSO presentationSettings;
        [SerializeField] private TMP_FontAsset fontAsset;
        [SerializeField] private Color positiveColor = new Color(0.92f, 0.78f, 0.35f, 1f);
        [SerializeField] private Color emptyColor = new Color(0.72f, 0.68f, 0.60f, 1f);
        [SerializeField, Min(0.1f)] private float visibleDuration = 2.4f;
        [SerializeField, Min(0.01f)] private float enterDuration = 0.24f;
        [SerializeField, Min(0.01f)] private float countDuration = 0.42f;
        [SerializeField, Min(0.01f)] private float exitDuration = 0.2f;
        [SerializeField, Min(0f)] private float enterOffset = 72f;

        [Header("Text")]
        [SerializeField] private string titleText = "보상 획득";
        [SerializeField] private string noRewardText = "보상 없음";
        [SerializeField] private string balancePrefix = "소지금";

        private CookingGamePanel _subscribedPanel;
        private Sequence _activeSequence;
        private Tween _counterTween;
        private Vector2 _restingPosition;
        private int _displayedAmount;
        private int _accumulatedAmount;
        private int _targetBalance;
        private bool _isVisible;

        public int DisplayedAmount => _displayedAmount;
        public int AccumulatedAmount => _accumulatedAmount;

        private void Awake()
        {
            EnsureReferences();
            CaptureRestingPosition();
            HideImmediate();
        }

        private void OnEnable()
        {
            EnsureReferences();
            CaptureRestingPosition();
            SubscribePanelEvents();
            BindCurrentBalance();
        }

        private void OnDisable()
        {
            KillAnimations();
            UnsubscribePanelEvents();
            HideImmediate();
        }

        public void Initialize(
            CookingGamePanel owner,
            CookingRewardWallet wallet,
            TMP_FontAsset defaultFontAsset = null)
        {
            gamePanel = owner;
            rewardWallet = wallet;
            if (defaultFontAsset != null)
                SetFontAsset(defaultFontAsset);
            EnsureReferences();
            if (isActiveAndEnabled)
                SubscribePanelEvents();
        }

        public void SetPresentationSettings(CookingUiPresentationSettingsSO value)
        {
            presentationSettings = value;
            if (value?.FontAsset != null)
                SetFontAsset(value.FontAsset);
            if (rewardIconImage != null)
            {
                rewardIconImage.sprite = value?.RewardIcon;
                rewardIconImage.enabled = rewardIconImage.sprite != null;
                rewardIconImage.preserveAspect = true;
            }
        }

        public void SetFontAsset(TMP_FontAsset value)
        {
            if (value == null)
                return;

            fontAsset = value;
            TextMeshProUGUI[] labels = GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < labels.Length; i++)
            {
                if (labels[i] != null)
                    labels[i].font = value;
            }
        }

        public void Show(CookingRewardGrant grant)
        {
            EnsureReferences();
            if (grant == null)
                return;

            bool append = _isVisible;
            if (append == false)
            {
                _accumulatedAmount = 0;
                _displayedAmount = 0;
            }

            _accumulatedAmount += Mathf.Max(0, grant.Amount);
            _targetBalance = grant.BalanceAfter;
            SetText(titleField, BuildTitleText(grant, append));
            if (rewardField != null)
                rewardField.color = _accumulatedAmount > 0 ? ResolvePositiveColor() : emptyColor;

            if (append == false)
                PlayEntrance();

            PlayCounterAndScheduleHide(append);
        }

        private void PlayEntrance()
        {
            _isVisible = true;
            gameObject.SetActive(true);
            KillAnimations();
            if (canvasGroup != null)
                canvasGroup.alpha = 0f;
            if (visualRoot != null)
            {
                visualRoot.anchoredPosition = _restingPosition + Vector2.right * enterOffset;
                visualRoot.localScale = new Vector3(0.94f, 0.94f, 1f);
            }

            Sequence sequence = DOTween.Sequence().SetUpdate(true);
            if (canvasGroup != null)
                sequence.Join(canvasGroup.DOFade(1f, enterDuration));
            if (visualRoot != null)
            {
                sequence.Join(visualRoot.DOAnchorPos(_restingPosition, enterDuration).SetEase(Ease.OutBack));
                sequence.Join(visualRoot.DOScale(1f, enterDuration).SetEase(Ease.OutQuad));
            }
            _activeSequence = sequence;
        }

        private void PlayCounterAndScheduleHide(bool appended)
        {
            _counterTween?.Kill();
            _counterTween = DOTween.To(
                    () => _displayedAmount,
                    value =>
                    {
                        _displayedAmount = value;
                        SetText(rewardField, _accumulatedAmount > 0 ? $"+{value}" : noRewardText);
                    },
                    _accumulatedAmount,
                    countDuration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true);

            Sequence hideSequence;
            if (appended || _activeSequence == null)
            {
                _activeSequence?.Kill(false);
                hideSequence = DOTween.Sequence().SetUpdate(true);
            }
            else
            {
                hideSequence = _activeSequence;
            }
            hideSequence.AppendInterval(visibleDuration);
            if (canvasGroup != null)
                hideSequence.Append(canvasGroup.DOFade(0f, exitDuration));
            if (visualRoot != null)
                hideSequence.Join(visualRoot.DOAnchorPos(_restingPosition + Vector2.right * enterOffset * 0.45f, exitDuration).SetEase(Ease.InQuad));
            hideSequence.OnComplete(HideImmediate);
            _activeSequence = hideSequence;

            SetText(balanceField, $"{balancePrefix} {_targetBalance}");
        }

        private void BindCurrentBalance()
        {
            if (rewardWallet != null && _isVisible == false)
                SetText(balanceField, $"{balancePrefix} {rewardWallet.Balance}");
        }

        private string BuildTitleText(CookingRewardGrant grant, bool appended)
        {
            if (appended)
                return "보상 추가 획득";

            switch (grant.Result)
            {
                case NpcConversationResult.Perfect:
                    return "완벽한 접대";
                case NpcConversationResult.Correct:
                    return "주문 만족";
                case NpcConversationResult.Similar:
                    return "흥미로운 요리";
                default:
                    return titleText;
            }
        }

        private Color ResolvePositiveColor()
        {
            return presentationSettings != null ? presentationSettings.PositiveColor : positiveColor;
        }

        private void EnsureReferences()
        {
            if (gamePanel == null)
                gamePanel = GetComponentInParent<CookingGamePanel>();
            if (rewardWallet == null && gamePanel != null)
                rewardWallet = gamePanel.RewardWallet;
            if (rewardWallet == null)
                rewardWallet = GetComponentInParent<CookingRewardWallet>();
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
            if (visualRoot == null)
                visualRoot = transform as RectTransform;
        }

        private void CaptureRestingPosition()
        {
            if (visualRoot != null && _isVisible == false)
                _restingPosition = visualRoot.anchoredPosition;
        }

        private void SubscribePanelEvents()
        {
            if (_subscribedPanel == gamePanel)
                return;

            UnsubscribePanelEvents();
            if (gamePanel == null)
                return;

            Bus<CookingRewardGrantedEvent>.Events += HandleRewardGranted;
            _subscribedPanel = gamePanel;
        }

        private void UnsubscribePanelEvents()
        {
            if (_subscribedPanel == null)
                return;

            Bus<CookingRewardGrantedEvent>.Events -= HandleRewardGranted;
            _subscribedPanel = null;
        }

        private void HandleRewardGranted(CookingRewardGrantedEvent gameEvent)
        {
            if (gameEvent.Source == gamePanel)
                Show(gameEvent.Grant);
        }

        private void KillAnimations()
        {
            _activeSequence?.Kill(false);
            _activeSequence = null;
            _counterTween?.Kill(false);
            _counterTween = null;
        }

        private void HideImmediate()
        {
            KillAnimations();
            _isVisible = false;
            _accumulatedAmount = 0;
            _displayedAmount = 0;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
            if (visualRoot != null)
            {
                visualRoot.anchoredPosition = _restingPosition;
                visualRoot.localScale = Vector3.one;
            }
        }

        private static void SetText(TextMeshProUGUI field, string text)
        {
            if (field != null)
                field.text = text ?? string.Empty;
        }
    }
}
