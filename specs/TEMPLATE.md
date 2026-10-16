# SPEC-NNN Title in plain words

| Field | Value |
|---|---|
| Status | Draft |
| Owner | |
| Created | YYYY-MM-DD |
| Approved | — (date, by whom) |
| Branch | `feature/<feature-name>` |
| Pull requests | — |
| Modules | Identity / Network / Journeys / Operations / Notifications |
| Apps | API / passenger app / driver app / console / kiosk / catering / simulator |
| Changes | API contract / events / data model / UI / permissions / observability |

## 1. Summary

One or two sentences: what changes, for whom.

## 2. Motivation

Which problem this solves and who has it. Why now.

## 3. Traceability

| Kind | IDs |
|---|---|
| Requirements | REQ-… |
| Use cases | UC-… |
| User stories | US-… |
| Cross-app flows | CF-… |
| Edge cases | EC-… |
| Decisions / open questions | D-…, OQ-… |
| ADRs | ADR-… |
| Design | Figma frame names or links |

## 4. Scope

**In scope**
- …

**Out of scope** (and why)
- …

## 5. Detailed design

### 5.1 User flow

1. …

### 5.2 API contract

| Method and route | Who may call | Request | Success | Errors |
|---|---|---|---|---|
| `POST /api/<module>/…` | Policy `…` | `{ … }` | `201` `{ id }` | `400` validation, `403`, `404`, `409` |

### 5.3 Events

| Event | Kind (domain / integration) | Raised when | Consumed by |
|---|---|---|---|

### 5.4 Data

| Table (schema) | Columns and types | Constraints and indexes | Migration |
|---|---|---|---|

### 5.5 Business rules

1. Rule, with the requirement it comes from.
2. What happens when data is missing, late, duplicated or out of order.

### 5.6 UI

Screens and states (loading, empty, error, offline, stale), with Figma frame names.

### 5.7 Security and permissions

Who can do what. What is audited. What is never logged.

### 5.8 Observability

Metrics, traces and log fields this change adds, and any alert.

## 6. Acceptance criteria

Each criterion is testable and maps to at least one automated test.

- [ ] **AC-1** Given …, when …, then ….
- [ ] **AC-2** …

## 7. Test plan

| AC | Test type | Test name or location |
|---|---|---|
| AC-1 | Unit / integration / UI / manual | |

## 8. Dependencies

Other specs, services or data this needs first.

## 9. Risks

| Risk | Likelihood | Mitigation |
|---|---|---|

## 10. Open questions

- Q: … (owner, date)

## 11. Tasks

- [ ] Domain
- [ ] Application (commands, queries, validators)
- [ ] Infrastructure (persistence, migration, adapters)
- [ ] API endpoints
- [ ] Clients (app screens)
- [ ] Tests for every AC
- [ ] Docs and traceability updated

## 12. Review record

| Date | Reviewer | Decision | Notes |
|---|---|---|---|

## 13. Verification

| AC | Result | Evidence (test run, screenshot, PR) |
|---|---|---|
| AC-1 | Pass / Fail | |

## 14. Sync status

- [ ] Requirements or use cases updated if this spec changed them
- [ ] ADR created or updated (if a decision was made)
- [ ] README or developer docs updated (if setup or behaviour changed)
- [ ] Traceability regenerated
- [ ] Register in `specs/README.md` updated

## 15. Change log

| Date | Change | Approved by |
|---|---|---|
