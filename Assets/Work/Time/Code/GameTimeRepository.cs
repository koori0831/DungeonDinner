using UnityEngine;

namespace Work.TimeSystem
{
    /// <summary>
    /// 기존 NPC 저장 방식과 동일하게 PlayerPrefs와 JsonUtility를 사용하는 시간 저장소입니다.
    /// </summary>
    public sealed class GameTimeRepository
    {
        public const string DefaultSaveKey = "DungeonDinner.GameTime";

        private readonly string _saveKey;

        public GameTimeRepository(string saveKey = DefaultSaveKey)
        {
            _saveKey = string.IsNullOrWhiteSpace(saveKey) ? DefaultSaveKey : saveKey;
        }

        public GameTimeSaveData Load(int fallbackTotalElapsedTime = 0)
        {
            if (PlayerPrefs.HasKey(_saveKey) == false)
            {
                return CreateFallback(fallbackTotalElapsedTime);
            }

            string json = PlayerPrefs.GetString(_saveKey, string.Empty);
            if (string.IsNullOrWhiteSpace(json))
            {
                return CreateFallback(fallbackTotalElapsedTime);
            }

            try
            {
                GameTimeSaveData saveData = JsonUtility.FromJson<GameTimeSaveData>(json);
                if (saveData == null)
                {
                    return CreateFallback(fallbackTotalElapsedTime);
                }

                saveData.TotalElapsedTime = Mathf.Max(0, saveData.TotalElapsedTime);
                return saveData;
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning($"게임 시간 저장 데이터를 불러오지 못했습니다. 기본값을 사용합니다. {exception.Message}");
                return CreateFallback(fallbackTotalElapsedTime);
            }
        }

        public void Save(int totalElapsedTime)
        {
            GameTimeSaveData saveData = new GameTimeSaveData
            {
                TotalElapsedTime = Mathf.Max(0, totalElapsedTime)
            };

            PlayerPrefs.SetString(_saveKey, JsonUtility.ToJson(saveData));
        }

        public void Delete()
        {
            PlayerPrefs.DeleteKey(_saveKey);
        }

        private static GameTimeSaveData CreateFallback(int totalElapsedTime)
        {
            return new GameTimeSaveData
            {
                TotalElapsedTime = Mathf.Max(0, totalElapsedTime)
            };
        }
    }
}
