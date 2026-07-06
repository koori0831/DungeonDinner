# 04. 모험 시스템 구현 명세

## 1. 목표

모험은 플레이어가 직접 지역을 진행하며 이벤트를 만나고, 선택지에 따라 위험과 보상을 조절하는 즉시 수급 콘텐츠다.

1차 구현 목표:

- 모험 지역 1개
- 이벤트 6개 이상
- 이벤트당 선택지 2~3개
- 선택지 결과 적용
- 위험도 변화
- 임시 보상
- 계속 진행/귀환 선택
- 귀환 또는 종료 정산
- 인벤토리 재료 지급

## 2. 핵심 원칙

- 모험은 영업 종료 후 준비 단계에서만 시작한다.
- 모험 중 얻은 보상은 인벤토리에 즉시 지급하지 않는다.
- 보상은 `temporaryRewards`에 쌓이고, 귀환/종료 정산 시 지급한다.
- 선택지는 단순 성공/실패가 아니라 위험, 보상, 후속 이벤트, 플래그를 바꿀 수 있다.
- 자유 이동 맵이 아니라 이벤트 카드/텍스트 기반 진행으로 1차 구현한다.

## 3. 신규 폴더

```text
Assets/Work/MaterialAcquisition/Code/Adventure/
Assets/Work/MaterialAcquisition/Data/Adventure/
Assets/Work/MaterialAcquisition/Prefabs/Adventure/
```

## 4. 데이터 구조

### 4.1 `AdventureRegionSO`

역할:

- 모험 가능한 지역 하나를 정의한다.

필드:

```text
- string regionId
- string displayName
- string description
- Sprite icon
- int baseDanger
- int maxDanger
- int maxProgress
- List<AdventureEventSO> eventPool
- AcquisitionRewardTableSO returnBonusRewardTable
- List<AcquisitionCondition> unlockConditions
- string rewardThemeText
```

규칙:

- `maxProgress`에 도달하면 지역 종료 또는 자동 귀환 가능 상태가 된다.
- `eventPool`은 1개 이상이어야 한다.
- `baseDanger`는 모험 시작 위험도다.

### 4.2 `AdventureEventSO`

역할:

- 모험 중 발생하는 하나의 상황을 정의한다.

필드:

```text
- string eventId
- string displayName
- string description
- AdventureEventType eventType
- List<AdventureChoice> choices
- int weight
- List<AcquisitionCondition> requiredConditions
- List<AcquisitionCondition> blockConditions
- bool canRepeatInSession
- string forcedNextEventId
```

`AdventureEventType`:

```text
Gathering
Discovery
Combat
Hazard
Rest
Story
```

규칙:

- `choices`는 1개 이상.
- `weight`는 0 이상.
- `canRepeatInSession`이 false면 같은 세션에서 반복 등장하지 않는다.

### 4.3 `AdventureChoice`

필드:

```text
- string choiceId
- string displayText
- string resultText
- AdventureChoiceResultType resultType
- float successRate
- AcquisitionRewardTableSO rewardTable
- int dangerDelta
- int fatigueDelta
- int progressDelta
- string nextEventId
- List<AdventureFlagChange> flagChanges
- List<AcquisitionCondition> requiredConditions
- List<AcquisitionCondition> blockConditions
```

`AdventureChoiceResultType`:

```text
Guaranteed
Chance
Conditional
Chain
Escape
Return
```

1차 지원:

- `Guaranteed`
- `Chance`
- `Chain`
- `Return`

2차 이후:

- `Conditional`
- 숨겨진 결과
- 복합 플래그 결과

### 4.4 `AdventureFlagChange`

필드:

```text
- string flagId
- bool value
```

사용 예:

- `found_slime_trail`
- `disturbed_spores`
- `heard_cave_echo`

1차에서는 플래그가 후속 이벤트 조건에만 쓰인다.

## 5. 런타임 모델

### 5.1 `AdventureSessionStatus`

```text
NotStarted
InProgress
AwaitingChoice
ReadyToContinue
Returning
Settled
Failed
```

### 5.2 `AdventureSession`

필드:

```text
- string sessionId
- string regionId
- int currentProgress
- int danger
- int fatigue
- string currentEventId
- List<AcquisitionRewardRoll> temporaryRewards
- HashSet<string> flags
- List<string> eventHistory
- List<string> choiceHistory
- int sessionSeed
- AdventureSessionStatus status
```

규칙:

- 세션 시작 시 `sessionSeed`를 생성한다.
- 이벤트 선택과 보상 계산은 세션 랜덤을 사용한다.
- 정산 전까지 `temporaryRewards`는 인벤토리에 지급되지 않는다.

### 5.3 `AdventureChoiceApplyResult`

필드:

```text
- string eventId
- string choiceId
- bool success
- string resultText
- int dangerBefore
- int dangerAfter
- int progressBefore
- int progressAfter
- IReadOnlyList<AcquisitionRewardRoll> addedTemporaryRewards
- string nextEventId
- bool shouldReturn
- bool shouldFail
```

### 5.4 `AdventureSettlementResult`

필드:

```text
- string sessionId
- string regionId
- AdventureSettlementType settlementType
- AcquisitionRewardResult inventoryResult
- int lostRewardAmount
- bool hasNewDiscovery
- string summaryText
```

`AdventureSettlementType`:

```text
ReturnedSafely
CompletedRegion
FailedWithLoss
ForcedReturn
```

## 6. 서비스

### 6.1 `AdventureService`

필드:

```text
- AdventureRegionRegistry regionRegistry
- AdventureSessionRepository sessionRepository
- AdventureEventPicker eventPicker
- AdventureChoiceResolver choiceResolver
- AdventureSettlementService settlementService
- IAcquisitionDayProvider dayProvider
```

공개 메서드:

```text
bool CanStartAdventure(string regionId, out string reason)
AdventureSession TryStartAdventure(string regionId, out string reason)
AdventureEventSO GetCurrentEvent(string sessionId)
AdventureChoiceApplyResult TrySelectChoice(string sessionId, string choiceId, out string reason)
bool TryContinue(string sessionId, out string reason)
AdventureSettlementResult TryReturnAndSettle(string sessionId, out string reason)
AdventureSettlementResult TryFailAndSettle(string sessionId, out string reason)
```

책임:

- 모험 세션 생성
- 현재 이벤트 관리
- 선택지 적용 요청
- 계속 진행 처리
- 귀환/실패 정산 처리

### 6.2 `AdventureEventPicker`

공개 메서드:

```text
AdventureEventSO PickNextEvent(
    AdventureRegionSO region,
    AdventureSession session,
    IAcquisitionRandom random)
```

선택 규칙:

1. 지역 이벤트 풀에서 후보 수집
2. 조건 불만족 이벤트 제거
3. 세션에서 반복 금지 이벤트 제거
4. 위험도/진행도에 따른 가중치 보정
5. 가중치 선택

1차에서는 위험도/진행도 보정은 단순하게 둔다.

기본 보정:

```text
danger >= maxDanger * 0.7이면 Hazard/Combat 가중치 +20%
progress >= maxProgress * 0.7이면 Discovery 가중치 +10%
```

### 6.3 `AdventureChoiceResolver`

공개 메서드:

```text
AdventureChoiceApplyResult ApplyChoice(
    AdventureSession session,
    AdventureEventSO currentEvent,
    AdventureChoice choice,
    IAcquisitionRandom random)
```

처리 순서:

1. 선택지 조건 검사
2. 성공/실패 판정
3. 결과 텍스트 선택
4. 위험도 변화 적용
5. 피로도 변화 적용
6. 진행도 변화 적용
7. 보상 테이블 계산
8. 임시 보상 추가
9. 플래그 변경
10. 후속 이벤트 또는 귀환/실패 상태 결정

### 6.4 `AdventureSettlementService`

공개 메서드:

```text
AdventureSettlementResult SettleReturn(AdventureSession session)
AdventureSettlementResult SettleFailure(AdventureSession session)
```

귀환 정산:

- 임시 보상을 모두 인벤토리에 지급
- 새 발견 기록
- 세션 상태 `Settled`

실패 정산:

- 정책에 따라 임시 보상 일부 제거
- 남은 보상 지급
- 세션 상태 `Failed` 또는 `Settled`

## 7. 모험 진행 규칙

### 7.1 시작

```text
준비 단계에서 모험 선택
-> 지역 선택
-> AdventureService.TryStartAdventure()
-> 첫 이벤트 선택
-> 상태 AwaitingChoice
```

### 7.2 선택지 선택

```text
선택지 클릭
-> AdventureChoiceResolver.ApplyChoice()
-> 결과 로그 표시
-> 상태 ReadyToContinue 또는 Returning 또는 Failed
```

### 7.3 계속 진행

```text
계속 진행 클릭
-> 진행도 증가
-> 위험도 확인
-> 다음 이벤트 선택
-> 상태 AwaitingChoice
```

### 7.4 귀환

```text
귀환 클릭
-> AdventureSettlementService.SettleReturn()
-> 인벤토리 지급
-> 결과 화면 표시
-> 준비 단계로 복귀
```

### 7.5 실패

실패 발생 조건 기본 제안:

```text
danger >= maxDanger
```

실패 처리 기본 제안:

- 임시 보상 총 수량의 50% 손실
- 희귀 보상부터 잃을지, 무작위로 잃을지는 사용자 결정 필요
- 1차는 무작위 손실

## 8. 전투 이벤트

전투는 별도 전투 시스템이 아니라 `AdventureEventType.Combat`으로 처리한다.

전투 선택지 예:

| 선택지 | 결과 방향 |
| --- | --- |
| 베기 | 살점/고기류 보상 증가, 핵 손상 가능 |
| 찌르기 | 핵/정밀 부위 보상 증가, 반격 위험 |
| 둔기 공격 | 뼈/껍질 보상 증가, 점액류 감소 |
| 포획 시도 | 희귀 생물 재료 가능, 실패 시 위험 증가 |
| 도망 | 보상 없음, 위험 일부 감소 또는 유지 |

1차 전투 선택지:

```text
베기
찌르기
도망
```

사용자 결정 필요:

- 1차에 둔기/포획까지 넣을지
- 전투 실패 시 즉시 모험 실패인지, 위험도만 증가인지

기본 제안:

- 1차는 위험도 증가만.
- 위험도 최대치에 도달하면 실패 정산.

## 9. UI

### 9.1 화면 구성

`AdventureScreen`:

```text
상단: 지역명, 진행도, 위험도
중앙: 현재 이벤트 이름과 설명
중앙 하단: 선택지 버튼 목록
우측 또는 하단: 임시 보상 목록
하단: 계속 진행, 귀환 버튼
```

### 9.2 이벤트 표시

필수:

- 이벤트 이름
- 상황 설명
- 선택지 텍스트
- 조건 미충족 선택지는 숨기거나 비활성화

기본 제안:

- 조건 미충족 선택지는 1차에서 숨긴다.

### 9.3 결과 로그

선택 후 표시:

- 결과 텍스트
- 위험도 변화
- 획득한 임시 보상
- 다음 행동 안내

예:

```text
점액 웅덩이를 조심히 병에 담았다.
임시 획득: 점액 점액질 x2
위험도 +1
```

### 9.4 정산 화면

필수:

- 총 획득 재료
- 손실 재료
- 미지급 재료
- 새 발견
- 확인 버튼

## 10. 1차 샘플 지역과 이벤트

임시 지역:

```text
regionId: wet_cavern
displayName: 축축한 동굴
baseDanger: 0
maxDanger: 10
maxProgress: 5
```

이벤트 6개 제안:

| ID | 이름 | 타입 | 선택지 예 |
| --- | --- | --- | --- |
| `glowing_slime_pool` | 빛나는 점액 웅덩이 | Gathering | 병에 담는다 / 농축해본다 / 지나간다 |
| `suspicious_mushroom_patch` | 수상한 버섯 군락 | Gathering | 조심히 채집 / 깊숙이 파본다 / 지나간다 |
| `monster_tracks` | 마물의 흔적 | Discovery | 추적한다 / 우회한다 / 덫을 놓는다 |
| `cave_slime_larva` | 동굴 점액충 | Combat | 베기 / 찌르기 / 도망 |
| `salt_crystal_wall` | 소금 결정 벽 | Gathering | 긁어낸다 / 크게 떼어낸다 / 둔다 |
| `unstable_ceiling` | 불안한 천장 | Hazard | 조심히 통과 / 빠르게 지나간다 / 돌아간다 |

사용자 결정 필요:

- 이벤트 이름과 텍스트가 세계관에 맞는지
- 점액/버섯/소금 중심의 첫 지역이 맞는지

## 11. 저장

`AdventureSaveData`:

```text
- int saveVersion
- AdventureSessionSaveData activeSession
- List<string> completedSessionIds
```

`AdventureSessionSaveData`:

```text
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

저장 정책 사용자 결정 필요:

- 모험 중 게임 종료 후 같은 이벤트에서 재개할지
- 모험 중 종료하면 강제 귀환 처리할지

기본 제안:

- 1차는 진행 중 세션 저장.
- 로드 시 같은 이벤트에서 재개.

## 12. 완료 기준

- 준비 단계에서만 모험을 시작할 수 있다.
- 지역 1개가 표시된다.
- 이벤트 6개가 데이터로 존재한다.
- 이벤트 선택지가 표시된다.
- 선택지 선택 시 위험도와 임시 보상이 변한다.
- 귀환 전에는 인벤토리에 보상이 들어가지 않는다.
- 귀환 정산 시 인벤토리에 보상이 지급된다.
- 실패 시 정책에 따라 일부 보상이 손실된다.
- 결과 화면에 획득/손실/미지급/새 발견이 표시된다.
- 같은 시드와 같은 선택 순서는 같은 결과를 만든다.

## 13. 테스트 기준

필수 테스트:

- 모험 시작 시 첫 이벤트가 선택된다.
- 조건 미충족 이벤트는 선택되지 않는다.
- 반복 금지 이벤트는 세션에서 반복되지 않는다.
- 선택지 적용 후 위험도가 올바르게 변한다.
- 선택지 보상이 임시 보상에 들어간다.
- 정산 전에는 인벤토리 지급이 일어나지 않는다.
- 귀환 정산 후 인벤토리 지급이 일어난다.
- 실패 정산 시 보상 손실이 적용된다.
- `danger >= maxDanger`이면 실패 상태가 된다.
- 같은 세션 시드와 선택 순서는 같은 결과를 만든다.

## 14. 사용자 결정 필요 체크

구현 전 확인:

- 1차 모험 지역은 `축축한 동굴`로 할 것인가?
- 이벤트 6개 콘셉트는 위 제안으로 시작할 것인가?
- 모험 실패 조건은 위험도 최대치 도달로 둘 것인가?
- 실패 시 임시 보상 손실률은 50%로 둘 것인가?
- 실패 시 희귀 보상을 우선 잃는가, 무작위로 잃는가?
- 전투 선택지는 1차에 몇 개까지 넣을 것인가?
- 귀환 버튼은 항상 보이게 할 것인가?

기본 제안:

```text
지역: 축축한 동굴
실패 조건: danger >= maxDanger
실패 손실: 임시 보상 50% 무작위 손실
전투 선택지: 베기/찌르기/도망
귀환 버튼: 강제 이벤트 외 항상 표시
```
