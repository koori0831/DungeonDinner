using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Work.Adventure.Code.UI
{
    public enum ItemLogStatusEnum
    {
        Destroy,
        Loss,
        Add,
        Use
    }

    public class ItemLogData
    {
        public string ItemName { get; private set; }
        public ItemLogStatusEnum ItemLogDescription { get; private set; }
        public Sprite IconImage { get; private set; }

        public string ConvertString()
        {
            switch (ItemLogDescription)
            {
                case ItemLogStatusEnum.Destroy:
                    return "파괴";
                case ItemLogStatusEnum.Loss:
                    return "분실";
                case ItemLogStatusEnum.Add:
                    return "획득";
                case ItemLogStatusEnum.Use:
                    return "사용";
                default:
                    return "";
            }

        }

        public ItemLogData(string name, ItemLogStatusEnum description, Sprite image)
        {
            ItemName = name;
            ItemLogDescription = description;
            IconImage = image;
        }
    }

    public class LogLabel : MonoBehaviour
    {
        [SerializeField] private RectTransform root;
        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private Image icon;
        [SerializeField] private float offset = 62.03997f;
        [SerializeField] private float time = 0.3f;


        public void Init(ItemLogData data)
        {
            SetIcon(data.IconImage);
            Vector2 vec = SetText(data.ItemName + " " + data.ConvertString());

            root.DOSizeDelta(vec, time).OnComplete(() =>
            {
                icon.gameObject.SetActive(true);
                DOVirtual.DelayedCall(5f, () =>
                {
                    icon.gameObject.SetActive(false);
                    root.DOSizeDelta(new Vector2(0, vec.y), time).OnComplete(() =>
                    {
                        Destroy(gameObject);
                    });
                });
            });
        }

        public void SetIcon(Sprite iconSprite)
        {
            icon.sprite = iconSprite;
            icon.gameObject.SetActive(false);
        }

        public Vector2 SetText(string message)
        {
            text.text = message;
            text.ForceMeshUpdate();

            Vector2 textSize = text.GetRenderedValues(false);
            Vector2 size = root.sizeDelta;
            size.x = textSize.x + offset; // 좌우 여백
            return size;
        }
    }
}