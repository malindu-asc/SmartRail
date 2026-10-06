# CF-03 — Driver offline and reconciliation

**Scenario:** J-1042's driver app loses coverage in a tunnel from 08:04 to 08:19. During that time the driver
records departure from Riverside (08:07) and a delay reason, and the controller sends an administrative message.

**The point of this flow:** losing the driver's connection must not blind anyone else, must not lose anything the
driver recorded, and must not leave the controller believing a message was received.

**Test:** E2E-2 · **Simulator:** SIM-09 · **Requirements:** REQ-DRV-005, REQ-DRV-006, REQ-DRV-007,
REQ-DRV-008, REQ-DRV-009, REQ-LIVE-006, ASM-008

## How real railways frame this

On a real railway the driver's phone is not how anyone knows where the train is — train detection in the
signalling does that — and safety-critical instructions go by railway radio. SmartRail mirrors this: the simulator
plays the train-detection feed, so **passengers keep receiving live data throughout**. The driver app is
supplementary: it adds reasons and confirmations, and is a fallback when the feed itself is missing.

## Timeline

| Time | Driver app | Backend | Ops console | Passengers / displays |
|---|---|---|---|---|
| 08:04 | Coverage lost → banner **"Offline · last synced 08:04"**; pending 0 | No driver events | Driver shown "app offline since 08:04" | Unaffected — feed continues |
| 08:06 | — | — | Controller sends message "Stop at Mill Lane extended dwell 2 min" → state `Sent` | — |
| 08:07 | Driver taps **Departed** at Riverside → queued: device time 08:07:00, seq 41, idempotency key, duty D-07; pending 1 | Feed records departure 08:07:30 | Departure shown 08:07:30 (feed) | "Departed Riverside 08:07" |
| 08:09 | Driver picks delay reason "signal" → queued; pending 2 | — | — | — |
| 08:09 | Banner adds **"Messages from control may be missing since 08:04"** | — | — | — |
| 08:19 | Coverage returns; replay seq 41, 42 in order | Idempotency check; seq 41 departure conflicts with feed (08:07:00 vs 08:07:30) → **feed wins**, driver time stored in history; seq 42 reason attached | Reason "signal" appears | Unchanged (time already correct) |
| 08:19 | Message delivered → displayed at next stop → driver taps **Acknowledge** | `Sent → Delivered → Acknowledged` | Controller sees acknowledged 08:20 | — |
| 08:20 | Banner clears; pending 0 | — | Driver shown online | — |

## Failure branches

| Branch | Behaviour | Requirement |
|---|---|---|
| App killed at 08:12 with 2 queued events | Queue persisted locally; replays on next launch | REQ-DRV-005 |
| Same batch replayed twice (flaky reconnect) | Idempotency makes the second replay a no-op | REQ-DRV-006 |
| Device clock is 6 minutes fast | Events accepted; device time flagged implausible; ordering uses sequence + server time; confidence downgraded | REQ-LIVE-006 |
| Feed **also** silent 08:10–08:16 | Driver's queued departure becomes the best available source when it arrives; until then journey goes stale (60 s) then Unknown (300 s) | CF-06 |
| Message validity expires before delivery | Controller alerted to use radio | REQ-DRV-007 |
| Driver's duty ended at 08:15, battery died | Events become `Orphaned`; surfaced at next sign-on for review, not silently applied to a new duty | REQ-DRV-008 |
| Driver D-11 signed on at 08:18 (handover) | D-07's events replaying at 08:19 attach to D-07's duty | REQ-DRV-009 |
| Driver reports skipping a stop while offline | Recorded as a reported skip; controller alerted on replay; passengers informed only after confirmation or feed evidence | source-precedence |

## Assertions for E2E-2

1. Passenger app shows live position throughout 08:04–08:19 (feed-driven).
2. Driver app shows the offline banner and pending count; killing and relaunching keeps the queue.
3. After reconnect, exactly one departure event exists for Riverside, with feed time authoritative and the driver
   time in history.
4. The controller's message transitions to Acknowledged only after the driver taps it.
