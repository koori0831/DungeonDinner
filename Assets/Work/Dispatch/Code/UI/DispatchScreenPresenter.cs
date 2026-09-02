using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Work.Core.EventBus;
using Work.Dispatch.Code.Data;
using Work.Dispatch.Code.Runtime;
using Work.NPC.Code.Data;
using Work.TimeSystem;

namespace Work.Dispatch.Code.UI
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class DispatchScreenPresenter : MonoBehaviour
    {
        [Header("Runtime")]
        [SerializeField] private DispatchManager dispatchManager;
        [SerializeField] private DispatchNpcQuery npcQuery;
        [SerializeField] private GameTimeService gameTimeService;

        [Header("UXML row templates")]
        [SerializeField] private VisualTreeAsset npcRowTemplate;
        [SerializeField] private VisualTreeAsset regionRowTemplate;
        [SerializeField] private VisualTreeAsset materialRowTemplate;
        [SerializeField] private VisualTreeAsset reportRowTemplate;

        private readonly List<NpcRowModel> _npcRows = new List<NpcRowModel>();
        private readonly List<RegionRowModel> _regionRows = new List<RegionRowModel>();
        private readonly List<MaterialRowModel> _materialRows = new List<MaterialRowModel>();
        private readonly List<ReportRowModel> _reportRows = new List<ReportRowModel>();
        private readonly Dictionary<string, int> _requestedAmounts =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        private UIDocument _document;
        private VisualElement _root;
        private VisualElement _requestPage;
        private VisualElement _activePage;
        private VisualElement _reportPage;
        private VisualElement _confirmationModal;
        private Label _dayLabel;
        private Label _timeLabel;
        private Label _summaryNpc;
        private Label _summaryRegion;
        private Label _summaryMaterials;
        private Label _summaryDuration;
        private Label _summaryReturn;
        private Label _summaryRare;
        private Label _requestMessage;
        private Label _activeNpc;
        private Label _activeRegion;
        private Label _activeRequests;
        private Label _activeRemaining;
        private Label _reportEmptyLabel;
        private Label _confirmationText;
        private Label _toastLabel;
        private ProgressBar _activeProgress;
        private ListView _npcList;
        private ListView _regionList;
        private ListView _materialList;
        private ListView _reportList;
        private Button _closeButton;
        private Button _requestTabButton;
        private Button _activeTabButton;
        private Button _reportTabButton;
        private Button _dispatchButton;
        private Button _confirmationAcceptButton;
        private Button _confirmationCancelButton;

        private string _selectedNpcId;
        private string _selectedRegionId;
        private DispatchDraft _pendingDraft;
        private DispatchNpcEligibility _pendingEligibility;
        private bool _initialized;
        private bool _subscribed;

        public event Action Closed;
        public bool IsVisible => _root != null && _root.style.display != DisplayStyle.None;

        private void Awake()
        {
            ResolveRuntimeReferences();
        }

        private void OnEnable()
        {
            ResolveRuntimeReferences();
            InitializeVisualTree();
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
            UnbindButtons();
            _initialized = false;
        }

        public void Show()
        {
            InitializeVisualTree();
            if (_root == null)
            {
                return;
            }

            SetVisible(_root, true);
            SetVisible(_confirmationModal, false);
            RefreshAll();

            if (dispatchManager != null && dispatchManager.HasActiveJob)
                ShowPage(PageType.Active);
            else
                ShowPage(PageType.Request);
        }

        public void Hide()
        {
            SetVisible(_confirmationModal, false);
            SetVisible(_root, false);
        }

        private void Close()
        {
            Hide();
            Closed?.Invoke();
        }

        private void InitializeVisualTree()
        {
            if (_initialized)
            {
                return;
            }

            _document ??= GetComponent<UIDocument>();
            VisualElement documentRoot = _document != null ? _document.rootVisualElement : null;
            _root = documentRoot?.Q<VisualElement>("dispatch-root");
            if (_root == null)
            {
                Debug.LogError("DispatchScreen.uxml의 dispatch-root를 찾을 수 없습니다.", this);
                return;
            }

            QueryElements();
            ConfigureLists();
            BindButtons();
            SetVisible(_root, false);
            SetVisible(_confirmationModal, false);
            _initialized = true;
        }

        private void QueryElements()
        {
            _requestPage = _root.Q<VisualElement>("request-page");
            _activePage = _root.Q<VisualElement>("active-page");
            _reportPage = _root.Q<VisualElement>("report-page");
            _confirmationModal = _root.Q<VisualElement>("confirmation-modal");
            _dayLabel = _root.Q<Label>("day-label");
            _timeLabel = _root.Q<Label>("time-label");
            _summaryNpc = _root.Q<Label>("summary-npc");
            _summaryRegion = _root.Q<Label>("summary-region");
            _summaryMaterials = _root.Q<Label>("summary-materials");
            _summaryDuration = _root.Q<Label>("summary-duration");
            _summaryReturn = _root.Q<Label>("summary-return");
            _summaryRare = _root.Q<Label>("summary-rare");
            _requestMessage = _root.Q<Label>("request-message");
            _activeNpc = _root.Q<Label>("active-npc");
            _activeRegion = _root.Q<Label>("active-region");
            _activeRequests = _root.Q<Label>("active-requests");
            _activeRemaining = _root.Q<Label>("active-remaining");
            _reportEmptyLabel = _root.Q<Label>("report-empty-label");
            _confirmationText = _root.Q<Label>("confirmation-text");
            _toastLabel = _root.Q<Label>("toast-label");
            _activeProgress = _root.Q<ProgressBar>("active-progress");
            _npcList = _root.Q<ListView>("npc-list");
            _regionList = _root.Q<ListView>("region-list");
            _materialList = _root.Q<ListView>("material-list");
            _reportList = _root.Q<ListView>("report-list");
            _closeButton = _root.Q<Button>("close-button");
            _requestTabButton = _root.Q<Button>("request-tab-button");
            _activeTabButton = _root.Q<Button>("active-tab-button");
            _reportTabButton = _root.Q<Button>("report-tab-button");
            _dispatchButton = _root.Q<Button>("dispatch-button");
            _confirmationAcceptButton = _root.Q<Button>("confirmation-accept-button");
            _confirmationCancelButton = _root.Q<Button>("confirmation-cancel-button");
        }

        private void ConfigureLists()
        {
            ConfigureNpcList();
            ConfigureRegionList();
            ConfigureMaterialList();
            ConfigureReportList();
        }

        private void ConfigureNpcList()
        {
            if (_npcList == null || npcRowTemplate == null)
                return;

            _npcList.selectionType = SelectionType.None;
            _npcList.itemsSource = _npcRows;
            _npcList.makeItem = () =>
            {
                VisualElement row = npcRowTemplate.CloneTree();
                row.RegisterCallback<ClickEvent>(_ =>
                {
                    if (row.userData is NpcRowModel model)
                        SelectNpc(model);
                });
                return row;
            };
            _npcList.bindItem = (row, index) => BindNpcRow(row, _npcRows[index]);
        }

        private void ConfigureRegionList()
        {
            if (_regionList == null || regionRowTemplate == null)
                return;

            _regionList.selectionType = SelectionType.None;
            _regionList.itemsSource = _regionRows;
            _regionList.makeItem = () =>
            {
                VisualElement row = regionRowTemplate.CloneTree();
                row.RegisterCallback<ClickEvent>(_ =>
                {
                    if (row.userData is RegionRowModel model)
                        SelectRegion(model);
                });
                return row;
            };
            _regionList.bindItem = (row, index) => BindRegionRow(row, _regionRows[index]);
        }

        private void ConfigureMaterialList()
        {
            if (_materialList == null || materialRowTemplate == null)
                return;

            _materialList.selectionType = SelectionType.None;
            _materialList.itemsSource = _materialRows;
            _materialList.makeItem = () =>
            {
                VisualElement row = materialRowTemplate.CloneTree();
                row.Q<Button>("decrease-button").clicked += () => ChangeMaterialAmount(row, -1);
                row.Q<Button>("increase-button").clicked += () => ChangeMaterialAmount(row, 1);
                return row;
            };
            _materialList.bindItem = (row, index) => BindMaterialRow(row, _materialRows[index]);
        }

        private void ConfigureReportList()
        {
            if (_reportList == null || reportRowTemplate == null)
                return;

            _reportList.selectionType = SelectionType.None;
            _reportList.itemsSource = _reportRows;
            _reportList.makeItem = () =>
            {
                VisualElement row = reportRowTemplate.CloneTree();
                row.Q<Button>("claim-button").clicked += () => ClaimReport(row);
                return row;
            };
            _reportList.bindItem = (row, index) => BindReportRow(row, _reportRows[index]);
        }

        private void BindButtons()
        {
            _closeButton.clicked += Close;
            _requestTabButton.clicked += ShowRequestPage;
            _activeTabButton.clicked += ShowActivePage;
            _reportTabButton.clicked += ShowReportPage;
            _dispatchButton.clicked += OpenConfirmation;
            _confirmationAcceptButton.clicked += ConfirmDispatch;
            _confirmationCancelButton.clicked += CancelConfirmation;
        }

        private void UnbindButtons()
        {
            if (_closeButton != null) _closeButton.clicked -= Close;
            if (_requestTabButton != null) _requestTabButton.clicked -= ShowRequestPage;
            if (_activeTabButton != null) _activeTabButton.clicked -= ShowActivePage;
            if (_reportTabButton != null) _reportTabButton.clicked -= ShowReportPage;
            if (_dispatchButton != null) _dispatchButton.clicked -= OpenConfirmation;
            if (_confirmationAcceptButton != null) _confirmationAcceptButton.clicked -= ConfirmDispatch;
            if (_confirmationCancelButton != null) _confirmationCancelButton.clicked -= CancelConfirmation;
        }

        private void RefreshAll()
        {
            RefreshHeader();
            RefreshNpcRows();
            RefreshRegionRows();
            RefreshMaterialRows();
            RefreshSummary();
            RefreshActivePage();
            RefreshReportRows();
        }

        private void RefreshHeader()
        {
            if (gameTimeService == null)
                return;

            _dayLabel.text = $"{gameTimeService.CurrentDay}일 차";
            _timeLabel.text = $"시간 {gameTimeService.CurrentTimeOfDay} / {GameTimeState.TimeUnitsPerDay}";
        }

        private void RefreshNpcRows()
        {
            _npcRows.Clear();
            DispatchCatalogSO catalog = dispatchManager != null ? dispatchManager.Catalog : null;
            IReadOnlyList<NpcData> npcs = npcQuery != null ? npcQuery.GetAllNpcs() : Array.Empty<NpcData>();

            for (int i = 0; i < npcs.Count; i++)
            {
                NpcData npc = npcs[i];
                DispatchNpcEligibility eligibility = npcQuery.GetEligibility(npc.NpcId);
                DispatchNpcRule rule = null;
                bool hasRule = catalog != null && catalog.TryFindNpcRule(npc.NpcId, out rule);
                int requiredAffinity = hasRule ? rule.RequiredAffinity : int.MaxValue;
                bool unlocked = hasRule && eligibility.Affinity >= requiredAffinity;
                _npcRows.Add(new NpcRowModel(
                    npc.NpcId,
                    npc.DisplayName,
                    eligibility.Affinity,
                    requiredAffinity,
                    unlocked));
            }

            if (_npcRows.All(row => row.NpcId != _selectedNpcId || row.Unlocked == false))
            {
                NpcRowModel firstUnlocked = _npcRows.FirstOrDefault(row => row.Unlocked);
                _selectedNpcId = firstUnlocked?.NpcId;
                _selectedRegionId = null;
                _requestedAmounts.Clear();
            }

            _npcList?.Rebuild();
        }

        private void RefreshRegionRows()
        {
            _regionRows.Clear();
            if (string.IsNullOrWhiteSpace(_selectedNpcId)
                || npcQuery == null
                || dispatchManager?.Catalog == null)
            {
                _regionList?.Rebuild();
                return;
            }

            DispatchNpcEligibility eligibility = npcQuery.GetEligibility(_selectedNpcId);
            for (int i = 0; i < dispatchManager.Catalog.Regions.Count; i++)
            {
                DispatchRegionSO region = dispatchManager.Catalog.Regions[i];
                if (region != null && eligibility.CanVisitRegion(region.RegionId))
                {
                    _regionRows.Add(new RegionRowModel(region));
                }
            }

            if (_regionRows.All(row => row.Region.RegionId != _selectedRegionId))
            {
                _selectedRegionId = _regionRows.Count > 0 ? _regionRows[0].Region.RegionId : null;
                _requestedAmounts.Clear();
            }

            _regionList?.Rebuild();
        }

        private void RefreshMaterialRows()
        {
            _materialRows.Clear();
            DispatchRegionSO region = FindSelectedRegion();
            if (region != null)
            {
                for (int i = 0; i < region.Materials.Count; i++)
                {
                    DispatchMaterialRule rule = region.Materials[i];
                    if (rule == null || rule.Item == null)
                        continue;

                    _requestedAmounts.TryGetValue(rule.ItemId, out int amount);
                    _materialRows.Add(new MaterialRowModel(rule, amount));
                }
            }

            _materialList?.Rebuild();
        }

        private void RefreshSummary()
        {
            NpcRowModel npc = _npcRows.FirstOrDefault(row => row.NpcId == _selectedNpcId);
            DispatchRegionSO region = FindSelectedRegion();
            _summaryNpc.text = npc != null ? npc.DisplayName : "선택되지 않음";
            _summaryRegion.text = region != null ? region.DisplayName : "선택되지 않음";

            DispatchDraft draft = BuildDraft();
            _summaryMaterials.text = BuildRequestText(draft.Requests, showEstimate: true);

            DispatchNpcEligibility eligibility = npcQuery != null
                ? npcQuery.GetEligibility(draft.NpcId)
                : default;

            DispatchEstimate estimate = null;
            DispatchValidationResult validation = new DispatchValidationResult(
                DispatchValidationError.ConfigurationMissing,
                "파견 시스템을 찾을 수 없습니다.");
            bool estimated = dispatchManager != null
                             && dispatchManager.TryBuildEstimate(
                                 draft,
                                 eligibility,
                                 out estimate,
                                 out validation);

            if (estimated)
            {
                _summaryDuration.text = $"예상 시간 {estimate.RequiredTime}";
                _summaryReturn.text = gameTimeService != null
                    ? $"누적 시간 {gameTimeService.TotalElapsedTime + estimate.RequiredTime}에 귀환"
                    : "귀환 예정 시간을 계산할 수 없습니다.";
                _summaryRare.text = estimate.HasRareRewardChance
                    ? "이 지역에서는 희귀한 재료를 발견할 수도 있습니다."
                    : string.Empty;
                _requestMessage.text = string.Empty;
                _dispatchButton.SetEnabled(true);
            }
            else
            {
                _summaryDuration.text = "예상 시간 -";
                _summaryReturn.text = "귀환 예정 -";
                _summaryRare.text = string.Empty;
                _requestMessage.text = dispatchManager != null ? validation.Message : "파견 시스템을 찾을 수 없습니다.";
                _dispatchButton.SetEnabled(false);
            }
        }

        private void RefreshActivePage()
        {
            DispatchJob job = dispatchManager != null ? dispatchManager.ActiveJob : null;
            if (job == null)
            {
                _activeNpc.text = "진행 중인 파견이 없습니다.";
                _activeRegion.text = string.Empty;
                _activeRequests.text = string.Empty;
                _activeRemaining.text = string.Empty;
                _activeProgress.lowValue = 0;
                _activeProgress.highValue = 1;
                _activeProgress.value = 0;
                return;
            }

            _activeNpc.text = $"{ResolveNpcName(job.NpcId)} 파견 중";
            _activeRegion.text = ResolveRegionName(job.RegionId);
            _activeRequests.text = BuildResolvedRequestText(job.Requests);
            int elapsed = gameTimeService != null
                ? Mathf.Clamp(gameTimeService.TotalElapsedTime - job.StartedAtTotalTime, 0, job.RequiredTime)
                : 0;
            int remaining = Mathf.Max(0, job.RequiredTime - elapsed);
            _activeProgress.lowValue = 0;
            _activeProgress.highValue = Mathf.Max(1, job.RequiredTime);
            _activeProgress.value = elapsed;
            _activeProgress.title = $"경과 {elapsed} / {job.RequiredTime}";
            _activeRemaining.text = $"남은 시간 {remaining}";
        }

        private void RefreshReportRows()
        {
            _reportRows.Clear();
            if (dispatchManager != null)
            {
                for (int i = 0; i < dispatchManager.ReturnedReports.Count; i++)
                {
                    DispatchJob report = dispatchManager.ReturnedReports[i];
                    if (report != null)
                        _reportRows.Add(new ReportRowModel(report));
                }
            }

            _reportList?.Rebuild();
            SetVisible(_reportEmptyLabel, _reportRows.Count == 0);
            SetVisible(_reportList, _reportRows.Count > 0);
        }

        private void BindNpcRow(VisualElement row, NpcRowModel model)
        {
            row.userData = model;
            row.EnableInClassList("is-selected", model.NpcId == _selectedNpcId);
            row.EnableInClassList("is-locked", model.Unlocked == false);
            row.Q<Label>("npc-name").text = model.DisplayName;
            row.Q<Label>("npc-affinity").text = model.RequiredAffinity == int.MaxValue
                ? $"친밀도 {model.Affinity}"
                : $"친밀도 {model.Affinity} / {model.RequiredAffinity}";
            row.Q<Label>("npc-initial").text = string.IsNullOrWhiteSpace(model.DisplayName)
                ? "?"
                : model.DisplayName.Substring(0, 1);
            row.Q<Label>("npc-state").text = model.Unlocked ? "가능" : "잠김";
        }

        private void BindRegionRow(VisualElement row, RegionRowModel model)
        {
            row.userData = model;
            row.EnableInClassList("is-selected", model.Region.RegionId == _selectedRegionId);
            row.Q<Label>("region-name").text = model.Region.DisplayName;
            row.Q<Label>("region-time").text = $"이동 {model.Region.BaseTravelTime}";
        }

        private void BindMaterialRow(VisualElement row, MaterialRowModel model)
        {
            row.userData = model;
            row.EnableInClassList("is-selected", model.Amount > 0);
            row.Q<Label>("material-name").text = model.Rule.Item.DisplayName;
            row.Q<Label>("quantity-label").text = model.Amount.ToString();
            row.Q<Image>("material-icon").sprite = model.Rule.Item.Icon;

            if (model.Amount > 0)
            {
                int minimum = Mathf.Max(1, Mathf.FloorToInt(model.Amount * model.Rule.MinYieldPercent / 100f));
                int maximum = Mathf.Max(minimum, Mathf.FloorToInt(model.Amount * model.Rule.MaxYieldPercent / 100f));
                row.Q<Label>("material-estimate").text = $"예상 {minimum}~{maximum} / 최대 {model.Rule.MaxRequestAmount}";
            }
            else
            {
                row.Q<Label>("material-estimate").text = $"최대 {model.Rule.MaxRequestAmount}개 요청";
            }
        }

        private void BindReportRow(VisualElement row, ReportRowModel model)
        {
            row.userData = model;
            row.Q<Label>("report-title").text = $"{ResolveNpcName(model.Job.NpcId)} 귀환";
            row.Q<Label>("report-region").text = ResolveRegionName(model.Job.RegionId);
            row.Q<Label>("report-rewards").text = BuildRewardText(model.Job.Rewards);
        }

        private void SelectNpc(NpcRowModel model)
        {
            if (model.Unlocked == false)
            {
                ShowToast(model.RequiredAffinity == int.MaxValue
                    ? "이 NPC의 파견 규칙이 아직 등록되지 않았습니다."
                    : $"친밀도 {model.RequiredAffinity}이 필요합니다.");
                return;
            }

            _selectedNpcId = model.NpcId;
            _selectedRegionId = null;
            _requestedAmounts.Clear();
            RefreshNpcRows();
            RefreshRegionRows();
            RefreshMaterialRows();
            RefreshSummary();
        }

        private void SelectRegion(RegionRowModel model)
        {
            _selectedRegionId = model.Region.RegionId;
            _requestedAmounts.Clear();
            RefreshRegionRows();
            RefreshMaterialRows();
            RefreshSummary();
        }

        private void ChangeMaterialAmount(VisualElement row, int delta)
        {
            if (row.userData is not MaterialRowModel model)
                return;

            int current = _requestedAmounts.TryGetValue(model.Rule.ItemId, out int amount) ? amount : 0;
            if (delta > 0 && current == 0)
            {
                int selectedCount = _requestedAmounts.Count(pair => pair.Value > 0);
                if (dispatchManager?.Catalog != null && selectedCount >= dispatchManager.Catalog.MaxMaterialTypes)
                {
                    ShowToast($"재료는 최대 {dispatchManager.Catalog.MaxMaterialTypes}종까지 선택할 수 있습니다.");
                    return;
                }
            }

            int next = Mathf.Clamp(current + delta, 0, model.Rule.MaxRequestAmount);
            if (next == 0)
                _requestedAmounts.Remove(model.Rule.ItemId);
            else
                _requestedAmounts[model.Rule.ItemId] = next;

            RefreshMaterialRows();
            RefreshSummary();
        }

        private void OpenConfirmation()
        {
            DispatchDraft draft = BuildDraft();
            DispatchNpcEligibility eligibility = npcQuery.GetEligibility(draft.NpcId);
            if (dispatchManager.TryBuildEstimate(draft, eligibility, out DispatchEstimate estimate, out DispatchValidationResult validation) == false)
            {
                ShowToast(validation.Message);
                return;
            }

            _pendingDraft = draft;
            _pendingEligibility = eligibility;
            _confirmationText.text =
                $"{ResolveNpcName(draft.NpcId)}에게 {ResolveRegionName(draft.RegionId)} 파견을 맡깁니다.\n\n" +
                $"{BuildRequestText(draft.Requests, true)}\n\n" +
                $"소요 시간 {estimate.RequiredTime}\n이 내용으로 파견을 보낼까요?";
            SetVisible(_confirmationModal, true);
        }

        private void ConfirmDispatch()
        {
            if (_pendingDraft == null)
                return;

            bool started = dispatchManager.TryStartDispatch(
                _pendingDraft,
                _pendingEligibility,
                out _,
                out DispatchValidationResult validation);

            SetVisible(_confirmationModal, false);
            _pendingDraft = null;
            if (started)
            {
                _requestedAmounts.Clear();
                RefreshAll();
                ShowPage(PageType.Active);
                ShowToast("파견을 보냈습니다.");
            }
            else
            {
                ShowToast(validation.Message);
            }
        }

        private void CancelConfirmation()
        {
            _pendingDraft = null;
            SetVisible(_confirmationModal, false);
        }

        private void ClaimReport(VisualElement row)
        {
            if (row.userData is not ReportRowModel model || dispatchManager == null)
                return;

            DispatchClaimResult result = dispatchManager.ClaimReport(model.Job.JobId);
            if (result.ReportFound == false)
                ShowToast("보고서를 찾을 수 없습니다.");
            else if (result.IsFullyClaimed)
                ShowToast($"물자 {result.AddedAmount}개를 수령했습니다.");
            else if (result.AddedAmount > 0)
                ShowToast($"{result.AddedAmount}개를 수령했습니다. 남은 물자 {result.RemainingAmount}개");
            else
                ShowToast("인벤토리 공간이 부족합니다.");

            RefreshReportRows();
        }

        private DispatchDraft BuildDraft()
        {
            DispatchDraft draft = new DispatchDraft
            {
                NpcId = _selectedNpcId,
                RegionId = _selectedRegionId
            };

            foreach (KeyValuePair<string, int> pair in _requestedAmounts)
            {
                if (pair.Value > 0)
                    draft.Requests.Add(new DispatchDraftRequest(pair.Key, pair.Value));
            }

            return draft;
        }

        private DispatchRegionSO FindSelectedRegion()
        {
            return dispatchManager?.Catalog != null
                   && dispatchManager.Catalog.TryFindRegion(_selectedRegionId, out DispatchRegionSO region)
                ? region
                : null;
        }

        private string ResolveNpcName(string npcId)
        {
            return npcQuery != null && npcQuery.TryGetNpc(npcId, out NpcData npc)
                ? npc.DisplayName
                : npcId;
        }

        private string ResolveRegionName(string regionId)
        {
            return dispatchManager?.Catalog != null
                   && dispatchManager.Catalog.TryFindRegion(regionId, out DispatchRegionSO region)
                ? region.DisplayName
                : regionId;
        }

        private string ResolveItemName(string itemId)
        {
            return dispatchManager?.Catalog?.ItemCatalog != null
                   && dispatchManager.Catalog.ItemCatalog.TryFindItem(itemId, out Work.Items.Code.ItemDataSO item)
                ? item.DisplayName
                : itemId;
        }

        private string BuildRequestText(IReadOnlyList<DispatchDraftRequest> requests, bool showEstimate)
        {
            if (requests == null || requests.Count == 0)
                return "재료를 선택해 주세요.";

            DispatchRegionSO region = FindSelectedRegion();
            List<string> lines = new List<string>(requests.Count);
            for (int i = 0; i < requests.Count; i++)
            {
                DispatchDraftRequest request = requests[i];
                string line = $"{ResolveItemName(request.ItemId)} ×{request.Amount}";
                if (showEstimate && region != null && region.TryFindMaterial(request.ItemId, out DispatchMaterialRule rule))
                {
                    int minimum = Mathf.Max(1, Mathf.FloorToInt(request.Amount * rule.MinYieldPercent / 100f));
                    int maximum = Mathf.Max(minimum, Mathf.FloorToInt(request.Amount * rule.MaxYieldPercent / 100f));
                    line += $"  (예상 {minimum}~{maximum})";
                }
                lines.Add(line);
            }
            return string.Join("\n", lines);
        }

        private string BuildResolvedRequestText(IReadOnlyList<DispatchResolvedRequest> requests)
        {
            if (requests == null || requests.Count == 0)
                return string.Empty;

            return string.Join("\n", requests.Select(request =>
                $"{ResolveItemName(request.ItemId)} ×{request.RequestedAmount}  " +
                $"(예상 {request.MinimumExpectedAmount}~{request.MaximumExpectedAmount})"));
        }

        private string BuildRewardText(IReadOnlyList<DispatchRewardData> rewards)
        {
            if (rewards == null || rewards.Count == 0)
                return "획득한 물자가 없습니다.";

            return string.Join("  ·  ", rewards
                .Where(reward => reward != null && reward.RemainingAmount > 0)
                .Select(reward =>
                    $"{(reward.IsRare ? "★ " : string.Empty)}{ResolveItemName(reward.ItemId)} ×{reward.RemainingAmount}"));
        }

        private void ShowRequestPage() => ShowPage(PageType.Request);
        private void ShowActivePage() => ShowPage(PageType.Active);
        private void ShowReportPage() => ShowPage(PageType.Report);

        private void ShowPage(PageType page)
        {
            SetVisible(_requestPage, page == PageType.Request);
            SetVisible(_activePage, page == PageType.Active);
            SetVisible(_reportPage, page == PageType.Report);
            _requestTabButton.EnableInClassList("is-selected", page == PageType.Request);
            _activeTabButton.EnableInClassList("is-selected", page == PageType.Active);
            _reportTabButton.EnableInClassList("is-selected", page == PageType.Report);

            if (page == PageType.Active) RefreshActivePage();
            if (page == PageType.Report) RefreshReportRows();
        }

        private void ShowToast(string message)
        {
            if (_toastLabel == null)
                return;

            _toastLabel.text = message ?? string.Empty;
            SetVisible(_toastLabel, string.IsNullOrWhiteSpace(message) == false);
            _toastLabel.schedule.Execute(() => SetVisible(_toastLabel, false)).StartingIn(2400);
        }

        private void ResolveRuntimeReferences()
        {
            if (dispatchManager == null) dispatchManager = FindFirstObjectByType<DispatchManager>();
            if (npcQuery == null) npcQuery = FindFirstObjectByType<DispatchNpcQuery>();
            if (gameTimeService == null) gameTimeService = FindFirstObjectByType<GameTimeService>();
        }

        private void Subscribe()
        {
            if (_subscribed)
                return;

            Bus<GameTimeAdvancedEvent>.Events += HandleTimeAdvanced;
            Bus<DispatchStartedEvent>.Events += HandleDispatchChanged;
            Bus<DispatchReturnedEvent>.Events += HandleDispatchReturned;
            Bus<DispatchReportsChangedEvent>.Events += HandleReportsChanged;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (_subscribed == false)
                return;

            Bus<GameTimeAdvancedEvent>.Events -= HandleTimeAdvanced;
            Bus<DispatchStartedEvent>.Events -= HandleDispatchChanged;
            Bus<DispatchReturnedEvent>.Events -= HandleDispatchReturned;
            Bus<DispatchReportsChangedEvent>.Events -= HandleReportsChanged;
            _subscribed = false;
        }

        private void HandleTimeAdvanced(GameTimeAdvancedEvent _) { if (IsVisible) { RefreshHeader(); RefreshActivePage(); } }
        private void HandleDispatchChanged(DispatchStartedEvent _) { if (IsVisible) RefreshAll(); }
        private void HandleDispatchReturned(DispatchReturnedEvent _) { if (IsVisible) { RefreshAll(); ShowToast("파견을 나간 NPC가 귀환했습니다."); } }
        private void HandleReportsChanged(DispatchReportsChangedEvent _) { if (IsVisible) RefreshReportRows(); }

        private static void SetVisible(VisualElement element, bool visible)
        {
            if (element != null)
                element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private enum PageType
        {
            Request,
            Active,
            Report
        }

        private sealed class NpcRowModel
        {
            public string NpcId { get; }
            public string DisplayName { get; }
            public int Affinity { get; }
            public int RequiredAffinity { get; }
            public bool Unlocked { get; }

            public NpcRowModel(string npcId, string displayName, int affinity, int requiredAffinity, bool unlocked)
            {
                NpcId = npcId;
                DisplayName = displayName;
                Affinity = affinity;
                RequiredAffinity = requiredAffinity;
                Unlocked = unlocked;
            }
        }

        private sealed class RegionRowModel
        {
            public DispatchRegionSO Region { get; }
            public RegionRowModel(DispatchRegionSO region) => Region = region;
        }

        private sealed class MaterialRowModel
        {
            public DispatchMaterialRule Rule { get; }
            public int Amount { get; }
            public MaterialRowModel(DispatchMaterialRule rule, int amount) { Rule = rule; Amount = amount; }
        }

        private sealed class ReportRowModel
        {
            public DispatchJob Job { get; }
            public ReportRowModel(DispatchJob job) => Job = job;
        }
    }
}
