# UX and design system

**Brief:** §15 UI/UX and design system, §23 Product and UX (user flows, wireframes, high-fidelity designs,
design-system definition, accessibility). **Status:** Planned — UX discovery follows the requirements baseline.

## Process

Separate from requirement discovery, and following the UX training flow:

| Stage | Outputs |
|---|---|
| **Discover** | Railway app audit (comparable products), proto-personas from [personas.md](../00-requirements/05-discovery/personas.md), empathy maps, 5 Whys on the top pain points |
| **Synthesise** | Affinity themes, point-of-view statements, How-Might-We questions |
| **Ideate** | Solution concepts, assumptions, shortlist |
| **MVP scope** | Must / Should / Could / Won't — reconciled with [mvp-scope.md](../00-requirements/11-scope/mvp-scope.md) |
| **Design** | Information architecture, user flows, wireframes, design system, high-fidelity for demo screens, prototype, accessibility review |

Competitive research is kept **separate** from requirement gaps: a requirement gap is something the brief failed to
specify; a market insight is something comparable products do. Mixing them muddies both.

## Constraints already fixed by requirements

| Constraint | Source |
|---|---|
| Status is always icon + text + colour | REQ-PAX-004 |
| Delay shown as a delta; scheduled time stays visible | REQ-PAX-002 |
| Every surface designs five states: empty, loading, stale, error, offline | REQ-NFR-006, degraded-mode matrix |
| Unknown and Interrupted have their own wording — never "+n" | REQ-LIVE-004, REQ-LIVE-013 |
| Schematic stop timeline, not a map | ASM-031 |
| Displays legible at 8 m; no flashing above 3 Hz | REQ-DSP-001, REQ-DSP-008 |
| Driver app: large one-tap actions, no free text between stops | REQ-DRV-004 |
| Screen reader and 200 % text on the passenger app | REQ-PAX-014 |
| WCAG 2.2 AA on passenger app, terminal and displays | REQ-NFR-003 |
| Lists do not jump under the finger during live updates | INT-06 |

## Design system direction

One token set, three density profiles — **shared tokens, divergent density** (ADR-0015):

| Profile | Used by | Character |
|---|---|---|
| Passenger | Passenger app, terminal | Touch, 48 dp targets, glanceable |
| Operations | Ops console | Dense, keyboard-first, data tables |
| Display | Platform and concourse | Very large type, maximum contrast, minimal motion, no interaction |

Tokens live in one `tokens.json` generating CSS variables and Dart constants.

## Planned documents

`discover/` · `synthesise/` · `ideate/` · `flows/` · `wireframes/` · `hifi/` · `design-system.md` ·
`content-guide.md` (plain wording, status vocabulary) · `accessibility.md`
