---
description: Check a finished feature end to end and produce a report (Markdown, HTML and PDF) of what passed, failed and was not run.
argument-hint: "<feature name> [areas: backend,web,mobile,catering,e2e] [screens to capture]"
---

# Feature check

Use `$ARGUMENTS`: the feature name, and optionally the areas to check and the screens to capture. Do not change source code.
This command produces evidence. It never commits, stages or pushes.

1. **Run the checks.** From the repository root:
   `node scripts/feature-check/run.mjs --name "<feature>"` (add `--areas backend,web,...` to choose; without it the areas come from `git status`,
   and the browser flows run when the stack answers on ports 3000 and 5080). The script runs the same commands as CI and writes
   `reports/feature-check/<stamp>-<feature>/` with `results.json`, `report.md` and `report.html`. Failures are real: report them, do not retry until green.
2. **Capture the screens.** If the stack is up and the feature has screens, use the `playwright-cli` skill. Sign in with the dev account for the
   role the feature serves (accounts are in `infra/keycloak/smartrail-realm.json`), open each screen and save
   `playwright-cli screenshot --filename="<report folder>/shots/<NN>-<screen>.png"`. Name the files in the order a reader should see them.
   Capture the failure case as well as the happy path when the feature has one. If the stack is down, say so in the report; do not invent screenshots.
3. **Rebuild the report with the screenshots, and make the PDF.** `node scripts/feature-check/run.mjs --report "<report folder>" --pdf`.
   (`playwright-cli` blocks `file:` pages, so the script makes the PDF with the Playwright library instead.) If it prints "PDF not made", say why.
4. **Tell the user** the verdict, the three numbers (passed, failed, not run), the report folder, and anything under "Not verified" with its reason.
   Say plainly if a check failed or did not run. Never describe an unrun check as passed.

The `reports/` folder is ignored by git because screenshots can show data. Leave `.playwright-cli/` alone for the same reason.
