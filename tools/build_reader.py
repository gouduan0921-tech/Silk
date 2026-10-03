#!/usr/bin/env python3
"""把 docs/*.md 生成单文件阅读器《华裳从蚕到神文档.html》。"""

from __future__ import annotations

import html
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DOCS = ROOT / "docs"
OUT = ROOT / "华裳从蚕到神文档.html"
STYLE_SOURCE = Path("/Users/liweng/Downloads/3D/LuoJi/丝瓜络工坊文档.html")

INLINE = re.compile(
    r"(\*\*[^*]+\*\*|`[^`]+`|\[[^\]]+\]\([^)]+\))"
)


def inline(text: str) -> str:
    parts = []
    pos = 0
    for match in INLINE.finditer(text):
        parts.append(html.escape(text[pos:match.start()]))
        token = match.group(0)
        if token.startswith("**"):
            parts.append("<strong>" + inline(token[2:-2]) + "</strong>")
        elif token.startswith("`"):
            parts.append("<code>" + html.escape(token[1:-1]) + "</code>")
        else:
            label, href = token[1:-1].split("](", 1)
            href = href.strip()
            if href.endswith(".md"):
                anchor = href.split("/")[-1][:-3]
                parts.append(f'<a href="#{html.escape(anchor)}">{inline(label)}</a>')
            else:
                parts.append(
                    f'<a href="{html.escape(href, quote=True)}">{inline(label)}</a>'
                )
        pos = match.end()
    parts.append(html.escape(text[pos:]))
    return "".join(parts)


def is_table_sep(line: str) -> bool:
    cells = [cell.strip() for cell in line.strip().strip("|").split("|")]
    return bool(cells) and all(re.fullmatch(r":?-{3,}:?", cell) for cell in cells)


def table_html(lines: list[str]) -> str:
    rows = []
    for line in lines:
        cells = [cell.strip() for cell in line.strip().strip("|").split("|")]
        rows.append(cells)
    head, body = rows[0], rows[2:]
    thead = "".join(f"<th scope=\"col\">{inline(cell)}</th>" for cell in head)
    body_html = []
    for row in body:
        body_html.append(
            "<tr>" + "".join(f"<td>{inline(cell)}</td>" for cell in row) + "</tr>"
        )
    return (
        '<div class="table-wrap" tabindex="0" aria-label="可横向滚动的表格"><table><thead><tr>'
        + thead
        + "</tr></thead><tbody>"
        + "".join(body_html)
        + "</tbody></table></div>"
    )


def blocks(md: str) -> list[str]:
    lines = md.replace("\r\n", "\n").split("\n")
    out: list[str] = []
    i = 0
    paragraph: list[tuple[str, bool]] = []

    def flush_paragraph() -> None:
        if paragraph:
            rendered = []
            for index, (text, hard_break) in enumerate(paragraph):
                rendered.append(inline(text))
                if index != len(paragraph) - 1:
                    rendered.append("<br>" if hard_break else " ")
            out.append("<p>" + "".join(rendered) + "</p>")
            paragraph.clear()

    while i < len(lines):
        line = lines[i]
        stripped = line.strip()
        if not stripped:
            flush_paragraph()
            i += 1
            continue
        if stripped.startswith("```"):
            flush_paragraph()
            lang = stripped[3:].strip() or "text"
            i += 1
            code = []
            while i < len(lines) and not lines[i].strip().startswith("```"):
                code.append(lines[i])
                i += 1
            i += 1
            out.append(
                '<div class="code-wrap"><div class="code-label">'
                + html.escape(lang.upper())
                + "</div><pre><code>"
                + html.escape("\n".join(code))
                + "</code></pre></div>"
            )
            continue
        if stripped.startswith("|") and i + 1 < len(lines) and is_table_sep(lines[i + 1]):
            flush_paragraph()
            table_lines = [stripped]
            i += 1
            table_lines.append(lines[i].strip())
            i += 1
            while i < len(lines) and lines[i].strip().startswith("|"):
                table_lines.append(lines[i].strip())
                i += 1
            out.append(table_html(table_lines))
            continue
        heading = re.match(r"^(#{1,4})\s+(.*)$", stripped)
        if heading:
            flush_paragraph()
            level = len(heading.group(1))
            tag = {1: "h2", 2: "h3", 3: "h4", 4: "h5"}[level]
            out.append(f"<{tag}>{inline(heading.group(2))}</{tag}>")
            i += 1
            continue
        if stripped == "---":
            flush_paragraph()
            out.append("<hr>")
            i += 1
            continue
        if re.match(r"^[-*] ", stripped):
            flush_paragraph()
            items = []
            while i < len(lines) and re.match(r"^[-*] ", lines[i].strip()):
                items.append(re.sub(r"^[-*] ", "", lines[i].strip()))
                i += 1
            out.append("<ul>" + "".join(f"<li>{inline(item)}</li>" for item in items) + "</ul>")
            continue
        if re.match(r"^\d+\. ", stripped):
            flush_paragraph()
            items = []
            while i < len(lines) and re.match(r"^\d+\. ", lines[i].strip()):
                items.append(re.sub(r"^\d+\. ", "", lines[i].strip()))
                i += 1
            out.append("<ol>" + "".join(f"<li>{inline(item)}</li>" for item in items) + "</ol>")
            continue
        hard_break = line.endswith("  ") or line.rstrip().endswith("\\")
        paragraph.append((stripped, hard_break))
        i += 1
    flush_paragraph()
    return out


def chapter(path: Path) -> tuple[str, str, str, str]:
    text = path.read_text(encoding="utf-8")
    title_match = re.search(r"^#\s+(.+)$", text, re.M)
    title = title_match.group(1).strip() if title_match else path.stem
    number = path.stem.split("_", 1)[0]
    nav = re.sub(r"^《华裳·从蚕到神》", "", title).strip() or title
    body = "\n".join(blocks(text))
    # 章节标题已由外壳的 h2 承担，去掉正文第一张 h2。
    body = re.sub(r"^<h2>.*?</h2>\s*", "", body, count=1)
    section = (
        f'<section id="{html.escape(path.stem)}" class="chapter" '
        f'data-title="{html.escape(title)}" tabindex="-1">'
        f'<div class="chapter-kicker">文档 {html.escape(number)}</div>'
        f"<h2>{inline(title)}</h2>{body}</section>"
    )
    link = (
        f'<a class="nav-link" href="#{html.escape(path.stem)}">'
        f'<span class="nav-number">{html.escape(number)}</span>'
        f"<span>{inline(nav)}</span></a>"
    )
    return number, nav, link, section


def retheme(style: str) -> str:
    style = re.sub(
        r"--forest-950:\s*#[0-9a-fA-F]+;",
        "--forest-950: #3a2418;",
        style,
        count=1,
    )
    replacements = {
        "--forest-900: #1e3f2e;": "--forest-900: #4a2c24;",
        "--forest-800: #28513a;": "--forest-800: #6b3a32;",
        "--forest-700: #356748;": "--forest-700: #8c4a3a;",
        "--forest-600: #4a7c59;": "--forest-600: #a85a42;",
        "--leaf-300: #a9c39c;": "--leaf-300: #e4c99a;",
        "--leaf-100: #e4ecdc;": "--leaf-100: #f3e6d4;",
        "--fiber-100: #f4ecd9;": "--fiber-100: #f6efe2;",
        "--fiber-200: #e7d9bd;": "--fiber-200: #eadcc4;",
        "--fiber-300: #d2bd95;": "--fiber-300: #d2b48c;",
        "--ink: #24352b;": "--ink: #2c2118;",
        "--muted: #657167;": "--muted: #7a6a5c;",
        "--accent: #b07836;": "--accent: #9a6232;",
        "#234b36": "#4a2c24",
        "rgba(169, 195, 156, .3)": "rgba(212, 176, 122, .35)",
        "rgba(28, 53, 38, .11)": "rgba(58, 36, 24, .12)",
        "rgba(18, 39, 27, .1)": "rgba(48, 28, 20, .12)",
        "#254934": "#5c3a32",
        "#edf2e8": "#f3ebe3",
        "#e9efe5": "#f6efe6",
        "#dfe9da": "#efe4d6",
        "#c9d6c5": "#e0d0bc",
        "#20392b": "#3a2a22",
        "#e7ede1": "#f0e4d4",
        "#45584a": "#6a5348",
        "rgba(74,124,89,.16)": "rgba(168,90,66,.18)",
        "rgba(43,63,48,.08)": "rgba(74,44,36,.08)",
        "rgba(23,51,38,.98)": "rgba(58,36,24,.98)",
        "rgba(45,92,63,.94)": "rgba(107,58,50,.94)",
        "rgba(23,51,38,.2)": "rgba(58,36,24,.2)",
        "rgba(10,25,16,.48)": "rgba(28,16,12,.48)",
        "rgba(35,75,54,.22)": "rgba(90,52,40,.28)",
        "rgba(35,75,54,.12)": "rgba(90,52,40,.16)",
    }
    for old, new in replacements.items():
        style = style.replace(old, new)
    return style


def main() -> None:
    source = STYLE_SOURCE.read_text(encoding="utf-8")
    style = re.search(r"<style>(.*)</style>", source, re.S).group(1)
    script = re.search(r"<script>\s*(\(\(\) => \{.*?\}\)\(\);)\s*</script>", source, re.S).group(1)
    style = retheme(style)
    files = sorted(DOCS.glob("[0-9][0-9]_*.md"))
    chapters = [chapter(path) for path in files]
    nav = "".join(item[2] for item in chapters)
    body = "".join(item[3] for item in chapters)
    page = f"""<!doctype html>
<html lang="zh-CN">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <meta name="color-scheme" content="light">
  <meta name="theme-color" content="#4a2c24">
  <title>华裳·从蚕到神 · 设计文档</title>
  <style>{style}</style>
</head>
<body>
  <a class="skip-link" href="#reader">跳到正文</a>
  <div class="mobile-bar">
    <strong>华裳·从蚕到神 · 设计文档</strong>
    <button class="mobile-nav-button" id="mobile-nav-button" type="button"
      aria-controls="sidebar" aria-expanded="false">章节</button>
  </div>
  <div class="nav-scrim" id="nav-scrim" aria-hidden="true"></div>
  <aside class="sidebar" id="sidebar" aria-label="文档章节">
    <div class="brand-block">
      <div class="brand-mark">
        <div class="brand-icon" aria-hidden="true">裳</div>
        <div>
          <div class="brand-name">华裳</div>
          <p class="brand-subtitle">SILK · 从蚕茧到展柜</p>
        </div>
      </div>
    </div>
    <div class="search-box">
      <label for="search">中文全文搜索</label>
      <div class="search-field">
        <input id="search" type="search" autocomplete="off"
          placeholder="输入关键词…" aria-describedby="search-status">
      </div>
      <div id="search-status" role="status" aria-live="polite"></div>
    </div>
    <nav class="chapter-nav" aria-label="章节导航">{nav}</nav>
  </aside>
  <main class="page" id="reader">
    <div class="page-inner">
      <header class="hero">
        <div class="eyebrow">HUASHANG · PRODUCTION HANDBOOK</div>
        <h1>从一缕丝，到能穿上舞台的衣服。</h1>
        <p>《华裳·从蚕到神》设计文档离线阅读器。查阅玩法边界、工序、数值、布料仿真、演出、验收和开发顺序。</p>
        <div class="hero-meta">
          <span class="pill">单文件离线阅读</span>
          <span class="pill">全文搜索</span>
          <span class="pill">{len(chapters)} 个章节</span>
          <span class="pill">适合打印与另存 PDF</span>
        </div>
      </header>
      <div class="toolbar" aria-label="阅读工具">
        <button id="previous" type="button" aria-label="上一章节">← 上一章</button>
        <button id="next" type="button" aria-label="下一章节">下一章 →</button>
        <button id="show-all" type="button" aria-pressed="false">展开全文</button>
        <button class="print-button" id="print" type="button">打印 / 另存 PDF</button>
      </div>
      <div id="chapters">{body}</div>
      <div id="no-results"><strong>没有匹配的章节</strong>换一个词，或清空搜索。</div>
      <footer>由 docs 目录生成。改 Markdown 后运行 tools/build_reader.py。</footer>
    </div>
  </main>
  <script>
    {script}
  </script>
</body>
</html>
"""
    OUT.write_text(page, encoding="utf-8")
    print(f"wrote {OUT} chapters={len(chapters)} bytes={OUT.stat().st_size}")


if __name__ == "__main__":
    main()
