using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Work.Cook.Code.Data;
using Work.Cook.Code.Runtime.Core;
using Work.Cook.Code.Runtime.Systems;

namespace Work.Cook.Code.Runtime.UI
{
    /// <summary>
    /// 외부 미니게임 계약의 유일한 구현체. 타입별 오버레이 컨트롤러를 선택하고 공통 호스트를 관리한다.
    /// </summary>
    public sealed class CookingMiniGameRouterView : MonoBehaviour, ICookingMiniGameView
    {
        [SerializeField] private CookingMiniGameOverlayHost overlayHost;
        [SerializeField] private CookingMiniGameOverlaySettingsSO overlaySettings;
        [SerializeField] private GameObject[] controllerObjects;
        [SerializeField] private bool autoCollectChildControllers = true;

        private readonly List<ICookingOverlayMiniGameController> _controllers = new List<ICookingOverlayMiniGameController>();
        private CookingGamePanel _owner;
        private CookingFlowRunner _runner;
        private TMP_FontAsset _fontAsset;
        private ICookingOverlayMiniGameController _activeController;
        private Action<CookingMiniGameResult> _completed;
        private bool _completionPending;

        public void Initialize(CookingGamePanel owner, CookingFlowRunner runner, TMP_FontAsset defaultFontAsset = null)
        {
            _owner = owner;
            _runner = runner;
            _fontAsset = defaultFontAsset;

            if (overlayHost == null)
                overlayHost = GetComponentInChildren<CookingMiniGameOverlayHost>(true);

            overlayHost?.Initialize(_owner, overlaySettings, _fontAsset);
            CollectControllers();
            for (int i = 0; i < _controllers.Count; i++)
                _controllers[i].Initialize(overlayHost, overlaySettings);

            SetAllControllersActive(false);
        }

        public void SetFontAsset(TMP_FontAsset value)
        {
            if (value == null)
                return;

            _fontAsset = value;
            overlayHost?.SetFontAsset(value);
        }

        public bool CanPlay(CookingMiniGameType miniGameType)
        {
            return FindController(miniGameType) != null;
        }

        public bool StartMiniGame(
            IngredientSO ingredient,
            IngredientPreparationOption option,
            Action<CookingMiniGameResult> completed)
        {
            if (ingredient == null || option == null || completed == null || overlayHost == null)
                return false;

            ICookingOverlayMiniGameController controller = FindController(option.MiniGameType);
            if (controller == null)
            {
                Debug.LogError($"CookingMiniGameRouterView has no controller for type {option.MiniGameType}.", this);
                return false;
            }

            CancelMiniGame();
            _completed = completed;
            _activeController = controller;
            SetControllerActive(controller, true);

            if (overlayHost.Begin(ingredient, option) == true
                && controller.StartMiniGame(ingredient, option, HandleControllerCompleted) == true)
            {
                return true;
            }

            controller.CancelMiniGame();
            SetControllerActive(controller, false);
            _activeController = null;
            _completed = null;
            overlayHost.EndImmediate();
            Debug.LogError($"CookingMiniGameRouterView failed to start type {option.MiniGameType}.", this);
            return false;
        }

        public void CancelMiniGame()
        {
            _completionPending = false;
            if (_activeController != null)
                _activeController.CancelMiniGame();

            _activeController = null;
            _completed = null;
            SetAllControllersActive(false);
            overlayHost?.EndImmediate();
        }

        private void HandleControllerCompleted(CookingMiniGameResult result)
        {
            if (_completionPending == true || _activeController == null)
                return;

            _completionPending = true;
            _activeController.CancelMiniGame();
            SetControllerActive(_activeController, false);
            _activeController = null;
            overlayHost.PlayResult(result, () => ForwardResult(result));
        }

        private void ForwardResult(CookingMiniGameResult result)
        {
            if (_completionPending == false)
                return;

            _completionPending = false;
            Action<CookingMiniGameResult> completed = _completed;
            _completed = null;
            completed?.Invoke(result);
        }

        private ICookingOverlayMiniGameController FindController(CookingMiniGameType miniGameType)
        {
            if (miniGameType == CookingMiniGameType.None)
                return null;

            CollectControllers();
            ICookingOverlayMiniGameController selected = null;
            for (int i = 0; i < _controllers.Count; i++)
            {
                ICookingOverlayMiniGameController candidate = _controllers[i];
                if (candidate == null || candidate.CanPlay(miniGameType) == false)
                    continue;

                if (selected != null)
                {
                    Debug.LogError($"CookingMiniGameRouterView has multiple controllers for type {miniGameType}.", this);
                    return null;
                }

                selected = candidate;
            }

            return selected;
        }

        private void CollectControllers()
        {
            _controllers.Clear();
            if (controllerObjects != null)
            {
                for (int i = 0; i < controllerObjects.Length; i++)
                    AddControllersFromObject(controllerObjects[i]);
            }

            if (autoCollectChildControllers == false)
                return;

            MonoBehaviour[] behaviours = GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
                AddController(behaviours[i] as ICookingOverlayMiniGameController);
        }

        private void AddControllersFromObject(GameObject source)
        {
            if (source == null)
                return;

            MonoBehaviour[] behaviours = source.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
                AddController(behaviours[i] as ICookingOverlayMiniGameController);
        }

        private void AddController(ICookingOverlayMiniGameController controller)
        {
            if (controller != null && _controllers.Contains(controller) == false)
                _controllers.Add(controller);
        }

        private void SetAllControllersActive(bool active)
        {
            for (int i = 0; i < _controllers.Count; i++)
                SetControllerActive(_controllers[i], active);
        }

        private static void SetControllerActive(ICookingOverlayMiniGameController controller, bool active)
        {
            Component component = controller != null ? controller.Component : null;
            if (component != null && component.gameObject.activeSelf != active)
                component.gameObject.SetActive(active);
        }
    }
}
