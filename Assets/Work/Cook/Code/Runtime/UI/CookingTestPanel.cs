using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
using UnityEngine.UI;
using Work.Cook.Code.Data;
using Work.NPC.Code.Data;
using Work.NPC.Code.Runtime;

#if UNITY_EDITOR
using UnityEditor;
#endif
using Work.Cook.Code.Runtime.Core;
using Work.Cook.Code.Runtime.Events;
using Work.Cook.Code.Runtime.Integration;
using Work.Cook.Code.Runtime.Systems;
using Work.Cook.Code.Runtime.UI;
using Work.Core.EventBus;

namespace Work.Cook.Code.Runtime.UI
{
    public sealed partial class CookingTestPanel : MonoBehaviour
    {
        [SerializeField] private CookingFlowRunner runner;
        [SerializeField] private NpcConversationRunner npcRunner;
        [SerializeField] private CookingDataCatalogSO catalog;
        [SerializeField] private TMP_FontAsset fontAsset;
        [SerializeField] private bool buildOnAwake = true;
        [SerializeField] private bool visibleOnStart = true;
        [SerializeField] private Vector2 panelSize = new Vector2(620f, 760f);

        private readonly List<IngredientSO> _directSelection = new List<IngredientSO>();

        private Canvas _canvas;
        private RectTransform _panel;
        private RectTransform _contentRoot;
        private Button _toggleButton;
        private TextMeshProUGUI _titleText;
        private RecipeSO _selectedRecipe;
        private bool _visible;

        private enum ButtonTone
        {
            Default,
            Primary,
            Selected,
            Warning,
            Danger
        }

        private void Awake()
        {
            EnsureReferences();

            if (runner == null)
                return;

            if (buildOnAwake == true)
            {
                BuildPanel();
                SetVisible(visibleOnStart);
                ShowStartScreen();
            }
        }

        public void Open()
        {
            BuildPanel();
            if (runner == null)
                return;

            SetVisible(true);
            ShowStartScreen();
        }

        public void Close()
        {
            SetVisible(false);
        }

        public void Toggle()
        {
            SetVisible(_visible == false);
        }

        private void EnsureReferences()
        {
#if UNITY_EDITOR
            if (catalog == null)
                catalog = FindFirstAsset<CookingDataCatalogSO>();

            if (fontAsset == null)
                fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Font/MangoDdobak-B(otf) SDF.asset");
#endif

            if (runner == null)
                runner = GetComponent<CookingFlowRunner>();

            if (runner == null)
            {
                Debug.LogError("CookingTestPanel needs a CookingFlowRunner assigned in the inspector or on the same GameObject.", this);
                return;
            }

            if (npcRunner == null)
                npcRunner = FindFirstObjectByType<NpcConversationRunner>();

            if (catalog != null)
                runner.SetCatalog(catalog);
        }

        private void BuildPanel()
        {
            if (_panel != null)
                return;

            EnsureReferences();
            if (runner == null)
                return;

            EnsureEventSystem();

            _canvas = CreateCanvas();

            _toggleButton = CreateButton(_canvas.transform, "Cooking Test", Toggle, new Vector2(142f, 38f), 38f, ButtonTone.Primary);
            RectTransform toggleRect = _toggleButton.transform as RectTransform;
            toggleRect.anchorMin = new Vector2(1f, 1f);
            toggleRect.anchorMax = new Vector2(1f, 1f);
            toggleRect.pivot = new Vector2(1f, 1f);
            toggleRect.anchoredPosition = new Vector2(-18f, -18f);

            _panel = CreatePanel(_canvas.transform);

            RectTransform header = CreateRow(_panel, "Header", 40f);
            MakeDragHandle(header, _panel, _canvas);

            _titleText = CreateText(header, "Title", "요리 테스트", 22f, TextAlignmentOptions.Left);
            _titleText.GetComponent<LayoutElement>().flexibleWidth = 1f;
            CreateButton(header, "X", Close, new Vector2(44f, 34f), 34f, ButtonTone.Default);

            _contentRoot = CreateScrollContent(_panel);
        }

        private void SetVisible(bool visible)
        {
            _visible = visible;

            if (_panel != null)
                _panel.gameObject.SetActive(visible);

            if (_toggleButton != null)
                SetButtonLabel(_toggleButton, visible ? "Hide Cooking" : "Cooking Test");
        }

        private void ShowStartScreen()
        {
            if (runner == null)
                return;

            runner.ResetFlow();
            _selectedRecipe = null;
            _directSelection.Clear();

            SetTitle("요리 시작");
            ClearContent();

            CreateInfoBox(
                _contentRoot,
                "Status",
                BuildCatalogStatusText(),
                108f,
                14f);

            CreateSectionLabel(_contentRoot, "시작 방식");
            RectTransform startRow = CreateRow(_contentRoot, "StartButtons", 42f);
            CreateButton(startRow, "레시피 선택", ShowRecipeSelectionScreen, null, 36f, ButtonTone.Primary);
            CreateButton(startRow, "재료 직접 선택", ShowDirectIngredientScreen);

            CreateSectionLabel(_contentRoot, "확인 포인트");
            CreateInfoBox(
                _contentRoot,
                "Guide",
                "손질 버튼에는 실제 연결된 손질법 ID, 이름 수식어, 독/괴식 위험이 함께 표시됩니다.\n" +
                "요리 결정은 레시피 슬롯의 필수 손질법으로 처리하고, 품질 변화는 각 손질 옵션의 효과로 처리합니다.",
                88f,
                14f);
        }

        private void ShowRecipeSelectionScreen()
        {
            SetTitle("레시피 선택");
            ClearContent();

            if (_selectedRecipe == null && runner.Recipes.Count > 0)
                _selectedRecipe = runner.Recipes[0];

            RectTransform actionRow = CreateRow(_contentRoot, "RecipeActionRow", 42f);
            CreateButton(actionRow, "이전", ShowStartScreen);
            CreateButton(actionRow, "선택한 레시피로 시작", StartSelectedRecipeCooking, null, 36f, ButtonTone.Primary);

            CreateSectionLabel(_contentRoot, "선택한 레시피");
            CreateInfoBox(_contentRoot, "SelectedRecipe", BuildRecipeInfo(_selectedRecipe), 198f, 14f);

            string warnings = BuildRecipeWarnings(_selectedRecipe);
            if (string.IsNullOrWhiteSpace(warnings) == false)
            {
                CreateSectionLabel(_contentRoot, "데이터 경고");
                CreateInfoBox(_contentRoot, "RecipeWarnings", warnings, 86f, 14f);
            }

            CreateSectionLabel(_contentRoot, "레시피 목록");
            for (int i = 0; i < runner.Recipes.Count; i++)
            {
                RecipeSO recipe = runner.Recipes[i];
                ButtonTone tone = recipe == _selectedRecipe ? ButtonTone.Selected : ButtonTone.Default;
                string label = recipe == _selectedRecipe
                    ? $"선택됨  {recipe.DisplayName}  ({recipe.RecipeId})"
                    : $"{recipe.DisplayName}  ({recipe.RecipeId})";

                CreateButton(_contentRoot, label, () =>
                {
                    _selectedRecipe = recipe;
                    ShowRecipeSelectionScreen();
                }, null, 38f, tone);
            }
        }

        private void StartSelectedRecipeCooking()
        {
            if (_selectedRecipe == null)
                return;

            if (runner.BeginRecipeCooking(_selectedRecipe))
                ShowPreparationScreen();
        }

        private void ShowDirectIngredientScreen()
        {
            SetTitle("재료 직접 선택");
            ClearContent();

            RecipeSO previewRecipe = FindRecipeByIngredients(_directSelection);
            CreateInfoBox(
                _contentRoot,
                "DirectStatus",
                BuildDirectSelectionText(previewRecipe),
                118f,
                14f);

            RectTransform actionRow = CreateRow(_contentRoot, "DirectActions", 42f);
            CreateButton(actionRow, "이전", ShowStartScreen);
            CreateButton(actionRow, "초기화", () =>
            {
                _directSelection.Clear();
                ShowDirectIngredientScreen();
            });

            Button startButton = CreateButton(actionRow, "요리 시작", StartDirectCooking, null, 36f, ButtonTone.Primary);
            startButton.interactable = _directSelection.Count > 0;

            CreateSectionLabel(_contentRoot, "재료 목록");
            for (int i = 0; i < runner.Ingredients.Count; i++)
            {
                IngredientSO ingredient = runner.Ingredients[i];
                bool selected = _directSelection.Contains(ingredient);
                ButtonTone tone = selected ? ButtonTone.Selected : ButtonTone.Default;
                string label = selected
                    ? $"선택됨  {ingredient.DisplayName}  ({ingredient.IngredientId})"
                    : $"{ingredient.DisplayName}  ({ingredient.IngredientId})";

                CreateButton(_contentRoot, label, () =>
                {
                    if (_directSelection.Contains(ingredient))
                        _directSelection.Remove(ingredient);
                    else
                        _directSelection.Add(ingredient);

                    ShowDirectIngredientScreen();
                }, null, 38f, tone);
            }
        }

        private void StartDirectCooking()
        {
            runner.BeginDirectSelection();

            for (int i = 0; i < _directSelection.Count; i++)
                runner.AddDirectIngredient(_directSelection[i]);

            if (runner.ConfirmDirectIngredients())
                ShowPreparationScreen();
        }

        private void ShowPreparationScreen()
        {
            IngredientSO ingredient = runner.GetNextUnpreparedIngredient();
            if (ingredient == null)
            {
                ShowReadyToCompleteScreen();
                return;
            }

            RecipeSO activeRecipe = GetActiveRecipe();

            SetTitle("재료 손질");
            ClearContent();

            CreateInfoBox(
                _contentRoot,
                "PrepStatus",
                BuildSessionStatusText(activeRecipe, ingredient),
                128f,
                14f);

            CreateSectionLabel(_contentRoot, "선택할 손질법");
            IReadOnlyList<IngredientPreparationOption> options = runner.GetPreparationOptions(ingredient);
            if (options.Count == 0)
            {
                CreateInfoBox(_contentRoot, "NoOptions", "이 재료에는 등록된 손질법이 없습니다.", 54f, 14f);
                CreateButton(_contentRoot, "손질 없이 진행", () =>
                {
                    runner.SelectPreparation(ingredient, null);
                    ShowPreparationScreen();
                }, null, 38f, ButtonTone.Warning);
            }
            else
            {
                for (int i = 0; i < options.Count; i++)
                {
                    IngredientPreparationOption option = options[i];
                    ButtonTone tone = GetPreparationTone(activeRecipe, ingredient, option);
                    CreateButton(
                        _contentRoot,
                        BuildPreparationButtonLabel(activeRecipe, ingredient, option),
                        () =>
                        {
                            Debug.Log(
                                $"CookingTestPanel selected preparation: ingredient={ingredient.IngredientId}, " +
                                $"method={(option.Method != null ? option.Method.MethodId : "none")}, " +
                                $"option={option.DisplayName}, disgusting={option.CausesDisgusting}, poison={option.AddsPoison}, " +
                                $"modifier={option.ResultNameModifier}",
                                this);

                            runner.SelectPreparation(ingredient, option);
                            ShowPreparationScreen();
                        },
                        null,
                        86f,
                        tone);
                }
            }

            CreateSectionLabel(_contentRoot, "진행 상황");
            CreateInfoBox(_contentRoot, "Progress", BuildPreparationProgressText(activeRecipe), 142f, 14f);
        }

        private void ShowReadyToCompleteScreen()
        {
            RecipeSO activeRecipe = GetActiveRecipe();

            SetTitle("요리 완성 준비");
            ClearContent();

            CreateInfoBox(
                _contentRoot,
                "PreparedSummary",
                BuildPreparationProgressText(activeRecipe),
                206f,
                14f);

            if (runner.TryPreviewCookingResult(out DishResult previewResult))
            {
                CreateSectionLabel(_contentRoot, "현재 NPC 요청 예상 일치도");
                CreateInfoBox(_contentRoot, "NpcPreviewMatch", BuildNpcMatchText(previewResult), 174f, 14f);
            }

            RectTransform row = CreateRow(_contentRoot, "CompleteActions", 42f);
            CreateButton(row, "결과 확인", CompleteCooking, null, 36f, ButtonTone.Primary);
            CreateButton(row, "처음으로", ShowStartScreen);
        }

        private void CompleteCooking()
        {
            if (runner.TryCompleteCooking(out DishResult result))
                ShowResultScreen(result);
        }

        private void ShowResultScreen(DishResult result)
        {
            SetTitle("요리 결과");
            ClearContent();

            CreateInfoBox(_contentRoot, "NpcMatchSummary", BuildNpcMatchText(result), 220f, 14f);

            CreateSectionLabel(_contentRoot, "요리 자체 정보");
            CreateInfoBox(_contentRoot, "ResultSummary", BuildResultText(result), 138f, 14f);

            CreateSectionLabel(_contentRoot, "손질 이력");
            CreateInfoBox(_contentRoot, "ResultPreparations", BuildResultPreparationText(result), 184f, 14f);

            if (result != null && result.Reasons.Count > 0)
            {
                CreateSectionLabel(_contentRoot, result.IsDisgusting ? "괴식 사유" : "판정 사유");
                CreateInfoBox(_contentRoot, "Reasons", BuildReasonText(result), 82f, 14f);
            }

            RectTransform row = CreateRow(_contentRoot, "ResultActions", 42f);
            CreateButton(row, "NPC에게 제출", () => SubmitDish(result), null, 36f, ButtonTone.Primary);
            CreateButton(row, "다시 요리하기", ShowStartScreen);
        }

        private void SubmitDish(DishResult result)
        {
            if (result == null)
                return;

            Bus<CookingTestDishSubmittedEvent>.Raise(new CookingTestDishSubmittedEvent(this, result));
            if (CookingNpcDishAdapter.SubmitToNpc(npcRunner, result, out string submitBlockReason) == false)
            {
                Debug.LogWarning($"CookingTestPanel could not submit the dish to NPC. reason={submitBlockReason}", this);
                return;
            }
            Debug.Log(
                $"NPC 제출 음식: RecipeId={result.RecipeId}, CategoryId={result.CategoryId}, " +
                $"Tags={result.BuildTagText()}, IsDisgusting={result.IsDisgusting}, Quality={result.Quality}, " +
                $"NpcDish=({CookingNpcDishAdapter.BuildSubmissionDebugSummary(result)})",
                this);
        }

        private RecipeSO GetActiveRecipe()
        {
            CookingSession session = runner.Controller.CurrentSession;
            if (session == null)
                return null;

            if (session.SelectedRecipe != null)
                return session.SelectedRecipe;

            return FindRecipeByIngredients(session.SelectedIngredients);
        }

        private RecipeSO FindRecipeByIngredients(IReadOnlyList<IngredientSO> ingredients)
        {
            if (ingredients == null || ingredients.Count == 0)
                return null;

            for (int i = 0; i < runner.Recipes.Count; i++)
            {
                RecipeSO recipe = runner.Recipes[i];
                if (recipe != null && recipe.MatchesIngredients(ingredients))
                    return recipe;
            }

            return null;
        }

        private ButtonTone GetPreparationTone(
            RecipeSO recipe,
            IngredientSO ingredient,
            IngredientPreparationOption option)
        {
            if (option == null)
                return ButtonTone.Warning;

            if (option.CausesDisgusting || option.AddsPoison)
                return ButtonTone.Danger;

            if (option.QualityDelta < 0 || string.IsNullOrWhiteSpace(option.ResultNameModifier) == false)
                return ButtonTone.Warning;

            return ButtonTone.Default;
        }

#if UNITY_EDITOR
        private static T FindFirstAsset<T>()
            where T : UnityEngine.Object
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            if (guids.Length == 0)
                return null;

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }
#endif
    }
}
