# AI usage

**Brief:** §21, §23 AI Usage. **Status:** Seeded. Day-to-day records live in the tracker workbook
(`SmartRail-Project-Tracker.xlsx` → AI Policy, AI Usage Log, Prompt Library, AI Failures, Modified Impl, AI Summary).
The documents here hold the **narrative** — the reasoned write-ups an assessor reads.

| §21 asks for | Where |
|---|---|
| Which AI tools or agents were used | Tracker → AI Policy; summary below |
| What responsibilities were assigned | Tracker → AI Policy (responsibility matrix) |
| Important prompts or workflows | [prompt-library.md](prompt-library.md) |
| How output was validated | Tracker → AI Usage Log (validation column); the policy's operating rules |
| Where AI output was incorrect or insufficient | [ai-was-wrong.md](ai-was-wrong.md) |
| Decisions where I overruled or changed AI proposals, and AI contributions I accepted | [owner-decisions.md](owner-decisions.md) |
| Where my own judgment improved the result | [ai-was-wrong.md](ai-was-wrong.md) — each entry states what I did instead |
| AI-generated implementation significantly modified | modified-implementations.md (from the build phase) |

## Tools and roles (summary)

| Tool | Main responsibility | Why this tool |
|---|---|---|
| ChatGPT | Requirement engineering structure, persona simulation | Holds a long structured document process across many turns |
| Gemini | Independent review of artifacts it did not author; comparable-product research | Independence is the point — kept out of authoring |
| Claude (chat and Claude Code) | Discovery consolidation, domain modelling, documentation, implementation, tests, tracking automation | Works directly on files in the repository; strong at multi-file reasoning |
| GitHub Copilot | Not used | Inline completion produces code I did not consciously choose and cannot log honestly (§28) |

**Governing rule:** the model that authored an artifact does not review it.

## How AI-drafted requirement content is validated

All seeded requirement documents start as `Proposed`. I review each row and mark it `Accepted`, `Changed` or
`Rejected`. Material changes get an entry in [ai-was-wrong.md](ai-was-wrong.md). Every finding is labelled with its
basis, so AI-simulated stakeholder input is never presented as evidence about the organisation (ASM-034).
