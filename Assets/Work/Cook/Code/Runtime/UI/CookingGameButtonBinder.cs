using UnityEngine;
using UnityEngine.UI;
using Work.Cook.Code.Data;
using Work.Cook.Code.Runtime.Core;
using Work.Cook.Code.Runtime.Events;
using Work.Cook.Code.Runtime.Integration;
using Work.Cook.Code.Runtime.Systems;
using Work.Cook.Code.Runtime.UI;
using Work.Core.EventBus;

namespace Work.Cook.Code.Runtime.UI
{
    public sealed class CookingGameButtonBinder : MonoBehaviour
    {
        [SerializeField] private CookingGamePanel gamePanel;
        [SerializeField] private Button button;
        [SerializeField] private bool findGamePanelOnEnable = true;
        [SerializeField] private bool bindButtonOnEnable = true;
        [SerializeField] private bool refreshInteractableFromSnapshot = true;
        [SerializeField] private CookingGameButtonAction action;

        [Header("Action Data")]
        [SerializeField] private RecipeSO recipe;
        [SerializeField] private IngredientSO ingredient;
        [SerializeField] private IngredientPreparationOption preparationOption;
        [SerializeField, Min(0)] private int preparationOptionIndex;

        private CookingGamePanel _subscribedPanel;

        private void Reset()
        {
            button = GetComponent<Button>();
            gamePanel = GetComponentInParent<CookingGamePanel>();
        }

        private void Awake()
        {
            EnsureReferences();
            BindButton();
        }

        private void OnEnable()
        {
            EnsureReferences();

            if (bindButtonOnEnable == true)
                BindButton();

            SubscribePanel();
            RefreshInteractable();
        }

        private void OnDisable()
        {
            UnsubscribePanel();
        }

        public void SetGamePanel(CookingGamePanel value)
        {
            if (gamePanel == value)
                return;

            UnsubscribePanel();
            gamePanel = value;

            if (isActiveAndEnabled == true)
                SubscribePanel();

            RefreshInteractable();
        }

        public void SetRecipe(RecipeSO value)
        {
            recipe = value;
            RefreshInteractable();
        }

        public void SetIngredient(IngredientSO value)
        {
            ingredient = value;
            RefreshInteractable();
        }

        public void SetPreparationOption(IngredientPreparationOption value)
        {
            preparationOption = value;
            RefreshInteractable();
        }

        public void SetPreparationOptionIndex(int value)
        {
            preparationOptionIndex = Mathf.Max(0, value);
            RefreshInteractable();
        }

        public void InvokeAction()
        {
            EnsureReferences();

            if (gamePanel == null)
                return;

            switch (action)
            {
                case CookingGameButtonAction.None:
                    break;
                case CookingGameButtonAction.OpenRecipeSelection:
                    Bus<CookingRecipeSelectionOpenRequestedEvent>.Raise(new CookingRecipeSelectionOpenRequestedEvent(gamePanel));
                    break;
                case CookingGameButtonAction.OpenDirectIngredientSelection:
                    Bus<CookingDirectIngredientSelectionOpenRequestedEvent>.Raise(new CookingDirectIngredientSelectionOpenRequestedEvent(gamePanel));
                    break;
                case CookingGameButtonAction.ConfirmIngredientSelection:
                    Bus<CookingIngredientSelectionConfirmRequestedEvent>.Raise(new CookingIngredientSelectionConfirmRequestedEvent(gamePanel));
                    break;
                case CookingGameButtonAction.ClearIngredientSelection:
                    Bus<CookingIngredientSelectionClearRequestedEvent>.Raise(new CookingIngredientSelectionClearRequestedEvent(gamePanel));
                    break;
                case CookingGameButtonAction.ConfirmRecipe:
                    Bus<CookingRecipeConfirmRequestedEvent>.Raise(new CookingRecipeConfirmRequestedEvent(gamePanel, recipe));
                    break;
                case CookingGameButtonAction.ToggleIngredientSelection:
                    Bus<CookingIngredientSelectionToggleRequestedEvent>.Raise(new CookingIngredientSelectionToggleRequestedEvent(gamePanel, ingredient));
                    break;
                case CookingGameButtonAction.RemoveIngredientSelection:
                    Bus<CookingIngredientSelectionRemoveRequestedEvent>.Raise(new CookingIngredientSelectionRemoveRequestedEvent(gamePanel, ingredient));
                    break;
                case CookingGameButtonAction.SelectCurrentPreparationByIndex:
                    Bus<CookingPreparationSelectCurrentByIndexRequestedEvent>.Raise(new CookingPreparationSelectCurrentByIndexRequestedEvent(gamePanel, preparationOptionIndex));
                    break;
                case CookingGameButtonAction.SelectCurrentPreparation:
                    Bus<CookingPreparationSelectCurrentRequestedEvent>.Raise(new CookingPreparationSelectCurrentRequestedEvent(gamePanel, preparationOption));
                    break;
                case CookingGameButtonAction.SelectPreparation:
                    Bus<CookingPreparationSelectRequestedEvent>.Raise(new CookingPreparationSelectRequestedEvent(gamePanel, ingredient, preparationOption));
                    break;
                case CookingGameButtonAction.CompleteCooking:
                    Bus<CookingCompleteRequestedEvent>.Raise(new CookingCompleteRequestedEvent(gamePanel));
                    break;
                case CookingGameButtonAction.HandResultToNpc:
                    Bus<CookingDishHandToNpcRequestedEvent>.Raise(new CookingDishHandToNpcRequestedEvent(gamePanel));
                    break;
                case CookingGameButtonAction.ReturnToNpcConversation:
                    Bus<CookingNpcConversationReturnRequestedEvent>.Raise(new CookingNpcConversationReturnRequestedEvent(gamePanel));
                    break;
                case CookingGameButtonAction.CloseCookingViews:
                    Bus<CookingViewsCloseRequestedEvent>.Raise(new CookingViewsCloseRequestedEvent(gamePanel));
                    break;
                case CookingGameButtonAction.RefreshCookingViews:
                    Bus<CookingViewsRefreshRequestedEvent>.Raise(new CookingViewsRefreshRequestedEvent(gamePanel));
                    break;
            }
        }

        public void RefreshInteractable()
        {
            if (button == null || refreshInteractableFromSnapshot == false)
                return;

            CookingGameSnapshot snapshot = gamePanel != null ? gamePanel.CurrentSnapshot : null;
            button.interactable = CanInvoke(snapshot);
        }

        private bool CanInvoke(CookingGameSnapshot snapshot)
        {
            if (gamePanel == null)
                return false;

            switch (action)
            {
                case CookingGameButtonAction.None:
                    return false;
                case CookingGameButtonAction.ConfirmIngredientSelection:
                    return snapshot != null && snapshot.HasSelectedIngredients;
                case CookingGameButtonAction.ConfirmRecipe:
                    return recipe != null;
                case CookingGameButtonAction.ToggleIngredientSelection:
                case CookingGameButtonAction.RemoveIngredientSelection:
                    return ingredient != null;
                case CookingGameButtonAction.SelectCurrentPreparationByIndex:
                case CookingGameButtonAction.SelectCurrentPreparation:
                    return snapshot != null && snapshot.HasCurrentIngredient;
                case CookingGameButtonAction.SelectPreparation:
                    return ingredient != null;
                case CookingGameButtonAction.CompleteCooking:
                    return snapshot != null && snapshot.IsEveryIngredientPrepared;
                case CookingGameButtonAction.HandResultToNpc:
                    return snapshot != null && snapshot.CanHandResultToNpc;
                default:
                    return true;
            }
        }

        private void EnsureReferences()
        {
            if (button == null)
                button = GetComponent<Button>();

            if (gamePanel != null || findGamePanelOnEnable == false)
                return;

            gamePanel = GetComponentInParent<CookingGamePanel>();
            if (gamePanel == null)
                gamePanel = FindFirstObjectByType<CookingGamePanel>();
        }

        private void BindButton()
        {
            if (button == null)
                return;

            button.onClick.RemoveListener(InvokeAction);
            button.onClick.AddListener(InvokeAction);
        }

        private void SubscribePanel()
        {
            if (_subscribedPanel == gamePanel)
                return;

            UnsubscribePanel();

            if (gamePanel == null)
                return;

            Bus<CookingGameSnapshotChangedEvent>.Events += HandleSnapshotChanged;
            _subscribedPanel = gamePanel;
        }

        private void UnsubscribePanel()
        {
            if (_subscribedPanel == null)
                return;

            Bus<CookingGameSnapshotChangedEvent>.Events -= HandleSnapshotChanged;
            _subscribedPanel = null;
        }

        private void HandleSnapshotChanged(CookingGameSnapshotChangedEvent gameEvent)
        {
            if (gameEvent.Source != gamePanel)
                return;

            RefreshInteractable();
        }
    }

    public enum CookingGameButtonAction
    {
        None,
        OpenRecipeSelection,
        OpenDirectIngredientSelection,
        ConfirmIngredientSelection,
        ClearIngredientSelection,
        ConfirmRecipe,
        ToggleIngredientSelection,
        RemoveIngredientSelection,
        SelectCurrentPreparationByIndex,
        SelectCurrentPreparation,
        SelectPreparation,
        CompleteCooking,
        HandResultToNpc,
        ReturnToNpcConversation,
        CloseCookingViews,
        RefreshCookingViews
    }
}
