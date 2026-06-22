using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using Work.NPC.Code.Data;

namespace Work.Cook.Code.Runtime
{
    public sealed class CookingRewardToastView : MonoBehaviour, ICookingRewardView
    {
        [Header("Flow")]
        [SerializeField] private CookingGamePanel gamePanel;
        [SerializeField] private CookingRewardWallet rewardWallet;

        [Header("Layout References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TextMeshProUGUI titleField;
        [SerializeField] private TextMeshProUGUI rewardField;
        [SerializeField] private TextMeshProUGUI balanceField;

        [Header("Default Layout")]
        [SerializeField] private bool buildDefaultLayoutWhenMissing;
        [SerializeField] private TMP_FontAsset fontAsset;
        [SerializeField] private Color positiveColor = new Color(0.92f, 0.78f, 0.35f, 1f);
        [SerializeField] private Color emptyColor = new Color(0.72f, 0.68f, 0.60f, 1f);
        [SerializeField, Min(0.1f)] private float visibleDuration = 3f;
        [SerializeField, Min(0.01f)] private float fadeDuration = 0.22f;

        [Header("Text")]
        [SerializeField] private string titleText = "보상 획득";
        [SerializeField] private string noRewardText = "보상 없음";
        [SerializeField] private string balancePrefix = "소지금";

        private CookingGamePanel _subscribedPanel;
        private CancellationTokenSource _hideCancellationTokenSource;

        private void Awake()
        {
            EnsureReferences();
            EnsureLayout();
            HideImmediate();
        }

        private void OnEnable()
        {
            EnsureReferences();
            EnsureLayout();
            SubscribePanelEvents();
            BindCurrentBalance();
        }

        private void OnDisable()
        {
            CancelHideRoutine();
            UnsubscribePanelEvents();
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

            EnsureLayout();

            if (isActiveAndEnabled)
            {
                SubscribePanelEvents();
                BindCurrentBalance();
            }
        }

        public void SetFontAsset(TMP_FontAsset value)
        {
            if (value == null)
                return;

            fontAsset = value;
            ApplyFontToExistingTexts();
        }

        public void Show(CookingRewardGrant grant)
        {
            EnsureReferences();
            EnsureLayout();

            if (grant == null)
                return;

            transform.SetAsLastSibling();

            SetText(titleField, BuildTitleText(grant));
            SetText(rewardField, grant.Amount > 0 ? $"+{grant.Amount}" : noRewardText);
            SetText(balanceField, $"{balancePrefix} {grant.BalanceAfter}");

            if (rewardField != null)
                rewardField.color = grant.Amount > 0 ? positiveColor : emptyColor;

            CancelHideRoutine();
            CancellationTokenSource hideCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            _hideCancellationTokenSource = hideCancellationTokenSource;
            ShowAsync(hideCancellationTokenSource).Forget();
        }

        private async UniTaskVoid ShowAsync(CancellationTokenSource hideCancellationTokenSource)
        {
            CancellationToken cancellationToken = hideCancellationTokenSource.Token;
            try
            {
                SetAlpha(1f);
                int visibleMilliseconds = Mathf.Max(1, Mathf.RoundToInt(visibleDuration * 1000f));
                await UniTask.Delay(visibleMilliseconds, cancellationToken: cancellationToken);

                float elapsed = 0f;
                while (elapsed < fadeDuration)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / fadeDuration);
                    SetAlpha(1f - t);
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                }

                HideImmediate();
            }
            catch (OperationCanceledException)
            {
                // 비활성화 또는 새 토스트 표시로 인한 정상 취소
            }
            finally
            {
                if (_hideCancellationTokenSource == hideCancellationTokenSource)
                {
                    _hideCancellationTokenSource = null;
                }

                hideCancellationTokenSource.Dispose();
            }
        }

        private void BindCurrentBalance()
        {
            if (rewardWallet == null || canvasGroup == null || canvasGroup.alpha > 0f)
                return;

            SetText(balanceField, $"{balancePrefix} {rewardWallet.Balance}");
        }

        private string BuildTitleText(CookingRewardGrant grant)
        {
            if (grant == null)
                return titleText;

            switch (grant.Result)
            {
                case NpcConversationResult.Perfect:
                    return "완벽한 접대";
                case NpcConversationResult.Correct:
                    return "요청 충족";
                case NpcConversationResult.Similar:
                    return "비슷한 요리";
                case NpcConversationResult.Disgusting:
                case NpcConversationResult.Wrong:
                default:
                    return titleText;
            }
        }

        private void EnsureReferences()
        {
            if (gamePanel == null)
                gamePanel = GetComponentInParent<CookingGamePanel>();

            if (rewardWallet == null && gamePanel != null)
                rewardWallet = gamePanel.RewardWallet;

            if (rewardWallet == null)
                rewardWallet = GetComponentInParent<CookingRewardWallet>();
        }

        private void EnsureLayout()
        {
            if (canvasGroup != null
                && titleField != null
                && rewardField != null
                && balanceField != null)
            {
                return;
            }

            if (buildDefaultLayoutWhenMissing == true)
            {
                Debug.LogWarning("CookingRewardToastView no longer builds default layout. Assign text and CanvasGroup references in the inspector.", this);
            }
        }

        private void SubscribePanelEvents()
        {
            if (_subscribedPanel == gamePanel)
                return;

            UnsubscribePanelEvents();

            if (gamePanel == null)
                return;

            gamePanel.RewardGranted += HandleRewardGranted;
            _subscribedPanel = gamePanel;
        }

        private void UnsubscribePanelEvents()
        {
            if (_subscribedPanel == null)
                return;

            _subscribedPanel.RewardGranted -= HandleRewardGranted;
            _subscribedPanel = null;
        }

        private void HandleRewardGranted(CookingRewardGrant grant)
        {
            Show(grant);
        }

        private void HideImmediate()
        {
            SetAlpha(0f);
        }

        private void CancelHideRoutine()
        {
            if (_hideCancellationTokenSource == null)
            {
                return;
            }

            _hideCancellationTokenSource.Cancel();
            _hideCancellationTokenSource = null;
        }

        private void SetAlpha(float value)
        {
            if (canvasGroup == null)
                return;

            canvasGroup.alpha = Mathf.Clamp01(value);
        }

        private void ApplyFontToExistingTexts()
        {
            if (fontAsset == null)
                return;

            TextMeshProUGUI[] labels = GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < labels.Length; i++)
            {
                if (labels[i] != null)
                    labels[i].font = fontAsset;
            }
        }

        private static void SetText(TextMeshProUGUI field, string text)
        {
            if (field != null)
                field.text = text;
        }
    }
}
