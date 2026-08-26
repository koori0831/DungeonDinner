using System;
using UnityEngine;

namespace Work.Cook.Code.Runtime.Systems
{
    internal sealed class CookingKnowledgePlayerPrefsRepository
    {
        public bool HasSave(string key)
        {
            return string.IsNullOrWhiteSpace(key) == false && PlayerPrefs.HasKey(key) == true;
        }

        public CookingKnowledgeSaveData Load(string key, UnityEngine.Object logContext)
        {
            if (string.IsNullOrWhiteSpace(key) == true || PlayerPrefs.HasKey(key) == false)
                return null;

            string json = PlayerPrefs.GetString(key);
            if (string.IsNullOrWhiteSpace(json) == true)
                return null;

            try
            {
                return JsonUtility.FromJson<CookingKnowledgeSaveData>(json);
            }
            catch (ArgumentException exception)
            {
                Debug.LogWarning($"Failed to load cooking knowledge data. {exception.Message}", logContext);
                return null;
            }
        }

        public void Save(string key, CookingKnowledgeSaveData saveData)
        {
            if (string.IsNullOrWhiteSpace(key) == true || saveData == null)
                return;

            PlayerPrefs.SetString(key, JsonUtility.ToJson(saveData));
            PlayerPrefs.Save();
        }

        public void Delete(string key)
        {
            if (string.IsNullOrWhiteSpace(key) == true)
                return;

            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }
    }
}
