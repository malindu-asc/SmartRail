# Interview — Customer-service representative

**Simulated by:** Claude subagent · **Date:** 2026-10-06 · **Persona:** Call-centre representative handling passenger calls and messages during disruption, with no control over trains.

## Who I am
I sit in a call centre and answer passengers by phone, chat and message. I have no say over trains and no line to the driver. I only know what my screens tell me. Most contacts are people who are tired, late or angry, and they want a straight answer and a fix.

## Answers

### Q1. A normal day
I log in, check whether any disruption notices are already open, and read the handover notes. Quiet hours are questions about the next train, platform, ticket validity or lost property. Then a delay or cancellation lands and the queue triples in ten minutes. Most calls are "what is happening, what should I do, who pays". I look up the train, tell the person what I can see, and either solve it or pass it on. I need to be fast, because there is always a queue behind the call.

### Q2. What breaks
The commonest break is that I and the passenger see different things. Their app says one thing, the board says another, and my screen says a third. When that happens I apologise, check the latest published status, and tell them honestly if the information is old. I can't fix the data. I flag the mismatch to operations and note it on the case. Payment and refund problems are the other big group. If I can't resolve one, the passenger has to call back, and that is the worst outcome. I'm guessing the volume is highest in the first half hour of a disruption.

### Q3. What I need to see, and when
I need the same status the passenger sees, for the same train, right now. I also need to know when it last changed and who or what changed it. I need to see whether a delay is an estimate or a confirmed figure. For a cancellation I need to know whether the disruption notice is published yet, since customers ring before we are told. I need it at the moment the call starts, not after a search through three screens.

### Q4. What I must never see, or never happen
I must never see full card details or passwords, and I should not see more of a passenger's journey history than the call needs. I must never be able to change a train's status, a platform or a timetable, even by accident. I must never be able to hand out refunds outside the rules, or give someone else's ticket details to a caller who can't prove who they are. Nothing I do should be possible without a trace.

### Q5. If the system lied to me
I would stop quoting it. I'd say to the caller, "My information may be out of date, here is what I can confirm." I'd report it straight away to operations with the train, the time and a screenshot. If it kept happening I would go back to asking control by phone, which is slow and which the whole floor would do at once. After that I would not trust any "on time" label without a time stamp.

### Probe 1. The board said "on time" and the train was cancelled
I need to see what the board and the app showed at the moment the passenger looked, and when the cancellation was entered. I need to know whether the cancellation was entered late or the board was stale. I say, "I'm sorry, you were given the wrong picture. The cancellation was recorded at this time, and here is what I can do for you now." I don't blame the passenger and I don't make up a reason. I then log it as a data complaint so someone checks why the board didn't update.

### Probe 2. Refund after a cancellation, but the app shows an error
I need to confirm the train really was cancelled and that this passenger's ticket is for it. I need to see the ticket's state: bought, used, already refunded or pending. I need to be able to start the refund for them within the rules, and see that it went through or why it didn't. I need to see the error the passenger saw. If I can't, I need a clear way to escalate with a case number so they don't have to explain it all again. I must be able to give them a reference and a time frame.

### Probe 3. What I can see that they can't, and what I must never do
I can see the history of the status: when it changed, the published notices, and the ticket and refund state for a caller I have verified. I can see the internal cause wording only if it is safe to repeat. I must never be able to cancel or move a train, edit a disruption, or see other people's tickets without a reason. I must never see full payment details or be able to override a refund rule without a second person approving. I must not be able to quietly change a passenger's account.

## Extracted by the persona
- **What I need**: the passenger's view of a train, plus when and why it changed; ticket and refund state for a verified caller; a way to start a refund and to escalate with a case reference; a time stamp on everything.
- **What must never happen**: me changing train operations; seeing card or password data; refunds outside the rules without approval; giving one passenger another's details; actions with no record.
- **What would make me stop trusting it**: an "on time" that turns out wrong more than once; no time stamp; my screen disagreeing with the passenger's app with no explanation.
- **What this system seems to be missing**: a customer-service role at all (the brief names one only as an example of a simulated role); a verified-caller step; a case or complaint log; a way to see what the passenger saw at a past moment; a clear refund status.
- **Where I disagree with the design as described**: it talks about passengers, drivers and operations, but not about the people who answer the passengers' calls. If I have to use the operations console to see anything, that gives me too much power. A cancellation that reaches the app but not the boards at the same moment is a problem for me.
- **Confidence**: Medium. I'm drawing on how call centres generally work in disruption, and I'm guessing at this railway's refund rules and tooling.
