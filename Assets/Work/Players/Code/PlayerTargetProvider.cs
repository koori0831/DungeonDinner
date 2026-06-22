using UnityEngine;

namespace Work.Players.Code
{
    /// <summary>
    /// 단일 플레이어 Transform 제공자
    /// </summary>
    public static class PlayerTargetProvider
    {
        private static Transform _target;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Clear()
        {
            _target = null;
        }

        /// <summary>
        /// 현재 플레이어 Transform 등록
        /// </summary>
        /// <param name="target">등록할 플레이어 Transform</param>
        public static void Register(Transform target)
        {
            if (target == null)
            {
                return;
            }

            _target = target;
        }

        /// <summary>
        /// 현재 플레이어 Transform 등록 해제
        /// </summary>
        /// <param name="target">등록 해제할 플레이어 Transform</param>
        public static void Unregister(Transform target)
        {
            if (_target != target)
            {
                return;
            }

            _target = null;
        }

        /// <summary>
        /// 현재 등록된 플레이어 Transform 조회
        /// </summary>
        /// <param name="target">조회된 플레이어 Transform</param>
        /// <returns>조회 성공 여부</returns>
        public static bool TryGetTarget(out Transform target)
        {
            if (IsValidTarget(_target) == false)
            {
                _target = null;
                target = null;
                return false;
            }

            target = _target;
            return true;
        }

        private static bool IsValidTarget(Transform target)
        {
            return target != null && target.gameObject.activeInHierarchy == true;
        }
    }
}
