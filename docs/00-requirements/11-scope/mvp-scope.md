# MVP scope

Brief §25: explore the broader problem, choose an achievable scope, prioritise, document what is deferred and why,
and build the chosen scope to quality. *A smaller, coherent, well-engineered solution is preferable to a large
collection of incomplete features* — but the assessment must not collapse into a single application.

## The scoping principle

> Build the **moment of change** end to end. The highest impacts in the stakeholder analysis cluster around one
> moment — something changes during a running service — and every application has a role in it. The MVP is every
> application doing its part in that moment, plus the planning and access foundations it needs.

## Build levels

| Level | Meaning |
|---|---|
| **Yes** | Built fully, tested, demoed |
| **Thin** | Built minimally — enough to demonstrate the mechanism honestly |
| **No** | Documented and designed; not built |

## By application

| Application | Yes | Thin | No (documented) |
|---|---|---|---|
| **Backend** | Journey lifecycle and three-dimension state; ingestion with validation; source precedence; ETA propagation; journey generation; service date; time-ranged assignments; platform state; disruptions with scope resolution; realtime snapshot/delta; audit; permissions; device identity | Rotation cascade; platform conflict warning | Knock-on delay between trains |
| **Passenger app** (Flutter) | Station board; journey timeline; staleness labels; follow + throttled push + inbox; disruption banners and corrections; offline cache; delete data; screen reader and text scaling | — | Accounts; map view; SMS/email |
| **Driver app** (Flutter) | Duty view offline; one-tap stop events; reason codes; offline queue and replay; message acknowledgement; request assistance | Crew handover | Guard role; location collection beyond duty-scoped |
| **Ops console** (React) | Live board; cancel / terminate / reinstate; platform change; disruption console with preview, approval, retraction; timetable versioning; unassigned queue; device health; users and roles; punctuality report | Incidents and emergency termination; vehicle unavailability; driver messages; data correction; planned works; customer-service lookup | Break-glass access; shift-pattern rostering |
| **Station display** (React kiosk) | Platform and concourse modes; device token; offline honesty; heartbeat; server-time countdowns | — | Audio announcements; signage CMS |
| **Station terminal** (React kiosk) | Destination search; station info and facilities; disruptions; idle reset; lockdown; offline mode | — | Printed itineraries |
| **Simulator** | Real-ingestion path; seeded scenarios; acceleration; fault injection; scenario library (SIM-01 … 17) | — | Real signalling integration |
| **External catering** (Node) | — | Separate app; outbound ETA events; signed webhooks; outbox/retry/DLQ; one-screen ordering | Payment beyond provider test mode; order modification; reconciliation |

## Deferred — and why

| Deferred | Reason |
|---|---|
| Ticketing, fares, reservations | Not in the brief; would double the domain and compliance surface |
| Passenger accounts | Following a train needs no account; anonymous subscriptions carry far less personal data (ASM-012) |
| Knock-on delay between trains | Needs a track-capacity model; named so it is not mistaken for an omission |
| Train formation, splitting and joining | Rare and hard; the vehicle is one consist (ASM-015) |
| Guard / train manager role | Second crew persona with no new architecture lesson (ASM-014) |
| Map view | Schematic timeline answers the question better, works offline, no third-party dependency (ASM-031) |
| SMS and email | Push plus inbox covers the demonstrated journeys |
| Audio announcements | Separate system in real stations; noted as an accessibility limitation |
| Machine-learning ETA | Unexplainable under live review (§28); deterministic model instead (ASM-032) |
| Kubernetes, multi-region | Documented production path; no assessment credit for the infrastructure time |
| Full catering workflow | User priority decision; thin integration proves the boundary (ASM-005) |

## Pending decisions that could change this page

| Decision | Default here | If changed |
|---|---|---|
| OQ-02 position source | Simulator as infrastructure feed | Driver-GPS-primary moves CF-03 into the critical path |
| OQ-03 station operations | Role-scoped console view | A separate app adds ~2 days |
| OQ-04 station experience | Both modes | Dropping one saves ~1 day |
| OQ-05 passenger accounts | None | Accounts add ~1 day and a privacy surface |
| OQ-06 catering depth | Thin, last | Cutting forfeits a graded row |

## The five E2E flows (brief §23)

| # | Flow | Cross-app flow | Why it is critical |
|---|---|---|---|
| E2E-1 | Passenger follows a delayed train | CF-01 | The core value proposition; crosses every layer |
| E2E-2 | Driver records offline and it reconciles | CF-03 | The highest-risk mechanism (§16) |
| E2E-3 | Controller cancels a running journey; every surface follows | CF-02 | Highest-consequence action; authorisation + audit + fan-out |
| E2E-4 | Food order across the external boundary, including a provider failure | UC-EXT-04 | The only organisational boundary (§11, §12) |
| E2E-5 | Display survives connectivity loss honestly | CF-06 | "Never lie about a train" under failure |
