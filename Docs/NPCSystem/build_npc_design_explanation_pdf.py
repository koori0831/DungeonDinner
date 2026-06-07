from __future__ import annotations

from pathlib import Path

from reportlab.lib import colors
from reportlab.lib.enums import TA_CENTER, TA_LEFT
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle, getSampleStyleSheet
from reportlab.lib.units import mm
from reportlab.platypus import Flowable, PageBreak, Paragraph, SimpleDocTemplate, Spacer, Table, TableStyle
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont


ROOT = Path(__file__).resolve().parent
OUTPUT = ROOT / "outputs" / "NPC_Design_Explanation.pdf"

PAGE_W, PAGE_H = A4
MARGIN_X = 16 * mm
CONTENT_W = PAGE_W - MARGIN_X * 2

INK = colors.HexColor("#1C2430")
MUTED = colors.HexColor("#667085")
NAVY = colors.HexColor("#172033")
GREEN = colors.HexColor("#24786A")
GOLD = colors.HexColor("#B7791F")
RED = colors.HexColor("#B54708")
BLUE = colors.HexColor("#28527A")
SOFT_GREEN = colors.HexColor("#EAF6F3")
SOFT_GOLD = colors.HexColor("#FFF5E5")
SOFT_BLUE = colors.HexColor("#EEF4FB")
SOFT_GRAY = colors.HexColor("#F5F7FA")
LINE = colors.HexColor("#D0DAE6")


def register_font() -> tuple[str, str]:
    for font_path in [
        Path("C:/Windows/Fonts/malgun.ttf"),
        Path("C:/Windows/Fonts/NanumGothic.ttf"),
        Path("C:/Windows/Fonts/gulim.ttc"),
    ]:
        if font_path.exists():
            pdfmetrics.registerFont(TTFont("KoreanBody", str(font_path)))
            pdfmetrics.registerFont(TTFont("KoreanBold", str(font_path)))
            return "KoreanBody", "KoreanBold"
    return "Helvetica", "Helvetica-Bold"


BODY_FONT, BOLD_FONT = register_font()


def make_styles() -> dict[str, ParagraphStyle]:
    base = getSampleStyleSheet()
    return {
        "coverTitle": ParagraphStyle(
            "coverTitle",
            parent=base["Title"],
            fontName=BOLD_FONT,
            fontSize=28,
            leading=35,
            textColor=colors.white,
            alignment=TA_LEFT,
        ),
        "coverSub": ParagraphStyle(
            "coverSub",
            parent=base["BodyText"],
            fontName=BODY_FONT,
            fontSize=11.2,
            leading=17,
            textColor=colors.HexColor("#D7E7F7"),
        ),
        "h1": ParagraphStyle(
            "h1",
            parent=base["Heading1"],
            fontName=BOLD_FONT,
            fontSize=16,
            leading=21,
            spaceBefore=13,
            spaceAfter=7,
            textColor=BLUE,
        ),
        "h2": ParagraphStyle(
            "h2",
            parent=base["Heading2"],
            fontName=BOLD_FONT,
            fontSize=11.5,
            leading=15,
            spaceBefore=8,
            spaceAfter=4,
            textColor=NAVY,
        ),
        "body": ParagraphStyle(
            "body",
            parent=base["BodyText"],
            fontName=BODY_FONT,
            fontSize=9.2,
            leading=14,
            textColor=INK,
            spaceAfter=5,
            wordWrap="CJK",
        ),
        "small": ParagraphStyle(
            "small",
            parent=base["BodyText"],
            fontName=BODY_FONT,
            fontSize=7.8,
            leading=10.5,
            textColor=INK,
            wordWrap="CJK",
        ),
        "smallWhite": ParagraphStyle(
            "smallWhite",
            parent=base["BodyText"],
            fontName=BODY_FONT,
            fontSize=8,
            leading=11,
            textColor=colors.white,
            wordWrap="CJK",
        ),
        "cardTitle": ParagraphStyle(
            "cardTitle",
            parent=base["BodyText"],
            fontName=BOLD_FONT,
            fontSize=10,
            leading=13,
            textColor=NAVY,
            wordWrap="CJK",
        ),
        "tableHeader": ParagraphStyle(
            "tableHeader",
            parent=base["BodyText"],
            fontName=BOLD_FONT,
            fontSize=7.6,
            leading=10,
            textColor=colors.white,
            alignment=TA_CENTER,
            wordWrap="CJK",
        ),
        "tableCell": ParagraphStyle(
            "tableCell",
            parent=base["BodyText"],
            fontName=BODY_FONT,
            fontSize=7.4,
            leading=10,
            textColor=INK,
            wordWrap="CJK",
        ),
        "quote": ParagraphStyle(
            "quote",
            parent=base["BodyText"],
            fontName=BOLD_FONT,
            fontSize=12,
            leading=18,
            textColor=GREEN,
            alignment=TA_CENTER,
            wordWrap="CJK",
        ),
    }


S = make_styles()


def p(text: str, style: str = "body") -> Paragraph:
    safe = text.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")
    return Paragraph(safe.replace("\n", "<br/>"), S[style])


class SpacerLine(Flowable):
    def __init__(self, height=10, color=LINE):
        super().__init__()
        self.width = CONTENT_W
        self.height = height
        self.color = color

    def draw(self):
        self.canv.saveState()
        self.canv.setStrokeColor(self.color)
        self.canv.setLineWidth(0.7)
        self.canv.line(0, self.height / 2, self.width, self.height / 2)
        self.canv.restoreState()


def card(title: str, body: str, bg=SOFT_GRAY, accent=BLUE, width: float | None = None) -> Table:
    width = width or CONTENT_W
    tbl = Table([[p(title, "cardTitle")], [p(body, "small")]], colWidths=[width])
    tbl.setStyle(
        TableStyle(
            [
                ("BACKGROUND", (0, 0), (-1, -1), bg),
                ("BOX", (0, 0), (-1, -1), 0.5, LINE),
                ("LINEBEFORE", (0, 0), (0, -1), 4, accent),
                ("LEFTPADDING", (0, 0), (-1, -1), 9),
                ("RIGHTPADDING", (0, 0), (-1, -1), 8),
                ("TOPPADDING", (0, 0), (-1, -1), 6),
                ("BOTTOMPADDING", (0, 0), (-1, -1), 7),
                ("VALIGN", (0, 0), (-1, -1), "TOP"),
            ]
        )
    )
    return tbl


def two_cards(left: Table, right: Table) -> Table:
    tbl = Table([[left, right]], colWidths=[CONTENT_W * 0.49, CONTENT_W * 0.49])
    tbl.setStyle(
        TableStyle(
            [
                ("VALIGN", (0, 0), (-1, -1), "TOP"),
                ("LEFTPADDING", (0, 0), (-1, -1), 0),
                ("RIGHTPADDING", (0, 0), (-1, -1), 0),
            ]
        )
    )
    return tbl


def table(headers: list[str], rows: list[list[str]], widths: list[float] | None = None) -> Table:
    if widths is None:
        widths = [CONTENT_W / len(headers)] * len(headers)
    data = [[p(h, "tableHeader") for h in headers]]
    data += [[p(str(c), "tableCell") for c in row] for row in rows]
    tbl = Table(data, colWidths=widths, repeatRows=1)
    tbl.setStyle(
        TableStyle(
            [
                ("BACKGROUND", (0, 0), (-1, 0), BLUE),
                ("ROWBACKGROUNDS", (0, 1), (-1, -1), [colors.white, colors.HexColor("#FBFCFE")]),
                ("GRID", (0, 0), (-1, -1), 0.35, LINE),
                ("LEFTPADDING", (0, 0), (-1, -1), 5),
                ("RIGHTPADDING", (0, 0), (-1, -1), 5),
                ("TOPPADDING", (0, 0), (-1, -1), 5),
                ("BOTTOMPADDING", (0, 0), (-1, -1), 5),
                ("VALIGN", (0, 0), (-1, -1), "TOP"),
            ]
        )
    )
    return tbl


def flow(items: list[tuple[str, str]]) -> Table:
    cells = []
    widths = []
    each = (CONTENT_W - 24) / len(items)
    for idx, (title, body) in enumerate(items):
        block = Table([[p(title, "cardTitle")], [p(body, "small")]], colWidths=[each])
        block.setStyle(
            TableStyle(
                [
                    ("BACKGROUND", (0, 0), (-1, -1), SOFT_BLUE),
                    ("BOX", (0, 0), (-1, -1), 0.45, LINE),
                    ("LEFTPADDING", (0, 0), (-1, -1), 6),
                    ("RIGHTPADDING", (0, 0), (-1, -1), 6),
                    ("TOPPADDING", (0, 0), (-1, -1), 5),
                    ("BOTTOMPADDING", (0, 0), (-1, -1), 6),
                    ("VALIGN", (0, 0), (-1, -1), "TOP"),
                ]
            )
        )
        cells.append(block)
        widths.append(each)
        if idx < len(items) - 1:
            cells.append(p("→", "h2"))
            widths.append(6)
    tbl = Table([cells], colWidths=widths)
    tbl.setStyle(TableStyle([("VALIGN", (0, 0), (-1, -1), "MIDDLE"), ("LEFTPADDING", (0, 0), (-1, -1), 0), ("RIGHTPADDING", (0, 0), (-1, -1), 0)]))
    return tbl


def cover() -> list:
    story: list = []
    hero = Table(
        [
            [p("던전 한 끼", "smallWhite")],
            [p("NPC 기획\n설명 문서", "coverTitle")],
            [p("손님을 이해하고, 단서를 해석해, 따뜻한 한 끼를 맞춰주는 경험 설계", "coverSub")],
        ],
        colWidths=[CONTENT_W],
        rowHeights=[22, 92, 50],
    )
    hero.setStyle(
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
    story.append(hero)
    story.append(Spacer(1, 18))
    story.append(p("NPC는 주문 목록을 가진 캐릭터가 아니라, 플레이어가 세계와 사람을 이해하도록 만드는 작은 추론 문제다.", "quote"))
    story.append(Spacer(1, 16))
    story.append(
        table(
            ["이 문서의 목적", "다루는 내용", "다루지 않는 내용"],
            [
                [
                    "NPC 기획의 의도와 플레이어 경험 설명",
                    "손님 역할, 대화 흐름, 질문 대화, 기억/호감도 감각, 프로토타입 예시",
                    "클래스 구조, 데이터 타입, 구현 우선순위 같은 개발 세부 설계",
                ]
            ],
            widths=[CONTENT_W * 0.34, CONTENT_W * 0.34, CONTENT_W * 0.32],
        )
    )
    story.append(PageBreak())
    return story


def build_story() -> list:
    story = cover()

    story.append(p("1. NPC 기획의 핵심", "h1"))
    story.append(
        two_cards(
            card(
                "한 줄 정의",
                "던전 한 끼의 NPC는 손님이자 대화 상대다. 플레이어는 서로 주고받는 말 속에서 손님의 상태, 취향, 이전 기억을 읽고 그 사람에게 필요한 음식을 추론한다.",
                SOFT_GREEN,
                GREEN,
                CONTENT_W * 0.49,
            ),
            card(
                "플레이어에게 남겨야 할 감정",
                "정답을 외워 맞힌 느낌보다, 이 사람이 지금 원하는 것을 내가 이해했다는 감각이 중요하다.",
                SOFT_GOLD,
                GOLD,
                CONTENT_W * 0.49,
            ),
        )
    )
    story.append(Spacer(1, 9))
    story.append(
        table(
            ["기획 축", "의미", "NPC에서 드러나는 방식"],
            [
                ["추론", "메뉴명을 직접 말하지 않는 주문", "인사, 기억, 잡담, 추가 질문 속 단서로 후보를 좁힌다."],
                ["서사", "손님의 배경과 던전 이야기를 알아감", "직업, 종족, 출신, 이전 방문 기억이 대사에 묻어난다."],
                ["발견", "취향과 레시피를 플레이어가 배움", "반응을 통해 맞은 부분과 틀린 부분을 학습한다."],
                ["관계", "단골이 될수록 더 잘 이해하게 됨", "호감도가 오르면 질문과 사적인 단서가 열린다."],
                ["지역성", "지역마다 다른 손님이 찾아옴", "이끼 동굴, 광산로, 화산 지대마다 손님 유형이 달라진다."],
            ],
            widths=[CONTENT_W * 0.16, CONTENT_W * 0.32, CONTENT_W * 0.52],
        )
    )

    story.append(p("2. 플레이어 경험 흐름", "h1"))
    story.append(
        flow(
            [
                ("인사", "NPC와 플레이어가 서로 반응한다."),
                ("대화", "이전 기억이나 현재 상황이 오간다."),
                ("묻기", "선택한 카테고리가 짧은 질문 대화로 이어진다."),
                ("추론", "맛, 종류, 효과, 손질 방식을 고민한다."),
                ("기억", "반응, 호감도, 재방문으로 관계가 남는다."),
            ]
        )
    )
    story.append(Spacer(1, 9))
    story.append(
        card(
            "핵심 경험 문장",
            "플레이어는 손님의 정답 메뉴를 듣는 것이 아니라, 서로 대화하며 흘러나온 단서를 모아 오늘 필요한 한 끼를 맞춰준다.",
            SOFT_BLUE,
            BLUE,
        )
    )
    story.append(Spacer(1, 8))
    story.append(
        table(
            ["순간", "플레이어가 생각해야 하는 것", "좋은 NPC 대사의 조건"],
            [
                ["기본 대화", "이 손님과 지금 어떤 관계인가?", "인사, 기억, 반응이 오가며 주문으로 자연스럽게 이어진다."],
                ["추가 질문", "내가 모르는 정보는 무엇인가?", "질문 선택이 짧은 대화 턴으로 이어진다."],
                ["요리 선택", "음식 종류와 태그가 맞는가?", "정답을 직접 말하지 않는다."],
                ["결과 반응", "무엇이 맞고 틀렸나?", "성공/실패 이유를 배울 수 있다."],
                ["재방문", "이전 선택이 관계에 남았나?", "지난 요리와 감정을 기억한다."],
            ],
            widths=[CONTENT_W * 0.18, CONTENT_W * 0.38, CONTENT_W * 0.44],
        )
    )
    story.append(Spacer(1, 8))
    story.append(
        card(
            "대화형 Visit Event",
            "주문은 첫 줄에 바로 나오지 않아도 된다. 인사, 재회 반응, 이전 기억, 플레이어의 응답을 거친 뒤 마지막에 주문 의도가 자연스럽게 드러나는 편이 더 좋다.",
            SOFT_GREEN,
            GREEN,
        )
    )
    story.append(Spacer(1, 8))
    story.append(
        table(
            ["순서", "화자", "대사", "기획 역할"],
            [
                ["1", "요른", "안녕하신가, 주인장!", "재방문 인사"],
                ["2", "플레이어", "어? 요른 씨, 또 오셨네요.", "플레이어도 관계를 기억함"],
                ["3", "요른", "저번에 먹었던 마그마 큐브 음식을 잊지 못하겠어서, 주변에 수소문해 다시 찾아왔다네.", "이전 요리 기억"],
                ["4", "플레이어", "그렇게 말해주시니 너무 기쁘네요. 이번에는 어떤 요리를 해드릴까요?", "주문으로 자연스럽게 연결"],
                ["5", "요른", "이번에도 저번처럼 매콤하고 뜨거운 음식이 당기는군.", "주문 의도와 추론 단서"],
            ],
            widths=[CONTENT_W * 0.10, CONTENT_W * 0.16, CONTENT_W * 0.48, CONTENT_W * 0.26],
        )
    )

    story.append(p("3. NPC가 맡는 역할", "h1"))
    story.append(
        table(
            ["역할", "설명", "예시"],
            [
                ["손님", "음식을 주문하고 결과에 반응한다.", "지친 초보 모험가가 달달한 음식을 찾는다."],
                ["단서 제공자", "정답을 직접 말하지 않고 메뉴 후보를 좁히는 정보를 준다.", "목이 마르고 씹기 귀찮다고 말한다."],
                ["세계관 창", "던전, 직업, 종족, 지역의 생활감을 보여준다.", "광산 정찰병이 으슬으슬한 몸 상태를 말한다."],
                ["관계 대상", "호감도와 기억을 통해 다시 만날 이유를 만든다.", "저번 음료가 생각나서 다시 찾아온다."],
                ["장기 보상", "단골이 되면 의뢰나 특별 이벤트로 확장된다.", "Level 5에서 소재 채취 의뢰가 열린다."],
            ],
            widths=[CONTENT_W * 0.18, CONTENT_W * 0.43, CONTENT_W * 0.39],
        )
    )
    story.append(Spacer(1, 9))
    story.append(
        two_cards(
            card(
                "좋은 NPC",
                "서로 주고받는 대화 속에서 성격과 관계가 보이고, 질문을 하면 요리 후보가 줄어들며, 결과 반응으로 플레이어가 배운다.",
                SOFT_GREEN,
                GREEN,
                CONTENT_W * 0.49,
            ),
            card(
                "피해야 할 NPC",
                "손님이 일방적으로 주문만 말하거나, 분위기 대사만 길고 어떤 음식을 원하는지 추론할 단서가 없다.",
                SOFT_GOLD,
                GOLD,
                CONTENT_W * 0.49,
            ),
        )
    )

    story.append(p("4. 질문 기획 톤", "h1"))
    story.append(
        card(
            "질문도 대화처럼",
            "질문은 정보 추출 버튼이 아니라 짧은 대화 턴이다. 플레이어가 카테고리를 고르면 질문, NPC의 반응, 실제 단서, 플레이어의 확인까지 자연스럽게 이어질 수 있다.",
            SOFT_BLUE,
            BLUE,
        )
    )
    story.append(Spacer(1, 8))
    story.append(
        table(
            ["카테고리", "질문 대화 예시", "주는 단서"],
            [
                ["맛", "플레이어: 최근에 좋아하시는 맛이 있으신가요?\n요른: 난 웬만한 음식은 다 잘 먹어서.\n요른: 아! 저번 마그마 큐브 음식의 단짠단짠이 예술이었지.\n플레이어: 단짠단짠... 넵, 조금만 기다려주세요!", "단짠, 마그마 큐브 기억, 이전 만족 경험"],
                ["온도/식감", "플레이어: 오늘도 뜨거운 쪽이 좋으세요?\n요른: 하! 속에서 불이 오르는 느낌이면 더 좋지.\n플레이어: 뜨겁고 힘 나는 쪽으로 준비해볼게요.", "뜨거움, 든든함"],
                ["몸 상태", "플레이어: 오늘도 많이 싸우고 오셨어요?\n요른: 팔이 좀 뻐근하군. 씹는 맛 있는 걸 먹으면 힘이 돌 것 같네.\n플레이어: 씹는 맛 있는 걸로요.", "씹는 맛, 회복, 든든함"],
                ["피하고 싶은 것", "플레이어: 오늘 피하고 싶은 건 있으세요?\n요른: 흐물흐물한 건 사양하겠네. 전사 음식은 이를 써야지!\n플레이어: 흐물한 식감은 빼둘게요.", "흐물흐물함 금지, 식감 중요"],
            ],
            widths=[CONTENT_W * 0.16, CONTENT_W * 0.58, CONTENT_W * 0.26],
        )
    )
    story.append(Spacer(1, 8))
    story.append(
        table(
            ["나쁜 질문", "왜 문제인가", "수정 방향"],
            [
                ["어디가 제일 지치셨나요? 몸이 무거운 쪽인가요, 기운이 빠진 쪽인가요?", "질문이 길고 상담처럼 느껴진다.", "많이 지치셨어요?"],
                ["혹시 허브 슬라임 스무디가 먹고 싶나요?", "정답을 직접 말한다.", "시원한 게 좋아요?"],
                ["정확히 어떤 요리를 원하시나요?", "추론 게임을 무너뜨린다.", "부드러운 쪽이 좋아요?"],
            ],
            widths=[CONTENT_W * 0.42, CONTENT_W * 0.30, CONTENT_W * 0.28],
        )
    )

    story.append(PageBreak())
    story.append(p("5. 기억과 호감도 기획", "h1"))
    story.append(
        two_cards(
            card(
                "호감도의 의미",
                "호감도는 단순 보상 수치가 아니라 손님이 더 솔직해지는 정도다. 친해질수록 더 많이, 더 깊게 물어볼 수 있다.",
                SOFT_GREEN,
                GREEN,
                CONTENT_W * 0.49,
            ),
            card(
                "재방문의 의미",
                "재방문은 플레이어의 선택이 세계에 남았다는 증거다. 손님이 이전 음식을 기억하면 관계가 실제처럼 느껴진다.",
                SOFT_GOLD,
                GOLD,
                CONTENT_W * 0.49,
            ),
        )
    )
    story.append(Spacer(1, 8))
    story.append(
        table(
            ["관계 단계", "플레이어가 체감할 변화", "기획 의도"],
            [
                ["처음 만남", "질문 1회, 기본 카테고리만 가능", "첫 대사와 핵심 질문만으로 유사 정답까지 도달 가능해야 한다."],
                ["안면 있음", "질문 2회, 피하고 싶은 것 해금", "친해졌기 때문에 더 편하게 물어볼 수 있다."],
                ["단골 단계", "이전 방문 기억, 사적인 단서, 특별 의뢰", "관계가 게임 진행과 재료 수급으로 이어진다."],
            ],
            widths=[CONTENT_W * 0.20, CONTENT_W * 0.42, CONTENT_W * 0.38],
        )
    )
    story.append(Spacer(1, 9))
    story.append(
        card(
            "재방문 기준",
            "프로토타입에서는 유사 정답 이상이면 재방문 기억 이벤트가 발생할 수 있다. 완벽히 맞히지 않아도 손님을 어느 정도 이해했다는 경험을 살리기 위해서다.",
            SOFT_BLUE,
            BLUE,
        )
    )

    story.append(p("6. 예시: 지친 초보 모험가", "h1"))
    story.append(
        flow(
            [
                ("첫 대사", "오늘은 너무 지쳤어. 달달한 게 먹고 싶어."),
                ("질문", "많이 지치셨어요? / 시원한 게 좋아요?"),
                ("단서", "목마름, 씹기 귀찮음, 산뜻한 단맛"),
                ("정답", "허브 슬라임 스무디"),
                ("기억", "저번에 마신 달달한 음료가 생각나서 또 왔어."),
            ]
        )
    )
    story.append(Spacer(1, 9))
    story.append(
        table(
            ["대사/반응", "플레이어가 얻는 정보"],
            [
                ["오늘은 너무 지쳤어. 달달한 게 먹고 싶어.", "달콤함, 회복 욕구"],
                ["응. 목이 좀 말라.", "음료 계열, 차가움 가능성"],
                ["완전 녹초야. 씹는 것도 귀찮아.", "부드러움, 가벼움"],
                ["아, 이거야. 기운이 좀 돌아오는 것 같아.", "정답에 가까웠다는 피드백"],
                ["달긴 한데... 내가 원한 느낌은 아니었어.", "맛은 맞지만 종류/식감이 빗나갔다는 학습"],
            ],
            widths=[CONTENT_W * 0.50, CONTENT_W * 0.50],
        )
    )

    story.append(p("7. 프로토타입 NPC 3명의 기획 역할", "h1"))
    story.append(
        table(
            ["NPC", "주문 느낌", "학습시키는 추론", "정답"],
            [
                ["지친 초보 모험가", "달달하고 지친 몸에 부담 없는 것", "맛 + 몸 상태 + 식감", "허브 슬라임 스무디"],
                ["광산길 정찰병", "으슬으슬하지만 속이 부담스럽지 않은 것", "온도 + 가벼움 + 맑음", "발광 버섯 맑은 수프"],
                ["겁 많은 마법 견습생", "마력 회복은 필요하지만 쓴 건 싫음", "효과 태그 + 맛 금지", "동굴 꿀 허브차"],
            ],
            widths=[CONTENT_W * 0.20, CONTENT_W * 0.35, CONTENT_W * 0.25, CONTENT_W * 0.20],
        )
    )
    story.append(Spacer(1, 8))
    story.append(
        card(
            "프로토타입에서 검증할 것",
            "손님 대사만으로 후보를 떠올릴 수 있는가, 추가 질문을 누르고 싶어지는가, 정답이 아니어도 유사 정답 판정이 납득되는가, 재방문 기억이 관계처럼 느껴지는가.",
            SOFT_GOLD,
            GOLD,
        )
    )

    story.append(p("8. 콘텐츠 작성 체크리스트", "h1"))
    story.append(
        table(
            ["체크 항목", "확인 질문"],
            [
                ["첫 대사", "상태, 취향, 원하는 효과 중 최소 1개 이상이 드러나는가?"],
                ["질문 대화", "카테고리 선택 후 질문, NPC 반응, 단서, 플레이어 확인이 자연스럽게 이어지는가?"],
                ["정답 은닉", "메뉴명이나 레시피를 직접 말하지 않는가?"],
                ["유사 정답", "플레이어가 합리적으로 추론한 대체 메뉴를 인정하는가?"],
                ["반응", "성공/실패 이유를 다음 시도에 활용할 수 있는가?"],
                ["기억", "이전 결과가 다음 만남의 대사나 태도에 남는가?"],
                ["톤", "대화가 너무 주문 접수처럼 느껴지지 않고 서로 말하는 느낌이 나는가?"],
            ],
            widths=[CONTENT_W * 0.25, CONTENT_W * 0.75],
        )
    )
    story.append(Spacer(1, 10))
    story.append(
        card(
            "최종 기준",
            "NPC 하나를 만들 때 목표는 '정답을 맞히게 만드는 것'이 아니라 '이 손님을 이해하게 만드는 것'이다.",
            SOFT_GREEN,
            GREEN,
        )
    )

    return story


def page_frame(canvas, doc):
    canvas.saveState()
    canvas.setFillColor(NAVY)
    canvas.rect(0, PAGE_H - 10 * mm, PAGE_W, 10 * mm, stroke=0, fill=1)
    canvas.setFillColor(colors.white)
    canvas.setFont(BOLD_FONT, 8)
    canvas.drawString(MARGIN_X, PAGE_H - 6.5 * mm, "Dungeon Dinner / NPC Design Explanation")
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
        title="NPC Design Explanation",
        author="Dungeon Dinner",
    )
    doc.build(build_story(), onFirstPage=page_frame, onLaterPages=page_frame)
    print(OUTPUT)


if __name__ == "__main__":
    main()
