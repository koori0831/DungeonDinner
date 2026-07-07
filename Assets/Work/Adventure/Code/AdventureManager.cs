using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;
using Work.Adventure.Code.UI;
using Work.Core.EventBus;
using Work.UtillUI.Code.Fade;

namespace Work.Adventure.Code
{
    public class AdventureManager : MonoBehaviour
    {
        [SerializeField] private AdventureMapUI adventureMap;
        [SerializeField] private AdventureBackground background;

        public void Init()
        {
            adventureMap.Init(StartAdventure);
        }

        public void OpenMap()
        {
            adventureMap.OpenMap();
        }

        public void StartAdventure()
        {
            Bus<OnFadeInEvent>.Raise(new OnFadeInEvent(() =>
            {
                adventureMap.CloseMap();
                background.Enable();
                DOVirtual.DelayedCall(0.5f, () => Bus<OnFadeOutEvent>.Raise(new OnFadeOutEvent(() => background.Walking())));
                //여기 워킹 안에 이벤트 뽑는거 연결 
            }));
        }
    }
}