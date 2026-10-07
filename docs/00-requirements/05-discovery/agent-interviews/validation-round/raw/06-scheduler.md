# Interview — Railway scheduler / planner

**Simulated by:** Claude subagent · **Date:** 2026-10-06 · **Persona:** Planner who builds timetables, assigns vehicles and drivers to journeys and schedules engineering works, days ahead.

## Who I am
I'm a scheduler. I build the timetable for the coming weeks, put a vehicle and a driver on every journey, and fit engineering works around the service. I work days ahead, and timetable changes go live on fixed dates. What frightens me is a mistake in my work that then shows up on every screen, in every passenger's pocket, before anyone has noticed.

## Answers

### Q1. A normal day
I start by checking what changed overnight: late engineering requests, vehicles reported unfit, drivers whose rosters moved. Then I work on the timetable period due to go live, usually a week or more out. I check that every journey has a vehicle and a driver, that nobody is rostered beyond their hours or without the right route knowledge, and that engineering works don't collide with services. In the afternoon I reconcile with control about what's actually happening today, because today's reality eats into my plan for tomorrow. Before I leave I check what is queued to go live. I'm guessing the exact tools vary, but the shape of the day is how real planning offices work.

### Q2. What breaks
Vehicles fail inspection, drivers go sick, works overrun, and someone gives me a late change that contradicts something already published. I find out by phone, message or a note from maintenance, and rarely from a system. Then I find a substitute vehicle or driver, check it is allowed, and tell control and the people downstream. The hard part is the knock-on effect: one swap can break two other journeys. I also keep a paper trail of who changed what and why, because I get asked afterwards.

### Q3. What I need to see, and when
I need to see conflicts before I publish, not after: double-booked vehicles, a driver on two trains, a vehicle in the wrong place for its next journey. I need to see what is already live versus what is only draft, and the date a change takes effect. For today's disruption I need a clear list of affected journeys with no gaps. I need to see it as soon as maintenance or the roster changes, not at the end of the shift.

### Q4. What must never happen
A published timetable must never silently differ between the passenger app, the driver app and the station boards. A journey must never lose its driver or vehicle without anyone being told. A draft must never leak out as if it were live. Nobody without the planning role should be able to change assignments. I'd also hate to see an edit overwrite someone else's edit with no trace.

### Q5. If the system lied to me
If it showed an assignment as confirmed when it wasn't, I would stop trusting it and go back to phoning and spreadsheets, and I'd tell everyone else to do the same. I'd check against my own records, find out how long the wrong data had been live, and raise it with control straight away. After one confident mistake I'd verify everything by hand for a long while. I'd want the system to show when something is uncertain, not hide it.

## Probes

### Probe 1. Vehicle into maintenance tomorrow, on three journeys
I need the three journeys listed together, with times and what each vehicle could be swapped with, and I need to know today, ideally the moment maintenance books it. Finding out tomorrow morning is too late. I need to see which spare vehicles are free and where they will be, whether they are suitable for the route, and whether the swap pushes a driver over their hours. If no substitute exists, I need to flag the journey for possible cancellation early, so passengers and catering are not told at the last minute. Everything I change needs a reason recorded.

### Probe 2. I publish a wrong timetable
First, I need to see exactly what went out and to whom. Then I need to put back the previous correct version, or issue a corrected one, quickly and without hand-editing every journey. Passengers who followed affected trains need a clear correction, not a silent change. Drivers must be told, because they act on their duty. The thing I'm most afraid of is people who already made plans or bought tickets on the wrong data. Recovery means the correction reaches all screens at the same time, and there is a record of what was wrong and who approved it. I'd want a check before publishing, but I'm guessing how heavy that check can be.

### Probe 3. Driver calls in sick at 05:30
I get the call or message, record who is off and from when, and see which journeys that driver had today. First trains are the problem because there's little time. I look for a spare or rest-day driver who is qualified for the route and legally rested, and phone them. If I find one, I assign them and the driver's own screen should change to show the new duty. If I can't, I tell control quickly so they can decide on cancellations. The system should show those journeys as uncovered, in a clear state, until I fix them, and should show the assignment history afterwards. Being truthful matters: an uncovered journey must not look covered.

## Extracted by the persona
- **What I need**: Conflict checks before publish; draft versus live clearly separated; early notice of vehicle and driver unavailability; a list of affected journeys; a way to revert a bad publish; a record of who changed what and why.
- **What must never happen**: Different screens showing different timetables; a journey without a driver or vehicle looking fine; drafts appearing as live; unauthorised or untraceable changes.
- **What would make me stop trusting it**: One confident wrong assignment or stale timetable, especially if I only learn of it from a passenger or driver.
- **What this system seems to be missing**: Driver working-hours and route-knowledge rules; vehicle location and spare availability; engineering works as a first-class thing; a lead time for change; a publish-and-revert process; how rosters feed in.
- **Where I disagree with the design as described**: It lumps timetables in with the live operations console as if it were quick to change. Planning needs approval and effective dates, not instant edits. I'm also uneasy that cancellations can be made live in the same console with no distinction from my planned changes.
- **Confidence**: Medium. I'm drawing on general planning practice, but I'm guessing at details such as hours rules and approval steps for this particular railway.
