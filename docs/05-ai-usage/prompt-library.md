# Prompt and workflow library

Prompts and workflows that changed an outcome. Not every message — only the ones worth reusing or explaining.
Mirrored on the tracker's **Prompt Library** sheet.

---

## P-01 — Stakeholder interview protocol (requirement discovery)

**Tool:** ChatGPT for authoring, Gemini for review · **Used in:** [stakeholder-interviews.md](../00-requirements/05-discovery/stakeholder-interviews.md)

```
You are <persona from personas.md>. Answer as that person, practically, from experience.
Answer five questions, briefly, in your own words:
1. Walk me through a normal day.
2. What breaks, and what do you do when it does?
3. What do you need to see, and when?
4. What must you never see?
5. What would you do if the system lied to you?
Do not suggest features. Describe your work and what goes wrong.
```

**Why structured this way.** Asking what someone *wants* produces feature lists. Asking what happens when the system
*lies* produces failure requirements. Forbidding feature suggestions keeps the answer about the work.

**Outcome.** Question 5 produced most of the failure-scenario findings, including unacknowledged control messages
(INT-01) and crowd movement on platform changes (INT-03).

---

## P-02 — Adversarial pass

**Tool:** ChatGPT, reviewed by Gemini

```
As <persona>, list the situations in which you would stop trusting SmartRail.
For each: what you saw, what was actually true, and what you did next.
```

**Outcome.** Fed the Unknown state, retraction as a distinct operation, and staleness labelling.

---

## P-03 — Independent review of a model (cross-model)

**Tool:** Gemini reviewing an artifact authored elsewhere

```
Here is a domain model for a railway passenger information system.
List railway situations this model cannot represent. Do not suggest improvements — only failures.
```

**Why structured this way.** "Only failures" stops the reviewer drifting into agreeable rewriting and makes each
answer checkable.

---

## P-04 — Practitioner lens sweep

**Tool:** Claude

```
Read this requirement set as (a) a requirements engineer, (b) a developer who must build it,
(c) a UX designer, (d) a QA engineer, (e) a security engineer, (f) the person on call in production.
For each lens: what would you need that is not written here?
```

**Outcome.** Testability findings (injectable clock, simulator through the real ingestion path), device identity
for unattended displays, and the degraded-mode matrix.
