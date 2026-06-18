using System.Collections.Generic;
using UnityEngine;
using Work.Core.EventBus;
using static Work.Items.Code.WorldLootEvents;

namespace Work.Items.Code
{
    /// <summary>
    /// 월드 루팅 아이템의 감지 트리거와 감지 이벤트 발행 담당
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WorldLootDetector : MonoBehaviour
    {
        [SerializeField]
        private WorldLootItem lootItem;

        [SerializeField]
        private Collider detectionCollider;

        [SerializeField]
        private Rigidbody detectionRigidbody;

        private readonly Dictionary<int, CharacterController> DETECTED_CONTROLLERS = new Dictionary<int, CharacterController>();
        private readonly Dictionary<int, int> DETECTED_CONTROLLER_COUNTS = new Dictionary<int, int>();

        private void Awake()
        {
            ResolveReferences();
            ConfigureDetectionCollider();
            ConfigureDetectionRigidbody();
        }

        private void OnDisable()
        {
            PublishLostEventsForDetectedControllers();
            DETECTED_CONTROLLERS.Clear();
            DETECTED_CONTROLLER_COUNTS.Clear();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (TryGetCollectorController(other, out CharacterController collectorController) == false)
            {
                return;
            }

            RegisterCollectorController(collectorController);
        }

        private void OnTriggerExit(Collider other)
        {
            if (TryGetCollectorController(other, out CharacterController collectorController) == false)
            {
                return;
            }

            UnregisterCollectorController(collectorController);
        }

        /// <summary>
        /// 감지 이벤트에 사용할 월드 루팅 아이템 지정
        /// </summary>
        /// <param name="newLootItem">연결할 월드 루팅 아이템</param>
        public void SetLootItem(WorldLootItem newLootItem)
        {
            lootItem = newLootItem;
        }

        /// <summary>
        /// 현재 감지 중인 수집 주체에게 루팅 아이템 감지 이벤트 재발행
        /// </summary>
        public void PublishDetectedEventsForDetectedControllers()
        {
            if (lootItem == null || lootItem.IsLootable == false)
            {
                return;
            }

            foreach (KeyValuePair<int, CharacterController> kvp in DETECTED_CONTROLLERS)
            {
                CharacterController collectorController = kvp.Value;

                if (collectorController == null)
                {
                    continue;
                }

                Bus<WorldLootDetectedEvent>.Raise(new WorldLootDetectedEvent(lootItem, collectorController));
            }
        }

        private void RegisterCollectorController(CharacterController collectorController)
        {
            if (collectorController == null)
            {
                return;
            }

            int controllerId = collectorController.GetInstanceID();

            if (DETECTED_CONTROLLER_COUNTS.TryGetValue(controllerId, out int enterCount) == true)
            {
                DETECTED_CONTROLLER_COUNTS[controllerId] = enterCount + 1;
                return;
            }

            DETECTED_CONTROLLERS.Add(controllerId, collectorController);
            DETECTED_CONTROLLER_COUNTS.Add(controllerId, 1);

            if (lootItem == null || lootItem.IsLootable == false)
            {
                return;
            }

            Bus<WorldLootDetectedEvent>.Raise(new WorldLootDetectedEvent(lootItem, collectorController));
        }

        private void UnregisterCollectorController(CharacterController collectorController)
        {
            if (lootItem == null || collectorController == null)
            {
                return;
            }

            int controllerId = collectorController.GetInstanceID();

            if (DETECTED_CONTROLLER_COUNTS.TryGetValue(controllerId, out int enterCount) == false)
            {
                return;
            }

            enterCount--;

            if (enterCount > 0)
            {
                DETECTED_CONTROLLER_COUNTS[controllerId] = enterCount;
                return;
            }

            DETECTED_CONTROLLER_COUNTS.Remove(controllerId);
            DETECTED_CONTROLLERS.Remove(controllerId);
            Bus<WorldLootLostEvent>.Raise(new WorldLootLostEvent(lootItem, collectorController));
        }

        private bool TryGetCollectorController(Collider other, out CharacterController collectorController)
        {
            collectorController = null;

            if (other == null)
            {
                return false;
            }

            collectorController = other.GetComponentInParent<CharacterController>();
            return collectorController != null;
        }

        private void ResolveReferences()
        {
            if (lootItem == null)
            {
                lootItem = GetComponentInParent<WorldLootItem>();
            }

            if (detectionCollider == null)
            {
                detectionCollider = GetComponent<Collider>();
            }

            if (detectionRigidbody == null)
            {
                detectionRigidbody = GetComponent<Rigidbody>();
            }
        }

        private void ConfigureDetectionCollider()
        {
            if (detectionCollider == null)
            {
                SphereCollider sphereCollider = gameObject.AddComponent<SphereCollider>();
                sphereCollider.radius = 0.5f;
                detectionCollider = sphereCollider;
            }

            detectionCollider.isTrigger = true;
        }

        private void ConfigureDetectionRigidbody()
        {
            if (detectionRigidbody == null)
            {
                detectionRigidbody = gameObject.AddComponent<Rigidbody>();
            }

            detectionRigidbody.isKinematic = true;
            detectionRigidbody.useGravity = false;
        }

        private void PublishLostEventsForDetectedControllers()
        {
            if (lootItem == null)
            {
                return;
            }

            foreach (KeyValuePair<int, CharacterController> kvp in DETECTED_CONTROLLERS)
            {
                CharacterController collectorController = kvp.Value;

                if (collectorController == null)
                {
                    continue;
                }

                Bus<WorldLootLostEvent>.Raise(new WorldLootLostEvent(lootItem, collectorController));
            }
        }

        private void OnValidate()
        {
            if (detectionCollider != null)
            {
                detectionCollider.isTrigger = true;
            }

            if (detectionRigidbody != null)
            {
                detectionRigidbody.isKinematic = true;
                detectionRigidbody.useGravity = false;
            }
        }
    }
}
