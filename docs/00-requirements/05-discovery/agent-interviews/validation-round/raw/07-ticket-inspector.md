# Interview — Ticket inspector

**Simulated by:** Claude subagent · **Date:** 2026-10-06 · **Persona:** Ticket inspector checking tickets on a moving train, one hand free, crowded carriage, patchy signal.

## Who I am
I check tickets on board, walking the length of the train between stops. I usually have one hand on a rail or seat and the other on the scanner phone. I have a few minutes between stations to get through a lot of people, so I can't stand and read.

## Answers

### Q1. A normal day
I sign on, check my train and which carriages I'm covering, and board. I start checking soon after departure, because the first stretch is when most people are settled. I move through each carriage, ask for tickets, scan, and move on. Most people are fine and it takes seconds. A few need a conversation: wrong train, wrong stop, a dead phone, a child's ticket. Near the next station I stop and let people off. I write down anything I had to deal with, and I do not like writing much. That's real practice as far as I know, though details differ between railways.

### Q2. What breaks
Signal is the big one, and tunnels and cuttings are the worst. Phones also die, screens are cracked or too dim in sunlight, and people can't find their ticket in their app. Sometimes the scanner is slow while the carriage lurches. When it breaks I don't stop the whole check. I look at what I can see, use judgement, and I'm usually kinder than strict if it's clearly the system's fault rather than the passenger's. What worries me is being the one who wrongly fines someone because a screen said so. I'm drawing on general experience here, not a specific operator.

### Q3. What I need to see, and when
One clear result, large, readable in a second: good or not good. If it's not good I need the reason in plain words, such as "ticket is for a different date" or "ticket was refunded", so I can say it politely. I need to see the stops the ticket covers, as names, not codes. I need to see the passenger's name or something to match to them only if it's needed. I need this in the moment I scan, not afterwards. I don't want to open a second screen unless something is wrong.

### Q4. What I must never see, or never happen
I never want a green tick on a ticket that's refunded or fake. I also never want a red cross with no reason, because then I'm arguing with a passenger without a leg to stand on. I don't want to see someone's payment details or full personal data. I don't want the app to freeze while I'm holding up a queue. I never want to be unsure whether a result is current or old.

### Q5. If the system lied to me
If it said something confidently wrong, I'd stop trusting the scan and go back to looking at the ticket myself, the way we did before. I'd tell the passenger it's a system problem and not penalise them if I'm in doubt. I'd report it, but only when I have time, and I'd want that to be quick. After a couple of wrong results I'd probably just stop using the scanner and tell colleagues to do the same. A wrong red does more harm than a wrong green, because I have to face the passenger.

### Probe 1. Travelling past the station their ticket covers
I need to see the stop the ticket ends at and the stop the train is at or heading to, side by side, in words. If they're riding beyond their stop, I tell them politely that their ticket ends at that station and they need to pay for the extra stretch or get off there. I'd want to be able to say exactly which stop it covers so it isn't a guess. If the train hasn't yet passed their stop, I just say "your ticket's good until X". I'm guessing that some railways let you top up on board, and I wouldn't assume this one does.

### Probe 2. No signal in a tunnel
I keep checking. I can't wait for signal; the carriage won't wait. What I need is for the app to still give me an answer, and to be honest about it: "checked on this phone, could not confirm with the server". I'd accept a ticket that looks right and passes a basic check, but I'd want it marked so it can be confirmed later. I don't want a blank screen or a spinner. If it can't tell, it should say "can't tell" and not guess. I'd rather the app say less and be honest than look sure.

### Probe 3. Refunded ticket or a screenshot
This is exactly where I get caught out, because a screenshot looks like a ticket. I need the app to tell me clearly, in words, "refunded", "cancelled train, refunded" or "this isn't a live ticket". For a screenshot, I need some sign that it's live, like something that moves, or a result that says it isn't genuine. If I scan a refunded one, I need the reason so I can explain: "the train was cancelled and you were refunded, so you'll need a new ticket for the replacement service". I'd want it to be kind in wording, since the passenger often did nothing wrong.

## Extracted by the persona
- **What I need**: A large, one-second answer with a plain-words reason; stop names on the ticket; an honest "can't confirm" when offline; one-handed use.
- **What must never happen**: A green result on a refunded or copied ticket; a red result with no reason; a frozen scanner in a queue; payment details or excess personal data shown to me.
- **What would make me stop trusting it**: One or two confident wrong results, especially wrong reds, or not knowing whether a result was fresh or old.
- **What this system seems to be missing**: What I do when a passenger is on a replacement or substituted train; how I record a decision I made by judgement; how offline checks get confirmed afterward; how a passenger rides past their stop and pays extra.
- **Where I disagree with the design as described**: Putting inspection in the driver's app worries me. Drivers are busy and I'm a different job with different needs, so I'd want to be sure I'm not sharing a screen or permissions that aren't mine. I'm also not sure a single QR on a phone is enough when phones die or screens crack.
- **Confidence**: Medium. I'm drawing on general knowledge of how ticket checks go, not a specific railway, and I'm guessing about top-ups, penalties and offline rules.
