# Interview — Railway Controller

**Simulated by:** Claude subagent · **Date:** 2026-10-06 · **Persona:** Control-office railway controller who watches the whole network, decides holds, cancellations, short terminations and platform changes, and coordinates with signallers, drivers, stations and the information team.

## Who I am
I am a controller in the control office. I work shifts, with several screens in front of me and a phone that rarely stops. When things go wrong I have seconds, not minutes, to decide. I make the call on holding, cancelling, terminating short and platform changes, then make sure everyone else hears it consistently.

## Answers

### Q1. A normal day
I take over from the previous shift with a verbal and written handover: what is late, what is blocked, which units or crews are out of place, what is planned (engineering work, special trains). Then I watch the network. Most of the day is small things: a train a few minutes down, a platform clash at a busy station, a driver asking about a stop. I talk to signallers about conflicts, tell stations and the information team when something changes, and log decisions. Quiet periods are for planning ahead: knowing where crews and trains will be in two hours. I am not sitting in a calm tool; I am glancing between screens while on the phone. (Drawing on real practice, in general terms.)

### Q2. What breaks, and what do I do
Trains break down, signals fail, people trespass, crews run out of hours, the weather closes a section. First I make the section and the people safe, which is the signaller's and the safety procedure's job, not a screen's. Then I decide the service: hold, delay, cancel, terminate short, divert. I tell the driver by radio or phone, tell stations, and get a message to passengers that says what is happening and what to do. Then I keep re-deciding as the situation changes. The worst part is that the first decision is rarely the last one.

### Q3. What do I need to see, and when
I need the whole network at a glance, with problems standing out: late, no report, cancelled, blocked. I need to know how fresh each piece of information is. For a given train I need where it is, what it is meant to do next, who is driving it, and what has already been told to passengers. During disruption I need to see what I have changed and whether it has gone out, so I do not do things twice. I need it immediately, not after a refresh.

### Q4. What must I never see, or never happen
I must never see a train drawn confidently somewhere it is not. I must never have a cancel go through on one accidental click with nothing to stop me. I must never find passengers, drivers and station screens telling three different stories. I must never lose a change I made because of a screen glitch, and never be unsure whether it took effect.

### Q5. If the system lied to me
I would stop trusting it for that train, and probably more widely, and go back to phone, radio and the signaller. I would say so out loud to the team, because others are looking at the same screen. I would want to flag the wrong item so someone fixes it, and I would be angry if I had to find out from a passenger or a station rather than from the system. One confident lie costs more trust than ten honest "I don't know" markers.

### Probe 1. Landslide in the morning peak
First I get the facts by phone from the signaller or driver: where exactly, which lines, is anyone at risk, is a train already stuck. Safety comes first and happens outside any screen. Then I stop trains approaching the section and decide which services I hold, which I terminate short either side, and which I cancel. I identify trains already past the blockage point and where they can be turned back. I ask stations to hold or redirect crowds. I need the system to show me every affected train in one list, let me apply the decision to many trains at once rather than one by one, and tell passengers consistently on the app and station screens: what is affected, what is not, and an honest "no estimate yet" if I do not have a time. I would not want it to invent a restart time. Afterwards I need to reverse or change those decisions quickly as the picture changes, and see who has been told what. Crews and vehicles will be out of place for hours afterwards, and I need to see that.

### Probe 2. A train I believe is moving has not reported for five minutes
I want to see that it has gone silent, shown as "no report" with the time of the last report and last known place, not as a train carrying on happily at the timetabled position. I would not accept a guess drawn as fact. I would phone the driver, ask the signaller what they can see, and check whether neighbouring trains saw it pass. Five minutes can be a dead zone or a phone fault, or it can be something serious. Until I know, passengers should see the last known state and that the position is uncertain, not a confident delay figure. If it is not found, I escalate through the normal incident procedure.

### Probe 3. I cancel the wrong train
I want to put it right fast, in a few seconds, and have it clearly marked as a correction rather than a second event that confuses everything. The train must come back in the network view with the right status, and I want a record of who did what and why, because I will be asked. Passengers who have seen the cancellation should get a prompt, clear message that the train is running after all, not silence, and anyone who already started a refund or rebooked needs to be handled fairly; I'm guessing that the refund rules need a decision from the business side. Station screens must flip back at the same time as the app. If the train has genuinely already left or been missed, I need to know that it cannot simply be reinstated and what the next step is.

## Extracted by the persona
- **What I need**: Live network view with problems highlighted; age of every piece of data; multi-train actions; a confirm step for destructive actions with a quick, visible undo; one consistent message to all audiences; a record of every decision.
- **What must never happen**: A confident but wrong train position; one-click irreversible cancellation; different stories on app, station screens and driver messages; a change that silently did not apply.
- **What would make me stop trusting it**: A single confident error I found by phone rather than on screen; stale data not marked as stale; screens disagreeing with each other.
- **What this system seems to be missing**: Any mention of how stale or missing data is shown; handover between shifts; how a decision is corrected; handling many trains at once; how it works alongside signalling, radio and safety procedure; what happens to crews and vehicles after disruption.
- **Where I disagree with the design as described**: Position from detection data is sensible, but the description says nothing about gaps in that data. Drivers' "delay reasons" must not overrule my own decision. A phone app cannot be where I learn that a train is stopped.
- **Confidence**: Medium. I am drawing on general knowledge of control-room work, not on a specific organisation's procedures, and I'm guessing on the refund and correction rules.
