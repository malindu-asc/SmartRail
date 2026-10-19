# SPEC-009 Passenger app: first live screens (launch, onboarding, home, stations, departures, journey, follow, offline)

| Field | Value |
|---|---|
| Status | In progress |
| Owner | Malindu |
| Created | 2026-10-19 |
| Approved | 2026-10-19, Malindu (on the instruction to start building the passenger app; decisions D1 to D14 of the walkthrough and Q1 to Q6 here accepted as drafted) |
| Branch | `feature/passenger-app` |
| Pull requests | — |
| Modules | none (uses the existing public API) |
| Apps | passenger app |
| Changes | UI, local storage, polling, motion |

**Pre-spec evaluation:** not trivial (the first real screens and the app's data layer). Touches only `apps/mobile`. Uses the public Network and Journeys endpoints that exist today. Deliverable in about three days. Depends on SPEC-008 (foundation). Design: Figma file `SmartRail-HighFed`, decisions in `docs/01-ux/passenger-app-walkthrough.md`.

## 1. Summary

A passenger can open the app, see the first-run introduction once, land on Home, pick a station, watch its live departures, open a journey and follow its stops with honest status, follow stations and journeys, and still see what the app last knew when there is no connection. Everything shows the scheduled time next to the live one, and says so when data is old or missing.

## 2. Motivation

This is the product's first visible promise: live, honest train information without an account (REQ-PAX-001). It proves the whole chain (feed, ingest, journey state, API, phone) on real screens, and it is what the simulator exists to show.

## 3. Traceability

| Kind | IDs |
|---|---|
| Requirements | REQ-PAX-001 (public information without sign-in), REQ-PAX-002 (station departure board), REQ-PAX-003 (journey detail timeline), REQ-PAX-004 (status never colour only), REQ-PAX-005 (staleness labelling), REQ-PAX-010 (offline cache), REQ-PAX-011 (indicative information note), REQ-PAX-014 (screen reader, text scaling), REQ-LIVE-012 (departure never earlier than scheduled), REQ-LIVE-013 (interrupted shows no estimate), REQ-NFR-005 (network time), REQ-NFR-006 (degraded modes) |
| Design | Figma "SmartRail-HighFed" page Passenger app: 00 Launch, 01 to 03 Onboarding, 06 and 34 Home, 18 Find a station, 07 Departures, 08, 09, 23 and 36 Journey, 20 Home offline |
| Decisions | `docs/01-ux/passenger-app-walkthrough.md` (D1 to D6, D13, D14) |

## 4. Scope

**In scope**
- Launch, onboarding (three slides, white art container, Skip, Next, Go to app), shown once.
- Home (hybrid, D3): green header with greeting, the From / To card (opens the station picker), *Followed stations* with their next departure and status, and an empty state that invites following a station. No bell (D1). The *Your trip today* card and the disruption banner appear when their data exists (tickets: SPEC-022, disruptions: SPEC-027); until then they are not shown.
- Stations: search by name or code with a debounce, results with their facilities as caption, recent stations.
- Departures at a station: the live board with expected time, delay chip, platform and platform-changed chip, cancelled, no-live-data, a freshness line ("Live · updated 14:18" or "Saved 14:06"), pull to refresh, polling while visible.
- Journey: the v2 layout for on time, delayed, no live data and interrupted: route line header, status card, platform banner, stop timeline (scheduled time kept visible, expected time beside it), estimate and confirmed wording per stop, an "estimates may change" note, Follow this journey / Following.
- Follow: followed stations and followed journeys stored on the phone (no account). Notifications for them are SPEC-020.
- Offline and stale: the last good answer with its fetch time is shown with an offline banner; stale and Unknown data say so.
- Motion (walkthrough section 5) and the Material component themes in the design system.

**Out of scope** (and the spec that builds it)

| Item | Where |
|---|---|
| Plan a journey, Results, date and time | SPEC-016 |
| Alerts tab, push notifications | SPEC-020 |
| Log in, Create profile, Me, delete my data | SPEC-021 |
| Tickets, choose a ticket, seat, review, ticket | SPEC-022 |
| Disruption screen and banner | SPEC-027 |
| Food at a stop | SPEC-030 |
| Location "near me" | SPEC-014 |
| Tabs that are not built (Plan, Tickets, Alerts, Me) show a clear "coming" placeholder, never a dead end | this spec |

## 5. Detailed design

### 5.1 Data (public API, no sign-in)

| Screen | Call |
|---|---|
| Stations search | `GET /api/network/stations?q=` |
| Departures | `GET /api/journeys/boards/{code}?count=20` |
| Journey | `GET /api/journeys/{id}` |

All reads go through `ApiClient` (ETag revalidation, language header, stale copy on failure). Times arrive as UTC and are shown in the network zone with `NetworkClock`.

### 5.2 Showing live data honestly

- A board row shows the **scheduled time** and, when different, the **expected time** with the scheduled one struck through (as in the design). Cancelled shows no expected time. A row with `confidence` Unknown shows "No live data" and its scheduled time.
- A journey with `isStale` shows "Live · updated 14:18" turning into "As of 14:31"; Unknown shows the "No live data since" card with "Showing the timetable. Times are not live. Listen for station announcements."; Interrupted (when it exists) shows no delay figure.
- Stops show `Departed 14:25 · on time`, `Expected 14:31`, or `Estimate · may change` from the stop's estimate source. A passing point is never shown (the API already hides it).
- The status line of the journey is an icon, words and a colour (REQ-PAX-004).

### 5.3 Polling

A board or journey screen refreshes every 10 seconds while it is on screen and the app is in the foreground, and immediately on pull to refresh and on resume. The API answers from a 5 second cache, so this does not hammer it. Polling stops when the screen is left and when the app goes to the background.

### 5.4 Local storage

`shared_preferences`: onboarding seen, recent stations (last 5), followed stations (codes), followed journeys (id and a small summary so Home and the offline view can show them). A thin repository per concern, behind a domain interface, so a real account can replace it later.

### 5.5 Structure

Feature folders as in the skill: `onboarding`, `stations`, `board` (departures), `journeys`, `follow`, `home`, each with `domain`, `data`, `presentation`. Shared widgets that are not feature-specific stay in the design system.

### 5.6 Motion

As in the walkthrough: fade-through between tabs, shared-axis forward for opening a journey, cross-fade of a changed status, the timeline marker easing, a press scale, a first-load stagger, quiet skeletons, a widening onboarding dot. Off when the phone's reduce-motion setting is on.

### 5.7 Material theme

The design system theme gains the component themes the screens need (inputs, cards, chips, bottom sheets, navigation bar labels, snackbars), so plain Material widgets already match the design.

## 6. Acceptance criteria

| AC | Given / When / Then | Traces to |
|---|---|---|
| AC-1 | First launch shows the launch screen then three onboarding slides with Skip, Next and Go to app; after finishing or skipping, the next launch goes straight to Home. | design 00 to 03 |
| AC-2 | The onboarding art container is white, not green. | owner decision |
| AC-3 | Home shows the green header, the From / To card and, for each followed station, its next departure and status; with none followed it shows an invitation; it has no bell. | D1, D3 |
| AC-4 | The station search finds stations by name or code as the user types (debounced), shows the facilities as a caption, remembers the last 5 stations, and says when nothing matches. | REQ-PAX-001 |
| AC-5 | Departures lists the live board in order with the expected time and the scheduled time struck through when they differ, a delay chip, the platform and a "platform changed" chip, cancelled, and no-live-data rows; each status has an icon, words and a colour. | REQ-PAX-002, 004 |
| AC-6 | The board shows "Live · updated hh:mm" in network time; when the data is stale it shows its age; when the API cannot be reached it shows the saved board with "Saved hh:mm" and the offline banner. | REQ-PAX-005, 010, NFR-006 |
| AC-7 | The board and journey screens refresh every 10 s while visible and in the foreground, stop when left or backgrounded, and refresh on pull and on resume. | 5.3 |
| AC-8 | The journey shows the route line header, the status card, the platform banner when the platform changed, and every stop with its scheduled time and its confirmed or expected time; a departure is never shown earlier than scheduled. | REQ-PAX-003, LIVE-012 |
| AC-9 | A delayed journey shows the delta, the delayed stop times with the scheduled ones struck through, and the "estimates may change" note. | REQ-PAX-002, 003 |
| AC-10 | A journey with no live data shows the no-live-data card and scheduled times only, with no delay figure and no "expected" wording. | REQ-LIVE-004, 013 |
| AC-11 | Following a station or a journey works without an account, survives an app restart, shows on Home, and can be undone; the follow button reads Follow this journey and Following. | follow |
| AC-12 | Switching language to Sinhala changes every string on these screens; station names come from the API in the chosen language. | REQ-PAX-013 |
| AC-13 | The five tabs exist; Plan, Tickets, Alerts and Me show a clear placeholder naming what is coming; the tab bar is hidden inside the journey flow only if it would hide a needed action (it is kept on Departures and Journey). | D2 |
| AC-14 | At 200 percent text the screens lay out without overflow; every tappable item is at least 48 dp; the screens have semantic labels (status read as words). | REQ-PAX-014 |
| AC-15 | With the phone's reduce-motion setting on, no transition or animation runs. | motion |
| AC-16 | The indicative-information note ("Times are provided for information") is available from the departures and journey screens. | REQ-PAX-011 |

## 7. Test plan

| Area | Kind | Where |
|---|---|---|
| Entities and mapping from the real response shapes, status wording, struck-through logic | Unit | `apps/passenger/test/` |
| Repositories with a fake HTTP adapter, local storage with an in-memory store | Unit | same |
| Each screen with provider overrides: onboarding, home, search, departures, journey (on time, delayed, no live data), follow, offline | Widget | same |
| Polling start and stop, reduce motion | Widget with a fake clock | same |
| A run against the real API with the simulator (manual, recorded in verification) | Manual | verification table |

## 8. Dependencies

SPEC-008 (foundation), SPEC-004 and SPEC-005 (the data), SPEC-006 (the simulator that makes it move).

## 9. Risks

| Risk | Mitigation |
|---|---|
| Design samples disagree with each other (walkthrough section 4) | Build from the real API shapes and the component rules, not from sample numbers |
| Polling drains battery | 10 s only while visible and foregrounded; the API caches 5 s; ETag keeps bodies tiny |
| Local follows are lost on a new phone | Stated; accounts (SPEC-021) can sync them later |
| The simulator cannot show Stale quickly | Tests use a fake clock; manual runs use speed 1 |

## 10. Design decisions

| # | Decision | Why |
|---|---|---|
| D1 | Follow is local first | Works without an account (REQ-PAX-001); notifications are a later layer |
| D2 | The screen asks the repository, the repository asks the API client, nothing else touches HTTP | Clean layers, testable |
| D3 | Times are converted at the edge by `NetworkClock` | One place for REQ-NFR-005 |
| D4 | Polling lives in a provider tied to screen visibility | No work when nobody looks |

## 11. Open questions

- **Q1.** Placeholders for the unbuilt tabs say what is coming (no dead ends). Draft: yes.
- **Q2.** Recent stations: last 5, newest first, stored on the phone. Draft: yes.
- **Q3.** The board shows 20 rows and a "Later trains" load more is deferred. Draft: yes.
- **Q4.** The poll interval is 10 s. Draft: yes.
- **Q5.** Pull to refresh on both screens. Draft: yes.
- **Q6.** English and Sinhala only; Sinhala wording to be reviewed by a Sinhala speaker. Draft: yes.

## 12. Tasks

- [ ] Design system: Material component themes, motion helpers, board and timeline widgets
- [ ] Onboarding and launch
- [ ] Stations (search, recents)
- [ ] Departures (board, polling, offline copy)
- [ ] Journey (timeline, statuses, follow)
- [ ] Follow storage and Home
- [ ] Shell: tabs, placeholders, transitions
- [ ] Localization (en, si)
- [ ] Tests for every AC; analyze clean
- [ ] Manual run with the simulator

## 13. Review record

| Date | Reviewer | Decision | Notes |
|---|---|---|---|
| 2026-10-19 | Malindu | Approved | Instruction to start building the passenger app |

## 14. Verification

| AC | Result | Evidence |
|---|---|---|
| AC-1 to AC-16 | Not run | |

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
