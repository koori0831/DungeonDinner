using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using Work.Cook.Code.Data;
using Work.Cook.Code.Runtime.Core;
using Work.Cook.Code.Runtime.Events;
using Work.Cook.Code.Runtime.Systems;
using Work.Core.EventBus;

namespace Work.Cook.Code.Runtime.UI
{
    /// <summary>
    /// 조리대 중심 카드 선택형 조리 뷰
    /// </summary>
    public sealed class CookingView : MonoBehaviour, ICookingPreparationView
    {
        [Header("Flow")]
        [SerializeField] private CookingGamePanel gamePanel;
        [SerializeField] private CookingFlowRunner flowRunner;
        [SerializeField] private CookingKnowledgeStore knowledgeStore;

        [Header("Sub Views")]
        [SerializeField] private CookingWorkbenchView workbenchView;
        [SerializeField] private CookingPreparationHandView handView;
        [SerializeField] private CookingActivePreparationSlotView activeSlotView;
        [SerializeField] private CookingMiniGameOverlayHost miniGameOverlayHost;
        [SerializeField] private CookingViewTransition transition;
        [SerializeField] private CookingUiPresentationSettingsSO presentationSettings;

        [Header("Text")]
        [SerializeField] private TMP_FontAsset fontAsset;

        private IngredientSO _currentIngredient;
        private IngredientSO _boundIngredient;
        private IngredientPreparationOption _committedOption;
        private bool _hasBuiltCards;
        private bool _isInteractionPending;
        private bool _isCompletingCooking;
        private int _observedPreparedCount;
        private CancellationTokenSource _completionDisplayCancellation;

        public CookingViewState State { get; private set; } = CookingViewState.None;
        public CookingUiPresentationSettingsSO PresentationSettings => presentationSettings;

        private void Awake()
        {
            EnsureReferences();
        }

        private void OnEnable()
        {
            EnsureReferences();
            SubscribeSources();
            if (TryShowRecentlyCompletedPreparation() == false)
                Refresh();
        }

        private void OnDisable()
        {
            CancelCompletionDisplay();
            UnsubscribeSources();
            _isInteractionPending = false;
            _boundIngredient = null;
            _hasBuiltCards = false;
        }

        public void Initialize(CookingGamePanel owner, CookingFlowRunner runner, TMP_FontAsset defaultFontAsset = null)
        {
            gamePanel = owner;
            flowRunner = runner;
            knowledgeStore = owner != null ? owner.KnowledgeStore : knowledgeStore;

            if (defaultFontAsset != null)
                SetFontAsset(defaultFontAsset);

            EnsureReferences();

            if (isActiveAndEnabled == true)
            {
                SubscribeSources();
                Refresh();
            }
        }

        public void SetFontAsset(TMP_FontAsset value)
        {
            if (value == null)
                return;

            fontAsset = value;
            workbenchView?.SetFontAsset(value);
            handView?.SetFontAsset(value);
            activeSlotView?.SetFontAsset(value);
        }

        public void Refresh()
        {
            EnsureReferences();

            if (flowRunner == null)
            {
                State = CookingViewState.None;
                return;
            }

            ObservePreparedCountReset();

            if (_isInteractionPending == true)
                return;

            IngredientSO ingredient = flowRunner.GetNextUnpreparedIngredient();
            if (ingredient == null)
            {
                State = CookingViewState.CompleteCooking;
                if (gamePanel == null)
                    CompleteCookingOnce();

                return;
            }

            BindIngredient(ingredient);
        }

        private void BindIngredient(IngredientSO ingredient)
        {
            bool shouldRebuildCards = _boundIngredient != ingredient
                                      || _hasBuiltCards == false
                                      || handView == null
                                      || handView.CardCount == 0;
            _currentIngredient = ingredient;
            _boundIngredient = ingredient;
            _committedOption = null;
            _isInteractionPending = false;
            State = CookingViewState.CardSelect;

            ObservePreparedCountReset();
            workbenchView?.BindIngredient(ingredient);
            activeSlotView?.Clear();

            if (shouldRebuildCards == false)
            {
                handView?.SetInteractable(true);
                EnsureCardHandBuiltAfterLayoutAsync(ingredient).Forget();
                return;
            }

            RebuildCardHand(ingredient);
            EnsureCardHandBuiltAfterLayoutAsync(ingredient).Forget();
        }

        private void RebuildCardHand(IngredientSO ingredient)
        {
            if (ingredient == null || flowRunner == null || handView == null)
                return;

            IReadOnlyList<IngredientPreparationOption> options = flowRunner.GetPreparationOptions(ingredient);
            handView.Initialize(gamePanel, knowledgeStore, fontAsset, presentationSettings);
            handView.Rebuild(ingredient, options, HandleCardSelected);
            _hasBuiltCards = handView.CardCount > 0;
        }

        private async UniTaskVoid EnsureCardHandBuiltAfterLayoutAsync(IngredientSO ingredient)
        {
            try
            {
                await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, this.GetCancellationTokenOnDestroy());
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (isActiveAndEnabled == false
                || State != CookingViewState.CardSelect
                || ingredient == null
                || _currentIngredient != ingredient
                || handView == null
                || handView.CardCount > 0)
            {
                return;
            }

            RebuildCardHand(ingredient);
        }

        private void HandleCardSelected(IngredientSO ingredient, IngredientPreparationOption option)
        {
            if (_isInteractionPending == true)
                return;

            if (ingredient == null)
                return;

            _currentIngredient = ingredient;
            _committedOption = option;
            _isInteractionPending = true;
            State = CookingViewState.CardCommit;

            handView?.SetInteractable(false);
            activeSlotView?.Bind(option);
            workbenchView?.BeginInteraction(ingredient, option, CompleteCommittedPreparation);
        }

        private void CompleteCommittedPreparation()
        {
            if (_isInteractionPending == false)
                return;

            IngredientSO ingredient = _currentIngredient;
            IngredientPreparationOption option = _committedOption;
            _isInteractionPending = false;
            State = CookingViewState.IngredientInteraction;

            workbenchView?.ShowInteractionStarted(ingredient, option);
            activeSlotView?.BindInProgress(option);
            handView?.ShowMiniGameState();

            if (gamePanel != null)
            {
                Bus<CookingPreparationInteractionCompleteRequestedEvent>.Raise(
                    new CookingPreparationInteractionCompleteRequestedEvent(gamePanel, ingredient, option, null));
                return;
            }

            if (flowRunner != null)
                flowRunner.SelectPreparation(ingredient, option, null);
        }

        private void CompleteCookingOnce()
        {
            if (_isCompletingCooking == true)
                return;

            _isCompletingCooking = true;
            State = CookingViewState.CompleteCooking;
            if (gamePanel != null)
                Bus<CookingCompleteRequestedEvent>.Raise(new CookingCompleteRequestedEvent(gamePanel));
            _isCompletingCooking = false;
        }

        private bool TryShowRecentlyCompletedPreparation()
        {
            if (flowRunner == null || _committedOption == null)
                return false;

            CookingSession session = flowRunner.Controller.CurrentSession;
            int preparedCount = session?.PreparedIngredients?.Count ?? 0;
            if (preparedCount <= _observedPreparedCount)
                return false;

            _observedPreparedCount = preparedCount;
            PreparedIngredientState prepared = session.PreparedIngredients[preparedCount - 1];
            activeSlotView?.BindResult(_committedOption, prepared);
            handView?.ShowResultState();
            ObservePreparedCountReset();

            CancelCompletionDisplay();
            _completionDisplayCancellation = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            CompleteResultDisplayAsync(_completionDisplayCancellation).Forget();
            return true;
        }

        private async UniTask CompleteResultDisplayAsync(CancellationTokenSource source)
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(0.5f), true, cancellationToken: source.Token);
                _committedOption = null;
                Refresh();
            }
            catch (OperationCanceledException)
            {
                return;
            }
            finally
            {
                if (_completionDisplayCancellation == source)
                {
                    source.Dispose();
                    _completionDisplayCancellation = null;
                }
            }
        }

        private void CancelCompletionDisplay()
        {
            if (_completionDisplayCancellation == null)
                return;

            _completionDisplayCancellation.Cancel();
            _completionDisplayCancellation.Dispose();
            _completionDisplayCancellation = null;
        }

        private void ObservePreparedCountReset()
        {
            CookingSession session = flowRunner?.Controller?.CurrentSession;
            if (session == null)
                return;

            if (_committedOption == null && session.PreparedIngredients.Count < _observedPreparedCount)
                _observedPreparedCount = session.PreparedIngredients.Count;
        }

        private void EnsureReferences()
        {
            if (gamePanel == null)
                gamePanel = GetComponentInParent<CookingGamePanel>();
            if (flowRunner == null)
                flowRunner = gamePanel != null ? gamePanel.FlowRunner : GetComponentInParent<CookingFlowRunner>();
            if (knowledgeStore == null && gamePanel != null)
                knowledgeStore = gamePanel.KnowledgeStore;
            if (workbenchView == null)
                workbenchView = GetComponentInChildren<CookingWorkbenchView>(true);
            if (handView == null)
                handView = GetComponentInChildren<CookingPreparationHandView>(true);
            if (activeSlotView == null)
                activeSlotView = GetComponentInChildren<CookingActivePreparationSlotView>(true);
            if (miniGameOverlayHost == null && gamePanel != null)
                miniGameOverlayHost = gamePanel.GetComponentInChildren<CookingMiniGameOverlayHost>(true);
            if (transition == null)
                transition = GetComponentInChildren<CookingViewTransition>(true);
            activeSlotView?.SetPresentationSettings(presentationSettings);
        }

        private void SubscribeSources()
        {
            Bus<CookingFlowStateChangedEvent>.Events -= HandleFlowStateChanged;
            Bus<CookingFlowStateChangedEvent>.Events += HandleFlowStateChanged;
            if (miniGameOverlayHost != null)
            {
                miniGameOverlayHost.ResultShown -= HandleMiniGameResultShown;
                miniGameOverlayHost.ResultShown += HandleMiniGameResultShown;
            }
        }

        private void UnsubscribeSources()
        {
            Bus<CookingFlowStateChangedEvent>.Events -= HandleFlowStateChanged;
            if (miniGameOverlayHost != null)
                miniGameOverlayHost.ResultShown -= HandleMiniGameResultShown;
        }

        private void HandleFlowStateChanged(CookingFlowStateChangedEvent gameEvent)
        {
            if (gameEvent.Source != flowRunner)
                return;

            if (isActiveAndEnabled == true && TryShowRecentlyCompletedPreparation() == false)
                Refresh();
        }

        private void HandleMiniGameResultShown(CookingMiniGameResult result)
        {
            if (_committedOption == null)
                return;

            activeSlotView?.BindResultPreview(_committedOption, result);
            handView?.ShowResultState();
            workbenchView?.ShowInteractionResult(_currentIngredient, _committedOption);
        }

    }
}
