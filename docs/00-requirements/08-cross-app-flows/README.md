# Cross-application flows

A use case describes one person using one app. A railway event does not stay in one app. A cancellation starts in
the ops console and has to land — consistently, quickly, and retractably — on phones, platform screens, concourse
boards, terminals, the driver's cab and an external provider. These flows document that journey **end to end**,
including what each surface shows at each moment and what happens when a link in the chain fails.

They are the most direct answer to "how do these applications connect".

| Flow | Starts in | Reaches | E2E test | Simulator |
|---|---|---|---|---|
| [CF-01 Delay propagation](cf-01-delay-propagation.md) | Train detection feed | Backend → ops, app, displays, terminal, catering, followers | **E2E-1** | SIM-02 |
| [CF-02 Cancellation](cf-02-cancellation.md) | Ops console | Every surface + driver + catering | **E2E-3** | SIM-08 |
| [CF-03 Driver offline and reconciliation](cf-03-driver-offline.md) | Driver app (no signal) | Backend, ops | **E2E-2** | SIM-09 |
| [CF-04 Disruption publish and retract](cf-04-disruption.md) | Ops console | Every surface | part of E2E-3 | — |
| [CF-05 Platform change](cf-05-platform-change.md) | Ops console | Displays, app, station view, followers | — | SIM-14 |
| [CF-06 Degraded and missing data](cf-06-degraded-data.md) | Feed silence / client offline | Every surface | **E2E-5** | SIM-05, SIM-10 |
| [CF-07 Breakdown and vehicle substitution](cf-07-breakdown-substitution.md) | Feed + scheduler | Every surface + later journeys | — | SIM-06, SIM-07 |
| [CF-08 Major incident](cf-08-major-incident.md) | Network controller | Every surface; legal hold | — | SIM-17 |
| [CF-09 Buying a ticket](cf-09-purchase.md) | Passenger app | Backend, payment provider, notifications | **E2E-4** | SIM-19 |
| [CF-10 Cancellation reaches tickets, then refund](cf-10-affected-refund.md) | Ops console | Tickets, passenger app, network controller (read-only) | **E2E-4** | SIM-20 |
| [CF-11 Ticket inspection](cf-11-inspection.md) | Staff app (Inspector mode) | Backend, live journey | part of **E2E-4** | — |

**E2E-4** is the ticket flow: [CF-09](cf-09-purchase.md) then [CF-10](cf-10-affected-refund.md), with inspection ([CF-11](cf-11-inspection.md)) asserted inside it. The catering variant was cut on 2026-10-06.

## Shared timing targets

| Hop | Target |
|---|---|
| Accepted update → all connected clients | ≤ 5 s p95 (REQ-NFR-001) |
| Operator action → all connected clients | ≤ 5 s p95 |
| Stale label after silence | 60 s (ASM-019) |
| Unknown after silence | 300 s (ASM-020) |
| Device shown offline | 90 s (ASM-029) |

## How to read a flow

Each flow has a **timeline table** — time, actor or component, what happens, and what each surface shows at that
moment — followed by failure branches. Times are illustrative and use a single example journey,
**J-1042 (08:00 Central → Hill Town)**, so the flows can be compared.
