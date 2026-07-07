using System;
using UnityEngine;
using UnityEngine.UI;

namespace Work.Adventure.Code.AdventureEvents
{
    [Serializable]
    public class CreateImageEvent : AdventrueDialogEvent
    {
        private RectTransform _root;
        [SerializeField] private Image imagePrefab;
        [SerializeField] private Vector2 position;

        public override void Init(RectTransform root)
        {
            _root = root;
        }

        public override void RaiseEvent()
        {
            Image image = MonoBehaviour.Instantiate(imagePrefab, _root);
            image.rectTransform.anchoredPosition = position;
        }
    }
    [Serializable]
    public class DeleteAllImageEvent : AdventrueDialogEvent
    {
        private RectTransform _root;

        public override void Init(RectTransform root)
        {
            _root = root;
        }

        public override void RaiseEvent()
        {
            int childCount = _root.childCount - 1;

            for (int i = childCount; i >= 0; i--)
            {
                MonoBehaviour.Destroy(_root.GetChild(i).gameObject);
            }
        }
    }
}