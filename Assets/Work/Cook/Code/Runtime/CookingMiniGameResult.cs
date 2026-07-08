using Work.Cook.Code.Data;

namespace Work.Cook.Code.Runtime
{
    public sealed class CookingMiniGameResult
    {
        public CookingMiniGameType MiniGameType { get; }
        public CookingMiniGameGrade Grade { get; }
        public float Score { get; }
        public int QualityDelta { get; }
        public string FeedbackText { get; }

        public CookingMiniGameResult(
            CookingMiniGameType miniGameType,
            CookingMiniGameGrade grade,
            float score,
            int qualityDelta,
            string feedbackText)
        {
            MiniGameType = miniGameType;
            Grade = grade;
            Score = score;
            QualityDelta = qualityDelta;
            FeedbackText = feedbackText ?? string.Empty;
        }

        public string BuildDebugSummary()
        {
            return $"type={MiniGameType}, grade={Grade}, score={Score:0.00}, qualityDelta={QualityDelta}";
        }
    }
}
