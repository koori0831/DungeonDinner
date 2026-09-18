using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Work.Cook.Code.Data;
using Work.Cook.Code.Runtime.Core;
using Work.Cook.Code.Runtime.Systems;
using Work.Cook.Code.Runtime.UI;

namespace Work.Cook.Code.Editor.PreviewLab
{
    /// <summary>
    /// 기존 런타임 공개 API만으로 Cooking UI를 구동하는 에디터 전용 드라이버.
    /// 미니게임의 즉시 판정처럼 공개 API가 없는 한 지점만 Editor Reflection을 사용한다.
    /// </summary>
    public static class CookingUiPreviewDriver
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        public static string LastMessage { get; private set; } = string.Empty;
        public static CookingGamePanel LastPanel { get; private set; }

        public static CookingGamePanel FindPanel()
        {
            CookingGamePanel[] panels = UnityEngine.Object.FindObjectsByType<CookingGamePanel>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            for (int i = 0; i < panels.Length; i++)
            {
                CookingGamePanel panel = panels[i];
                if (panel != null && panel.gameObject.scene.IsValid())
                    return panel;
            }

            return null;
        }

        public static bool Apply(
            CookingGamePanel panel,
            CookingUiPreviewScenario scenario,
            CookingUiPreviewScreen? screenOverride = null,
            bool logResult = true)
        {
            LastPanel = panel;
            if (panel == null)
                return Fail("CookingGamePanel을 찾지 못했습니다. CookTestScene을 Play Mode로 실행했는지 확인하세요.", null, logResult);
            if (scenario == null)
                return Fail("적용할 Cooking UI Preview Scenario가 없습니다.", panel, logResult);

            EnsurePreviewAudioListener(panel);

            CookingFlowRunner runner = panel.FlowRunner;
            if (runner == null)
                return Fail("Preview 대상 CookingGamePanel에 CookingFlowRunner가 연결되지 않았습니다.", panel, logResult);

            if (scenario.CatalogOverride != null && runner.Catalog != scenario.CatalogOverride)
                runner.SetCatalog(scenario.CatalogOverride);

            runner.ResetFlow();
            if (panel.OpenDirectIngredientSelection() == false)
                return Fail("재료 선택 화면을 열지 못했습니다.", panel, logResult);

            panel.SetIngredientSelectionLimits(scenario.MinimumSelection, scenario.MaximumSelection);

            List<CookingUiPreviewIngredientEntry> entries = CollectEntries(scenario, runner.Catalog);
            for (int i = 0; i < entries.Count; i++)
            {
                CookingUiPreviewIngredientEntry entry = entries[i];
                for (int quantity = 0; quantity < entry.Quantity; quantity++)
                    runner.AddDirectIngredient(entry.Ingredient);
            }

            panel.RefreshCookingViews();
            CookingUiPreviewScreen target = screenOverride ?? scenario.TargetScreen;
            switch (target)
            {
                case CookingUiPreviewScreen.IngredientSelection:
                    return Succeed($"재료 선택 프리뷰 적용 · 선택 {runner.SelectedIngredients.Count}개", panel, logResult);

                case CookingUiPreviewScreen.Preparation:
                    if (BeginPreparation(panel, runner, entries, logResult) == false)
                        return false;
                    return Succeed("손질 화면 프리뷰 적용", panel, logResult);

                case CookingUiPreviewScreen.MiniGame:
                    if (BeginPreparation(panel, runner, entries, logResult) == false)
                        return false;
                    return StartMiniGame(panel, runner, entries, logResult);

                case CookingUiPreviewScreen.Result:
                    return OpenResult(panel, runner, scenario, entries, logResult);

                default:
                    return Fail($"지원하지 않는 프리뷰 화면입니다: {target}", panel, logResult);
            }
        }

        public static bool ForceActiveMiniGameResult(
            CookingGamePanel panel,
            CookingUiPreviewScenario scenario,
            bool logResult = true)
        {
            LastPanel = panel;
            if (panel == null || scenario == null)
                return Fail("미니게임 판정에 필요한 Panel 또는 Scenario가 없습니다.", panel, logResult);
            if (panel.CurrentScreen != CookingGameScreenState.MiniGame)
                return Fail("현재 화면이 MiniGame이 아닙니다. 먼저 미니게임 프리뷰를 시작하세요.", panel, logResult);

            FieldInfo optionField = typeof(CookingGamePanel).GetField("_pendingMiniGameOption", PrivateInstance);
            IngredientPreparationOption option = optionField?.GetValue(panel) as IngredientPreparationOption;
            if (option == null || option.MiniGameType == CookingMiniGameType.None)
                return Fail("진행 중인 미니게임 손질 옵션을 찾지 못했습니다.", panel, logResult);

            CookingMiniGameRouterView router = panel.MiniGameView != null
                ? panel.MiniGameView.GetComponentInChildren<CookingMiniGameRouterView>(true)
                : null;
            if (router == null)
                return Fail("CookingMiniGameRouterView를 찾지 못했습니다.", panel, logResult);

            MethodInfo completionMethod = typeof(CookingMiniGameRouterView).GetMethod(
                "HandleControllerCompleted",
                PrivateInstance);
            if (completionMethod == null)
                return Fail("미니게임 완료 진입점을 찾지 못했습니다. Router 구현 변경 여부를 확인하세요.", router, logResult);

            CookingMiniGameResult forcedResult = CookingMiniGameUtility.CreateResult(
                option.MiniGameType,
                scenario.ForcedGrade,
                scenario.ForcedScore,
                scenario.ForcedFeedback);

            try
            {
                completionMethod.Invoke(router, new object[] { forcedResult });
            }
            catch (TargetInvocationException exception)
            {
                Exception cause = exception.InnerException ?? exception;
                return Fail($"미니게임 강제 판정 중 오류가 발생했습니다: {cause.Message}", router, logResult);
            }

            return Succeed(
                $"미니게임 판정 강제 · {option.MiniGameType} / {scenario.ForcedGrade} / {scenario.ForcedScore:0.00}",
                router,
                logResult);
        }

        private static bool BeginPreparation(
            CookingGamePanel panel,
            CookingFlowRunner runner,
            IReadOnlyList<CookingUiPreviewIngredientEntry> entries,
            bool logResult)
        {
            if (entries == null || entries.Count == 0 || runner.SelectedIngredients.Count == 0)
                return Fail("손질 프리뷰에는 재료가 최소 한 개 필요합니다.", panel, logResult);
            if (runner.ConfirmDirectIngredients() == false)
                return Fail("프리뷰 재료 선택을 확정하지 못했습니다.", panel, logResult);

            panel.OpenPreparation();
            panel.RefreshCookingViews();
            return true;
        }

        private static void EnsurePreviewAudioListener(CookingGamePanel panel)
        {
            AudioListener panelListener = panel.GetComponent<AudioListener>();
            if (panelListener != null)
            {
                panelListener.enabled = true;
                return;
            }

            if (UnityEngine.Object.FindFirstObjectByType<AudioListener>() != null)
                return;

            AudioListener listener = panel.gameObject.AddComponent<AudioListener>();
            if (listener != null)
                listener.hideFlags = HideFlags.DontSave;
        }

        private static bool StartMiniGame(
            CookingGamePanel panel,
            CookingFlowRunner runner,
            IReadOnlyList<CookingUiPreviewIngredientEntry> entries,
            bool logResult)
        {
            IngredientSO ingredient = runner.GetNextUnpreparedIngredient();
            IngredientPreparationOption option = ResolvePreparationOption(ingredient, entries);
            if (ingredient == null)
                return Fail("미니게임을 시작할 재료를 찾지 못했습니다.", panel, logResult);
            if (option == null)
                return Fail($"{ingredient.DisplayName}에 프리뷰할 손질 옵션이 없습니다.", ingredient, logResult);
            if (option.MiniGameType == CookingMiniGameType.None)
                return Fail($"{option.DisplayName} 손질법에는 미니게임이 연결되어 있지 않습니다.", ingredient, logResult);
            if (panel.SelectPreparation(ingredient, option) == false)
                return Fail($"{option.DisplayName} 미니게임을 시작하지 못했습니다.", panel, logResult);

            return Succeed($"미니게임 프리뷰 시작 · {option.MiniGameType}", panel, logResult);
        }

        private static bool OpenResult(
            CookingGamePanel panel,
            CookingFlowRunner runner,
            CookingUiPreviewScenario scenario,
            IReadOnlyList<CookingUiPreviewIngredientEntry> entries,
            bool logResult)
        {
            if (BeginPreparation(panel, runner, entries, logResult) == false)
                return false;

            int safety = Mathf.Max(8, runner.SelectedIngredients.Count * 2);
            while (runner.GetNextUnpreparedIngredient() != null && safety-- > 0)
            {
                IngredientSO ingredient = runner.GetNextUnpreparedIngredient();
                IngredientPreparationOption option = ResolvePreparationOption(ingredient, entries);
                CookingMiniGameResult miniGameResult = option != null && option.MiniGameType != CookingMiniGameType.None
                    ? CookingMiniGameUtility.CreateResult(
                        option.MiniGameType,
                        scenario.ForcedGrade,
                        scenario.ForcedScore,
                        scenario.ForcedFeedback)
                    : null;

                if (runner.SelectPreparation(ingredient, option, miniGameResult) == false)
                    return Fail($"{ingredient.DisplayName}의 프리뷰 손질 결과를 만들지 못했습니다.", panel, logResult);
            }

            if (runner.GetNextUnpreparedIngredient() != null)
                return Fail("프리뷰 손질 반복이 안전 한도를 초과했습니다.", panel, logResult);
            if (runner.TryPreviewCookingResult(out DishResult result) == false || result == null)
                return Fail("프리뷰 DishResult를 생성하지 못했습니다.", panel, logResult);
            if (panel.OpenResult(result) == false)
                return Fail("결과 화면을 열지 못했습니다.", panel, logResult);

            return Succeed($"결과 프리뷰 적용 · {result.DisplayName} / {result.CraftGrade}", panel, logResult);
        }

        private static List<CookingUiPreviewIngredientEntry> CollectEntries(
            CookingUiPreviewScenario scenario,
            CookingDataCatalogSO catalog)
        {
            List<CookingUiPreviewIngredientEntry> values = new List<CookingUiPreviewIngredientEntry>();
            IReadOnlyList<CookingUiPreviewIngredientEntry> source = scenario?.Ingredients;
            if (source != null)
            {
                for (int i = 0; i < source.Count; i++)
                {
                    CookingUiPreviewIngredientEntry entry = source[i];
                    if (entry != null && entry.Ingredient != null)
                        values.Add(entry);
                }
            }

            if (values.Count > 0 || catalog?.Ingredients == null)
                return values;

            // 시나리오가 비어 있을 때 임의 객체를 만들지 않는다. 사용자가 어떤 재료가
            // 선택되는지 명확히 볼 수 있도록 에디터 창에서 안내한다.
            return values;
        }

        private static IngredientPreparationOption ResolvePreparationOption(
            IngredientSO ingredient,
            IReadOnlyList<CookingUiPreviewIngredientEntry> entries)
        {
            if (ingredient == null || ingredient.PreparationOptions == null || ingredient.PreparationOptions.Count == 0)
                return null;

            int optionIndex = 0;
            if (entries != null)
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    CookingUiPreviewIngredientEntry entry = entries[i];
                    if (entry != null && entry.Ingredient == ingredient)
                    {
                        optionIndex = entry.PreparationOptionIndex;
                        break;
                    }
                }
            }

            optionIndex = Mathf.Clamp(optionIndex, 0, ingredient.PreparationOptions.Count - 1);
            return ingredient.PreparationOptions[optionIndex];
        }

        private static bool Succeed(string message, UnityEngine.Object context, bool logResult)
        {
            LastMessage = message;
            if (logResult)
                Debug.Log($"[Cooking UI Preview] {message}", context);
            return true;
        }

        private static bool Fail(string message, UnityEngine.Object context, bool logResult)
        {
            LastMessage = message;
            if (logResult)
                Debug.LogWarning($"[Cooking UI Preview] {message}", context);
            return false;
        }
    }
}
