# Personas

> Personas are **composites** built from the brief and general knowledge of railway work. They are not real
> people from this organisation (ASM-034). They exist to make requirements concrete and to give the simulated
> interviews a consistent point of view. Pronouns are deliberately neutral.

Each persona states the context of use, because context — not preference — drives most design decisions here.

---

## Passengers

### PER-01 — Daily commuter

| | |
|---|---|
| **Who** | Takes the 07:42 most weekdays, same route for years |
| **Context** | Walking to the station, phone in one hand, often in sunlight; checks in under five seconds |
| **Goal** | "Is my train running and from which platform?" — then put the phone away |
| **Already knows** | The route, the usual platform, the alternatives |
| **Frustrations** | Screens saying "on time" when the train obviously isn't; finding out about a cancellation on the platform |
| **Must never see** | A time that looks live but is ten minutes old |
| **Would like** | One alert if something changes, not six |
| **Says** | "Just tell me if it's late. If you don't know, tell me you don't know." |
| **Drives** | REQ-PAX-002, REQ-PAX-005, REQ-PAX-007 (throttling) |

### PER-02 — Occasional traveller

| | |
|---|---|
| **Who** | Travelling to a city they have visited twice, with luggage |
| **Context** | Unfamiliar station, crowded concourse, anxious about missing the train |
| **Goal** | Which train, which platform, whether to change, where to get off |
| **Frustrations** | Jargon ("terminates", "formed of"), boards sorted in ways they don't understand, platform changes announced only by audio |
| **Must never see** | A connection that looks fine when the first train is already too late to make it |
| **Uses** | The station terminal as much as the app — may not install anything |
| **Says** | "I don't know what 'TBA' means and I'm scared to ask." |
| **Drives** | REQ-PAX-006 (connection at risk), REQ-TRM-001, plain wording (DF ax-lang) |

### PER-03 — Passenger with low vision, screen-reader user

| | |
|---|---|
| **Who** | Travels independently with a cane; uses TalkBack daily |
| **Context** | Relies on announcements and the app; cannot read platform screens |
| **Goal** | Know the platform early enough to get there and, if needed, meet assistance staff |
| **Frustrations** | Icons with no labels; status shown by colour; late platform changes |
| **Must never see** | (Hear) a status that is just "red" |
| **Says** | "A platform change two minutes before departure is a missed train for me." |
| **Drives** | REQ-PAX-004, REQ-PAX-014, platform-change notification (REQ-PAX-007) |

### PER-04 — Wheelchair user

| | |
|---|---|
| **Who** | Books assistance in advance |
| **Context** | Needs step-free routes and to know whether a station's lift is working |
| **Goal** | Plan a journey that is actually possible for them |
| **Frustrations** | Information about facilities that is out of date |
| **Says** | "If the lift is out, I need to know before I leave home, not when I get there." |
| **Drives** | REQ-TRM-002 (facilities), station facility data (DF dm-facilities) |

---

## Operational staff

### PER-05 — Driver

| | |
|---|---|
| **Who** | Fifteen years on the route |
| **Context** | In the cab: gloves, vibration, glare. Eyes on the line. Safety communication is by radio with the signaller, and always will be. |
| **Goal** | Know their duty, their stops, and any special instructions — and record what happened without fuss |
| **Frustrations** | Being blamed for delays they didn't cause; systems that ask them to type; instructions that arrive late or not at all |
| **Must never** | Be expected to read a phone while the train is moving |
| **Concerns** | Being tracked: "Is this for passengers, or for watching me?" |
| **Says** | "I'll press a big button at a stop. I won't type a sentence at 80 km/h. And if your app goes dark in the tunnel, the train doesn't stop." |
| **Drives** | REQ-DRV-003, REQ-DRV-004, REQ-DRV-005, REQ-DRV-010, reason codes |

### PER-06 — Network controller

| | |
|---|---|
| **Who** | Works shifts in the control office |
| **Context** | Several screens, phone ringing, decisions in seconds during disruption |
| **Goal** | See the whole network now; decide what happens when it goes wrong |
| **Frustrations** | Not knowing if a driver got the message; information that is stale without saying so; having to tell three systems the same thing |
| **Must never see** | A train shown as moving when the feed has been dead for five minutes |
| **Says** | "If I cancel a train I want to say it once and know every screen and phone has it." |
| **Drives** | REQ-OPS-002, REQ-OPS-004, REQ-DRV-007, REQ-LIVE-004 |

### PER-07 — Station manager

| | |
|---|---|
| **Who** | Responsible for one busy interchange station |
| **Context** | On the concourse as much as the office; manages staff and crowds |
| **Goal** | Screens that are right; enough warning to move staff before a crowd moves |
| **Frustrations** | Platform changes that hit the screens before staff know; a dead screen nobody reported |
| **Must never see** | A platform change that sends 300 people down one staircase with no staff there |
| **Says** | "A wrong screen isn't an inconvenience at rush hour. It's a crowd problem." |
| **Drives** | REQ-OPS-009 (device health), CF-05 platform change, REQ-DSP-005 |

### PER-08 — Scheduler

| | |
|---|---|
| **Who** | Builds timetables and assigns vehicles and crews |
| **Context** | Works days ahead; changes go live on fixed dates |
| **Goal** | Publish a correct timetable; make sure every journey has a vehicle and a driver |
| **Frustrations** | A change that silently alters trains already running; discovering a missing vehicle on the morning |
| **Says** | "Publishing a timetable should feel like a release, not like saving a spreadsheet." |
| **Drives** | REQ-OPS-003, REQ-LIVE-011, REQ-LIVE-017 |

### PER-09 — Incident manager

| | |
|---|---|
| **Who** | Takes ownership of serious incidents |
| **Context** | High pressure, legal and reputational exposure |
| **Goal** | One controlled voice to the public; evidence preserved |
| **Must never see** | Automated messages speculating about an accident; an incident with no owner after shift change |
| **Says** | "In a serious incident, the system's job is to stay accurate and quiet, not to be clever." |
| **Drives** | REQ-OPS-007, REQ-OPS-008, legal hold (REQ-DATA-003) |

### PER-10 — Customer service agent

| | |
|---|---|
| **Who** | Takes passenger calls and messages |
| **Context** | Angry callers during disruption |
| **Goal** | See exactly what the passenger sees, plus the reason |
| **Must never** | Contradict the station board |
| **Says** | "If my screen says something different to the board, I've lost the call." |
| **Drives** | REQ-OPS-015 |

### PER-11 — Fleet controller

| | |
|---|---|
| **Who** | Knows which vehicles are fit to run |
| **Goal** | Take a vehicle out of service and have the consequences visible |
| **Says** | "If I pull a unit for maintenance, someone needs to know which trains just lost their vehicle." |
| **Drives** | REQ-OPS-012, REQ-LIVE-018 |

### PER-12 — System administrator

| | |
|---|---|
| **Who** | Manages accounts, roles and devices |
| **Goal** | Grant and revoke access safely; provision screens |
| **Must never** | Be able to cancel trains — that is not their job and is the point of separation of duties |
| **Drives** | REQ-OPS-001, REQ-OPS-009 |

---

## External

### PER-13 — Catering operations lead (RailBite)

| | |
|---|---|
| **Who** | Runs food preparation at collection stations |
| **Context** | Needs lead time; food waste costs money |
| **Goal** | Know reliably when a train with orders will arrive |
| **Frustrations** | Learning about a 25-minute delay when the food is already packed |
| **Must never** | Receive passenger personal data they don't need |
| **Says** | "Give me ETA changes as they happen. Don't give me names and phone numbers." |
| **Drives** | REQ-INT-002, REQ-INT-004 |
