using Assets.Work.Adventure.Code;
using System;
using UnityEngine;

namespace Work.Adventure.Code.UI
{
    public class MapSelectButton : MonoBehaviour
    {
        [SerializeField] private MapInfoSO mapInfo;
        [SerializeField] private bool _isCanAdventure;
        private Action<MapInfoSO,bool> _callback;


        public void Init(Action<MapInfoSO,bool> callback)
        {
            _callback = callback;
        }

        public void ClickButton() => _callback?.Invoke(mapInfo, _isCanAdventure);
    }
}