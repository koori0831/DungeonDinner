# 01. 준비 단계와 게임 루프 구현 명세

## 1. 목표

파견과 모험은 영업 중 즉시 재료를 보충하는 기능이 아니다. 따라서 가장 먼저 구현해야 하는 것은 `영업 종료 후 준비 단계`다.

이 단계의 목표:

- 손님 응대가 모두 끝나면 준비 단계로 진입한다.
- 준비 단계에서만 파견과 모험을 열 수 있다.
- 준비 단계에서 다음 영업일로 넘어갈 수 있다.
- 파견 복귀 가능 상태 갱신은 날짜 진행과 연결한다.

## 2. 현재 연결 가능한 기존 구조

| 기존 구조 | 역할 | 연결 방식 |
| --- | --- | --- |
| `CookingBusinessFlowController` | 손님 진행과 영업 종료 | `BusinessClosed` 이벤트 수신 |
| `NpcEncounterDirector` | 현재 영업일 관리 | `CurrentDay`, `AdvanceDay()` 사용 |
| `CookingGamePanel` | 요리 UI 상태 | 준비 단계 진입 시 요리 UI 닫기 |
| `NpcConversationRunner` | 손님 대화 상태 | 대화 중 준비 단계 진입 차단 |

## 3. 신규 클래스 목록

권장 위치:

```text
Assets/Work/MaterialAcquisition/Code/Integration/
Assets/Work/MaterialAcquisition/Code/UI/
```

### 3.1 `PreparationPhaseState`

```text
None
BusinessOpen
BusinessClosing
PreparationOpen
DispatchOpen
AdventureOpen
AdvancingDay
```

역할:

- 현재 루프가 영업 중인지, 준비 단계인지, 파견/모험 화면인지 구분한다.
- UI 활성화와 버튼 상태 판단에 사용한다.

### 3.2 `IAcquisitionDayProvider`

```text
int CurrentDay { get; }
string CurrentDayText { get; }
void AdvanceDay();
```

역할:

- 재료 획득 시스템이 `NpcEncounterDirector`에 직접 강하게 묶이지 않도록 날짜 조회를 추상화한다.

### 3.3 `NpcEncounterDayProvider`

필드:

```text
- NpcEncounterDirector encounterDirector
```

역할:

- `IAcquisitionDayProvider` 구현체
- `CurrentDay`를 `encounterDirector.CurrentDay`에서 가져온다.
- `AdvanceDay()` 호출 시 `encounterDirector.AdvanceDay()`를 호출한다.

### 3.4 `PreparationPhaseController`

필드:

```text
- CookingBusinessFlowController businessFlowController
- CookingGamePanel cookingGamePanel
- NpcConversationRunner npcRunner
- MonoBehaviour dayProviderBehaviour
- PreparationPhaseView view
- bool allowDispatchAndAdventureSameDay
- int maxDispatchStartsPerDay
- int maxAdventureStartsPerDay
```

런타임 상태:

```text
- PreparationPhaseState currentState
- int activePreparationDay
- int dispatchStartsToday
- int adventureStartsToday
- bool hasOpenedPreparationToday
```

이벤트:

```text
- PreparationPhaseOpened
- PreparationPhaseClosed
- DayAdvanced
- DispatchRequested
- AdventureRequested
```

책임:

- 영업 종료 이벤트를 받아 준비 단계를 연다.
- 손님 대화 중이면 준비 단계 진입을 막는다.
- 파견/모험 화면 진입 가능 여부를 판단한다.
- 다음날 진행 시 날짜를 증가시킨다.
- 다음날 시작 후 영업 상태를 복구한다.

### 3.5 `PreparationPhaseView`

필드:

```text
- GameObject root
- TextMeshProUGUI titleText
- TextMeshProUGUI dayText
- TextMeshProUGUI summaryText
- Button dispatchButton
- Button adventureButton
- Button nextDayButton
- Button closeButton
```

책임:

- 준비 단계 상태 표시
- 버튼 클릭을 `PreparationPhaseController`로 전달
- 가능하지 않은 행동은 버튼 비활성화와 사유 텍스트로 표시

주의:

- 버튼 클릭 메서드에서 날짜를 직접 바꾸지 않는다.
- 파견/모험 서비스를 직접 호출하지 않는다.
- 보상 계산을 하지 않는다.

## 4. 기본 흐름

### 4.1 영업 종료 후 준비 단계 진입

```text
CookingBusinessFlowController.CloseShop()
-> BusinessClosed 이벤트
-> PreparationPhaseController.OpenPreparationPhase()
-> CookingGamePanel.CloseCookingViews()
-> PreparationPhaseView.Show()
```

진입 조건:

- 현재 손님 대화가 없어야 한다.
- 현재 상태가 이미 준비 단계가 아니어야 한다.
- 현재 영업일이 유효해야 한다.

### 4.2 파견 화면 열기

```text
플레이어가 파견 버튼 클릭
-> CanOpenDispatch() 확인
-> DispatchRequested 이벤트 발생
-> DispatchScreen 표시
-> 상태 DispatchOpen
```

`CanOpenDispatch()` 기본 조건:

- 준비 단계가 열려 있다.
- 오늘 파견 시작 횟수가 제한보다 적다.
- 모험 중이 아니다.
- 파견 화면이 이미 열려 있지 않다.

### 4.3 모험 화면 열기

```text
플레이어가 모험 버튼 클릭
-> CanOpenAdventure() 확인
-> AdventureRequested 이벤트 발생
-> AdventureScreen 표시
-> 상태 AdventureOpen
```

`CanOpenAdventure()` 기본 조건:

- 준비 단계가 열려 있다.
- 오늘 모험 시작 횟수가 제한보다 적다.
- 파견 화면이 열려 있지 않다.
- `allowDispatchAndAdventureSameDay` 정책을 만족한다.

### 4.4 다음날 시작

```text
플레이어가 다음날 버튼 클릭
-> CanAdvanceDay() 확인
-> 복귀 가능 파견 상태 갱신 요청
-> dayProvider.AdvanceDay()
-> 준비 단계 닫기
-> 영업 시작 상태 복구
```

`CanAdvanceDay()` 기본 조건:

- 준비 단계가 열려 있다.
- 모험 세션이 진행 중이면 안 된다.
- 파견 결과 수령 강제 정책이 켜져 있다면, 미수령 복귀 결과가 없어야 한다.

## 5. 기존 `CookingBusinessFlowController` 보강 필요 사항

현재 구조에서는 `CloseShop()` 이후 `_businessClosed`가 true가 되며 다음 영업일을 다시 시작하는 공개 흐름이 부족하다.

권장 추가 메서드:

```text
public void OpenShopForNextDay(bool startFirstCustomer)
```

동작:

```text
_businessClosed = false
_dishHandedToCurrentCustomer = false
HideActions()
SetStatus(waitingText)
if (startFirstCustomer)
    StartNextCustomer()
```

또는 더 명확하게:

```text
public void BeginBusinessDay()
public void EndBusinessDay()
```

로 이름을 정리해도 된다.

사용자 결정 필요:

- 다음날 버튼을 누르면 첫 손님을 자동 시작할지
- 다음날 버튼 후 별도 `영업 시작` 버튼을 둘지

기본 제안:

- 1차 구현에서는 다음날 버튼을 누르면 첫 손님 자동 시작.

## 6. 준비 단계 정책

### 6.1 하루 파견/모험 허용 정책

설정 필드:

```text
allowDispatchAndAdventureSameDay = true
maxDispatchStartsPerDay = 1
maxAdventureStartsPerDay = 1
```

기본 제안:

- 1차는 하루에 파견 1회, 모험 1회 모두 가능.
- 파견은 장기 투자, 모험은 즉시 수급이라 역할이 겹치지 않는다.

사용자 결정 필요:

- 둘 다 가능하면 하루 준비 단계가 길어질 수 있다.
- 둘 중 하나만 가능하면 선택 압박은 강하지만 콘텐츠 노출이 줄어든다.

### 6.2 복귀 결과 수령 정책

설정 필드:

```text
requireClaimReadyDispatchBeforeNextDay
```

선택지:

| 정책 | 설명 |
| --- | --- |
| 강제 수령 | 다음날로 넘기기 전에 복귀 결과를 반드시 확인 |
| 보류 가능 | 결과를 나중에 받아도 됨 |

기본 제안:

- 1차는 보류 가능.
- 이유: 흐름을 막지 않고, 결과 알림 UI만 있으면 충분하다.

### 6.3 준비 단계 닫기 정책

준비 단계는 다음날 시작 전까지 유지된다. 단, 파견이나 모험 화면을 열 때는 준비 단계 루트는 뒤에 남기거나 숨길 수 있다.

기본 제안:

- 준비 단계 루트는 유지하고, 파견/모험 화면을 상위 패널로 띄운다.

## 7. UI 요구사항

### 7.1 준비 단계 화면 필수 표시

- 현재 영업일
- 오늘 가능한 행동
- 진행 중인 파견 수
- 복귀 가능한 파견 수
- 오늘 모험 가능 여부
- 파견 버튼
- 모험 버튼
- 다음날 시작 버튼

### 7.2 버튼 상태

| 버튼 | 활성 조건 | 비활성 사유 예시 |
| --- | --- | --- |
| 파견 | 준비 단계, 횟수 남음 | 오늘 파견 가능 횟수를 모두 사용했습니다 |
| 모험 | 준비 단계, 횟수 남음 | 오늘 모험을 이미 진행했습니다 |
| 다음날 | 진행 중 모험 없음 | 모험을 종료하거나 귀환해야 합니다 |

### 7.3 1차 UI 형태

1차는 목록형 패널을 권장한다.

```text
상단: 오늘의 준비
중앙: 파견 / 모험 / 복귀 알림 요약
하단: 파견, 모험, 다음날 버튼
```

최종 아트가 없어도 프리팹 기반으로 만든다. 최종 UI 클래스에서 런타임으로 화면 전체를 생성하지 않는다.

## 8. 파견/모험 서비스와의 연결점

준비 단계 컨트롤러는 구체 구현에 직접 깊게 의존하지 않도록 아래 인터페이스를 두는 것이 좋다.

```text
IDispatchPreparationGateway
- int ActiveTaskCount { get; }
- int ReadyToClaimCount { get; }
- void RefreshTasksForDay(int currentDay)
- bool HasBlockingReadyToClaimTask { get; }

IAdventurePreparationGateway
- bool HasActiveSession { get; }
- bool CanStartAdventure(int currentDay)
```

`PreparationPhaseController`는 이 인터페이스만 보고 버튼 상태를 갱신한다.

## 9. 완료 기준

- `BusinessClosed` 후 준비 단계가 열린다.
- 손님 대화 중에는 준비 단계가 열리지 않는다.
- 준비 단계에서 파견 버튼 클릭 이벤트를 발생시킬 수 있다.
- 준비 단계에서 모험 버튼 클릭 이벤트를 발생시킬 수 있다.
- 다음날 버튼을 누르면 `CurrentDay`가 1 증가한다.
- 다음날 후 첫 손님 시작 흐름이 복구된다.
- 준비 단계 UI가 각 버튼의 비활성 사유를 표시한다.

## 10. 테스트 기준

도메인 테스트 또는 플레이 모드 테스트:

- 영업 종료 이벤트 수신 시 상태가 `PreparationOpen`이 되는가
- 중복으로 영업 종료 이벤트를 받아도 준비 단계가 두 번 열리지 않는가
- 파견 횟수를 모두 사용하면 파견 버튼이 비활성화되는가
- 모험 세션 진행 중 다음날 버튼이 막히는가
- 다음날 진행 시 날짜가 1 증가하는가
- 다음날 진행 후 오늘 사용 횟수가 초기화되는가

## 11. 사용자 결정 필요 체크

구현 전 확인:

- 하루에 파견과 모험을 둘 다 가능하게 할 것인가?
- 하루 파견 가능 횟수는 몇 회인가?
- 하루 모험 가능 횟수는 몇 회인가?
- 다음날 버튼을 누르면 첫 손님을 자동 시작할 것인가?
- 복귀 가능 파견 결과를 수령하지 않아도 다음날로 넘어갈 수 있는가?

기본 제안:

```text
하루 파견 1회
하루 모험 1회
파견과 모험 둘 다 가능
다음날 버튼 후 첫 손님 자동 시작
복귀 결과는 보류 가능
```
