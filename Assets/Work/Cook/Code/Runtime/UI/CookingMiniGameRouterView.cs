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
    /// 손질법 미니게임 타입에 맞는 실제 미니게임 뷰 선택 실행
    /// </summary>
    public sealed class CookingMiniGameRouterView : MonoBehaviour, ICookingMiniGameView
    {
        [SerializeField] private GameObject[] miniGameViewObjects;
        [SerializeField] private bool autoCollectChildViews = true;

        private readonly List<ICookingMiniGameView> _miniGameViews = new List<ICookingMiniGameView>();
        private CookingGamePanel _owner;
        private CookingFlowRunner _runner;
        private TMP_FontAsset _fontAsset;
        private ICookingMiniGameView _activeMiniGame;
        private Action<CookingMiniGameResult> _completed;

        /// <summary>
        /// 라우터 및 하위 미니게임 뷰 초기화
        /// </summary>
        /// <param name="owner">요리 패널</param>
        /// <param name="runner">요리 플로우 러너</param>
        /// <param name="defaultFontAsset">기본 UI 폰트</param>
        public void Initialize(CookingGamePanel owner, CookingFlowRunner runner, TMP_FontAsset defaultFontAsset = null)
        {
            _owner = owner;
            _runner = runner;
            _fontAsset = defaultFontAsset;

            CollectMiniGameViews();
            for (int i = 0; i < _miniGameViews.Count; i++)
                _miniGameViews[i].Initialize(_owner, _runner, _fontAsset);

            SetAllMiniGamesActive(false);
        }

        /// <summary>
        /// 하위 미니게임 뷰에 폰트 적용
        /// </summary>
        /// <param name="value">적용할 폰트</param>
        public void SetFontAsset(TMP_FontAsset value)
        {
            if (value == null)
                return;

            _fontAsset = value;
            CollectMiniGameViews();
            for (int i = 0; i < _miniGameViews.Count; i++)
                _miniGameViews[i].SetFontAsset(value);
        }

        /// <summary>
        /// 지정한 미니게임 타입 실행 가능 여부 확인
        /// </summary>
        /// <param name="miniGameType">확인할 미니게임 타입</param>
        /// <returns>실행 가능 여부</returns>
        public bool CanPlay(CookingMiniGameType miniGameType)
        {
            return FindMiniGameView(miniGameType) != null;
        }

        /// <summary>
        /// 지정한 손질 옵션의 미니게임 시작
        /// </summary>
        /// <param name="ingredient">손질 대상 재료</param>
        /// <param name="option">선택한 손질 옵션</param>
        /// <param name="completed">미니게임 완료 콜백</param>
        public bool StartMiniGame(
            IngredientSO ingredient,
            IngredientPreparationOption option,
            Action<CookingMiniGameResult> completed)
        {
            if (ingredient == null || option == null || completed == null)
                return false;

            CookingMiniGameType miniGameType = option != null ? option.MiniGameType : CookingMiniGameType.None;
            ICookingMiniGameView miniGame = FindMiniGameView(miniGameType);
            if (miniGame == null)
            {
                Debug.LogError($"CookingMiniGameRouterView has no view for type {miniGameType}.", this);
                return false;
            }

            _completed = completed;
            _activeMiniGame = miniGame;
            SetAllMiniGamesActive(false);
            SetMiniGameActive(miniGame, true);
            if (miniGame.StartMiniGame(ingredient, option, HandleMiniGameCompleted) == true)
                return true;

            SetMiniGameActive(miniGame, false);
            _activeMiniGame = null;
            _completed = null;
            Debug.LogError($"CookingMiniGameRouterView failed to start type {miniGameType}.", this);
            return false;
        }

        /// <summary>
        /// 현재 실행 중인 미니게임 취소
        /// </summary>
        public void CancelMiniGame()
        {
            if (_activeMiniGame != null)
                _activeMiniGame.CancelMiniGame();

            _activeMiniGame = null;
            _completed = null;
            SetAllMiniGamesActive(false);
        }

        private void HandleMiniGameCompleted(CookingMiniGameResult result)
        {
            Action<CookingMiniGameResult> completed = _completed;
            _activeMiniGame = null;
            _completed = null;
            SetAllMiniGamesActive(false);
            completed?.Invoke(result);
        }

        private ICookingMiniGameView FindMiniGameView(CookingMiniGameType miniGameType)
        {
            if (miniGameType == CookingMiniGameType.None)
                return null;

            CollectMiniGameViews();
            ICookingMiniGameView selectedMiniGame = null;
            for (int i = 0; i < _miniGameViews.Count; i++)
            {
                ICookingMiniGameView miniGame = _miniGameViews[i];
                if (miniGame == null || miniGame.CanPlay(miniGameType) == false)
                    continue;

                if (selectedMiniGame != null)
                {
                    Debug.LogError($"CookingMiniGameRouterView has multiple views for type {miniGameType}.", this);
                    return null;
                }

                selectedMiniGame = miniGame;
            }

            return selectedMiniGame;
        }

        private void CollectMiniGameViews()
        {
            _miniGameViews.Clear();

            if (miniGameViewObjects != null)
            {
                for (int i = 0; i < miniGameViewObjects.Length; i++)
                    AddMiniGameViewsFromObject(miniGameViewObjects[i]);
            }

            if (autoCollectChildViews == true)
            {
                MonoBehaviour[] behaviours = GetComponentsInChildren<MonoBehaviour>(true);
                for (int i = 0; i < behaviours.Length; i++)
                {
                    ICookingMiniGameView miniGame = behaviours[i] as ICookingMiniGameView;
                    AddMiniGameView(miniGame);
                }
            }
        }

        private void AddMiniGameViewsFromObject(GameObject viewObject)
        {
            if (viewObject == null)
                return;

            MonoBehaviour[] behaviours = viewObject.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                ICookingMiniGameView miniGame = behaviours[i] as ICookingMiniGameView;
                AddMiniGameView(miniGame);
            }
        }

        private void AddMiniGameView(ICookingMiniGameView miniGame)
        {
            if (miniGame == null || ReferenceEquals(miniGame, this) == true)
                return;

            if (_miniGameViews.Contains(miniGame) == true)
                return;

            _miniGameViews.Add(miniGame);
        }

        private void SetAllMiniGamesActive(bool active)
        {
            for (int i = 0; i < _miniGameViews.Count; i++)
                SetMiniGameActive(_miniGameViews[i], active);
        }

        private static void SetMiniGameActive(ICookingMiniGameView miniGame, bool active)
        {
            Component component = miniGame as Component;
            if (component != null && component.gameObject.activeSelf != active)
                component.gameObject.SetActive(active);
        }
    }
}
