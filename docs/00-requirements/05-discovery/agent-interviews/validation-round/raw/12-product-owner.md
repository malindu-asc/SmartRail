# Interview — Product Owner

**Simulated by:** Claude subagent · **Date:** 2026-10-06 · **Persona:** Product owner for a national railway's digital platform, accountable for value, scope and executive-facing outcomes; impatient with feature lists.

## Who I am
I own the digital platform roadmap for a national railway. I answer for what we promise, what we ship, and what the executives see. I have been burned by demos that were a tour of screens with no point. I will push hard on scope, because one engineer with AI help has about two weeks.

## Answers

### Q1. A normal day
I start with the delivery picture: what moved yesterday, what is blocked, what changed in the operational world that affects priorities. Then I spend most of the day in conversations: operations wanting something, customer service reporting complaints, finance asking about cost, executives asking "when". I decide what is in and what is out, and I say no a lot. Before any executive session I rehearse the story, because they remember one clear thing, not twelve. I'm drawing on real product-ownership practice, not a specific organisation.

### Q2. What breaks
Scope creeps, and every stakeholder's request sounds reasonable alone. Integrations are usually late and the demo environment is never like production. When something breaks I cut scope, not quality, and I tell people early, in writing, what moved and why. I never let a half-working feature into an executive demo; a clean smaller demo beats an ambitious flaky one.

### Q3. What I need to see, and when
Early, a one-page statement of what is in, what is out, and the riskiest assumptions. Midway, a working thin slice end to end, even ugly. Before the executive session, a rehearsed run with a fallback for every step that depends on live data. I need to see honest status, including what is not working.

### Q4. What I must never see
A demo that fails live. Claims that are not true, like "real-time" when the data is simulated and nobody said so. Anything that looks like it replaces signalling or safety procedures. Personal data of passengers shown on a screen in front of executives. And a scope list that quietly doubled.

### Q5. If the system lied to me
I would stop the demo or the rollout and ask what the source of the number was. For a passenger-facing wrong answer, say a train shown on time that is cancelled, I would treat it as a serious incident, because trust is the whole product. I would want to know how long it was wrong and who saw it. Then I would want the system to say "I don't know" next time rather than guess.

### Probe 1. What the demo must show, in order
First, the problem in one sentence: a disruption, and passengers and staff not agreeing on what is happening. Second, a normal running network on the operations console, so the audience sees the live picture. Third, a train delayed and then cancelled; I want to see that one decision in operations reaches the passenger app, the station display and the driver within moments, without anyone re-typing it. Fourth, the passenger follows the train, gets the alert, and gets a refund for the cancelled ticket; this is the money moment, where value is obvious. Fifth, the driver side: the control message arrives, the driver acknowledges. Last, thirty seconds on what was left out and why, and how the data would be real in production. One story, one disruption, six minutes.

### Probe 2. Cutting half
First to go: the interactive terminal, anything catering beyond a token integration, broad timetable editing, extra roles, reporting, and polish on secondary screens. Platform displays can be a simple, read-only screen. I'm guessing a two-week build cannot afford depth in all five apps, so some must be thin and honestly labelled thin. Never cut: the disruption propagating across passenger, operations, driver and display; the simulator that makes the demo repeatable; basic access control so a passenger cannot do an operator's job; and the ticket and refund path if ticketing is in the story at all. If ticketing is the thing that threatens the schedule, I would cut ticketing depth, not the disruption story.

### Probe 3. Rejection and biggest risk
I would reject it if the demo needs the engineer to explain around broken parts, if it cannot be reset and rerun, if the story is a screen tour, or if the engineer cannot explain how any part works when challenged. The biggest risk I see is breadth: six apps plus a simulator plus ticketing in two weeks means each ends up shallow and the integration between them, which is the actual story, gets the least time. The second risk is that AI-generated code is accepted without understanding and falls apart in the review questions.

## Extracted by the persona
- **What I need**: one clear disruption story; a written scope with explicit exclusions; a repeatable demo with a reset; honest labelling of what is simulated.
- **What must never happen**: a live demo failure; claims of real-time or safety function that are not true; passenger personal data on show; hidden scope growth.
- **What would make me stop trusting it**: any confidently wrong status; a demo that only works with the builder's hand on it; features that exist on a slide but not in the build.
- **What this system seems to be missing**: a clear statement of success measures for executives; a plan for what "good enough" means per app; a fallback plan if live data fails during the demo.
- **Where I disagree with the design as described**: it lists too many surfaces for the time; the terminal and the ticket-inspection mode add breadth without strengthening the story; food ordering is in the brief but I would defer it.
- **Confidence**: Medium. I know product ownership and demo risk well; I'm guessing about this railway's real priorities and the engineer's actual pace.
