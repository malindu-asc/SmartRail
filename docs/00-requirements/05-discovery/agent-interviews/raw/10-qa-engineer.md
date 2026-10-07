# Discovery interview — QA engineer (time-dependent, multi-application, partly-offline systems)

Scenario probes removed; follow-ups chosen by the persona.

**Simulated by:** Claude subagent (discovery method, no system described) · **Date:** 2026-10-06 · **Persona:** QA engineer who must prove a time-dependent, multi-application, partly-offline system works.

## Who I am
I'm a QA engineer with about ten years in test automation, the last few on systems that depend on time and on several components agreeing with each other. In a national railway I'd be the person asked "how do we know it works?" and expected to answer with evidence rather than opinion. I care about determinism, testability and what would convince a sceptic. I'm guessing at railway specifics, since I'm describing how I'd work.

## Answers

### Q1. A normal day
I start with the overnight pipeline results and triage failures into three buckets: real defect, environment problem, flaky test. Flaky tests get quarantined with an owner and a deadline, not ignored. Then I review new changes for testability: can I set the state I need, and can I observe the outcome? I write or maintain automated checks, run exploratory sessions on risky areas, and sit in refinement asking "what is the expected result, exactly?". Late in the day I update the evidence record so anyone can see what was verified, against which build.

### Q2. What breaks, and what do I do?
Time breaks things most: tests that depend on the wall clock, time zones, daylight saving, midnight rollover, and "now". Next is shared environments, where someone else's data or deploy changes my result. Third is asynchronous behaviour, where a test checks before the message has arrived and passes or fails by luck. When something breaks I reproduce it with a fixed seed and a fixed clock, capture logs and the exact inputs, and refuse to call it "intermittent" until I know why. If I can't reproduce, I add observability before closing it.

### Q3. Information I rely on
I need requirements stated as checkable statements with thresholds, not adjectives. I need a way to know the build and configuration under test, plus logs and traces correlated across components by a shared identifier. I get these today from tickets, pipeline output and logs, and I need them before testing starts, not after. I also need known-good reference data and a record of which data was real versus synthetic.

### Q4. What must never happen
I must never be unable to tell a passed test from a test that didn't run. Test data and test notifications must never reach real people or real operational records. A fix must never silently remove a check to make a pipeline go green. And nobody should accept "works on my machine" or a screenshot as evidence.

### Q5. If the information were confidently wrong
Confidently wrong is the worst case, because nobody investigates it. I'd cross-check against an independent second source, compare timestamps and sequence numbers, and look for the point where two components disagree. I'd treat it as a defect in the evidence chain first, quarantine the affected results, and re-run on a clean setup. Then I'd ask what check should have caught it and add one.

## Follow-ups

### Follow-up 1. Clock-dependent test that failed only some nights (Q2, cost: time and trust)
I'm describing a typical case, not a named project. Overnight run, one time-based check red, green on re-run. First I saw the same test failing about one night in five. I checked the logs, then asked the developer who owned that code. We lost roughly two days: it only failed when the run crossed midnight in a different time zone from the test server. The wrong turn was blaming the shared environment first. It ended with the clock made injectable and the test pinned to a fixed time; the team trusted the pipeline less for a week.

### Follow-up 2. Test notification reaching real people (Q4, cost: safety and trust)
I'm guessing at the details, since this is the one I fear most rather than one I can date exactly. The pattern: a test run points at a configuration still holding real contact details. First sign is a real person replying "why did I get this?". I'd check the config, stop the run, and contact whoever owns that channel; maybe an hour to contain, days to explain. What went wrong was no separation between test and real recipients. It ended with an allow-list of recipients enforced in the test environment.

## Extracted by the persona
- **What I rely on today** (sources, tools, people): tickets and requirements, pipeline output, logs and traces, shared test environments, developers and analysts.
- **What is hardest or most error-prone in my work**: time-dependent logic, asynchronous timing, cross-application consistency, offline-to-online sync, flaky tests eroding trust, unstable shared environments. (Follow-up 1 adds: time-zone and midnight edges.)
- **What must never happen**: test data reaching real people or records; passes that can't be told from not-run; checks silently removed; sign-off without evidence tied to a build.
- **What would make me stop trusting information**: results that can't be reproduced, timestamps or sequences that disagree between components, anything confident but with no traceable source.
- **What nobody gives me today that I would want**: a controllable clock, replayable inputs, a correlation identifier across components, measurable acceptance criteria, and a hard test/real recipient separation (new from Follow-up 2).
- **Confidence**: Medium. Confident about testing practice; guessing about railway specifics and the follow-up incidents.
