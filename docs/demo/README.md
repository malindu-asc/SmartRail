# Final demonstration

**Brief:** §27 — present to stakeholders as a story, not a screen tour, and explain discoveries, assumptions, exclusions,
prioritisation, architecture and UX decisions, security, QA, AI contribution and challenges, and what comes next.
**Status:** Planned — built D20.

## The story in two acts

**Act 1 — the happy path between two stations (about three minutes, nothing goes wrong).** Driven by the simulator, repeatable
with a reset.

1. A passenger signs in, picks the train from **Central to Riverside**, pays with the fake provider and gets a QR ticket (CF-09).
2. **Board scan at the Central gate:** valid, the ticket becomes Boarded (CF-11).
3. The train departs. The passenger app shows "Running · on time"; the platform display shows the departure.
4. The **driver app** records Departed and Arrived at each stop. The controller and the Central Station Master see it live.
5. The passenger follows the journey stop by stop, and opens **Food at Riverside**: the menu with prices from the separate catering service, and a call-to-order number.
6. **Exit scan at the Riverside gate:** the ticket becomes Completed.
7. The **Station Masters** see the scans in their logs, each for their own station only.

**Act 2 — one thing goes wrong, and the system stays honest.** A second journey:

8. The same QR scanned twice at boarding is rejected. Someone exits two stops past their destination and the gate shows the excess
   fare; staff collect it in person (CF-11).
9. The train is held. The passenger gets **one** alert, not six; the board shows the scheduled time and "+8"; the ticket is unchanged (CF-01).
10. The driver loses signal in a tunnel. The app says so, queues what the driver records, and passengers see no change because
    position comes from the train-detection feed (CF-03).
11. A platform is blocked. The controller changes it; the Station Master sees it at once; screens show "changed from" (CF-05).
12. The train fails. The controller terminates it at Mill Lane; every surface follows within seconds with an audit record (CF-02).
    The passenger's ticket becomes **Affected**, they tap Refund, and it completes once even if tapped twice (CF-10). The old QR is
    rejected at the gate and by the inspector.
13. We cut the network to a platform screen and a gate. They say they cannot verify, instead of guessing (CF-06). Stopping the catering service shows "Menu unavailable" while everything else keeps working.

Then the engineering story: what the brief did not say, what I assumed, what I cut and why (catering ordering, reserved seats, rebooking, physical
gates), the decisions I would defend, where AI was wrong, and what I would build next.

## Rules for the demo

- Everything is seeded and resettable; rehearse twice end to end.
- One scenario clock, so every screen shows the same "as of" time.
- Label what is simulated: the train-detection feed, the payment provider, the gates.
- A recorded run as a fallback.

## Planned documents

`script.md` · `presentation.md` · `seed-scenario.md` · recording of a full run
