# SmartRail station displays

The wall screens of the stations (React, TypeScript, Vite). They only read: nothing here changes a train.

## Addresses

| Address | What it shows |
|---|---|
| `/` | A list of the stations; pick one to open its display |
| `/display/CEN` | The display of a station (here Central): next train, departures, arrivals, and the controller's notices |
| `/display/CEN/1` | The same, for one platform (its next trains only) |
| `/gallery` | The design review with sample data (not real trains) |

A terminal is set to open the address of its station. The screen moves between its screens by itself: the next train (10 s),
departures (14 s), arrivals (14 s), and the notices the controller has published for the station (10 s, only while there are any).

## Where the information comes from

All of it is read from the public API, every 10 seconds or so:

- `GET /api/journeys/boards/{station}`: departures, with delays, platforms and cancellations
- `GET /api/journeys/boards/{station}/arrivals`: arrivals, with where each train started
- `GET /api/operations/disruptions`: the notices the controller published (those for this station and for the whole network)
- `GET /api/journeys/{id}`: the stops of the next train, for "Calling at"

The time on the screen is the server's, not the screen's own clock, and is shown in the network time zone.

## When something goes wrong

| Situation | What the screen does |
|---|---|
| No answer for 60 seconds | Keeps the last board, footer says "Updated 14:57 · live information unavailable" |
| No answer for 5 minutes | Timetable only: every train is "Scheduled", a banner says "Live information unavailable. Listen for announcements." |
| The connection returns | Back to live on the next answer, with no one touching the screen |
| No answer since start | "Live information unavailable. Listen for announcements." and it keeps trying |
| Unknown station in the address | "This display is not set up" |
| A train it has not heard from | Shown as Scheduled, never a guessed time |
| Cancelled train | Struck through; the platform screen offers the next train to the same place |
| No trains left today | "No more trains today" |

After a failure it asks again a little later each time (10, 15, 20, then 30 seconds, with jitter), so many displays do not retry together.

## Run it

```bash
npm run dev -w kiosk      # http://localhost:5173 (Vite proxies /api to http://localhost:5080)
npm run test -w kiosk     # the display rules, with node's own test runner
```

In Docker the `kiosk` service serves it on port 3001, and nginx puts the API on the same address.
