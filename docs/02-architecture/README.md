# Architecture

**Brief:** §23 Architecture (high-level architecture, application boundaries, major components, data ownership,
integration architecture, authentication and authorisation approach, decisions and trade-offs), §24 technical
freedom. **Status:** Simulator design seeded; the rest is planned for D4 per the tracker.

## Committed stack (plan.md §2)

| Layer | Choice |
|---|---|
| Backend | .NET clean architecture (Domain / Application / Infrastructure / Api), modular monolith |
| Web | React + TypeScript — ops console and station kiosk (two modes) |
| Mobile | Flutter, feature-based clean architecture — passenger and driver apps |
| External | Node.js — RailBite catering platform, separate database |
| Data | PostgreSQL (authoritative), Redis (live state, fan-out) |
| Realtime | SignalR with long-poll fallback, snapshot then delta (REQ-NFR-007) |
| Reliability | Transactional outbox, background dispatcher, dead-letter queue (REQ-INT-004) |

## Documents

| Document | Contents | Status |
|---|---|---|
| [simulator-design.md](simulator-design.md) | The train detection feed stand-in; scenario library SIM-01 … 22 | Seeded |
| [database-schema.md](database-schema.md) | The whole database: 59 tables in 6 schemas, ownership, retention, indexes. DDL in [database-schema.sql](database-schema.sql), checked on PostgreSQL 16 | Agreed |
| architecture.md | C4 context and container diagrams; module boundaries | Planned (D4) |
| data-ownership.md | Which module owns which data; no cross-module writes | Planned (D4) |
| integration.md | Feed contract, catering contract (OpenAPI), webhook signing | Planned (D4) |
| auth.md | Four authentication postures (REQ-SEC-001), permission model, device identity | Planned (D5) |
| adr/ | One file per decision; index mirrored on the tracker's Decisions sheet | Planned (D4) |

## Inputs from requirements that architecture must honour

| From requirements | Architectural consequence |
|---|---|
| [system-context.md](../00-requirements/04-domain/system-context.md) — writers vs readers | Public read path separate from staff write path; readers never write journey state |
| [identity-model.md](../00-requirements/04-domain/identity-model.md) | Journey is the aggregate root; time-ranged assignments; no vehicle-keyed tracking |
| [state-models.md](../00-requirements/04-domain/state-models.md) | State machines live in the Domain layer with exhaustive transition tests |
| [source-precedence.md](../00-requirements/04-domain/source-precedence.md) | Event history per journey; precedence applied in the Application layer |
| REQ-NFR-005 | `IClock` abstraction everywhere; no direct `DateTime.Now` |
| REQ-SIM-001 | Simulator compiles only against published contracts |
| REQ-SEC-004 | Per-source feed keys at the ingestion boundary |
| ASM-009 | No component claims or implies a safety function |

## ADRs already decided (titles; full text pending)

0001 modular monolith · 0002 clean architecture layering · 0003 PostgreSQL + EF Core · 0004 SignalR · 0005 Redis
for live state · 0006 three-source live tracking with confidence · 0007 outbox instead of a broker · 0008 Node.js for the external platform *(revived thin 2026-10-07: a read-only menu service)* · 0009 one kiosk codebase, two modes · 0010 Flutter feature-based clean architecture ·
0011 permission-based authorisation · 0012 device identity · 0013 no passenger location · 0014 injectable clock and
seeded simulator · 0015 shared design tokens, divergent density. Added 2026-10-06: 0016 provider-neutral payment port with a fake provider · 0017 signed QR tickets with status held server-side · 0018 accounts scoped to purchase while public data stays anonymous.
