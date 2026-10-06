# Final demonstration

**Brief:** §27 — present to stakeholders as a story, not a screen tour, and explain discoveries, assumptions,
exclusions, prioritisation, architecture and UX decisions, security, QA, AI contribution and challenges, and what
comes next. **Status:** Planned — built D20.

## Narrative (draft)

One morning on one line, driven by simulator scenario **SIM-18**, told across every application:

1. **07:42** — J-1042 leaves on time. A commuter follows it in the app; the Hill Town platform screen shows it due.
2. **08:04** — The driver enters a tunnel. The driver app goes offline and says so. Passengers see no change,
   because position comes from the infrastructure feed, not the driver's phone (CF-03).
3. **08:14** — The train is held at a signal. The commuter gets **one** notification, not six; the platform screen
   shows the scheduled time and "+8"; the catering provider gets the new arrival time (CF-01).
4. **08:19** — The driver's signal returns. Their offline entries reconcile; the controller sees the delay reason and
   that the earlier message was acknowledged.
5. **08:34** — A platform is blocked. The controller moves the train to platform 5; the station manager sees it at the
   same moment the screens do (CF-05).
6. **08:40** — A different train fails. The controller terminates it; every surface follows within seconds, with an
   audit record (CF-02).
7. **08:45** — We cut the network to a platform screen. It says it is offline instead of guessing (CF-06).

Then the engineering story: what the brief did not say, what I assumed, what I cut and why, the decisions I would
defend, where AI was wrong, and what I would build next.

## Planned documents

`script.md` · `presentation.md` · `seed-scenario.md` · recording of a full run as fallback
