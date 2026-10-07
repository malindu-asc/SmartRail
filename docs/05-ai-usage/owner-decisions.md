# Owner decisions versus AI proposals

Brief §21 asks for examples where **my own judgment changed or rejected what AI suggested**, and for how AI output was
validated. This file is the running record. It is written to be honest in both directions: where I overruled the AI,
where I accepted it, and where the outcome is not yet known.

Entries describe what happened; they do not claim the owner's choice was right or wrong until there is evidence.

## 1. Where I overruled, rejected or changed an AI proposal

| # | Date | Topic | AI proposed | I decided | My reason | Effect | Outcome |
|---|---|---|---|---|---|---|---|
| 1 | 2026-10-06 | **Ticketing, accounts, payments** | Treat as scope creep; document as a deferred design and revisit at D14 | **Build as a thin, gated slice** — it is the main flow the demo must show | A demo that shows the flow beats one that explains it | About +5 days; reversed ASM-012, X-01, X-02; gate at D14 | **Open** — judged at the D14 gate |
| 2 | 2026-10-06 | **Catering platform** | Keep a thin integration, built last, to keep the graded Integration row | **Cut entirely** | Not a priority; ticketing is the main flow | Forgoes the Integration row of §26 and one §23 artifact — knowingly (ASM-005) | Accepted risk |
| 3 | 2026-10-06 | **Refund Officer role** | Add a dedicated role to view and retry refunds | **Reject**: no unnecessary roles | Reuse the roles and capabilities the MVP already needs | Role removed; first moved to Customer Service, then replaced by automatic retry (ASM-046) | Done |
| 4 | 2026-10-06 | **Number of roles** | Build eleven roles to varying depth | **Build six**; fold or defer the rest, with a written justification for each | Roles are permission bundles; the MVP needs capabilities, not accounts | Fewer accounts and tests; Network Controller concentrates power (ASM-045, mvp-roles.md) | Done; risk mitigated by audit, second approver, confirmation step |
| 5 | 2026-10-06 | **Journey QR** | Offer a journey QR (terminal to phone) as the way to show scanning | **Use a ticket QR instead** | The demo needs tickets, not a link | Ticket wallet and inspector scan replace it | Done |
| 6 | 2026-10-06 | **Scope after the 14-day check** | Cut the terminal, report, planned works, incident workflow, handover, timetable authoring UI | **Keep the full scope**; build with AI | Once the backend exists, most roles and rules are business logic AI builds quickly | Nothing cut yet; cut order and D14 gate remain | **Open** — judged at the D14 gate |
| 7 | 2026-10-06 | **Refund approval** *(my idea, AI changed it)* | I proposed manual back-office approval | AI changed it to **auto-approval**, with retry; I accepted | Eligibility after a railway cancellation is a fact, not a judgment | One approval screen removed | Accepted with edits |
| 8 | 2026-10-06 | **Edits I had not asked for** | Updated registers and wrote ticketing documents while I was only asking questions | **Interrupted twice**; edit only when asked | I need to control what changes in my own documents | Working rule saved to memory | Done |
| 9 | 2026-10-06 | **Interview method** | Describe the system to each persona and add scripted scenario probes | **Reject**: discovery interviews are open and follow what the person says | Real interviews do not lead | Discovery round with no system described; probes replaced by persona-chosen follow-ups; first round kept as a validation round | Done |

## 2. AI contributions I accepted, and how each was validated

"Reviewed" means I discussed or confirmed it in conversation. Everything else is still `Proposed`.

| AI contribution | Where | How it was validated | Status |
|---|---|---|---|
| Simulator plays the **train detection feed**, not driver GPS | system-context.md, simulator-design.md, ASM-008 | Reasoned from how railways derive position; consequence checked against the driver-offline scenario | **Pending my confirmation** (OQ-02) |
| Journey state as **three dimensions** (lifecycle, punctuality, confidence) | state-models.md | Worked through composition examples; found the flaw in ChatGPT's single-status model | Used; not yet formally reviewed |
| **Identity model** — journey id is the key; vehicles and crews attach by time-ranged assignment | identity-model.md | Walked substitution, handover and breakdown cases; I asked the breakdown/crash question that drove it | Reviewed in conversation |
| **Source precedence** table | source-precedence.md | Worked conflict cases (offline replay vs feed time) | Not yet reviewed |
| Driver edge cases — five additions (EDG-144 … 148, REQ-DRV-011) | driver docs | I supplied 16 cases; AI proposed merges and additions; I said add only what is missing | Reviewed in conversation |
| Ticketing refinements: ticket bound to journey id; terminate-short affects only the lost part; double availability check; automatic refund if cancelled mid-checkout; inspector as a permission mode | 12-ticketing, CF-09 … 11 | Checked against the 35 decisions I supplied | **Not yet reviewed** by me line by line |
| Role mapping (six roles; where the others went) | mvp-roles.md | I proposed the structure; AI wrote the justification and the risk controls | Reviewed in conversation |
| 135 requirements, 100+ edge cases, 34+ assumptions | requirements, edge cases, assumptions | Cross-reference script (no broken links, every Must has a story and a use case or spec); **content not yet reviewed** | **Proposed** — review pending |

## 3. What this shows about the process

- I changed the AI's scope three times (ticketing in, catering out, roles down) and the AI's detail twice (driver cases,
  refund approval). The challenge rate is not zero.
- Two decisions (rows 1 and 6) went against the AI's advice. They are recorded as **open**, not as wins.
- Most of the AI-drafted volume is not yet reviewed. That is the largest honesty risk in this record, and the reason
  every seeded row is still `Proposed`.
