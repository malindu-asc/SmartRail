# SPEC-021 Passenger accounts

| Field | Value |
|---|---|
| Status | In progress |
| Owner | Malindu |
| Created | 2026-10-19 |
| Approved | 2026-10-19, Malindu ("yes implement those") |
| Branch | `feature/passenger-app` |
| Modules | Identity (owner), Keycloak realm, passenger app |
| Changes | API contract (`/api/account/*`), Keycloak realm, passenger screens Log in, Create profile, Me |

## 1. Summary

A passenger can create a profile, log in, see and change app settings, download their data, delete their data and sign out. Browsing never needs an account (ASM-012); tickets do.

## 2. Traceability

REQ-ACC-001..004, REQ-PAX-012 (delete data), REQ-PAX-013 (language), ASM-012. Design: Log in (16), Create profile (17), Me (32), Delete my data (33).

## 3. Scope

**In scope:** `POST /api/account/register`, `POST /api/account/forgot-password` (always 202), `GET /api/account/me`, `GET /api/account/me/export`, `DELETE /api/account/me`. Keycloak holds the credentials; the API creates users through a service account (`smartrail-identity-admin`, user management only). Other modules join the export and delete through `IPersonalDataContributor` (Ticketing does).
App: secure token storage, refresh before expiry, Me rows (language, text size, notifications on/off, download, delete, call centre, sign out).

**Decision D-ACC-1:** the app has its own forms (Figma), so it signs in with the password grant on `smartrail-mobile` (brute-force lockout stays on). Moving to the browser flow (authorization code with PKCE) is a later hardening; the client already allows it.

**Out of scope:** email verification and SMS (no email service in the MVP), social login, phone push.

## 4. Acceptance criteria

| ID | Criterion |
|---|---|
| AC-1 | Creating a profile with a taken email says so without revealing anything else. |
| AC-2 | The password needs 8 characters and the terms must be accepted. |
| AC-3 | Forgot password gives the same answer for any email. |
| AC-4 | A signed-in app renews its token before it expires and signs out only when the refresh is refused. |
| AC-5 | Delete my data asks first, removes the profile and personal links, and keeps ticket records without a person attached. |
| AC-6 | Download my data shows the profile and each module's data as JSON. |

## 5. Verification

Backend unit and Testcontainers tests, widget tests of the three screens: run later. Needs `infra/keycloak/smartrail-realm.json` re-imported (new client and the mobile direct-grant flag).

## 6. Change log

- 2026-10-19: created.
