# Edge-case catalogue

Built by walking through a railway day and asking, at every step, *what goes wrong here?* — then checking each
answer against the five applications.

**Depth:** `Registered` (identified, behaviour stated) · `Specified` (Given/When/Then in
[specified.md](specified.md), will be tested) · `Tested` (automated test exists — update as tests land).

**Severity:** High — wrong information reaches passengers or a safety-adjacent decision · Medium — degraded
experience or operational confusion · Low — inconvenience.

Apps: PAX passenger · DRV driver · DSP display · TRM terminal · OPS console · BE backend · EXT catering.

---

## A. Planning and timetable (EDG-001 – 009)

| ID | Scenario | Expected behaviour | Apps | Sev | Requirements | Sim | Depth |
|---|---|---|---|---|---|---|---|
| EDG-001 | Timetable changes while journeys from the old version are running | Running and generated journeys untouched; new version applies from its effective date | BE, OPS | High | REQ-LIVE-017 | — | Registered |
| EDG-002 | Service departs 23:50, arrives 00:35 | Belongs to the earlier service date; still on the previous day's board at 00:10 | BE, PAX, DSP | High | REQ-LIVE-009 | SIM-13 | **Specified** |
| EDG-003 | Public holiday / calendar exception | No journey generated for excluded dates; special services only where defined | BE | Medium | REQ-LIVE-008 | — | Registered |
| EDG-004 | Special or relief train added on the day | Manual journey creation; appears on boards and search like any other | OPS, BE | Low | REQ-LIVE-008 | — | Registered |
| EDG-005 | Wrong timetable version published | Publish a corrected version; bad version superseded, never edited; approver ≠ author prevents most cases | OPS | High | REQ-OPS-003 | — | Registered |
| EDG-006 | Journey starts at an intermediate station (short working) | Origin is the first stop of that journey; passengers at earlier stations never see it | BE, PAX | Low | REQ-LIVE-008 | — | Registered |
| EDG-007 | Pick-up-only or set-down-only stop | Marked; not offered as a destination (or origin) where invalid | PAX, TRM | Low | REQ-PAX-006 | — | Registered |
| EDG-008 | Passing point (train passes, does not stop) | Present in working data, hidden from passenger surfaces | BE, PAX | Low | identity-model | — | Registered |
| EDG-009 | Same train number runs tomorrow / changes at terminus | Distinct journey ids; following today's never follows tomorrow's | BE, PAX | Medium | REQ-LIVE-019 | — | Registered |

## B. Allocation, crew and sign-on (EDG-010 – 019)

| ID | Scenario | Expected behaviour | Apps | Sev | Requirements | Sim | Depth |
|---|---|---|---|---|---|---|---|
| EDG-010 | Vehicle enters planned maintenance; future journeys lose it | Alert at T-24h, cancellation candidate at T-2h | OPS, BE | High | REQ-LIVE-011, REQ-OPS-012 | — | Registered |
| EDG-011 | Driver unavailable (sick, no-show) | Same alert path; cancellation reason "crew unavailable" | OPS | High | REQ-LIVE-011 | — | Registered |
| EDG-012 | Driver signs on with no network, cache from yesterday | "Duty not current" — yesterday's duty never shown as today's | DRV | Medium | REQ-DRV-002 | — | Registered |
| EDG-013 | Unsent events from a previous duty found at sign-on | Surfaced for review as orphaned; not applied to the new duty | DRV, BE | Medium | REQ-DRV-008 | — | Registered |
| EDG-014 | Driver assigned to overlapping journeys | Assignment rejected | OPS | Medium | REQ-LIVE-010 | — | Registered |
| EDG-015 | Driver device lost or shared | Short session, re-auth, remote revoke; previous user's cache cleared | DRV | Medium | REQ-DRV-001 | — | Registered |
| EDG-016 | Vehicle fails; its later journeys in today's rotation are affected | All later journeys flagged "no vehicle" within 60 s | BE, OPS | High | REQ-LIVE-018 | SIM-06 | **Specified** |
| EDG-017 | Journey still unassigned at T-2h | Cancellation candidate; controller alerted | OPS | High | REQ-LIVE-011 | — | Registered |

## C. Pre-departure and departure (EDG-020 – 029)

| ID | Scenario | Expected behaviour | Apps | Sev | Requirements | Sim | Depth |
|---|---|---|---|---|---|---|---|
| EDG-020 | Platform not yet assigned | "Platform —" with accessible "to be announced" | PAX, DSP, TRM | Low | REQ-LIVE-014 | — | Registered |
| EDG-021 | Platform changed after boarding started | Published immediately; "changed from"; urgent push; station manager highlighted | all | High | REQ-LIVE-014 | SIM-14 | **Specified** |
| EDG-022 | Two journeys assigned the same platform overlapping | Warning naming the other journey; saved | OPS | Medium | REQ-LIVE-020 | — | Registered |
| EDG-023 | Vehicle fails inspection at origin | Substitute (delay), or cancel; reason recorded | OPS | Medium | REQ-OPS-004 | — | Registered |
| EDG-024 | Train ready early | Departure never shown earlier than scheduled | PAX, DSP | Medium | REQ-LIVE-012 | — | Registered |
| EDG-025 | Held for a late connecting train | Delay with reason "held for connection"; controller decision | OPS | Low | REQ-OPS-004 | — | Registered |
| EDG-026 | Cancelled, then reinstated before departure | Reinstatement pushed to followers; boards restore the row | all | Medium | REQ-OPS-004 | — | Registered |
| EDG-027 | Inbound vehicle late, so a Planned journey will leave late | "Expected 14:30 (+5)" while still Planned, confidence Live | PAX, DSP | Medium | REQ-LIVE-002 | — | Registered |

## D. En route (EDG-030 – 039)

| ID | Scenario | Expected behaviour | Apps | Sev | Requirements | Sim | Depth |
|---|---|---|---|---|---|---|---|
| EDG-030 | Breakdown between stations | `Interrupted`; position frozen; ETA "no estimate", never "+n" | all | High | REQ-LIVE-013 | SIM-06 | **Specified** |
| EDG-031 | Feed silent for one journey | Stale at 60 s, Unknown at 300 s; schedule shown with label | all | High | REQ-LIVE-004 | SIM-05 | **Specified** |
| EDG-032 | Feed and driver both silent | Unknown; never extrapolated; ops alerted | all | High | REQ-LIVE-004 | SIM-10 | Registered |
| EDG-033 | Rescue locomotive replaces failed vehicle mid-journey | Same journey id; second time-ranged assignment; continuous history | BE, PAX | High | REQ-LIVE-010 | SIM-07 | **Specified** |
| EDG-034 | Controller decides to skip a stop | Stop `Skipped`; followers at that stop pushed; displays there updated | all | Medium | REQ-LIVE-016 | SIM-03 | Registered |
| EDG-035 | Unscheduled stop (signal, passenger incident) | Shown as delay; no new stop added to the public timeline | BE | Low | REQ-LIVE-002 | — | Registered |
| EDG-036 | Terminated short | Later stops cancelled; message names the last stop | all | High | REQ-OPS-004 | SIM-04 | Registered |
| EDG-037 | Collision, derailment or serious accident | Incident manager declares; `TerminatedEmergency`; templates only; legal hold | all | High | REQ-OPS-008 | SIM-17 | **Specified** |
| EDG-038 | Power or signal failure blocks a section | Section-scoped disruption resolves to all affected journeys | all | High | REQ-LIVE-016 | SIM-15 | Registered |
| EDG-039 | Temporary speed restriction or weather | Modelled as delay on the section via disruption; no special model | BE | Low | REQ-LIVE-016 | — | Registered |

## E. Driver app and control communication (EDG-040 – 050)

| ID | Scenario | Expected behaviour | Apps | Sev | Requirements | Sim | Depth |
|---|---|---|---|---|---|---|---|
| EDG-040 | Driver forgets to record stop events | No passenger impact — the feed covers position and timing | DRV, BE | Low | REQ-LIVE-006 | — | Registered |
| EDG-041 | Driver taps Departed twice | Idempotent; one event | DRV, BE | Low | REQ-DRV-006 | — | Registered |
| EDG-042 | Driver device clock wrong | Accepted with lower confidence; ordering by sequence and server time | DRV, BE | Medium | REQ-LIVE-006 | SIM-16 | Registered |
| EDG-043 | Driver reports a skip nobody planned | Recorded as reported; controller alerted; public only after confirmation or feed evidence | DRV, OPS | Medium | REQ-DRV-003 | — | Registered |
| EDG-044 | Driver app offline mid-journey | Explicit banner, last sync, pending count; actions queued; passengers unaffected | DRV | High | REQ-DRV-005 | SIM-09 | **Specified** |
| EDG-045 | App killed while offline with queued events | Queue persisted; replays on next launch | DRV | High | REQ-DRV-005 | SIM-09 | Registered |
| EDG-046 | Replay arrives after the feed recorded the same event | Feed time authoritative; driver time and reason in history | BE | Medium | REQ-DRV-006, REQ-LIVE-006 | SIM-09 | **Specified** |
| EDG-047 | Handover: queued events replay after the next driver signs on | Events attach to the original duty | DRV, BE | Medium | REQ-DRV-009 | — | Registered |
| EDG-048 | Replayed events fall outside the duty window | Flagged; not applied silently | BE | Medium | REQ-DRV-006 | — | Registered |
| EDG-049 | Control message sent while driver offline | Stays `Sent`; controller sees unacknowledged; driver warned on reconnect | DRV, OPS | High | REQ-DRV-007 | SIM-09 | **Specified** |
| EDG-050 | Request assistance: accidental tap, or offline | Confirm step; offline queued urgent with "use radio" prompt | DRV, OPS | High | REQ-DRV-003 | — | Registered |

## F. Telemetry and data quality (EDG-051 – 058)

| ID | Scenario | Expected behaviour | Apps | Sev | Requirements | Sim | Depth |
|---|---|---|---|---|---|---|---|
| EDG-051 | Duplicate telemetry | No state change; counted | BE | Medium | REQ-LIVE-005 | SIM-11 | Registered |
| EDG-052 | Out-of-order / late telemetry | Older than latest accepted → rejected with reason | BE | Medium | REQ-LIVE-005 | SIM-11 | Registered |
| EDG-053 | Implausible jump or wrong-line drift | Rejected "implausible"; does not move the train | BE | High | REQ-LIVE-005 | SIM-11 | **Specified** |
| EDG-054 | Movement with no matching journey (empty stock, test run) | Never public; ops list only | BE, OPS | Medium | REQ-LIVE-015 | SIM-12 | Registered |
| EDG-055 | Scheduled journey with no feed in its active window | Flagged to ops; degrades to Unknown | BE, OPS | Medium | REQ-LIVE-015 | SIM-12 | Registered |
| EDG-056 | Unknown or forged feed source | 401; counted; never applied | BE | High | REQ-SEC-004 | — | Registered |
| EDG-057 | Later stop confirmed, earlier stop never was | Earlier stop resolved as passed, confidence Unknown; never `Pending` forever | BE | Medium | REQ-LIVE-001 | SIM-11 | Registered |
| EDG-058 | Two sources disagree on position | Plausibility rejects the impossible one; both recorded with source id | BE | Medium | REQ-LIVE-006 | — | Registered |

## G. Passenger side (EDG-060 – 067)

| ID | Scenario | Expected behaviour | Apps | Sev | Requirements | Sim | Depth |
|---|---|---|---|---|---|---|---|
| EDG-060 | Phone offline in a tunnel | Cached data with age; nothing marked live | PAX | Medium | REQ-PAX-010 | — | Registered |
| EDG-061 | App and station board disagree | Impossible by design (one state); if observed it is a high-severity defect | PAX, DSP | High | system-context | — | Registered |
| EDG-062 | First-leg delay makes a connection unsafe | "At risk" with next alternative | PAX, TRM | Medium | REQ-PAX-006 | SIM-02 | Registered |
| EDG-063 | Second leg of an itinerary cancelled | Itinerary marked broken; alternatives | PAX | Medium | REQ-PAX-006 | — | Registered |
| EDG-064 | 200% text or screen reader with live updates | Layout intact; updates announced politely, list does not jump | PAX | Medium | REQ-PAX-014 | — | Registered |
| EDG-065 | Passenger follows the wrong day's train | Journey date shown prominently on follow | PAX | Low | REQ-LIVE-019 | — | Registered |
| EDG-066 | Passenger boards an express not stopping at their station | Journey detail shows "does not stop at …" clearly | PAX | Medium | REQ-PAX-003 | — | Registered |
| EDG-067 | Station temporarily closed | Closure notice; stops skipped; terminal shows alternatives | all | High | REQ-LIVE-016 | SIM-15 | Registered |

## H. Notifications (EDG-070 – 075)

| ID | Scenario | Expected behaviour | Apps | Sev | Requirements | Sim | Depth |
|---|---|---|---|---|---|---|---|
| EDG-070 | Many delay updates in a short window | One push per 5 min per journey for routine changes | PAX | Medium | REQ-PAX-007 | SIM-02 | **Specified** |
| EDG-071 | Push provider down or token expired | Inbox still updated; retry once; token cleanup | PAX | Medium | REQ-PAX-015 | — | Registered |
| EDG-072 | Disruption retracted after a push | Correction push to everyone notified | PAX | High | REQ-PAX-009 | — | **Specified** |
| EDG-073 | Followed journey cancelled | Immediate push with reason and link to alternatives | PAX | High | REQ-PAX-007 | SIM-08 | Registered |
| EDG-074 | Planned works affect a followed journey tomorrow | One push the day before; "planned" label | PAX | Low | REQ-OPS-016 | — | Registered |
| EDG-075 | Interrupted then resumed | One push for each; none in between | PAX | Medium | REQ-PAX-007 | SIM-06 | Registered |

## I. Stations — displays and terminals (EDG-080 – 088)

| ID | Scenario | Expected behaviour | Apps | Sev | Requirements | Sim | Depth |
|---|---|---|---|---|---|---|---|
| EDG-080 | Display or terminal loses network | Last board with age at 60 s; scheduled-only at 300 s; never live markers | DSP, TRM | High | REQ-DSP-004, REQ-TRM-005 | — | **Specified** |
| EDG-081 | Display clock wrong | Countdown from server offset; unaffected | DSP | Medium | REQ-DSP-006 | — | Registered |
| EDG-082 | Display dead and nobody notices | Offline in device health at 90 s, visible to station manager | DSP, OPS | High | REQ-DSP-005 | — | Registered |
| EDG-083 | Hundreds of displays reconnect at once | Jittered backoff; snapshot then delta | DSP, BE | Medium | REQ-NFR-007 | — | Registered |
| EDG-084 | Display token revoked | Rejected on next request | DSP | Medium | REQ-DSP-003 | — | Registered |
| EDG-085 | Terminal user walks away mid-search | Reset and cleared at 60 s | TRM | Medium | REQ-TRM-003 | — | Registered |
| EDG-086 | Attempt to escape the terminal kiosk | No reachable navigation out | TRM | High | REQ-TRM-004 | — | Registered |
| EDG-087 | More departures than rows | Paging on concourse; platform display shows next 3 only | DSP | Low | REQ-DSP-002 | — | Registered |
| EDG-088 | Station closure mode | Full-screen closure notice on that station's displays | DSP, TRM | High | REQ-LIVE-016 | SIM-15 | Registered |

## J. Operations (EDG-090 – 099)

| ID | Scenario | Expected behaviour | Apps | Sev | Requirements | Sim | Depth |
|---|---|---|---|---|---|---|---|
| EDG-090 | Two controllers edit the same journey | Second save rejected with current version | OPS | Medium | REQ-OPS-010 | — | Registered |
| EDG-091 | Disruption published in error | Retract; correction push; both recorded | OPS, all | High | REQ-OPS-005 | — | Registered |
| EDG-092 | Network-wide alert at 03:00 with no second approver | Publish route-scoped disruptions instead (no approval needed) | OPS | Medium | REQ-OPS-005 | — | Registered |
| EDG-093 | Disruption left active past review-by | Expired; owner alerted | OPS | Medium | REQ-OPS-006 | — | Registered |
| EDG-094 | Incident outlives the owner's shift | Ownership must be handed over with a note | OPS | Medium | REQ-OPS-007 | — | Registered |
| EDG-095 | Cancellation attempted without reason | Rejected | OPS | Low | REQ-OPS-004 | — | Registered |
| EDG-096 | System administrator attempts an operational action | 403 — separation of duties | OPS | High | REQ-OPS-001 | — | Registered |
| EDG-097 | Station manager edits another station | 403 — scoped permission (IDOR) | OPS | High | REQ-SEC-003 | — | **Specified** |
| EDG-098 | Recorded time later found wrong | Authorised correction; original kept; reports use corrected | OPS | Low | REQ-OPS-014 | — | Registered |
| EDG-099 | Console loses connection mid-action | Banner; writes disabled; no "it worked" without confirmation | OPS | Medium | REQ-NFR-006 | — | Registered |

## K. Platform and backend (EDG-100 – 105)

| ID | Scenario | Expected behaviour | Apps | Sev | Requirements | Sim | Depth |
|---|---|---|---|---|---|---|---|
| EDG-100 | Database unavailable | Reads from cache where possible; writes rejected clearly | BE | High | REQ-NFR-006 | — | Registered |
| EDG-101 | Redis (live state) unavailable | Estimates degrade to Scheduled; core reads continue | BE | High | REQ-NFR-006 | — | Registered |
| EDG-102 | WebSockets blocked | Long-poll fallback | BE | Medium | REQ-NFR-007 | — | Registered |
| EDG-103 | Traffic spike during major disruption | Cached reads; push fan-out rather than polling | BE | Medium | REQ-NFR-002 | — | Registered |
| EDG-104 | Deploy during operations | Documented rolling deploy; not built | BE | Low | — | — | Registered |
| EDG-105 | Script or markup in disruption text | Rendered as plain text everywhere | all | High | REQ-SEC-005 | — | Registered |

## L. External catering (EDG-110 – 116)

| ID | Scenario | Expected behaviour | Apps | Sev | Requirements | Sim | Depth |
|---|---|---|---|---|---|---|---|
| EDG-110 | Provider down | Outbox retains; retry with backoff; DLQ; railway unaffected | BE, EXT | Medium | REQ-INT-004 | — | **Specified** |
| EDG-111 | Duplicate webhook | No-op | BE | Low | REQ-INT-003 | — | Registered |
| EDG-112 | Bad signature or outside replay window | Rejected; counted | BE | Medium | REQ-INT-003 | — | Registered |
| EDG-113 | Train delayed with an order outstanding | ETA change sent; provider adjusts; passenger notified | BE, EXT, PAX | Medium | REQ-INT-002 | SIM-02 | Registered |
| EDG-114 | Train cancelled / collection stop skipped with order outstanding | Event sent; provider applies its policy; passenger notified | BE, EXT, PAX | Medium | REQ-INT-002 | SIM-08 | Registered |
| EDG-115 | Stock-out or price change at confirm | Clear rejection; no half-state | PAX, EXT | Low | REQ-INT-005 | — | Registered |
| EDG-116 | Order events out of order | Tolerated; recorded; final state consistent | BE | Low | REQ-INT-003 | — | Registered |

---

**Totals:** 100 edge cases · 16 specified · 0 tested (update as tests land).
