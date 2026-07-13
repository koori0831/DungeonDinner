using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Work.Adventure.Code.UI
{
    public class AdventureItemIconUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI countText;
        [SerializeField] private Image iconImage;

        public void Init(Sprite itemIcon, int count)
        {
            iconImage.sprite = itemIcon;
            SetCount(count);
        }

        public void SetCount(int count)
        {
            countText.text = count.ToString();
        }
    }
}