# 03 — Current state: how does it happen today?

## 3.1 Why this section is short

The training places "how does it happen today" after stakeholder impact, to uncover workarounds, manual effort,
risks and frustrations. That step assumes a client whose current process can be observed.

**The brief describes a fictional organisation and says nothing about its current systems or processes.** The
honest output of this step is therefore a list of unknowns, not a description. Inventing a current process —
"passengers currently use website X", "drivers currently use radio system Y" — would present fiction as
evidence and contaminate everything built on it.

So the method is adapted, deliberately:

1. Record **what the brief states** about the intended future.
2. Record **what is unknown** about the present.
3. Record **how comparable railways typically work**, labelled as general domain knowledge rather than facts
   about this organisation.
4. Turn each unknown into a question for the organisation ([open-questions.md](05-discovery/open-questions.md))
   and an assumption with a risk-if-wrong line ([assumptions.md](05-discovery/assumptions.md)).

This adaptation is itself a recorded decision (ASM-034, ASM-018).

## 3.2 Known, unknown, and how railways typically do it

| Activity | What the brief tells us | Unknown for this organisation | How comparable railways typically work (domain knowledge, not fact) |
|---|---|---|---|
| Knowing where trains are | It should be shown "where appropriate" (§3, §5) | Any existing train-detection or tracking system | Position is derived from **trackside train detection** (track circuits, axle counters) through the signalling system into a train-describer feed. Driver phones are not the source. |
| Producing timetables | Services run at different times, days and routes (§4) | Who builds them, with what tool, how often they change | Planners maintain a **working timetable** (every movement, including passing times and empty trains) and derive a simpler **public timetable** from it. Changes follow fixed change dates. |
| Assigning vehicles and crews | Drivers have "assigned journeys" (§8) | Any rostering or allocation process | Vehicles follow daily **diagrams** (rotations); crews follow rosters. Substitutions happen on the day. |
| Communicating with drivers | Drivers receive "operational notifications" (§8) | Existing radio or messaging | Safety-critical communication uses dedicated railway radio. Administrative messages may go by other channels. **An app is never the safety channel.** |
| Managing disruption | Delays, cancellations, platform changes and more occur (§10) | Who decides, who publishes, how quickly | A control office regulates trains and decides on cancellations; a separate information function writes customer messages. Delay **reason codes** are recorded and used to attribute delay. |
| Station information | Displays and terminals exist (§6, §7) | Display hardware, content management | A **customer information system** drives platform and concourse screens from the same running data; announcements are made in parallel. |
| Passenger information | A mobile app is wanted (§3) | Any existing app or website | Most operators publish a journey planner and live departures; push alerts for followed trains are common. |
| Catering | Provider has its own platform (§11) | Its systems, its workflow | Unknown; treated as an external system with a contract. |

## 3.3 Workarounds, risks and frustrations — expected rather than observed

Because nothing was observed, these are framed as **risks the design must not reproduce**, drawn from how
railway information commonly fails:

| Typical failure | Consequence | How SmartRail responds |
|---|---|---|
| Screens keep showing "on time" after a train has stopped | Passengers wait on the wrong assumption | Staleness and Unknown are first-class states (REQ-LIVE-004) |
| Different screens and channels disagree | Passengers trust none of them | One journey state, rendered everywhere (system-context) |
| Platform changes reach screens before staff know | Crowds move without staff in position | Station manager sees changes in the console as they publish (CF-05) |
| Cancellation messages are corrected silently | Passengers who saw the first message never learn it was wrong | Retraction is a distinct, visible operation (REQ-PAX-009) |
| Controllers assume a message reached the driver | Instructions are missed | Delivered and acknowledged states (REQ-DRV-007) |

## 3.4 What this step hands to discovery

- The live-position source is the most consequential unknown, and the domain knowledge above points to a clear
  answer: model the **infrastructure feed**, not the driver's phone (ASM-008).
- The working-timetable / public-timetable distinction becomes part of the domain model.
- Reason codes, rotations, and the separation between control decisions and customer messaging all become
  requirements rather than assumptions about an existing system.
