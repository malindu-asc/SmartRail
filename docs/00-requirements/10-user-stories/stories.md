# User stories

Stories are grouped into epics. Each has testable Given/When/Then acceptance criteria and traces to requirements.
Thresholds come from the assumptions register so docs, stories and tests use the same numbers.

**Definition of Ready** (training): problem understood · business value known · story written · acceptance
criteria defined · priority agreed · dependencies identified · I can explain it. A story enters a build day only
when all seven are true.

**Status:** `Proposed` → `Ready` → `In progress` → `Done` (Done = criteria met, tested, documented).

---

## Epic E1 — Passenger information

### US-001 — See the next departures from my station
*As a commuter, I want the next departures from my station, so that I can decide which train to take in a few
seconds.* · Must · Traces: REQ-PAX-001, REQ-PAX-002, REQ-PAX-004 · Status: Proposed

- **Given** I have not signed in **When** I open a station **Then** departures load without any credentials
- **Given** a train is 7 min late **When** I view the board **Then** I see scheduled 14:25 and "+7", not just 14:32
- **Given** any status **When** shown **Then** it carries an icon, text and colour together

### US-002 — Know when information is old
*As a commuter, I want to see when information is not current, so that I don't trust a stale "on time".* · Must
· Traces: REQ-PAX-005, REQ-LIVE-004 · Status: Proposed

- **Given** no update for 60 s **When** I view the board **Then** live values show "as of hh:mm"
- **Given** no update for 300 s **When** I view the journey **Then** I see "No live data — showing timetable" and no
  delay figure

### US-003 — Follow a train's progress along its stops
*As a passenger, I want to see where my train is and when it reaches my stop, so that I can time my arrival.* ·
Must · Traces: REQ-PAX-003, REQ-LIVE-003, REQ-LIVE-007, REQ-PAX-011 · Status: Proposed

- **Given** a running journey **When** I open it **Then** I see a stop timeline with scheduled, estimated and actual
  times and the current section
- **Given** a skipped stop **When** I view the timeline **Then** it is marked "not stopping" and "next stop" skips it
- **Given** an accepted update **When** ETAs are recomputed **Then** downstream stops change within 5 s
- **Given** any journey detail **Then** the "live information is indicative" notice is visible

### US-004 — See honest status when a train has stopped
*As a passenger, I want to be told when there is no estimate, so that I'm not counting down to an invented time.* ·
Must · Traces: REQ-LIVE-013 · Status: Proposed

- **Given** a journey is Interrupted **When** I view it anywhere **Then** I see "Stopped — no estimate" and no "+n"

### US-005 — Find a journey with at most one change
*As an occasional traveller, I want routes from A to B including one change, so that I can plan a trip I don't
know.* · Should · Traces: REQ-PAX-006 · Status: Proposed

- **Given** no direct train **When** I search **Then** I get one-change options with ≥ 5 min transfer
- **Given** a first-leg delay leaves 2 min to transfer **When** I view it **Then** it is marked "at risk"

### US-006 — Use the app without signal
*As a commuter, I want my saved stations available offline, labelled with their age, so that I can still check.* ·
Should · Traces: REQ-PAX-010 · Status: Proposed

- **Given** airplane mode **When** I open a saved station **Then** I see the last board with its age and no live
  markers

### US-007 — Read everything with a screen reader
*As a passenger using TalkBack, I want every status read as words, so that I can travel independently.* · Must ·
Traces: REQ-PAX-014, REQ-NFR-003 · Status: Proposed

- **Given** TalkBack **When** I move through the board **Then** each row reads destination, time, status and platform
- **Given** 200 % text **When** I view the board **Then** no status is truncated

## Epic E2 — Following and notifications

### US-010 — Follow a journey without an account
*As a commuter, I want to follow my train without signing up, so that I get alerts with no friction.* · Must ·
Traces: REQ-PAX-007, ASM-012 · Status: Proposed

- **Given** I tap Follow **When** the subscription is created **Then** no personal data is requested
- **Given** the journey completes **When** an hour has passed **Then** the follow expires

### US-011 — One alert per meaningful change
*As a commuter, I want one notification when my train is significantly delayed, not one per minute.* · Must ·
Traces: REQ-PAX-007, REQ-PAX-008, REQ-NFR-008 · Status: Proposed

- **Given** six delay changes within 5 min **When** I follow the journey **Then** I receive one push
- **Given** a cancellation **When** it happens **Then** I am pushed immediately regardless of throttle
- **Given** any push **When** I read it **Then** it states old → new value

### US-012 — Be told when an alert was wrong
*As a passenger, I want a correction when a disruption alert was sent in error, so that I don't act on it.* · Must
· Traces: REQ-PAX-009 · Status: Proposed

- **Given** I was pushed a disruption **When** it is retracted **Then** I receive one correction push

### US-013 — Never miss an alert because push failed
*As a passenger, I want an inbox of notifications, so that a failed push doesn't lose information.* · Should ·
Traces: REQ-PAX-015 · Status: Proposed

- **Given** push delivery fails **When** I open the app **Then** the notification is in my inbox

### US-014 — Delete my data
*As a passenger, I want to delete all my follows and device data.* · Must · Traces: REQ-PAX-012 · Status: Proposed

- **Given** I confirm deletion **When** it completes **Then** no further pushes arrive and server records are gone

## Epic E3 — Driver operations

### US-020 — See my duty, even offline
*As a driver, I want today's journeys and stops on my device, so that I can work even without signal.* · Must ·
Traces: REQ-DRV-001, REQ-DRV-002 · Status: Proposed

- **Given** I synced once **When** I lose signal **Then** my duty is still visible
- **Given** my cache is from yesterday **When** I sign on offline **Then** I see "duty not current"
- **Given** another driver's duty id **When** I request it **Then** I receive 403

### US-021 — Record stops with one tap
*As a driver, I want large one-tap Arrived and Departed buttons, so that recording never distracts me.* · Must ·
Traces: REQ-DRV-003, REQ-DRV-004 · Status: Proposed

- **Given** the train is between stops **When** I open the app **Then** free-text entry is disabled
- **Given** I tap Departed twice **When** events are sent **Then** one event exists

### US-022 — Say why we're late
*As a driver, I want to record a delay reason from a short list, so that delay isn't attributed to me wrongly.* ·
Must · Traces: REQ-DRV-003 · Status: Proposed

- **Given** a delay **When** I pick "signal" **Then** the controller sees the reason on the journey

### US-023 — Keep working without signal
*As a driver, I want my actions saved and sent when signal returns, so that nothing I record is lost.* · Must ·
Traces: REQ-DRV-005, REQ-DRV-006, REQ-LIVE-006 · Status: Proposed

- **Given** no signal **When** I record actions **Then** I see an offline banner, last sync time and pending count
- **Given** queued actions **When** the app is killed and relaunched **Then** the queue is intact
- **Given** reconnection **When** replay runs twice **Then** state is identical to running once

### US-024 — Know if I might have missed a message
*As a driver, I want to be warned that control messages may be missing while offline.* · Must · Traces:
REQ-DRV-007 · Status: Proposed

- **Given** I am offline **When** I view the app **Then** it says "messages may be missing since hh:mm"
- **Given** a message arrives **When** I tap Acknowledge **Then** control sees Acknowledged

### US-025 — Request assistance safely
*As a driver, I want to request assistance with a confirm step, so that I can't trigger it by accident.* · Should ·
Traces: REQ-DRV-003 · Status: Proposed

- **Given** I tap Request assistance **When** I don't confirm **Then** nothing is sent

### US-026 — My location is not tracked off duty
*As a driver, I want location collected only while I am on a duty and never shown to passengers, so that the
system serves operations without monitoring me.* · Must · Traces: REQ-DRV-010 · Status: Proposed

- **Given** I am signed off **When** any location endpoint is queried **Then** nothing is returned for me
- **Given** any passenger API **Then** no driver identity or driver location is exposed
- **Given** raw driver location older than 7 days **When** the purge runs **Then** it is deleted

## Epic E4 — Network control

### US-030 — See the whole network honestly
*As a network controller, I want every journey with its delay and data confidence, so that I never mistake a dead
feed for a moving train.* · Must · Traces: REQ-OPS-002, REQ-LIVE-004, REQ-NFR-001 · Status: Proposed

- **Given** a journey goes Unknown **When** I view the board **Then** it is highlighted and not shown as moving
- **Given** a simulator delay **When** accepted **Then** my board updates within 5 s without refresh

### US-031 — Cancel or terminate a journey once, everywhere
*As a network controller, I want to cancel a train once and have every surface follow.* · Must · Traces:
REQ-OPS-004, REQ-OPS-011 · Status: Proposed

- **Given** I cancel with a reason **When** I confirm **Then** app, displays and terminal show it within 5 s
- **Given** no reason **When** I try to cancel **Then** it is rejected
- **Given** the action succeeds **Then** exactly one audit entry exists with before and after

### US-032 — Change a platform safely
*As a controller or station manager, I want platform changes to reach screens, staff and followers at once.* · Must
· Traces: REQ-LIVE-014, REQ-LIVE-020 · Status: Proposed

- **Given** I change 3 → 5 **When** I confirm **Then** displays show "changed from 3" and followers are pushed
- **Given** platform 5 is occupied ± 5 min **When** I change **Then** I see a warning and can still save
- **Given** I am a station manager at another station **When** I try **Then** I receive 403

### US-033 — Message a driver and know they got it
*As a controller, I want to see delivered and acknowledged states, so that I know to use the radio if not.* ·
Should · Traces: REQ-OPS-013 · Status: Proposed

## Epic E5 — Disruptions and incidents

### US-040 — Publish a disruption with a preview
*As a controller, I want to see exactly how a disruption will appear before publishing.* · Must · Traces:
REQ-OPS-005, REQ-LIVE-016 · Status: Proposed

- **Given** a section scope **When** I publish **Then** affected journeys and stations are resolved automatically
- **Given** network scope **When** I publish **Then** it waits for a second approver

### US-041 — Retract a mistaken disruption
*As a controller, I want to retract a disruption sent in error.* · Must · Traces: REQ-OPS-005, REQ-PAX-009 ·
Status: Proposed

### US-042 — Never leave a disruption forgotten
*As a disruption owner, I want a review-by time and a warning when it passes.* · Should · Traces: REQ-OPS-006 ·
Status: Proposed

### US-043 — Run a serious incident with one controlled voice
*As an incident manager, I want approved templates, legal hold and handover.* · Must · Traces: REQ-OPS-007,
REQ-OPS-008, REQ-DATA-003 · Status: Proposed

- **Given** I terminate a journey for an emergency **Then** only template text is public and telemetry is held
- **Given** a controller attempts the same **Then** they receive 403

## Epic E6 — Planning and allocation

### US-050 — Publish a timetable like a release
*As a scheduler, I want versioned timetables approved by someone else.* · Must · Traces: REQ-OPS-003, REQ-LIVE-017
· Status: Proposed

- **Given** I authored a version **When** I approve it **Then** it is rejected
- **Given** a new version is published **When** journeys are already running **Then** they are unchanged

### US-051 — Never discover a missing vehicle at 06:00
*As a scheduler, I want journeys without a vehicle or driver flagged early.* · Must · Traces: REQ-LIVE-011,
REQ-LIVE-018 · Status: Proposed

- **Given** a vehicle marked unavailable **When** within 60 s **Then** all its journeys, including later rotation
  journeys, are in the queue

### US-052 — Schedule planned works
*As a scheduler, I want to publish engineering works in advance.* · Should · Traces: REQ-OPS-016 · Status: Proposed

## Epic E7 — Stations

### US-060 — A platform screen that is right from 8 metres
*As a passenger on a platform, I want the next three departures readable from a distance.* · Must · Traces:
REQ-DSP-001, REQ-DSP-008 · Status: Proposed

### US-061 — A screen that admits it is offline
*As a passenger, I want an offline screen to say so instead of showing old times as live.* · Must · Traces:
REQ-DSP-004 · Status: Proposed

- See EDG-080 for the full criteria (65 s banner, 305 s scheduled-only, 90 s offline in device health)

### US-062 — Know every screen is alive
*As a station manager, I want offline screens flagged within 90 s.* · Must · Traces: REQ-DSP-005, REQ-OPS-009 ·
Status: Proposed

### US-063 — Find my way from a terminal without installing anything
*As a visitor, I want to search a destination on a station terminal.* · Must · Traces: REQ-TRM-001, REQ-TRM-002 ·
Status: Proposed

### US-064 — A terminal that forgets me
*As a passenger using a shared terminal, I want my search cleared when I leave.* · Must · Traces: REQ-TRM-003,
REQ-TRM-004 · Status: Proposed

- **Given** 60 s idle **When** the timeout fires **Then** the terminal returns home with all input cleared

### US-065 — A concourse board for the whole station
*As a passenger in the main hall, I want all departures, arrivals and active disruptions on one board.* · Must ·
Traces: REQ-DSP-002 · Status: Proposed

- **Given** more departures than rows **When** displayed **Then** the board pages without truncating any row
- **Given** an active disruption for this station **Then** it is shown in a banner

## Epic E8 — Access and security

### US-070 — Roles that match responsibility
*As the organisation, I want write authority split by accountability, and administrators unable to operate trains.*
· Must · Traces: REQ-OPS-001, REQ-SEC-003, REQ-SEC-001 · Status: Proposed

- **Given** a system administrator token **When** cancel or publish is called **Then** 403
- **Given** every permission-matrix cell **Then** a positive and a negative automated test exists

### US-071 — Devices that can be revoked
*As a system administrator, I want to provision and revoke screens individually.* · Must · Traces: REQ-DSP-003,
REQ-OPS-009 · Status: Proposed

### US-072 — Feeds that cannot be forged
*As the organisation, I want only known feed sources to move trains.* · Must · Traces: REQ-SEC-004 · Status: Proposed

- **Given** an unknown source key **When** it posts telemetry **Then** 401 and nothing changes

### US-073 — Public information that can't be abused
*As the organisation, I want anonymous endpoints rate-limited and cached, so that scraping or floods don't take
information away from passengers.* · Must · Traces: REQ-SEC-002 · Status: Proposed

- **Given** 60 requests in a minute from one client **When** the 61st arrives **Then** 429

### US-074 — Disruption text is only ever text
*As the organisation, I want staff-entered messages rendered as plain text, so that nothing harmful reaches public
screens.* · Must · Traces: REQ-SEC-005 · Status: Proposed

- **Given** a disruption containing a script tag **When** shown on any surface **Then** it renders literally

### US-075 — Data classified, logs clean
*As the data-protection owner, I want every data element classified and no personal data in logs.* · Must · Traces:
REQ-DATA-001, REQ-DATA-002 · Status: Proposed

- **Given** the classification table **Then** every table and event type appears in it
- **Given** a sample of logs from an E2E run **When** scanned **Then** no tokens, emails or driver coordinates appear

## Epic E9 — Live data platform and simulator

### US-080 — Reproducible railway scenarios
*As the engineer, I want seeded, accelerable scenarios that use the real ingestion API.* · Must · Traces:
REQ-SIM-001, REQ-SIM-002, REQ-SIM-004, REQ-NFR-005 · Status: Proposed

- **Given** the same seed **When** run twice **Then** event logs are identical
- **Given** the ingestion API is disabled **When** the simulator runs **Then** no state changes

### US-081 — Bad data cannot move a train
*As a controller, I want duplicate, late and impossible telemetry rejected.* · Must · Traces: REQ-LIVE-005,
REQ-SIM-003 · Status: Proposed

### US-082 — One journey identity through a vehicle swap
*As a passenger following a train, I want my follow to survive a locomotive replacement.* · Must · Traces:
REQ-LIVE-010 · Status: Proposed

### US-083 — One vocabulary for the state of a train
*As every user of the system, I want a train's lifecycle, punctuality and data confidence kept separate, so that
"cancelled while late" and "late with no data" can both be said.* · Must · Traces: REQ-LIVE-001, REQ-LIVE-002 ·
Status: Proposed

- **Given** every transition in the state table **Then** legal ones succeed and illegal ones return a modelled error
- **Given** a running journey 8 min late **Then** lifecycle = Running, delta = +8, band = MinorDelay

### US-084 — Journeys exist for every service day, including after midnight
*As a scheduler, I want journeys generated reliably, so that passengers can always see the next train.* · Must ·
Traces: REQ-LIVE-008, REQ-LIVE-009 · Status: Proposed

- **Given** generation runs twice **Then** no duplicate journeys exist
- **Given** a 23:50 departure **When** viewed at 00:10 **Then** it belongs to the previous service date

### US-085 — See the system's health
*As the person operating SmartRail, I want logs, traces and metrics for the things that fail, so that I find
problems before passengers do.* · Must · Traces: REQ-NFR-004 · Status: Proposed

- **Given** a live scenario **Then** the dashboard shows ingest lag, ETA recompute time, connected clients,
  dead-letter count and device heartbeat freshness
- **Given** a request from the app **Then** one correlation id appears across every log line it caused

### US-086 — Every app fails honestly
*As the organisation, I want each application's degraded behaviour defined and tested.* · Must · Traces:
REQ-NFR-006 · Status: Proposed

- **Given** the degraded-mode matrix **Then** every row has a corresponding test or simulator scenario

## Epic E10 — External catering (thin)

### US-090 — Railway keeps working when the provider fails
*As the organisation, I want a catering outage to have no effect on railway information.* · Could · Traces:
REQ-INT-004 · Status: Proposed

### US-091 — Provider learns about delays as they happen
*As the catering provider, I want ETA changes and cancellations for journeys with orders, without passenger
identity.* · Could · Traces: REQ-INT-002, REQ-INT-003 · Status: Proposed

### US-092 — Order food for collection in one screen
*As a passenger, I want to order for collection at a stop on my journey.* · Could · Traces: REQ-INT-005 · Status:
Proposed
