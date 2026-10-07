# Discovery interview — Railway controller

Scenario probes removed; follow-ups chosen by the persona.

**Simulated by:** Claude subagent (discovery method, no system described) · **Date:** 2026-10-06 · **Persona:** Controller in a national railway control office, deciding holds, cancellations, short terminations and platform changes during disruption.

## Who I am
I'm a controller in the control office. I watch the whole network on several screens with a phone that never stops. When something goes wrong, I decide what happens to the trains: hold, cancel, terminate short, change platform. I coordinate with signallers, drivers, stations and the passenger-information team. In disruption I have seconds, not minutes.

## Answers

### Q1. A normal day
I take over at shift handover and read the log of what is still open: speed restrictions, failed trains, planned works. Then I watch the live picture and the timetable for trains drifting late or conflicting. Most of the day is small decisions: a late train held for a connection, a platform swapped because one is occupied, a crew who will run out of hours. I talk to signallers constantly, because they own the track and I own the plan. I tell the information team when something will change for passengers. At the end I write the handover so the next person doesn't relearn everything.

### Q2. What breaks
Trains fail, signalling faults, people on the line, weather, late crews, and infrastructure blockages. I first make it safe through the signaller, then decide the service: delay, cancel, terminate short, divert. Then I tell everyone who needs to know. The hard part is that many people need to know different things at the same moment, and the phone is how I do it. I'm guessing at exact numbers, but in a bad hour I make dozens of calls.

### Q3. Information I rely on
I need where each train is, what it is, who is driving it, what the timetable says should happen, and what the signaller has told me about the track. I get it from the control screens, the timetable system, radio and phone, and my own notes. I need it right now, and I need to know how old it is. Position data is usually fine, but it lags and sometimes drops out. Crew and stock details are often on a different system, so I phone.

### Q4. What must never happen
Never a conflict between my plan and what the signaller is doing. Never a train sent into a section I believe is closed. Never a passenger told a train is running when I've cancelled it, or the reverse. Never a decision of mine that nobody can trace afterwards. Never losing the record of who was told what and when.

### Q5. Information confidently wrong
I would not know it was wrong unless something contradicted it, and that is the danger. I'd verify with the signaller or the driver by radio before acting on anything serious. I'd treat the source as suspect until checked, and tell colleagues so they don't rely on it either. In the meantime I'd choose the cautious option, usually holding trains. Afterwards I'd want the error recorded.

### Follow-up 1. Correcting a change after people already acted on it
Last time, I'm guessing it was a few weeks ago, I cancelled a stopping service because I misread two similar train numbers on the screen. The first sign was a station phoning to say the train was standing at the platform. I checked the timetable and the signaller's view; it took about two minutes to confirm my error. I reinstated it and rang the driver, who had already been told to stand down, then the information team, then crew control. The driver and station were fixed in about ten minutes, but passengers had already heard "cancelled" and left, and crew control had moved the crew. It ended with the train running about 25 minutes late (I'm guessing) and me writing it in the log by hand afterwards. What went wrong: the correction reached people one by one, in whatever order I could dial.

### Follow-up 2. A train whose position I couldn't trust
Last time, again I'm guessing roughly a month ago, a train's position froze on my screen while the ones around it moved. I noticed after about four minutes because following trains were bunching up. I asked the signaller what they could see on the track, then tried the driver by radio, who answered after a minute or two. The train was fine; the data had dropped out. In the meantime I had held two following trains for roughly ten minutes (guessing) and told the information team to expect delays. What went wrong: nothing on the screen said the position was stale, so I only knew from the context. It ended with a delay I had caused for no real reason, but the cautious choice was right.

## Extracted by the persona
- **What I rely on today**: control screens, timetable system, signallers, radio and phone, drivers, station staff, handover log, my own notes.
- **What is hardest or most error-prone in my work**: correcting a mistake after others have acted; telling many people one change together; judging data that is old or missing; shift handover.
- **What must never happen**: a plan that conflicts with the signaller; a train sent into a closed section; passengers told the opposite of reality; a decision with no trace of who was told what.
- **What would make me stop trusting information**: position that contradicts the signaller or driver; a source that is stale without saying so; two systems disagreeing; a correction that never reached everyone.
- **What nobody gives me today that I would want**: a visible age on every piece of data; one record of who was told what and when; a reliable way to withdraw a mistaken message from everyone at once.
- **Confidence**: Medium. Plausible working practice, but simulated; the incidents, times and tools are guesses.
