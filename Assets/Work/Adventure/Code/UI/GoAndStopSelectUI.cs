using DG.Tweening;
using System;
using UnityEngine;

namespace Work.Adventure.Code.UI
{
    public class GoAndStopSelectUI : MonoBehaviour
    {
        [SerializeField] private RectTransform root;
        [SerializeField] private float openHeight = 1080, time = 0.3f;

        public void Enable()
        {
            gameObject.SetActive(true);
            root.DOSizeDelta(new Vector2(root.sizeDelta.x, openHeight),time);
        }

        public void Disable()
        {
            root.DOSizeDelta(new Vector2(root.sizeDelta.x, 0), time);
            gameObject.SetActive(false);
        }
    }
}