# Stakeholder interviews (simulated)

> **These interviews are simulated.** They were run with AI personas built from [personas.md](personas.md) using
> the fixed protocol in [method.md](method.md), and then reviewed and edited by me. They represent plausible,
> practice-informed viewpoints, **not statements from this organisation's staff** (ASM-034). Each ends with what it
> changed, so the influence of the simulation on the requirements is traceable.

Protocol: (1) a normal day · (2) what breaks · (3) what you need to see · (4) what you must never see ·
(5) **what would you do if the system lied to you?**

---

## INT-01 — Driver (PER-05)

**A normal day.** "Sign on at the depot, check the notices, walk to the unit, do my checks. Out of the depot, run
the diagram — three or four trips. Everything that matters for safety comes by radio from the signaller and from
the signals in front of me."

**What breaks.** "Unit faults. Doors that won't close. Late crew coming the other way so I wait. Someone on the
line. Then I'm held at a red and nobody tells me why for ten minutes."

**What you need.** "My duty. The stops — especially anything unusual, like a stop that's been added or one we're
skipping. Any speed restrictions. And when I'm late, somewhere to say *why*, because otherwise it gets put down to
me."

**Never.** "Anything that wants me to read or type while I'm moving. And I don't want my phone to be the thing
that decides whether passengers know where the train is. The phone dies in the tunnel every single day."

**If it lied.** "If the app said a message was sent and I never got it, and then someone says I ignored an
instruction — that's a disciplinary. So I need to know when I might have missed something."

**What it changed:** the driver app becomes supplementary, not the position source (ASM-008); no free text
between stops (REQ-DRV-004); reason codes on delay (REQ-DRV-003); offline banner and "messages may be missing
since …" (REQ-DRV-005, REQ-DRV-007); duty-scoped location only (REQ-DRV-010).

---

## INT-02 — Network controller (PER-06)

**A normal day.** "Watching the running, mostly. It's quiet until it isn't. Then I've got a failed train, three
following trains backing up, a station asking which platform, and the information team asking what to tell
people."

**What breaks.** "Information going out inconsistent. I cancel something and the app has it but the platform board
doesn't. Or the driver hasn't been told."

**What you need.** "Every train, now, with how late it is and whether that's real. If a train has gone quiet I need
to see that — a train I *think* is moving and actually isn't is the worst case."

**Never.** "A train painted as on time just because no data came in."

**If it lied.** "I'd stop trusting the screen and go back to the phone. Then the system is dead weight."

**What it changed:** Unknown as a first-class state visible in ops (REQ-LIVE-004); one action propagates to every
surface (CF-02); delivery and acknowledgement states (REQ-DRV-007); reason codes on every cancellation
(REQ-OPS-004).

---

## INT-03 — Station manager (PER-07)

**A normal day.** "Rush hour is the job. Staff on the platforms, gates, the information point."

**What breaks.** "Platform changes. If the board flips from 3 to 5 two minutes before departure, three hundred
people go down one staircase and I've got nobody there. And dead screens — nobody tells me a screen is off until a
passenger complains."

**What you need.** "To know about a platform change at the same moment it goes public, ideally before. And to know
when a screen is dead."

**Never.** "A board that's confidently wrong."

**If it lied.** "If the board said on time and the train was cancelled, the crowd builds and builds. That's how
people get hurt."

**What it changed:** device heartbeat and health (REQ-DSP-005, REQ-OPS-009); station-scoped view of changes
(CF-05); platform changes shown as "changed from" (REQ-LIVE-014); displays honest when offline (REQ-DSP-004).

---

## INT-04 — Daily commuter (PER-01)

**A normal day.** "Check the app walking to the station. Look at the board when I get there."

**What breaks.** "The app says on time, the board says delayed. Which one?"

**What you need.** "Late or not. Platform. That's it."

**Never.** "Ten notifications for one delay."

**If it lied.** "I'd delete it."

**What it changed:** one journey state rendered everywhere (system-context); delay as a delta with the scheduled
time kept (REQ-PAX-002); notification throttling (REQ-PAX-007, ASM-022).

---

## INT-05 — Occasional traveller (PER-02)

**What breaks.** "I don't know the words. 'Terminates', 'TBA', 'formed of'. And I don't know if my change is still
OK when the first train's late."

**What you need.** "Plain words. Whether my connection still works."

**If it lied.** "I'd ask a person. If there was no person, I'd panic."

**What it changed:** connection-at-risk indicator (REQ-PAX-006); plain-language copy guidelines (01-ux, to come);
terminal as an equal channel (REQ-TRM-001).

---

## INT-06 — Passenger with low vision (PER-03)

**What breaks.** "Apps that put a red dot next to a train and nothing else. TalkBack just reads 'image'."

**What you need.** "Every status as words. Platform as early as possible — I need time."

**Never.** "Colour-only status. Things that move or update under my finger while I'm reading."

**If it lied.** "I'd miss the train and then miss my assistance booking."

**What it changed:** status as icon + text + colour (REQ-PAX-004); screen-reader pass (REQ-PAX-014); stable
list positions during live updates (UX requirement for 01-ux).

---

## INT-07 — Scheduler (PER-08)

**What breaks.** "Someone edits the timetable and a train that's already running changes under the driver's feet.
Or we find out at 06:00 that a vehicle went into the workshop yesterday."

**What you need.** "Versions. Effective dates. Warnings about journeys with no vehicle or driver, early."

**What it changed:** versioned, immutable published timetables (REQ-OPS-003); changes never alter generated
journeys (REQ-LIVE-017); unassigned alerts at T-24h and T-2h (REQ-LIVE-011).

---

## INT-08 — Incident manager (PER-09)

**What breaks.** "Speculation. Someone puts out a message before we know what happened. And handover — an incident
outlives a shift."

**What you need.** "Templates for serious incidents, one owner, a timeline, and the data preserved."

**Never.** "An automatic message saying a train has 'crashed' because a sensor stopped."

**If it lied.** "Then we'd be on the news for the wrong reason."

**What it changed:** only an incident manager declares emergencies, never inferred (REQ-OPS-007); approved
templates and legal hold (REQ-OPS-008, REQ-DATA-003); ownership handover (REQ-OPS-007).

---

## INT-09 — Customer service agent (PER-10)

**What you need.** "What the passenger sees, and the reason. And not to be the last to know."

**What it changed:** read-only console view with the same vocabulary as passengers (REQ-OPS-015).

---

## INT-10 — Catering operations lead (PER-13)

**What breaks.** "We pack food for the 12:10 and it turns up at 12:35. Or it's cancelled and nobody tells us."

**What you need.** "ETA changes pushed as they happen. Cancellations immediately."

**Never.** "Personal data we don't need. I don't want that liability."

**What it changed:** outbound ETA and cancellation events (REQ-INT-002); minimal payload (DF dp-catering-min);
provider outage isolated from railway functions (REQ-INT-004). Catering kept **thin** per the user's priority
decision (ASM-005).

---

## INT-11 — Security engineer (lens)

**Questions raised.** "How does a screen on a platform authenticate? Who can forge a train position? Can a
driver token cancel a train? Who can change roles — and can that same person cancel trains?"

**What it changed:** device identity (REQ-DSP-003); per-source feed authentication (REQ-SEC-004); negative
authorisation tests per permission (REQ-SEC-003); separation of duties for system administrators (REQ-OPS-001).

---

## INT-12 — QA engineer (lens)

**Questions raised.** "How do I test 'twelve minutes late at 14:37' without waiting? How do I make a feed go dark
on demand? How do I know a test of the simulator isn't testing a shortcut?"

**What it changed:** injectable clock (REQ-NFR-005); fault injection (REQ-SIM-003); simulator uses the real
ingestion path (REQ-SIM-001); deterministic seeds (REQ-SIM-002).

---

## INT-13 — Accessibility specialist (lens)

**Questions raised.** "Can a display be read from eight metres? Does anything flash? Can the terminal be used
from a wheelchair? Is every status available as text?"

**What it changed:** display type scale and distance check (REQ-DSP-001), motion limits (REQ-DSP-008), terminal
lockdown and reach (REQ-TRM-004), WCAG target (REQ-NFR-003).

---

## Where interviews disagreed

| Disagreement | Resolution | Recorded as |
|---|---|---|
| Controller wants every change pushed to passengers immediately; commuter wants fewer notifications | Throttle routine delay changes; always send cancellations, platform changes and terminations | CON-05 |
| Driver wants minimal tracking; controller wants to see where every train is | Position comes from the infrastructure feed, not the driver's phone; driver location only during duty | CON-07 |
| Station manager wants platform changes early; controller wants freedom to change late | Changes publish immediately but the station manager sees them in the console at the same moment; no hold-back period | CON-08 |
