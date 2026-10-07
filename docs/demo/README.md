# Final demonstration

**Brief:** §27 — present to stakeholders as a story, not a screen tour, and explain discoveries, assumptions,
exclusions, prioritisation, architecture and UX decisions, security, QA, AI contribution and challenges, and what
comes next. **Status:** Planned — built D20.

## Narrative (draft)

One morning on one line, driven by simulator scenario **SIM-18**, told across every deliverable:

1. **07:35** — A commuter signs in and buys a ticket to Hill Town. The fake provider confirms; the ticket and its QR
   appear in the wallet (CF-09).
2. **07:42** — J-1042 leaves on time. The commuter follows it; the Hill Town platform screen shows it due.
3. **08:04** — The driver enters a tunnel. The driver app goes offline and says so. Passengers see no change, because
   position comes from the infrastructure feed, not the driver's phone (CF-03).
4. **08:14** — The train is held at a signal. The commuter gets **one** notification, not six; the platform screen shows
   the scheduled time and "+8"; the ticket is **unchanged** by the delay (CF-01).
5. **08:19** — The driver's signal returns. Offline entries reconcile; the controller sees the delay reason and that the
   earlier message was acknowledged.
6. **08:34** — A platform is blocked. The controller moves the train to platform 5; the station view shows it at the
   same moment the screens do (CF-05).
7. **08:40** — The train fails. The controller terminates it at Mill Lane; every surface follows within seconds with an
   audit record (CF-02). The commuter's ticket becomes **Affected**, and a push says so (CF-10).
8. **08:41** — The commuter taps **Refund**. It completes once, even when tapped twice. A forced provider failure is
   retried automatically.
9. **08:43** — An inspector scans the same QR on a train that is still running: **rejected, refunded** (CF-11).
10. **08:45** — We cut the network to a platform screen. It says it is offline instead of guessing (CF-06).

Then the engineering story: what the brief did not say, what I assumed, what I cut and why (catering, reserved seats,
rebooking), the decisions I would defend, where AI was wrong, and what I would build next.

## Planned documents

`script.md` · `presentation.md` · `seed-scenario.md` · recording of a full run as fallback
