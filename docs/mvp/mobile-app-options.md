# Mobile apps: what each one can do

A note of the options in the two Flutter apps for the demo (21 October 2026). It lists what the apps do now, not what is planned.

## Driver app

**First launch**
- Choose a language, English or Sinhala (once; changeable in Me).
- Sign in with email or staff ID and password. Only driver accounts are accepted.

**Duty tab**
- Today's date, unit, shift times and journeys, with an Active and Past switch.
- Journey tags: Next (blue), Running (green), Done (dark), Later (grey).
- Banner with the newest new alert from control; a severe network alert is a solid red banner.
- Sign on to duty (from 30 minutes before), Start journey, Sign off duty (with confirmation).
- Past duties for the last 30 days, with "Needs review" or "Signed off".
- States for no duty assigned, duty not current, and duty could not load.

**Journey screen**
- Next stop (or at stop) card with a time estimate and a delay capsule.
- Stop timeline: departed, current, ahead.
- One tap: Arrived at X, then Departed X. A wrong stop is explained and the right one is offered.
- Delay reason (signal fault, door fault, crew, passenger incident, track, other). It is also sent to control.
- Not stopping here, with a reason (platform closed, instructed by control, safety).
- Help (blue button): medical, overcrowding, unsafe passenger situation, other; confirm; request sent.
- Journey complete summary.

**Alerts tab**
- Swipe or tap between Direct (messages to this driver, acknowledged in the app) and Network (notices control published for everyone).
- Each alert is tagged with where it came from. Only the Severe capsule is red.
- Unread alerts have a bold heading and a green border and sit above read ones. A red number on the tab counts them.

**Me tab**
- Name and email.
- Language (English, Sinhala).
- Report a technical issue (app, phone or signal, train equipment, other, with a note). Sent to control, not saved on the phone.
- Emergency contacts: police 119, ambulance 1990, fire 110.
- Shared-phone notice, Sign out (with confirmation).

**Reaches the controller:** delay reasons, technical issues and help requests appear in the console under Driver messages, Driver reports. The controller can publish one to passengers or mark it handled.

**Left out on purpose:** undo, coach letter, PIN, fingerprint, text size, location settings, offline queue, hand-over, push notifications, Tamil.

## Passenger app

**First launch**
- Launch screen and onboarding, with a language choice (English, Sinhala).
- No account is needed to browse. Sign in or create a profile only for tickets.

**Home**
- Followed stations and followed trains with their live status.

**Stations and departures**
- Search for a station.
- Departures board for a station, with live status (on time, delayed, cancelled, platform changed).
- Food menu for stations that have a caterer.

**Journey**
- Journey details with stops and live status.
- Follow a journey to get alerts.

**Plan a trip**
- Choose from and to, depart or arrive time.
- Results: direct trains and one-change options.

**Tickets** (signed in)
- Choose a ticket, choose seats where the train reserves them, review, and get the ticket.
- Ticket list (active and past), a ticket with its QR code, add a ticket by its code.

**Alerts tab**
- Alerts for followed trains (delay, cancellation, platform change).
- Disruptions published by control, with a detail page that lists the affected trains.

**Me tab**
- Language, text size, notifications.
- Download my data, delete my data.
- Call centre.
- Sign out.

**Always**
- Offline banner and "last updated" times when the API cannot be reached.
- Times shown in the network time zone (Asia/Colombo).
