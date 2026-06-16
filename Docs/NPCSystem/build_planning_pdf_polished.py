from __future__ import annotations

from pathlib import Path

from reportlab.lib import colors
from reportlab.lib.enums import TA_CENTER, TA_LEFT
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle, getSampleStyleSheet
from reportlab.lib.units import mm
from reportlab.platypus import (
    Flowable,
    KeepTogether,
    PageBreak,
    Paragraph,
    SimpleDocTemplate,
    Spacer,
    Table,
    TableStyle,
)
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont


ROOT = Path(__file__).resolve().parent
OUTPUT = ROOT / "outputs" / "NPC_System_Planning_Polished.pdf"

PAGE_W, PAGE_H = A4
MARGIN_X = 16 * mm
CONTENT_W = PAGE_W - (MARGIN_X * 2)

NAVY = colors.HexColor("#172033")
BLUE = colors.HexColor("#254A7C")
TEAL = colors.HexColor("#237A73")
AMBER = colors.HexColor("#C27A21")
MINT_BG = colors.HexColor("#EAF6F3")
BLUE_BG = colors.HexColor("#EDF3FA")
AMBER_BG = colors.HexColor("#FFF4E5")
GRAY_BG = colors.HexColor("#F4F6F8")
LINE = colors.HexColor("#D0DAE6")
TEXT = colors.HexColor("#202A37")
MUTED = colors.HexColor("#667085")


def register_font() -> tuple[str, str]:
    candidates = [
        Path("C:/Windows/Fonts/malgun.ttf"),
        Path("C:/Windows/Fonts/NanumGothic.ttf"),
        Path("C:/Windows/Fonts/gulim.ttc"),
    ]
    for path in candidates:
        if path.exists():
            pdfmetrics.registerFont(TTFont("KoreanBody", str(path)))
            pdfmetrics.registerFont(TTFont("KoreanBold", str(path)))
            return "KoreanBody", "KoreanBold"
    return "Helvetica", "Helvetica-Bold"


BODY_FONT, BOLD_FONT = register_font()


def styles() -> dict[str, ParagraphStyle]:
    base = getSampleStyleSheet()
    return {
        "coverTitle": ParagraphStyle(
            "coverTitle",
            parent=base["Title"],
            fontName=BOLD_FONT,
            fontSize=29,
            leading=36,
            alignment=TA_LEFT,
            textColor=colors.white,
        ),
        "coverSub": ParagraphStyle(
            "coverSub",
            parent=base["BodyText"],
            fontName=BODY_FONT,
            fontSize=12,
            leading=18,
            textColor=colors.HexColor("#DCE8F8"),
        ),
        "h1": ParagraphStyle(
            "h1",
            parent=base["Heading1"],
            fontName=BOLD_FONT,
            fontSize=17,
            leading=22,
            spaceBefore=14,
            spaceAfter=8,
            textColor=BLUE,
        ),
        "h2": ParagraphStyle(
            "h2",
            parent=base["Heading2"],
            fontName=BOLD_FONT,
            fontSize=12,
            leading=16,
            spaceBefore=8,
            spaceAfter=5,
            textColor=NAVY,
        ),
        "body": ParagraphStyle(
            "body",
            parent=base["BodyText"],
            fontName=BODY_FONT,
            fontSize=9.1,
            leading=14,
            spaceAfter=5,
            textColor=TEXT,
            wordWrap="CJK",
        ),
        "small": ParagraphStyle(
            "small",
            parent=base["BodyText"],
            fontName=BODY_FONT,
            fontSize=7.7,
            leading=10,
            textColor=TEXT,
            wordWrap="CJK",
        ),
        "smallWhite": ParagraphStyle(
            "smallWhite",
            parent=base["BodyText"],
            fontName=BODY_FONT,
            fontSize=7.8,
            leading=11,
            textColor=colors.white,
            wordWrap="CJK",
        ),
        "tableHeader": ParagraphStyle(
            "tableHeader",
            parent=base["BodyText"],
            fontName=BOLD_FONT,
            fontSize=7.7,
            leading=10,
            textColor=colors.white,
            alignment=TA_CENTER,
            wordWrap="CJK",
        ),
        "tableCell": ParagraphStyle(
            "tableCell",
            parent=base["BodyText"],
            fontName=BODY_FONT,
            fontSize=7.5,
            leading=10,
            textColor=TEXT,
            wordWrap="CJK",
        ),
        "pill": ParagraphStyle(
            "pill",
            parent=base["BodyText"],
            fontName=BOLD_FONT,
            fontSize=7.5,
            leading=9,
            alignment=TA_CENTER,
            textColor=colors.white,
        ),
        "cardTitle": ParagraphStyle(
            "cardTitle",
            parent=base["BodyText"],
            fontName=BOLD_FONT,
            fontSize=10.2,
            leading=13,
            textColor=NAVY,
            wordWrap="CJK",
        ),
    }


S = styles()


def p(text: str, style: str = "body") -> Paragraph:
    return Paragraph(text.replace("\n", "<br/>"), S[style])


class ColorBand(Flowable):
    def __init__(self, height: float, color, text: str | None = None):
        super().__init__()
        self.width = CONTENT_W
        self.height = height
        self.color = color
        self.text = text

    def draw(self):
        c = self.canv
        c.saveState()
        c.setFillColor(self.color)
        c.roundRect(0, 0, self.width, self.height, 8, stroke=0, fill=1)
        if self.text:
            c.setFillColor(colors.white)
            c.setFont(BOLD_FONT, 12)
            c.drawString(10, self.height / 2 - 4, self.text)
        c.restoreState()


class Divider(Flowable):
    def __init__(self, color=LINE):
        super().__init__()
        self.width = CONTENT_W
        self.height = 8
        self.color = color

    def draw(self):
        self.canv.saveState()
        self.canv.setStrokeColor(self.color)
        self.canv.setLineWidth(0.7)
        self.canv.line(0, 4, self.width, 4)
        self.canv.restoreState()


def card(title: str, body: str, bg=GRAY_BG, accent=TEAL, width: float | None = None) -> Table:
    width = width or CONTENT_W
    data = [[p(title, "cardTitle")], [p(body, "small")]]
    tbl = Table(data, colWidths=[width])
    tbl.setStyle(
        TableStyle(
            [
                ("BACKGROUND", (0, 0), (-1, -1), bg),
                ("BOX", (0, 0), (-1, -1), 0.5, LINE),
                ("LINEBEFORE", (0, 0), (0, -1), 4, accent),
                ("VALIGN", (0, 0), (-1, -1), "TOP"),
                ("LEFTPADDING", (0, 0), (-1, -1), 9),
                ("RIGHTPADDING", (0, 0), (-1, -1), 8),
                ("TOPPADDING", (0, 0), (-1, -1), 6),
                ("BOTTOMPADDING", (0, 0), (-1, -1), 7),
            ]
        )
    )
    return tbl


def table(headers: list[str], rows: list[list[str]], widths: list[float] | None = None) -> Table:
    col_count = len(headers)
    if widths is None:
        widths = [CONTENT_W / col_count] * col_count
    data = [[p(h, "tableHeader") for h in headers]]
    data += [[p(str(cell), "tableCell") for cell in row] for row in rows]
    tbl = Table(data, colWidths=widths, repeatRows=1, hAlign="LEFT")
    tbl.setStyle(
        TableStyle(
            [
                ("BACKGROUND", (0, 0), (-1, 0), BLUE),
                ("BACKGROUND", (0, 1), (-1, -1), colors.white),
                ("GRID", (0, 0), (-1, -1), 0.35, LINE),
                ("VALIGN", (0, 0), (-1, -1), "TOP"),
                ("LEFTPADDING", (0, 0), (-1, -1), 5),
                ("RIGHTPADDING", (0, 0), (-1, -1), 5),
                ("TOPPADDING", (0, 0), (-1, -1), 5),
                ("BOTTOMPADDING", (0, 0), (-1, -1), 5),
                ("ROWBACKGROUNDS", (0, 1), (-1, -1), [colors.white, colors.HexColor("#FAFBFC")]),
            ]
        )
    )
    return tbl


def two_cards(left: Table, right: Table) -> Table:
    tbl = Table([[left, right]], colWidths=[CONTENT_W * 0.49, CONTENT_W * 0.49], hAlign="LEFT")
    tbl.setStyle(TableStyle([("VALIGN", (0, 0), (-1, -1), "TOP"), ("LEFTPADDING", (0, 0), (-1, -1), 0), ("RIGHTPADDING", (0, 0), (-1, -1), 0)]))
    return tbl


def flow(items: list[tuple[str, str]]) -> Table:
    cells = []
    widths = []
    for i, (title, body) in enumerate(items):
        block = Table([[p(title, "cardTitle")], [p(body, "small")]], colWidths=[CONTENT_W / len(items) - 7])
        block.setStyle(
            TableStyle(
                [
                    ("BACKGROUND", (0, 0), (-1, -1), BLUE_BG),
                    ("BOX", (0, 0), (-1, -1), 0.4, LINE),
                    ("VALIGN", (0, 0), (-1, -1), "TOP"),
                    ("LEFTPADDING", (0, 0), (-1, -1), 6),
                    ("RIGHTPADDING", (0, 0), (-1, -1), 6),
                    ("TOPPADDING", (0, 0), (-1, -1), 5),
                    ("BOTTOMPADDING", (0, 0), (-1, -1), 6),
                ]
            )
        )
        cells.append(block)
        widths.append(CONTENT_W / len(items) - 7)
        if i < len(items) - 1:
            cells.append(p("→", "h2"))
            widths.append(7)
    tbl = Table([cells], colWidths=widths, hAlign="LEFT")
    tbl.setStyle(TableStyle([("VALIGN", (0, 0), (-1, -1), "MIDDLE"), ("LEFTPADDING", (0, 0), (-1, -1), 0), ("RIGHTPADDING", (0, 0), (-1, -1), 0)]))
    return tbl


def cover() -> list:
    story: list = []
    top = Table(
        [
            [p("던전 한 끼", "smallWhite")],
            [p("NPC 시스템<br/>기획 문서", "coverTitle")],
            [p("방문 이벤트 중심 NPC Pool / 질문 / 호감도 / 재방문 설계안", "coverSub")],
        ],
        colWidths=[CONTENT_W],
        rowHeights=[22, 92, 48],
    )
    top.setStyle(
        TableStyle(
            [
                ("BACKGROUND", (0, 0), (-1, -1), NAVY),
                ("LEFTPADDING", (0, 0), (-1, -1), 18),
                ("RIGHTPADDING", (0, 0), (-1, -1), 18),
                ("TOPPADDING", (0, 0), (-1, -1), 10),
                ("BOTTOMPADDING", (0, 0), (-1, -1), 10),
                ("VALIGN", (0, 0), (-1, -1), "MIDDLE"),
            ]
        )
    )
    story.append(top)
    story.append(Spacer(1, 16))

    story.append(
        table(
            ["문서 성격", "프로토타입 범위", "검토 포인트"],
            [
                [
                    "기획/개발 연결용 1차 설계서",
                    "이끼 동굴, NPC 3명, Visit Event 5개, 하루 손님 3명",
                    "질문 톤, 판정 기준, 호감도 보상, 재방문 조건",
                ]
            ],
            widths=[CONTENT_W * 0.30, CONTENT_W * 0.36, CONTENT_W * 0.34],
        )
    )
    story.append(Spacer(1, 12))
    story.append(
        two_cards(
            card(
                "핵심 결정",
                "NPC 자체보다 Visit Event를 중심에 둔다.<br/>Visit Event는 대화 장면, 자연스럽게 드러나는 주문 의도, 질문 대화, 판정, 결과 반응을 한 번에 가진다.",
                MINT_BG,
                TEAL,
                CONTENT_W * 0.49,
            ),
            card(
                "프로토타입 목표",
                "손님 추론이 재미있는지, 질문 카테고리가 작동하는지, 유사 정답과 재방문 기억이 납득되는지 검증한다.",
                AMBER_BG,
                AMBER,
                CONTENT_W * 0.49,
            ),
        )
    )
    story.append(Spacer(1, 18))
    story.append(p("작성 기준: 현재 기획 대화 내용 반영 / 산출물: 보기 좋은 피드백용 PDF", "small"))
    story.append(PageBreak())
    return story


def build_story() -> list:
    story: list = []
    story.extend(cover())

    story.append(p("1. 한눈에 보는 시스템", "h1"))
    story.append(
        flow(
            [
                ("지역 Pool", "현재 지역에서 등장 가능한 NPC 후보를 가진다."),
                ("NPC 선택", "쿨다운, 오늘 등장 여부, 우선 이벤트를 확인한다."),
                ("Visit Event", "인사, 기억, 주문 의도, 질문 대화를 결정한다."),
                ("요리 판정", "괴식, 정답, 유사 정답, 비정답을 판정한다."),
                ("기억 갱신", "호감도, 플래그, 쿨다운, 다음 이벤트를 갱신한다."),
            ]
        )
    )
    story.append(Spacer(1, 10))
    story.append(
        card(
            "설계 원칙",
            "손님은 메뉴명을 직접 말하지 않는다. 플레이어는 서로 주고받는 대화, 추가 질문, 백과사전, 재료 지식을 조합해 원하는 한 끼를 추론한다.",
            BLUE_BG,
            BLUE,
        )
    )
    story.append(Spacer(1, 8))
    story.append(
        table(
            ["구분", "결정안", "의도"],
            [
                ["중심 단위", "Visit Event", "대화와 주문이 따로 놀지 않게 한다."],
                ["지역 Pool", "기본은 NPC 추첨", "지역 손님이라는 감각을 살린다."],
                ["중요 이벤트", "Pool보다 우선 가능", "스토리/호감도/재방문 보상을 놓치지 않는다."],
                ["하루 손님", "고정 3명", "Cozy 분위기와 추론 시간을 보장한다."],
                ["손님 결정", "올 때마다 1명씩 결정", "방금 결과가 다음 상태에 즉시 반영된다."],
            ],
            widths=[CONTENT_W * 0.20, CONTENT_W * 0.28, CONTENT_W * 0.52],
        )
    )

    story.append(p("2. 데이터 구조", "h1"))
    story.append(
        two_cards(
            card(
                "NPC Sheet",
                "변하지 않는 손님 정보.<br/>이름, 말투, 성향, 선호 태그, 쿨다운, 호감도 규칙, 의뢰 해금 여부를 가진다.",
                GRAY_BG,
                BLUE,
                CONTENT_W * 0.49,
            ),
            card(
                "Visit Event Sheet",
                "이번 방문에서 일어나는 일.<br/>기본 대화, 주문 의도, 질문 대화, 정답 판정, 결과 반응, 완료 후 변화를 가진다.",
                GRAY_BG,
                TEAL,
                CONTENT_W * 0.49,
            ),
        )
    )
    story.append(Spacer(1, 8))
    story.append(
        table(
            ["시트", "한 줄의 의미", "주요 필드"],
            [
                ["NPC", "NPC 한 명", "NPC ID, 이름, 말투, 선호/비선호 태그, 기본 쿨다운, 호감도 규칙"],
                ["VisitEvents", "방문 이벤트 하나", "Event ID, NPC ID, 이벤트 타입, 반복 정책, 정답 조건, 결과 반응"],
                ["DialogueLines", "대화 한 줄", "Event ID, Dialogue Group, Line Order, Speaker, Text, Purpose, Question Category"],
                ["RegionPools", "지역 Pool 항목 하나", "Region ID, NPC ID, Weight, Min Day, Pool Type, Condition"],
                ["Recipes", "레시피 하나", "Recipe ID, Food Type, Tags, Ingredients, Handling, Disgusting Rules"],
                ["QuestionCategories", "질문 카테고리 하나", "Category ID, 표시 이름, 해금 레벨, 예시 질문 톤"],
                ["Enums", "공통 값 목록", "System Type, Repeat Policy, Priority, Result, Affinity Level"],
            ],
            widths=[CONTENT_W * 0.18, CONTENT_W * 0.25, CONTENT_W * 0.57],
        )
    )

    story.append(p("3. Visit Event 규칙", "h1"))
    story.append(
        table(
            ["항목", "내용"],
            [
                ["정의", "대화 장면 + 주문 의도 + 질문 대화 + 정답 판정 + 결과 반응 + 완료 후 변화"],
                ["예시", "Novice_FirstVisit_SweetDrink, Novice_Revisit_SmoothieMemory"],
                ["첫 방문", "NeverRepeat. 모든 일반 대화가 소진되어도 다시 나오지 않는다."],
                ["일반 주문", "CycleRepeat. 반복 가능한 이벤트를 모두 본 뒤 순환한다."],
                ["약한 기억", "CooldownRepeat. 반복 가능하지만 최근 N회 또는 N영업일 동안 제외한다."],
                ["강한 기억", "대부분 NeverRepeat. 특별한 재방문 감각을 유지한다."],
            ],
            widths=[CONTENT_W * 0.24, CONTENT_W * 0.76],
        )
    )
    story.append(Spacer(1, 8))
    story.append(
        table(
            ["Dialogue Group", "역할", "예시"],
            [
                ["Intro", "인사와 재회 반응", "요른: 안녕하신가, 주인장!"],
                ["Memory", "이전 방문/요리 기억", "요른: 저번 마그마 큐브 음식을 잊지 못하겠어서 다시 찾아왔다네."],
                ["OrderIntent", "주문 의도 노출", "요른: 이번에도 매콤하고 뜨거운 음식이 당기는군."],
                ["Question_Taste", "맛 질문 선택 시 대화", "플레이어: 최근에 좋아하시는 맛이 있으신가요?"],
                ["Result_Correct", "정답 결과 반응", "NPC가 무엇이 좋았는지 말한다."],
            ],
            widths=[CONTENT_W * 0.22, CONTENT_W * 0.32, CONTENT_W * 0.46],
        )
    )
    story.append(Spacer(1, 8))
    story.append(
        table(
            ["우선순위", "이벤트 종류", "메모"],
            [
                ["1", "튜토리얼 / 메인 스토리 필수", "진행에 필요한 이벤트"],
                ["2", "지역 해금 / 진행 필수", "다음 구역, 허가서 등"],
                ["3", "호감도 단계 상승", "관계 보상 체감"],
                ["4", "강한 기억 / 재방문", "이전 요리 결과 반응"],
                ["5", "진행 중인 연속 이벤트", "체인형 대화"],
                ["6", "약한 기억 / 단골 이벤트", "쿨다운 반복"],
                ["7", "일반 주문 이벤트", "평소 장사 흐름"],
            ],
            widths=[CONTENT_W * 0.16, CONTENT_W * 0.34, CONTENT_W * 0.50],
        )
    )

    story.append(p("4. 질문과 호감도", "h1"))
    story.append(
        card(
            "질문도 대화처럼",
            "카테고리 선택 후 채팅창에는 짧은 질문 대화가 나온다. 플레이어 질문, NPC의 반응, 실제 단서, 플레이어의 확인까지 이어질 수 있다.",
            MINT_BG,
            TEAL,
        )
    )
    story.append(Spacer(1, 8))
    story.append(
        table(
            ["카테고리", "해금", "질문 대화 예시", "주는 단서"],
            [
                ["맛", "Lv0", "플레이어: 최근에 좋아하시는 맛이 있으신가요?\n요른: 아! 저번 마그마 큐브 음식의 단짠단짠이 예술이었지.\n플레이어: 단짠단짠... 넵.", "맛 방향과 이전 기억을 좁힌다."],
                ["온도/식감", "Lv0", "플레이어: 오늘도 뜨거운 쪽이 좋으세요?\n요른: 속에서 불이 오르는 느낌이면 더 좋지.\n플레이어: 뜨겁게 준비해볼게요.", "온도와 식감 방향을 좁힌다."],
                ["몸 상태", "Lv0", "플레이어: 오늘도 많이 싸우고 오셨어요?\n요른: 팔이 뻐근하군. 씹는 맛 있는 걸 먹으면 힘이 돌 것 같네.", "효과와 식감 태그를 드러낸다."],
                ["피하고 싶은 것", "Lv1", "플레이어: 오늘 피하고 싶은 건 있으세요?\n요른: 흐물흐물한 건 사양하겠네.", "금지 태그를 확인한다."],
            ],
            widths=[CONTENT_W * 0.16, CONTENT_W * 0.10, CONTENT_W * 0.50, CONTENT_W * 0.24],
        )
    )
    story.append(Spacer(1, 8))
    story.append(
        two_cards(
            card(
                "Level 0",
                "질문 1회.<br/>맛 / 온도·식감 / 몸 상태 질문 가능.",
                BLUE_BG,
                BLUE,
                CONTENT_W * 0.49,
            ),
            card(
                "Level 1",
                "질문 2회.<br/>피하고 싶은 것 카테고리 해금.",
                AMBER_BG,
                AMBER,
                CONTENT_W * 0.49,
            ),
        )
    )
    story.append(Spacer(1, 8))
    story.append(
        table(
            ["요리 결과", "호감도 변화", "재방문 조건"],
            [
                ["괴식", "0", "불만 반응만 기록"],
                ["비정답", "0", "재방문 기억 없음"],
                ["유사 정답", "+5", "재방문 기억 조건 만족"],
                ["정답", "+15", "재방문 기억 조건 만족"],
                ["완벽한 정답", "+25", "정식 확장용"],
            ],
            widths=[CONTENT_W * 0.30, CONTENT_W * 0.20, CONTENT_W * 0.50],
        )
    )

    story.append(p("5. 정답 판정과 재방문", "h1"))
    story.append(
        flow(
            [
                ("1. 괴식", "금지 조합 또는 금지 손질법이면 즉시 괴식."),
                ("2. 정답", "정답 레시피와 일치하면 정답."),
                ("3. 유사", "음식 종류, 필수 태그, 보너스 태그를 확인."),
                ("4. 비정답", "핵심 조건을 만족하지 못하면 비정답."),
            ]
        )
    )
    story.append(Spacer(1, 9))
    story.append(
        table(
            ["판정 축", "예시"],
            [
                ["정답 레시피", "허브 슬라임 스무디"],
                ["허용 음식 종류", "음료, 디저트"],
                ["필수 태그", "달콤함"],
                ["보너스 태그", "차가움, 부드러움, 가벼움"],
                ["금지 태그", "기름짐, 쓴맛"],
                ["괴식 조건", "슬라임 젤리 굽기"],
            ],
            widths=[CONTENT_W * 0.30, CONTENT_W * 0.70],
        )
    )
    story.append(Spacer(1, 8))
    story.append(
        card(
            "재방문 기억 이벤트",
            "프로토타입에서는 유사 정답 이상이면 재방문 기억 이벤트 조건을 만족한다. 결과 등급은 Wrong / Similar / Correct / Perfect로 저장해두고, 정식 확장 때 Similar는 약한 기억, Correct 이상은 강한 기억으로 나눌 수 있다.",
            AMBER_BG,
            AMBER,
        )
    )

    story.append(PageBreak())
    story.append(p("6. 프로토타입 콘텐츠 예시", "h1"))
    story.append(
        table(
            ["NPC", "Visit Event", "첫 대사", "정답", "역할"],
            [
                [
                    "지친 초보 모험가",
                    "Novice_FirstVisit_SweetDrink",
                    "오늘은 너무 지쳤어. 달달한 게 먹고 싶어.",
                    "허브 슬라임 스무디",
                    "첫 손님 추론 튜토리얼",
                ],
                [
                    "지친 초보 모험가",
                    "Novice_AffinityLv1_Greeting",
                    "아, 지난번 그 주방차다. 여기서 또 보니까 반갑네.",
                    "-",
                    "호감도 보상 체감",
                ],
                [
                    "지친 초보 모험가",
                    "Novice_Revisit_SmoothieMemory",
                    "저번에 마신 달달한 음료가 생각나서 또 왔어.",
                    "허브 슬라임 스무디 계열",
                    "재방문 기억 검증",
                ],
                [
                    "광산길 정찰병",
                    "MineScout_FirstVisit_WarmSoup",
                    "몸이 으슬으슬한데, 속은 부담스럽지 않았으면 좋겠어.",
                    "발광 버섯 맑은 수프",
                    "따뜻함/가벼움 추론",
                ],
                [
                    "겁 많은 마법 견습생",
                    "MagicApprentice_FirstVisit_ManaTea",
                    "마력을 조금 회복하고 싶은데... 너무 쓴 건 싫어요.",
                    "동굴 꿀 허브차",
                    "효과 태그 추론",
                ],
            ],
            widths=[CONTENT_W * 0.16, CONTENT_W * 0.22, CONTENT_W * 0.31, CONTENT_W * 0.16, CONTENT_W * 0.15],
        )
    )
    story.append(Spacer(1, 10))
    story.append(
        table(
            ["NPC", "맛 질문", "온도/식감 질문", "몸 상태 질문"],
            [
                [
                    "지친 초보 모험가",
                    "단 건 괜찮아요?<br/>응. 근데 너무 끈적한 건 말고.",
                    "시원한 게 좋아요?<br/>응. 목이 좀 말라.",
                    "많이 지치셨어요?<br/>완전 녹초야. 씹는 것도 귀찮아.",
                ],
                [
                    "광산길 정찰병",
                    "진한 맛이 좋아요?<br/>아니, 오늘은 좀 맑은 게 좋아.",
                    "따뜻한 게 좋아요?<br/>응. 속부터 데워지는 걸로.",
                    "속은 괜찮으세요?<br/>무거운 건 먹기 힘들 것 같아.",
                ],
                [
                    "겁 많은 마법 견습생",
                    "쓴맛은 싫으세요?<br/>네. 약 같은 맛은 조금 무서워요.",
                    "따뜻한 차도 괜찮아요?<br/>그런 거라면 마시기 편할 것 같아요.",
                    "마력이 많이 빠졌어요?<br/>네, 손끝이 살짝 떨려요.",
                ],
            ],
            widths=[CONTENT_W * 0.17, CONTENT_W * 0.28, CONTENT_W * 0.28, CONTENT_W * 0.27],
        )
    )

    story.append(p("7. 정식 확장 자리", "h1"))
    story.append(
        table(
            ["확장 요소", "현재 문서에서 남겨둔 자리", "정식 설계 시 추가할 것"],
            [
                ["연속 이벤트", "Chain ID / Chain Step / Next Event ID", "체인별 강제/우선 후보 규칙, 중단 조건"],
                ["약한 기억", "WeakMemory / CooldownRepeat", "최근 N회 제한, 대사 변주, 등장 빈도"],
                ["의뢰", "Request Available / Unlock Level / Unlock Event", "의뢰 지역, 재료, 소요 시간, 보수, 성공률"],
                ["호감도 고단계", "Level 2~5 의미만 정의", "출신/문화 질문, 단골 대사, 특별 의뢰"],
                ["대사 변주", "Memo 또는 별도 Variation 필드 후보", "같은 이벤트의 표현 2~3종"],
            ],
            widths=[CONTENT_W * 0.20, CONTENT_W * 0.37, CONTENT_W * 0.43],
        )
    )

    story.append(p("8. 피드백 체크리스트", "h1"))
    checklist = [
        ["Visit Event 중심 구조가 직관적인가?", "대화와 주문을 한 이벤트에 묶는 방식이 작업하기 쉬운지 확인"],
        ["NPC 방문이 대화 장면처럼 느껴지는가?", "NPC와 플레이어가 서로 반응하고 주문이 자연스럽게 이어지는지 확인"],
        ["질문 대화가 자연스러운가?", "카테고리 선택 후 질문, NPC 반응, 단서, 플레이어 확인이 이어지는지 확인"],
        ["유사 정답 기준이 납득되는가?", "음식 종류 + 필수 태그 + 보너스 태그 조합 확인"],
        ["호감도 Level 1 보상이 체감되는가?", "질문 2회 + 피하고 싶은 것 해금이 충분한지 확인"],
        ["재방문 조건이 적당한가?", "유사 정답 이상이면 기억 이벤트 발생하는 현재 기준 검토"],
        ["프로토타입 범위가 적당한가?", "4주 검증 기준에서 과하거나 부족한 항목 확인"],
    ]
    story.append(table(["검토 항목", "확인 내용"], checklist, widths=[CONTENT_W * 0.36, CONTENT_W * 0.64]))
    story.append(Spacer(1, 10))
    story.append(
        card(
            "다음 작업 제안",
            "이 PDF에 피드백을 반영한 뒤, 엑셀 템플릿의 VisitEvents 시트에 실제 대사를 조금 더 늘려 프로토타입 데이터로 전환한다.",
            MINT_BG,
            TEAL,
        )
    )

    return story


def page_frame(canvas, doc):
    canvas.saveState()
    canvas.setFillColor(NAVY)
    canvas.rect(0, PAGE_H - 10 * mm, PAGE_W, 10 * mm, stroke=0, fill=1)
    canvas.setFillColor(colors.white)
    canvas.setFont(BOLD_FONT, 8)
    canvas.drawString(MARGIN_X, PAGE_H - 6.5 * mm, "Dungeon Dinner / NPC System Planning")
    canvas.setFillColor(MUTED)
    canvas.setFont(BODY_FONT, 8)
    canvas.drawRightString(PAGE_W - MARGIN_X, 10 * mm, f"{doc.page}")
    canvas.setStrokeColor(LINE)
    canvas.line(MARGIN_X, 14 * mm, PAGE_W - MARGIN_X, 14 * mm)
    canvas.restoreState()


def main() -> None:
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    doc = SimpleDocTemplate(
        str(OUTPUT),
        pagesize=A4,
        rightMargin=MARGIN_X,
        leftMargin=MARGIN_X,
        topMargin=18 * mm,
        bottomMargin=18 * mm,
        title="NPC System Planning Polished",
        author="Dungeon Dinner",
    )
    story = build_story()
    doc.build(story, onFirstPage=page_frame, onLaterPages=page_frame)
    print(OUTPUT)


if __name__ == "__main__":
    main()
