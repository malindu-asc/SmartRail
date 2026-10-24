# Passenger app: walkthrough, decisions and screen inventory

**Source:** Figma file `ZCUQkZpq24jKc7zLvs5KFR` "SmartRail-HighFed", page *Passenger app* (node `3:3`), read screen by screen on 2026-10-19.
**Status:** Decisions below are the designer-engineer proposal for owner review. Where the owner has already decided, it says so.
**Owner decisions already made (2026-10-19, Malindu):**
1. Seat booking exists **only for long-distance trains**. A normal (regional) train has no seat step.
2. **No payment in the MVP.** Screens 13 and 14 and everything that needs money (receipts, refunds, booking fee, card) are out for now.
3. The onboarding illustration container is **white**, not light green.
4. Food at a stop (screens 36 to 38) is in; the map screen 19 is **not needed**.
5. Use **Material 3** the way Libra does (`C:\works\MobileWorkshop\Libra`: `ThemeData` with component themes), with **small, purposeful motion**.

---

## 1. The end-to-end flow as designed

```
Launch (00) -> Onboarding x3 (01 to 03) -> Home (06 / 34)
 |
 |- Home -> Departures at a station (07) -> Journey (08 on time | 09 delayed | 23 no live data) -> Follow this journey
 |       -> Disruption (22) via the banner          -> Food at a stop (36 -> 37 | 38 menu unavailable)
 |       -> Stations search (18)                    -> [Allow location (05) only when "near me" is tapped]
 |
 |- Plan tab -> Plan a journey (10) -> Date and time sheet (11) -> Results (12 / 35)
 |        -> Choose a ticket (29) -> Who is travelling (30) -> [Choose a seat (31): long-distance only]
 |        -> Review (13, without payment) -> Ticket (15) -> Tickets list (26) -> "Ticket ready" sheet (27)
 |
 |- Tickets tab: empty (24), add by booking code (25), list (26), cancelled train (28)
 |- Alerts tab (21): platform changed, cancelled, delay, correction
 |- Me tab (32): language, text size, notifications, download or delete my data (33), contact, sign out
 |- Log in (16) / Create profile (17): only when a ticket is needed. Browsing never needs an account.
 |- Offline Home (20): saved data with its age, honest about being old
```

## 2. Decisions

| # | Question | Decision | Why |
|---|---|---|---|
| D1 | The notification **bell** (top right) and the **Alerts** tab | **Keep the Alerts tab (with an unread dot), remove the bell.** | Both open the same screen: two doors to one room. Alerts (platform change, cancellation) are the product's core promise, so they earn a permanent, one-tap tab. The bell appears only on Home (not on other tabs) and again on the offline Home, which is inconsistent. The top right of Home is better used for the live-data freshness ("Live · updated 14:18"). |
| D2 | Bottom navigation | **Keep five tabs: Home, Plan, Tickets, Alerts, Me.** Material 3 `NavigationBar`, hidden inside task flows (choose ticket, seat, review, ticket detail). | Five is the Material limit and every tab is a distinct job. Hiding it in a funnel removes escape routes that cause half-finished bookings; the back arrow and a clear close are enough. |
| D3 | **Home v1 or v2** | **A hybrid.** Use v2's green header, greeting and compact "From / To" card. Keep v1's *Your trip today* card, the disruption banner and *Followed stations*. **Drop the 8-icon shortcut grid.** | v2 puts the main action (find a train) first, which is right. But v2 drops the trip-today card and followed stations, which are the reason a returning passenger opens the app. The grid duplicates tabs (Plan, Tickets, Alerts), and three of its eight items are not MVP (Refunds needs payment, Step-free needs station access data, Food needs a train and a stop to mean anything). |
| D4 | The Home "From / To" card and the Plan tab | **One planner screen, two entry points.** Tapping the Home card opens the Plan route with the stations prefilled; there is no second copy of the logic. | Avoids two divergent implementations. |
| D5 | **Results v1 or v2** | **v2** (date strip, route-line cards, filter) **plus** v1's *Earlier trains / Later trains* and the *Tight connection* note. | The date strip answers "tomorrow?" in one tap and shows the line of the journey. v1's extras are real rules (REQ-PAX-006 one transfer) and must not be lost. |
| D6 | **Journey v1 or v2** | **v2 for all three states** (on time, delayed, no live data). v2 so far exists only for "on time". | The compact header with the route line is better, and the food row belongs on every journey that has catering. Mixed layouts across states would be jarring. |
| D7 | **Seat selection** | A service has `seatReservation = required | none`. The seat step and the "Seat required" label appear **only** when `required`. Seeded long-distance service: the Northern line. | Owner decision 1. Today screen 29 shows "Seat required" on all three trains and Results v1 on a 45-minute direct train. The Network module has no such flag yet (see section 6). |
| D8 | **Payment** | Out. Screens 13 and 14, *Receipts*, *Booking fee*, *Card*, the refund amount and "3 to 5 working days" are removed. Screen 13 becomes **Review** (journey, passengers, seat, fare as information) with a **Confirm booking** button. | Owner decision 2. See the open question on what a booking is (Q1). |
| D9 | A train is **cancelled** (screen 28) | Keep the sheet; the money lines go. It says the ticket can no longer be used, offers **Find another train**, and **Contact the call centre**. | There is nothing to refund without payment. |
| D10 | **Food at a stop** | In, as designed: a row on the journey for a stop that has catering, a read-only menu with prices, **Call to order**, and the *menu unavailable* state that says trains and tickets are not affected. | Matches the catering decision (call to order, failure isolated). Built after the catering service (SPEC-030). |
| D11 | **Location permission** (screen 05) | Ask **only when the passenger taps "near me"**, with the explainer screen, never at launch. Never block the app. | Least surprise and least privacy cost; the app works fully without it. |
| D12 | **Map** (screen 19) | Dropped. | Owner decision 4 and ASM-031 (schematic timeline, not a map). |
| D13 | **Onboarding art** | White container. Three slides (the designed dots show three). Skip and Next / Go to app as designed. | Owner decision 3. The second set of onboarding frames numbered 01 to 04 is the same three slides. |
| D14 | **Design copies** | Treat the second set of the first eight frames (`77:xxxx`) and the repeated frames of 28, 31, 36 and 37 as **duplicates** of the first. | They are identical in content; the only change is numbering. |

## 3. Screen inventory (final)

| # | Screen | Figma node(s) | Decision | Built in |
|---|---|---|---|---|
| 00 | Launch | `5:2` | Keep | SPEC-009 |
| 01 to 03 | Onboarding | `5:18`, `5:46`, `5:69` | Keep, white art container | SPEC-009 |
| 05 | Allow location | `8:100` | Keep, shown on "near me" only | SPEC-014 |
| 06 / 34 | Home | `5:158`, `59:350` | Hybrid (D3) | SPEC-009 |
| 07 | Departures at a station | `5:245` | Keep | SPEC-009 |
| 08 / 09 / 23 | Journey on time, delayed, no live data | `7:66`, `7:132`, `8:494`, v2 `59:650` | v2 layout for all (D6) | SPEC-009 |
| 20 | Home, offline | `8:289` | Keep, without the bell | SPEC-009 |
| 22 | Disruption | `8:432` | Keep | SPEC-009 |
| 18 | Find a station | `8:179` | Keep | SPEC-014 |
| 10, 11 | Plan a journey, date and time | `7:199`, `7:290` | Keep | SPEC-016 |
| 12 / 35 | Results | `7:400`, `59:492` | v2 plus v1 extras (D5) | SPEC-016 |
| 21 | Alerts | `8:347` | Keep | SPEC-020 |
| 16, 17 | Log in, Create profile | `8:116`, `8:150` | Keep | SPEC-021 |
| 32, 33 | Me, Delete my data | `9:767`, `9:873` | Keep; no *Receipts* anywhere | SPEC-021 |
| 24, 25, 26, 27 | Tickets empty, add, list, ready | `9:119`, `9:183`, `9:267`, `9:325` | Keep; remove *Receipts* | SPEC-022 |
| 29, 30 | Choose a ticket, Who is travelling | `9:478`, `9:535` | Keep; "Seat required" only when it is (D7) | SPEC-022 |
| 31 | Choose a seat | `9:657` | Keep, long-distance only (D7) | SPEC-022 |
| 13 | Review | `7:447` | Adapt: no payment (D8) | SPEC-022 |
| 15 | Ticket | `7:547` | Keep | SPEC-022 |
| 28 | Train cancelled | `9:396` | Keep without money (D9) | SPEC-022 |
| 36, 37, 38 | Journey food row, Food menu, Menu unavailable | `59:650`, `59:743`, `59:800` | Keep (D10) | SPEC-030 |
| 14 | Confirm payment | `7:488` | **Dropped** (D8) | n/a |
| 19 | Station on map | `8:242` | **Dropped** (D12) | n/a |

## 4. Mismatches found in the design (to fix in Figma before they reach code)

| # | Where | Mismatch |
|---|---|---|
| M1 | Home v1, Alerts, offline Home | A bell and an Alerts tab: same destination twice (D1). |
| M2 | Home v2 | Shortcut grid duplicates three tabs and includes items outside the MVP (D3). |
| M3 | Choose a ticket (29), Results v1 | "Seat required" on every train, including a 45-minute direct one (D7). |
| M4 | 13, 14, 25, 28, Home v2 | Payment, receipts, refunds and the booking fee (D8). |
| M5 | Journey v2 | Only the "on time" journey has the v2 layout; delayed and no-live-data still use v1 (D6). |
| M6 | Departures, Disruption, Alerts, Home, Results, Tickets | The same 14:31 Hill Town train is **cancelled** in Departures, Disruption and Alerts, but **+7 min** in Home and Results; the ticket for "Central to Riverside 14:31" is *Affected* while the cancelled train goes to Hill Town. |
| M7 | Home vs Choose a ticket | The 15:20 train is 22 minutes (15:20 to 15:42) on Home and 48 minutes (15:20 to 16:08) in Choose a ticket. |
| M8 | Results to Journey | A Hill Town result opens a journey titled Central to Riverside. |
| M9 | Journey screens | The header says "14:25 to Riverside" and the big title repeats "Central to Riverside" (v2 removes the repetition). |
| M10 | Onboarding | The third slide shows an aeroplane and an airport tower for a railway app; the speech bubble is a taxi sign. |
| M11 | Ticket (15), Add ticket (25) | The sample booking code `SR-8821-0097` has 10 letters and digits, while the hint says "8 or 12 characters". |
| M12 | Plan sheet, Results | Dates are "Wed 16 Oct" (a 2024 calendar); the app shows real dates, so only the samples are wrong. |
| M13 | Find a station | "Harbour line" and "Northern line" are named differently from the seeded routes (Main, Hill, Coast, North). Names come from the API, so the sample copy should follow the seed. |
| M14 | Duplicates | Two copies of the first eight frames and repeated copies of 28, 31, 36, 37 (D14). |

## 5. Motion (small, purposeful, never in the way)

Material 3 motion with short durations (150 to 300 ms, standard easing). All of it switches off when the phone's reduce-motion setting is on.

| Where | Motion | Why |
|---|---|---|
| Between tabs | Fade-through, 200 ms | Keeps the sense of place without sliding |
| Opening a journey or a sheet | Shared-axis forward and bottom-sheet slide | Shows hierarchy |
| Status changes (on time to delayed, platform changed) | `AnimatedSwitcher` cross-fade of the chip, 200 ms | A change is noticed, not announced |
| Timeline | The "train is here" marker eases to the next stop when the data updates | Shows movement without a map |
| Seat picker | `AnimatedContainer` on select, and a short scale on tap | Immediate feedback |
| Buttons | 0.98 scale on press, 100 ms | Touch feedback |
| Lists | One-time staggered fade-in of the first screenful (30 ms apart), never on refresh | Calm first load |
| Loading | A quiet skeleton, not a spinner, for lists and the board | Reduces perceived wait |
| Onboarding | The active dot widens (as Libra's onboarding does) | Orientation |
| Stale or Unknown | No motion: nothing pulses or flashes (REQ-DSP-008 spirit) | Honesty over attention |

## 6. What this changes elsewhere

- **Network module:** a service needs a `seat_reservation` flag (required or none), seeded so only the long-distance route requires seats. Today there is none. Owned by SPEC-022 with SPEC-003's data.
- **Payments and refunds (SPEC-023, SPEC-024) and scenarios SIM-19 and SIM-20:** deferred out of the MVP by this decision. The requirement documents still say "MVP: Yes" for REQ-PAY-001 to 003, REQ-TKT-006, REQ-TKT-007 and REQ-REF-001 to 005. They need the owner's approval to change, and the coverage check needs a "deferred" owner state (a small change to `tools/docs/check_spec_coverage.py`).
- **Tickets without payment:** see Q1.
- **Theme:** the design system adds Material component themes (inputs, cards, chips, sheets, navigation bar) so plain Material widgets are already correct, as Libra does.

## 7. Open questions

- **Q1.** Without payment, what is a booking? Draft: a **reservation** that is issued immediately as a valid ticket for the demo, with the fare shown for information. The alternative is "pay at the station", which needs wording the design does not have.
- **Q2.** Does a ticket still need an account? Draft: yes (Tickets tab and booking ask for log in; browsing never does), as designed.
- **Q3.** Should the *Alerts* tab show an unread dot or a count? Draft: a dot (a count invites anxiety).
- **Q4.** Which route is "long-distance" in the demo? Draft: the Northern line.
- **Q5.** The delayed journey button reads **Following**: tapping it again unfollows. Draft: yes, with a confirmation toast and *Undo*.
