using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Work.Cook.Code.Data;
using Work.Cook.Code.Runtime.Core;
using Work.Cook.Code.Runtime.Events;
using Work.Cook.Code.Runtime.Systems;
using Work.Core.EventBus;
using Work.NPC.Code.Runtime;

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
            bool shouldRebuildCards = _boundIngredient != ingredient || _hasBuiltCards == false;
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
                return;
            }

            IReadOnlyList<IngredientPreparationOption> options = flowRunner.GetPreparationOptions(ingredient);
            handView?.Initialize(gamePanel, knowledgeStore, fontAsset, presentationSettings);
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
            State = CookingViewState.IngredientInteraction;

            workbenchView?.ShowInteractionStarted(ingredient, option);
            activeSlotView?.BindInProgress(option);

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
            handView?.SetInteractable(false);
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
            if (transition == null)
                transition = GetComponentInChildren<CookingViewTransition>(true);
            activeSlotView?.SetPresentationSettings(presentationSettings);
        }

        private void SubscribeSources()
        {
            Bus<CookingFlowStateChangedEvent>.Events -= HandleFlowStateChanged;
            Bus<CookingFlowStateChangedEvent>.Events += HandleFlowStateChanged;
        }

        private void UnsubscribeSources()
        {
            Bus<CookingFlowStateChangedEvent>.Events -= HandleFlowStateChanged;
        }

        private void HandleFlowStateChanged(CookingFlowStateChangedEvent gameEvent)
        {
            if (gameEvent.Source != flowRunner)
                return;

            if (isActiveAndEnabled == true && TryShowRecentlyCompletedPreparation() == false)
                Refresh();
        }

    }

    /// <summary>
    /// 씬에 배치된 팝업과 런타임 생성 UI가 동일한 UIAsset 스킨을 사용하도록 보정한다.
    /// 에디터 설치기를 다시 실행하지 않은 씬에서도 동일한 표현을 보장한다.
    /// </summary>
    internal static class CookingUiRuntimeSkinApplicator
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
            Apply(SceneManager.GetActiveScene());
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Apply(scene);
        }

        private static void Apply(Scene scene)
        {
            CookingView[] views = UnityEngine.Object.FindObjectsByType<CookingView>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            CookingView view = null;
            for (int i = 0; i < views.Length; i++)
            {
                if (views[i].gameObject.scene == scene)
                {
                    view = views[i];
                    break;
                }
            }

            CookingUiPresentationSettingsSO settings = view?.PresentationSettings;
            if (settings == null || settings.PanelSprite == null)
                return;

            HideLegacyHud(scene);
            ApplyImages(scene, settings);
            ConfigureOrderSlip(scene, settings);
            ConfigureLayerOrder(scene);
        }

        private static void ApplyImages(Scene scene, CookingUiPresentationSettingsSO settings)
        {
            Image[] images = UnityEngine.Object.FindObjectsByType<Image>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (image.gameObject.scene != scene)
                    continue;

                CookingWorkbenchView workbench = image.GetComponentInParent<CookingWorkbenchView>();
                if (workbench != null && image.gameObject.name == "IngredientButton")
                {
                    image.type = Image.Type.Simple;
                    image.preserveAspect = true;
                    continue;
                }

                CookingIngredientButtonView ingredientButton = image.GetComponent<CookingIngredientButtonView>();
                if (ingredientButton != null)
                {
                    SetSliced(image, settings.CardSprite);
                    SetDarkText(image.transform);
                    continue;
                }

                Button button = image.GetComponent<Button>();
                if (button != null)
                {
                    SetSliced(image, IsPrimaryButton(button.name)
                        ? settings.PrimaryButtonSprite
                        : settings.SecondaryButtonSprite);
                    SetButtonText(button, Color.white);
                    continue;
                }

                if (IsPanel(image.name))
                {
                    SetSliced(image, settings.PanelSprite);
                }
                else if (IsCard(image.name))
                {
                    SetSliced(image, settings.CardSprite);
                    SetDarkText(image.transform);
                }
                else if (IsLabel(image.name))
                {
                    SetSliced(image, settings.LabelSprite);
                    image.raycastTarget = false;
                }
            }
        }

        private static void ConfigureOrderSlip(Scene scene, CookingUiPresentationSettingsSO settings)
        {
            NpcOrderSlipPanel[] slips = UnityEngine.Object.FindObjectsByType<NpcOrderSlipPanel>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int i = 0; i < slips.Length; i++)
            {
                if (slips[i].gameObject.scene != scene)
                    continue;

                SetSliced(slips[i].GetComponent<Image>(), settings.ReceiptSprite);
                Graphic[] graphics = slips[i].GetComponentsInChildren<Graphic>(true);
                for (int graphicIndex = 0; graphicIndex < graphics.Length; graphicIndex++)
                    graphics[graphicIndex].raycastTarget = false;

                CanvasGroup group = slips[i].GetComponent<CanvasGroup>();
                if (group != null)
                {
                    group.interactable = false;
                    group.blocksRaycasts = false;
                }
            }
        }

        private static void ConfigureLayerOrder(Scene scene)
        {
            Canvas[] canvases = UnityEngine.Object.FindObjectsByType<Canvas>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            Canvas canvas = null;
            for (int i = 0; i < canvases.Length; i++)
            {
                if (canvases[i].gameObject.scene == scene && canvases[i].name == "CookUICanvas")
                {
                    canvas = canvases[i].rootCanvas;
                    break;
                }
            }

            if (canvas == null)
                return;

            Transform root = canvas.transform;
            Transform cooking = FindDescendant(root, "CookingViewRoot");
            Transform miniGame = FindDescendant(root, "CookingMiniGameOverlayRoot");
            Transform orderSlip = FindDescendant(root, "NpcOrderSlipPanel");
            Transform result = FindDescendant(root, "CookingResultPresentationRoot");
            Transform reward = FindDescendant(root, "CookingRewardToastRoot");

            if (orderSlip != null && orderSlip.parent != root)
                orderSlip.SetParent(root, false);

            cooking?.SetAsLastSibling();
            miniGame?.SetAsLastSibling();
            orderSlip?.SetAsLastSibling();
            result?.SetAsLastSibling();
            reward?.SetAsLastSibling();
        }

        private static void HideLegacyHud(Scene scene)
        {
            CookingOrderNoteView[] notes = UnityEngine.Object.FindObjectsByType<CookingOrderNoteView>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int i = 0; i < notes.Length; i++)
            {
                if (notes[i].gameObject.scene == scene)
                    notes[i].gameObject.SetActive(false);
            }

            CookingIngredientProgressView[] progressViews =
                UnityEngine.Object.FindObjectsByType<CookingIngredientProgressView>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            for (int i = 0; i < progressViews.Length; i++)
            {
                if (progressViews[i].gameObject.scene == scene)
                    progressViews[i].gameObject.SetActive(false);
            }
        }

        private static Transform FindDescendant(Transform root, string name)
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i].name == name)
                    return transforms[i];
            }

            return null;
        }

        private static bool IsPanel(string name)
        {
            return name == "TemporaryIngredientSelectionView"
                   || name == "Workbench"
                   || name == "ActivePreparationSlot"
                   || name == "PreparationHand"
                   || name == "PreparationTooltip"
                   || name == "DishHeroPanel"
                   || name == "ResultSummaryPanel"
                   || name == "ResultDetailsDrawer"
                   || name == "CookingRewardToastRoot"
                   || name == "InfoDictionaryPanel"
                   || name == "InfoRecipePanel"
                   || name == "RecipeDictionaryPanel"
                   || name == "IngredientDetail"
                   || name == "Information"
                   || name == "ChatPanel";
        }

        private static bool IsCard(string name)
        {
            return name.EndsWith("Card", StringComparison.Ordinal)
                   || name == "RewardPreview"
                   || name == "ExpectedReaction"
                   || name == "PreparedIngredientRowTemplate"
                   || name == "TagComparisonSection"
                   || name == "PreparationBreakdown";
        }

        private static bool IsLabel(string name)
        {
            return name.EndsWith("TitleLabel", StringComparison.Ordinal)
                   || name == "CookingTitleLabel"
                   || name == "SlotTitleLabel"
                   || name == "ResultDetailsTitleLabel";
        }

        private static bool IsPrimaryButton(string name)
        {
            return name.IndexOf("Confirm", StringComparison.OrdinalIgnoreCase) >= 0
                   || name.IndexOf("Complete", StringComparison.OrdinalIgnoreCase) >= 0
                   || name.IndexOf("HandToNpc", StringComparison.OrdinalIgnoreCase) >= 0
                   || name.IndexOf("Submit", StringComparison.OrdinalIgnoreCase) >= 0
                   || name.IndexOf("Start", StringComparison.OrdinalIgnoreCase) >= 0
                   || name.IndexOf("Next", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void SetSliced(Image image, Sprite sprite)
        {
            if (image == null || sprite == null)
                return;

            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.preserveAspect = false;
            image.color = Color.white;
            image.pixelsPerUnitMultiplier = 1f;
        }

        private static void SetDarkText(Transform root)
        {
            TextMeshProUGUI[] fields = root.GetComponentsInChildren<TextMeshProUGUI>(true);
            Color color = new Color(0.24f, 0.18f, 0.15f, 1f);
            for (int i = 0; i < fields.Length; i++)
                fields[i].color = color;
        }

        private static void SetButtonText(Button button, Color color)
        {
            TextMeshProUGUI[] fields = button.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < fields.Length; i++)
                fields[i].color = color;
        }
    }
}
