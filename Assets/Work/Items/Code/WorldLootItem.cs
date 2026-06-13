using UnityEngine;

namespace Work.Items.Code
{
    /// <summary>
    /// 월드에 떨어진 루팅 가능한 아이템 스택
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WorldLootItem : MonoBehaviour
    {
        private const int MIN_AMOUNT = 1;

        [SerializeField]
        private ItemDataSO item;

        [SerializeField]
        [Min(MIN_AMOUNT)]
        private int amount = MIN_AMOUNT;

        [SerializeField]
        private Collider lootCollider;

        [SerializeField]
        private bool destroyWhenEmpty = true;

        /// <summary>
        /// 루팅 가능한 아이템 데이터
        /// </summary>
        public ItemDataSO Item => item;

        /// <summary>
        /// 월드에 남아있는 아이템 수량
        /// </summary>
        public int Amount => amount;

        /// <summary>
        /// 현재 루팅 가능한 상태 여부
        /// </summary>
        public bool IsLootable => item != null && amount > 0 && isActiveAndEnabled == true;

        private void Awake()
        {
            ResolveLootCollider();
            UpdateObjectName();
        }

        /// <summary>
        /// 월드 루팅 아이템 데이터 초기화
        /// </summary>
        /// <param name="newItem">루팅 가능한 아이템 데이터</param>
        /// <param name="newAmount">루팅 가능한 아이템 수량</param>
        public void Initialize(ItemDataSO newItem, int newAmount)
        {
            item = newItem;
            amount = Mathf.Max(MIN_AMOUNT, newAmount);
            ResolveLootCollider();
            UpdateObjectName();
        }

        /// <summary>
        /// 인벤토리 추가 요청용 아이템 스택 생성
        /// </summary>
        /// <returns>아이템 스택 값</returns>
        public InventoryItemStack CreateItemStack()
        {
            if (IsLootable == false)
            {
                return default;
            }

            return new InventoryItemStack(item, amount);
        }

        /// <summary>
        /// 루팅 완료된 수량만큼 월드 아이템 수량 차감
        /// </summary>
        /// <param name="requestedAmount">차감 요청 수량</param>
        /// <returns>실제로 차감된 수량</returns>
        public int ConsumeAmount(int requestedAmount)
        {
            if (IsLootable == false || requestedAmount <= 0)
            {
                return 0;
            }

            int consumedAmount = Mathf.Min(amount, requestedAmount);
            amount -= consumedAmount;

            if (amount <= 0)
            {
                amount = 0;
                Deplete();
                return consumedAmount;
            }

            UpdateObjectName();
            return consumedAmount;
        }

        private void ResolveLootCollider()
        {
            if (lootCollider == null)
            {
                lootCollider = GetComponent<Collider>();
            }

            if (lootCollider == null)
            {
                SphereCollider sphereCollider = gameObject.AddComponent<SphereCollider>();
                sphereCollider.radius = 0.5f;
                lootCollider = sphereCollider;
            }

            lootCollider.isTrigger = true;
        }

        private void Deplete()
        {
            if (destroyWhenEmpty == true)
            {
                Destroy(gameObject);
                return;
            }

            gameObject.SetActive(false);
        }

        private void UpdateObjectName()
        {
            if (item == null)
            {
                gameObject.name = nameof(WorldLootItem);
                return;
            }

            gameObject.name = $"{nameof(WorldLootItem)}_{item.DisplayName}_x{amount}";
        }

        private void OnValidate()
        {
            amount = Mathf.Max(MIN_AMOUNT, amount);

            if (lootCollider != null)
            {
                lootCollider.isTrigger = true;
            }
        }
    }
}
