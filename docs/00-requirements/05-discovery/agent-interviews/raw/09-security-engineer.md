# Discovery interview — Security engineer

Scenario probes removed; follow-ups chosen by the persona.

**Simulated by:** Claude subagent (discovery method, no system described) · **Date:** 2026-10-06 · **Persona:** Security engineer advising a national railway on a new digital platform for passenger information and operations.

## Who I am
I'm a security engineer. I've reviewed systems that mix public-facing information with operational control. I think in trust boundaries, who can lie to whom, and how far one stolen credential can reach. Login is the easy part; I worry about what an authenticated person or device is allowed to do.

## Answers
### Q1 Normal day
I review designs and change requests, read access logs and alerts, and chase findings from earlier reviews. I spend a lot of time on who has access to what: joiners, movers, leavers, contractors, and shared accounts that should not exist. I sit in meetings where operations asks for speed and I ask what happens when the input is hostile. I check certificate and key expiry, patch status on field devices, and whether anyone is still using default credentials. Honestly I'm guessing at railway specifics; the pattern is the same in most critical-infrastructure work.

### Q2 What breaks
Credentials leak, people share logins to get through a shift, and field devices go unpatched for years because touching them is risky. Integrations break silently and someone adds a permissive exception "temporarily". When it breaks I contain first: revoke the credential or isolate the device, then work out what it touched from the logs. If there are no logs of who did what, I can't scope the damage, and that's the worst position to be in.

### Q3 Information I rely on
I rely on audit logs, an inventory of accounts, roles and devices, and alerts for unusual behaviour. I need them continuously, not after an incident. Today they're scattered across systems, often with different clocks and different identities for the same person. I need to be able to answer "who changed this, from where, and on whose authority" within minutes.

### Q4 Never
A single account must never be able to do everything. Nobody should be able to approve their own sensitive change. A public device must never hold secrets that work elsewhere. Unauthenticated input must never be trusted as operational truth. Audit records must never be editable by the people they describe. Personal data must never be visible to people who have no need for it.

### Q5 Confidently wrong information
I'd first ask where it came from and whether the source can be proved, not whether it looks plausible. Confident and wrong is more dangerous than missing, because people act on it. I'd want to compare it with an independent source, stop it spreading further, and keep a record of what was shown and when. If it was malicious, I treat it as an incident and preserve evidence.

## Follow-ups
### Follow-up 1 — A leaver's account that still worked
I chose this because stale access costs me the most trust. I'm guessing at the details, since I can't name a real organisation. The last time, a contractor had left months earlier. First I saw a login from them in an access-log review, at an odd hour. I checked the account inventory, which showed them as active, then asked their old manager, who said they'd gone in the spring. HR's leaver list and the system's account list were separate, so nobody had told the system. I disabled the account within minutes, but working out what it had touched took about two days, because three systems logged under different usernames. It ended with no proven damage, but I couldn't prove nothing happened. What went wrong: nobody owned the leaver step across systems.

### Follow-up 2 — A change nobody could account for
This is the second costliest, in time and money. A setting on a shared service had been loosened (I'm guessing it was a permissive rule added "temporarily"). I first saw it when a monitoring alert showed traffic from a place that shouldn't have had access. I checked the change record, which had no matching entry, then asked the two teams who could have made it; each said it wasn't them. The shared admin login meant the log showed only the group name. It took about a week to settle, and then only by inference from timing. I tightened the rule the same day, but the cause stayed unproven. It ended with a new rule that every admin change has a named person and a second approver.

## Extracted by the persona
- **What I rely on today**: audit logs, account and device inventories, alerts, access reviews, HR leaver lists, and the people who run each system.
- **What is hardest or most error-prone in my work**: keeping access correct over time (leavers, movers, shared accounts), proving who made a change, and securing old field devices that are hard to patch.
- **What must never happen**: one account doing everything; shared admin logins; forged operational data being trusted; a stolen public device giving network access; editable audit trails.
- **What would make me stop trusting information**: unknown or unauthenticated source, no named person behind a change, no audit trail, disagreement with an independent source, or implausible values.
- **What nobody gives me today that I would want**: one trustworthy, tamper-evident record of who did what across everything, one identity per person across systems, and a quick way to cut off a compromised source, account or device.
- **Confidence**: Medium. The security reasoning is general and solid; railway-specific details and the two stories are my guesses.
