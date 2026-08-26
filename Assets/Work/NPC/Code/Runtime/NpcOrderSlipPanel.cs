using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Work.NPC.Code.Runtime
{
    public sealed class NpcOrderSlipPanel : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI contentText;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Initial State")]
        [SerializeField] private bool visibleOnStart;

        [Header("Typing")]
        [SerializeField, Min(0.001f)] private float characterDelay = 0.025f;
        [SerializeField, Min(1)] private int maxEntries = 12;

        [Header("Enter Motion")]
        [SerializeField] private bool playEnterAnimation = true;
        [SerializeField, Min(0f)] private float enterDropDistance = 96f;
        [SerializeField, Min(0f)] private float enterDuration = 0.35f;
        [SerializeField, Min(0f)] private float enterFadeDuration = 0.14f;
        [SerializeField] private Ease enterEase = Ease.OutBack;

        private readonly Queue<string> _queuedEntries = new Queue<string>();
        private readonly List<string> _completedEntries = new List<string>();
        private readonly StringBuilder _displayedText = new StringBuilder();
        private CancellationTokenSource _animationCancellationTokenSource;
        private RectTransform _rectTransform;
        private Sequence _enterSequence;
        private Vector2 _defaultAnchoredPosition;
        private bool _isProcessingQueue;
        private bool _hasEntered;
        private bool _hasDefaultAnchoredPosition;
        private bool _isVisible;
        private int _entrySequence;

        private void Awake()
        {
            _rectTransform = transform as RectTransform;

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            if (contentText == null)
            {
                Debug.LogError("NpcOrderSlipPanel contentText is missing. Assign titleText/contentText/canvasGroup from a prefab or inspector reference.", this);
            }

            if (canvasGroup == null)
            {
                Debug.LogError("NpcOrderSlipPanel canvasGroup is missing. Assign it from a prefab or inspector reference.", this);
            }

            CaptureDefaultAnchoredPosition();
            SetVisible(visibleOnStart);
            RefreshContentText();
        }

        private void OnDestroy()
        {
            CancelAnimation();
            KillEnterSequence(false);
        }

        public void ResetForConversation(string eventId = "", string npcId = "")
        {
            CancelAnimation();

            _queuedEntries.Clear();
            _completedEntries.Clear();
            _displayedText.Clear();
            _entrySequence = 0;
            _hasEntered = false;
            _isProcessingQueue = false;
            SetVisible(false);
            RefreshContentText();
        }

        public void AppendOrderClues(IEnumerable<string> clues)
        {
            if (clues == null)
                return;

            bool startedFirstEntry = false;
            foreach (string clue in clues)
            {
                string normalized = NormalizeClue(clue);
                if (string.IsNullOrWhiteSpace(normalized))
                    continue;

                _entrySequence++;
                string entry = $"{_entrySequence:00}  {normalized}";
                if (_hasEntered == false && startedFirstEntry == false)
                {
                    AppendVisibleCompletedEntry(entry);
                    _hasEntered = true;
                    startedFirstEntry = true;
                    SetVisible(true);
                    StartProcessingQueue();
                    continue;
                }

                _queuedEntries.Enqueue(entry);
            }

            if (_queuedEntries.Count > 0 && _isProcessingQueue == false)
            {
                StartProcessingQueue();
            }
        }

        public void SetVisible(bool visible)
        {
            if (canvasGroup == null)
                return;

            if (visible == true)
            {
                ShowVisible();
                return;
            }

            HideVisible();
        }

        private void ShowVisible()
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            if (_isVisible == true)
            {
                return;
            }

            _isVisible = true;
            if (playEnterAnimation == false || enterDuration <= 0f || _rectTransform == null)
            {
                RestoreDefaultAnchoredPosition();
                canvasGroup.alpha = 1f;
                return;
            }

            PlayEnterAnimation();
        }

        private void HideVisible()
        {
            KillEnterSequence(false);
            RestoreDefaultAnchoredPosition();
            _isVisible = false;
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        private void PlayEnterAnimation()
        {
            CaptureDefaultAnchoredPosition();
            KillEnterSequence(false);

            _rectTransform.anchoredPosition = _defaultAnchoredPosition + Vector2.up * enterDropDistance;
            canvasGroup.alpha = enterFadeDuration <= 0f ? 1f : 0f;

            Sequence sequence = DOTween.Sequence();
            sequence.SetTarget(this);
            sequence.Join(_rectTransform.DOAnchorPos(_defaultAnchoredPosition, enterDuration).SetEase(enterEase));

            if (enterFadeDuration > 0f)
            {
                sequence.Join(canvasGroup.DOFade(1f, enterFadeDuration).SetEase(Ease.OutQuad));
            }

            sequence.OnKill(() =>
            {
                if (_enterSequence == sequence)
                {
                    _enterSequence = null;
                }
            });
            _enterSequence = sequence;
        }

        private void KillEnterSequence(bool complete)
        {
            if (_enterSequence == null)
            {
                return;
            }

            _enterSequence.Kill(complete);
            _enterSequence = null;
        }

        private void CaptureDefaultAnchoredPosition()
        {
            if (_hasDefaultAnchoredPosition == true || _rectTransform == null)
            {
                return;
            }

            _defaultAnchoredPosition = _rectTransform.anchoredPosition;
            _hasDefaultAnchoredPosition = true;
        }

        private void RestoreDefaultAnchoredPosition()
        {
            if (_hasDefaultAnchoredPosition == false || _rectTransform == null)
            {
                return;
            }

            _rectTransform.anchoredPosition = _defaultAnchoredPosition;
        }

        private void StartProcessingQueue()
        {
            CancelAnimation();
            _animationCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            ProcessQueuedEntriesAsync(_animationCancellationTokenSource).Forget();
        }

        private async UniTask ProcessQueuedEntriesAsync(CancellationTokenSource cancellationTokenSource)
        {
            CancellationToken cancellationToken = cancellationTokenSource.Token;
            _isProcessingQueue = true;
            try
            {
                SetVisible(true);

                while (_queuedEntries.Count > 0)
                {
                    await TypeQueuedEntryAsync(_queuedEntries.Dequeue(), cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // 새 손님 시작 또는 패널 초기화로 연출이 취소되는 정상 흐름
            }
            finally
            {
                _isProcessingQueue = false;
                if (_animationCancellationTokenSource == cancellationTokenSource)
                {
                    _animationCancellationTokenSource.Dispose();
                    _animationCancellationTokenSource = null;
                }
            }
        }

        private async UniTask TypeQueuedEntryAsync(string entry, CancellationToken cancellationToken)
        {
            if (_displayedText.Length > 0)
            {
                _displayedText.AppendLine();
            }

            int delayMilliseconds = Mathf.Max(1, Mathf.RoundToInt(characterDelay * 1000f));
            for (int i = 0; i < entry.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                _displayedText.Append(entry[i]);
                RefreshContentText();
                await UniTask.Delay(delayMilliseconds, cancellationToken: cancellationToken);
            }

            AppendCompletedEntry(entry);
        }

        private void AppendCompletedEntry(string entry)
        {
            _completedEntries.Add(entry);
            TrimCompletedEntries();
        }

        private void AppendVisibleCompletedEntry(string entry)
        {
            if (_displayedText.Length > 0)
            {
                _displayedText.AppendLine();
            }

            _displayedText.Append(entry);
            AppendCompletedEntry(entry);
            RefreshContentText();
        }

        private void TrimCompletedEntries()
        {
            if (_completedEntries.Count <= maxEntries)
                return;

            while (_completedEntries.Count > maxEntries)
                _completedEntries.RemoveAt(0);

            _displayedText.Clear();
            for (int i = 0; i < _completedEntries.Count; i++)
            {
                if (i > 0)
                    _displayedText.AppendLine();

                _displayedText.Append(_completedEntries[i]);
            }

            RefreshContentText();
        }

        private void RefreshContentText()
        {
            if (contentText == null)
                return;

            contentText.text = _displayedText.Length > 0 ? _displayedText.ToString() : "...";
        }

        private void CancelAnimation()
        {
            if (_animationCancellationTokenSource == null)
            {
                return;
            }

            _animationCancellationTokenSource.Cancel();
            _animationCancellationTokenSource.Dispose();
            _animationCancellationTokenSource = null;
        }


        private static string NormalizeClue(string clue)
        {
            return string.IsNullOrWhiteSpace(clue)
                ? string.Empty
                : clue.Trim().Replace("\r", " ").Replace("\n", " ");
        }
    }
}
