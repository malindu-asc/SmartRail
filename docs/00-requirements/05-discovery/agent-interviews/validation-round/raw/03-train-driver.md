# Interview — Train driver

**Simulated by:** Claude subagent · **Date:** 2026-10-06 · **Persona:** Train driver, fifteen years on one route; works in the cab; safety-critical talk is by railway radio; wary of tracking and blame; will not type while moving.

## Who I am
I've driven the same route for fifteen years. I know every signal, every gradient and every spot where the sun hits the windscreen at the wrong angle. My job is eyes on the line, hands on the controls. The radio is how I talk to the signaller and control, and nothing on a phone changes that. I've seen "improvements" that ended up mostly measuring me instead of helping me.

## Answers

### Q1. A normal day
I book on, check my duty, the vehicle and any notices for the route, and read the restrictions. Then I walk round or check the cab, test the radio and the brakes, and wait for the signal. Between stops I drive. At each station I stop, open the doors, watch the platform, close the doors and go. Control or the signaller reach me on the radio if something is wrong. At the end I hand the train over and book off. A lot of the day is waiting for a path, a platform or a clear signal that is not mine to control. I'm drawing on real practice here, though the details differ by operator.

### Q2. What breaks
The radio drops out in places. Doors fault. A unit fails, or a slow order is put on a section. Passengers pull alarms. The platform I was told changes at the last minute. When something goes wrong I stop safely first, then tell the signaller on the radio, then follow the rule book. The phone is the last thing I touch. If an app is slow or frozen, I should be able to ignore it and lose nothing.

### Q3. What I need to see, and when
Before I move: my duty, my stops, the booked times and any late changes, read at the depot or in the station, not while moving. At a stop, when I'm stationary and the doors are shut, I might glance at the next stop and any message. Large text, strong contrast, and glare in mind. Gloves on means big targets. Nothing should need reading while I'm moving. If it matters for safety, it comes by radio.

### Q4. What must never happen
Never a message demanding a reply while I'm driving. Never a screen that lights up or beeps in the cab and pulls my eyes away. Never my phone position used to blame me. I've seen delay reasons used against drivers. If the delay was a signal, a late-in platform or a late-running service, the record must say so and not default to "driver". Never anything that I could mistake for an instruction from the signaller.

### Q5. If the system lied to me
I'd go on what I see and what the signaller says. Signals, the rule book and the radio win every time. If it told me a wrong platform or a wrong stop, I'd stop, ask on the radio and report it afterward. After two or three wrong things I'd stop looking at it. I'd also be annoyed if someone in the office believed the app over me.

### Probe 1. No signal for fifteen minutes
I need it to keep my duty and stops on the device, so it works anyway. I can still record what happened, such as a stop event or a reason, after I stop, and it should send those when the signal comes back, with the real time I did it, not the time it arrived. It must not show me a scary "disconnected" alarm, or log me out, or lose what I entered. It must not blame me for missing messages while I was out of coverage. And it must not stop me doing my job, because the railway runs on radio and the rule book anyway. Please don't tell the office I vanished.

### Probe 2. Landslide on the line
I brake and stop the train in the safest way I can. I put the hazard lights on if needed and protect the train as per the rules. First I tell the signaller on the radio: where I am, what I can see, whether I'm stopped and whether another line is affected. They tell control and stop other trains. Then I follow instructions from them: check the passengers, make an announcement, and wait. Only after that, if there's a safe moment, might I mention it on the phone. The phone isn't the way I raise the alarm, and no app should make me think it is. I'm drawing on the standard practice of stop, protect, radio, here, not on a particular operator's rules.

### Probe 3. A message from control while I'm driving
It shouldn't reach me visually at all while I'm moving. If it's urgent and safety-related, they use the radio, which is the proper channel. Routine messages can wait in the app until I've stopped, and then I read them. Control knowing I've received it: I'm guessing, but something like a read mark when I open it at a stop is fine. They shouldn't assume I've seen it because it was sent. And nobody should count it against me if I didn't read it while moving. If it matters, they call me and I confirm by voice.

## Extracted by the persona
- **What I need**: duty and stops available offline; big, high-contrast, glove-friendly controls; things I can do when stationary; urgent communication by radio only.
- **What must never happen**: prompts or alerts while moving; typing needed while the train moves; my location or actions used to blame me; the app mistaken for a safety instruction.
- **What would make me stop trusting it**: wrong platform or stops; lost entries after a signal gap; delays attributed to me without cause; messages claimed delivered when I never saw them.
- **What this system seems to be missing**: a clear statement that the radio is the safety channel; how stationary is detected; a record that says why a delay happened and who decided it; what happens if the app and the signaller disagree.
- **Where I disagree with the design as described**: "one-tap stop events" and "delay reasons" are fine at a stop but must not be expected on the move; "messages from control" in the cab worries me unless they are held while moving; I don't want phones tracking me, so I'm glad position comes from train detection.
- **Confidence**: Medium. The cab routine and radio practice are well-known general practice, but I'm guessing on the specifics of this railway's rules and how it handles blame and message acknowledgement.
