using UnityEngine;
using Work.MaterialAcquisition.Code.Integration;

public class AdventureManager : MonoBehaviour
{
    [SerializeField] private PreparationPhaseController preparation;

    private void OnEnable()
    {
        preparation.AdventureRequested += StartAdventure;
    }

    private void OnDisable()
    {
        preparation.AdventureRequested -= StartAdventure;
    }

    private void StartAdventure()
    {
        preparation.MarkAdventureStarted();

        // 여기서 네 모험 UI 열기

    }

    public void FinishAdventure()
    {
        // 보상 정산 후
        preparation.ReturnToPreparationPhase();
    }
}
