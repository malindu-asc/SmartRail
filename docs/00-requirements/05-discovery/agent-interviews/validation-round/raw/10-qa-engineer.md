# Interview — QA Engineer

**Simulated by:** Claude subagent · **Date:** 2026-10-06 · **Persona:** QA engineer who must prove a time-dependent, multi-app, offline-capable system works.

## Who I am
I'm a QA engineer with about ten years in test automation, mostly on systems where time and state matter: booking, logistics, a bit of transport. I'm paid to be the sceptic. I don't care that a demo works once; I care that it works the same way on the hundredth run, on a bad network, at 03:00 on a clock-change night. I get nervous when "live" data and "tests" are supposed to share the same pipeline.

## Answers

### Q1. A normal day
I start by reading overnight pipeline results and triaging failures into three buckets: real defects, flaky tests, and environment problems. The third and second buckets eat most of my time. Then I review what changed, decide what needs exploratory testing, and write or maintain automated checks. Late in the day I look at the release candidate and ask whether we have evidence for the risky areas, not whether tests are green. Green means little to me if I can't say what the tests actually cover.

### Q2. What breaks, and what do I do?
Tests break because of time: a hard-coded "now", a timeout that's too short on a busy runner, ordering that differs between runs. They also break because services are shared and someone else's data is in my way. When something fails I first try to reproduce it deterministically; if I can't, I treat it as a defect in the test or the design until proven otherwise. I quarantine flaky tests with an owner and a deadline, because a test everyone ignores is worse than none. In a multi-app system like this, the usual failure is that one app shows the new state and another still shows the old one, and nobody can say which is right.

### Q3. What do I need to see, and when?
I need to see the same event in every app, with the timestamp it was decided and the source it came from, so I can compare them. I need logs or an audit trail that let me reconstruct a sequence after the fact. I need a way to put the system into a known state before a test, and to see afterwards that nothing was left behind. I need this at test time and again during a release check, not only after an incident.

### Q4. What must I never see, or never happen?
I must never see a test that passes by reading its own expectations back from the system under test. I must never see real passenger data in a test environment, or real QR tickets that work. I never want a refund, a charge or a notification to happen twice because a retry arrived. And I never want a "simulate" control to be reachable in production.

### Q5. If the system lied to me
If an app showed a confident wrong time or status, I'd capture the exact inputs, the clock and the version, then check the other apps and the raw source data to find where the disagreement began. I'd raise it as a high severity defect, not a cosmetic one, because a confident error is worse than an honest "unknown". Then I'd ask why no test or monitor caught it. The gap in detection is the second bug.

### Probe 1. "The train is twelve minutes late at 14:37" without waiting
I would not wait and I would not fake it in the UI. I need to control the clock: the system has to use an injectable time source, so my test can say it is 14:37 and move forward on demand. I also need to feed the train-detection input myself, in a scripted, repeatable way. I'm guessing at the details, but since the brief says there's a simulator standing in for the real feed, I'd want it to be scriptable, seedable and replayable, so the same scenario gives the same result. Then I check that the passenger app, the station display, the ops console and any alerts all show twelve minutes and the same estimated arrival. I'd also try the edges: delay exactly at a threshold, delay that grows, delay that recovers, and a clock that crosses midnight. If the only way to get a late train is to wait for the simulator's random behaviour, I can't test this.

### Probe 2. Five end-to-end flows to automate
First, a delay detected by the feed reaching the passenger app, the displays and the ops console with consistent values, because that is the heart of the product. Second, a cancellation producing the right notices and a refund exactly once, because it touches money and trust. Third, a platform change reaching displays and followers, because wrong platforms send people to the wrong place. Fourth, purchase then QR ticket then inspection scan, including a rejected duplicate or invalid ticket. Fifth, the driver working offline, recording stop events and delay reasons, then reconnecting and reconciling without duplicates or overwrites. I'd note that the brief mentions catering ordering too; I'd cover it with contract-level checks rather than a full end-to-end flow, which is a judgement call, not a rule.

### Probe 3. What would make me refuse to sign off
I'd refuse if the clock can't be controlled, so important behaviour can only be checked in real time. I'd refuse if apps disagree about the same event and nobody can show which one is authoritative. I'd refuse if retries or reconnection can double-apply a refund, order or stop event. I'd refuse if a test or simulator control is reachable in a real environment. I'd also refuse if the evidence is only screenshots of a working demo, with no repeatable runs, no recorded defects and no honest statement of what wasn't tested.

## Extracted by the persona
- **What I need**: controllable time; scriptable, repeatable train-detection input; a known starting state and clean-up; a shared event identity and timestamps across apps; an audit trail; evidence I can re-run.
- **What must never happen**: duplicate refunds, charges or notifications; real data or valid tickets in test; simulator controls in production; a confident wrong status shown with no way to tell.
- **What would make me stop trusting it**: apps disagreeing; flaky results nobody owns; tests whose expectations come from the system itself; a demo that only works with a lucky timing.
- **What this system seems to be missing**: any stated rule for how stale data is shown; the definition of "late" (thresholds, rounding); what happens at midnight, clock changes and service days that run past midnight; how conflicting inputs are resolved; test data and environment rules.
- **Where I disagree with the design as described**: relying on a simulator is fine only if it is deterministic and replayable; if it is a random "demo mode", it's useless for proof. Also, "working offline" for drivers is a promise I can't accept without a defined reconciliation rule.
- **Confidence**: Medium. This is drawn from general practice in time-dependent systems, not from railway-specific experience; the specific thresholds and flows are my guesses.
