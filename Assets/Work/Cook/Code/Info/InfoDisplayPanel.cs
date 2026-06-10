using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Work.Cook.Code.Info
{
    [Serializable]
    public class InfoDisplayPanel : MonoBehaviour, IDisplayInfo
    {
        [SerializeField] protected ViewHaveInfoEnum viewInfo;
        [SerializeField] protected Image iconImage;
        [SerializeField] protected TextMeshProUGUI nameField, descriptionField;
        [SerializeField] protected Button backBtn;

        public ViewHaveInfoEnum ViewInfo => viewInfo;

        public virtual void InitializeDisplay(Action backAction)
        {
            if (backBtn == null)
            {
                Debug.LogWarning("InfoDisplayPanel needs a back button before it can bind back navigation.", this);
                return;
            }

            backBtn.onClick.AddListener(() => backAction?.Invoke());
        }

        public virtual void Enable(InfoDictionaryEntryData displayInfo)
        {
            gameObject.SetActive(true);
            if (displayInfo == null)
                return;

            if (iconImage != null)
                iconImage.sprite = displayInfo.Icon;

            if (nameField != null)
                nameField.text = displayInfo.DisplayName;

            if (descriptionField != null)
                descriptionField.text = displayInfo.Description;
        }

        public virtual void Disable()
        {
            gameObject.SetActive(false);
        }
    }
}
