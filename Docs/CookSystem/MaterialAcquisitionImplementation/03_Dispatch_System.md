# 03. 파견 시스템 구현 명세

## 1. 목표

파견은 NPC를 지역에 보내고, 지정된 영업일이 지난 뒤 재료를 가져오는 장기 수급 시스템이다.

1차 구현 목표:

- 파견 지역 3개
- 파견 가능 NPC 2명
- 지역과 NPC 선택
- 영업일 단위 진행
- 복귀 가능 상태
- 결과 수령
- 공통 보상 모델을 통한 인벤토리 지급

## 2. 기존 파견 프로토타입과의 차이

| 항목 | 기존 `Assets/Work/Dispatch` | 최종 파견 |
| --- | --- | --- |
| 진행 시간 | 초 단위 `durationSeconds` | 영업일 단위 `requiredDays` |
| 보상 지급 | 타이머 종료 즉시 지급 | 복귀 결과 수령 시 지급 |
| NPC | 없음 | NPC 또는 파견 에이전트 배정 |
| 상태 | 단일 진행 상태 | InProgress, ReadyToReturn, Completed |
| 저장 | 없음 | taskId, regionId, npcId, returnDay 저장 |
| 보상 | 고정 리스트 | 공통 보상 테이블 + 희귀/실패/보너스 |

기존 프로토타입은 참고하되 최종 구현은 새 `MaterialAcquisition` 구조에서 진행한다.

## 3. 신규 폴더

```text
Assets/Work/MaterialAcquisition/Code/Dispatch/
Assets/Work/MaterialAcquisition/Data/Dispatch/
Assets/Work/MaterialAcquisition/Prefabs/Dispatch/
```

## 4. 데이터 구조

### 4.1 `DispatchRegionSO`

역할:

- 파견 가능한 지역 하나를 정의한다.

필드:

```text
- string regionId
- string displayName
- string description
- Sprite icon
- int requiredDays
- DispatchDangerLevel dangerLevel
- AcquisitionRewardTableSO commonRewardTable
- AcquisitionRewardTableSO rareRewardTable
- List<AcquisitionCondition> unlockConditions
- List<string> recommendedTraitIds
- string previewRewardGroupText
- string flavorText
```

규칙:

- `regionId`는 저장과 참조에 사용하므로 변경에 주의한다.
- `requiredDays`는 1 이상.
- `commonRewardTable`은 1차에서 필수.
- `rareRewardTable`은 없어도 되지만 희귀 보상 표시가 비활성화된다.

### 4.2 `DispatchDangerLevel`

```text
Low
Normal
High
Extreme
```

기본 실패 확률 제안:

| 위험도 | 실패 확률 | 부분 성공 확률 | 희귀 보너스 |
| --- | ---: | ---: | ---: |
| Low | 0.00 | 0.10 | 0.00 |
| Normal | 0.05 | 0.15 | 0.02 |
| High | 0.12 | 0.25 | 0.06 |
| Extreme | 0.20 | 0.35 | 0.12 |

사용자 결정 필요:

- 실패 확률을 이 수치로 시작할지
- 위험도가 높을수록 희귀 보상이 좋아지는 방향이 맞는지

### 4.3 `DispatchAgentSO`

역할:

- 파견에 보낼 수 있는 NPC 또는 에이전트를 정의한다.
- 기존 `NpcData`와는 `npcId` 문자열로 연결한다.

필드:

```text
- string agentId
- string displayName
- string npcId
- Sprite portrait
- List<string> traitIds
- List<DispatchAgentBonus> bonuses
- bool availableByDefault
- List<AcquisitionCondition> unlockConditions
```

규칙:

- `agentId`는 파견 시스템 내부 ID다.
- `npcId`가 있으면 기존 NPC와 연결된다.
- 1차에서는 `NpcData`를 수정하지 않는다.

### 4.4 `DispatchAgentBonus`

필드:

```text
- DispatchAgentBonusType type
- string targetId
- float value
- string displayText
```

`DispatchAgentBonusType`:

```text
RewardAmountMultiplier
RewardChanceMultiplier
RareChanceBonus
DangerFailureReduction
RequiredDayReduction
MinimumRewardGuarantee
```

1차 지원 범위:

- `RewardAmountMultiplier`
- `RareChanceBonus`
- `DangerFailureReduction`

2차 이후:

- 특정 지역 보너스
- 특정 재료군 보너스
- 파견 일수 감소
- 실패 시 최소 보상 보장

## 5. 런타임 모델

### 5.1 `DispatchTaskStatus`

```text
InProgress
ReadyToReturn
Completed
Cancelled
```

설명:

| 상태 | 설명 |
| --- | --- |
| `InProgress` | NPC가 파견 중 |
| `ReadyToReturn` | 현재 영업일이 복귀일 이상이라 결과 수령 가능 |
| `Completed` | 결과를 수령하고 보상 지급까지 끝남 |
| `Cancelled` | 취소됨. 1차에서는 사용하지 않아도 됨 |

### 5.2 `DispatchTask`

필드:

```text
- string taskId
- string regionId
- string assignedAgentId
- string assignedNpcId
- int startDay
- int returnDay
- DispatchTaskStatus status
- int resultSeed
- bool rewardClaimed
- int claimedDay
```

규칙:

- `taskId`는 생성 시 고유해야 한다.
- `returnDay = startDay + requiredDays`.
- `currentDay >= returnDay`이면 `ReadyToReturn`으로 갱신 가능하다.
- 결과 수령 전까지 인벤토리에 보상을 지급하지 않는다.

### 5.3 `DispatchTaskResult`

필드:

```text
- string taskId
- string regionId
- string assignedAgentId
- DispatchOutcomeType outcomeType
- IReadOnlyList<AcquisitionRewardRoll> rewardRolls
- string resultTitle
- string resultDescription
- bool hasRareReward
- bool isPartialSuccess
- bool isFailure
```

`DispatchOutcomeType`:

```text
Success
PartialSuccess
Failure
GreatSuccess
```

## 6. 서비스

### 6.1 `DispatchService`

필드:

```text
- DispatchRegionRegistry regionRegistry
- DispatchAgentRegistry agentRegistry
- DispatchTaskRepository taskRepository
- DispatchResultResolver resultResolver
- AcquisitionInventoryGateway inventoryGateway
- IAcquisitionDayProvider dayProvider
```

공개 메서드:

```text
bool CanStartDispatch(string regionId, string agentId, out string reason)
DispatchTask TryStartDispatch(string regionId, string agentId, out string reason)
void RefreshTasksForCurrentDay()
IReadOnlyList<DispatchTask> GetActiveTasks()
IReadOnlyList<DispatchTask> GetReadyToReturnTasks()
DispatchTaskResult BuildResultPreview(string taskId)
AcquisitionRewardResult TryClaimResult(string taskId, out string reason)
bool IsAgentBusy(string agentId)
```

책임:

- 파견 가능 여부 검사
- 파견 작업 생성
- NPC 중복 배정 방지
- 날짜 기준 상태 갱신
- 결과 수령 처리
- 공통 인벤토리 게이트웨이 호출

### 6.2 `DispatchTaskRepository`

1차는 메모리 저장소로 시작 가능하다.

필드:

```text
- List<DispatchTask> tasks
```

메서드:

```text
Add(DispatchTask task)
FindById(string taskId)
GetActiveTasks()
GetReadyTasks()
GetTasksByAgent(string agentId)
Update(DispatchTask task)
```

이후 저장 시스템과 연결할 때 `DispatchSaveData`로 직렬화한다.

### 6.3 `DispatchResultResolver`

공개 메서드:

```text
DispatchTaskResult Resolve(
    DispatchTask task,
    DispatchRegionSO region,
    DispatchAgentSO agent)
```

처리 순서:

1. `resultSeed`로 랜덤 생성
2. 위험도 기반 결과 유형 판정
3. NPC 보너스 적용
4. 기본 보상 테이블 계산
5. 희귀 보상 테이블 계산
6. 실패/부분 성공에 따른 보상 조정
7. 결과 텍스트 생성

### 6.4 결과 판정 기본 규칙

1차 기본:

```text
실패 판정
-> 실패면 보상 없음 또는 최소 보상
-> 실패가 아니면 부분 성공 판정
-> 부분 성공이면 보상 수량 감소, 희귀 보상 제외
-> 일반 성공이면 기본 보상 + 희귀 보상 확률 판정
-> 대성공은 2차로 미룸
```

실패 시 기본 제안:

- `Low`: 실패 없음
- `Normal` 이상: 실패 가능
- 실패해도 결과 텍스트는 반드시 표시
- 실패 보상은 1차에서는 없음

사용자 결정 필요:

- 실패해도 최소 보상을 줄지
- 실패 시 NPC 부상/피로 같은 페널티를 줄지

## 7. NPC와 손님 등장 정책

### 7.1 정책 객체

```text
DispatchNpcAvailabilityPolicy
- BlocksNpcVisit
- AllowsNpcVisitWithPenalty
- IgnoresNpcVisit
```

1차 기본 제안:

```text
BlocksNpcVisit
```

즉, 파견 중인 NPC는 손님으로 등장하지 않는다.

### 7.2 기존 NPC 시스템 연결

기존 `NpcEncounterDirector`의 손님 풀 필터링 기능이 직접 제공되지 않으면, 1차에서는 아래 중 하나를 선택한다.

1. 파견 NPC를 손님으로 쓰지 않는 별도 에이전트로 둔다.
2. `NpcEncounterDirector`에 제외 NPC ID 목록을 제공하는 확장 지점을 추가한다.
3. 1차에서는 정책만 문서화하고 실제 손님 제외는 2차로 미룬다.

권장:

- 기능 일관성을 위해 2번이 좋다.
- 단, NPC 등장 로직을 크게 건드려야 하면 1차에서는 1번으로 간다.

사용자 결정 필요:

- 파견 NPC와 손님 NPC를 같은 인물로 볼 것인가?

## 8. UI

### 8.1 화면 구성

`DispatchScreen`:

```text
상단: 파견 제목, 현재 날짜, 닫기
좌측: 파견 지역 목록
중앙: 선택 지역 상세
우측: 파견 가능 NPC 목록
하단: 진행 중/복귀 가능 파견 목록
```

### 8.2 지역 카드 표시

필수 정보:

- 지역 이름
- 설명
- 예상 소요 영업일
- 위험도
- 주요 재료군
- 희귀 보상 가능성
- 추천 특성

### 8.3 NPC 슬롯 표시

필수 정보:

- NPC 이름
- 초상화 또는 임시 아이콘
- 특성
- 선택 지역에 대한 보너스
- 현재 파견 중 여부

### 8.4 파견 작업 행 표시

필수 정보:

- 지역 이름
- 배정 NPC
- 출발일
- 복귀 예정일
- 현재 상태
- 결과 수령 버튼

## 9. 데이터 1차 샘플 제안

사용자 확정 전 임시 샘플:

| 지역 ID | 이름 | 소요 | 위험도 | 주요 보상 |
| --- | --- | ---: | --- | --- |
| `near_forest` | 근처 숲 | 1 | Low | 버섯, 허브, 열매 |
| `wet_cavern` | 축축한 동굴 | 2 | Normal | 점액, 광물염, 희귀 버섯 |
| `salt_cavern` | 소금 동굴 | 2 | Normal | 암염, 광물염, 동굴 버섯 |

NPC 임시 샘플:

| 에이전트 ID | 이름 | 특성 | 보너스 |
| --- | --- | --- | --- |
| `moss_runner` | 이끼길잡이 | 숲길 익숙함 | 보상 수량 +10% |
| `cave_scavenger` | 동굴수색꾼 | 동굴 감각 | 희귀 확률 +5% |

사용자 결정 필요:

- 실제 기존 NPC 이름으로 바꿀지
- 세계관에 맞는 지역명을 다시 정할지

## 10. 저장

`DispatchSaveData`:

```text
- int saveVersion
- List<DispatchTaskSaveData> tasks
```

`DispatchTaskSaveData`:

```text
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

저장 시점:

- 파견 시작 직후
- 다음날 진행 직후
- 결과 수령 직후

## 11. 완료 기준

- 준비 단계에서만 파견 화면을 열 수 있다.
- 지역 3개가 데이터로 표시된다.
- NPC 2명을 선택할 수 있다.
- 바쁜 NPC는 다시 선택할 수 없다.
- 파견 시작 시 작업이 생성된다.
- 파견 시작 시 보상은 지급되지 않는다.
- 날짜가 복귀일에 도달하면 `ReadyToReturn`이 된다.
- 결과 수령 시 보상이 지급된다.
- 인벤토리 미지급 수량이 결과에 표시된다.
- 같은 `resultSeed`로 같은 결과가 나온다.

## 12. 테스트 기준

필수 테스트:

- `returnDay = startDay + requiredDays`
- 같은 NPC를 동시에 두 번 파견할 수 없다.
- `currentDay < returnDay`이면 수령할 수 없다.
- `currentDay >= returnDay`이면 수령할 수 있다.
- 결과 수령 후 상태가 `Completed`가 된다.
- 결과 수령을 두 번 할 수 없다.
- 실패 결과도 결과 객체를 만든다.
- 부분 성공은 희귀 보상을 제외한다.
- NPC 보너스가 보상 계산 컨텍스트에 반영된다.

## 13. 사용자 결정 필요 체크

구현 전 확인:

- 1차 파견 지역 3개는 무엇인가?
- 지역별 소요 영업일은 몇 일인가?
- 지역별 위험도는 어떻게 둘 것인가?
- 지역별 기본 보상과 희귀 보상은 무엇인가?
- 1차 파견 NPC 2명은 누구인가?
- 파견 중 NPC는 손님으로 등장하지 않는가?
- 실패 시 보상 없음으로 둘 것인가, 최소 보상으로 둘 것인가?
- 파견 결과 수령을 보류할 수 있는가?

기본 제안:

```text
지역: 근처 숲, 축축한 동굴, 소금 동굴
NPC: 기존 NPC 2명 또는 임시 파견 에이전트 2명
파견 중 NPC: 손님 등장 불가
실패: 1차는 보상 없음
결과 수령: 보류 가능
```
