using UnityEngine;
using Work.Cook.Code.Data;
using Work.Cook.Code.Runtime.Core;

namespace Work.Cook.Code.Runtime.UI
{
    /// <summary>
    /// 조리 미니게임 공통 등급 및 기본 결과 생성
    /// </summary>
    public static class CookingMiniGameUtility
    {
        public static CookingMiniGameGrade ResolveGrade(float score)
        {
            if (score >= 0.9f)
                return CookingMiniGameGrade.Perfect;
            if (score >= 0.7f)
                return CookingMiniGameGrade.Good;
            if (score >= 0.45f)
                return CookingMiniGameGrade.Normal;

            return CookingMiniGameGrade.Bad;
        }

        public static CookingMiniGameResult CreateResult(
            CookingMiniGameType miniGameType,
            CookingMiniGameGrade grade,
            float score,
            string feedbackText)
        {
            return new CookingMiniGameResult(
                miniGameType,
                grade,
                Mathf.Clamp01(score),
                ResolveQualityDelta(grade),
                feedbackText);
        }

        private static int ResolveQualityDelta(CookingMiniGameGrade grade)
        {
            switch (grade)
            {
                case CookingMiniGameGrade.Perfect:
                    return 2;
                case CookingMiniGameGrade.Good:
                    return 1;
                case CookingMiniGameGrade.Bad:
                    return -1;
                default:
                    return 0;
            }
        }
    }
}
