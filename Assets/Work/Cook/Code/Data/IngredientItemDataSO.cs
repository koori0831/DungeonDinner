using UnityEngine;
using Work.Items.Code;

namespace Work.Cook.Code.Data
{
    /// <summary>
    /// 인벤토리 아이템과 조리 재료 데이터를 연결하는 재료 아이템 데이터
    /// </summary>
    [CreateAssetMenu(menuName = "Items/Ingredient Item")]
    public sealed class IngredientItemDataSO : ItemDataSO
    {
        [SerializeField]
        private IngredientSO ingredient;

        /// <summary>
        /// 연결된 조리 재료 데이터
        /// </summary>
        public IngredientSO Ingredient => ingredient;

        /// <summary>
        /// 조리 재료로 사용할 수 있는 아이템인지 반환
        /// </summary>
        public bool IsValidIngredientItem => HasCategoryRole(ItemCategoryRole.Ingredient) == true && ingredient != null;
    }
}
