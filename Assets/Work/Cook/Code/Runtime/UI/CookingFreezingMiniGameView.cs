using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Work.Cook.Code.Data;
using Work.Cook.Code.Runtime.Core;

namespace Work.Cook.Code.Runtime.UI
{
    public sealed class CookingFreezingMiniGameView : CookingOverlayMiniGameController,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private Image[] frostCells;
        [SerializeField] private Image handIndicator;
        [SerializeField] private Color frostColor = new Color(0.55f, 0.9f, 1f, 0.82f);

        private CookingMiniGameOverlayProfile _profile;
        private float[] _cells;
        private float _startedTime;
        private Vector2 _lastPoint;

        public override bool CanPlay(CookingMiniGameType miniGameType)
        {
            return miniGameType == CookingMiniGameType.Freezing;
        }

        public override bool StartMiniGame(
            IngredientSO ingredient,
            IngredientPreparationOption option,
            Action<CookingMiniGameResult> completed)
        {
            if (Begin(ingredient, option, CookingMiniGameType.Freezing, completed) == false
                || frostCells == null
                || frostCells.Length == 0)
            {
                return false;
            }

            _profile = GetProfile(CookingMiniGameType.Freezing);
            _cells = new float[frostCells.Length];
            _startedTime = Time.unscaledTime;
            for (int i = 0; i < frostCells.Length; i++)
                RefreshCell(i);
            if (handIndicator != null)
                handIndicator.gameObject.SetActive(false);
            Host.SetInstruction("재료 표면 전체를 문질러 냉기를 고르게 퍼뜨리세요.");
            Host.SetStatus("서리가 빈 곳을 채우세요");
            ConfigureHud("표면 전체를 고르게 문지르기 ↔", false, true, true);
            SetTargetState(0f, _profile.TargetMin, _profile.TargetMax, "냉각 부족");
            SetTimer(_profile.MaximumDuration, _profile.MaximumDuration);
            return true;
        }

        private void Update()
        {
            if (Completion == null)
                return;
            float elapsed = Time.unscaledTime - _startedTime;
            SetTimer(Mathf.Max(0f, _profile.MaximumDuration - elapsed), _profile.MaximumDuration);
            if (elapsed >= _profile.MaximumDuration)
                Finish(CookingMiniGameType.Freezing, Mathf.Min(0.44f, CookingMiniGameScoring.ScoreFreezing(
                    _cells, _profile.TargetMin, _profile.TargetMax)), "냉기를 충분히 고르게 퍼뜨리지 못했습니다.");
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (Completion == null || TryCapturePointer(eventData) == false
                || TryGetLocalPosition(eventData, out _lastPoint) == false)
            {
                ReleasePointer();
                return;
            }
            MoveHand(_lastPoint);
            CoolAt(_lastPoint, 0.06f);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (IsActivePointer(eventData) == false || TryGetLocalPosition(eventData, out Vector2 point) == false)
                return;

            float amount = Mathf.Clamp(Vector2.Distance(_lastPoint, point) / 240f, 0.025f, 0.12f);
            _lastPoint = point;
            MoveHand(point);
            CoolAt(point, amount);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (IsActivePointer(eventData) == false)
                return;
            ReleasePointer();
            if (handIndicator != null)
                handIndicator.gameObject.SetActive(false);

            float mean = 0f;
            float min = 1f;
            for (int i = 0; i < _cells.Length; i++)
            {
                mean += _cells[i];
                min = Mathf.Min(min, _cells[i]);
            }
            mean /= _cells.Length;
            if (mean >= _profile.TargetMin && min >= _profile.TargetMin * 0.72f)
            {
                Finish(CookingMiniGameType.Freezing,
                    CookingMiniGameScoring.ScoreFreezing(_cells, _profile.TargetMin, _profile.TargetMax),
                    "표면 전체에 냉기를 고르게 분산했습니다.");
            }
        }

        private void CoolAt(Vector2 point, float amount)
        {
            bool hit = false;
            for (int i = 0; i < frostCells.Length; i++)
            {
                Image cell = frostCells[i];
                if (cell == null)
                    continue;
                Vector2 size = cell.rectTransform.rect.size;
                if (Mathf.Abs(point.x - cell.rectTransform.anchoredPosition.x) > size.x * 0.62f
                    || Mathf.Abs(point.y - cell.rectTransform.anchoredPosition.y) > size.y * 0.62f)
                {
                    continue;
                }

                hit = true;
                _cells[i] = Mathf.Clamp01(_cells[i] + amount);
                RefreshCell(i);
            }

            if (hit)
                MarkProgress();
            else
                RegisterMistake("재료 표면 안쪽을 문질러주세요.");

            float mean = 0f;
            float minimum = 1f;
            for (int i = 0; i < _cells.Length; i++)
            {
                mean += _cells[i];
                minimum = Mathf.Min(minimum, _cells[i]);
            }
            mean /= _cells.Length;
            string stateLabel;
            if (mean < _profile.TargetMin)
                stateLabel = $"냉각 {Mathf.RoundToInt(mean * 100f)}% · 빈 곳을 채우세요";
            else if (mean <= _profile.TargetMax && minimum >= _profile.TargetMin * 0.72f)
                stateLabel = "적정 · 고르게 얼었습니다";
            else if (mean > _profile.TargetMax)
                stateLabel = "과냉각 위험 · 같은 곳을 피하세요";
            else
                stateLabel = "냉각량은 충분 · 빈 곳을 더 채우세요";
            SetTargetState(mean, _profile.TargetMin, _profile.TargetMax, stateLabel);
            if (mean > _profile.TargetMax + 0.12f)
            {
                Finish(CookingMiniGameType.Freezing,
                    CookingMiniGameScoring.ScoreFreezing(_cells, _profile.TargetMin, _profile.TargetMax),
                    "일부 표면이 지나치게 얼었습니다.");
            }
        }

        private void RefreshCell(int index)
        {
            if (frostCells[index] == null)
                return;
            Color color = frostColor;
            color.a *= Mathf.Lerp(0.18f, 1f, _cells[index]);
            frostCells[index].color = color;
        }

        private void MoveHand(Vector2 point)
        {
            if (handIndicator == null)
                return;
            handIndicator.gameObject.SetActive(true);
            handIndicator.rectTransform.anchoredPosition = point;
        }
    }
}
