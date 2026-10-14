# Notification matrix

Brief §13 is a single sentence: *different users may need to receive information when important events occur.*
This matrix is what that sentence has to mean before it can be built (REQ-NFR-008).

## Principles

1. **A notification is a pointer, never the source of truth.** It says what changed and sends you to where the
   current state lives (REQ-PAX-008). Pushes are late, lost or batched by the OS; the app is authoritative.
2. **Routine changes are throttled; consequential ones are not.** At most one routine delay push per followed
   journey per 5 minutes, and only when expected arrival moves by 5 minutes or more. Cancellation, platform change,
   termination and retraction are always sent immediately (ASM-022, CON-05).
3. **Every push is also in the in-app inbox** (REQ-PAX-015).
4. **Silence is a state.** Losing live data does not push passengers — it would be noise — but it is shown
   in-app and alerted to operations.
5. **Corrections are as visible as the mistake.** A retracted disruption that was pushed gets a correction push.

## Passenger-facing events

| Event | Followers (push) | Passenger app (in-app) | Displays | Terminal | Catering | Throttle |
|---|---|---|---|---|---|---|
| Expected arrival changes by ≥ 5 min | ✓ "Now expected 14:32 (+7)" | ✓ live | ✓ live | ✓ live | ✓ if journey has orders | 1 per 5 min per journey |
| Expected arrival changes by < 5 min | ✗ | ✓ live | ✓ live | ✓ live | ✓ if orders | — |
| Cancelled | ✓ immediate, with reason | ✓ banner | ✓ "Cancelled" 15 min | ✓ | ✓ immediate | none |
| Reinstated after cancellation | ✓ immediate | ✓ | ✓ | ✓ | ✓ | none |
| Platform changed | ✓ immediate "Platform 5 (was 3)" | ✓ highlighted | ✓ "changed from" | ✓ | ✗ | none |
| Platform assigned (from TBA) | ✗ | ✓ | ✓ | ✓ | ✗ | — |
| Terminated short | ✓ immediate, names last stop | ✓ | ✓ at remaining stops | ✓ | ✓ | none |
| Interrupted (stopped, no estimate) | ✓ once | ✓ "Stopped, no estimate" | ✓ "Delayed — no estimate" | ✓ | ✓ | once per interruption |
| Resumed after interruption | ✓ once | ✓ | ✓ | ✓ | ✓ | once |
| Stop skipped (affects follower's stop) | ✓ immediate | ✓ | ✓ at that stop | ✓ | ✓ if collection stop | none |
| Live data lost (Unknown) | ✗ | ✓ "No live data" | ✓ banner | ✓ banner | ✗ | — |
| Disruption published affecting followed journey | ✓ if severity ≥ Major | ✓ banner | ✓ if station in scope | ✓ | ✗ | 1 per disruption |
| Disruption updated | ✗ (in-app only) unless severity rises | ✓ | ✓ | ✓ | ✗ | — |
| Disruption retracted (after a push) | ✓ **correction** | ✓ "Correction" | ✓ removed | ✓ removed | ✗ | none |
| Planned works affecting followed journey | ✓ day before, once | ✓ "Planned" | ✓ from start | ✓ | ✗ | once |
| Emergency termination | ✓ approved template only | ✓ template | ✓ template | ✓ template | ✓ | none |

## Operational events

| Event | Audience | Channel | Urgency |
|---|---|---|---|
| Driver requests assistance | Network controller on duty | Console alert, audible | High |
| Control message to driver | Driver | Driver app (in-app + device notification) | As set; delivery and acknowledgement tracked |
| Message unacknowledged after its validity | Sending controller | Console alert | Medium |
| Journey has no vehicle / driver at T-24h | Scheduler | Console alert | Medium |
| Journey still unassigned at T-2h | Scheduler + network controller | Console alert, "cancellation candidate" | High |
| Vehicle marked unavailable | Scheduler | Console list of affected journeys | Medium |
| Journey confidence → Unknown | Network controller | Console highlight | Medium |
| Phantom movement / ghost journey | Network controller | Console list | Low |
| Unplanned skip reported by driver | Network controller | Console alert | Medium |
| Display, terminal or gate scanner offline (90 s) | Station Master (own station), network controller | Console device health | Medium |
| Platform change at a station | Station Master of that station | Console highlight at publish time | High |
| Network-wide disruption awaiting approval | Second approver role holders | Console queue | High |
| Disruption review-by passed | Disruption owner | Console "Expired — review" | Medium |
| Critical incident declared | Network controllers | Console alert | Critical |
| Excess fare due at an exit scan | Shown on the gate screen; listed for the Station Master | Gate screen and console list | Medium |
| Timetable version awaiting approval | Schedulers other than the author | Console queue | Low |
| Feed lag, dead-letter queue, error rate | System owner (me) | Observability alerts | Varies |

## Not in MVP

SMS and email channels (deferred, plan.md §3.2), audio announcements, quiet hours beyond a simple setting.

## Ticket events (added 2026-10-06)

| Event | Holder (push) | In-app | Throttle |
|---|---|---|---|
| Ticket issued | ✗ (shown on screen) | ✓ inbox | — |
| Ticket becomes Affected | ✓ immediate: "Your ticket … is affected. Refund available." | ✓ | none |
| Refund REQUESTED / PROCESSING | ✗ | ✓ status | — |
| Refund REFUNDED | ✓ once: "Refunded LKR …" | ✓ | once |
| Refund FAILED (will retry) | ✗ | ✓ "being retried" | — |
| Automatic refund (no ticket could be issued) | ✓ immediate with the reason | ✓ | none |
| Platform change on a held journey | ✓ immediate | ✓ | none (existing rule) |

Operations: a refund still FAILED after its automatic retries is counted on the network controller's read-only refund view.
