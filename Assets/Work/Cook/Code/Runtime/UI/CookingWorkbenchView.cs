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
        private Vector2 _ingredientOriginalSize;
        private Vector2 _ingredientOriginalPosition;
        private Color _nameOriginalColor;
        private Color _instructionOriginalColor;
        private bool _focusLayoutCached;
        private bool _miniGameFocused;

        public RectTransform IngredientInteractionRect
            => ingredientButton != null ? ingredientButton.transform as RectTransform : null;
        public Image IngredientImage => ingredientImage;
        public Sprite IngredientSprite => ingredientImage != null ? ingredientImage.sprite : null;

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
            ExitMiniGameFocus();
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
            ExitMiniGameFocus();
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

        public void EnterMiniGameFocus()
        {
            EnsureReferences();
            if (_miniGameFocused == true)
                return;

            RectTransform ingredientRect = IngredientInteractionRect;
            if (ingredientRect == null)
                return;

            if (_focusLayoutCached == false)
            {
                _ingredientOriginalSize = ingredientRect.sizeDelta;
                _ingredientOriginalPosition = ingredientRect.anchoredPosition;
                _nameOriginalColor = ingredientNameField != null ? ingredientNameField.color : Color.white;
                _instructionOriginalColor = instructionField != null ? instructionField.color : Color.white;
                _focusLayoutCached = true;
            }

            SetIngredientAction(null);
            ingredientRect.sizeDelta = new Vector2(460f, 280f);
            SetTextAlpha(ingredientNameField, 0.12f);
            SetTextAlpha(instructionField, 0.12f);
            _miniGameFocused = true;
            Canvas.ForceUpdateCanvases();
        }

        public void ExitMiniGameFocus()
        {
            if (_miniGameFocused == false)
                return;

            RectTransform ingredientRect = IngredientInteractionRect;
            if (ingredientRect != null && _focusLayoutCached == true)
            {
                ingredientRect.sizeDelta = _ingredientOriginalSize;
                ingredientRect.anchoredPosition = _ingredientOriginalPosition;
            }
            if (ingredientNameField != null)
                ingredientNameField.color = _nameOriginalColor;
            if (instructionField != null)
                instructionField.color = _instructionOriginalColor;
            _miniGameFocused = false;
            Canvas.ForceUpdateCanvases();
        }

        public void SetFocusedIngredientScreenPosition(Vector2 screenPosition, Camera eventCamera)
        {
            if (_miniGameFocused == false)
                return;

            RectTransform ingredientRect = IngredientInteractionRect;
            RectTransform parentRect = ingredientRect != null ? ingredientRect.parent as RectTransform : null;
            if (ingredientRect == null || parentRect == null)
                return;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRect, screenPosition, eventCamera, out Vector2 localPosition))
            {
                ingredientRect.anchoredPosition = localPosition;
            }
        }

        public void ResetFocusedIngredientPosition()
        {
            RectTransform ingredientRect = IngredientInteractionRect;
            if (_miniGameFocused == true && ingredientRect != null && _focusLayoutCached == true)
                ingredientRect.anchoredPosition = _ingredientOriginalPosition;
        }

        public bool GetDisplayedIngredientWorldCorners(Vector3[] worldCorners)
        {
            EnsureReferences();
            if (worldCorners == null || worldCorners.Length < 4 || ingredientImage == null || ingredientImage.enabled == false)
                return false;

            RectTransform rectTransform = ingredientImage.rectTransform;
            Rect fittedRect = rectTransform.rect;
            Sprite sprite = ingredientImage.sprite;
            if (ingredientImage.preserveAspect == true && sprite != null && sprite.rect.height > 0f)
                fittedRect = CalculatePreservedAspectRect(fittedRect, sprite.rect.size);

            worldCorners[0] = rectTransform.TransformPoint(new Vector3(fittedRect.xMin, fittedRect.yMin));
            worldCorners[1] = rectTransform.TransformPoint(new Vector3(fittedRect.xMin, fittedRect.yMax));
            worldCorners[2] = rectTransform.TransformPoint(new Vector3(fittedRect.xMax, fittedRect.yMax));
            worldCorners[3] = rectTransform.TransformPoint(new Vector3(fittedRect.xMax, fittedRect.yMin));
            return true;
        }

        public static Rect CalculatePreservedAspectRect(Rect container, Vector2 contentSize)
        {
            if (container.width <= 0f || container.height <= 0f || contentSize.x <= 0f || contentSize.y <= 0f)
                return container;

            float contentAspect = contentSize.x / contentSize.y;
            float containerAspect = container.width / container.height;
            if (contentAspect > containerAspect)
            {
                float height = container.width / contentAspect;
                container.y += (container.height - height) * 0.5f;
                container.height = height;
            }
            else
            {
                float width = container.height * contentAspect;
                container.x += (container.width - width) * 0.5f;
                container.width = width;
            }

            return container;
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

        private static void SetTextAlpha(TextMeshProUGUI field, float alpha)
        {
            if (field == null)
                return;

            Color color = field.color;
            color.a = alpha;
            field.color = color;
        }
    }
}
