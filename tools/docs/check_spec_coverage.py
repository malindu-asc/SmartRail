#!/usr/bin/env python3
"""Check that every MVP requirement is owned by exactly one spec, and rebuild specs/COVERAGE.md.

Inputs
    docs/00-requirements/06-requirements/requirements.md   requirements with their MVP flag (Yes, Thin, No)
    specs/coverage-map.csv                                  req,spec,note   (hand edited: the owning spec of each requirement)
    specs/README.md                                         the register (which specs exist, and their titles)

Fails (exit 1) when
    an MVP (Yes or Thin) requirement has no owner, or has more than one,
    the map names a requirement that does not exist or is not in the MVP,
    the map names a spec that is not in the register.

Run from anywhere:  python tools/docs/check_spec_coverage.py
Zero dependencies.
"""
from __future__ import annotations

import csv
import re
import sys
from collections import defaultdict
from datetime import date
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
REQ_FILE = ROOT / "docs" / "00-requirements" / "06-requirements" / "requirements.md"
MAP_FILE = ROOT / "specs" / "coverage-map.csv"
REGISTER = ROOT / "specs" / "README.md"
OUT = ROOT / "specs" / "COVERAGE.md"

HEAD_RE = re.compile(r"^####\s+(REQ-[A-Z]+-\d{3})\s+—\s+(.+)$", re.M)
META_RE = re.compile(r"\*\*Priority:\*\*\s*(\w+).*?\*\*MVP:\*\*\s*(\w+)")
ROW_RE = re.compile(r"^\|\s*(SPEC-\d{3})\s*\|\s*(.+?)\s*\|\s*(.+?)\s*\|", re.M)


def read_requirements() -> dict[str, tuple[str, str, str]]:
    text = REQ_FILE.read_text(encoding="utf-8")
    heads = list(HEAD_RE.finditer(text))
    out = {}
    for i, h in enumerate(heads):
        end = heads[i + 1].start() if i + 1 < len(heads) else len(text)
        meta = META_RE.search(text[h.start():end])
        out[h.group(1)] = (h.group(2).strip(), meta.group(1), meta.group(2))
    return out


def read_register() -> dict[str, tuple[str, str]]:
    """spec id -> (title, status) from the register table in specs/README.md."""
    text = REGISTER.read_text(encoding="utf-8")
    return {m.group(1): (m.group(2), m.group(3)) for m in ROW_RE.finditer(text)}


def main() -> int:
    reqs = read_requirements()
    register = read_register()
    owners: dict[str, list[str]] = defaultdict(list)
    notes: dict[str, str] = {}
    problems: list[str] = []

    with MAP_FILE.open(encoding="utf-8", newline="") as f:
        for row in csv.DictReader(f):
            rid, spec = row["req"].strip(), row["spec"].strip()
            owners[rid].append(spec)
            notes[rid] = row.get("note", "").strip()

    mvp = {r for r, (_, _, level) in reqs.items() if level in ("Yes", "Thin")}

    for rid in sorted(mvp):
        if rid not in owners:
            problems.append(f"{rid} ({reqs[rid][0]}) has no owning spec")
        elif len(owners[rid]) > 1:
            problems.append(f"{rid} is owned by more than one spec: {', '.join(owners[rid])}")

    for rid, specs in owners.items():
        if rid not in reqs:
            problems.append(f"{rid} is in the map but not in requirements.md")
        elif rid not in mvp:
            problems.append(f"{rid} is in the map but its MVP flag is {reqs[rid][2]}")
        for spec in specs:
            if spec not in register:
                problems.append(f"{rid} is owned by {spec}, which is not in the register (specs/README.md)")

    by_spec: dict[str, list[str]] = defaultdict(list)
    for rid, specs in owners.items():
        if rid in mvp:
            by_spec[specs[0]].append(rid)

    lines = [
        "# Spec coverage",
        "",
        f"> Generated {date.today().isoformat()} by `tools/docs/check_spec_coverage.py` from `specs/coverage-map.csv`. Edit the CSV, not this file.",
        "",
        "Every requirement with MVP set to Yes or Thin is owned by exactly one spec: the spec that finishes it. "
        "A spec may touch other requirements on the way (a data field in one spec, its behaviour in another); the note says so.",
        "",
        "## Summary",
        "",
        f"- MVP requirements (Yes or Thin): **{len(mvp)}**",
        f"- Owned by exactly one spec: **{sum(1 for r in mvp if len(owners.get(r, [])) == 1)}**",
        f"- Problems: **{len(problems)}**",
        "",
        "| Spec | Title | Status | Requirements |",
        "|---|---|---|---|",
    ]
    for spec in sorted(register):
        title, status = register[spec]
        lines.append(f"| {spec} | {title} | {status} | {len(by_spec.get(spec, []))} |")

    if problems:
        lines += ["", "## Problems", ""] + [f"- {p}" for p in problems]

    for spec in sorted(register):
        ids = sorted(by_spec.get(spec, []))
        if not ids:
            continue
        title, status = register[spec]
        lines += ["", f"## {spec} {title}", "", f"Status: {status}", "", "| Requirement | Title | Priority | MVP | Note |", "|---|---|---|---|---|"]
        for rid in ids:
            t, pri, level = reqs[rid]
            lines.append(f"| {rid} | {t} | {pri} | {level} | {notes.get(rid, '')} |")

    OUT.write_text("\n".join(lines) + "\n", encoding="utf-8")

    for p in problems:
        print("PROBLEM:", p)
    print(f"{len(mvp)} MVP requirements, {len(problems)} problems. Wrote {OUT.relative_to(ROOT)}")
    return 1 if problems else 0


if __name__ == "__main__":
    sys.exit(main())
