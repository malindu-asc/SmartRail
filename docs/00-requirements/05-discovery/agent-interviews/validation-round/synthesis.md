# Synthesis — Claude agent interviews

> **Status: proposals only.** Nothing here has been adopted into the requirements. Each item is a recommendation for
> the owner to accept, change or reject. Source transcripts: [raw/](raw/). Method: [protocol.md](protocol.md).

## 1. How far to trust this round

- 12 independent agents, one per persona, clean context, same five questions plus three probes each.
- Each agent reports that it read only the protocol and the brief. That is **self-reported**, not independently checked.
- **Some agreement is built in.** The system description given to the agents already says position comes from
  train-detection data, so a driver "being glad" about that is partly an echo. Treat agreement on that point as weak.
- All agents are the same model family as the assistant that drafted our docs. The **cross-check with a different model
  (Gemini / ChatGPT) has not been done**; until it is, this is one model's view from twelve angles.
- All 12 rated their own confidence **Medium** and flagged where they were guessing.

## 2. What most personas converged on

| Theme | Raised by | Our position |
|---|---|---|
| **Never show a confident wrong answer; show how old data is** | commuter, traveller, controller, station manager, inspector, accessibility, QA, PO, security | Core design (REQ-LIVE-004, REQ-PAX-005, REQ-DSP-004) — **confirmed** |
| **A wrong platform is the worst failure** | commuter, traveller, station manager, accessibility, QA | CF-05, urgent push, "changed from" — **confirmed** |
| **Radio is the safety channel; the app must not claim to be** | driver, controller, station manager, PO | ASM-009, CF-08 — **confirmed** |
| **Every high-impact action is attributable, with a second approver** | controller, scheduler, CS, security, QA | Audit, second approver, separation of duties — **confirmed** |
| **Step-free and lift status** | accessibility, station manager, traveller | Only static facilities today — **gap** (NF-01) |
| **Inspection should not share the driver's app** | inspector, security (PO: breadth) | We decided a mode in the driver app — **challenged** (NF-32) |

## 3. Already covered — no action

| Raised | By | Where we cover it |
|---|---|---|
| Stale data labelled, "as of" times | commuter, controller, CS, accessibility | REQ-PAX-005, REQ-DSP-004, REQ-LIVE-004 |
| Notification spam; important alerts only | commuter, station manager | ASM-022, notification-matrix |
| Automatic refund, nothing paid for a train that did not run | commuter | CF-10, REQ-REF-001, REQ-REF-005 |
| Offline duty and queue; entries keep their timestamps | driver, QA | REQ-DRV-005, REQ-DRV-006 |
| Control messages: delivery proof, held while moving | driver, controller | REQ-DRV-007, UC-DRV-05 |
| Driver delay reason must not overrule the controller | controller | source-precedence (original retained, controller may reclassify) |
| Audit of every decision | controller, scheduler, security | REQ-OPS-011 |
| Draft versus live timetables; effective dates; planned works | scheduler | REQ-OPS-003, REQ-OPS-016, REQ-LIVE-017 |
| Per-device revocable credentials; terminals keep no data | security | REQ-DSP-003, REQ-TRM-004 |
| Admin cannot operate; user management separated from operations | security | REQ-OPS-001 |
| Feed authenticated, replay and implausible updates rejected | security, QA | REQ-SEC-004, REQ-LIVE-005 |
| Injectable clock; deterministic, replayable simulator | QA | REQ-NFR-005, REQ-SIM-002 |
| Midnight / service-date handling; "late" thresholds | QA | REQ-LIVE-009, ASM-021 |
| Status never colour-only; plain wording; second language | accessibility, traveller | REQ-PAX-004, ASM-030 |
| Connection at risk | traveller | REQ-PAX-006 |
| Replacement vehicle on the same journey keeps the ticket valid | inspector | identity-model, REQ-LIVE-010 |

## 4. New findings

**Effort:** S under about 2 hours · M about half a day. **Rec:** Adopt = recommend building · Wording = a rule, no build ·
Could = if time · Doc = document only · Decide = needs the owner.

| # | Finding | By | Today | Rec | Effort | Why |
|---|---|---|---|---|---|---|
| NF-01 | **Lift / stair / gate status as first-class data**, set by the station view and shown on passenger app and terminal | accessibility, station mgr, traveller | Static facilities only | **Adopt** | M | Three personas converge; high value for a wheelchair user; small data model |
| NF-02 | Station view in the console works **on a phone** | station mgr | Not stated | **Adopt** | S | Responsive layout; staff are on the concourse, not at a desk |
| NF-03 | **One-click undo** of a mistaken cancel/terminate for a short window, not only reinstate before departure | controller | Reinstate before departure | **Adopt** | S | Cancelling the wrong train is the highest-consequence mistake |
| NF-04 | **Publish receipt**: "applied to 7 displays, 1 offline, 12 followers notified" | controller | Not stated | **Adopt** | M | Answers "a change that silently did not apply" |
| NF-05 | Bulk actions on many journeys at once | controller | Scope-based disruptions resolve to many; no bulk cancel | Could | M | Disruption scope covers most of it |
| NF-06 | General shift handover | controller | Incident handover only | Doc | — | Incident handover exists |
| NF-07 | Crew and vehicle recovery after a disruption | controller, scheduler | Out of scope | Doc | — | Needs a capacity/rotation model (X-03) |
| NF-08 | Crowd size and crowd safety at stations | station mgr | None | Doc | — | No sensors; a staff procedure, not an app feature |
| NF-09 | Alerts to station staff's own phones | station mgr | None | Doc | — | Covered in practice by NF-02 |
| NF-10 | **"My trips"**: saved regular trains and a calm "all fine" view | commuter | Follow requires a tap per journey | **Adopt** | S | Reduces setup effort; the commuter is the main user |
| NF-11 | **Refund status and expected time** visible to the passenger | commuter, CS | State exists; timing not shown | **Adopt** | S | Cheap; stated gap from two personas |
| NF-12 | Per-station transfer and walking time; luggage awareness | traveller | One flat 5-minute value | Could | S | Seed per-station values |
| NF-13 | "Ask staff / where to get help" on every screen and on stale displays | traveller, accessibility | None | **Adopt** | S | A human fallback is cheap and honest |
| NF-14 | **Terminal warns before the idle reset** ("Still there?") | accessibility | Silent reset at 60 s | **Adopt** | S | A silent timeout is an accessibility failure |
| NF-15 | Audible announcements | accessibility | Out of scope, noted | Doc | — | Already a stated limitation |
| NF-16 | Screen-reader / visual **parity test**; state that testing with disabled users was not possible | accessibility | Automated + manual planned | Wording | S | Honest limit plus a concrete test |
| NF-17 | **Calm offline indicator** for the driver, never alarming; messages held until stationary, derived from feed state; unknown state treated as moving | driver | Explicit offline banner | Wording | S | Resolves a tension (section 5) |
| NF-18 | Delay reasons categorised by **cause, never blame**; show who decided | driver | Reason list exists | Wording | S | Driver's trust trigger |
| NF-19 | **Publish checklist**: warn on unassigned journeys and double-booked vehicles or drivers | scheduler | Unassigned queue; driver overlap rejected | **Adopt** | S | Small extension of existing checks |
| NF-20 | Separate **Planning** and **Live** areas in the console | scheduler | One console | Wording | S | Navigation only; stops a planning edit looking like a live action |
| NF-21 | Inspector can record a judgement call with a reason | inspector | None | Could | S | |
| NF-22 | Re-validate scans queued offline once back online and flag invalid ones | inspector | Queued scans only logged | Could | M | |
| NF-23 | Rolling QR code to defeat screenshots | inspector | Documented limit | Doc | — | Real defence, but extra cryptography and offline complexity |
| NF-24 | Excess fare for riding past a stop | inspector, CS | Out of scope | Doc | — | Railway process, not SmartRail |
| NF-25 | If Customer Service is ever built: verified-caller step, case log, "what the passenger saw then" | CS | Role deferred | Doc | — | Keeps the deferral honest |
| NF-26 | **Rate limit on the feed per source**; alert on bursts of high-impact actions; tamper-evident (hash-chained) audit | security | Sequence and plausibility checks | Adopt rate limit; Could the other two | S | Cheap hardening of the most dangerous input |
| NF-27 | **Area-scoped** least privilege for staff | security | Folded into a global Network Controller (ASM-045) | Accepted risk | — | Direct tension with our role decision (section 5) |
| NF-28 | Simulator source key and controls **must not exist in production config** | QA, security | Reset endpoint only | **Adopt** | S | Extend REQ-SIM-005 |
| NF-29 | Test expectations derived **independently of the system** | QA | Scenario files hold `expect:` blocks | Wording | S | State it as a test-strategy rule |
| NF-30 | Success measures per app ("good enough"), a **fallback if live data fails during the demo**, "simulated" labelled honestly | PO | Fallback video planned | **Adopt** | S | Documentation |
| NF-31 | Platform change inside the five E2E flows | QA | Not asserted anywhere | Wording | S | Fold into E2E-3; keep our display-offline flow (E2E-5) |
| NF-32 | **Ticket inspection separate from the driver's app** | inspector, security, PO (breadth) | A mode in the driver app (ASM-043) | **Decide** | S–M | See T-1: separate entry point and sign-in |
| NF-33 | Terminal as the first thing to cut | PO | Lowest priority in the build order | Decide | — | See T-4: boards stay, so the traveller's need holds |

## 5. Disagreements and tensions

| # | Tension | Sides | Recommendation |
|---|---|---|---|
| T-1 | **Inspection inside the driver app** | Inspector and security: separate. PO: fewer surfaces | Keep one Flutter codebase but a **separate entry point with its own sign-in and role gate**; drivers never see it. Costs about half a day. **Owner decision (NF-32).** |
| T-2 | **Calm offline state vs explicit offline banner** | Driver: nothing alarming. Us: explicit | Neutral, calm wording and no red alert, but still show last sync and "messages may be missing" (NF-17) |
| T-3 | **Area-scoped roles vs one Network Controller** | Security: scope by area. Us: consolidated for the MVP | Accept the risk, mitigated by audit, second approver, confirmation step; note it as the first thing to restore in production (NF-27) |
| T-4 | **Cut the terminal first** | PO: cut first. Traveller: boards and terminals are the main way in | Both hold if you read the traveller carefully: they need **boards**, which stay. The terminal adds search and planning. Keep it last in the build order (NF-33) |
| T-5 | **Planning vs live in one console** | Scheduler: separate. Us: one console | Separate areas inside one console (NF-20) |
| T-6 | **Full scope vs breadth risk** | PO: biggest risk is breadth; cut terminal, extra roles, reporting, timetable editing, display polish first. Owner: keep full scope | Same advice as the earlier 14-day check. Recorded as **open**; judged at the D14 gate |

## 6. What this round did not change

The core held: honest staleness, platform-change handling, the radio as the safety channel, auditability, separation of
duties, a deterministic simulator. Nothing here argues for restructuring the architecture. Most new items are small.

## 7. Recommended adoption

Adopt now, about **1.5 days** of work, mostly small:
NF-01 lift status · NF-03 undo · NF-04 publish receipt · NF-11 refund timing · NF-14 idle warning · NF-10 my trips ·
NF-19 publish checklist · NF-28 simulator in production · NF-26 feed rate limit.

Adopt as wording or rules, about 0 build: NF-02, 13, 16, 17, 18, 20, 29, 30, 31.

Document only: NF-06 to 09, 15, 23 to 25, 27. Could, if ahead at D14: NF-05, 12, 21, 22.

## 8. Decisions needed from the owner

1. **NF-32 / T-1:** keep ticket inspection as a mode in the driver app, or give it a separate entry point with its own sign-in?
2. Adopt the "now" list as proposed, or trim it?
3. Run the cross-check in Gemini or ChatGPT for the sharpest four (controller, driver, product owner, security)?
