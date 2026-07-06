# 파견 & 모험 재료 획득 시스템 구현 규칙

## 1. 문서 목적

이 문서는 `Material_Acquisition_Dispatch_Adventure_Planning.md`의 기획을 구현할 때 따를 코딩 컨벤션, 구조 규칙, 구현 판단 기준을 정의한다.

이번 구현은 기존 파견 프로토타입이나 이전 구현 규칙에 종속되지 않는다. 단, 최종적으로 요리, 인벤토리, NPC, 영업일 시스템과 연결될 때는 각 시스템의 공개 API 또는 별도 어댑터를 통해 연결한다.

## 2. 최상위 구현 원칙

### 2.1 영업 중 즉시 보충을 만들지 않는다

파견과 모험은 손님 응대 중 부족한 재료를 즉시 해결하는 시스템이 아니다. 모든 진입 조건은 `영업 종료 후 준비 단계`를 기준으로 판단한다.

금지:

- 손님 주문을 받은 직후 파견 또는 모험을 자동으로 여는 흐름
- 현재 손님의 취향이나 주문을 직접 참조해서 보상을 조정하는 로직
- 조리 중 재료 부족을 이유로 파견 또는 모험을 호출하는 로직

허용:

- 영업 종료 이벤트 이후 파견 또는 모험 버튼 활성화
- 다음 영업을 대비하는 재료 수급
- 도감, 지식, 지역 정보 갱신

### 2.2 파견과 모험의 역할을 코드에서도 분리한다

파견은 `며칠 뒤를 보는 운영 판단`이고, 모험은 `현재 이벤트에서 위험과 보상을 고르는 선택 판단`이다.

파견 구현은 다음을 중심으로 한다.

- NPC 배정
- 지역 선택
- 영업일 단위 소요 시간
- 복귀 가능 상태
- 예상 보상과 실제 결과의 차이

모험 구현은 다음을 중심으로 한다.

- 모험 세션
- 진행도
- 현재 이벤트
- 선택지 결과
- 임시 보상
- 귀환 및 정산

두 시스템이 보상, 조건, 확률 계산을 공유할 수는 있지만 상태 머신과 UI 흐름은 섞지 않는다.

### 2.3 데이터 주도 구현을 우선한다

지역, 이벤트, 선택지, 보상, 위험도, 조건은 코드에 직접 박지 않고 데이터로 작성한다.

코드에 직접 작성해도 되는 것:

- 상태 전이 규칙
- 확률 계산 방식
- 보상 테이블 해석 방식
- 인벤토리 지급 절차
- 유효성 검사

데이터로 분리해야 하는 것:

- 지역 이름과 설명
- 지역별 보상 테이블
- 이벤트 이름과 설명
- 선택지 텍스트
- 선택지별 보상, 위험 변화, 후속 이벤트
- 해금 조건
- NPC 보너스 종류와 수치

## 3. 권장 폴더 구조

새 구현은 기존 임시 파견 구조에 덧대기보다 독립된 기능 루트에서 시작한다.

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

## 4. 계층 규칙

### 4.1 Domain 계층

순수 C# 클래스로 작성한다. 가능하면 `MonoBehaviour`, `ScriptableObject`, `GameObject`, `Transform`, `TextMeshProUGUI`, `Button`에 의존하지 않는다.

담당:

- 파견 시작 가능 여부
- 파견 상태 전이
- 모험 세션 진행
- 선택지 적용
- 위험도 변화
- 보상 결과 계산

### 4.2 Data 계층

`ScriptableObject`로 작성한다. 데이터 클래스는 게임 상태를 직접 변경하지 않는다.

담당:

- 지역 정의
- 이벤트 정의
- 선택지 정의
- 보상 테이블 정의
- 조건 정의
- NPC 보너스 정의

### 4.3 Runtime 계층

Unity 씬과 도메인 로직을 연결한다.

담당:

- 현재 영업일 조회
- 인벤토리 지급 요청
- 도감 또는 지식 갱신 요청
- UI에 표시할 상태 전달
- 저장/불러오기 연결

### 4.4 UI 계층

UI는 상태를 표시하고 명령을 전달한다. 보상 굴림, 상태 전이, 인벤토리 지급을 UI 클래스 안에서 처리하지 않는다.

금지:

- 버튼 클릭 메서드 안에서 직접 보상 랜덤 계산
- UI 텍스트를 기준으로 게임 상태 판단
- UI 클래스가 저장 데이터를 직접 수정

## 5. 네이밍 컨벤션

### 5.1 클래스명

데이터:

- `DispatchRegionSO`
- `DispatchRewardTableSO`
- `AdventureRegionSO`
- `AdventureEventSO`
- `AdventureChoiceSO`

런타임 상태:

- `DispatchTask`
- `DispatchTaskResult`
- `AdventureSession`
- `AdventureEventResult`
- `AcquisitionRewardResult`

서비스:

- `DispatchService`
- `AdventureService`
- `AcquisitionRewardResolver`
- `AcquisitionInventoryGateway`

UI:

- `DispatchRegionListView`
- `DispatchTaskView`
- `DispatchResultView`
- `AdventureEventView`
- `AdventureSettlementView`

### 5.2 메서드명

`Try` 접두사:

- 실패 가능성이 있고 호출자가 결과를 분기해야 하는 메서드
- 예: `TryStartDispatch`, `TryClaimDispatchResult`, `TrySelectAdventureChoice`

`Build` 접두사:

- 입력값으로 표시 텍스트나 결과 객체를 만들며 외부 상태를 바꾸지 않는 메서드
- 예: `BuildRewardPreview`, `BuildSettlementSummary`

`Apply` 접두사:

- 세션이나 상태에 변화를 적용하는 메서드
- 예: `ApplyChoiceResult`, `ApplyDangerDelta`

`Grant` 접두사:

- 인벤토리, 도감, 지식 등 외부 시스템에 실제 보상을 지급하는 메서드
- 예: `GrantSettlementRewards`

### 5.3 ID 규칙

ID는 저장, 조건, 참조에 사용되므로 표시명과 분리한다.

규칙:

- 영문 `snake_case` 사용
- 대소문자 구분에 의존하지 않기
- 표시용 한국어 이름은 `displayName`에만 작성
- 이미 배포된 ID는 이름이 마음에 들지 않아도 변경하지 않기

예:

```text
near_forest
wet_cavern
slime_pool_glow
choice_collect_carefully
```

## 6. 공통 보상 규칙

### 6.1 보상은 공통 모델을 사용한다

파견과 모험은 서로 다른 콘텐츠지만 최종적으로 재료를 지급한다. 따라서 보상 계산 결과는 공통 모델로 통일한다.

권장 모델:

```text
AcquisitionRewardEntry
- Item
- MinAmount
- MaxAmount
- Weight
- Chance
- Rarity
- Tags

AcquisitionRewardResultEntry
- Item
- RequestedAmount
- GrantedAmount
- RemainingAmount
- SourceType
- SourceId
- IsNewDiscovery
- IsRare
```

### 6.2 보상 지급은 정산 단계에서만 한다

파견:

- 시작 시 인벤토리에 지급하지 않는다.
- 복귀 결과를 확인하거나 수령할 때 지급한다.

모험:

- 모험 중 보상은 임시 보상 목록에 쌓는다.
- 귀환 또는 종료 정산 시 인벤토리에 지급한다.

예외가 필요하면 데이터에 `ImmediateGrant` 같은 명시 옵션을 둔다. 기본값은 정산 지급이다.

### 6.3 인벤토리 부족을 결과로 남긴다

인벤토리에 전부 들어가지 못한 보상은 조용히 사라지게 하지 않는다.

필수 처리:

- 요청 수량
- 실제 지급 수량
- 미지급 수량
- 현재 보유 수량
- UI 표시

## 7. 랜덤과 재현성 규칙

### 7.1 도메인 로직에서 `UnityEngine.Random`을 직접 사용하지 않는다

확률 계산은 주입된 RNG 또는 래퍼를 사용한다.

권장:

```text
IAcquisitionRandom
- RangeInt
- RangeFloat01
- PickWeighted
```

### 7.2 결과 재현용 시드를 저장한다

파견 작업과 모험 세션에는 결과 재현을 위한 시드를 저장한다.

파견:

- `resultSeed`는 파견 시작 시 생성한다.
- 복귀 결과 계산은 `resultSeed`를 기준으로 한다.

모험:

- `sessionSeed`는 모험 시작 시 생성한다.
- 이벤트 선택, 확률 결과, 후속 이벤트 선택은 세션 RNG를 통해 처리한다.

### 7.3 확률 공개와 내부 확률을 분리한다

UI에 보여주는 표현과 실제 수치는 분리한다.

예:

```text
낮음 / 보통 / 높음 / 매우 높음
```

실제 확률:

```text
0.15 / 0.35 / 0.60 / 0.85
```

## 8. 파견 구현 규칙

### 8.1 파견 시간은 영업일 단위다

파견은 실시간 초 단위 진행이 아니라 영업일 기준으로 진행한다.

필수 필드:

```text
DispatchTask
- taskId
- regionId
- assignedNpcId
- startDay
- returnDay
- status
- resultSeed
```

`currentDay >= returnDay`이면 복귀 가능 상태가 된다.

### 8.2 상태는 명시적으로 관리한다

권장 상태:

```text
DispatchTaskStatus
- Waiting
- InProgress
- ReadyToReturn
- Completed
- Cancelled
```

상태 전이는 서비스에서만 수행한다.

금지:

- UI에서 상태 enum 직접 변경
- 저장 데이터 로드 직후 상태 보정 없이 사용
- 날짜 변화와 무관하게 임의로 완료 처리

### 8.3 NPC 배정 규칙

NPC는 동시에 하나의 파견에만 배정할 수 있다.

파견 중 NPC 처리 정책은 기획 미정 사항이므로 코드에 고정하지 않는다. 설정값 또는 정책 객체로 둔다.

정책 예:

```text
DispatchNpcAvailabilityPolicy
- BlocksNpcVisit
- AllowsNpcVisitWithPenalty
- IgnoresNpcVisit
```

### 8.4 보상 구조를 분리한다

파견 결과는 다음 항목을 별도로 계산하고 결과에 남긴다.

- 기본 보상
- 추가 보상
- 희귀 보상
- 실패 또는 부분 성공
- 부가 발견

실패하더라도 결과 객체는 반드시 생성한다. 실패는 `보상이 없음`이 아니라 `실패 결과`다.

### 8.5 예상 보상과 실제 보상을 구분한다

파견 선택 화면은 확정 결과가 아니라 예상 정보를 보여준다.

표시 가능:

- 주요 재료군
- 예상 소요 일수
- 위험도
- 희귀 보상 가능성
- NPC 보너스 요약

표시 금지:

- 숨겨진 결과의 정확한 조건
- 결과 시드로 미리 계산한 실제 보상

## 9. 모험 구현 규칙

### 9.1 모험은 세션 단위로 관리한다

필수 상태:

```text
AdventureSession
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

권장 상태:

```text
AdventureSessionStatus
- NotStarted
- InProgress
- AwaitingChoice
- ReadyToContinue
- Returning
- Settled
- Failed
```

### 9.2 선택지는 결과 타입을 명시한다

선택지 결과는 성공/실패 하나로 축소하지 않는다.

지원해야 할 결과 타입:

- 확정 결과
- 확률 결과
- 조건부 결과
- 연쇄 결과
- 누적 결과
- 숨겨진 결과

### 9.3 모험 중 획득물은 임시 보상이다

모험 중 얻은 재료는 기본적으로 `temporaryRewards`에 들어간다. 인벤토리 반영은 정산에서 한다.

정산 조건:

- 귀환 선택
- 지역 종료
- 실패 후 회수 가능한 정산

정산 결과에는 잃은 보상, 획득한 보상, 새 발견을 모두 표현할 수 있어야 한다.

### 9.4 귀환은 항상 선택 가능한 기본 행동이다

모험이 진행 중이고 강제 이벤트 상태가 아니라면 귀환을 선택할 수 있어야 한다.

계속 진행은 더 큰 보상과 더 큰 위험을 의미해야 한다. 코드상에서도 진행할수록 위험도, 피로도, 이벤트 난이도, 보상 테이블 중 하나 이상이 변화해야 한다.

### 9.5 전투는 모험 이벤트의 한 유형이다

전투 시스템을 별도로 크게 만들기 전까지는 전투를 `AdventureEventType.Combat`으로 다룬다.

전투 선택지는 승패뿐 아니라 재료 상태를 바꾼다.

예:

- 베기: 살점류 보상 증가, 핵 손상 가능성 증가
- 찌르기: 핵 보상 증가, 반격 위험 증가
- 둔기: 뼈/껍질 보상 증가, 점액류 보상 감소
- 포획: 희귀 생물 재료 가능, 실패 피해 증가
- 도망: 보상 없음, 위험 일부 감소

## 10. 조건과 해금 규칙

조건은 문자열 비교가 아니라 명시 타입으로 다룬다.

권장 조건:

```text
AcquisitionCondition
- RequiredDay
- RequiredRegionUnlocked
- RequiredKnowledgeId
- RequiredToolId
- RequiredNpcTraitId
- RequiredFlag
- BlockedFlag
```

조건 판정은 데이터 클래스가 직접 하지 않는다. 조건 판정 서비스가 현재 진행 상태를 받아 평가한다.

## 11. 저장 규칙

저장 데이터에는 Unity 오브젝트 참조를 직접 저장하지 않는다. ID와 값만 저장한다.

파견 저장:

```text
- taskId
- regionId
- assignedNpcId
- startDay
- returnDay
- status
- resultSeed
- claimedRewardIds
```

모험 저장:

```text
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

데이터 버전 필드를 둔다.

```text
saveVersion
```

## 12. 검증 규칙

에디터 검증 도구 또는 `OnValidate`에서 다음을 확인한다.

공통:

- ID 비어 있음
- ID 중복
- 참조 누락
- 음수 확률
- 음수 가중치
- 최소 수량이 최대 수량보다 큼

파견:

- 소요 일수 1 이상
- 보상 테이블 누락
- 위험도 범위 초과
- NPC 보너스 대상 누락

모험:

- 이벤트 선택지 없음
- 선택지 결과 없음
- 후속 이벤트 ID가 존재하지 않음
- 귀환 불가능한 무한 루프
- 조건부 선택지의 조건 데이터 누락

## 13. UI 구현 규칙

### 13.1 UI는 프리팹 우선으로 만든다

최종 UI는 런타임에 `GameObject`, `Button`, `TextMeshProUGUI`, `LayoutGroup`을 조립해서 생성하지 않는다. 화면 구조는 최대한 프리팹으로 만들고, 런타임 코드는 직렬화된 참조에 데이터를 바인딩한다.

권장:

- 화면 단위 프리팹
- 반복 항목 단위 프리팹
- 버튼, 카드, 보상 행, 선택지 행을 별도 프리팹으로 분리
- `SerializeField` 참조를 통해 텍스트, 이미지, 버튼, 루트 오브젝트 연결
- 데이터 변경 시 `Refresh`, `Bind`, `SetData` 계열 메서드로 표시 갱신

런타임 생성이 허용되는 경우:

- 보상 목록, 선택지 목록처럼 개수가 변하는 반복 항목을 항목 프리팹으로 인스턴스화
- 디버그 전용 임시 UI
- 에디터 테스트용 프로토타입 UI

금지:

- 최종 UI 클래스에서 화면 전체 레이아웃을 코드로 생성
- 누락된 참조를 자동으로 만들며 조용히 진행
- `autoCreateDefaultUI`, `buildDefaultLayoutWhenMissing` 같은 옵션을 최종 구현의 기본 흐름으로 사용
- UI 구조 생성 코드와 게임 규칙 코드를 같은 클래스에 섞기

프리팹 권장 예:

```text
Assets/Work/MaterialAcquisition/Prefabs/Dispatch/
- DispatchScreen.prefab
- DispatchRegionCard.prefab
- DispatchNpcSlot.prefab
- DispatchTaskRow.prefab
- DispatchRewardResultRow.prefab

Assets/Work/MaterialAcquisition/Prefabs/Adventure/
- AdventureScreen.prefab
- AdventureChoiceButton.prefab
- AdventureRewardRow.prefab
- AdventureSettlementPanel.prefab
```

### 13.2 UI는 상태를 표시하고 명령만 전달한다

UI는 다음 정보를 명확히 보여준다.

파견:

- 지역 목록
- 주요 재료군
- 소요 영업일
- 위험도
- NPC 선택
- NPC 보너스
- 진행 중인 파견
- 복귀 가능 파견
- 결과 수령

모험:

- 현재 지역
- 진행도
- 현재 이벤트
- 상황 설명
- 선택지
- 선택 결과
- 임시 보상
- 위험도 또는 피로도
- 계속 진행
- 귀환

UI 텍스트는 데이터의 `displayName`, `description`, `resultText`에서 가져온다. 디버그용 임시 텍스트를 최종 UI 로직에 남기지 않는다.

### 13.3 누락된 프리팹 참조는 빠르게 드러낸다

필수 UI 참조가 누락된 경우 런타임에서 대체 UI를 만들지 말고 경고 또는 오류를 남긴다.

규칙:

- 선택 기능에 필요한 참조가 없으면 해당 기능을 비활성화한다.
- 결과 확인에 필요한 참조가 없으면 정산 처리를 중단하고 명시 로그를 남긴다.
- 개발 편의를 위한 자동 생성은 `Debug`, `Prototype`, `Temp` 이름이 붙은 클래스에서만 허용한다.
- 최종 씬에 들어가는 UI 클래스는 프리팹 참조가 있다는 전제로 동작한다.

## 14. 테스트 기준

최소 테스트 범위:

- 파견 시작 시 `returnDay`가 올바르게 계산되는가
- 현재 영업일이 `returnDay`에 도달하면 복귀 가능 상태가 되는가
- 파견 결과가 같은 시드에서 같은 결과를 내는가
- NPC가 중복 파견되지 않는가
- 모험 선택지가 위험도와 임시 보상을 올바르게 변경하는가
- 귀환 정산 전에는 인벤토리에 보상이 들어가지 않는가
- 정산 시 인벤토리 부족 결과가 보존되는가
- 조건부 이벤트가 조건을 만족할 때만 등장하는가

## 15. 미정 사항 처리 규칙

기획서의 미정 사항은 코드에 하드코딩하지 않는다. 설정값, 정책 객체, 또는 데이터 필드로 둔다.

미정 사항 예:

- 하루에 파견과 모험을 둘 다 할 수 있는가
- 하루에 보낼 수 있는 파견 NPC 수
- 파견 중 NPC가 손님으로 등장할 수 있는가
- 모험 실패 시 획득 재료를 잃는가
- 모험에 체력, 피로도, 도구 내구도를 둘 것인가
- 희귀 재료 확률을 얼마나 공개할 것인가

규칙:

- 확정되지 않은 정책은 기본값을 두되 이름으로 의도를 드러낸다.
- 임시 결정은 주석이 아니라 설정 필드와 문서에 남긴다.
- 이후 기획 변경 시 데이터 변경만으로 조정 가능하게 만든다.

## 16. 1차 구현 완료 기준

파견 1차:

- 파견 지역 3개
- NPC 2명 이상 배정 가능
- 영업일 단위 진행
- 복귀 가능 상태
- 보상 수령
- 인벤토리 지급 결과 표시

모험 1차:

- 모험 지역 1개
- 이벤트 6개 이상
- 이벤트당 선택지 2개 이상
- 위험도 변화
- 임시 보상
- 귀환 정산
- 인벤토리 지급 결과 표시

공통:

- 보상 계산이 UI와 분리되어 있음
- 결과가 시드 기반으로 재현 가능함
- 데이터 검증 방법이 있음
- 기존 영업 중 조리 흐름에 끼어들지 않음

## 17. 핵심 체크 문장

구현 중 판단이 애매하면 아래 문장으로 확인한다.

```text
이 코드는 손님 응대 중 즉시 해결책을 주는가,
아니면 영업 종료 후 다음 날을 대비하는 선택지를 넓히는가?
```

전자라면 기획 의도에서 벗어난 것이다. 후자라면 파견과 모험의 핵심 역할에 맞다.
