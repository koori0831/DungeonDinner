# 시연 피드백 구현 기록

- 보존 커밋: `fba8a843` (`chore: snapshot current demo work before feedback fixes`)
- 구현 브랜치: `codex/playtest-feedback`
- 기준 씬: TitleScene → MainScene, Unity 6000.3.13f1
- 합의: 첫 시작만 자동 모험, 첫 채집에서 기본 재료 6종 각 1개, 조기 마감, 발견 전 물음표, 감각 중심 주문 힌트.
- 미니게임: 도구 라벨·문자 화살표 제거, 숫자·게이지·그래픽 동작 안내 사용. 후속 요청에 따라 도마 아래 조작 안내문을 복원했다.

## 작업 현황

- [x] 현재 작업 보존 및 브랜치 생성
- [x] 모험·영업·초기 수확·입력 전환
- [x] 미니게임 마스크·드래그·목적지·설명글 제거
- [x] 공통 테마·가방·결과창·아이콘
- [x] 도감 저장 v3·발견 이벤트
- [x] 대화 위치·힌트 대사·미완성 요리 그림
- [x] 최종 레이아웃 변경 후 회귀 검사와 캡처 갱신

## 구현 내용

### 모험과 영업

새 게임의 자동 손님 호출과 초기 재료 20개 지급을 제거했다. 타이틀에서 시작하면 이끼동굴의 텍스트 모험으로 바로 이동한다. 지도 UI와 지도 선택 스크립트는 삭제하고, 파견에서 사용하는 지역 데이터는 유지했다.

첫 채집은 도구나 소모품 없이 암염·슬라임 점액·슬라임 핵·버섯 갓·납작버섯·콘치즈를 각각 1개 지급한다. 계획에서 버섯 조각으로 지칭한 기존 재료는 프로젝트의 `flat_mushroom`/납작버섯 에셋에 연결했다. 같은 새 게임에서 첫 채집을 반복할 수 없다. 첫 귀환부터 수동 영업을 시작할 수 있고, 접대 후 조기 마감과 재탐험이 가능하다. 재료가 없으면 다음 손님을 부르는 대신 마감을 안내한다.

`GameUiInput`이 화면별 입력 문맥과 전환 잠금을 관리한다. 이전 화면에서 누르던 키와 포인터를 놓아야 새 입력을 받는다. EventSystem의 Submit에서 Space를 제외해, 대사 넘기기가 숨은 귀환 버튼을 누르는 문제를 막았다. 전환·보상·시간 반영은 상태와 실행 버전으로 중복을 방지하고, 비활성화와 취소 시 입력 및 콜백을 정리한다.

### 요리와 화면 구성

10종 미니게임은 `CookingGesture`/진행값/피드백 상태를 사용한다. 재료·도구에 붙던 라벨과 상태 문장, 문자 화살표를 제거했다. 숫자·게이지와 벡터 그림, 움직이는 시범, 적중·실수 표식으로 안내한다. 후속 요청에 따라 도마 아래 전용 패널에 현재 단계의 조작 방법을 표시한다. 상세 결과 피드백은 종료 후에 제공한다.

재료 모양 마스크 안에는 재료 효과만 남겼다. 필수 타격점과 도구는 공통 조작 레이어에, 잡은 도구 그림은 상단 Canvas에 배치했다. 드래그 포인터는 종료까지 유지하고 화면 밖 놓기, 취소, 포커스 상실 시 복구한다. 폐기와 붓기 목적지는 보이는 `RectTransform`을 판정에도 사용한다. 폐기 영역은 붉은 틴트·테두리·폐기통 그림으로 표시한다. 통합 요리 프리팹과 독립 프리팹 모두 동일한 구조를 적용했다.

잘못 슬라이스된 1254×1254 아이콘 시트의 UI 참조를 개별 아이콘으로 교체했다. 파견 UI Toolkit과 요리 uGUI는 기존 갈색·크림색 에셋과 폰트를 공유한다. 일반 패널 본문은 불투명하게 하고, 중복 배경과 숨은 패널의 입력을 정리했다.

재료 가방은 720×680, 4열 팝업으로 바꾸고 제목 드래그·접기·닫기·상시 열기 버튼을 추가했다. 검색어와 선택·스크롤 상태를 보존하고 화면 안으로 이동을 제한한다. 좁은 화면에서 도감 상세를 열면 접히며 미니게임에서는 숨는다. 결과창은 요리 이미지와 이름, 품질, 펼칠 수 있는 내부 스크롤 상세, 건네기 순으로 정리했다. 손님 판정 결과와 다음 손님·마감 버튼이 겹쳐 열리지 않는다.

### 도감과 주문

일반 도감과 요리 도감은 고유 ID와 같은 `CookingKnowledgeStore` 기록을 사용한다. 미발견 항목은 이름·그림·설명을 숨긴 물음표 슬롯이며 검색과 상세에서도 정보를 노출하지 않는다. 모험에서 실제 표시한 대사·삽화와 획득한 물품만 발견 처리한다. 결과 생성 기록과 접대 반응 기록을 분리해 중복 기록을 막았다. 저장 형식은 v3이며 기존 v2의 발견과 시도 기록을 이전하고 새 게임에서 초기화한다.

플레이어 말풍선은 왼쪽, NPC는 오른쪽에 배치했다. 주문과 질문의 대사는 완성 요리명을 직접 알려주지 않도록 맛·식감·온도·먹는 방식 단서로 수정했고 실제 주문 판정과 대조했다.

`incomplete_dish`는 일반 레시피 목록과 별개 표시 데이터다. 결과창과 도감이 동일한 이름·설명·그림을 사용하며 품질이 낮은 완성 요리와 구분한다.

## 그림 원본과 규격 차이

내장 imagegen으로 기존 갈색 펜선·해칭·색감의 미완성 요리 그림을 제작했다. `Assets/Work/Adventure/Graphics/Item/IncompleteDish.png`는 실제 알파 채널이 있는 PNG이며 Unity에서는 단일 FullRect Sprite, 최대 크기 512로 등록했다.

생성 도구가 반환한 원본은 **1254×1254**다. 계획의 1024×1024 및 가장자리 10~12% 여백과는 차이가 있다. 여백 수정 시도에서 배경에 체크무늬가 구워진 결과가 나와 이를 채택하지 않고 실제 투명도가 있는 원본을 보존했다. 이 원본 규격 차이는 남아 있으며, Unity 표시 크기와 투명도·잘림은 별도로 검수했다.

## 검증 방법과 산출물

- EditMode: 108/108 통과. 도메인 규칙, 발견 정보 가림, 실제 표시 분기, v2→v3 이전, 결과/접대 중복 방지, 미완성 표시 데이터와 프리팹 연결을 검사했다.
- PlayMode: 15/15 통과. 타이틀 시작, 초기 영업과 조기 마감, 파견·공유 시간, 툴팁과 화면 배치를 검사했다.
- Windows Development 빌드: Unity 6000.3.13f1, Windows x64, TitleScene → MainScene, 빌드 오류 0개.
- Windows 입력 검사: `-feedback-qa`를 명시했을 때만 실행하는 개발용 드라이버다. Input System의 가상 마우스와 키보드를 EventSystem에 전달한다. 미니게임 완료 함수를 직접 호출하지 않고 클릭·선 긋기·원형 드래그·대상 문지르기·타이밍 입력으로 완료한다.
- 통합/독립 프리팹의 현재 재료·손질 조합과 10종 전체를 검사했다. 실제 카탈로그에 연결되지 않은 유형은 테스트에서 명시적으로 손질 옵션을 만들어 검증했다. 상단 도구 정렬, UI가 겹친 상태의 드래그 유지, 화면 밖 놓기와 포커스 상실 복구, 결과 콜백 1회, 6개 해상도의 도감 경계를 포함한 검사 **373개가 통과**했다. 런타임 오류는 0개였다.
- 해상도는 1280×720, 1920×1080, 1920×1200, 2560×1440, 3440×1440, 1600×1200을 사용한다. 숨겨진 Windows 플레이어의 카메라와 Canvas를 해당 크기의 RenderTexture에 렌더링해 PNG와 경계 검사를 남긴다. 실제 모니터 모드 6종을 바꾼 수동 플레이와는 다르다. 포커스 상실은 Unity 콜백으로 재현했다.

실행 파일은 `Builds/PlaytestFeedback/DungeonDinner.exe`, 이미지와 입력 검사 기록은 `Builds/PlaytestFeedback/FeedbackQA/Windows/`, NUnit 결과는 `Logs/feedback-editmode.xml`, `Logs/feedback-playmode.xml`에 있다. 전체 373개 검사의 원본은 `full-regression.txt`에 보존했다. 이후 가방 버튼의 색상만 조정한 최종 빌드에서 시작·접대·파견·재탐험·팝업 검사 **59개를 다시 통과**했고, 해당 결과는 `report.txt`에 있다. 빌드와 로그는 Git에서 제외한다. QA를 실행하지 않는 일반 실행에서는 검증용 입력과 저장 백업 코드가 동작하지 않는다.

화면 검수에서 도감 상위 컨테이너가 1920×1080에 고정되어 울트라와이드에서 잘리는 문제를 찾아 수정했다. 파견의 기본 영문 빈 목록 문구도 한국어로 교체했다. 가방 하단 버튼은 활성·비활성 상태 모두 글자가 읽히도록 대비를 보완했다.

### 대표 캡처

- [재료 가방 1920×1080](D:/UnityProjects/DungeonDinner/Builds/PlaytestFeedback/FeedbackQA/Windows/06-bag-1920x1080.png)
- [결과 상세](D:/UnityProjects/DungeonDinner/Builds/PlaytestFeedback/FeedbackQA/Windows/08-result-details.png)
- [도감과 결과창 3440×1440](D:/UnityProjects/DungeonDinner/Builds/PlaytestFeedback/FeedbackQA/Windows/07-result-3440x1440.png)
- [텍스트 없이 표시한 폐기 목적지](D:/UnityProjects/DungeonDinner/Builds/PlaytestFeedback/FeedbackQA/Windows/stew-discard-slime_mucus.png)
- [파견 공통 테마](D:/UnityProjects/DungeonDinner/Builds/PlaytestFeedback/FeedbackQA/Windows/09-dispatch-theme.png)

## 다시 생성하고 검사하기

Unity 메뉴 `Tools/Dungeon Dinner/Apply Playtest Feedback`로 씬과 프리팹의 반영을 재실행할 수 있다. 생성 도구에도 같은 적용 경로와 발견 ID·힌트 규칙을 연결했다. `PlaytestFeedbackValidation.ApplyAndBuild`는 반영 후 Windows 빌드를 만든다. `PlaytestFeedbackValidation.BuildWindows`는 현재 상태만 빌드한다.

Windows 전체 입력 검사는 실행 파일에 `-feedback-qa -force-d3d11`을 전달한다. `-feedback-layout-only`를 추가하면 타이틀부터 접대·마감·파견·재탐험과 팝업 해상도 검사까지만 실행한다. 검사 전 기존 저장을 백업하고 씬 종료 후 복원한다.

## 후속 UI 조정 (2026-09-10)

미니게임에서 기존 도마와 조리대를 가리던 불투명 크림색 InputShield를 투명하게 복원했다. 통합·독립 프리팹과 생성 도구에 반영했으며, 배경 UI의 입력을 막는 기능은 유지한다. 손질 카드와 작업 슬롯의 진행 중 설명글은 숨겨 배경 복원 후에도 미니게임 조작 영역에 문구가 다시 비치지 않게 했다. 요리 결과창은 현재 디자인을 유지한다.

접객 말풍선은 플레이어가 크림색 배경·짙은 글자, NPC가 갈색 배경·흰 글자가 되도록 교환했다. 플레이어 왼쪽·NPC 오른쪽 배치는 유지한다.

두 프리팹의 입력 차단, 기존 도마 이미지·색상·비율, 생성 경로, 36개 씬의 배경색 덮어쓰기 유무, 말풍선 에셋 연결을 정적으로 확인했다. 이 후속 변경에 대한 Unity 플레이 검사와 Windows 재빌드는 실행하지 않았으며, 위의 검사 수치와 캡처는 이 조정 전 산출물이다.

## 요리 중 대화·주문서 유지와 손질 안내 복원

재료 선택·손질·미니게임·결과 화면에서 NPC 대화를 숨기던 처리를 제거했다. 요리 중에는 오른쪽 400 단위 영역에 기존 주문서와 스크롤 가능한 대화 기록을 배치한다. 가방은 이 영역에 들어오지 않게 제한하고, 조리 영역도 같은 경계를 사용한다. 결과창은 기존 크기와 내부 배치를 유지하면서 주문서와 겹치지 않는 위치로 이동한다. 접객 화면으로 돌아갈 때는 원래 배치를 복원한다.

주문서가 `**강조**` 문장만 수집하던 문제도 수정했다. 실제 재생한 `OrderIntent`와 직접 선택한 추가 질문의 일반 문장도 기록한다. 미선택 질문과 결과 대사는 이 경로에서 공개하지 않는다. 주문서 제목과 본문의 색상 대비와 글자 크기를 보완했다.

통합·독립 미니게임의 `ActionHUD/Instruction`에 24 크기의 조작 안내문을 추가했다. 재료 마스크 밖의 불투명 패널에 표시하며 클릭을 가로채지 않는다. 안내 패널은 도마·칼이 차지하는 범위 아래에 배치한다. 뒤집은 뒤 꺼내기, 끓이기의 이동·젓기·거품 폐기처럼 조작 단계가 바뀌면 문구도 갱신한다. 종료·취소 때는 안내문을 비운다. 프리팹을 다시 생성해도 안내문이 삭제되지 않도록 생성 도구와 기존 검사를 수정했다.

검증 파일은 `FeedbackQA/CurrentUiChecks/`에 있다. 런타임·에디터·EditMode 검사 어셈블리는 Unity의 기존 컴파일 설정으로 다시 컴파일했다. 두 프리팹의 안내 필드 연결·크기·마스크·입력 설정은 별도로 검사했다.

실행 검증은 기존 Windows 개발 빌드의 사본에 최신 런타임 코드를 적용해 진행했다. 사본의 제품 이름은 `DinnerUiCheck`로 분리해 사용자의 저장 공간을 사용하지 않는다. 기존 빌드의 직렬화 형식과 맞추기 위해 검증 사본에서는 새 안내 필드를 런타임에 생성하고, 입력 차단 배경을 현재 프리팹과 동일한 투명색으로 적용했다. 따라서 현재 프로젝트의 전체 Windows 재빌드나 Unity EditMode·PlayMode 재실행을 대신하는 검사는 아니다.

최종 사본에서 **505개 검사를 통과**했다. 6개 해상도에서 가방과 대화·주문서의 겹침 여부, 일반 문장의 주문 기록, 마우스 휠로 대화 읽기, 손질 중 주문 유지, 10종 미니게임의 실제 입력 완료, 단계별 안내 전환, 드래그 복구를 확인했다. 런타임 오류는 0개다. 기록은 `FeedbackQA/CurrentUiChecks/Player/FeedbackQA/Windows/report.txt`, 실행 로그는 `FeedbackQA/CurrentUiChecks/player-final.log`에 남겼다.

- [가방·주문서·대화 기록](D:/UnityProjects/DungeonDinner/FeedbackQA/CurrentUiChecks/Player/FeedbackQA/Windows/06-bag-1920x1080.png)
- [손질 안내와 주문서](D:/UnityProjects/DungeonDinner/FeedbackQA/CurrentUiChecks/Player/FeedbackQA/Windows/cooking-active-order-flat_mushroom-Chopping.png)
- [기존 크기를 유지한 결과창](D:/UnityProjects/DungeonDinner/FeedbackQA/CurrentUiChecks/Player/FeedbackQA/Windows/07-result-1920x1080.png)

## 손질 안내가 종이 뒤에 가려지던 문제 (2026-09-11)

실제 통합·독립 프리팹에서 `ActionHUD/Instruction`이 첫 번째 자식이고 불투명한 `PaperBackground`가 그 다음 자식이었다. uGUI가 종이를 나중에 그리면서 안내문을 덮었다. 앞선 검증에서는 안내문을 런타임에 마지막 자식으로 생성했기 때문에 저장된 프리팹의 순서 오류를 놓쳤다.

두 프리팹의 종이를 첫 번째, 안내문을 마지막 자식으로 정렬했다. 실행 시 안내 필드를 준비할 때도 같은 순서를 보장하고 생성 도구에 반영했다. 안내문에는 좌우·위쪽 28 단위 여백을 확보했다. 기존 설명 문자열과 숫자 게이지, 도마와 결과 UI는 유지한다. 프리팹 검사에 종이와 안내문의 표시 순서를 추가하고, 개발용 플레이 검사도 글자 내용뿐 아니라 실제 글자 메시와 그리기 순서를 검사한다.

- 실제 프로젝트의 Unity EditMode 미니게임 프리팹 검사 **10/10 통과**. 결과는 `Logs/instruction-visibility-editmode.xml`에 있다.
- 격리된 Windows 실행 사본에서 **104개 검사 통과**, 런타임 오류 0개. 프리팹의 안내문과 동일하게 진행률 텍스트를 복제한 후 잘못된 자식 순서를 재현하고, 운영 코드의 안내 필드 설정으로 복구했다. 통합·독립 두 형태와 6개 해상도에서 안내문을 켜고 끈 렌더링 픽셀을 비교해 실제 표시를 확인했다. 뒤집기 이후 문구 전환과 취소 시 초기화도 검사했다.
- Windows 검증은 앞선 실행 사본과 같은 격리 방식이며 현재 프로젝트 전체를 다시 빌드한 결과는 아니다. 실행 로그는 `FeedbackQA/InstructionVisibility/player-final.log`, 최종 보고서와 전후 화면은 `FeedbackQA/CurrentUiChecks/Player/FeedbackQA/InstructionVisibility/`에 있다.

[수정 전 화면](D:/UnityProjects/DungeonDinner/FeedbackQA/CurrentUiChecks/Player/FeedbackQA/InstructionVisibility/integrated-before.png) · [수정 후 화면](D:/UnityProjects/DungeonDinner/FeedbackQA/CurrentUiChecks/Player/FeedbackQA/InstructionVisibility/integrated-after-1920x1080.png)

## 파견 UI 테마 누락 수정 (2026-09-11)

파견 화면의 초기 버튼에만 공통 테마가 적용되고, 목록이 나중에 만드는 수량 조절·보상 수령 버튼은 빠져 있었다. 선택·잠금 클래스도 실제 행 대신 TemplateContainer에 붙어 표시되지 않았다. 확인창을 찾는 클래스 이름은 실제 UXML과 달랐고, 모든 Label에 지정한 글자색이 알림의 밝은 글자색을 덮었다.

목록 생성 시 실제 행을 반환하고 동적 버튼에도 테마를 적용한다. 외곽 장부·의뢰 전표·진행 카드·귀환 보고서·확인창에 공통 갈색 프레임과 크림색 종이를 배치했다. 버튼·탭·잠금·선택 상태와 진행 막대·스크롤바의 색상을 통일하고, 스프라이트에 저장된 슬라이스 경계와 투명 여백을 사용한다. 의뢰 전표와 확인창 본문은 내부 스크롤로 처리하며 핵심 버튼은 스크롤 밖에 유지한다. 프리팹 생성 도구도 공통 테마 참조를 저장한다.

- Unity EditMode 파견 검사 **38/38 통과**. 신규 14개 검사는 프리팹의 테마 연결, 실제 목록 행과 동적 버튼, 선택·잠금 상태, 공통 패널을 확인한다. 결과는 `Logs/dispatch-theme-editmode.xml`에 있다.
- 현재 프로젝트의 코드와 직렬화된 에셋으로 Windows x64 Development 빌드를 다시 생성했다. 최종 빌드 오류는 **0개**, 로그는 `Logs/dispatch-theme-final-build.log`에 있다.
- 이 최종 빌드의 사본에서 파견 UI 검사 **43/43 통과**. 파견 작성·확인·취소·발송·귀환·수령과 다시 열기, 동적 버튼의 에셋, 선택 표시, 알림 대비, 버튼 경계와 스크롤바 색상을 확인했다. 1280×720, 1920×1080, 1920×1200, 2560×1440, 3440×1440, 1600×1200으로 작성 화면과 확인창을 렌더링했다.
- 검증 사본은 제품 이름만 `DinnerUiCheck`로 바꾸어 사용자 저장 공간과 분리했다. 코드 교체나 UI 대체 생성 없이 최종 빌드에 들어간 실제 화면을 사용했다. 조작 검사는 UI Toolkit 클릭·제출 이벤트를 전달하며 캡처는 UIDocument를 RenderTexture에 직접 그린다. 물리 마우스를 사용한 수동 플레이나 uGUI와의 전체 화면 합성 검증을 대신하지 않는다.
- 최종 보고서는 `FeedbackQA/DispatchTheme/Player/FeedbackQA/Windows/report.txt`, 실행 로그는 `FeedbackQA/DispatchTheme/player-final.log`에 있다. C# 예외와 실패 검사는 없었다. 종료 로그에는 Unity의 GraphicsBuffer/ComputeBuffer 및 JobTempAlloc 정리 경고가 남아 있다.

배포용 압축 파일은 `Builds/DungeonDinner-Windows-20260911-100631.zip`이며, 검증 데이터와 디버그 심볼을 제외한 런타임 파일을 포함한다. 기존 압축 파일은 보존했다.

[파견 작성 화면](D:/UnityProjects/DungeonDinner/FeedbackQA/DispatchTheme/Player/FeedbackQA/Windows/dispatch-request-1920x1080.png) · [확인창](D:/UnityProjects/DungeonDinner/FeedbackQA/DispatchTheme/Player/FeedbackQA/Windows/dispatch-confirm-1920x1080.png) · [넓은 화면의 스크롤바](D:/UnityProjects/DungeonDinner/FeedbackQA/DispatchTheme/Player/FeedbackQA/Windows/dispatch-request-3440x1440.png)
