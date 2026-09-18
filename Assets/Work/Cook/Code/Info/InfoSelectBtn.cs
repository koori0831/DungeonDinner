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
        [SerializeField] private Image entryIcon;

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
            if (entryIcon != null)
            {
                entryIcon.sprite = entryData?.Icon;
                entryIcon.preserveAspect = true;
                entryIcon.enabled = entryData?.Icon != null;
                entryIcon.raycastTarget = false;
                if (entryIcon.transform.parent is RectTransform iconFrame && iconFrame != transform)
                    iconFrame.anchoredPosition = new Vector2(iconFrame.anchoredPosition.x, 22);
            }

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
            // A centered, fixed-width label can overflow the card or its ancestor mask.
            // Reserve two lines inside the card, independently of prefab/scene size overrides.
            var labelRect = nameField.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = new Vector2(1, 0);
            labelRect.offsetMin = new Vector2(8, 8);
            labelRect.offsetMax = new Vector2(-8, 52);
            nameField.alignment = TextAlignmentOptions.Center;
            nameField.overflowMode = TextOverflowModes.Overflow;
            nameField.enableAutoSizing = true;
            nameField.fontSizeMin = 14;
            nameField.fontSizeMax = 18;
            nameField.color = new Color(0.26f, 0.19f, 0.13f);
            nameField.textWrappingMode = TextWrappingModes.Normal;
            nameField.raycastTarget = false;
        }
    }
}
