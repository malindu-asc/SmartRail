# Synthesis — discovery round (primary)

> **Status: proposals only.** Nothing here is adopted into the requirements. Transcripts: [raw/](raw/). Method:
> [protocol.md](protocol.md). The earlier, leading round: [validation-round/](validation-round/README.md).

## 1. How far to trust this round

- 12 agents, clean context, **no system described**. Each persona talked about their own work and what they rely on today, then was resumed once to give two follow-up stories of their own choosing.
- Isolation is **self-reported**: each agent says it read only the protocol file. Not independently checked.
- **Scenario probes removed.** A first pass used three interviewer-written scenarios per persona (including the landslide). They steered the answers, so they were deleted. Points that came only from our scenarios were dropped — for example the inspector's photo-of-ticket and refunded-ticket concerns.
- Same model family as the assistant that drafted our docs. The **different-model cross-check has not been done.**
- All 12 rated their own confidence Medium and flagged guesses. "How many raised X" below is **my reading** of the extracted
  sections, not a measurement.

## 2. What they raised unprompted

| Theme | Raised by | Count | Our position |
|---|---|---|---|
| **Never confidently wrong; know how old information is** | all 12 | **12** | Core design (REQ-LIVE-004, REQ-PAX-005, REQ-DSP-004) — confirmed |
| **Sources disagree** (board vs phone vs announcement vs own screen) | commuter, traveller, controller, station mgr, CS, accessibility, PO | 7 | One journey state rendered everywhere — confirmed |
| **Learn about changes late** (platform, cancellation, faults) | commuter, traveller, station mgr, inspector, scheduler, driver | 6 | Immediate propagation; station view sees it first (CF-05) — confirmed |
| **A record of who did or was told what, and when** | controller, scheduler, CS, security, driver (fair blame), station mgr, QA | 7 | Audit log exists; **no per-journey "what each channel was told, and when"** — partial |
| **Corrections and stale copies do not reach everyone** (a mistaken cancel, an outdated plan in circulation, "delayed" never corrected to "cancelled", a stale step-free listing, a screen showing a train that had left) | controller, scheduler, CS, accessibility, station mgr | 5 | Retraction exists; no "reached N of M" proof — partial |
| **Fairness / not being blamed or tracked** | driver, inspector | 2 | Neutral reason codes, no driver tracking — partly stated |
| **Handover between shifts is a source of error** | controller | 1 (+ scheduler: version currency) | Incident handover only |
| **Live access status (lifts, ramps, stairs)** — a fault known internally but not told in time | accessibility, traveller | 2 | Static facilities only |
| **A record you can cite later** (cancellation proof, how long something was wrong, a named person behind a change) | CS, station mgr, security, driver | 4 | Ticket history and audit exist; see D-01, D-06 |
| **A wrong assignment, not just a missing one** (driver not signed off for the route or vehicle) | scheduler | 1 | Overlaps and unassigned are checked; qualification is not |
| **Leavers' accounts and shared logins** | security | 1 | Named users and audit; no explicit deactivation rule |
| **"Am I on the right train?" with no data** | traveller | 1 | Needs data; train identity is on ticket and boards |

## 3. Already covered — no action

Stale labelling and "as of" times · one consistent state across channels · immediate platform-change handling with
"changed from" · radio as the safety channel and read-back style confirmation of messages · audit and separation of duties ·
two-person approval for high-impact actions · per-device revocable identity and no secrets on public devices · position
authenticated, replay and plausibility checked · injectable clock, replayable simulator, correlation id across apps ·
measurable acceptance criteria · draft vs live timetables with effective dates · refund automatic after a railway cancellation ·
"label what is simulated" in the demo.

QA's five flows, unprompted, were: delay propagation, cross-app consistency, a stale or lost data source, offline sync without
loss or duplication, and role access. **Four of five match our E2E flows** (1, 3, 5, 2). Role access is covered by negative
tests inside E2E-3 and the permission suite, not as its own flow.

## 4. New or sharper findings

**Effort:** S under about 2 hours · M about half a day. **Rec:** Adopt = recommend · Wording = rule only · Could · Doc = document only.

| # | Finding | By | Today | Rec | Effort |
|---|---|---|---|---|---|
| D-01 | **Per-journey communication timeline** in the console: what was published to each channel (board, app, push, driver message) and when | controller, CS, station mgr, security, driver | Audit log and event history exist, not as one view | **Adopt** | S–M |
| D-02 | **Publish receipt** — "applied to 7 displays, 1 offline, 12 followers notified" — and proof a correction reached everyone | controller, scheduler | None | **Adopt** | M |
| D-03 | **Undo a mistake** once others have acted (fast, visible, one place) | controller | Reinstate before departure only | **Adopt** | S |
| D-04 | Reason codes are **by cause, never blame**; the record shows who decided | driver, inspector | Reason list exists | Wording | S |
| D-05 | Inspector mode shows **disruptions and cancellations on the journey** being inspected | inspector | Shows journey and stop | **Adopt** | S |
| D-06 | Cancellation **cause kept on the ticket history** as proof for later disputes | CS | History exists; cause not guaranteed | Wording | S |
| D-07 | **Shift handover note** (general, not only incidents) | controller | Incident handover only | Could | S |
| D-08 | Live lift / ramp / stair status | accessibility, traveller | Static facilities | **Adopt** (cheap, high value for one user group) | M |
| D-09 | **Alternatives** shown during a cancellation (next trains to the same destination) for staff and passengers | station mgr | Journey search exists | Could | S |
| D-10 | Walking time and route for a change of train; plain language; a language the passenger reads | traveller | Flat 5-minute transfer; one secondary language | Could | S |
| D-11 | Works with **no data**: boards and announcements are the main channel for some passengers | traveller | Displays planned | Doc (confirms displays stay) | — |
| D-12 | **User lifecycle**: deactivate leavers, review shared accounts, tamper-evident audit | security | User management; append-only audit | Wording | S |
| D-13 | Evidence **tied to a build**, acceptance criteria as measurable thresholds | QA | Traceability generated | Wording | S |
| D-14 | Speed restrictions and notices **reach the driver before the duty starts, and after sign-on** | driver | Notices on the duty; no restriction model | Doc | — |
| D-15 | Driver fatigue, route knowledge, rest rules | scheduler, driver | Out of scope | Doc | — |
| D-16 | Crowd build-up and evacuation routes | station mgr | Out of scope; a staff procedure | Doc | — |
| D-17 | **Assignment validity**: driver qualified for the route and vehicle type; a late change must not skip the check | scheduler | Overlap and unassigned checked | Could | M |
| D-18 | "Am I on the right train?" without data: service number and destination prominent on ticket and boards | traveller | Present on ticket and journey | Wording | S |
| D-19 | Test and demo environments can never send real notifications; one scenario clock so every screen shows the same "as of" time | QA, PO | Fake providers; one scenario clock | Wording | S |
| D-20 | Written scope and a change log against verbal requests | PO | Change log exists | Doc | — |

## 5. How the two rounds compare

| Validation-round finding | Discovery evidence | Effect |
|---|---|---|
| Staleness / confidently wrong | 12 of 12 unprompted | **Confirmed — the strongest signal** |
| Platform change is the worst failure | traveller, station mgr, commuter, accessibility | Confirmed |
| Publish receipt (NF-04) | controller and scheduler asked for proof of reach, unprompted | **Strengthened → D-02** |
| One-click undo (NF-03) | controller: "correct a mistake after others have acted" | **Strengthened → D-03** |
| Station manager wants dead-screen alerts | "found by walking past it" | Confirmed (device health) |
| Lift / step-free status as first-class (NF-01) | accessibility chose it as a story; traveller also asked for stairs and lift information | **Restored** — two personas, from their own stories |
| Inspector: refunded-ticket and photo-of-ticket checks (NF-22, NF-23) | dropped once the inspector chose their own stories; our probe had introduced them | **Weakened** — Could / document only |
| "My trips" saved regular trains (NF-10) | commuter wanted an early warning, not a setup flow | **Weakened → Could** |
| Station view on a phone (NF-02) | not raised unprompted | Weakened; still cheap |
| Terminal idle warning (NF-14) | accessibility raised time-outs | Kept |
| Inspection inside the driver app (NF-32) | **cannot be tested** — personas were not told about the apps | **Validation-only critique**; no discovery support |
| Calm offline indicator for the driver | driver says a signal gap barely affects the cab; the risk is "sent" but not received | Reframed: keep the app quiet, make message state truthful |
| Customer-service needs (verified caller, case log) | CS wanted "what each channel told passengers" | Folded into D-01 |
| Breadth is the biggest risk (T-6) | PO again, unprompted: "scope vs time for one engineer" | **Confirmed a third time** |

## 6. Tensions revisited

| # | Tension | Revised view |
|---|---|---|
| T-1 | Inspection inside the driver app | Not supported by discovery. Keep it a **role-gated mode** with its own sign-in prompt; do not spend the half-day on a separate entry point unless you want it |
| T-3 | Global Network Controller vs area scope | Security, unprompted: "one account doing everything" must never happen; wants two-person approval and time-limited elevation. Consistent with ASM-045's mitigations; the accepted risk stands |
| T-4 | Terminal vs boards | Traveller's main channel is **boards**, which stay. Terminal remains last in the build order |
| T-6 | Breadth | Open. Three independent signals now agree (feasibility check, validation PO, discovery PO) |

## 7. Recommended shortlist

By strength of evidence, not novelty:

1. **D-02 publish receipt** and **D-03 undo** — both unprompted by the controller, one by the scheduler. About 0.75 day together.
2. **D-01 communication timeline** — seven personas, small view over data we already store. S–M.
3. **D-05 inspector sees journey disruptions**, **D-04 / D-06 wording**. S.
4. **D-08 live lift status** — two personas, from their own stories. M.
5. **D-17 assignment validity** — a safety story from the scheduler; a Could unless time allows. M.
6. Everything else: wording, Could, or document only.

Total roughly **2 days** if all adopted; the first three items are about **1 day**.

## 8. Decisions needed

1. Adopt the shortlist, trim it, or leave all of it as proposals until the cross-check?
2. Which round is primary evidence in the submission? I suggest **discovery as primary**, validation as a documented comparison.
3. Run the Gemini / ChatGPT cross-check on controller, driver, product owner and security? I can write the prompts.
