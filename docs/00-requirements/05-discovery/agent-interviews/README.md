# Agent interviews (Claude level)

Two rounds of simulated stakeholder interviews, kept apart because they answer different questions.

| Round | Method | Folder | Question it answers |
|---|---|---|---|
| **Discovery** *(primary)* | Persona is told **nothing** about the system; describes their work, what goes wrong, what they rely on | `raw/` + [synthesis.md](synthesis.md) | What do these people actually need and struggle with? |
| Validation *(earlier)* | Persona is shown the proposed system and reacts | [validation-round/](validation-round/README.md) | Does what we propose make sense to them? |

Real teams do the two in that order: discover first (no leading), validate after (react to the concept). Running the
validation round first led the personas, so some of its agreement was an echo. The discovery round replaces it as the
primary evidence; the validation round is kept for comparison, not deleted.

This folder is deliberately **separate**: nothing here changes the requirements. Findings are compared with our register in
`synthesis.md` and only adopted by decision.

| File | Purpose |
|---|---|
| [protocol.md](protocol.md) | Discovery rules, the five questions, output format |
| `raw/` | One discovery transcript per persona |
| [synthesis.md](synthesis.md) | What they raised unprompted; what we already had; what we missed; where the two rounds differ |
| [validation-round/](validation-round/README.md) | The earlier round and its synthesis |

## Personas

| # | Persona | Brief §22? |
|---|---|---|
| 01 | Daily commuter (passenger) | yes |
| 02 | Occasional traveller (passenger) | yes |
| 03 | Train driver | yes |
| 04 | Railway controller | yes |
| 05 | Station manager | yes |
| 06 | Scheduler | added |
| 07 | Ticket inspector | added |
| 08 | Customer-service representative | yes |
| 09 | Security engineer | yes |
| 10 | QA engineer | yes |
| 11 | Accessibility specialist | yes |
| 12 | Product owner | yes |

The catering provider is omitted because catering was cut from the MVP (ASM-005).

## Limits (stated, not hidden)

- **Revision:** a first pass used three interviewer-written scenario probes per persona. They steered the answers, so they were removed and each persona instead chose two follow-up stories from their own answers ([protocol.md](protocol.md)).

- All agents are the same model family as the assistant that drafted our docs. The cross-check with a **different model**
  (Gemini / ChatGPT) is a separate step.
- These are simulations, not evidence about a real organisation (ASM-034). Each agent's claim to have read only the
  protocol is self-reported.
- The four lens personas (security, QA, accessibility, product owner) are told only that a railway is planning a digital
  platform for passenger information and operations — they cannot review nothing — and nothing more.
