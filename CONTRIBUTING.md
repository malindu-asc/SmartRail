# Contributing

## Spec-Driven Development

All non-trivial changes follow Spec-Driven Development. The full guide and the spec register are in [`specs/README.md`](specs/README.md).

1. **Propose:** copy [`specs/TEMPLATE.md`](specs/TEMPLATE.md) to `specs/SPEC-NNN-short-title.md`, fill it in, set status `In review`.
2. **Review:** the owner approves it in the spec's review record and sets status `Approved`.
3. **Implement:** branch `feature/<feature-name>`, build only what the spec says, and write a test for every acceptance criterion.
4. **Verify:** fill the spec's verification table with results and evidence, set status `Verifying`.
5. **Archive:** after the PR is merged, set status `Done` and move the file to `specs/archive/`.

No code is merged without an approved spec, except trivial changes and critical hotfixes (spec within one day).

## Branches

| Prefix | Use |
|---|---|
| `feature/<feature-name>` | Work for a spec (one or more branches per spec) |
| `fix/…` | Bug fix (with or without a spec, see `specs/README.md`) |
| `chore/…`, `ci/…`, `docs/…` | Tooling, pipeline and documentation |

Pull requests go into `dev`. `dev` is merged into `main` when it is stable.

## Commits

Conventional commits: `type(scope): subject`, with an optional short body. No `Co-Authored-By` lines and no AI attribution. Reference the spec in the body:

```
feat(journeys): record stop events from the driver app

Refs: SPEC-003
```

## Pull requests

Fill in the pull request template, including the spec ID and the acceptance-criteria checklist. Reviewers check that:

- the change matches the approved spec, with no scope creep;
- every acceptance criterion has a passing test;
- module boundaries hold (architecture tests pass);
- the spec's verification and sync sections are filled in.

## Backend conventions

Backend code follows the rules in [`.claude/skills/smartrail-modular-monolith/SKILL.md`](.claude/skills/smartrail-modular-monolith/SKILL.md).
