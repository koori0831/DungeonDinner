using UnityEngine;
using Work.Cook.Code.Data;

namespace Work.Cook.Code.Runtime
{
    public sealed class CookingPreparationBoardView : MonoBehaviour
    {
        [SerializeField] private CookingGamePanel gamePanel;
        [SerializeField] private Transform modelAnchor;
        [SerializeField] private GameObject emptyModelFallback;
        [SerializeField] private bool findGamePanelOnEnable = true;
        [SerializeField] private bool hideFallbackWhenIngredientHasModel = true;

        private CookingGamePanel _subscribedPanel;
        private IngredientSO _currentIngredient;
        private GameObject _spawnedModel;

        private void Reset()
        {
            modelAnchor = transform;
            gamePanel = GetComponentInParent<CookingGamePanel>();
        }

        private void OnEnable()
        {
            EnsureReferences();
            SubscribePanel();
            Refresh();
        }

        private void OnDisable()
        {
            UnsubscribePanel();
            ClearSpawnedModel();
        }

        public void SetGamePanel(CookingGamePanel value)
        {
            if (gamePanel == value)
                return;

            UnsubscribePanel();
            gamePanel = value;

            if (isActiveAndEnabled)
                SubscribePanel();

            Refresh();
        }

        public void Refresh()
        {
            EnsureReferences();

            IngredientSO ingredient = gamePanel != null ? gamePanel.GetCurrentPreparationIngredient() : null;
            if (_currentIngredient == ingredient && _spawnedModel != null)
                return;

            _currentIngredient = ingredient;
            ClearSpawnedModel();

            GameObject prefab = ingredient != null ? ingredient.ModelPrefab : null;
            if (prefab != null && modelAnchor != null)
            {
                _spawnedModel = Instantiate(prefab, modelAnchor);
                _spawnedModel.transform.localPosition = Vector3.zero;
                _spawnedModel.transform.localRotation = Quaternion.identity;
                _spawnedModel.transform.localScale = Vector3.one;
            }

            if (emptyModelFallback != null)
                emptyModelFallback.SetActive(prefab == null || hideFallbackWhenIngredientHasModel == false);
        }

        private void EnsureReferences()
        {
            if (modelAnchor == null)
                modelAnchor = transform;

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

            gamePanel.SnapshotChanged += HandleSnapshotChanged;
            _subscribedPanel = gamePanel;
        }

        private void UnsubscribePanel()
        {
            if (_subscribedPanel == null)
                return;

            _subscribedPanel.SnapshotChanged -= HandleSnapshotChanged;
            _subscribedPanel = null;
        }

        private void HandleSnapshotChanged(CookingGameSnapshot snapshot)
        {
            if (snapshot == null
                || snapshot.Screen == CookingGameScreenState.Preparation
                || snapshot.Screen == CookingGameScreenState.MiniGame)
            {
                Refresh();
            }
        }

        private void ClearSpawnedModel()
        {
            if (_spawnedModel == null)
                return;

            if (Application.isPlaying)
                Destroy(_spawnedModel);
            else
                DestroyImmediate(_spawnedModel);

            _spawnedModel = null;
        }
    }
}
