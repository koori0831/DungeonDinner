using System.Collections.Generic;
using UnityEngine;
using Work.Combat.Code.Core;

namespace Work.Combat.Code.Runtime
{
    /// <summary>
    /// 공격 범위 내의 피격 가능 대상 탐색 담당 컴포넌트
    /// </summary>
    public sealed class HitCaster : MonoBehaviour, IHitCaster
    {
        private const float MIN_DIRECTION_SQR_MAGNITUDE = 0.0001f;

        [SerializeField]
        private QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Collide;

        [SerializeField]
        [Min(1)]
        private int maxColliderCount = 64;

        private Collider[] _colliderResults;
        private readonly Dictionary<int, IHitable> HITABLE_BY_COLLIDER_ID = new Dictionary<int, IHitable>();
        private readonly HashSet<IHitable> UNIQUE_HITABLES = new HashSet<IHitable>();

        private void Awake()
        {
            EnsureColliderBuffer();
        }

        /// <summary>
        /// 범위 안의 피격 가능 대상 탐색
        /// </summary>
        /// <param name="request">타격 범위 탐색 요청</param>
        /// <param name="results">피격 가능 대상별 판정 결과 배열</param>
        /// <returns>결과 배열에 저장된 대상 수</returns>
        public int Cast(in HitCastRequest request, HitCastResult[] results)
        {
            if (results == null || results.Length == 0)
            {
                return 0;
            }

            if (request.Radius <= 0f)
            {
                return 0;
            }

            EnsureColliderBuffer();

            Vector3 direction = GetNormalizedDirection(request.Direction);
            float range = Mathf.Max(0f, request.Range);
            Vector3 startPoint = request.Origin;
            Vector3 endPoint = request.Origin + direction * range;
            Vector3 capsuleCenter = (startPoint + endPoint) * 0.5f;
            Transform ownerTransform = request.Owner != null ? request.Owner.transform : null;

            int colliderCount = Physics.OverlapCapsuleNonAlloc(
                startPoint,
                endPoint,
                request.Radius,
                _colliderResults,
                request.TargetLayerMask,
                queryTriggerInteraction
            );

            int resultCount = 0;
            UNIQUE_HITABLES.Clear();

            for (int i = 0; i < colliderCount; i++)
            {
                Collider targetCollider = _colliderResults[i];

                if (targetCollider == null)
                {
                    continue;
                }

                if (IsSelf(targetCollider, ownerTransform) == true)
                {
                    continue;
                }

                IHitable hitable = GetCachedHitable(targetCollider);

                if (IsMissingHitable(hitable) == true)
                {
                    continue;
                }

                if (UNIQUE_HITABLES.Add(hitable) == false)
                {
                    continue;
                }

                results[resultCount] = CreateHitCastResult(in request, targetCollider, hitable, capsuleCenter, ownerTransform);
                resultCount++;

                if (resultCount >= results.Length)
                {
                    break;
                }
            }

            UNIQUE_HITABLES.Clear();
            return resultCount;
        }

        private static HitCastResult CreateHitCastResult(
            in HitCastRequest request,
            Collider targetCollider,
            IHitable hitable,
            Vector3 capsuleCenter,
            Transform ownerTransform
        )
        {
            Vector3 hitPoint = targetCollider.ClosestPoint(capsuleCenter);
            Vector3 hitDirection = GetHitDirection(in request, targetCollider, hitable, ownerTransform);

            return new HitCastResult(hitable, targetCollider, hitPoint, hitDirection);
        }

        private void OnDisable()
        {
            HITABLE_BY_COLLIDER_ID.Clear();
            UNIQUE_HITABLES.Clear();
        }

        private void EnsureColliderBuffer()
        {
            int capacity = Mathf.Max(1, maxColliderCount);

            if (_colliderResults != null && _colliderResults.Length == capacity)
            {
                return;
            }

            _colliderResults = new Collider[capacity];
        }

        private static Vector3 GetNormalizedDirection(Vector3 direction)
        {
            if (direction.sqrMagnitude <= MIN_DIRECTION_SQR_MAGNITUDE)
            {
                return Vector3.zero;
            }

            return direction.normalized;
        }

        private IHitable GetCachedHitable(Collider targetCollider)
        {
            int colliderId = targetCollider.GetInstanceID();

            if (HITABLE_BY_COLLIDER_ID.TryGetValue(colliderId, out IHitable cachedHitable) == true)
            {
                return cachedHitable;
            }

            IHitable hitable = targetCollider.GetComponentInParent<IHitable>();
            HITABLE_BY_COLLIDER_ID.Add(colliderId, hitable);
            return hitable;
        }

        private static bool IsSelf(Collider targetCollider, Transform ownerTransform)
        {
            if (ownerTransform == null)
            {
                return false;
            }

            Transform targetTransform = targetCollider.transform;

            return targetTransform == ownerTransform || targetTransform.IsChildOf(ownerTransform);
        }

        private static Vector3 GetHitDirection(in HitCastRequest request, Collider targetCollider, IHitable hitable, Transform ownerTransform)
        {
            Vector3 origin = ownerTransform != null ? ownerTransform.position : request.Origin;
            Vector3 targetPosition = GetTargetPosition(targetCollider, hitable);
            Vector3 rawDirection = targetPosition - origin;

            if (rawDirection.sqrMagnitude <= MIN_DIRECTION_SQR_MAGNITUDE)
            {
                return GetNormalizedDirection(request.Direction);
            }

            return rawDirection.normalized;
        }

        private static Vector3 GetTargetPosition(Collider targetCollider, IHitable hitable)
        {
            if (hitable is Component hitableComponent)
            {
                return hitableComponent.transform.position;
            }

            return targetCollider.bounds.center;
        }

        private static bool IsMissingHitable(IHitable hitable)
        {
            if (hitable == null)
            {
                return true;
            }

            if (hitable is Component hitableComponent)
            {
                return hitableComponent == null;
            }

            return false;
        }

    }
}
