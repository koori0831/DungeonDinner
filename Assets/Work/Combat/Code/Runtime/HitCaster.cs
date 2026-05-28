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
        private QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;

        [SerializeField]
        [Min(1)]
        private int maxColliderCount = 64;

        private Collider[] _colliderResults;

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

            int colliderCount = Physics.OverlapCapsuleNonAlloc(
                startPoint,
                endPoint,
                request.Radius,
                _colliderResults,
                request.TargetLayerMask,
                queryTriggerInteraction
            );

            int resultCount = 0;

            for (int i = 0; i < colliderCount; i++)
            {
                Collider targetCollider = _colliderResults[i];

                if (targetCollider == null)
                {
                    continue;
                }

                if (IsSelf(targetCollider, request.Owner) == true)
                {
                    continue;
                }

                IHitable hitable = targetCollider.GetComponentInParent<IHitable>();

                if (hitable == null)
                {
                    continue;
                }

                if (ContainsHitable(results, resultCount, hitable) == true)
                {
                    continue;
                }

                results[resultCount] = CreateHitCastResult(in request, targetCollider, hitable, capsuleCenter);
                resultCount++;

                if (resultCount >= results.Length)
                {
                    break;
                }
            }

            return resultCount;
        }

        private static HitCastResult CreateHitCastResult(
            in HitCastRequest request,
            Collider targetCollider,
            IHitable hitable,
            Vector3 capsuleCenter
        )
        {
            Vector3 hitPoint = targetCollider.ClosestPoint(capsuleCenter);
            Vector3 hitDirection = GetHitDirection(in request, targetCollider, hitable);

            return new HitCastResult(hitable, targetCollider, hitPoint, hitDirection);
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

        private static bool IsSelf(Collider targetCollider, GameObject owner)
        {
            if (owner == null)
            {
                return false;
            }

            Transform ownerTransform = owner.transform;
            Transform targetTransform = targetCollider.transform;

            return targetTransform == ownerTransform || targetTransform.IsChildOf(ownerTransform);
        }

        private static Vector3 GetHitDirection(in HitCastRequest request, Collider targetCollider, IHitable hitable)
        {
            Vector3 origin = request.Owner != null ? request.Owner.transform.position : request.Origin;
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

        private static bool ContainsHitable(HitCastResult[] results, int count, IHitable target)
        {
            for (int i = 0; i < count; i++)
            {
                if (ReferenceEquals(results[i].Hitable, target) == true)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
