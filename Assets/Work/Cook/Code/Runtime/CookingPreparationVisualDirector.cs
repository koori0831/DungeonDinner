using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using Work.Cook.Code.Data;

namespace Work.Cook.Code.Runtime
{
    /// <summary>
    /// 재료 손질 선택 후 도마 위 재료와 완성 요리 교체 연출 관리
    /// </summary>
    public sealed class CookingPreparationVisualDirector : MonoBehaviour
    {
        [SerializeField] private Transform cuttingBoard;
        [SerializeField] private GameObject temporaryDishPrefab;
        [SerializeField] private Vector3 boardCompletionLocalOffset = new Vector3(0f, 0f, -10f);
        [SerializeField, Min(0f)] private float boardExitDuration = 0.45f;
        [SerializeField, Min(0f)] private float swapDelay = 0.25f;
        [SerializeField, Min(0f)] private float boardReturnDuration = 0.45f;
        [SerializeField, Min(0f)] private float handButtonEnableDelay = 1.5f;
        [SerializeField] private Ease boardExitEase = Ease.InBack;
        [SerializeField] private Ease boardReturnEase = Ease.OutBack;

        private readonly List<GameObject> _spawnedIngredientObjects = new List<GameObject>();
        private GameObject _spawnedDishObject;
        private Vector3 _cuttingBoardDefaultLocalPosition;
        private bool _hasCapturedBoardDefault;
        private bool _isPlayingCompletionSequence;
        private Sequence _activeSequence;

        public bool IsPlayingCompletionSequence => _isPlayingCompletionSequence;

        private void OnDisable()
        {
            KillActiveSequence();
            _isPlayingCompletionSequence = false;
        }

        private void OnDestroy()
        {
            KillActiveSequence();
        }

        /// <summary>
        /// 선택된 손질 재료 프리팹을 도마 위 원점에 생성
        /// </summary>
        /// <param name="ingredient">손질 완료된 재료 데이터</param>
        public void SpawnPreparedIngredient(IngredientSO ingredient)
        {
            if (cuttingBoard == null || ingredient == null || ingredient.ModelPrefab == null)
            {
                return;
            }

            ClearDishObject();

            GameObject ingredientObject = Instantiate(ingredient.ModelPrefab, cuttingBoard);
            ingredientObject.name = ingredient.ModelPrefab.name;
            Transform ingredientTransform = ingredientObject.transform;
            ingredientTransform.localPosition = Vector3.zero;
            ingredientTransform.localRotation = ingredient.ModelPrefab.transform.localRotation;
            ingredientTransform.localScale = ingredient.ModelPrefab.transform.localScale;
            _spawnedIngredientObjects.Add(ingredientObject);
        }

        /// <summary>
        /// 도마 퇴장 후 손질 재료를 완성 요리 프리팹으로 교체하고 도마를 복귀
        /// </summary>
        /// <param name="dishReplaced">도마 위 내용물이 완성 요리로 교체된 후 실행할 콜백</param>
        /// <param name="completed">요리 도마 복귀 및 버튼 활성화 대기 완료 후 실행할 콜백</param>
        /// <returns>연출 시작 여부</returns>
        public bool PlayCompletionSequence(Action dishReplaced, Action completed)
        {
            CaptureBoardDefaultIfNeeded();

            if (cuttingBoard == null || temporaryDishPrefab == null || _isPlayingCompletionSequence == true)
            {
                return false;
            }

            KillActiveSequence();
            _isPlayingCompletionSequence = true;

            Vector3 hiddenPosition = _cuttingBoardDefaultLocalPosition + boardCompletionLocalOffset;
            _activeSequence = DOTween.Sequence();
            _activeSequence.SetTarget(cuttingBoard);
            _activeSequence.Append(cuttingBoard.DOLocalMove(hiddenPosition, boardExitDuration).SetEase(boardExitEase));
            _activeSequence.AppendInterval(swapDelay);
            _activeSequence.AppendCallback(SwapIngredientsToDish);
            _activeSequence.AppendCallback(() => dishReplaced?.Invoke());
            _activeSequence.Append(cuttingBoard.DOLocalMove(_cuttingBoardDefaultLocalPosition, boardReturnDuration).SetEase(boardReturnEase));
            _activeSequence.AppendInterval(handButtonEnableDelay);
            _activeSequence.OnComplete(() => CompleteSequence(completed));
            return true;
        }

        /// <summary>
        /// NPC에게 음식을 건넨 뒤 도마를 치우고 남은 요리 오브젝트 제거
        /// </summary>
        public void PlayDishDismissSequence()
        {
            CaptureBoardDefaultIfNeeded();

            if (cuttingBoard == null || _hasCapturedBoardDefault == false)
            {
                ClearDishObject();
                return;
            }

            KillActiveSequence();

            Vector3 hiddenPosition = _cuttingBoardDefaultLocalPosition + boardCompletionLocalOffset;
            _activeSequence = DOTween.Sequence();
            _activeSequence.SetTarget(cuttingBoard);
            _activeSequence.Append(cuttingBoard.DOLocalMove(hiddenPosition, boardExitDuration).SetEase(boardExitEase));
            _activeSequence.AppendCallback(ClearDishObject);
            _activeSequence.OnComplete(() => _activeSequence = null);
        }

        private void CaptureBoardDefaultIfNeeded()
        {
            if (_hasCapturedBoardDefault == true || cuttingBoard == null)
            {
                return;
            }

            _cuttingBoardDefaultLocalPosition = cuttingBoard.localPosition;
            _hasCapturedBoardDefault = true;
        }

        private void SwapIngredientsToDish()
        {
            ClearIngredientObjects();
            ClearDishObject();

            if (temporaryDishPrefab == null || cuttingBoard == null)
            {
                return;
            }

            _spawnedDishObject = Instantiate(temporaryDishPrefab, cuttingBoard);
            _spawnedDishObject.name = temporaryDishPrefab.name;
            Transform dishTransform = _spawnedDishObject.transform;
            dishTransform.localPosition = Vector3.zero;
            dishTransform.localRotation = temporaryDishPrefab.transform.localRotation;
            dishTransform.localScale = temporaryDishPrefab.transform.localScale;
        }

        private void CompleteSequence(Action completed)
        {
            if (cuttingBoard != null)
            {
                cuttingBoard.localPosition = _cuttingBoardDefaultLocalPosition;
            }

            _activeSequence = null;
            _isPlayingCompletionSequence = false;
            completed?.Invoke();
        }

        private void KillActiveSequence()
        {
            if (_activeSequence == null)
            {
                return;
            }

            _activeSequence.Kill();
            _activeSequence = null;
        }

        private void ClearIngredientObjects()
        {
            for (int i = _spawnedIngredientObjects.Count - 1; i >= 0; i--)
            {
                if (_spawnedIngredientObjects[i] != null)
                {
                    Destroy(_spawnedIngredientObjects[i]);
                }
            }

            _spawnedIngredientObjects.Clear();
        }

        private void ClearDishObject()
        {
            if (_spawnedDishObject == null)
            {
                return;
            }

            Destroy(_spawnedDishObject);
            _spawnedDishObject = null;
        }
    }
}
