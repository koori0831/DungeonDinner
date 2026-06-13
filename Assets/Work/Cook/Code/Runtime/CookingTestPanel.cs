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

namespace Work.Cook.Code.Runtime
{
    public sealed class CookingTestPanel : MonoBehaviour
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

        public event Action<DishResult> DishSubmitted;

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

            if (buildOnAwake)
            {
                BuildPanel();
                SetVisible(visibleOnStart);
                ShowStartScreen();
            }
        }

        public void Open()
        {
            BuildPanel();
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
                runner = gameObject.AddComponent<CookingFlowRunner>();

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

            DishSubmitted?.Invoke(result);
            CookingNpcDishAdapter.SubmitToNpc(npcRunner, result);
            Debug.Log(
                $"NPC 제출 음식: RecipeId={result.RecipeId}, CategoryId={result.CategoryId}, " +
                $"Tags={result.BuildTagText()}, IsDisgusting={result.IsDisgusting}, Quality={result.Quality}",
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

        private string BuildCatalogStatusText()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("카탈로그 상태");
            builder.AppendLine($"레시피 {runner.Recipes.Count}개 / 재료 {runner.Ingredients.Count}개");

            if (catalog == null)
                builder.AppendLine("경고: CookingDataCatalogSO가 연결되지 않았습니다.");

            if (runner.Recipes.Count == 0 || runner.Ingredients.Count == 0)
                builder.AppendLine("경고: 레시피 또는 재료가 비어 있습니다.");

            return builder.ToString();
        }

        private string BuildRecipeInfo(RecipeSO recipe)
        {
            if (recipe == null)
                return "선택된 레시피가 없습니다.";

            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"{recipe.DisplayName}  ({recipe.RecipeId})");
            builder.AppendLine($"카테고리: {(recipe.Category != null ? recipe.Category.DisplayName : "없음")}");
            builder.AppendLine($"태그: {BuildTagDisplayText(recipe.BaseTags)}");
            builder.AppendLine("필요 재료:");

            for (int i = 0; i < recipe.RequiredIngredients.Count; i++)
            {
                RecipeIngredientRequirement requirement = recipe.RequiredIngredients[i];
                if (requirement == null || requirement.Ingredient == null)
                    continue;

                builder.Append($"- {requirement.Ingredient.DisplayName}");
                string alternativeText = BuildAlternativeText(requirement);
                if (string.IsNullOrWhiteSpace(alternativeText) == false)
                    builder.Append($" / 대체: {alternativeText}");

                builder.AppendLine();
            }

            return builder.ToString();
        }

        private static string BuildAlternativeText(RecipeIngredientRequirement requirement)
        {
            if (requirement == null)
                return string.Empty;

            List<string> names = new List<string>();
            for (int i = 0; i < requirement.AlternativeOptions.Count; i++)
            {
                RecipeIngredientAlternative alternative = requirement.AlternativeOptions[i];
                if (alternative == null || alternative.Ingredient == null)
                    continue;

                string label = alternative.Ingredient.DisplayName;
                if (string.IsNullOrWhiteSpace(alternative.ResultNameModifier) == false)
                    label += $" -> {alternative.ResultNameModifier}";

                names.Add(label);
            }

            for (int i = 0; i < requirement.Alternatives.Count; i++)
            {
                IngredientSO ingredient = requirement.Alternatives[i];
                if (ingredient != null)
                    names.Add(ingredient.DisplayName);
            }

            return names.Count > 0 ? string.Join(", ", names) : string.Empty;
        }

        private string BuildRecipeWarnings(RecipeSO recipe)
        {
            return string.Empty;
        }

        private string BuildDirectSelectionText(RecipeSO previewRecipe)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("선택한 재료");

            if (_directSelection.Count == 0)
            {
                builder.AppendLine("- 없음");
            }
            else
            {
                for (int i = 0; i < _directSelection.Count; i++)
                    builder.AppendLine($"- {_directSelection[i].DisplayName} ({_directSelection[i].IngredientId})");
            }

            builder.Append("예상 매칭: ");
            builder.AppendLine(previewRecipe != null
                ? $"{previewRecipe.DisplayName} ({previewRecipe.RecipeId})"
                : "알려진 레시피 없음 - 완성 시 괴식 판정 가능");

            return builder.ToString();
        }

        private string BuildSessionStatusText(RecipeSO recipe, IngredientSO currentIngredient)
        {
            CookingSession session = runner.Controller.CurrentSession;
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"현재 재료: {currentIngredient.DisplayName} ({currentIngredient.IngredientId})");
            builder.AppendLine($"요리 방식: {(session != null && session.Mode == CookingMode.Recipe ? "레시피 선택" : "재료 직접 선택")}");
            builder.AppendLine($"예상 레시피: {(recipe != null ? $"{recipe.DisplayName} ({recipe.RecipeId})" : "없음")}");

            return builder.ToString();
        }

        private string BuildPreparationButtonLabel(
            RecipeSO recipe,
            IngredientSO ingredient,
            IngredientPreparationOption option)
        {
            if (option == null)
                return "손질 없음";

            string prefix = option.CausesDisgusting || option.AddsPoison ? "[위험]" : "[선택]";

            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"{prefix} {option.DisplayName}");

            if (option.Method != null)
                builder.AppendLine($"손질법 ID: {option.Method.MethodId}");

            builder.Append(BuildPreparationEffectText(option));
            return builder.ToString();
        }

        private string BuildPreparationProgressText(RecipeSO recipe)
        {
            CookingSession session = runner.Controller.CurrentSession;
            if (session == null)
                return "진행 중인 요리가 없습니다.";

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < session.SelectedIngredients.Count; i++)
            {
                IngredientSO ingredient = session.SelectedIngredients[i];
                PreparedIngredientState prepared = session.GetPreparedIngredient(ingredient);

                builder.Append($"- {ingredient.DisplayName}: ");
                if (prepared == null)
                {
                    builder.AppendLine("대기 중");
                    continue;
                }

                string methodName = prepared.Method != null ? prepared.Method.DisplayName : "손질 없음";
                string methodId = prepared.Method != null ? prepared.Method.MethodId : "none";
                builder.AppendLine($"{methodName} ({methodId})");

                string effects = BuildPreparedEffectText(prepared);
                if (string.IsNullOrWhiteSpace(effects) == false)
                    builder.AppendLine($"  {effects}");
            }

            return builder.ToString();
        }

        private string BuildPreparedWarnings(RecipeSO recipe)
        {
            CookingSession session = runner.Controller.CurrentSession;
            if (session == null)
                return string.Empty;

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < session.PreparedIngredients.Count; i++)
            {
                PreparedIngredientState prepared = session.PreparedIngredients[i];
                if (prepared == null)
                    continue;

                if (prepared.CausesDisgusting)
                    builder.AppendLine($"- {prepared.Ingredient.DisplayName}: 이 손질은 괴식을 만듭니다.");

                if (prepared.AddsPoison)
                    builder.AppendLine($"- {prepared.Ingredient.DisplayName}: 이 손질은 독을 추가합니다.");

                if (string.IsNullOrWhiteSpace(prepared.ResultNameModifier) == false)
                    builder.AppendLine($"- {prepared.Ingredient.DisplayName}: 이름 수식어 \"{prepared.ResultNameModifier}\"가 붙습니다.");

            }

            return builder.ToString();
        }

        private string BuildResultText(DishResult result)
        {
            if (result == null)
                return "요리 결과가 없습니다.";

            StringBuilder builder = new StringBuilder();
            builder.AppendLine(result.DisplayName);
            builder.AppendLine($"품질: {BuildQualityText(result.Quality)}");
            builder.AppendLine($"괴식: {(result.IsDisgusting ? "예" : "아니오")}");
            builder.AppendLine($"레시피 매칭: {(result.IsRecipeMatched ? "성공" : "실패")}");
            builder.AppendLine($"레시피: {(result.BaseRecipe != null ? $"{result.BaseRecipe.DisplayName} ({result.RecipeId})" : "없음")}");
            builder.AppendLine($"카테고리: {(result.Category != null ? $"{result.Category.DisplayName} ({result.CategoryId})" : "없음")}");
            builder.AppendLine($"태그 ID: {result.BuildTagText()}");
            return builder.ToString();
        }

        private string BuildResultPreparationText(DishResult result)
        {
            if (result == null || result.PreparedIngredients.Count == 0)
                return "손질 이력이 없습니다.";

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < result.PreparedIngredients.Count; i++)
            {
                PreparedIngredientState prepared = result.PreparedIngredients[i];
                if (prepared == null)
                    continue;

                builder.AppendLine($"- {prepared.Ingredient.DisplayName}");
                builder.AppendLine($"  손질: {(prepared.Method != null ? $"{prepared.Method.DisplayName} ({prepared.Method.MethodId})" : "없음")}");

                string effects = BuildPreparedEffectText(prepared);
                if (string.IsNullOrWhiteSpace(effects) == false)
                    builder.AppendLine($"  효과: {effects}");
            }

            return builder.ToString();
        }

        private static string BuildReasonText(DishResult result)
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < result.Reasons.Count; i++)
                builder.AppendLine($"- {result.Reasons[i]}");

            return builder.ToString();
        }

        private string BuildNpcMatchText(DishResult result)
        {
            if (result == null)
                return "요리 결과가 없어 NPC 요청과 비교할 수 없습니다.";

            if (npcRunner == null)
                npcRunner = FindFirstObjectByType<NpcConversationRunner>();

            if (npcRunner == null)
                return "현재 씬에서 NpcConversationRunner를 찾지 못했습니다.\nNPC 대화 UI와 연결하면 현재 캐릭터의 요청 일치도를 표시할 수 있습니다.";

            if (CookingNpcDishAdapter.TryBuildMatchReport(npcRunner, result, out NpcDishMatchReport report) == false)
                return "현재 NPC 주문이 아직 준비되지 않았습니다.\nNPC 대화에서 요리 단계까지 진행한 뒤 다시 확인하세요.";

            int percent = Mathf.RoundToInt(report.MatchRatio * 100f);
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"현재 NPC: {ValueOrNone(report.Order.NpcId)}");
            builder.AppendLine($"판정 예상: {BuildNpcResultText(report.Evaluation.Result)}");
            builder.AppendLine($"요청 일치도: {report.MatchScore}/{report.MaxMatchScore} ({percent}%)");
            builder.AppendLine($"레시피: {BuildMatchStateText(report.RecipeMatches)}  목표 {ValueOrNone(report.Order.CorrectRecipeId)} / 제출 {ValueOrNone(report.Dish.RecipeId)}");
            builder.AppendLine($"분류: {BuildMatchStateText(report.FoodTypeMatches)}  목표 {BuildStringListText(report.Order.AllowedFoodTypes)} / 제출 {ValueOrNone(report.Dish.FoodType)}");
            builder.AppendLine($"필수 태그: 맞음 {BuildStringListText(report.MatchedRequiredTags)} / 부족 {BuildStringListText(report.MissingRequiredTags)}");
            builder.AppendLine($"선호 태그: 맞음 {BuildStringListText(report.MatchedPreferredTags)} / 남음 {BuildStringListText(report.MissingPreferredTags)}");

            if (report.MatchedAvoidTags.Count > 0)
                builder.AppendLine($"회피 태그 감지: {BuildStringListText(report.MatchedAvoidTags)}");

            if (report.Dish.IsDisgusting || report.MatchedDisgustingTags.Count > 0)
            {
                string tags = report.MatchedDisgustingTags.Count > 0
                    ? BuildStringListText(report.MatchedDisgustingTags)
                    : "요리 결과가 괴식으로 표시됨";
                builder.AppendLine($"괴식 위험: {tags}");
            }

            builder.AppendLine($"판정 사유: {report.Evaluation.Reason}");
            return builder.ToString();
        }

        private static string BuildNpcResultText(NpcConversationResult result)
        {
            switch (result)
            {
                case NpcConversationResult.Perfect:
                    return "완전 일치";
                case NpcConversationResult.Correct:
                    return "요청 충족";
                case NpcConversationResult.Similar:
                    return "일부 일치";
                case NpcConversationResult.Disgusting:
                    return "괴식";
                case NpcConversationResult.Wrong:
                default:
                    return "불일치";
            }
        }

        private static string BuildMatchStateText(bool isMatched)
        {
            return isMatched ? "일치" : "불일치";
        }

        private static string BuildStringListText(IReadOnlyList<string> values)
        {
            return values != null && values.Count > 0 ? string.Join("|", values) : "없음";
        }

        private static string ValueOrNone(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "없음" : value;
        }

        private static string BuildPreparationEffectText(IngredientPreparationOption option)
        {
            if (option == null)
                return "효과: 없음";

            List<string> facts = new List<string>();
            if (option.QualityDelta != 0)
                facts.Add($"품질 {option.QualityDelta:+#;-#;0}");
            if (option.AddTags.Count > 0)
                facts.Add($"추가 태그 {BuildTagDisplayText(option.AddTags)}");
            if (option.RemoveTags.Count > 0)
                facts.Add($"제거 태그 {BuildTagDisplayText(option.RemoveTags)}");
            if (string.IsNullOrWhiteSpace(option.ResultNameModifier) == false)
                facts.Add($"이름 \"{option.ResultNameModifier}\"");
            if (option.CausesDisgusting)
                facts.Add("괴식");
            if (option.AddsPoison)
                facts.Add("독");

            return facts.Count > 0 ? $"효과: {string.Join(" / ", facts)}" : "효과: 없음";
        }

        private static string BuildPreparedEffectText(PreparedIngredientState prepared)
        {
            if (prepared == null)
                return string.Empty;

            List<string> facts = new List<string>();
            if (prepared.QualityDelta != 0)
                facts.Add($"품질 {prepared.QualityDelta:+#;-#;0}");
            if (prepared.AddTags.Count > 0)
                facts.Add($"추가 {BuildTagDisplayText(prepared.AddTags)}");
            if (prepared.RemoveTags.Count > 0)
                facts.Add($"제거 {BuildTagDisplayText(prepared.RemoveTags)}");
            if (string.IsNullOrWhiteSpace(prepared.ResultNameModifier) == false)
                facts.Add($"이름 \"{prepared.ResultNameModifier}\"");
            if (prepared.CausesDisgusting)
                facts.Add("괴식");
            if (prepared.AddsPoison)
                facts.Add("독");

            return string.Join(" / ", facts);
        }

        private static string BuildTagDisplayText(IReadOnlyList<FoodTagSO> tags)
        {
            if (tags == null || tags.Count == 0)
                return "없음";

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < tags.Count; i++)
            {
                FoodTagSO tag = tags[i];
                if (tag == null)
                    continue;

                if (builder.Length > 0)
                    builder.Append(", ");

                builder.Append(tag.DisplayName);
                if (string.IsNullOrWhiteSpace(tag.TagId) == false)
                    builder.Append($"({tag.TagId})");
            }

            return builder.Length > 0 ? builder.ToString() : "없음";
        }

        private static string BuildQualityText(DishQuality quality)
        {
            switch (quality)
            {
                case DishQuality.Perfect:
                    return "완벽";
                case DishQuality.Altered:
                    return "변형";
                case DishQuality.Disgusting:
                    return "괴식";
                case DishQuality.Normal:
                default:
                    return "일반";
            }
        }

        private void SetTitle(string text)
        {
            if (_titleText != null)
                _titleText.text = text;
        }

        private void ClearContent()
        {
            if (_contentRoot == null)
                return;

            for (int i = _contentRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = _contentRoot.GetChild(i);
                child.gameObject.SetActive(false);

                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }

        private Canvas CreateCanvas()
        {
            GameObject canvasObject = new GameObject("CookingTestCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        private RectTransform CreatePanel(Transform parent)
        {
            GameObject panelObject = new GameObject("CookingTestPanel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            panelObject.transform.SetParent(parent, false);

            RectTransform rect = panelObject.transform as RectTransform;
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-18f, -62f);
            rect.sizeDelta = GetEffectivePanelSize();

            Image image = panelObject.GetComponent<Image>();
            image.color = new Color(0.08f, 0.09f, 0.11f, 0.96f);

            VerticalLayoutGroup layout = panelObject.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 14, 14);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            return rect;
        }

        private RectTransform CreateScrollContent(Transform parent)
        {
            GameObject scrollObject = new GameObject("ScrollView", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(LayoutElement));
            scrollObject.transform.SetParent(parent, false);

            Image scrollImage = scrollObject.GetComponent<Image>();
            scrollImage.color = new Color(0f, 0f, 0f, 0f);
            scrollImage.raycastTarget = true;

            LayoutElement scrollLayout = scrollObject.GetComponent<LayoutElement>();
            scrollLayout.flexibleWidth = 1f;
            scrollLayout.flexibleHeight = 1f;

            GameObject viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            viewportObject.transform.SetParent(scrollObject.transform, false);

            RectTransform viewportRect = viewportObject.transform as RectTransform;
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = new Vector2(-12f, 0f);

            Image viewportImage = viewportObject.GetComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.001f);
            viewportImage.raycastTarget = true;

            Scrollbar scrollbar = CreateVerticalScrollbar(scrollObject.transform);

            GameObject contentObject = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentObject.transform.SetParent(viewportObject.transform, false);

            RectTransform contentRect = contentObject.transform as RectTransform;
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;

            VerticalLayoutGroup contentLayout = contentObject.GetComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(0, 8, 0, 8);
            contentLayout.spacing = 8f;
            contentLayout.childAlignment = TextAnchor.UpperLeft;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = scrollObject.GetComponent<ScrollRect>();
            scroll.viewport = viewportRect;
            scroll.content = contentRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 32f;
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

            return contentRect;
        }

        private Vector2 GetEffectivePanelSize()
        {
            float width = Mathf.Clamp(panelSize.x, 540f, 680f);
            float height = Mathf.Clamp(panelSize.y, 560f, 860f);
            return new Vector2(width, height);
        }

        private Scrollbar CreateVerticalScrollbar(Transform parent)
        {
            GameObject scrollbarObject = new GameObject("VerticalScrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
            scrollbarObject.transform.SetParent(parent, false);

            RectTransform scrollbarRect = scrollbarObject.transform as RectTransform;
            scrollbarRect.anchorMin = new Vector2(1f, 0f);
            scrollbarRect.anchorMax = new Vector2(1f, 1f);
            scrollbarRect.pivot = new Vector2(1f, 0.5f);
            scrollbarRect.sizeDelta = new Vector2(10f, 0f);
            scrollbarRect.anchoredPosition = Vector2.zero;

            Image background = scrollbarObject.GetComponent<Image>();
            background.color = new Color(0.04f, 0.05f, 0.07f, 0.85f);

            GameObject slidingAreaObject = new GameObject("Sliding Area", typeof(RectTransform));
            slidingAreaObject.transform.SetParent(scrollbarObject.transform, false);
            RectTransform slidingArea = slidingAreaObject.transform as RectTransform;
            slidingArea.anchorMin = Vector2.zero;
            slidingArea.anchorMax = Vector2.one;
            slidingArea.offsetMin = new Vector2(1f, 1f);
            slidingArea.offsetMax = new Vector2(-1f, -1f);

            GameObject handleObject = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handleObject.transform.SetParent(slidingAreaObject.transform, false);
            RectTransform handleRect = handleObject.transform as RectTransform;
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.one;
            handleRect.offsetMin = Vector2.zero;
            handleRect.offsetMax = Vector2.zero;

            Image handleImage = handleObject.GetComponent<Image>();
            handleImage.color = new Color(0.36f, 0.46f, 0.60f, 1f);

            Scrollbar scrollbar = scrollbarObject.GetComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.handleRect = handleRect;
            scrollbar.targetGraphic = handleImage;
            scrollbar.size = 0.2f;

            ColorBlock colors = scrollbar.colors;
            colors.normalColor = handleImage.color;
            colors.highlightedColor = new Color(0.55f, 0.68f, 0.86f, 1f);
            colors.pressedColor = new Color(0.28f, 0.36f, 0.48f, 1f);
            colors.selectedColor = colors.highlightedColor;
            scrollbar.colors = colors;

            return scrollbar;
        }

        private RectTransform CreateRow(Transform parent, string name, float height)
        {
            GameObject rowObject = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            rowObject.transform.SetParent(parent, false);

            HorizontalLayoutGroup layout = rowObject.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            LayoutElement layoutElement = rowObject.GetComponent<LayoutElement>();
            layoutElement.preferredHeight = height;
            layoutElement.minHeight = height;
            layoutElement.flexibleWidth = 1f;

            return rowObject.transform as RectTransform;
        }

        private void MakeDragHandle(RectTransform handle, RectTransform target, Canvas canvas)
        {
            Image image = handle.gameObject.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0f);
            image.raycastTarget = true;

            CookingTestDragHandle dragHandle = handle.gameObject.AddComponent<CookingTestDragHandle>();
            dragHandle.Initialize(target, canvas);
        }

        private void CreateSectionLabel(Transform parent, string text)
        {
            TextMeshProUGUI label = CreateText(parent, $"Section_{text}", text, 16f, TextAlignmentOptions.Left);
            label.color = new Color(0.78f, 0.86f, 1f, 1f);

            LayoutElement layoutElement = label.GetComponent<LayoutElement>();
            layoutElement.preferredHeight = 22f;
            layoutElement.minHeight = 22f;
        }

        private Button CreateButton(
            Transform parent,
            string label,
            UnityAction onClick,
            Vector2? size = null,
            float height = 34f,
            ButtonTone tone = ButtonTone.Default)
        {
            GameObject buttonObject = new GameObject($"Button_{SanitizeName(label)}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonObject.transform.SetParent(parent, false);

            Image image = buttonObject.GetComponent<Image>();
            image.color = GetButtonColor(tone);

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);

            ColorBlock colors = button.colors;
            colors.normalColor = image.color;
            colors.highlightedColor = GetButtonHighlightColor(tone);
            colors.pressedColor = new Color(0.12f, 0.16f, 0.22f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.16f, 0.16f, 0.17f, 0.62f);
            button.colors = colors;

            LayoutElement layoutElement = buttonObject.GetComponent<LayoutElement>();
            layoutElement.minHeight = size?.y ?? height;
            layoutElement.preferredHeight = size?.y ?? height;
            layoutElement.preferredWidth = size?.x ?? 0f;
            layoutElement.flexibleWidth = size.HasValue ? 0f : 1f;

            TextMeshProUGUI text = CreateText(buttonObject.transform, "Label", label, height > 48f ? 14f : 16f, TextAlignmentOptions.Center);
            text.textWrappingMode = height > 48f ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Truncate;

            RectTransform textRect = text.transform as RectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8f, 4f);
            textRect.offsetMax = new Vector2(-8f, -4f);
            DestroyComponent(text.GetComponent<LayoutElement>());

            return button;
        }

        private TextMeshProUGUI CreateInfoBox(Transform parent, string name, string text, float height, float fontSize)
        {
            TextMeshProUGUI box = CreateTextBox(parent, name, "Text", height, fontSize);
            box.text = text;
            return box;
        }

        private TextMeshProUGUI CreateTextBox(
            Transform parent,
            string boxName,
            string textName,
            float height,
            float fontSize)
        {
            GameObject boxObject = new GameObject(boxName, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            boxObject.transform.SetParent(parent, false);

            Image image = boxObject.GetComponent<Image>();
            image.color = new Color(0.04f, 0.05f, 0.07f, 0.96f);

            LayoutElement layoutElement = boxObject.GetComponent<LayoutElement>();
            layoutElement.minHeight = height;
            layoutElement.preferredHeight = height;
            layoutElement.flexibleWidth = 1f;

            TextMeshProUGUI text = CreateText(boxObject.transform, textName, string.Empty, fontSize, TextAlignmentOptions.TopLeft);
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Truncate;

            RectTransform textRect = text.transform as RectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10f, 8f);
            textRect.offsetMax = new Vector2(-10f, -8f);
            DestroyComponent(text.GetComponent<LayoutElement>());

            return text;
        }

        private TextMeshProUGUI CreateText(
            Transform parent,
            string name,
            string text,
            float fontSize,
            TextAlignmentOptions alignment)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            textObject.transform.SetParent(parent, false);

            TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.color = Color.white;
            label.alignment = alignment;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;

            if (fontAsset != null)
                label.font = fontAsset;

            LayoutElement layoutElement = textObject.GetComponent<LayoutElement>();
            layoutElement.minHeight = 24f;
            layoutElement.preferredHeight = 24f;
            layoutElement.flexibleWidth = 1f;

            return label;
        }

        private static Color GetButtonColor(ButtonTone tone)
        {
            switch (tone)
            {
                case ButtonTone.Primary:
                    return new Color(0.20f, 0.36f, 0.52f, 0.98f);
                case ButtonTone.Selected:
                    return new Color(0.22f, 0.44f, 0.32f, 0.98f);
                case ButtonTone.Warning:
                    return new Color(0.45f, 0.34f, 0.16f, 0.98f);
                case ButtonTone.Danger:
                    return new Color(0.48f, 0.20f, 0.22f, 0.98f);
                case ButtonTone.Default:
                default:
                    return new Color(0.19f, 0.24f, 0.31f, 0.98f);
            }
        }

        private static Color GetButtonHighlightColor(ButtonTone tone)
        {
            switch (tone)
            {
                case ButtonTone.Primary:
                    return new Color(0.28f, 0.48f, 0.68f, 1f);
                case ButtonTone.Selected:
                    return new Color(0.30f, 0.56f, 0.42f, 1f);
                case ButtonTone.Warning:
                    return new Color(0.60f, 0.45f, 0.22f, 1f);
                case ButtonTone.Danger:
                    return new Color(0.65f, 0.28f, 0.30f, 1f);
                case ButtonTone.Default:
                default:
                    return new Color(0.26f, 0.33f, 0.43f, 1f);
            }
        }

        private static void SetButtonLabel(Button button, string label)
        {
            TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (text != null)
                text.text = label;
        }

        private static string SanitizeName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Empty";

            return value.Replace('\n', ' ').Replace('\r', ' ');
        }

        private static void DestroyComponent(Component component)
        {
            if (component == null)
                return;

            if (Application.isPlaying)
                Destroy(component);
            else
                DestroyImmediate(component);
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
                return;

            GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
#else
            eventSystemObject.AddComponent<StandaloneInputModule>();
#endif
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

    public sealed class CookingTestDragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        private RectTransform _target;
        private Canvas _canvas;
        private Vector2 _dragOffset;

        public void Initialize(RectTransform target, Canvas canvas)
        {
            _target = target;
            _canvas = canvas;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_target == null)
                return;

            if (TryGetParentPoint(eventData, out Vector2 parentPoint))
                _dragOffset = _target.anchoredPosition - parentPoint;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_target == null)
                return;

            if (TryGetParentPoint(eventData, out Vector2 parentPoint))
                _target.anchoredPosition = parentPoint + _dragOffset;
        }

        private bool TryGetParentPoint(PointerEventData eventData, out Vector2 parentPoint)
        {
            parentPoint = Vector2.zero;
            if (_target == null || _target.parent == null)
                return false;

            Camera camera = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? _canvas.worldCamera
                : null;

            return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _target.parent as RectTransform,
                eventData.position,
                camera,
                out parentPoint);
        }
    }
}
