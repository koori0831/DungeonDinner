using System;
using System.Collections.Generic;
using UnityEngine;

namespace Work.Cook.Code.Info
{
    public class InfoDictionaryScrollViewField : MonoBehaviour
    {
        [SerializeField] private List<InfoSelectBtn> selectButtons = new List<InfoSelectBtn>();

        private readonly List<InfoDictionaryEntryData> _entries = new List<InfoDictionaryEntryData>();
        private Action<InfoDictionaryEntryData> _selectAction;

        public IReadOnlyList<InfoDictionaryEntryData> Entries => _entries;

        public void InitializeField(IReadOnlyList<InfoDictionaryEntryData> entries, Action<InfoDictionaryEntryData> action)
        {
            _entries.Clear();
            _selectAction = action;

            if (entries != null)
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    if (entries[i] != null)
                        _entries.Add(entries[i]);
                }
            }

            BindPreauthoredButtons();
        }

        public void SelectEntry(int index)
        {
            if (index < 0 || index >= _entries.Count)
                return;

            _selectAction?.Invoke(_entries[index]);
        }

        private void BindPreauthoredButtons()
        {
            for (int i = 0; i < selectButtons.Count; i++)
            {
                InfoSelectBtn button = selectButtons[i];
                if (button == null)
                    continue;

                bool hasEntry = i < _entries.Count;
                button.gameObject.SetActive(hasEntry);

                if (hasEntry)
                    button.InitializeBtn(_entries[i], _selectAction);
            }
        }

        public void Enable()
        {
            gameObject.SetActive(true);
        }

        public void Disable()
        {
            gameObject.SetActive(false);
        }
    }
}
