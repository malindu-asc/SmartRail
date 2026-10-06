# Glossary

Terms are used consistently across docs, code, UI copy and tests. When a railway term and a software term
collide, the railway term wins in the UI and the domain layer.

| Term | Meaning in SmartRail |
|---|---|
| **Service** | A recurring timetabled train pattern (stops, times, days) within one timetable version. |
| **Journey** | One run of a service on one service date. The thing passengers follow. Primary identity. |
| **Service date** | The operating day a journey belongs to. Not the same as the calendar date — a 23:50 departure arriving 00:35 belongs to the earlier service date (REQ-LIVE-009). |
| **Train number** | The public number of a service ("1042"). A label, not an identity (REQ-LIVE-019). |
| **Vehicle / consist** | The physical train: a trainset, or a locomotive plus coaches, treated as one unit. |
| **Locomotive** | The engine of a locomotive-hauled train; can be replaced independently. |
| **Rotation / diagram** | The sequence of journeys one vehicle works in a day. |
| **Crew duty** | One driver's shift; attaches to journeys by time-ranged assignment. |
| **Assignment** | A time-ranged link from a journey to a vehicle or a duty. |
| **Working timetable** | The full operational plan including passing points and non-passenger movements. |
| **Public timetable** | The passenger-facing projection of the working timetable. |
| **Train detection feed** | Infrastructure-derived position data. In SmartRail it is produced by the simulator. |
| **Telemetry / update** | One message from a feed source about a train's position or event. |
| **Dwell** | Time a train spends at a stop. |
| **Section** | Track between two stations. A journey is `InSection` when between stops. |
| **Regulation** | A controller's decision about the order and priority of trains. Out of scope to model; named for context. |
| **Possession / planned works** | Planned closure of track for engineering, known in advance. |
| **Reason code** | Standardised cause attached to a delay, skip, cancellation or termination. Used for reporting and attribution. |
| **Delay attribution** | Assigning responsibility for delay to a cause. Out of scope beyond recording the reason code. |
| **Disruption** | A published notice that changes what passengers should expect, with a scope. |
| **Incident** | An operational event with an owner and a lifecycle; may produce one or more disruptions. |
| **Scope (of a disruption)** | What it affects: network, route, section, station, service or journey. Resolved to journeys and stops automatically. |
| **Retraction** | Withdrawing a disruption that should never have been published. Distinct from resolving one. |
| **Confidence** | How the system knows a time: Confirmed, Live, Scheduled or Unknown. |
| **Stale** | Shown, but older than 60 s; displayed with its age. |
| **Unknown** | No accepted update for 300 s; the system stops claiming to know. |
| **Interrupted** | Running but stopped and unable to continue; no ETA exists. |
| **Customer information system (CIS)** | The industry term for station displays driven by running data. |
| **Platform display** | Screen for one platform; next few departures. |
| **Concourse display** | Main station board; all departures, arrivals and disruptions. |
| **Station terminal** | Interactive public kiosk. |
| **Device token** | Credential for an unattended display or terminal. |
| **Heartbeat** | Periodic signal from a device proving it is alive. |
| **Follow** | A passenger's anonymous subscription to a journey for notifications. |
| **Snapshot / delta** | Full state on connect, then only changes. |
| **Outbox** | Reliable delivery pattern: events written with the data change, sent by a background dispatcher. |
| **Dead-letter queue (DLQ)** | Where events go after delivery retries are exhausted. |
| **Legal hold** | Suspension of data deletion for records relevant to an investigation. |
| **RailBite** | The name used for the simulated external catering provider. |
