using System;
using UnityEngine;
using Work.Core.EventBus;

namespace Work.Adventure.Code.UI
{
    public readonly record struct OnPlusLogCreateEvent(ItemLogData data) : IEvent;
    public readonly record struct OnMinusLogCreateEvent(ItemLogData data) : IEvent;

    public class LogPanel : MonoBehaviour
    {
        [SerializeField] private LogLabel plusPrefab, minusPrefab;

        public void Awake()
        {
            Bus<OnPlusLogCreateEvent>.Events += HandlePlusLogEvent;
            Bus<OnMinusLogCreateEvent>.Events += HandleMinusLogEvent;
        }

        private void OnDestroy()
        {
            Bus<OnPlusLogCreateEvent>.Events -= HandlePlusLogEvent;
            Bus<OnMinusLogCreateEvent>.Events -= HandleMinusLogEvent;
        }

        private void LateUpdate()
        {
            if (Work.UtillUI.Code.GameUiInput.Context == Work.UtillUI.Code.GameUiContext.Adventure) return;
            for (int i = transform.childCount - 1; i >= 0; i--)
                if (transform.GetChild(i).GetComponent<LogLabel>() != null) Destroy(transform.GetChild(i).gameObject);
        }

        private void HandlePlusLogEvent(OnPlusLogCreateEvent evt)
        {
            LogLabel label = Instantiate(plusPrefab,transform);
            label.Init(evt.data);
        }

        private void HandleMinusLogEvent(OnMinusLogCreateEvent evt)
        {
            LogLabel label = Instantiate(minusPrefab, transform);
            label.Init(evt.data);
        }
    }
}
