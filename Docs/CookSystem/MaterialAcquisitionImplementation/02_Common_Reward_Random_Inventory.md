# 02. 공통 보상, 랜덤, 인벤토리 정산 구현 명세

## 1. 목표

파견과 모험은 진행 방식이 다르지만 최종적으로 재료를 얻는 시스템이다. 따라서 보상 계산과 인벤토리 지급은 공통 기반을 사용한다.

이 문서의 목표:

- 파견과 모험이 같은 보상 테이블 구조를 사용한다.
- 같은 시드와 같은 입력은 같은 결과를 만든다.
- 인벤토리에 전부 들어가지 못한 보상도 결과에 남긴다.
- 새 재료 발견 여부를 보상 결과에 포함할 수 있다.
- UI는 보상을 계산하지 않고 결과만 표시한다.

## 2. 신규 폴더

```text
Assets/Work/MaterialAcquisition/Code/Common/
Assets/Work/MaterialAcquisition/Data/Common/
```

## 3. 데이터 구조

### 3.1 `AcquisitionRewardTableSO`

역할:

- 하나의 보상 묶음을 정의한다.
- 파견 지역, 모험 선택지, 전투 결과 등에서 참조한다.

필드:

```text
- string tableId
- string displayName
- List<AcquisitionRewardEntry> entries
- int minRollCount
- int maxRollCount
- bool allowDuplicateItems
- AcquisitionRewardTableMode mode
```

`AcquisitionRewardTableMode`:

```text
AllGuaranteed
WeightedPick
ChanceEach
GuaranteedThenWeighted
```

모드 설명:

| 모드 | 설명 |
| --- | --- |
| `AllGuaranteed` | 유효한 보상 항목을 모두 지급 |
| `WeightedPick` | 가중치 기반으로 지정 횟수만큼 선택 |
| `ChanceEach` | 각 항목의 확률을 독립 판정 |
| `GuaranteedThenWeighted` | 확정 보상 지급 후 나머지를 가중치 선택 |

### 3.2 `AcquisitionRewardEntry`

필드:

```text
- ItemDataSO item
- int minAmount
- int maxAmount
- float chance
- int weight
- AcquisitionRewardRarity rarity
- bool guaranteed
- string previewGroupLabel
- List<string> tags
```

규칙:

- `item`은 반드시 있어야 한다.
- `minAmount`는 1 이상.
- `maxAmount`는 `minAmount` 이상.
- `chance`는 0~1 범위.
- `weight`는 0 이상.
- `previewGroupLabel`은 UI에서 `버섯류`, `점액류` 같은 예상 재료군 표시용으로 쓴다.

### 3.3 `AcquisitionRewardRarity`

```text
Common
Uncommon
Rare
Special
Dangerous
```

사용자 결정 필요:

- 실제 게임 표시명을 한국어로 어떻게 할지 결정해야 한다.

기본 표시 제안:

```text
Common: 일반
Uncommon: 고급
Rare: 희귀
Special: 특수
Dangerous: 위험
```

### 3.4 `AcquisitionRewardPreview`

UI에서 실제 결과를 미리 계산하지 않고 예상만 보여주기 위한 모델이다.

필드:

```text
- string groupLabel
- AcquisitionRewardRarity highestRarity
- bool hasGuaranteedReward
- bool hasRareChance
- string chanceLabel
```

예시:

```text
버섯류 / 일반~희귀 / 희귀 가능성 낮음
점액류 / 일반 / 확정
광물염 / 고급 / 가능성 보통
```

## 4. 런타임 결과 모델

### 4.1 `AcquisitionRewardRoll`

보상 계산 중간 결과.

필드:

```text
- ItemDataSO item
- int amount
- AcquisitionRewardRarity rarity
- string sourceTableId
- bool isRare
- bool isDiscoveryCandidate
```

### 4.2 `AcquisitionRewardResult`

보상 계산과 인벤토리 지급 후 최종 결과.

필드:

```text
- AcquisitionRewardSourceType sourceType
- string sourceId
- int seed
- IReadOnlyList<AcquisitionRewardResultEntry> entries
- int requestedTotalAmount
- int grantedTotalAmount
- int remainingTotalAmount
- bool hasAnyReward
- bool hasRareReward
- bool hasNewDiscovery
```

`AcquisitionRewardSourceType`:

```text
Dispatch
Adventure
AdventureSettlement
Debug
```

### 4.3 `AcquisitionRewardResultEntry`

필드:

```text
- ItemDataSO item
- int requestedAmount
- int grantedAmount
- int remainingAmount
- int currentInventoryAmount
- AcquisitionRewardRarity rarity
- bool isRare
- bool isNewDiscovery
- string sourceId
```

규칙:

- 인벤토리에 지급하지 못한 수량은 `remainingAmount`에 남긴다.
- `grantedAmount == 0`이어도 결과 항목은 남길 수 있다.
- UI는 이 결과를 기반으로 획득/미획득/보유량을 표시한다.

## 5. 랜덤

### 5.1 `IAcquisitionRandom`

```text
int RangeInt(int minInclusive, int maxExclusive);
float RangeFloat01();
int PickWeighted(IReadOnlyList<int> weights);
```

규칙:

- 도메인 로직에서 `UnityEngine.Random`을 직접 사용하지 않는다.
- 시드를 저장해서 결과를 재현할 수 있어야 한다.

### 5.2 `SeededAcquisitionRandom`

구현:

- 내부적으로 `System.Random` 사용.
- 생성자에서 `seed`를 받는다.
- `RangeFloat01()`은 0 이상 1 미만의 값을 반환한다.

주의:

- Unity의 `Random.Range`와 정수 상한 처리 방식이 다르므로 테스트로 고정한다.

## 6. 보상 계산 서비스

### 6.1 `AcquisitionRewardResolver`

공개 메서드:

```text
AcquisitionRewardRoll[] Resolve(
    AcquisitionRewardTableSO table,
    IAcquisitionRandom random,
    AcquisitionRewardResolveContext context)
```

`AcquisitionRewardResolveContext` 필드:

```text
- AcquisitionRewardSourceType sourceType
- string sourceId
- float chanceMultiplier
- float amountMultiplier
- float rareChanceBonus
- IReadOnlyList<string> bonusTags
```

처리 순서:

1. 테이블과 항목 유효성 검사
2. 확정 보상 처리
3. 모드별 확률/가중치 처리
4. 수량 범위 굴림
5. 보너스 적용
6. 결과 병합

### 6.2 보너스 적용 규칙

보너스는 원본 데이터 값을 바꾸지 않고 계산 컨텍스트에서 적용한다.

예:

```text
숲 보상 +20%
희귀 발견 확률 +10%
점액류 수량 +1
위험도 감소
```

1차 구현에서는 아래 보너스만 지원한다.

- `chanceMultiplier`
- `amountMultiplier`
- `rareChanceBonus`

NPC 특성별 세부 태그 보너스는 2차로 미룬다.

## 7. 인벤토리 지급

### 7.1 `AcquisitionInventoryGateway`

필드:

```text
- PlayerInventoryModule inventoryModule
- AcquisitionDiscoveryTracker discoveryTracker
```

공개 메서드:

```text
AcquisitionRewardResult Grant(
    AcquisitionRewardSourceType sourceType,
    string sourceId,
    int seed,
    IReadOnlyList<AcquisitionRewardRoll> rolls)
```

책임:

- `AcquisitionRewardRoll`을 `InventoryItemStack`으로 변환
- `PlayerInventoryModule.AddItems()` 호출
- 개별 지급 결과 수집
- 현재 보유 수량 조회
- 새 발견 여부 계산
- 최종 `AcquisitionRewardResult` 생성

### 7.2 지급 전후 발견 처리

새 재료 발견 판정은 지급 요청 전에 현재 보유 상태 또는 발견 기록을 확인해야 한다.

기본 제안:

- `IngredientItemDataSO`이고 연결된 `IngredientSO`가 있는 경우 발견 대상으로 본다.
- `AcquisitionDiscoveryTracker`가 이미 발견한 재료 ID를 알고 있다.
- 지급 성공 수량이 1 이상일 때 새 발견으로 기록한다.

### 7.3 `AcquisitionDiscoveryTracker`

1차 최소 구조:

```text
- HashSet<string> discoveredIngredientIds
- bool IsDiscovered(IngredientSO ingredient)
- bool MarkDiscovered(IngredientSO ingredient)
```

나중에 `CookingKnowledgeStore`나 도감 시스템과 연결한다.

## 8. 인벤토리 부족 처리

인벤토리에 보상이 다 들어가지 못하는 경우:

- 결과창에 미획득 수량을 표시한다.
- 조용히 삭제하지 않는다.
- 1차에서는 바닥 드랍 생성까지는 하지 않는다.

사용자 결정 필요:

- 미지급 보상을 나중에 다시 받을 수 있게 할 것인가?
- 인벤토리 부족 시 보상을 우편함/보관함에 넣을 것인가?

기본 제안:

- 1차는 미지급 수량을 표시하고 지급하지 않는다.
- 보관함/우편함은 2차 기능으로 둔다.

## 9. UI 표시용 텍스트

보상 결과 UI는 아래 정보를 표시한다.

```text
아이템 이름
획득 수량
미획득 수량
현재 보유 수량
희귀 여부
새 발견 여부
```

예:

```text
점액 핵 +1  보유 2  새 발견
납작 버섯 +2 / 미획득 1  보유 99
```

## 10. 파견과 모험에서의 사용 방식

### 10.1 파견

```text
DispatchResultResolver
-> AcquisitionRewardResolver.Resolve(commonRewardTable)
-> AcquisitionRewardResolver.Resolve(rareRewardTable)
-> DispatchTaskResult에 rolls 저장
-> 결과 수령 시 AcquisitionInventoryGateway.Grant()
```

파견은 복귀 가능 상태가 되었을 때 결과를 계산하거나, 파견 시작 시 저장한 시드로 결과 수령 시 계산한다.

권장:

- 결과 수령 시 계산.
- 단, `resultSeed`를 파견 시작 시 저장해서 결과는 재현 가능하게 한다.

### 10.2 모험

```text
AdventureChoiceResolver
-> 선택지 보상 테이블 Resolve
-> AdventureSession.temporaryRewards에 rolls 추가
-> 귀환/종료 시 AcquisitionInventoryGateway.Grant()
```

모험 중에는 인벤토리에 지급하지 않는다.

## 11. 검증 규칙

`AcquisitionRewardTableSO` 검증:

- `tableId` 비어 있음
- `entries` 비어 있음
- `item` 누락
- `minAmount < 1`
- `maxAmount < minAmount`
- `chance < 0` 또는 `chance > 1`
- `weight < 0`
- `WeightedPick`인데 모든 가중치가 0
- `minRollCount > maxRollCount`

## 12. 테스트 기준

필수 테스트:

- 같은 시드와 같은 테이블은 같은 보상 결과를 만든다.
- `AllGuaranteed`는 모든 확정 보상을 만든다.
- `ChanceEach`는 확률 판정을 각 항목에 적용한다.
- `WeightedPick`은 가중치 0 항목을 선택하지 않는다.
- 수량은 `minAmount`와 `maxAmount` 범위 안에 있다.
- 인벤토리 부족 시 `remainingAmount`가 기록된다.
- 새 재료 최초 지급 시 `isNewDiscovery`가 true가 된다.
- 이미 발견한 재료는 다시 새 발견으로 표시되지 않는다.

## 13. 사용자 결정 필요 체크

구현 전 확인:

- 희귀도 한국어 표시명은 무엇인가?
- 희귀 보상 확률을 숫자로 공개할 것인가?
- 인벤토리 부족으로 못 받은 보상은 사라지는가, 보관되는가?
- 새 발견은 지급 성공 시점에만 기록할 것인가?
- 도감 연동은 1차에 포함할 것인가, 결과 플래그만 남길 것인가?

기본 제안:

```text
희귀도: 일반/고급/희귀/특수/위험
확률 공개: 낮음/보통/높음 텍스트
인벤토리 부족: 1차는 미지급 표시 후 소멸
새 발견: 지급 성공 시 기록
도감 연동: 1차는 결과 플래그와 간단 기록만
```
