using System.Collections.Generic;
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
        [SerializeField] private CookingOrderNoteView orderNoteView;
        [SerializeField] private CookingViewTransition transition;

        [Header("Text")]
        [SerializeField] private TextMeshProUGUI progressField;
        [SerializeField] private TMP_FontAsset fontAsset;

        private CookingGamePanel _subscribedPanel;
        private IngredientSO _currentIngredient;
        private IngredientSO _boundIngredient;
        private IngredientPreparationOption _committedOption;
        private bool _hasBuiltCards;
        private bool _isInteractionPending;
        private bool _isCompletingCooking;

        public CookingViewState State { get; private set; } = CookingViewState.None;

        private void Awake()
        {
            EnsureReferences();
        }

        private void OnEnable()
        {
            EnsureReferences();
            SubscribeSources();
            Refresh();
        }

        private void OnDisable()
        {
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
            if (progressField != null)
                progressField.font = value;
            workbenchView?.SetFontAsset(value);
            handView?.SetFontAsset(value);
            activeSlotView?.SetFontAsset(value);
            orderNoteView?.SetFontAsset(value);
        }

        public void Refresh()
        {
            EnsureReferences();

            if (flowRunner == null)
            {
                State = CookingViewState.None;
                SetText(progressField, "조리 데이터 없음");
                return;
            }

            if (_isInteractionPending == true)
            {
                orderNoteView?.Refresh(gamePanel);
                return;
            }

            IngredientSO ingredient = flowRunner.GetNextUnpreparedIngredient();
            if (ingredient == null)
            {
                State = CookingViewState.CompleteCooking;
                SetText(progressField, "조리 완료 처리 중");
                if (gamePanel == null)
                    CompleteCookingOnce();

                return;
            }

            BindIngredient(ingredient);
        }

        private void BindIngredient(IngredientSO ingredient)
        {
            bool shouldRebuildCards = _boundIngredient != ingredient || _hasBuiltCards == false;
            _currentIngredient = ingredient;
            _boundIngredient = ingredient;
            _committedOption = null;
            _isInteractionPending = false;
            State = CookingViewState.CardSelect;

            SetText(progressField, BuildProgressText());
            workbenchView?.BindIngredient(ingredient);
            activeSlotView?.Clear();
            orderNoteView?.Refresh(gamePanel);

            if (shouldRebuildCards == false)
            {
                handView?.SetInteractable(true);
                return;
            }

            IReadOnlyList<IngredientPreparationOption> options = flowRunner.GetPreparationOptions(ingredient);
            handView?.Initialize(gamePanel, knowledgeStore, fontAsset);
            handView?.Rebuild(ingredient, options, HandleCardSelected);
            _hasBuiltCards = true;
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
            State = CookingViewState.InteractionResult;

            workbenchView?.ShowInteractionResult(ingredient, option);
            activeSlotView?.BindResult(option);

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

        private string BuildProgressText()
        {
            if (flowRunner == null)
                return string.Empty;

            CookingSession session = flowRunner.Controller.CurrentSession;
            if (session == null || session.SelectedIngredients.Count == 0)
                return string.Empty;

            int preparedCount = session.PreparedIngredients.Count;
            return $"조리 진행 {preparedCount} / {session.SelectedIngredients.Count}";
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
            if (orderNoteView == null)
                orderNoteView = GetComponentInChildren<CookingOrderNoteView>(true);
            if (transition == null)
                transition = GetComponentInChildren<CookingViewTransition>(true);
        }

        private void SubscribeSources()
        {
            if (_subscribedPanel != gamePanel)
            {
                if (_subscribedPanel != null)
                    Bus<CookingGameSnapshotChangedEvent>.Events -= HandleSnapshotChanged;

                _subscribedPanel = gamePanel;
                if (_subscribedPanel != null)
                    Bus<CookingGameSnapshotChangedEvent>.Events += HandleSnapshotChanged;
            }

            Bus<CookingFlowStateChangedEvent>.Events -= HandleFlowStateChanged;
            Bus<CookingFlowStateChangedEvent>.Events += HandleFlowStateChanged;
        }

        private void UnsubscribeSources()
        {
            if (_subscribedPanel != null)
                Bus<CookingGameSnapshotChangedEvent>.Events -= HandleSnapshotChanged;
            Bus<CookingFlowStateChangedEvent>.Events -= HandleFlowStateChanged;

            _subscribedPanel = null;
        }

        private void HandleSnapshotChanged(CookingGameSnapshotChangedEvent gameEvent)
        {
            if (gameEvent.Source != gamePanel)
                return;

            if (isActiveAndEnabled == false)
                return;

            orderNoteView?.Refresh(gamePanel);
        }

        private void HandleFlowStateChanged(CookingFlowStateChangedEvent gameEvent)
        {
            if (gameEvent.Source != flowRunner)
                return;

            if (isActiveAndEnabled == true)
                Refresh();
        }

        private static void SetText(TextMeshProUGUI field, string text)
        {
            if (field != null)
                field.text = text ?? string.Empty;
        }
    }
}
