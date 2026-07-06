# 05. 데이터 작성과 밸런싱 명세

## 1. 목표

파견과 모험은 데이터 주도형으로 구현한다. 지역, 이벤트, 선택지, 보상, 위험도, NPC 보너스는 코드에 박지 않고 `ScriptableObject`와 검증 가능한 데이터로 작성한다.

이 문서의 목표:

- 데이터 ID 규칙을 통일한다.
- 1차 구현에 필요한 샘플 데이터를 정의한다.
- 사용자 작성/확정이 필요한 데이터를 분리한다.
- 밸런싱을 조정할 표를 제공한다.
- 에디터 검증 기준을 정한다.

## 2. ID 규칙

모든 저장/참조용 ID는 아래 규칙을 따른다.

```text
영문 snake_case
소문자 권장
표시명과 분리
배포 후 변경 금지
```

예:

```text
near_forest
wet_cavern
salt_cavern
glowing_slime_pool
choice_collect_carefully
reward_wet_cavern_common
```

금지:

```text
근처숲
NearForest
near forest
temp1
new_event
```

## 3. 데이터 폴더 구조

```text
Assets/Work/MaterialAcquisition/Data/
  Common/
    RewardTables/
  Dispatch/
    Regions/
    Agents/
  Adventure/
    Regions/
    Events/
```

권장 파일명:

```text
RewardTable_WetCavern_Common.asset
DispatchRegion_NearForest.asset
DispatchAgent_MossRunner.asset
AdventureRegion_WetCavern.asset
AdventureEvent_GlowingSlimePool.asset
```

## 4. 공통 보상 데이터

### 4.1 보상 테이블 1차 목록

1차에 필요한 최소 보상 테이블:

| ID | 용도 |
| --- | --- |
| `reward_near_forest_common` | 근처 숲 기본 보상 |
| `reward_near_forest_rare` | 근처 숲 희귀 보상 |
| `reward_wet_cavern_common` | 축축한 동굴 기본 보상 |
| `reward_wet_cavern_rare` | 축축한 동굴 희귀 보상 |
| `reward_salt_cavern_common` | 소금 동굴 기본 보상 |
| `reward_salt_cavern_rare` | 소금 동굴 희귀 보상 |
| `reward_adventure_slime_pool_safe` | 점액 웅덩이 안전 선택 보상 |
| `reward_adventure_slime_pool_risky` | 점액 웅덩이 위험 선택 보상 |
| `reward_adventure_mushroom_safe` | 버섯 군락 안전 선택 보상 |
| `reward_adventure_combat_slash` | 전투 베기 보상 |
| `reward_adventure_combat_pierce` | 전투 찌르기 보상 |

### 4.2 기존 재료 아이템 후보

현재 프로젝트에서 확인된 임시 재료 아이템 후보:

```text
TempSlimeNucleusIngredientItem
TempSlimeMucusIngredientItem
TempRockSaltIngredientItem
TempMushroomCapIngredientItem
TempFlatMushroomIngredientItem
TestCaveBeefIngredientItem
```

주의:

- 실제 표시명과 세계관 명칭은 에셋 내용을 확인하고 사용자와 맞춰야 한다.
- 1차에서는 기존 임시 아이템을 사용해 기능을 검증하고, 이후 정식 데이터로 교체한다.

### 4.3 보상 수량 기본값

| 보상 등급 | 기본 수량 | 설명 |
| --- | ---: | --- |
| 일반 | 1~3 | 안정적 재료 |
| 고급 | 1~2 | 조금 좋은 재료 |
| 희귀 | 1 | 낮은 확률 재료 |
| 특수 | 1 | 이벤트/조건부 재료 |
| 위험 | 1 | 실패나 페널티 가능 재료 |

## 5. 파견 지역 데이터

### 5.1 1차 지역 제안

| regionId | 표시명 | 소요 일수 | 위험도 | 주요 재료군 | 비고 |
| --- | --- | ---: | --- | --- | --- |
| `near_forest` | 근처 숲 | 1 | Low | 버섯, 허브, 열매 | 가장 안정적인 기본 지역 |
| `wet_cavern` | 축축한 동굴 | 2 | Normal | 점액, 광물염, 희귀 버섯 | 모험 지역과도 연결 |
| `salt_cavern` | 소금 동굴 | 2 | Normal | 암염, 광물염, 동굴 버섯 | 보존/간 계열 재료 |

사용자 결정 필요:

- 지역명을 세계관에 맞춰 바꿀지
- `salt_cavern` 대신 문서 초안의 `폐허 지하` 또는 `얼어붙은 길`을 먼저 넣을지

### 5.2 파견 지역별 보상 초안

#### `near_forest`

기본 보상:

- 버섯류 1~3
- 허브류 1~2
- 열매류 1~2

희귀 보상:

- 희귀 버섯 1
- 향이 강한 허브 1

#### `wet_cavern`

기본 보상:

- 점액류 2~3
- 광물염 1~2
- 동굴 버섯 1~2

희귀 보상:

- 점액 핵 1
- 발광 버섯 1

#### `salt_cavern`

기본 보상:

- 암염 2~3
- 광물염 1~2
- 납작 버섯 1

희귀 보상:

- 결정 소금 1
- 오래된 보존석 1

사용자 결정 필요:

- 실제 존재하는 `IngredientItemDataSO`와 매핑 필요
- 없는 재료는 새 에셋을 만들지, 기존 임시 재료로 대체할지 결정 필요

## 6. 파견 NPC 데이터

### 6.1 1차 NPC 방식 선택

선택지:

| 방식 | 설명 | 장점 | 단점 |
| --- | --- | --- | --- |
| 기존 NPC 연결 | `NpcData.npcId`와 연결 | 세계관 일관성 좋음 | 손님 등장 제외 처리가 필요 |
| 별도 파견 에이전트 | 파견 전용 NPC | 구현 쉬움 | 손님 시스템과 연결감 약함 |

기본 제안:

- 1차는 기존 NPC 2명을 연결하되, 손님 제외가 어렵다면 별도 파견 에이전트로 시작한다.

### 6.2 임시 에이전트 제안

| agentId | 표시명 | 특성 | 보너스 |
| --- | --- | --- | --- |
| `moss_runner` | 이끼길잡이 | 숲길 익숙함 | 보상 수량 +10% |
| `cave_scavenger` | 동굴수색꾼 | 동굴 감각 | 희귀 확률 +5% |

사용자 결정 필요:

- 실제 NPC 이름과 연결할지
- 파견 NPC의 말투/소개문을 만들지

## 7. 모험 지역 데이터

### 7.1 1차 지역 제안

```text
regionId: wet_cavern
displayName: 축축한 동굴
baseDanger: 0
maxDanger: 10
maxProgress: 5
rewardThemeText: 점액, 광물염, 동굴 버섯
```

기본 방향:

- 점액류와 버섯류를 중심으로 한다.
- 파견 지역 `wet_cavern`과 같은 장소를 모험으로도 경험하게 한다.
- 파견은 안정 수급, 모험은 위험 선택으로 희귀 재료를 노리는 차이를 보여준다.

사용자 결정 필요:

- 첫 모험 지역을 `축축한 동굴`로 확정할지
- 파견 지역과 모험 지역이 같은 이름을 써도 되는지

## 8. 모험 이벤트 데이터

### 8.1 1차 이벤트 6개

| eventId | 표시명 | 타입 | 위험도 성격 |
| --- | --- | --- | --- |
| `glowing_slime_pool` | 빛나는 점액 웅덩이 | Gathering | 위험 선택 시 보상 증가 |
| `suspicious_mushroom_patch` | 수상한 버섯 군락 | Gathering | 깊이 채집하면 독성 위험 |
| `monster_tracks` | 마물의 흔적 | Discovery | 전투 이벤트로 연결 가능 |
| `cave_slime_larva` | 동굴 점액충 | Combat | 공격 방식별 보상 차이 |
| `salt_crystal_wall` | 소금 결정 벽 | Gathering | 무리하면 위험 증가 |
| `unstable_ceiling` | 불안한 천장 | Hazard | 보상보다 위험 회피 판단 |

### 8.2 이벤트별 선택지 초안

#### `glowing_slime_pool`

| choiceId | 선택지 | 결과 |
| --- | --- | --- |
| `bottle_carefully` | 조심히 병에 담는다 | 점액류 획득, 위험도 소폭 증가 |
| `concentrate_slime` | 농축해본다 | 고급 점액 가능, 실패 시 위험 증가 |
| `leave_pool` | 건드리지 않는다 | 보상 없음, 안전 |

#### `suspicious_mushroom_patch`

| choiceId | 선택지 | 결과 |
| --- | --- | --- |
| `pick_carefully` | 조심히 채집한다 | 일반 버섯 획득 |
| `dig_deeper` | 깊숙이 파본다 | 희귀 버섯 가능, 위험 증가 |
| `pass_by` | 지나간다 | 보상 없음 |

#### `monster_tracks`

| choiceId | 선택지 | 결과 |
| --- | --- | --- |
| `track_monster` | 추적한다 | 전투 이벤트 연결 가능 |
| `avoid_tracks` | 우회한다 | 위험 감소 또는 유지 |
| `set_trap` | 덫을 놓는다 | 성공 시 부산물, 실패 시 위험 증가 |

#### `cave_slime_larva`

| choiceId | 선택지 | 결과 |
| --- | --- | --- |
| `slash` | 베기 | 점액 살점 계열 보상 |
| `pierce` | 찌르기 | 점액 핵 가능, 위험 증가 |
| `run_away` | 도망친다 | 보상 없음, 위험 회피 |

#### `salt_crystal_wall`

| choiceId | 선택지 | 결과 |
| --- | --- | --- |
| `scrape_salt` | 조심히 긁어낸다 | 암염 획득 |
| `break_chunk` | 크게 떼어낸다 | 많은 암염 가능, 위험 증가 |
| `leave_wall` | 그냥 둔다 | 보상 없음 |

#### `unstable_ceiling`

| choiceId | 선택지 | 결과 |
| --- | --- | --- |
| `move_slowly` | 천천히 통과한다 | 위험도 소폭 증가 |
| `rush_through` | 빠르게 지나간다 | 진행도 증가, 실패 시 위험 크게 증가 |
| `turn_back` | 돌아간다 | 진행도 없음, 위험 감소 |

사용자 결정 필요:

- 선택지 문구가 게임 톤에 맞는지
- 결과 텍스트를 더 서사적으로 쓸지, 짧은 로그형으로 쓸지

## 9. 위험도 밸런스

### 9.1 기본 수치

```text
baseDanger = 0
maxDanger = 10
safe choice dangerDelta = 0~1
risky choice dangerDelta = 2~3
combat risky dangerDelta = 2~4
avoid choice dangerDelta = -1~0
```

### 9.2 실패 조건

기본:

```text
danger >= maxDanger
```

실패 시:

```text
임시 보상 50% 손실
남은 보상 정산
준비 단계로 복귀
```

사용자 결정 필요:

- 실패 시 모든 보상을 잃을지
- 희귀 보상부터 잃을지
- 실패 후 체력/피로도 같은 장기 페널티가 있는지

## 10. 하루 평균 보상 목표

1차 밸런스 목표:

| 콘텐츠 | 평균 획득량 | 성격 |
| --- | ---: | --- |
| 파견 1일 지역 | 2~4개 | 안정적 |
| 파견 2일 지역 | 4~6개 | 조금 큰 보상, 지연됨 |
| 모험 안전 귀환 | 3~5개 | 즉시 보충 |
| 모험 위험 선택 | 5~8개 | 위험과 희귀 보상 |

해석:

- 파견은 며칠 뒤 보상이라 총량이 조금 높아도 된다.
- 모험은 즉시 보상이지만 실패 위험이 있어야 한다.
- 하루에 둘 다 가능하면 전체 보상량이 높아지므로 인벤토리 슬롯 압박도 같이 검토한다.

## 11. 에디터 검증

### 11.1 공통 검증

- ID 비어 있음
- ID 중복
- 표시명 비어 있음
- 참조 누락
- 보상 테이블 비어 있음
- 음수 수량
- 확률 범위 오류
- 가중치 오류

### 11.2 파견 검증

- `requiredDays < 1`
- `commonRewardTable == null`
- 지역 ID 중복
- 에이전트 ID 중복
- 에이전트 `npcId`가 비어 있는데 기존 NPC 연결 정책이 켜져 있음
- 보너스 타입과 값 불일치

### 11.3 모험 검증

- 이벤트 선택지 없음
- 선택지 결과 텍스트 없음
- `nextEventId`가 존재하지 않음
- 반복 불가 이벤트만 있어서 이벤트 풀이 고갈될 수 있음
- `maxDanger <= baseDanger`
- `maxProgress < 1`
- 귀환 선택지가 없고 강제 진행만 있는 이벤트

## 12. 데이터 작성 순서

권장:

1. 기존 재료 아이템 목록 정리
2. 공통 보상 테이블 생성
3. 파견 지역 3개 생성
4. 파견 에이전트 2명 생성
5. 모험 지역 1개 생성
6. 모험 이벤트 6개 생성
7. 선택지별 보상 테이블 연결
8. 검증 도구로 누락 확인
9. 테스트 플레이로 수량 조정

## 13. 사용자 결정 필요 체크

데이터 작성 전에 확인:

- 기존 임시 재료 아이템을 그대로 사용할 것인가?
- 없는 재료는 새 에셋을 만들 것인가?
- 1차 지역명을 제안대로 사용할 것인가?
- 첫 모험 지역을 `축축한 동굴`로 할 것인가?
- 보상 평균량 목표가 적절한가?
- 이벤트 텍스트 톤은 짧은 로그형인가, 묘사형인가?

기본 제안:

```text
기능 검증 중에는 기존 임시 재료 아이템 사용
없는 재료는 2차에 추가
지역명은 1차 제안 사용
이벤트 텍스트는 짧은 로그형
모험 실패 손실은 50%
```
