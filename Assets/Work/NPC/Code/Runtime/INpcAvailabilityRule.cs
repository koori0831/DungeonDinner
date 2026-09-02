namespace Work.NPC.Code.Runtime
{
    /// <summary>
    /// NPC가 음식점 및 일반 조우 후보에 포함될 수 있는지 판단하는 외부 규칙입니다.
    /// </summary>
    public interface INpcAvailabilityRule
    {
        bool IsNpcAvailable(string npcId);
    }
}
