using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Work.Cook.Code.Data;

namespace Work.Cook.Code.Runtime.UI
{
    /// <summary>
    /// 중앙 조리대와 현재 재료 직접 상호작용 표시
    /// </summary>
    public sealed class CookingWorkbenchView : MonoBehaviour
    {
        [SerializeField] private Image boardImage;
        [SerializeField] private Button ingredientButton;
        [SerializeField] private Image ingredientImage;
        [SerializeField] private TextMeshProUGUI ingredientNameField;
        [SerializeField] private TextMeshProUGUI instructionField;
        [SerializeField] private Color boardIdleColor = new Color(0.45f, 0.26f, 0.13f, 1f);
        [SerializeField] private Color boardCommittedColor = new Color(0.62f, 0.36f, 0.16f, 1f);
        [SerializeField] private Color ingredientIdleColor = Color.white;
        [SerializeField] private Color ingredientCommittedColor = new Color(1f, 0.88f, 0.52f, 1f);

        private UnityAction _ingredientAction;

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

            if (ingredientImage != null)
            {
                ingredientImage.sprite = CookingTempVisualUtility.ResolveIngredientIcon(ingredient);
                ingredientImage.enabled = ingredient != null;
                ingredientImage.color = ingredientIdleColor;
                ingredientImage.preserveAspect = true;
            }

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

            if (ingredientImage != null)
            {
                ingredientImage.sprite = CookingTempVisualUtility.ResolveIngredientIcon(ingredient);
                ingredientImage.enabled = ingredient != null;
                ingredientImage.color = ingredientCommittedColor;
            }

            string optionName = option != null ? option.DisplayName : "그대로 사용";
            SetText(ingredientNameField, ingredient != null ? ingredient.DisplayName : "조리할 재료 없음");
            SetText(instructionField, $"[{optionName}] 카드가 작업 슬롯에 놓였습니다. 도마 위 재료를 눌러 조리를 시작하세요.");
        }

        /// <summary>
        /// 선택한 손질법의 조리 실행 시작 상태 표시
        /// </summary>
        /// <param name="ingredient">조리 대상 재료</param>
        /// <param name="option">선택한 손질 옵션</param>
        public void ShowInteractionStarted(IngredientSO ingredient, IngredientPreparationOption option)
        {
            EnsureReferences();
            SetBoardCommitted(true);
            SetIngredientAction(null);

            if (ingredientImage != null)
                ingredientImage.color = ingredientCommittedColor;

            string ingredientName = ingredient != null ? ingredient.DisplayName : "재료";
            string optionName = option != null ? option.DisplayName : "그대로 사용";
            SetText(instructionField, $"{ingredientName}에 [{optionName}] 조리를 진행 중입니다.");
        }

        public void ShowInteractionResult(IngredientSO ingredient, IngredientPreparationOption option)
        {
            EnsureReferences();
            SetBoardCommitted(false);
            SetIngredientAction(null);

            if (ingredientImage != null)
            {
                ingredientImage.color = ingredientIdleColor;
            }

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

        private void EnsureReferences()
        {
            if (ingredientButton == null)
                ingredientButton = GetComponentInChildren<Button>(true);
            if (ingredientImage == null && ingredientButton != null)
                ingredientImage = ingredientButton.GetComponent<Image>();
        }

        private static void SetText(TextMeshProUGUI field, string text)
        {
            if (field != null)
                field.text = text ?? string.Empty;
        }
    }
}
