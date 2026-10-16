---
name: spec-driven-development
description: Use before implementing any non-trivial SmartRail change (feature, endpoint, screen, event, table, permission, behaviour change) and whenever the user asks to write, review, approve, implement, verify or archive a spec. Enforces the specs/ workflow - proposal, review, implementation, verification, archive - with traceability to REQ/UC/US/CF ids and acceptance criteria mapped to tests.
---

# Spec-Driven Development for SmartRail

The process, statuses and register live in `specs/README.md`. The template is `specs/TEMPLATE.md`. This skill tells you how to act at each stage.

## Golden rules

1. **No functional code without an `Approved` spec.** If none exists, stop and propose one. Exceptions: trivial changes and critical hotfixes (see `specs/README.md`).
2. **The owner approves, not you.** Never set a spec to `Approved` yourself. Ask the user, then record their decision with the date in the review record.
3. **Build only what the spec says.** If something is missing or wrong, stop, update the spec, note it in the change log, and get it re-approved.
4. **Every acceptance criterion gets a test.** Name tests so the AC is obvious.
5. **Close the loop.** Fill verification and sync sections, update the register, archive.

## Stage 1: Propose

1. Run the pre-spec evaluation in `specs/README.md` and record the answers in the header.
2. Pick the next free number from the register. Create `specs/SPEC-NNN-short-title.md` from the template.
3. Fill every section from the real sources, not from assumptions:
   - requirements, use cases, stories and flows under `docs/00-requirements/`
   - MVP scope in `docs/mvp/mvp.md`
   - designs in the Figma file `ZCUQkZpq24jKc7zLvs5KFR` (frame names)
   - backend rules in `.claude/skills/smartrail-modular-monolith/SKILL.md`
4. Write acceptance criteria as Given / When / Then, numbered `AC-1`, `AC-2` …, each testable.
5. Put anything uncertain under open questions instead of deciding silently.
6. Set status `In review`, add the row or update it in the register, and ask the user to review.

## Stage 2: Review

- Summarise the spec for the user in a few lines: scope, contract, risks, open questions.
- Apply requested changes to the spec, not to code.
- When the user approves, record it: date, reviewer, decision. Set status `Approved`.

## Stage 3: Implement

1. Branch `feature/<feature-name>` from `dev` (named after the feature, not the spec number; use as many branches as the work needs). Set status `In progress`.
2. Work through the spec's task list in order: Domain → Application → Infrastructure → API → clients → tests.
3. Follow `smartrail-modular-monolith` for backend code.
4. Tick tasks in the spec as they are done.
5. Commits: conventional subject, `Refs: SPEC-NNN` in the body. No other trailers, and never a `Co-Authored-By` line or any AI attribution.

## Stage 4: Verify

1. Run the build and all tests (`dotnet build -warnaserror`, `dotnet test`, client builds as relevant).
2. For each AC, record Pass or Fail and the evidence (test name, screenshot, manual check) in the verification table.
3. Fill the sync status checklist. Regenerate traceability with `python tools/docs/build_traceability.py` when requirements docs are linked.
4. Set status `Verifying`. Report results honestly, including anything not verified.

## Stage 5: Archive

After the user merges the PR: set status `Done`, add the PR links, move the file to `specs/archive/`, update the register.

## Review checklist (when asked to review a PR)

- [ ] PR names an approved spec
- [ ] Change matches the spec, with no scope creep
- [ ] Every AC has a passing test
- [ ] Architecture tests pass
- [ ] Spec verification and sync sections filled in
