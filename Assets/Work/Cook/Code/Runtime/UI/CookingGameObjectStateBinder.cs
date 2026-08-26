using UnityEngine;
using UnityEngine.UI;
using Work.Cook.Code.Runtime.Core;
using Work.Cook.Code.Runtime.Events;
using Work.Cook.Code.Runtime.Integration;
using Work.Cook.Code.Runtime.Systems;
using Work.Cook.Code.Runtime.UI;
using Work.Core.EventBus;

namespace Work.Cook.Code.Runtime.UI
{
    public sealed class CookingGameObjectStateBinder : MonoBehaviour
    {
        [SerializeField] private CookingGamePanel gamePanel;
        [SerializeField] private GameObject target;
        [SerializeField] private Selectable selectable;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private bool findGamePanelOnEnable = true;
        [SerializeField] private CookingGameSnapshotCondition condition;
        [SerializeField] private CookingGameScreenState screen;
        [SerializeField] private bool invert;
        [SerializeField] private bool controlActive = true;
        [SerializeField] private bool controlSelectable;
        [SerializeField] private bool controlCanvasGroup;

        private CookingGamePanel _subscribedPanel;

        private void Reset()
        {
            target = gameObject;
            selectable = GetComponent<Selectable>();
            canvasGroup = GetComponent<CanvasGroup>();
            gamePanel = GetComponentInParent<CookingGamePanel>();
        }

        private void Awake()
        {
            EnsureReferences();
        }

        private void OnEnable()
        {
            EnsureReferences();
            SubscribePanel();
            Refresh();
        }

        private void OnDisable()
        {
            if (controlActive && target == gameObject)
                return;

            UnsubscribePanel();
        }

        private void OnDestroy()
        {
            UnsubscribePanel();
        }

        public void SetGamePanel(CookingGamePanel value)
        {
            if (gamePanel == value)
                return;

            UnsubscribePanel();
            gamePanel = value;

            if (isActiveAndEnabled == true)
                SubscribePanel();

            Refresh();
        }

        public void Refresh()
        {
            ApplySnapshot(gamePanel != null ? gamePanel.CurrentSnapshot : null);
        }

        public void ApplySnapshot(CookingGameSnapshot snapshot)
        {
            bool passed = Evaluate(snapshot);
            if (invert == true)
                passed = !passed;

            if (controlActive && target != null && target.activeSelf != passed)
                target.SetActive(passed);

            if (controlSelectable && selectable != null)
                selectable.interactable = passed;

            if (controlCanvasGroup && canvasGroup != null)
            {
                canvasGroup.alpha = passed ? 1f : 0f;
                canvasGroup.interactable = passed;
                canvasGroup.blocksRaycasts = passed;
            }
        }

        private bool Evaluate(CookingGameSnapshot snapshot)
        {
            switch (condition)
            {
                case CookingGameSnapshotCondition.Always:
                    return true;
                case CookingGameSnapshotCondition.Screen:
                    return snapshot != null && snapshot.Screen == screen;
                case CookingGameSnapshotCondition.RecipeMode:
                    return snapshot?.Mode == CookingMode.Recipe;
                case CookingGameSnapshotCondition.DirectIngredientMode:
                    return snapshot?.Mode == CookingMode.DirectIngredients;
                case CookingGameSnapshotCondition.HasSelectedIngredients:
                    return snapshot != null && snapshot.HasSelectedIngredients;
                case CookingGameSnapshotCondition.HasCurrentIngredient:
                    return snapshot != null && snapshot.HasCurrentIngredient;
                case CookingGameSnapshotCondition.IsEveryIngredientPrepared:
                    return snapshot != null && snapshot.IsEveryIngredientPrepared;
                case CookingGameSnapshotCondition.HasCurrentResult:
                    return snapshot != null && snapshot.HasCurrentResult;
                case CookingGameSnapshotCondition.CanHandResultToNpc:
                    return snapshot != null && snapshot.CanHandResultToNpc;
                case CookingGameSnapshotCondition.HasNpcMatchReport:
                    return snapshot != null && snapshot.HasNpcMatchReport;
                default:
                    return false;
            }
        }

        private void EnsureReferences()
        {
            if (target == null)
                target = gameObject;

            if (selectable == null)
                selectable = GetComponent<Selectable>();

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            if (gamePanel != null || findGamePanelOnEnable == false)
                return;

            gamePanel = GetComponentInParent<CookingGamePanel>();
            if (gamePanel == null)
                gamePanel = FindFirstObjectByType<CookingGamePanel>();
        }

        private void SubscribePanel()
        {
            if (_subscribedPanel == gamePanel)
                return;

            UnsubscribePanel();

            if (gamePanel == null)
                return;

            Bus<CookingGameSnapshotChangedEvent>.Events += HandleSnapshotChanged;
            _subscribedPanel = gamePanel;
        }

        private void UnsubscribePanel()
        {
            if (_subscribedPanel == null)
                return;

            Bus<CookingGameSnapshotChangedEvent>.Events -= HandleSnapshotChanged;
            _subscribedPanel = null;
        }

        private void HandleSnapshotChanged(CookingGameSnapshotChangedEvent gameEvent)
        {
            if (gameEvent.Source != gamePanel)
                return;

            ApplySnapshot(gameEvent.Snapshot);
        }
    }

    public enum CookingGameSnapshotCondition
    {
        Always,
        Screen,
        RecipeMode,
        DirectIngredientMode,
        HasSelectedIngredients,
        HasCurrentIngredient,
        IsEveryIngredientPrepared,
        HasCurrentResult,
        CanHandResultToNpc,
        HasNpcMatchReport
    }
}
