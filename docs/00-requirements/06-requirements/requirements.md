# Refined requirements

> Seeded 2026-10-05 from the discovery work. **This file is now the source of truth — edit it directly.**
> Every seeded row starts as `Proposed` (AI-drafted). Review each one and change it to `Accepted`, `Changed` or `Rejected`. The count of reviewed rows is evidence for brief §21.

Every requirement here is written to be **testable**. Where a number was needed and the brief gave none, the number is an assumption with its own ID (listed under *Assumptions*), so it can be changed in one place and the reasoning is never buried.

## Conventions

| Field | Values |
|---|---|
| **ID** | `REQ-<AREA>-nnn`. IDs are never reused; a removed requirement is marked `Rejected`, not deleted. |
| **Priority** | MoSCoW: `Must` / `Should` / `Could` / `Won't`. |
| **MVP** | `Yes` (built fully) · `Thin` (built minimally, enough to demonstrate) · `No` (documented only). |
| **Status** | `Proposed` → `Accepted` / `Changed` / `Rejected`. |
| **Sources** | Discovery findings that produced it ([discovery-register.md](../05-discovery/discovery-register.md)). |

## Index

| Area | Requirements | Must | Should | Could | MVP thin |
|---|---|---|---|---|---|
| [Live operations (backend)](#live) | 20 | 14 | 5 | 1 | 2 |
| [Passenger app](#pax) | 15 | 11 | 4 | 0 | 0 |
| [Driver app](#drv) | 10 | 7 | 3 | 0 | 1 |
| [Station display](#dsp) | 8 | 5 | 3 | 0 | 0 |
| [Station terminal](#trm) | 5 | 2 | 3 | 0 | 0 |
| [Operations console](#ops) | 17 | 9 | 6 | 2 | 5 |
| [Security](#sec) | 6 | 5 | 1 | 0 | 1 |
| [Data and privacy](#data) | 3 | 3 | 0 | 0 | 0 |
| [Platform / non-functional](#nfr) | 8 | 6 | 2 | 0 | 0 |
| [Simulator](#sim) | 5 | 4 | 1 | 0 | 0 |
| [External catering integration](#int) | 5 | 0 | 0 | 5 | 5 |
| **Total** | **102** | **66** | **28** | **8** | **14** |

<a id="live"></a>

## Live operations (backend)

#### REQ-LIVE-001 — Journey lifecycle states

- **Type:** Functional · **Priority:** Must · **MVP:** Yes · **Status:** Proposed
- **Requirement:** Every Journey has exactly one lifecycle state from: Planned, Ready, Running, Completed, Cancelled, TerminatedShort, Interrupted, TerminatedEmergency. Transitions not in the state table are rejected and logged.
- **Rationale:** Removes ambiguity about whether a train is cancelled, not started or finished.
- **Acceptance:** Unit tests cover every legal and illegal transition in the table; an illegal transition returns a modelled error with a reason.
- **Sources:** DF-045

#### REQ-LIVE-002 — Punctuality separate from lifecycle

- **Type:** Functional · **Priority:** Must · **MVP:** Yes · **Status:** Proposed
- **Requirement:** Punctuality is a separate attribute from lifecycle: a signed delta in minutes against schedule plus a band (Early, OnTime, MinorDelay, MajorDelay).
- **Rationale:** Delay is not a state; separating it expresses 'cancelled while late' and 'late with data lost'.
- **Acceptance:** A Running journey 8 minutes behind reports delta=+8 and band=MinorDelay; the lifecycle value is unchanged.
- **Sources:** DF-046
- **Assumptions:** ASM-021

#### REQ-LIVE-003 — Source and confidence on every estimate

- **Type:** Functional · **Priority:** Must · **MVP:** Yes · **Status:** Proposed
- **Requirement:** Every time estimate carries a source (Confirmed, Live, Scheduled) and the journey exposes a data confidence (Confirmed, Live, Scheduled, Unknown).
- **Rationale:** ETA must not imply more precision than exists.
- **Acceptance:** API responses for any estimate include source and confidence; a test asserts none can be null.
- **Sources:** DF-047, DF-068

#### REQ-LIVE-004 — Stale and Unknown thresholds

- **Type:** Functional · **Priority:** Must · **MVP:** Yes · **Status:** Proposed
- **Requirement:** A Running journey with no accepted update for 300 seconds becomes Unknown: ETAs revert to scheduled times and every client states that live data is unavailable. At 60 seconds the data is marked stale with its age.
- **Rationale:** The platform must never silently interpolate a train that may have stopped.
- **Acceptance:** Simulator blackout scenario: stale marker at 60s +/- 5s, Unknown at 300s +/- 5s, in passenger app, display and ops console.
- **Sources:** DF-029, DF-030, DF-068
- **Assumptions:** ASM-019, ASM-020

#### REQ-LIVE-005 — Telemetry validation

- **Type:** Functional · **Priority:** Must · **MVP:** Yes · **Status:** Proposed
- **Requirement:** Telemetry ingestion rejects duplicates (same sequence), out-of-order updates older than the latest accepted, and physically implausible updates, recording a reason. Accepted and rejected counts are observable metrics.
- **Rationale:** Bad pings corrupt ETAs for every downstream stop.
- **Acceptance:** Replaying a duplicate batch changes no state; an injected 40 km jump in 10 seconds is rejected with reason 'implausible'.
- **Sources:** DF-031

#### REQ-LIVE-006 — Source precedence on conflict

- **Type:** Functional · **Priority:** Must · **MVP:** Yes · **Status:** Proposed
- **Requirement:** Conflicting facts are resolved by the documented source-precedence table, and the losing value is retained in the event history.
- **Rationale:** Without a rule two correct-looking writers produce a flip-flopping system.
- **Acceptance:** Driver 'departed 08:07:00' versus feed 'departed 08:07:30' stores the feed time as authoritative and keeps the driver time and reason code.
- **Sources:** DF-002, DF-028

#### REQ-LIVE-007 — ETA recomputation

- **Type:** Functional · **Priority:** Must · **MVP:** Yes · **Status:** Proposed
- **Requirement:** Downstream ETAs are recomputed within 5 seconds (p95) of an accepted update using offset propagation and a per-stop dwell model.
- **Rationale:** Freshness target; explainable method.
- **Acceptance:** k6 / trace timestamps show p95 under 5 seconds locally.
- **Sources:** DF-068
- **Assumptions:** ASM-032

#### REQ-LIVE-008 — Journey generation

- **Type:** Functional · **Priority:** Must · **MVP:** Yes · **Status:** Proposed
- **Requirement:** Journeys are generated nightly for the next 7 days from Service x calendar, with a manual path for special or relief services.
- **Rationale:** Passengers follow journeys, not services.
- **Acceptance:** Generation is idempotent: running twice creates no duplicates; a holiday exception date produces no journey.
- **Sources:** DF-014
- **Assumptions:** ASM-026

#### REQ-LIVE-009 — Service date versus calendar date

- **Type:** Functional · **Priority:** Must · **MVP:** Yes · **Status:** Proposed
- **Requirement:** Service date is distinct from calendar date. A service departing 23:50 and arriving 00:35 belongs to the earlier service date, and stop times may exceed 24:00.
- **Rationale:** Calendar-date logic breaks 'next train' for the last hour of every day.
- **Acceptance:** At 00:10 the 23:50 service still appears as Running on the previous service day's board.
- **Sources:** DF-096

#### REQ-LIVE-010 — Time-ranged vehicle assignment

- **Type:** Functional · **Priority:** Must · **MVP:** Yes · **Status:** Proposed
- **Requirement:** Vehicle assignment is time-ranged. Telemetry reaches a journey through its current assignment, and substitution mid-journey keeps the journey id and full history.
- **Rationale:** Tracking by vehicle id breaks on substitution; tracking by service breaks on repeat days.
- **Acceptance:** Swapping the vehicle at stop 4 leaves one continuous journey history with two assignment rows.
- **Sources:** DF-093, DF-094
- **Assumptions:** ASM-015

#### REQ-LIVE-011 — Unassigned vehicle or driver alert

- **Type:** Functional · **Priority:** Must · **MVP:** Yes · **Status:** Proposed
- **Requirement:** A journey with no vehicle or no driver raises an alert to the Scheduler 24 hours before departure and is flagged a cancellation candidate at 2 hours.
- **Rationale:** Maintenance and crew shortage are common real causes of cancellation.
- **Acceptance:** Marking a vehicle unavailable creates alerts for all its journeys within 60 seconds.
- **Sources:** DF-025, DF-027, DF-012, DF-013

#### REQ-LIVE-012 — Early running rule

- **Type:** Functional · **Priority:** Should · **MVP:** Yes · **Status:** Proposed
- **Requirement:** Early running may be shown for arrivals, but a departure time is never shown earlier than scheduled.
- **Rationale:** Passengers who trust an early time miss the train.
- **Acceptance:** A train arriving 4 minutes early shows departure at the scheduled time.
- **Sources:** DF-099

#### REQ-LIVE-013 — Interrupted journey shows Unknown ETA

- **Type:** Functional · **Priority:** Must · **MVP:** Yes · **Status:** Proposed
- **Requirement:** An Interrupted journey shows its last known position frozen and ETA 'Unknown'. It shows no delay figure until resumed, terminated short or cancelled.
- **Rationale:** A stopped train has no honest ETA; '+N' would be false.
- **Acceptance:** Breakdown scenario: ETA field is Unknown on all clients; no '+minutes' text is rendered.
- **Sources:** DF-024

#### REQ-LIVE-014 — Platform per stop with change state

- **Type:** Functional · **Priority:** Must · **MVP:** Yes · **Status:** Proposed
- **Requirement:** Platform is held per stop per journey with state TBA, Assigned or Changed and a change timestamp; changes reach all clients within 5 seconds.
- **Rationale:** Platform changes late and often; it is not a property of the route.
- **Acceptance:** Changing a platform updates a connected display and passenger app within 5 seconds and shows 'Platform changed'.
- **Sources:** DF-049

#### REQ-LIVE-015 — Ghost and phantom movements

- **Type:** Functional · **Priority:** Should · **MVP:** Yes · **Status:** Proposed
- **Requirement:** Telemetry with no matching journey is never shown publicly; a journey with no feed during its active window is flagged internally.
- **Rationale:** Ghosts look live; phantoms leak onto public screens.
- **Acceptance:** Injected phantom position does not appear on any public endpoint.
- **Sources:** DF-032

#### REQ-LIVE-016 — Scoped disruption resolution

- **Type:** Functional · **Priority:** Should · **MVP:** Yes · **Status:** Proposed
- **Requirement:** A disruption scoped to a section, route or station resolves automatically to the affected journeys and stops.
- **Rationale:** Bulk impact from one cause.
- **Acceptance:** Closing a station marks its remaining stops Skipped on all affected journeys.
- **Sources:** DF-039, DF-033

#### REQ-LIVE-017 — Timetable changes apply from effective date

- **Type:** Functional · **Priority:** Must · **MVP:** Yes · **Status:** Proposed
- **Requirement:** Changes to a published timetable apply from their effective date and never alter journeys already generated.
- **Rationale:** Prevents silent rewriting of running services.
- **Acceptance:** Publishing a new version at 10:00 does not alter a Running 09:30 journey.
- **Sources:** DF-100, DF-011

#### REQ-LIVE-018 — Vehicle failure cascades through its rotation

- **Type:** Functional · **Priority:** Should · **MVP:** Thin · **Status:** Proposed
- **Requirement:** When a vehicle becomes unavailable, every later journey in that vehicle's rotation for the service day is flagged. Infrastructure knock-on delay to other trains is explicitly out of scope.
- **Rationale:** A failure affects more than one journey; naming the excluded cascade is part of the requirement.
- **Acceptance:** Failing a vehicle with three later journeys flags all three within 60 seconds.
- **Sources:** DF-095

#### REQ-LIVE-019 — Train number is not identity

- **Type:** Functional · **Priority:** Should · **MVP:** Yes · **Status:** Proposed
- **Requirement:** Train number is a display attribute only. Journey id is the identity in every API, event and subscription.
- **Rationale:** Train numbers repeat daily and change at a terminus.
- **Acceptance:** Two journeys with the same train number on consecutive days have distinct ids and independent subscriptions.
- **Sources:** DF-097, DF-093

#### REQ-LIVE-020 — Platform conflict warning

- **Type:** Functional · **Priority:** Could · **MVP:** Thin · **Status:** Proposed
- **Requirement:** Assigning a platform already occupied by another journey within 5 minutes either side produces a warning, not a block.
- **Rationale:** Physical conflict the system can see; controllers sometimes do it deliberately, so it must not be blocked.
- **Acceptance:** Overlapping assignment shows a warning naming the other journey and still saves.
- **Sources:** DF-101

<a id="pax"></a>

## Passenger app

#### REQ-PAX-001 — Public information without sign-in

- **Type:** Functional · **Priority:** Must · **MVP:** Yes · **Status:** Proposed
- **Requirement:** Stations, timetable, departures and disruptions are available without sign-in.
- **Rationale:** Information needs to be usable before and without any account.
- **Acceptance:** All listed endpoints respond 200 with no credentials; any write endpoint returns 401.
- **Sources:** DF-059

#### REQ-PAX-002 — Station departure board

- **Type:** Functional · **Priority:** Must · **MVP:** Yes · **Status:** Proposed
- **Requirement:** A station departure board shows the next departures sorted by expected time, each with destination, scheduled time, expected time or delta, platform, status and data confidence.
- **Rationale:** The most frequent passenger question is 'when is my next train'.
- **Acceptance:** A journey 7 minutes late shows scheduled 14:25 and '+7'; the scheduled time stays visible.
- **Sources:** DF-068, DF-084
- **Assumptions:** ASM-033

#### REQ-PAX-003 — Journey detail timeline

- **Type:** Functional · **Priority:** Must · **MVP:** Yes · **Status:** Proposed
- **Requirement:** Journey detail shows ordered stops with scheduled, estimated and actual times, per-stop status, current section, platform and confidence, as a schematic timeline.
- **Rationale:** Answers 'where is it and what stops where'.
- **Acceptance:** A skipped stop is visibly marked and 'next stop' advances past it.
- **Sources:** DF-090, DF-048
- **Assumptions:** ASM-031

#### REQ-PAX-004 — Status never colour-only

- **Type:** Functional · **Priority:** Must · **MVP:** Yes · **Status:** Proposed
- **Requirement:** Every status (OnTime, MinorDelay, MajorDelay, Cancelled, Unknown) is conveyed by icon, text and colour together.
- **Rationale:** Colour alone fails colour-blind users and sunlit screens.
- **Acceptance:** Greyscale screenshot of every status remains distinguishable; axe finds no colour-only violation.
- **Sources:** DF-077

#### REQ-PAX-005 — Staleness labelling

- **Type:** Functional · **Priority:** Must · **MVP:** Yes · **Status:** Proposed
- **Requirement:** Data older than 60 seconds shows 'as of hh:mm'; Unknown shows 'No live data, showing timetable'; offline shows cached data with its age.
- **Rationale:** The product promise: never present stale or unknown as current.
- **Acceptance:** Simulated blackout and airplane-mode each produce the correct label within 5 seconds.
- **Sources:** DF-068, DF-047
- **Assumptions:** ASM-019

#### REQ-PAX-006 — Direct and one-transfer journey search

- **Type:** Functional · **Priority:** Should · **MVP:** Yes · **Status:** Proposed
- **Requirement:** Journey search returns direct trains and one-transfer connections respecting a minimum transfer time, and marks a connection 'at risk' when delay erodes the transfer.
- **Rationale:** Brief 3 asks for connections; scoped to what is defensible.
- **Acceptance:** A 6-minute transfer with a 3-minute delay on the first leg shows 'at risk'.
- **Sources:** DF-098
- **Assumptions:** ASM-025

#### REQ-PAX-007 — Follow a journey and notifications

- **Type:** Functional · **Priority:** Must · **MVP:** Yes · **Status:** Proposed
- **Requirement:** A passenger can follow a journey using an anonymous device subscription and is notified of expected-arrival change of 5 minutes or more, cancellation, platform change and termination, at most once per 5 minutes per journey (excluding cancel, platform change, termination).
- **Rationale:** Notifications are the main reason to follow; throttling prevents spam.
- **Acceptance:** Six consecutive delay updates within 5 minutes produce one push.
- **Sources:** DF-023, DF-067
- **Assumptions:** ASM-022, ASM-012

#### REQ-PAX-008 — Notification content

- **Type:** Functional · **Priority:** Must · **MVP:** Yes · **Status:** Proposed
- **Requirement:** A notification states what changed (old to new) and never replaces the app as the source of truth.
- **Rationale:** A push can be late or lost.
- **Acceptance:** Notification payload includes previous and new value; wording reviewed against the content guide.
- **Sources:** DF-023

#### REQ-PAX-009 — Disruption banners and retraction

- **Type:** Functional · **Priority:** Must · **MVP:** Yes · **Status:** Proposed
- **Requirement:** Disruption banners show scope, severity, message and updated time. A retracted disruption disappears and, if a push was sent, a correction is shown.
- **Rationale:** Retraction must be as visible as the original.
- **Acceptance:** Retract in ops removes the banner on all clients within 5 seconds and sends a correction for followed journeys.
- **Sources:** DF-016, DF-042

#### REQ-PAX-010 — Offline cache

- **Type:** Functional · **Priority:** Should · **MVP:** Yes · **Status:** Proposed
- **Requirement:** Saved stations and followed journeys are cached for offline viewing for up to 24 hours, clearly labelled with age.
- **Rationale:** Passengers lose signal in tunnels.
- **Acceptance:** Airplane-mode shows the last board with an age label; no live-only controls are enabled.
- **Sources:** DF-035

#### REQ-PAX-011 — Indicative-information disclaimer

- **Type:** Functional · **Priority:** Must · **MVP:** Yes · **Status:** Proposed
- **Requirement:** Journey and information screens state that live information is indicative.
- **Rationale:** Standard operator position; sets expectations.
- **Acceptance:** Disclaimer present on journey detail; verified by UI test.
- **Sources:** DF-109

#### REQ-PAX-012 — Delete device data

- **Type:** Functional · **Priority:** Must · **MVP:** Yes · **Status:** Proposed
- **Requirement:** A passenger can delete all of their follows and device data from within the app.
- **Rationale:** Privacy by design with anonymous tokens.
- **Acceptance:** After deletion no push is delivered and server records are removed.
- **Sources:** DF-105

#### REQ-PAX-013 — Two languages

- **Type:** Functional · **Priority:** Should · **MVP:** Yes · **Status:** Proposed
- **Requirement:** The passenger app supports English and one secondary language, with layouts that tolerate text expansion.
- **Rationale:** Occasional travellers and non-native speakers.
- **Acceptance:** Departures list renders without truncation of status in the secondary language.
- **Sources:** DF-082
- **Assumptions:** ASM-030

#### REQ-PAX-014 — Screen reader and text scaling

- **Type:** Non-functional · **Priority:** Must · **MVP:** Yes · **Status:** Proposed
- **Requirement:** Core journeys are usable with TalkBack/VoiceOver and at 200 percent text size.
- **Rationale:** Accessibility is tested, not asserted.
- **Acceptance:** Manual screen-reader pass plus automated checks; board layout intact at 200 percent text.
- **Sources:** DF-079, DF-080, DF-008

#### REQ-PAX-015 — Notification inbox and push reliability

- **Type:** Functional · **Priority:** Should · **MVP:** Yes · **Status:** Proposed
- **Requirement:** Every notification is also stored in an in-app inbox; expired push tokens are removed; a failed delivery is retried once.
- **Rationale:** Push is best-effort; the inbox is the fallback.
- **Acceptance:** A notification sent while push is unavailable appears in the inbox on next app open.
- **Sources:** DF-089

<a id="drv"></a>

## Driver app

#### REQ-DRV-001 — Driver authentication and session

- **Type:** Functional · **Priority:** Must · **MVP:** Yes · **Status:** Proposed
- **Requirement:** Drivers authenticate with an organisation account; sessions are short; PIN or biometric is required after inactivity; only the driver's own duty is visible.
- **Rationale:** Shared and lost devices.
- **Acceptance:** Another driver's roster is not retrievable by id manipulation (negative test).
- **Sources:** DF-060

#### REQ-DRV-002 — Duty view, offline-capable

- **Type:** Functional · **Priority:** Must · **MVP:** Yes · **Status:** Proposed
- **Requirement:** The driver sees today's assigned journeys with route, stops and scheduled times, cached so they remain visible offline.
- **Rationale:** Brief 8 'assigned journeys'.
- **Acceptance:** Duty remains viewable with network off after one successful sync.
- **Sources:** DF-013

#### REQ-DRV-003 — Permitted driver actions

- **Type:** Functional · **Priority:** Must · **MVP:** Yes · **Status:** Proposed
- **Requirement:** A driver can record arrived, departed, skipped (with reason), delay reason and request assistance. A driver cannot cancel a journey, edit the timetable or act on other journeys.
- **Rationale:** Brief 8 asks what they may update; minimum privilege.
- **Acceptance:** Driver token calling the cancel endpoint returns 403 (negative test).
- **Sources:** DF-004, DF-057

#### REQ-DRV-004 — No typing between stops

- **Type:** Functional · **Priority:** Should · **MVP:** Yes · **Status:** Proposed
- **Requirement:** While the journey is between stops the driver app offers only large one-tap and pick-list actions; free-text entry is disabled.
- **Rationale:** No typing while a train is moving.
- **Acceptance:** Free-text control is disabled when the journey section state is InSection.
- **Sources:** DF-041

#### REQ-DRV-005 — Offline action queue

- **Type:** Functional · **Priority:** Must · **MVP:** Yes · **Status:** Proposed
- **Requirement:** When connectivity is lost the app shows an explicit offline banner and last-sync time, keeps working, and queues every action as an event with device time, sequence number, idempotency key and duty id. The queue survives app restart and shows a pending count.
- **Rationale:** Brief 16: the central resilience scenario.
- **Acceptance:** Kill and relaunch the app with 3 queued actions: all 3 remain and replay on reconnect.
- **Sources:** DF-028, DF-044

#### REQ-DRV-006 — Replay and reconciliation

- **Type:** Functional · **Priority:** Must · **MVP:** Yes · **Status:** Proposed
- **Requirement:** On reconnect queued events replay in order; the server de-duplicates by idempotency key; events outside the duty window are flagged; infrastructure timing wins on conflict while the driver's reason is kept.
- **Rationale:** Deterministic reconciliation.
- **Acceptance:** Replaying the same batch twice yields identical state.
- **Sources:** DF-028

#### REQ-DRV-007 — Control message delivery states

- **Type:** Functional · **Priority:** Must · **MVP:** Yes · **Status:** Proposed
- **Requirement:** Control messages to a driver have states Sent, Delivered, Acknowledged visible to the controller; an offline driver app warns that messages may be missing since last sync.
- **Rationale:** A controller who believes a driver was told is a hazard.
- **Acceptance:** Message sent while the app is offline stays 'Sent' and the controller view shows it as unacknowledged.
- **Sources:** DF-028

#### REQ-DRV-008 — Orphaned events from an ended duty

- **Type:** Functional · **Priority:** Should · **MVP:** Yes · **Status:** Proposed
- **Requirement:** On sign-on, unsent events from an ended duty are surfaced for review rather than silently applied or dropped.
- **Rationale:** Battery death mid-duty.
- **Acceptance:** Events from a previous duty are not applied to the new duty.
- **Sources:** DF-052

#### REQ-DRV-009 — Crew handover mid-journey

- **Type:** Functional · **Priority:** Should · **MVP:** Thin · **Status:** Proposed
- **Requirement:** A second driver may sign on to a running journey; events are scoped to the duty, and the handover is logged.
- **Rationale:** Crew change mid-journey.
- **Acceptance:** Handover creates a new duty row; earlier queued events attach to the earlier duty.
- **Sources:** DF-004

#### REQ-DRV-010 — Driver location limits

- **Type:** Functional · **Priority:** Must · **MVP:** Yes · **Status:** Proposed
- **Requirement:** Driver location, if collected, is collected only while signed on to a duty, never shown to passengers, and retained for at most 7 days.
- **Rationale:** Employee monitoring.
- **Acceptance:** Location endpoints return nothing outside a duty window; passenger API never exposes driver identity.
- **Sources:** DF-104
- **Assumptions:** ASM-027

<a id="dsp"></a>

## Station display

#### REQ-DSP-001 — Platform display

- **Type:** Functional · **Priority:** Must · **MVP:** Yes · **Status:** Proposed
- **Requirement:** Platform display shows the next 3 departures from that platform with destination, scheduled time, expected time or delta, and status, legible at 8 metres.
- **Rationale:** Different locations have different needs (brief 6).
- **Acceptance:** Type size and contrast verified against the display scale at distance.
- **Sources:** DF-078

#### REQ-DSP-002 — Concourse display

- **Type:** Functional · **Priority:** Must · **MVP:** Yes · **Status:** Proposed
- **Requirement:** Concourse display shows departures and arrivals, active disruptions and pages when rows exceed the screen.
- **Rationale:** Main boards carry more than platform boards.
- **Acceptance:** Twelve departures render across pages without truncation.
- **Sources:** —

#### REQ-DSP-003 — Device identity and delta stream

- **Type:** Functional · **Priority:** Must · **MVP:** Yes · **Status:** Proposed
- **Requirement:** A display authenticates as a device with a read-only revocable token, receives a snapshot and then deltas, and reconnects with jittered backoff.
- **Rationale:** No human logs in; avoids reconnect storms.
- **Acceptance:** Revoking a token disconnects the display within one heartbeat interval.
- **Sources:** DF-055

#### REQ-DSP-004 — Display offline honesty

- **Type:** Functional · **Priority:** Must · **MVP:** Yes · **Status:** Proposed
- **Requirement:** Offline, the display keeps the last board with 'Updated hh:mm, live data unavailable' after 60 seconds, and after 300 seconds shows scheduled-only times with a banner. It never presents stale expected times as live.
- **Rationale:** A cheerful stale board is worse than an honest 'offline'.
- **Acceptance:** Playwright offline test asserts the banner and the absence of a live indicator.
- **Sources:** DF-034
- **Assumptions:** ASM-019, ASM-020

#### REQ-DSP-005 — Device heartbeat

- **Type:** Functional · **Priority:** Must · **MVP:** Yes · **Status:** Proposed
- **Requirement:** Devices send a heartbeat every 30 seconds and appear offline in ops after 90 seconds.
- **Rationale:** Dead displays are invisible otherwise.
- **Acceptance:** Unplugging a test display raises an offline flag within 90 seconds.
- **Sources:** DF-069
- **Assumptions:** ASM-029

#### REQ-DSP-006 — Server-time countdowns

- **Type:** Functional · **Priority:** Should · **MVP:** Yes · **Status:** Proposed
- **Requirement:** Displays compute countdowns from a server-time offset, not from the device clock.
- **Rationale:** Wrong device clocks show wrong minutes.
- **Acceptance:** Skewing the display clock by 5 minutes does not change the displayed countdown.
- **Sources:** DF-044

#### REQ-DSP-007 — Cancelled and departed row timing

- **Type:** Functional · **Priority:** Should · **MVP:** Yes · **Status:** Proposed
- **Requirement:** Cancelled trains remain visible as Cancelled for 15 minutes after scheduled departure; departed trains disappear after 1 minute.
- **Rationale:** Passengers must see a cancellation, not a vanished train.
- **Acceptance:** Cancelled row persists then removes at the configured time.
- **Sources:** DF-045
- **Assumptions:** ASM-024

#### REQ-DSP-008 — Motion and flashing limits

- **Type:** Non-functional · **Priority:** Should · **MVP:** Yes · **Status:** Proposed
- **Requirement:** Displays use no flashing above 3 Hz and minimal motion, with high contrast.
- **Rationale:** Photosensitivity and readability.
- **Acceptance:** Automated check for animation frequency; contrast ratio meets the display profile.
- **Sources:** DF-083

<a id="trm"></a>

## Station terminal

#### REQ-TRM-001 — Destination search

- **Type:** Functional · **Priority:** Must · **MVP:** Yes · **Status:** Proposed
- **Requirement:** The terminal lets a passenger search a destination and see routes and next trains, including one-transfer options, using the same status vocabulary as the app.
- **Rationale:** Brief 7 core.
- **Acceptance:** Searching an inter-route destination returns a transfer itinerary.
- **Sources:** DF-098

#### REQ-TRM-002 — Station information and facilities

- **Type:** Functional · **Priority:** Should · **MVP:** Yes · **Status:** Proposed
- **Requirement:** The terminal shows station information, facilities, step-free access and active disruptions.
- **Rationale:** Brief 7.
- **Acceptance:** Facilities page renders for each seeded station.
- **Sources:** DF-102

#### REQ-TRM-003 — Idle reset

- **Type:** Functional · **Priority:** Must · **MVP:** Yes · **Status:** Proposed
- **Requirement:** After 60 seconds idle the terminal returns to home and clears any entered data.
- **Rationale:** No session left behind on a shared device.
- **Acceptance:** Entered search text is gone after the idle timeout.
- **Sources:** DF-056
- **Assumptions:** ASM-023

#### REQ-TRM-004 — Kiosk lockdown

- **Type:** Non-functional · **Priority:** Should · **MVP:** Yes · **Status:** Proposed
- **Requirement:** The terminal runs locked down: no URL bar, no external navigation, no file access, no personal data entry or storage; touch targets suit public use.
- **Rationale:** Physically accessible public device.
- **Acceptance:** Manual lockdown checklist passes; no input field accepts personal data.
- **Sources:** DF-056, DF-081

#### REQ-TRM-005 — Timetable-only offline mode

- **Type:** Functional · **Priority:** Should · **MVP:** Yes · **Status:** Proposed
- **Requirement:** Offline, the terminal switches to a timetable-only mode with a banner and disables live-only features.
- **Rationale:** Honest degradation.
- **Acceptance:** Network cut shows the banner and hides the live status column.
- **Sources:** DF-034

<a id="ops"></a>

## Operations console

#### REQ-OPS-001 — Roles and separation of duties

- **Type:** Functional · **Priority:** Must · **MVP:** Yes · **Status:** Proposed
- **Requirement:** Ten roles exist (NetworkController, Scheduler, StationManager, CustomerServiceAgent, IncidentManager, FleetController, ReportingAnalyst, SystemAdministrator, Driver, Device) with permission-based authorisation. SystemAdministrator manages users and devices but holds no operational permission.
- **Rationale:** Write authority divided by accountability; separation of duties.
- **Acceptance:** SystemAdministrator token calling cancel or publish returns 403.
- **Sources:** DF-010, DF-066, DF-057
- **Assumptions:** ASM-013, ASM-006

#### REQ-OPS-002 — Live network board

- **Type:** Functional · **Priority:** Must · **MVP:** Yes · **Status:** Proposed
- **Requirement:** The live network board lists today's journeys with state, delta, confidence, vehicle and driver, filterable, updating in real time.
- **Rationale:** Brief 9: understand what is happening across the network.
- **Acceptance:** A simulator delay appears on the board within 5 seconds without refresh.
- **Sources:** DF-045

#### REQ-OPS-003 — Timetable versioning and approval

- **Type:** Functional · **Priority:** Must · **MVP:** Yes · **Status:** Proposed
- **Requirement:** Timetables are versioned Draft, InReview, Published, Superseded. A published version is immutable; publish requires preview and an approver different from the author.
- **Rationale:** Silent timetable edits and bad publishes.
- **Acceptance:** Editing a Published version is rejected; author cannot approve own publish.
- **Sources:** DF-011, DF-043, DF-054

#### REQ-OPS-004 — Journey actions with reasons

- **Type:** Functional · **Priority:** Must · **MVP:** Yes · **Status:** Proposed
- **Requirement:** Authorised users can cancel, terminate short, change platform and reassign vehicle or driver on a journey, each requiring a reason code and writing an audit entry.
- **Rationale:** High-consequence actions must be attributable.
- **Acceptance:** Cancel without a reason is rejected; every successful action produces an audit row.
- **Sources:** DF-058, DF-015

#### REQ-OPS-005 — Disruption console

- **Type:** Functional · **Priority:** Must · **MVP:** Yes · **Status:** Proposed
- **Requirement:** The disruption console supports create, update, retract and resolve, a scope selector, and a preview of the passenger rendering. Network-wide publication requires a second approver.
- **Rationale:** Brief 10; a wrong network-wide alert is high harm.
- **Acceptance:** Network-wide publish stays Draft until a second authorised user approves.
- **Sources:** DF-015, DF-016, DF-042

#### REQ-OPS-006 — Disruption review-by time

- **Type:** Functional · **Priority:** Should · **MVP:** Yes · **Status:** Proposed
- **Requirement:** Every disruption has a mandatory review-by time; passing it raises a warning and marks the disruption Expired for review.
- **Rationale:** Stale disruptions linger.
- **Acceptance:** A disruption past review-by appears in an 'Expired - review' list.
- **Sources:** DF-050

#### REQ-OPS-007 — Incident lifecycle

- **Type:** Functional · **Priority:** Must · **MVP:** Yes · **Status:** Proposed
- **Requirement:** Incidents have severity, an owner and a timeline; only IncidentManager can declare Critical; ownership can be reassigned with a note.
- **Rationale:** Incident lifecycle.
- **Acceptance:** NetworkController declaring Critical returns 403.
- **Sources:** DF-015, DF-017, DF-026

#### REQ-OPS-008 — Major incident (emergency termination)

- **Type:** Functional · **Priority:** Must · **MVP:** Thin · **Status:** Proposed
- **Requirement:** Declaring TerminatedEmergency uses approved message templates, sets ETA Unknown, blocks public speculation, and places that journey's raw telemetry under legal hold.
- **Rationale:** Worst-case handling must be safe and preserve evidence.
- **Acceptance:** Legal-hold flag prevents retention purge in a test; template text is used verbatim.
- **Sources:** DF-026

#### REQ-OPS-009 — Device health and revocation

- **Type:** Functional · **Priority:** Must · **MVP:** Yes · **Status:** Proposed
- **Requirement:** Device health lists each device with last heartbeat and status, and allows revocation.
- **Rationale:** Closes the 'dead display' gap.
- **Acceptance:** Revoked device is rejected on its next request.
- **Sources:** DF-069, DF-055, DF-018, DF-006, DF-051

#### REQ-OPS-010 — Concurrent edit detection

- **Type:** Functional · **Priority:** Should · **MVP:** Yes · **Status:** Proposed
- **Requirement:** Concurrent edits to the same journey are detected with optimistic concurrency and the second editor is shown the conflict.
- **Rationale:** Two controllers acting on one train.
- **Acceptance:** Two sessions editing one journey: the second save is rejected with the current version.
- **Sources:** —

#### REQ-OPS-011 — Append-only audit

- **Type:** Functional · **Priority:** Must · **MVP:** Yes · **Status:** Proposed
- **Requirement:** An append-only audit log records actor, role, action, target, reason, before and after, and timestamp for high-consequence actions, and cannot be edited.
- **Rationale:** Accountability.
- **Acceptance:** Direct update/delete on the audit table is blocked by permission.
- **Sources:** DF-058

#### REQ-OPS-012 — Vehicle unavailability

- **Type:** Functional · **Priority:** Should · **MVP:** Thin · **Status:** Proposed
- **Requirement:** FleetController can mark a vehicle unavailable for a time window, creating alerts for journeys that lose their vehicle.
- **Rationale:** Maintenance and failure.
- **Acceptance:** Marking a vehicle unavailable lists the affected journeys.
- **Sources:** DF-003, DF-025, DF-053

#### REQ-OPS-013 — Messages to drivers

- **Type:** Functional · **Priority:** Should · **MVP:** Thin · **Status:** Proposed
- **Requirement:** Controllers can send messages to drivers and see Sent, Delivered and Acknowledged states.
- **Rationale:** Closed-loop communication.
- **Acceptance:** Acknowledgement from the driver app updates the controller view.
- **Sources:** DF-028

#### REQ-OPS-014 — Punctuality report

- **Type:** Functional · **Priority:** Should · **MVP:** Yes · **Status:** Proposed
- **Requirement:** A punctuality report shows on-time percentage by service, route and day, with CSV export.
- **Rationale:** Brief 9 Reporting; corrected values are used.
- **Acceptance:** Report totals reconcile with journey records for a seeded day.
- **Sources:** DF-019, DF-022

#### REQ-OPS-015 — Customer service lookup

- **Type:** Functional · **Priority:** Could · **MVP:** Thin · **Status:** Proposed
- **Requirement:** CustomerServiceAgent has read-only lookup of journeys and disruptions using the same status vocabulary as passengers.
- **Rationale:** Support must see what passengers see.
- **Acceptance:** CustomerServiceAgent token on a write endpoint returns 403.
- **Sources:** DF-007, DF-021, DF-110

#### REQ-OPS-016 — Planned disruptions (engineering works)

- **Type:** Functional · **Priority:** Should · **MVP:** Thin · **Status:** Proposed
- **Requirement:** A disruption can be scheduled with a future start and end (planned works). It is visible in advance on affected journeys and becomes active automatically at its start.
- **Rationale:** Engineering works are known days ahead and must be warned about early.
- **Acceptance:** A disruption scheduled for tomorrow appears on tomorrow's affected journeys today, marked 'planned'.
- **Sources:** DF-020

#### REQ-OPS-017 — Time-boxed elevated access

- **Type:** Functional · **Priority:** Could · **MVP:** No · **Status:** Proposed
- **Requirement:** A user may request time-boxed elevated permissions during a major incident, with a mandatory reason, automatic expiry and audit.
- **Rationale:** Rigid roles obstruct incident response; unchecked elevation is a hole.
- **Acceptance:** Documented only for MVP; design reviewed in the threat model.
- **Sources:** DF-061

<a id="sec"></a>

## Security

#### REQ-SEC-001 — Four authentication postures

- **Type:** Non-functional · **Priority:** Must · **MVP:** Yes · **Status:** Proposed
- **Requirement:** Four authentication postures are supported: anonymous public, passenger device, staff, and unattended device.
- **Rationale:** Brief 14.
- **Acceptance:** Each posture has a documented token type, scope and a test.
- **Sources:** DF-055, DF-010

#### REQ-SEC-002 — Public rate limiting

- **Type:** Non-functional · **Priority:** Must · **MVP:** Yes · **Status:** Proposed
- **Requirement:** Public endpoints are rate-limited to 60 requests per minute per client and served from cache where possible.
- **Rationale:** Abuse protection.
- **Acceptance:** 61st request in a minute returns 429.
- **Sources:** DF-059, DF-075
- **Assumptions:** ASM-028

#### REQ-SEC-003 — Permission matrix tests

- **Type:** Non-functional · **Priority:** Must · **MVP:** Yes · **Status:** Proposed
- **Requirement:** Every permission-matrix cell has a positive and a negative automated test, including IDOR attempts on follows and cross-station edits.
- **Rationale:** Authorisation is the real risk.
- **Acceptance:** Coverage report shows every cell exercised.
- **Sources:** DF-057

#### REQ-SEC-004 — Feed source authentication

- **Type:** Non-functional · **Priority:** Must · **MVP:** Yes · **Status:** Proposed
- **Requirement:** Each telemetry/feed source authenticates with its own key; unknown sources are rejected; the source id is stored with every accepted update.
- **Rationale:** A forged position is a way to publish false information.
- **Acceptance:** Unknown key returns 401 and is counted.
- **Sources:** DF-062, DF-086

#### REQ-SEC-005 — Plain-text disruption content

- **Type:** Non-functional · **Priority:** Must · **MVP:** Yes · **Status:** Proposed
- **Requirement:** Disruption and message text is plain text, length-limited, escaped on render, with templates for severe incidents.
- **Rationale:** Staff-entered text reaches the public.
- **Acceptance:** Script tags in a disruption render as literal text on all clients.
- **Sources:** DF-063

#### REQ-SEC-006 — Staff account protection

- **Type:** Non-functional · **Priority:** Should · **MVP:** Thin · **Status:** Proposed
- **Requirement:** Staff accounts have lockout after repeated failure and MFA for the highest-consequence roles.
- **Rationale:** Ops accounts can cancel trains.
- **Acceptance:** Five failed logins locks the account for a defined period.
- **Sources:** DF-065

<a id="data"></a>

## Data and privacy

#### REQ-DATA-001 — Data classification

- **Type:** Non-functional · **Priority:** Must · **MVP:** Yes · **Status:** Proposed
- **Requirement:** A classification table assigns every data element to Public, Operational, Personal or Sensitive-operational with an owner, retention and log-redaction rule.
- **Rationale:** Brief 20.
- **Acceptance:** Every table and event type appears in the classification document.
- **Sources:** DF-103

#### REQ-DATA-002 — Log hygiene

- **Type:** Non-functional · **Priority:** Must · **MVP:** Yes · **Status:** Proposed
- **Requirement:** Logs contain no tokens, email addresses or precise driver coordinates and use correlation ids instead of identities.
- **Rationale:** Log hygiene.
- **Acceptance:** Automated scan of sample logs finds no token or coordinate patterns.
- **Sources:** DF-106

#### REQ-DATA-003 — Retention and legal hold

- **Type:** Non-functional · **Priority:** Must · **MVP:** Yes · **Status:** Proposed
- **Requirement:** Retention: raw telemetry 7 days, aggregated punctuality 13 months, audit 24 months, with legal hold overriding purge for an incident.
- **Rationale:** Brief 20 and investigation needs.
- **Acceptance:** Purge job removes expired telemetry but not held telemetry.
- **Sources:** DF-107
- **Assumptions:** ASM-027

<a id="nfr"></a>

## Platform / non-functional

#### REQ-NFR-001 — End-to-end freshness

- **Type:** Non-functional · **Priority:** Must · **MVP:** Yes · **Status:** Proposed
- **Requirement:** An accepted telemetry update is visible to connected clients within 5 seconds (p95, local environment).
- **Rationale:** Makes 'live' measurable.
- **Acceptance:** Trace timestamps show p95 under 5 seconds.
- **Sources:** DF-068

#### REQ-NFR-002 — Departures performance

- **Type:** Non-functional · **Priority:** Should · **MVP:** Yes · **Status:** Proposed
- **Requirement:** The public departures endpoint responds in under 300 ms at p95 when cached.
- **Rationale:** Passenger responsiveness and spike resilience.
- **Acceptance:** k6 smoke run meets the target.
- **Sources:** DF-075

#### REQ-NFR-003 — WCAG 2.2 AA

- **Type:** Non-functional · **Priority:** Must · **MVP:** Yes · **Status:** Proposed
- **Requirement:** Passenger app, terminal and displays meet WCAG 2.2 AA with zero critical axe violations.
- **Rationale:** Accessibility target.
- **Acceptance:** axe report plus manual pass.
- **Sources:** DF-077, DF-079, DF-008

#### REQ-NFR-004 — Observability

- **Type:** Non-functional · **Priority:** Must · **MVP:** Yes · **Status:** Proposed
- **Requirement:** Structured logs with correlation ids, traces across SmartRail and RailBite, health endpoints, and metrics for ingest lag, ETA recompute time, connected clients, dead-letter count and device heartbeat freshness.
- **Rationale:** Brief 19.
- **Acceptance:** Dashboard shows each metric for a live scenario.
- **Sources:** DF-070, DF-071, DF-069

#### REQ-NFR-005 — Time handling

- **Type:** Non-functional · **Priority:** Must · **MVP:** Yes · **Status:** Proposed
- **Requirement:** All timestamps are stored in UTC and displayed in the network time zone; the server is the clock authority and the code uses an injectable clock.
- **Rationale:** Ordering and staleness depend on one clock.
- **Acceptance:** Tests control time through the clock abstraction only.
- **Sources:** DF-074, DF-111
- **Assumptions:** ASM-017

#### REQ-NFR-006 — Degraded-mode matrix

- **Type:** Non-functional · **Priority:** Must · **MVP:** Yes · **Status:** Proposed
- **Requirement:** Each application has a documented degraded-mode matrix: what it caches, for how long, what it hides when stale, and what it refuses to do offline.
- **Rationale:** Brief 16 across five apps.
- **Acceptance:** Matrix exists and each row has a corresponding test or scenario.
- **Sources:** DF-034, DF-035, DF-036

#### REQ-NFR-007 — Realtime fallback

- **Type:** Non-functional · **Priority:** Should · **MVP:** Yes · **Status:** Proposed
- **Requirement:** The realtime channel falls back to long polling automatically and uses a snapshot-then-delta protocol with jittered reconnect.
- **Rationale:** Blocked WebSockets and reconnect storms.
- **Acceptance:** Blocking WebSockets in a test still delivers updates.
- **Sources:** DF-036

#### REQ-NFR-008 — Notification event matrix

- **Type:** Functional · **Priority:** Must · **MVP:** Yes · **Status:** Proposed
- **Requirement:** A notification event matrix defines event, audience, channel and throttle for every event the system emits.
- **Rationale:** Brief 13 is one line.
- **Acceptance:** Matrix document exists; each row maps to a test.
- **Sources:** DF-023

<a id="sim"></a>

## Simulator

#### REQ-SIM-001 — Simulator uses the real ingestion path

- **Type:** Functional · **Priority:** Must · **MVP:** Yes · **Status:** Proposed
- **Requirement:** The simulator publishes through the same authenticated ingestion API as any real feed and writes nothing directly to storage.
- **Rationale:** A simulator with a back door tests a shortcut.
- **Acceptance:** Disabling the ingestion API stops the simulator from affecting state.
- **Sources:** DF-112, DF-086

#### REQ-SIM-002 — Deterministic, accelerable scenarios

- **Type:** Functional · **Priority:** Must · **MVP:** Yes · **Status:** Proposed
- **Requirement:** Scenarios are scripted and seeded, producing identical event streams on every run, with time acceleration from 1x to 60x via the injectable clock.
- **Rationale:** Repeatable demos and E2E tests.
- **Acceptance:** Two runs of one seed produce byte-identical event logs.
- **Sources:** DF-113, DF-115

#### REQ-SIM-003 — Fault injection

- **Type:** Functional · **Priority:** Must · **MVP:** Yes · **Status:** Proposed
- **Requirement:** The simulator can inject dropouts, duplicates, reordering, clock skew, implausible jumps and phantom movements.
- **Rationale:** Resilience claims need producible failure.
- **Acceptance:** Each fault type has a scenario and an assertion on system behaviour.
- **Sources:** DF-114, DF-031, DF-032

#### REQ-SIM-004 — Scenario library

- **Type:** Functional · **Priority:** Must · **MVP:** Yes · **Status:** Proposed
- **Requirement:** A scenario library covers on-time, delay, skipped stop, terminate short, telemetry blackout, vehicle breakdown, vehicle substitution, cancellation and driver-offline.
- **Rationale:** Coverage of the edge-case catalogue.
- **Acceptance:** Library listed in the Simulator sheet with each scenario implemented.
- **Sources:** DF-113, DF-116

#### REQ-SIM-005 — Scenario reset outside production

- **Type:** Functional · **Priority:** Should · **MVP:** Yes · **Status:** Proposed
- **Requirement:** Scenarios can be reset through a test endpoint that does not exist in production configuration.
- **Rationale:** Repeatable demos.
- **Acceptance:** Endpoint returns 404 under production configuration.
- **Sources:** DF-113

<a id="int"></a>

## External catering integration

#### REQ-INT-001 — Separate catering system

- **Type:** Functional · **Priority:** Could · **MVP:** Thin · **Status:** Proposed
- **Requirement:** RailBite is a separate application with its own database. SmartRail never reads its storage.
- **Rationale:** Brief 12 separation.
- **Acceptance:** SmartRail has no connection string for the RailBite database.
- **Sources:** DF-087

#### REQ-INT-002 — Outbound reference and ETA feed

- **Type:** Functional · **Priority:** Could · **MVP:** Thin · **Status:** Proposed
- **Requirement:** SmartRail sends journey and station reference data, ETA changes and cancellations to RailBite using an API key, with a minimal payload that excludes passenger identity.
- **Rationale:** Brief 11 and data minimisation.
- **Acceptance:** Payload schema contains no personal fields (schema test).
- **Sources:** DF-108, DF-088

#### REQ-INT-003 — Signed inbound webhooks

- **Type:** Functional · **Priority:** Could · **MVP:** Thin · **Status:** Proposed
- **Requirement:** RailBite sends order events as webhooks that are HMAC-signed, timestamped, idempotent and rejected outside a 5-minute replay window.
- **Rationale:** At-least-once delivery requires idempotency and authenticity.
- **Acceptance:** Replayed or tampered webhook is rejected; duplicate delivery is a no-op.
- **Sources:** DF-087, DF-064

#### REQ-INT-004 — Outbox, retry and dead-letter

- **Type:** Functional · **Priority:** Could · **MVP:** Thin · **Status:** Proposed
- **Requirement:** Outbound events use an outbox with retry, backoff and a dead-letter queue; a RailBite outage never affects railway functions.
- **Rationale:** Separation under failure.
- **Acceptance:** Stopping RailBite leaves all core endpoints healthy and queues events.
- **Sources:** DF-037

#### REQ-INT-005 — Single-screen collection order

- **Type:** Functional · **Priority:** Could · **MVP:** Thin · **Status:** Proposed
- **Requirement:** Passengers can place a single collection order from one screen using RailBite's menu; the order may be rejected at confirm and the UI handles it.
- **Rationale:** Brief 12, minimum viable.
- **Acceptance:** Forced stock-out at confirm shows a clear message and no broken state.
- **Sources:** DF-092
