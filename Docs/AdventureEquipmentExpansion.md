# 어드벤처 장비 확장

어드벤처 장비를 칼·망치 2종에서 6종으로 늘리고, 획득·교환 이벤트 6개와 활용 이벤트 6개(총 36개 선택지)를 추가했다. 기존 AdventureItemSO, AdventureItemReward, LockedOption, IngredientLockedOption과 이미지 프리팹 방식을 사용한다.

| 장비 | 획득 경로 | 활용 |
| --- | --- | --- |
| 밧줄 | 바위 아래 짐 꾸러미 / 머쉬룸맨의 매듭 작업 돕기 | 절벽 식량 회수, 저장고 문 열기, 구덩이 모험가 구조 |
| 탐험 등불 | 노인의 짐 정리 / 병 교환 / 집게로 등불 수리 | 절벽 발판 찾기, 어두운 둥지 조사, 저장고 선반 조사 |
| 긴 집게 | 야영지 도구 상자 / 암염 교환 | 등불 수리, 둥지 조사, 온천의 게 꺼내기, 점액 속 핵 채집 |
| 채집병 | 야영지 도구 상자 / 콘치즈 교환 | 등불 교환, 모험가에게 물 전달, 온천·점액 웅덩이 채집 |
| 칼·망치 | 공구 운반자에게 받기, 돌 틈의 칼 꺼내기, 구조 사례 | 기존 채집·거래 선택지 및 새 획득 선택지 |

도구는 보통 유지된다. 구덩이에 고정하는 밧줄, 물과 함께 전달하는 채집병, 등불과 교환하는 채집병은 1개 소모한다. 일반 채집 후에는 병을 비우고 씻어 다시 사용한다. 모든 만남은 독립적인 무작위 이벤트이며 퀘스트 진행 상태나 제작 시스템을 추가하지 않았다.

## 추가 이벤트

| 에셋 | 내용 |
| --- | --- |
| Find_RopeCache | 밧줄 챙기기 / 망치로 칼 꺼내기 |
| Meet_LanternKeeper | 짐 정리로 등불 받기 / 채집병을 등불과 교환 |
| Find_CookKit | 긴 집게 또는 채집병 중 하나 챙기기 |
| Meet_SupplyPorter | 칼 또는 망치 중 하나 받기 |
| Meet_RopeWeaver | 점액을 주고 밧줄 받기 / 집게로 등불 수리 |
| Meet_BottleTrader | 콘치즈를 채집병과 교환 / 암염을 집게와 교환 |
| Find_LedgePantry | 밧줄로 식량 회수 / 등불로 발판 조사 |
| Meet_PitAdventurer | 밧줄 구조 / 채집병으로 물 전달 |
| Find_DarkNest | 등불 조사 / 집게 채집 |
| Find_HotSpringBasket | 집게로 게 채집 / 병으로 점액 채집 |
| Find_StickyPool | 병으로 점액 채집 / 집게로 핵 채집 |
| Find_RootCellar | 밧줄로 문 열기 / 등불로 선반 조사 |

각 이벤트에 떠나기 선택지가 있다. 툴팁은 행동·필요 도구·소모량만 안내하며 보상 수량이나 결과를 예고하지 않는다. 도구 중복 획득 시 기존 아이콘의 수량을 갱신하고, 수량이 0인 도구의 추가 제거는 무시한다.

## 이미지와 프리팹

내장 image_gen으로 새 PNG 4장을 생성했다. 기존 knife.png와 Hamer.png의 갈색 윤곽선, 금속·가죽·나무 질감과 따뜻한 색을 참고했다. 모든 출력은 1254×1254이며 실제 알파 투명을 확인했다. 원본 알파를 유지하고 Unity Sprite로 임포트한다.

- 이미지: Assets/Work/Adventure/Graphics/Item/{Rope,Lantern,Tongs,CollectingBottle}.png
- 장비 데이터: Assets/Work/Adventure/SO/AdventureItem/의 같은 이름 .asset
- 이벤트 이미지 프리팹: Assets/Work/Adventure/Prefabs/Item/의 같은 이름 .prefab
- 이벤트 원고: Tools/adventure-equipment-content.cjs
- 에셋 생성 도구: Tools/generate-adventure-equipment.cjs (프로젝트 루트에서 Node로 실행)
- 이미지 생성 프롬프트: Docs/AdventureEquipmentImagePrompts.json

생성 도구는 이 확장의 에셋만 작성하고 기존 GUID를 유지한다. 두 씬의 이벤트 목록에는 중복 없이 추가한다. 해당 에셋을 Inspector에서 편집한 뒤 생성 도구를 다시 실행하면 원고로 덮어쓰므로 먼저 원고에도 반영해야 한다.

## 등록과 검증

AdventureTestScene과 CookTestScene의 이벤트 목록에 등록했다. 열려 있는 씬에 미저장 수정이 있으면 Ctrl+Shift+F7 메뉴로 새 이벤트만 병합할 수 있다. 이 메뉴는 씬을 저장하지 않는다.

Ctrl+Shift+F8: 에디터 에셋·씬 참조 검사와 장비별 복수 획득·활용 경로 검사.
Ctrl+Shift+F9: AdventureTestScene 플레이 중 실행. 앞서 추가한 8개와 이번 12개 이벤트의 실제 선택지 60개, 도구·재료 부족 잠금, 보상·소모 수량, 반복 획득 아이콘 수량, 중복 클릭, 대사·이미지 정리 및 글자 화면 경계를 검증한다. 테스트용 인벤토리와 대사 속도를 사용하므로 끝나면 플레이 모드를 종료한다.

검증 결과는 Temp/AdventureEditModeValidation.txt와 Temp/AdventurePlayValidation.txt에 기록된다. 이벤트별 Game View 캡처도 Temp에 저장한다.

2026-09-08 Unity 6000.3.13f1 검증 완료: 에디터 검사 8개 통과, 1920×1080 Game View에서 실제 선택지 60개 통과. 반복 획득 아이콘 수량, 소모 후 재획득, 잠금 및 중복 클릭, 보상·소모, 이미지 정리, 선택지 글자 경계를 확인했다. 현재 글꼴에 없는 대사 괄호를 ASCII 기호로 교체한 뒤 다시 실행하여 런타임 오류·누락 글자·삭제된 트윈 대상 경고가 없음을 확인했다. 별도 실행 파일 빌드는 수행하지 않았다.

열려 있던 씬의 미저장 수정은 보존했다. 자동 승인 검토가 외부 변경 감지 후 씬 전체 저장을 거부했으므로, 디스크에는 이벤트 목록을 추가하고 열려 있는 씬에는 등록 메뉴로 동일 항목만 병합했다. 씬 전체 저장이나 다시 불러오기는 하지 않았다.
