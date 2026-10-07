# Open questions

Questions we would put to the organisation. In this assessment the people who can answer are the **assessors**
and **Malindu** (for scope). Each has a default assumption so the work never blocks on an answer.

| Status | Meaning |
|---|---|
| Open | Not yet asked |
| Sent | Asked; awaiting answer |
| Answered | Answer recorded; linked items updated |
| Assumed | No answer by the baseline; default assumption stands |

## For Malindu — scope decisions (block the most work)

| ID | Question | Why it matters | Default if unanswered | Blocks | Status |
|---|---|---|---|---|---|
| OQ-01 | Is it acceptable that discovery relies on simulated stakeholders and general domain knowledge, clearly labelled? | Integrity of the evidence | Yes, labelled throughout (ASM-034) | — | Open |
| OQ-02 | Simulator as the infrastructure feed, driver app supplementary — or driver GPS as the primary position source? | Defines the live-data architecture | Infrastructure feed (ASM-008) | CF-03, CF-06, SIM-* | Open |
| OQ-03 | Station operations: role-scoped console view, or a separate application? | ~2 days of scope | Role-scoped view (ASM-010) | UC-OPS | Open |
| OQ-04 | Station experience: both displays and terminal, or one? | ~1 day | Both, one codebase (ASM-011) | UC-DSP, UC-TRM | Open |
| OQ-05 | Passenger accounts, or anonymous device subscriptions? | ~1 day plus privacy surface | Anonymous (ASM-012) | UC-PAX-05, REQ-PAX-007 | Answered 2026-10-06: accounts exist for ticketing; public information stays anonymous (ASM-012) |
| OQ-06 | Catering depth: thin and last (one day), contract only, or cut? | A graded §23/§26 row | Thin and last (ASM-005) | UC-EXT | Answered 2026-10-06: catering cut entirely (ASM-005) |

## For the assessors

| ID | Question | Why it matters | Default if unanswered | Status |
|---|---|---|---|---|
| OQ-07 | Is ticketing or fare handling in scope? | Would double the domain | Out of scope | Answered 2026-10-06 by the owner: ticketing is in the MVP as a thin slice (ASM-035); assessors can still overrule |
| OQ-08 | Is a modular monolith acceptable, or is service decomposition expected? | Architecture shape | Modular monolith (ADR-0001) | Open |
| OQ-09 | Must mobile apps run on iOS, or is Android sufficient for the demo? | Toolchain time | Android demo; iOS buildable, untested | Open |
| OQ-10 | Is local Docker deployment sufficient, or is a cloud deployment expected? | Infrastructure time | Local, with documented deployment path | Open |
| OQ-11 | Expected language coverage? | i18n effort | English plus one secondary on passenger surfaces (ASM-030) | Open |

## For the organisation (would ask a real client; recorded for completeness)

| ID | Question | Why it matters | Default | Status |
|---|---|---|---|---|
| OQ-12 | What existing train-detection, signalling or tracking data is available, and in what form? | Real position source | Simulator as stand-in | Assumed |
| OQ-13 | What reason-code list is used for delay attribution? | Reporting consistency | Short generic list seeded | Assumed |
| OQ-14 | What punctuality threshold does the organisation report against? | Bands and reports | ASM-021 bands | Assumed |
| OQ-15 | Which stations have catering collection points, and what lead time does the provider need? | Catering scope | Two stations, provider decides lead time | Assumed |
| OQ-16 | What are the legal retention obligations for operational and incident data? | Retention policy | ASM-027 | Assumed |

## Added 2026-10-06 (ticketing)

| ID | Question | Why it matters | Default if unanswered | Status |
|---|---|---|---|---|
| OQ-17 | Will you provide PayHere sandbox merchant keys, to be kept out of the repository? | Needed only for the stretch PayHere adapter | Fake provider only | Open |
| OQ-18 | Should the fare table use real-looking Sri Lankan fares or placeholders? | Demo credibility | Placeholders (ASM-039) | Open |
| OQ-19 | Is a fine or excess fare for travelling past the destination in scope? | Inspection outcome | Out of scope; recorded only | Open |
