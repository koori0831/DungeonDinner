using System;
using System.Collections.Generic;
using System.Text;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Work.Cook.Code.Data;
using Work.Cook.Code.Runtime.Core;
using Work.Cook.Code.Runtime.Systems;

namespace Work.Cook.Code.Runtime.UI
{
    public enum CookingPreparationHandState
    {
        Interactive,
        Selected,
        MiniGame,
        Result
    }

    /// <summary>
    /// 현재 재료에 가능한 손질 카드 손패를 1~7장 Fan으로 배치하고 입력 상태를 관리한다.
    /// </summary>
    public sealed class CookingPreparationHandView : MonoBehaviour
    {
        private const float MiniGameBackdropAlpha = 0.08f;
        private const float ResultBackdropAlpha = 0.72f;

        [SerializeField] private RectTransform cardRoot;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private CookingPreparationOptionCardView preparationOptionCardPrefab;
        [SerializeField] private CanvasGroup cardGroup;
        [SerializeField] private TMP_FontAsset fontAsset;
        [SerializeField] private CookingUiPresentationSettingsSO presentationSettings;
        [SerializeField] private CookingPreparationTooltipView tooltipView;
        [SerializeField] private string noOptionText = "이 재료에는 등록된 손질법이 없습니다.";
        [SerializeField] private string noOptionButtonText = "그대로 진행";
        [SerializeField] private string unknownEffectText = "아직 결과를 모릅니다.";
        [SerializeField] private string knownEffectTitleText = "확인한 효과";

        private readonly List<CookingPreparationOptionCardView> _cards = new List<CookingPreparationOptionCardView>();
        private CookingKnowledgeStore _knowledgeStore;
        private CookingPreparationHandState _state = CookingPreparationHandState.Interactive;
        private int _focusedIndex = -1;
        private int _selectedIndex = -1;
        private bool _overflowWarningShown;
        private IngredientPreparationOption _recommendedOption;
        private Func<IngredientPreparationOption, bool> _isRecipeAllowed;
        private CookingPreparationRecommendationKind _recommendationKind;

        public CookingPreparationHandState State => _state;
        public int CardCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _cards.Count; i++)
                {
                    if (_cards[i] != null && _cards[i].transform.parent == cardRoot)
                        count++;
                }
                return count;
            }
        }

        private void OnDisable()
        {
            tooltipView?.Hide(null);
            KillLayoutTweens();
            _focusedIndex = -1;
            ApplyFanLayout(true);
        }

        private void OnRectTransformDimensionsChange()
        {
            if (Application.isPlaying == true && _cards.Count > 0)
                ApplyFanLayout(true);
        }

        public void Initialize(
            CookingGamePanel owner,
            CookingKnowledgeStore knowledge,
            TMP_FontAsset defaultFontAsset,
            CookingUiPresentationSettingsSO defaultPresentationSettings = null)
        {
            _knowledgeStore = knowledge;
            if (defaultPresentationSettings != null)
                presentationSettings = defaultPresentationSettings;
            if (defaultFontAsset != null)
                SetFontAsset(defaultFontAsset);
            EnsureReferences();
        }

        public void SetFontAsset(TMP_FontAsset value)
        {
            if (value == null)
                return;

            fontAsset = value;
            ApplyFontToExistingTexts();
        }

        public void Rebuild(
            IngredientSO ingredient,
            IReadOnlyList<IngredientPreparationOption> options,
            Action<IngredientSO, IngredientPreparationOption> selected)
        {
            Rebuild(ingredient, options, null, null, selected);
        }

        public void Rebuild(
            IngredientSO ingredient,
            IReadOnlyList<IngredientPreparationOption> options,
            PlannedPreparation recommendation,
            Func<IngredientPreparationOption, bool> isRecipeAllowed,
            Action<IngredientSO, IngredientPreparationOption> selected)
        {
            EnsureReferences();
            tooltipView?.Hide(null);
            ClearCards();
            _focusedIndex = -1;
            _selectedIndex = -1;
            _overflowWarningShown = false;
            _recommendedOption = recommendation?.PreparationOption;
            _recommendationKind = recommendation?.Kind ?? CookingPreparationRecommendationKind.None;
            _isRecipeAllowed = isRecipeAllowed;
            SetState(CookingPreparationHandState.Interactive);

            if (cardRoot == null || ingredient == null)
                return;

            if (options == null || options.Count == 0)
            {
                CreateNoOptionCard(ingredient, selected);
                ApplyFanLayout(true);
                return;
            }

            for (int i = 0; i < options.Count; i++)
            {
                IngredientPreparationOption option = options[i];
                if (option != null)
                    CreatePreparationCard(ingredient, option, i, selected);
            }

            if (_cards.Count == 0)
                CreateNoOptionCard(ingredient, selected);

            ApplyFanLayout(true);
        }

        /// <summary>
        /// 기존 호출 호환용. false는 선택 완료 상태이며 시각적 흐림은 적용하지 않는다.
        /// 미니게임과 결과의 흐림은 각각 ShowMiniGameState/ShowResultState를 사용한다.
        /// </summary>
        public void SetInteractable(bool interactable)
        {
            SetState(interactable
                ? CookingPreparationHandState.Interactive
                : CookingPreparationHandState.Selected);
        }

        public void ShowMiniGameState()
        {
            SetState(CookingPreparationHandState.MiniGame);
        }

        public void ShowResultState()
        {
            SetState(CookingPreparationHandState.Result);
        }

        private void SetState(CookingPreparationHandState state)
        {
            _state = state;
            bool inputEnabled = state == CookingPreparationHandState.Interactive;
            float alpha;
            switch (state)
            {
                case CookingPreparationHandState.MiniGame:
                    alpha = MiniGameBackdropAlpha;
                    break;
                case CookingPreparationHandState.Result:
                    alpha = ResultBackdropAlpha;
                    break;
                default:
                    alpha = 1f;
                    break;
            }

            if (cardGroup != null)
            {
                cardGroup.alpha = alpha;
                cardGroup.interactable = inputEnabled;
                cardGroup.blocksRaycasts = inputEnabled;
            }

            for (int i = 0; i < _cards.Count; i++)
                _cards[i]?.SetInputEnabled(inputEnabled);

            if (inputEnabled == false)
            {
                _focusedIndex = -1;
                tooltipView?.Hide(null);
            }

            ApplyFanLayout(false);
        }

        private void CreateNoOptionCard(
            IngredientSO ingredient,
            Action<IngredientSO, IngredientPreparationOption> selected)
        {
            CookingPreparationOptionCardView view = CreateCard();
            if (view == null)
                return;

            view.Bind(
                string.Empty,
                null,
                noOptionButtonText,
                noOptionText,
                string.Empty,
                "선택",
                false,
                () => HandleCardSelected(view, ingredient, null, selected));
        }

        private void CreatePreparationCard(
            IngredientSO ingredient,
            IngredientPreparationOption option,
            int index,
            Action<IngredientSO, IngredientPreparationOption> selected)
        {
            CookingPreparationOptionCardView view = CreateCard();
            if (view == null)
                return;

            Sprite icon = option.Method != null ? option.Method.IconSprite : null;
            view.Bind(
                BuildOptionIconText(index, option),
                icon,
                option.DisplayName,
                BuildOptionDescription(option),
                BuildKnownEffectText(ingredient, option),
                "선택",
                true,
                () => HandleCardSelected(view, ingredient, option, selected));
            view.SetSelected(option == _recommendedOption);
        }

        private CookingPreparationOptionCardView CreateCard()
        {
            if (preparationOptionCardPrefab == null)
            {
                Debug.LogError("CookingPreparationHandView preparationOptionCardPrefab is missing.", this);
                return null;
            }

            CookingPreparationOptionCardView view = Instantiate(preparationOptionCardPrefab, cardRoot);
            view.SetPresentation(presentationSettings, tooltipView);
            view.HoverChanged += HandleCardHoverChanged;
            _cards.Add(view);
            ApplyFont(view.gameObject);
            return view;
        }

        private void HandleCardSelected(
            CookingPreparationOptionCardView view,
            IngredientSO ingredient,
            IngredientPreparationOption option,
            Action<IngredientSO, IngredientPreparationOption> selected)
        {
            if (_state != CookingPreparationHandState.Interactive)
                return;

            _selectedIndex = _cards.IndexOf(view);
            for (int i = 0; i < _cards.Count; i++)
                _cards[i]?.SetSelected(i == _selectedIndex);
            SetState(CookingPreparationHandState.Selected);
            selected?.Invoke(ingredient, option);
        }

        private void HandleCardHoverChanged(CookingPreparationOptionCardView view, bool hovered)
        {
            if (_state != CookingPreparationHandState.Interactive)
                return;

            int index = _cards.IndexOf(view);
            if (hovered == true)
                _focusedIndex = index;
            else if (_focusedIndex == index)
                _focusedIndex = -1;

            ApplyFanLayout(false);
        }

        private void ApplyFanLayout(bool immediate)
        {
            if (cardRoot == null || _cards.Count == 0)
                return;

            Canvas.ForceUpdateCanvases();
            ConfigureScrollFallback();

            RectTransform first = null;
            for (int i = 0; i < _cards.Count; i++)
            {
                if (_cards[i] == null)
                    continue;

                first = _cards[i].LayoutRoot;
                if (first != null)
                    break;
            }

            if (first == null)
                return;

            float cardWidth = Mathf.Max(first.rect.width, first.sizeDelta.x);
            float availableWidth = Mathf.Max(cardWidth, cardRoot.rect.width);
            int count = _cards.Count;

            int maxFanCount = presentationSettings != null ? presentationSettings.MaxFanCardCount : 7;
            if (count > maxFanCount && _overflowWarningShown == false)
            {
                _overflowWarningShown = true;
                Debug.LogWarning(
                    $"Cooking preparation hand has {count} cards. Scroll fallback is disabled, so the fan uses minimum compression.",
                    this);
            }

            for (int i = 0; i < count; i++)
            {
                CookingPreparationOptionCardView card = _cards[i];
                RectTransform rect = card != null ? card.LayoutRoot : null;
                if (rect == null)
                    continue;

                CookingPreparationFanLayout.CardPose pose = CookingPreparationFanLayout.Calculate(
                    i,
                    count,
                    availableWidth,
                    cardWidth,
                    presentationSettings != null ? presentationSettings.MaxFanAngle : 13f,
                    presentationSettings != null ? presentationSettings.MinFanCardSpacing : 132f,
                    presentationSettings != null ? presentationSettings.MaxFanCardSpacing : 220f,
                    presentationSettings != null ? presentationSettings.MinFanCardScale : 0.86f,
                    presentationSettings != null ? presentationSettings.FanArcHeight : 70f,
                    _focusedIndex,
                    _selectedIndex,
                    presentationSettings != null ? presentationSettings.FanFocusLift : 68f,
                    presentationSettings != null ? presentationSettings.FanFocusScale : 1.08f,
                    presentationSettings != null ? presentationSettings.FanSelectedLift : 18f,
                    presentationSettings != null ? presentationSettings.FanNeighborSpread : 36f,
                    presentationSettings != null ? presentationSettings.FanPeerDrop : 24f);

                rect.DOKill(false);
                if (immediate == true || Application.isPlaying == false)
                {
                    rect.anchoredPosition = pose.AnchoredPosition;
                    rect.localRotation = Quaternion.Euler(0f, 0f, pose.Rotation);
                    rect.localScale = Vector3.one * pose.Scale;
                    continue;
                }

                float duration = presentationSettings != null ? presentationSettings.FanTweenDuration : 0.16f;
                Sequence sequence = DOTween.Sequence().SetUpdate(true).SetTarget(rect);
                sequence.Join(rect.DOAnchorPos(pose.AnchoredPosition, duration).SetEase(Ease.OutQuad));
                sequence.Join(rect.DOLocalRotate(new Vector3(0f, 0f, pose.Rotation), duration).SetEase(Ease.OutQuad));
                sequence.Join(rect.DOScale(pose.Scale, duration).SetEase(Ease.OutQuad));
            }

            ApplySiblingOrder();
        }

        private void ApplySiblingOrder()
        {
            if (cardRoot == null || cardRoot.gameObject.activeInHierarchy == false)
                return;

            float center = (_cards.Count - 1) * 0.5f;
            List<int> order = new List<int>(_cards.Count);
            for (int i = 0; i < _cards.Count; i++)
                order.Add(i);

            order.Sort((left, right) =>
                Mathf.Abs(right - center).CompareTo(Mathf.Abs(left - center)));
            for (int i = 0; i < order.Count; i++)
                _cards[order[i]]?.transform.SetSiblingIndex(i);

            if (_selectedIndex >= 0 && _selectedIndex < _cards.Count)
                _cards[_selectedIndex]?.transform.SetAsLastSibling();
            if (_focusedIndex >= 0 && _focusedIndex < _cards.Count)
                _cards[_focusedIndex]?.transform.SetAsLastSibling();
        }

        private void ConfigureScrollFallback()
        {
            bool useScroll = presentationSettings != null
                             && presentationSettings.EnableScrollFallback
                             && _cards.Count >= presentationSettings.ScrollFallbackThreshold;
            if (scrollRect != null)
            {
                scrollRect.horizontal = useScroll;
                scrollRect.vertical = false;
                scrollRect.StopMovement();
                if (useScroll == false)
                    scrollRect.horizontalNormalizedPosition = 0.5f;
            }

            RectMask2D viewportMask = scrollRect?.viewport != null
                ? scrollRect.viewport.GetComponent<RectMask2D>()
                : null;
            if (viewportMask != null)
                viewportMask.enabled = useScroll;
        }

        private string BuildKnownEffectText(IngredientSO ingredient, IngredientPreparationOption option)
        {
            if (option == null)
                return unknownEffectText;

            if (_knowledgeStore == null || _knowledgeStore.IsPreparationEffectKnown(ingredient, option) == false)
                return unknownEffectText;

            StringBuilder builder = new StringBuilder();
            builder.AppendLine(knownEffectTitleText);

            if (option.QualityDelta != 0)
                builder.AppendLine($"품질 변화: {option.QualityDelta:+#;-#;0}");

            AppendTags(builder, "추가 태그", option.AddTags);
            AppendTags(builder, "제거 태그", option.RemoveTags);

            if (string.IsNullOrWhiteSpace(option.ResultNameModifier) == false)
                builder.AppendLine($"이름 변화: {option.ResultNameModifier}");
            if (option.CausesDisgusting == true)
                builder.AppendLine("괴식 위험이 있습니다.");
            if (option.AddsPoison == true)
                builder.AppendLine("독성이 추가됩니다.");

            return builder.Length > knownEffectTitleText.Length + 1
                ? builder.ToString()
                : $"{knownEffectTitleText}\n특별한 변화 없음";
        }

        private string BuildOptionDescription(IngredientPreparationOption option)
        {
            if (option == null)
                return string.Empty;
            StringBuilder builder = new StringBuilder();
            if (option == _recommendedOption)
                builder.AppendLine(BuildRecommendationLabel(_recommendationKind));
            else if (_isRecipeAllowed?.Invoke(option) == true)
                builder.AppendLine("레시피 허용");
            if (string.IsNullOrWhiteSpace(option.Description) == false)
                builder.Append(option.Description);
            else if (option.Method != null && string.IsNullOrWhiteSpace(option.Method.Description) == false)
                builder.Append(option.Method.Description);
            else
                builder.Append("이 방식으로 재료를 손질합니다.");
            return builder.ToString();
        }

        private static string BuildRecommendationLabel(CookingPreparationRecommendationKind kind)
        {
            switch (kind)
            {
                case CookingPreparationRecommendationKind.ReplayedVariant:
                    return "추천 · 변형 기록";
                case CookingPreparationRecommendationKind.KnownPerfect:
                    return "추천 · 확인한 최적 손질";
                default:
                    return "추천 · 유일한 레시피 허용 손질";
            }
        }

        private static string BuildOptionIconText(int index, IngredientPreparationOption option)
        {
            if (option?.Method != null && string.IsNullOrWhiteSpace(option.Method.MethodId) == false)
                return option.Method.MethodId.Substring(0, 1).ToUpperInvariant();
            return (index + 1).ToString();
        }

        private static void AppendTags(StringBuilder builder, string label, IReadOnlyList<FoodTagSO> tags)
        {
            if (tags == null || tags.Count == 0)
                return;

            builder.Append(label);
            builder.Append(": ");
            bool appended = false;
            for (int i = 0; i < tags.Count; i++)
            {
                if (tags[i] == null)
                    continue;
                if (appended == true)
                    builder.Append(", ");
                builder.Append(tags[i].DisplayName);
                appended = true;
            }
            builder.AppendLine();
        }

        private void EnsureReferences()
        {
            if (cardRoot == null)
                cardRoot = transform as RectTransform;
            if (scrollRect == null && cardRoot != null)
                scrollRect = cardRoot.GetComponentInParent<ScrollRect>();
            if (scrollRect == null)
                scrollRect = GetComponentInChildren<ScrollRect>(true);
            if (cardGroup == null)
                cardGroup = GetComponent<CanvasGroup>();
            if (cardGroup == null)
                Debug.LogError("CookingPreparationHandView needs a CanvasGroup assigned or attached to the same GameObject.", this);
        }

        private void ApplyFontToExistingTexts()
        {
            ApplyFont(gameObject);
        }

        private void ApplyFont(GameObject target)
        {
            if (fontAsset == null || target == null)
                return;

            TextMeshProUGUI[] labels = target.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < labels.Length; i++)
            {
                if (labels[i] != null)
                    labels[i].font = fontAsset;
            }
        }

        private void KillLayoutTweens()
        {
            for (int i = 0; i < _cards.Count; i++)
            {
                RectTransform rect = _cards[i] != null ? _cards[i].LayoutRoot : null;
                if (rect != null)
                    rect.DOKill(false);
            }
        }

        private void ClearCards()
        {
            KillLayoutTweens();
            for (int i = _cards.Count - 1; i >= 0; i--)
            {
                CookingPreparationOptionCardView card = _cards[i];
                if (card == null)
                    continue;
                card.HoverChanged -= HandleCardHoverChanged;
                card.transform.SetParent(null, false);
                if (Application.isPlaying == true)
                    Destroy(card.gameObject);
                else
                    DestroyImmediate(card.gameObject);
            }
            _cards.Clear();

            if (cardRoot == null || cardRoot.childCount == 0)
                return;
            for (int i = cardRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = cardRoot.GetChild(i);
                child.SetParent(null, false);
                if (Application.isPlaying == true)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }
    }
}
