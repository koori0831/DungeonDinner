using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Work.Cook.Code.Data;
using Work.Cook.Code.Runtime.Core;

namespace Work.Cook.Code.Runtime.UI
{
    public sealed class CookingChoppingMiniGameView : CookingOverlayMiniGameController,
        IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private Image[] targetImages;
        [SerializeField] private Color pendingColor = new Color(1f, 1f, 1f, 0.18f);
        [SerializeField] private Color activeColor = new Color(1f, 0.86f, 0.35f, 0.95f);
        [SerializeField] private Color completedColor = new Color(0.38f, 0.9f, 0.45f, 0.55f);

        private int[] _order;
        private int _orderIndex;
        private int _mistakes;
        private float _accuracySum;
        private float _startedTime;
        private CookingMiniGameOverlayProfile _profile;

        public override bool CanPlay(CookingMiniGameType miniGameType)
        {
            return miniGameType == CookingMiniGameType.Chopping;
        }

        public override bool StartMiniGame(
            IngredientSO ingredient,
            IngredientPreparationOption option,
            Action<CookingMiniGameResult> completed)
        {
            if (Begin(ingredient, option, CookingMiniGameType.Chopping, completed) == false
                || targetImages == null
                || targetImages.Length == 0)
            {
                return false;
            }

            _order = new int[targetImages.Length];
            for (int i = 0; i < _order.Length; i++)
                _order[i] = i;
            for (int i = _order.Length - 1; i > 0; i--)
            {
                int swap = UnityEngine.Random.Range(0, i + 1);
                (_order[i], _order[swap]) = (_order[swap], _order[i]);
            }

            _orderIndex = 0;
            _mistakes = 0;
            _accuracySum = 0f;
            _startedTime = Time.unscaledTime;
            _profile = GetProfile(CookingMiniGameType.Chopping);
            Host.SetInstruction("빛나는 타격점을 순서대로 빠르게 누르세요.");
            ConfigureHud("빛나는 지점을 빠르게 연타!", true, false, true);
            SetProgress(0f, $"타격 0/{targetImages.Length}");
            SetTimer(_profile.Duration, _profile.Duration);
            RefreshTargets();
            return true;
        }

        private void Update()
        {
            if (Completion == null || _profile == null)
                return;

            float elapsed = Time.unscaledTime - _startedTime;
            SetTimer(Mathf.Max(0f, _profile.Duration - elapsed), _profile.Duration);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (Completion != null)
                TryCapturePointer(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (Completion == null || IsActivePointer(eventData) == false)
                return;
            ReleasePointer();
            if (TryGetLocalPosition(eventData, out Vector2 point) == false)
                return;

            int targetIndex = _order[_orderIndex];
            Image target = targetImages[targetIndex];
            float radius = target != null ? Mathf.Max(target.rectTransform.rect.width, target.rectTransform.rect.height) * 0.75f : 1f;
            float distance = target != null ? Vector2.Distance(point, target.rectTransform.anchoredPosition) : float.MaxValue;
            if (distance > radius)
            {
                _mistakes++;
                RegisterMistake("빛나는 지점을 정확히 눌러주세요.");
                return;
            }

            _accuracySum += Mathf.Clamp01(1f - distance / Mathf.Max(1f, radius));
            _orderIndex++;
            MarkProgress();
            SetProgress((float)_orderIndex / _order.Length, $"타격 {_orderIndex}/{_order.Length}");
            if (_orderIndex >= _order.Length)
            {
                float elapsed = Time.unscaledTime - _startedTime;
                float speedScore = 1f - Mathf.InverseLerp(_profile.Duration * 0.45f, _profile.Duration, elapsed);
                float accuracyScore = Mathf.Clamp01(_accuracySum / _order.Length - _mistakes * 0.12f);
                Finish(CookingMiniGameType.Chopping, speedScore * 0.4f + accuracyScore * 0.6f,
                    "타격점을 빠르고 고르게 다졌습니다.");
                return;
            }

            RefreshTargets();
        }

        private void RefreshTargets()
        {
            int active = _order != null && _orderIndex < _order.Length ? _order[_orderIndex] : -1;
            for (int i = 0; i < targetImages.Length; i++)
            {
                if (targetImages[i] == null)
                    continue;
                bool done = IsCompleted(i);
                targetImages[i].color = done ? completedColor : i == active ? activeColor : pendingColor;
            }
            Host.SetStatus($"다음 타격점 · {_orderIndex + 1}/{targetImages.Length}");
        }

        private bool IsCompleted(int targetIndex)
        {
            for (int i = 0; i < _orderIndex; i++)
            {
                if (_order[i] == targetIndex)
                    return true;
            }
            return false;
        }
    }
}
