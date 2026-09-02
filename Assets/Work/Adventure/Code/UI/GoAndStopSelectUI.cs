using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Work.Adventure.Code.UI
{
    public class GoAndStopSelectUI : MonoBehaviour
    {
        [SerializeField] private Image root;
        [SerializeField] private float openHeight = 1080, time = 0.3f;

        [SerializeField] private float fadeValue = 0.5882353f;
        [SerializeField] private Button goButton, stopButton;


        public void Enable()
        {
            gameObject.SetActive(true);
            root.DOFade(fadeValue, time);
            goButton.image.DOFade(1, time);
            stopButton.image.DOFade(1, time);
            goButton.image.raycastTarget = true;
            stopButton.image.raycastTarget = true;
        }

        public void Disable()
        {
            root.DOFade(0f, time);
            goButton.image.DOFade(0, time);
            stopButton.image.DOFade(0, time);
            goButton.image.raycastTarget = false;
            stopButton.image.raycastTarget = false;
            gameObject.SetActive(false);
        }
    }
}