using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Work.Cook.Code.Data;
using Work.Cook.Code.Runtime.Core;
using Work.Cook.Code.Runtime.Systems;

namespace Work.Cook.Code.Runtime.UI
{
    /// <summary>
    /// 실제 조작 미니게임 구현 전 공통 결과 흐름을 검증하는 기본 미니게임 뷰
    /// </summary>
    public sealed class CookingSimpleMiniGameView : MonoBehaviour, ICookingMiniGameView
    {
        [SerializeField] private bool playAnyMiniGameType = true;
        [SerializeField] private CookingMiniGameType[] playableTypes;
        [SerializeField] private TextMeshProUGUI titleField;
        [SerializeField] private TextMeshProUGUI instructionField;
        [SerializeField] private TextMeshProUGUI resultField;
        [SerializeField] private Button perfectButton;
        [SerializeField] private Button goodButton;
        [SerializeField] private Button normalButton;
        [SerializeField] private Button badButton;
        [SerializeField] private int perfectQualityDelta = 2;
        [SerializeField] private int goodQualityDelta = 1;
        [SerializeField] private int normalQualityDelta;
        [SerializeField] private int badQualityDelta = -1;

        private Action<CookingMiniGameResult> _completed;
        private CookingMiniGameType _currentMiniGameType = CookingMiniGameType.None;
        private IngredientSO _currentIngredient;
        private IngredientPreparationOption _currentOption;
        private TMP_FontAsset _fontAsset;
        private bool _buttonsBound;

        private void Awake()
        {
            EnsureReferences();
            BindButtons();
        }

        /// <summary>
        /// 기본 미니게임 뷰 초기화
        /// </summary>
        /// <param name="owner">요리 패널</param>
        /// <param name="runner">요리 플로우 러너</param>
        /// <param name="defaultFontAsset">기본 UI 폰트</param>
        public void Initialize(CookingGamePanel owner, CookingFlowRunner runner, TMP_FontAsset defaultFontAsset = null)
        {
            if (defaultFontAsset != null)
                SetFontAsset(defaultFontAsset);

            EnsureReferences();
            BindButtons();
            SetButtonsInteractable(false);
        }

        /// <summary>
        /// 텍스트 필드 폰트 적용
        /// </summary>
        /// <param name="value">적용할 폰트</param>
        public void SetFontAsset(TMP_FontAsset value)
        {
            if (value == null)
                return;

            _fontAsset = value;
            if (titleField != null)
                titleField.font = value;
            if (instructionField != null)
                instructionField.font = value;
            if (resultField != null)
                resultField.font = value;
        }

        /// <summary>
        /// 지정한 타입의 기본 미니게임 실행 가능 여부 확인
        /// </summary>
        /// <param name="miniGameType">확인할 미니게임 타입</param>
        /// <returns>실행 가능 여부</returns>
        public bool CanPlay(CookingMiniGameType miniGameType)
        {
            if (miniGameType == CookingMiniGameType.None)
                return false;

            if (playAnyMiniGameType == true)
                return true;

            if (playableTypes == null)
                return false;

            for (int i = 0; i < playableTypes.Length; i++)
            {
                if (playableTypes[i] == miniGameType)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 기본 등급 선택 미니게임 시작
        /// </summary>
        /// <param name="ingredient">손질 대상 재료</param>
        /// <param name="option">선택한 손질 옵션</param>
        /// <param name="completed">미니게임 완료 콜백</param>
        public void StartMiniGame(
            IngredientSO ingredient,
            IngredientPreparationOption option,
            Action<CookingMiniGameResult> completed)
        {
            _currentIngredient = ingredient;
            _currentOption = option;
            _currentMiniGameType = option != null ? option.MiniGameType : CookingMiniGameType.None;
            _completed = completed;

            EnsureReferences();
            BindButtons();
            SetButtonsInteractable(true);

            string ingredientName = ingredient != null ? ingredient.DisplayName : "재료";
            string optionName = option != null ? option.DisplayName : "손질";
            SetText(titleField, $"{optionName} 미니게임");
            SetText(instructionField, $"{ingredientName}의 조리 완성도를 선택하세요. 실제 조작 미니게임은 이 뷰를 교체해 구현합니다.");
            SetText(resultField, string.Empty);
        }

        /// <summary>
        /// 현재 기본 미니게임 취소
        /// </summary>
        public void CancelMiniGame()
        {
            _completed = null;
            _currentIngredient = null;
            _currentOption = null;
            _currentMiniGameType = CookingMiniGameType.None;
            SetButtonsInteractable(false);
            SetText(resultField, "미니게임 취소");
        }

        private void CompletePerfect()
        {
            Complete(CookingMiniGameGrade.Perfect);
        }

        private void CompleteGood()
        {
            Complete(CookingMiniGameGrade.Good);
        }

        private void CompleteNormal()
        {
            Complete(CookingMiniGameGrade.Normal);
        }

        private void CompleteBad()
        {
            Complete(CookingMiniGameGrade.Bad);
        }

        private void Complete(CookingMiniGameGrade grade)
        {
            Action<CookingMiniGameResult> completed = _completed;
            if (completed == null)
                return;

            _completed = null;
            SetButtonsInteractable(false);

            CookingMiniGameResult result = new CookingMiniGameResult(
                _currentMiniGameType,
                grade,
                ResolveScore(grade),
                ResolveQualityDelta(grade),
                BuildFeedbackText(grade));

            SetText(resultField, result.FeedbackText);
            _currentIngredient = null;
            _currentOption = null;
            _currentMiniGameType = CookingMiniGameType.None;
            completed.Invoke(result);
        }

        private string BuildFeedbackText(CookingMiniGameGrade grade)
        {
            string ingredientName = _currentIngredient != null ? _currentIngredient.DisplayName : "재료";
            string optionName = _currentOption != null ? _currentOption.DisplayName : "손질";
            return $"{ingredientName} {optionName} 결과: {BuildGradeText(grade)}";
        }

        private static string BuildGradeText(CookingMiniGameGrade grade)
        {
            switch (grade)
            {
                case CookingMiniGameGrade.Perfect:
                    return "Perfect";
                case CookingMiniGameGrade.Good:
                    return "Good";
                case CookingMiniGameGrade.Bad:
                    return "Bad";
                default:
                    return "Normal";
            }
        }

        private float ResolveScore(CookingMiniGameGrade grade)
        {
            switch (grade)
            {
                case CookingMiniGameGrade.Perfect:
                    return 1f;
                case CookingMiniGameGrade.Good:
                    return 0.75f;
                case CookingMiniGameGrade.Bad:
                    return 0.15f;
                default:
                    return 0.5f;
            }
        }

        private int ResolveQualityDelta(CookingMiniGameGrade grade)
        {
            switch (grade)
            {
                case CookingMiniGameGrade.Perfect:
                    return perfectQualityDelta;
                case CookingMiniGameGrade.Good:
                    return goodQualityDelta;
                case CookingMiniGameGrade.Bad:
                    return badQualityDelta;
                default:
                    return normalQualityDelta;
            }
        }

        private void BindButtons()
        {
            if (_buttonsBound == true)
                return;

            if (perfectButton != null)
            {
                perfectButton.onClick.RemoveListener(CompletePerfect);
                perfectButton.onClick.AddListener(CompletePerfect);
            }

            if (goodButton != null)
            {
                goodButton.onClick.RemoveListener(CompleteGood);
                goodButton.onClick.AddListener(CompleteGood);
            }

            if (normalButton != null)
            {
                normalButton.onClick.RemoveListener(CompleteNormal);
                normalButton.onClick.AddListener(CompleteNormal);
            }

            if (badButton != null)
            {
                badButton.onClick.RemoveListener(CompleteBad);
                badButton.onClick.AddListener(CompleteBad);
            }

            _buttonsBound = true;
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (perfectButton != null)
                perfectButton.interactable = interactable;
            if (goodButton != null)
                goodButton.interactable = interactable;
            if (normalButton != null)
                normalButton.interactable = interactable;
            if (badButton != null)
                badButton.interactable = interactable;
        }

        private void EnsureReferences()
        {
            if (_fontAsset != null)
                SetFontAsset(_fontAsset);
        }

        private static void SetText(TextMeshProUGUI field, string text)
        {
            if (field != null)
                field.text = text ?? string.Empty;
        }
    }
}
