# Interview — Daily commuter (passenger)

**Simulated by:** Claude subagent · **Date:** 2026-10-06 · **Persona:** Suburban-line commuter, same morning and evening train five days a week for years, one-handed phone use in sunlight, not technical, no patience for nagging apps.

## Who I am
I catch the same train in the morning and the same one home in the evening, Monday to Friday. I know my platform and I know roughly how the line behaves. I look at my phone while walking to the station, usually with one hand and in bright sun, so I get a few seconds at most. I am not a technical person and I just want to know whether to hurry.

## Answers

### Q1. A normal day
I leave home, and on the walk I check one thing: is my train on time, and is it on my usual platform. If yes, I put the phone away. On the platform I glance at the board more than my phone. On the train I mostly don't look at anything until something odd happens, like we stop between stations. In the evening it is the same again. I have a rhythm, and an app that interrupts it is worse than no app. I don't browse timetables; I already know them.

### Q2. What breaks
Trains run late, platforms change at the last minute, and sometimes a train is just cancelled and I find out on the platform. When it breaks I look for the next train that gets me where I need to go, and I ask other people standing around. Honestly, the worst thing is not knowing. A clear "late by 12 minutes" I can work with. Silence I can't. I'm guessing a lot of other commuters behave the same, but that is from my own experience only.

### Q3. What I need to see, and when
Before I leave the house and while I'm walking: is my train running, how late, which platform. That should be the first thing on screen with no tapping around. Big text, strong contrast, readable in sun. I want it to already know my train. On the platform I want to know if the platform changes. I don't need to see where the train is on a map every day; that matters more when things go wrong.

### Q4. What must never happen
I must never be told the wrong platform confidently and then run up the stairs for nothing. I must never get a flood of messages. I must never be asked to log in again, or hit a screen of pop-ups, when I have five seconds. And I must never lose money for a train that did not run.

### Q5. If the system lied to me
I would stop believing the app and go back to the station board and asking staff. If it happened twice, I'd delete it. One confident wrong answer costs me more trust than ten "I'm not sure" answers. I'd rather it said "not sure" than guess.

## Probes

### Probe 1. Information stops updating for ten minutes at the station
What I see now is usually a number that just sits there and looks fine. That is the dangerous part: I assume it is true. I want the app to say plainly that it has not heard anything new for a while, and when it last did, so I know not to trust the time. I'd want to know whether to wait or go find another train. What I'd actually do is look up at the platform board, ask someone in uniform, and stand near the usual spot. I would not keep refreshing, though I'd probably pull down once.

### Probe 2. A notification for every small change
I would turn the notifications off within a day, probably all of them, and then I'd miss the one that mattered. A two-minute shift is not worth buzzing me. I want to hear about it when my train is cancelled, when the platform changes, or when it is badly late. Nothing else. I'm one of those people who ignores an app after three pointless buzzes.

### Probe 3. Ticket bought, then train cancelled
I expect to be told straight away, ideally before I'm at the station, and I expect my money back without having to fill in a form or phone anyone. I'd want it to just say "your train is cancelled, you'll be refunded" and show me what I can take instead. If I'm still at the station I need that within minutes. If it takes days to hear about the money, I'll be annoyed, but not about a short wait if it is clearly promised. If I have to chase it, that is when I get angry.

## Extracted by the persona
- **What I need**: My own train first, no taps; on time or how late; platform; sunlight-readable big text; a clear sign when information is stale; alerts only for cancellation, platform change, or big delay.
- **What must never happen**: A confident wrong platform or time; notification spam; forced login or pop-ups at the wrong moment; paying for a train that did not run.
- **What would make me stop trusting it**: One confident wrong answer; information that silently stops updating; alerts that arrive after I already found out on the platform.
- **What this system seems to be missing**: Nothing about knowing my regular trains, or about the app showing how fresh its information is. It describes a lot of features, but not a quiet "everything is fine" view. It also doesn't say how fast a refund arrives.
- **Where I disagree with the design as described**: "Alerts for followed trains" sounds like I must set things up; I'd want my usual trains to be picked up without effort. The long feature list worries me, because I only want a small part of it.
- **Confidence**: Medium. This is drawn from ordinary commuter experience, but I'm guessing about how a real railway handles refunds and data outages.
