using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Work.Cook.Code.Data;
using Work.NPC.Code.Data;
using Work.NPC.Code.Runtime;

namespace Work.Cook.Code.Editor
{
    public static class NpcCookingDataSyncEditor
    {
        private const string MenuPath = "Tools/Dungeon Dinner/Sync Cooking Data From NPC Events";
        private const string CatalogPath = "Assets/Work/Cook/SO/CookingDataCatalog.asset";
        private const string GeneratedRoot = "Assets/Work/Cook/SO/NpcGenerated";
        private const string CategoryFolder = GeneratedRoot + "/Categories";
        private const string TagFolder = GeneratedRoot + "/Tags";
        private const string MethodFolder = GeneratedRoot + "/Methods";
        private const string IngredientFolder = GeneratedRoot + "/Ingredients";
        private const string RecipeFolder = GeneratedRoot + "/Recipes";

        private const string FinishMethodId = "finish_order";
        private const string DiluteMethodId = "dilute";
        private const string OvercookMethodId = "overcook";

        private static readonly Dictionary<string, string> CategoryNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Bread", "빵요리" },
            { "Dessert", "디저트" },
            { "Drink", "음료" },
            { "Grill", "구이" },
            { "RiceBowl", "덮밥" },
            { "Roast", "직화구이" },
            { "Salad", "샐러드" },
            { "Snack", "간식" },
            { "Soup", "수프" },
            { "Stew", "스튜" },
            { "Tea", "차" }
        };

        private static readonly Dictionary<string, string> TagNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Aromatic", "향긋함" },
            { "Bitter", "쓴맛" },
            { "Burnt", "탄맛" },
            { "Charred", "그을린 향" },
            { "Chewy", "씹는맛" },
            { "Clean", "깔끔함" },
            { "Cold", "차가움" },
            { "Crispy", "바삭함" },
            { "Decorative", "장식뿐인" },
            { "Fish", "생선" },
            { "Fishy", "비린내" },
            { "Fresh", "신선함" },
            { "FreshOnly", "풋내" },
            { "Frozen", "얼음처럼 차가움" },
            { "Fruity", "과일향" },
            { "Greasy", "기름짐" },
            { "Heavy", "묵직함" },
            { "Hearty", "든든함" },
            { "Herbal", "허브향" },
            { "Hot", "뜨거움" },
            { "Leaking", "새는 음식" },
            { "Light", "가벼움" },
            { "LingeringSmell", "남는 냄새" },
            { "Meat", "고기" },
            { "Messy", "손에 묻음" },
            { "Mineral", "광물향" },
            { "Oily", "과한 기름" },
            { "OverSpiced", "과한 향신료" },
            { "Portable", "휴대성" },
            { "Rotten", "상한맛" },
            { "Salty", "짭짤함" },
            { "Savory", "감칠맛" },
            { "Smoky", "훈연향" },
            { "Smooth", "부드러운 목넘김" },
            { "Soft", "부드러움" },
            { "Soggy", "눅눅함" },
            { "Spicy", "매콤함" },
            { "StrongSmell", "강한 냄새" },
            { "Sweet", "달콤함" },
            { "SweetOnly", "단맛만 남음" },
            { "SweetSalty", "단짠" },
            { "SweetSour", "새콤달콤" },
            { "TinyPortion", "작은 양" },
            { "Vegetable", "채소" },
            { "Warm", "따뜻함" },
            { "Watery", "묽음" }
        };

        [MenuItem(MenuPath)]
        public static void SyncFromNpcEvents()
        {
            EnsureFolders();

            NpcConversationDatabase database = NpcConversationDatabase.LoadFromResources("NPCData");
            List<VisitEventData> orderEvents = database.VisitEvents.Values
                .Where(visitEvent => string.IsNullOrWhiteSpace(visitEvent.CorrectRecipeId) == false)
                .OrderBy(visitEvent => visitEvent.NpcId, StringComparer.OrdinalIgnoreCase)
                .ThenBy(visitEvent => visitEvent.EventId, StringComparer.OrdinalIgnoreCase)
                .ToList();

            CookingDataCatalogSO catalog = LoadOrCreateAsset<CookingDataCatalogSO>(CatalogPath);
            PreparationMethodSO finishMethod = EnsurePreparationMethod(
                FinishMethodId,
                "정석으로 마무리",
                "NPC 주문 조건을 만족하도록 조리 마무리를 맞춥니다.");
            PreparationMethodSO diluteMethod = EnsurePreparationMethod(
                DiluteMethodId,
                "묽게 만들기",
                "요리가 묽어져 일부 NPC가 피하는 결과가 될 수 있습니다.");
            PreparationMethodSO overcookMethod = EnsurePreparationMethod(
                OvercookMethodId,
                "태워버리기",
                "탄맛이 강해지고 괴식 판정을 받을 수 있습니다.");

            HashSet<string> categoryIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> tagIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (VisitEventData visitEvent in orderEvents)
            {
                AddAll(categoryIds, visitEvent.AllowedFoodTypes);
                AddAll(tagIds, visitEvent.RequiredTags);
                AddAll(tagIds, visitEvent.PreferredTags);
                AddAll(tagIds, visitEvent.AvoidTags);
                AddAll(tagIds, visitEvent.DisgustingTags);
            }

            Dictionary<string, FoodCategorySO> categories = categoryIds
                .Select(EnsureCategory)
                .Where(category => category != null)
                .ToDictionary(category => category.CategoryId, StringComparer.OrdinalIgnoreCase);

            Dictionary<string, FoodTagSO> tags = tagIds
                .Select(EnsureTag)
                .Where(tag => tag != null)
                .ToDictionary(tag => tag.TagId.Trim(), StringComparer.OrdinalIgnoreCase);

            foreach (VisitEventData visitEvent in orderEvents)
            {
                string categoryId = visitEvent.AllowedFoodTypes.FirstOrDefault() ?? string.Empty;
                FoodCategorySO category = GetOrDefault(categories, categoryId);
                List<FoodTagSO> recipeTags = BuildTagList(tags, visitEvent.RequiredTags, visitEvent.PreferredTags);
                IngredientSO ingredient = EnsureOrderIngredient(
                    visitEvent,
                    database,
                    recipeTags,
                    finishMethod,
                    diluteMethod,
                    overcookMethod,
                    tags);

                EnsureRecipe(visitEvent, database, category, recipeTags, ingredient, finishMethod);
            }

            UpdateCatalog(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"NPC cooking data sync complete. events={orderEvents.Count}, categories={categoryIds.Count}, tags={tagIds.Count}, catalog={CatalogPath}");
        }

        private static IngredientSO EnsureOrderIngredient(
            VisitEventData visitEvent,
            NpcConversationDatabase database,
            IReadOnlyList<FoodTagSO> recipeTags,
            PreparationMethodSO finishMethod,
            PreparationMethodSO diluteMethod,
            PreparationMethodSO overcookMethod,
            IReadOnlyDictionary<string, FoodTagSO> tags)
        {
            string ingredientId = $"{visitEvent.CorrectRecipeId}_ingredient";
            string path = $"{IngredientFolder}/{SanitizeFileName(ingredientId)}.asset";
            IngredientSO ingredient = LoadOrCreateAsset<IngredientSO>(path);
            string recipeName = BuildRecipeDisplayName(visitEvent, database);

            SerializedObject serialized = new SerializedObject(ingredient);
            serialized.FindProperty("ingredientId").stringValue = ingredientId;
            serialized.FindProperty("displayName").stringValue = $"{recipeName} 재료";
            serialized.FindProperty("description").stringValue =
                $"NPC 이벤트 '{visitEvent.EventId}'의 주문 레시피를 테스트하기 위한 생성 재료입니다.";
            SetObjectArray(serialized.FindProperty("baseTags"), recipeTags);
            SetPreparationOptions(
                serialized.FindProperty("preparationOptions"),
                finishMethod,
                diluteMethod,
                overcookMethod,
                GetOrDefault(tags, "Watery"),
                GetOrDefault(tags, "Burnt"));
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(ingredient);
            return ingredient;
        }

        private static RecipeSO EnsureRecipe(
            VisitEventData visitEvent,
            NpcConversationDatabase database,
            FoodCategorySO category,
            IReadOnlyList<FoodTagSO> recipeTags,
            IngredientSO ingredient,
            PreparationMethodSO finishMethod)
        {
            string path = $"{RecipeFolder}/{SanitizeFileName(visitEvent.CorrectRecipeId)}.asset";
            RecipeSO recipe = LoadOrCreateAsset<RecipeSO>(path);
            SerializedObject serialized = new SerializedObject(recipe);
            serialized.FindProperty("recipeId").stringValue = visitEvent.CorrectRecipeId;
            serialized.FindProperty("displayName").stringValue = BuildRecipeDisplayName(visitEvent, database);
            serialized.FindProperty("description").stringValue = BuildRecipeDescription(visitEvent);
            serialized.FindProperty("category").objectReferenceValue = category;
            SetObjectArray(serialized.FindProperty("baseTags"), recipeTags);
            SetRequiredIngredient(serialized.FindProperty("requiredIngredients"), ingredient, finishMethod);
            serialized.FindProperty("perfectPreparationRules").ClearArray();
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(recipe);
            return recipe;
        }

        private static FoodCategorySO EnsureCategory(string categoryId)
        {
            FoodCategorySO category = FindAssetById<FoodCategorySO>("categoryId", categoryId);
            if (category == null)
                category = LoadOrCreateAsset<FoodCategorySO>($"{CategoryFolder}/{SanitizeFileName(categoryId)}.asset");

            SerializedObject serialized = new SerializedObject(category);
            serialized.FindProperty("categoryId").stringValue = categoryId;
            serialized.FindProperty("displayName").stringValue = GetDisplayName(CategoryNames, categoryId);
            serialized.FindProperty("description").stringValue = $"NPC 주문 평가에 사용되는 음식 타입 '{categoryId}'입니다.";
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(category);
            return category;
        }

        private static FoodTagSO EnsureTag(string tagId)
        {
            FoodTagSO tag = FindAssetById<FoodTagSO>("tagId", tagId);
            if (tag == null)
                tag = LoadOrCreateAsset<FoodTagSO>($"{TagFolder}/{SanitizeFileName(tagId)}.asset");

            SerializedObject serialized = new SerializedObject(tag);
            serialized.FindProperty("tagId").stringValue = tagId;
            serialized.FindProperty("displayName").stringValue = GetDisplayName(TagNames, tagId);
            serialized.FindProperty("description").stringValue = $"NPC 주문 평가에 사용되는 태그 '{tagId}'입니다.";
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(tag);
            return tag;
        }

        private static PreparationMethodSO EnsurePreparationMethod(string methodId, string displayName, string description)
        {
            PreparationMethodSO method = FindAssetById<PreparationMethodSO>("methodId", methodId);
            if (method == null)
                method = LoadOrCreateAsset<PreparationMethodSO>($"{MethodFolder}/{SanitizeFileName(methodId)}.asset");

            SerializedObject serialized = new SerializedObject(method);
            serialized.FindProperty("methodId").stringValue = methodId;
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.FindProperty("description").stringValue = description;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(method);
            return method;
        }

        private static void SetPreparationOptions(
            SerializedProperty property,
            PreparationMethodSO finishMethod,
            PreparationMethodSO diluteMethod,
            PreparationMethodSO overcookMethod,
            FoodTagSO wateryTag,
            FoodTagSO burntTag)
        {
            property.ClearArray();
            AddPreparationOption(
                property,
                finishMethod,
                "정석으로 마무리",
                "주문 조건에 맞춰 맛과 형태를 정돈합니다.",
                null,
                1,
                false,
                false,
                string.Empty);
            AddPreparationOption(
                property,
                diluteMethod,
                "묽게 만들기",
                "맛이 흐려지고 묽은 음식 태그가 붙습니다.",
                wateryTag,
                -1,
                false,
                false,
                "묽어진");
            AddPreparationOption(
                property,
                overcookMethod,
                "태워버리기",
                "탄맛이 붙고 괴식 판정을 받을 수 있습니다.",
                burntTag,
                -3,
                true,
                false,
                "탄");
        }

        private static void AddPreparationOption(
            SerializedProperty property,
            PreparationMethodSO method,
            string displayNameOverride,
            string description,
            FoodTagSO addTag,
            int qualityDelta,
            bool causesDisgusting,
            bool addsPoison,
            string resultNameModifier)
        {
            property.InsertArrayElementAtIndex(property.arraySize);
            SerializedProperty element = property.GetArrayElementAtIndex(property.arraySize - 1);
            element.FindPropertyRelative("method").objectReferenceValue = method;
            element.FindPropertyRelative("displayNameOverride").stringValue = displayNameOverride;
            element.FindPropertyRelative("description").stringValue = description;
            SerializedProperty addTags = element.FindPropertyRelative("addTags");
            addTags.ClearArray();
            if (addTag != null)
            {
                addTags.InsertArrayElementAtIndex(0);
                addTags.GetArrayElementAtIndex(0).objectReferenceValue = addTag;
            }

            element.FindPropertyRelative("removeTags").ClearArray();
            element.FindPropertyRelative("qualityDelta").intValue = qualityDelta;
            element.FindPropertyRelative("causesDisgusting").boolValue = causesDisgusting;
            element.FindPropertyRelative("addsPoison").boolValue = addsPoison;
            element.FindPropertyRelative("resultNameModifier").stringValue = resultNameModifier;
        }

        private static void SetRequiredIngredient(
            SerializedProperty property,
            IngredientSO ingredient,
            PreparationMethodSO requiredMethod)
        {
            property.ClearArray();
            property.InsertArrayElementAtIndex(0);
            SerializedProperty element = property.GetArrayElementAtIndex(0);
            element.FindPropertyRelative("ingredient").objectReferenceValue = ingredient;
            element.FindPropertyRelative("ingredientCategory").objectReferenceValue = null;
            element.FindPropertyRelative("requiredTags").ClearArray();
            element.FindPropertyRelative("alternatives").ClearArray();
            element.FindPropertyRelative("alternativeOptions").ClearArray();
            element.FindPropertyRelative("requiredPreparationMethod").objectReferenceValue = null;
            SerializedProperty requiredMethods = element.FindPropertyRelative("requiredPreparationMethods");
            requiredMethods.ClearArray();
            if (requiredMethod != null)
            {
                requiredMethods.InsertArrayElementAtIndex(0);
                requiredMethods.GetArrayElementAtIndex(0).objectReferenceValue = requiredMethod;
            }
            element.FindPropertyRelative("minCount").intValue = 1;
            element.FindPropertyRelative("maxCount").intValue = 1;
            element.FindPropertyRelative("recipeDefining").boolValue = true;
            element.FindPropertyRelative("requireManualPreparation").boolValue = false;
        }

        private static void UpdateCatalog(CookingDataCatalogSO catalog)
        {
            SerializedObject serialized = new SerializedObject(catalog);
            SetObjectArray(serialized.FindProperty("categories"), LoadAllAssets<FoodCategorySO>().OrderBy(category => category.CategoryId).ToList());
            SetObjectArray(serialized.FindProperty("tags"), LoadAllAssets<FoodTagSO>().OrderBy(tag => tag.TagId).ToList());
            SetObjectArray(serialized.FindProperty("preparationMethods"), LoadAllAssets<PreparationMethodSO>().OrderBy(method => method.MethodId).ToList());
            SetObjectArray(serialized.FindProperty("ingredients"), LoadAllAssets<IngredientSO>().OrderBy(ingredient => ingredient.IngredientId).ToList());
            SetObjectArray(serialized.FindProperty("recipes"), LoadAllAssets<RecipeSO>().OrderBy(recipe => recipe.RecipeId).ToList());
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
        }

        private static List<FoodTagSO> BuildTagList(
            IReadOnlyDictionary<string, FoodTagSO> tagLookup,
            params IReadOnlyList<string>[] tagGroups)
        {
            List<FoodTagSO> tags = new List<FoodTagSO>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int groupIndex = 0; groupIndex < tagGroups.Length; groupIndex++)
            {
                IReadOnlyList<string> group = tagGroups[groupIndex];
                if (group == null)
                    continue;

                for (int i = 0; i < group.Count; i++)
                {
                    string tagId = group[i];
                    if (string.IsNullOrWhiteSpace(tagId) || seen.Add(tagId.Trim()) == false)
                        continue;

                    FoodTagSO tag = GetOrDefault(tagLookup, tagId);
                    if (tag != null)
                        tags.Add(tag);
                }
            }

            return tags;
        }

        private static string BuildRecipeDisplayName(VisitEventData visitEvent, NpcConversationDatabase database)
        {
            string npcName = visitEvent.NpcId;
            if (database.Npcs.TryGetValue(visitEvent.NpcId, out NpcData npc))
                npcName = npc.DisplayName;

            string recipePart = visitEvent.CorrectRecipeId;
            string prefix = visitEvent.NpcId + "_";
            if (recipePart.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                recipePart = recipePart.Substring(prefix.Length);

            return $"{npcName} - {SplitPascalCase(recipePart)}";
        }

        private static string BuildRecipeDescription(VisitEventData visitEvent)
        {
            return
                $"NPC 이벤트 '{visitEvent.EventId}'에서 요구하는 자동 생성 레시피입니다.\n" +
                $"허용 음식 타입: {string.Join("|", visitEvent.AllowedFoodTypes)}\n" +
                $"필수 태그: {string.Join("|", visitEvent.RequiredTags)}\n" +
                $"선호 태그: {string.Join("|", visitEvent.PreferredTags)}\n" +
                $"회피 태그: {string.Join("|", visitEvent.AvoidTags)}\n" +
                $"괴식 태그: {string.Join("|", visitEvent.DisgustingTags)}";
        }

        private static void AddAll(ISet<string> target, IReadOnlyList<string> values)
        {
            if (values == null)
                return;

            for (int i = 0; i < values.Count; i++)
            {
                string value = values[i];
                if (string.IsNullOrWhiteSpace(value) == false)
                    target.Add(value.Trim());
            }
        }

        private static void SetObjectArray<T>(SerializedProperty property, IReadOnlyList<T> values)
            where T : UnityEngine.Object
        {
            property.ClearArray();
            if (values == null)
                return;

            for (int i = 0; i < values.Count; i++)
            {
                property.InsertArrayElementAtIndex(property.arraySize);
                property.GetArrayElementAtIndex(property.arraySize - 1).objectReferenceValue = values[i];
            }
        }

        private static T GetOrDefault<T>(IReadOnlyDictionary<string, T> values, string key)
            where T : UnityEngine.Object
        {
            if (string.IsNullOrWhiteSpace(key))
                return null;

            return values.TryGetValue(key.Trim(), out T value) ? value : null;
        }

        private static T FindAssetById<T>(string propertyName, string id)
            where T : UnityEngine.Object
        {
            string normalizedId = NormalizeId(id);
            foreach (T asset in LoadAllAssets<T>())
            {
                SerializedObject serialized = new SerializedObject(asset);
                SerializedProperty property = serialized.FindProperty(propertyName);
                if (property != null && NormalizeId(property.stringValue) == normalizedId)
                    return asset;
            }

            return null;
        }

        private static List<T> LoadAllAssets<T>()
            where T : UnityEngine.Object
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            List<T> assets = new List<T>();
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null)
                    assets.Add(asset);
            }

            return assets;
        }

        private static T LoadOrCreateAsset<T>(string path)
            where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;

            EnsureFolder(Path.GetDirectoryName(path)?.Replace("\\", "/"));
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureFolders()
        {
            EnsureFolder(GeneratedRoot);
            EnsureFolder(CategoryFolder);
            EnsureFolder(TagFolder);
            EnsureFolder(MethodFolder);
            EnsureFolder(IngredientFolder);
            EnsureFolder(RecipeFolder);
        }

        private static void EnsureFolder(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder) || AssetDatabase.IsValidFolder(folder))
                return;

            string parent = Path.GetDirectoryName(folder)?.Replace("\\", "/");
            string name = Path.GetFileName(folder);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private static string GetDisplayName(IReadOnlyDictionary<string, string> displayNames, string id)
        {
            return displayNames.TryGetValue(id, out string displayName) ? displayName : SplitPascalCase(id);
        }

        private static string SplitPascalCase(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            List<char> chars = new List<char>();
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (i > 0 && char.IsUpper(c) && char.IsLetterOrDigit(value[i - 1]))
                    chars.Add(' ');

                chars.Add(c == '_' ? ' ' : c);
            }

            return new string(chars.ToArray()).Trim();
        }

        private static string SanitizeFileName(string value)
        {
            string safe = value;
            foreach (char invalid in Path.GetInvalidFileNameChars())
                safe = safe.Replace(invalid, '_');

            return safe;
        }

        private static string NormalizeId(string id)
        {
            return (id ?? string.Empty).Trim().ToLowerInvariant();
        }
    }
}
