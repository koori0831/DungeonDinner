from __future__ import annotations

import html
import re
from pathlib import Path

from reportlab.lib import colors
from reportlab.lib.enums import TA_CENTER, TA_LEFT
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle, getSampleStyleSheet
from reportlab.lib.units import mm
from reportlab.platypus import (
    PageBreak,
    Paragraph,
    Preformatted,
    SimpleDocTemplate,
    Spacer,
    Table,
    TableStyle,
)
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont


ROOT = Path(__file__).resolve().parent
INPUT = ROOT / "NPC_System_Planning.md"
OUTPUT = ROOT / "outputs" / "NPC_System_Planning.pdf"


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


def make_styles() -> dict[str, ParagraphStyle]:
    base = getSampleStyleSheet()
    return {
        "title": ParagraphStyle(
            "TitleKo",
            parent=base["Title"],
            fontName=BOLD_FONT,
            fontSize=22,
            leading=28,
            alignment=TA_CENTER,
            spaceAfter=14,
            textColor=colors.HexColor("#182230"),
        ),
        "h1": ParagraphStyle(
            "H1Ko",
            parent=base["Heading1"],
            fontName=BOLD_FONT,
            fontSize=16,
            leading=21,
            spaceBefore=14,
            spaceAfter=8,
            textColor=colors.HexColor("#1F3A5F"),
        ),
        "h2": ParagraphStyle(
            "H2Ko",
            parent=base["Heading2"],
            fontName=BOLD_FONT,
            fontSize=13,
            leading=18,
            spaceBefore=10,
            spaceAfter=6,
            textColor=colors.HexColor("#27364A"),
        ),
        "body": ParagraphStyle(
            "BodyKo",
            parent=base["BodyText"],
            fontName=BODY_FONT,
            fontSize=9.5,
            leading=15,
            alignment=TA_LEFT,
            spaceAfter=6,
            wordWrap="CJK",
        ),
        "bullet": ParagraphStyle(
            "BulletKo",
            parent=base["BodyText"],
            fontName=BODY_FONT,
            fontSize=9.5,
            leading=15,
            leftIndent=12,
            firstLineIndent=-8,
            spaceAfter=4,
            wordWrap="CJK",
        ),
        "code": ParagraphStyle(
            "CodeKo",
            parent=base["Code"],
            fontName=BODY_FONT,
            fontSize=8.2,
            leading=12,
            leftIndent=4,
            rightIndent=4,
            backColor=colors.HexColor("#F4F6F8"),
            borderColor=colors.HexColor("#D6DEE8"),
            borderWidth=0.4,
            borderPadding=5,
            spaceBefore=4,
            spaceAfter=7,
            wordWrap="CJK",
        ),
        "table": ParagraphStyle(
            "TableKo",
            parent=base["BodyText"],
            fontName=BODY_FONT,
            fontSize=7.4,
            leading=10,
            wordWrap="CJK",
        ),
        "tableHeader": ParagraphStyle(
            "TableHeaderKo",
            parent=base["BodyText"],
            fontName=BOLD_FONT,
            fontSize=7.5,
            leading=10,
            textColor=colors.white,
            wordWrap="CJK",
        ),
    }


STYLES = make_styles()


def escape_inline(text: str) -> str:
    text = html.escape(text)
    text = re.sub(r"`([^`]+)`", r"<font color='#8A3FFC'>\1</font>", text)
    text = re.sub(r"\*\*([^*]+)\*\*", r"<b>\1</b>", text)
    return text


def flush_paragraph(buffer: list[str], story: list) -> None:
    if not buffer:
        return
    text = " ".join(line.strip() for line in buffer if line.strip())
    if text:
        story.append(Paragraph(escape_inline(text), STYLES["body"]))
    buffer.clear()


def parse_table(lines: list[str]) -> list[list[str]]:
    rows: list[list[str]] = []
    for line in lines:
        stripped = line.strip()
        if re.fullmatch(r"\|?\s*:?-{3,}:?\s*(\|\s*:?-{3,}:?\s*)+\|?", stripped):
            continue
        cells = [cell.strip() for cell in stripped.strip("|").split("|")]
        rows.append(cells)
    return rows


def add_table(table_lines: list[str], story: list) -> None:
    rows = parse_table(table_lines)
    if not rows:
        return

    col_count = max(len(row) for row in rows)
    normalized = [row + [""] * (col_count - len(row)) for row in rows]
    data = []
    for r, row in enumerate(normalized):
        style = STYLES["tableHeader"] if r == 0 else STYLES["table"]
        data.append([Paragraph(escape_inline(cell), style) for cell in row])

    page_width = A4[0] - 36 * mm
    col_width = page_width / col_count
    table = Table(data, colWidths=[col_width] * col_count, hAlign="LEFT", repeatRows=1)
    table.setStyle(
        TableStyle(
            [
                ("BACKGROUND", (0, 0), (-1, 0), colors.HexColor("#27364A")),
                ("GRID", (0, 0), (-1, -1), 0.35, colors.HexColor("#CAD3DF")),
                ("VALIGN", (0, 0), (-1, -1), "TOP"),
                ("LEFTPADDING", (0, 0), (-1, -1), 4),
                ("RIGHTPADDING", (0, 0), (-1, -1), 4),
                ("TOPPADDING", (0, 0), (-1, -1), 4),
                ("BOTTOMPADDING", (0, 0), (-1, -1), 4),
                ("BACKGROUND", (0, 1), (-1, -1), colors.HexColor("#FBFCFE")),
            ]
        )
    )
    story.append(table)
    story.append(Spacer(1, 6))


def build_story(markdown: str) -> list:
    story: list = []
    paragraph_buffer: list[str] = []
    code_buffer: list[str] = []
    table_buffer: list[str] = []
    in_code = False

    lines = markdown.splitlines()
    for line in lines:
        stripped = line.strip()

        if stripped.startswith("```"):
            flush_paragraph(paragraph_buffer, story)
            if table_buffer:
                add_table(table_buffer, story)
                table_buffer.clear()
            if in_code:
                story.append(Preformatted("\n".join(code_buffer), STYLES["code"]))
                code_buffer.clear()
                in_code = False
            else:
                in_code = True
            continue

        if in_code:
            code_buffer.append(line)
            continue

        is_table_line = stripped.startswith("|") and stripped.endswith("|")
        if is_table_line:
            flush_paragraph(paragraph_buffer, story)
            table_buffer.append(line)
            continue
        if table_buffer:
            add_table(table_buffer, story)
            table_buffer.clear()

        if not stripped:
            flush_paragraph(paragraph_buffer, story)
            continue

        if stripped.startswith("# "):
            flush_paragraph(paragraph_buffer, story)
            story.append(Paragraph(escape_inline(stripped[2:].strip()), STYLES["title"]))
            story.append(Spacer(1, 4))
            continue

        if stripped.startswith("## "):
            flush_paragraph(paragraph_buffer, story)
            story.append(Paragraph(escape_inline(stripped[3:].strip()), STYLES["h1"]))
            continue

        if stripped.startswith("### "):
            flush_paragraph(paragraph_buffer, story)
            story.append(Paragraph(escape_inline(stripped[4:].strip()), STYLES["h2"]))
            continue

        if stripped == "---":
            flush_paragraph(paragraph_buffer, story)
            story.append(PageBreak())
            continue

        if stripped.startswith("- "):
            flush_paragraph(paragraph_buffer, story)
            story.append(Paragraph("- " + escape_inline(stripped[2:].strip()), STYLES["bullet"]))
            continue

        numbered = re.match(r"^(\d+)\.\s+(.*)$", stripped)
        if numbered:
            flush_paragraph(paragraph_buffer, story)
            story.append(
                Paragraph(
                    f"{numbered.group(1)}. {escape_inline(numbered.group(2))}",
                    STYLES["bullet"],
                )
            )
            continue

        paragraph_buffer.append(line)

    flush_paragraph(paragraph_buffer, story)
    if table_buffer:
        add_table(table_buffer, story)
    if code_buffer:
        story.append(Preformatted("\n".join(code_buffer), STYLES["code"]))

    return story


def add_page_number(canvas, doc) -> None:
    canvas.saveState()
    canvas.setFont(BODY_FONT, 8)
    canvas.setFillColor(colors.HexColor("#667085"))
    canvas.drawRightString(A4[0] - 18 * mm, 12 * mm, f"{doc.page}")
    canvas.restoreState()


def main() -> None:
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    markdown = INPUT.read_text(encoding="utf-8")
    story = build_story(markdown)
    doc = SimpleDocTemplate(
        str(OUTPUT),
        pagesize=A4,
        rightMargin=18 * mm,
        leftMargin=18 * mm,
        topMargin=18 * mm,
        bottomMargin=18 * mm,
        title="NPC System Planning",
        author="Dungeon Dinner",
    )
    doc.build(story, onFirstPage=add_page_number, onLaterPages=add_page_number)
    print(OUTPUT)


if __name__ == "__main__":
    main()
