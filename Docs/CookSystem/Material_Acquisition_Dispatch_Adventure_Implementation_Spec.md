# 파견 & 모험 재료 획득 시스템 구현 명세서

> 이 문서는 전체 방향을 빠르게 보기 위한 요약 명세서다. 실제로 처음부터 끝까지 구현할 때는 `Docs/CookSystem/MaterialAcquisitionImplementation/` 아래의 분리 문서를 기준으로 진행한다.

## 1. 문서 목적

이 문서는 `Material_Acquisition_Dispatch_Adventure_Planning.md`의 기획을 실제 개발 작업으로 옮기기 위한 단계별 구현 명세서다.

기존 코드 구조를 확인한 결과, 현재 프로젝트에는 파견 프로토타입, 영업 흐름, 날짜 관리, 인벤토리/재료 연결 구조가 이미 일부 존재한다. 따라서 구현은 완전 신규 기능처럼 시작하기보다, 아래 방향으로 진행한다.

- 기존 파견 프로토타입에서 검증된 인벤토리 지급 흐름은 참고한다.
- 최종 구조는 `MaterialAcquisition` 기능 루트로 분리한다.
- 파견과 모험은 영업 종료 후 준비 단계에서만 진입한다.
- 보상, 조건, 랜덤, 인벤토리 지급은 공통 기반으로 만든다.
- 사용자 결정이 필요한 기획 사항은 코드에 임의로 고정하지 않는다.

관련 문서:

- `Docs/CookSystem/Material_Acquisition_Dispatch_Adventure_Planning.md`
- `Docs/CookSystem/Material_Acquisition_Dispatch_Adventure_Implementation_Rules.md`

## 2. 현재 프로젝트 구조 요약

### 2.1 이미 있는 구조

현재 존재하는 주요 연결점은 다음과 같다.

| 영역 | 현재 구조 | 활용 방향 |
| --- | --- | --- |
| 영업 흐름 | `CookingBusinessFlowController` | 영업 종료 후 준비 단계 진입 이벤트로 사용 |
| 날짜 | `NpcEncounterDirector.CurrentDay`, `AdvanceDay()` | 파견 복귀일 계산 기준으로 사용 |
| 인벤토리 | `PlayerInventoryModule.AddItems()` | 파견/모험 정산 지급에 사용 |
| 재료 | `IngredientSO`, `IngredientItemDataSO` | 보상 아이템과 요리 재료 연결에 사용 |
| 파견 임시 구현 | `Assets/Work/Dispatch` | 프로토타입 참고 또는 임시 호환 레이어로 사용 |
| 모험 | 별도 구현 없음 | 신규 구현 필요 |

### 2.2 현재 파견 프로토타입의 한계

기존 `Assets/Work/Dispatch` 구조는 다음 점에서 최종 기획과 다르다.

- 파견 시간이 영업일 단위가 아니라 초 단위다.
- 파견 시작 후 즉시 타이머가 돌고, 완료 즉시 보상을 지급한다.
- NPC 배정, 복귀 가능 상태, 저장 가능한 파견 작업 상태가 없다.
- 보상이 고정 리스트라 실패, 부분 성공, 희귀 보상, NPC 보너스를 표현하기 어렵다.
- 영업 종료 후 준비 단계에만 열리는 구조가 아니라, 손님 대화 중이 아니면 열릴 수 있다.

따라서 최종 구현에서는 기존 클래스를 직접 확장하기보다, `MaterialAcquisition` 아래에 새 도메인 구조를 만들고 필요하면 기존 파견 UI/데이터를 참고한다.

## 3. 최종 권장 폴더 구조

```text
Assets/Work/MaterialAcquisition/
  Code/
    Common/
    Dispatch/
    Adventure/
    Integration/
    UI/
    Editor/
  Data/
    Common/
    Dispatch/
    Adventure/
  Prefabs/
    Common/
    Dispatch/
    Adventure/
  Tests/
```

권장 네임스페이스:

```text
Work.MaterialAcquisition.Code.Common
Work.MaterialAcquisition.Code.Dispatch
Work.MaterialAcquisition.Code.Adventure
Work.MaterialAcquisition.Code.Integration
Work.MaterialAcquisition.Code.UI
Work.MaterialAcquisition.Code.Editor
```

기존 `Assets/Work/Dispatch`는 아래 중 하나로 처리한다.

1. 1차 개발 중 참고용으로 유지하고 최종 씬에서는 사용하지 않는다.
2. 새 구조가 안정화되면 제거 또는 `Temp` 명칭으로 격리한다.
3. 기존 임시 데이터만 새 `DispatchRegionSO` 데이터로 변환한다.

## 4. 전체 구현 로드맵

권장 순서는 다음과 같다.

```text
0단계: 기획 정책 확정
-> 1단계: 영업 종료 후 준비 단계 구축
-> 2단계: 공통 보상/랜덤/인벤토리 정산 기반 구축
-> 3단계: 파견 1차 구현
-> 4단계: 모험 1차 구현
-> 5단계: 도감/지식/해금 연동
-> 6단계: 저장/검증/테스트
-> 7단계: 밸런싱과 UI 연출 보강
```

각 단계는 독립적으로 검증 가능해야 한다. 특히 1단계와 2단계는 파견과 모험 모두의 기반이므로 먼저 고정한다.

## 5. 0단계: 기획 정책 확정

### 5.1 목표

코드에 박으면 나중에 바꾸기 어려운 정책을 먼저 결정한다. 결정되지 않은 항목은 설정값 또는 정책 객체로 남긴다.

### 5.2 사용자 결정 필요

아래 항목은 개발자가 임의로 확정하면 게임 루프가 달라지므로 사용자 결정이 필요하다.

| 결정 항목 | 기본 제안 | 사용자 개입 필요 이유 |
| --- | --- | --- |
| 하루에 파견과 모험을 둘 다 가능한가 | 1차는 둘 다 가능 | 하루 준비 단계의 밀도와 플레이 타임이 달라짐 |
| 하루 파견 가능 NPC 수 | 1차는 1명 | 운영 난이도와 보상량에 직접 영향 |
| 파견 중 NPC가 손님으로 등장 가능한가 | 1차는 등장 불가 | NPC 세계관 일관성과 손님 풀 난이도에 영향 |
| 모험 실패 시 임시 보상을 잃는가 | 1차는 일부 손실 | 위험 선택의 긴장감에 영향 |
| 모험에 체력/피로도/도구 내구도를 둘 것인가 | 1차는 위험도만 사용 | 시스템 복잡도와 UI 정보량에 영향 |
| 모험 중 획득 재료는 언제 확정되는가 | 귀환/종료 정산 시 확정 | 모험의 귀환 판단 재미에 영향 |
| 희귀 재료 확률을 얼마나 공개할 것인가 | 낮음/보통/높음 텍스트만 공개 | 플레이어 정보량과 공략성에 영향 |
| 도감 지식 해금 범위 | 새 재료 발견만 1차 적용 | 지식 시스템과 연결 범위에 영향 |

### 5.3 개발자가 임시로 진행 가능한 항목

아래 항목은 1차 구현을 위해 개발자가 기본값으로 진행해도 된다.

- 클래스명, 폴더명, 네임스페이스
- 도메인 서비스 분리 방식
- 보상 결과 객체 구조
- UI 프리팹의 내부 컴포넌트 이름
- 테스트용 임시 데이터 ID
- 에디터 검증 도구의 표시 방식

## 6. 1단계: 영업 종료 후 준비 단계 구축

### 6.1 목표

파견과 모험이 손님 응대 중에 끼어들지 않도록, 영업 종료 후 준비 단계라는 명확한 상태를 만든다.

### 6.2 새로 만들 클래스

```text
PreparationPhaseController
PreparationPhaseState
PreparationPhaseView
PreparationPhaseCommand
```

권장 위치:

```text
Assets/Work/MaterialAcquisition/Code/Integration/
Assets/Work/MaterialAcquisition/Code/UI/
```

### 6.3 책임

`PreparationPhaseController`의 책임:

- `CookingBusinessFlowController.BusinessClosed` 이벤트 수신
- 준비 단계 시작
- 파견 화면 열기 가능 여부 관리
- 모험 화면 열기 가능 여부 관리
- 다음 날 시작 처리
- 준비 단계 종료

`PreparationPhaseView`의 책임:

- 현재 날짜 표시
- 오늘 준비 단계에서 가능한 행동 표시
- 파견 버튼
- 모험 버튼
- 다음 날 시작 버튼
- 복귀 가능한 파견 알림

### 6.4 날짜 처리

현재 날짜는 `NpcEncounterDirector.CurrentDay`를 기준으로 조회한다. 다음 날 시작 시점에는 다음 처리가 필요하다.

```text
준비 단계 종료
-> 복귀 가능 파견 상태 갱신
-> 필요하면 미수령 결과 알림
-> NpcEncounterDirector.AdvanceDay()
-> 다음 영업 시작 상태로 전환
```

주의:

- 현재 `CookingBusinessFlowController.CloseShop()`은 영업 종료 이벤트를 쏘지만, 다음 영업일을 시작하는 공개 메서드가 부족하다.
- `_businessClosed`를 다시 여는 흐름이 필요하다.
- 1차 구현에서는 `CookingBusinessFlowController`에 `OpenShopForNextDay()` 또는 유사 메서드를 추가하는 것을 권장한다.

### 6.5 완료 기준

- 손님 응대 중에는 파견/모험 버튼이 비활성화된다.
- 영업 종료 후 준비 화면이 열린다.
- 준비 화면에서 파견/모험/다음날 버튼이 보인다.
- 다음날 버튼을 누르면 날짜가 1 증가한다.
- 다음 영업을 시작할 수 있다.

### 6.6 사용자 결정 필요

- 준비 단계 화면 이름과 UI 분위기
- 하루에 파견/모험을 각각 몇 번 허용할지
- 파견 결과 수령을 다음날 시작 전에 강제할지, 나중에 받아도 되는지

## 7. 2단계: 공통 보상, 랜덤, 인벤토리 정산 기반

### 7.1 목표

파견과 모험이 같은 방식으로 재료 보상을 계산하고 인벤토리에 지급하도록 공통 기반을 만든다.

### 7.2 새로 만들 데이터

```text
AcquisitionRewardTableSO
AcquisitionRewardEntry
AcquisitionRewardRarity
AcquisitionRewardPreview
```

예상 필드:

```text
AcquisitionRewardEntry
- item
- minAmount
- maxAmount
- chance
- weight
- rarity
- isGuaranteed
- previewGroupLabel
```

### 7.3 새로 만들 런타임 모델

```text
AcquisitionRewardRoll
AcquisitionRewardResult
AcquisitionRewardResultEntry
AcquisitionInventoryGrantResult
```

결과 항목 필수 정보:

- 아이템
- 요청 수량
- 실제 지급 수량
- 미지급 수량
- 희귀 여부
- 새 발견 여부
- 보상 출처 타입
- 보상 출처 ID

### 7.4 새로 만들 서비스

```text
IAcquisitionRandom
SeededAcquisitionRandom
AcquisitionRewardResolver
AcquisitionInventoryGateway
```

`AcquisitionRewardResolver` 책임:

- 보상 테이블 해석
- 확정 보상 계산
- 확률 보상 계산
- 가중치 선택
- 희귀 보상 판정

`AcquisitionInventoryGateway` 책임:

- `PlayerInventoryModule`에 보상 지급
- 부분 지급 결과 보존
- 지급 실패 로그 작성

### 7.5 완료 기준

- 같은 시드와 같은 보상 테이블은 같은 결과를 만든다.
- 인벤토리가 가득 찬 경우 미지급 수량이 결과에 남는다.
- 보상 계산 코드는 UI 클래스에 들어가지 않는다.
- 파견과 모험이 같은 보상 결과 모델을 사용할 수 있다.

### 7.6 사용자 결정 필요

- 희귀도 명칭: 예를 들어 `일반`, `희귀`, `특수`, `위험`
- UI에 실제 확률 숫자를 보여줄지, 텍스트 등급만 보여줄지
- 새 재료 발견 시 도감 갱신을 즉시 보여줄지, 정산 화면에서만 보여줄지

## 8. 3단계: 파견 1차 구현

### 8.1 목표

문서의 1차 범위를 구현한다.

1차 범위:

- 파견 지역 3개
- 파견 가능 NPC 2명
- 지역별 기본 보상/희귀 보상
- 영업일 단위 파견
- 복귀 결과 UI
- 인벤토리 재료 지급

### 8.2 새로 만들 데이터

```text
DispatchRegionSO
DispatchAgentSO
DispatchAgentBonus
DispatchDangerLevel
DispatchNpcAvailabilityPolicy
```

`DispatchRegionSO` 예상 필드:

```text
- regionId
- displayName
- description
- icon
- requiredDays
- dangerLevel
- commonRewardTable
- rareRewardTable
- unlockConditions
- recommendedTraitIds
```

`DispatchAgentSO` 예상 필드:

```text
- agentId
- displayName
- npcId
- portrait
- traits
- bonuses
```

초기에는 `NpcData`를 직접 수정하지 않고, `npcId` 문자열로 연결한다.

### 8.3 새로 만들 런타임 모델

```text
DispatchTask
DispatchTaskStatus
DispatchTaskResult
DispatchTaskStartRequest
DispatchTaskClaimResult
```

`DispatchTask` 필수 필드:

```text
- taskId
- regionId
- assignedAgentId
- assignedNpcId
- startDay
- returnDay
- status
- resultSeed
```

상태:

```text
Waiting
InProgress
ReadyToReturn
Completed
Cancelled
```

### 8.4 새로 만들 서비스

```text
DispatchService
DispatchTaskRepository
DispatchResultResolver
DispatchAvailabilityService
```

`DispatchService` 책임:

- 파견 시작 가능 여부 검사
- NPC 중복 파견 방지
- 파견 작업 생성
- 현재 날짜 기준 복귀 가능 상태 갱신
- 결과 수령 처리

`DispatchResultResolver` 책임:

- 파견 지역 보상 계산
- 위험도에 따른 실패/부분 성공 계산
- NPC 보너스 적용
- 결과 시드 기반 재현성 보장

### 8.5 UI

권장 프리팹:

```text
DispatchScreen.prefab
DispatchRegionCard.prefab
DispatchAgentSlot.prefab
DispatchTaskRow.prefab
DispatchResultPanel.prefab
DispatchRewardResultRow.prefab
```

화면 정보:

- 지역 목록
- 지역 설명
- 주요 재료군
- 예상 소요 영업일
- 위험도
- 희귀 보상 가능성
- NPC 선택
- NPC 보너스
- 진행 중인 파견
- 복귀 가능 파견
- 결과 수령

### 8.6 기존 파견 프로토타입 처리

기존 `Assets/Work/Dispatch`는 아래처럼 다룬다.

- 기존 `DispatchPointSO`의 포인트 3개는 새 `DispatchRegionSO` 샘플 데이터로 옮긴다.
- 기존 `DispatchRewardEntry`의 아이템/수량은 새 `AcquisitionRewardTableSO` 샘플로 옮긴다.
- 기존 `DispatchController`는 최종 씬에서 사용하지 않는다.
- 기존 클래스 삭제는 새 파견 구현이 안정화된 뒤 별도 작업으로 한다.

### 8.7 완료 기준

- 영업 종료 후 파견 화면에 진입할 수 있다.
- 지역과 NPC를 선택해 파견을 시작할 수 있다.
- 시작 즉시 보상이 지급되지 않는다.
- `currentDay >= returnDay`가 되면 복귀 가능 상태가 된다.
- 복귀 결과 수령 시 보상이 인벤토리에 지급된다.
- 파견 중 NPC는 다시 파견할 수 없다.
- 같은 시드의 파견 결과는 재현 가능하다.

### 8.8 사용자 결정 필요

- 1차 파견 지역 3개 확정
- 지역별 표시명, 설명, 주요 재료군
- 지역별 소요 영업일
- 지역별 위험도
- 지역별 기본/희귀 보상 테이블
- 파견 가능 NPC 2명
- NPC별 파견 특성
- 파견 중 NPC가 손님 풀에서 빠지는지 여부

## 9. 4단계: 모험 1차 구현

### 9.1 목표

문서의 1차 범위를 구현한다.

1차 범위:

- 모험 지역 1개
- 이벤트 6개 이상
- 이벤트당 선택지 2~3개
- 채집 이벤트
- 마물 조우 이벤트
- 전투 선택지 3종
- 귀환/정산 처리
- 인벤토리 재료 지급

### 9.2 새로 만들 데이터

```text
AdventureRegionSO
AdventureEventSO
AdventureChoice
AdventureChoiceResult
AdventureEventType
AdventureCondition
AdventureFlagDefinition
```

`AdventureRegionSO` 예상 필드:

```text
- regionId
- displayName
- description
- baseDanger
- maxProgress
- eventPool
- rewardTheme
- unlockConditions
```

`AdventureEventSO` 예상 필드:

```text
- eventId
- displayName
- description
- eventType
- choices
- weight
- requiredConditions
- blockConditions
```

`AdventureChoice` 예상 필드:

```text
- choiceId
- displayText
- resultText
- resultType
- successRate
- rewardTable
- dangerDelta
- fatigueDelta
- nextEventId
- flagsToAdd
- flagsToRemove
- requiredConditions
```

### 9.3 새로 만들 런타임 모델

```text
AdventureSession
AdventureSessionStatus
AdventureChoiceSelection
AdventureChoiceApplyResult
AdventureSettlementResult
```

`AdventureSession` 필수 필드:

```text
- sessionId
- regionId
- currentProgress
- danger
- fatigue
- currentEventId
- temporaryRewards
- flags
- eventHistory
- sessionSeed
- status
```

상태:

```text
NotStarted
InProgress
AwaitingChoice
ReadyToContinue
Returning
Settled
Failed
```

### 9.4 새로 만들 서비스

```text
AdventureService
AdventureEventPicker
AdventureChoiceResolver
AdventureSettlementService
```

`AdventureService` 책임:

- 모험 시작
- 다음 이벤트 선택
- 선택지 적용
- 진행도 증가
- 귀환 가능 여부 판단
- 정산 요청

`AdventureChoiceResolver` 책임:

- 선택지 조건 검사
- 성공/실패/조건부 결과 계산
- 위험도와 피로도 변화 적용
- 임시 보상 추가
- 후속 이벤트 결정

`AdventureSettlementService` 책임:

- 임시 보상 최종 확정
- 실패 시 보상 손실 처리
- 인벤토리 지급
- 도감/발견 결과 생성

### 9.5 UI

권장 프리팹:

```text
AdventureScreen.prefab
AdventureChoiceButton.prefab
AdventureRewardRow.prefab
AdventureResultLogRow.prefab
AdventureSettlementPanel.prefab
```

화면 정보:

- 현재 지역
- 진행도
- 위험도
- 현재 이벤트 이름
- 상황 설명
- 선택지 버튼
- 선택 결과 연출 영역
- 임시 보상 목록
- 계속 진행 버튼
- 귀환 버튼

### 9.6 모험 진행 기본 규칙

1차 기본 규칙:

- 모험 시작 시 첫 이벤트를 뽑는다.
- 선택지를 고르면 결과가 적용된다.
- 결과 적용 후 `계속 진행` 또는 `귀환`을 선택한다.
- 계속 진행 시 진행도가 증가하고 다음 이벤트를 뽑는다.
- 위험도가 일정 수치 이상이면 실패 이벤트 또는 강제 귀환 가능성이 생긴다.
- 귀환하면 임시 보상이 정산된다.

### 9.7 완료 기준

- 영업 종료 후 모험 화면에 진입할 수 있다.
- 지역 1개에서 모험을 시작할 수 있다.
- 이벤트와 선택지가 데이터 기반으로 표시된다.
- 선택에 따라 위험도와 임시 보상이 변한다.
- 귀환 전에는 인벤토리에 지급되지 않는다.
- 귀환 정산 시 보상이 인벤토리에 지급된다.
- 같은 세션 시드와 같은 선택 순서는 같은 결과를 만든다.

### 9.8 사용자 결정 필요

- 1차 모험 지역 이름과 콘셉트
- 이벤트 6개 내용
- 각 이벤트 선택지 텍스트
- 선택지별 결과 텍스트
- 위험도 최대치와 실패 조건
- 실패 시 보상 손실 비율
- 전투 선택지 3종의 명칭과 결과 방향
- 귀환 버튼을 언제나 보여줄지, 일부 이벤트에서 숨길지

## 10. 5단계: 도감, 지식, 해금 연동

### 10.1 목표

재료 획득이 단순 인벤토리 수량 증가로 끝나지 않고, 요리 가능성과 지식 확장으로 이어지게 한다.

### 10.2 연결 후보

현재 연결 가능한 구조:

- `IngredientSO.Category`
- `IngredientSO.BaseTags`
- `CookingKnowledgeStore`
- 정보/도감 UI 계열 클래스

### 10.3 1차 적용 범위

1차는 아래 정도만 적용한다.

- 새 재료 최초 획득 여부 기록
- 정산 화면에서 `새 발견` 표시
- 도감 또는 지식 저장소에 발견 기록 추가

손질법, 레시피 힌트, 지역 해금은 2차로 미룬다.

### 10.4 사용자 결정 필요

- 새 재료 발견 연출 방식
- 도감에 바로 공개할 정보 범위
- 지역별 재료 특징을 텍스트로 보여줄지 여부
- 파견/모험으로 레시피 힌트를 해금할지 여부

## 11. 6단계: 저장, 검증, 테스트

### 11.1 저장

저장 데이터에는 Unity 오브젝트 참조를 직접 저장하지 않는다. ID와 값만 저장한다.

파견 저장:

```text
- saveVersion
- taskId
- regionId
- assignedAgentId
- assignedNpcId
- startDay
- returnDay
- status
- resultSeed
- claimedAtDay
```

모험 저장:

```text
- saveVersion
- sessionId
- regionId
- progress
- danger
- fatigue
- currentEventId
- temporaryRewards
- flags
- eventHistory
- sessionSeed
- status
```

1차에서는 프로젝트의 기존 저장 시스템이 명확하지 않으면 메모리 기반 저장소로 시작하고, 이후 실제 저장 시스템에 연결한다.

### 11.2 에디터 검증

검증 항목:

- ID 비어 있음
- ID 중복
- 보상 아이템 누락
- 보상 수량 범위 오류
- 확률/가중치 음수
- 파견 소요 일수 1 미만
- 모험 이벤트 선택지 없음
- 선택지 결과 없음
- 후속 이벤트 ID 누락
- 조건 참조 누락

### 11.3 테스트

최소 테스트:

- 파견 시작 시 `returnDay` 계산
- 현재 날짜가 `returnDay`에 도달하면 복귀 가능
- 파견 결과 시드 재현
- NPC 중복 파견 방지
- 모험 선택지 적용
- 모험 임시 보상과 정산 보상 분리
- 인벤토리 부족 시 미지급 결과 보존
- 조건부 이벤트 등장 여부

### 11.4 사용자 결정 필요

- 실제 저장 시스템 연결 시점
- 자동 저장이 필요한 타이밍
- 실패한 모험 세션을 로드했을 때 재개할지, 정산 화면으로 보낼지

## 12. 7단계: 밸런싱과 UI 연출 보강

### 12.1 목표

기능 검증이 끝난 뒤 플레이 감각을 다듬는다.

### 12.2 밸런싱 항목

- 파견 소요 영업일
- 파견 위험도별 실패 확률
- 희귀 보상 확률
- 모험 위험도 증가량
- 모험 진행도별 이벤트 난이도
- 모험 귀환 판단 난이도
- 하루 준비 단계에서 얻는 평균 재료량
- 인벤토리 슬롯 압박 정도

### 12.3 UI 연출 항목

- 파견 지역 카드 가독성
- NPC 보너스 표시 방식
- 복귀 가능 알림
- 모험 이벤트 결과 텍스트 연출
- 위험도 변화 피드백
- 새 재료 발견 표시
- 인벤토리 미지급 경고

### 12.4 사용자 결정 필요

- 화면 와이어프레임 또는 레이아웃 방향
- 파견 지도 연출 방식
- 모험 화면의 텍스트 톤
- 위험도 표현 방식
- 희귀 보상 연출 강도

## 13. 사용자 개입 체크리스트

아래 항목은 개발 전 또는 개발 중 사용자 확인이 필요하다.

### 13.1 개발 시작 전에 확정하면 좋은 것

- 하루에 파견과 모험을 모두 할 수 있는지
- 하루 파견 가능 NPC 수
- 파견 중 NPC가 손님으로 등장 가능한지
- 1차 파견 지역 3개
- 1차 파견 NPC 2명
- 1차 모험 지역 1개
- 모험 실패 시 보상 손실 정책
- 모험에 피로도/체력/도구를 넣을지 여부

### 13.2 데이터 작성 중 필요한 것

- 지역 표시명과 설명
- 지역별 주요 재료군
- 보상 아이템 목록
- 희귀 보상 후보
- 이벤트 상황 설명
- 선택지 문구
- 결과 문구
- 새 재료 발견 문구

### 13.3 UI 작업 전에 필요한 것

- 파견 화면 레이아웃 방향
- 모험 화면 레이아웃 방향
- 지도형 UI를 쓸지, 목록형 UI를 쓸지
- 결과창의 정보량
- 위험도 표시 방식
- 희귀 재료 표시 방식

### 13.4 나중에 미뤄도 되는 것

- 최종 아트 리소스
- 아이콘 완성본
- 사운드
- 세부 밸런스 수치
- 모든 지역과 이벤트의 완성 데이터
- 고급 도감/레시피 힌트 연동

## 14. 1차 개발 권장 스프린트

### Sprint 1: 준비 단계와 날짜 루프

작업:

- `PreparationPhaseController` 추가
- 영업 종료 이벤트 연결
- 준비 화면 임시 UI 또는 프리팹 연결
- 다음날 진행 연결

사용자 확인:

- 준비 단계에서 파견/모험 둘 다 가능한지
- 다음날 버튼의 문구와 흐름

완료 기준:

- 영업 종료 후 준비 화면으로 넘어간다.
- 다음날 시작이 가능하다.

### Sprint 2: 공통 보상 기반

작업:

- 보상 테이블 데이터 작성
- 시드 기반 랜덤 작성
- 보상 결과 모델 작성
- 인벤토리 지급 게이트웨이 작성

사용자 확인:

- 희귀도 명칭
- 확률 공개 방식

완료 기준:

- 보상 테이블을 시드로 굴려 결과를 만들 수 있다.
- 결과를 인벤토리에 지급하고 부분 지급을 표시할 수 있다.

### Sprint 3: 파견 MVP

작업:

- 파견 지역/에이전트 데이터
- 파견 작업 상태
- 파견 시작/복귀/수령
- 파견 UI
- 샘플 데이터 3지역/2NPC

사용자 확인:

- 지역과 NPC 콘셉트
- 파견 중 NPC 등장 정책
- 보상 데이터

완료 기준:

- 영업일 단위 파견이 동작한다.
- N일 뒤 결과를 수령할 수 있다.

### Sprint 4: 모험 MVP

작업:

- 모험 지역/이벤트/선택지 데이터
- 모험 세션
- 선택지 결과 적용
- 임시 보상
- 귀환 정산
- 샘플 이벤트 6개

사용자 확인:

- 모험 지역 콘셉트
- 이벤트/선택지 문구
- 위험도와 실패 정책

완료 기준:

- 모험이 선택지 기반으로 진행된다.
- 귀환 시 보상이 지급된다.

### Sprint 5: 저장, 검증, 테스트

작업:

- 파견 작업 저장
- 모험 세션 저장 또는 1차 메모리 정책 확정
- 에디터 검증
- 도메인 테스트

사용자 확인:

- 저장 타이밍
- 모험 중 종료/로드 정책

완료 기준:

- 주요 상태가 재현 가능하다.
- 데이터 오류를 에디터에서 확인할 수 있다.

## 15. 구현 중 금지 사항

- 손님 응대 중 부족한 재료를 이유로 파견/모험을 열지 않는다.
- UI 버튼 클릭 메서드에서 직접 보상 랜덤을 계산하지 않는다.
- `UnityEngine.Random`을 도메인 로직에서 직접 호출하지 않는다.
- 저장 데이터에 `ScriptableObject` 참조를 직접 저장하지 않는다.
- 기획 미정 사항을 코드 상수로 박아두지 않는다.
- 기존 `Assets/Work/Dispatch` 임시 UI를 최종 UI로 간주하지 않는다.

## 16. 최종 1차 완료 정의

아래 조건을 만족하면 파견/모험 1차 구현 완료로 본다.

- 영업 종료 후 준비 단계가 열린다.
- 준비 단계에서 파견을 시작할 수 있다.
- 파견은 영업일 단위로 진행된다.
- 파견 결과는 복귀 가능 상태에서 수령한다.
- 준비 단계에서 모험을 시작할 수 있다.
- 모험은 이벤트와 선택지 기반으로 진행된다.
- 모험 보상은 귀환/종료 정산 시 지급된다.
- 파견과 모험 보상은 같은 공통 보상 모델을 사용한다.
- 인벤토리 지급 결과가 UI에 표시된다.
- 새 재료 발견 여부를 결과에 남길 수 있다.
- 손님 응대 중 즉시 수급 루프가 생기지 않는다.

## 17. 다음 작업 제안

이 문서 기준으로 바로 개발을 시작한다면 첫 작업은 `Sprint 1: 준비 단계와 날짜 루프`다.

첫 구현에서 만들어야 하는 최소 파일:

```text
Assets/Work/MaterialAcquisition/Code/Integration/PreparationPhaseController.cs
Assets/Work/MaterialAcquisition/Code/Integration/PreparationPhaseState.cs
Assets/Work/MaterialAcquisition/Code/UI/PreparationPhaseView.cs
```

첫 구현 전에 사용자에게 확인받을 질문:

1. 하루에 파견과 모험을 둘 다 할 수 있게 할 것인가?
2. 1차에서 하루 파견 가능 NPC 수는 1명으로 둘 것인가?
3. 파견 중인 NPC는 손님으로 등장하지 않게 할 것인가?
4. 모험 실패 시 임시 보상은 일부 손실로 둘 것인가?
