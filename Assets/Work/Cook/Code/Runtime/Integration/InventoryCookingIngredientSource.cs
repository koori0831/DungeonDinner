using System;
using System.Collections.Generic;
using UnityEngine;
using Work.Cook.Code.Data;
using Work.Core.EventBus;
using Work.Items.Code;
using Work.Players.Code.Inventory;
using Work.Cook.Code.Runtime.Core;
using Work.Cook.Code.Runtime.Integration;
using Work.Cook.Code.Runtime.Systems;
using Work.Cook.Code.Runtime.UI;

namespace Work.Cook.Code.Runtime.Integration
{
    /// <summary>
    /// 플레이어 인벤토리 스냅샷 이벤트를 조리 재료 선택 소스로 제공
    /// </summary>
    public sealed class InventoryCookingIngredientSource : MonoBehaviour, ICookingIngredientSource, ICookingIngredientQuantitySource,
        ICookingIngredientIconSource,
        ICookingIngredientConsumer
    {
        [SerializeField]
        private string sourceName = "인벤토리 재료";

        private readonly Dictionary<IngredientSO, int> INGREDIENT_AMOUNTS = new Dictionary<IngredientSO, int>();
        private readonly Dictionary<IngredientSO, Sprite> INGREDIENT_ICONS = new Dictionary<IngredientSO, Sprite>();
        private readonly Dictionary<IngredientSO, int> REQUIRED_INGREDIENT_AMOUNTS = new Dictionary<IngredientSO, int>();
        private readonly Dictionary<IngredientItemDataSO, int> INGREDIENT_ITEM_AMOUNTS = new Dictionary<IngredientItemDataSO, int>();
        private readonly List<IngredientSO> AVAILABLE_INGREDIENTS = new List<IngredientSO>();
        private readonly List<InventoryItemStack> CONSUME_REQUESTS = new List<InventoryItemStack>();

        private bool _isSubscribedToInventoryEvents;

        /// <summary>
        /// 재료 목록이 변경될 때 발생하는 이벤트
        /// </summary>
        public event Action IngredientsChanged;

        /// <summary>
        /// 조리 재료 소스 표시 이름
        /// </summary>
        public string SourceName => sourceName;

        private void OnEnable()
        {
            SubscribeInventoryEvents();
            RefreshCache(true);
        }

        private void OnDisable()
        {
            UnsubscribeInventoryEvents();
        }

        /// <summary>
        /// 현재 인벤토리 기반 사용 가능한 조리 재료 목록 반환
        /// </summary>
        /// <param name="owner">요청한 조리 패널</param>
        /// <param name="runner">요청한 조리 플로우 러너</param>
        /// <returns>사용 가능한 조리 재료 목록</returns>
        public IReadOnlyList<IngredientSO> GetAvailableIngredients(CookingGamePanel owner, CookingFlowRunner runner)
        {
            RefreshCache(false);
            return AVAILABLE_INGREDIENTS;
        }

        /// <summary>
        /// 현재 인벤토리 기반 특정 조리 재료 보유 수량 반환
        /// </summary>
        /// <param name="ingredient">조회할 조리 재료</param>
        /// <param name="owner">요청한 조리 패널</param>
        /// <param name="runner">요청한 조리 플로우 러너</param>
        /// <returns>보유 수량</returns>
        public int GetAvailableIngredientQuantity(IngredientSO ingredient, CookingGamePanel owner, CookingFlowRunner runner)
        {
            if (ingredient == null)
            {
                return 0;
            }

            RefreshCache(false);

            if (INGREDIENT_AMOUNTS.TryGetValue(ingredient, out int amount) == true)
            {
                return amount;
            }

            return 0;
        }

        /// <summary>
        /// 현재 인벤토리 기반 특정 조리 재료의 표시 아이콘 반환
        /// </summary>
        /// <param name="ingredient">조회할 조리 재료</param>
        /// <param name="owner">요청한 조리 패널</param>
        /// <param name="runner">요청한 조리 플로우 러너</param>
        /// <returns>표시용 아이콘</returns>
        public Sprite GetAvailableIngredientIcon(IngredientSO ingredient, CookingGamePanel owner, CookingFlowRunner runner)
        {
            if (ingredient == null)
            {
                return null;
            }

            RefreshCache(false);

            if (INGREDIENT_ICONS.TryGetValue(ingredient, out Sprite icon) == true && icon != null)
            {
                return icon;
            }

            return CookingTempVisualUtility.ResolveIngredientIcon(ingredient);
        }

        /// <summary>
        /// 선택 재료를 현재 인벤토리에서 소비할 수 있는지 확인
        /// </summary>
        /// <param name="ingredients">소비할 조리 재료 목록</param>
        /// <param name="owner">요청한 조리 패널</param>
        /// <param name="runner">요청한 조리 플로우 러너</param>
        /// <param name="reason">소비 불가 사유</param>
        /// <returns>소비 가능 여부</returns>
        public bool CanConsumeIngredients(
            IReadOnlyList<IngredientSO> ingredients,
            CookingGamePanel owner,
            CookingFlowRunner runner,
            out string reason)
        {
            RefreshCache(false);

            if (TryBuildRequiredIngredientAmounts(ingredients, REQUIRED_INGREDIENT_AMOUNTS, out reason) == false)
            {
                return false;
            }

            foreach (KeyValuePair<IngredientSO, int> requirement in REQUIRED_INGREDIENT_AMOUNTS)
            {
                int availableAmount = 0;
                if (INGREDIENT_AMOUNTS.TryGetValue(requirement.Key, out int cachedAmount) == true)
                {
                    availableAmount = cachedAmount;
                }

                if (availableAmount < requirement.Value)
                {
                    reason = $"Not enough {GetIngredientName(requirement.Key)}. required={requirement.Value}, available={availableAmount}";
                    return false;
                }
            }

            reason = string.Empty;
            return true;
        }

        /// <summary>
        /// 선택 재료를 현재 인벤토리에서 소비
        /// </summary>
        /// <param name="ingredients">소비할 조리 재료 목록</param>
        /// <param name="owner">요청한 조리 패널</param>
        /// <param name="runner">요청한 조리 플로우 러너</param>
        /// <param name="reason">소비 실패 사유</param>
        /// <returns>소비 성공 여부</returns>
        public bool TryConsumeIngredients(
            IReadOnlyList<IngredientSO> ingredients,
            CookingGamePanel owner,
            CookingFlowRunner runner,
            out string reason)
        {
            if (CanConsumeIngredients(ingredients, owner, runner, out reason) == false)
            {
                return false;
            }

            if (TryBuildRequiredIngredientAmounts(ingredients, REQUIRED_INGREDIENT_AMOUNTS, out reason) == false)
            {
                return false;
            }

            foreach (KeyValuePair<IngredientSO, int> requirement in REQUIRED_INGREDIENT_AMOUNTS)
            {
                int removedAmount;
                if (TryConsumeIngredientAmount(requirement.Key, requirement.Value, out removedAmount) == false
                    || removedAmount < requirement.Value)
                {
                    reason = $"Could not consume {GetIngredientName(requirement.Key)}. required={requirement.Value}, removed={removedAmount}";
                    RefreshCache(true);
                    return false;
                }
            }

            reason = string.Empty;
            RefreshCache(true);
            return true;
        }

        private void SubscribeInventoryEvents()
        {
            if (_isSubscribedToInventoryEvents == true)
            {
                return;
            }

            Bus<InventoryChangedEvent>.Events += HandleInventoryChanged;
            Bus<InventorySnapshotPublishedEvent>.Events += HandleInventorySnapshotPublished;
            _isSubscribedToInventoryEvents = true;
        }

        private void UnsubscribeInventoryEvents()
        {
            if (_isSubscribedToInventoryEvents == false)
            {
                return;
            }

            Bus<InventoryChangedEvent>.Events -= HandleInventoryChanged;
            Bus<InventorySnapshotPublishedEvent>.Events -= HandleInventorySnapshotPublished;
            _isSubscribedToInventoryEvents = false;
        }

        private void HandleInventoryChanged(InventoryChangedEvent evt)
        {
            RefreshCache(true);
        }

        private void HandleInventorySnapshotPublished(InventorySnapshotPublishedEvent evt)
        {
            if (evt.ItemStacks == null || evt.Count <= 0)
            {
                return;
            }

            int itemStackCount = Mathf.Min(evt.Count, evt.ItemStacks.Length);
            for (int i = 0; i < itemStackCount; i++)
            {
                InventoryItemStack itemStack = evt.ItemStacks[i];
                if (itemStack.IsValid == false)
                {
                    continue;
                }

                IngredientItemDataSO ingredientItem = itemStack.Item as IngredientItemDataSO;
                if (ingredientItem == null || ingredientItem.IsValidIngredientItem == false)
                {
                    continue;
                }

                AddIngredientItem(ingredientItem, itemStack.Amount);
            }
        }

        private void RefreshCache(bool notifyChanged)
        {
            SubscribeInventoryEvents();
            ClearCache();
            Bus<InventorySnapshotRequestedEvent>.Raise(new InventorySnapshotRequestedEvent());

            foreach (KeyValuePair<IngredientSO, int> kvp in INGREDIENT_AMOUNTS)
            {
                if (kvp.Key == null || kvp.Value <= 0)
                {
                    continue;
                }

                AVAILABLE_INGREDIENTS.Add(kvp.Key);
            }

            if (notifyChanged == true)
            {
                NotifyIngredientsChanged();
            }
        }

        private void ClearCache()
        {
            INGREDIENT_AMOUNTS.Clear();
            INGREDIENT_ICONS.Clear();
            INGREDIENT_ITEM_AMOUNTS.Clear();
            AVAILABLE_INGREDIENTS.Clear();
        }

        private void AddIngredientItem(IngredientItemDataSO ingredientItem, int amount)
        {
            if (ingredientItem == null || amount <= 0 || ingredientItem.IsValidIngredientItem == false)
            {
                return;
            }

            IngredientSO ingredient = ingredientItem.Ingredient;
            if (ingredient == null)
            {
                return;
            }

            if (INGREDIENT_AMOUNTS.TryGetValue(ingredient, out int ingredientAmount) == true)
            {
                INGREDIENT_AMOUNTS[ingredient] = ingredientAmount + amount;
            }
            else
            {
                INGREDIENT_AMOUNTS.Add(ingredient, amount);
            }

            if (INGREDIENT_ITEM_AMOUNTS.TryGetValue(ingredientItem, out int itemAmount) == true)
            {
                INGREDIENT_ITEM_AMOUNTS[ingredientItem] = itemAmount + amount;
            }
            else
            {
                INGREDIENT_ITEM_AMOUNTS.Add(ingredientItem, amount);
            }

            if (INGREDIENT_ICONS.ContainsKey(ingredient) == false)
            {
                Sprite icon = ItemIconUtility.ResolveIcon(ingredientItem);
                INGREDIENT_ICONS.Add(ingredient, icon != null ? icon : CookingTempVisualUtility.ResolveIngredientIcon(ingredient));
            }
        }

        private bool TryBuildRequiredIngredientAmounts(
            IReadOnlyList<IngredientSO> ingredients,
            Dictionary<IngredientSO, int> results,
            out string reason)
        {
            if (results == null)
            {
                reason = "Ingredient amount result buffer is missing.";
                return false;
            }

            results.Clear();

            if (ingredients == null || ingredients.Count == 0)
            {
                reason = "No cooking ingredients were selected.";
                return false;
            }

            for (int i = 0; i < ingredients.Count; i++)
            {
                IngredientSO ingredient = ingredients[i];
                if (ingredient == null)
                {
                    continue;
                }

                if (results.TryGetValue(ingredient, out int currentAmount) == true)
                {
                    results[ingredient] = currentAmount + 1;
                    continue;
                }

                results.Add(ingredient, 1);
            }

            if (results.Count == 0)
            {
                reason = "No valid cooking ingredients were selected.";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        private bool TryConsumeIngredientAmount(IngredientSO ingredient, int amount, out int removedAmount)
        {
            removedAmount = 0;
            CONSUME_REQUESTS.Clear();

            if (ingredient == null || amount <= 0)
            {
                return false;
            }

            int remainingAmount = amount;
            foreach (KeyValuePair<IngredientItemDataSO, int> kvp in INGREDIENT_ITEM_AMOUNTS)
            {
                IngredientItemDataSO ingredientItem = kvp.Key;
                if (ingredientItem == null
                    || ingredientItem.IsValidIngredientItem == false
                    || ingredientItem.Ingredient != ingredient)
                {
                    continue;
                }

                int itemRemovedAmount = Mathf.Min(remainingAmount, kvp.Value);
                if (itemRemovedAmount <= 0)
                {
                    continue;
                }

                CONSUME_REQUESTS.Add(new InventoryItemStack(ingredientItem, itemRemovedAmount));
                removedAmount += itemRemovedAmount;
                remainingAmount -= itemRemovedAmount;

                if (remainingAmount <= 0)
                {
                    break;
                }
            }

            if (removedAmount < amount)
            {
                CONSUME_REQUESTS.Clear();
                return false;
            }

            for (int i = 0; i < CONSUME_REQUESTS.Count; i++)
            {
                InventoryItemStack itemStack = CONSUME_REQUESTS[i];
                Bus<InventoryItemRemoveRequestedEvent>.Raise(new InventoryItemRemoveRequestedEvent(itemStack.Item, itemStack.Amount));
            }

            CONSUME_REQUESTS.Clear();
            return true;
        }

        private static string GetIngredientName(IngredientSO ingredient)
        {
            return ingredient != null ? ingredient.DisplayName : "Unknown ingredient";
        }

        private void NotifyIngredientsChanged()
        {
            Action handler = IngredientsChanged;

            if (handler == null)
            {
                return;
            }

            handler.Invoke();
        }
    }
}
