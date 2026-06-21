using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using Work.Cook.Code.Data;
using Work.NPC.Code.Runtime;

namespace Work.NPC.Code.Editor
{
    public sealed class NpcCsvEditorWindow : EditorWindow
    {
        private const string NpcCsvPath = "Assets/Resources/NPCData/NPCs.csv";
        private const string DialogueCsvPath = "Assets/Resources/NPCData/DialogueLines.csv";
        private const string VisitEventCsvPath = "Assets/Resources/NPCData/VisitEvents.csv";
        private const string QuestionCategoryCsvPath = "Assets/Resources/NPCData/QuestionCategories.csv";
        private const string RegionPoolCsvPath = "Assets/Resources/NPCData/RegionPools.csv";
        private const string DialogueTextControlName = "NpcDialogueCsvEditor.Text";
        private const string BoldMarker = "**";

        private static readonly string[] NpcHeaders =
        {
            "NpcId",
            "DisplayName",
            "Race",
            "Role",
            "PreferredTags",
            "PreferredFoodTypes",
            "AvoidTags",
            "Notes",
            "RequestAvailable",
            "RequestUnlockLevel",
            "RequestUnlockEvent"
        };

        private static readonly string[] DialogueHeaders =
        {
            "EventId",
            "Group",
            "QuestionCategory",
            "LineOrder",
            "Speaker",
            "Text"
        };

        private static readonly string[] RegionPoolHeaders =
        {
            "RegionId",
            "NpcId",
            "Weight",
            "MinDay",
            "CooldownDays",
            "PoolType",
            "Condition"
        };

        private static readonly string[] DefaultVisitEventHeaders =
        {
            "EventId",
            "NpcId",
            "RegionId",
            "StartGroups",
            "QuestionLimit",
            "AvailableQuestionCategories",
            "EventType",
            "Priority",
            "RepeatMode",
            "CooldownDays",
            "RequiredNpcVisits",
            "RequiredAffinity",
            "RequiredCorrectCount",
            "RequiredLastResult",
            "RequiredEventIds",
            "SequenceGroup",
            "SequenceIndex",
            "CorrectRecipeId",
            "AllowedFoodTypes",
            "RequiredTags",
            "PreferredTags",
            "AvoidTags",
            "DisgustingTags",
            "RequiredRequestState",
            "BlockedAtRequestState",
            "RequestStateAfterEncounter",
            "RequestSuccessResults",
            "RequestStateAfterSuccessResult"
        };

        private static readonly Dictionary<string, string> DisplayLabels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "NpcId", "NPC ID" },
            { "DisplayName", "표시 이름" },
            { "Race", "종족" },
            { "Role", "역할" },
            { "PreferredTags", "선호 태그" },
            { "PreferredFoodTypes", "선호 음식 종류" },
            { "AvoidTags", "기피 태그" },
            { "Notes", "메모" },
            { "RequestAvailable", "요청 가능" },
            { "RequestUnlockLevel", "요청 해금 레벨" },
            { "RequestUnlockEvent", "요청 해금 이벤트" },
            { "EventId", "이벤트 ID" },
            { "Group", "대화 그룹" },
            { "QuestionCategory", "질문 카테고리" },
            { "LineOrder", "대사 순서" },
            { "Speaker", "화자" },
            { "Text", "대사" },
            { "RegionId", "지역 ID" },
            { "StartGroups", "시작 대화 그룹" },
            { "QuestionLimit", "질문 가능 횟수" },
            { "AvailableQuestionCategories", "사용 가능한 질문 카테고리" },
            { "EventType", "이벤트 타입" },
            { "Priority", "우선순위" },
            { "RepeatMode", "반복 방식" },
            { "CooldownDays", "재등장 대기일" },
            { "RequiredNpcVisits", "필요 방문 횟수" },
            { "RequiredAffinity", "필요 호감도" },
            { "RequiredCorrectCount", "필요 정답 횟수" },
            { "RequiredLastResult", "필요 이전 결과" },
            { "RequiredEventIds", "필요 선행 이벤트" },
            { "SequenceGroup", "연계 그룹" },
            { "SequenceIndex", "연계 순서" },
            { "CorrectRecipeId", "정답 레시피 ID" },
            { "AllowedFoodTypes", "허용 음식 종류" },
            { "RequiredTags", "필수 태그" },
            { "DisgustingTags", "실패 유발 태그" },
            { "RequiredRequestState", "필요 요청 상태" },
            { "BlockedAtRequestState", "차단 요청 상태" },
            { "RequestStateAfterEncounter", "만남 후 요청 상태" },
            { "RequestSuccessResults", "요청 성공 결과" },
            { "RequestStateAfterSuccessResult", "성공 후 요청 상태" },
            { "Weight", "등장 가중치" },
            { "MinDay", "최소 등장일" },
            { "PoolType", "풀 타입" },
            { "Condition", "조건" }
        };

        private static readonly DropdownOption[] EventTypeOptions =
        {
            new DropdownOption("Normal", "일반"),
            new DropdownOption("Special", "특수"),
            new DropdownOption("Sequence", "연계"),
            new DropdownOption("Request", "요청")
        };

        private static readonly DropdownOption[] RepeatModeOptions =
        {
            new DropdownOption("Once", "1회만"),
            new DropdownOption("Cycle", "순환"),
            new DropdownOption("Repeat", "반복")
        };

        private static readonly DropdownOption[] ConversationResultOptions =
        {
            new DropdownOption("Wrong", "실패"),
            new DropdownOption("Similar", "유사"),
            new DropdownOption("Correct", "성공"),
            new DropdownOption("Perfect", "완벽")
        };

        private static readonly DropdownOption[] RequestStateOptions =
        {
            new DropdownOption("Locked", "잠김"),
            new DropdownOption("Unlocked", "해금됨"),
            new DropdownOption("Offered", "제안됨"),
            new DropdownOption("Accepted", "수락됨"),
            new DropdownOption("InProgress", "진행 중"),
            new DropdownOption("ReadyToComplete", "완료 가능"),
            new DropdownOption("Completed", "완료됨"),
            new DropdownOption("EpilogueAvailable", "후일담 가능"),
            new DropdownOption("EpilogueCompleted", "후일담 완료")
        };

        private static readonly DropdownOption[] PoolTypeOptions =
        {
            new DropdownOption("Normal", "일반"),
            new DropdownOption("Traveler", "여행자")
        };

        private static readonly Dictionary<string, string> RegionDisplayNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "*", "모든 지역" },
            { "MossCave", "이끼 동굴" },
            { "Volcano", "화산 지대" }
        };

        private static readonly Dictionary<string, string> DialogueGroupDisplayNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Intro", "등장 대화" },
            { "OrderIntent", "주문 의도" },
            { "Question_Taste", "맛 질문" },
            { "Question_TextureTemp", "온도/식감 질문" },
            { "Question_Condition", "몸 상태 질문" },
            { "Question_Avoid", "기피 음식 질문" },
            { "Result_Wrong", "실패 결과" },
            { "Result_Similar", "유사 결과" },
            { "Result_Correct", "성공 결과" },
            { "Result_Perfect", "완벽 결과" }
        };

        private static readonly Dictionary<string, string> FoodCategoryDisplayNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Bread", "빵요리" },
            { "Drink", "음료" },
            { "Grill", "구이" },
            { "Noodle", "면요리" },
            { "RiceBowl", "덮밥" },
            { "Salad", "샐러드" },
            { "Snack", "간식" },
            { "Soup", "수프" },
            { "Stew", "스튜" },
            { "Tea", "차" }
        };

        private static readonly Dictionary<string, string> FoodTagDisplayNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "AncientRite", "의식의 맛" },
            { "BeerFriendly", "술안주감" },
            { "Bitter", "쓴맛" },
            { "Burnt", "탄맛" },
            { "Charred", "그을림" },
            { "Clean", "깔끔함" },
            { "Cold", "차가움" },
            { "Cool", "서늘함" },
            { "Crispy", "바삭함" },
            { "Dry", "마름" },
            { "Fish", "생선" },
            { "Fresh", "신선함" },
            { "Grain", "곡물" },
            { "Greasy", "기름짐" },
            { "Hearty", "든든함" },
            { "Heavy", "묵직함" },
            { "Herbal", "허브향" },
            { "Hot", "뜨거움" },
            { "Light", "가벼움" },
            { "Magic", "마력 안정" },
            { "Meat", "고기" },
            { "Messy", "지저분함" },
            { "Mineral", "광물향" },
            { "Moist", "촉촉함" },
            { "Poisonous", "독성" },
            { "Portable", "휴대성" },
            { "Rotten", "상한맛" },
            { "Salty", "짭짤함" },
            { "Savory", "감칠맛" },
            { "Smoky", "훈연향" },
            { "Soft", "부드러움" },
            { "Sour", "신맛" },
            { "Spicy", "매콤함" },
            { "StrongSmell", "강한 냄새" },
            { "Sweet", "달콤함" },
            { "SweetOnly", "단맛만 남음" },
            { "SweetSour", "새콤달콤" },
            { "TinyPortion", "작은 양" },
            { "Vegetable", "채소" },
            { "Warm", "따뜻함" },
            { "Watery", "묽음" }
        };

        private readonly List<NpcDraft> _npcs = new List<NpcDraft>();
        private readonly List<DialogueDraft> _dialogues = new List<DialogueDraft>();
        private readonly List<VisitEventReference> _visitEvents = new List<VisitEventReference>();
        private readonly List<RegionPoolDraft> _regionPools = new List<RegionPoolDraft>();
        private readonly List<NpcCsvValidationIssue> _validationIssues = new List<NpcCsvValidationIssue>();
        private readonly List<string> _visibleEventIds = new List<string>();
        private readonly List<DialogueDraft> _visibleDialogues = new List<DialogueDraft>();
        private readonly List<string> _visitEventHeaders = new List<string>();
        private readonly List<RecipeSO> _recipeAssets = new List<RecipeSO>();
        private readonly List<FoodCategorySO> _foodCategoryAssets = new List<FoodCategorySO>();
        private readonly List<FoodTagSO> _foodTagAssets = new List<FoodTagSO>();
        private readonly List<DropdownOption> _recipeOptions = new List<DropdownOption>();
        private readonly List<DropdownOption> _foodCategoryOptions = new List<DropdownOption>();
        private readonly List<DropdownOption> _foodTagOptions = new List<DropdownOption>();
        private readonly List<DropdownOption> _questionCategoryOptions = new List<DropdownOption>();
        private readonly List<DropdownOption> _regionOptions = new List<DropdownOption>();
        private readonly Dictionary<string, int> _npcMultiSelectAddIndices = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        private Vector2 _npcListScroll;
        private Vector2 _npcDetailScroll;
        private Vector2 _eventListScroll;
        private Vector2 _visitEventDetailScroll;
        private Vector2 _dialogueListScroll;
        private Vector2 _dialogueDetailScroll;
        private Vector2 _validationScroll;
        private string _npcSearch = string.Empty;
        private string _dialogueSearch = string.Empty;
        private string _newEventId = string.Empty;
        private NpcDraft _selectedNpc;
        private string _selectedEventId;
        private DialogueDraft _selectedDialogue;
        private DialogueDraft _textSelectionDialogue;
        private DialogueDraft _draggedDialogue;
        private int _textSelectionStart;
        private int _textSelectionEnd;
        private bool _isDraggingDialogue;
        private bool _showNpcDetail = true;
        private bool _showVisitEventDetail = true;
        private bool _showValidationPanel;
        private bool _hasValidationRun;
        private bool _validationIsStale;
        private bool _cookingAssetsLoaded;
        private bool _cookingAssetsReloadRequested = true;
        private float _npcPanelWidth = 240f;
        private float _eventPanelWidth = 320f;
        private float _dialoguePanelWidth = 340f;
        private string _activeResizeHandle;
        private bool _hasUnsavedChanges;
        private string _statusMessage = string.Empty;
        private DateTime _npcLastWriteTime;
        private DateTime _dialogueLastWriteTime;
        private DateTime _visitEventLastWriteTime;
        private DateTime _regionPoolLastWriteTime;
        private double _lastExternalChangeCheck;

        [MenuItem("Tools/Dungeon Dinner/NPC CSV Editor")]
        [MenuItem("Window/Dungeon Dinner/NPC CSV Editor")]
        public static void Open()
        {
            NpcCsvEditorWindow window = GetWindow<NpcCsvEditorWindow>("NPC CSV Editor");
            window.minSize = new Vector2(1180f, 680f);
            window.Show();
        }

        private void OnEnable()
        {
            LoadData(false);
            EditorApplication.update += CheckExternalChanges;
            EditorApplication.projectChanged += RequestCookingAssetReload;
        }

        private void OnDisable()
        {
            EditorApplication.update -= CheckExternalChanges;
            EditorApplication.projectChanged -= RequestCookingAssetReload;
        }

        private void OnGUI()
        {
            ReconcileSelectionState();
            DrawToolbar();
            DrawValidationPanel();
            EditorGUILayout.Space(6f);

            EditorGUILayout.BeginHorizontal();
            DrawNpcListPanel(_npcPanelWidth);
            DrawResizeHandle("npc-event", ref _npcPanelWidth, 190f, 420f);
            DrawEventListPanel(_eventPanelWidth);
            DrawResizeHandle("event-dialogue", ref _eventPanelWidth, 260f, 560f);
            DrawDialogueListPanel(_dialoguePanelWidth);
            DrawResizeHandle("dialogue-detail", ref _dialoguePanelWidth, 240f, 560f);
            DrawDialogueDetailPanel();
            EditorGUILayout.EndHorizontal();
            ClearDialogueDragStateOnMouseUp();
        }

        private void DrawResizeHandle(string handleId, ref float width, float minWidth, float maxWidth)
        {
            Rect rect = GUILayoutUtility.GetRect(6f, 6f, GUILayout.Width(6f), GUILayout.ExpandHeight(true));
            EditorGUIUtility.AddCursorRect(rect, MouseCursor.ResizeHorizontal);
            EditorGUI.DrawRect(new Rect(rect.x + 2f, rect.y + 2f, 1f, Mathf.Max(0f, rect.height - 4f)), new Color(0.28f, 0.28f, 0.28f, 0.9f));

            Event current = Event.current;
            if (current.type == EventType.MouseDown && rect.Contains(current.mousePosition))
            {
                _activeResizeHandle = handleId;
                current.Use();
            }

            if (_activeResizeHandle != handleId)
                return;

            if (current.type == EventType.MouseDrag)
            {
                width = Mathf.Clamp(width + current.delta.x, minWidth, maxWidth);
                Repaint();
                current.Use();
            }
            else if (current.rawType == EventType.MouseUp)
            {
                _activeResizeHandle = null;
                current.Use();
            }
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            GUILayout.Label("NPC / 대사 CSV 에디터", EditorStyles.boldLabel, GUILayout.Width(190f));
            GUILayout.Label($"NPC: {_npcs.Count}", GUILayout.Width(90f));
            GUILayout.Label($"대사: {_dialogues.Count}", GUILayout.Width(120f));

            GUI.enabled = _hasUnsavedChanges;
            if (GUILayout.Button("CSV 저장", GUILayout.Width(100f)))
                SaveData();
            GUI.enabled = true;

            if (GUILayout.Button("다시 불러오기", GUILayout.Width(100f)))
                TryReloadWithPrompt();

            if (GUILayout.Button("검증", GUILayout.Width(70f)))
                RunValidation();

            GUILayout.FlexibleSpace();

            GUIStyle statusStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleRight,
                wordWrap = false
            };
            GUILayout.Label(_hasUnsavedChanges ? "저장되지 않음" : "저장됨", statusStyle, GUILayout.Width(140f));
            EditorGUILayout.EndHorizontal();

            if (_hasValidationRun)
            {
                GUILayout.Label(GetValidationSummaryText(), EditorStyles.miniLabel);
            }

            if (string.IsNullOrWhiteSpace(_statusMessage) == false)
                EditorGUILayout.HelpBox(_statusMessage, MessageType.Info);

            EditorGUILayout.EndVertical();
        }

        private void DrawValidationPanel()
        {
            if (_hasValidationRun == false)
                return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            _showValidationPanel = EditorGUILayout.Foldout(_showValidationPanel, "검증 결과", true);
            GUILayout.Label(GetValidationSummaryText(), EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("지우기", GUILayout.Width(70f)))
            {
                _validationIssues.Clear();
                _hasValidationRun = false;
                _validationIsStale = false;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.EndHorizontal();

            if (_showValidationPanel)
            {
                if (_validationIssues.Count == 0)
                {
                    EditorGUILayout.HelpBox("검증 문제가 없습니다.", MessageType.Info);
                }
                else
                {
                    _validationScroll = EditorGUILayout.BeginScrollView(_validationScroll, GUILayout.MaxHeight(180f));
                    foreach (NpcCsvValidationIssue issue in _validationIssues)
                        DrawValidationIssue(issue);
                    EditorGUILayout.EndScrollView();
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawValidationIssue(NpcCsvValidationIssue issue)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(GetValidationSeverityLabel(issue.Severity), GUILayout.Width(58f));

            string label = issue.BuildDisplayText();
            if (GUILayout.Button(label, EditorStyles.miniButtonLeft))
                NavigateToValidationIssue(issue);

            EditorGUILayout.EndHorizontal();
        }

        private void RunValidation()
        {
            _validationIssues.Clear();
            _validationIssues.AddRange(ValidateNpcCsvData());
            _validationIssues.Sort(CompareValidationIssues);
            _hasValidationRun = true;
            _validationIsStale = false;
            _showValidationPanel = true;
            _validationScroll = Vector2.zero;

            int errors = CountValidationIssues(CsvValidationSeverity.Error);
            int warnings = CountValidationIssues(CsvValidationSeverity.Warning);
            int infos = CountValidationIssues(CsvValidationSeverity.Info);
            _statusMessage = $"검증 완료. 오류 {errors}개, 경고 {warnings}개, 정보 {infos}개.";
            Repaint();
        }

        private void ClearValidationResults()
        {
            _validationIssues.Clear();
            _hasValidationRun = false;
            _validationIsStale = false;
            _showValidationPanel = false;
            _validationScroll = Vector2.zero;
        }

        private List<NpcCsvValidationIssue> ValidateNpcCsvData()
        {
            List<NpcCsvValidationIssue> issues = new List<NpcCsvValidationIssue>();

            Dictionary<string, NpcDraft> npcById = BuildNpcLookup(issues);
            Dictionary<string, VisitEventReference> visitEventById = BuildVisitEventLookup(issues, npcById);
            Dictionary<string, string> questionGroupByCategory = BuildQuestionCategoryLookup(issues);

            ValidateNpcDrafts(issues, visitEventById);
            ValidateDialogueDrafts(issues, npcById, visitEventById);
            ValidateVisitEventReferences(issues, visitEventById, npcById, questionGroupByCategory);
            ValidateRegionPoolRows(issues, npcById, visitEventById);

            return issues;
        }

        private Dictionary<string, NpcDraft> BuildNpcLookup(List<NpcCsvValidationIssue> issues)
        {
            Dictionary<string, NpcDraft> lookup = new Dictionary<string, NpcDraft>(StringComparer.OrdinalIgnoreCase);
            foreach (NpcDraft npc in _npcs)
            {
                if (string.IsNullOrWhiteSpace(npc.NpcId))
                    continue;

                string npcId = npc.NpcId.Trim();
                if (lookup.ContainsKey(npcId))
                    continue;

                lookup[npcId] = npc;
            }

            foreach (IGrouping<string, NpcDraft> duplicateGroup in _npcs
                         .Where(npc => string.IsNullOrWhiteSpace(npc.NpcId) == false)
                         .GroupBy(npc => npc.NpcId.Trim(), StringComparer.OrdinalIgnoreCase)
                         .Where(group => group.Count() > 1))
            {
                AddValidationIssue(
                    issues,
                    CsvValidationSeverity.Error,
                    "DuplicateNpcId",
                    $"중복된 NPC ID입니다: '{duplicateGroup.Key}'.",
                    duplicateGroup.Key);
            }

            return lookup;
        }

        private Dictionary<string, VisitEventReference> BuildVisitEventLookup(
            List<NpcCsvValidationIssue> issues,
            IReadOnlyDictionary<string, NpcDraft> npcById)
        {
            Dictionary<string, VisitEventReference> lookup = new Dictionary<string, VisitEventReference>(StringComparer.OrdinalIgnoreCase);
            foreach (VisitEventReference visitEvent in _visitEvents)
            {
                if (string.IsNullOrWhiteSpace(visitEvent.EventId))
                    continue;

                string eventId = visitEvent.EventId.Trim();
                if (lookup.ContainsKey(eventId))
                    continue;

                lookup[eventId] = visitEvent;
            }

            foreach (IGrouping<string, VisitEventReference> duplicateGroup in _visitEvents
                         .Where(visitEvent => string.IsNullOrWhiteSpace(visitEvent.EventId) == false)
                         .GroupBy(visitEvent => visitEvent.EventId.Trim(), StringComparer.OrdinalIgnoreCase)
                         .Where(group => group.Count() > 1))
            {
                VisitEventReference first = duplicateGroup.First();
                AddValidationIssue(
                    issues,
                    CsvValidationSeverity.Error,
                    "DuplicateVisitEventId",
                    $"VisitEvents에 중복된 EventId가 있습니다: '{duplicateGroup.Key}'.",
                    first.NpcId,
                    duplicateGroup.Key);
            }

            foreach (VisitEventReference visitEvent in _visitEvents)
            {
                if (string.IsNullOrWhiteSpace(visitEvent.EventId))
                {
                    AddValidationIssue(
                        issues,
                        CsvValidationSeverity.Error,
                        "VisitEventIdEmpty",
                        "VisitEvents 행의 EventId가 비어 있습니다.",
                        visitEvent.NpcId);
                }

                if (string.IsNullOrWhiteSpace(visitEvent.NpcId))
                {
                    AddValidationIssue(
                        issues,
                        CsvValidationSeverity.Error,
                        "VisitEventNpcIdEmpty",
                        "VisitEvents 행의 NpcId가 비어 있습니다.",
                        eventId: visitEvent.EventId);
                    continue;
                }

                if (npcById.ContainsKey(visitEvent.NpcId))
                    continue;

                AddValidationIssue(
                    issues,
                    CsvValidationSeverity.Error,
                    "VisitEventNpcMissing",
                    $"방문 이벤트가 존재하지 않는 NPC를 참조합니다: '{visitEvent.NpcId}'.",
                    visitEvent.NpcId,
                    visitEvent.EventId);
            }

            return lookup;
        }

        private Dictionary<string, string> BuildQuestionCategoryLookup(List<NpcCsvValidationIssue> issues)
        {
            Dictionary<string, string> lookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (Dictionary<string, string> row in ReadCsv(QuestionCategoryCsvPath))
            {
                string categoryId = Get(row, "CategoryId");
                string dialogueGroup = Get(row, "DialogueGroup");
                if (string.IsNullOrWhiteSpace(categoryId))
                {
                    AddValidationIssue(
                        issues,
                        CsvValidationSeverity.Warning,
                        "QuestionCategoryIdEmpty",
                        "질문 카테고리 행의 CategoryId가 비어 있습니다.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(dialogueGroup))
                {
                    AddValidationIssue(
                        issues,
                        CsvValidationSeverity.Warning,
                        "QuestionDialogueGroupEmpty",
                        $"질문 카테고리 '{categoryId}'의 DialogueGroup이 비어 있습니다.");
                    continue;
                }

                lookup[categoryId.Trim()] = dialogueGroup.Trim();
            }

            return lookup;
        }

        private void ValidateNpcDrafts(
            List<NpcCsvValidationIssue> issues,
            IReadOnlyDictionary<string, VisitEventReference> visitEventById)
        {
            foreach (NpcDraft npc in _npcs)
            {
                if (string.IsNullOrWhiteSpace(npc.NpcId))
                {
                    AddValidationIssue(
                        issues,
                        CsvValidationSeverity.Error,
                        "NpcIdEmpty",
                        "NPC 행의 NpcId가 비어 있습니다.");
                    continue;
                }

                if (npc.RequestAvailable
                    && string.IsNullOrWhiteSpace(npc.RequestUnlockEvent) == false
                    && visitEventById.ContainsKey(npc.RequestUnlockEvent) == false)
                {
                    AddValidationIssue(
                        issues,
                        CsvValidationSeverity.Error,
                        "RequestUnlockEventMissing",
                        $"NPC 요청 해금 이벤트가 존재하지 않습니다: '{npc.RequestUnlockEvent}'.",
                        npc.NpcId,
                        npc.RequestUnlockEvent);
                }
            }
        }

        private void ValidateDialogueDrafts(
            List<NpcCsvValidationIssue> issues,
            IReadOnlyDictionary<string, NpcDraft> npcById,
            IReadOnlyDictionary<string, VisitEventReference> visitEventById)
        {
            foreach (DialogueDraft dialogue in _dialogues)
            {
                if (string.IsNullOrWhiteSpace(dialogue.EventId))
                {
                    AddDialogueValidationIssue(
                        issues,
                        CsvValidationSeverity.Error,
                        "DialogueEventIdEmpty",
                        "대사 줄의 EventId가 비어 있습니다.",
                        dialogue);
                }

                if (string.IsNullOrWhiteSpace(dialogue.Group))
                {
                    AddDialogueValidationIssue(
                        issues,
                        CsvValidationSeverity.Error,
                        "DialogueGroupEmpty",
                        "대사 줄의 Group이 비어 있습니다.",
                        dialogue);
                }

                if (dialogue.LineOrder <= 0)
                {
                    AddDialogueValidationIssue(
                        issues,
                        CsvValidationSeverity.Error,
                        "DialogueLineOrderInvalid",
                        "대사 순서는 0보다 커야 합니다.",
                        dialogue);
                }

                if (string.IsNullOrWhiteSpace(dialogue.Speaker))
                {
                    AddDialogueValidationIssue(
                        issues,
                        CsvValidationSeverity.Error,
                        "DialogueSpeakerEmpty",
                        "대사 줄의 Speaker가 비어 있습니다.",
                        dialogue);
                }
                else if (IsPlayerSpeaker(dialogue.Speaker) == false && npcById.ContainsKey(dialogue.Speaker) == false)
                {
                    AddDialogueValidationIssue(
                        issues,
                        CsvValidationSeverity.Error,
                        "DialogueSpeakerMissing",
                        $"대사 줄이 존재하지 않는 화자를 참조합니다: '{dialogue.Speaker}'.",
                        dialogue);
                }

                if (string.IsNullOrWhiteSpace(dialogue.Text))
                {
                    AddDialogueValidationIssue(
                        issues,
                        CsvValidationSeverity.Warning,
                        "DialogueTextEmpty",
                        "대사 줄의 Text가 비어 있습니다.",
                        dialogue);
                }

                if (CountOccurrences(dialogue.Text, BoldMarker) % 2 != 0)
                {
                    AddDialogueValidationIssue(
                        issues,
                        CsvValidationSeverity.Error,
                        "BoldMarkerUnbalanced",
                        "대사 Text에 짝이 맞지 않는 볼드 마커 '**'가 있습니다.",
                        dialogue);
                }
            }

            foreach (IGrouping<string, DialogueDraft> eventGroup in _dialogues
                         .Where(dialogue => string.IsNullOrWhiteSpace(dialogue.EventId) == false)
                         .GroupBy(dialogue => dialogue.EventId.Trim(), StringComparer.OrdinalIgnoreCase))
            {
                if (visitEventById.ContainsKey(eventGroup.Key) == false)
                {
                    DialogueDraft first = eventGroup.First();
                    AddDialogueValidationIssue(
                        issues,
                        CsvValidationSeverity.Warning,
                        "DialogueEventUnlinked",
                        $"대사 이벤트 '{eventGroup.Key}'가 VisitEvents.csv에 연결되어 있지 않습니다.",
                        first);
                }
            }

            foreach (IGrouping<string, DialogueDraft> group in _dialogues
                         .Where(dialogue => string.IsNullOrWhiteSpace(dialogue.EventId) == false
                                            && string.IsNullOrWhiteSpace(dialogue.Group) == false)
                         .GroupBy(dialogue => $"{dialogue.EventId.Trim()}|{dialogue.Group.Trim()}", StringComparer.OrdinalIgnoreCase))
            {
                ValidateDialogueLineOrders(issues, group.ToList());
            }
        }

        private void ValidateDialogueLineOrders(List<NpcCsvValidationIssue> issues, List<DialogueDraft> groupLines)
        {
            foreach (IGrouping<int, DialogueDraft> duplicateGroup in groupLines
                         .Where(dialogue => dialogue.LineOrder > 0)
                         .GroupBy(dialogue => dialogue.LineOrder)
                         .Where(group => group.Count() > 1))
            {
                DialogueDraft first = duplicateGroup.First();
                AddDialogueValidationIssue(
                    issues,
                    CsvValidationSeverity.Error,
                    "DialogueLineOrderDuplicate",
                    $"{first.EventId}/{first.Group} 안에 중복된 LineOrder {duplicateGroup.Key}가 있습니다.",
                    first);
            }

            List<int> orders = groupLines
                .Where(dialogue => dialogue.LineOrder > 0)
                .Select(dialogue => dialogue.LineOrder)
                .Distinct()
                .OrderBy(order => order)
                .ToList();

            for (int i = 0; i < orders.Count; i++)
            {
                int expected = i + 1;
                if (orders[i] == expected)
                    continue;

                DialogueDraft first = groupLines.First();
                AddDialogueValidationIssue(
                    issues,
                    CsvValidationSeverity.Warning,
                    "DialogueLineOrderGap",
                    $"{first.EventId}/{first.Group}의 LineOrder가 이어지지 않습니다. 예상 {expected}, 실제 {orders[i]}.",
                    first);
                break;
            }
        }

        private void ValidateVisitEventReferences(
            List<NpcCsvValidationIssue> issues,
            IReadOnlyDictionary<string, VisitEventReference> visitEventById,
            IReadOnlyDictionary<string, NpcDraft> npcById,
            IReadOnlyDictionary<string, string> questionGroupByCategory)
        {
            HashSet<string> dialogueEventIds = new HashSet<string>(
                _dialogues
                    .Where(dialogue => string.IsNullOrWhiteSpace(dialogue.EventId) == false)
                    .Select(dialogue => dialogue.EventId.Trim()),
                StringComparer.OrdinalIgnoreCase);

            foreach (VisitEventReference visitEvent in _visitEvents)
            {
                if (string.IsNullOrWhiteSpace(visitEvent.EventId))
                    continue;

                if (dialogueEventIds.Contains(visitEvent.EventId) == false)
                {
                    AddValidationIssue(
                        issues,
                        CsvValidationSeverity.Error,
                        "VisitEventDialogueMissing",
                        "방문 이벤트에 연결된 대사가 없습니다.",
                        visitEvent.NpcId,
                        visitEvent.EventId);
                    continue;
                }

                ValidateVisitEventRequiredEvents(issues, visitEvent, visitEventById);
                ValidateVisitEventDialogueGroups(issues, visitEvent, questionGroupByCategory);
                ValidateVisitEventRepeatRules(issues, visitEvent);
            }

            ValidateSequenceGroups(issues);
            ValidateRequestNpcCoverage(issues, npcById);
        }

        private void ValidateVisitEventRequiredEvents(
            List<NpcCsvValidationIssue> issues,
            VisitEventReference visitEvent,
            IReadOnlyDictionary<string, VisitEventReference> visitEventById)
        {
            foreach (string requiredEventId in SplitList(visitEvent.RequiredEventIds))
            {
                if (string.Equals(visitEvent.EventId, requiredEventId, StringComparison.OrdinalIgnoreCase))
                {
                    AddValidationIssue(
                        issues,
                        CsvValidationSeverity.Error,
                        "RequiredEventSelfReference",
                        "방문 이벤트가 자기 자신을 선행 이벤트로 요구하고 있습니다.",
                        visitEvent.NpcId,
                        visitEvent.EventId);
                    continue;
                }

                if (visitEventById.ContainsKey(requiredEventId))
                    continue;

                AddValidationIssue(
                    issues,
                    CsvValidationSeverity.Error,
                    "RequiredEventMissing",
                    $"필요 선행 이벤트가 존재하지 않습니다: '{requiredEventId}'.",
                    visitEvent.NpcId,
                    visitEvent.EventId);
            }
        }

        private void ValidateVisitEventDialogueGroups(
            List<NpcCsvValidationIssue> issues,
            VisitEventReference visitEvent,
            IReadOnlyDictionary<string, string> questionGroupByCategory)
        {
            foreach (string startGroup in SplitList(visitEvent.StartGroups))
            {
                if (HasDialogueGroup(visitEvent.EventId, startGroup))
                    continue;

                AddValidationIssue(
                    issues,
                    CsvValidationSeverity.Warning,
                    "StartDialogueGroupMissing",
                    $"시작 그룹 '{startGroup}'이 DialogueLines.csv에 없습니다.",
                    visitEvent.NpcId,
                    visitEvent.EventId,
                    startGroup);
            }

            List<string> availableQuestionCategories = SplitList(visitEvent.AvailableQuestionCategories);
            if (visitEvent.QuestionLimit > 0 && availableQuestionCategories.Count == 0)
            {
                AddValidationIssue(
                    issues,
                    CsvValidationSeverity.Warning,
                    "QuestionCategoriesEmpty",
                    "방문 이벤트에 QuestionLimit은 있지만 AvailableQuestionCategories가 비어 있습니다.",
                    visitEvent.NpcId,
                    visitEvent.EventId);
            }

            if (visitEvent.QuestionLimit > availableQuestionCategories.Count)
            {
                AddValidationIssue(
                    issues,
                    CsvValidationSeverity.Warning,
                    "QuestionLimitExceedsCategories",
                    $"QuestionLimit {visitEvent.QuestionLimit}이 사용 가능한 카테고리 수 {availableQuestionCategories.Count}보다 큽니다.",
                    visitEvent.NpcId,
                    visitEvent.EventId);
            }

            foreach (string categoryId in availableQuestionCategories)
            {
                if (questionGroupByCategory.TryGetValue(categoryId, out string dialogueGroup) == false)
                {
                    AddValidationIssue(
                        issues,
                        CsvValidationSeverity.Error,
                        "QuestionCategoryMissing",
                        $"질문 카테고리가 존재하지 않습니다: '{categoryId}'.",
                        visitEvent.NpcId,
                        visitEvent.EventId);
                    continue;
                }

                if (HasDialogueGroup(visitEvent.EventId, dialogueGroup))
                    continue;

                AddValidationIssue(
                    issues,
                    CsvValidationSeverity.Warning,
                    "QuestionDialogueGroupMissing",
                    $"카테고리 '{categoryId}'에 연결된 질문 그룹 '{dialogueGroup}'이 없습니다.",
                    visitEvent.NpcId,
                    visitEvent.EventId,
                    dialogueGroup);
            }

            if (RequiresCookingResultGroups(visitEvent))
                ValidateResultDialogueGroups(issues, visitEvent);
        }

        private void ValidateResultDialogueGroups(List<NpcCsvValidationIssue> issues, VisitEventReference visitEvent)
        {
            string[] requiredGroups =
            {
                "Result_Correct",
                "Result_Wrong"
            };

            foreach (string group in requiredGroups)
            {
                if (HasDialogueGroup(visitEvent.EventId, group))
                    continue;

                AddValidationIssue(
                    issues,
                    CsvValidationSeverity.Warning,
                    "ResultDialogueGroupMissing",
                    $"결과 그룹 '{group}'이 없습니다.",
                    visitEvent.NpcId,
                    visitEvent.EventId,
                    group);
            }
        }

        private void ValidateVisitEventRepeatRules(List<NpcCsvValidationIssue> issues, VisitEventReference visitEvent)
        {
            bool looksLikeFirstEvent = visitEvent.EventId.IndexOf("_First_", StringComparison.OrdinalIgnoreCase) >= 0;
            bool isSequenceEvent = IsSequenceVisitEvent(visitEvent);
            bool isRequestEvent = string.Equals(visitEvent.EventType, "Request", StringComparison.OrdinalIgnoreCase);

            if ((looksLikeFirstEvent || isSequenceEvent || isRequestEvent)
                && string.Equals(visitEvent.RepeatMode, "Once", StringComparison.OrdinalIgnoreCase) == false)
            {
                AddValidationIssue(
                    issues,
                    CsvValidationSeverity.Warning,
                    "RepeatModeShouldBeOnce",
                    "첫 방문, 연계, 요청 이벤트는 보통 RepeatMode를 Once로 설정하는 것이 좋습니다.",
                    visitEvent.NpcId,
                    visitEvent.EventId);
            }

            if (isSequenceEvent)
            {
                if (string.IsNullOrWhiteSpace(visitEvent.SequenceGroup))
                {
                    AddValidationIssue(
                        issues,
                        CsvValidationSeverity.Warning,
                        "SequenceGroupEmpty",
                        "연계 이벤트의 SequenceGroup이 비어 있습니다.",
                        visitEvent.NpcId,
                        visitEvent.EventId);
                }

                if (visitEvent.SequenceIndex <= 0)
                {
                    AddValidationIssue(
                        issues,
                        CsvValidationSeverity.Warning,
                        "SequenceIndexInvalid",
                        "연계 이벤트는 0보다 큰 SequenceIndex가 필요합니다.",
                        visitEvent.NpcId,
                        visitEvent.EventId);
                }
            }
        }

        private void ValidateSequenceGroups(List<NpcCsvValidationIssue> issues)
        {
            foreach (IGrouping<string, VisitEventReference> group in _visitEvents
                         .Where(IsSequenceVisitEvent)
                         .Where(visitEvent => string.IsNullOrWhiteSpace(visitEvent.SequenceGroup) == false)
                         .GroupBy(visitEvent => visitEvent.SequenceGroup.Trim(), StringComparer.OrdinalIgnoreCase))
            {
                foreach (IGrouping<int, VisitEventReference> duplicateIndex in group
                             .Where(visitEvent => visitEvent.SequenceIndex > 0)
                             .GroupBy(visitEvent => visitEvent.SequenceIndex)
                             .Where(indexGroup => indexGroup.Count() > 1))
                {
                    VisitEventReference first = duplicateIndex.First();
                    AddValidationIssue(
                        issues,
                        CsvValidationSeverity.Warning,
                        "SequenceDuplicateIndex",
                        $"연계 그룹 '{group.Key}' 안에 중복된 SequenceIndex {duplicateIndex.Key}가 있습니다.",
                        first.NpcId,
                        first.EventId);
                }
            }
        }

        private void ValidateRequestNpcCoverage(
            List<NpcCsvValidationIssue> issues,
            IReadOnlyDictionary<string, NpcDraft> npcById)
        {
            HashSet<string> npcIdsWithRequestEvent = new HashSet<string>(
                _visitEvents
                    .Where(visitEvent => string.Equals(visitEvent.EventType, "Request", StringComparison.OrdinalIgnoreCase))
                    .Select(visitEvent => visitEvent.NpcId),
                StringComparer.OrdinalIgnoreCase);

            foreach (NpcDraft npc in npcById.Values)
            {
                if (npc.RequestAvailable == false || npcIdsWithRequestEvent.Contains(npc.NpcId))
                    continue;

                AddValidationIssue(
                    issues,
                    CsvValidationSeverity.Warning,
                    "RequestEventMissing",
                    "NPC는 RequestAvailable 상태지만 Request 방문 이벤트가 없습니다.",
                    npc.NpcId);
            }
        }

        private void ValidateRegionPoolRows(
            List<NpcCsvValidationIssue> issues,
            IReadOnlyDictionary<string, NpcDraft> npcById,
            IReadOnlyDictionary<string, VisitEventReference> visitEventById)
        {
            foreach (RegionPoolDraft pool in _regionPools)
            {
                string regionId = pool.RegionId;
                string npcId = pool.NpcId;

                if (string.IsNullOrWhiteSpace(npcId))
                {
                    AddValidationIssue(
                        issues,
                        CsvValidationSeverity.Error,
                        "RegionPoolNpcIdEmpty",
                        "지역 풀 행의 NpcId가 비어 있습니다.");
                    continue;
                }

                if (npcById.ContainsKey(npcId) == false)
                {
                    AddValidationIssue(
                        issues,
                        CsvValidationSeverity.Error,
                        "RegionPoolNpcMissing",
                        $"지역 풀이 존재하지 않는 NPC를 참조합니다: '{npcId}'.",
                        npcId);
                }

                if (pool.Weight <= 0)
                {
                    AddValidationIssue(
                        issues,
                        CsvValidationSeverity.Warning,
                        "RegionPoolWeightInvalid",
                        "지역 풀 Weight는 0보다 커야 합니다.",
                        npcId);
                }

                bool hasMatchingEvent = visitEventById.Values.Any(visitEvent =>
                    string.Equals(visitEvent.NpcId, npcId, StringComparison.OrdinalIgnoreCase)
                    && RegionMatches(visitEvent.RegionId, regionId));

                if (hasMatchingEvent == false)
                {
                    AddValidationIssue(
                        issues,
                        CsvValidationSeverity.Warning,
                        "RegionPoolNpcHasNoEvents",
                        $"NPC '{npcId}'는 지역 '{regionId}'에 포함되어 있지만 일치하는 방문 이벤트가 없습니다.",
                        npcId);
                }
            }

            foreach (IGrouping<string, RegionPoolDraft> duplicateGroup in _regionPools
                         .Where(pool => string.IsNullOrWhiteSpace(pool.RegionId) == false
                                        && string.IsNullOrWhiteSpace(pool.NpcId) == false)
                         .GroupBy(pool => $"{pool.RegionId.Trim()}|{pool.NpcId.Trim()}", StringComparer.OrdinalIgnoreCase)
                         .Where(group => group.Count() > 1))
            {
                RegionPoolDraft first = duplicateGroup.First();
                AddValidationIssue(
                    issues,
                    CsvValidationSeverity.Warning,
                    "RegionPoolDuplicateNpc",
                    $"NPC '{first.NpcId}'가 지역 '{first.RegionId}'에 두 번 이상 등장합니다.",
                    first.NpcId);
            }
        }

        private void NavigateToValidationIssue(NpcCsvValidationIssue issue)
        {
            if (issue == null)
                return;

            NpcDraft npc = null;
            if (string.IsNullOrWhiteSpace(issue.NpcId) == false)
                npc = _npcs.FirstOrDefault(item => string.Equals(item.NpcId, issue.NpcId, StringComparison.OrdinalIgnoreCase));

            if (npc == null && string.IsNullOrWhiteSpace(issue.EventId) == false)
            {
                VisitEventReference visitEvent = _visitEvents.FirstOrDefault(item =>
                    string.Equals(item.EventId, issue.EventId, StringComparison.OrdinalIgnoreCase));
                if (visitEvent != null)
                    npc = _npcs.FirstOrDefault(item => string.Equals(item.NpcId, visitEvent.NpcId, StringComparison.OrdinalIgnoreCase));
            }

            if (npc == null && string.IsNullOrWhiteSpace(issue.Speaker) == false && IsPlayerSpeaker(issue.Speaker) == false)
                npc = _npcs.FirstOrDefault(item => string.Equals(item.NpcId, issue.Speaker, StringComparison.OrdinalIgnoreCase));

            if (npc != null)
                SelectNpc(npc);

            if (string.IsNullOrWhiteSpace(issue.EventId) == false)
                SelectEvent(issue.EventId);

            DialogueDraft dialogue = FindDialogueForIssue(issue);
            if (dialogue != null)
                SelectDialogue(dialogue);

            _statusMessage = $"검증 항목 선택됨: {issue.Code}";
            Repaint();
        }

        private DialogueDraft FindDialogueForIssue(NpcCsvValidationIssue issue)
        {
            if (issue == null || string.IsNullOrWhiteSpace(issue.EventId))
                return null;

            IEnumerable<DialogueDraft> candidates = _dialogues
                .Where(dialogue => string.Equals(dialogue.EventId, issue.EventId, StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrWhiteSpace(issue.Group) == false)
                candidates = candidates.Where(dialogue => string.Equals(dialogue.Group, issue.Group, StringComparison.OrdinalIgnoreCase));

            if (issue.LineOrder > 0)
                candidates = candidates.Where(dialogue => dialogue.LineOrder == issue.LineOrder);

            if (string.IsNullOrWhiteSpace(issue.Speaker) == false)
                candidates = candidates.Where(dialogue => string.Equals(dialogue.Speaker, issue.Speaker, StringComparison.OrdinalIgnoreCase));

            return candidates.OrderBy(dialogue => dialogue, DialogueComparer.Instance).FirstOrDefault();
        }

        private void AddDialogueValidationIssue(
            List<NpcCsvValidationIssue> issues,
            CsvValidationSeverity severity,
            string code,
            string message,
            DialogueDraft dialogue)
        {
            AddValidationIssue(
                issues,
                severity,
                code,
                message,
                GetNpcIdForDialogue(dialogue),
                dialogue?.EventId,
                dialogue?.Group,
                dialogue != null ? dialogue.LineOrder : -1,
                dialogue?.Speaker);
        }

        private void AddValidationIssue(
            List<NpcCsvValidationIssue> issues,
            CsvValidationSeverity severity,
            string code,
            string message,
            string npcId = "",
            string eventId = "",
            string group = "",
            int lineOrder = -1,
            string speaker = "")
        {
            issues.Add(new NpcCsvValidationIssue(severity, code, message, npcId, eventId, group, lineOrder, speaker));
        }

        private string GetNpcIdForDialogue(DialogueDraft dialogue)
        {
            if (dialogue == null)
                return string.Empty;

            VisitEventReference visitEvent = _visitEvents.FirstOrDefault(item =>
                string.Equals(item.EventId, dialogue.EventId, StringComparison.OrdinalIgnoreCase));
            if (visitEvent != null && string.IsNullOrWhiteSpace(visitEvent.NpcId) == false)
                return visitEvent.NpcId;

            return IsPlayerSpeaker(dialogue.Speaker) ? string.Empty : dialogue.Speaker;
        }

        private bool HasDialogueGroup(string eventId, string group)
        {
            return string.IsNullOrWhiteSpace(eventId) == false
                   && string.IsNullOrWhiteSpace(group) == false
                   && _dialogues.Any(dialogue =>
                       string.Equals(dialogue.EventId, eventId, StringComparison.OrdinalIgnoreCase)
                       && string.Equals(dialogue.Group, group, StringComparison.OrdinalIgnoreCase));
        }

        private bool RequiresCookingResultGroups(VisitEventReference visitEvent)
        {
            if (visitEvent == null)
                return false;

            if (string.Equals(visitEvent.EventType, "Complete", StringComparison.OrdinalIgnoreCase)
                || string.Equals(visitEvent.EventType, "Epilogue", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return string.IsNullOrWhiteSpace(visitEvent.CorrectRecipeId) == false
                   || string.IsNullOrWhiteSpace(visitEvent.AllowedFoodTypes) == false
                   || string.IsNullOrWhiteSpace(visitEvent.RequiredTags) == false
                   || string.IsNullOrWhiteSpace(visitEvent.PreferredTags) == false
                   || string.IsNullOrWhiteSpace(visitEvent.AvoidTags) == false
                   || string.IsNullOrWhiteSpace(visitEvent.DisgustingTags) == false;
        }

        private static bool IsSequenceVisitEvent(VisitEventReference visitEvent)
        {
            return visitEvent != null
                   && (string.Equals(visitEvent.EventType, "Sequence", StringComparison.OrdinalIgnoreCase)
                       || string.IsNullOrWhiteSpace(visitEvent.SequenceGroup) == false);
        }

        private static bool RegionMatches(string eventRegionId, string targetRegionId)
        {
            if (string.IsNullOrWhiteSpace(targetRegionId))
                return false;

            if (string.IsNullOrWhiteSpace(eventRegionId)
                || string.Equals(eventRegionId, "*", StringComparison.OrdinalIgnoreCase)
                || string.Equals(eventRegionId, "Any", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return SplitList(eventRegionId).Any(regionId => string.Equals(regionId, targetRegionId, StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsPlayerSpeaker(string speaker)
        {
            return string.Equals(speaker, "Player", StringComparison.OrdinalIgnoreCase);
        }

        private static List<string> SplitList(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return new List<string>();

            return value
                .Split('|')
                .Select(item => item.Trim())
                .Where(item => string.IsNullOrWhiteSpace(item) == false)
                .ToList();
        }

        private static int CountOccurrences(string value, string pattern)
        {
            if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(pattern))
                return 0;

            int count = 0;
            int index = 0;
            while (index < value.Length)
            {
                int next = value.IndexOf(pattern, index, StringComparison.Ordinal);
                if (next < 0)
                    break;

                count++;
                index = next + pattern.Length;
            }

            return count;
        }

        private int CountValidationIssues(CsvValidationSeverity severity)
        {
            return _validationIssues.Count(issue => issue.Severity == severity);
        }

        private string GetValidationSummaryText()
        {
            int errors = CountValidationIssues(CsvValidationSeverity.Error);
            int warnings = CountValidationIssues(CsvValidationSeverity.Warning);
            int infos = CountValidationIssues(CsvValidationSeverity.Info);
            string stale = _validationIsStale ? " (다시 검증 필요)" : string.Empty;
            return $"검증{stale}: 오류 {errors}개, 경고 {warnings}개, 정보 {infos}개";
        }

        private static string GetValidationSeverityLabel(CsvValidationSeverity severity)
        {
            switch (severity)
            {
                case CsvValidationSeverity.Error:
                    return "오류";
                case CsvValidationSeverity.Warning:
                    return "경고";
                default:
                    return "정보";
            }
        }

        private static int CompareValidationIssues(NpcCsvValidationIssue left, NpcCsvValidationIssue right)
        {
            int severityCompare = GetSeveritySortOrder(left.Severity).CompareTo(GetSeveritySortOrder(right.Severity));
            if (severityCompare != 0)
                return severityCompare;

            int npcCompare = string.Compare(left.NpcId, right.NpcId, StringComparison.OrdinalIgnoreCase);
            if (npcCompare != 0)
                return npcCompare;

            int eventCompare = string.Compare(left.EventId, right.EventId, StringComparison.OrdinalIgnoreCase);
            if (eventCompare != 0)
                return eventCompare;

            int groupCompare = string.Compare(left.Group, right.Group, StringComparison.OrdinalIgnoreCase);
            if (groupCompare != 0)
                return groupCompare;

            int lineCompare = left.LineOrder.CompareTo(right.LineOrder);
            return lineCompare != 0 ? lineCompare : string.Compare(left.Code, right.Code, StringComparison.OrdinalIgnoreCase);
        }

        private static int GetSeveritySortOrder(CsvValidationSeverity severity)
        {
            switch (severity)
            {
                case CsvValidationSeverity.Error:
                    return 0;
                case CsvValidationSeverity.Warning:
                    return 1;
                default:
                    return 2;
            }
        }

        private void DrawNpcListPanel(float width)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(width), GUILayout.ExpandHeight(true));
            GUILayout.Label("NPC 목록", EditorStyles.boldLabel);
            _npcSearch = EditorGUILayout.TextField("검색", _npcSearch);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("NPC 추가"))
                AddNpc();
            GUI.enabled = _selectedNpc != null;
            if (GUILayout.Button("삭제"))
                DeleteSelectedNpc();
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            _npcListScroll = EditorGUILayout.BeginScrollView(_npcListScroll);
            foreach (NpcDraft npc in GetVisibleNpcs())
            {
                bool selected = npc == _selectedNpc;
                string label = string.IsNullOrWhiteSpace(npc.DisplayName)
                    ? npc.NpcId
                    : $"{npc.DisplayName} ({npc.NpcId})";

                if (GUILayout.Toggle(selected, label, "Button") != selected)
                    SelectNpc(npc);
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(6f);
            _showNpcDetail = EditorGUILayout.Foldout(_showNpcDetail, "선택한 NPC 상세", true);
            if (_showNpcDetail)
                DrawNpcDetailFields();

            EditorGUILayout.EndVertical();
        }

        private void DrawNpcDetailFields()
        {
            if (_selectedNpc == null)
            {
                EditorGUILayout.HelpBox("NPC를 선택하거나 새 NPC를 추가하세요.", MessageType.Info);
                return;
            }

            _npcDetailScroll = EditorGUILayout.BeginScrollView(_npcDetailScroll, GUILayout.MinHeight(120f), GUILayout.MaxHeight(280f));
            DrawNpcIdField(_selectedNpc);
            DrawNpcTextField("DisplayName", ref _selectedNpc.DisplayName);
            DrawNpcTextField("Race", ref _selectedNpc.Race);
            DrawNpcTextField("Role", ref _selectedNpc.Role);
            DrawNpcMultiSelectField("PreferredTags", ref _selectedNpc.PreferredTags, GetFoodTagOptions());
            DrawNpcMultiSelectField("PreferredFoodTypes", ref _selectedNpc.PreferredFoodTypes, GetFoodCategoryOptions());
            DrawNpcMultiSelectField("AvoidTags", ref _selectedNpc.AvoidTags, GetFoodTagOptions());
            DrawNpcTextArea("Notes", ref _selectedNpc.Notes, 52f);

            EditorGUILayout.Space(6f);
            GUILayout.Label("요청", EditorStyles.boldLabel);
            DrawNpcBoolField("RequestAvailable", ref _selectedNpc.RequestAvailable);
            DrawNpcIntField("RequestUnlockLevel", ref _selectedNpc.RequestUnlockLevel);
            DrawNpcDropdownField("RequestUnlockEvent", ref _selectedNpc.RequestUnlockEvent, GetNpcEventOptions(_selectedNpc.NpcId, string.Empty), true);

            EditorGUILayout.Space(6f);
            DrawNpcRegionPoolFields(_selectedNpc);

            EditorGUILayout.Space(6f);
            DrawNpcReferenceSummary(_selectedNpc.NpcId);
            EditorGUILayout.EndScrollView();
        }

        private void DrawNpcDetailPanel()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(310f), GUILayout.ExpandHeight(true));
            GUILayout.Label("NPC 상세", EditorStyles.boldLabel);

            if (_selectedNpc == null)
            {
                EditorGUILayout.HelpBox("왼쪽에서 NPC를 선택하거나 새 NPC를 추가하세요.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            _npcDetailScroll = EditorGUILayout.BeginScrollView(_npcDetailScroll);
            DrawNpcIdField(_selectedNpc);
            DrawNpcTextField("DisplayName", ref _selectedNpc.DisplayName);
            DrawNpcTextField("Race", ref _selectedNpc.Race);
            DrawNpcTextField("Role", ref _selectedNpc.Role);
            DrawNpcMultiSelectField("PreferredTags", ref _selectedNpc.PreferredTags, GetFoodTagOptions());
            DrawNpcMultiSelectField("PreferredFoodTypes", ref _selectedNpc.PreferredFoodTypes, GetFoodCategoryOptions());
            DrawNpcMultiSelectField("AvoidTags", ref _selectedNpc.AvoidTags, GetFoodTagOptions());
            DrawNpcTextArea("Notes", ref _selectedNpc.Notes, 58f);

            EditorGUILayout.Space(8f);
            GUILayout.Label("요청", EditorStyles.boldLabel);
            DrawNpcBoolField("RequestAvailable", ref _selectedNpc.RequestAvailable);
            DrawNpcIntField("RequestUnlockLevel", ref _selectedNpc.RequestUnlockLevel);
            DrawNpcDropdownField("RequestUnlockEvent", ref _selectedNpc.RequestUnlockEvent, GetNpcEventOptions(_selectedNpc.NpcId, string.Empty), true);

            EditorGUILayout.Space(8f);
            DrawNpcRegionPoolFields(_selectedNpc);

            EditorGUILayout.Space(8f);
            DrawNpcReferenceSummary(_selectedNpc.NpcId);
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawEventListPanel(float width)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(width), GUILayout.ExpandHeight(true));
            GUILayout.Label("이벤트", EditorStyles.boldLabel);

            if (_selectedNpc == null)
            {
                EditorGUILayout.HelpBox("NPC를 먼저 선택하세요.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            DrawSelectedNpcSummary();
            BuildVisibleEventList();
            GUILayout.Label($"{_selectedNpc.NpcId} / 이벤트: {_visibleEventIds.Count}", EditorStyles.miniLabel);

            EditorGUILayout.BeginHorizontal();
            _newEventId = EditorGUILayout.TextField(_newEventId);
            if (GUILayout.Button("이벤트 추가", GUILayout.Width(92f)))
                AddEvent();
            GUI.enabled = string.IsNullOrWhiteSpace(_selectedEventId) == false;
            if (GUILayout.Button("삭제", GUILayout.Width(60f)))
                DeleteSelectedEvent();
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            _eventListScroll = EditorGUILayout.BeginScrollView(_eventListScroll);
            foreach (string eventId in _visibleEventIds)
            {
                bool selected = string.Equals(eventId, _selectedEventId, StringComparison.OrdinalIgnoreCase);
                int lineCount = _dialogues.Count(line => string.Equals(line.EventId, eventId, StringComparison.OrdinalIgnoreCase));
                int npcLineCount = _dialogues.Count(line => string.Equals(line.EventId, eventId, StringComparison.OrdinalIgnoreCase)
                                                            && string.Equals(line.Speaker, _selectedNpc.NpcId, StringComparison.OrdinalIgnoreCase));
                string label = $"{eventId}\n대사 {lineCount} / NPC {npcLineCount}";
                if (GUILayout.Toggle(selected, label, "Button") != selected)
                    SelectEvent(eventId);
            }
            EditorGUILayout.EndScrollView();

            if (_visibleEventIds.Count == 0)
                EditorGUILayout.HelpBox("이 NPC와 연결된 이벤트가 없습니다. 대사 추가로 새 이벤트 대사를 만들 수 있습니다.", MessageType.Info);

            EditorGUILayout.Space(6f);
            DrawVisitEventDetailFields();
            EditorGUILayout.EndVertical();
        }

        private void DrawVisitEventDetailFields()
        {
            _showVisitEventDetail = EditorGUILayout.Foldout(_showVisitEventDetail, "선택한 방문 이벤트", true);
            if (_showVisitEventDetail == false)
                return;

            VisitEventReference visitEvent = GetSelectedVisitEvent();
            if (visitEvent == null)
            {
                EditorGUILayout.HelpBox("VisitEvents.csv에 연결되지 않은 이벤트입니다. VisitEvent 연결 생성을 누르면 기본 이벤트 메타를 생성합니다.", MessageType.Warning);
                GUI.enabled = string.IsNullOrWhiteSpace(_selectedEventId) == false && _selectedNpc != null;
                if (GUILayout.Button("VisitEvent 연결 생성"))
                    CreateVisitEventForSelectedEvent();
                GUI.enabled = true;
                return;
            }

            bool previousWideMode = EditorGUIUtility.wideMode;
            float previousLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.wideMode = false;
            EditorGUIUtility.labelWidth = 116f;

            _visitEventDetailScroll = EditorGUILayout.BeginScrollView(_visitEventDetailScroll, false, false, GUILayout.MinHeight(140f), GUILayout.MaxHeight(320f));
            try
            {
                DrawVisitEventIdField(visitEvent);
                DrawVisitEventTextField("NpcId", visitEvent, "NpcId", ref visitEvent.NpcId);
                DrawVisitEventMultiSelectField("RegionId", visitEvent, "RegionId", ref visitEvent.RegionId, GetRegionOptions());
                DrawVisitEventMultiSelectField("StartGroups", visitEvent, "StartGroups", ref visitEvent.StartGroups, GetDialogueGroupOptions(visitEvent.EventId));
                DrawVisitEventIntField("QuestionLimit", visitEvent, "QuestionLimit", ref visitEvent.QuestionLimit);
                DrawVisitEventMultiSelectField("AvailableQuestionCategories", visitEvent, "AvailableQuestionCategories", ref visitEvent.AvailableQuestionCategories, GetQuestionCategoryOptions());
                DrawVisitEventDropdownField("EventType", visitEvent, "EventType", ref visitEvent.EventType, EventTypeOptions, false);
                DrawVisitEventIntField("Priority", visitEvent, "Priority", ref visitEvent.Priority);
                DrawVisitEventDropdownField("RepeatMode", visitEvent, "RepeatMode", ref visitEvent.RepeatMode, RepeatModeOptions, false);
                DrawVisitEventIntField("CooldownDays", visitEvent, "CooldownDays", ref visitEvent.CooldownDays);
                DrawVisitEventIntField("RequiredNpcVisits", visitEvent, "RequiredNpcVisits", ref visitEvent.RequiredNpcVisits);
                DrawVisitEventIntField("RequiredAffinity", visitEvent, "RequiredAffinity", ref visitEvent.RequiredAffinity);
                DrawVisitEventIntField("RequiredCorrectCount", visitEvent, "RequiredCorrectCount", ref visitEvent.RequiredCorrectCount);
                DrawVisitEventDropdownField("RequiredLastResult", visitEvent, "RequiredLastResult", ref visitEvent.RequiredLastResult, ConversationResultOptions, true);
                DrawVisitEventMultiSelectField("RequiredEventIds", visitEvent, "RequiredEventIds", ref visitEvent.RequiredEventIds, GetNpcEventOptions(visitEvent.NpcId, visitEvent.EventId));
                DrawVisitEventTextField("SequenceGroup", visitEvent, "SequenceGroup", ref visitEvent.SequenceGroup);
                DrawVisitEventIntField("SequenceIndex", visitEvent, "SequenceIndex", ref visitEvent.SequenceIndex);

                EditorGUILayout.Space(6f);
                GUILayout.Label("주문 조건", EditorStyles.boldLabel);
                DrawVisitEventOrderContractSoFields(visitEvent);

                EditorGUILayout.Space(6f);
                GUILayout.Label("요청 상태", EditorStyles.boldLabel);
                DrawVisitEventDropdownField("RequiredRequestState", visitEvent, "RequiredRequestState", ref visitEvent.RequiredRequestState, RequestStateOptions, true);
                DrawVisitEventDropdownField("BlockedAtRequestState", visitEvent, "BlockedAtRequestState", ref visitEvent.BlockedAtRequestState, RequestStateOptions, true);
                DrawVisitEventDropdownField("RequestStateAfterEncounter", visitEvent, "RequestStateAfterEncounter", ref visitEvent.RequestStateAfterEncounter, RequestStateOptions, true);
                DrawVisitEventMultiSelectField("RequestSuccessResults", visitEvent, "RequestSuccessResults", ref visitEvent.RequestSuccessResults, ConversationResultOptions);
                DrawVisitEventDropdownField("RequestStateAfterSuccessResult", visitEvent, "RequestStateAfterSuccessResult", ref visitEvent.RequestStateAfterSuccessResult, RequestStateOptions, true);
            }
            finally
            {
                EditorGUILayout.EndScrollView();
                EditorGUIUtility.wideMode = previousWideMode;
                EditorGUIUtility.labelWidth = previousLabelWidth;
            }
        }

        private void DrawDialogueListPanel(float width)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(width), GUILayout.ExpandHeight(true));
            GUILayout.Label("관련 대사", EditorStyles.boldLabel);
            _dialogueSearch = EditorGUILayout.TextField("검색", _dialogueSearch);

            EditorGUILayout.BeginHorizontal();
            GUI.enabled = _selectedNpc != null;
            if (GUILayout.Button("대사 추가"))
                AddDialogueLine();
            GUI.enabled = _selectedDialogue != null;
            if (GUILayout.Button("삭제"))
                DeleteSelectedDialogue();
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            if (_selectedNpc == null || string.IsNullOrWhiteSpace(_selectedEventId))
            {
                EditorGUILayout.HelpBox("NPC와 이벤트를 선택하면 해당 이벤트 대사만 표시됩니다.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            BuildVisibleDialogueList();
            GUILayout.Label($"{_selectedEventId} / 표시 중: {_visibleDialogues.Count}", EditorStyles.miniLabel);

            _dialogueListScroll = EditorGUILayout.BeginScrollView(_dialogueListScroll);
            foreach (DialogueDraft dialogue in _visibleDialogues)
            {
                DrawDialogueListItem(dialogue);
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawDialogueListItem(DialogueDraft dialogue)
        {
            bool selected = dialogue == _selectedDialogue;
            string label = $"{dialogue.LineOrder}. {dialogue.Speaker}: {StripBold(dialogue.Text)}";
            if (label.Length > 96)
                label = label.Substring(0, 93) + "...";

            Rect rect = GUILayoutUtility.GetRect(0f, 38f, GUILayout.ExpandWidth(true));
            Rect handleRect = new Rect(rect.x + 4f, rect.y + 4f, 18f, rect.height - 8f);
            Rect labelRect = new Rect(rect.x + 26f, rect.y, rect.width - 28f, rect.height);

            Event current = Event.current;
            bool hover = rect.Contains(current.mousePosition);
            Color background = selected
                ? new Color(0.25f, 0.45f, 0.75f, 0.55f)
                : hover
                    ? new Color(0.35f, 0.35f, 0.35f, 0.22f)
                    : new Color(0.18f, 0.18f, 0.18f, 0.12f);
            EditorGUI.DrawRect(rect, background);
            EditorGUIUtility.AddCursorRect(handleRect, MouseCursor.MoveArrow);
            GUI.Label(handleRect, "≡", EditorStyles.boldLabel);

            GUIStyle labelStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true
            };
            GUI.Label(labelRect, label, labelStyle);

            if (current.type == EventType.MouseDown && rect.Contains(current.mousePosition))
            {
                SelectDialogue(dialogue);
                _draggedDialogue = dialogue;
                _isDraggingDialogue = false;
                current.Use();
            }

            if (current.type == EventType.MouseDrag && _draggedDialogue != null)
            {
                _isDraggingDialogue = true;
                current.Use();
                Repaint();
            }

            if (_isDraggingDialogue && _draggedDialogue != null && rect.Contains(current.mousePosition))
            {
                Rect insertRect = current.mousePosition.y < rect.center.y
                    ? new Rect(rect.x, rect.y, rect.width, 2f)
                    : new Rect(rect.x, rect.yMax - 2f, rect.width, 2f);
                EditorGUI.DrawRect(insertRect, new Color(0.95f, 0.75f, 0.22f, 1f));
            }

            if (current.rawType != EventType.MouseUp || _draggedDialogue == null)
                return;

            if (_isDraggingDialogue && rect.Contains(current.mousePosition))
            {
                bool insertAfter = current.mousePosition.y >= rect.center.y;
                ReorderDialogueLine(_draggedDialogue, dialogue, insertAfter);
                _draggedDialogue = null;
                _isDraggingDialogue = false;
                current.Use();
            }
        }

        private void DrawDialogueDetailPanel()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            GUILayout.Label("대사 상세 / 볼드 편집", EditorStyles.boldLabel);

            if (_selectedDialogue == null)
            {
                EditorGUILayout.HelpBox("대사를 선택하거나 대사 추가로 새 대사를 추가하세요.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            _dialogueDetailScroll = EditorGUILayout.BeginScrollView(_dialogueDetailScroll);
            DrawDialogueTextField("EventId", ref _selectedDialogue.EventId);
            DrawDialogueDropdownField("Group", ref _selectedDialogue.Group, GetDialogueGroupOptions(_selectedDialogue.EventId), false);
            DrawDialogueDropdownField("QuestionCategory", ref _selectedDialogue.QuestionCategory, GetQuestionCategoryOptions(), true);
            DrawDialogueIntField("LineOrder", ref _selectedDialogue.LineOrder);
            DrawDialogueTextField("Speaker", ref _selectedDialogue.Speaker);

            EditorGUILayout.Space(8f);
            GUILayout.Label("대사", EditorStyles.boldLabel);
            string textControlName = GetDialogueTextControlName(_selectedDialogue);
            GUIStyle textAreaStyle = GetDialogueTextAreaStyle();
            Rect textAreaRect = GUILayoutUtility.GetRect(GUIContent.none, textAreaStyle, GUILayout.Height(120f), GUILayout.ExpandWidth(true));
            GUI.SetNextControlName(textControlName);
            EditorGUI.BeginChangeCheck();
            string text = EditorGUI.TextArea(textAreaRect, _selectedDialogue.Text, textAreaStyle);
            if (EditorGUI.EndChangeCheck())
            {
                _selectedDialogue.Text = text;
                ResetDialogueTextSelection();
                MarkDirty("대사 수정됨");
            }
            CaptureDialogueTextSelection(textControlName);

            DrawBoldTools();
            DrawDialoguePreview();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawBoldTools()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("선택 영역 볼드"))
                BoldSelectedText();

            if (GUILayout.Button("전체 볼드"))
                WrapWholeDialogueText();

            if (GUILayout.Button("볼드 제거"))
                RemoveBoldMarkers();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox("볼드 처리된 내용은 인게임 말풍선에서 강조되고, NPC 대사일 때 주문 명세서에 기록됩니다.", MessageType.None);
        }

        private void DrawDialoguePreview()
        {
            NpcDialogueMarkupResult result = NpcDialogueMarkupUtility.Parse(_selectedDialogue.Text);
            EditorGUILayout.Space(8f);
            GUILayout.Label("말풍선 미리보기", EditorStyles.boldLabel);

            GUIStyle previewStyle = new GUIStyle(EditorStyles.helpBox)
            {
                richText = true,
                wordWrap = true,
                padding = new RectOffset(10, 10, 8, 8)
            };
            GUILayout.Label(result.RichText, previewStyle, GUILayout.MinHeight(42f));

            GUILayout.Label("주문 명세서 기록 미리보기", EditorStyles.boldLabel);
            if (_selectedDialogue.Speaker.Equals("Player", StringComparison.OrdinalIgnoreCase))
            {
                EditorGUILayout.HelpBox("Player 대사는 볼드가 있어도 주문 명세서에 기록되지 않습니다.", MessageType.Info);
                return;
            }

            if (result.BoldSegments.Count == 0)
            {
                EditorGUILayout.HelpBox("볼드 구간이 없어서 기록될 주문 정보가 없습니다.", MessageType.Warning);
                return;
            }

            foreach (string segment in result.BoldSegments)
                GUILayout.Label($"- {segment}", EditorStyles.miniLabel);
        }

        private void DrawNpcReferenceSummary(string npcId)
        {
            if (string.IsNullOrWhiteSpace(npcId))
                return;

            int speakerLineCount = _dialogues.Count(line => string.Equals(line.Speaker, npcId, StringComparison.OrdinalIgnoreCase));
            int visitEventCount = _visitEvents.Count(visitEvent => string.Equals(visitEvent.NpcId, npcId, StringComparison.OrdinalIgnoreCase));
            EditorGUILayout.HelpBox(
                $"관련 대사 Speaker 수: {speakerLineCount}\n관련 VisitEvent 수: {visitEventCount}\n지금 버전은 NPC 삭제 시 VisitEvents.csv를 자동 수정하지 않고 경고만 표시합니다.",
                MessageType.Info);
        }

        private void DrawSelectedNpcSummary()
        {
            if (_selectedNpc == null)
                return;

            string displayName = string.IsNullOrWhiteSpace(_selectedNpc.DisplayName) ? _selectedNpc.NpcId : _selectedNpc.DisplayName;
            string summary =
                $"{displayName} ({_selectedNpc.NpcId})\n" +
                $"종족: {ValueOrDash(_selectedNpc.Race)} / 역할: {ValueOrDash(_selectedNpc.Role)}\n" +
                $"음식 종류: {ValueOrDash(_selectedNpc.PreferredFoodTypes)}\n" +
                $"태그: {ValueOrDash(_selectedNpc.PreferredTags)}";
            EditorGUILayout.HelpBox(summary, MessageType.None);
        }

        private void DrawNpcIdField(NpcDraft npc)
        {
            if (npc == null)
                return;

            string oldNpcId = npc.NpcId;
            EditorGUI.BeginChangeCheck();
            string next = EditorGUILayout.TextField(ToDisplayLabel("NpcId"), npc.NpcId);
            if (EditorGUI.EndChangeCheck())
                RenameNpcId(npc, oldNpcId, next);
        }

        private void DrawNpcTextField(string label, ref string value)
        {
            EditorGUI.BeginChangeCheck();
            string next = EditorGUILayout.TextField(ToDisplayLabel(label), value);
            if (EditorGUI.EndChangeCheck())
            {
                value = next;
                MarkDirty($"{ToDisplayLabel(label)} 수정됨");
            }
        }

        private void DrawNpcTextArea(string label, ref string value, float height)
        {
            GUILayout.Label(ToDisplayLabel(label));
            EditorGUI.BeginChangeCheck();
            string next = EditorGUILayout.TextArea(value, GUILayout.Height(height));
            if (EditorGUI.EndChangeCheck())
            {
                value = next;
                MarkDirty($"{ToDisplayLabel(label)} 수정됨");
            }
        }

        private static GUIStyle GetDialogueTextAreaStyle()
        {
            return new GUIStyle(EditorStyles.textArea)
            {
                wordWrap = true
            };
        }

        private void DrawNpcBoolField(string label, ref bool value)
        {
            EditorGUI.BeginChangeCheck();
            bool next = EditorGUILayout.Toggle(ToDisplayLabel(label), value);
            if (EditorGUI.EndChangeCheck())
            {
                value = next;
                MarkDirty($"{ToDisplayLabel(label)} 수정됨");
            }
        }

        private void DrawNpcIntField(string label, ref int value)
        {
            EditorGUI.BeginChangeCheck();
            int next = EditorGUILayout.IntField(ToDisplayLabel(label), value);
            if (EditorGUI.EndChangeCheck())
            {
                value = Mathf.Max(0, next);
                MarkDirty($"{ToDisplayLabel(label)} 수정됨");
            }
        }

        private void DrawNpcDropdownField(
            string label,
            ref string value,
            IReadOnlyList<DropdownOption> options,
            bool allowEmpty)
        {
            List<DropdownOption> displayOptions = BuildDropdownOptions(value, options, allowEmpty);
            string[] labels = displayOptions.Select(option => option.DisplayText).ToArray();
            int currentIndex = FindDropdownIndex(displayOptions, value);

            EditorGUI.BeginChangeCheck();
            int nextIndex = EditorGUILayout.Popup(ToDisplayLabel(label), currentIndex, labels);
            if (EditorGUI.EndChangeCheck() && nextIndex >= 0 && nextIndex < displayOptions.Count)
            {
                value = displayOptions[nextIndex].Value;
                MarkDirty($"{ToDisplayLabel(label)} 수정됨");
            }
        }

        private void DrawNpcMultiSelectField(string label, ref string value, IReadOnlyList<DropdownOption> options)
        {
            List<DropdownOption> displayOptions = BuildMultiSelectOptions(value, options);
            if (displayOptions.Count == 0)
            {
                DrawNpcTextField(label, ref value);
                return;
            }

            List<string> selectedIds = ParseIdList(value);
            bool changed = false;
            List<DropdownOption> addOptions = displayOptions
                .Where(option => ContainsId(selectedIds, option.Value) == false)
                .ToList();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label(ToDisplayLabel(label), EditorStyles.miniBoldLabel);

            GUI.enabled = addOptions.Count > 0;
            string addIndexKey = BuildNpcMultiSelectAddIndexKey(label);
            int addIndex = GetNpcMultiSelectAddIndex(addIndexKey, addOptions.Count);
            EditorGUI.BeginChangeCheck();
            addIndex = EditorGUILayout.Popup("추가할 항목", addIndex, BuildAddOptionLabels(addOptions));
            if (EditorGUI.EndChangeCheck())
                _npcMultiSelectAddIndices[addIndexKey] = addIndex;

            if (GUILayout.Button("선택 항목 추가", GUILayout.Height(22f)) && addOptions.Count > 0)
            {
                int safeIndex = Mathf.Clamp(addIndex, 0, addOptions.Count - 1);
                selectedIds.Add(addOptions[safeIndex].Value);
                _npcMultiSelectAddIndices[addIndexKey] = Mathf.Clamp(safeIndex, 0, Mathf.Max(0, addOptions.Count - 2));
                changed = true;
            }

            GUI.enabled = true;

            if (selectedIds.Count == 0)
            {
                GUILayout.Label("선택된 항목 없음", EditorStyles.miniLabel);
            }
            else
            {
                for (int i = selectedIds.Count - 1; i >= 0; i--)
                {
                    string selectedId = selectedIds[i];
                    DropdownOption option = FindOptionById(displayOptions, selectedId);
                    EditorGUI.BeginChangeCheck();
                    bool keep = EditorGUILayout.ToggleLeft(option.DisplayText, true);
                    if (EditorGUI.EndChangeCheck() && keep == false)
                    {
                        selectedIds.RemoveAt(i);
                        changed = true;
                    }
                }
            }

            EditorGUILayout.EndVertical();

            if (changed)
            {
                value = BuildIdList(selectedIds);
                MarkDirty($"{ToDisplayLabel(label)} 수정됨");
            }
        }

        private void DrawNpcRegionPoolFields(NpcDraft npc)
        {
            if (npc == null)
                return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("지역 풀", EditorStyles.boldLabel);
            GUILayout.Label("체크된 지역에서 이 NPC가 등장 후보에 포함됩니다.", EditorStyles.miniLabel);

            foreach (DropdownOption regionOption in GetEditableRegionOptions())
            {
                RegionPoolDraft pool = FindRegionPool(regionOption.Value, npc.NpcId);
                bool isIncluded = pool != null;

                EditorGUI.BeginChangeCheck();
                bool nextIncluded = EditorGUILayout.ToggleLeft(regionOption.DisplayText, isIncluded, EditorStyles.boldLabel);
                if (EditorGUI.EndChangeCheck())
                {
                    if (nextIncluded)
                        pool = AddRegionPool(regionOption.Value, npc.NpcId);
                    else
                        RemoveRegionPool(regionOption.Value, npc.NpcId);
                }

                if (nextIncluded == false)
                    continue;

                pool = pool ?? FindRegionPool(regionOption.Value, npc.NpcId);
                if (pool == null)
                    continue;

                EditorGUI.indentLevel++;
                DrawRegionPoolIntField("Weight", pool, ref pool.Weight, 1);
                DrawRegionPoolIntField("MinDay", pool, ref pool.MinDay, 1);
                DrawRegionPoolIntField("CooldownDays", pool, ref pool.CooldownDays, 0);
                DrawRegionPoolDropdownField("PoolType", pool, ref pool.PoolType, PoolTypeOptions);
                DrawRegionPoolTextField("Condition", pool, ref pool.Condition);
                EditorGUI.indentLevel--;
                EditorGUILayout.Space(4f);
            }

            EditorGUILayout.EndVertical();
        }

        private List<DropdownOption> GetEditableRegionOptions()
        {
            List<DropdownOption> options = RegionDisplayNames
                .Where(pair => string.Equals(pair.Key, "*", StringComparison.OrdinalIgnoreCase) == false)
                .Select(pair => new DropdownOption(pair.Key, pair.Value))
                .ToList();

            AddIdOptions(options, _regionPools.Select(pool => pool.RegionId), GetRegionDisplayName);
            AddIdOptions(
                options,
                _visitEvents.SelectMany(visitEvent => ParseIdList(visitEvent.RegionId))
                    .Where(regionId => string.Equals(regionId, "*", StringComparison.OrdinalIgnoreCase) == false),
                GetRegionDisplayName);

            return NormalizeDropdownOptions(options);
        }

        private RegionPoolDraft FindRegionPool(string regionId, string npcId)
        {
            return _regionPools.FirstOrDefault(pool =>
                string.Equals(pool.RegionId, regionId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(pool.NpcId, npcId, StringComparison.OrdinalIgnoreCase));
        }

        private RegionPoolDraft AddRegionPool(string regionId, string npcId)
        {
            RegionPoolDraft existing = FindRegionPool(regionId, npcId);
            if (existing != null)
                return existing;

            RegionPoolDraft pool = RegionPoolDraft.CreateDefault(regionId, npcId);
            _regionPools.Add(pool);
            RebuildRegionOptions();
            MarkDirty("지역 풀 추가됨");
            return pool;
        }

        private void RemoveRegionPool(string regionId, string npcId)
        {
            int removed = _regionPools.RemoveAll(pool =>
                string.Equals(pool.RegionId, regionId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(pool.NpcId, npcId, StringComparison.OrdinalIgnoreCase));
            if (removed <= 0)
                return;

            RebuildRegionOptions();
            MarkDirty("지역 풀 제거됨");
        }

        private void DrawRegionPoolIntField(string label, RegionPoolDraft pool, ref int value, int minValue)
        {
            EditorGUI.BeginChangeCheck();
            int next = EditorGUILayout.IntField(ToDisplayLabel(label), value);
            if (EditorGUI.EndChangeCheck())
            {
                value = Mathf.Max(minValue, next);
                pool.SyncRawValues();
                MarkDirty($"{ToDisplayLabel(label)} 수정됨");
            }
        }

        private void DrawRegionPoolTextField(string label, RegionPoolDraft pool, ref string value)
        {
            EditorGUI.BeginChangeCheck();
            string next = EditorGUILayout.TextField(ToDisplayLabel(label), value);
            if (EditorGUI.EndChangeCheck())
            {
                value = next;
                pool.SyncRawValues();
                MarkDirty($"{ToDisplayLabel(label)} 수정됨");
            }
        }

        private void DrawRegionPoolDropdownField(
            string label,
            RegionPoolDraft pool,
            ref string value,
            IReadOnlyList<DropdownOption> options)
        {
            List<DropdownOption> displayOptions = BuildDropdownOptions(value, options, false);
            string[] labels = displayOptions.Select(option => option.DisplayText).ToArray();
            int currentIndex = FindDropdownIndex(displayOptions, value);

            EditorGUI.BeginChangeCheck();
            int nextIndex = EditorGUILayout.Popup(ToDisplayLabel(label), currentIndex, labels);
            if (EditorGUI.EndChangeCheck() && nextIndex >= 0 && nextIndex < displayOptions.Count)
            {
                value = displayOptions[nextIndex].Value;
                pool.SyncRawValues();
                MarkDirty($"{ToDisplayLabel(label)} 수정됨");
            }
        }

        private void DrawDialogueTextField(string label, ref string value)
        {
            EditorGUI.BeginChangeCheck();
            string next = EditorGUILayout.TextField(ToDisplayLabel(label), value);
            if (EditorGUI.EndChangeCheck())
            {
                value = next;
                MarkDirty($"{label} 수정됨");
            }
        }

        private void DrawDialogueDropdownField(
            string label,
            ref string value,
            IReadOnlyList<DropdownOption> options,
            bool allowEmpty)
        {
            List<DropdownOption> displayOptions = BuildDropdownOptions(value, options, allowEmpty);
            string[] labels = displayOptions.Select(option => option.DisplayText).ToArray();
            int currentIndex = FindDropdownIndex(displayOptions, value);

            EditorGUI.BeginChangeCheck();
            int nextIndex = EditorGUILayout.Popup(ToDisplayLabel(label), currentIndex, labels);
            if (EditorGUI.EndChangeCheck() && nextIndex >= 0 && nextIndex < displayOptions.Count)
            {
                value = displayOptions[nextIndex].Value;
                MarkDirty($"{ToDisplayLabel(label)} 수정됨");
            }
        }

        private void DrawDialogueIntField(string label, ref int value)
        {
            EditorGUI.BeginChangeCheck();
            int next = EditorGUILayout.IntField(ToDisplayLabel(label), value);
            if (EditorGUI.EndChangeCheck())
            {
                value = Mathf.Max(0, next);
                MarkDirty($"{label} 수정됨");
            }
        }

        private void DrawVisitEventIdField(VisitEventReference visitEvent)
        {
            string oldEventId = visitEvent.EventId;
            EditorGUI.BeginChangeCheck();
            string next = EditorGUILayout.DelayedTextField(ToDisplayLabel("EventId"), visitEvent.EventId);
            if (EditorGUI.EndChangeCheck() && string.Equals(oldEventId, next, StringComparison.Ordinal) == false)
                RenameVisitEventId(visitEvent, oldEventId, next);
        }

        private void DrawVisitEventOrderContractSoFields(VisitEventReference visitEvent)
        {
            EnsureCookingAssetsLoaded();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("SO 선택", EditorStyles.boldLabel);
            if (GUILayout.Button("새로고침", GUILayout.Width(80f)))
                ReloadCookingAssets();
            EditorGUILayout.EndHorizontal();

            if (_recipeAssets.Count == 0 && _foodCategoryAssets.Count == 0 && _foodTagAssets.Count == 0)
            {
                EditorGUILayout.HelpBox("요리 ScriptableObject를 찾지 못했습니다. 먼저 RecipeSO, FoodCategorySO, FoodTagSO 에셋을 만들어주세요.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            DrawVisitEventDropdownField("CorrectRecipeId", visitEvent, "CorrectRecipeId", ref visitEvent.CorrectRecipeId, GetRecipeOptions(), true);
            DrawVisitEventMultiSelectField("AllowedFoodTypes", visitEvent, "AllowedFoodTypes", ref visitEvent.AllowedFoodTypes, GetFoodCategoryOptions());
            DrawVisitEventMultiSelectField("RequiredTags", visitEvent, "RequiredTags", ref visitEvent.RequiredTags, GetFoodTagOptions());
            DrawVisitEventMultiSelectField("PreferredTags", visitEvent, "PreferredTags", ref visitEvent.PreferredTags, GetFoodTagOptions());
            DrawVisitEventMultiSelectField("AvoidTags", visitEvent, "AvoidTags", ref visitEvent.AvoidTags, GetFoodTagOptions());
            DrawVisitEventMultiSelectField("DisgustingTags", visitEvent, "DisgustingTags", ref visitEvent.DisgustingTags, GetFoodTagOptions());
            EditorGUILayout.EndVertical();
        }

        private void DrawRecipeObjectField(VisitEventReference visitEvent)
        {
            RecipeSO current = FindRecipeById(visitEvent.CorrectRecipeId);
            EditorGUI.BeginChangeCheck();
            RecipeSO next = (RecipeSO)EditorGUILayout.ObjectField("정답 레시피", current, typeof(RecipeSO), false);
            if (EditorGUI.EndChangeCheck())
                SetVisitEventRawValue(visitEvent, "CorrectRecipeId", ref visitEvent.CorrectRecipeId, next != null ? next.RecipeId : string.Empty);

            if (string.IsNullOrWhiteSpace(visitEvent.CorrectRecipeId) == false && current == null)
                EditorGUILayout.HelpBox($"해당 ID의 RecipeSO를 찾지 못했습니다: {visitEvent.CorrectRecipeId}", MessageType.Warning);
        }

        private void DrawCategoryObjectListField(VisitEventReference visitEvent, string label, string columnName, ref string value)
        {
            List<string> ids = ParseIdList(value);
            bool changed = false;

            GUILayout.Label(label, EditorStyles.miniBoldLabel);
            for (int i = 0; i < ids.Count; i++)
            {
                FoodCategorySO current = FindFoodCategoryById(ids[i]);
                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginChangeCheck();
                FoodCategorySO next = (FoodCategorySO)EditorGUILayout.ObjectField(current, typeof(FoodCategorySO), false);
                if (EditorGUI.EndChangeCheck())
                {
                    ids[i] = next != null ? next.CategoryId : string.Empty;
                    changed = true;
                }

                if (GUILayout.Button("삭제", GUILayout.Width(70f)))
                {
                    ids.RemoveAt(i);
                    changed = true;
                    i--;
                }

                EditorGUILayout.EndHorizontal();

                if (current == null && i >= 0 && i < ids.Count && string.IsNullOrWhiteSpace(ids[i]) == false)
                    EditorGUILayout.HelpBox($"해당 ID의 FoodCategorySO를 찾지 못했습니다: {ids[i]}", MessageType.Warning);
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(18f);
            EditorGUI.BeginChangeCheck();
            FoodCategorySO added = (FoodCategorySO)EditorGUILayout.ObjectField("추가", null, typeof(FoodCategorySO), false);
            if (EditorGUI.EndChangeCheck() && added != null && ContainsId(ids, added.CategoryId) == false)
            {
                ids.Add(added.CategoryId);
                changed = true;
            }

            EditorGUILayout.EndHorizontal();

            if (changed)
                SetVisitEventRawValue(visitEvent, columnName, ref value, BuildIdList(ids));
        }

        private void DrawTagObjectListField(VisitEventReference visitEvent, string label, string columnName, ref string value)
        {
            List<string> ids = ParseIdList(value);
            bool changed = false;

            GUILayout.Label(label, EditorStyles.miniBoldLabel);
            for (int i = 0; i < ids.Count; i++)
            {
                FoodTagSO current = FindFoodTagById(ids[i]);
                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginChangeCheck();
                FoodTagSO next = (FoodTagSO)EditorGUILayout.ObjectField(current, typeof(FoodTagSO), false);
                if (EditorGUI.EndChangeCheck())
                {
                    ids[i] = next != null ? next.TagId : string.Empty;
                    changed = true;
                }

                if (GUILayout.Button("삭제", GUILayout.Width(70f)))
                {
                    ids.RemoveAt(i);
                    changed = true;
                    i--;
                }

                EditorGUILayout.EndHorizontal();

                if (current == null && i >= 0 && i < ids.Count && string.IsNullOrWhiteSpace(ids[i]) == false)
                    EditorGUILayout.HelpBox($"해당 ID의 FoodTagSO를 찾지 못했습니다: {ids[i]}", MessageType.Warning);
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(18f);
            EditorGUI.BeginChangeCheck();
            FoodTagSO added = (FoodTagSO)EditorGUILayout.ObjectField("추가", null, typeof(FoodTagSO), false);
            if (EditorGUI.EndChangeCheck() && added != null && ContainsId(ids, added.TagId) == false)
            {
                ids.Add(added.TagId);
                changed = true;
            }

            EditorGUILayout.EndHorizontal();

            if (changed)
                SetVisitEventRawValue(visitEvent, columnName, ref value, BuildIdList(ids));
        }

        private void SetVisitEventRawValue(VisitEventReference visitEvent, string columnName, ref string value, string next)
        {
            value = next ?? string.Empty;
            visitEvent.SetRaw(columnName, value);
            if (string.Equals(columnName, "RegionId", StringComparison.OrdinalIgnoreCase))
                SyncRegionPoolsForNpcEvents(visitEvent.NpcId);
            MarkDirty($"방문 이벤트 {ToDisplayLabel(columnName)} 수정됨");
        }

        private bool EnsureRegionPoolsForVisitEvent(VisitEventReference visitEvent)
        {
            if (visitEvent == null || string.IsNullOrWhiteSpace(visitEvent.NpcId))
                return false;

            bool changed = false;
            foreach (string regionId in GetConcreteRegionIds(visitEvent.RegionId))
            {
                if (FindRegionPool(regionId, visitEvent.NpcId) != null)
                    continue;

                _regionPools.Add(RegionPoolDraft.CreateDefault(regionId, visitEvent.NpcId));
                changed = true;
            }

            if (changed)
                RebuildRegionOptions();

            return changed;
        }

        private bool SyncRegionPoolsForNpcEvents(string npcId)
        {
            if (string.IsNullOrWhiteSpace(npcId))
                return false;

            HashSet<string> requiredRegions = GetNpcConcreteEventRegionIds(npcId);
            int beforeCount = _regionPools.Count;
            _regionPools.RemoveAll(pool =>
                string.Equals(pool.NpcId, npcId, StringComparison.OrdinalIgnoreCase)
                && requiredRegions.Contains(pool.RegionId) == false);

            bool changed = _regionPools.Count != beforeCount;
            foreach (string regionId in requiredRegions)
            {
                if (FindRegionPool(regionId, npcId) != null)
                    continue;

                _regionPools.Add(RegionPoolDraft.CreateDefault(regionId, npcId));
                changed = true;
            }

            if (changed)
                RebuildRegionOptions();

            return changed;
        }

        private HashSet<string> GetNpcConcreteEventRegionIds(string npcId)
        {
            HashSet<string> regionIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (VisitEventReference visitEvent in _visitEvents)
            {
                if (string.Equals(visitEvent.NpcId, npcId, StringComparison.OrdinalIgnoreCase) == false)
                    continue;

                foreach (string regionId in GetConcreteRegionIds(visitEvent.RegionId))
                    regionIds.Add(regionId);
            }

            return regionIds;
        }

        private static List<string> GetConcreteRegionIds(string regionText)
        {
            List<string> regionIds = ParseIdList(regionText)
                .Where(regionId => string.Equals(regionId, "*", StringComparison.OrdinalIgnoreCase) == false
                                   && string.Equals(regionId, "Any", StringComparison.OrdinalIgnoreCase) == false)
                .ToList();

            if (regionIds.Count > 0)
                return regionIds;

            if (string.IsNullOrWhiteSpace(regionText)
                || string.Equals(regionText.Trim(), "*", StringComparison.OrdinalIgnoreCase)
                || string.Equals(regionText.Trim(), "Any", StringComparison.OrdinalIgnoreCase))
            {
                return RegionDisplayNames.Keys
                    .Where(regionId => string.Equals(regionId, "*", StringComparison.OrdinalIgnoreCase) == false)
                    .ToList();
            }

            return regionIds;
        }

        private void EnsureCookingAssetsLoaded()
        {
            if (_cookingAssetsLoaded && _cookingAssetsReloadRequested == false)
                return;

            ReloadCookingAssets();
        }

        private void ReloadCookingAssets()
        {
            LoadAssets(_recipeAssets);
            LoadAssets(_foodCategoryAssets);
            LoadAssets(_foodTagAssets);
            _recipeAssets.Sort((left, right) => string.Compare(GetRecipeSortKey(left), GetRecipeSortKey(right), StringComparison.OrdinalIgnoreCase));
            _foodCategoryAssets.Sort((left, right) => string.Compare(GetCategorySortKey(left), GetCategorySortKey(right), StringComparison.OrdinalIgnoreCase));
            _foodTagAssets.Sort((left, right) => string.Compare(GetTagSortKey(left), GetTagSortKey(right), StringComparison.OrdinalIgnoreCase));
            RebuildCookingDropdownOptions();
            _cookingAssetsLoaded = true;
            _cookingAssetsReloadRequested = false;
        }

        private void RequestCookingAssetReload()
        {
            _cookingAssetsReloadRequested = true;
            Repaint();
        }

        private static void LoadAssets<T>(List<T> target)
            where T : UnityEngine.Object
        {
            target.Clear();
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null)
                    target.Add(asset);
            }
        }

        private RecipeSO FindRecipeById(string id)
        {
            string normalized = NormalizeId(id);
            return _recipeAssets.FirstOrDefault(recipe => recipe != null && NormalizeId(recipe.RecipeId) == normalized);
        }

        private FoodCategorySO FindFoodCategoryById(string id)
        {
            string normalized = NormalizeId(id);
            return _foodCategoryAssets.FirstOrDefault(category => category != null && NormalizeId(category.CategoryId) == normalized);
        }

        private FoodTagSO FindFoodTagById(string id)
        {
            string normalized = NormalizeId(id);
            return _foodTagAssets.FirstOrDefault(tag => tag != null && NormalizeId(tag.TagId) == normalized);
        }

        private static List<string> ParseIdList(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return new List<string>();

            return value
                .Split(new[] { '|', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part.Trim())
                .Where(part => string.IsNullOrWhiteSpace(part) == false)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static string BuildIdList(IEnumerable<string> ids)
        {
            return string.Join("|", ids
                .Where(id => string.IsNullOrWhiteSpace(id) == false)
                .Select(id => id.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase));
        }

        private static bool ContainsId(IEnumerable<string> ids, string id)
        {
            string normalized = NormalizeId(id);
            return ids.Any(value => NormalizeId(value) == normalized);
        }

        private static string GetRecipeSortKey(RecipeSO recipe)
        {
            return recipe != null ? $"{GetSerializedDisplayName(recipe, "displayName", recipe.DisplayName)} {recipe.RecipeId}" : string.Empty;
        }

        private static string GetCategorySortKey(FoodCategorySO category)
        {
            return category != null ? $"{GetSerializedDisplayName(category, "displayName", category.DisplayName)} {category.CategoryId}" : string.Empty;
        }

        private static string GetTagSortKey(FoodTagSO tag)
        {
            return tag != null ? $"{GetSerializedDisplayName(tag, "displayName", tag.DisplayName)} {tag.TagId}" : string.Empty;
        }

        private void RebuildCookingDropdownOptions()
        {
            _recipeOptions.Clear();
            _recipeOptions.AddRange(NormalizeDropdownOptions(_recipeAssets
                .Where(recipe => recipe != null && string.IsNullOrWhiteSpace(recipe.RecipeId) == false)
                .Select(recipe => new DropdownOption(recipe.RecipeId.Trim(), GetSerializedDisplayName(recipe, "displayName", recipe.DisplayName)))));

            _foodCategoryOptions.Clear();
            _foodCategoryOptions.AddRange(NormalizeDropdownOptions(_foodCategoryAssets
                .Where(category => category != null && string.IsNullOrWhiteSpace(category.CategoryId) == false)
                .Select(category => new DropdownOption(category.CategoryId.Trim(), GetSerializedDisplayName(category, "displayName", category.DisplayName)))));

            _foodTagOptions.Clear();
            _foodTagOptions.AddRange(NormalizeDropdownOptions(_foodTagAssets
                .Where(tag => tag != null && string.IsNullOrWhiteSpace(tag.TagId) == false)
                .Select(tag => new DropdownOption(tag.TagId.Trim(), GetSerializedDisplayName(tag, "displayName", tag.DisplayName)))));
        }

        private List<DropdownOption> GetRecipeOptions()
        {
            EnsureCookingAssetsLoaded();
            return _recipeOptions;
        }

        private List<DropdownOption> GetFoodCategoryOptions()
        {
            EnsureCookingAssetsLoaded();
            return _foodCategoryOptions;
        }

        private List<DropdownOption> GetFoodTagOptions()
        {
            EnsureCookingAssetsLoaded();
            return _foodTagOptions;
        }

        private static void AddIdOptions(
            List<DropdownOption> options,
            IEnumerable<string> ids,
            Func<string, string> displayNameResolver)
        {
            foreach (string id in ids)
            {
                if (string.IsNullOrWhiteSpace(id) || options.Any(option => SameId(option.Value, id)))
                    continue;

                string trimmed = id.Trim();
                string displayName = displayNameResolver != null ? displayNameResolver(trimmed) : trimmed;
                options.Add(new DropdownOption(trimmed, displayName));
            }
        }

        private static List<DropdownOption> NormalizeDropdownOptions(IEnumerable<DropdownOption> options)
        {
            return options
                .Where(option => string.IsNullOrWhiteSpace(option.Value) == false)
                .GroupBy(option => NormalizeId(option.Value))
                .Select(group => group.First())
                .OrderBy(option => option.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static string NormalizeId(string id)
        {
            return (id ?? string.Empty).Trim().ToLowerInvariant();
        }

        private static string ToDisplayLabel(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return string.Empty;

            return DisplayLabels.TryGetValue(key, out string label) ? label : key;
        }

        private static string GetSerializedDisplayName(UnityEngine.Object asset, string propertyName, string fallback)
        {
            if (asset == null)
                return fallback ?? string.Empty;

            SerializedObject serializedObject = new SerializedObject(asset);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null && property.propertyType == SerializedPropertyType.String && string.IsNullOrWhiteSpace(property.stringValue) == false)
                return property.stringValue.Trim();

            return string.IsNullOrWhiteSpace(fallback) ? asset.name : fallback.Trim();
        }

        private static string GetFoodCategoryDisplayName(string categoryId)
        {
            if (string.IsNullOrWhiteSpace(categoryId))
                return string.Empty;

            string trimmed = categoryId.Trim();
            return FoodCategoryDisplayNames.TryGetValue(trimmed, out string displayName) ? displayName : GetCurrentValueDisplayName(trimmed);
        }

        private static string GetFoodTagDisplayName(string tagId)
        {
            if (string.IsNullOrWhiteSpace(tagId))
                return string.Empty;

            string trimmed = tagId.Trim();
            return FoodTagDisplayNames.TryGetValue(trimmed, out string displayName) ? displayName : GetCurrentValueDisplayName(trimmed);
        }

        private static string GetCurrentValueDisplayName(string id)
        {
            return string.IsNullOrWhiteSpace(id) ? string.Empty : $"현재 값: {id.Trim()}";
        }

        private void DrawVisitEventDropdownField(
            string label,
            VisitEventReference visitEvent,
            string columnName,
            ref string value,
            IReadOnlyList<DropdownOption> options,
            bool allowEmpty)
        {
            List<DropdownOption> displayOptions = BuildDropdownOptions(value, options, allowEmpty);
            string[] labels = displayOptions.Select(option => option.DisplayText).ToArray();
            int currentIndex = FindDropdownIndex(displayOptions, value);

            EditorGUI.BeginChangeCheck();
            int nextIndex = EditorGUILayout.Popup(ToDisplayLabel(label), currentIndex, labels);
            if (EditorGUI.EndChangeCheck() && nextIndex >= 0 && nextIndex < displayOptions.Count)
                SetVisitEventRawValue(visitEvent, columnName, ref value, displayOptions[nextIndex].Value);
        }

        private void DrawVisitEventMultiSelectField(
            string label,
            VisitEventReference visitEvent,
            string columnName,
            ref string value,
            IReadOnlyList<DropdownOption> options)
        {
            List<DropdownOption> displayOptions = BuildMultiSelectOptions(value, options);
            if (displayOptions.Count > 120)
            {
                EditorGUILayout.HelpBox($"{ToDisplayLabel(label)} 후보가 너무 많아서 다중 선택 UI 대신 직접 입력으로 표시합니다.", MessageType.Warning);
                DrawVisitEventTextField(label, visitEvent, columnName, ref value);
                return;
            }

            if (displayOptions.Count == 0)
            {
                EditorGUILayout.HelpBox($"{ToDisplayLabel(label)} 후보가 없습니다. 직접 입력으로 표시합니다.", MessageType.Info);
                DrawVisitEventTextField(label, visitEvent, columnName, ref value);
                return;
            }

            List<string> selectedIds = ParseIdList(value);
            bool changed = false;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label(ToDisplayLabel(label), EditorStyles.miniBoldLabel);
            for (int i = 0; i < displayOptions.Count; i++)
            {
                DropdownOption option = displayOptions[i];
                bool selected = ContainsId(selectedIds, option.Value);
                EditorGUI.BeginChangeCheck();
                bool next = EditorGUILayout.ToggleLeft(option.DisplayText, selected);
                if (EditorGUI.EndChangeCheck())
                {
                    if (next)
                    {
                        if (ContainsId(selectedIds, option.Value) == false)
                            selectedIds.Add(option.Value);
                    }
                    else
                    {
                        selectedIds.RemoveAll(id => SameId(id, option.Value));
                    }

                    changed = true;
                }
            }

            EditorGUILayout.EndVertical();

            if (changed)
                SetVisitEventRawValue(visitEvent, columnName, ref value, BuildIdList(selectedIds));
        }

        private List<DropdownOption> GetQuestionCategoryOptions()
        {
            return _questionCategoryOptions;
        }

        private void RebuildQuestionCategoryOptions()
        {
            _questionCategoryOptions.Clear();
            List<DropdownOption> options = new List<DropdownOption>();
            foreach (Dictionary<string, string> row in ReadCsv(QuestionCategoryCsvPath))
            {
                string categoryId = Get(row, "CategoryId");
                if (string.IsNullOrWhiteSpace(categoryId))
                    continue;

                string displayName = Get(row, "DisplayName");
                options.Add(new DropdownOption(categoryId.Trim(), string.IsNullOrWhiteSpace(displayName) ? categoryId.Trim() : displayName.Trim()));
            }

            if (options.Count == 0)
            {
                options.Add(new DropdownOption("Taste", "맛"));
                options.Add(new DropdownOption("TextureTemp", "온도/식감"));
                options.Add(new DropdownOption("Condition", "몸 상태"));
                options.Add(new DropdownOption("Avoid", "피하고 싶은 음식"));
            }

            _questionCategoryOptions.AddRange(options
                .GroupBy(option => NormalizeId(option.Value))
                .Select(group => group.First())
                .OrderBy(option => option.Value, StringComparer.OrdinalIgnoreCase));
        }

        private List<DropdownOption> GetRegionOptions()
        {
            return _regionOptions;
        }

        private void RebuildRegionOptions()
        {
            _regionOptions.Clear();
            List<DropdownOption> options = new List<DropdownOption>
            {
                new DropdownOption("*", "모든 지역")
            };

            AddIdOptions(options, RegionDisplayNames.Keys.Where(regionId => string.Equals(regionId, "*", StringComparison.OrdinalIgnoreCase) == false), GetRegionDisplayName);

            foreach (RegionPoolDraft pool in _regionPools)
            {
                string regionId = pool.RegionId;
                if (string.IsNullOrWhiteSpace(regionId) == false)
                    options.Add(new DropdownOption(regionId.Trim(), GetRegionDisplayName(regionId)));
            }

            foreach (VisitEventReference visitEvent in _visitEvents)
            {
                foreach (string regionId in ParseIdList(visitEvent.RegionId))
                {
                    if (string.IsNullOrWhiteSpace(regionId) == false)
                        options.Add(new DropdownOption(regionId.Trim(), GetRegionDisplayName(regionId)));
                }
            }

            _regionOptions.AddRange(options
                .GroupBy(option => NormalizeId(option.Value))
                .Select(group => group.First())
                .OrderBy(option => option.Value == "*" ? string.Empty : option.Value, StringComparer.OrdinalIgnoreCase));
        }

        private List<DropdownOption> GetNpcEventOptions(string npcId, string excludeEventId)
        {
            return _visitEvents
                .Where(visitEvent => string.IsNullOrWhiteSpace(visitEvent.EventId) == false)
                .Where(visitEvent => string.IsNullOrWhiteSpace(npcId)
                                     || string.Equals(visitEvent.NpcId, npcId, StringComparison.OrdinalIgnoreCase))
                .Where(visitEvent => string.IsNullOrWhiteSpace(excludeEventId)
                                     || string.Equals(visitEvent.EventId, excludeEventId, StringComparison.OrdinalIgnoreCase) == false)
                .Select(visitEvent => new DropdownOption(visitEvent.EventId.Trim(), BuildEventOptionLabel(visitEvent)))
                .GroupBy(option => NormalizeId(option.Value))
                .Select(group => group.First())
                .OrderBy(option => option.Value, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private List<DropdownOption> GetDialogueGroupOptions(string eventId)
        {
            List<DropdownOption> options = DialogueGroupDisplayNames
                .Select(pair => new DropdownOption(pair.Key, pair.Value))
                .ToList();

            AddIdOptions(
                options,
                _dialogues
                    .Where(dialogue => string.Equals(dialogue.EventId, eventId, StringComparison.OrdinalIgnoreCase))
                    .Select(dialogue => dialogue.Group),
                GetDialogueGroupDisplayName);

            AddIdOptions(
                options,
                _dialogues.Select(dialogue => dialogue.Group),
                GetDialogueGroupDisplayName);

            return options
                .Where(option => string.IsNullOrWhiteSpace(option.Value) == false)
                .GroupBy(option => NormalizeId(option.Value))
                .Select(group => group.First())
                .OrderBy(option => GetDialogueGroupOrder(option.Value))
                .ThenBy(option => option.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private string BuildEventOptionLabel(VisitEventReference visitEvent)
        {
            if (visitEvent == null)
                return string.Empty;

            string npcName = GetNpcDisplayName(visitEvent.NpcId);
            string type = GetDropdownDisplayName(EventTypeOptions, visitEvent.EventType, "이벤트");
            string preview = GetEventPreviewText(visitEvent.EventId);

            if (string.IsNullOrWhiteSpace(preview))
                return $"{npcName} / {type}";

            return $"{npcName} / {type} / {preview}";
        }

        private static string GetRegionDisplayName(string regionId)
        {
            if (string.IsNullOrWhiteSpace(regionId))
                return string.Empty;

            string trimmed = regionId.Trim();
            return RegionDisplayNames.TryGetValue(trimmed, out string displayName) ? displayName : trimmed;
        }

        private static string GetDialogueGroupDisplayName(string group)
        {
            if (string.IsNullOrWhiteSpace(group))
                return string.Empty;

            string trimmed = group.Trim();
            return DialogueGroupDisplayNames.TryGetValue(trimmed, out string displayName) ? displayName : trimmed;
        }

        private static string GetDropdownDisplayName(IReadOnlyList<DropdownOption> options, string value, string fallback)
        {
            if (options != null)
            {
                for (int i = 0; i < options.Count; i++)
                {
                    if (SameId(options[i].Value, value))
                        return options[i].DisplayName;
                }
            }

            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }

        private string GetNpcDisplayName(string npcId)
        {
            if (string.IsNullOrWhiteSpace(npcId))
                return "NPC";

            NpcDraft npc = _npcs.FirstOrDefault(candidate => string.Equals(candidate.NpcId, npcId, StringComparison.OrdinalIgnoreCase));
            if (npc == null || string.IsNullOrWhiteSpace(npc.DisplayName))
                return npcId.Trim();

            return npc.DisplayName.Trim();
        }

        private string GetEventPreviewText(string eventId)
        {
            DialogueDraft firstLine = _dialogues
                .Where(dialogue => string.Equals(dialogue.EventId, eventId, StringComparison.OrdinalIgnoreCase))
                .OrderBy(dialogue => GetDialogueGroupOrder(dialogue.Group))
                .ThenBy(dialogue => dialogue.LineOrder)
                .FirstOrDefault(dialogue => string.IsNullOrWhiteSpace(dialogue.Text) == false);

            if (firstLine == null)
                return string.Empty;

            return ShortenLabel(RemoveBoldMarkers(firstLine.Text), 28);
        }

        private static int GetDialogueGroupOrder(string group)
        {
            if (string.Equals(group, "Intro", StringComparison.OrdinalIgnoreCase))
                return 0;

            if (string.Equals(group, "OrderIntent", StringComparison.OrdinalIgnoreCase))
                return 1;

            return 10;
        }

        private static string RemoveBoldMarkers(string text)
        {
            return string.IsNullOrWhiteSpace(text) ? string.Empty : text.Replace(BoldMarker, string.Empty);
        }

        private static string ShortenLabel(string text, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            string trimmed = text.Trim();
            if (trimmed.Length <= maxLength)
                return trimmed;

            return $"{trimmed.Substring(0, maxLength)}...";
        }

        private static List<DropdownOption> BuildDropdownOptions(string currentValue, IReadOnlyList<DropdownOption> options, bool allowEmpty)
        {
            List<DropdownOption> result = new List<DropdownOption>();
            if (allowEmpty)
                result.Add(new DropdownOption(string.Empty, "없음"));

            AddDropdownOptions(result, options);
            if (string.IsNullOrWhiteSpace(currentValue) == false && result.Any(option => SameId(option.Value, currentValue)) == false)
                result.Insert(allowEmpty ? 1 : 0, new DropdownOption(currentValue.Trim(), $"현재 값: {currentValue.Trim()}"));

            return result;
        }

        private static List<DropdownOption> BuildMultiSelectOptions(string currentValue, IReadOnlyList<DropdownOption> options)
        {
            List<DropdownOption> result = new List<DropdownOption>();
            AddDropdownOptions(result, options);

            foreach (string id in ParseIdList(currentValue))
            {
                if (result.Any(option => SameId(option.Value, id)))
                    continue;

                result.Add(new DropdownOption(id, $"현재 값: {id}"));
            }

            return result;
        }

        private static string[] BuildAddOptionLabels(IReadOnlyList<DropdownOption> options)
        {
            if (options == null || options.Count == 0)
                return new[] { "추가할 항목 없음" };

            return options.Select(option => option.DisplayText).ToArray();
        }

        private static DropdownOption FindOptionById(IReadOnlyList<DropdownOption> options, string id)
        {
            if (options != null)
            {
                for (int i = 0; i < options.Count; i++)
                {
                    if (SameId(options[i].Value, id))
                        return options[i];
                }
            }

            return new DropdownOption(id ?? string.Empty, $"현재 값: {id}");
        }

        private string BuildNpcMultiSelectAddIndexKey(string label)
        {
            string npcId = _selectedNpc != null ? _selectedNpc.NpcId : string.Empty;
            return $"{npcId}:{label}";
        }

        private int GetNpcMultiSelectAddIndex(string key, int optionCount)
        {
            if (optionCount <= 0)
                return 0;

            if (_npcMultiSelectAddIndices.TryGetValue(key, out int index) == false)
                return 0;

            int clamped = Mathf.Clamp(index, 0, optionCount - 1);
            if (clamped != index)
                _npcMultiSelectAddIndices[key] = clamped;

            return clamped;
        }

        private static void AddDropdownOptions(List<DropdownOption> target, IReadOnlyList<DropdownOption> source)
        {
            if (source == null)
                return;

            for (int i = 0; i < source.Count; i++)
            {
                DropdownOption option = source[i];
                if (string.IsNullOrWhiteSpace(option.Value) || target.Any(existing => SameId(existing.Value, option.Value)))
                    continue;

                target.Add(option);
            }
        }

        private static int FindDropdownIndex(IReadOnlyList<DropdownOption> options, string value)
        {
            for (int i = 0; i < options.Count; i++)
            {
                if (SameId(options[i].Value, value))
                    return i;
            }

            return 0;
        }

        private static bool SameId(string left, string right)
        {
            return string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private void DrawVisitEventTextField(string label, VisitEventReference visitEvent, string columnName, ref string value)
        {
            EditorGUI.BeginChangeCheck();
            string next = EditorGUILayout.TextField(ToDisplayLabel(label), value);
            if (EditorGUI.EndChangeCheck())
            {
                value = next;
                visitEvent.SetRaw(columnName, next);
                MarkDirty($"방문 이벤트 {ToDisplayLabel(label)} 수정됨");
            }
        }

        private void DrawVisitEventIntField(string label, VisitEventReference visitEvent, string columnName, ref int value)
        {
            EditorGUI.BeginChangeCheck();
            int next = EditorGUILayout.IntField(ToDisplayLabel(label), value);
            if (EditorGUI.EndChangeCheck())
            {
                value = Mathf.Max(0, next);
                visitEvent.SetRaw(columnName, value.ToString());
                MarkDirty($"방문 이벤트 {ToDisplayLabel(label)} 수정됨");
            }
        }

        private void BoldSelectedText()
        {
            if (_selectedDialogue == null)
                return;

            CaptureDialogueTextSelection(GetDialogueTextControlName(_selectedDialogue));

            if (!TryGetDialogueTextSelection(out int start, out int end))
            {
                _statusMessage = "대사 영역에서 볼드 처리할 글자를 드래그로 선택한 뒤 다시 눌러주세요.";
                return;
            }

            string value = _selectedDialogue.Text ?? string.Empty;
            if (RangeContainsBoldMarker(value, start, end))
            {
                _statusMessage = "선택 영역에 기존 볼드 마커가 포함되어 있어요. 먼저 볼드 마커를 제거하거나 텍스트만 선택해주세요.";
                return;
            }

            if (TryGetContainingBoldSpan(value, start, end, out int markerStart, out int contentStart, out int contentEnd, out int markerEnd))
            {
                ApplySelectedDialogueText(
                    value.Substring(0, markerStart)
                    + value.Substring(contentStart, start - contentStart)
                    + BoldMarker
                    + value.Substring(start, end - start)
                    + BoldMarker
                    + value.Substring(end, contentEnd - end)
                    + value.Substring(markerEnd + BoldMarker.Length),
                    "선택 영역 볼드 적용됨");
                return;
            }

            ApplySelectedDialogueText(
                value.Substring(0, start)
                + BoldMarker
                + value.Substring(start, end - start)
                + BoldMarker
                + value.Substring(end),
                "선택 영역 볼드 적용됨");
        }

        private void WrapWholeDialogueText()
        {
            if (_selectedDialogue == null)
                return;

            string value = _selectedDialogue.Text ?? string.Empty;
            string plainValue = value.Replace(BoldMarker, string.Empty);
            string wholeBoldValue = $"{BoldMarker}{plainValue}{BoldMarker}";
            if (string.Equals(value, wholeBoldValue, StringComparison.Ordinal))
                return;

            ApplySelectedDialogueText(wholeBoldValue, "전체 볼드 적용됨");
        }

        private void RemoveBoldMarkers()
        {
            if (_selectedDialogue == null)
                return;

            ApplySelectedDialogueText((_selectedDialogue.Text ?? string.Empty).Replace(BoldMarker, string.Empty), "볼드 마커 제거됨");
        }

        private void CaptureDialogueTextSelection(string controlName)
        {
            if (_selectedDialogue == null)
                return;

            string value = _selectedDialogue.Text ?? string.Empty;
            TextEditor editor = FindActiveDialogueTextEditor(value);
            if (editor == null)
                return;

            int start = Mathf.Clamp(Mathf.Min(editor.cursorIndex, editor.selectIndex), 0, value.Length);
            int end = Mathf.Clamp(Mathf.Max(editor.cursorIndex, editor.selectIndex), 0, value.Length);

            if (start == end)
                return;

            StoreDialogueTextSelection(start, end);
        }

        private static TextEditor FindActiveDialogueTextEditor(string dialogueText)
        {
            TextEditor keyboardEditor = GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl) as TextEditor;
            if (IsUsableDialogueTextEditor(keyboardEditor, dialogueText))
                return keyboardEditor;

            const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            foreach (FieldInfo field in typeof(EditorGUI).GetFields(flags))
            {
                if (!typeof(TextEditor).IsAssignableFrom(field.FieldType))
                    continue;

                if (IsUsableDialogueTextEditor(field.GetValue(null) as TextEditor, dialogueText))
                    return field.GetValue(null) as TextEditor;
            }

            foreach (PropertyInfo property in typeof(EditorGUI).GetProperties(flags))
            {
                if (!typeof(TextEditor).IsAssignableFrom(property.PropertyType) || property.GetIndexParameters().Length > 0)
                    continue;

                TextEditor editor;
                try
                {
                    editor = property.GetValue(null) as TextEditor;
                }
                catch
                {
                    continue;
                }

                if (IsUsableDialogueTextEditor(editor, dialogueText))
                    return editor;
            }

            return null;
        }

        private static bool IsUsableDialogueTextEditor(TextEditor editor, string dialogueText)
        {
            if (editor == null || editor.cursorIndex == editor.selectIndex)
                return false;

            string editorText = editor.text ?? string.Empty;
            if (string.Equals(editorText, dialogueText ?? string.Empty, StringComparison.Ordinal))
                return true;

            int maxIndex = Mathf.Max(editor.cursorIndex, editor.selectIndex);
            return maxIndex <= (dialogueText ?? string.Empty).Length;
        }

        private void StoreDialogueTextSelection(int start, int end)
        {
            string value = _selectedDialogue != null ? _selectedDialogue.Text ?? string.Empty : string.Empty;
            _textSelectionDialogue = _selectedDialogue;
            _textSelectionStart = Mathf.Clamp(start, 0, value.Length);
            _textSelectionEnd = Mathf.Clamp(end, 0, value.Length);
        }

        private bool HasStoredDialogueTextSelection()
        {
            if (_selectedDialogue == null || _textSelectionDialogue != _selectedDialogue)
                return false;

            return _textSelectionStart != _textSelectionEnd;
        }

        private bool TryGetDialogueTextSelection(out int start, out int end)
        {
            start = 0;
            end = 0;

            if (_selectedDialogue == null || _textSelectionDialogue != _selectedDialogue)
                return false;

            string value = _selectedDialogue.Text ?? string.Empty;
            start = Mathf.Clamp(Mathf.Min(_textSelectionStart, _textSelectionEnd), 0, value.Length);
            end = Mathf.Clamp(Mathf.Max(_textSelectionStart, _textSelectionEnd), 0, value.Length);
            return start < end;
        }

        private static bool RangeContainsBoldMarker(string value, int start, int end)
        {
            if (string.IsNullOrEmpty(value) || start >= end)
                return false;

            int searchIndex = 0;
            while (searchIndex < value.Length)
            {
                int markerIndex = value.IndexOf(BoldMarker, searchIndex, StringComparison.Ordinal);
                if (markerIndex < 0)
                    return false;

                int markerEnd = markerIndex + BoldMarker.Length;
                if (markerIndex < end && markerEnd > start)
                    return true;

                searchIndex = markerEnd;
            }

            return false;
        }

        private static bool TryGetContainingBoldSpan(
            string value,
            int start,
            int end,
            out int markerStart,
            out int contentStart,
            out int contentEnd,
            out int markerEnd)
        {
            markerStart = -1;
            contentStart = -1;
            contentEnd = -1;
            markerEnd = -1;

            if (string.IsNullOrEmpty(value) || start >= end)
                return false;

            int searchIndex = 0;
            while (searchIndex < value.Length)
            {
                markerStart = value.IndexOf(BoldMarker, searchIndex, StringComparison.Ordinal);
                if (markerStart < 0)
                    return false;

                contentStart = markerStart + BoldMarker.Length;
                markerEnd = value.IndexOf(BoldMarker, contentStart, StringComparison.Ordinal);
                if (markerEnd < 0)
                    return false;

                if (start >= contentStart && end <= markerEnd)
                {
                    contentEnd = markerEnd;
                    return true;
                }

                searchIndex = markerEnd + BoldMarker.Length;
            }

            return false;
        }

        private void ApplySelectedDialogueText(string value, string message)
        {
            if (_selectedDialogue == null)
                return;

            _selectedDialogue.Text = value ?? string.Empty;
            ResetDialogueTextSelection();
            ClearDialogueTextFocus();
            MarkDirty(message);
            Repaint();
        }

        private void SelectDialogue(DialogueDraft dialogue)
        {
            if (_selectedDialogue == dialogue)
                return;

            ClearDialogueTextFocus();
            _selectedDialogue = dialogue;
            ResetDialogueTextSelection();
            Repaint();
        }

        private void ClearDialogueTextFocus()
        {
            GUI.FocusControl(null);
            EditorGUIUtility.editingTextField = false;
        }

        private void ResetDialogueTextSelection()
        {
            _textSelectionDialogue = null;
            _textSelectionStart = 0;
            _textSelectionEnd = 0;
        }

        private string GetDialogueTextControlName(DialogueDraft dialogue)
        {
            if (dialogue == null)
                return DialogueTextControlName;

            return $"{DialogueTextControlName}.{Mathf.Max(0, _dialogues.IndexOf(dialogue))}";
        }

        private void ReconcileSelectionState()
        {
            if (_selectedNpc != null && _npcs.Contains(_selectedNpc) == false)
            {
                _selectedNpc = _npcs.FirstOrDefault();
                _selectedEventId = _selectedNpc != null
                    ? GetEventIdsForNpc(_selectedNpc.NpcId).FirstOrDefault() ?? string.Empty
                    : string.Empty;
                _newEventId = _selectedNpc != null ? GenerateUniqueEventId(_selectedNpc.NpcId) : string.Empty;
                _selectedDialogue = null;
                ResetDialogueTextSelection();
            }

            if (_selectedDialogue != null && _dialogues.Contains(_selectedDialogue) == false)
            {
                _selectedDialogue = null;
                ResetDialogueTextSelection();
            }

            if (_selectedNpc == null)
            {
                _selectedEventId = string.Empty;
                _newEventId = string.Empty;
                _selectedDialogue = null;
                ResetDialogueTextSelection();
            }
        }

        private void SelectNpc(NpcDraft npc)
        {
            ClearDialogueTextFocus();
            _selectedNpc = npc;
            _selectedEventId = npc != null ? GetEventIdsForNpc(npc.NpcId).FirstOrDefault() ?? string.Empty : string.Empty;
            _newEventId = npc != null ? GenerateUniqueEventId(npc.NpcId) : string.Empty;
            _selectedDialogue = null;
            ResetDialogueTextSelection();
            _eventListScroll = Vector2.zero;
            _dialogueListScroll = Vector2.zero;
            _dialogueDetailScroll = Vector2.zero;
        }

        private void SelectEvent(string eventId)
        {
            ClearDialogueTextFocus();
            _selectedEventId = eventId ?? string.Empty;
            _selectedDialogue = null;
            ResetDialogueTextSelection();
            _dialogueListScroll = Vector2.zero;
            _dialogueDetailScroll = Vector2.zero;
            _visitEventDetailScroll = Vector2.zero;
        }

        private VisitEventReference GetSelectedVisitEvent()
        {
            if (string.IsNullOrWhiteSpace(_selectedEventId))
                return null;

            return _visitEvents.FirstOrDefault(visitEvent =>
                string.Equals(visitEvent.EventId, _selectedEventId, StringComparison.OrdinalIgnoreCase));
        }

        private void CreateVisitEventForSelectedEvent()
        {
            if (_selectedNpc == null || string.IsNullOrWhiteSpace(_selectedEventId))
                return;

            if (GetSelectedVisitEvent() != null)
                return;

            VisitEventReference visitEvent = VisitEventReference.CreateDefault(_selectedEventId, _selectedNpc.NpcId);
            _visitEvents.Add(visitEvent);
            EnsureVisitEventHeaders();
            MarkDirty("VisitEvent 연결 생성됨");
        }

        private void RenameVisitEventId(VisitEventReference visitEvent, string oldEventId, string newEventId)
        {
            if (visitEvent == null)
                return;

            newEventId = newEventId?.Trim() ?? string.Empty;
            oldEventId = oldEventId?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(newEventId))
            {
                _statusMessage = "이벤트 ID는 비워둘 수 없습니다.";
                Repaint();
                return;
            }

            if (string.Equals(oldEventId, newEventId, StringComparison.OrdinalIgnoreCase) == false
                && _visitEvents.Any(existing => existing != visitEvent
                                                && string.Equals(existing.EventId, newEventId, StringComparison.OrdinalIgnoreCase)))
            {
                _statusMessage = $"이미 존재하는 이벤트 ID입니다: {newEventId}";
                Repaint();
                return;
            }

            visitEvent.EventId = newEventId;
            visitEvent.SetRaw("EventId", newEventId);

            if (string.IsNullOrWhiteSpace(oldEventId) == false)
            {
                foreach (DialogueDraft dialogue in _dialogues)
                {
                    if (string.Equals(dialogue.EventId, oldEventId, StringComparison.OrdinalIgnoreCase))
                        dialogue.EventId = newEventId;
                }
            }

            _selectedEventId = newEventId;
            MarkDirty("VisitEvent EventId 수정됨");
        }

        private void RenameNpcId(NpcDraft npc, string oldNpcId, string newNpcId)
        {
            if (npc == null)
                return;

            oldNpcId = oldNpcId?.Trim() ?? string.Empty;
            newNpcId = newNpcId?.Trim() ?? string.Empty;
            npc.NpcId = newNpcId;

            if (string.IsNullOrWhiteSpace(oldNpcId) == false
                && string.Equals(oldNpcId, newNpcId, StringComparison.OrdinalIgnoreCase) == false)
            {
                foreach (DialogueDraft dialogue in _dialogues)
                {
                    if (string.Equals(dialogue.Speaker, oldNpcId, StringComparison.OrdinalIgnoreCase))
                        dialogue.Speaker = newNpcId;
                }

                foreach (VisitEventReference visitEvent in _visitEvents)
                {
                    if (string.Equals(visitEvent.NpcId, oldNpcId, StringComparison.OrdinalIgnoreCase) == false)
                        continue;

                    visitEvent.NpcId = newNpcId;
                    visitEvent.SetRaw("NpcId", newNpcId);
                }

                foreach (RegionPoolDraft pool in _regionPools)
                {
                    if (string.Equals(pool.NpcId, oldNpcId, StringComparison.OrdinalIgnoreCase) == false)
                        continue;

                    pool.NpcId = newNpcId;
                    pool.SyncRawValues();
                }
            }

            if (_selectedNpc == npc)
            {
                BuildVisibleEventList();
                _newEventId = GenerateUniqueEventId(newNpcId);
            }

            MarkDirty("NPC ID 수정됨");
        }

        private void AddNpc()
        {
            string id = GenerateUniqueNpcId();
            NpcDraft npc = new NpcDraft
            {
                NpcId = id,
                DisplayName = "새 NPC",
                Race = string.Empty,
                Role = string.Empty,
                PreferredTags = string.Empty,
                PreferredFoodTypes = string.Empty,
                AvoidTags = string.Empty,
                Notes = string.Empty,
                RequestAvailable = false,
                RequestUnlockLevel = 5,
                RequestUnlockEvent = string.Empty
            };

            _npcs.Add(npc);
            _npcSearch = string.Empty;
            SelectNpc(npc);
            MarkDirty("NPC 추가됨");
        }

        private void DeleteSelectedNpc()
        {
            if (_selectedNpc == null)
                return;

            string npcId = _selectedNpc.NpcId;
            HashSet<string> ownedEventIds = new HashSet<string>(
                _visitEvents
                    .Where(visitEvent => string.Equals(visitEvent.NpcId, npcId, StringComparison.OrdinalIgnoreCase))
                    .Select(visitEvent => visitEvent.EventId)
                    .Where(eventId => string.IsNullOrWhiteSpace(eventId) == false),
                StringComparer.OrdinalIgnoreCase);
            int visitEventCount = ownedEventIds.Count;
            int ownedDialogueCount = _dialogues.Count(line =>
                string.IsNullOrWhiteSpace(line.EventId) == false && ownedEventIds.Contains(line.EventId));
            int orphanSpeakerLineCount = _dialogues.Count(line =>
                string.Equals(line.Speaker, npcId, StringComparison.OrdinalIgnoreCase)
                && (string.IsNullOrWhiteSpace(line.EventId) || ownedEventIds.Contains(line.EventId) == false));
            int deleteDialogueCount = ownedDialogueCount + orphanSpeakerLineCount;
            bool confirmed = EditorUtility.DisplayDialog(
                "NPC 삭제",
                $"NPC '{npcId}'를 삭제할까요?\n\n함께 삭제됩니다:\nVisitEvent {visitEventCount}개\n연결 대사 {deleteDialogueCount}개\n\n이 작업은 저장 전까지 CSV에 반영되지 않습니다.",
                "삭제",
                "취소");

            if (confirmed == false)
                return;

            ClearDialogueTextFocus();
            _npcs.Remove(_selectedNpc);
            _visitEvents.RemoveAll(visitEvent => string.Equals(visitEvent.NpcId, npcId, StringComparison.OrdinalIgnoreCase));
            _regionPools.RemoveAll(pool => string.Equals(pool.NpcId, npcId, StringComparison.OrdinalIgnoreCase));
            _dialogues.RemoveAll(line =>
                string.IsNullOrWhiteSpace(line.EventId) == false && ownedEventIds.Contains(line.EventId)
                || string.Equals(line.Speaker, npcId, StringComparison.OrdinalIgnoreCase));
            _selectedNpc = _npcs.FirstOrDefault();
            _selectedEventId = _selectedNpc != null
                ? GetEventIdsForNpc(_selectedNpc.NpcId).FirstOrDefault() ?? string.Empty
                : string.Empty;
            _newEventId = _selectedNpc != null ? GenerateUniqueEventId(_selectedNpc.NpcId) : string.Empty;
            _selectedDialogue = null;
            ResetDialogueTextSelection();
            _eventListScroll = Vector2.zero;
            _dialogueListScroll = Vector2.zero;
            _dialogueDetailScroll = Vector2.zero;
            _visitEventDetailScroll = Vector2.zero;
            MarkDirty($"NPC 삭제됨. VisitEvent {visitEventCount}개, 대사 {deleteDialogueCount}개도 함께 삭제됨");
        }

        private void AddEvent()
        {
            if (_selectedNpc == null)
                return;

            string eventId = string.IsNullOrWhiteSpace(_newEventId)
                ? GenerateUniqueEventId(_selectedNpc.NpcId)
                : _newEventId.Trim();

            if (_dialogues.Any(line => string.Equals(line.EventId, eventId, StringComparison.OrdinalIgnoreCase)))
            {
                SelectEvent(eventId);
                _statusMessage = "이미 존재하는 이벤트를 선택했습니다.";
                return;
            }

            DialogueDraft dialogue = new DialogueDraft
            {
                EventId = eventId,
                Group = "Intro",
                QuestionCategory = string.Empty,
                LineOrder = 1,
                Speaker = _selectedNpc.NpcId,
                Text = "**새 이벤트 대사입니다.**"
            };

            _dialogues.Add(dialogue);
            VisitEventReference visitEvent = VisitEventReference.CreateDefault(eventId, _selectedNpc.NpcId);
            _visitEvents.Add(visitEvent);
            EnsureRegionPoolsForVisitEvent(visitEvent);
            EnsureVisitEventHeaders();
            _selectedEventId = eventId;
            SelectDialogue(dialogue);
            _newEventId = GenerateUniqueEventId(_selectedNpc.NpcId);
            MarkDirty("이벤트와 지역 풀이 추가됨");
        }

        private void DeleteSelectedEvent()
        {
            if (string.IsNullOrWhiteSpace(_selectedEventId))
                return;

            string eventId = _selectedEventId;
            int dialogueCount = _dialogues.Count(dialogue => string.Equals(dialogue.EventId, eventId, StringComparison.OrdinalIgnoreCase));
            int visitEventCount = _visitEvents.Count(visitEvent => string.Equals(visitEvent.EventId, eventId, StringComparison.OrdinalIgnoreCase));
            int option = EditorUtility.DisplayDialogComplex(
                "이벤트 삭제",
                $"이벤트 '{eventId}'를 삭제할까요?\n\n대사 줄 수: {dialogueCount}\nVisitEvent 행 수: {visitEventCount}",
                "이벤트와 대사 삭제",
                "취소",
                "VisitEvent만 삭제");

            if (option == 1)
                return;

            _visitEvents.RemoveAll(visitEvent => string.Equals(visitEvent.EventId, eventId, StringComparison.OrdinalIgnoreCase));

            if (option == 0)
                _dialogues.RemoveAll(dialogue => string.Equals(dialogue.EventId, eventId, StringComparison.OrdinalIgnoreCase));

            bool regionPoolChanged = _selectedNpc != null && SyncRegionPoolsForNpcEvents(_selectedNpc.NpcId);

            ClearDialogueTextFocus();
            _selectedEventId = _selectedNpc != null ? GetEventIdsForNpc(_selectedNpc.NpcId).FirstOrDefault() ?? string.Empty : string.Empty;
            _selectedDialogue = null;
            ResetDialogueTextSelection();
            _eventListScroll = Vector2.zero;
            _dialogueListScroll = Vector2.zero;
            _dialogueDetailScroll = Vector2.zero;
            _visitEventDetailScroll = Vector2.zero;
            MarkDirty(regionPoolChanged
                ? "이벤트 삭제에 맞춰 지역 풀이 갱신됨"
                : option == 0 ? "이벤트와 대사 삭제됨" : "VisitEvent 연결 삭제됨");
        }

        private void AddDialogueLine()
        {
            if (_selectedNpc == null)
                return;

            string eventId = string.IsNullOrWhiteSpace(_selectedEventId)
                ? GuessEventIdForNpc(_selectedNpc.NpcId)
                : _selectedEventId;
            List<DialogueDraft> eventLines = GetSortedEventDialogues(eventId);
            DialogueDraft referenceLine = _selectedDialogue != null
                                          && string.Equals(_selectedDialogue.EventId, eventId, StringComparison.OrdinalIgnoreCase)
                ? _selectedDialogue
                : null;
            DialogueDraft fallbackLine = eventLines.LastOrDefault();
            string group = referenceLine?.Group ?? fallbackLine?.Group ?? "Intro";
            string questionCategory = referenceLine?.QuestionCategory ?? fallbackLine?.QuestionCategory ?? string.Empty;

            DialogueDraft dialogue = new DialogueDraft
            {
                EventId = eventId,
                Group = group,
                QuestionCategory = questionCategory,
                LineOrder = 0,
                Speaker = _selectedNpc.NpcId,
                Text = "**새 대사입니다.**"
            };

            _dialogues.Add(dialogue);
            InsertDialogueAfterReference(eventId, group, dialogue, referenceLine);
            _selectedEventId = eventId;
            SelectDialogue(dialogue);
            MarkDirty("대사 추가됨");
        }

        private void DeleteSelectedDialogue()
        {
            if (_selectedDialogue == null)
                return;

            bool confirmed = EditorUtility.DisplayDialog(
                "대사 삭제",
                $"{_selectedDialogue.EventId}/{_selectedDialogue.Group} #{_selectedDialogue.LineOrder} 대사를 삭제할까요?",
                "삭제",
                "취소");

            if (confirmed == false)
                return;

            string eventId = _selectedDialogue.EventId;
            string group = _selectedDialogue.Group;
            _dialogues.Remove(_selectedDialogue);
            ReindexDialogueGroup(eventId, group);
            SelectDialogue(GetSortedEventDialogues(eventId).FirstOrDefault());
            MarkDirty("대사 삭제됨");
        }

        private void ReorderDialogueLine(DialogueDraft dragged, DialogueDraft target, bool insertAfter)
        {
            if (dragged == null
                || target == null
                || dragged == target
                || string.Equals(dragged.EventId, target.EventId, StringComparison.OrdinalIgnoreCase) == false)
            {
                return;
            }

            string oldGroup = dragged.Group;
            if (string.Equals(dragged.Group, target.Group, StringComparison.OrdinalIgnoreCase) == false)
            {
                dragged.Group = target.Group;
                dragged.QuestionCategory = target.QuestionCategory;
                ReindexDialogueGroup(dragged.EventId, oldGroup);
            }

            List<DialogueDraft> groupLines = GetSortedDialogueGroup(dragged.EventId, dragged.Group);
            groupLines.Remove(dragged);

            int targetIndex = groupLines.IndexOf(target);
            if (targetIndex < 0)
                return;

            int insertIndex = Mathf.Clamp(targetIndex + (insertAfter ? 1 : 0), 0, groupLines.Count);
            groupLines.Insert(insertIndex, dragged);
            ReindexOrderedLines(groupLines);

            SelectDialogue(dragged);
            MarkDirty("대사 순서 변경됨");
        }

        private void InsertDialogueAfterReference(
            string eventId,
            string group,
            DialogueDraft dialogue,
            DialogueDraft referenceLine)
        {
            List<DialogueDraft> groupLines = GetSortedDialogueGroup(eventId, group);
            groupLines.Remove(dialogue);

            int insertIndex = groupLines.Count;
            if (referenceLine != null
                && string.Equals(referenceLine.EventId, eventId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(referenceLine.Group, group, StringComparison.OrdinalIgnoreCase))
            {
                int referenceIndex = groupLines.IndexOf(referenceLine);
                if (referenceIndex >= 0)
                    insertIndex = referenceIndex + 1;
            }

            groupLines.Insert(insertIndex, dialogue);
            ReindexOrderedLines(groupLines);
        }

        private List<DialogueDraft> GetSortedEventDialogues(string eventId)
        {
            return _dialogues
                .Where(line => string.Equals(line.EventId, eventId, StringComparison.OrdinalIgnoreCase))
                .OrderBy(line => line, DialogueComparer.Instance)
                .ToList();
        }

        private List<DialogueDraft> GetSortedDialogueGroup(string eventId, string group)
        {
            return _dialogues
                .Where(line => string.Equals(line.EventId, eventId, StringComparison.OrdinalIgnoreCase)
                               && string.Equals(line.Group, group, StringComparison.OrdinalIgnoreCase))
                .OrderBy(line => line.LineOrder)
                .ThenBy(line => _dialogues.IndexOf(line))
                .ToList();
        }

        private void ReindexDialogueGroup(string eventId, string group)
        {
            if (string.IsNullOrWhiteSpace(group))
                return;

            ReindexOrderedLines(GetSortedDialogueGroup(eventId, group));
        }

        private static void ReindexOrderedLines(IReadOnlyList<DialogueDraft> lines)
        {
            for (int i = 0; i < lines.Count; i++)
                lines[i].LineOrder = i + 1;
        }

        private void ClearDialogueDragStateOnMouseUp()
        {
            Event current = Event.current;
            if (current == null || current.rawType != EventType.MouseUp)
                return;

            _draggedDialogue = null;
            _isDraggingDialogue = false;
        }

        private IEnumerable<NpcDraft> GetVisibleNpcs()
        {
            if (string.IsNullOrWhiteSpace(_npcSearch))
                return _npcs;

            return _npcs.Where(npc => npc == _selectedNpc
                                      || Contains(npc.NpcId, _npcSearch)
                                      || Contains(npc.DisplayName, _npcSearch)
                                      || Contains(npc.Race, _npcSearch)
                                      || Contains(npc.Role, _npcSearch));
        }

        private void BuildVisibleEventList()
        {
            _visibleEventIds.Clear();
            if (_selectedNpc == null)
                return;

            foreach (string eventId in GetEventIdsForNpc(_selectedNpc.NpcId))
                _visibleEventIds.Add(eventId);

            bool selectedEventIsValid = string.IsNullOrWhiteSpace(_selectedEventId) == false
                                        && _visibleEventIds.Any(eventId =>
                                            string.Equals(eventId, _selectedEventId, StringComparison.OrdinalIgnoreCase));
            if (selectedEventIsValid)
                return;

            string nextEventId = _visibleEventIds.FirstOrDefault() ?? string.Empty;
            if (string.Equals(_selectedEventId, nextEventId, StringComparison.OrdinalIgnoreCase))
                return;

            _selectedEventId = nextEventId;
            ClearDialogueTextFocus();
            _selectedDialogue = null;
            ResetDialogueTextSelection();
        }

        private List<string> GetEventIdsForNpc(string npcId)
        {
            HashSet<string> eventIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (VisitEventReference visitEvent in _visitEvents)
            {
                if (string.Equals(visitEvent.NpcId, npcId, StringComparison.OrdinalIgnoreCase)
                    && string.IsNullOrWhiteSpace(visitEvent.EventId) == false)
                {
                    eventIds.Add(visitEvent.EventId);
                }
            }

            foreach (DialogueDraft dialogue in _dialogues)
            {
                if (string.Equals(dialogue.Speaker, npcId, StringComparison.OrdinalIgnoreCase)
                    && string.IsNullOrWhiteSpace(dialogue.EventId) == false)
                {
                    eventIds.Add(dialogue.EventId);
                }
            }

            return eventIds.OrderBy(eventId => eventId, StringComparer.OrdinalIgnoreCase).ToList();
        }

        private void BuildVisibleDialogueList()
        {
            _visibleDialogues.Clear();
            if (_selectedNpc == null || string.IsNullOrWhiteSpace(_selectedEventId))
                return;

            foreach (DialogueDraft dialogue in _dialogues)
            {
                if (string.Equals(dialogue.EventId, _selectedEventId, StringComparison.OrdinalIgnoreCase) == false)
                    continue;

                if (string.IsNullOrWhiteSpace(_dialogueSearch) == false
                    && dialogue != _selectedDialogue
                    && Contains(dialogue.Group, _dialogueSearch) == false
                    && Contains(dialogue.Speaker, _dialogueSearch) == false
                    && Contains(dialogue.Text, _dialogueSearch) == false)
                {
                    continue;
                }

                _visibleDialogues.Add(dialogue);
            }

            _visibleDialogues.Sort(CompareDialogues);
        }

        private void LoadData(bool updateStatus)
        {
            _npcs.Clear();
            _dialogues.Clear();
            _visitEvents.Clear();
            _regionPools.Clear();
            _visitEventHeaders.Clear();

            foreach (Dictionary<string, string> row in ReadCsv(NpcCsvPath))
                _npcs.Add(NpcDraft.FromRow(row));

            foreach (Dictionary<string, string> row in ReadCsv(DialogueCsvPath))
                _dialogues.Add(DialogueDraft.FromRow(row));

            CsvTable visitEventTable = ReadCsvTable(VisitEventCsvPath);
            _visitEventHeaders.AddRange(EnsureHeaders(visitEventTable.Headers, DefaultVisitEventHeaders));
            foreach (Dictionary<string, string> row in visitEventTable.Rows)
                _visitEvents.Add(VisitEventReference.FromRow(row));

            foreach (Dictionary<string, string> row in ReadCsv(RegionPoolCsvPath))
                _regionPools.Add(RegionPoolDraft.FromRow(row));

            bool regionPoolsWereRepaired = EnsureRegionPoolsForExistingEvents();
            RebuildQuestionCategoryOptions();
            RebuildRegionOptions();

            _selectedNpc = _npcs.FirstOrDefault();
            ClearDialogueTextFocus();
            _selectedEventId = _selectedNpc != null
                ? GetEventIdsForNpc(_selectedNpc.NpcId).FirstOrDefault() ?? string.Empty
                : string.Empty;
            _newEventId = _selectedNpc != null ? GenerateUniqueEventId(_selectedNpc.NpcId) : string.Empty;
            _selectedDialogue = null;
            ResetDialogueTextSelection();
            _hasUnsavedChanges = regionPoolsWereRepaired;
            ClearValidationResults();
            CaptureWriteTimes();

            if (regionPoolsWereRepaired)
                _statusMessage = "방문 이벤트 기준으로 지역 풀이 갱신되었습니다. CSV 저장이 필요합니다.";
            else if (updateStatus)
                _statusMessage = "CSV 다시 불러오기 완료";
        }

        private bool EnsureRegionPoolsForExistingEvents()
        {
            List<string> beforeKeys = _regionPools
                .Select(pool => $"{pool.RegionId}|{pool.NpcId}")
                .OrderBy(key => key, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (string npcId in _visitEvents
                         .Select(visitEvent => visitEvent.NpcId)
                         .Where(npcId => string.IsNullOrWhiteSpace(npcId) == false)
                         .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                SyncRegionPoolsForNpcEvents(npcId);
            }

            List<string> afterKeys = _regionPools
                .Select(pool => $"{pool.RegionId}|{pool.NpcId}")
                .OrderBy(key => key, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (beforeKeys.SequenceEqual(afterKeys, StringComparer.OrdinalIgnoreCase))
                return false;

            RebuildRegionOptions();
            return true;
        }

        private void SaveData()
        {
            string validation = ValidateBeforeSave();
            if (string.IsNullOrWhiteSpace(validation) == false)
            {
                EditorUtility.DisplayDialog("저장 전 확인", validation, "확인");
                return;
            }

            WriteCsv(NpcCsvPath, NpcHeaders, _npcs.Select(npc => npc.ToRow()));
            WriteCsv(DialogueCsvPath, DialogueHeaders, _dialogues.OrderBy(line => line, DialogueComparer.Instance).Select(line => line.ToRow()));
            WriteCsv(VisitEventCsvPath, GetVisitEventHeadersForSave(), _visitEvents.Select(visitEvent => visitEvent.ToRow()));
            WriteCsv(RegionPoolCsvPath, RegionPoolHeaders, _regionPools.Select(pool => pool.ToRow()));
            AssetDatabase.Refresh();

            _hasUnsavedChanges = false;
            CaptureWriteTimes();
            _statusMessage = "CSV 저장 완료";
        }

        private string ValidateBeforeSave()
        {
            List<string> warnings = new List<string>();
            List<string> duplicateNpcIds = _npcs
                .Where(npc => string.IsNullOrWhiteSpace(npc.NpcId) == false)
                .GroupBy(npc => npc.NpcId.Trim(), StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToList();

            if (duplicateNpcIds.Count > 0)
                warnings.Add($"중복 NPC ID: {string.Join(", ", duplicateNpcIds)}");

            if (_npcs.Any(npc => string.IsNullOrWhiteSpace(npc.NpcId)))
                warnings.Add("NpcId가 비어 있는 NPC가 있습니다.");

            if (_dialogues.Any(line => string.IsNullOrWhiteSpace(line.EventId) || string.IsNullOrWhiteSpace(line.Group)))
                warnings.Add("EventId 또는 Group이 비어 있는 대사가 있습니다.");

            List<string> duplicateLines = _dialogues
                .GroupBy(line => $"{line.EventId}/{line.Group}/{line.LineOrder}", StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .Take(8)
                .ToList();

            if (duplicateLines.Count > 0)
                warnings.Add($"LineOrder 중복: {string.Join(", ", duplicateLines)}");

            return warnings.Count == 0 ? string.Empty : string.Join("\n", warnings);
        }

        private void TryReloadWithPrompt()
        {
            if (_hasUnsavedChanges
                && EditorUtility.DisplayDialog("CSV 다시 불러오기", "저장하지 않은 변경사항을 버리고 CSV를 다시 불러올까요?", "다시 불러오기", "취소") == false)
            {
                return;
            }

            LoadData(true);
        }

        private void CheckExternalChanges()
        {
            if (EditorApplication.timeSinceStartup - _lastExternalChangeCheck < 1.5d)
                return;

            _lastExternalChangeCheck = EditorApplication.timeSinceStartup;
            if (HasExternalWriteTimeChanged() == false)
                return;

            if (_hasUnsavedChanges)
            {
                _statusMessage = "외부 CSV 변경 감지됨. 현재 편집 중이라 자동 새로고침하지 않았습니다.";
                CaptureWriteTimes();
                Repaint();
                return;
            }

            LoadData(true);
            Repaint();
        }

        private bool HasExternalWriteTimeChanged()
        {
            return GetLastWriteTime(NpcCsvPath) != _npcLastWriteTime
                   || GetLastWriteTime(DialogueCsvPath) != _dialogueLastWriteTime
                   || GetLastWriteTime(VisitEventCsvPath) != _visitEventLastWriteTime
                   || GetLastWriteTime(RegionPoolCsvPath) != _regionPoolLastWriteTime;
        }

        private void CaptureWriteTimes()
        {
            _npcLastWriteTime = GetLastWriteTime(NpcCsvPath);
            _dialogueLastWriteTime = GetLastWriteTime(DialogueCsvPath);
            _visitEventLastWriteTime = GetLastWriteTime(VisitEventCsvPath);
            _regionPoolLastWriteTime = GetLastWriteTime(RegionPoolCsvPath);
        }

        private void MarkDirty(string message)
        {
            _hasUnsavedChanges = true;
            if (_hasValidationRun)
                _validationIsStale = true;
            _statusMessage = message;
        }

        private string GenerateUniqueNpcId()
        {
            const string baseId = "NewNpc";
            int index = 1;
            string candidate = baseId;
            while (_npcs.Any(npc => string.Equals(npc.NpcId, candidate, StringComparison.OrdinalIgnoreCase)))
            {
                index++;
                candidate = $"{baseId}{index}";
            }

            return candidate;
        }

        private string GenerateUniqueEventId(string npcId)
        {
            string safeNpcId = string.IsNullOrWhiteSpace(npcId) ? "Npc" : npcId.Trim();
            string baseId = $"{safeNpcId}_NewEvent";
            int index = 1;
            string candidate = baseId;
            while (_dialogues.Any(line => string.Equals(line.EventId, candidate, StringComparison.OrdinalIgnoreCase))
                   || _visitEvents.Any(visitEvent => string.Equals(visitEvent.EventId, candidate, StringComparison.OrdinalIgnoreCase)))
            {
                index++;
                candidate = $"{baseId}{index}";
            }

            return candidate;
        }

        private string GuessEventIdForNpc(string npcId)
        {
            VisitEventReference visitEvent = _visitEvents.FirstOrDefault(e => string.Equals(e.NpcId, npcId, StringComparison.OrdinalIgnoreCase));
            if (visitEvent != null && string.IsNullOrWhiteSpace(visitEvent.EventId) == false)
                return visitEvent.EventId;

            DialogueDraft dialogue = _dialogues.FirstOrDefault(d => string.Equals(d.Speaker, npcId, StringComparison.OrdinalIgnoreCase));
            if (dialogue != null && string.IsNullOrWhiteSpace(dialogue.EventId) == false)
                return dialogue.EventId;

            return $"{npcId}_NewEvent";
        }

        private static List<Dictionary<string, string>> ReadCsv(string assetPath)
        {
            return ReadCsvTable(assetPath).Rows;
        }

        private static CsvTable ReadCsvTable(string assetPath)
        {
            string fullPath = ToFullPath(assetPath);
            if (File.Exists(fullPath) == false)
                return new CsvTable(new List<string>(), new List<Dictionary<string, string>>());

            return ParseCsvTable(File.ReadAllText(fullPath, Encoding.UTF8));
        }

        private static List<Dictionary<string, string>> ParseCsv(string text)
        {
            return ParseCsvTable(text).Rows;
        }

        private static CsvTable ParseCsvTable(string text)
        {
            List<List<string>> rows = ParseCsvRows(text);
            List<Dictionary<string, string>> result = new List<Dictionary<string, string>>();
            List<string> headers = null;

            foreach (List<string> row in rows)
            {
                if (row.Count == 0 || row.All(string.IsNullOrWhiteSpace))
                    continue;

                if (headers == null)
                {
                    headers = row;
                    if (headers.Count > 0)
                        headers[0] = headers[0].TrimStart('\uFEFF');
                    continue;
                }

                Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < headers.Count; i++)
                    values[headers[i]] = i < row.Count ? row[i] : string.Empty;

                result.Add(values);
            }

            return new CsvTable(headers ?? new List<string>(), result);
        }

        private void EnsureVisitEventHeaders()
        {
            List<string> headers = EnsureHeaders(_visitEventHeaders, DefaultVisitEventHeaders);
            _visitEventHeaders.Clear();
            _visitEventHeaders.AddRange(headers);
        }

        private IEnumerable<string> GetVisitEventHeadersForSave()
        {
            return EnsureHeaders(_visitEventHeaders, DefaultVisitEventHeaders);
        }

        private static List<string> EnsureHeaders(IEnumerable<string> currentHeaders, IEnumerable<string> requiredHeaders)
        {
            List<string> headers = currentHeaders?
                .Where(header => string.IsNullOrWhiteSpace(header) == false)
                .Select(header => header.TrimStart('\uFEFF'))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? new List<string>();

            foreach (string requiredHeader in requiredHeaders)
            {
                if (headers.Any(header => string.Equals(header, requiredHeader, StringComparison.OrdinalIgnoreCase)))
                    continue;

                headers.Add(requiredHeader);
            }

            return headers;
        }

        private static List<List<string>> ParseCsvRows(string text)
        {
            List<List<string>> rows = new List<List<string>>();
            List<string> row = new List<string>();
            StringBuilder field = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < text.Length && text[i + 1] == '"')
                        {
                            field.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        field.Append(c);
                    }

                    continue;
                }

                switch (c)
                {
                    case '"':
                        inQuotes = true;
                        break;
                    case ',':
                        row.Add(field.ToString());
                        field.Clear();
                        break;
                    case '\r':
                        if (i + 1 < text.Length && text[i + 1] == '\n')
                            i++;
                        row.Add(field.ToString());
                        field.Clear();
                        rows.Add(new List<string>(row));
                        row.Clear();
                        break;
                    case '\n':
                        row.Add(field.ToString());
                        field.Clear();
                        rows.Add(new List<string>(row));
                        row.Clear();
                        break;
                    default:
                        field.Append(c);
                        break;
                }
            }

            if (field.Length > 0 || row.Count > 0)
            {
                row.Add(field.ToString());
                rows.Add(row);
            }

            return rows;
        }

        private static void WriteCsv(string assetPath, IEnumerable<string> headers, IEnumerable<Dictionary<string, string>> rows)
        {
            StringBuilder builder = new StringBuilder();
            string[] headerArray = headers.ToArray();
            builder.AppendLine(string.Join(",", headerArray));

            foreach (Dictionary<string, string> row in rows)
            {
                for (int i = 0; i < headerArray.Length; i++)
                {
                    if (i > 0)
                        builder.Append(',');

                    row.TryGetValue(headerArray[i], out string value);
                    builder.Append(EscapeCsv(value));
                }

                builder.AppendLine();
            }

            File.WriteAllText(ToFullPath(assetPath), builder.ToString(), new UTF8Encoding(false));
        }

        private static string EscapeCsv(string value)
        {
            value ??= string.Empty;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private static string ToFullPath(string assetPath)
        {
            return Path.Combine(Directory.GetCurrentDirectory(), assetPath);
        }

        private static DateTime GetLastWriteTime(string assetPath)
        {
            string fullPath = ToFullPath(assetPath);
            return File.Exists(fullPath) ? File.GetLastWriteTimeUtc(fullPath) : DateTime.MinValue;
        }

        private static bool Contains(string value, string search)
        {
            return string.IsNullOrWhiteSpace(value) == false
                   && value.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string StripBold(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Replace("**", string.Empty);
        }

        private static string ValueOrDash(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "-" : value;
        }

        private static int CompareDialogues(DialogueDraft left, DialogueDraft right)
        {
            int eventCompare = string.Compare(left.EventId, right.EventId, StringComparison.OrdinalIgnoreCase);
            if (eventCompare != 0)
                return eventCompare;

            int groupCompare = string.Compare(left.Group, right.Group, StringComparison.OrdinalIgnoreCase);
            if (groupCompare != 0)
                return groupCompare;

            int orderCompare = left.LineOrder.CompareTo(right.LineOrder);
            if (orderCompare != 0)
                return orderCompare;

            return string.Compare(left.Speaker, right.Speaker, StringComparison.OrdinalIgnoreCase);
        }

        private sealed class CsvTable
        {
            public CsvTable(List<string> headers, List<Dictionary<string, string>> rows)
            {
                Headers = headers ?? new List<string>();
                Rows = rows ?? new List<Dictionary<string, string>>();
            }

            public List<string> Headers { get; }
            public List<Dictionary<string, string>> Rows { get; }
        }

        private enum CsvValidationSeverity
        {
            Info,
            Warning,
            Error
        }

        private readonly struct DropdownOption
        {
            public DropdownOption(string value, string displayName)
            {
                Value = value ?? string.Empty;
                DisplayName = string.IsNullOrWhiteSpace(displayName) ? Value : displayName;
            }

            public string Value { get; }
            public string DisplayName { get; }
            public string DisplayText => string.IsNullOrWhiteSpace(DisplayName) ? Value : DisplayName;
        }

        private sealed class NpcCsvValidationIssue
        {
            public NpcCsvValidationIssue(
                CsvValidationSeverity severity,
                string code,
                string message,
                string npcId,
                string eventId,
                string group,
                int lineOrder,
                string speaker)
            {
                Severity = severity;
                Code = code ?? string.Empty;
                Message = message ?? string.Empty;
                NpcId = npcId ?? string.Empty;
                EventId = eventId ?? string.Empty;
                Group = group ?? string.Empty;
                LineOrder = lineOrder;
                Speaker = speaker ?? string.Empty;
            }

            public CsvValidationSeverity Severity { get; }
            public string Code { get; }
            public string Message { get; }
            public string NpcId { get; }
            public string EventId { get; }
            public string Group { get; }
            public int LineOrder { get; }
            public string Speaker { get; }

            public string BuildDisplayText()
            {
                List<string> context = new List<string>();
                if (string.IsNullOrWhiteSpace(NpcId) == false)
                    context.Add($"npc={NpcId}");
                if (string.IsNullOrWhiteSpace(EventId) == false)
                    context.Add($"event={EventId}");
                if (string.IsNullOrWhiteSpace(Group) == false)
                    context.Add($"group={Group}");
                if (LineOrder > 0)
                    context.Add($"line={LineOrder}");
                if (string.IsNullOrWhiteSpace(Speaker) == false)
                    context.Add($"speaker={Speaker}");

                string suffix = context.Count == 0 ? string.Empty : $" ({string.Join(", ", context)})";
                return $"{Code}: {Message}{suffix}";
            }
        }

        [Serializable]
        private sealed class NpcDraft
        {
            public string NpcId;
            public string DisplayName;
            public string Race;
            public string Role;
            public string PreferredTags;
            public string PreferredFoodTypes;
            public string AvoidTags;
            public string Notes;
            public bool RequestAvailable;
            public int RequestUnlockLevel;
            public string RequestUnlockEvent;

            public static NpcDraft FromRow(IReadOnlyDictionary<string, string> row)
            {
                return new NpcDraft
                {
                    NpcId = Get(row, "NpcId"),
                    DisplayName = Get(row, "DisplayName"),
                    Race = Get(row, "Race"),
                    Role = Get(row, "Role"),
                    PreferredTags = Get(row, "PreferredTags"),
                    PreferredFoodTypes = Get(row, "PreferredFoodTypes"),
                    AvoidTags = Get(row, "AvoidTags"),
                    Notes = Get(row, "Notes"),
                    RequestAvailable = bool.TryParse(Get(row, "RequestAvailable"), out bool available) && available,
                    RequestUnlockLevel = int.TryParse(Get(row, "RequestUnlockLevel"), out int level) ? level : 5,
                    RequestUnlockEvent = Get(row, "RequestUnlockEvent")
                };
            }

            public Dictionary<string, string> ToRow()
            {
                return new Dictionary<string, string>
                {
                    ["NpcId"] = NpcId,
                    ["DisplayName"] = DisplayName,
                    ["Race"] = Race,
                    ["Role"] = Role,
                    ["PreferredTags"] = PreferredTags,
                    ["PreferredFoodTypes"] = PreferredFoodTypes,
                    ["AvoidTags"] = AvoidTags,
                    ["Notes"] = Notes,
                    ["RequestAvailable"] = RequestAvailable ? "TRUE" : "FALSE",
                    ["RequestUnlockLevel"] = RequestUnlockLevel.ToString(),
                    ["RequestUnlockEvent"] = RequestUnlockEvent
                };
            }
        }

        [Serializable]
        private sealed class DialogueDraft
        {
            public string EventId;
            public string Group;
            public string QuestionCategory;
            public int LineOrder;
            public string Speaker;
            public string Text;

            public static DialogueDraft FromRow(IReadOnlyDictionary<string, string> row)
            {
                return new DialogueDraft
                {
                    EventId = Get(row, "EventId"),
                    Group = Get(row, "Group"),
                    QuestionCategory = Get(row, "QuestionCategory"),
                    LineOrder = int.TryParse(Get(row, "LineOrder"), out int order) ? order : 0,
                    Speaker = Get(row, "Speaker"),
                    Text = Get(row, "Text")
                };
            }

            public Dictionary<string, string> ToRow()
            {
                return new Dictionary<string, string>
                {
                    ["EventId"] = EventId,
                    ["Group"] = Group,
                    ["QuestionCategory"] = QuestionCategory,
                    ["LineOrder"] = LineOrder.ToString(),
                    ["Speaker"] = Speaker,
                    ["Text"] = Text
                };
            }
        }

        private sealed class VisitEventReference
        {
            private readonly Dictionary<string, string> _rawValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            public string EventId;
            public string NpcId;
            public string RegionId;
            public string StartGroups;
            public int QuestionLimit;
            public string AvailableQuestionCategories;
            public string EventType;
            public int Priority;
            public string RepeatMode;
            public int CooldownDays;
            public int RequiredNpcVisits;
            public int RequiredAffinity;
            public int RequiredCorrectCount;
            public string RequiredLastResult;
            public string RequiredEventIds;
            public string SequenceGroup;
            public int SequenceIndex;
            public string CorrectRecipeId;
            public string AllowedFoodTypes;
            public string RequiredTags;
            public string PreferredTags;
            public string AvoidTags;
            public string DisgustingTags;
            public string RequiredRequestState;
            public string BlockedAtRequestState;
            public string RequestStateAfterEncounter;
            public string RequestSuccessResults;
            public string RequestStateAfterSuccessResult;

            public static VisitEventReference CreateDefault(string eventId, string npcId)
            {
                VisitEventReference visitEvent = new VisitEventReference
                {
                    EventId = eventId,
                    NpcId = npcId,
                    RegionId = "*",
                    StartGroups = "Intro|OrderIntent",
                    QuestionLimit = 2,
                    AvailableQuestionCategories = "Taste|TextureTemp|Condition|Avoid",
                    EventType = "Normal",
                    Priority = 0,
                    RepeatMode = "Cycle",
                    CooldownDays = 1,
                    RequiredNpcVisits = 0,
                    RequiredAffinity = 0,
                    RequiredCorrectCount = 0,
                    RequiredLastResult = string.Empty,
                    RequiredEventIds = string.Empty,
                    SequenceGroup = string.Empty,
                    SequenceIndex = 0,
                    CorrectRecipeId = string.Empty,
                    AllowedFoodTypes = string.Empty,
                    RequiredTags = string.Empty,
                    PreferredTags = string.Empty,
                    AvoidTags = string.Empty,
                    DisgustingTags = string.Empty,
                    RequiredRequestState = string.Empty,
                    BlockedAtRequestState = string.Empty,
                    RequestStateAfterEncounter = string.Empty,
                    RequestSuccessResults = string.Empty,
                    RequestStateAfterSuccessResult = string.Empty
                };
                visitEvent.SyncRawValues();
                return visitEvent;
            }

            public static VisitEventReference FromRow(IReadOnlyDictionary<string, string> row)
            {
                VisitEventReference visitEvent = new VisitEventReference
                {
                    EventId = Get(row, "EventId"),
                    NpcId = Get(row, "NpcId"),
                    RegionId = Get(row, "RegionId"),
                    StartGroups = Get(row, "StartGroups"),
                    QuestionLimit = int.TryParse(Get(row, "QuestionLimit"), out int questionLimit) ? questionLimit : 0,
                    AvailableQuestionCategories = Get(row, "AvailableQuestionCategories"),
                    EventType = Get(row, "EventType"),
                    Priority = int.TryParse(Get(row, "Priority"), out int priority) ? priority : 0,
                    RepeatMode = Get(row, "RepeatMode"),
                    CooldownDays = int.TryParse(Get(row, "CooldownDays"), out int cooldownDays) ? cooldownDays : 0,
                    RequiredNpcVisits = int.TryParse(Get(row, "RequiredNpcVisits"), out int requiredNpcVisits) ? requiredNpcVisits : 0,
                    RequiredAffinity = int.TryParse(Get(row, "RequiredAffinity"), out int requiredAffinity) ? requiredAffinity : 0,
                    RequiredCorrectCount = int.TryParse(Get(row, "RequiredCorrectCount"), out int requiredCorrectCount) ? requiredCorrectCount : 0,
                    RequiredLastResult = Get(row, "RequiredLastResult"),
                    RequiredEventIds = Get(row, "RequiredEventIds"),
                    SequenceGroup = Get(row, "SequenceGroup"),
                    SequenceIndex = int.TryParse(Get(row, "SequenceIndex"), out int sequenceIndex) ? sequenceIndex : 0,
                    CorrectRecipeId = Get(row, "CorrectRecipeId"),
                    AllowedFoodTypes = Get(row, "AllowedFoodTypes"),
                    RequiredTags = Get(row, "RequiredTags"),
                    PreferredTags = Get(row, "PreferredTags"),
                    AvoidTags = Get(row, "AvoidTags"),
                    DisgustingTags = Get(row, "DisgustingTags"),
                    RequiredRequestState = Get(row, "RequiredRequestState"),
                    BlockedAtRequestState = Get(row, "BlockedAtRequestState"),
                    RequestStateAfterEncounter = Get(row, "RequestStateAfterEncounter"),
                    RequestSuccessResults = Get(row, "RequestSuccessResults"),
                    RequestStateAfterSuccessResult = Get(row, "RequestStateAfterSuccessResult")
                };

                if (row != null)
                {
                    foreach (KeyValuePair<string, string> pair in row)
                        visitEvent._rawValues[pair.Key] = pair.Value ?? string.Empty;
                }

                visitEvent.SyncRawValues();
                return visitEvent;
            }

            public Dictionary<string, string> ToRow()
            {
                SyncRawValues();
                return new Dictionary<string, string>(_rawValues, StringComparer.OrdinalIgnoreCase);
            }

            public void SetRaw(string key, string value)
            {
                if (string.IsNullOrWhiteSpace(key))
                    return;

                _rawValues[key] = value ?? string.Empty;
            }

            private void SyncRawValues()
            {
                SetRaw("EventId", EventId);
                SetRaw("NpcId", NpcId);
                SetRaw("RegionId", RegionId);
                SetRaw("StartGroups", StartGroups);
                SetRaw("QuestionLimit", QuestionLimit.ToString());
                SetRaw("AvailableQuestionCategories", AvailableQuestionCategories);
                SetRaw("EventType", EventType);
                SetRaw("Priority", Priority.ToString());
                SetRaw("RepeatMode", RepeatMode);
                SetRaw("CooldownDays", CooldownDays.ToString());
                SetRaw("RequiredNpcVisits", RequiredNpcVisits.ToString());
                SetRaw("RequiredAffinity", RequiredAffinity.ToString());
                SetRaw("RequiredCorrectCount", RequiredCorrectCount.ToString());
                SetRaw("RequiredLastResult", RequiredLastResult);
                SetRaw("RequiredEventIds", RequiredEventIds);
                SetRaw("SequenceGroup", SequenceGroup);
                SetRaw("SequenceIndex", SequenceIndex.ToString());
                SetRaw("CorrectRecipeId", CorrectRecipeId);
                SetRaw("AllowedFoodTypes", AllowedFoodTypes);
                SetRaw("RequiredTags", RequiredTags);
                SetRaw("PreferredTags", PreferredTags);
                SetRaw("AvoidTags", AvoidTags);
                SetRaw("DisgustingTags", DisgustingTags);
                SetRaw("RequiredRequestState", RequiredRequestState);
                SetRaw("BlockedAtRequestState", BlockedAtRequestState);
                SetRaw("RequestStateAfterEncounter", RequestStateAfterEncounter);
                SetRaw("RequestSuccessResults", RequestSuccessResults);
                SetRaw("RequestStateAfterSuccessResult", RequestStateAfterSuccessResult);
            }
        }

        private sealed class RegionPoolDraft
        {
            private readonly Dictionary<string, string> _rawValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            public string RegionId;
            public string NpcId;
            public int Weight;
            public int MinDay;
            public int CooldownDays;
            public string PoolType;
            public string Condition;

            public static RegionPoolDraft CreateDefault(string regionId, string npcId)
            {
                RegionPoolDraft pool = new RegionPoolDraft
                {
                    RegionId = regionId ?? string.Empty,
                    NpcId = npcId ?? string.Empty,
                    Weight = 80,
                    MinDay = 1,
                    CooldownDays = 1,
                    PoolType = "Normal",
                    Condition = string.Empty
                };
                pool.SyncRawValues();
                return pool;
            }

            public static RegionPoolDraft FromRow(IReadOnlyDictionary<string, string> row)
            {
                RegionPoolDraft pool = new RegionPoolDraft
                {
                    RegionId = Get(row, "RegionId"),
                    NpcId = Get(row, "NpcId"),
                    Weight = int.TryParse(Get(row, "Weight"), out int weight) ? weight : 1,
                    MinDay = int.TryParse(Get(row, "MinDay"), out int minDay) ? minDay : 1,
                    CooldownDays = int.TryParse(Get(row, "CooldownDays"), out int cooldownDays) ? cooldownDays : 1,
                    PoolType = string.IsNullOrWhiteSpace(Get(row, "PoolType")) ? "Normal" : Get(row, "PoolType"),
                    Condition = Get(row, "Condition")
                };

                if (row != null)
                {
                    foreach (KeyValuePair<string, string> pair in row)
                        pool._rawValues[pair.Key] = pair.Value ?? string.Empty;
                }

                pool.SyncRawValues();
                return pool;
            }

            public Dictionary<string, string> ToRow()
            {
                SyncRawValues();
                return new Dictionary<string, string>(_rawValues, StringComparer.OrdinalIgnoreCase);
            }

            public void SyncRawValues()
            {
                _rawValues["RegionId"] = RegionId ?? string.Empty;
                _rawValues["NpcId"] = NpcId ?? string.Empty;
                _rawValues["Weight"] = Mathf.Max(1, Weight).ToString();
                _rawValues["MinDay"] = Mathf.Max(1, MinDay).ToString();
                _rawValues["CooldownDays"] = Mathf.Max(0, CooldownDays).ToString();
                _rawValues["PoolType"] = string.IsNullOrWhiteSpace(PoolType) ? "Normal" : PoolType;
                _rawValues["Condition"] = Condition ?? string.Empty;
            }
        }

        private sealed class DialogueComparer : IComparer<DialogueDraft>
        {
            public static readonly DialogueComparer Instance = new DialogueComparer();

            public int Compare(DialogueDraft x, DialogueDraft y)
            {
                if (ReferenceEquals(x, y))
                    return 0;

                if (x == null)
                    return -1;

                if (y == null)
                    return 1;

                return CompareDialogues(x, y);
            }
        }

        private static string Get(IReadOnlyDictionary<string, string> row, string key)
        {
            return row != null && row.TryGetValue(key, out string value) ? value ?? string.Empty : string.Empty;
        }
    }
}
