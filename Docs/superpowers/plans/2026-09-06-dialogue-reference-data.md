# Dialogue Reference Data Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 목업 요리 데이터를 정리하고 정식 메뉴 3종과 오딘을 근거로 재사용 가능한 대화 생성 기준 자료를 만든다.

**Architecture:** 현재 Unity 카탈로그와 CSV를 실제 데이터의 원본으로 유지한다. GUID를 유지하는 에셋 이동으로 참조를 보존하고, 제거한 임시 레시피 ID는 저장 데이터 로드 시 이관한다. 생성 기준 문서는 확정 설정과 코드가 실제로 지원하는 조건을 구분한다.

**Tech Stack:** Unity 6000.3.13f1, ScriptableObject YAML, CSV, C#, EditMode NUnit.

**Spec:** 이 대화에서 합의한 메뉴는 버섯 수프 빠네, 슬라임 핵 당고, 콘치즈 퐁듀다. 퐁듀는 옥수수 모양 치즈인 콘치즈, 버섯 조각, 슬라임 점액으로 만든다. 이번 단계는 기존 목업 정리, 실제 데이터 등록, 대화 생성 기준 자료 작성이다.

## Global Constraints

- 현재 체크아웃의 미커밋 씬/UI 변경사항을 보존한다.
- 콘치즈는 옥수수와 치즈의 혼합물이 아니라 옥수수 모양 치즈다.
- 실재료 6종과 기존 메뉴의 레시피 슬롯 ID, 에셋 GUID, 인벤토리 아이템 ID를 유지한다.
- 새 NPC와 전체 대사 제작, 영업 상태머신 변경은 후속 단계다. 현재 실등록 NPC는 Odin이다.
- 스킬의 별도 승인 절차를 반복하지 않고 이미 요청된 정리·등록 작업을 실행한다. 커밋은 하지 않는다.

## Task 1: Register canonical food data

**Files:** `Assets/Work/Cook/Data/Recipes`, `Assets/Work/Cook/Data/Ingredients/corn_cheese.asset`, `Assets/Work/Cook/SO/CookingDataCatalog.asset`, `Assets/Work/Items/SO/Ingredients`, `Assets/Resources/NPCData/VisitEvents.csv`.

**Interfaces:** Produces recipe IDs `mushroom_soup_pane`, `slime_nucleus_dango`, `corn_cheese_fondue`; consumes existing ingredient IDs and preparation methods.

- [x] Add integration tests requiring exactly these 3 recipes and verifying prepared ingredient formation for fondue. Run and confirm missing canonical recipes.
- [x] Rename the real soup asset and its ID with GUID unchanged; update every live order reference.
- [x] Register fondue: roast corn cheese to melt it, slice flat mushroom into pieces, boil slime mucus; one of each, all defining ingredients.
- [x] Correct corn cheese descriptions in cooking and inventory data; add discoverable recipe descriptions and hints grounded in actual preparation.
- [x] Move the six live ingredient item assets out of `TempCookingTest` with their meta files, preserving item IDs.
- [x] Archive unreferenced generated recipe/ingredient/method assets outside Assets after checking GUID references; archive the legacy editor command so it cannot recreate them from Unity menus.

## Task 2: Preserve existing recipe knowledge and clean Odin references

**Files:** `Assets/Work/Cook/Code/Runtime/Systems/CookingKnowledgePlayerPrefsRepository.cs`, a focused recipe ID migration utility, `Assets/Work/Cook/Code/Editor/CanonicalCookingDataTests.cs`, `Assets/Resources/NPCData/NPCs.csv`, `Assets/Resources/NPCData/DialogueLines.csv`.

**Interfaces:** Migration consumes `CookingKnowledgeSaveData`, produces the same data with canonical recipe IDs and matching variant IDs. No PlayerPrefs reset.

- [x] Add regression tests for old soup knowledge, variants, and legacy ID lists; run before migration implementation.
- [x] Migrate only the old `NewRecipe` identity and its recipe-prefixed legacy keys. Preserve completion counts, tags, guest summaries, and replay components.
- [x] Keep Odin's existing event IDs and long-term dessert preference; distinguish long-term NPC preferences from the current soup order and correct unconditional success memories in cyclic dialogue.
- [x] Verify correct-result dialogue does not assume an exact recipe when the evaluator accepts another recipe.

## Task 3: Author reference pack and verify

**Files:** `Docs/NPCSystem/Dialogue_Generation_Reference.md`, `Docs/NPCSystem/Dialogue_Generation_Prompt.md`, `Docs/NPCSystem/Data_Registration_Report.md`, existing authoring documents.

**Interfaces:** Consumes registered assets, CSV schemas, actual runtime rules. Produces a human-readable canon and copyable generation prompt.

- [x] Document Odin's voice, relationship limits, actual 3 recipes, 6 ingredients, available preparation effects, and food-tag meanings.
- [x] Document event types/repeat rules, affinity point thresholds, exact last-result matching, result dialogue fallback, and bold clue extraction.
- [x] Provide a generation contract with CSV headers and a complete small Odin example; clearly distinguish later NPC proposals from registered canon.
- [x] Mark older planning documents as historical so their example IDs and unsupported conditions are not copied into live data.
- [x] Run focused Unity EditMode tests including NPC data validation and existing cooking/dispatch tests; inspect logs and results, verify GUID references and `git diff --check`.
- [x] Record exact validation outcomes and remaining loop work without claiming a multi-day playtest.
