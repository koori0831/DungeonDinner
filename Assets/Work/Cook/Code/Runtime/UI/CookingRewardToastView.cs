using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using Work.NPC.Code.Data;
using Work.Cook.Code.Runtime.Core;
using Work.Cook.Code.Runtime.Events;
using Work.Cook.Code.Runtime.Integration;
using Work.Cook.Code.Runtime.Systems;
using Work.Cook.Code.Runtime.UI;
using Work.Core.EventBus;

namespace Work.Cook.Code.Runtime.UI
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

        [Header("View Settings")]
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

            if (isActiveAndEnabled == true)
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

            SetText(titleField, BuildTitleText(grant));
            SetText(rewardField, grant.Amount > 0 ? $"+{grant.Amount}" : noRewardText);
            SetText(balanceField, $"{balancePrefix} {grant.BalanceAfter}");

            if (rewardField != null)
                rewardField.color = grant.Amount > 0 ? positiveColor : emptyColor;

            CancelHideRoutine();

            _hideCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            ShowRoutineAsync(_hideCancellationTokenSource).Forget();
        }

        private async UniTask ShowRoutineAsync(CancellationTokenSource cancellationTokenSource)
        {
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            try
            {
                SetAlpha(1f);
                await UniTask.Delay(TimeSpan.FromSeconds(visibleDuration), cancellationToken: cancellationToken);

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
                return;
            }
            finally
            {
                if (_hideCancellationTokenSource == cancellationTokenSource)
                {
                    _hideCancellationTokenSource.Dispose();
                    _hideCancellationTokenSource = null;
                }
            }
        }

        private void CancelHideRoutine()
        {
            if (_hideCancellationTokenSource == null)
                return;

            _hideCancellationTokenSource.Cancel();
            _hideCancellationTokenSource.Dispose();
            _hideCancellationTokenSource = null;
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

            Debug.LogError("CookingRewardToastView is missing canvasGroup/titleField/rewardField/balanceField references. Assign a prefab/inspector based toast.", this);
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
            if (gameEvent.Source != gamePanel)
                return;

            Show(gameEvent.Grant);
        }

        private void HideImmediate()
        {
            SetAlpha(0f);
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
