using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Work.Dispatch.Code.Data
{
    /// <summary>
    /// 지도에서 선택 가능한 단일 파견 포인트 데이터
    /// </summary>
    [CreateAssetMenu(fileName = "DispatchPoint", menuName = "Dungeon Dinner/Dispatch/Point")]
    public sealed class DispatchPointSO : ScriptableObject
    {
        private const float MIN_DURATION_SECONDS = 2f;
        private const float MAX_DURATION_SECONDS = 4f;

        [SerializeField]
        private string pointId;

        [SerializeField]
        private string displayName;

        [SerializeField]
        [TextArea]
        private string description;

        [SerializeField]
        private Sprite icon;

        [SerializeField]
        private Vector2 normalizedMapPosition = new Vector2(0.5f, 0.5f);

        [SerializeField]
        [Range(MIN_DURATION_SECONDS, MAX_DURATION_SECONDS)]
        private float durationSeconds = 3f;

        [SerializeField]
        private List<DispatchRewardEntry> rewards = new List<DispatchRewardEntry>();

        /// <summary>
        /// 파견 포인트 식별자
        /// </summary>
        public string PointId => pointId;

        /// <summary>
        /// 표시용 파견 포인트 이름
        /// </summary>
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) == false ? displayName : name;

        /// <summary>
        /// 표시용 파견 포인트 설명
        /// </summary>
        public string Description => description;

        /// <summary>
        /// 진행 UI에 표시할 파견 포인트 아이콘
        /// </summary>
        public Sprite Icon => icon;

        /// <summary>
        /// 지도 패널 내 정규화 위치
        /// </summary>
        public Vector2 NormalizedMapPosition => new Vector2(
            Mathf.Clamp01(normalizedMapPosition.x),
            Mathf.Clamp01(normalizedMapPosition.y));

        /// <summary>
        /// 파견 진행 시간
        /// </summary>
        public float DurationSeconds => Mathf.Clamp(durationSeconds, MIN_DURATION_SECONDS, MAX_DURATION_SECONDS);

        /// <summary>
        /// 파견 완료 시 지급할 보상 목록
        /// </summary>
        public IReadOnlyList<DispatchRewardEntry> Rewards => rewards;

        /// <summary>
        /// 지급 가능한 보상이 하나 이상 있는지 여부
        /// </summary>
        public bool HasValidReward => CountValidRewards() > 0;

        /// <summary>
        /// UI 표시용 보상 목록 텍스트 생성
        /// </summary>
        /// <returns>보상 목록 텍스트</returns>
        public string BuildRewardSummaryText()
        {
            if (rewards == null || rewards.Count == 0)
            {
                return "보상 없음";
            }

            StringBuilder builder = new StringBuilder();
            int appendedCount = 0;

            for (int i = 0; i < rewards.Count; i++)
            {
                DispatchRewardEntry reward = rewards[i];
                if (reward == null || reward.IsValid == false)
                {
                    continue;
                }

                if (appendedCount > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(reward.BuildDisplayText());
                appendedCount++;
            }

            return appendedCount > 0 ? builder.ToString() : "보상 없음";
        }

        private int CountValidRewards()
        {
            if (rewards == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < rewards.Count; i++)
            {
                DispatchRewardEntry reward = rewards[i];
                if (reward != null && reward.IsValid == true)
                {
                    count++;
                }
            }

            return count;
        }

        private void OnValidate()
        {
            normalizedMapPosition = new Vector2(
                Mathf.Clamp01(normalizedMapPosition.x),
                Mathf.Clamp01(normalizedMapPosition.y));
            durationSeconds = Mathf.Clamp(durationSeconds, MIN_DURATION_SECONDS, MAX_DURATION_SECONDS);
        }
    }
}
