using System.Collections.Generic;
using Work.Cook.Code.Data;
using Work.Players.Code.Inventory;
using Work.Cook.Code.Runtime.Core;
using Work.Cook.Code.Runtime.Integration;
using Work.Cook.Code.Runtime.Systems;
using Work.Cook.Code.Runtime.UI;

namespace Work.Cook.Code.Runtime.Integration
{
    /// <summary>
    /// 플레이어 인벤토리의 재료 아이템을 조리 재료 수량으로 변환
    /// </summary>
    public static class InventoryIngredientQuery
    {
        /// <summary>
        /// 인벤토리 내 조리 재료별 보유 수량 딕셔너리 생성
        /// </summary>
        /// <param name="inventory">조회할 플레이어 인벤토리</param>
        /// <returns>조리 재료별 보유 수량</returns>
        public static Dictionary<IngredientSO, int> BuildIngredientAmounts(PlayerInventoryModule inventory)
        {
            Dictionary<IngredientSO, int> ingredientAmounts = new Dictionary<IngredientSO, int>();
            FillIngredientAmounts(inventory, ingredientAmounts);
            return ingredientAmounts;
        }

        /// <summary>
        /// 인벤토리 내 조리 재료별 보유 수량을 지정 딕셔너리에 채움
        /// </summary>
        /// <param name="inventory">조회할 플레이어 인벤토리</param>
        /// <param name="results">결과를 채울 딕셔너리</param>
        public static void FillIngredientAmounts(PlayerInventoryModule inventory, Dictionary<IngredientSO, int> results)
        {
            if (results == null)
            {
                return;
            }

            results.Clear();

            if (inventory == null)
            {
                return;
            }

            int slotCapacity = inventory.SlotCapacity;

            for (int i = 0; i < slotCapacity; i++)
            {
                InventorySlot slot = inventory.GetSlot(i);

                if (slot == null || slot.IsEmpty == true)
                {
                    continue;
                }

                IngredientItemDataSO ingredientItem = slot.Item as IngredientItemDataSO;

                if (ingredientItem == null || ingredientItem.IsValidIngredientItem == false)
                {
                    continue;
                }

                AddIngredientAmount(results, ingredientItem.Ingredient, slot.Amount);
            }
        }

        /// <summary>
        /// 인벤토리 내 특정 조리 재료 보유 수량 조회
        /// </summary>
        /// <param name="inventory">조회할 플레이어 인벤토리</param>
        /// <param name="ingredient">조회할 조리 재료</param>
        /// <returns>보유 수량</returns>
        public static int GetIngredientAmount(PlayerInventoryModule inventory, IngredientSO ingredient)
        {
            if (inventory == null || ingredient == null)
            {
                return 0;
            }
            int slotCapacity = inventory.SlotCapacity;
            int totalAmount = 0;

            for (int i = 0; i < slotCapacity; i++)
            {
                InventorySlot slot = inventory.GetSlot(i);

                if (slot == null || slot.IsEmpty == true)
                {
                    continue;
                }

                IngredientItemDataSO ingredientItem = slot.Item as IngredientItemDataSO;

                if (ingredientItem == null || ingredientItem.IsValidIngredientItem == false)
                {
                    continue;
                }

                if (ingredientItem.Ingredient != ingredient)
                {
                    continue;
                }

                totalAmount += slot.Amount;
            }

            return totalAmount;
        }

        private static void AddIngredientAmount(Dictionary<IngredientSO, int> results, IngredientSO ingredient, int amount)
        {
            if (ingredient == null || amount <= 0)
            {
                return;
            }

            if (results.TryGetValue(ingredient, out int currentAmount) == true)
            {
                results[ingredient] = currentAmount + amount;
                return;
            }

            results.Add(ingredient, amount);
        }
    }
}
