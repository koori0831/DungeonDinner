using System;
using System.Collections.Generic;
using UnityEngine;
using Work.Cook.Code.Data;
using Work.Players.Code.Inventory;

namespace Work.Cook.Code.Runtime
{
    /// <summary>
    /// 플레이어 인벤토리의 재료 아이템을 조리 재료 선택 소스로 제공
    /// </summary>
    public sealed class InventoryCookingIngredientSource : MonoBehaviour, ICookingIngredientSource, ICookingIngredientQuantitySource
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
