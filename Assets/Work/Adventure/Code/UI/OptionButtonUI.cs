using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Work.Core.EventBus;
using Work.Players.Code.Inventory;

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
        private PlayerInventoryModule _inventory;
        private Tween _sizeTween;

        public void Init(Options optionInfo, Action<Options> resultDialog)
        {
            _currentOption = optionInfo;
            nameField.text = optionInfo.OptionName;
            _defaultWidth = root.sizeDelta.x;
            root.sizeDelta = new Vector2(0, root.sizeDelta.y);
            root.DOSizeDelta(new Vector2(_defaultWidth, root.sizeDelta.y), time).SetLink(gameObject);
            AnimateWidth(_defaultWidth);
            button.onClick.RemoveAllListeners();

            if (optionInfo is IngredientLockedOption ingredientOption)
            {
                _inventory = FindFirstObjectByType<PlayerInventoryModule>();
                button.interactable = ingredientOption.CanSelect(_inventory);
                if (_inventory != null)
                    _inventory.InventoryChanged += RefreshIngredientAvailability;
                button.onClick.AddListener(() =>
                {
                    if (!button.interactable || !ingredientOption.TryConsume(_inventory))
                    {
                        button.interactable = false;
                        return;
                    }

                    button.interactable = false;
                    for (int i = 0; i < ingredientOption.RequiredAmount; i++)
                        Bus<OnMinusLogCreateEvent>.Raise(new OnMinusLogCreateEvent(new ItemLogData(
                            ingredientOption.RequiredIngredient.DisplayName, ItemLogStatusEnum.Use,
                            ingredientOption.RequiredIngredient.Icon)));
                    resultDialog?.Invoke(optionInfo);
                });
            }
            else if (optionInfo is LockedOption lockedOption)
            {
                bool isHaveItem = false;
                isHaveItem = Bus<OnHaveItemEvent, BoolenReturnValue>.Raise(new OnHaveItemEvent(lockedOption.KeyItem)).isTrue;
                button.interactable = isHaveItem != lockedOption.IsUnLockOption;
                button.onClick.AddListener(() =>
                {
                    if (!button.interactable || Bus<OnHaveItemEvent, BoolenReturnValue>.Raise(
                        new OnHaveItemEvent(lockedOption.KeyItem)).isTrue == lockedOption.IsUnLockOption)
                        return;
                    button.interactable = false;
                    if (lockedOption.IsUseItemOption)
                    {
                        Bus<OnRemoveAdventureItemEvent>.Raise(new OnRemoveAdventureItemEvent(lockedOption.KeyItem));
                        Bus<OnMinusLogCreateEvent>.Raise(new OnMinusLogCreateEvent(new ItemLogData(lockedOption.KeyItem.ItemName, lockedOption.LogStatus, lockedOption.KeyItem.ItemIcon)));

                    }
                    resultDialog?.Invoke(optionInfo);
                });
            }
            else
                button.onClick.AddListener(() =>
                {
                    if (!button.interactable) return;
                    button.interactable = false;
                    resultDialog?.Invoke(optionInfo);
                });
        }

        private void RefreshIngredientAvailability(PlayerInventoryModule inventory)
        {
            if (_currentOption is IngredientLockedOption option)
                button.interactable = option.CanSelect(inventory);
        }

        public void MouseEnter()
        {
            if (button.interactable == false)
            {
                if (_currentOption is LockedOption lockedOption)
                    Bus<OnEnableTooltipEvent>.Raise(new OnEnableTooltipEvent(lockedOption.LockTooltip));
                else if (_currentOption is IngredientLockedOption ingredientOption)
                    Bus<OnEnableTooltipEvent>.Raise(new OnEnableTooltipEvent(ingredientOption.LockTooltip));
                return;
            }
            if (!string.IsNullOrWhiteSpace(_currentOption.OptionTooltip))
                Bus<OnEnableTooltipEvent>.Raise(new OnEnableTooltipEvent(_currentOption.OptionTooltip));
            root.DOKill();
            root.DOSizeDelta(new Vector2(_defaultWidth + widthOffset, root.sizeDelta.y), time).SetLink(gameObject);
            AnimateWidth(_defaultWidth + widthOffset);

        }

        public void MouseExit()
        {
            Bus<OnDisableTooltipEvent>.Raise(new OnDisableTooltipEvent());
            if (button.interactable == false)
            {
                return;
            }
            root.DOKill();
            root.DOSizeDelta(new Vector2(_defaultWidth, root.sizeDelta.y), time).SetLink(gameObject);
            AnimateWidth(_defaultWidth);
            //Bus<OnDisableTooltipEvent>.Raise(new OnDisableTooltipEvent());
        }

        private void OnDestroy()
        {
            if (_inventory != null)
                _inventory.InventoryChanged -= RefreshIngredientAvailability;
            KillSizeTween();
            button.onClick.RemoveAllListeners();
            Bus<OnDisableTooltipEvent>.Raise(new OnDisableTooltipEvent());
        }

        private void OnDisable()
        {
            KillSizeTween();
        }

        private void AnimateWidth(float width)
        {
            if (root == null)
                return;

            KillSizeTween();
            _sizeTween = root.DOSizeDelta(new Vector2(width, root.sizeDelta.y), time)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        private void KillSizeTween()
        {
            _sizeTween?.Kill(false);
            _sizeTween = null;
            root?.DOKill(false);
        }
    }
}
