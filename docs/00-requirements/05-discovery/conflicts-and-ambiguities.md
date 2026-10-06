# Conflicts and ambiguities

> Status values: `Proposed` (AI-drafted, pending review) → `Accepted` / `Changed` / `Rejected`.

"No explicit conflict identified" is not a credible finding for a system with several writers, several audiences
and unreliable connectivity. These are the tensions that shape the design. Each records the **losing option**, so
the choice can be revisited honestly.

## Conflicts

| ID | Conflict | Side A | Side B | Resolution | Reasoning | Losing option | Status |
|---|---|---|---|---|---|---|---|
| CON-01 | Realtime vs unreliable connectivity (§5 vs §16) | "Live" information everywhere | Trains, terminals and phones lose connection | Live where possible; every surface shows the age of what it shows and degrades to schedule-only with a label | A system that only works with perfect connectivity fails exactly when needed | Hiding stale data to keep screens "clean" | Proposed |
| CON-02 | Accuracy vs freshness | Show nothing until certain | Show the latest, even if old | Show the latest **with its age**; switch to Unknown at 300 s (ASM-019, ASM-020) | Passengers are better served by "as of 14:31" than by a blank | Blank screens on uncertainty | Proposed |
| CON-03 | Several writers vs one source of truth | Feed, driver and controller all assert facts | Passengers need one answer | Per-fact source precedence, losing value retained ([source-precedence.md](../04-domain/source-precedence.md)) | Disagreement is normal; an unwritten rule is not | Last-write-wins | Proposed |
| CON-04 | Speed of publishing vs correctness | Disruptions out immediately | Wrong network-wide messages cause mass harm | Single-scope disruptions publish immediately; network-wide needs a second approver; retraction is first-class | Proportionate control: friction only where the blast radius is large | Approval on every disruption | Proposed |
| CON-05 | Notification timeliness vs fatigue | Every change pushed now | One alert per meaningful change | Throttle routine delay changes (5 min, ≥5 min change); always send cancel, platform change, termination (ASM-022) | Spam trains people to ignore alerts — including the important one | Push every change | Proposed |
| CON-06 | Public access vs abuse protection | Information without sign-in (§14) | Anonymous endpoints are scraped and flooded | Anonymous, read-only, cached, rate-limited (REQ-SEC-002) | Keeps the public promise while bounding cost | Requiring accounts for departures | Proposed |
| CON-07 | Operational visibility vs driver privacy | Controllers want to see every train | Drivers object to being tracked | Position from the infrastructure feed; driver location only during a duty, never public, 7-day retention (REQ-DRV-010) | Meets the operational need without monitoring people | Continuous driver-phone tracking | Proposed |
| CON-08 | Platform change lead time vs operational flexibility | Station staff want warning | Controllers need to change late | Publish immediately; station manager sees it in the console at the same moment; "changed from" shown everywhere | A hold-back delay would leave passengers on the wrong platform | Delaying publication for staff | Proposed |
| CON-09 | Display density vs legibility at distance | Show everything | Readable from 8 m | Platform displays show 3 departures; concourse pages; detail moves to terminal and app | Different surfaces, different jobs (§6) | One board design everywhere | Proposed |
| CON-10 | Catering cut-off vs delay uncertainty | Provider needs a firm collection time | ETA keeps moving | Provider owns cut-off policy; SmartRail sends ETA changes as they happen; passenger notified | The provider bears the cost of its own policy; SmartRail supplies the facts | SmartRail computing catering cut-offs | Proposed |

## Ambiguities

| ID | Ambiguous phrase | Brief | Questions it raises | Interpretation adopted | Recorded as | Status |
|---|---|---|---|---|---|---|
| AMB-01 | "Current train locations **where appropriate**" | §3 | Which trains? Appropriate to whom? From what source? | All running passenger journeys; position shown as a schematic stop timeline, not a map; never for non-passenger movements | ASM-031, REQ-LIVE-015 | Proposed |
| AMB-02 | "**Accurate** information" | §2 | How accurate? How fresh? | Freshness is measurable (stale 60 s, Unknown 300 s); accuracy is expressed as confidence, never implied | REQ-LIVE-003, REQ-LIVE-004 | Proposed |
| AMB-03 | "**Live** train information" | §5 | What counts as live? | Within 5 s of an accepted update (p95) | REQ-NFR-001 | Proposed |
| AMB-04 | "**Upcoming** trains" | §3, §7 | How far ahead? | Next departures for the day on boards; 7 days for search | ASM-026 | Proposed |
| AMB-05 | Driver "**permitted to update**" | §8 | Update what, exactly? | Stop events, skip with reason, delay reason, request assistance. Never cancellations or timetable. | REQ-DRV-003 | Proposed |
| AMB-06 | "**Station operation** application" (user wording) vs §6, §7, §9 | — | A separate app, or a scoped view? | A role-scoped view of the ops console | ASM-010 | Pending confirmation |
| AMB-07 | "Disruption information **propagates**" | §10 | To whom, how fast, how is it withdrawn? | To every surface showing an affected journey or station within 5 s; retraction propagates the same way | CF-04 | Proposed |
| AMB-08 | "Users may need to receive information when important events occur" | §13 | Which events, which users, which channel? | The notification matrix | [notification-matrix.md](../06-requirements/notification-matrix.md) | Proposed |
| AMB-09 | "**Available connections** from another station" | §3 | Journey planning — how many changes? | Direct and one-transfer, minimum transfer time 5 min | REQ-PAX-006, ASM-025 | Proposed |
| AMB-10 | "**Station** information" | §7 | What information? | Facilities, step-free access, disruptions affecting the station | REQ-TRM-002 | Proposed |
