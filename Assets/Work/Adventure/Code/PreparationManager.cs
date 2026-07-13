using UnityEngine;
using Work.Adventure.Code.UI;
using Work.Cook.Code.Runtime.Systems;
using Work.Cook.Code.Runtime.UI;
using Work.Core.EventBus;

namespace Work.Adventure.Code
{
    public class PreparationManager : MonoBehaviour
    {
        [SerializeField] private AdventureManager adventureManager;

        [SerializeField] private PreparationMenu preparationMenuUI;
        [SerializeField] private MainUI mainUIroot;

        public void Awake()
        {
            Bus<CookingBusinessClosedEvent>.Events += EndBusiness;
            Bus<OnSelectPreparationEvent>.Events += HandleSelectPreparationEvent;
            preparationMenuUI.Init(() => mainUIroot.HideUI(), () => mainUIroot.ShowUI());
            adventureManager.Init();
        }

        private void HandleSelectPreparationEvent(OnSelectPreparationEvent evt)
        {
            if (evt.preparationType == PreparationEnum.Adventure)
                SelectAdventure();
            else if (evt.preparationType == PreparationEnum.Dispatch)
                SelectDispatch();
        }

        private void OnDestroy()
        {
            Bus<CookingBusinessClosedEvent>.Events -= EndBusiness;
            Bus<OnSelectPreparationEvent>.Events -= HandleSelectPreparationEvent;
        }

        [ContextMenu("TestEndBusiness")]
        public void Test()
        {
            preparationMenuUI.ShowUI();
        }

        public void StopAdventure()
        {
            preparationMenuUI.ShowUI();
            mainUIroot.ShowUI();
        }

        public void EndBusiness(CookingBusinessClosedEvent evt)
        {
            preparationMenuUI.ShowUI();
        }

        public void SelectAdventure()
        {
            //지도 나오고 어디 갈지 선택하고 거기에 맞춰서 
            //다시 페이드 인아웃 나오고 배경 바뀌고 
            adventureManager.OpenMap();
        }

        public void SelectDispatch()
        {

        }
    }
}
