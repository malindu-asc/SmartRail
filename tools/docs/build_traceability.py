#!/usr/bin/env python3
"""Rebuild docs/00-requirements/11-scope/traceability.md from the markdown docs and the code.

The requirements list is read from 06-requirements/requirements.md. Every other file is scanned
for REQ-<AREA>-nnn references (ranges like REQ-DRV-005…009 are expanded) and attributed to:

    stories      10-user-stories/stories.md        per "### US-nnn" section
    use cases    07-use-cases/*.md                 per "### UC-..." section
    flows        08-cross-app-flows/cf-nn-*.md     per file
    edge cases   09-edge-cases/catalogue.md        per "| EDG-nnn |" row
    tests        backend/ web/ mobile/ external/   any source file mentioning the id

Run from anywhere:  python tools/docs/build_traceability.py
Zero dependencies.
"""
from __future__ import annotations

import re
import sys
from collections import defaultdict
from datetime import date
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
REQ_DIR = ROOT / "docs" / "00-requirements"
REQ_FILE = REQ_DIR / "06-requirements" / "requirements.md"
OUT = REQ_DIR / "11-scope" / "traceability.md"
CODE_DIRS = ["backend", "web", "mobile", "external"]
CODE_EXT = {".cs", ".ts", ".tsx", ".js", ".mjs", ".dart", ".py", ".feature"}

REQ_RE = re.compile(r"REQ-([A-Z]+)-(\d{3})(?:\s*(?:…|\.\.\.)\s*(\d{3}))?")
HEAD_RE = re.compile(r"^####\s+(REQ-[A-Z]+-\d{3})\s+—\s+(.+)$")
META_RE = re.compile(r"\*\*Priority:\*\*\s*(\w+).*?\*\*MVP:\*\*\s*(\w+).*?\*\*Status:\*\*\s*(\w+)")


def refs(text: str) -> set[str]:
    out = set()
    for area, start, end in REQ_RE.findall(text):
        a = int(start)
        b = int(end) if end else a
        for n in range(a, b + 1):
            out.add(f"REQ-{area}-{n:03d}")
    return out


def load_requirements() -> dict[str, dict]:
    reqs: dict[str, dict] = {}
    current = None
    for line in REQ_FILE.read_text(encoding="utf-8").splitlines():
        m = HEAD_RE.match(line)
        if m:
            current = m.group(1)
            reqs[current] = {"title": m.group(2).strip(), "prio": "?", "mvp": "?", "status": "?"}
            continue
        if current and "**Priority:**" in line:
            mm = META_RE.search(line)
            if mm:
                reqs[current].update(prio=mm.group(1), mvp=mm.group(2), status=mm.group(3))
    if not reqs:
        sys.exit(f"No requirements parsed from {REQ_FILE}")
    return reqs


def sections(text: str, heading: re.Pattern) -> list[tuple[str, str]]:
    """Split text into (id, body) on headings matching `heading` (group 1 = id)."""
    out, cur_id, buf = [], None, []
    for line in text.splitlines():
        m = heading.match(line)
        if m:
            if cur_id:
                out.append((cur_id, "\n".join(buf)))
            cur_id, buf = m.group(1), []
        elif cur_id:
            buf.append(line)
    if cur_id:
        out.append((cur_id, "\n".join(buf)))
    return out


def main() -> None:
    reqs = load_requirements()
    link = {k: defaultdict(set) for k in ("stories", "usecases", "flows", "edges", "specs", "tests")}

    stories = REQ_DIR / "10-user-stories" / "stories.md"
    if stories.exists():
        for sid, body in sections(stories.read_text(encoding="utf-8"), re.compile(r"^###\s+(US-\d{3})")):
            for r in refs(body):
                link["stories"][r].add(sid)

    for f in sorted((REQ_DIR / "07-use-cases").glob("*.md")):
        for uid, body in sections(f.read_text(encoding="utf-8"), re.compile(r"^###\s+(UC-[A-Z]+-\d{2})")):
            for r in refs(body):
                link["usecases"][r].add(uid)

    for f in sorted((REQ_DIR / "08-cross-app-flows").glob("cf-*.md")):
        cf = "CF-" + f.name.split("-")[1]
        for r in refs(f.read_text(encoding="utf-8")):
            link["flows"][r].add(cf)

    # specification documents: domain model, matrices, and design docs in later phases
    spec_files = list((REQ_DIR / "04-domain").glob("*.md"))
    spec_files += [f for f in (REQ_DIR / "06-requirements").glob("*.md") if f.name != "requirements.md"]
    for sub in ("01-ux", "02-architecture", "03-qa", "04-security", "06-operations"):
        d = ROOT / "docs" / sub
        if d.exists():
            spec_files += list(d.rglob("*.md"))
    for f in spec_files:
        name = f.relative_to(ROOT / "docs").as_posix()
        for r in refs(f.read_text(encoding="utf-8")):
            link["specs"][r].add(name)

    cat = REQ_DIR / "09-edge-cases" / "catalogue.md"
    if cat.exists():
        for line in cat.read_text(encoding="utf-8").splitlines():
            m = re.match(r"^\|\s*(EDG-\d{3})\s*\|", line)
            if m:
                for r in refs(line):
                    link["edges"][r].add(m.group(1))

    for d in CODE_DIRS:
        base = ROOT / d
        if not base.exists():
            continue
        for f in base.rglob("*"):
            if f.suffix in CODE_EXT and f.is_file() and "node_modules" not in f.parts:
                try:
                    txt = f.read_text(encoding="utf-8", errors="ignore")
                except OSError:
                    continue
                for r in refs(txt):
                    link["tests"][r].add(str(f.relative_to(ROOT)).replace("\\", "/"))

    unknown = sorted({r for kind in link.values() for r in kind} - set(reqs))

    def fmt(s: set[str], limit: int = 6) -> str:
        items = sorted(s)
        if not items:
            return "—"
        more = f" +{len(items) - limit}" if len(items) > limit else ""
        return ", ".join(items[:limit]) + more

    must = [r for r, v in reqs.items() if v["prio"] == "Must"]
    no_story = [r for r in must if not link["stories"][r]]
    no_uc = [r for r in must if not link["usecases"][r] and not link["flows"][r] and not link["specs"][r]]
    no_test = [r for r in must if not link["tests"][r]]

    L = [
        "# Traceability",
        "",
        f"> **Generated {date.today().isoformat()}** by `tools/docs/build_traceability.py` — do not edit by hand. "
        "Re-run after changing any requirement, story, use case, flow, edge case or test.",
        "",
        "Tests are linked by mentioning the requirement id in the test (name, attribute or comment), "
        "e.g. `[Trait(\"Req\", \"REQ-LIVE-004\")]` in xUnit or `test('REQ-PAX-002 board shows delta', …)` "
        "in Playwright / Flutter.",
        "",
        "## Coverage",
        "",
        "| Measure | Count |",
        "|---|---|",
        f"| Requirements | {len(reqs)} |",
        f"| Must | {len(must)} |",
        f"| Must with at least one story | {len(must) - len(no_story)} |",
        f"| Must with a use case, flow or specification document | {len(must) - len(no_uc)} |",
        f"| Must with at least one test | {len(must) - len(no_test)} |",
        f"| All requirements with at least one test | {sum(1 for r in reqs if link['tests'][r])} |",
        "",
    ]
    if unknown:
        L += ["## ⚠ References to requirement ids that do not exist", "",
              "Fix the reference or add the requirement:", "", ", ".join(unknown), ""]
    L += ["## Must requirements without a story", "", ", ".join(no_story) or "None ✅", "",
          "## Must requirements without a use case, flow or specification", "", ", ".join(no_uc) or "None ✅", "",
          "## Must requirements without a test", "",
          (", ".join(no_test) or "None ✅") + (" *(expected before the build starts)*" if len(no_test) == len(must) else ""),
          "",
          "## Matrix", "",
          "| Requirement | Title | Pri | MVP | Status | Stories | Use cases | Flows | Specs | Edge cases | Tests |",
          "|---|---|---|---|---|---|---|---|---|---|---|"]
    for r, v in reqs.items():
        L.append(f"| {r} | {v['title']} | {v['prio']} | {v['mvp']} | {v['status']} | "
                 f"{fmt(link['stories'][r])} | {fmt(link['usecases'][r])} | {fmt(link['flows'][r])} | "
                 f"{fmt(link['specs'][r], 3)} | "
                 f"{fmt(link['edges'][r])} | {fmt(link['tests'][r], 3)} |")
    L.append("")
    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text("\n".join(L), encoding="utf-8", newline="\n")

    print(f"traceability: {len(reqs)} requirements, {len(must)} Must")
    print(f"  Must without story:          {len(no_story)}")
    print(f"  Must without UC/flow/spec:   {len(no_uc)}")
    print(f"  Must without test:           {len(no_test)}")
    if unknown:
        print(f"  UNKNOWN ids referenced:      {', '.join(unknown)}")
    print(f"  -> {OUT.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
