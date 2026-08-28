using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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
    /// 완성 요리를 단계적으로 공개하고 요약/상세 판정을 표시한다.
    /// </summary>
    public sealed class CookingResultView : MonoBehaviour, ICookingResultView, IPointerDownHandler
    {
        [Header("Flow")]
        [SerializeField] private CookingGamePanel gamePanel;
        [SerializeField] private CookingFlowRunner flowRunner;

        [Header("Presentation")]
        [SerializeField] private CookingUiPresentationSettingsSO presentationSettings;
        [SerializeField] private TMP_FontAsset fontAsset;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private CanvasGroup backdropGroup;
        [SerializeField] private Image revealInputBlocker;

        [Header("Dish Hero")]
        [SerializeField] private RectTransform dishVisualRoot;
        [SerializeField] private Image dishIconImage;
        [SerializeField] private TextMeshProUGUI dishNameField;
        [SerializeField] private TextMeshProUGUI recipeField;
        [SerializeField] private TextMeshProUGUI representativeTagsField;

        [Header("Quality")]
        [SerializeField] private CanvasGroup qualityGroup;
        [SerializeField] private RectTransform qualityVisualRoot;
        [SerializeField] private Image qualityIconImage;
        [SerializeField] private TextMeshProUGUI qualityNameField;
        [SerializeField] private TextMeshProUGUI qualityScoreField;

        [Header("Post-Serve Verdict (Reserved)")]
        [SerializeField] private CanvasGroup reactionGroup;
        [SerializeField] private RectTransform reactionVisualRoot;
        [SerializeField] private Image npcIconImage;
        [SerializeField] private Image reactionIconImage;
        [SerializeField] private TextMeshProUGUI npcNameField;
        [SerializeField] private TextMeshProUGUI reactionNameField;
        [SerializeField] private TextMeshProUGUI reactionSummaryField;
        [SerializeField] private CanvasGroup rewardPreviewGroup;
        [SerializeField] private Image rewardIconImage;
        [SerializeField] private TextMeshProUGUI rewardPreviewField;

        [Header("Details")]
        [SerializeField] private Button detailsToggleButton;
        [SerializeField] private GameObject detailsDrawer;
        [SerializeField] private RectTransform tagComparisonRoot;
        [SerializeField] private CookingUiChipView tagChipTemplate;
        [SerializeField] private TextMeshProUGUI exactMatchField;
        [SerializeField] private RectTransform preparationRoot;
        [SerializeField] private CookingPreparedIngredientRowView preparedIngredientRowPrefab;
        [SerializeField] private TextMeshProUGUI reasonsField;

        [Header("Actions")]
        [SerializeField] private CanvasGroup actionGroup;
        [SerializeField] private Button handToNpcButton;

        [Header("Text")]
        [SerializeField] private string noResultText = "완성된 음식이 없습니다.";

        private CookingGamePanel _subscribedPanel;
        private CookingResultPresentationModel _model;
        private DishResult _lastAnimatedResult;
        private Sequence _revealSequence;
        private bool _isRevealing;
        private bool _detailsOpen;
        private int _displayedReward;
        private Coroutine _releaseBlockerRoutine;

        public bool IsRevealing => _isRevealing;
        public bool DetailsOpen => _detailsOpen;
        public CookingResultPresentationModel CurrentPresentation => _model;

        private void Awake()
        {
            EnsureReferences();
            BindButtons();
        }

        private void OnEnable()
        {
            EnsureReferences();
            BindButtons();
            SubscribePanelEvents();
            Refresh();
        }

        private void OnDisable()
        {
            KillRevealSequence();
            _isRevealing = false;
            SetBlocker(false);
            if (_releaseBlockerRoutine != null)
            {
                StopCoroutine(_releaseBlockerRoutine);
                _releaseBlockerRoutine = null;
            }
            UnsubscribePanelEvents();
        }

        private void Update()
        {
            if (_isRevealing && UnityEngine.Input.anyKeyDown)
                CompleteReveal(true);
        }

        public void Initialize(CookingGamePanel owner, CookingFlowRunner runner, TMP_FontAsset defaultFontAsset = null)
        {
            gamePanel = owner;
            flowRunner = runner;
            if (defaultFontAsset != null)
                SetFontAsset(defaultFontAsset);

            EnsureReferences();
            BindButtons();
            if (isActiveAndEnabled)
            {
                SubscribePanelEvents();
                Refresh();
            }
        }

        public void SetPresentationSettings(CookingUiPresentationSettingsSO value)
        {
            presentationSettings = value;
            if (value?.FontAsset != null)
                SetFontAsset(value.FontAsset);
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

        public void Refresh()
        {
            EnsureReferences();
            DishResult result = GetCurrentResult();
            if (result == null)
            {
                BindEmptyState();
                return;
            }

            CookingGameSnapshot snapshot = gamePanel?.CurrentSnapshot;
            CookingDataCatalogSO catalog = flowRunner?.Catalog ?? gamePanel?.FlowRunner?.Catalog;
            Func<string, string> npcNameResolver = gamePanel?.NpcRunner != null
                ? gamePanel.NpcRunner.GetNpcDisplayName
                : (Func<string, string>)null;
            bool canHand = gamePanel != null && gamePanel.CanHandCurrentResultToNpc();

            _model = CookingResultPresentationBuilder.BuildResult(
                result,
                snapshot,
                null,
                catalog,
                presentationSettings,
                npcNameResolver,
                0,
                canHand);
            BindModel(_model);

            if (_lastAnimatedResult != result)
            {
                _lastAnimatedResult = result;
                StartReveal();
            }
            else if (_isRevealing == false)
            {
                ApplyRevealFinalState();
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_isRevealing)
                CompleteReveal(true);
        }

        public void ToggleDetails()
        {
            if (_isRevealing || detailsDrawer == null)
                return;

            _detailsOpen = !_detailsOpen;
            detailsDrawer.SetActive(_detailsOpen);
            UpdateDetailsButtonLabel();
        }

        private void BindModel(CookingResultPresentationModel model)
        {
            if (model == null)
                return;

            BindDishIcon(model.Source);
            SetText(dishNameField, model.DishName);
            SetText(recipeField, $"{model.RecipeName} · {model.CategoryName}");
            SetText(representativeTagsField, BuildRepresentativeTagText(model.RepresentativeTags));

            CookingQualityVisual qualityVisual = presentationSettings?.GetQualityVisual(model.Quality);
            BindImage(qualityIconImage, qualityVisual?.Icon);
            SetText(qualityNameField, model.QualityName);
            SetText(qualityScoreField, model.QualityScore == 0 ? "완성도 변화 없음" : $"완성도 {model.QualityScore:+#;-#;0}");
            if (qualityNameField != null && qualityVisual != null)
                qualityNameField.color = qualityVisual.Color;

            ApplyPreServeVisibility();
            RebuildPreparationEntries(model.PreparedIngredients);
            BindReasons(model.Reasons);

            _detailsOpen = false;
            SetActive(detailsDrawer, false);
            UpdateDetailsButtonLabel();
        }

        private void BindEmptyState()
        {
            _model = null;
            KillRevealSequence();
            SetText(dishNameField, noResultText);
            SetText(recipeField, string.Empty);
            SetText(representativeTagsField, string.Empty);
            SetText(qualityNameField, string.Empty);
            SetText(qualityScoreField, string.Empty);
            SetText(npcNameField, string.Empty);
            SetText(reactionNameField, string.Empty);
            SetText(reactionSummaryField, "요리 결과가 준비되면 예상 만족도가 표시됩니다.");
            SetText(rewardPreviewField, string.Empty);
            BindDishIcon(null);
            ClearChildren(tagComparisonRoot, tagChipTemplate != null ? tagChipTemplate.transform : null);
            ClearChildren(preparationRoot, preparedIngredientRowPrefab != null ? preparedIngredientRowPrefab.transform : null);
            SetText(reasonsField, string.Empty);
            SetButtonInteractable(detailsToggleButton, false);
            SetButtonInteractable(handToNpcButton, false);
            SetActive(detailsDrawer, false);
            ApplyPreServeVisibility();
            SetBlocker(false);
        }

        private void ApplyPreServeVisibility()
        {
            SetActive(reactionGroup != null ? reactionGroup.gameObject : null, false);
            SetActive(rewardPreviewGroup != null ? rewardPreviewGroup.gameObject : null, false);
            SetActive(exactMatchField != null ? exactMatchField.gameObject : null, false);
            SetActive(tagComparisonRoot != null ? tagComparisonRoot.gameObject : null, false);

            SetText(npcNameField, string.Empty);
            SetText(reactionNameField, string.Empty);
            SetText(reactionSummaryField, string.Empty);
            SetText(rewardPreviewField, string.Empty);
            SetText(exactMatchField, string.Empty);
            ClearChildren(tagComparisonRoot, tagChipTemplate != null ? tagChipTemplate.transform : null);
        }

        private void StartReveal()
        {
            KillRevealSequence();
            _isRevealing = true;
            SetBlocker(true);
            SetButtonInteractable(detailsToggleButton, false);
            SetButtonInteractable(handToNpcButton, false);
            SetActionGroup(false);

            if (backdropGroup != null)
                backdropGroup.alpha = 0f;
            if (dishVisualRoot != null)
                dishVisualRoot.localScale = new Vector3(0.84f, 0.84f, 1f);
            SetCanvasGroup(qualityGroup, 0f, false);
            if (qualityVisualRoot != null)
                qualityVisualRoot.localScale = new Vector3(0.68f, 0.68f, 1f);
            ApplyPreServeVisibility();

            float backdropDuration = presentationSettings != null ? presentationSettings.BackdropDuration : 0.4f;
            float qualityDuration = presentationSettings != null ? presentationSettings.QualityDuration : 0.4f;
            float actionDuration = presentationSettings != null ? presentationSettings.ActionDuration : 0.2f;

            Sequence sequence = DOTween.Sequence().SetUpdate(true);
            sequence.AppendCallback(() => PlayClip(presentationSettings?.DishRevealClip));
            if (backdropGroup != null)
                sequence.Append(backdropGroup.DOFade(1f, backdropDuration));
            else
                sequence.AppendInterval(backdropDuration);
            if (dishVisualRoot != null)
                sequence.Join(dishVisualRoot.DOScale(1f, backdropDuration).SetEase(Ease.OutBack));

            sequence.AppendCallback(() => PlayClip(presentationSettings?.QualityStampClip));
            if (qualityGroup != null)
                sequence.Append(qualityGroup.DOFade(1f, qualityDuration));
            else
                sequence.AppendInterval(qualityDuration);
            if (qualityVisualRoot != null)
                sequence.Join(qualityVisualRoot.DOScale(1f, qualityDuration).SetEase(Ease.OutBack));

            if (actionGroup != null)
                sequence.Append(actionGroup.DOFade(1f, actionDuration));
            else
                sequence.AppendInterval(actionDuration);
            sequence.OnComplete(() => CompleteReveal(false));
            _revealSequence = sequence;
        }

        private void CompleteReveal(bool skipped)
        {
            if (_isRevealing == false)
                return;

            KillRevealSequence();
            ApplyRevealFinalState();
            if (skipped)
            {
                if (_releaseBlockerRoutine != null)
                    StopCoroutine(_releaseBlockerRoutine);
                _releaseBlockerRoutine = StartCoroutine(ReleaseBlockerAfterPointerCycle());
            }
            else
            {
                SetBlocker(false);
            }
        }

        private IEnumerator ReleaseBlockerAfterPointerCycle()
        {
            yield return null;
            SetBlocker(false);
            _releaseBlockerRoutine = null;
        }

        private void ApplyRevealFinalState()
        {
            _isRevealing = false;
            if (backdropGroup != null)
                backdropGroup.alpha = 1f;
            if (dishVisualRoot != null)
                dishVisualRoot.localScale = Vector3.one;
            SetCanvasGroup(qualityGroup, 1f, false);
            if (qualityVisualRoot != null)
                qualityVisualRoot.localScale = Vector3.one;
            ApplyPreServeVisibility();
            SetActionGroup(true);
            SetButtonInteractable(detailsToggleButton, _model != null);
            SetButtonInteractable(handToNpcButton, _model != null && _model.CanHandToNpc);
        }

        private void SetActionGroup(bool visible)
        {
            if (actionGroup == null)
                return;

            actionGroup.alpha = visible ? 1f : 0f;
            actionGroup.interactable = visible;
            actionGroup.blocksRaycasts = visible;
        }

        private void SetRewardPreview(int amount)
        {
            _displayedReward = Mathf.Max(0, amount);
            SetText(rewardPreviewField, $"예상 보상  {_displayedReward}");
        }

        private void RebuildTagComparisons(IReadOnlyList<CookingTagChipModel> chips)
        {
            if (tagComparisonRoot == null || tagChipTemplate == null)
                return;

            ClearChildren(tagComparisonRoot, tagChipTemplate.transform);
            for (int i = 0; i < chips.Count; i++)
            {
                CookingUiChipView chip = Instantiate(tagChipTemplate, tagComparisonRoot);
                chip.gameObject.name = $"ResultTagChip{i + 1}";
                chip.Bind(chips[i], presentationSettings);
            }
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(tagComparisonRoot);
        }

        private void RebuildPreparationEntries(IReadOnlyList<CookingPreparedIngredientPresentationModel> preparedIngredients)
        {
            if (preparationRoot == null || preparedIngredientRowPrefab == null)
                return;

            ClearChildren(preparationRoot, preparedIngredientRowPrefab.transform);
            for (int i = 0; i < preparedIngredients.Count; i++)
            {
                CookingPreparedIngredientPresentationModel model = preparedIngredients[i];
                CookingPreparedIngredientRowView view = Instantiate(preparedIngredientRowPrefab, preparationRoot);
                view.gameObject.name = $"PreparedIngredient{i + 1}";
                view.SetPresentationSettings(presentationSettings);
                Sprite icon = model.Source != null
                    ? CookingTempVisualUtility.ResolveIngredientIcon(model.Source.Ingredient)
                    : null;
                view.Bind(model, icon);
            }
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(preparationRoot);
        }

        private void BindReasons(IReadOnlyList<string> reasons)
        {
            if (reasons == null || reasons.Count == 0)
            {
                SetText(reasonsField, "추가 판정 사유 없음");
                return;
            }

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < reasons.Count; i++)
                builder.AppendLine($"• {reasons[i]}");
            SetText(reasonsField, builder.ToString());
        }

        private DishResult GetCurrentResult()
        {
            return gamePanel != null ? gamePanel.GetCurrentDishResult() : flowRunner?.LastResult;
        }

        private void HandToNpc()
        {
            if (_isRevealing || gamePanel == null)
                return;

            Bus<CookingResultAdvanceRequestedEvent>.Raise(new CookingResultAdvanceRequestedEvent(gamePanel));
        }

        private void BindButtons()
        {
            if (detailsToggleButton != null)
            {
                detailsToggleButton.onClick.RemoveListener(ToggleDetails);
                detailsToggleButton.onClick.AddListener(ToggleDetails);
            }
            if (handToNpcButton != null)
            {
                handToNpcButton.onClick.RemoveListener(HandToNpc);
                handToNpcButton.onClick.AddListener(HandToNpc);
            }
        }

        private void UpdateDetailsButtonLabel()
        {
            if (detailsToggleButton == null)
                return;

            TextMeshProUGUI label = detailsToggleButton.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
                label.text = _detailsOpen ? "상세 접기" : "상세 보기";
        }

        private void EnsureReferences()
        {
            if (gamePanel == null)
                gamePanel = GetComponentInParent<CookingGamePanel>();
            if (flowRunner == null)
                flowRunner = gamePanel != null ? gamePanel.FlowRunner : GetComponentInParent<CookingFlowRunner>();
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
        }

        private void SubscribePanelEvents()
        {
            if (_subscribedPanel == gamePanel)
                return;

            UnsubscribePanelEvents();
            if (gamePanel == null)
                return;

            Bus<CookingGameSnapshotChangedEvent>.Events += HandleSnapshotChanged;
            Bus<CookingDishResultReadyEvent>.Events += HandleDishResultReady;
            _subscribedPanel = gamePanel;
        }

        private void UnsubscribePanelEvents()
        {
            if (_subscribedPanel == null)
                return;

            Bus<CookingGameSnapshotChangedEvent>.Events -= HandleSnapshotChanged;
            Bus<CookingDishResultReadyEvent>.Events -= HandleDishResultReady;
            _subscribedPanel = null;
        }

        private void HandleSnapshotChanged(CookingGameSnapshotChangedEvent gameEvent)
        {
            if (gameEvent.Source == gamePanel && isActiveAndEnabled)
                Refresh();
        }

        private void HandleDishResultReady(CookingDishResultReadyEvent gameEvent)
        {
            if (gameEvent.Source == gamePanel && isActiveAndEnabled)
                Refresh();
        }

        private void KillRevealSequence()
        {
            if (_revealSequence == null)
                return;

            _revealSequence.Kill(false);
            _revealSequence = null;
        }

        private void PlayClip(AudioClip clip)
        {
            if (audioSource != null && clip != null)
                audioSource.PlayOneShot(clip);
        }

        private void BindDishIcon(DishResult result)
        {
            if (dishIconImage == null)
                return;

            dishIconImage.sprite = CookingTempVisualUtility.ResolveDishIcon(result);
            dishIconImage.enabled = dishIconImage.sprite != null;
            dishIconImage.color = Color.white;
            dishIconImage.preserveAspect = true;
        }

        private void SetBlocker(bool active)
        {
            if (revealInputBlocker != null)
            {
                revealInputBlocker.raycastTarget = active;
                revealInputBlocker.gameObject.SetActive(active);
            }
        }

        private static string BuildRepresentativeTagText(IReadOnlyList<string> tags)
        {
            if (tags == null || tags.Count == 0)
                return "대표 태그 없음";

            return "#" + string.Join("  #", tags);
        }

        private static void BindImage(Image image, Sprite sprite)
        {
            if (image == null)
                return;

            image.sprite = sprite;
            image.enabled = sprite != null;
            image.preserveAspect = true;
        }

        private static void ClearChildren(RectTransform root, Transform preserved)
        {
            if (root == null)
                return;

            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Transform child = root.GetChild(i);
                if (child == preserved)
                    continue;

                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }

        private static void SetCanvasGroup(CanvasGroup group, float alpha, bool interactive)
        {
            if (group == null)
                return;

            group.alpha = alpha;
            group.interactable = interactive;
            group.blocksRaycasts = interactive;
        }

        private static void SetButtonInteractable(Button button, bool interactable)
        {
            if (button != null)
                button.interactable = interactable;
        }

        private static void SetText(TextMeshProUGUI field, string text)
        {
            if (field != null)
                field.text = text ?? string.Empty;
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
                target.SetActive(active);
        }
    }
}
