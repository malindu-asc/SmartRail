# SPEC-008 Flutter foundation: shared packages, app shell, theme, router, localization

| Field | Value |
|---|---|
| Status | In progress |
| Owner | Malindu |
| Created | 2026-10-19 |
| Approved | 2026-10-19, Malindu (on the instruction to start the Flutter foundation; drafts accepted for Q1 to Q6) |
| Branch | `feature/flutter-foundation` |
| Pull requests | — |
| Modules | none (client only) |
| Apps | passenger app, driver app (`apps/mobile`) |
| Changes | UI foundation, shared packages, localization, API client |

**Pre-spec evaluation:** not trivial (the base every screen of two apps stands on). It touches only `apps/mobile`. No backend change: it calls the existing public API. Deliverable in about three days. It does not build any feature screen: the departures board is SPEC-009, driver sign-in and duty are SPEC-011. Skill: `smartrail-flutter`.

## 1. Summary

Turn the empty Flutter workspace into a working base: three shared packages (`core`, `design_system`, `api_client`) and two apps (`passenger`, `driver`) that boot into a themed shell with routing, English and Sinhala strings, an API client that understands the platform's errors and caching, honest offline handling, and tests. Every later screen only adds a feature folder.

## 2. Motivation

- **Two apps, one design language.** Colours, spacing, type and the status chips must be written once, mirror the Figma, and make it impossible to show status by colour alone (REQ-PAX-004).
- **The platform's honesty rules reach the phone.** Times are UTC on the wire and shown in the network time zone (REQ-NFR-005). Stale data is shown as stale, not hidden (REQ-PAX-005). A phone that cannot reach the API says so (REQ-NFR-006).
- **Everything after it is faster.** SPEC-009 (board and journey) and SPEC-011 (driver) start from a green, tested base.

## 3. Traceability

| Kind | IDs |
|---|---|
| Requirements | REQ-PAX-004 (status never colour only), REQ-PAX-013 (two languages), REQ-PAX-014 (screen reader and text scaling), REQ-NFR-003 (WCAG 2.2 AA baseline for the apps), REQ-NFR-005 (time handling), REQ-NFR-006 (degraded mode: say when offline), REQ-PAX-005 (staleness is shown, partly: the widget), REQ-SEC-001 (no secrets in logs) |
| Skill | `.claude/skills/smartrail-flutter/SKILL.md` (stack, layout, dependency rules, design rules) |
| Design | Figma file `ZCUQkZpq24jKc7zLvs5KFR` "SmartRail-HighFed", Components page (buttons, status chip, input, driver components): the source of the tokens. The FigJam board `EcKnblwhKIQNeH6lDlPI5N` holds the wireframes (layout, touch targets) |
| Reference | `C:\works\MobileWorkshop\Libra` (patterns only, not copied) |

## 4. Scope

**In scope**
- **`smartrail_core`**: `Failure` hierarchy, `UseCase` base, a `NetworkClock` that formats UTC instants in the network time zone, small extensions.
- **`smartrail_design_system`**: colour, spacing, radius and type tokens (Inter), the light `ThemeData`, a passenger and a driver theme variation (touch target 48 dp and 72 dp), and shared widgets: `PrimaryButton`, `StatusChip`, `StalenessLabel`, `EmptyState`, `ErrorState`, `OfflineBanner`, `Skeleton`. Widgets take their text from the caller (no hard-coded strings).
- **`smartrail_api_client`**: a Dio client with the base URL from `--dart-define`, `Accept-Language`, `ProblemDetails` mapped to `Failure`, `ETag` revalidation for public reads with the fetch time kept, a timeout and one safe retry for reads, an optional bearer-token provider, and **no token or personal data in logs**.
- **Both apps**: `main.dart`, `ProviderScope`, `MaterialApp.router`, themes, a go_router with a shell and placeholder home screens, a not-found route, English and Sinhala ARB files kept in step, locale switch, and an API reachability provider that drives the offline banner.
- Tests for every acceptance criterion; `flutter analyze` clean; Melos scripts work.

**Out of scope** (and the spec that builds it)

| Item | Where |
|---|---|
| Departures board, journey timeline, staleness on real data (REQ-PAX-002, 003, 005) | SPEC-009 |
| Driver sign-in, duty, offline action queue (REQ-DRV) | SPEC-011 |
| Accounts and sign-in for passengers (Keycloak, PKCE) | SPEC-021 |
| Push notifications, follow a journey | SPEC-020 |
| Station search | SPEC-014 |
| Dark theme, Tamil | not designed; added when designed |
| CI for the Flutter apps | a separate small change to `.github/workflows` once the base is green locally |

## 5. Detailed design

### 5.1 Packages and apps

As in the skill: `packages/{core,design_system,api_client}` and `apps/{passenger,driver}` in one pub workspace. A package never imports an app; apps never import each other; the domain layer of a feature never imports Flutter.

### 5.2 Core

- `Failure` is a sealed class: `NetworkFailure` (no connection or timeout), `UnauthorizedFailure` (401), `ForbiddenFailure` (403), `NotFoundFailure` (404), `ConflictFailure` (409), `ValidationFailure` (400, with field messages), `ServerFailure` (5xx) and `UnknownFailure`. Each carries a stable `code` (the API's error code when there is one) and no user-facing text: screens choose the words.
- `UseCase<Output, Params>` returns `Either<Failure, Output>` (fpdart).
- `NetworkClock.format(instant)`: formats a UTC instant as `HH:mm` in the network time zone from the response's `timeZone` field (default `Asia/Colombo`), and tells whether it falls on a different calendar day from a given service date (so "00:35" after midnight can be shown with its day). Uses the `timezone` package data.

### 5.3 Design system

- Tokens mirror the Figma foundations and live in one file each (`colors.dart`, `spacing.dart`, `radius.dart`, `typography.dart`). Feature code uses tokens only; a test fails the build on a hex colour or a raw `fontSize` in an app `lib/` folder.
- Palette, read from the Figma file's components: brand `primary #118167`, `pressed #0B5F4B`, `subtle #E3F3EE`, `on-primary #FFFFFF`; text `primary #0F1412`, `secondary #4B5563`, `tertiary #6B7280`; `surface` and `page` white, `border/strong #D1D5DB`; status text and soft background pairs: delayed `#B45309` on `#FEF3C7`, cancelled `#B91C1C` on `#FEE2E2`, changed `#1D4ED8` on `#DBEAFE`, stale `#4B5563` on `#F1F5F9` with a dashed `#94A3B8` outline. Radius: pill 999, md 12. Type: Inter, Button 16 SemiBold, Body 16 and 14 Regular, Label 14 SemiBold, Caption 12 Medium, Title 20 Bold (driver actions). Two values are not in the file's components and are carried over: the light divider `#E5E7EB` and the Headline 32 and Title 18 styles (from the board); they are marked in the token file for confirmation.
- `StatusChip(kind, label)`: an **icon, words and a colour**: delayed, cancelled, changed (platform), no live data (dashed outline), and a quiet on-time state with no colour (the Figma says colour is reserved for exceptions), with a semantic label for screen readers.
- `SmartRailButton(kind)`: full-width pill, Primary, Secondary, Tint, Danger; 56 dp and Button 16 SemiBold in the passenger app, 72 dp and Title 20 Bold in the driver app (from the theme).
- `InfoBanner(kind)` (offline, warning, message) and `SmartRailTextField` (label above, never a placeholder alone).
- `StalenessLabel(asOf, label)`: shows "as of 14:31" style text from a caller-supplied string and time; the board uses it in SPEC-009.
- `OfflineBanner(title, detail)`, `EmptyState`, `ErrorState(title, message, onRetry)`, `Skeleton`.
- Passenger theme: minimum touch target 48 dp, supports text scaling to 200 percent without overflow. Driver theme: touch target 72 dp for actions, minimum text 14.

### 5.4 API client

- `ApiClient` wraps Dio. Base URL from `String.fromEnvironment('API_BASE_URL')` (default the local API). Sends `Accept-Language` from the current locale.
- Errors: the platform returns `application/problem+json`; `ApiClient` maps status and `code` to a `Failure` and never throws raw Dio errors to features.
- Public reads use `ETag`: the client remembers the last good body and its fetch time per URL, sends `If-None-Match`, and on `304` returns the remembered body with the **original fetch time**, so a screen can show how old the data is. When the request fails and a remembered body exists, features can ask for it explicitly (stale, not hidden).
- Reads time out after 10 s and are retried once on a connection error; writes are never retried by the client.
- A log interceptor records method, path and status only: never headers, tokens, bodies or locations.

### 5.5 App shell, router, localization

- `lib/main.dart` -> `ProviderScope` -> `MaterialApp.router` with the theme, locale and `appRouterProvider`.
- Passenger router: a `StatefulShellRoute` with tabs (Home, Trips, Account) and placeholder screens that state what they will hold; `/not-found` for unknown routes. Driver router: a duty shell and a placeholder; no tab bar.
- Strings: `app_en.arb` and `app_si.arb` (flutter gen-l10n). A test fails when the key sets differ. Station and route names are not translated in the app; the API sends them localized.
- `reachabilityProvider` (in `smartrail_api_client`, shared by both apps) asks a cheap public read (`/api/network/routes`) every 30 s and when the app returns to the foreground; when it fails, `OfflineBanner` is shown over the shell with the last successful time. The health route `/alive` is not used because it exists only in Development.

## 6. Acceptance criteria

| AC | Given / When / Then | Traces to |
|---|---|---|
| AC-1 | Given a clean checkout, then `flutter pub get` resolves the workspace, `flutter analyze` reports no issues in every package and app, and all tests pass. | skill section 13 |
| AC-2 | Given each HTTP status (400, 401, 403, 404, 409, 500, 503), a timeout and a refused connection, then `ApiClient` returns the matching `Failure` with the API's error code, never a raw exception. | skill section 7 |
| AC-3 | Given a public read returns an `ETag`, then the next request sends `If-None-Match`; a `304` returns the remembered body with the original fetch time; a failure with a remembered body lets the caller ask for the stale copy. | REQ-PAX-005, REQ-NFR-006 |
| AC-4 | Given the logging interceptor, when a request carries a token, a body and a location, then the log contains the method, path and status and none of those values. | REQ-SEC-001 |
| AC-5 | Given a UTC instant and the zone `Asia/Colombo`, then `NetworkClock` formats it in local time; an instant just after local midnight is reported as the next calendar day relative to the service date. | REQ-NFR-005 |
| AC-6 | Given each status, then `StatusChip` shows an icon, a text label and a colour, and exposes a semantic label; no status is shown by colour alone. | REQ-PAX-004 |
| AC-7 | Given text scaling at 200 percent, then the shared widgets lay out without overflow. Given the passenger theme, interactive widgets are at least 48 dp; given the driver theme, action buttons are at least 72 dp. | REQ-PAX-014, REQ-NFR-003 |
| AC-8 | Given the token files, then they equal the agreed palette and type scale, and no hex colour or raw font size appears in any app `lib/` folder. | skill section 9 |
| AC-9 | Given the passenger and driver apps, then each boots to its home shell with the theme applied, an unknown route shows the not-found screen, and the passenger tabs navigate. | skill section 8 |
| AC-10 | Given the English and Sinhala ARB files, then their key sets are identical and every key has a value; switching the locale changes the visible text. | REQ-PAX-013 |
| AC-11 | Given the API cannot be reached, then the offline banner appears with the last successful time, and it disappears when the API answers again. | REQ-NFR-006 |

## 7. Test plan

| Area | Kind | Where |
|---|---|---|
| Failure mapping, ETag cache, logging, clock (AC-2 to 5) | Unit with a fake Dio adapter | `packages/core/test`, `packages/api_client/test` |
| Tokens, widgets, text scaling, touch targets (AC-6 to 8) | Widget tests | `packages/design_system/test` |
| Boot, router, localization, offline banner (AC-9 to 11) | Widget tests with provider overrides | `apps/*/test` |
| Analyze and tests (AC-1) | `melos run analyze`, `melos run test` | workspace |

## 8. Dependencies

Existing public API only (`/alive`, `/api/network/*`, `/api/journeys/*`). No backend change. Next: SPEC-009 builds the board and journey screens on this base.

## 9. Risks

| Risk | Mitigation |
|---|---|
| A token the file does not show (light divider, Headline and Title 18) is carried over from the board | Marked in the token file; confirm when SPEC-009 reads the board and journey frames |
| Inter is not bundled | The `google_fonts` package is not used (runtime download); Inter is added as an asset when the font files are provided, falling back to the platform font meanwhile |
| Package versions drift | Versions are checked with `flutter pub outdated` before adding; the lockfile is committed |
| Time zone data adds size | The `timezone` package is used with the single zone data needed |

## 10. Design decisions

| # | Decision | Why |
|---|---|---|
| D1 | Tokens in the design system only, enforced by a test | "No hex values or magic numbers in feature code" must be checkable |
| D2 | `Failure` carries a code, never text | Localization and wording belong to the screen |
| D3 | The client keeps the last good body with its fetch time | Stale must be shown, not hidden (REQ-PAX-005) |
| D4 | Only reads are retried | A repeated write could duplicate an action |
| D5 | Light theme only | Nothing else is designed |
| D6 | Plain Riverpod providers, no code generation yet | Nothing in the foundation needs a provider family; `@riverpod` is added with the first feature that does |

## 11. Open questions

- **Q1.** *Answered 2026-10-19 by Malindu, who gave the Figma file `SmartRail-HighFed`:* the file is the source of truth, brand `#118167`. The FigJam board's `#00A63E` was an older draft and is not used.
- **Q2.** Dio for the HTTP client (already in the scaffold). Draft: yes.
- **Q3.** Passenger tabs: Home, Trips, Account as placeholders (the file's components only show the driver tab bar: Duty, Messages, Me). Draft: yes, replaced by the real passenger tabs in SPEC-009.
- **Q4.** English and Sinhala now, Tamil later. Draft: yes.
- **Q5.** No CI workflow in this spec. Draft: yes, a follow-up.
- **Q6.** Inter font files are added when provided; until then the platform font. Draft: yes.

## 12. Tasks

- [ ] Core: failures, use case, network clock
- [ ] Design system: tokens, themes, shared widgets
- [ ] API client: Dio client, error mapping, ETag cache, log interceptor
- [ ] Passenger app: main, theme, router, shell, localization, reachability banner
- [ ] Driver app: main, theme, router, localization, reachability banner
- [ ] Tests for every AC; analyze clean
- [ ] README for `apps/mobile`

## 13. Review record

| Date | Reviewer | Decision | Notes |
|---|---|---|---|
| 2026-10-19 | Malindu | Approved | Instruction to start the Flutter foundation; drafts for Q1 to Q6 accepted |

## 14. Verification

| AC | Result | Evidence |
|---|---|---|
| AC-1 to AC-11 | In progress | Packages and the passenger app pass (core 8, design system 26, api client 17, passenger 8); the driver app shell is not built yet |

## 15. Sync status

- [ ] Requirements or use cases updated if this spec changed them
- [ ] ADR created or updated
- [ ] README or developer docs updated
- [ ] Traceability regenerated
- [ ] Register in `specs/README.md` updated

## 16. Change log

| Date | Change | Approved by |
|---|---|---|
| 2026-10-19 | Drafted and approved on instruction | Malindu |
