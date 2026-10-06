# Where AI was wrong or insufficient — and what I did instead

Target: at least eight substantive entries by D19. Each entry states what the AI produced, why it was wrong or
insufficient, how I found out, and what I did instead. Mirrored on the tracker's **AI Failures** sheet.

---

## 1. An over-sized requirement-engineering process — 2026-10-01 · ChatGPT

**What it produced.** A 14-step requirement-engineering process, including a separate "Current-State Analysis"
phase and two large frames for the stakeholder-impact step.

**Why it was insufficient.** Not wrong in principle, but wrong for the situation. The brief describes a fictional
organisation with no documented current process and no client to interview, so a current-state phase could only
output "unknown". And the process was sized for a multi-month engagement, not a 21-day window that also has to
produce six running applications.

**How I found out.** I compared the step count against the remaining calendar and the six-app build scope.

**What I did instead.** Collapsed current state into a short section that states plainly what cannot be known and
why the method was adapted ([03-current-state.md](../00-requirements/03-current-state.md)); consolidated stakeholder
impact into one matrix; timeboxed requirements to a D5–D6 baseline.

**Impact had it shipped.** Polished requirements with half-built applications — the failure mode §25 warns about.

---

## 2. Decisions passed off as assumptions — 2026-10-01 · ChatGPT

**What it produced.** An assumption register in which "SmartRail will have an authoritative source for operational
railway information" was listed as an assumption with status "To validate".

**Why it was wrong.** That is a design decision I am making, not an assumption about something unknown. The register
mixed three kinds of statement — facts stated in the brief, decisions I made, and genuine unknowns — and "To
validate" could never close, because there is no client to validate with.

**How I found out.** I tried to fill in "what breaks if this is wrong" and the honest answer was "nothing — I chose
it".

**What I did instead.** Split the register into facts, user decisions, my decisions and assumptions; moved decisions
to ADRs; introduced closable statuses and a risk-if-wrong column
([assumptions.md](../00-requirements/05-discovery/assumptions.md)).

---

## 3. "Delayed" modelled as a journey state — 2026-10-02 · ChatGPT

**What it produced.** A journey state model `Scheduled → Active → Delayed → Arrived`, with Cancelled, Interrupted and
Terminated as further states.

**Why it was wrong.** Delay is not a lifecycle state. A train is Running *and* late, or Planned and predicted late.
The model could not express "cancelled while running late" or "late, and we've lost the data". It also had no state
for "we don't know", which is the case that matters most for honesty.

**How I found out.** Tried to describe what a passenger should see for a late train whose feed had gone silent, and
the model had nowhere to put it.

**What I did instead.** Three independent dimensions — lifecycle, punctuality, data confidence — composed by the UI
([state-models.md](../00-requirements/04-domain/state-models.md)).

---

## 4. "No conflicts identified" — 2026-10-02 · ChatGPT

**What it produced.** A discovery matrix stating "no explicit conflict identified yet".

**Why it was insufficient.** A system with several writers, several audiences and unreliable connectivity is full of
tensions; §22 explicitly asks for them. Saying there are none signals the analysis did not look.

**What I did instead.** Ten conflicts, each with both sides, a resolution, the reasoning and the losing option
([conflicts-and-ambiguities.md](../00-requirements/05-discovery/conflicts-and-ambiguities.md)).

---

*Entries 5 onward to be added during the build.*
