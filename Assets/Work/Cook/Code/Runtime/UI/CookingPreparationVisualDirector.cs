using System;
using System.Collections.Generic;
using DG.Tweening;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using Work.Cook.Code.Data;
using Work.Cook.Code.Runtime.Core;
using Work.Cook.Code.Runtime.Integration;
using Work.Cook.Code.Runtime.Systems;
using Work.Cook.Code.Runtime.UI;

namespace Work.Cook.Code.Runtime.UI
{
    /// <summary>
    /// 재료 손질 선택 후 도마 위 재료와 완성 요리 교체 연출 관리
    /// </summary>
    public sealed class CookingPreparationVisualDirector : MonoBehaviour
    {
        [SerializeField] private Transform cuttingBoard;
        [SerializeField] private GameObject temporaryDishPrefab;
        [SerializeField] private Vector3 ingredientSettleStartLocalOffset = new Vector3(0f, 0.35f, 0f);
        [SerializeField, Min(0f)] private float ingredientSettleDuration = 0.25f;
        [SerializeField] private Vector3 boardCompletionLocalOffset = new Vector3(0f, 4f, 0f);
        [SerializeField, Min(0f)] private float boardCompletionHoldDelay = 0.7f;
        [SerializeField, Min(0f)] private float boardExitDuration = 0.45f;
        [SerializeField, Min(0f)] private float swapDelay = 0.25f;
        [SerializeField, Min(0f)] private float boardReturnDuration = 0.45f;
        [SerializeField, Min(0f)] private float handButtonEnableDelay = 1.5f;
        [SerializeField] private Ease ingredientSettleEase = Ease.OutQuad;
        [SerializeField] private Ease boardExitEase = Ease.InBack;
        [SerializeField] private Ease boardReturnEase = Ease.OutBack;

        private readonly List<GameObject> _spawnedIngredientObjects = new List<GameObject>();
        private GameObject _spawnedDishObject;
        private Vector3 _cuttingBoardDefaultLocalPosition;
        private bool _hasCapturedBoardDefault;
        private bool _isPlayingCompletionSequence;
        private Sequence _activeSequence;
        private Tween _activeIngredientSettleTween;

        public bool IsPlayingCompletionSequence => _isPlayingCompletionSequence;

        private void OnDisable()
        {
            KillActiveSequence();
            KillIngredientSettleTween(false);
            _isPlayingCompletionSequence = false;
        }

        private void OnDestroy()
        {
            KillActiveSequence();
            KillIngredientSettleTween(false);
        }

        /// <summary>
        /// 선택된 손질 재료 프리팹을 도마 위 원점에 생성
        /// </summary>
        /// <param name="ingredient">손질 완료된 재료 데이터</param>
        public void SpawnPreparedIngredient(IngredientSO ingredient)
        {
            SpawnPreparedIngredientInternal(ingredient, true);
        }

        /// <summary>
        /// 도마 퇴장 후 손질 재료를 완성 요리 프리팹으로 교체하고 도마를 복귀
        /// </summary>
        /// <param name="dishReplaced">도마 위 완성 요리가 복귀한 후 실행할 콜백</param>
        /// <param name="completed">요리 도마 복귀 및 버튼 활성화 대기 완료 후 실행할 콜백</param>
        /// <returns>연출 시작 여부</returns>
        public bool PlayCompletionSequence(Action dishReplaced, Action completed)
        {
            return PlayCompletionSequence(null, dishReplaced, completed);
        }

        /// <summary>
        /// 마지막 손질 재료 안착 후 도마 퇴장, 완성 요리 교체, 도마 복귀 연출 재생
        /// </summary>
        /// <param name="finalIngredient">마지막으로 도마 위에 올릴 손질 재료</param>
        /// <param name="dishReplaced">도마 위 완성 요리가 복귀한 후 실행할 콜백</param>
        /// <param name="completed">요리 도마 복귀 및 버튼 활성화 대기 완료 후 실행할 콜백</param>
        /// <returns>연출 시작 여부</returns>
        public bool PlayCompletionSequence(IngredientSO finalIngredient, Action dishReplaced, Action completed)
        {
            CaptureBoardDefaultIfNeeded();

            if (cuttingBoard == null || temporaryDishPrefab == null || _isPlayingCompletionSequence == true)
            {
                return false;
            }

            KillActiveSequence();
            _isPlayingCompletionSequence = true;

            if (finalIngredient != null)
            {
                SpawnPreparedIngredientInternal(finalIngredient, true);
            }

            Vector3 hiddenPosition = _cuttingBoardDefaultLocalPosition + boardCompletionLocalOffset;
            float settleDelay = GetRemainingIngredientSettleDelay();
            _activeSequence = DOTween.Sequence();
            _activeSequence.SetTarget(cuttingBoard);
            if (settleDelay > 0f)
            {
                _activeSequence.AppendInterval(settleDelay);
            }

            _activeSequence.AppendInterval(boardCompletionHoldDelay);
            _activeSequence.Append(cuttingBoard.DOLocalMove(hiddenPosition, boardExitDuration).SetEase(boardExitEase));
            _activeSequence.AppendInterval(swapDelay);
            _activeSequence.AppendCallback(SwapIngredientsToDish);
            _activeSequence.Append(cuttingBoard.DOLocalMove(_cuttingBoardDefaultLocalPosition, boardReturnDuration).SetEase(boardReturnEase));
            _activeSequence.AppendCallback(() => dishReplaced?.Invoke());
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

        private bool SpawnPreparedIngredientInternal(IngredientSO ingredient, bool playSettleAnimation)
        {
            if (cuttingBoard == null || ingredient == null || ingredient.ModelPrefab == null)
            {
                return false;
            }

            KillIngredientSettleTween(true);
            ClearDishObject();

            GameObject ingredientObject = Instantiate(ingredient.ModelPrefab, cuttingBoard);
            ingredientObject.name = ingredient.ModelPrefab.name;
            Transform ingredientTransform = ingredientObject.transform;
            ingredientTransform.localRotation = ingredient.ModelPrefab.transform.localRotation;
            ingredientTransform.localScale = ingredient.ModelPrefab.transform.localScale;
            _spawnedIngredientObjects.Add(ingredientObject);

            PlayIngredientSettle(ingredientTransform, playSettleAnimation);
            return true;
        }

        private void PlayIngredientSettle(Transform ingredientTransform, bool playSettleAnimation)
        {
            if (ingredientTransform == null)
            {
                return;
            }

            if (playSettleAnimation == false
                || ingredientSettleDuration <= 0f
                || ingredientSettleStartLocalOffset == Vector3.zero)
            {
                ingredientTransform.localPosition = Vector3.zero;
                return;
            }

            ingredientTransform.localPosition = ingredientSettleStartLocalOffset;
            _activeIngredientSettleTween = ingredientTransform
                .DOLocalMove(Vector3.zero, ingredientSettleDuration)
                .SetEase(ingredientSettleEase)
                .SetTarget(ingredientTransform)
                .OnComplete(() => _activeIngredientSettleTween = null);
        }

        private float GetRemainingIngredientSettleDelay()
        {
            if (_activeIngredientSettleTween == null
                || _activeIngredientSettleTween.IsActive() == false
                || _activeIngredientSettleTween.IsComplete() == true)
            {
                return 0f;
            }

            return Mathf.Max(
                0f,
                _activeIngredientSettleTween.Duration(false) - _activeIngredientSettleTween.Elapsed(false));
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

        private void KillIngredientSettleTween(bool complete)
        {
            if (_activeIngredientSettleTween == null)
            {
                return;
            }

            _activeIngredientSettleTween.Kill(complete);
            _activeIngredientSettleTween = null;
        }

        private void ClearIngredientObjects()
        {
            KillIngredientSettleTween(false);

            for (int i = _spawnedIngredientObjects.Count - 1; i >= 0; i--)
            {
                if (_spawnedIngredientObjects[i] != null)
                {
                    ClearEditorSelectionIfTargetWillBeDestroyed(_spawnedIngredientObjects[i]);
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

            ClearEditorSelectionIfTargetWillBeDestroyed(_spawnedDishObject);
            Destroy(_spawnedDishObject);
            _spawnedDishObject = null;
        }

        private static void ClearEditorSelectionIfTargetWillBeDestroyed(GameObject target)
        {
#if UNITY_EDITOR
            if (target == null)
            {
                return;
            }

            Transform targetTransform = target.transform;
            UnityEngine.Object[] selectedObjects = Selection.objects;
            for (int i = 0; i < selectedObjects.Length; i++)
            {
                Transform selectedTransform = GetSelectedTransform(selectedObjects[i]);
                if (selectedTransform != null
                    && (selectedTransform == targetTransform || selectedTransform.IsChildOf(targetTransform) == true))
                {
                    Selection.objects = Array.Empty<UnityEngine.Object>();
                    return;
                }
            }
#endif
        }

#if UNITY_EDITOR
        private static Transform GetSelectedTransform(UnityEngine.Object selectedObject)
        {
            GameObject selectedGameObject = selectedObject as GameObject;
            if (selectedGameObject != null)
            {
                return selectedGameObject.transform;
            }

            Component selectedComponent = selectedObject as Component;
            return selectedComponent != null ? selectedComponent.transform : null;
        }
#endif
    }
}
