# NPC Request Authoring Guide

이 문서는 NPC 대화 시스템에서 "의뢰형 이벤트"를 작성할 때 필요한 최소 구조를 정리한다.
요리 판정 자체는 별도 시스템에서 처리하고, NPC 시스템은 의뢰 제안, 결과에 따른 상태 전환, 완료 대화, 후일담 대화를 담당한다.

## 기본 흐름

| 단계 | 상태 | 작성/처리 방식 |
| --- | --- | --- |
| 1 | `Locked` | 아직 의뢰가 열리지 않은 상태. 호감도 레벨과 선행 이벤트로 해금한다. |
| 2 | `Unlocked` | 의뢰 제안 이벤트가 등장할 수 있는 상태. |
| 3 | `Offered` | NPC가 의뢰를 제안한 상태. 같은 제안 이벤트가 다시 나오지 않아야 한다. |
| 4 | `Accepted` | 플레이어가 의뢰를 받은 상태. 외부 UI나 디버그 버튼으로 전환한다. |
| 5 | `InProgress` | 요리/퀘스트 쪽에서 진행 중인 상태. |
| 6 | `ReadyToComplete` | 성공 결과가 기록되어 완료 대화가 나올 수 있는 상태. |
| 7 | `Completed` | 완료 대화가 끝난 상태. |
| 8 | `EpilogueAvailable` | 후일담이 열릴 수 있는 상태. 필요할 때 외부 조건으로 전환한다. |
| 9 | `EpilogueCompleted` | 후일담까지 끝난 상태. |

## NPCs.csv

의뢰를 가진 NPC는 `RequestAvailable`을 `true`로 둔다.
`RequestUnlockLevel`은 호감도 레벨 조건이고, `RequestUnlockEvent`는 반드시 먼저 본 이벤트 조건이다.

```csv
NpcId,DisplayName,Race,Role,PreferredTags,PreferredFoodTypes,AvoidTags,Notes,RequestAvailable,RequestUnlockLevel,RequestUnlockEvent
Dorin,도린,드워프,대장장이,Smoky|Hearty,Stew|Roast,Cold,"불맛과 술향을 좋아하는 장인",true,2,Dorin_First_DragonCoalStew
```

## VisitEvents.csv

의뢰형 이벤트는 보통 세 줄로 나눈다.

### 1. 의뢰 제안 이벤트

```csv
EventId,NpcId,RegionId,StartGroups,QuestionLimit,AvailableQuestionCategories,EventType,Priority,RepeatMode,CooldownDays,RequiredNpcVisits,RequiredAffinity,RequiredCorrectCount,RequiredLastResult,RequiredEventIds,SequenceGroup,SequenceIndex,CorrectRecipeId,AllowedFoodTypes,RequiredTags,PreferredTags,AvoidTags,DisgustingTags,RequiredRequestState,BlockedAtRequestState,RequestStateAfterEncounter,RequestSuccessResults,RequestStateAfterSuccessResult
Dorin_Request_ForgeAleRoast,Dorin,MossCave,Dorin_Request_ForgeAleRoast_Start,3,Taste|TextureTemp|Condition|Avoid,Request,80,Once,0,0,0,0,,,,0,,Roast,Hot|Smoky,Hearty,Cold,,Unlocked,Offered,Offered,Perfect|Correct,ReadyToComplete
```

핵심 규칙:

- `EventType=Request`
- `RepeatMode=Once`
- `RequiredRequestState=Unlocked`
- `BlockedAtRequestState=Offered`
- `RequestStateAfterEncounter=Offered`
- `RequestStateAfterSuccessResult=ReadyToComplete`

`RequestSuccessResults`가 비어 있으면 코드의 기본 성공 판정을 사용한다. 현재 명시 작성은 `Perfect|Correct`를 권장한다.

### 2. 완료 이벤트

```csv
Dorin_Request_ForgeAleRoast_Complete,Dorin,MossCave,Dorin_Request_ForgeAleRoast_Complete_Start,0,,Special,90,Once,0,0,0,0,,,,0,,,,,,,,ReadyToComplete,Completed,Completed,,
```

핵심 규칙:

- `RequiredRequestState=ReadyToComplete`
- `BlockedAtRequestState=Completed`
- `RequestStateAfterEncounter=Completed`

### 3. 후일담 이벤트

```csv
Dorin_Request_ForgeAleRoast_Epilogue,Dorin,MossCave,Dorin_Request_ForgeAleRoast_Epilogue_Start,0,,Special,70,Once,0,0,0,0,,,,0,,,,,,,,EpilogueAvailable,EpilogueCompleted,EpilogueCompleted,,
```

후일담이 없는 의뢰도 가능하지만, Validator는 정보 메시지로 알려준다.

## DialogueLines.csv

대사는 이벤트별 `StartGroups`와 같은 `GroupId`로 작성한다.
말풍선이 너무 길어지지 않게 한 줄은 한 인물의 한 호흡 정도로 나눈다.

```csv
EventId,GroupId,LineIndex,Speaker,Text
Dorin_Request_ForgeAleRoast,Dorin_Request_ForgeAleRoast_Start,0,Dorin,주인장. 오늘은 잠깐 부탁할 일이 있네.
Dorin_Request_ForgeAleRoast,Dorin_Request_ForgeAleRoast_Start,1,Player,도린 씨 부탁이면 먼저 들어봐야죠. 어떤 일인가요?
Dorin_Request_ForgeAleRoast,Dorin_Request_ForgeAleRoast_Start,2,Dorin,화덕 앞에서 먹을 만한 뜨겁고 묵직한 고기가 필요하네.
Dorin_Request_ForgeAleRoast,Dorin_Request_ForgeAleRoast_Start,3,Player,좋아요. 뜨겁고 묵직한 쪽으로 준비해볼게요.
```

## Debug Popup 사용법

- `Request State`: 현재 NPC의 의뢰 상태와 각 상태에 도달한 날짜를 확인한다.
- `Request Flow`: 지금 해야 할 다음 행동과 현재 지역에서 등장 가능한 의뢰 관련 이벤트 상태를 확인한다.
- `Event Preview`: 전체 이벤트 후보가 왜 막혔는지 확인한다.
- `Force`: 특정 이벤트 한 줄을 강제로 실행한다. 실제 랜덤 선택 검증보다는 대사와 상태 전환 확인용이다.
- `Accept`, `Progress`, `Ready`, `Complete`, `Epilogue`, `Done`: 아직 외부 의뢰 UI가 붙기 전 상태를 수동으로 넘기는 디버그 버튼이다.

## 작성 체크리스트

- 의뢰 NPC의 `RequestAvailable`이 `true`인가?
- 선행 이벤트가 있다면 `RequestUnlockEvent`가 실제 `VisitEvents.csv`에 존재하는가?
- 제안 이벤트가 `Unlocked -> Offered`로 한 번만 진행되는가?
- 성공 결과가 `ReadyToComplete`로 이어지는가?
- 완료 이벤트가 `ReadyToComplete -> Completed`로 이어지는가?
- 후일담이 필요하다면 `EpilogueAvailable -> EpilogueCompleted` 이벤트가 있는가?
- `DialogueLines.csv`의 `EventId`와 `GroupId`가 `VisitEvents.csv`의 `StartGroups`와 정확히 일치하는가?
