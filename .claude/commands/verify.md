---
description: Run the CI checks that apply to the files you changed, and report only what actually ran.
argument-hint: "[backend | web | mobile | catering | all]"
---

# Verify

Run the checks from the "Check your work" section of [CLAUDE.md](../../CLAUDE.md) for the area named in `$ARGUMENTS`.
With no argument, look at `git status` and `git diff --stat`, pick the areas that changed (backend under `src/` and `tests/`,
web under `apps/web`, mobile under `apps/mobile`, catering under `services/catering`) and run only those.

- Run the backend build with `-warnaserror` first, because CI does, then the tests.
- Report a table: check, result, and the real counts (for example "dotnet test: 121 passed").
- If a check could not run (no network, no device, stack not running), list it under "Not verified" with the reason. Never list an unrun check as passed.
- Do not change code to make a check pass unless asked. Report the failure and the likely cause.
