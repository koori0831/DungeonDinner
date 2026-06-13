using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
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

        private readonly List<NpcDraft> _npcs = new List<NpcDraft>();
        private readonly List<DialogueDraft> _dialogues = new List<DialogueDraft>();
        private readonly List<VisitEventReference> _visitEvents = new List<VisitEventReference>();
        private readonly List<NpcCsvValidationIssue> _validationIssues = new List<NpcCsvValidationIssue>();
        private readonly List<string> _visibleEventIds = new List<string>();
        private readonly List<DialogueDraft> _visibleDialogues = new List<DialogueDraft>();
        private readonly List<string> _visitEventHeaders = new List<string>();

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
        private float _npcPanelWidth = 240f;
        private float _eventPanelWidth = 260f;
        private float _dialoguePanelWidth = 340f;
        private string _activeResizeHandle;
        private bool _hasUnsavedChanges;
        private string _statusMessage = string.Empty;
        private DateTime _npcLastWriteTime;
        private DateTime _dialogueLastWriteTime;
        private DateTime _visitEventLastWriteTime;
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
        }

        private void OnDisable()
        {
            EditorApplication.update -= CheckExternalChanges;
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
            DrawResizeHandle("event-dialogue", ref _eventPanelWidth, 180f, 440f);
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
            GUILayout.Label($"NPCs: {_npcs.Count}", GUILayout.Width(90f));
            GUILayout.Label($"Dialogue: {_dialogues.Count}", GUILayout.Width(120f));

            GUI.enabled = _hasUnsavedChanges;
            if (GUILayout.Button("Save CSV", GUILayout.Width(100f)))
                SaveData();
            GUI.enabled = true;

            if (GUILayout.Button("Reload", GUILayout.Width(90f)))
                TryReloadWithPrompt();

            if (GUILayout.Button("Validate", GUILayout.Width(90f)))
                RunValidation();

            GUILayout.FlexibleSpace();

            GUIStyle statusStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleRight,
                wordWrap = false
            };
            GUILayout.Label(_hasUnsavedChanges ? "Unsaved changes" : "Saved", statusStyle, GUILayout.Width(140f));
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
            _showValidationPanel = EditorGUILayout.Foldout(_showValidationPanel, "Validation Results", true);
            GUILayout.Label(GetValidationSummaryText(), EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Clear", GUILayout.Width(70f)))
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
                    EditorGUILayout.HelpBox("No validation issues found.", MessageType.Info);
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
            _statusMessage = $"Validation complete. Errors {errors}, Warnings {warnings}, Infos {infos}.";
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
                    $"Duplicate NPC ID '{duplicateGroup.Key}'.",
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
                    $"Duplicate VisitEvents EventId '{duplicateGroup.Key}'.",
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
                        "VisitEvents row has an empty EventId.",
                        visitEvent.NpcId);
                }

                if (string.IsNullOrWhiteSpace(visitEvent.NpcId))
                {
                    AddValidationIssue(
                        issues,
                        CsvValidationSeverity.Error,
                        "VisitEventNpcIdEmpty",
                        "VisitEvents row has an empty NpcId.",
                        eventId: visitEvent.EventId);
                    continue;
                }

                if (npcById.ContainsKey(visitEvent.NpcId))
                    continue;

                AddValidationIssue(
                    issues,
                    CsvValidationSeverity.Error,
                    "VisitEventNpcMissing",
                    $"Visit event references unknown NPC '{visitEvent.NpcId}'.",
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
                        "Question category row has an empty CategoryId.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(dialogueGroup))
                {
                    AddValidationIssue(
                        issues,
                        CsvValidationSeverity.Warning,
                        "QuestionDialogueGroupEmpty",
                        $"Question category '{categoryId}' has an empty DialogueGroup.");
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
                        "NPC row has an empty NpcId.");
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
                        $"NPC request unlock event '{npc.RequestUnlockEvent}' does not exist.",
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
                        "Dialogue line has an empty EventId.",
                        dialogue);
                }

                if (string.IsNullOrWhiteSpace(dialogue.Group))
                {
                    AddDialogueValidationIssue(
                        issues,
                        CsvValidationSeverity.Error,
                        "DialogueGroupEmpty",
                        "Dialogue line has an empty Group.",
                        dialogue);
                }

                if (dialogue.LineOrder <= 0)
                {
                    AddDialogueValidationIssue(
                        issues,
                        CsvValidationSeverity.Error,
                        "DialogueLineOrderInvalid",
                        "Dialogue line order must be greater than 0.",
                        dialogue);
                }

                if (string.IsNullOrWhiteSpace(dialogue.Speaker))
                {
                    AddDialogueValidationIssue(
                        issues,
                        CsvValidationSeverity.Error,
                        "DialogueSpeakerEmpty",
                        "Dialogue line has an empty Speaker.",
                        dialogue);
                }
                else if (IsPlayerSpeaker(dialogue.Speaker) == false && npcById.ContainsKey(dialogue.Speaker) == false)
                {
                    AddDialogueValidationIssue(
                        issues,
                        CsvValidationSeverity.Error,
                        "DialogueSpeakerMissing",
                        $"Dialogue line references unknown speaker '{dialogue.Speaker}'.",
                        dialogue);
                }

                if (string.IsNullOrWhiteSpace(dialogue.Text))
                {
                    AddDialogueValidationIssue(
                        issues,
                        CsvValidationSeverity.Warning,
                        "DialogueTextEmpty",
                        "Dialogue line has empty Text.",
                        dialogue);
                }

                if (CountOccurrences(dialogue.Text, BoldMarker) % 2 != 0)
                {
                    AddDialogueValidationIssue(
                        issues,
                        CsvValidationSeverity.Error,
                        "BoldMarkerUnbalanced",
                        "Dialogue Text has an unmatched bold marker '**'.",
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
                        $"Dialogue event '{eventGroup.Key}' is not linked from VisitEvents.csv.",
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
                    $"Duplicate LineOrder {duplicateGroup.Key} in {first.EventId}/{first.Group}.",
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
                    $"LineOrder gap in {first.EventId}/{first.Group}. Expected {expected}, found {orders[i]}.",
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
                        "Visit event has no dialogue lines.",
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
                        "Visit event requires itself.",
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
                    $"Required event '{requiredEventId}' does not exist.",
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
                    $"Start group '{startGroup}' is missing from DialogueLines.csv.",
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
                    "Visit event has QuestionLimit but no AvailableQuestionCategories.",
                    visitEvent.NpcId,
                    visitEvent.EventId);
            }

            if (visitEvent.QuestionLimit > availableQuestionCategories.Count)
            {
                AddValidationIssue(
                    issues,
                    CsvValidationSeverity.Warning,
                    "QuestionLimitExceedsCategories",
                    $"QuestionLimit {visitEvent.QuestionLimit} is greater than available category count {availableQuestionCategories.Count}.",
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
                        $"Question category '{categoryId}' does not exist.",
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
                    $"Question group '{dialogueGroup}' for category '{categoryId}' is missing.",
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
                "Result_Wrong",
                "Result_Disgusting"
            };

            foreach (string group in requiredGroups)
            {
                if (HasDialogueGroup(visitEvent.EventId, group))
                    continue;

                AddValidationIssue(
                    issues,
                    CsvValidationSeverity.Warning,
                    "ResultDialogueGroupMissing",
                    $"Result group '{group}' is missing.",
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
                    "First, Sequence, and Request events usually should use RepeatMode Once.",
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
                        "Sequence event has an empty SequenceGroup.",
                        visitEvent.NpcId,
                        visitEvent.EventId);
                }

                if (visitEvent.SequenceIndex <= 0)
                {
                    AddValidationIssue(
                        issues,
                        CsvValidationSeverity.Warning,
                        "SequenceIndexInvalid",
                        "Sequence event needs SequenceIndex greater than 0.",
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
                        $"Duplicate SequenceIndex {duplicateIndex.Key} in group '{group.Key}'.",
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
                    "NPC has RequestAvailable but no Request visit event.",
                    npc.NpcId);
            }
        }

        private void ValidateRegionPoolRows(
            List<NpcCsvValidationIssue> issues,
            IReadOnlyDictionary<string, NpcDraft> npcById,
            IReadOnlyDictionary<string, VisitEventReference> visitEventById)
        {
            List<Dictionary<string, string>> rows = ReadCsv(RegionPoolCsvPath);
            foreach (Dictionary<string, string> row in rows)
            {
                string regionId = Get(row, "RegionId");
                string npcId = Get(row, "NpcId");
                string weightText = Get(row, "Weight");

                if (string.IsNullOrWhiteSpace(npcId))
                {
                    AddValidationIssue(
                        issues,
                        CsvValidationSeverity.Error,
                        "RegionPoolNpcIdEmpty",
                        "Region pool row has an empty NpcId.");
                    continue;
                }

                if (npcById.ContainsKey(npcId) == false)
                {
                    AddValidationIssue(
                        issues,
                        CsvValidationSeverity.Error,
                        "RegionPoolNpcMissing",
                        $"Region pool references unknown NPC '{npcId}'.",
                        npcId);
                }

                if (int.TryParse(weightText, out int weight) && weight <= 0)
                {
                    AddValidationIssue(
                        issues,
                        CsvValidationSeverity.Warning,
                        "RegionPoolWeightInvalid",
                        "Region pool Weight should be greater than 0.",
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
                        $"NPC '{npcId}' is in region '{regionId}' but has no matching visit event.",
                        npcId);
                }
            }

            foreach (IGrouping<string, Dictionary<string, string>> duplicateGroup in rows
                         .Where(row => string.IsNullOrWhiteSpace(Get(row, "RegionId")) == false
                                       && string.IsNullOrWhiteSpace(Get(row, "NpcId")) == false)
                         .GroupBy(row => $"{Get(row, "RegionId").Trim()}|{Get(row, "NpcId").Trim()}", StringComparer.OrdinalIgnoreCase)
                         .Where(group => group.Count() > 1))
            {
                Dictionary<string, string> first = duplicateGroup.First();
                AddValidationIssue(
                    issues,
                    CsvValidationSeverity.Warning,
                    "RegionPoolDuplicateNpc",
                    $"NPC '{Get(first, "NpcId")}' appears more than once in region '{Get(first, "RegionId")}'.",
                    Get(first, "NpcId"));
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

            _statusMessage = $"Selected validation issue: {issue.Code}";
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
            string stale = _validationIsStale ? " (stale)" : string.Empty;
            return $"Validation{stale}: {errors} errors, {warnings} warnings, {infos} infos";
        }

        private static string GetValidationSeverityLabel(CsvValidationSeverity severity)
        {
            switch (severity)
            {
                case CsvValidationSeverity.Error:
                    return "ERROR";
                case CsvValidationSeverity.Warning:
                    return "WARN";
                default:
                    return "INFO";
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
            _npcSearch = EditorGUILayout.TextField("Search", _npcSearch);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add NPC"))
                AddNpc();
            GUI.enabled = _selectedNpc != null;
            if (GUILayout.Button("Delete"))
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
            _showNpcDetail = EditorGUILayout.Foldout(_showNpcDetail, "Selected NPC Detail", true);
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
            DrawNpcTextField("PreferredTags", ref _selectedNpc.PreferredTags);
            DrawNpcTextField("PreferredFoodTypes", ref _selectedNpc.PreferredFoodTypes);
            DrawNpcTextField("AvoidTags", ref _selectedNpc.AvoidTags);
            DrawNpcTextArea("Notes", ref _selectedNpc.Notes, 52f);

            EditorGUILayout.Space(6f);
            GUILayout.Label("Request", EditorStyles.boldLabel);
            DrawNpcBoolField("RequestAvailable", ref _selectedNpc.RequestAvailable);
            DrawNpcIntField("RequestUnlockLevel", ref _selectedNpc.RequestUnlockLevel);
            DrawNpcTextField("RequestUnlockEvent", ref _selectedNpc.RequestUnlockEvent);

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
            DrawNpcTextField("PreferredTags", ref _selectedNpc.PreferredTags);
            DrawNpcTextField("PreferredFoodTypes", ref _selectedNpc.PreferredFoodTypes);
            DrawNpcTextField("AvoidTags", ref _selectedNpc.AvoidTags);
            DrawNpcTextArea("Notes", ref _selectedNpc.Notes, 58f);

            EditorGUILayout.Space(8f);
            GUILayout.Label("Request", EditorStyles.boldLabel);
            DrawNpcBoolField("RequestAvailable", ref _selectedNpc.RequestAvailable);
            DrawNpcIntField("RequestUnlockLevel", ref _selectedNpc.RequestUnlockLevel);
            DrawNpcTextField("RequestUnlockEvent", ref _selectedNpc.RequestUnlockEvent);

            EditorGUILayout.Space(8f);
            DrawNpcReferenceSummary(_selectedNpc.NpcId);
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawEventListPanel(float width)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(width), GUILayout.ExpandHeight(true));
            GUILayout.Label("Event", EditorStyles.boldLabel);

            if (_selectedNpc == null)
            {
                EditorGUILayout.HelpBox("NPC를 먼저 선택하세요.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            DrawSelectedNpcSummary();
            BuildVisibleEventList();
            GUILayout.Label($"{_selectedNpc.NpcId} / Events: {_visibleEventIds.Count}", EditorStyles.miniLabel);

            EditorGUILayout.BeginHorizontal();
            _newEventId = EditorGUILayout.TextField(_newEventId);
            if (GUILayout.Button("Add Event", GUILayout.Width(82f)))
                AddEvent();
            GUI.enabled = string.IsNullOrWhiteSpace(_selectedEventId) == false;
            if (GUILayout.Button("Delete", GUILayout.Width(60f)))
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
                string label = $"{eventId}\nLines {lineCount} / NPC {npcLineCount}";
                if (GUILayout.Toggle(selected, label, "Button") != selected)
                    SelectEvent(eventId);
            }
            EditorGUILayout.EndScrollView();

            if (_visibleEventIds.Count == 0)
                EditorGUILayout.HelpBox("이 NPC와 연결된 이벤트가 없습니다. Add Line으로 새 이벤트 대사를 만들 수 있습니다.", MessageType.Info);

            EditorGUILayout.Space(6f);
            DrawVisitEventDetailFields();
            EditorGUILayout.EndVertical();
        }

        private void DrawVisitEventDetailFields()
        {
            _showVisitEventDetail = EditorGUILayout.Foldout(_showVisitEventDetail, "Selected Visit Event", true);
            if (_showVisitEventDetail == false)
                return;

            VisitEventReference visitEvent = GetSelectedVisitEvent();
            if (visitEvent == null)
            {
                EditorGUILayout.HelpBox("VisitEvents.csv에 연결되지 않은 이벤트입니다. Create Link를 누르면 기본 이벤트 메타를 생성합니다.", MessageType.Warning);
                GUI.enabled = string.IsNullOrWhiteSpace(_selectedEventId) == false && _selectedNpc != null;
                if (GUILayout.Button("Create VisitEvent Link"))
                    CreateVisitEventForSelectedEvent();
                GUI.enabled = true;
                return;
            }

            _visitEventDetailScroll = EditorGUILayout.BeginScrollView(_visitEventDetailScroll, GUILayout.MinHeight(140f), GUILayout.MaxHeight(260f));
            DrawVisitEventIdField(visitEvent);
            DrawVisitEventTextField("NpcId", visitEvent, "NpcId", ref visitEvent.NpcId);
            DrawVisitEventTextField("RegionId", visitEvent, "RegionId", ref visitEvent.RegionId);
            DrawVisitEventTextField("StartGroups", visitEvent, "StartGroups", ref visitEvent.StartGroups);
            DrawVisitEventIntField("QuestionLimit", visitEvent, "QuestionLimit", ref visitEvent.QuestionLimit);
            DrawVisitEventTextField("AvailableQuestionCategories", visitEvent, "AvailableQuestionCategories", ref visitEvent.AvailableQuestionCategories);
            DrawVisitEventTextField("EventType", visitEvent, "EventType", ref visitEvent.EventType);
            DrawVisitEventIntField("Priority", visitEvent, "Priority", ref visitEvent.Priority);
            DrawVisitEventTextField("RepeatMode", visitEvent, "RepeatMode", ref visitEvent.RepeatMode);
            DrawVisitEventIntField("CooldownDays", visitEvent, "CooldownDays", ref visitEvent.CooldownDays);
            DrawVisitEventIntField("RequiredNpcVisits", visitEvent, "RequiredNpcVisits", ref visitEvent.RequiredNpcVisits);
            DrawVisitEventIntField("RequiredAffinity", visitEvent, "RequiredAffinity", ref visitEvent.RequiredAffinity);
            DrawVisitEventIntField("RequiredCorrectCount", visitEvent, "RequiredCorrectCount", ref visitEvent.RequiredCorrectCount);
            DrawVisitEventTextField("RequiredLastResult", visitEvent, "RequiredLastResult", ref visitEvent.RequiredLastResult);
            DrawVisitEventTextField("RequiredEventIds", visitEvent, "RequiredEventIds", ref visitEvent.RequiredEventIds);
            DrawVisitEventTextField("SequenceGroup", visitEvent, "SequenceGroup", ref visitEvent.SequenceGroup);
            DrawVisitEventIntField("SequenceIndex", visitEvent, "SequenceIndex", ref visitEvent.SequenceIndex);

            EditorGUILayout.Space(6f);
            GUILayout.Label("Order Contract", EditorStyles.boldLabel);
            DrawVisitEventTextField("CorrectRecipeId", visitEvent, "CorrectRecipeId", ref visitEvent.CorrectRecipeId);
            DrawVisitEventTextField("AllowedFoodTypes", visitEvent, "AllowedFoodTypes", ref visitEvent.AllowedFoodTypes);
            DrawVisitEventTextField("RequiredTags", visitEvent, "RequiredTags", ref visitEvent.RequiredTags);
            DrawVisitEventTextField("PreferredTags", visitEvent, "PreferredTags", ref visitEvent.PreferredTags);
            DrawVisitEventTextField("AvoidTags", visitEvent, "AvoidTags", ref visitEvent.AvoidTags);
            DrawVisitEventTextField("DisgustingTags", visitEvent, "DisgustingTags", ref visitEvent.DisgustingTags);

            EditorGUILayout.Space(6f);
            GUILayout.Label("Request State", EditorStyles.boldLabel);
            DrawVisitEventTextField("RequiredRequestState", visitEvent, "RequiredRequestState", ref visitEvent.RequiredRequestState);
            DrawVisitEventTextField("BlockedAtRequestState", visitEvent, "BlockedAtRequestState", ref visitEvent.BlockedAtRequestState);
            DrawVisitEventTextField("RequestStateAfterEncounter", visitEvent, "RequestStateAfterEncounter", ref visitEvent.RequestStateAfterEncounter);
            DrawVisitEventTextField("RequestSuccessResults", visitEvent, "RequestSuccessResults", ref visitEvent.RequestSuccessResults);
            DrawVisitEventTextField("RequestStateAfterSuccessResult", visitEvent, "RequestStateAfterSuccessResult", ref visitEvent.RequestStateAfterSuccessResult);
            EditorGUILayout.EndScrollView();
        }

        private void DrawDialogueListPanel(float width)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(width), GUILayout.ExpandHeight(true));
            GUILayout.Label("관련 대사", EditorStyles.boldLabel);
            _dialogueSearch = EditorGUILayout.TextField("Search", _dialogueSearch);

            EditorGUILayout.BeginHorizontal();
            GUI.enabled = _selectedNpc != null;
            if (GUILayout.Button("Add Line"))
                AddDialogueLine();
            GUI.enabled = _selectedDialogue != null;
            if (GUILayout.Button("Delete"))
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
            GUILayout.Label($"{_selectedEventId} / Shown: {_visibleDialogues.Count}", EditorStyles.miniLabel);

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
                EditorGUILayout.HelpBox("대사를 선택하거나 Add Line으로 새 대사를 추가하세요.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            _dialogueDetailScroll = EditorGUILayout.BeginScrollView(_dialogueDetailScroll);
            DrawDialogueTextField("EventId", ref _selectedDialogue.EventId);
            DrawDialogueTextField("Group", ref _selectedDialogue.Group);
            DrawDialogueTextField("QuestionCategory", ref _selectedDialogue.QuestionCategory);
            DrawDialogueIntField("LineOrder", ref _selectedDialogue.LineOrder);
            DrawDialogueTextField("Speaker", ref _selectedDialogue.Speaker);

            EditorGUILayout.Space(8f);
            GUILayout.Label("Text", EditorStyles.boldLabel);
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
                MarkDirty("대사 Text 수정됨");
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
                $"Race: {ValueOrDash(_selectedNpc.Race)} / Role: {ValueOrDash(_selectedNpc.Role)}\n" +
                $"Food: {ValueOrDash(_selectedNpc.PreferredFoodTypes)}\n" +
                $"Tags: {ValueOrDash(_selectedNpc.PreferredTags)}";
            EditorGUILayout.HelpBox(summary, MessageType.None);
        }

        private void DrawNpcIdField(NpcDraft npc)
        {
            if (npc == null)
                return;

            string oldNpcId = npc.NpcId;
            EditorGUI.BeginChangeCheck();
            string next = EditorGUILayout.TextField("NpcId", npc.NpcId);
            if (EditorGUI.EndChangeCheck())
                RenameNpcId(npc, oldNpcId, next);
        }

        private void DrawNpcTextField(string label, ref string value)
        {
            EditorGUI.BeginChangeCheck();
            string next = EditorGUILayout.TextField(label, value);
            if (EditorGUI.EndChangeCheck())
            {
                value = next;
                MarkDirty($"{label} 수정됨");
            }
        }

        private void DrawNpcTextArea(string label, ref string value, float height)
        {
            GUILayout.Label(label);
            EditorGUI.BeginChangeCheck();
            string next = EditorGUILayout.TextArea(value, GUILayout.Height(height));
            if (EditorGUI.EndChangeCheck())
            {
                value = next;
                MarkDirty($"{label} 수정됨");
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
            bool next = EditorGUILayout.Toggle(label, value);
            if (EditorGUI.EndChangeCheck())
            {
                value = next;
                MarkDirty($"{label} 수정됨");
            }
        }

        private void DrawNpcIntField(string label, ref int value)
        {
            EditorGUI.BeginChangeCheck();
            int next = EditorGUILayout.IntField(label, value);
            if (EditorGUI.EndChangeCheck())
            {
                value = Mathf.Max(0, next);
                MarkDirty($"{label} 수정됨");
            }
        }

        private void DrawDialogueTextField(string label, ref string value)
        {
            EditorGUI.BeginChangeCheck();
            string next = EditorGUILayout.TextField(label, value);
            if (EditorGUI.EndChangeCheck())
            {
                value = next;
                MarkDirty($"{label} 수정됨");
            }
        }

        private void DrawDialogueIntField(string label, ref int value)
        {
            EditorGUI.BeginChangeCheck();
            int next = EditorGUILayout.IntField(label, value);
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
            string next = EditorGUILayout.TextField("EventId", visitEvent.EventId);
            if (EditorGUI.EndChangeCheck())
                RenameVisitEventId(visitEvent, oldEventId, next);
        }

        private void DrawVisitEventTextField(string label, VisitEventReference visitEvent, string columnName, ref string value)
        {
            EditorGUI.BeginChangeCheck();
            string next = EditorGUILayout.TextField(label, value);
            if (EditorGUI.EndChangeCheck())
            {
                value = next;
                visitEvent.SetRaw(columnName, next);
                MarkDirty($"VisitEvent {label} 수정됨");
            }
        }

        private void DrawVisitEventIntField(string label, VisitEventReference visitEvent, string columnName, ref int value)
        {
            EditorGUI.BeginChangeCheck();
            int next = EditorGUILayout.IntField(label, value);
            if (EditorGUI.EndChangeCheck())
            {
                value = Mathf.Max(0, next);
                visitEvent.SetRaw(columnName, value.ToString());
                MarkDirty($"VisitEvent {label} 수정됨");
            }
        }

        private void BoldSelectedText()
        {
            if (_selectedDialogue == null)
                return;

            CaptureDialogueTextSelection(GetDialogueTextControlName(_selectedDialogue));

            if (!TryGetDialogueTextSelection(out int start, out int end))
            {
                _statusMessage = "Text 영역에서 볼드 처리할 글자를 드래그로 선택한 뒤 다시 눌러주세요.";
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
            int speakerLineCount = _dialogues.Count(line => string.Equals(line.Speaker, npcId, StringComparison.OrdinalIgnoreCase));
            int visitEventCount = _visitEvents.Count(visitEvent => string.Equals(visitEvent.NpcId, npcId, StringComparison.OrdinalIgnoreCase));
            bool confirmed = EditorUtility.DisplayDialog(
                "NPC 삭제",
                $"NPC '{npcId}'를 삭제할까요?\n\nSpeaker 대사 {speakerLineCount}개와 VisitEvent {visitEventCount}개는 자동 삭제하지 않습니다.",
                "삭제",
                "취소");

            if (confirmed == false)
                return;

            ClearDialogueTextFocus();
            _npcs.Remove(_selectedNpc);
            _selectedNpc = _npcs.FirstOrDefault();
            _selectedEventId = _selectedNpc != null
                ? GetEventIdsForNpc(_selectedNpc.NpcId).FirstOrDefault() ?? string.Empty
                : string.Empty;
            _newEventId = _selectedNpc != null ? GenerateUniqueEventId(_selectedNpc.NpcId) : string.Empty;
            _selectedDialogue = null;
            ResetDialogueTextSelection();
            MarkDirty("NPC 삭제됨");
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
            _visitEvents.Add(VisitEventReference.CreateDefault(eventId, _selectedNpc.NpcId));
            EnsureVisitEventHeaders();
            _selectedEventId = eventId;
            SelectDialogue(dialogue);
            _newEventId = GenerateUniqueEventId(_selectedNpc.NpcId);
            MarkDirty("이벤트 대사 추가됨. VisitEvents.csv는 아직 자동 수정하지 않습니다.");
        }

        private void DeleteSelectedEvent()
        {
            if (string.IsNullOrWhiteSpace(_selectedEventId))
                return;

            string eventId = _selectedEventId;
            int dialogueCount = _dialogues.Count(dialogue => string.Equals(dialogue.EventId, eventId, StringComparison.OrdinalIgnoreCase));
            int visitEventCount = _visitEvents.Count(visitEvent => string.Equals(visitEvent.EventId, eventId, StringComparison.OrdinalIgnoreCase));
            int option = EditorUtility.DisplayDialogComplex(
                "Delete Event",
                $"Delete event '{eventId}'?\n\nDialogue lines: {dialogueCount}\nVisitEvent rows: {visitEventCount}",
                "Delete Event + Lines",
                "Cancel",
                "VisitEvent Only");

            if (option == 1)
                return;

            _visitEvents.RemoveAll(visitEvent => string.Equals(visitEvent.EventId, eventId, StringComparison.OrdinalIgnoreCase));

            if (option == 0)
                _dialogues.RemoveAll(dialogue => string.Equals(dialogue.EventId, eventId, StringComparison.OrdinalIgnoreCase));

            ClearDialogueTextFocus();
            _selectedEventId = _selectedNpc != null ? GetEventIdsForNpc(_selectedNpc.NpcId).FirstOrDefault() ?? string.Empty : string.Empty;
            _selectedDialogue = null;
            ResetDialogueTextSelection();
            _eventListScroll = Vector2.zero;
            _dialogueListScroll = Vector2.zero;
            _dialogueDetailScroll = Vector2.zero;
            _visitEventDetailScroll = Vector2.zero;
            MarkDirty(option == 0 ? "이벤트와 대사 삭제됨" : "VisitEvent 연결 삭제됨");
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
            _visitEventHeaders.Clear();

            foreach (Dictionary<string, string> row in ReadCsv(NpcCsvPath))
                _npcs.Add(NpcDraft.FromRow(row));

            foreach (Dictionary<string, string> row in ReadCsv(DialogueCsvPath))
                _dialogues.Add(DialogueDraft.FromRow(row));

            CsvTable visitEventTable = ReadCsvTable(VisitEventCsvPath);
            _visitEventHeaders.AddRange(EnsureHeaders(visitEventTable.Headers, DefaultVisitEventHeaders));
            foreach (Dictionary<string, string> row in visitEventTable.Rows)
                _visitEvents.Add(VisitEventReference.FromRow(row));

            _selectedNpc = _npcs.FirstOrDefault();
            ClearDialogueTextFocus();
            _selectedEventId = _selectedNpc != null
                ? GetEventIdsForNpc(_selectedNpc.NpcId).FirstOrDefault() ?? string.Empty
                : string.Empty;
            _newEventId = _selectedNpc != null ? GenerateUniqueEventId(_selectedNpc.NpcId) : string.Empty;
            _selectedDialogue = null;
            ResetDialogueTextSelection();
            _hasUnsavedChanges = false;
            ClearValidationResults();
            CaptureWriteTimes();

            if (updateStatus)
                _statusMessage = "CSV 다시 불러오기 완료";
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
                && EditorUtility.DisplayDialog("Reload CSV", "저장하지 않은 변경사항을 버리고 CSV를 다시 불러올까요?", "Reload", "Cancel") == false)
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
                   || GetLastWriteTime(VisitEventCsvPath) != _visitEventLastWriteTime;
        }

        private void CaptureWriteTimes()
        {
            _npcLastWriteTime = GetLastWriteTime(NpcCsvPath);
            _dialogueLastWriteTime = GetLastWriteTime(DialogueCsvPath);
            _visitEventLastWriteTime = GetLastWriteTime(VisitEventCsvPath);
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
