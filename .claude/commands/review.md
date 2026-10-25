---
description: Review a spec, files, staged or working changes, or a branch. Read-only unless you ask for fixes.
argument-hint: "[spec file, files, staged, working, or branch] [fix if requested]"
---

# Review

Review the target in `$ARGUMENTS`. With no target, review staged and unstaged changes and relevant untracked files.
For a branch comparison, work out the base from the repository (PRs go into `dev`); ask if it is ambiguous.

Check, in this order:

1. Correctness and regressions against the spec's acceptance criteria.
2. The rules in [CLAUDE.md](../../CLAUDE.md): module boundaries, audit and permission on operator actions, migrations, logging of request values, no scope creep.
3. Tests: every acceptance criterion has one that can fail.

Lead with findings ordered by impact. For each give the file and line, the concrete problem and its consequence, and a fix.
Skip cosmetic remarks without a practical benefit. If nothing is wrong, say so, and say what you inspected and what you could not verify.
This is read-only. Apply fixes only if asked, then re-run the relevant checks and separate resolved findings from open ones.
