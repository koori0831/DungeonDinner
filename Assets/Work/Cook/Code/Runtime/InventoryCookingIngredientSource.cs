using System;
using System.Collections.Generic;
using UnityEngine;
using Work.Cook.Code.Data;
using Work.Items.Code;
using Work.Players.Code.Inventory;

namespace Work.Cook.Code.Runtime
{
    /// <summary>
    /// 플레이어 인벤토리의 재료 아이템을 조리 재료 선택 소스로 제공
    /// </summary>
    public sealed class InventoryCookingIngredientSource : MonoBehaviour, ICookingIngredientSource, ICookingIngredientQuantitySource,
        ICookingIngredientIconSource,
        ICookingIngredientConsumer
    {
        [SerializeField]
        private PlayerInventoryModule inventoryModule;

        [SerializeField]
        private string sourceName = "인벤토리 재료";

        [SerializeField]
        private bool searchInventoryInParents = true;

        [SerializeField]
        private bool searchInventoryInChildren = true;

        private readonly Dictionary<IngredientSO, int> INGREDIENT_AMOUNTS = new Dictionary<IngredientSO, int>();
        private readonly Dictionary<IngredientSO, Sprite> INGREDIENT_ICONS = new Dictionary<IngredientSO, Sprite>();
        private readonly Dictionary<IngredientSO, int> REQUIRED_INGREDIENT_AMOUNTS = new Dictionary<IngredientSO, int>();
        private readonly List<IngredientSO> AVAILABLE_INGREDIENTS = new List<IngredientSO>();
        private PlayerInventoryModule _subscribedInventoryModule;

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
            ResolveInventoryModule();
            SubscribeInventory();
            RefreshCache(true);
        }

        private void OnDisable()
        {
            UnsubscribeInventory();
        }

        /// <summary>
        /// 사용할 인벤토리 모듈 지정
        /// </summary>
        /// <param name="newInventoryModule">조리 재료 소스로 사용할 인벤토리 모듈</param>
        public void SetInventoryModule(PlayerInventoryModule newInventoryModule)
        {
            if (inventoryModule == newInventoryModule)
            {
                return;
            }

            UnsubscribeInventory();
            inventoryModule = newInventoryModule;
            SubscribeInventory();
            RefreshCache(true);
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

            if (inventoryModule == null)
            {
                reason = "Player inventory is missing.";
                return false;
            }

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

        private void ResolveInventoryModule()
        {
            if (inventoryModule != null)
            {
                return;
            }

            if (searchInventoryInParents == true)
            {
                inventoryModule = GetComponentInParent<PlayerInventoryModule>();

                if (inventoryModule != null)
                {
                    return;
                }
            }

            if (searchInventoryInChildren == true)
            {
                inventoryModule = GetComponentInChildren<PlayerInventoryModule>();
            }
        }

        private void SubscribeInventory()
        {
            if (_subscribedInventoryModule == inventoryModule)
            {
                return;
            }

            UnsubscribeInventory();

            if (inventoryModule == null)
            {
                return;
            }

            inventoryModule.InventoryChanged += HandleInventoryChanged;
            _subscribedInventoryModule = inventoryModule;
        }

        private void UnsubscribeInventory()
        {
            if (_subscribedInventoryModule == null)
            {
                return;
            }

            _subscribedInventoryModule.InventoryChanged -= HandleInventoryChanged;
            _subscribedInventoryModule = null;
        }

        private void HandleInventoryChanged(PlayerInventoryModule changedInventoryModule)
        {
            if (changedInventoryModule != inventoryModule)
            {
                return;
            }

            RefreshCache(true);
        }

        private void RefreshCache(bool notifyChanged)
        {
            ResolveInventoryModule();
            SubscribeInventory();
            InventoryIngredientQuery.FillIngredientAmounts(inventoryModule, INGREDIENT_AMOUNTS);
            FillIngredientIcons();
            AVAILABLE_INGREDIENTS.Clear();

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

        private void FillIngredientIcons()
        {
            INGREDIENT_ICONS.Clear();

            if (inventoryModule == null)
            {
                return;
            }

            int slotCapacity = inventoryModule.SlotCapacity;
            for (int i = 0; i < slotCapacity; i++)
            {
                InventorySlot slot = inventoryModule.GetSlot(i);
                if (slot == null || slot.IsEmpty == true)
                {
                    continue;
                }

                IngredientItemDataSO ingredientItem = slot.Item as IngredientItemDataSO;
                if (ingredientItem == null || ingredientItem.IsValidIngredientItem == false)
                {
                    continue;
                }

                IngredientSO ingredient = ingredientItem.Ingredient;
                if (ingredient == null || INGREDIENT_ICONS.ContainsKey(ingredient) == true)
                {
                    continue;
                }

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

            if (inventoryModule == null || ingredient == null || amount <= 0)
            {
                return false;
            }

            int remainingAmount = amount;
            int slotCapacity = inventoryModule.SlotCapacity;

            for (int i = 0; i < slotCapacity; i++)
            {
                InventorySlot slot = inventoryModule.GetSlot(i);
                if (slot == null || slot.IsEmpty == true)
                {
                    continue;
                }

                IngredientItemDataSO ingredientItem = slot.Item as IngredientItemDataSO;
                if (ingredientItem == null
                    || ingredientItem.IsValidIngredientItem == false
                    || ingredientItem.Ingredient != ingredient)
                {
                    continue;
                }

                int itemRemovedAmount = PlayerInventoryItemEvents.RequestRemoveItem(
                    ingredientItem,
                    remainingAmount,
                    out bool handled,
                    out string reason);
                if (handled == false)
                {
                    Debug.LogWarning($"Ingredient consume request was not handled. reason={reason}", this);
                    return false;
                }

                if (itemRemovedAmount <= 0)
                {
                    continue;
                }

                removedAmount += itemRemovedAmount;
                remainingAmount -= itemRemovedAmount;

                if (remainingAmount <= 0)
                {
                    return true;
                }
            }

            return removedAmount >= amount;
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
