using System;
using System.Collections.Generic;
using UnityEngine;
using Work.Core.EventBus;
using Work.Dispatch.Code.Data;
using Work.NPC.Code.Runtime;
using Work.Items.Code;
using Work.Players.Code.Inventory;
using Work.TimeSystem;

namespace Work.Dispatch.Code.Runtime
{
    [DefaultExecutionOrder(-800)]
    public sealed class DispatchManager : MonoBehaviour, INpcAvailabilityRule
    {
        [SerializeField] private DispatchCatalogSO catalog;
        [SerializeField] private GameTimeService gameTimeService;
        [SerializeField] private PlayerInventoryModule playerInventory;
        [SerializeField] private bool persistDispatch = true;
        [SerializeField] private string saveKey = DispatchRepository.DefaultSaveKey;

        private readonly DispatchValidator _validator = new DispatchValidator();
        private readonly DispatchDurationCalculator _durationCalculator = new DispatchDurationCalculator();
        private readonly DispatchOutcomeResolver _outcomeResolver = new DispatchOutcomeResolver();

        private DispatchRepository _repository;
        private DispatchRuntimeState _state;
        private bool _subscribed;

        public DispatchCatalogSO Catalog => catalog;
        public DispatchJob ActiveJob => EnsureState().ActiveJob;
        public IReadOnlyList<DispatchJob> ReturnedReports => EnsureState().ReturnedReports;
        public bool HasActiveJob => EnsureState().HasActiveJob;

        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void Initialize()
        {
            if (_state != null)
            {
                return;
            }

            if (gameTimeService == null)
            {
                gameTimeService = FindFirstObjectByType<GameTimeService>();
            }

            _repository = new DispatchRepository(saveKey);
            DispatchSaveData saveData = persistDispatch ? _repository.Load() : new DispatchSaveData();
            _state = new DispatchRuntimeState(saveData);

            if (gameTimeService != null && _state.Reconcile(gameTimeService.TotalElapsedTime) != null)
            {
                Save();
            }
        }

        public DispatchValidationResult ValidateDraft(
            DispatchDraft draft,
            DispatchNpcEligibility eligibility)
        {
            return _validator.Validate(draft, catalog, eligibility, HasActiveJob);
        }

        public bool TryBuildEstimate(
            DispatchDraft draft,
            DispatchNpcEligibility eligibility,
            out DispatchEstimate estimate,
            out DispatchValidationResult validation)
        {
            validation = ValidateDraft(draft, eligibility);
            if (validation.IsValid == false)
            {
                estimate = null;
                return false;
            }

            catalog.TryFindRegion(draft.RegionId, out DispatchRegionSO region);
            catalog.TryFindNpcRule(draft.NpcId, out DispatchNpcRule npcRule);
            estimate = DispatchEstimate.Build(draft, region, npcRule, _durationCalculator);
            return true;
        }

        public bool TryStartDispatch(
            DispatchDraft draft,
            DispatchNpcEligibility eligibility,
            out DispatchJob job,
            out DispatchValidationResult validation)
        {
            if (gameTimeService == null)
            {
                job = null;
                validation = new DispatchValidationResult(
                    DispatchValidationError.ConfigurationMissing,
                    "게임 시간 시스템을 찾을 수 없습니다.");
                return false;
            }

            if (TryBuildEstimate(draft, eligibility, out DispatchEstimate estimate, out validation) == false)
            {
                job = null;
                return false;
            }

            catalog.TryFindRegion(draft.RegionId, out DispatchRegionSO region);
            int randomSeed = Guid.NewGuid().GetHashCode();
            DispatchRareSettings rareSettings = _outcomeResolver.BuildRareSettings(
                region.RareRewards,
                estimate.GatherTime);
            List<DispatchRewardData> plannedRewards = _outcomeResolver.Resolve(
                estimate.Requests,
                rareSettings,
                randomSeed);

            int startedAt = gameTimeService.TotalElapsedTime;
            job = new DispatchJob
            {
                JobId = Guid.NewGuid().ToString("N"),
                NpcId = draft.NpcId,
                RegionId = draft.RegionId,
                Requests = new List<DispatchResolvedRequest>(estimate.Requests),
                StartedAtTotalTime = startedAt,
                RequiredTime = estimate.RequiredTime,
                CompleteAtTotalTime = checked(startedAt + estimate.RequiredTime),
                RandomSeed = randomSeed,
                State = DispatchState.Active,
                Rewards = plannedRewards
            };

            if (EnsureState().TryStart(job) == false)
            {
                validation = new DispatchValidationResult(
                    DispatchValidationError.ActiveDispatchExists,
                    "현재 다른 파견이 진행 중입니다.");
                job = null;
                return false;
            }

            Save();
            Bus<DispatchStartedEvent>.Raise(new DispatchStartedEvent(job));
            return true;
        }

        public bool IsNpcDispatched(string npcId)
        {
            return EnsureState().IsNpcDispatched(npcId);
        }

        public bool IsNpcAvailable(string npcId)
        {
            return IsNpcDispatched(npcId) == false;
        }

        public DispatchClaimResult ClaimReport(string jobId)
        {
            DispatchJob report = EnsureState().FindReturnedReport(jobId);
            if (report == null)
            {
                return new DispatchClaimResult(false, 0, 0);
            }

            if (playerInventory == null)
            {
                playerInventory = FindFirstObjectByType<PlayerInventoryModule>();
            }

            if (playerInventory == null || catalog == null || catalog.ItemCatalog == null)
            {
                return new DispatchClaimResult(true, 0, CalculateRemainingAmount(report));
            }

            List<DispatchRewardData> claimableRewards = new List<DispatchRewardData>();
            List<InventoryItemStack> itemStacks = new List<InventoryItemStack>();

            for (int i = 0; i < report.Rewards.Count; i++)
            {
                DispatchRewardData reward = report.Rewards[i];
                if (reward == null || reward.RemainingAmount <= 0)
                {
                    continue;
                }

                if (catalog.ItemCatalog.TryFindItem(reward.ItemId, out ItemDataSO item) == false)
                {
                    continue;
                }

                claimableRewards.Add(reward);
                itemStacks.Add(new InventoryItemStack(item, reward.RemainingAmount));
            }

            int addedAmount = 0;
            if (itemStacks.Count > 0)
            {
                InventoryAddResult[] addResults = new InventoryAddResult[itemStacks.Count];
                InventoryBatchAddResult batchResult = playerInventory.AddItems(
                    itemStacks.ToArray(),
                    0,
                    itemStacks.Count,
                    addResults,
                    0);
                addedAmount = batchResult.AddedAmount;

                for (int i = 0; i < claimableRewards.Count; i++)
                {
                    claimableRewards[i].RemainingAmount = addResults[i].RemainingAmount;
                }
            }

            int remainingAmount = CalculateRemainingAmount(report);
            if (remainingAmount <= 0)
            {
                report.State = DispatchState.Claimed;
                EnsureState().RemoveClaimedReport(report.JobId);
            }

            Save();
            Bus<DispatchReportsChangedEvent>.Raise(new DispatchReportsChangedEvent());
            return new DispatchClaimResult(true, addedAmount, remainingAmount);
        }

        public void Save()
        {
            if (persistDispatch)
            {
                EnsureRepository().Save(EnsureState().CreateSaveData());
            }
        }

        [ContextMenu("Reset Saved Dispatch")]
        public void ResetSavedDispatch()
        {
            EnsureRepository().Delete();
            _state = new DispatchRuntimeState();
            Bus<DispatchReportsChangedEvent>.Raise(new DispatchReportsChangedEvent());
        }

        private void HandleGameTimeAdvanced(GameTimeAdvancedEvent gameEvent)
        {
            DispatchJob returnedJob = EnsureState().Reconcile(gameEvent.CurrentTotalTime);
            if (returnedJob == null)
            {
                return;
            }

            Save();
            Bus<DispatchReturnedEvent>.Raise(new DispatchReturnedEvent(returnedJob));
            Bus<DispatchReportsChangedEvent>.Raise(new DispatchReportsChangedEvent());
        }

        private void Subscribe()
        {
            if (_subscribed)
            {
                return;
            }

            Bus<GameTimeAdvancedEvent>.Events += HandleGameTimeAdvanced;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (_subscribed == false)
            {
                return;
            }

            Bus<GameTimeAdvancedEvent>.Events -= HandleGameTimeAdvanced;
            _subscribed = false;
        }

        private DispatchRuntimeState EnsureState()
        {
            if (_state == null)
            {
                Initialize();
            }

            return _state;
        }

        private DispatchRepository EnsureRepository()
        {
            if (_repository == null)
            {
                _repository = new DispatchRepository(saveKey);
            }

            return _repository;
        }

        private static int CalculateRemainingAmount(DispatchJob report)
        {
            int total = 0;
            if (report?.Rewards == null)
            {
                return total;
            }

            for (int i = 0; i < report.Rewards.Count; i++)
            {
                DispatchRewardData reward = report.Rewards[i];
                if (reward != null)
                {
                    total += Mathf.Max(0, reward.RemainingAmount);
                }
            }

            return total;
        }
    }
}
