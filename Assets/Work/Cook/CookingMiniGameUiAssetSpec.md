# 요리 미니게임 UI QA 및 에셋 명세

## 1. 범위와 판정 기준

- 대상: `CookTestScene`, `CookingPresentationRoot.prefab`, 10종 요리 미니게임, 결과 화면
- 확인 환경: Unity 6.3 LTS, Game View Full HD `(1920×1080)`
- 확인 기준: 플레이어가 **현재 목표 → 입력 방법 → 진행 상태 → 결과 원인**을 빠르게 이해하고, 입력 직후 피드백을 받고, 판정 결과를 납득할 수 있는가
- 에셋 분류 기준
  - **UIAsset**: `Assets/Work/Cook/Graphics/UIAsset`
  - **공유 기반 에셋**: 프로젝트 공용 폰트 및 Adventure 아이템 아이콘. UIAsset 밖이지만 의도된 공유 참조
  - **UI 확장 에셋**: Cook 전용이지만 UIAsset이 아닌 `Graphics/UIPresentation`
  - **임시 에셋**: `TempImage`, `TemporaryLabel`, 런타임 생성 스프라이트/톤, 비어 있는 최종 에셋 슬롯

## 2. QA 결과 요약

10종을 실제 Game View에서 순차 확인했고, 마지막으로 Chopping 진입과 `Good / 0.80` 강제 판정 흐름을 다시 확인했다.

| 타입 | 현재 판정 | 확인 및 수정 내용 | 남은 아트 작업 |
| --- | --- | --- | --- |
| Slicing | 통과 | 절단 순서, 진행 상태, 완료 피드백 식별 가능 | 칼/절단선 최종 스프라이트 |
| Roasting | 통과 | 뒤집기와 접시 이동 단계 식별 가능 | 팬, 접시, 익힘/연기 단계 |
| Cleansing | 통과 | 문지르기 범위와 제거 진행도 식별 가능 | 브러시, 얼룩 단계, 거품/닦임 FX |
| Chopping | 수정 후 통과 | 상단 `타격 1/5`와 하단 `타격 0/5`가 충돌하던 문구를 `다음 타격점 · 1/5`로 수정 | 활성 타격점에 어두운 외곽선 필요 |
| Burning | 수정 후 통과 | 아이콘 없는 생성 재료가 큰 흰 사각형으로 보이던 경로를 공용 임시 아이콘 해석기로 통일 | 생성 재료 최종 아이콘, 그을음/연기 단계 |
| Boiling | 통과 | 목표 구간과 드롭 동작 식별 가능 | 국자, 접시, 거품/증기 단계 |
| Stewing | 수정 후 통과 | 폰트 미지원 원형 화살표 문자를 제거하고 단계 안내를 텍스트로 명확화 | 화력 노브, 젓기 궤도/방향 스프라이트 |
| Freezing | 수정 후 통과 | 밝은 재료에서 거의 보이지 않던 초기 냉기 오버레이 알파를 `0.06 → 0.18`로 보강 | 냉기 누적/과냉각 단계 |
| Grinding | 수정 후 통과 | 폰트 미지원 `↻` 문자를 제거 | 절구/막자, 시계 방향 화살표 스프라이트 |
| Diluting | 조건부 통과 | 입력 단계는 식별되나 생성 재료 아이콘이 런타임 대체 이미지에 의존 | 물통, 물줄기, 농도 단계, 생성 재료 아이콘 |

추가로 결과 배지의 흰색 텍스트가 밝은 판정색과 겹치던 대비 결함을 수정했다. 판정명, 점수, 사유를 공통 짙은 갈색 `RGB(20, 14, 9)` 계열로 변경했다.

## 3. 현재 사용 에셋 감사

### 3.1 UIAsset에서 정상 사용 중인 에셋

`CookingPresentationRoot.prefab`의 주 프레임과 카드 계열은 다음 UIAsset을 사용한다.

- `Btn1.png`, `Btn2.png`
- `Receipt.png`
- `ChatBubblePlayer.png`, `ChatBubbleOther.png`
- `Label.png`
- `CardBg.png`
- `Pannel.png`

현재 대상 씬/프리팹/SO에서 중복된 레거시 폴더 `Assets/Work/Cook/Graphics/UI`의 참조는 확인되지 않았다.

### 3.2 UIAsset 외 사용 중인 에셋

| 분류 | 경로/대상 | 사용 위치 | 판정 |
| --- | --- | --- | --- |
| UI 확장 | `Assets/Work/Cook/Graphics/UIPresentation/CookingUiEmblems.png` | 품질 4종, 반응 5종, 태그 4종, 보상, NPC 대체 아이콘 | 의도된 Cook UI 에셋. 장기적으로 UIAsset 또는 전용 Atlas 규칙에 편입 권장 |
| 공유 기반 | `Assets/Font/MangoDdobak-B(otf) SDF.asset` | Cook UI 전체 TMP 폰트 | 정상 공유 참조 |
| 공유 기반 | `Assets/Work/Adventure/Graphics/Item/*.png` | 실제 재료 6종 아이콘 | 정상 콘텐츠 공유 참조 |
| 임시 UI | `Assets/Work/Cook/Graphics/TempImage/FREE/FREE version/Icon set 1/1x/bone head 512 px.png` | `CookTestScene`의 기본 카테고리 아이콘, `ChapterButton`, `RecipeInfoDisplayPanel`, `InfoDisplayPanel` | 교체 필요. 미니게임 오버레이 바깥의 인접 요리 UI |
| 임시 배경 | `Assets/Work/Cook/Graphics/TempImage/Materials/TempBackGround.mat` | `CookTestScene`의 `Background` MeshRenderer | 교체 필요. UI가 아닌 씬 환경 배경 |

재료 아이콘의 실제 공유 참조는 다음과 같다.

| 재료 SO | 공유 아이콘 |
| --- | --- |
| `flat_mushroom.asset` | `FlatMushroom.png` |
| `mushroom_cap.asset` | `MushroomCap.png` |
| `rock_salt.asset` | `RockSalt.png` |
| `slime_mucus.asset` | `SlimeMucus.png` |
| `corn_cheese.asset` | `CornCheese.png` |
| `slime_nucleus.asset` | `SlimeCore.png` |

### 3.3 명시적 임시 에셋/표현

| 위치 | 현재 상태 | 영향 | 교체 대상 |
| --- | --- | --- | --- |
| `CookingPresentationRoot.prefab` | `TemporaryLabel` 오브젝트 12개 | 도구가 문자/단색 도형처럼 보여 입력 방식 파악이 느림 | 동작별 도구 및 제스처 스프라이트 |
| `CookingMiniGameOverlaySettings.asset` | `knife/brush/pan/plate/pestle/pitcherSprite` 6개가 비어 있음 | 코드 생성 도형 또는 문자 대체 표현 사용 | 직접 연결 가능한 도구 스프라이트 6종 |
| 같은 설정 | `action/success/mistakeClip` 3개가 비어 있음 | 런타임 생성 톤 사용 | 공통 입력/성공/실수 효과음 |
| `CookingPresentationRoot.prefab` | `useTemporaryFeedbackAudio = true` | 최종 사운드가 없어 판정 질감이 임시 상태 | 최종 오디오 연결 후 `false` 전환 |
| `CookingUiPresentationSettings.asset` | `dishReveal/qualityStamp/rewardCountClip` 3개가 비어 있음 | 요리 결과 연출 사운드가 없음 | 결과 공개/도장/보상 카운트 효과음 |
| 생성 재료 SO 2개 | `NewRecipe_ingredient`, `slime_nucleus_dango_ingredient`에 `iconSprite` 없음 | 런타임 텍스트 아이콘 사용 | 재료 아이콘 2종 또는 공식 Unknown 아이콘 |
| 결과 요리 | 결과 아이콘 소스가 없으면 `dish_missing`/런타임 텍스트 아이콘 사용 | 결과 화면의 완성도 저하 | 공식 미완성 요리/Unknown Dish 아이콘 |
| 미니게임 가이드 | 다수 `Image.m_Sprite = null` + 단색 채움 | 기능은 동작하나 재료와 세계관 표현이 분리됨 | 목표선, 게이지, 드롭존, 방향 가이드, FX |

`CookingPresentationRoot.prefab` 내부에는 `Graphics/TempImage`의 정적 스프라이트 참조는 없다. 현재 임시성은 주로 비어 있는 슬롯, `TemporaryLabel`, 런타임 생성 이미지/오디오에서 발생한다.

## 4. 텍스트와 배경 겹침/대비 감사

### 4.1 확인된 결함과 조치

| 구간 | 기존 문제 | 조치 | 상태 |
| --- | --- | --- | --- |
| 결과 배지 | 밝은 녹색·노랑·주황·빨강 배경 위 흰색/밝은 베이지 텍스트 | 판정명/점수/사유를 짙은 갈색으로 통일 | 수정 완료 |
| Chopping 상태 | 상단 진행 문구와 하단 실제 타격 수가 서로 다른 의미인데 동일한 `타격`으로 표시 | 상단을 `다음 타격점`으로 변경 | 수정 완료 |
| Stewing/Grinding | 폰트가 지원하지 않는 원형 화살표 문자가 사각형으로 표시 | 텍스트에서 제거, 최종 방향 아이콘 명세 추가 | 수정 완료 |
| Freezing | 녹색/밝은 재료 위 초기 냉기 표현이 거의 사라짐 | 초기 오버레이 알파 증가 | 수정 완료 |

새 결과 텍스트색의 계산 대비는 현재 판정 배경 기준으로 다음과 같다.

- Perfect: 약 `11.86:1`
- Good: 약 `13.41:1`
- Normal: 약 `10.13:1`
- Bad: 약 `5.78:1`

모두 일반 텍스트 목표 `4.5:1`을 넘는다.

### 4.2 최종 아트 적용 시 재확인이 필요한 구간

- Chopping의 노란 타격점은 베이지 버섯 재료와 가까운 명도다. 최소 3 px의 짙은 외곽선 또는 검은 그림자를 포함해야 한다.
- Grinding의 재료명은 상단 장식/재료 비주얼과 가까워질 수 있다. 텍스트 안전 영역을 상단 96 px 이내로 고정하고 재료 아트가 침범하지 않게 한다.
- 활성 가이드, 목표 구간, 완료 상태는 색만 바꾸지 않고 실루엣, 체크, 점멸/펄스를 함께 사용한다.
- 일반 텍스트는 배경과 `4.5:1`, 큰 텍스트 및 필수 UI 표식은 `3:1` 이상을 유지한다.

## 5. 현재 필요한 에셋 명세

### 5.1 P0 — 직접 연결 가능한 필수 에셋

| 묶음 | 수량 | 파일/역할 | 원본 규격 |
| --- | ---: | --- | --- |
| 조리법 아이콘 | 10종 | Slicing, Roasting, Cleansing, Chopping, Burning, Boiling, Stewing, Freezing, Grinding, Diluting | 투명 PNG, 512×512, 안전 여백 12% |
| 설정 연결 도구 | 6종 | Knife, Brush, Pan, Plate, Pestle, Pitcher | 투명 PNG, 512×512. 접촉 도구는 접촉점 Pivot 제공 |
| 공통 제스처 | 5종 | Tap, Linear Drag, Circular Drag, Scrub, Release/Drop | 투명 PNG, 256×256, 색 없이도 구분되는 실루엣 |
| 공통 HUD 패널 | 3종 | ActionDock, MistakeToast, ResultBadge 바탕 | 9-slice PNG, 1024×256, Border 24~40 px |
| 결과 배지 | 4종 | Perfect, Good, Normal, Bad | 투명 PNG, 512×256. 색과 서로 다른 외곽 실루엣 병행 |
| 공통 피드백 FX | 4종 | Target Pulse, Hit, Mistake, Complete | 투명 Sprite Sheet, 프레임당 256×256, 12~24 fps |
| 생성 재료 아이콘 | 2종 | `NewRecipe`, `slime_nucleus_dango` | 투명 PNG, 512×512, 재료 실루엣 안전 여백 10~12% |
| 공식 대체 아이콘 | 2종 | Unknown Ingredient, Unknown/Incomplete Dish | 투명 PNG, 512×512, 문자 없이 식별 가능한 실루엣 |
| 카테고리 대체 아이콘 | 1종 이상 | 현재 `bone head 512 px.png` 교체 | 투명 PNG, 256×256 또는 512×512 |
| 공통 미니게임 SFX | 3종 | Action, Success, Mistake | WAV, 48 kHz, 24-bit, mono |
| 결과 연출 SFX | 3종 | Dish Reveal, Quality Stamp, Reward Count | WAV, 48 kHz, 24-bit, mono |

도구 6종은 현재 `CookingMiniGameOverlaySettings.asset`에 슬롯이 있어 코드 변경 없이 연결할 수 있다. 국자, 화력 노브, 젓기 손잡이, 냉기 커서, 다지기 전용 망치 등 별도 도구를 추가할 경우 SO 필드 추가가 필요하다.

### 5.2 P1 — 동작별 가이드와 상태 단계

| 타입 | 필요한 에셋 | 상태/변형 |
| --- | --- | --- |
| Slicing | 칼 포인터, 절단선, 끝점, 절단 Hit FX | `next / active / complete`, 선 두께 8~12 px |
| Chopping | 타격점, 순번 마커, 적중 파동 | `idle / active / hit / miss`, 활성점 짙은 외곽선 3 px 이상 |
| Cleansing | 얼룩, 브러시 접촉 범위, 거품/닦임 FX | 얼룩 `dirty / partial / clean` 3단계 |
| Roasting | 뒤집기 가이드, 팬, 접시 드롭존, 익힘/연기 오버레이 | 익힘 `raw / target / over`, 연기 3단계 |
| Burning | 집게/뒤집기 가이드, 접시 드롭존, 그을음/연기 오버레이 | 그을음 `low / target / over`, 연기 3단계 |
| Boiling | 국자, 접시 드롭존, 거품/증기 오버레이 | 거품/증기 각 3단계, 목표 밴드 |
| Stewing | 화력 노브, 젓기 손잡이, 원형 방향 가이드, 단계 체크 | `heat / stir / stop`, clockwise 방향 스프라이트 |
| Freezing | 냉기 커서, 표면 그리드, 서리 가장자리/누적 오버레이 | `start / half / complete / overfreeze` 4단계 |
| Grinding | 절구, 막자, 원형 트랙, 시계 방향 화살표, 회전 Tick | `idle / active / complete`, 유효 반경 링 |
| Diluting | 물통, 용기/드롭존, 물줄기, 농도/채움 오버레이 | 농도 `strong / target / weak`, 물줄기 시작/유지/끝 |

### 5.3 P2 — 화면 완성도용 에셋

- 미니게임 배경 가장자리 비네트/집중 프레임 1종: 9-slice 또는 1920×1080 투명 오버레이
- 재료별 마스크/외곽광이 필요한 경우 공용 Soft Mask 1종: 512×512, 중앙 불투명/가장자리 페더 12~16 px
- 현재 `TempBackGround.mat`을 교체할 CookTestScene 환경 배경 텍스처/머티리얼 1세트. 미니게임 UI 납품과는 별도 작업으로 관리
- `CookingUiEmblems.png`를 UIAsset 정책에 편입할 경우 품질 4 + 반응 5 + 태그 4 + 보상 1 + NPC 대체 1, 총 15개 슬라이스를 유지한 전용 SpriteAtlas

## 6. 제작 및 Unity 임포트 규칙

### 스프라이트

- 포맷: PNG RGBA, 투명 배경
- Texture Type: `Sprite (2D and UI)`
- sRGB: 켬, Alpha Is Transparency: 켬, Mip Maps: 끔
- Filter Mode: Bilinear, Compression: High Quality
- Max Size: 아이콘/도구/FX 512, 패널 1024
- Pixels Per Unit: 공통 `100` 권장
- Pivot: 기본 중앙 `(0.5, 0.5)`; 칼끝, 브러시 면, 막자 끝, 물통 주둥이는 실제 접촉점 Pivot
- SpriteAtlas: Padding 4 px 이상, Extrude 2 px 이상
- 9-slice: Border 24~40 px, 코너 장식은 Border 안에 포함
- 모든 텍스트는 이미지에 굽지 않고 TMP로 유지한다.

### 오디오

- WAV, 48 kHz, 24-bit, mono, Peak `-3 dBFS` 이하
- Action `0.05~0.12초`, Mistake `0.15~0.35초`, Success `0.4~0.8초`
- 앞뒤 무음 제거, 루프 없음
- Import: Decompress On Load, Force To Mono 켬

### 파일명

- UI: `ui_cook_minigame_<type>_<role>_<state>.png`
- 공통: `ui_cook_minigame_common_<role>_<state>.png`
- 재료: `icon_cook_ingredient_<id>.png`
- 효과음: `sfx_cook_minigame_<type|common>_<action>.wav`
- 예: `ui_cook_minigame_chopping_target_active.png`

## 7. 연결 지점

| 현재 위치 | 최종 연결 |
| --- | --- |
| `CookingMiniGameOverlaySettings.asset`의 6개 Sprite 슬롯 | Knife, Brush, Pan, Plate, Pestle, Pitcher |
| 같은 설정의 3개 AudioClip 슬롯 | Action, Success, Mistake |
| `CookingUiPresentationSettings.asset`의 3개 AudioClip 슬롯 | Dish Reveal, Quality Stamp, Reward Count |
| `TemporaryLabel` 12개 | 동작별 도구/제스처 스프라이트 |
| `KnifeGuide`, `CutLine1~3` | 칼 포인터와 절단선 상태 |
| `LocalActionHUD`, `MistakeToast`, `ResultBadge` | 공통 9-slice 패널 및 판정 배지 |
| 생성 재료 SO의 `iconSprite` | 생성 재료 2종 아이콘 |
| 카테고리 기본 아이콘 필드 | `bone head` 대체 아이콘 |

## 8. 최종 납품 확인 기준

- 플레이어가 조작 대상을 1초 이내에 찾고 현재/다음/완료 상태를 구분한다.
- HUD와 가이드가 재료 실루엣, 결과명, 점수, 사유 텍스트를 가리지 않는다.
- 입력 후 100 ms 안에 시각 또는 소리 피드백이 시작된다.
- 색각과 무관하게 상태가 실루엣, 패턴, 체크, 움직임으로 구분된다.
- 1920×1080 및 16:10에서 잘림이 없고, 본문 20 px·핵심 상태 22 px 이상으로 읽힌다.
- 일반 텍스트 대비 `4.5:1`, 큰 텍스트/필수 표식 `3:1` 이상을 충족한다.
- SpriteAtlas 적용 후 경계 번짐, 9-slice 왜곡, 압축 노이즈가 없다.
- 최종 오디오 연결 뒤 `useTemporaryFeedbackAudio`를 끄고 런타임 생성 톤이 더 이상 재생되지 않는다.
