# CF-06 — Degraded and missing data

**The product promise under test:** never present stale or unknown data as current.

Two kinds of failure, handled differently:

| Kind | Example | Who is affected |
|---|---|---|
| **Source failure** | The train detection feed stops reporting J-1042 | Everyone viewing J-1042 |
| **Client failure** | One platform display loses its network | Only people looking at that display |

**Test:** E2E-5 (client failure) · **Simulator:** SIM-05 (feed blackout), SIM-10 (feed and driver both dark)
· **Requirements:** REQ-LIVE-004, REQ-PAX-005, REQ-DSP-004, REQ-TRM-005, REQ-NFR-006

## A. Source failure — feed goes silent

| Time | Backend | Passenger app | Displays | Ops console |
|---|---|---|---|---|
| 08:15:00 | Last accepted update for J-1042 (+2, between Riverside and Mill Lane) | "+2 · live" | "Expected 08:42" | +2 |
| 08:16:00 | 60 s without update → **stale** (ASM-019) | "+2 · as of 08:15" | "Expected 08:42 · updated 08:15" | Age shown |
| 08:20:00 | 300 s → confidence **Unknown** (ASM-020); ETAs revert to scheduled | "**No live data — showing timetable 08:40**"; no delay figure | "08:40 · Live information unavailable" | **Highlighted** "Unknown since 08:20"; driver contact suggested |
| 08:23 | Driver app event arrives (driver confirms arrival at Mill Lane) | Updated from driver confirmation, confidence `Confirmed` | Updated | Source: driver |
| 08:26 | Feed returns | "+3 · live" | Updated | Source: feed |

**What must never happen:** extrapolating J-1042 forward along the line after 08:15 as if it were still moving.
If it broke down at 08:15, every downstream screen would be counting down to a train that is not coming.

## B. Client failure — a platform display loses its network (E2E-5)

| Time | Display | Everyone else |
|---|---|---|
| 08:30:00 | Network cut. Keeps last board. | Unaffected |
| 08:30:30 | Missed heartbeat → `Stale` | — |
| 08:31:00 | 60 s → banner **"Updated 08:30 — live information unavailable"**; live markers removed | — |
| 08:31:30 | — | Device health shows display **Offline** to the network controller (90 s) |
| 08:35:00 | 300 s → **scheduled times only**, "Listen for announcements" | — |
| 08:37:00 | Network returns → jittered backoff → snapshot → deltas → normal board | Device Online |

## C. Both feed and driver silent (SIM-10)

Same as A, but no driver confirmation rescues it. J-1042 remains `Unknown` until a source returns. Operations sees
it as an alert. The lifecycle stays `Running` — we don't know it has stopped either — and the UI says exactly
that.

## Assertions for E2E-5

1. After the network is cut to the kiosk, within 65 s the banner "live information unavailable" is visible.
2. No element on the page carries the live indicator after 65 s.
3. After 300 s, expected times are replaced by scheduled times.
4. On reconnect, the board returns to live within 10 s and no full-page reload storm occurs (single snapshot
   request observed).
