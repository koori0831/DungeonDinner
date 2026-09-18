using System;
using UnityEngine;

namespace Work.Dispatch.Code.Runtime
{
    public sealed class DispatchRepository
    {
        public const string DefaultSaveKey = "DungeonDinner.Dispatch";

        private readonly string _saveKey;

        public DispatchRepository(string saveKey = DefaultSaveKey)
        {
            _saveKey = string.IsNullOrWhiteSpace(saveKey) ? DefaultSaveKey : saveKey;
        }

        public DispatchSaveData Load()
        {
            if (PlayerPrefs.HasKey(_saveKey) == false)
            {
                return new DispatchSaveData();
            }

            string json = PlayerPrefs.GetString(_saveKey, string.Empty);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new DispatchSaveData();
            }

            try
            {
                DispatchSaveData saveData = JsonUtility.FromJson<DispatchSaveData>(json);
                if (saveData == null)
                {
                    return new DispatchSaveData();
                }

                saveData.ReturnedReports ??= new System.Collections.Generic.List<DispatchJob>();
                return saveData;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"파견 저장 데이터를 불러오지 못했습니다. 새 데이터로 시작합니다. {exception.Message}");
                return new DispatchSaveData();
            }
        }

        public void Save(DispatchSaveData saveData)
        {
            PlayerPrefs.SetString(_saveKey, JsonUtility.ToJson(saveData ?? new DispatchSaveData()));
        }

        public void Delete()
        {
            PlayerPrefs.DeleteKey(_saveKey);
        }
    }
}
