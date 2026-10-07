# SmartRail — Documentation

This folder is the engineering record for the SmartRail assessment. It is written to be **used** during the
build, not assembled at the end: requirements are traced to code and tests, decisions carry their rejected
alternatives, and anything assumed is labelled as assumed.

| Need | Go to |
|---|---|
| What are we building and why | [00-requirements/README.md](00-requirements/README.md) |
| How the five applications connect | [00-requirements/04-domain/system-context.md](00-requirements/04-domain/system-context.md) |
| What happens when things go wrong | [00-requirements/08-cross-app-flows/](00-requirements/08-cross-app-flows/README.md) and [09-edge-cases/](00-requirements/09-edge-cases/catalogue.md) |
| What is in and out of scope | [00-requirements/11-scope/mvp-scope.md](00-requirements/11-scope/mvp-scope.md) |
| Schedule, progress, AI usage log | `SmartRail-Project-Tracker.xlsx` at the repo root (spreadsheet, not markdown) |
| Overall delivery plan | [../plan.md](../plan.md) |

## Folder map

| Folder | Contents | Brief section | Status |
|---|---|---|---|
| [00-requirements](00-requirements/README.md) | Problem framing, stakeholders, domain model, discovery, requirements, use cases, cross-app flows, edge cases, stories, scope, ticketing | §3–§14, §20, §22, §25 | **Seeded — under review** |
| [01-ux](01-ux/README.md) | UX discovery, flows, wireframes, design system, accessibility | §15, §23 Product and UX | Planned |
| [02-architecture](02-architecture/README.md) | Architecture, ADRs, integration, simulator design | §23 Architecture, §24 | Simulator design seeded |
| [03-qa](03-qa/README.md) | Test strategy, scenarios, defects, evidence, quality assessment | §18, §23 QA | Planned |
| [04-security](04-security/README.md) | Threat model, security decisions, testing evidence | §17, §23 Security | Planned |
| [05-ai-usage](05-ai-usage/README.md) | AI policy narrative, prompt library, where AI was wrong | §21, §23 AI Usage | Seeded |
| [06-operations](06-operations/README.md) | Observability, runbook, SLOs, deployment | §19 | Planned |
| [demo](demo/README.md) | Demo narrative, script, seed scenario | §27 | Planned |

## Conventions used everywhere

**IDs never change and are never reused.** A withdrawn item is marked `Rejected`, not deleted, so references
elsewhere never break.

| Prefix | Thing | Lives in |
|---|---|---|
| `DF-nnn` | Discovery finding (a gap in the brief) | 05-discovery/discovery-register.md |
| `ASM-nnn` | Assumption, fact or decision | 05-discovery/assumptions.md |
| `CON-nn` / `AMB-nn` | Conflict / ambiguity | 05-discovery/conflicts-and-ambiguities.md |
| `OQ-nn` | Open question | 05-discovery/open-questions.md |
| `PER-nn` | Persona | 05-discovery/personas.md |
| `REQ-<AREA>-nnn` | Requirement | 06-requirements/requirements.md |
| `UC-<APP>-nn` | Use case | 07-use-cases/ |
| `CF-nn` | Cross-application flow | 08-cross-app-flows/ |
| `EDG-nnn` | Edge case | 09-edge-cases/catalogue.md |
| `US-nnn` | User story | 10-user-stories/stories.md |
| `SIM-nn` | Simulator scenario | 02-architecture/simulator-design.md |
| `ADR-nnnn` | Architecture decision record | 02-architecture/adr/ (and the tracker's Decisions sheet) |

**Status of AI-drafted content.** Much of the first draft was produced with AI assistance and reviewed by me.
Every seeded row starts as `Proposed`. I move it to `Accepted`, `Changed` or `Rejected` after reviewing it, and
anything I changed materially is logged in [05-ai-usage/ai-was-wrong.md](05-ai-usage/ai-was-wrong.md). The
reviewed-versus-proposed count is part of the evidence for brief §21.

**Traceability is generated, not maintained by hand.** Run:

```bash
python tools/docs/build_traceability.py
```

It scans every markdown file for `REQ-` references and rebuilds
[00-requirements/11-scope/traceability.md](00-requirements/11-scope/traceability.md), including the list of
Must requirements that have no story or no test yet.
