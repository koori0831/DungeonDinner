using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Work.Cook.Code.Data;

namespace Work.Cook.Code.Editor
{
    public sealed partial class CookingDataEditorWindow
    {
        private void DrawRecipeForm()
        {
            if (_recipeDraft == null)
                return;

            _recipeDraft.Priority = EditorGUILayout.IntField("매칭 우선순위", _recipeDraft.Priority);

            EditorGUILayout.LabelField("기본 정보", EditorStyles.boldLabel);
            _recipeDraft.RecipeId = EditorGUILayout.TextField("레시피 ID", _recipeDraft.RecipeId);
            _recipeDraft.DisplayName = EditorGUILayout.TextField("표시 이름", _recipeDraft.DisplayName);
            _recipeDraft.IconSprite = (Sprite)EditorGUILayout.ObjectField("완성 요리 아이콘", _recipeDraft.IconSprite, typeof(Sprite), false);
            _recipeDraft.Category = (FoodCategorySO)EditorGUILayout.ObjectField("카테고리", _recipeDraft.Category, typeof(FoodCategorySO), false);

            EditorGUILayout.LabelField("설명");
            _recipeDraft.Description = EditorGUILayout.TextArea(_recipeDraft.Description, GUILayout.MinHeight(54f));
            EditorGUILayout.Space(8f);

            if (DrawObjectList("기본 태그", _recipeDraft.BaseTags, typeof(FoodTagSO), "+ 태그 추가"))
                MarkDraftDirty();

            DrawRequiredIngredients();
            DrawWarnings(BuildRecipeWarnings());
        }

        private void DrawCategoryForm()
        {
            if (_categoryDraft == null)
                return;

            EditorGUILayout.LabelField("카테고리 정보", EditorStyles.boldLabel);
            _categoryDraft.CategoryId = EditorGUILayout.TextField("카테고리 ID", _categoryDraft.CategoryId);
            _categoryDraft.DisplayName = EditorGUILayout.TextField("표시 이름", _categoryDraft.DisplayName);
            _categoryDraft.Icon = (Sprite)EditorGUILayout.ObjectField("책갈피 아이콘", _categoryDraft.Icon, typeof(Sprite), false);
            EditorGUILayout.LabelField("설명");
            _categoryDraft.Description = EditorGUILayout.TextArea(_categoryDraft.Description, GUILayout.MinHeight(80f));
            EditorGUILayout.HelpBox("카테고리는 음식의 큰 분류입니다. 예: 찌개, 구이, 디저트, 괴식.", MessageType.None);
            DrawWarnings(BuildCategoryWarnings());
        }

        private void DrawIngredientCategoryForm()
        {
            if (_ingredientCategoryDraft == null)
                return;

            EditorGUILayout.LabelField("재료군 정보", EditorStyles.boldLabel);
            _ingredientCategoryDraft.CategoryId = EditorGUILayout.TextField("재료군 ID", _ingredientCategoryDraft.CategoryId);
            _ingredientCategoryDraft.DisplayName = EditorGUILayout.TextField("표시 이름", _ingredientCategoryDraft.DisplayName);
            _ingredientCategoryDraft.Icon = (Sprite)EditorGUILayout.ObjectField("아이콘", _ingredientCategoryDraft.Icon, typeof(Sprite), false);
            EditorGUILayout.LabelField("설명");
            _ingredientCategoryDraft.Description = EditorGUILayout.TextArea(_ingredientCategoryDraft.Description, GUILayout.MinHeight(80f));
            EditorGUILayout.HelpBox("고기, 채소, 향신료처럼 레시피 슬롯에서 대체 가능한 큰 재료 묶음을 정의합니다.", MessageType.None);
            DrawWarnings(BuildIngredientCategoryWarnings());
        }

        private void DrawTagForm()
        {
            if (_tagDraft == null)
                return;

            EditorGUILayout.LabelField("태그 정보", EditorStyles.boldLabel);
            _tagDraft.TagId = EditorGUILayout.TextField("태그 ID", _tagDraft.TagId);
            _tagDraft.DisplayName = EditorGUILayout.TextField("표시 이름", _tagDraft.DisplayName);
            EditorGUILayout.LabelField("설명");
            _tagDraft.Description = EditorGUILayout.TextArea(_tagDraft.Description, GUILayout.MinHeight(80f));
            EditorGUILayout.HelpBox("태그는 맛/온도/식감/위험 속성입니다. 예: spicy, sweet, poisonous, hot.", MessageType.None);
            DrawWarnings(BuildTagWarnings());
        }

        private void DrawMethodForm()
        {
            if (_methodDraft == null)
                return;

            EditorGUILayout.LabelField("손질법 정보", EditorStyles.boldLabel);
            _methodDraft.MethodId = EditorGUILayout.TextField("손질법 ID", _methodDraft.MethodId);
            _methodDraft.DisplayName = EditorGUILayout.TextField("표시 이름", _methodDraft.DisplayName);
            _methodDraft.IconSprite = (Sprite)EditorGUILayout.ObjectField("카드 아이콘", _methodDraft.IconSprite, typeof(Sprite), false);
            EditorGUILayout.LabelField("설명");
            _methodDraft.Description = EditorGUILayout.TextArea(_methodDraft.Description, GUILayout.MinHeight(80f));
            EditorGUILayout.HelpBox("손질법 자체는 선택지 이름입니다. 이 손질법이 태그를 추가하거나 괴식을 만드는 효과는 재료 탭의 '손질법별 효과'에서 설정합니다.", MessageType.None);
            DrawWarnings(BuildMethodWarnings());
        }

        private void DrawIngredientForm()
        {
            if (_ingredientDraft == null)
                return;

            _ingredientDraft.Category = (IngredientCategorySO)EditorGUILayout.ObjectField("재료군", _ingredientDraft.Category, typeof(IngredientCategorySO), false);

            EditorGUILayout.LabelField("재료 정보", EditorStyles.boldLabel);
            _ingredientDraft.IngredientId = EditorGUILayout.TextField("재료 ID", _ingredientDraft.IngredientId);
            _ingredientDraft.DisplayName = EditorGUILayout.TextField("표시 이름", _ingredientDraft.DisplayName);
            _ingredientDraft.IconSprite = (Sprite)EditorGUILayout.ObjectField("재료 아이콘", _ingredientDraft.IconSprite, typeof(Sprite), false);
            _ingredientDraft.ModelPrefab = (GameObject)EditorGUILayout.ObjectField("3D 모델 프리팹", _ingredientDraft.ModelPrefab, typeof(GameObject), false);
            EditorGUILayout.LabelField("설명");
            _ingredientDraft.Description = EditorGUILayout.TextArea(_ingredientDraft.Description, GUILayout.MinHeight(64f));
            EditorGUILayout.Space(8f);

            if (DrawObjectList("재료 기본 태그", _ingredientDraft.BaseTags, typeof(FoodTagSO), "+ 기본 태그 추가"))
                MarkDraftDirty();

            DrawPreparationOptions();
            DrawWarnings(BuildIngredientWarnings());
        }

        private void DrawRequiredIngredients()
        {
            EditorGUILayout.LabelField("필요 재료", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("직접 선택한 재료가 이 목록과 매칭되면 이 레시피 음식으로 판정됩니다.", MessageType.None);

            for (int i = 0; i < _recipeDraft.RequiredIngredients.Count; i++)
            {
                IngredientRequirementDraft requirement = _recipeDraft.RequiredIngredients[i];
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"필요 재료 {i + 1}", EditorStyles.boldLabel);
                if (GUILayout.Button("삭제", GUILayout.Width(52f)))
                {
                    _recipeDraft.RequiredIngredients.RemoveAt(i);
                    MarkDraftDirty();
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    GUIUtility.ExitGUI();
                }

                EditorGUILayout.EndHorizontal();
                requirement.RequirementId = EditorGUILayout.TextField("슬롯 ID", requirement.RequirementId);
                requirement.Ingredient = (IngredientSO)EditorGUILayout.ObjectField("기준 재료", requirement.Ingredient, typeof(IngredientSO), false);
                requirement.IngredientCategory = (IngredientCategorySO)EditorGUILayout.ObjectField("재료군 조건", requirement.IngredientCategory, typeof(IngredientCategorySO), false);
                requirement.MinCount = Mathf.Max(0, EditorGUILayout.IntField("최소 개수", requirement.MinCount));
                requirement.MaxCount = Mathf.Max(0, EditorGUILayout.IntField("최대 개수 (0 = 제한 없음)", requirement.MaxCount));
                requirement.RecipeDefining = EditorGUILayout.Toggle("요리 결정 조건", requirement.RecipeDefining);

                if (DrawObjectList("필수 태그", requirement.RequiredTags, typeof(FoodTagSO), "+ 필수 태그"))
                    MarkDraftDirty();

                if (DrawObjectList("단순 대체 재료", requirement.SimpleAlternatives, typeof(IngredientSO), "+ 대체 재료"))
                    MarkDraftDirty();

                if (DrawAlternativeList(requirement.Alternatives))
                    MarkDraftDirty();

                if (DrawLimitedObjectList("필수 손질법", requirement.RequiredPreparationMethods, typeof(PreparationMethodSO), "+ 필수 손질법 추가", "필수 손질법 없음", 2))
                    MarkDraftDirty();

                bool usePreparationModifier = EditorGUILayout.Toggle("손질 수식어 반영", requirement.UsePreparationResultNameModifier);
                if (usePreparationModifier != requirement.UsePreparationResultNameModifier)
                {
                    requirement.UsePreparationResultNameModifier = usePreparationModifier;
                    MarkDraftDirty();
                }
                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("+ 필요 재료 추가"))
            {
                _recipeDraft.RequiredIngredients.Add(new IngredientRequirementDraft());
                MarkDraftDirty();
            }

            EditorGUILayout.Space(8f);
        }

        private static bool DrawAlternativeList(List<IngredientAlternativeDraft> alternatives)
        {
            bool changed = false;
            EditorGUILayout.LabelField("대체 재료", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("대체 재료를 사용했을 때 완성 음식 이름 앞에 붙일 수식어를 지정합니다. 예: 참치, 버섯, 고급.", MessageType.None);

            for (int i = 0; i < alternatives.Count; i++)
            {
                IngredientAlternativeDraft alternative = alternatives[i] ?? new IngredientAlternativeDraft();
                alternatives[i] = alternative;

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"대체 재료 {i + 1}", EditorStyles.boldLabel);
                if (GUILayout.Button("삭제", GUILayout.Width(52f)))
                {
                    alternatives.RemoveAt(i);
                    changed = true;
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }

                EditorGUILayout.EndHorizontal();

                IngredientSO ingredient = (IngredientSO)EditorGUILayout.ObjectField("재료", alternative.Ingredient, typeof(IngredientSO), false);
                if (ingredient != alternative.Ingredient)
                {
                    alternative.Ingredient = ingredient;
                    changed = true;
                }

                string modifier = EditorGUILayout.TextField("이름 수식어", alternative.ResultNameModifier);
                if (modifier != alternative.ResultNameModifier)
                {
                    alternative.ResultNameModifier = modifier;
                    changed = true;
                }

                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("+ 대체 재료 추가"))
            {
                alternatives.Add(new IngredientAlternativeDraft());
                changed = true;
            }

            EditorGUILayout.Space(8f);
            return changed;
        }

        private static bool DrawLimitedObjectList(
            string label,
            IList list,
            Type objectType,
            string addButtonLabel,
            string emptyMessage,
            int maxCount)
        {
            bool changed = false;
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

            if (list == null)
                return false;

            if (list.Count == 0)
                EditorGUILayout.HelpBox(emptyMessage, MessageType.None);

            for (int i = 0; i < list.Count; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"{label} {i + 1}", GUILayout.Width(92f));
                    UnityEngine.Object current = list[i] as UnityEngine.Object;
                    UnityEngine.Object next = EditorGUILayout.ObjectField(current, objectType, false);
                    if (next != current)
                    {
                        list[i] = next;
                        changed = true;
                    }

                    if (GUILayout.Button("삭제", GUILayout.Width(52f)))
                    {
                        list.RemoveAt(i);
                        changed = true;
                        break;
                    }
                }
            }

            if (list.Count < maxCount && GUILayout.Button(addButtonLabel))
            {
                list.Add(null);
                changed = true;
            }

            return changed;
        }

        private void DrawPerfectRules()
        {
            EditorGUILayout.LabelField("정석 손질 조건", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("완벽한 음식 판정을 위해 각 재료가 선택해야 하는 손질법입니다.", MessageType.None);

            for (int i = 0; i < _recipeDraft.PerfectRules.Count; i++)
            {
                PerfectRuleDraft rule = _recipeDraft.PerfectRules[i];
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"정석 조건 {i + 1}", EditorStyles.boldLabel);
                if (GUILayout.Button("삭제", GUILayout.Width(52f)))
                {
                    _recipeDraft.PerfectRules.RemoveAt(i);
                    MarkDraftDirty();
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    GUIUtility.ExitGUI();
                }

                EditorGUILayout.EndHorizontal();
                rule.Ingredient = (IngredientSO)EditorGUILayout.ObjectField("재료", rule.Ingredient, typeof(IngredientSO), false);
                rule.PerfectMethod = (PreparationMethodSO)EditorGUILayout.ObjectField("정석 손질법", rule.PerfectMethod, typeof(PreparationMethodSO), false);
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ 정석 조건 추가"))
            {
                _recipeDraft.PerfectRules.Add(new PerfectRuleDraft());
                MarkDraftDirty();
            }

            if (GUILayout.Button("필요 재료를 정석 조건에 추가"))
            {
                AddMissingPerfectRulesFromRequirements();
                MarkDraftDirty();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(8f);
        }

        private void DrawPreparationOptions()
        {
            EditorGUILayout.LabelField("손질법별 효과", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("이 재료에서 플레이어가 고를 수 있는 손질법과, 그 손질법이 요리 결과에 추가/제거할 태그 및 위험 효과를 설정합니다.", MessageType.Info);

            for (int i = 0; i < _ingredientDraft.PreparationOptions.Count; i++)
            {
                PreparationOptionDraft option = _ingredientDraft.PreparationOptions[i];
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"손질 선택지 {i + 1}", EditorStyles.boldLabel);
                if (GUILayout.Button("삭제", GUILayout.Width(52f)))
                {
                    _ingredientDraft.PreparationOptions.RemoveAt(i);
                    MarkDraftDirty();
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    GUIUtility.ExitGUI();
                }

                EditorGUILayout.EndHorizontal();
                option.PreparationOptionId = EditorGUILayout.TextField("손질 옵션 ID", option.PreparationOptionId);
                option.Method = (PreparationMethodSO)EditorGUILayout.ObjectField("손질법", option.Method, typeof(PreparationMethodSO), false);
                option.DisplayNameOverride = EditorGUILayout.TextField("표시 이름 덮어쓰기", option.DisplayNameOverride);
                EditorGUILayout.LabelField("설명");
                option.Description = EditorGUILayout.TextArea(option.Description, GUILayout.MinHeight(48f));

                if (DrawObjectList("요리에 추가할 태그", option.AddTags, typeof(FoodTagSO), "+ 추가 태그"))
                    MarkDraftDirty();
                if (DrawObjectList("요리에서 제거할 태그", option.RemoveTags, typeof(FoodTagSO), "+ 제거 태그"))
                    MarkDraftDirty();

                option.QualityDelta = EditorGUILayout.IntField("품질 변화", option.QualityDelta);
                option.CausesDisgusting = EditorGUILayout.Toggle("괴식으로 만듦", option.CausesDisgusting);
                option.AddsPoison = EditorGUILayout.Toggle("독 속성 추가", option.AddsPoison);
                option.ResultNameModifier = EditorGUILayout.TextField("결과 이름 수식어", option.ResultNameModifier);

                EditorGUILayout.LabelField("미니게임 등급 효과", EditorStyles.boldLabel);
                for (int ruleIndex = 0; ruleIndex < option.MiniGameFeedbackRules.Count; ruleIndex++)
                {
                    MiniGameFeedbackRuleDraft rule = option.MiniGameFeedbackRules[ruleIndex];
                    EditorGUILayout.BeginVertical("box");
                    EditorGUILayout.BeginHorizontal();
                    rule.Grade = (CookingMiniGameGrade)EditorGUILayout.EnumPopup("등급", rule.Grade);
                    if (GUILayout.Button("삭제", GUILayout.Width(52f)))
                    {
                        option.MiniGameFeedbackRules.RemoveAt(ruleIndex);
                        MarkDraftDirty();
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndVertical();
                        GUIUtility.ExitGUI();
                    }
                    EditorGUILayout.EndHorizontal();
                    rule.VariantEffectId = EditorGUILayout.TextField("변형 효과 ID", rule.VariantEffectId);
                    rule.QualityDelta = EditorGUILayout.IntField("품질 변화", rule.QualityDelta);
                    if (DrawObjectList("추가 태그", rule.AddTags, typeof(FoodTagSO), "+ 추가 태그"))
                        MarkDraftDirty();
                    if (DrawObjectList("제거 태그", rule.RemoveTags, typeof(FoodTagSO), "+ 제거 태그"))
                        MarkDraftDirty();
                    rule.ResultNameModifier = EditorGUILayout.TextField("결과 이름 수식어", rule.ResultNameModifier);
                    EditorGUILayout.LabelField("피드백 문구");
                    rule.FeedbackText = EditorGUILayout.TextArea(rule.FeedbackText, GUILayout.MinHeight(36f));
                    EditorGUILayout.EndVertical();
                }
                if (GUILayout.Button("+ 미니게임 등급 효과"))
                {
                    option.MiniGameFeedbackRules.Add(new MiniGameFeedbackRuleDraft());
                    MarkDraftDirty();
                }
                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("+ 손질 선택지 추가"))
            {
                _ingredientDraft.PreparationOptions.Add(new PreparationOptionDraft());
                MarkDraftDirty();
            }

            EditorGUILayout.Space(8f);
        }

        private static bool DrawObjectList<T>(string title, List<T> values, Type objectType, string addLabel)
            where T : UnityEngine.Object
        {
            bool changed = false;
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

            for (int i = 0; i < values.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"{i + 1}.", GUILayout.Width(24f));
                T newValue = (T)EditorGUILayout.ObjectField(values[i], objectType, false);
                if (newValue != values[i])
                {
                    values[i] = newValue;
                    changed = true;
                }

                if (GUILayout.Button("삭제", GUILayout.Width(52f)))
                {
                    values.RemoveAt(i);
                    i--;
                    changed = true;
                }

                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button(addLabel))
            {
                values.Add(null);
                changed = true;
            }

            EditorGUILayout.Space(8f);
            return changed;
        }

        private void DrawWarnings(List<string> warnings)
        {
            if (warnings.Count == 0)
            {
                EditorGUILayout.HelpBox("현재 입력값에서 눈에 띄는 문제는 없습니다.", MessageType.Info);
                return;
            }

            for (int i = 0; i < warnings.Count; i++)
                EditorGUILayout.HelpBox(warnings[i], MessageType.Warning);
        }
    }
}
