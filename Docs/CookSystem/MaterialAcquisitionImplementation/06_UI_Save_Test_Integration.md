# 06. UI, 저장, 테스트, 통합 명세

## 1. 목표

이 문서는 파견과 모험을 실제 씬에 붙이고, 저장/로드와 테스트까지 마감하기 위한 명세다.

대상:

- UI 프리팹 구조
- 화면 전환
- 기존 요리/NPC/인벤토리 시스템 연결
- 저장 데이터 구조
- 테스트 계획
- 기존 임시 파견 구조 정리

## 2. UI 구현 원칙

### 2.1 프리팹 우선

최종 UI는 런타임에서 전체 레이아웃을 코드로 만들지 않는다.

권장:

- 화면 단위 프리팹
- 반복 항목 단위 프리팹
- 직렬화된 참조 연결
- `Bind`, `Refresh`, `SetData` 메서드로 표시 갱신

허용:

- 보상 행, 선택지 버튼 같은 반복 요소를 항목 프리팹으로 생성
- 디버그/프로토타입 전용 자동 생성 UI

금지:

- 최종 UI 클래스에서 전체 화면을 `new GameObject`로 생성
- 필수 참조가 없는데 자동 대체 UI를 만들어 조용히 진행
- UI 클래스에서 보상 랜덤 계산
- UI 클래스에서 저장 데이터 직접 수정

### 2.2 UI는 명령만 전달

UI 역할:

- 상태 표시
- 버튼 클릭 전달
- 선택된 데이터 ID 전달
- 결과 표시

UI가 하지 않는 것:

- 보상 계산
- 날짜 증가
- 파견 상태 직접 변경
- 모험 세션 직접 수정
- 인벤토리 지급

## 3. UI 프리팹 목록

### 3.1 준비 단계

```text
Assets/Work/MaterialAcquisition/Prefabs/Common/
  PreparationPhaseScreen.prefab
```

필수 컴포넌트:

- `PreparationPhaseView`
- 날짜 텍스트
- 요약 텍스트
- 파견 버튼
- 모험 버튼
- 다음날 버튼

### 3.2 파견

```text
Assets/Work/MaterialAcquisition/Prefabs/Dispatch/
  DispatchScreen.prefab
  DispatchRegionCard.prefab
  DispatchAgentSlot.prefab
  DispatchTaskRow.prefab
  DispatchResultPanel.prefab
  DispatchRewardResultRow.prefab
```

`DispatchScreen` 필수 영역:

- 지역 목록 루트
- 선택 지역 상세
- NPC 목록 루트
- 진행 중 파견 목록 루트
- 복귀 가능 목록 루트
- 시작 버튼
- 닫기 버튼

### 3.3 모험

```text
Assets/Work/MaterialAcquisition/Prefabs/Adventure/
  AdventureScreen.prefab
  AdventureChoiceButton.prefab
  AdventureRewardRow.prefab
  AdventureResultLogRow.prefab
  AdventureSettlementPanel.prefab
```

`AdventureScreen` 필수 영역:

- 지역명
- 진행도
- 위험도
- 이벤트 제목
- 이벤트 설명
- 선택지 버튼 루트
- 결과 로그 루트
- 임시 보상 목록
- 계속 진행 버튼
- 귀환 버튼

## 4. 화면 전환

### 4.1 전체 흐름

```text
영업 중
-> 영업 종료
-> 준비 단계 화면
   -> 파견 화면
   -> 모험 화면
-> 다음날 시작
-> 영업 중
```

### 4.2 파견 화면

```text
준비 단계에서 파견 버튼
-> DispatchScreen.Show()
-> 지역 선택
-> NPC 선택
-> 파견 시작
-> DispatchScreen.Refresh()
-> 준비 단계로 복귀 가능
```

파견 결과 수령:

```text
복귀 가능 작업 선택
-> 결과 수령
-> DispatchResultPanel.Show()
-> 확인
-> DispatchScreen.Refresh()
```

### 4.3 모험 화면

```text
준비 단계에서 모험 버튼
-> AdventureScreen.ShowRegionSelect()
-> 지역 선택
-> 모험 시작
-> 이벤트 표시
-> 선택지 선택
-> 결과 로그
-> 계속 진행 또는 귀환
```

귀환:

```text
귀환 클릭
-> 정산
-> AdventureSettlementPanel.Show()
-> 확인
-> 준비 단계로 복귀
```

## 5. 기존 시스템 연결

### 5.1 영업 흐름

연결 대상:

- `CookingBusinessFlowController.BusinessClosed`
- `CookingBusinessFlowController`의 다음 영업 시작 메서드 추가 필요

필요 작업:

- 영업 종료 시 `PreparationPhaseController.OpenPreparationPhase()` 호출
- 다음날 시작 시 `NpcEncounterDirector.AdvanceDay()` 호출
- 이후 `CookingBusinessFlowController.OpenShopForNextDay(true)` 또는 동등 메서드 호출

사용자 결정 필요:

- 다음날 버튼 클릭 후 첫 손님 자동 시작 여부

### 5.2 날짜

연결 대상:

- `NpcEncounterDirector.CurrentDay`
- `NpcEncounterDirector.AdvanceDay()`
- `NpcEncounterDirector.CurrentDateText`

어댑터:

```text
NpcEncounterDayProvider : IAcquisitionDayProvider
```

주의:

- 파견 복귀일은 반드시 이 날짜 기준으로 계산한다.
- 모험은 하루 준비 단계 콘텐츠라 1차에서는 모험 진행 중 날짜를 바꾸지 않는다.

### 5.3 인벤토리

연결 대상:

- `PlayerInventoryModule.AddItems()`
- `PlayerInventoryModule.GetItemAmount()`

어댑터:

```text
AcquisitionInventoryGateway
```

주의:

- UI에서 직접 `AddItems()`를 호출하지 않는다.
- 파견과 모험 모두 같은 게이트웨이를 사용한다.

### 5.4 재료/도감

연결 대상:

- `IngredientItemDataSO`
- `IngredientSO`
- `CookingKnowledgeStore`
- 정보/도감 UI

1차:

- `IngredientItemDataSO.Ingredient` 기준으로 새 재료 발견 여부만 기록한다.

2차:

- 도감 페이지 해금
- 손질법 힌트 해금
- 레시피 힌트 해금

### 5.5 NPC

연결 대상:

- `NpcData`
- `NpcEncounterDirector`

1차 선택:

- 파견 NPC를 기존 NPC와 연결할 경우 `npcId`를 저장한다.
- 손님 등장 제외가 필요하면 `NpcEncounterDirector`에 제외 목록 필터를 추가한다.

대안:

- 파견 전용 에이전트를 사용하고 손님 등장 제외 처리는 2차로 미룬다.

## 6. 저장 구조

### 6.1 최상위 저장 데이터

```text
AcquisitionSaveData
- int saveVersion
- DispatchSaveData dispatch
- AdventureSaveData adventure
- DiscoverySaveData discovery
- PreparationPhaseSaveData preparation
```

### 6.2 준비 단계 저장

```text
PreparationPhaseSaveData
- int activePreparationDay
- int dispatchStartsToday
- int adventureStartsToday
- bool preparationOpen
```

저장 필요성:

- 준비 단계 중 게임을 껐다 켜도 오늘 사용 횟수를 보존해야 한다.

### 6.3 파견 저장

```text
DispatchSaveData
- int saveVersion
- List<DispatchTaskSaveData> tasks
```

```text
DispatchTaskSaveData
- string taskId
- string regionId
- string assignedAgentId
- string assignedNpcId
- int startDay
- int returnDay
- string status
- int resultSeed
- bool rewardClaimed
- int claimedDay
```

### 6.4 모험 저장

```text
AdventureSaveData
- int saveVersion
- AdventureSessionSaveData activeSession
- List<string> completedSessionIds
```

```text
AdventureSessionSaveData
- string sessionId
- string regionId
- int currentProgress
- int danger
- int fatigue
- string currentEventId
- List<RewardRollSaveData> temporaryRewards
- List<string> flags
- List<string> eventHistory
- List<string> choiceHistory
- int sessionSeed
- string status
```

### 6.5 발견 저장

```text
DiscoverySaveData
- int saveVersion
- List<string> discoveredIngredientIds
```

### 6.6 저장 타이밍

필수 저장:

- 파견 시작 후
- 파견 결과 수령 후
- 모험 시작 후
- 모험 선택지 적용 후
- 모험 정산 후
- 다음날 진행 후
- 새 재료 발견 후

사용자 결정 필요:

- 자동 저장을 모든 선택지마다 할지
- 준비 단계 진입/종료 시에만 할지

기본 제안:

- 1차는 상태 변경마다 저장.

## 7. 저장 시스템 연결 방식

프로젝트의 기존 저장 시스템이 명확하지 않다면 1차는 아래 방식으로 진행한다.

1차:

```text
IAcquisitionSaveStore
- AcquisitionSaveData Load()
- void Save(AcquisitionSaveData data)
```

임시 구현:

```text
PlayerPrefsAcquisitionSaveStore
```

또는 메모리 구현:

```text
MemoryAcquisitionSaveStore
```

2차:

- 프로젝트 전체 저장 시스템과 연결
- 세이브 버전 마이그레이션 추가

## 8. 테스트 계획

### 8.1 테스트 폴더

```text
Assets/Work/MaterialAcquisition/Tests/
  EditMode/
  PlayMode/
```

### 8.2 EditMode 테스트

공통:

- 보상 테이블 시드 재현
- 보상 수량 범위
- 인벤토리 지급 결과 모델 생성
- 발견 기록

파견:

- 파견 시작 가능 여부
- 복귀일 계산
- NPC 중복 파견 방지
- 복귀 가능 상태 갱신
- 결과 중복 수령 방지

모험:

- 모험 시작
- 이벤트 선택
- 선택지 적용
- 위험도 변화
- 임시 보상 추가
- 실패 조건
- 정산 결과

### 8.3 PlayMode 테스트

필수:

- 영업 종료 후 준비 단계 UI 표시
- 파견 시작 버튼 동작
- 날짜 진행 후 복귀 결과 수령
- 모험 선택지 버튼 동작
- 귀환 정산 UI 표시
- 인벤토리 수량 증가

## 9. 디버그 도구

1차 개발 편의를 위해 디버그 패널을 둘 수 있다.

기능:

- 현재 영업일 표시
- 강제 다음날
- 파견 작업 목록
- 파견 즉시 복귀 가능 처리
- 모험 세션 상태 표시
- 위험도 강제 변경
- 임시 보상 확인

주의:

- 디버그 도구는 `Debug` 또는 `Temp` 명칭을 사용한다.
- 최종 UI와 섞지 않는다.

## 10. 기존 임시 파견 구조 정리

현재 존재:

```text
Assets/Work/Dispatch/
```

정리 단계:

### 10.1 1차 개발 중

- 삭제하지 않는다.
- 새 구현과 충돌하지 않도록 최종 씬에서 비활성화한다.
- 새 문서와 코드에서는 `Work.MaterialAcquisition`을 기준으로 한다.

### 10.2 새 파견 MVP 완료 후

- 기존 `DispatchPointSO` 데이터를 `DispatchRegionSO`로 변환한다.
- 기존 `DispatchRewardEntry` 데이터를 `AcquisitionRewardTableSO`로 변환한다.
- 기존 `DispatchController`는 참조가 없으면 제거 후보로 표시한다.

### 10.3 삭제 전 확인

- 씬에서 기존 `DispatchController` 참조가 없는가
- 프리팹에서 기존 `DispatchMapView` 참조가 없는가
- 테스트 데이터가 새 데이터로 옮겨졌는가

## 11. 완료 체크리스트

### 11.1 UI

- 준비 단계 화면 프리팹이 있다.
- 파견 화면 프리팹이 있다.
- 모험 화면 프리팹이 있다.
- 결과 화면이 획득/미획득/새 발견을 표시한다.
- 필수 참조 누락 시 명시 오류가 난다.

### 11.2 통합

- 영업 종료 후 준비 단계가 열린다.
- 다음날 진행이 날짜 시스템과 연결된다.
- 파견과 모험은 준비 단계에서만 열린다.
- 인벤토리 지급은 공통 게이트웨이를 사용한다.
- 새 재료 발견 기록이 남는다.

### 11.3 저장

- 파견 작업이 저장된다.
- 파견 복귀 가능 상태가 로드 후 복원된다.
- 모험 세션이 저장된다.
- 임시 보상이 저장된다.
- 발견 기록이 저장된다.

### 11.4 테스트

- 공통 보상 테스트가 있다.
- 파견 도메인 테스트가 있다.
- 모험 도메인 테스트가 있다.
- 최소 PlayMode 테스트가 있다.

## 12. 사용자 결정 필요 체크

UI 전 확인:

- 파견 화면은 목록형으로 시작해도 되는가?
- 모험 화면은 텍스트 로그형으로 시작해도 되는가?
- 결과창에서 미지급 수량을 어떻게 표현할 것인가?
- 새 발견 연출은 간단 배지로 충분한가?

저장 전 확인:

- 저장은 상태 변경마다 자동으로 할 것인가?
- 모험 중 게임 종료 후 재개할 것인가?
- 파견 결과 수령 전 저장/로드 시 결과가 같은 시드로 재현되면 되는가?

통합 전 확인:

- 기존 `Assets/Work/Dispatch`를 언제 제거할 것인가?
- 기존 NPC 시스템을 수정해서 파견 중 NPC를 손님 풀에서 제외할 것인가?

기본 제안:

```text
UI: 1차는 목록형/텍스트 로그형
새 발견: 결과 행 배지
저장: 상태 변경마다 자동 저장
모험 중 종료: 같은 이벤트에서 재개
기존 Dispatch: 새 MVP 안정화 후 제거
NPC 제외: 1차는 가능하면 적용, 어렵다면 별도 파견 에이전트 사용
```
