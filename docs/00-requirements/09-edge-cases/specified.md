# Specified edge cases

The sixteen edge cases specified to test level. They are chosen because each is either **High** severity, part
of a cross-app flow that the five E2E tests cover, or the single clearest demonstration of a design decision.

Format: Given / When / Then, with the thresholds from the assumptions register so the tests and the docs use the
same numbers.

---

### EDG-002 — Service crosses midnight

- **Given** service 2350 departs Central at 23:50 on service date D and arrives Hill Town 00:35 on calendar date D+1
- **When** a passenger opens the Hill Town board at 00:10 on D+1
- **Then** the journey appears as arriving 00:35, belonging to service date D
- **And** it does not appear as a D+1 journey, and no duplicate exists
- Ref: REQ-LIVE-009 · SIM-13

### EDG-016 — Vehicle failure cascades through its rotation

- **Given** vehicle S12 is assigned to journeys 1042, 1043 and 1050 today
- **When** the fleet controller marks S12 `Failed` at 08:47
- **Then** within 60 s journeys 1043 and 1050 appear in the "no vehicle" queue
- **And** journeys assigned to other vehicles are unaffected
- Ref: REQ-LIVE-018, REQ-LIVE-011 · SIM-06

### EDG-021 — Platform changed after boarding started

- **Given** J-1042 is boarding on platform 3 at Hill Town, departure 08:40, followed by one device
- **When** the controller changes it to platform 5 at 08:34 with reason "platform blocked"
- **Then** within 5 s the platform 3 display shows "now platform 5", the platform 5 display shows
  "changed from 3", the concourse board and app show "Platform 5 (changed from 3)"
- **And** the follower receives one immediate push regardless of throttle
- **And** the Hill Town station manager's console highlights the change at the same moment
- Ref: REQ-LIVE-014 · CF-05 · SIM-14

### EDG-030 — Breakdown between stations

- **Given** J-1042 is Running at +4 between Mill Lane and Hill Town
- **When** the controller marks it `Interrupted`
- **Then** every surface shows "no estimate"; no surface shows a "+n" figure
- **And** position is shown as last known with its time
- **And** followers receive exactly one push for the interruption
- Ref: REQ-LIVE-013 · CF-07 · SIM-06

### EDG-031 — Feed silent for one journey

- **Given** J-1042's last accepted update was at 08:15:00
- **When** no further update arrives
- **Then** at 08:16:00 (± 5 s) values show "as of 08:15"
- **And** at 08:20:00 (± 5 s) confidence is `Unknown`, ETAs show scheduled times with "No live data", and no delay
  figure is shown
- **And** the train is never shown further along the line than its last accepted position
- Ref: REQ-LIVE-004, ASM-019, ASM-020 · CF-06 · SIM-05

### EDG-033 — Rescue locomotive replaces failed vehicle

- **Given** J-1042 is `Interrupted` with S12 assigned since 07:55
- **When** locomotive 841 is assigned at 09:25 and the journey resumes at 09:30
- **Then** J-1042 keeps the same id, history is continuous, and there are two assignment rows (S12 07:55–09:25,
  841 09:25–)
- **And** telemetry from 841 after 09:25 updates J-1042
- **And** followers did not need to re-subscribe
- Ref: REQ-LIVE-010 · CF-07 · SIM-07

### EDG-037 — Serious accident

- **Given** J-1042 is Running
- **When** an incident manager declares a Critical incident and moves J-1042 to `TerminatedEmergency`
- **Then** every public surface shows the approved template text only, and no ETA
- **And** raw telemetry for J-1042 is under legal hold and excluded from the retention purge
- **And** a network controller attempting the same transition receives 403
- Ref: REQ-OPS-007, REQ-OPS-008, REQ-DATA-003 · CF-08 · SIM-17

### EDG-044 — Driver app offline mid-journey

- **Given** the driver app is signed on to J-1042
- **When** connectivity is lost at 08:04
- **Then** the app shows "Offline · last synced 08:04" and a pending count of 0
- **And** after the driver taps Departed, pending is 1 and the action is persisted locally
- **And** the passenger app continues to show live position from the feed
- Ref: REQ-DRV-005 · CF-03 · SIM-09

### EDG-046 — Offline replay after the feed recorded the event

- **Given** the feed recorded departure from Riverside at 08:07:30
- **When** the driver's queued Departed event (device time 08:07:00) replays at 08:19
- **Then** exactly one departure exists, time 08:07:30
- **And** the driver's 08:07:00 and reason code are retained in the event history
- **And** replaying the same batch again changes nothing
- Ref: REQ-DRV-006, REQ-LIVE-006 · CF-03

### EDG-049 — Message sent to an offline driver

- **Given** the driver app is offline
- **When** the controller sends a message
- **Then** its state remains `Sent` and the controller view shows it unacknowledged
- **And** on reconnect it becomes `Delivered`, and `Acknowledged` only after the driver taps it
- Ref: REQ-DRV-007, REQ-OPS-013 · CF-03

### EDG-053 — Implausible telemetry

- **Given** J-1042's last accepted position is 1 km past Riverside at 08:10:00
- **When** an update places it 40 km away at 08:10:10
- **Then** the update is rejected with reason `implausible`, counted in metrics, and J-1042's position is unchanged
- Ref: REQ-LIVE-005 · SIM-11

### EDG-070 — Notification throttling

- **Given** a device follows J-1042
- **When** the expected arrival changes +5, +7, +6, +9, +8, +10 within 5 minutes
- **Then** exactly one push is delivered for that window
- **And** all six changes are visible in-app
- **And** a cancellation arriving in the same window is pushed immediately
- Ref: REQ-PAX-007, ASM-022 · CF-01 · SIM-02

### EDG-072 — Retraction after a push

- **Given** a Major disruption was published and pushed to followers of 4 journeys
- **When** the controller retracts it as published in error
- **Then** it disappears from every surface within 5 s
- **And** each notified device receives one correction push
- **And** publication and retraction are both in the audit log
- Ref: REQ-PAX-009, REQ-OPS-005 · CF-04

### EDG-080 — Display loses network (E2E-5)

- **Given** a platform display showing live departures
- **When** its network is cut at T
- **Then** by T+65 s it shows "Updated hh:mm — live information unavailable" and no live markers
- **And** by T+305 s expected times are replaced by scheduled times
- **And** by T+90 s the device shows Offline in the station manager's device health
- **And** on reconnect it returns to live with a single snapshot request
- Ref: REQ-DSP-004, REQ-DSP-005 · CF-06

### EDG-097 — Station manager edits another station

- **Given** a station manager scoped to Hill Town
- **When** they change a platform at Mill Lane through the API with an otherwise valid request
- **Then** the response is 403, no change is made, and the attempt is audited
- Ref: REQ-SEC-003, REQ-OPS-001

### EDG-110 — Catering provider down (E2E-4 variant)

- **Given** a journey with an outstanding order
- **When** RailBite is stopped and J-1042 is delayed by 8 minutes
- **Then** every railway surface updates normally
- **And** the ETA event stays in the outbox and is retried with backoff
- **And** when RailBite returns, the event is delivered exactly once
- Ref: REQ-INT-004
