# 02 — Stakeholders and impact

> Impact ratings are **hypotheses** derived from the brief and from general railway practice. No stakeholder of
> this organisation has been interviewed (ASM-034). They are refined by the simulated interviews in
> [05-discovery/stakeholder-interviews.md](05-discovery/stakeholder-interviews.md).

## 2.1 Who is involved

Grouped the way the training separates them: who feels the pain, who lives with the workflow, and who is
affected downstream. Actors marked **missing** are not named in the brief and were found in discovery.

### Customer — feels the business pain

| Actor | Who they are in practice | What they need |
|---|---|---|
| Daily commuter | Same train most days; knows the route; checks in seconds | "Is my usual train running, and from which platform?" |
| Occasional traveller | Unfamiliar station and route; anxious about changes | "Which train, which platform, do I change, where do I get off?" |
| Passenger with access needs **(missing)** | Low vision, screen-reader user, wheelchair user, non-native speaker | The same information, through a channel they can use, early enough to arrange help |

### System users — live with the workflow daily

| Actor | Responsibility in practice | Origin |
|---|---|---|
| Driver | Drives the train; reports what happened at stops and why; receives instructions | Named (§8) |
| Network controller | Watches running across the network; decides what happens when it goes wrong (regulation, cancellation, platform changes) | Named (§9 network operations) |
| Scheduler | Builds and publishes timetables; assigns vehicles and drivers to journeys | Named (§9 scheduling) — **assignment role missing** |
| Station manager | Responsible for a station: its information, its screens, its crowd | Named (§9 stations) |
| Incident manager | Owns serious incidents from declaration to review | Named (§9 incident management) |
| Customer service agent | Answers passengers; needs the same truth they see | Named (§22) — **no application given** |
| Fleet controller **(missing)** | Knows which vehicles are available or in maintenance | Missing |
| Reporting analyst | Punctuality and performance | Named (§9 reporting) |
| System administrator | Manages users, roles and devices — **not** operations | Named (§9) |

### Indirectly impacted — downstream

| Actor | How they are affected |
|---|---|
| External catering provider (system and staff) | Food is prepared against journey times that may change |
| Infrastructure / signalling **(missing)** | In a real railway, the source of where trains physically are; here, played by the simulator |
| Device technician **(missing)** | Installs, replaces and revokes screens and terminals |
| Emergency services **(missing)** | Involved in any serious incident; no integration, but the process must acknowledge them |
| Guard / train manager **(missing)** | Passenger-side crew on board; deferred in the MVP |
| Security, QA, accessibility, data protection | Requirements owners for their concerns |

## 2.2 Impact matrix

| Stakeholder | Group | Business | User | Operational | Why |
|---|---|---|---|---|---|
| Daily commuter | Customer | 🟠 | 🔴 | 🟢 | Journey decisions made in seconds depend on current information; repeated failures erode trust in the whole service |
| Occasional traveller | Customer | 🟠 | 🔴 | 🟢 | Least able to recover from wrong information — does not know the alternatives |
| Passenger with access needs | Customer | 🟠 | 🔴 | 🟠 | A platform change without accessible notice can mean a missed train and a missed assistance booking |
| Driver | System user | 🟠 | 🟠 | 🔴 | Acting on an instruction that never arrived is an operational hazard |
| Network controller | System user | 🔴 | 🟠 | 🔴 | Every decision across the network rests on a current picture |
| Scheduler | System user | 🟠 | 🟢 | 🔴 | An error in a published timetable propagates to every downstream surface |
| Station manager | System user | 🟠 | 🟠 | 🔴 | Wrong screens move crowds to the wrong platform — a safety issue, not just inconvenience |
| Incident manager | System user | 🔴 | 🟠 | 🔴 | Public messaging during a serious incident carries reputational and legal weight |
| Customer service agent | System user | 🟠 | 🟠 | 🟠 | Contradicting the station board in a phone call damages credibility |
| Fleet controller | System user | 🟠 | 🟢 | 🟠 | Vehicle unavailability is a leading cause of cancellation |
| Catering provider | Indirect | 🟠 | 🟢 | 🟠 | Prepares food against times that change; waste and missed collections |
| Security / QA / accessibility / data protection | Indirect | 🟠 | ⚪ | 🟠 | Own cross-cutting requirements |

**Legend:** 🔴 high — directly affects a core decision or outcome · 🟠 moderate — meaningful workflow impact ·
🟢 low — secondary · ⚪ not enough evidence to rate.

## 2.3 Impact dimensions

| Dimension | Level | Reasoning |
|---|---|---|
| Business — risk | 🔴 | Wrong information on platforms can move crowds unsafely; wrong messages during an incident carry legal exposure |
| Business — reputation | 🟠 | Passenger information is the most visible part of a railway; "the board said on time" is a common complaint |
| Business — cost | ⚪ | No evidence in the brief to quantify |
| Business — revenue | ⚪ | No evidence; ticketing is outside the brief |
| User — time | 🔴 | Information determines which train, which platform, and whether to wait |
| User — effort | 🟠 | Checking several sources that disagree |
| User — frustration | 🔴 | Uncertainty is worse than bad news; an honest "delayed, cause unknown" beats a wrong time |
| Operational — process | 🔴 | Scheduling, running, stations, crews and incidents are all coupled through the same journey |
| Operational — tools | 🟠 | Five applications serving very different contexts of use |
| Operational — efficiency | 🔴 | Coordination during disruption is where time is lost |

## 2.4 The real-world point the matrix makes

The highest impacts cluster around **one moment**: when something changes during a running service. That is
where the controller decides, the station must react, the driver must be told, and passengers must be informed —
quickly and consistently. The MVP is shaped around that moment: the five E2E flows in the plan are all variations
of it.
