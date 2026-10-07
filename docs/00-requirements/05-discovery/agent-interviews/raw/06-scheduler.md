# Discovery interview — Railway scheduler / planner

Scenario probes removed; follow-ups chosen by the persona.

**Simulated by:** Claude subagent (discovery method, no system described) · **Date:** 2026-10-06 · **Persona:** Timetable and resource planner at a national railway; plans days to months ahead.

## Who I am
I build timetables and assign vehicles and drivers to journeys. I also plan engineering works so they do not wreck the service. I work days, weeks and months ahead, and changes go live on fixed dates. What scares me is a mistake that is copied everywhere: every screen, every announcement, every passenger.

## Answers

### Q1
I start by checking what changed overnight: new engineering works requests, vehicle availability from the maintenance people, and driver rosters. Then I work on the next timetable change date, building or adjusting journeys and checking that each one has a vehicle and a driver who is legally rested and qualified for that route. I spend a lot of time on conflicts, such as a vehicle in two places or a driver with too short a turnaround. I talk to depot supervisors and maintenance by phone and email more than I would like. Before anything is released I check it again, because after release it is hard to pull back. Late in the day I hand the next days' plan to operations.

### Q2
Vehicles fail, drivers go sick, and works overrun. Maintenance finds a defect and the vehicle is out, but I often hear by phone or a message rather than from a system I trust. I then look at what that vehicle was doing, find a spare of the right type, and check it can run the route. If there is no spare I tell operations so they can decide what to cancel. Roster gaps are handled by a duty manager, and I find out afterwards. Whatever I do, I need to tell several people, and I can never be sure everyone got it.

### Q3
I rely on the timetable base, vehicle status and maintenance plans, driver rosters with route knowledge and rest rules, and the engineering works calendar. I get these from several separate lists, spreadsheets and people. I need them days ahead for planning, and the same morning for anything that has changed. The worst gap is knowing which version is the current one. I am guessing that others hold older copies than I think.

### Q4
Never a vehicle or driver double-booked without anyone knowing. Never a published timetable that passengers and staff see differently from each other. Never an unqualified driver on a route, or a vehicle that is not allowed to run. Never a change that goes live with nobody able to say who made it and why.

### Q5
If I thought a source was confidently wrong, I would stop using it and phone someone who can see the real thing: the depot, the signaller, the maintenance lead. I would hold any release that depended on it. A wrong value that looks certain is worse than a gap, because a gap makes me ask. I would also want to know how long it had been wrong and who else used it.

## Follow-ups

### Follow-up 1 — A wrong version of the plan in circulation (cost: trust)
I'm guessing at exact dates; this is the most recent time I recall. A depot supervisor phoned to say a driver had turned up for a duty I had already changed. First I saw a mismatch between his sheet and mine. I checked my file, then the shared copy, then emailed the revised plan to the three depots and operations. That took about two hours. What went wrong: one depot still worked from the earlier copy and I only found out when I rang to confirm. It ended with a late swap of drivers, one short delay, and me phoning everyone before I would trust any plan again.

### Follow-up 2 — A late vehicle or driver change under time pressure (cost: safety)
Last time, a vehicle came out of service the evening before. Maintenance rang me at about 18:00. I looked at its three journeys and found a spare of the same type. I checked the route approval by memory and a quick look at a list, and I did not check the driver's route knowledge on that vehicle type. The whole thing took about forty minutes. The next morning the depot noticed the driver was not signed off on that type, and it was swapped at the last minute. It ended without harm, but I'm guessing it could easily have gone the other way. The rush is the cause.

## Extracted by the persona
- **What I rely on today**: timetable base, vehicle and maintenance lists, driver rosters and route-knowledge records, engineering works calendar, and phone calls and emails with depots, maintenance and operations.
- **What is hardest or most error-prone in my work**: knowing which version is current, confirming a change reached every depot, and checking driver and vehicle compatibility when rushed.
- **What must never happen**: double-booked resources nobody knows about, different people seeing different plans, unqualified or unrested drivers on duty, and changes with no record of who and why.
- **What would make me stop trusting information**: anything that contradicts what the depot says, stale data with no timestamp, or a source that has been wrong once and not explained.
- **What nobody gives me today that I would want**: one trusted current picture of vehicles and drivers, early reliable warning when a plan is no longer possible, and proof that a change reached everyone.
- **Confidence**: Medium. The flow is plausible, but I'm guessing at the rules and tools of a real national railway.
