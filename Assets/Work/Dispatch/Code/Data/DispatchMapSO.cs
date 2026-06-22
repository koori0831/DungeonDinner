using System.Collections.Generic;
using UnityEngine;

namespace Work.Dispatch.Code.Data
{
    /// <summary>
    /// 파견 지도와 방문 가능한 포인트 목록 데이터
    /// </summary>
    [CreateAssetMenu(fileName = "DispatchMap", menuName = "Dungeon Dinner/Dispatch/Map")]
    public sealed class DispatchMapSO : ScriptableObject
    {
        [SerializeField]
        private string displayName;

        [SerializeField]
        [TextArea]
        private string description;

        [SerializeField]
        private List<DispatchPointSO> points = new List<DispatchPointSO>();

        /// <summary>
        /// 표시용 지도 이름
        /// </summary>
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) == false ? displayName : name;

        /// <summary>
        /// 표시용 지도 설명
        /// </summary>
        public string Description => description;

        /// <summary>
        /// 방문 가능한 파견 포인트 목록
        /// </summary>
        public IReadOnlyList<DispatchPointSO> Points => points;

        /// <summary>
        /// 식별자에 맞는 파견 포인트 검색
        /// </summary>
        /// <param name="pointId">검색할 포인트 식별자</param>
        /// <returns>검색된 파견 포인트</returns>
        public DispatchPointSO FindPointById(string pointId)
        {
            if (string.IsNullOrWhiteSpace(pointId) == true || points == null)
            {
                return null;
            }

            for (int i = 0; i < points.Count; i++)
            {
                DispatchPointSO point = points[i];
                if (point != null && string.Equals(point.PointId, pointId, System.StringComparison.OrdinalIgnoreCase) == true)
                {
                    return point;
                }
            }

            return null;
        }
    }
}
