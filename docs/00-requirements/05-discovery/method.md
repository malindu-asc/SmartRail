# Discovery method

## Objective

Brief §22: discover what the brief does **not** specify — missing actors, processes, failure scenarios, states,
security, operational and accessibility requirements, integration cases, conflicts, ambiguities and assumptions —
and consolidate them into refined requirements.

## Constraints that shaped the method

| Constraint | Consequence |
|---|---|
| No real stakeholder can be interviewed | Stakeholder input is **simulated** with AI personas. Every finding is labelled with its basis so simulation is never presented as evidence about this organisation (ASM-034). |
| One person, three weeks | Discovery is broad; specification is deep only where we build. |
| AI output is not automatically correct (§21) | Persona outputs are inputs. Conflicts between personas are resolved by me in writing, never averaged. |

## Steps

### 1. Brief decomposition

Every sentence of the brief is split into atomic statements, each tagged:

| Tag | Meaning | Example |
|---|---|---|
| GIVEN | Stated outright | "The catering provider should remain a separate external system" (§12) |
| IMPLIED | Necessarily true if the brief is true | Drivers have "assigned journeys" → something assigns them |
| ABSENT | Required for a working system, not mentioned | Who may cancel a running train |

The ABSENT and IMPLIED statements become candidate findings.

### 2. Simulated stakeholder interviews

One structured interview per persona ([personas.md](personas.md)), always the same five questions:

1. Walk me through a normal day.
2. What breaks today, and what do you do when it does?
3. What do you need to see, and when?
4. What must you **never** see?
5. **What would you do if the system lied to you?**

Question 5 is the one that matters. Asking what people *want* produces feature lists; asking what happens when
the system is wrong produces failure requirements and edge cases.

### 3. Adversarial pass

Each persona is re-run with a single question: *what would make you stop trusting this system?* The answers fed
the failure-scenario and state findings.

### 4. Practitioner lens

The register was then read through four professional lenses — requirements engineer, developer, UX, QA — plus
security and operations, asking for each: what would I need that is not here?

### 5. Railway domain check

General knowledge of how railways run (train detection, working timetables, rotations, reason codes, control
office versus customer information) was used to test whether the findings matched how the work is actually done.
Findings from this step are labelled `Domain knowledge`.

### 6. Reconciliation

Where personas or lenses disagreed, I chose, and recorded the losing option
([conflicts-and-ambiguities.md](conflicts-and-ambiguities.md)).

### 7. Consolidation

Each finding was resolved to exactly one of: requirement, assumption, decision (ADR), open question or deferral.
A finding resolved as "requirement" must be cited by at least one requirement; the seeding check found 11 that
were not and closed them before the baseline.

## Categories

The register uses §22's eleven categories plus three added deliberately — **a checklist only finds what it
names**, and these three hold some of the highest-impact findings:

| Added category | Example finding none of §22's headings would have surfaced |
|---|---|
| Domain and identity model | A 23:50 departure belongs to the previous service day — calendar-date logic breaks "next train" every night |
| Data and privacy | Driver position is employee monitoring; it needs limits the brief never mentions |
| Testability and simulation | Without an injectable clock, "12 minutes late at 14:37" cannot be tested |

## Outputs

| Output | Count at seed |
|---|---|
| Discovery findings | 116 |
| Requirements | 102 |
| Assumptions / facts / decisions | 34 |
| Conflicts | 10 |
| Ambiguities | 10 |
| Open questions | 16 |

## What the method cannot do

It cannot establish facts about this organisation's existing systems, people or processes. Every such gap is
recorded as an open question and an assumption with a risk-if-wrong line rather than filled in.
