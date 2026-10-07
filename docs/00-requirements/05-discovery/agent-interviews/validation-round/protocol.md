# Agent interview protocol — validation round

> **Validation method.** Each persona was shown the proposed system and reacted to it. That leads: people tend to agree
> with what they are shown. Kept for comparison; the primary evidence is the discovery round
> ([../protocol.md](../protocol.md)).

Shared instructions used for every simulated stakeholder interview in this round (brief §21, §22).

## Rules for the persona agent

1. **Independence.** Open only (a) this file and (b) the brief at `C:\works\SmartRail\final-assessment-scoped 2.md`.
   Do **not** open, search or list any other file in the repository. Do not look at `docs/`.
2. **Be the person.** Answer in the first person, from the working experience of a real person in that job.
   Be practical and specific. Use the vocabulary the person would use, not software vocabulary.
3. **Do not design the system.** Describe your work, what goes wrong, what you need to see, and what worries you.
4. **Disagree where you would.**
5. **Be honest about certainty.** Say "I'm guessing" when you are.
6. **Write only your own output file.**

## The system, as described to every persona

SmartRail is a digital platform for a national railway. It provides:

- a **passenger mobile app**: departures, journey progress, delay and platform information, disruption notices, alerts for
  followed trains, ticket purchase, a QR ticket, and refunds when a train is cancelled;
- a **driver app**: the driver's duty and stops, one-tap stop events, delay reasons, messages from control, working offline;
  the same app has a ticket-inspection mode that scans passenger QR tickets;
- an **operations web console**: a live view of every train, cancelling or terminating a train, changing platforms,
  publishing disruptions, assigning vehicles and drivers, timetables, user and device management;
- **station information displays** (platform and main concourse) and an **interactive terminal**;
- a **simulator** that stands in for the railway's train-detection feed (there is no real signalling integration).

Train position is expected to come from the railway's own train-detection data, not from drivers' phones. The system is an
information system: it does not replace signalling, radio or safety procedures.

## The five questions

1. Walk me through a normal day in your role.
2. What breaks, and what do you do when it does?
3. What do you need to see, and when?
4. What must you **never** see (or never happen)?
5. If this system lied to you — showed something confidently wrong — what would you do?

Then three probes specific to the persona.

## Output format

`# Interview — <persona>` · Who I am · Answers (Q1–Q5, Probes 1–3) · Extracted by the persona (what I need; what must never
happen; what would make me stop trusting it; what this system seems to be missing; where I disagree with the design;
confidence).
