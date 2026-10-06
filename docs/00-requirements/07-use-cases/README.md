# Use cases

One file per application. Every use case has **failure flows**, not just a happy path — in this domain the
failure flows are where most of the requirements live.

| File | Application | Use cases |
|---|---|---|
| [passenger.md](passenger.md) | Passenger app (Flutter) | UC-PAX-01 … 07 |
| [driver.md](driver.md) | Driver app (Flutter) | UC-DRV-01 … 07 |
| [operations.md](operations.md) | Ops console (React), all staff roles including station operations | UC-OPS-01 … 15 |
| [station-display.md](station-display.md) | Station displays (React kiosk, display mode) | UC-DSP-01 … 04 |
| [station-terminal.md](station-terminal.md) | Station terminal (React kiosk, terminal mode) | UC-TRM-01 … 05 |
| [external-catering.md](external-catering.md) | External catering provider (RailBite) and its passenger touchpoint | UC-EXT-01 … 05 |

Cross-application behaviour — one event travelling through every app — is in
[../08-cross-app-flows/](../08-cross-app-flows/README.md). Use cases link to those flows rather than repeating them.

## Template

```
### UC-<APP>-nn — <goal in the actor's words>

| | |
|---|---|
| Actor | primary actor (persona) |
| Goal | what they want |
| Trigger | what starts it |
| Preconditions | what must be true |
| Authority | permission required (permission-matrix.md) |
| Priority | Must / Should / Could · MVP Yes / Thin / No |

Main flow — numbered steps
Alternate flows — A1, A2 …
Failure flows — F1, F2 … (each states what the actor SEES)
Requirements — REQ ids
Edge cases — EDG ids
Cross-app flows — CF ids
```

## Coverage summary

| Application | Use cases | Must | MVP Yes | MVP Thin |
|---|---|---|---|---|
| Passenger | 7 | 5 | 7 | 0 |
| Driver | 7 | 5 | 6 | 1 |
| Operations | 15 | 9 | 9 | 6 |
| Station display | 4 | 4 | 4 | 0 |
| Station terminal | 5 | 3 | 5 | 0 |
| External catering | 5 | 0 | 0 | 5 |
