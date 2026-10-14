# Requirement Engineering

## The process, and why it has this shape

The brief is explicit that it is *an initial business brief rather than a complete requirements
specification* (§1, §22). The work here therefore follows the problem-first flow from the training —
problem → impact → current state → discovery → solution → documentation → backlog — adapted in two places
for the realities of this assessment:

1. **There is no client to interview.** The current-state step can only record what is unknown. Rather than
   inventing a current process, [03-current-state.md](03-current-state.md) says so plainly and turns the
   unknowns into questions. Stakeholder input comes from structured AI persona simulation, and every finding
   is labelled with its basis so nothing simulated is presented as fact (ASM-034).
2. **The window is three weeks for six applications.** Discovery is broad, specification is deep only where we
   build. About 70 edge cases are registered; about 15 are fully specified and tested.

A domain section (04) sits between framing and discovery. Without a shared model of what a "train" is, and of
who is allowed to say what about it, discovery produces a list of features rather than a connected system.

## Reading order

| # | Document | Answers | Status |
|---|---|---|---|
| 01 | [Problem framing](01-problem-framing.md) | What did the organisation ask for, and what is it really trying to fix? | Seeded |
| 02 | [Stakeholders and impact](02-stakeholders-and-impact.md) | Who is affected, how, and how much? | Seeded |
| 03 | [Current state](03-current-state.md) | How does it work today — and what we cannot know | Seeded |
| 04 | [Domain: system context](04-domain/system-context.md) | **How the five applications connect** — who writes truth, who reads it | Seeded |
| 04 | [Domain: identity model](04-domain/identity-model.md) | Service vs journey vs vehicle vs locomotive vs crew | Seeded |
| 04 | [Domain: state models](04-domain/state-models.md) | Every state machine in the system | Seeded |
| 04 | [Domain: source precedence](04-domain/source-precedence.md) | When two sources disagree, who wins | Seeded |
| 04 | [Domain: glossary](04-domain/glossary.md) | Railway terms used throughout | Seeded |
| 05 | [Discovery method](05-discovery/method.md) | How discovery was done | Seeded |
| 05 | [Personas](05-discovery/personas.md) | Who we are designing for | Seeded |
| 05 | [Stakeholder interviews](05-discovery/stakeholder-interviews.md) | Simulated interviews, from each person's point of view | Seeded |
| 05 | [Discovery register](05-discovery/discovery-register.md) | **The §22 deliverable** — 116 findings across 14 categories | Seeded |
| 05 | [Conflicts and ambiguities](05-discovery/conflicts-and-ambiguities.md) | Where requirements pull against each other | Seeded |
| 05 | [Assumptions](05-discovery/assumptions.md) | Facts, decisions and assumptions — kept apart | Seeded |
| 05 | [Open questions](05-discovery/open-questions.md) | What we would ask the organisation | Seeded |
| 06 | [Requirements](06-requirements/requirements.md) | 147 testable requirements | Seeded |
| 06 | [Permission matrix](06-requirements/permission-matrix.md) | Who may do what | Seeded |
| 06 | [MVP roles](06-requirements/mvp-roles.md) | The six roles built in the MVP, and where the others went | Seeded |
| 06 | [Notification matrix](06-requirements/notification-matrix.md) | Which event notifies whom, how, how often | Seeded |
| 06 | [Degraded-mode matrix](06-requirements/degraded-mode-matrix.md) | What each app does when the network fails | Seeded |
| 06 | [Data classification](06-requirements/data-classification.md) | What data exists, who owns it, how long it lives | Seeded |
| 07 | [Use cases](07-use-cases/README.md) | Per application, including failure flows | Seeded |
| 08 | [Cross-app flows](08-cross-app-flows/README.md) | **How one event travels across all five applications** | Seeded |
| 09 | [Edge cases](09-edge-cases/catalogue.md) | The catalogue, and the [fully specified set](09-edge-cases/specified.md) | Seeded |
| 10 | [User stories](10-user-stories/stories.md) | Stories with Given/When/Then acceptance criteria | Seeded |
| 11 | [MVP scope](11-scope/mvp-scope.md) | Build / thin / defer, with reasons | Seeded |
| 11 | [Traceability](11-scope/traceability.md) | Generated — requirement to story, flow, edge case, test | Generated |
| 12 | [Ticketing](12-ticketing/README.md) | Accounts, tickets, payments, refunds, inspection — the 35 decisions and the build gate | Seeded |

## How this maps to the brief's required artifacts (§23 Requirements)

| §23 asks for | Where it is |
|---|---|
| Refined requirements | [06-requirements/requirements.md](06-requirements/requirements.md) |
| Identified assumptions | [05-discovery/assumptions.md](05-discovery/assumptions.md) |
| User personas or actors | [05-discovery/personas.md](05-discovery/personas.md), [02-stakeholders-and-impact.md](02-stakeholders-and-impact.md) |
| User journeys | [07-use-cases/](07-use-cases/README.md) (journeys are the main flows) and [08-cross-app-flows/](08-cross-app-flows/README.md) |
| User stories or equivalent | [10-user-stories/stories.md](10-user-stories/stories.md) |
| Acceptance criteria | In each story, and the **Acceptance** line of every requirement |
| Identified edge cases | [09-edge-cases/](09-edge-cases/catalogue.md) |

And §22's eleven discovery categories are each a section of the
[discovery register](05-discovery/discovery-register.md), plus three categories added deliberately.

## Freeze

Requirements baseline at the end of **D5–D6**. After the baseline the registers keep growing, but every change is
recorded in the change log below with its impact, rather than rewritten silently.

## Change log

| Date | Change | Impact | By |
|---|---|---|---|
| 2026-10-05 | Initial seed of all requirement documents from discovery work | — | Malindu (AI-assisted, under review) |
| 2026-10-06 | Ticketing, accounts and payments brought into the MVP as a thin gated slice; catering cut entirely; E2E-4 replaced; role TicketInspector added; refund recovery is automatic; ASM-005, ASM-012, ASM-013 changed; REQ-ACC/TKT/PAY/REF/INS, UC-TKT, CF-09..11, EDG-120..143, US-100..108 added | Scope: +~4-5 days gated at D14; catering Integration row forgone | Malindu decision, Claude drafted |
| 2026-10-06 | Role consolidation: MVP builds six roles; Station Manager, Incident Manager, Fleet, Reporting folded into Network Controller / Scheduler; Customer Service deferred; mvp-roles.md added; ASM-013, ASM-043 changed; ASM-045..047 added; REQ-OPS-001, 007, 012, 015, REQ-REF-004 reworded; EDG-097 and US-043, US-106 changed | Fewer accounts and tests; Network Controller concentration mitigated by audit, second approver and confirmation step | Malindu decision, Claude drafted |
| 2026-10-07 | Station Master restored as a role (seven roles); gate board/exit validation added (tickets gain Boarded and Completed); excess fare shown at the exit, collected in person; fraud handled physically; inspector uses own phone for spot checks; customer information handled by the call center; REQ-GAT-001..010, REQ-OPS-018/018, UC-GAT-01..04, UC-OPS-16, EDG-149..159, US-109..114, SIM-21/22 added; CF-11 rewritten | Scope: gates +1-1.5 days, first to be cut at D14; Network Controller less concentrated | Malindu decisions, Claude drafted |
| 2026-10-07 | Catering reduced to a minimal read-only menu with call-to-order (separate seeded service, API key, 5-minute cache, failure isolation); ordering, payment, collection, ETA feed and webhooks not built; ASM-005, ASM-044 changed, ASM-054 added; REQ-INT-001, 004, 005 revived as Thin, REQ-INT-002, 003 stay Rejected; UC-EXT, US-090..092, EDG-110, 160, 161 updated | Scope: about +0.5 day; Integration row partly recovered | Malindu decision, Claude drafted |
