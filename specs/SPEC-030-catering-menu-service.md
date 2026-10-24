# SPEC-030 Catering menu service (Node)

| Field | Value |
|---|---|
| Status | In progress |
| Owner | Malindu |
| Created | 2026-10-19 |
| Approved | 2026-10-19, Malindu ("yes implement those") |
| Branch | `feature/passenger-app` |
| Modules | `services/catering` (Node, owner), Network (client only), passenger app |

## 1. Summary

RailBite is a separate Node.js service that owns the food menus. The .NET API has a thin client that reads a station's menu with an API key and passes it on; the phone only calls the .NET API. If the catering service is down, only the menu says so (REQ-INT-001).

## 2. Scope

- Node: `GET /v1/stations/:code/menu` (needs `x-api-key`; 404 when the station has no kitchen), prices in LKR, names in English and Sinhala, an order phone number. No SmartRail database access.
- .NET: `GET /api/network/stations/{code}/menu`, 3 second timeout, no retry; provider down gives a plain "menu unavailable"; settings `Catering:BaseUrl` and `Catering:ApiKey`.
- App: "Food at {station}" from a station's departures (only when the station has a menu), category filter, "Call to order" opens the phone dialler, unavailable state with Try again.

**Out of scope:** ordering and payment (the provider's), webhooks, menu editing.

## 3. Acceptance criteria

| ID | Criterion |
|---|---|
| AC-1 | A station without a kitchen shows no food link. |
| AC-2 | A provider outage gives "Menu unavailable right now. Trains and tickets are not affected." and nothing else in the app changes. |
| AC-3 | The request without the key is refused. |

## 4. Verification

Node smoke check done (menu, 404, 401). .NET and app tests: run later.

## 5. Change log

- 2026-10-19: created.
