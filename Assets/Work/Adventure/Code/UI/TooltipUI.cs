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

            // Screen -> Canvas Local 좌표 (중심 기준)
            Vector2 pos = mouse - new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

            // 마우스에서 약간 띄우기
            pos += new Vector2(20f, -20f);

            Vector2 size = root.sizeDelta;

            float halfW = Screen.width * 0.5f;
            float halfH = Screen.height * 0.5f;

            // Pivot = (1,1)이므로
            // pos가 우상단 기준 위치가 된다.

            // 왼쪽
            if (pos.x < -halfW + size.x)
                pos.x = -halfW + size.x;

            // 오른쪽
            if (pos.x > halfW)
                pos.x = halfW;

            // 아래
            if (pos.y < -halfH + size.y)
                pos.y = -halfH + size.y;

            // 위
            if (pos.y > halfH)
                pos.y = halfH;

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
            Debug.Log(textSize);
            Vector2 size = root.sizeDelta;
            size.x = textSize.x + offset; // 좌우 여백
            root.sizeDelta = size;
        }
    }
}