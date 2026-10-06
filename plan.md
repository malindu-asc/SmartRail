# SmartRail — Master Delivery Plan

> **Status:** Draft v0.1 (template + committed decisions)
> **Owner:** Malindu Pabasara (individual assessment)
> **Window:** 3 weeks — 2026-09-29 (Tue) → 2026-10-19 (Mon), demo on the final day
> **Source brief:** [final-assessment-scoped 2.md](final-assessment-scoped%202.md)

---

## 0. How to use this document

This is the single control document for the assessment. It holds four kinds of content, deliberately kept apart:

| Marker | Meaning |
|---|---|
| **[DECIDED]** | A decision already made and justified. Change only via an ADR. |
| **[PLAN]** | Committed schedule or process. Change only by moving something to Deferred. |
| **[FILL]** | Template slot — to be completed as work progresses. Empty here on purpose. |
| **[OPEN]** | Question that needs an assessor answer, or an assumption to be recorded and defended. |

Rule: nothing gets built that is not traceable to a line in §5/§6. Nothing gets demoed that is not in §9.

---

## 1. What is actually being graded (decoding the brief)

The brief is not a feature list. Read §1, §22, §25, §26 and §28 together and the real grading target is:

1. **Can you turn an incomplete brief into defensible requirements?** (§22 is weighted as heavily as the code)
2. **Can you scope down and say no, coherently?** (§25 explicitly prefers small-and-good over large-and-broken)
3. **Can you drive AI and *disagree* with it, with evidence?** (§21 asks for examples where AI was wrong — that means a written register)
4. **Can you explain and modify everything you submit, live?** (§28 — you may be asked to change a requirement on the spot)

Three consequences that shape this entire plan:

- **Documentation is a first-class deliverable, not a write-up at the end.** Roughly 35–40% of the visible assessment surface (§23) is documents. They must be produced *as the work happens* — an AI-usage log cannot be honestly reconstructed on day 20.
- **Breadth beats depth, but only if each app is genuinely functional.** Five thin-but-working apps plus one solid backend scores higher than one polished app.
- **Anything I cannot explain is a liability.** No exotic libraries, no generated code merged without an explain-back note (see §17 Definition of Done).

---

## 2. Pre-committed technology stack **[DECIDED]**

These are fixed. Each has a justification that must survive a "why?" in the review.

| Layer | Choice | Why this, for *this* problem |
|---|---|---|
| Backend | **.NET 9 / ASP.NET Core, Clean Architecture** (Domain → Application → Infrastructure → API) | The railway domain is rule-heavy (schedules, calendars, journey state machine). A domain layer with no framework dependencies lets those rules be unit-tested without a DB, and the layering is legible to an assessor in seconds. |
| Backend shape | **Modular monolith**, one deployable, hard module boundaries | Microservices would cost days of infra for zero assessment credit and make distributed-transaction bugs my problem. Boundaries are enforced in code (no cross-module entity references; module-to-module only via contracts) so the *architecture* still demonstrates separation. |
| Database | **PostgreSQL + EF Core** | Relational fits the timetable/graph-of-stops model; `jsonb` covers snapshot payloads; free and Docker-friendly. |
| Live state | **Redis** (current journey positions, display snapshots, pub/sub fan-out) | Journey progress is hot, small and expendable — it does not belong in the Postgres write path. |
| Realtime push | **SignalR** (WebSocket, automatic long-poll fallback) | First-class in .NET, one abstraction serves web, displays, terminal and Flutter. The fallback matters for §16. |
| Async / reliability | **Outbox pattern + background dispatcher** (no broker) | Gives at-least-once delivery to the catering provider and to notifications without operating RabbitMQ/Kafka. |
| Ops web app | **React 19 + TypeScript + Vite**, TanStack Query, Zustand, Tailwind + shadcn/ui | Data-dense, keyboard-driven console. TanStack Query is the right cache for "server is the source of truth, invalidate on SignalR event". |
| Passenger mobile | **Flutter**, feature-based Clean Architecture | One codebase, two platforms, and feature-sliced layering mirrors the backend so I explain one model twice. |
| Driver mobile | **Flutter**, same monorepo, shared `core` + `design_system` packages | Two apps, one design language, one HTTP/auth/offline stack. Reuse is itself an architecture argument. |
| Flutter state/data | Riverpod, Dio, freezed + json_serializable, go_router, **Drift** (offline cache + outbound action queue) | Riverpod for testable DI without context tricks; Drift because the driver app needs a real queryable offline queue, not key-value storage. |
| Station display | **React kiosk app** (separate build target in the web monorepo), service worker + last-known-good snapshot | Displays are read-only and must render from cache when offline — a PWA does that with no native work. |
| Station terminal | **Same React kiosk codebase, `terminal` mode** (touch, interactive, idle reset) | Displays and terminals share most of the data layer and all of the design tokens. Two codebases would be a scope error, and I can defend "one app, two interaction profiles". |
| External catering | **Node.js + Express + its own Postgres schema** — a genuinely separate app ("RailBite") | §12 says the provider must remain external. A *different runtime* makes the boundary undeniable, exercises the Node.js track from §24, and forces a real contract (HMAC-signed webhooks, API keys, versioned DTOs) instead of a shared-type shortcut. |
| Identity | ASP.NET Core Identity + **JWT with per-audience tokens**, permission claims, device tokens for kiosks | §14 needs four different auth postures in one model — see §7.5. |
| Observability | Serilog (structured JSON) + **OpenTelemetry** traces/metrics → Seq + Prometheus/Grafana, ASP.NET health checks | §19 asks for production-operability *thinking*; traces spanning SmartRail → RailBite are the proof. |
| Tests | xUnit + FluentAssertions + **Testcontainers**; Playwright (web/kiosk E2E); `flutter_test` + `integration_test`; k6 smoke; axe-core; OWASP ZAP baseline | Testcontainers gives real-Postgres integration tests with no shared environment. Playwright drives the five E2E flows required by §23. |
| Local env + CI | Docker Compose (db, redis, seq, api, railbite) + **GitHub Actions** | One `docker compose up` is the difference between a demo that works and a demo that doesn't. |

**Explicitly rejected, with reasons** (keep these for the review):

- Microservices / Kubernetes — cost without credit for one engineer in three weeks.
- Node.js for the main backend — the domain rules benefit from a typed domain layer and .NET's testing story; Node is used where it genuinely belongs (the external third party).
- MongoDB — the timetable is relational; joins are the whole job.
- GraphQL — clients are few and known; REST + SignalR is less to explain.
- MAUI / React Native — Flutter is the committed mobile stack and one mobile toolchain is all there is time to debug.
- Real GPS hardware / third-party transit APIs — replaced by a deterministic simulator (§7.4), which is also what makes the system testable.

---

## 3. Scope decision **[DECIDED]**

### 3.1 In scope — the MVP

Six deliverable applications, one backend, one simulator.

| # | Deliverable | Scope ceiling |
|---|---|---|
| 1 | **SmartRail API** (.NET) | 6 modules: Network & Timetable, Live Operations, Disruptions, Catering Gateway, Identity & Access, Notifications |
| 2 | **Ops Web Console** (React) | 5 screens: Live Network Board, Timetable & Journeys, Disruption Console, Station & Device Health, Punctuality Report |
| 3 | **Passenger App** (Flutter) | 6 screens: Plan/search, Station departures, Journey detail with live progress, Follow & notifications, Food ordering, Account |
| 4 | **Driver App** (Flutter) | 4 screens: Duty/roster, Active journey with stop list, Stop confirm + delay reason, Notices inbox |
| 5 | **Station Kiosk** (React, two modes) | `display` mode: platform board + concourse board. `terminal` mode: search, route explore, station info, disruptions, food options |
| 6 | **RailBite** (Node.js, external) | Menu ownership, stock, order lifecycle, hosted test-mode payment, webhooks to SmartRail, small internal ops UI, fault-injection switch |
| 7 | **Journey Simulator** (.NET worker/CLI) | Replays seeded scenarios: on-time, delay, skip-stop, terminate-short, telemetry blackout, mid-journey cancellation |

### 3.2 Deferred — and why **[DECIDED]**

Deferral is a graded skill (§25.6). Each line needs the *reason*, not just the exclusion.

| Deferred | Why |
|---|---|
| Ticketing, fares, seat reservation | Not in the brief. SmartRail is an *information* platform; adding commerce would double the domain and the compliance surface. |
| Real card payments | Payment ownership is assigned to the catering provider (§7.6). SmartRail never sees a card number. Provider test mode only. |
| Full multilingual rollout | i18n scaffolding plus two languages on the passenger app and displays only. Ops console stays English — internal audience. |
| SMS / email notification channels | Push and in-app cover the demonstrated journeys; adding a carrier is integration cost with no new architectural lesson. |
| Train splitting/joining, coach composition, crowding | Rare-but-hard modelling; documented in the domain model as an intentional extension point. |
| ML-based ETA prediction | The deterministic propagation model (§7.4) is explainable and defensible; an unexplainable model violates §28. |
| On-board catering delivery by staff | Collection-at-station only. One fulfilment mode fully worked beats two half-modes. |
| Native display hardware / signage CMS | A browser kiosk demonstrates everything that matters (freshness, offline behaviour, contrast, legibility). |
| Kubernetes, multi-region, autoscaling | Documented as a production path in §13; not built. |
| Formal accessibility audit | WCAG 2.2 AA as a *design and test* target with axe plus manual checks; no certification. |
| Knock-on delay propagation between trains | Named in §6.2 as a known real-world behaviour, explicitly out of scope; requires a network-capacity model. |

### 3.3 Scope guardrails **[PLAN]**

- **No new feature after D14.** D15–D21 is hardening, tests, docs and demo. Anything discovered late goes to the Deferred table with a reason.
- **Vertical slices only.** A feature ships end-to-end (DB → API → UI → test → doc) or it does not count as done.
- **Two-hour rule.** Any single problem unresolved after two hours gets a documented workaround or a deferral. Log it — stuck-time handled well is evidence of judgement, not failure.

---

## 4. Non-functional targets **[DECIDED]**

Targets exist so QA and observability have something to measure. Deliberately modest and honest.

| Concern | Target | How verified |
|---|---|---|
| Live data freshness | Position/ETA visible to clients ≤ 5s after telemetry (p95, local) | k6 + trace timestamps |
| Public departures endpoint | p95 < 300ms, cached | k6 smoke |
| Staleness honesty | Banner plus timestamp whenever data is older than 60s; explicit "no live data" state — never a stale time that looks fresh | Playwright offline test |
| Offline driver actions | Queue survives app kill, replays in order, server de-duplicates | Flutter integration test |
| Accessibility | WCAG 2.2 AA on passenger app, terminal and display; zero critical axe violations | axe-core plus manual keyboard/screen-reader pass |
| Security | No high-severity ZAP baseline finding; no known-critical dependency CVEs | CI gates |
| Test health | Domain + Application unit coverage ≥ 70%; five E2E flows green in CI | GitHub Actions |

---

## 5. Requirement discovery plan **[PLAN]**

### 5.1 Method (D1–D3)

1. **Brief decomposition** — every sentence of the brief becomes an atomic statement tagged `GIVEN` (stated), `IMPLIED`, or `ABSENT`.
2. **AI stakeholder simulation** — 12 personas, each a separate structured interview with a fixed protocol: *their day → what breaks today → what they need to see → what they must never see → what they would do if the system lied to them.* The last question is the one that finds failure requirements.
   Personas: Daily Commuter · Occasional/First-time Traveller · Visually Impaired Passenger · Train Driver · Station Manager · Network Controller · Customer-Service Agent · Catering Provider Ops · Security Engineer · QA Engineer · Accessibility Specialist · Data-Protection Officer.
3. **Adversarial pass** — re-run each persona as "what would make you distrust this system?" This is where §22's *missing failure scenarios* come from.
4. **My own reconciliation** — conflicts resolved by me, in writing, with the losing option recorded. AI output is input, not conclusion.
5. **Freeze** — requirements baselined at end of D3. Later changes go through a change log with impact noted.

### 5.2 Artifacts and ID scheme **[FILL]**

| Artifact | File | ID prefix |
|---|---|---|
| Refined requirements | `docs/00-requirements/requirements.md` | `REQ-<MODULE>-nnn` |
| Assumptions register | `docs/00-requirements/assumptions.md` | `ASM-nnn` |
| Personas | `docs/00-requirements/personas.md` | `PER-nn` |
| User journeys | `docs/00-requirements/journeys.md` | `JRN-nn` |
| User stories + acceptance criteria | `docs/00-requirements/stories.md` | `US-nnn` / `AC-nnn.n` |
| Edge cases | `docs/00-requirements/edge-cases.md` | `EDG-nnn` |
| Open questions | `docs/00-requirements/open-questions.md` | `OQ-nn` |
| Traceability matrix | `docs/00-requirements/traceability.md` | — |

**The traceability matrix is the spine.** One row per requirement: `REQ → US → AC → test id → implementing module → status`. It is how I answer "show me where that requirement lives" in five seconds, and it doubles as the coverage report for §23 QA.

### 5.3 Prioritisation

MoSCoW per requirement plus a **risk × value** score. Anything Must-have must appear in one of the five E2E flows (§11.3) — if a requirement is critical but untested, one of those two labels is wrong.

---

## 6. The "unseen" register — gaps, conflicts and ambiguities in the brief

This is the differentiating section: what the brief does *not* say, found by reading it through four professional lenses. Each item becomes a `REQ`, an `ASM`, an `EDG`, or an `OQ` during D1–D3.

### 6.1 Requirements engineer — missing actors and processes

1. **No timetable authoring process.** The brief lists "train schedules" as data but never says who creates or changes them, or what happens to journeys already running when a timetable changes. Needs **versioned timetables** with `effectiveFrom`/`effectiveTo`, draft → review → publish, and immutability of past service days. Edit history is a real audit requirement.
2. **No service-calendar exceptions.** Public holidays, special event trains, weekend patterns, temporary engineering-work timetables. GTFS solves this with a calendar plus exception dates; the brief omits it entirely.
3. **Crew rostering is absent.** §8 assumes a driver has "assigned journeys" but nothing assigns them. Also **crew handover mid-journey** — a driver change at an intermediate station — which breaks the naive "one journey = one driver" model.
4. **No customer-service application** in the brief's app list, yet §22 names a customer-service representative. What do they use? Assumption: the Ops Console with a read-mostly role plus passenger lookup by order or journey reference.
5. **No incident lifecycle.** §10 lists disruption *types* but no states, no ownership, no resolution, no post-incident record. A disruption needs: draft → published → updated → resolved, with author, reason code, affected-entity scope and an audit trail.
6. **Who is allowed to publish bad news?** Nothing says who may cancel a running journey or publish a network-wide alert. Cancelling a train is a high-consequence, customer-visible act. Needs permission separation, a mandatory reason code, an audit entry, and a confirmation gate for network-wide broadcasts.
7. **No data-correction path.** A wrong disruption pushed to 200 displays — how is it retracted? Retraction is a different operation from update and needs its own UX and propagation rules.
8. **§13 Notifications is one sentence** and hides an entire subsystem: channels, opt-in, per-journey subscription, deduplication and throttling (a train delayed six times must not send six pushes), quiet hours, and the reality that a push is never the authoritative source of truth.
9. **§19 Observability is one sentence** and hides the fact that **a dead display is a customer-facing outage nobody notices.** Devices need heartbeats and alerting, which means a device registry, which means device identity (see 6.7).
10. **No disclaimer or liability position.** Every real railway states that live information is indicative. Absent here; needs to be an explicit product decision surfaced in the UI.

### 6.2 Domain modelling — missing states and hard cases

11. **Midnight-crossing services.** A 23:50 departure arriving 00:35 belongs to the *previous* service day. Using wall-clock dates silently corrupts "next train" for the last hour of every day. Model **service date** separately from clock time (GTFS-style times may exceed 24:00).
12. **Time zone and DST discipline.** Store UTC, compute and display in the network's local zone, never trust a device clock for a business decision.
13. **The journey lifecycle has no defined states.** Without an explicit state machine, "is it cancelled or just not started yet?" becomes a bug farm. Proposed: `Scheduled → Ready → Running (AtStation | InTransit) → Completed`, plus `Cancelled`, `PartiallyCancelled`, `TerminatedShort`, `Diverted`, and critically **`Unknown` (telemetry lost)**.
14. **"Unknown" must be a first-class state, not a blank.** If live data is lost, the system must degrade to schedule-only and *say so*. The worst failure mode of a passenger-information system is confident wrongness. This is the requirement I consider most important in the whole project.
15. **Out-of-order, duplicate and impossible telemetry.** Late-arriving pings, replays, GPS drift onto a parallel line, a 40 km jump in 10 seconds. Ingestion needs monotonic sequence numbers, idempotency, and plausibility rejection with a logged reason.
16. **Skipped stop, unscheduled stop, short termination, reversal.** All are normal railway life and all break "next stop = next row in the list".
17. **Overtaking and knock-on delay.** A delayed train can be overtaken; a late train can delay the one behind it. Real, and deliberately out of scope — but it must be *named*, not missed.
18. **Two journeys, one physical train.** The train arriving as service 1042 departs as 1043. Vehicle reuse means delay propagates across journeys through the vehicle, not the route.
19. **Platform is a property of a stop on a day, not of a route** — and it changes at short notice. §10's "platform change" only works if platform lives on the stop, with a change event and a change timestamp.

### 6.3 Live data — the mechanism the brief deliberately leaves to me (§5)

20. **Where does position actually come from?** Three candidate sources with different trust levels: (a) driver app events — high trust, low frequency, human-dependent; (b) device GPS pings — high frequency, noisy, connectivity-dependent; (c) inference from schedule — always available, low truth. Decision in §7.4: fuse all three and surface an explicit confidence level.
21. **ETA must not imply precision it does not have.** Showing "14:32" derived from a schedule-only guess is dishonest. ETA carries a `source` and `confidence`, and the UI renders "live", "≈ estimated" and "scheduled" differently.
22. **Driver location is employee monitoring.** A privacy and labour-relations issue the brief never mentions: collect position only while on an assigned duty, never from a personal device without consent, retain raw pings briefly, and never expose driver identity to passengers.
23. **Clock skew on driver actions.** Record both device time and server-receive time and flag divergence. An offline-queued action replayed 20 minutes later must not be treated as having happened now.

### 6.4 UI/UX and accessibility

24. **Three audiences means three design profiles from one token set.** Passenger/terminal (touch, glanceable, 48dp targets), Operations (dense, keyboard-first, information-rich), Display (viewed from 5–10 m, huge type scale, maximum contrast, no interaction, minimal motion). §15 asks whether they share a design language — the answer is *shared tokens, divergent density*.
25. **Colour can never be the only signal.** Delay status must carry icon plus text plus colour — for colourblind passengers, sun-washed screens, and monochrome displays.
26. **Delay is better expressed as a delta.** "+7 min" next to the scheduled time beats silently rewriting the time; a passenger who memorised 14:25 needs to see that it *was* 14:25.
27. **Context of use is brutal and unmodelled.** Passenger: one hand, walking, direct sunlight, three seconds of attention. Terminal: standing, shared screen, no keyboard skill, hygiene concerns, the previous user's input possibly still on screen. Driver: gloves, vibration, glare, and a hard safety constraint — **no typing while moving**; large confirm-only controls, actions gated to at-stop/stationary.
28. **Every screen needs designed empty / loading / stale / error / offline states.** In a system whose entire value is trust, these five states *are* the product, not edge cases.
29. **Terminal idle reset.** No session left behind: an idle timeout returns to home and clears anything entered.
30. **Screen-reader and dynamic-type support on the passenger app** — not a checkbox but an actual pass with TalkBack/VoiceOver, plus text scaling that does not break the departures list.

### 6.5 Connectivity and resilience (§16)

31. **A per-app degraded-mode matrix is missing.** Needed explicitly: what each app caches, for how long, what it hides when stale, and what it refuses to do offline. A display cheerfully showing a 40-minute-old board is worse than a display saying "offline".
32. **Driver offline writes need conflict rules.** Driver actions must be modelled as **events with timestamps**, not state overwrites, so replay is deterministic and the server stays authoritative.
33. **Reconnect storms.** 200 displays reconnecting after a network blip needs jittered backoff and a snapshot-then-delta protocol, not 200 simultaneous full refreshes.

### 6.6 Integration with the catering provider (§11 / §12)

34. **Ownership must be settled explicitly or the integration is guesswork.** Decision in §7.6: RailBite owns menu, price, stock, order state and payment. SmartRail owns journey, station, timetable, ETA and disruption. Neither writes the other's data.
35. **SmartRail's food UI is a facade over a third party** — which means menu staleness, price change between browse and pay, and stock-out at confirm time are *normal*, not exceptional. The UI must handle a rejected order gracefully.
36. **Webhook reliability is the whole integration.** At-least-once delivery means idempotency keys, HMAC signatures, replay-window checks, retries with backoff, a dead-letter queue, and a **reconciliation job** that asks "which orders do we disagree about?"
37. **Delay changes the order's cut-off.** If the train is 25 minutes late the collection window moves. Who recalculates, and does the passenger re-confirm? SmartRail publishes ETA changes; RailBite owns the cut-off policy; SmartRail notifies the passenger.
38. **Missed collection, cancelled train, passenger leaves early.** Each needs a defined outcome (hold / refund / forfeit), owned by RailBite, triggered by SmartRail events.
39. **Data minimisation across the boundary.** RailBite receives an order reference, collection station, journey reference and ETA. It does **not** receive passenger name, phone or account id unless the workflow provably requires it.
40. **Contract versioning and a published schema.** OpenAPI on both sides plus consumer contract tests, so "they changed their API" is a test failure rather than a demo failure.

### 6.7 Security (§17)

41. **Displays and terminals have no human to log in.** They need **device identity**: provisioning flow, per-device token, rotation, revocation and a device registry. This is the single most commonly missed requirement in a brief like this.
42. **Kiosk lockdown.** No escape to browser chrome, no file access, no external links, no keyboard shortcuts, idle reset, and nothing personal left on screen.
43. **Public API abuse.** The departures endpoint is unauthenticated by design, so it needs rate limiting, caching, and an answer for scraping and denial-of-service pressure.
44. **Driver devices are shared and physical.** Short sessions, PIN/biometric re-auth, remote revoke, and no offline cache of another driver's roster.
45. **Authorization is the real risk surface, not authentication.** IDOR on food orders, a station manager editing another station's data, a reporting role reading passenger PII. Needs a written permission matrix *and deliberate negative tests*.
46. **A tamper-evident audit trail** for cancellations, disruption publishing and permission changes — append-only, with actor, reason and before/after values.
47. **Secrets and signing keys.** No secrets in the repo, rotatable HMAC keys, CI secret scanning.

### 6.8 Data and privacy (§20)

48. **Continuous passenger location is not required** to satisfy any requirement in the brief, so it is not collected. Station selection is manual or a coarse one-shot lookup. A deliberate privacy-by-design decision worth stating out loud.
49. **A data classification table is mandatory** — Public (timetable, stations, disruptions) / Operational (journeys, telemetry, incidents) / Personal (account, order, subscriptions) / Sensitive-operational (driver identity, roster) — each with owner, retention period and log-redaction rule.
50. **Log hygiene.** No tokens, no email addresses, no precise driver coordinates in logs. Correlation ids instead of identities.
51. **Retention schedule.** Raw telemetry short (for example 7 days), aggregated punctuality long, passenger orders per the provider's policy, audit long.
52. **Account deletion and data export.** A passenger account implies these rights; documented at minimum, ideally a working delete.

### 6.9 QA (§18)

53. **The system is untestable unless time is injectable.** An `IClock` abstraction plus the seeded simulator is what turns "the train is 12 minutes late at 14:37" into a deterministic test instead of a waiting game. An architecture decision *driven by testability* — worth calling out in the review.
54. **The integration needs a controllable stub.** RailBite must be able to fail on demand: timeout, 500, invalid signature, duplicate webhook, out-of-order webhook.
55. **What not to test matters as much as what to test** — no UI snapshot tests, no coverage chasing in infrastructure code, no E2E for anything a unit test already proves.
56. **Defect discipline from D1.** An honest defect log (including defects I caused and fixed) is evidence; a clean log looks like no testing happened.

### 6.10 Process risks specific to this assessment

57. **The AI-usage narrative cannot be reconstructed.** It must be journaled daily, including the failures. Target: at least eight concrete "AI was wrong or insufficient" entries with the correction.
58. **§28 explain-back risk.** Any merged code I cannot walk through cold is a scoring liability. Enforced in the Definition of Done.
59. **Demo fragility.** A live multi-app realtime demo will break. Mitigation: deterministic seeded scenario, scripted narrative, two rehearsals on D19–D20, and a recorded fallback.

---

## 7. Target architecture **[DECIDED]**

### 7.1 System context

```
                        ┌──────────────────────────┐
  Passenger (Flutter) ──┤                          ├── SignalR hub (live deltas)
  Driver    (Flutter) ──┤                          │
  Ops Web   (React)   ──┤      SmartRail API       ├── PostgreSQL (authoritative)
  Kiosk display/term. ──┤  (.NET modular monolith) ├── Redis (live state + fan-out)
                        │                          ├── Outbox → dispatcher
  Journey Simulator  ───┤   (telemetry ingestion)  │
                        └───────────┬──────────────┘
                                    │  REST out (API key)
                                    │  Webhooks in (HMAC)
                                    ▼
                        ┌──────────────────────────┐
                        │  RailBite (Node.js)      │  external catering platform
                        │  own DB, own menu,       │  owns payment (test mode)
                        │  own order lifecycle     │
                        └──────────────────────────┘
```

### 7.2 Backend modules and data ownership

| Module | Owns | Never touches |
|---|---|---|
| **Network & Timetable** | Station, Platform, Route, Service, ScheduledStop, TimetableVersion, ServiceCalendar | live positions, orders |
| **Live Operations** | Journey, JourneyStop progress, TelemetryPing, ETA projection, vehicle assignment, crew duty | menus, passenger accounts |
| **Disruptions** | Disruption, ReasonCode, AffectedScope, publication and retraction, audit | order state |
| **Catering Gateway** | OrderReference (SmartRail side only), outbound events, inbound webhook handling, reconciliation | menu, price, stock, payment |
| **Identity & Access** | User, Role, Permission, Device, Session, audit of access changes | everything else |
| **Notifications** | Subscription, NotificationPreference, outbox → push, dedup/throttle state | journey truth |

Boundary rule: modules communicate only through published contracts and integration events. No cross-module EF navigation properties. Enforced by folder structure plus a NetArchTest architecture test in CI — cheap, and it proves the boundaries are real rather than aspirational.

### 7.3 Core domain sketch **[FILL — refine on D4]**

```
Station(id, code, name, coords, facilities[], accessibility)
Platform(stationId, code, length?)
Route(id, name, direction, orderedStops[])
Service(id, code, routeId, timetableVersionId, calendar, stoppingPattern)
ScheduledStop(serviceId, stationId, seq, arrOffset, depOffset, platform?, isPickup, isDropoff)
TimetableVersion(id, effectiveFrom, effectiveTo, status: Draft|Published|Superseded)
ServiceCalendar(weekdays[], includeDates[], excludeDates[])

Journey(id, serviceId, serviceDate, vehicleId?, state, cancelReason?, createdFrom)   ← aggregate root
JourneyStop(journeyId, seq, stationId, schedArr, schedDep, actualArr?, actualDep?,
            estArr?, estimateSource, confidence, platform,
            status: Pending|Arrived|Departed|Skipped|Cancelled)
TelemetryPing(journeyId, seq, deviceTime, serverTime, lat, lng, speed?, source,
              accepted, rejectReason?)

Disruption(id, type, severity, scope{network|route|service|journey|station},
           message[lang], reasonCode, publishedBy, publishedAt, retractedAt?, state)

Device(id, stationId?, kind: PlatformDisplay|ConcourseDisplay|Terminal,
       tokenHash, lastHeartbeat, status)
```

Invariants that live in the domain layer, and become the first unit tests written:

- a journey cannot depart a stop before arriving at it;
- stop sequence is strictly ordered and gaps are explicit skips;
- a cancelled journey accepts no further telemetry;
- a published timetable version is immutable;
- a service date is not a calendar date;
- an estimate always carries a source and a confidence.

### 7.4 Live tracking design **[DECIDED]**

**Three-source fusion with explicit confidence.**

| Source | Trust | Produces |
|---|---|---|
| Driver app events (`departed`, `arrived`, `skipped`, delay reason) | Highest — a human confirmed it | actual times, `confidence: Confirmed` |
| Telemetry pings (simulator now, real device later) | Medium — noisy, gaps | interpolated position, `confidence: Live` |
| Schedule plus last known offset | Lowest — always available | projected ETA, `confidence: Scheduled` |

Pipeline: `ingest → validate (sequence, plausibility, journey state) → update JourneyProgress in Redis → recompute downstream ETAs (offset propagation + per-stop dwell model) → persist material changes to Postgres → publish delta over SignalR → enqueue notification and webhook events via the outbox`.

Degradation: no accepted telemetry for N minutes moves the journey to `Unknown`, ETAs fall back to `Scheduled` confidence, and **every client surfaces that fact**. The system never silently interpolates a train that has gone dark.

The **simulator is a first-class deliverable**: deterministic, seeded and scenario-driven (`on-time`, `delay-8min-at-stop-3`, `skip-stop-5`, `terminate-short`, `telemetry-blackout`, `cancel-mid-journey`). It is what makes the demo reproducible and the E2E tests possible.

### 7.5 Identity and access model **[DECIDED]**

Four postures, one model — this is the answer to §14:

1. **Anonymous public** — stations, timetable, departures, disruptions. Rate-limited, cached, no PII.
2. **Passenger account** — optional, required only for follow/notify and food orders. Email plus password, JWT, refresh rotation.
3. **Operational staff** — driver and ops roles, organisation-issued accounts, short access tokens, device-bound refresh, re-auth on sensitive actions.
4. **Devices (displays and terminals)** — no human; provisioned device token, read-only scope, heartbeat, revocable.

Roles, deliberately not one "Administrator" (§9): `NetworkController`, `Scheduler`, `StationManager`, `CustomerServiceAgent`, `IncidentManager`, `ReportingAnalyst`, `SystemAdministrator`, `Driver`, `Passenger`, `Device`.

Authorization is **permission-based** (`disruption.publish`, `journey.cancel`, `timetable.publish`, `station.edit:own`); roles are bundles of permissions; scope-limited permissions (`:own`) are checked in the Application layer. The written matrix lands in `docs/04-security/permission-matrix.md`, and every cell gets a positive *and* a negative test.

### 7.6 Catering integration contract **[DECIDED]**

- **SmartRail → RailBite** (REST, API key, mTLS optional): journey and station reference data, scheduled and live ETA, service-change and cancellation events.
- **RailBite → SmartRail** (webhooks, HMAC-SHA256 signed, timestamped, idempotency key): order placed / confirmed / ready / collected / cancelled / refunded.
- **Ownership:** RailBite owns menu, price, stock, order state, payment, refunds and collection cut-offs. SmartRail owns journey truth and passenger notification.
- **Reliability:** outbox, retry with exponential backoff and jitter, circuit breaker, dead-letter queue, replay-window rejection, nightly order-state reconciliation.
- **Privacy:** minimum payload (order reference, journey reference, collection station, ETA). No passenger name, phone or account id.
- **Versioning:** OpenAPI on both sides, `/v1` paths, consumer-driven contract tests in CI.

### 7.7 Architecture decision records **[FILL]**

One ADR per significant decision in `docs/02-architecture/adr/NNNN-title.md`. Pre-identified set:

`0001` modular monolith over microservices · `0002` .NET Clean Architecture layering · `0003` PostgreSQL + EF Core · `0004` SignalR for realtime · `0005` Redis for live state · `0006` three-source live-tracking fusion with confidence · `0007` outbox instead of a message broker · `0008` Node.js for the external catering platform · `0009` one kiosk codebase, two modes · `0010` Flutter feature-based clean architecture with Riverpod · `0011` permission-based authorization with scoped permissions · `0012` device identity for unattended clients · `0013` no passenger location collection · `0014` injectable clock plus seeded simulator for testability · `0015` shared design tokens with divergent density profiles.

---

## 8. Repository layout **[DECIDED]**

```
smartrail/
├─ docs/
│  ├─ 00-requirements/   01-ux/   02-architecture/{adr,diagrams}/
│  ├─ 03-qa/   04-security/   05-ai-usage/   06-operations/
│  └─ demo/                     # narrative, script, seed scenarios
├─ backend/
│  ├─ src/SmartRail.Domain/ .Application/ .Infrastructure/ .Api/ .Simulator/
│  └─ tests/ .Domain.Tests/ .Application.Tests/ .Api.IntegrationTests/ .ArchitectureTests/
├─ web/                          # pnpm workspace
│  ├─ apps/ops-console/  apps/station-kiosk/
│  ├─ packages/design-tokens/ ui/ api-client/
│  └─ e2e/                       # Playwright
├─ mobile/                       # melos workspace
│  ├─ apps/passenger_app/ apps/driver_app/
│  └─ packages/core/ design_system/ api_client/
├─ external/railbite/            # Node.js external catering platform + its own tests
├─ infra/                        # docker-compose, seeds, grafana/seq config
└─ .github/workflows/
```

Single repository, three toolchains. Justification: one engineer, one CI pipeline, atomic cross-stack changes, and an assessor can see the whole system at once.

**Design tokens as one source of truth:** `web/packages/design-tokens/tokens.json` generates CSS custom properties **and** Dart constants from one build script. No drift between React and Flutter. This is the concrete answer to §15.

---

## 9. Per-application scope, screen by screen **[DECIDED]**

### 9.1 Ops Web Console (React) — 5 screens
1. **Live Network Board** — all journeys today, state, delay, filters, drill-in. Realtime.
2. **Timetable & Journeys** — browse services, view and edit a draft timetable version, publish; per-journey actions (cancel, terminate short, change platform) with reason codes.
3. **Disruption Console** — create / update / retract, scope picker, preview of exactly what passengers will see, confirmation gate for network-wide broadcasts.
4. **Station & Device Health** — device registry, heartbeat status, last-seen, revoke. (Directly answers gap 6.1.9.)
5. **Punctuality Report** — on-time performance by service, route and day, exportable.

### 9.2 Passenger App (Flutter) — 6 screens
1. **Home / Plan** — from → to, next departures, recent stations.
2. **Station departures** — live board with confidence indicator.
3. **Journey detail** — stop timeline, live progress, platform, delay delta, disruption banner.
4. **Follow & notifications** — subscribe to a journey, preferences, quiet hours.
5. **Food ordering** — eligible collection stations, menu from RailBite, place order, order status, collection code.
6. **Account** — sign in/up, data and privacy, delete account.

### 9.3 Driver App (Flutter) — 4 screens
1. **Duty** — today's assigned journeys, sign-on.
2. **Active journey** — stop list, next stop, running delay, large glanceable layout.
3. **Confirm** — arrived / departed / skipped plus delay reason; offline queue with a visible pending count.
4. **Notices** — operational messages from control, with acknowledgement.

### 9.4 Station Kiosk (React, two modes)
- **`display/platform`** — next departures from this platform, huge type, delay delta, staleness banner.
- **`display/concourse`** — all departures and arrivals plus active disruptions, rotating detail.
- **`terminal`** — touch: search a destination, explore a route, station info and facilities, disruptions, food options; idle reset.

### 9.5 RailBite (Node.js, external)
Menu and stock management, station eligibility rules, order lifecycle, hosted test-mode payment page, webhook emitter with retry, a small internal ops screen, and a **fault-injection switch** so QA can force timeouts, 500s, bad signatures and duplicate deliveries.

---

## 10. Design system plan **[PLAN]**

- **D4–D5:** token set — colour including status semantics, three type scales (`passenger` / `ops` / `display`), spacing, radius, elevation, motion; light and dark; contrast verified.
- Status semantics defined once: `onTime` / `minorDelay` / `majorDelay` / `cancelled` / `unknown`, each with **icon plus label plus colour** — never colour alone.
- Component inventory, building only what the 19 screens need: DepartureRow, DelayBadge, ConfidenceChip, StalenessBanner, DisruptionCard, StopTimeline, BigConfirmButton, DataTable, EmptyState, OfflineState.
- Wireframes for all 19 screens; high-fidelity for the six screens in the demo narrative.
- Accessibility baked into component specs: target sizes, focus rings, contrast ratios, semantic labels, reduced-motion variants.

---

## 11. QA strategy **[PLAN]**

### 11.1 Shape
Risk-based, not coverage-based. Heavy at the domain layer where the rules are, selective at integration where the contracts are, thin and deliberate at E2E where the cost is.

| Level | Tool | Scope |
|---|---|---|
| Unit (backend) | xUnit + FluentAssertions | domain invariants, journey state machine, ETA propagation, telemetry validation, permission evaluation |
| Unit (mobile/web) | flutter_test / vitest | mappers, state notifiers, offline queue, time and delay formatting |
| Integration | Testcontainers (Postgres, Redis) | repositories, endpoints, authorization matrix, outbox, webhook handling |
| Contract | consumer-driven tests | SmartRail ↔ RailBite, both directions |
| E2E | Playwright (web/kiosk), integration_test (Flutter) | the five critical flows below |
| Non-functional | k6, axe-core, ZAP baseline, Lighthouse | performance smoke, accessibility, security baseline |

### 11.2 Testability enablers (architecture choices made for QA's sake)
`IClock` everywhere · a seeded deterministic simulator · RailBite fault-injection switch · a correlation id on every API response · a `/test/reset-scenario` endpoint available only outside production.

### 11.3 The five automated E2E flows **[DECIDED]**

§23 asks for exactly five, chosen as the most critical:

1. **Passenger follows a delayed train, end to end.** Simulator injects an 8-minute delay → API recomputes ETAs → passenger app shows `+8`, the display board updates, and a follow-notification fires. *Chosen because it is the platform's core value proposition and it crosses every layer.*
2. **Driver confirms a stop offline, and it reconciles.** Driver app offline → confirms departure → queue persists across an app restart → reconnect → server de-duplicates and downstream ETAs shift. *Chosen because it is the highest-risk mechanism in the system (§16, gaps 32 and 23).*
3. **Ops cancels a running journey and it propagates.** Controller cancels with a reason → audit entry written → display, terminal and passenger app all reflect the cancellation and a push goes out. *Chosen because it is the highest-consequence action and exercises authorization, audit and fan-out together.*
4. **Food order across the external boundary.** Passenger orders → RailBite confirms via a signed webhook → train is delayed → cut-off recalculated and passenger notified → collection confirmed. *Chosen because it is the only flow crossing an organisational boundary (§11/§12).*
5. **Display survives connectivity loss honestly.** Kiosk loses network → renders last-known-good with a staleness banner → refuses to imply freshness → recovers on reconnect with a snapshot rather than a storm. *Chosen because "the system must not lie" is the requirement I consider most important (gap 14).*

### 11.4 Evidence pack **[FILL]**
`docs/03-qa/`: `test-strategy.md` · `test-scenarios.md` · `defect-log.md` (id, severity, steps, evidence, fix commit) · `evidence/` (CI runs, screenshots, recordings) · `quality-assessment.md` (honest final verdict including known weaknesses).

---

## 12. Security plan **[PLAN]**

- **D5:** STRIDE threat model per trust boundary — public internet → API, kiosk device → API, driver device → API, SmartRail ↔ RailBite, ops user → administrative functions. Output: `docs/04-security/threat-model.md` with risks ranked and each mapped to a control.
- **Controls to implement:** short-lived JWTs with refresh rotation; permission-based authorization with scoped checks; device tokens with revocation; rate limiting on public endpoints; HMAC webhook verification with a replay window; idempotency keys; strict input validation at the Application boundary; parameterised data access only; security headers and CSP on web and kiosk; kiosk lockdown; secrets out of the repository; append-only audit for high-consequence actions; log redaction.
- **Security testing evidence:** a deliberate authorization negative-test suite (IDOR on orders, cross-station edits, role escalation), ZAP baseline in CI, `dotnet list package --vulnerable` plus `npm audit` plus `dart pub outdated` gates, secret scanning, and a written record of anything found and fixed.
- **Shift-left discipline:** threat model *before* the modules are written; security checks in the Definition of Done, not a week-three audit. This is the direct answer to §17.

---

## 13. Observability and operations **[PLAN]**

- Structured logs (Serilog JSON) with a correlation id propagated client → API → RailBite.
- OpenTelemetry traces and metrics. Custom metrics that matter: telemetry ingest lag, ETA recompute duration, connected SignalR clients, webhook success/retry/DLQ counts, **device heartbeat freshness**.
- Health endpoints: `/health/live`, `/health/ready` (database, Redis, RailBite reachability).
- Dashboards: Network Health (journeys tracked, stale journeys, ingest lag) · Integration Health (webhooks, retries, DLQ) · Device Health (displays online/offline).
- Alerts, defined even if only logged locally: telemetry lag > 60s · DLQ non-empty · more than 10% of displays offline · error rate > 2%.
- `docs/06-operations/`: `runbook.md` (top five incidents with steps), `slo.md`, `deployment.md`, `data-retention.md`.

---

## 14. AI-assisted engineering operating model **[PLAN]**

§21 is graded on orchestration and scepticism, not volume.

### 14.1 Assigned roles

| AI role | Responsibility | How its output is validated |
|---|---|---|
| Requirements analyst | Brief decomposition, gap hunting | Every generated requirement must trace to a brief line or be labelled an assumption by me |
| Stakeholder personas (12) | Simulated interviews, adversarial pass | I reconcile conflicts; contradictions are recorded, not averaged away |
| Domain modeller | Aggregate and state-machine options | Validated by writing the invariant unit tests *first* — a model that cannot be tested is rejected |
| Architect | Option analysis, ADR drafting | I make the call; the ADR records the rejected option and why |
| UX designer | Flows, wireframe ideation, microcopy | Checked against the context-of-use constraints in 6.4; glanceability judged by me |
| Implementer | Scaffolding, boilerplate, mappers, tests | Compiles, tests pass, **and I write an explain-back note** before merge |
| Reviewer | Code review, security review, accessibility review | Findings triaged by me; false positives recorded (they are evidence too) |
| Test engineer | Test case generation, edge-case enumeration | Every generated test must fail before it passes — no tautological tests |
| Technical writer | Documentation polish, diagram source | Facts re-checked against the code |

### 14.2 Daily discipline **[FILL]**

- `docs/05-ai-usage/journal/YYYY-MM-DD.md` — what I delegated, what came back, what I changed, what I rejected, time saved or lost.
- `docs/05-ai-usage/prompt-library.md` — the prompts and workflows that actually worked.
- `docs/05-ai-usage/ai-was-wrong.md` — **target ≥ 8 entries**, each with: what it claimed, why it was wrong, how I found out, what I did instead. Likely hunting grounds: timezone and service-date handling, EF Core query semantics, over-engineered abstractions, plausible-but-nonexistent library APIs, security advice that ignores the threat model, tests that assert the implementation instead of the behaviour.
- `docs/05-ai-usage/modified-implementations.md` — generated code I significantly rewrote, and why.

### 14.3 Hard rule
**Nothing merges that I cannot explain line by line.** If the explanation takes longer than rewriting it simpler, rewrite it simpler. (§28)

---

## 15. Three-week schedule **[PLAN]**

Roughly 8 productive hours a day; 21 days ≈ 165 hours. Each day has an exit criterion — if it is not met by end of day, the next day starts with a scope cut, not a longer day.

### Week 1 — Discover, decide, get the spine running (Sep 29 – Oct 5)

| Day | Date | Focus | Exit criterion |
|---|---|---|---|
| D1 | Tue 29 Sep | Brief decomposition; persona interviews 1–6; repo and docs skeleton; AI journal started | 60+ atomic requirement statements; repo scaffolded; D1 journal written |
| D2 | Wed 30 Sep | Personas 7–12; adversarial pass; edge cases; assumptions register | §6 gap register converted to REQ/ASM/EDG/OQ ids; edge-case list ≥ 40 |
| D3 | Thu 1 Oct | Reconcile, prioritise (MoSCoW + risk), user stories with acceptance criteria, **scope freeze** | Requirements baselined; traceability matrix skeleton; scope document finalised |
| D4 | Fri 2 Oct | Domain model and journey state machine; ADRs 0001–0008; C4 context and container diagrams | Domain model doc plus state-machine diagram; 8 ADRs written |
| D5 | Sat 3 Oct | Threat model (STRIDE); permission matrix; data classification and retention; design tokens v1 | `threat-model.md`, `permission-matrix.md`, `data-classification.md`, `tokens.json` |
| D6 | Sun 4 Oct | Backend spine: solution layering, EF model and migrations, seed network (~12 stations, 6 routes, ~40 services), auth and JWT, first domain unit tests | `docker compose up` → API healthy, seeded, an authenticated request works |
| D7 | Mon 5 Oct | Network & Timetable module complete (queries, admin writes, versioning); OpenAPI; CI pipeline green | Public departures endpoint live and tested; CI runs build + unit + integration |

**Week 1 gate:** requirements frozen, architecture decided, backend skeleton runs from a single command, CI green. If not met, cut a screen from §9 — never cut the gate.

### Week 2 — Build the vertical slices (Oct 6 – Oct 12)

| Day | Date | Focus | Exit criterion |
|---|---|---|---|
| D8 | Tue 6 Oct | Live Operations module: journey generation from timetable, telemetry ingest and validation, ETA propagation, Redis projection | Simulator drives a journey; ETAs recompute; state-machine unit tests pass |
| D9 | Wed 7 Oct | SignalR hub with snapshot/delta protocol; Disruptions module with publish/retract plus audit | Two clients receive a live delta; a disruption propagates to subscribers |
| D10 | Thu 8 Oct | Ops Console: shell, auth, Live Network Board, Timetable & Journeys | A controller can watch live journeys and publish a disruption from the UI |
| D11 | Fri 9 Oct | Ops Console: Disruption Console, Device Health, Punctuality Report; kiosk `display` mode (platform + concourse) with offline snapshot | A display board renders live and survives network loss with a staleness banner |
| D12 | Sat 10 Oct | Flutter monorepo and shared packages; Passenger app: plan, departures, journey detail with live progress | Passenger app shows a live-tracked journey on a device or emulator |
| D13 | Sun 11 Oct | Passenger follow and notifications (FCM); Driver app: duty, active journey, confirm plus offline queue | A driver confirmation shifts the passenger's ETA; offline queue replays after restart |
| D14 | Mon 12 Oct | RailBite plus integration: outbound reference/ETA feed, signed webhooks inbound, outbox/retry/DLQ, passenger food-ordering UI | A food order completes across the boundary, including one forced failure recovered |

**Week 2 gate = feature freeze.** All six apps exist and demonstrate their primary flow. Anything unfinished moves to Deferred **with a written reason**, not into week three.

### Week 3 — Harden, prove, tell the story (Oct 13 – Oct 19)

| Day | Date | Focus | Exit criterion |
|---|---|---|---|
| D15 | Tue 13 Oct | Kiosk `terminal` mode; empty/stale/error/offline states across all apps; reconciliation job | Every screen has all five states; terminal idle-reset works |
| D16 | Wed 14 Oct | The five E2E flows automated and green in CI; contract tests; defect log populated | 5/5 E2E green in a clean CI run |
| D17 | Thu 15 Oct | Security testing pass (authorization negative suite, ZAP, dependency and secret scan) and fixes | Findings triaged, fixed or accepted-with-reason in writing |
| D18 | Fri 16 Oct | Accessibility pass (axe, keyboard, screen reader, contrast, dynamic type, display legibility at distance); observability dashboards and runbook | Zero critical accessibility findings; dashboards show a live journey |
| D19 | Sat 17 Oct | Documentation consolidation: traceability final, ADRs, quality assessment, AI-usage pack, data and privacy doc | `docs/` complete; every §23 checkbox has an artifact |
| D20 | Sun 18 Oct | Demo build: seeded narrative scenario, script, two rehearsals, fallback recording, README verified on a clean checkout | Full demo runs end to end from `docker compose up`, twice in a row |
| D21 | Mon 19 Oct | Buffer, final polish, presentation, submission | Submitted |

**Buffer reality check:** D20–D21 is the only slack and it will be consumed. Plan as if the deadline is D19.

---

## 16. Demo narrative **[PLAN]**

Not a screen tour — one story told across six applications (§27).

> *07:42. Service 1042 leaves the terminus on time. A passenger is following it in the app. The platform display at the third station shows it due at 08:05.*
>
> *08:01. The driver reports a nine-minute delay at a signal — from a tunnel, offline. The confirmation queues on the device.*
>
> *08:04. Connectivity returns. The delay replays, ETAs shift down the line, the display updates to +9, the following passenger receives one push (not four), and the passenger's food order at the collection station has its cut-off recalculated by the catering provider.*
>
> *08:20. Control declares a disruption; the concourse board and the terminal both show it within seconds — and one display has been offline for three minutes, which Device Health already flagged.*
>
> *08:25. We cut the network to a display and show it telling the truth instead of guessing.*

Then the engineering story: what I discovered that the brief did not say · what I assumed · what I excluded and why · the architecture decisions I would defend · where AI was wrong · what I would build next.

---

## 17. Definition of Done

A slice is done only when **all** of these hold:

1. It traces to a `REQ`/`US` whose acceptance criteria are met.
2. Domain rules are unit-tested; the endpoint is integration-tested; the UI handles empty, loading, stale, error and offline states.
3. Authorization is enforced **and negatively tested**.
4. Structured logging with a correlation id; failure paths return a modelled error, not a 500.
5. Accessibility basics met — target sizes, contrast, labels, focus order.
6. Documented: any decision worth defending has an ADR or a line in the requirements doc.
7. **An explain-back note is written** — I can walk through it cold (§28).
8. The day's AI journal entry records what was delegated and what I changed.

---

## 18. Risk register **[FILL — review every Friday]**

| # | Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|---|
| R1 | Scope creep across six apps | High | High | D14 feature freeze; screen ceilings in §9; deferral table |
| R2 | Realtime plus offline mechanics eat days | High | High | Build the spine D8–D9 while energy is high; the simulator makes it testable; fall back to polling if SignalR fights back |
| R3 | Flutter/Android toolchain time sink | Medium | High | Validate emulator and FCM on D6, not D12; iOS explicitly out of testing scope |
| R4 | Documentation left to week three | Medium | High | Docs produced daily; D19 is consolidation, not authoring |
| R5 | AI-usage evidence unconvincing | Medium | High | Daily journal from D1; ≥ 8 "AI was wrong" entries with proof |
| R6 | Demo breaks live | Medium | High | Deterministic seeds, two rehearsals, fallback recording |
| R7 | Cannot explain a generated module | Medium | High | DoD item 7; simplify rather than keep clever code |
| R8 | Integration flakiness with RailBite | Medium | Medium | Fault-injection switch, contract tests, outbox plus DLQ |
| R9 | Time lost to infrastructure (Docker/CI) | Medium | Medium | Compose and CI working by D7 or drop CI to a local script |
| R10 | Illness or external interruption | Low | High | D20–D21 buffer; week gates allow graceful degradation |

---

## 19. Deliverables checklist mapped to brief §23 **[FILL]**

| §23 item | Artifact | Status |
|---|---|---|
| Refined requirements | `docs/00-requirements/requirements.md` | ☐ |
| Assumptions | `assumptions.md` | ☐ |
| Personas / actors | `personas.md` | ☐ |
| User journeys | `journeys.md` | ☐ |
| User stories + acceptance criteria | `stories.md` | ☐ |
| Edge cases | `edge-cases.md` | ☐ |
| User flows | `docs/01-ux/flows/` | ☐ |
| Wireframes | `docs/01-ux/wireframes/` (19 screens) | ☐ |
| High-fidelity designs | `docs/01-ux/hifi/` (6 demo screens) | ☐ |
| Design-system definition | `docs/01-ux/design-system.md` + `design-tokens` package | ☐ |
| Accessibility considerations | `docs/01-ux/accessibility.md` + axe report | ☐ |
| Solution architecture, boundaries, components | `docs/02-architecture/architecture.md` + C4 diagrams | ☐ |
| Data ownership | `docs/02-architecture/data-ownership.md` | ☐ |
| Integration architecture | `docs/02-architecture/integration.md` + OpenAPI specs | ☐ |
| Authentication / authorization approach | `docs/04-security/permission-matrix.md` + ADR 0011/0012 | ☐ |
| Architecture decisions and trade-offs | `docs/02-architecture/adr/` (15 ADRs) | ☐ |
| Working backend | `backend/` | ☐ |
| Web application | `web/apps/ops-console` | ☐ |
| Passenger mobile application | `mobile/apps/passenger_app` | ☐ |
| Operational mobile application | `mobile/apps/driver_app` | ☐ |
| Station experience | `web/apps/station-kiosk` (display + terminal) | ☐ |
| External integration | `external/railbite` + contract tests | ☐ |
| Test strategy | `docs/03-qa/test-strategy.md` | ☐ |
| Functional test scenarios | `docs/03-qa/test-scenarios.md` | ☐ |
| 5 automated E2E flows | `web/e2e/` + `mobile/**/integration_test` | ☐ |
| Identified defects | `docs/03-qa/defect-log.md` | ☐ |
| Evidence of testing | `docs/03-qa/evidence/` + CI runs | ☐ |
| Quality assessment | `docs/03-qa/quality-assessment.md` | ☐ |
| Threat model | `docs/04-security/threat-model.md` | ☐ |
| Security decisions / risks / controls | `docs/04-security/security-decisions.md` | ☐ |
| Security testing evidence | `docs/04-security/evidence/` | ☐ |
| Observability and operations | `docs/06-operations/` | ☐ |
| Data and privacy | `docs/00-requirements/data-privacy.md` | ☐ |
| AI tools, responsibilities, prompts | `docs/05-ai-usage/` | ☐ |
| AI validation and where AI was wrong | `ai-was-wrong.md`, `modified-implementations.md` | ☐ |
| Scope management evidence | this file, §3 | ☐ |
| Final presentation | `docs/demo/presentation.md` + recording | ☐ |

---

## 20. Open questions for assessors **[OPEN]**

Ask early; proceed on a stated assumption if there is no answer.

| # | Question | Assumption if unanswered |
|---|---|---|
| OQ-01 | Is ticketing or fare handling in scope at all? | No — information platform only |
| OQ-02 | Is real payment expected, or is provider test mode acceptable? | Test mode, provider-owned |
| OQ-03 | Must the mobile apps run on iOS, or is Android sufficient for the demo? | Android for the demo; iOS buildable but untested |
| OQ-04 | Is a modular monolith acceptable, or is service decomposition expected? | Modular monolith, justified by ADR 0001 |
| OQ-05 | Is a simulated telemetry source acceptable for live tracking? | Yes — deterministic simulator, with a real-source adapter documented |
| OQ-06 | Any constraint on cloud deployment, or is local Docker sufficient? | Local Docker Compose plus a documented deployment path |
| OQ-07 | Expected language coverage? | English primary, one secondary language on passenger app and displays |
| OQ-08 | Should the driver app collect device GPS, or are stop confirmations enough? | Confirmations primary; GPS optional and duty-scoped |

---

## 21. Appendices — templates **[FILL]**

### 21.1 ADR

```
# ADR-NNNN: <title>
Date: YYYY-MM-DD   Status: Proposed | Accepted | Superseded by ADR-MMMM

## Context
## Options considered
| Option | Pros | Cons |
## Decision
## Consequences (including what this makes harder)
## Rejected because
## AI involvement (what was suggested, what I changed)
```

### 21.2 User story

```
US-nnn  [MODULE]  Priority: Must|Should|Could|Won't   Traces: REQ-xxx-nnn

As a <persona> I want <capability> so that <outcome>.

Acceptance criteria (Given / When / Then):
  AC-nnn.1 ...
  AC-nnn.2 ...
Edge cases: EDG-nnn, EDG-nnn
Tests: <test ids>
```

### 21.3 Defect

```
DEF-nnn  Severity: Critical|Major|Minor|Trivial   Found by: <how>   Area: <app/module>
Steps · Expected · Actual · Evidence · Root cause · Fix (commit) · Prevention (test added)
```

### 21.4 AI journal entry

```
## YYYY-MM-DD
Delegated: ...
Accepted as-is: ...
Modified: ... (why)
Rejected: ... (why, and what I did instead)
AI was wrong about: ... (how I detected it)
Net effect on the day: ...
```

### 21.5 Requirement row

```
REQ-LIVE-007 | Must | Source: brief §5 / persona PER-04 / assumption ASM-012
Statement: ...
Rationale: ...
Acceptance: AC-...
Verified by: TEST-...
```

---

## 22. Immediate next actions

1. `git init`, create the repository skeleton from §8, commit this plan.
2. Start `docs/00-requirements/brief-decomposition.md` — every brief sentence tagged GIVEN / IMPLIED / ABSENT.
3. Convert §6's 59 findings into numbered `REQ` / `ASM` / `EDG` / `OQ` entries.
4. Run persona interviews 1–6 and write the D1 AI journal entry.
5. Send §20's open questions to the assessors.
