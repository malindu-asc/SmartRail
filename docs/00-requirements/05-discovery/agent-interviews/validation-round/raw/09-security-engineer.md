# Interview — Security engineer

**Simulated by:** Claude subagent · **Date:** 2026-10-06 · **Persona:** Security engineer reviewing a national transport platform; thinks in trust boundaries, attackers, abuse and blast radius.

## Who I am
I review systems before they go live and I get called when they go wrong. I draw the trust boundaries first and the screens last. I care more about what an authenticated person is allowed to do than about how they logged in. I always ask what one stolen credential or one stolen device can do.

## Answers

### Q1. A normal day
I start with what changed: new endpoints, new roles, new integrations, new devices. I read access logs and alerts for odd patterns, such as one account touching many trains or a device calling from the wrong place. I review code and design changes against the threat model. I check that secrets, keys and device credentials are rotated on schedule, and that leavers lost access. I also chase fixes with product teams. Most of my day is asking "who can do this, and how would we know if the wrong person did?"

### Q2. What breaks, and what do I do
The usual failures are over-broad permissions, forgotten test accounts, long-lived tokens and unpatched devices. When something breaks I contain first: revoke the credential or device, then work out what it touched. That only works if every action is attributable and logged. If it is not, I cannot tell what happened and I have to assume the worst. I then fix the cause, not just the one account. I am guessing at the details for this platform, but this is how it goes in any system.

### Q3. What I need to see, and when
I need a trustworthy audit trail of every consequential action: who, what, which device, when, and the before and after values. I need it immediately, not at the end of the month. I need alerts on bursts of cancellations, platform changes or role changes. I need a list of every device and credential, with last-seen times. I need to see failed authorisation attempts, not just failed logins.

### Q4. What must never happen
A passenger must never be able to see another passenger's tickets, orders or location. Public screens must never show anything operational or personal. No single person should be able to do a high-impact action alone and unseen, such as cancelling a service network-wide. The catering provider must never receive more passenger data than it needs. Secrets and ticket data must never appear in logs. A driver's app must never be able to do control-room actions.

### Q5. If the system lied to me
I would assume tampering until proven otherwise. I would check where the data came from, whether it was signed or authenticated, and whether the audit log shows who changed it. I would freeze the affected feed and compare it against a second source. I'd want to know how far the bad data spread: screens, apps, catering. Then I would treat it as an incident, not a bug.

### Probe 1. A forged train-position message
If nothing stops it, anyone who can reach the feed can move trains on the map, trigger false "arriving" or "cancelled" alerts, and cause chaos. Passengers would be told to the wrong platform, catering would prepare food for trains that are not coming, and a false cancellation could even trigger refunds. It looks like an information problem but it becomes real-world crowding and money loss. I require that the feed source is authenticated and that every message is verified. Replayed old messages must be rejected. The values must be checked for plausibility, for example a train cannot jump across the country. The feed must be rate-limited. Anything rejected is logged and alerted on. I also require that a bad message cannot silently overwrite good state without a trace. I'm guessing at how the simulator is wired, but the feed boundary is the one I would test first.

### Probe 2. A stolen or tampered display or terminal
I assume devices in public places will be touched. Each device needs its own identity and its own narrow credential, never a shared one, so I can revoke one device without touching the rest. The credential should only allow reading public information. A terminal must not hold any passenger data after a session ends, must auto-clear, and must be locked down so a member of the public cannot reach the operating system or other sites. A device that goes quiet, reports from a new place, or fails an integrity check should be flagged. A display showing altered content is a safety-adjacent risk, so staff need a way to see and cut off a bad device quickly. A device must also fail safe: if it cannot verify its data, it should show a clear "information unavailable" message rather than stale content that looks real.

### Probe 3. A compromised staff or admin account
The worst case is an attacker cancelling trains, changing platforms and publishing false disruptions across the whole network, or reading passenger data, or creating more accounts for themselves. What limits it is the least privilege: roles scoped to a job and to a station or area, so one account cannot touch everything. The administrator who manages users should not also be the one who can operate trains. The strongest controls must be applied to the riskiest actions: strong authentication, a second approver or a confirmation for network-wide changes, and short sessions. I want per-role rate limits and alerts on unusual bursts. Every action is logged somewhere the admin cannot edit. Passenger data should be visible only to roles that need it, and each access is logged. I also need a fast way to revoke sessions and devices at once.

## Extracted by the persona
- **What I need**: Authenticated, verified, replay-resistant train-position input. Per-device identities. Least-privilege roles that are scoped to an area. Tamper-resistant audit logs with before and after values. Fast revocation for accounts, sessions and devices. Alerts on bursts of high-impact actions.
- **What must never happen**: One passenger seeing another's data. A single account making unseen network-wide changes. Secrets or ticket data in logs. Public devices exposing anything beyond public information. Extra passenger data going to the catering provider.
- **What would make me stop trusting it**: Missing or editable audit logs. Shared device credentials. An admin role that can do everything. Stale data shown as if it were live. Any sign that a forged message was accepted.
- **What this system seems to be missing**: A stated threat model and trust boundaries. A rule for who can approve high-impact actions. Device lifecycle (enrol, revoke, replace). A data-minimisation rule for the catering integration. Separation between user management and operations. A plan for abuse of the refund path.
- **Where I disagree with the design as described**: The brief treats security as a general principle with no concrete requirements. Operations roles are not defined, so blast radius is unknown. "User and device management" sits in the same console as train control, which concentrates too much power. Driver and ticket-inspection modes sharing one app widens the attack surface.
- **Confidence**: Medium. The principles are standard practice, but I'm guessing at how the specific feed, devices and catering link are built.
