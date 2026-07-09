using DG.Tweening;
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
        [SerializeField] private RectTransform root;
        [SerializeField] private TextMeshProUGUI nameField;
        [SerializeField] private Button button;
        [SerializeField] private float time;
        [SerializeField] private float widthOffset = 40f;

        private float _defaultWidth;

        private Options _currentOption;

        public void Init(Options optionInfo, Action<Options> resultDialog)
        {
            _currentOption = optionInfo;
            nameField.text = optionInfo.OptionName;
            _defaultWidth = root.sizeDelta.x;
            root.sizeDelta = new Vector2(0, root.sizeDelta.y);
            root.DOSizeDelta(new Vector2(_defaultWidth, root.sizeDelta.y), time);

            if (optionInfo is LockedOption lockedOption)
            {
                bool isHaveItem = false;
                isHaveItem = Bus<OnHaveItemEvent, BoolenReturnValue>.Raise(new OnHaveItemEvent(lockedOption.KeyItem)).isTrue;
                button.interactable = isHaveItem != lockedOption.IsUnLockOption;
                button.onClick.AddListener(() =>
                {
                    resultDialog?.Invoke(optionInfo);
                    if (lockedOption.IsUseItemOption)
                    {
                        Bus<OnUseAdventureItem>.Raise(new OnUseAdventureItem(lockedOption.KeyItem));
                        Bus<OnMinusLogCreateEvent>.Raise(new OnMinusLogCreateEvent(new ItemLogData(lockedOption.KeyItem.ItemName, lockedOption.LogStatus, lockedOption.KeyItem.ItemIcon)));

                    }
                });
            }
            else
                button.onClick.AddListener(() => resultDialog?.Invoke(optionInfo));
        }

        public void MouseEnter()
        {
            if (button.interactable == false)
            {
                if (_currentOption is LockedOption lockedOption)
                    Bus<OnEnableTooltipEvent>.Raise(new OnEnableTooltipEvent(lockedOption.LockTooltip));
                return;
            }
            root.DOSizeDelta(new Vector2(_defaultWidth + widthOffset, root.sizeDelta.y), time);

        }

        public void MouseExit()
        {
            Bus<OnDisableTooltipEvent>.Raise(new OnDisableTooltipEvent());
            if (button.interactable == false)
            {
                return;
            }
            root.DOSizeDelta(new Vector2(_defaultWidth, root.sizeDelta.y), time);
            //Bus<OnDisableTooltipEvent>.Raise(new OnDisableTooltipEvent());
        }

        private void OnDestroy()
        {
            button.onClick.RemoveAllListeners();
            Bus<OnDisableTooltipEvent>.Raise(new OnDisableTooltipEvent());
        }
    }
}