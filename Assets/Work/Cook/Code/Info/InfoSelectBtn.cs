using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Work.Cook.Code.Info
{
    [RequireComponent(typeof(Button))]
    public class InfoSelectBtn : MonoBehaviour
    {
        private Button _button;
        private InfoDictionaryEntryData _entryData;

        [SerializeField] private TextMeshProUGUI nameField;

        public InfoDictionaryEntryData EntryData => _entryData;

        public void InitializeBtn(InfoDictionaryEntryData entryData, Action<InfoDictionaryEntryData> action)
        {
            _entryData = entryData;
            _button = GetComponent<Button>();

            if (_button == null)
            {
                Debug.LogWarning("InfoSelectBtn needs a Button component before it can be initialized.", this);
                return;
            }

            BindName();

            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() => action?.Invoke(_entryData));
        }

        private void BindName()
        {
            if (_entryData == null)
                return;

            if (nameField == null)
            {
                Debug.LogWarning("InfoSelectBtn needs a serialized name field before it can display an entry name.", this);
                return;
            }

            nameField.text = _entryData.DisplayName;
        }
    }
}
