# CF-04 — Disruption: publish, update, retract

**Scenario:** Signalling failure between Mill Lane and Hill Town. The controller publishes a route-scoped
disruption, updates it twice, then resolves it. In a second variant, a disruption is published against the wrong
route and must be **retracted**.

**Requirements:** REQ-OPS-005, REQ-OPS-006, REQ-LIVE-016, REQ-PAX-009, REQ-SEC-005 · **Test:** part of E2E-3

## Publish → update → resolve

| Time | Actor | What happens | Passenger app | Concourse display | Terminal | Followers |
|---|---|---|---|---|---|---|
| 09:02 | Controller | Creates disruption: type *signalling*, severity *Major*, scope *section Mill Lane–Hill Town*, message, review-by 10:00 | — | — | — | — |
| 09:03 | Console | Preview shows the rendering on each surface | — | — | — | — |
| 09:03 | Controller | Publish (section scope → no second approver needed) | — | — | — | — |
| 09:03 | Backend | Scope resolved to 6 journeys and 2 stations (REQ-LIVE-016) | Banner on affected journeys and stations | Disruption banner | Disruption listed | Push to followers of the 6 journeys (severity Major) |
| 09:25 | Controller | Update: "expect delays of up to 20 minutes" | Banner updated, "updated 09:25" | Updated | Updated | No push (in-app only unless severity rises) |
| 09:58 | Console | Review-by approaching → reminder to owner | — | — | — | — |
| 10:10 | Controller | Resolve | Banner removed | Removed | Removed | — |

## The retraction variant

| Time | Actor | What happens | Followers |
|---|---|---|---|
| 09:03 | Controller | Publishes the disruption against the **wrong route** | Followers of 4 unaffected journeys pushed "Major disruption" |
| 09:06 | Controller | Notices; **Retract** with reason "published in error" | — |
| 09:06 | Backend | Disruption → `Retracted`; removed from every surface | **Correction push:** "Please ignore the earlier disruption alert for route X — it was sent in error." |
| 09:06 | Audit | Publication and retraction both recorded | — |

Retraction is not the same as resolution: "resolved" says the problem is over; "retracted" says it never existed.
A passenger who was told something wrong must be told it was wrong — quietly removing the banner leaves them
believing the first message.

## Network-wide variant

A disruption with network scope stays `Draft` until a **second** authorised person approves (REQ-OPS-005). If no
second approver is available, the controller publishes route-scoped disruptions instead, which need no approval —
the rule adds friction only where the blast radius is largest, and never blocks communication entirely.

## Failure branches

| Branch | Behaviour |
|---|---|
| Message contains HTML or script | Rendered as literal text everywhere (REQ-SEC-005) |
| Review-by passes without action | Disruption → `Expired`; listed for review; still shown as published (REQ-OPS-006) |
| Display offline during publish | Shows last board with its age; receives the disruption on reconnect via snapshot |
| Two controllers edit the same disruption | Optimistic concurrency conflict |
