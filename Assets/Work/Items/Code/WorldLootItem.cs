using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Work.Core.ObjectPool.RunTime;

namespace Work.Items.Code
{
    /// <summary>
    /// 월드에 떨어진 루팅 가능한 아이템 스택
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WorldLootItem : MonoBehaviour, IPoolable
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
        private WorldLootDetector lootDetector;

        [SerializeField]
        private PoolItemSO poolItem;

        [SerializeField]
        private bool returnToPoolWhenEmpty = true;

        [SerializeField]
        private bool destroyWhenEmpty = true;

        private Pool _pool;
        private bool _isDropAnimating;
        private CancellationTokenSource _dropArcCancellationTokenSource;

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
        public bool IsLootable => item != null && amount > 0 && isActiveAndEnabled == true && _isDropAnimating == false;

        /// <summary>
        /// 풀 매니저에서 사용하는 풀 아이템 데이터
        /// </summary>
        public PoolItemSO PoolItem => poolItem;

        /// <summary>
        /// 풀 매니저에서 제어할 게임 오브젝트
        /// </summary>
        public GameObject GameObject => gameObject;

        private void Awake()
        {
            ResolveLootCollider();
            ResolveLootDetector();
            UpdateObjectName();
        }

        private void OnDisable()
        {
            CancelDropArc();
            _isDropAnimating = false;
        }

        /// <summary>
        /// 월드 루팅 아이템 데이터 초기화
        /// </summary>
        /// <param name="newItem">루팅 가능한 아이템 데이터</param>
        /// <param name="newAmount">루팅 가능한 아이템 수량</param>
        public void Initialize(ItemDataSO newItem, int newAmount)
        {
            CancelDropArc();
            _isDropAnimating = false;
            item = newItem;
            amount = Mathf.Max(MIN_AMOUNT, newAmount);
            ResolveLootCollider();
            ResolveLootDetector();
            UpdateObjectName();
        }

        /// <summary>
        /// 시작 위치에서 착지 위치까지 포물선 드랍 연출 재생
        /// </summary>
        /// <param name="startPosition">드랍 시작 위치</param>
        /// <param name="endPosition">드랍 착지 위치</param>
        /// <param name="arcHeight">포물선 최고 높이</param>
        /// <param name="duration">드랍 연출 시간</param>
        public void PlayDropArc(Vector3 startPosition, Vector3 endPosition, float arcHeight, float duration)
        {
            CancelDropArc();
            transform.position = startPosition;

            if (duration <= 0f)
            {
                transform.position = endPosition;
                _isDropAnimating = false;
                PublishDetectedEventsAfterLanding();
                return;
            }

            _isDropAnimating = true;
            _dropArcCancellationTokenSource = new CancellationTokenSource();
            PlayDropArcAsync(startPosition, endPosition, Mathf.Max(0f, arcHeight), duration, _dropArcCancellationTokenSource.Token).Forget();
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

        /// <summary>
        /// 풀 인스턴스 연결
        /// </summary>
        /// <param name="pool">이 아이템을 관리하는 풀</param>
        public void SetUpPool(Pool pool)
        {
            _pool = pool;
        }

        /// <summary>
        /// 풀에서 다시 꺼낼 때 월드 루팅 아이템 상태 초기화
        /// </summary>
        public void ResetItem()
        {
            CancelDropArc();
            _isDropAnimating = false;
            item = null;
            amount = MIN_AMOUNT;
            ResolveLootCollider();
            ResolveLootDetector();
            UpdateObjectName();
        }

        private async UniTask PlayDropArcAsync(
            Vector3 startPosition,
            Vector3 endPosition,
            float arcHeight,
            float duration,
            CancellationToken cancellationToken
        )
        {
            try
            {
                float elapsedTime = 0f;

                while (elapsedTime < duration)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    float normalizedTime = Mathf.Clamp01(elapsedTime / duration);
                    SetDropArcPosition(startPosition, endPosition, arcHeight, normalizedTime);
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                    elapsedTime += Time.deltaTime;
                }

                transform.position = endPosition;
                _isDropAnimating = false;
                PublishDetectedEventsAfterLanding();
            }
            catch (OperationCanceledException)
            {
                // 풀 반환 또는 비활성화로 취소된 드랍 연출은 정상 흐름으로 처리
            }
        }

        private void SetDropArcPosition(Vector3 startPosition, Vector3 endPosition, float arcHeight, float normalizedTime)
        {
            Vector3 position = Vector3.Lerp(startPosition, endPosition, normalizedTime);
            position.y += Mathf.Sin(normalizedTime * Mathf.PI) * arcHeight;
            transform.position = position;
        }

        private void PublishDetectedEventsAfterLanding()
        {
            if (lootDetector == null)
            {
                return;
            }

            lootDetector.PublishDetectedEventsForDetectedControllers();
        }

        private void CancelDropArc()
        {
            if (_dropArcCancellationTokenSource == null)
            {
                return;
            }

            _dropArcCancellationTokenSource.Cancel();
            _dropArcCancellationTokenSource.Dispose();
            _dropArcCancellationTokenSource = null;
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

        private void ResolveLootDetector()
        {
            if (lootDetector == null)
            {
                lootDetector = GetComponent<WorldLootDetector>();
            }

            if (lootDetector == null)
            {
                lootDetector = gameObject.AddComponent<WorldLootDetector>();
            }

            lootDetector.SetLootItem(this);
        }

        private void Deplete()
        {
            if (_pool != null && returnToPoolWhenEmpty == true)
            {
                _pool.Push(this);
                return;
            }

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
