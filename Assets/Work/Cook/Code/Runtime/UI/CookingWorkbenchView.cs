using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Work.Cook.Code.Data;

namespace Work.Cook.Code.Runtime.UI
{
    /// <summary>
    /// 중앙 조리대와 현재 재료 직접 상호작용 표시.
    /// 별도의 ReadyVisual 대신 IngredientVisual 하나가 모든 조리 상태를 표현한다.
    /// </summary>
    public sealed class CookingWorkbenchView : MonoBehaviour
    {
        [SerializeField] private Image boardImage;
        [SerializeField] private Button ingredientButton;
        [SerializeField] private RectTransform ingredientAnchor;
        [SerializeField] private Image ingredientImage;
        [SerializeField] private CanvasGroup ingredientGroup;
        [SerializeField] private Image readyFrameImage;
        [SerializeField] private TextMeshProUGUI ingredientNameField;
        [SerializeField] private TextMeshProUGUI instructionField;
        [SerializeField] private Color boardIdleColor = new Color(0.45f, 0.26f, 0.13f, 1f);
        [SerializeField] private Color boardCommittedColor = new Color(0.62f, 0.36f, 0.16f, 1f);

        private UnityAction _ingredientAction;

        public RectTransform IngredientAnchor => ingredientAnchor;

        private void Awake()
        {
            EnsureReferences();
        }

        private void OnDisable()
        {
            if (ingredientAnchor == null)
                return;

            ingredientAnchor.DOKill(false);
            ingredientAnchor.localScale = Vector3.one;
        }

        public void SetFontAsset(TMP_FontAsset value)
        {
            if (value == null)
                return;

            if (ingredientNameField != null)
                ingredientNameField.font = value;
            if (instructionField != null)
                instructionField.font = value;
        }

        public void BindIngredient(IngredientSO ingredient)
        {
            EnsureReferences();
            SetBoardCommitted(false);
            SetIngredientAction(null);
            BindIngredientVisual(ingredient);
            SetIngredientState(ingredient != null, false, false);

            SetText(ingredientNameField, ingredient != null ? ingredient.DisplayName : "조리할 재료 없음");
            SetText(instructionField, ingredient != null
                ? "하단 카드에서 손질법을 선택하세요."
                : "재료 선택을 먼저 완료하세요.");
        }

        public void BeginInteraction(
            IngredientSO ingredient,
            IngredientPreparationOption option,
            UnityAction completed)
        {
            EnsureReferences();
            SetBoardCommitted(true);
            SetIngredientAction(completed);
            BindIngredientVisual(ingredient);
            SetIngredientState(ingredient != null, true, false);

            string optionName = option != null ? option.DisplayName : "그대로 사용";
            SetText(ingredientNameField, ingredient != null ? ingredient.DisplayName : "조리할 재료 없음");
            SetText(instructionField, $"[{optionName}] 준비 완료 · 중앙 재료를 눌러 조리를 시작하세요.");
        }

        public void ShowInteractionStarted(IngredientSO ingredient, IngredientPreparationOption option)
        {
            EnsureReferences();
            SetBoardCommitted(true);
            SetIngredientAction(null);
            BindIngredientVisual(ingredient);
            SetIngredientState(ingredient != null, false, true);

            string ingredientName = ingredient != null ? ingredient.DisplayName : "재료";
            string optionName = option != null ? option.DisplayName : "그대로 사용";
            SetText(instructionField, $"{ingredientName}에 [{optionName}] 조리를 진행 중입니다.");
        }

        public void ShowInteractionResult(IngredientSO ingredient, IngredientPreparationOption option)
        {
            EnsureReferences();
            SetBoardCommitted(false);
            SetIngredientAction(null);
            BindIngredientVisual(ingredient);
            SetIngredientState(ingredient != null, false, false);

            string ingredientName = ingredient != null ? ingredient.DisplayName : "재료";
            string optionName = option != null ? option.DisplayName : "그대로 사용";
            SetText(instructionField, $"{ingredientName}에 [{optionName}] 손질을 적용했습니다.");
        }

        private void SetIngredientAction(UnityAction action)
        {
            _ingredientAction = action;
            if (ingredientButton == null)
                return;

            ingredientButton.onClick.RemoveListener(HandleIngredientClicked);
            ingredientButton.interactable = action != null;
            if (action != null)
                ingredientButton.onClick.AddListener(HandleIngredientClicked);
        }

        private void HandleIngredientClicked()
        {
            UnityAction action = _ingredientAction;
            if (action == null)
                return;

            SetIngredientAction(null);
            action.Invoke();
        }

        private void SetBoardCommitted(bool committed)
        {
            if (boardImage != null)
                boardImage.color = committed == true ? boardCommittedColor : boardIdleColor;
        }

        private void BindIngredientVisual(IngredientSO ingredient)
        {
            if (ingredientImage == null)
                return;

            Sprite icon = ingredient != null ? ingredient.IconSprite : null;
            ingredientImage.sprite = icon;
            ingredientImage.enabled = icon != null;
            ingredientImage.preserveAspect = true;
            ingredientImage.raycastTarget = false;
        }

        private void SetIngredientState(bool visible, bool ready, bool inProgress)
        {
            if (ingredientGroup != null)
            {
                ingredientGroup.alpha = visible == false ? 0f : inProgress ? 0.72f : 1f;
                ingredientGroup.interactable = false;
                ingredientGroup.blocksRaycasts = false;
            }

            if (readyFrameImage != null)
                readyFrameImage.enabled = visible && ready;
            if (ingredientImage != null)
            {
                ingredientImage.color = ready
                    ? new Color(1f, 0.94f, 0.72f, 1f)
                    : Color.white;
            }

            if (ingredientAnchor == null)
                return;

            ingredientAnchor.DOKill(false);
            ingredientAnchor.localScale = Vector3.one;
            if (ready == true && Application.isPlaying == true)
            {
                ingredientAnchor
                    .DOPunchScale(Vector3.one * 0.08f, 0.28f, 4, 0.45f)
                    .SetEase(Ease.OutQuad)
                    .SetUpdate(true);
            }
        }

        private void EnsureReferences()
        {
            if (ingredientButton == null)
                ingredientButton = GetComponentInChildren<Button>(true);
            if (ingredientAnchor == null)
            {
                Transform found = transform.Find("IngredientAnchor");
                if (found != null)
                    ingredientAnchor = found as RectTransform;
            }
            if (ingredientImage == null && ingredientAnchor != null)
            {
                Transform found = ingredientAnchor.Find("IngredientVisual");
                if (found != null)
                    ingredientImage = found.GetComponent<Image>();
            }
            if (ingredientGroup == null && ingredientAnchor != null)
                ingredientGroup = ingredientAnchor.GetComponent<CanvasGroup>();
            if (readyFrameImage == null && ingredientAnchor != null)
            {
                Transform found = ingredientAnchor.Find("IngredientReadyFrame");
                if (found != null)
                    readyFrameImage = found.GetComponent<Image>();
            }

            HideIngredientButtonGraphic();
        }

        private void HideIngredientButtonGraphic()
        {
            if (ingredientButton == null)
                return;

            Graphic graphic = ingredientButton.GetComponent<Graphic>();
            if (graphic == null)
                return;

            Color color = graphic.color;
            color.a = 0f;
            graphic.color = color;
            graphic.raycastTarget = true;
            if (graphic is Image image)
            {
                image.sprite = null;
                image.preserveAspect = false;
            }

            if (ingredientButton.targetGraphic == graphic)
                ingredientButton.targetGraphic = null;
        }

        private static void SetText(TextMeshProUGUI field, string text)
        {
            if (field != null)
                field.text = text ?? string.Empty;
        }
    }
}
