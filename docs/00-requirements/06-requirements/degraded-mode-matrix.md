# Degraded-mode matrix

Brief §16 asks how applications behave when connectivity is unreliable. The answer has to be specific per
application, because each fails differently and each has a different worst case (REQ-NFR-006).

**The rule behind every row:** degrade *visibly*. A screen that quietly keeps showing old information is worse than
one that says it is offline.

Thresholds: stale at 60 s (ASM-019), Unknown at 300 s (ASM-020), device offline at 90 s (ASM-029).

## Client applications

| Application | Caches | Cache life | When stale (60 s+) | When offline / Unknown | Refuses to do | Worst case it prevents |
|---|---|---|---|---|---|---|
| **Passenger app** | Saved stations' boards, followed journeys, station list, timetable for saved routes | 24 h, labelled with age | "as of hh:mm" on every live value | Last data with age; "No live data — showing timetable"; pull-to-refresh retries | Follow/unfollow (queued, not lost); nothing claims "live" | A commuter trusting a 20-minute-old "on time" |
| **Driver app** | Today's duty, routes, stops, scheduled times, notices as of last sync | Duty length | Banner "last synced hh:mm" | Explicit offline banner; actions queue as events with device time, sequence, idempotency key; pending count shown; "messages may be missing since hh:mm" | Nothing — the driver can always record what happened | A driver acting on an instruction that never arrived, unaware |
| **Platform display** | Last board snapshot | Until reconnect | "Updated hh:mm" banner | After 300 s: scheduled times only, "Live information unavailable — listen for announcements"; no live markers | Showing expected times as live | A platform crowd waiting for a train that has been cancelled |
| **Concourse display** | Last board + disruptions | Until reconnect | As platform | As platform; disruptions retained with their published time | As platform | As platform |
| **Station terminal** | Station info, facilities, timetable | 24 h | "as of" on live values | Timetable-only mode; banner; live search columns hidden | Live-only features (follow, live status) | A visitor planning around live data that isn't live |
| **Ops console** | Nothing persistent (desk, wired) | — | Per-journey age shown in the board | Connection-lost banner; actions disabled until reconnected | Any write while disconnected (no offline writes for high-consequence actions) | A controller cancelling a train believing it worked |

## Backend and integrations

| Failure | Detection | Behaviour | Visible to |
|---|---|---|---|
| Train detection feed silent | No accepted update per journey | 60 s stale, 300 s Unknown; ETAs revert to schedule | Everyone, as labels; ops as an alert |
| Feed sending bad data | Plausibility / sequence checks | Rejected with reason; counters exposed | Ops metrics |
| Realtime channel blocked | Connection failure | Automatic long-poll fallback (REQ-NFR-007) | Nobody, if it works |
| Realtime reconnect storm (many displays) | Mass reconnect | Jittered backoff; snapshot then delta | Nobody |
| Database unavailable | Health check | Read endpoints served from cache where possible; writes rejected with a clear error | Ops; passengers see "as of" |
| Redis (live state) unavailable | Health check | Live estimates unavailable → Scheduled confidence; core reads continue from database | Everyone, as labels |
| Push provider failing | Delivery errors | Retry once; inbox remains (REQ-PAX-015) | Passengers via inbox |
| Catering provider down | Outbox failures | Events retained, retried with backoff, dead-lettered after limit; railway unaffected (REQ-INT-004) | Ops metrics |

## How this is verified

Each row is backed by a simulator scenario or an E2E test:

| Row | Verified by |
|---|---|
| Feed silent | SIM-05 telemetry blackout; E2E flow 5 |
| Driver offline | SIM-09 driver offline; E2E flow 2 |
| Display offline | E2E flow 5 (network cut to the kiosk) |
| Catering down | E2E flow 4 (forced provider failure) |
| Realtime blocked | Integration test with WebSockets disabled |
