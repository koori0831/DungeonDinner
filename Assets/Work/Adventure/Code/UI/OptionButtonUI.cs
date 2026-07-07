using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Work.Core.EventBus;

namespace Work.Adventure.Code.UI
{
    public readonly record struct BoolenReturnValue(bool isTrue) : IReturnValue;
    public readonly record struct OnHaveItemEvent(AdventureItemSO itemSO) : IEvent;

    public class OptionButtonUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI nameField;
        [SerializeField] private Button button;

        private Options _currentOption;

        public void Init(Options optionInfo, Action<Options> resultDialog)
        {
            _currentOption = optionInfo;
            nameField.text = optionInfo.OptionName;

            if (optionInfo is LockedOption lockedOption)
            {
                bool isHaveItem = false;
                isHaveItem = Bus<OnHaveItemEvent, BoolenReturnValue>.Raise(new OnHaveItemEvent(lockedOption.KeyItem)).isTrue;
                button.interactable = isHaveItem;
                button.onClick.AddListener(() =>
                {
                    resultDialog?.Invoke(optionInfo);
                    Bus<OnUseAdventureItem>.Raise(new OnUseAdventureItem(lockedOption.KeyItem));
                });
            }
            else
                button.onClick.AddListener(() => resultDialog?.Invoke(optionInfo));
        }

        private void OnMouseEnter()
        {
            Bus<OnEnableTooltipEvent>.Raise(new OnEnableTooltipEvent(_currentOption.OptionTooltip));
        }

        private void OnMouseExit()
        {
            Bus<OnDisableTooltipEvent>.Raise(new OnDisableTooltipEvent());
        }

        private void OnDestroy()
        {
            button.onClick.RemoveAllListeners();
        }
    }
}