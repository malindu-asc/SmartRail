# Interview — Accessibility specialist (public transport)

**Simulated by:** Claude subagent · **Date:** 2026-10-06 · **Persona:** Accessibility specialist who has worked on public transport information and speaks for passengers with low vision, blindness, limited mobility, hearing loss, cognitive differences and limited language.

## Who I am
I have spent years helping transport operators work out why disabled passengers still cannot use what was built "to standard". I know WCAG, but I mostly test with real people in real stations. I care about the person who is stressed, carrying bags, and has about ten seconds to find out whether to run.

## Answers

### Q1. A normal day
I spend most of it with passengers, or with the teams who build for them. Typically I walk a station with someone who has low vision or uses a wheelchair, and we try to get one journey done: find the train, find the platform, get on. Then I sit with designers and developers and show them where it fell apart. I review new screens, signage and apps before release. I also handle complaints, and those are almost always "I could not tell what was happening", not "the system was down".

### Q2. What breaks
The usual failures are contrast and size on screens, which people cannot read from distance or in sun. Apps that look fine but do not work with a screen reader come next. Information that changes without telling anyone is another. Lifts or step-free routes are the big one, because a lift out of service is a trip ruined, and nobody tells the passenger until they are on the platform. When it breaks, people ask a staff member, if one is there. So staff must be able to see and say the same thing the screen says. I'm drawing on real practice here.

### Q3. What I need to see, and when
People need the platform and the time, first, and large. They need step-free route status for the stations on their journey, before they leave. They need changes announced in more than one way: visual, audible, and text that assistive technology can read. For a passenger already on the move, a change must be unmissable but not overwhelming. Plain wording matters too. "Platform 3, 10:42, to Galle, delayed 8 minutes" beats clever phrasing.

### Q4. What must never happen
Never convey anything by colour alone. Never let a change happen silently. Never trap someone in a flow with a timeout they cannot extend. Never show a cancelled or changed train in a way that looks the same as an ordinary one. Never show a confident wrong platform. No flashing content. Never make the terminal the only way to get something, such as buying or checking a ticket.

### Q5. If the system lied to me
Honestly, I would stop telling people to rely on it. For my own testing I would record it with timestamps and compare it to a staff announcement. A passenger who has been wrongly told "platform 2" and cannot easily run to platform 5 is hurt far more than another passenger. So a wrong display is a safety-of-access issue, not a cosmetic one. I would want a visible "last updated" and a clear fallback to staff.

### Probe 1. Display readable at eight metres, in glare, in a crowd
From my experience: the text must be large enough that the key line reads at that distance, which in practice means a very large, simple departure line, not a table of small text. Contrast has to be very high; light text on dark usually survives glare better, and the screen itself must not be glossy. The important item (next train, platform, delay) must come first and stay in a fixed place so people learn where to look. Do not rotate or scroll the information people need. Do not rely on colour for status; add a word or a shape. Keep few items on screen. In a crowd people see only the top rows, so cancellations and platform changes must be at the top, not buried. I'm guessing at exact sizes, but I would test with real people at that distance in sun.

### Probe 2. Screen reader with live updates
What goes wrong is the screen reader either says nothing about the change, or announces everything and talks over the user, or loses their place when the list rearranges. A blind passenger who has just found their train can have the list reshuffle so the item they were on is gone. What must be true: the list does not reorder under them, only important changes are announced, and announcements are polite, not interrupting what they are doing, except for something urgent like a cancellation of the train they follow. Keyboard and focus must stay where they were. Each item needs a proper text equivalent, including status. They need a way to pause updates and refresh when they choose. I'd test it with actual screen readers on a phone, not only automated checkers.

### Probe 3. Wheelchair user at the interactive terminal
The device must be reachable: controls within comfortable reach from a seated position, and a clear space in front of it with knees able to go under, which is a physical installation matter and not just software. The screens must keep important buttons in the lower and middle area, not at the top. Touch targets must be large, with no need for drag, pinch or precision. Timeouts must be long and give warning. A user must be able to do everything without a second person and without needing to read tiny text. The terminal should also work with headphones or audio for blind users, and have a way to reach staff. Do not hide the step-free information on a deep menu: it is the first thing this user needs.

## Extracted by the persona
- **What I need**: Large, high-contrast, fixed-position key information. Multi-channel changes (visual, audible, text). Step-free route and lift status before travel. Plain wording. A visible "last updated". A clear path to staff.
- **What must never happen**: Colour-only meaning. Silent changes. Reordering lists under assistive technology users. Confident wrong platform. Timeouts without warning. Terminal as the only route to something.
- **What would make me stop trusting it**: One confident wrong platform or a missed cancellation. A screen reader that behaves differently from the visual screen. Step-free status that is out of date.
- **What this system seems to be missing**: Step-free and lift status as a first-class item. Any statement about accessibility standards or testing with disabled people. Audible announcements. Staff-assist and fallback behaviour when a display is stale. Plain-language and multilingual wording. Reach and installation requirements for the terminal.
- **Where I disagree with the design as described**: "Intuitive" and "understand quickly" are stated but no accessibility requirement is. Accessibility cannot be a later review step; it has to shape the screens. A terminal "without the app" is fine only if it is genuinely usable by everyone.
- **Confidence**: Medium. I'm drawing on common real practice for displays, screen readers and terminals, but I'm guessing at exact sizes and at what this railway's stations physically look like.
