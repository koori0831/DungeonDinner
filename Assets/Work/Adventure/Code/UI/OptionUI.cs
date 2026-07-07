using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Work.Adventure.Code.UI
{
    public class OptionUI : MonoBehaviour
    {
        [SerializeField] private OptionButtonUI optionButtonPrefab;
        private List<OptionButtonUI> buttons = new List<OptionButtonUI>();
        private Action<Options> _resultDialog;
        public void Enable(List<Options> options, Action<Options> resultDialog)
        {
            _resultDialog = resultDialog;

            options.ForEach(x =>
            {
                OptionButtonUI button = Instantiate(optionButtonPrefab,transform);
                button.Init(x, SelectOption);
                buttons.Add(button);
            });
        }

        public void SelectOption(Options option)
        {
            _resultDialog?.Invoke(option);
            DestroyAllButton();
        }

        public void DestroyAllButton()
        {
            for(int i = buttons.Count - 1; i >= 0; i--)
            {
                Destroy(buttons[i].gameObject);
            }

            buttons.Clear();
        }
    }
}