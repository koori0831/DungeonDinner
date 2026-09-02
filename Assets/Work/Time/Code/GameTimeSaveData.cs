using System;

namespace Work.TimeSystem
{
    [Serializable]
    public sealed class GameTimeSaveData
    {
        public const int CurrentSaveVersion = 1;

        public int SaveVersion = CurrentSaveVersion;
        public int TotalElapsedTime;
    }
}
