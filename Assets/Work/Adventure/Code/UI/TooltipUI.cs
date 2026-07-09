using DG.Tweening.Core.Easing;
using System;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using Work.Core.EventBus;

namespace Work.Adventure.Code.UI
{

    public readonly record struct OnEnableTooltipEvent(string value) : IEvent;
    public readonly record struct OnDisableTooltipEvent() : IEvent;

    public class TooltipUI : MonoBehaviour
    {
        [SerializeField] private RectTransform root;
        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private float offset = 30f;

        public void Awake()
        {
            Bus<OnEnableTooltipEvent>.Events += HandleEnableEvent;
            Bus<OnDisableTooltipEvent>.Events += HandleDisableEvent;

            SetText("");
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            Bus<OnEnableTooltipEvent>.Events -= HandleEnableEvent;
            Bus<OnDisableTooltipEvent>.Events -= HandleDisableEvent;
        }

        private void Update()
        {
            if (Mouse.current == null)
                return;

            Vector2 mouse = Mouse.current.position.ReadValue();

            Vector2 pos = mouse;
            pos += new Vector2(20f, -20f);
            root.anchoredPosition = pos;
        }

        private void HandleEnableEvent(OnEnableTooltipEvent evt)
        {
            gameObject.SetActive(true);
            SetText(evt.value);
        }

        private void HandleDisableEvent(OnDisableTooltipEvent evt)
        {
            SetText("");
            gameObject.SetActive(false);
        }

        public void SetText(string message)
        {
            text.text = message;
            text.ForceMeshUpdate();

            Vector2 textSize = text.GetRenderedValues(false);
            Vector2 size = root.sizeDelta;
            size.x = textSize.x + offset; // 좌우 여백
            root.sizeDelta = size;
        }
    }
}