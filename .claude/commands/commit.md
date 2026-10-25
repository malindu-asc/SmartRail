---
description: Draft a conventional commit message for the staged changes. Does not stage, commit or push.
argument-hint: "[optional context, e.g. the spec id]"
---

# Commit message

Read `git diff --staged --stat` and the staged diff. If nothing is staged, say so; do not stage anything.

Draft one message: `type(scope): imperative subject`, subject about 50 characters, types feat, fix, refactor, test, docs, chore.
Add a short body only when it explains why, and a `Refs: SPEC-NNN` line when the change belongs to a spec.
**No `Co-Authored-By` line and no AI attribution.**

If the staged work mixes unrelated changes, explain the groups and suggest a split instead of one message.
Return the message in a code block. Do not invent test results. Commit only when the user explicitly asks you to, using the files they intend.
