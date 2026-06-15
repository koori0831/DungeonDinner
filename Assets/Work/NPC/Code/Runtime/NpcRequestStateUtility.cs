using System;

namespace Work.NPC.Code.Runtime
{
    public static class NpcRequestStateUtility
    {
        public static bool TryParse(string value, out NpcRequestState state)
        {
            state = NpcRequestState.Locked;
            return string.IsNullOrWhiteSpace(value) == false
                   && Enum.TryParse(value.Trim(), true, out state);
        }

        public static int GetRank(NpcRequestState state)
        {
            return (int)state;
        }

        public static bool IsAtLeast(NpcRequestState currentState, NpcRequestState requiredState)
        {
            return GetRank(currentState) >= GetRank(requiredState);
        }

        public static bool IsBlockedAtOrAfter(NpcRequestState currentState, NpcRequestState blockedState)
        {
            return GetRank(currentState) >= GetRank(blockedState);
        }
    }
}
