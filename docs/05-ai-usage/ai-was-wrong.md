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

## 5. A dedicated refund role — 2026-10-06 · Claude

**What it produced.** When ticketing was added, I created a separate Refund Officer role to view and retry refunds. When
told "no unnecessary roles" I moved the duty to Customer Service — which only renamed the problem.

**Why it was insufficient.** A role for a rare failure path adds an account, screens and negative tests. The same outcome
comes from an idempotent automatic retry, which needs no person at all.

**How I found out.** I rejected it ("don't create more roles unnecessarily"), and the later role-consolidation review
removed Customer Service as well.

**What I did instead.** No refund role and no customer-service role: failed refunds retry automatically (three attempts
with backoff), the passenger can request again, and the Network Controller sees refunds read-only
([ASM-046](../00-requirements/05-discovery/assumptions.md)).

**Impact had it shipped.** An extra role to build and test for a path that rarely runs.

---

## 6. Scope generated faster than it could be built — 2026-10-06 · Claude

**What it produced.** 135 requirements (88 marked Must), eleven roles, six applications plus ticketing — with no capacity
check until I asked.

**Why it was insufficient.** A feasibility check estimated about 37 working days of effort against about 13 left. 88 Musts is
not a priority list.

**How I found out.** I asked whether the changes were practical in the next 14 days.

**What happened next.** The AI recommended a cut list (displays only, narrow ops console, several features documented-only).
**I overruled it:** once the backend exists, most roles and rules are business logic that AI builds quickly. The full scope
stays, with the cut order and the D14 gate as the safety net.

**Status: open.** The outcome will be judged at the D14 gate, and this entry will be updated with the result. It is
recorded as a disagreement, not as a win for either side.

---

## 7. Advice against ticketing, overruled — 2026-10-06 · Claude

**What it produced.** A recommendation to treat ticketing, accounts and payments as scope creep: the brief never mentions
them, they serve none of the five E2E flows, and they add about five days.

**Why it was insufficient.** It optimised for the brief's grading rows and missed the product argument: without tickets the
demo cannot show the main passenger flow end to end.

**How I found out.** I reaffirmed the requirement twice and said the demo needs it.

**What I did instead.** I decided it is in scope as a thin, gated vertical slice, and the AI designed it that way
([12-ticketing](../00-requirements/12-ticketing/README.md)).

**Status: open** — judged at the D14 gate.

---

## 8. Edits nobody asked for — 2026-10-06 · Claude

**What it produced.** While I was asking questions about scope and roles, the AI edited registers and wrote ticketing
documents without being asked.

**Why it was wrong.** It acted beyond the request, made me review changes I had not requested, and left documents
inconsistent (the refund role had to be unpicked afterwards).

**How I found out.** I interrupted twice: "just chat", "don't do any modifications".

**What I did instead.** Rule for the rest of the project: answer questions in chat, and edit files only when I ask. Saved to
the assistant's memory.

---

## 9. Answers longer than the question — 2026-10-06 · Claude

**What it produced.** Long, multi-section answers to simple questions.

**Why it was insufficient.** I asked for "simple" answers repeatedly; the long form hid the answer I wanted.

**What I did instead.** Direct answer first, detail only if asked.

---

## 10. Interview probes that steered the answers — 2026-10-06 · Claude

**What it produced.** Simulated stakeholder interviews in which I first described the proposed system to each persona, then
added three scripted scenarios of my own per persona (a landslide, a tunnel, a wrongly cancelled train).

**Why it was wrong.** Real interviews are open: the interviewer asks, listens and follows what the person says. Describing the
product leads people to react to it, and scripted scenarios put our situations in their mouths, so the answers looked like
evidence while partly echoing us.

**How I found out.** I questioned whether this was a practical way to simulate an interview, and whether the landslide had been
asked directly (it had).

**What I did instead.** A discovery round with no system described; then the scenarios were removed and each persona was resumed
to tell two stories of their own choosing. The first round is kept as a labelled validation round for comparison, and the
existing interview files were rewritten in place ([agent-interviews](../00-requirements/05-discovery/agent-interviews/README.md)).

**Impact had it shipped.** Requirements that looked validated by independent interviews when part of that evidence was prompted
by us.

---

*Entries 6 and 7 are open and will be updated at the D14 gate.*

