using UnityEngine;

namespace Work.Items.Code
{
    /// <summary>
    /// 루팅과 인벤토리에서 공통으로 사용하는 아이템 기본 데이터
    /// </summary>
    [CreateAssetMenu(menuName = "Items/Item Data")]
    public class ItemDataSO : ScriptableObject
    {
        private const int MIN_STACK_AMOUNT = 1;

        [SerializeField]
        private string itemId;

        [SerializeField]
        private string displayName;

        [SerializeField]
        [TextArea]
        private string description;

        [SerializeField]
        private Sprite icon;

        [SerializeField]
        private ItemCategorySO category;

        [SerializeField]
        private bool isStackable = true;

        [SerializeField]
        [Min(MIN_STACK_AMOUNT)]
        private int maxStackAmount = 99;

        /// <summary>
        /// 아이템 식별자
        /// </summary>
        public string ItemId => itemId;

        /// <summary>
        /// 표시용 아이템 이름
        /// </summary>
        public string DisplayName => string.IsNullOrEmpty(displayName) == false ? displayName : name;

        /// <summary>
        /// 표시용 아이템 설명
        /// </summary>
        public string Description => description;

        /// <summary>
        /// 인벤토리 UI 표시용 아이콘
        /// </summary>
        public Sprite Icon => icon;

        /// <summary>
        /// 아이템 대분류 데이터
        /// </summary>
        public ItemCategorySO Category => category;

        /// <summary>
        /// 동일 아이템 스택 가능 여부
        /// </summary>
        public bool IsStackable => isStackable;

        /// <summary>
        /// 단일 슬롯 최대 보관 수량
        /// </summary>
        public int MaxStackAmount => isStackable == true ? Mathf.Max(MIN_STACK_AMOUNT, maxStackAmount) : MIN_STACK_AMOUNT;

        /// <summary>
        /// 지정 카테고리인지 반환
        /// </summary>
        /// <param name="targetCategory">비교할 아이템 카테고리</param>
        /// <returns>동일 카테고리 여부</returns>
        public bool HasCategory(ItemCategorySO targetCategory)
        {
            if (targetCategory == null)
            {
                return false;
            }

            return category == targetCategory;
        }

        /// <summary>
        /// 지정 카테고리 역할을 가진 아이템인지 반환
        /// </summary>
        /// <param name="role">비교할 카테고리 역할</param>
        /// <returns>동일 카테고리 역할 여부</returns>
        public bool HasCategoryRole(ItemCategoryRole role)
        {
            if (category == null)
            {
                return false;
            }

            return category.HasRole(role);
        }

        /// <summary>
        /// 다른 아이템 데이터와 같은 스택에 담을 수 있는지 반환
        /// </summary>
        /// <param name="otherItem">비교할 아이템 데이터</param>
        /// <returns>동일 스택 사용 가능 여부</returns>
        public bool CanStackWith(ItemDataSO otherItem)
        {
            if (otherItem == null)
            {
                return false;
            }

            if (isStackable == false)
            {
                return false;
            }

            return otherItem == this;
        }

        protected virtual void OnValidate()
        {
            if (isStackable == false)
            {
                maxStackAmount = MIN_STACK_AMOUNT;
                return;
            }

            maxStackAmount = Mathf.Max(MIN_STACK_AMOUNT, maxStackAmount);
        }
    }
}
